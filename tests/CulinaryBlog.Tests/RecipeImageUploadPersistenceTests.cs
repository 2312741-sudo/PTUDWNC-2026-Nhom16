using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// C3 — Tải ảnh vào recipe đã có sẵn trong DB phải INSERT dòng RecipeImages mới.
/// Lỗi cũ: RecipeImage.Id sinh ở domain nhưng EF coi là ValueGeneratedOnAdd -> ảnh mới trong collection bị coi là
/// dòng đã tồn tại, phát UPDATE (0 dòng) -> DbUpdateConcurrencyException -> 422 recipe.version_conflict.
/// Chạy trên Postgres thật (ApiFactory + migration thật); chỉ thay IFileStorageService để loại MinIO.
/// </summary>
public sealed class RecipeImageUploadPersistenceTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    private readonly ApiFactory _factory;
    private readonly WebApplicationFactory<Program> _app;

    public RecipeImageUploadPersistenceTests(ApiFactory factory)
    {
        _factory = factory;
        factory.EnsureMigrated();
        _app = factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFileStorageService>();
            services.AddScoped<IFileStorageService, InMemoryFileStorage>();
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

    private sealed record ImageRow(Guid Id, bool IsPrimary, int OrderIndex);

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

    private async Task<(HttpClient Client, string Email, string Password)> NewAuthorClient()
    {
        var client = _app.CreateClient();
        var cmd = new RegisterCommand($"tv3-img-{Guid.NewGuid():N}@example.test", "Recipe-Flow9!", "Huỳnh Quốc Trung TV3");
        var res = await client.PostAsJsonAsync("/api/v1/auth/register", cmd);
        await AssertStatus(HttpStatusCode.Created, res);
        var auth = (await res.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, cmd.Email, cmd.Password);
    }

    /// <summary>Lấy 1 danh mục có sẵn; DB test chưa seed thì tạo bằng tài khoản Admin.</summary>
    private async Task<Guid> AnyCategoryId()
    {
        var list = await DataOf(await _app.CreateClient().GetAsync("/api/v1/categories"));
        var items = list.ValueKind == JsonValueKind.Array ? list
            : list.TryGetProperty("items", out var it) ? it : default;
        if (items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
            return items[0].GetProperty("id").GetGuid();

        var (client, email, password) = await NewAuthorClient();
        using (var scope = _app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var added = await users.AddToRoleAsync((await users.FindByEmailAsync(email))!, Roles.Admin);
            Assert.True(added.Succeeded, string.Join("; ", added.Errors.Select(e => e.Description)));
        }
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        await AssertStatus(HttpStatusCode.OK, login);
        var adminToken = (await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var ctor = typeof(CreateCategoryCommand).GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var args = ctor.GetParameters().Select(p => p.Name switch
        {
            "Name" => (object?)$"Danh mục test {Guid.NewGuid():N}"[..30],
            "Description" => "Tạo bởi RecipeImageUploadPersistenceTests",
            _ => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null,
        }).ToArray();
        var created = await client.PostAsJsonAsync("/api/v1/categories", (CreateCategoryCommand)ctor.Invoke(args));
        await AssertStatus(HttpStatusCode.Created, created);
        return (await DataOf(created)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateRecipe(HttpClient client, Guid categoryId)
    {
        var res = await client.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeCommand(
            $"Bánh xèo {Guid.NewGuid():N}"[..30], "Bánh xèo miền Tây", "Đổ bánh theo các bước",
            20, 15, 4, RecipeDifficulty.Easy, categoryId, null));
        await AssertStatus(HttpStatusCode.Created, res);
        return (await DataOf(res)).GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> UploadPng(HttpClient client, Guid recipeId, string altText)
    {
        var file = new ByteArrayContent(TinyPng);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent
        {
            { file, "file", "anh.png" },
            { new StringContent(altText), "altText" },
        };
        return client.PostAsync($"/api/v1/recipes/{recipeId}/images", form);
    }

    /// <summary>Đọc thẳng bảng RecipeImages bằng scope mới.</summary>
    private async Task<List<ImageRow>> ImagesInDb(Guid recipeId)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await db.RecipeImages.IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.RecipeId == recipeId)
            .OrderBy(i => i.OrderIndex).ThenBy(i => i.CreatedAt)
            .Select(i => new ImageRow(i.Id, i.IsPrimary, i.OrderIndex))
            .ToListAsync();
    }

    [Fact]
    public async Task Upload_to_existing_recipe_inserts_rows_first_is_primary_second_is_not()
    {
        var (client, _, _) = await NewAuthorClient();
        var recipeId = await CreateRecipe(client, await AnyCategoryId());
        Assert.Empty(await ImagesInDb(recipeId));

        var first = await UploadPng(client, recipeId, "Ảnh thứ nhất");
        await AssertStatus(HttpStatusCode.Created, first);
        var firstDto = await DataOf(first);
        Assert.True(firstDto.GetProperty("isPrimary").GetBoolean());

        var afterFirst = await ImagesInDb(recipeId);
        var row1 = Assert.Single(afterFirst);
        Assert.Equal(firstDto.GetProperty("id").GetGuid(), row1.Id);
        Assert.True(row1.IsPrimary, "Ảnh đầu tiên phải là ảnh chính.");

        var second = await UploadPng(client, recipeId, "Ảnh thứ hai");
        await AssertStatus(HttpStatusCode.Created, second);
        var secondId = (await DataOf(second)).GetProperty("id").GetGuid();

        var afterSecond = await ImagesInDb(recipeId);
        Assert.Equal(2, afterSecond.Count);
        Assert.True(afterSecond.Single(r => r.Id == row1.Id).IsPrimary);
        Assert.False(afterSecond.Single(r => r.Id == secondId).IsPrimary, "Ảnh thứ hai không được là ảnh chính.");
        Assert.Equal(1, afterSecond.Count(r => r.IsPrimary));
    }
}
