using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// D27 (TV4, PA-2) — proxy ảnh base media URL `GET /api/v1/resources/images/{key}`.
/// Published -> public + cache; Draft/Archived/Unpublished -> chỉ owner/Admin else 403 image.forbidden;
/// key không hợp lệ / recipe đã soft-delete -> 404. Cần MinIO local (giống MinioE2ETests: down thì skip).
/// </summary>
public sealed class ImageProxyD27Tests : IAsyncLifetime
{
    private readonly ApiFactoryWithMinio factory;
    private readonly HttpClient ownerClient;
    private readonly bool minioUp;
    private Guid categoryId;

    public ImageProxyD27Tests()
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

    // Header JPEG hợp lệ (`FF D8 FF E0` + JFIF) rồi đệm tới `ImageFormats.MinBytes`.
    // Validator chặn `file.too_small` **trước** khi đối chiếu magic bytes, nên payload 22 byte
    // sẽ bị chặn ở ngưỡng kích thước thay vì tới nhánh kiểm tra ảnh.
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

    private async Task RegisterAsync(HttpClient http)
    {
        var register = await http.PostAsJsonAsync("/api/v1/auth/register",
            new { email = $"tv4-d27-{Guid.NewGuid():N}@example.test", password = "Demo-Password9!", displayName = "TV4 D27" });
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
        var category = new Category("Món ảnh D27", $"mon-anh-d27-{Guid.NewGuid():N}", "D27 proxy");
        db.Categories.Add(category);
        db.SaveChanges();
        categoryId = category.Id;
    }

    private async Task<RecipeDto> CreateRecipeAsync(HttpClient http, string title)
    {
        var created = await http.PostAsJsonAsync("/api/v1/recipes", new
        {
            title = $"{title} {Guid.NewGuid():N}",
            description = "Recipe cho E2E proxy D27.",
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
        var apiRes = (await created.Content.ReadFromJsonAsync<ApiResponse<RecipeDto>>())!;
        return apiRes.Data;
    }

    private async Task<string> UploadJpegAsync(HttpClient http, Guid recipeId)
    {
        var jpeg = new ByteArrayContent(JpegBytes);
        jpeg.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        using var content = new MultipartFormDataContent { { jpeg, "file", "anh-d27.jpg" } };
        var upload = await http.PostAsync($"/api/v1/recipes/{recipeId}/images", content);
        var raw = await upload.Content.ReadAsStringAsync();
        if (!upload.IsSuccessStatusCode)
            throw new InvalidOperationException($"upload failed {upload.StatusCode}: {raw}");
        var image = (await upload.Content.ReadFromJsonAsync<ApiResponse<RecipeImageSummaryDto>>())!.Data;
        return image.OriginalUrl;
    }

    private async Task MakePublishableAsync(HttpClient http, Guid recipeId)
    {
        var ing = await http.PostAsJsonAsync($"/api/v1/recipes/{recipeId}/ingredients", new { name = "Ớt", quantity = 1, unit = "trái" });
        if (!ing.IsSuccessStatusCode)
            throw new InvalidOperationException($"add ingredient failed {ing.StatusCode}");
        var step = await http.PostAsJsonAsync($"/api/v1/recipes/{recipeId}/steps", new { title = "Xào", description = "Xào nhanh." });
        if (!step.IsSuccessStatusCode)
            throw new InvalidOperationException($"add step failed {step.StatusCode}");
    }

    private async Task<HttpResponseMessage> GetImageAsync(HttpClient http, string key)
        => await http.GetAsync($"/api/v1/resources/images/{key}");

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task Published_image_is_public_and_cacheable()
    {
        if (!minioUp) return;

        await RegisterAsync(ownerClient);
        SeedCategory();
        var recipe = await CreateRecipeAsync(ownerClient, "Phở D27 public");
        var key = await UploadJpegAsync(ownerClient, recipe.Id);
        await MakePublishableAsync(ownerClient, recipe.Id);
        var publish = await ownerClient.PatchAsync($"/api/v1/recipes/{recipe.Id}/publish", new StringContent("{}"));
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);

        var anon = factory.CreateClient();
        var proxy = await GetImageAsync(anon, key);

        Assert.Equal(HttpStatusCode.OK, proxy.StatusCode);
        Assert.Equal("image/jpeg", proxy.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("public", proxy.Headers.CacheControl?.ToString() ?? "");
        var bytes = await proxy.Content.ReadAsByteArrayAsync();
        Assert.Equal(JpegBytes, bytes);
    }

    [Fact]
    public async Task Draft_image_403_for_anonymous_but_200_for_owner()
    {
        if (!minioUp) return;

        await RegisterAsync(ownerClient);
        SeedCategory();
        var recipe = await CreateRecipeAsync(ownerClient, "Bún D27 draft");
        var key = await UploadJpegAsync(ownerClient, recipe.Id);   // mặc định Draft

        var anon = factory.CreateClient();
        var forbidden = await GetImageAsync(anon, key);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        var problem = await ReadProblemAsync(forbidden);
        Assert.Equal("image.forbidden", problem.GetProperty("code").GetString());

        var owner = await GetImageAsync(ownerClient, key);
        Assert.Equal(HttpStatusCode.OK, owner.StatusCode);
        Assert.Equal("no-store", owner.Headers.CacheControl?.ToString());
        Assert.Equal(JpegBytes, await owner.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Non_owner_member_cannot_view_draft_image()
    {
        if (!minioUp) return;

        await RegisterAsync(ownerClient);
        SeedCategory();
        var recipe = await CreateRecipeAsync(ownerClient, "Chả D27 draft");
        var key = await UploadJpegAsync(ownerClient, recipe.Id);

        var otherMember = factory.CreateClient();
        await RegisterAsync(otherMember);   // author khác, đăng nhập hợp lệ

        var forbidden = await GetImageAsync(otherMember, key);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        var problem = await ReadProblemAsync(forbidden);
        Assert.Equal("image.forbidden", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Archived_image_403_for_anonymous_but_owner_still_200()
    {
        if (!minioUp) return;

        await RegisterAsync(ownerClient);
        SeedCategory();
        var recipe = await CreateRecipeAsync(ownerClient, "Cá kho D27");
        var key = await UploadJpegAsync(ownerClient, recipe.Id);
        await MakePublishableAsync(ownerClient, recipe.Id);
        var publish = await ownerClient.PatchAsync($"/api/v1/recipes/{recipe.Id}/publish", new StringContent("{}"));
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
        var archive = await ownerClient.PatchAsync($"/api/v1/recipes/{recipe.Id}/archive", new StringContent("{}"));
        Assert.Equal(HttpStatusCode.OK, archive.StatusCode);

        var anon = factory.CreateClient();
        var forbidden = await GetImageAsync(anon, key);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var owner = await GetImageAsync(ownerClient, key);
        Assert.Equal(HttpStatusCode.OK, owner.StatusCode);
        Assert.Equal(JpegBytes, await owner.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Unpublished_image_403_for_anonymous()
    {
        if (!minioUp) return;

        await RegisterAsync(ownerClient);
        SeedCategory();
        var recipe = await CreateRecipeAsync(ownerClient, "Xôi D27");
        var key = await UploadJpegAsync(ownerClient, recipe.Id);
        await MakePublishableAsync(ownerClient, recipe.Id);
        var publish = await ownerClient.PatchAsync($"/api/v1/recipes/{recipe.Id}/publish", new StringContent("{}"));
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
        var unpublish = await ownerClient.PatchAsync($"/api/v1/recipes/{recipe.Id}/unpublish", new StringContent("{}"));
        Assert.Equal(HttpStatusCode.OK, unpublish.StatusCode);

        var anon = factory.CreateClient();
        var forbidden = await GetImageAsync(anon, key);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Invalid_or_unknown_or_deleted_key_returns_404()
    {
        if (!minioUp) return;

        await RegisterAsync(ownerClient);
        SeedCategory();
        var recipe = await CreateRecipeAsync(ownerClient, "Hủ tiếu D27 404");
        var key = await UploadJpegAsync(ownerClient, recipe.Id);

        var anon = factory.CreateClient();

        // prefix sai
        var wrongPrefix = await anon.GetAsync($"/api/v1/resources/images/recipesx/{recipe.Id}/abc.jpg");
        Assert.Equal(HttpStatusCode.NotFound, wrongPrefix.StatusCode);

        // recipeId không phải Guid
        var badGuid = await anon.GetAsync($"/api/v1/resources/images/recipes/not-a-guid/abc.jpg");
        Assert.Equal(HttpStatusCode.NotFound, badGuid.StatusCode);

        // recipe không tồn tại
        var unknown = await anon.GetAsync($"/api/v1/resources/images/recipes/{Guid.NewGuid():N}/abc.jpg");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        // recipe soft-delete -> global filter -> 404 (D3.2c)
        var deleted = await ownerClient.DeleteAsync($"/api/v1/recipes/{recipe.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        var afterDelete = await anon.GetAsync($"/api/v1/resources/images/{key}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }
}
