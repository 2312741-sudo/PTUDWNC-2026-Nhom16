using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// D2 (TV4) — resize ảnh 300×300 + 800×600 chạy ngoài request (D23 PA-1 Hangfire).
/// Ở môi trường Testing queue chạy inline (InlineImageResizeQueue) nên assert deterministic sau khi upload.
/// Cần MinIO local (MinioE2ETests pattern: down thì skip).
/// FR-JOB-002 idempotent · FR-JOB-003 original fallback · delete-vs-resize không tái sinh object.
/// </summary>
public sealed class ImageResizeD2Tests : IAsyncLifetime
{
    private readonly ApiFactoryWithMinio factory;
    private readonly HttpClient ownerClient;
    private readonly bool minioUp;
    private Guid categoryId;

    public ImageResizeD2Tests()
    {
        factory = new ApiFactoryWithMinio();
        ownerClient = factory.CreateClient();
        factory.EnsureMigrated();
        minioUp = ApiFactoryWithMinio.MinioIsReachableAsync().GetAwaiter().GetResult();
    }

    public Task DisposeAsync()
    {
        ownerClient.Dispose();
        return factory.DisposeAsync().AsTask();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>JPEG hợp lệ 1200×800 để ImageSharp decode được (E2E cũ dùng JPEG giá 22 byte).</summary>
    private static byte[] RealJpeg(int width = 1200, int height = 800)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(100, 149, 237));
        using var output = new MemoryStream();
        image.Save(output, new JpegEncoder());
        return output.ToArray();
    }

    /// <summary>JPEG "header + EOI" không decode được — mô phỏng ảnh hỏng để kiểm tra original fallback.</summary>
    private static readonly byte[] TruncatedJpeg =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
        0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9
    ];

    private async Task RegisterAsync(HttpClient http)
    {
        var register = await http.PostAsJsonAsync("/api/v1/auth/register",
            new { email = $"tv4-d2-{Guid.NewGuid():N}@example.test", password = "Demo-Password9!", displayName = "TV4 D2" });
        var body = await register.Content.ReadAsStringAsync();
        if (register.StatusCode != HttpStatusCode.Created)
            throw new InvalidOperationException($"register failed {register.StatusCode}: {body}");
        var apiRes = JsonSerializer.Deserialize<ApiResponse<AuthResponse>>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        http.DefaultRequestHeaders.Authorization = new("Bearer", apiRes.Data.AccessToken);
    }

    private void SeedCategory()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var category = new Category("Món ảnh D2", $"mon-anh-d2-{Guid.NewGuid():N}", "D2 resize");
        db.Categories.Add(category);
        db.SaveChanges();
        categoryId = category.Id;
    }

    private async Task<RecipeDto> CreateRecipeAsync(string title)
    {
        var created = await ownerClient.PostAsJsonAsync("/api/v1/recipes", new
        {
            title = $"{title} {Guid.NewGuid():N}",
            description = "Recipe cho E2E resize D2.",
            instructions = "",
            prepTimeMinutes = 10,
            cookTimeMinutes = 20,
            servings = 2,
            difficulty = 1,
            categoryId
        });
        var raw = await created.Content.ReadAsStringAsync();
        if (!created.IsSuccessStatusCode)
            throw new InvalidOperationException($"create failed {created.StatusCode}: {raw}");
        return (await created.Content.ReadFromJsonAsync<ApiResponse<RecipeDto>>())!.Data;
    }

    private async Task<RecipeImageDto> UploadAsync(Guid recipeId, byte[] bytes, string fileName)
    {
        var part = new ByteArrayContent(bytes);
        part.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        using var content = new MultipartFormDataContent { { part, "file", fileName } };
        var upload = await ownerClient.PostAsync($"/api/v1/recipes/{recipeId}/images", content);
        var raw = await upload.Content.ReadAsStringAsync();
        if (upload.StatusCode != HttpStatusCode.Created)
            throw new InvalidOperationException($"upload failed {upload.StatusCode}: {raw}");
        return (await upload.Content.ReadFromJsonAsync<ApiResponse<RecipeImageDto>>())!.Data;
    }

    private async Task<(int Width, int Height)> FetchImageSizeAsync(string key)
    {
        var response = await ownerClient.GetAsync($"/api/v1/resources/images/{key}");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"proxy {key} -> {(int)response.StatusCode}: {System.Text.Encoding.UTF8.GetString(bytes)}");
        var info = Image.Identify(bytes);
        Assert.NotNull(info);
        return (info!.Width, info.Height);
    }

    private async Task<RecipeDetailDto> GetRecipeDetailAsync(string slug)
    {
        var response = await ownerClient.GetAsync($"/api/v1/recipes/{slug}");
        var raw = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"get detail failed {response.StatusCode}: {raw}");
        return (await response.Content.ReadFromJsonAsync<ApiResponse<RecipeDetailDto>>())!.Data;
    }

    private (Guid ImageId, string? MediumUrl, string? ThumbnailUrl) ReadRow(Guid recipeId, string originalKey)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var row = db.RecipeImages.AsNoTracking().First(i => i.OriginalUrl == originalKey);
        return (row.Id, row.MediumUrl, row.ThumbnailUrl);
    }

    private async Task RunJobAsync(Guid recipeId, Guid imageId, string originalKey)
    {
        using var scope = factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<ResizeImageJob>();
        await job.ExecuteAsync(recipeId, imageId, originalKey, CancellationToken.None);
    }

    private async Task<bool> DerivedExistsAsync(string key)
    {
        using var scope = factory.Services.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<IObjectStorageWriter>();
        return await writer.ExistsAsync(key);
    }

    [Fact]
    public async Task Real_jpeg_upload_produces_300x300_and_800x600_variants()
    {
        if (!minioUp) return;

        await RegisterAsync(ownerClient);
        SeedCategory();
        var recipe = await CreateRecipeAsync("Phở D2 resize");
        var image = await UploadAsync(recipe.Id, RealJpeg(), "anh-d2.jpg");

        // Key phái sinh đúng contract: {base}_300x300.jpg (thumbnail) và {base}_800x600.jpg (medium).
        var keys = RecipeImageKeys.ResizedKeys(image.OriginalUrl);
        Assert.NotNull(keys);
        Assert.EndsWith("_300x300.jpg", keys!.Value.ThumbnailKey);
        Assert.EndsWith("_800x600.jpg", keys.Value.MediumKey);

        // Resize chạy ngoài request (queue) -> response upload không kèm URL resize; DB đã cập nhật.
        var row = ReadRow(recipe.Id, image.OriginalUrl);
        Assert.Equal(keys.Value.MediumKey, row.MediumUrl);
        Assert.Equal(keys.Value.ThumbnailKey, row.ThumbnailUrl);

        // Detail recipe (request mới) trả URL resize cho FE (imageSrc dùng thumbnail/medium).
        var detailImage = Assert.Single((await GetRecipeDetailAsync(recipe.Slug)).Images);
        Assert.Equal(keys.Value.ThumbnailKey, detailImage.ThumbnailUrl);
        Assert.Equal(keys.Value.MediumKey, detailImage.MediumUrl);

        // Object phái sinh tồn tại và đúng kích thước: 1200×800 -> max 300×300 = 300×200, max 800×600 = 800×533.
        Assert.Equal((300, 200), await FetchImageSizeAsync(keys.Value.ThumbnailKey));
        Assert.Equal((800, 533), await FetchImageSizeAsync(keys.Value.MediumKey));
    }

    [Fact]
    public async Task Resize_is_idempotent_when_job_runs_twice()
    {
        if (!minioUp) return;

        await RegisterAsync(ownerClient);
        SeedCategory();
        var recipe = await CreateRecipeAsync("Bún D2 idempotent");
        var image = await UploadAsync(recipe.Id, RealJpeg(900, 900), "anh-d2-idem.jpg");
        var row = ReadRow(recipe.Id, image.OriginalUrl);
        Assert.NotNull(row.ThumbnailUrl);

        // Chạy lại job (mô phỏng retry/restart): ExistsAsync chặn ghi đè, URL không đổi.
        await RunJobAsync(recipe.Id, row.ImageId, image.OriginalUrl);
        var after = ReadRow(recipe.Id, image.OriginalUrl);
        Assert.Equal(row.MediumUrl, after.MediumUrl);
        Assert.Equal(row.ThumbnailUrl, after.ThumbnailUrl);
        Assert.Equal((300, 300), await FetchImageSizeAsync(after.ThumbnailUrl!));
    }

    [Fact]
    public async Task Undecodable_upload_keeps_original_without_resized_urls()
    {
        if (!minioUp) return;

        await RegisterAsync(ownerClient);
        SeedCategory();
        var recipe = await CreateRecipeAsync("Cá D2 fallback");
        var image = await UploadAsync(recipe.Id, TruncatedJpeg, "anh-d2-hong.jpg");

        // FR-JOB-003: ảnh hỏng -> upload vẫn 201, không có URL resize (FE fallback original).
        Assert.Null(image.ThumbnailUrl);
        Assert.Null(image.MediumUrl);
        var row = ReadRow(recipe.Id, image.OriginalUrl);
        Assert.Null(row.ThumbnailUrl);
        Assert.Null(row.MediumUrl);
        var detailImage = Assert.Single((await GetRecipeDetailAsync(recipe.Slug)).Images);
        Assert.Null(detailImage.ThumbnailUrl);
        Assert.Null(detailImage.MediumUrl);

        // Original vẫn phục vụ được qua proxy, đúng bytes đã upload.
        var response = await ownerClient.GetAsync($"/api/v1/resources/images/{image.OriginalUrl}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TruncatedJpeg, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Deleted_image_is_not_regenerated_by_resize_job()
    {
        if (!minioUp) return;

        await RegisterAsync(ownerClient);
        SeedCategory();
        var recipe = await CreateRecipeAsync("Gà D2 delete");
        var image = await UploadAsync(recipe.Id, RealJpeg(), "anh-d2-delete.jpg");
        var row = ReadRow(recipe.Id, image.OriginalUrl);
        Assert.NotNull(row.ThumbnailUrl);

        var deleted = await ownerClient.DeleteAsync($"/api/v1/recipes/{recipe.Id}/images/{row.ImageId}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        // Job chạy trễ (queue đã enqueue trước khi xoá) -> không được tái sinh object.
        await RunJobAsync(recipe.Id, row.ImageId, image.OriginalUrl);
        Assert.False(await DerivedExistsAsync(row.ThumbnailUrl!));
        Assert.False(await DerivedExistsAsync(row.MediumUrl!));

        // Proxy: ảnh đã xoá -> 404 (owner cũng 404 vì object không còn).
        var thumb = await ownerClient.GetAsync($"/api/v1/resources/images/{row.ThumbnailUrl}");
        Assert.Equal(HttpStatusCode.NotFound, thumb.StatusCode);
    }
}
