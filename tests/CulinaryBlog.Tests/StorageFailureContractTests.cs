using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.API;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// B1 (issue #20, N1-7): lỗi hạ tầng object storage phải trả <c>503 storage.unavailable</c>,
/// KHÔNG phải <c>500 server.error</c> — để người dùng biết đây là lỗi tạm thời và retry được.
///
/// Hai cách tái hiện, đều không phụ thuộc Docker nên chạy được trong CI:
/// 1. <see cref="BadCredentialApiFactory"/> — endpoint ĐÚNG nhưng credential SAI (tái hiện đúng
///    <c>Minio__AccessKey=wrong</c> trong issue).
/// 2. <see cref="StorageDownApiFactory"/> — endpoint trỏ cổng không có gì lắng nghe (storage down).
///
/// Thứ tự ưu tiên lỗi cũng được khoá lại ở đây: DB/nghiệp vụ (404/403) và validator file (400)
/// phải thắng lỗi storage (503) — nếu không, người dùng thấy "hệ thống lỗi" cho một file quá lớn.
/// </summary>
/// <summary>
/// Host test dùng chung cho các ca lỗi storage: môi trường Testing, DB test, JWT key giả,
/// cấu hình Minio do lớp con ghi đè. Không sửa <c>ApiFactoryWithMinio</c> (sealed) để giữ nguyên
/// hạ tầng test của tuần 1-3.
/// </summary>
public abstract class StorageFailureApiFactoryBase : WebApplicationFactory<Program>
{
    private static readonly object MigrationLock = new();
    private static bool migrated;

    protected abstract void ConfigureStorage(Dictionary<string, string?> settings);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = EnvFileLoader.Get("TEST_DATABASE",
                    "Host=127.0.0.1;Port=5432;Database=culinary_test;Username=postgres;Password=postgres"),
                ["Jwt:SigningKey"] = new string('t', 64),
                ["Minio:UseSsl"] = "false"
            };
            ConfigureStorage(settings);
            config.AddInMemoryCollection(settings);
        });
        builder.ConfigureServices(_ => { });
    }

    public void EnsureMigrated()
    {
        if (migrated) return;
        lock (MigrationLock)
        {
            if (migrated) return;
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.Migrate();
            migrated = true;
        }
    }
}

/// <summary>Endpoint ĐÚNG nhưng credential SAI — tái hiện đúng <c>Minio__AccessKey=wrong</c> trong issue #20.</summary>
public sealed class BadCredentialApiFactory : StorageFailureApiFactoryBase
{
    protected override void ConfigureStorage(Dictionary<string, string?> settings)
    {
        settings["Minio:Endpoint"] = EnvFileLoader.Get("MINIO_ENDPOINT", "127.0.0.1:9000");
        settings["Minio:AccessKey"] = "wrong-access-key-" + Guid.NewGuid().ToString("N");
        settings["Minio:SecretKey"] = "wrong-secret-key-" + Guid.NewGuid().ToString("N");
        settings["Minio:Bucket"] = EnvFileLoader.Get("MINIO_BUCKET", "culinary-blog");
    }
}

/// <summary>Storage down: endpoint trỏ cổng không có gì lắng nghe (cổng 1) — không phụ thuộc Docker.</summary>
public sealed class StorageDownApiFactory : StorageFailureApiFactoryBase
{
    protected override void ConfigureStorage(Dictionary<string, string?> settings)
    {
        settings["Minio:Endpoint"] = "127.0.0.1:1";
        settings["Minio:AccessKey"] = "minioadmin";
        settings["Minio:SecretKey"] = "minioadmin";
        settings["Minio:Bucket"] = "culinary-blog";
    }
}

/// <summary>Credential rỗng (không phải sai) — đúng trạng thái CI khi không có file <c>.env</c>.</summary>
public sealed class MissingCredentialApiFactory : StorageFailureApiFactoryBase
{
    protected override void ConfigureStorage(Dictionary<string, string?> settings)
    {
        settings["Minio:Endpoint"] = EnvFileLoader.Get("MINIO_ENDPOINT", "127.0.0.1:9000");
        settings["Minio:AccessKey"] = "";
        settings["Minio:SecretKey"] = "";
        settings["Minio:Bucket"] = EnvFileLoader.Get("MINIO_BUCKET", "culinary-blog");
    }
}

public sealed class StorageFailureContractTests : IAsyncLifetime
{
    // Đệm tới `ImageFormats.MinBytes`: validator chặn `file.too_small` trước khi đối chiếu magic
    // bytes, nên payload 22 byte sẽ bị chặn ở ngưỡng kích thước thay vì tới nhánh kiểm tra ảnh.
    private static readonly byte[] JpegBytes = BuildJpeg();

    private static byte[] BuildJpeg()
    {
        byte[] header =
        [
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
                0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9
        ];
        var bytes = new byte[Math.Max(header.Length, (int)ImageFormats.MinBytes)];
        header.CopyTo(bytes, 0);
        return bytes;
    }

    private BadCredentialApiFactory badCredential = null!;
    private StorageDownApiFactory storageDown = null!;
    private MissingCredentialApiFactory missingCredential = null!;

    public Task InitializeAsync()
    {
        badCredential = new BadCredentialApiFactory();
        storageDown = new StorageDownApiFactory();
        missingCredential = new MissingCredentialApiFactory();
        badCredential.EnsureMigrated();
        storageDown.EnsureMigrated();
        missingCredential.EnsureMigrated();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await badCredential.DisposeAsync();
        await storageDown.DisposeAsync();
        await missingCredential.DisposeAsync();
    }

    /// <summary>
    /// Tạo recipe và trả cả <c>slug</c> để đọc đúng endpoint công khai <c>GET /recipes/{slug}</c>.
    /// Tách khỏi <see cref="CreateRecipeAsync"/> vì hàm đó không trả slug, mà recipe vừa tạo là Draft
    /// nên danh sách công khai không có để mà lấy slug gián tiếp.
    /// </summary>
    private static async Task<string> CreateDraftRecipeAndGetSlugAsync(
        StorageFailureApiFactoryBase factory, HttpClient client)
    {
        await AuthorizeAsync(client);

        Guid categoryId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var category = new Category($"Món Storage Fail {Guid.NewGuid():N}",
                $"mon-storage-fail-{Guid.NewGuid():N}", "storage fail test");
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            categoryId = category.Id;
        }

        var created = await client.PostAsJsonAsync("/api/v1/recipes", new
        {
            title = $"Phở storage fail {Guid.NewGuid():N}",
            description = "Công thức cho test hợp đồng lỗi storage.",
            instructions = "",
            prepTimeMinutes = 20,
            cookTimeMinutes = 40,
            servings = 2,
            difficulty = 2,
            categoryId
        });
        var createdBody = await created.Content.ReadAsStringAsync();
        Assert.True(created.IsSuccessStatusCode, $"tạo recipe thất bại {created.StatusCode}: {createdBody}");

        using var doc = JsonDocument.Parse(createdBody);
        return doc.RootElement.GetProperty("data").GetProperty("slug").GetString()!;
    }

    /// <summary>
    /// N2-C1d (chốt hồi quy): thiếu credential object storage KHÔNG được làm hỏng endpoint đọc.
    ///
    /// B5 (issue #24) cho <c>GetRecipeBySlugHandler</c> tiêm <c>IRecipeImageDtoFactory</c> →
    /// <c>IObjectStorageUrlSigner</c> → <c>MinioStorageService</c>. Trước đó service này dựng
    /// <c>MinioClient</c> ngay trong constructor, mà <c>Build()</c> NÉM
    /// <c>MinioException: User Access Credentials not initialized</c> khi credential rỗng ⇒ chỉ cần
    /// thiếu credential là <c>GET /recipes/{slug}</c> trả <c>500 server.error</c>, tức hỏng cả
    /// endpoint không liên quan gì tới ảnh. Lỗi này lọt qua test local vì máy dev có
    /// <c>Minio__*</c> trong <c>.env</c>, còn CI không có <c>.env</c> nên mới đỏ.
    ///
    /// Không có test này thì lỗi quay lại đúng lúc deploy lên môi trường thiếu credential.
    /// </summary>
    [Fact]
    public async Task Recipe_detail_still_readable_when_storage_credentials_are_missing()
    {
        using var client = missingCredential.CreateClient();
        var slug = await CreateDraftRecipeAndGetSlugAsync(missingCredential, client);

        var response = await client.GetAsync($"/api/v1/recipes/{slug}");

        Assert.True(response.IsSuccessStatusCode,
            $"GET /recipes/{slug} phai tra 200 du thieu credential storage, nhung ra {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(slug, body.GetProperty("data").GetProperty("slug").GetString());
    }

    /// <summary>
    /// Đối chiếu với test trên: endpoint <em>có</em> cần storage thì phải báo lỗi TẠM THỜI (503) để
    /// client retry, không phải 500 — cùng nguyên nhân thiếu credential nhưng báo lỗi đúng cách.
    /// </summary>
    [Fact]
    public async Task Upload_with_missing_storage_credentials_returns_503_not_500()
    {
        using var client = missingCredential.CreateClient();
        var (recipeId, _) = await CreateRecipeAsync(missingCredential, client);

        var (status, body) = await UploadAsync(client, recipeId, JpegBytes, "image/jpeg");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
        Assert.Equal("storage.unavailable", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Upload_with_wrong_storage_credentials_returns_503_storage_unavailable()
    {
        using var client = badCredential.CreateClient();
        var (recipeId, _) = await CreateRecipeAsync(badCredential, client);

        var (status, body) = await UploadAsync(client, recipeId, JpegBytes, "image/jpeg");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
        Assert.Equal("storage.unavailable", body.GetProperty("code").GetString());
        // Thông điệp phải nói rõ là lỗi TẠM THỜI để người dùng retry, không phải "lỗi hệ thống".
        Assert.Contains("tạm thời", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Read_media_with_wrong_storage_credentials_returns_503_not_500()
    {
        using var client = badCredential.CreateClient();
        var (recipeId, _) = await CreateRecipeAsync(badCredential, client);

        // Proxy D27 kiểm tra recipe trong DB và quyền TRƯỚC rồi mới đọc storage — phải dùng recipe có thật
        // (và đang là chủ sở hữu) để thực sự chạm tới object storage.
        var response = await client.GetAsync($"/api/v1/resources/images/recipes/{recipeId}/anh-bia.jpg");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("storage.unavailable", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Read_media_of_unknown_recipe_wins_over_storage_failure_returns_404()
    {
        using var client = badCredential.CreateClient();
        await AuthorizeAsync(client);

        var response = await client.GetAsync(
            $"/api/v1/resources/images/recipes/{Guid.NewGuid()}/anh-bia.jpg");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("image.not_found", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Upload_when_storage_is_down_returns_503_storage_unavailable()
    {
        using var client = storageDown.CreateClient();
        var (recipeId, _) = await CreateRecipeAsync(storageDown, client);

        var (status, body) = await UploadAsync(client, recipeId, JpegBytes, "image/jpeg");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
        Assert.Equal("storage.unavailable", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Missing_recipe_wins_over_storage_failure_returns_404()
    {
        using var client = storageDown.CreateClient();
        await AuthorizeAsync(client);

        var (status, body) = await UploadAsync(
            client, Guid.NewGuid(), JpegBytes, "image/jpeg");

        Assert.Equal(HttpStatusCode.NotFound, status);
        Assert.Equal("recipe.not_found", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task File_too_large_wins_over_storage_failure_returns_400_file_too_large()
    {
        using var client = storageDown.CreateClient();
        var (recipeId, _) = await CreateRecipeAsync(storageDown, client);

        // 6 MiB > giới hạn 5 MiB (FR-FILE-002). Validator chạy TRƯỚC khi chạm storage.
        var oversized = new byte[6 * 1024 * 1024];
        Array.Copy(JpegBytes, oversized, JpegBytes.Length);
        var (status, body) = await UploadAsync(client, recipeId, oversized, "image/jpeg");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal("file.too_large", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Upload_with_valid_format_never_leaks_500_server_error_code()
    {
        // Chốt hợp đồng: KHÔNG còn mã server.error cho đường lỗi storage (đây là hồi quy của B1).
        using var client = badCredential.CreateClient();
        var (recipeId, _) = await CreateRecipeAsync(badCredential, client);

        var (_, body) = await UploadAsync(client, recipeId, JpegBytes, "image/jpeg");

        Assert.False(body.TryGetProperty("code", out var code) && code.GetString() == "server.error",
            "Lỗi storage không được trả về code 'server.error' (B1, issue #20).");
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> UploadAsync(
        HttpClient client, Guid recipeId, byte[] bytes, string mime)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(mime);
        using var multipart = new MultipartFormDataContent
        {
            { content, "file", "anh-bia.jpg" },
            { new StringContent("ảnh bìa"), "altText" }
        };

        var response = await client.PostAsync($"/api/v1/recipes/{recipeId}/images", multipart);
        var raw = await response.Content.ReadAsStringAsync();
        JsonElement body;
        using (var doc = JsonDocument.Parse(raw))
            body = doc.RootElement.Clone();
        return (response.StatusCode, body);
    }

    private static async Task AuthorizeAsync(HttpClient client)
    {
        var email = $"tv4-storagefail-{Guid.NewGuid():N}@example.test";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = "Demo-Password9!", displayName = "TV4 Storage Fail" });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var body = await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", body!.Data.AccessToken);
    }

    private static async Task<(Guid RecipeId, Guid CategoryId)> CreateRecipeAsync(
        StorageFailureApiFactoryBase factory, HttpClient client)
    {
        await AuthorizeAsync(client);

        Guid categoryId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var category = new Category($"Món Storage Fail {Guid.NewGuid():N}",
                $"mon-storage-fail-{Guid.NewGuid():N}", "storage fail test");
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            categoryId = category.Id;
        }

        var created = await client.PostAsJsonAsync("/api/v1/recipes", new
        {
            title = $"Phở storage fail {Guid.NewGuid():N}",
            description = "Công thức cho test hợp đồng lỗi storage.",
            instructions = "",
            prepTimeMinutes = 20,
            cookTimeMinutes = 40,
            servings = 2,
            difficulty = 2,
            categoryId
        });
        var createdBody = await created.Content.ReadAsStringAsync();
        Assert.True(created.IsSuccessStatusCode, $"tạo recipe thất bại {created.StatusCode}: {createdBody}");

        var recipe = JsonSerializer.Deserialize<ApiResponse<RecipeDto>>(createdBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        return (recipe.Data.Id, categoryId);
    }
}

/// <summary>
/// B2 (issue #21, N1-7): fail-fast lúc khởi động khi thiếu cấu hình storage.
/// Validate chỉ bắt chuỗi RỖNG — credential sai (có giá trị) vẫn khởi động được và thuộc về #20.
/// </summary>
public sealed class MinioOptionsValidatorTests
{
    private static MinioOptions Valid() => new()
    {
        Endpoint = "127.0.0.1:9000",
        AccessKey = "minioadmin",
        SecretKey = "minioadmin",
        Bucket = "culinary-blog"
    };

    [Fact]
    public void Complete_config_passes_validation()
    {
        var result = new MinioOptionsValidator().Validate(null, Valid());
        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_access_key_fails_and_names_the_env_var(string value)
    {
        var options = Valid();
        options.AccessKey = value;

        var result = new MinioOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Minio__AccessKey", result.FailureMessage);
    }

    [Fact]
    public void Empty_secret_key_fails_and_names_the_env_var()
    {
        var options = Valid();
        options.SecretKey = "";

        var result = new MinioOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Minio__SecretKey", result.FailureMessage);
    }

    [Fact]
    public void Empty_bucket_and_endpoint_also_fail()
    {
        var options = Valid();
        options.Bucket = "";
        options.Endpoint = "";

        var result = new MinioOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Minio__Bucket", result.FailureMessage);
        Assert.Contains("Minio__Endpoint", result.FailureMessage);
    }

    [Fact]
    public void All_missing_problems_are_reported_together_not_one_at_a_time()
    {
        // Người vận hành phải thấy hết biến đang thiếu trong một lần khởi động, không phải sửa lại 3 lần.
        var options = new MinioOptions { Endpoint = "", AccessKey = "", SecretKey = "", Bucket = "" };

        var result = new MinioOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Minio__Endpoint", result.FailureMessage);
        Assert.Contains("Minio__AccessKey", result.FailureMessage);
        Assert.Contains("Minio__SecretKey", result.FailureMessage);
        Assert.Contains("Minio__Bucket", result.FailureMessage);
    }
}
