using System.Net;
using System.Net.Http.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
// main có thêm class CulinaryBlog.Domain.Recipe (discovery) — chỉ định tường minh entity DDD.
using Recipe = CulinaryBlog.Domain.Entities.Recipe;

namespace CulinaryBlog.Tests;

/// <summary>
/// W5-10 (TV4 09/10) — NFR-SEC-004: test path traversal `../` trong tên file ảnh.
///
/// Củng cố contract: client CHỈ gửi FileName hiển thị; object key do server sinh
/// (folder `recipes/{recipeId}` + tên do server đặt theo MIME phát hiện + UUID) — nên `../`
/// không thể thoát khỏi prefix của recipe. Proxy `GET /api/v1/resources/images/{key}` chỉ đọc
/// object đúng tên (namespace S3 phẳng) → key chứa `../` không phân giải, trả 404.
/// </summary>
public sealed class PathTraversalSecurityTests
{
    private static Recipe NewDraftRecipe() =>
        Recipe.CreateDraft(
            title: "Phở bò xuyên tường",
            slug: $"pho-bo-{Guid.NewGuid():N}",
            description: "Test bảo mật path traversal.",
            instructions: null,
            prepTimeMinutes: 30,
            cookTimeMinutes: 120,
            servings: 2,
            difficulty: RecipeDifficulty.Medium,
            categoryId: Guid.NewGuid(),
            authorId: "author-1");

    private static Stream JpegStream()
    {
        var bytes = new byte[Math.Max(16, (int)ImageFormats.MinBytes)];
        new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }.CopyTo(bytes, 0);
        return new MemoryStream(bytes);
    }

    private static RecipeImageDtoFactory DtoFactory() => new(new FakeObjectStorageUrlSigner());

    [Theory]
    [InlineData("../../etc/passwd.jpg")]
    [InlineData("..\\..\\Windows\\system32\\config\\SAM.jpg")]
    [InlineData("images/../../../secret.png")]
    [InlineData("..")]
    [InlineData("normal.jpg")]
    public async Task Upload_server_derives_key_ignoring_client_filename(string clientFileName)
    {
        var recipe = NewDraftRecipe();
        var repo = new FakeRecipeImageRepository();
        repo.Store.Add(recipe);
        var storage = new RecordingFileStorageService();
        var queue = new FakeImageResizeQueue();
        var handler = new UploadRecipeImageHandler(repo, storage, new FakeCurrentUser("author-1"), queue, DtoFactory());

        var dto = await handler.Handle(new UploadRecipeImageCommand(
            recipe.Id, clientFileName, "image/jpeg", "Ảnh", ImageFormats.MinBytes, JpegStream()), CancellationToken.None);

        var recorded = Assert.Single(storage.Uploaded);
        // 1) Folder là prefix do server chọn — không phụ thuộc tên client.
        Assert.Equal($"recipes/{recipe.Id}", recorded.Folder);
        // 2) Key + tên đối tượng do server đặt: folder cố định + `image{ext}` — KHÔNG chứa thư mục ảo từ input.
        Assert.Equal($"recipes/{recipe.Id}/image.jpg", recorded.Key);
        // 3) Handler truyền xuống storage tên server-đặt (`image{ext}`), không phải FileName client.
        Assert.Equal("image.jpg", recorded.FileName);
        // 4) Key lưu trữ = key trả về = key enqueue resize.
        Assert.Equal(recorded.Key, dto.OriginalUrl);
        var enqueued = Assert.Single(queue.Enqueued);
        Assert.Equal(recorded.Key, enqueued.OriginalKey);
    }

    /// <summary>Bản storage ghi lại cả `fileName` để assert handler không dùng tên client làm path.</summary>
    private sealed class RecordingFileStorageService : IFileStorageService
    {
        public readonly List<(string Key, string Folder, string FileName)> Uploaded = [];

        public Task<StoredFile> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default)
        {
            var key = $"{folder.Trim('/')}/image{Path.GetExtension(fileName)}";
            Uploaded.Add((key, folder, fileName));
            return Task.FromResult(new StoredFile(key, key, contentType, content.Length));
        }

        public Task DeleteAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    }

    // ---------------- Tích hợp: proxy ảnh D27 với key chứa `../` ----------------

    private readonly ApiFactoryWithMinio factory;
    private readonly HttpClient ownerClient;
    private readonly bool minioUp;
    private Guid categoryId;

    public PathTraversalSecurityTests()
    {
        factory = new ApiFactoryWithMinio();
        ownerClient = factory.CreateClient();
        factory.EnsureMigrated();
        minioUp = ApiFactoryWithMinio.MinioIsReachableAsync().GetAwaiter().GetResult();
    }

    private async Task RegisterAsync()
    {
        var register = await ownerClient.PostAsJsonAsync("/api/v1/auth/register",
            new { email = $"tv4-traversal-{Guid.NewGuid():N}@example.test", password = "Demo-Password9!", displayName = "TV4 Traversal" });
        if (register.StatusCode != HttpStatusCode.Created)
            throw new InvalidOperationException($"register failed {register.StatusCode}");
        var apiRes = (await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!;
        ownerClient.DefaultRequestHeaders.Authorization = new("Bearer", apiRes.Data.AccessToken);
    }

    private void SeedCategory()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CulinaryBlog.Infrastructure.AuthDbContext>();
        var category = new Category("Món traversal", $"mon-traversal-{Guid.NewGuid():N}", "proxy");
        db.Categories.Add(category);
        db.SaveChanges();
        categoryId = category.Id;
    }

    private static async Task<Guid> CreateRecipeAsync(HttpClient http, Guid categoryId)
    {
        var created = await http.PostAsJsonAsync("/api/v1/recipes", new
        {
            title = $"Công thức traversal {Guid.NewGuid():N}",
            description = "Recipe cho W5-10 / NFR-SEC-004.",
            instructions = "",
            prepTimeMinutes = 10,
            cookTimeMinutes = 20,
            servings = 2,
            difficulty = 1,
            categoryId
        });
        if (!created.IsSuccessStatusCode)
            throw new InvalidOperationException($"create failed {created.StatusCode}");
        var apiRes = (await created.Content.ReadFromJsonAsync<ApiResponse<RecipeDto>>())!;
        return apiRes.Data.Id;
    }

    private async Task<string> UploadJpegAsync(Guid recipeId)
    {
        var jpeg = new ByteArrayContent(BuildJpeg());
        jpeg.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        using var content = new MultipartFormDataContent { { jpeg, "file", "anh-an-toan.jpg" } };
        var upload = await ownerClient.PostAsync($"/api/v1/recipes/{recipeId}/images", content);
        if (!upload.IsSuccessStatusCode)
            throw new InvalidOperationException($"upload failed {upload.StatusCode}");
        var image = (await upload.Content.ReadFromJsonAsync<ApiResponse<RecipeImageSummaryDto>>())!.Data;
        return image.OriginalUrl;
    }

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

    [Fact]
    public async Task Proxy_404_for_traversal_keys_and_never_serves_outside_prefix()
    {
        if (!minioUp) return;

        await RegisterAsync();
        SeedCategory();
        var recipeId = await CreateRecipeAsync(ownerClient, categoryId);
        var cleanKey = await UploadJpegAsync(recipeId);

        // Key sạch đọc được (owner, draft) — đối chứng cho việc recipe thật tồn tại.
        var ok = await ownerClient.GetAsync($"/api/v1/resources/images/{cleanKey}");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        // Mọi biến thể `../` không phân giải thành object khác -> 404, không bao giờ 200.
        foreach (var traversal in new[]
        {
            $"recipes/{recipeId}/../../etc/passwd",
            $"recipes/{recipeId}/..%2f..%2fetc%2fpasswd",
            $"recipes/{recipeId}/../{Path.GetFileName(cleanKey)}",
            $"recipes/{recipeId}/../../../{cleanKey}",
            $"../../etc/passwd",
        })
        {
            var resp = await ownerClient.GetAsync($"/api/v1/resources/images/{traversal}");
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }
    }
}