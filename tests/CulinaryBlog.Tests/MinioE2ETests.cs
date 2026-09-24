using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Recipe = CulinaryBlog.Domain.Entities.Recipe;

namespace CulinaryBlog.Tests;

/// <summary>
/// E2E (D1.3/D1.1c, TV4): chạy thật qua MinIO. Cần MinIO local (docker compose -f docker-compose.dev.yml up -d minio minio-init)
/// hoặc service MinIO trong CI. Nếu MinIO không sẵn sàng, bỏ qua để không phá CI/local không có MinIO.
/// </summary>
public sealed class ApiFactoryWithMinio : WebApplicationFactory<Program>
{
    private static readonly object MigrationLock = new();
    private static bool migrated;

    public static string MinioEndpoint =
        Environment.GetEnvironmentVariable("MINIO_ENDPOINT") ?? "127.0.0.1:9000";
    public static string MinioAccess =
        Environment.GetEnvironmentVariable("MINIO_ACCESS") ?? "minioadmin";
    public static string MinioSecret =
        Environment.GetEnvironmentVariable("MINIO_SECRET") ?? "minioadmin";
    public static string MinioBucket =
        Environment.GetEnvironmentVariable("MINIO_BUCKET") ?? "culinary-blog";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = Environment.GetEnvironmentVariable("TEST_DATABASE")
                ?? "Host=127.0.0.1;Port=5432;Database=culinary_test;Username=postgres;Password=admin123",
            ["Jwt:SigningKey"] = new string('t', 64),
            ["Minio:Endpoint"] = MinioEndpoint,
            ["Minio:AccessKey"] = MinioAccess,
            ["Minio:SecretKey"] = MinioSecret,
            ["Minio:Bucket"] = MinioBucket,
            ["Minio:UseSsl"] = "false"
        }));
        builder.ConfigureServices(services => { });
    }

    public void EnsureMigrated()
    {
        if (migrated) return;
        lock (MigrationLock)
        {
            if (migrated) return;
            try
            {
                using var scope = Services.CreateScope();
                scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.Migrate();
            }
            catch
            {
                // Ignored if already created or degraded state
            }
            finally
            {
                migrated = true;
            }
        }
    }

    /// <summary>Kiểm tra nhanh MinIO live (D1.1c: down => trả false để skip test).</summary>
    public static async Task<bool> MinioIsReachableAsync(CancellationToken ct = default)
    {
        try
        {
            using var tcp = new System.Net.Sockets.TcpClient();
            var hostPort = MinioEndpoint.Split(':');
            var host = hostPort[0];
            var port = hostPort.Length > 1 ? int.Parse(hostPort[1]) : 9000;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
            await tcp.ConnectAsync(host, port, linked.Token);
            return tcp.Connected;
        }
        catch
        {
            return false;
        }
    }
}

public sealed class MinioE2ETests : IAsyncLifetime
{
    private readonly ApiFactoryWithMinio factory;
    private readonly HttpClient client;
    private readonly bool minioUp;
    private string? accessToken;
    private Guid categoryId;

    public MinioE2ETests()
    {
        factory = new ApiFactoryWithMinio();
        client = factory.CreateClient();
        factory.EnsureMigrated();
        minioUp = ApiFactoryWithMinio.MinioIsReachableAsync().GetAwaiter().GetResult();
    }

    public Task DisposeAsync()
    {
        client.Dispose();
        return factory.DisposeAsync().AsTask();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    private async Task<(string Email, string Password)> RegisterAuthorAsync()
    {
        var email = $"tv4-e2e-{Guid.NewGuid():N}@example.test";
        var password = "Demo-Password9!";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password, displayName = "TV4 E2E" });
        var registerBody = await register.Content.ReadAsStringAsync();
        if (register.StatusCode != HttpStatusCode.Created)
            throw new InvalidOperationException($"register failed {register.StatusCode}: {registerBody}");

        // Token từ register chứa role claim (giống AuthTests) — login endpoint không xài.
        var apiRes = JsonSerializer.Deserialize<ApiResponse<AuthResponse>>(registerBody, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        accessToken = apiRes.Data.AccessToken;
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

        // Debug: xác nhận roles gắn trên tài khoản thực sự có Author.
        var me = await client.GetAsync("/api/v1/auth/me");
        var meBody = await me.Content.ReadAsStringAsync();
        if (!me.IsSuccessStatusCode)
            throw new InvalidOperationException($"me failed {me.StatusCode}: {meBody}");
        return (email, password);
    }

    private async Task SeedCategoryAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var category = new Category("Món Việt E2E", $"mon-viet-e2e-{Guid.NewGuid():N}", "E2E MinIO");
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        categoryId = category.Id;
    }

    private static readonly byte[] JpegBytes =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
        0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9
    ];

    [Fact]
    public async Task Upload_and_readback_recipe_image_e2e_across_minio()
    {
        if (!minioUp)
        {
            // D1.1c/D27 phụ thuộc hạ tầng MinIO — bỏ qua khi không có (CI hoặc local không dựng MinIO).
            return;
        }

        await RegisterAuthorAsync();
        await SeedCategoryAsync();

        var created = await client.PostAsJsonAsync("/api/v1/recipes", new
        {
            title = $"Phở bò E2E {Guid.NewGuid():N}",
            description = "Phở bò chuẩn vị cho E2E.",
            instructions = "",
            prepTimeMinutes = 20,
            cookTimeMinutes = 40,
            servings = 2,
            difficulty = 2,
            categoryId
        });
        created.EnsureSuccessStatusCode();
        var recipeRes = await created.Content.ReadFromJsonAsync<ApiResponse<RecipeDto>>();
        var recipe = recipeRes!.Data;

        // Upload ảnh thật qua API (multipart) -> MinIO
        var jpegContent = new ByteArrayContent(JpegBytes);
        jpegContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        using var content = new MultipartFormDataContent
        {
            { jpegContent, "file", "anh-bia.jpg" },
            { new StringContent("Ảnh bìa phở bò E2E"), "altText" }
        };
        var upload = await client.PostAsync($"/api/v1/recipes/{recipe.Id}/images", content);
        var uploadRaw = await upload.Content.ReadAsStringAsync();
        Assert.True(upload.IsSuccessStatusCode, $"upload {upload.StatusCode}: {uploadRaw}");
        var uploadBody = await upload.Content.ReadFromJsonAsync<ApiResponse<RecipeImageSummaryDto>>();
        var image = uploadBody!.Data;
        Assert.True(image.IsPrimary, "Ảnh đầu tiên phải tự thành primary.");
        Assert.StartsWith($"recipes/{recipe.Id}/", image.OriginalUrl);
        Assert.EndsWith(".jpg", image.OriginalUrl);

        // Đọc lại detail qua API: ảnh phải nằm trong danh sách Images (E2E readback)
        var detail = await client.GetFromJsonAsync<ApiResponse<RecipeDetailDto>>($"/api/v1/recipes/{recipe.Slug}");
        Assert.NotNull(detail!.Data);
        Assert.Contains(detail.Data.Images, i => i.Id == image.Id);
        Assert.True(detail.Data.Images.Single().IsPrimary);

        // D1.1c nửa tích cực: DELETE ảnh cũng đi qua MinIO thật (object bị xoá)
        var del = await client.DeleteAsync($"/api/v1/recipes/{recipe.Id}/images/{image.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var detailAfter = await client.GetFromJsonAsync<ApiResponse<RecipeDetailDto>>($"/api/v1/recipes/{recipe.Slug}");
        Assert.Empty(detailAfter!.Data.Images);
    }

    [Fact]
    public async Task Delete_recipe_image_removes_object_from_minio()
    {
        if (!minioUp) return;

        await RegisterAuthorAsync();
        await SeedCategoryAsync();

        var created = await client.PostAsJsonAsync("/api/v1/recipes", new
        {
            title = $"Bún chả E2E {Guid.NewGuid():N}",
            description = "Bún chả cho E2E.",
            instructions = "",
            prepTimeMinutes = 10,
            cookTimeMinutes = 20,
            servings = 1,
            difficulty = 1,
            categoryId
        });
        created.EnsureSuccessStatusCode();
        var recipe = (await created.Content.ReadFromJsonAsync<ApiResponse<RecipeDto>>())!.Data;

        var jpeg1 = new ByteArrayContent(JpegBytes);
        jpeg1.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        using var content = new MultipartFormDataContent
        {
            { jpeg1, "file", "anh-1.jpg" }
        };
        var upload = await client.PostAsync($"/api/v1/recipes/{recipe.Id}/images", content);
        var image = (await upload.Content.ReadFromJsonAsync<ApiResponse<RecipeImageSummaryDto>>())!.Data;

        // Xoá ảnh thật: MinIO.DeleteObject phải chạy (không lỗi)
        var del = await client.DeleteAsync($"/api/v1/recipes/{recipe.Id}/images/{image.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact]
    public async Task Unpublish_recipe_is_still_viewable_by_owner_and_archived_by_public()
    {
        if (!minioUp) return;

        await RegisterAuthorAsync();
        await SeedCategoryAsync();

        var created = await client.PostAsJsonAsync("/api/v1/recipes", new
        {
            title = $"Cá kho tộ E2E {Guid.NewGuid():N}",
            description = "Cá kho cho lifecycle E2E.",
            instructions = "",
            prepTimeMinutes = 15,
            cookTimeMinutes = 30,
            servings = 3,
            difficulty = 3,
            categoryId
        });
        created.EnsureSuccessStatusCode();
        var recipe = (await created.Content.ReadFromJsonAsync<ApiResponse<RecipeDto>>())!.Data;

        // Publish
        var pub = await client.PatchAsync($"/api/v1/recipes/{recipe.Id}/publish", new StringContent("{}"));
        Assert.Equal(HttpStatusCode.OK, pub.StatusCode);

        // Anonymous reader: publish -> thấy; unpublish -> 404; archive -> 404
        var anon = factory.CreateClient(); // no auth
        var readable = await anon.GetAsync($"/api/v1/recipes/{recipe.Slug}");
        Assert.Equal(HttpStatusCode.OK, readable.StatusCode);

        await client.PatchAsync($"/api/v1/recipes/{recipe.Id}/unpublish", new StringContent("{}"));
        var hidden = await anon.GetAsync($"/api/v1/recipes/{recipe.Slug}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        await client.PatchAsync($"/api/v1/recipes/{recipe.Id}/archive", new StringContent("{}"));
        var archivedHidden = await anon.GetAsync($"/api/v1/recipes/{recipe.Slug}");
        Assert.Equal(HttpStatusCode.NotFound, archivedHidden.StatusCode);

        // Owner vẫn xem được sau khi archive (404 cho người khác nhưng không phải public)
        var ownerView = await client.GetAsync($"/api/v1/recipes/{recipe.Slug}");
        Assert.Equal(HttpStatusCode.OK, ownerView.StatusCode);
    }
}
