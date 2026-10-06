using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// Bảo vệ `DbSeeder` không ghi đè URL ảnh thật của người dùng.
///
/// Lỗi: điều kiện lọc ảnh mẫu là
/// <c>Contains("photo-...") || !OriginalUrl.StartsWith("/images/recipes/")</c>. Vế thứ hai khớp
/// LUÔN khoá ảnh do app tải lên (<c>recipes/{id}/{uuid}.ext</c>) và mọi URL tuyệt đối qua proxy D27.
/// Vì `DbSeeder` chạy ở mỗi lần API khởi động, ảnh người dùng bị đổi thành
/// <c>/images/recipes/{slug}.jpg</c> ⇒ vỡ ở trang chi tiết. Báo bởi TV3
/// (<c>docs/evidence/TV3/TV3_BAN_GIAO_TUAN4.md</c> §C.1).
///
/// Chạy trên Postgres thật (ApiFactory + migration thật) vì cần đọc lại URL từ DB sau khi seed.
/// Storage và hàng đợi resize thay bằng bản giả để không cần MinIO.
/// </summary>
public sealed class DbSeederUserImageUrlTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    private readonly ApiFactory _factory;
    private readonly WebApplicationFactory<Program> _app;
    private readonly FakeImageResizeQueue _resizeQueue = new();

    public DbSeederUserImageUrlTests(ApiFactory factory)
    {
        _factory = factory;
        factory.EnsureMigrated();
        _app = factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFileStorageService>();
            services.AddScoped<IFileStorageService, InMemoryFileStorage>();
            services.RemoveAll<IImageResizeQueue>();
            services.AddSingleton<IImageResizeQueue>(_resizeQueue);
        }));
    }

    private sealed class InMemoryFileStorage : IFileStorageService
    {
        public Task<StoredFile> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default)
        {
            var key = $"{folder.Trim('/')}/{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
            return Task.FromResult(new StoredFile(key, key, contentType, content.Length));
        }

        public Task DeleteAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static async Task<JsonElement> DataOf(HttpResponseMessage res)
    {
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    private static async Task AssertStatus(HttpStatusCode expected, HttpResponseMessage res)
    {
        if (res.StatusCode != expected)
            Assert.Fail($"{res.RequestMessage?.Method} {res.RequestMessage?.RequestUri} -> {(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
    }

    private async Task<HttpClient> NewAuthorClient()
    {
        var client = _app.CreateClient();
        var cmd = new RegisterCommand($"seeder-url-{Guid.NewGuid():N}@example.test", "Recipe-Flow9!", "Nhanh Son TV4");
        var res = await client.PostAsJsonAsync("/api/v1/auth/register", cmd);
        await AssertStatus(HttpStatusCode.Created, res);
        var auth = (await res.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    private async Task<Guid> AnyCategoryId()
    {
        var list = await DataOf(await _app.CreateClient().GetAsync("/api/v1/categories"));
        var items = list.ValueKind == JsonValueKind.Array ? list
            : list.TryGetProperty("items", out var it) ? it : default;
        return items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0
            ? items[0].GetProperty("id").GetGuid()
            : throw new InvalidOperationException("DB test chưa có danh mục nào để tạo công thức.");
    }

    private static async Task<Guid> CreateRecipe(HttpClient client, Guid categoryId)
    {
        var res = await client.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeCommand(
            $"Bánh xèo {Guid.NewGuid():N}"[..30], "Bánh xèo miền Tây", "Dễ bánh theo các bước",
            20, 15, 4, RecipeDifficulty.Easy, categoryId, null));
        await AssertStatus(HttpStatusCode.Created, res);
        return (await DataOf(res)).GetProperty("id").GetGuid();
    }

    private static async Task UploadPng(HttpClient client, Guid recipeId, string altText)
    {
        var file = new ByteArrayContent(TinyPng);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent
        {
            { file, "file", "anh.png" },
            { new StringContent(altText), "altText" },
        };
        await AssertStatus(HttpStatusCode.Created, await client.PostAsync($"/api/v1/recipes/{recipeId}/images", form));
    }

    private async Task<List<(Guid Id, string OriginalUrl)>> ImagesInDb(Guid recipeId)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await db.RecipeImages.IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.RecipeId == recipeId)
            .OrderBy(i => i.OrderIndex)
            .Select(i => new ValueTuple<Guid, string>(i.Id, i.OriginalUrl))
            .ToListAsync();
    }

    private async Task ReseedAsync()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await DbSeeder.SeedAsync(db);
    }

    [Fact]
    public async Task Seeding_does_not_overwrite_the_url_of_a_user_uploaded_image()
    {
        var client = await NewAuthorClient();
        var recipeId = await CreateRecipe(client, await AnyCategoryId());
        await UploadPng(client, recipeId, "ảnh nhà");

        var before = Assert.Single(await ImagesInDb(recipeId));
        // Khoá ảnh do app sinh: `recipes/{recipeId}/{uuid}.png` — KHÔNG phải ảnh mẫu.
        Assert.StartsWith($"recipes/{recipeId}/", before.OriginalUrl);
        Assert.False(before.OriginalUrl.StartsWith("/images/recipes/"));

        await ReseedAsync();
        await ReseedAsync();

        var after = Assert.Single(await ImagesInDb(recipeId));
        Assert.Equal(before.OriginalUrl, after.OriginalUrl);
    }

    [Fact]
    public async Task Seeding_does_not_overwrite_the_url_of_a_user_uploaded_image_behind_the_proxy()
    {
        var client = await NewAuthorClient();
        var recipeId = await CreateRecipe(client, await AnyCategoryId());
        await UploadPng(client, recipeId, "ảnh nhà 2");

        var before = Assert.Single(await ImagesInDb(recipeId));

        // Trường hợp này không đi qua upload endpoint: URL tuyệt đối của proxy D27 do người dùng
        // dán vào (điều kiện cũ khớp mọi thứ không bắt đầu bằng `/images/recipes/`).
        var absolute = $"http://localhost:5080/api/v1/resources/images/{Guid.NewGuid():N}";
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var image = await db.RecipeImages.FirstAsync(i => i.Id == before.Id);
            image.SetOriginalUrl(absolute);
            await db.SaveChangesAsync();
        }

        await ReseedAsync();

        var after = Assert.Single(await ImagesInDb(recipeId));
        Assert.Equal(absolute, after.OriginalUrl);
    }
}
