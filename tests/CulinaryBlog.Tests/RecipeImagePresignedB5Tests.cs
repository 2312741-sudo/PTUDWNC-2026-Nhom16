using CulinaryBlog.Application;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Recipe = CulinaryBlog.Domain.Entities.Recipe;

namespace CulinaryBlog.Tests;

/// <summary>
/// B5 (TV4, PA-3) — ảnh private (recipe chưa Published) trả kèm URL có chữ ký để thẻ &lt;img&gt;
/// tải được, vì proxy D27 chỉ cho owner/Admin và thẻ &lt;img&gt; không gửi header Bearer.
/// PA-A: hạn tối đa 10 phút (chặn &gt;15), chỉ ký khi CHƯA Published, không log URL ký.
/// </summary>
public sealed class RecipeImagePresignedB5Tests
{
    private static Recipe NewRecipe(RecipeStatus status)
    {
        var recipe = Recipe.CreateDraft(
            title: "Bún bò Huế",
            slug: $"bun-bo-hue-{Guid.NewGuid():N}",
            description: "Món Huế.",
            instructions: null,
            prepTimeMinutes: 20,
            cookTimeMinutes: 60,
            servings: 4,
            difficulty: RecipeDifficulty.Hard,
            categoryId: Guid.NewGuid(),
            authorId: "author-1");

        // Publish() (D07) chặn recipe thiếu nguyên liệu/bước -> dựng đủ điều kiện cho cả 3 trạng thái.
        recipe.AddIngredient("Thịt bò", 300, "gram", null);
        recipe.AddStep("Ninh nước dùng", "Ninh 2 tiếng.", null, null);

        if (status == RecipeStatus.Published)
            recipe.Publish();
        else if (status == RecipeStatus.Archived)
        {
            recipe.Publish();
            recipe.Archive();
        }

        return recipe;
    }

    // ---------- Quy tắc nào được ký ----------

    [Theory]
    [InlineData(RecipeStatus.Draft, true)]
    [InlineData(RecipeStatus.Archived, true)]
    [InlineData(RecipeStatus.Published, false)]
    public void Needs_presigned_urls_only_before_published(RecipeStatus status, bool expected)
        => Assert.Equal(expected, RecipeImageAccessProbe.NeedsPresignedUrls(NewRecipe(status)));

    [Fact]
    public async Task Draft_image_gets_presigned_url()
    {
        var recipe = NewRecipe(RecipeStatus.Draft);
        var image = recipe.AddImage("recipes/r1/a.jpg", "Ảnh");
        var signer = new FakeObjectStorageUrlSigner();
        var factory = new RecipeImageDtoFactory(signer);

        var dto = await factory.CreateAsync(image, includePresignedUrl: true);

        Assert.Equal("https://storage.local/presigned", dto.PresignedUrl);
        Assert.Equal(["recipes/r1/a.jpg"], signer.SignedKeys);
    }

    [Fact]
    public async Task Published_image_is_not_signed()
    {
        var recipe = NewRecipe(RecipeStatus.Published);
        var image = recipe.AddImage("recipes/r1/a.jpg", "Ảnh");
        var signer = new FakeObjectStorageUrlSigner();
        var factory = new RecipeImageDtoFactory(signer);

        var dto = await factory.CreateAsync(image, includePresignedUrl: false);

        Assert.Null(dto.PresignedUrl);
        Assert.Empty(signer.SignedKeys);   // không gọi signer -> không rải URL ký thừa
    }

    [Fact]
    public async Task Prefers_thumbnail_then_medium_then_original()
    {
        var recipe = NewRecipe(RecipeStatus.Draft);

        // Cả 3 biến thể có mặt (job resize xong) -> ưu tiên thumbnail nhẹ nhất.
        var full = recipe.AddImage("recipes/r1/a.jpg", "Ảnh");
        full.SetResizedUrls("recipes/r1/a_800x600.jpg", "recipes/r1/a_300x300.jpg");
        var all = new FakeObjectStorageUrlSigner();
        await new RecipeImageDtoFactory(all).CreateAsync(full, includePresignedUrl: true);
        Assert.Equal(["recipes/r1/a_300x300.jpg"], all.SignedKeys);

        // Chỉ có medium -> ký medium.
        var mediumOnly = recipe.AddImage("recipes/r1/b.jpg", "Ảnh");
        mediumOnly.SetResizedUrls("recipes/r1/b_800x600.jpg", null);
        var medium = new FakeObjectStorageUrlSigner();
        await new RecipeImageDtoFactory(medium).CreateAsync(mediumOnly, includePresignedUrl: true);
        Assert.Equal(["recipes/r1/b_800x600.jpg"], medium.SignedKeys);

        // Chưa resize xong -> ký original.
        var fresh = recipe.AddImage("recipes/r1/c.jpg", "Ảnh");
        var original = new FakeObjectStorageUrlSigner();
        await new RecipeImageDtoFactory(original).CreateAsync(fresh, includePresignedUrl: true);
        Assert.Equal(["recipes/r1/c.jpg"], original.SignedKeys);
    }

    [Fact]
    public async Task Signer_failure_is_fail_soft_not_500()
    {
        var recipe = NewRecipe(RecipeStatus.Draft);
        var image = recipe.AddImage("recipes/r1/a.jpg", "Ảnh");
        var signer = new FakeObjectStorageUrlSigner { Result = null };   // RustFS/MinIO lỗi
        var factory = new RecipeImageDtoFactory(signer);

        var dto = await factory.CreateAsync(image, includePresignedUrl: true);

        Assert.Null(dto.PresignedUrl);                     // ảnh vẫn trả về, FE hiện placeholder
        Assert.Equal(image.Id, dto.Id);                     // không mất metadata
        Assert.Equal(image.OriginalUrl, dto.OriginalUrl);
    }

    [Fact]
    public async Task Public_dto_never_carries_presigned_url()
    {
        var recipe = NewRecipe(RecipeStatus.Draft);
        var image = recipe.AddImage("recipes/r1/a.jpg", "Ảnh");

        // RecipeImageDto.From là đường dùng cho response công khai -> phải luôn null.
        Assert.Null(RecipeImageDto.From(image).PresignedUrl);
        await Task.CompletedTask;
    }

    // ---------- Hạn URL: cấu hình + ký thật (không cần mạng) ----------

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(15)]
    public void Options_accept_expiry_within_cap(int minutes)
        => Assert.Empty(NewStorage(minutes).Options.Validate());

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Options_reject_non_positive_expiry(int minutes)
        => Assert.Contains(NewStorage(minutes).Options.Validate(),
            p => p.Contains("PresignedUrlExpiryMinutes", StringComparison.Ordinal));

    [Theory]
    [InlineData(16)]
    [InlineData(60)]
    [InlineData(1440)]
    public void Options_reject_expiry_above_cap(int minutes)
        => Assert.Contains(NewStorage(minutes).Options.Validate(),
            p => p.Contains("PresignedUrlExpiryMinutes", StringComparison.Ordinal));

    [Fact]
    public void Default_expiry_is_ten_minutes()
        => Assert.Equal(10, new MinioOptions().PresignedUrlExpiryMinutes);

    [Fact]
    public async Task Signs_real_url_without_network_and_caps_expiry_at_15_minutes()
    {
        // PresignedGetObjectAsync chỉ ký HMAC cục bộ -> không cần RustFS/MinIO thật.
        var storage = NewStorage(600);   // cấu hình vượt trần -> phải bị clamp xuống 15 phút
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var url = await storage.Service.CreatePresignedUrlAsync("recipes/r1/a.jpg", cts.Token);

        Assert.NotNull(url);
        Assert.Contains("X-Amz-Signature=", url);
        Assert.Contains("X-Amz-Expires=900", url);      // 15 phút = 900 giây
    }

    [Fact]
    public async Task Signed_url_expiry_follows_configured_minutes()
    {
        var storage = NewStorage(5);
        var url = await storage.Service.CreatePresignedUrlAsync("recipes/r1/a.jpg");

        Assert.NotNull(url);
        Assert.Contains("X-Amz-Expires=300", url);      // 5 phút = 300 giây
    }

    [Fact]
    public async Task Signed_url_never_appears_in_logs()
    {
        var logger = new CapturingLogger();
        var storage = NewStorage(10, logger);

        var url = await storage.Service.CreatePresignedUrlAsync("recipes/r1/a.jpg");

        Assert.NotNull(url);
        Assert.DoesNotContain("X-Amz-Signature", logger.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(url, logger.Text, StringComparison.Ordinal);
        // Bucket + key thì được log (không phải bí mật) đ�g tra cứu được sự cố.
        Assert.Contains("recipes/r1/a.jpg", logger.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_key_returns_null_without_signing(string key)
        => Assert.Null(await NewStorage(10).Service.CreatePresignedUrlAsync(key));

    private static (MinioOptions Options, MinioStorageService Service) NewStorage(
        int expiryMinutes, ILogger<MinioStorageService>? logger = null)
    {
        // Endpoint không tồn tại: chứng minh việc ký không gọi mạng.
        var options = new MinioOptions
        {
            Endpoint = "127.0.0.1:9",
            AccessKey = "test-access",
            SecretKey = "test-secret-key",
            Bucket = "culinary-blog",
            UseSsl = false,
            PresignedUrlExpiryMinutes = expiryMinutes
        };
        var service = new MinioStorageService(
            Options.Create(options), logger ?? NullLogger<MinioStorageService>.Instance);
        return (options, service);
    }

    private sealed class CapturingLogger : ILogger<MinioStorageService>
    {
        private readonly List<string> _lines = [];

        public string Text => string.Join('\n', _lines);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => _lines.Add(formatter(state, exception));
    }
}

/// <summary>
/// <c>RecipeImageAccess</c> là internal nên test cùng assembly không thấy; bọc lại để assert
/// đúng quy tắc mà handler thực sự dùng (nếu đổi tên/visibility thì test phải đỏ).
/// </summary>
internal static class RecipeImageAccessProbe
{
    public static bool NeedsPresignedUrls(Recipe recipe)
    {
        var method = typeof(UploadRecipeImageHandler).Assembly
            .GetType("CulinaryBlog.Application.RecipeImageAccess")!
            .GetMethod("NeedsPresignedUrls",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;
        return (bool)method.Invoke(null, [recipe])!;
    }
}

/// <summary>B5: repo giả — chỉ FindBySlugAsync được dùng bởi GetRecipeBySlugHandler.</summary>
internal sealed class FakeSlugRecipeRepository(Recipe? recipe = null) : IRecipeRepository
{
    public Recipe? Recipe { get; set; } = recipe;

    public Task<Recipe?> FindBySlugAsync(string slug, CancellationToken ct)
        => Task.FromResult(Recipe is not null && Recipe.Slug == slug ? Recipe : null);

    public Task<Recipe?> FindForWriteAsync(Guid id, CancellationToken ct)
        => Task.FromResult(Recipe is not null && Recipe.Id == id ? Recipe : null);

    public Task<IReadOnlyList<string>> FindUsedSlugsAsync(string baseSlug, Guid? excludeRecipeId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken ct) => Task.FromResult(true);

    public void Add(Recipe recipe) => Recipe = recipe;
    public void Remove(Recipe recipe) => Recipe = null;
    public void RemoveIngredient(RecipeIngredient ingredient) { }
    public void RemoveStep(RecipeStep step) { }
    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}

/// <summary>
/// B5: endpoint detail theo slug phục vụ CẢ owner (wizard) LẪN người xem công khai — đây là nơi rò
/// rỉ dễ xảy ra nhất. Phải ký cho owner/Admin khi Draft, và TUYỆT ĐỐI không ký cho Published.
/// </summary>
public sealed class RecipeDetailPresignedB5Tests
{
    private static Recipe NewRecipe(RecipeStatus status)
    {
        var recipe = Recipe.CreateDraft(
            "Bún đậu", $"bun-dau-{Guid.NewGuid():N}", "Món ngon.", null, 15, 15, 2,
            CulinaryBlog.Domain.Enums.RecipeDifficulty.Easy, Guid.NewGuid(), "author-1");
        recipe.AddIngredient("Đậu", 100, "gram", null);
        recipe.AddStep("Rang thơm", "Rang đậu.", null, null);
        if (status == RecipeStatus.Published)
            recipe.Publish();
        return recipe;
    }

    private static RecipeImageDtoFactory Factory(FakeObjectStorageUrlSigner signer) => new(signer);

    [Fact]
    public async Task Draft_detail_for_owner_includes_presigned_url()
    {
        var recipe = NewRecipe(RecipeStatus.Draft);
        recipe.AddImage("recipes/r1/a.jpg", "Ảnh");
        var signer = new FakeObjectStorageUrlSigner();
        var handler = new GetRecipeBySlugHandler(
            new FakeSlugRecipeRepository(recipe), new FakeCurrentUser("author-1"), Factory(signer));

        var dto = await handler.Handle(new GetRecipeBySlugQuery(recipe.Slug), CancellationToken.None);

        Assert.Equal("https://storage.local/presigned", Assert.Single(dto.Images).PresignedUrl);
    }

    [Fact]
    public async Task Draft_detail_for_admin_includes_presigned_url()
    {
        var recipe = NewRecipe(RecipeStatus.Draft);
        recipe.AddImage("recipes/r1/a.jpg", "Ảnh");
        var signer = new FakeObjectStorageUrlSigner();
        var handler = new GetRecipeBySlugHandler(
            new FakeSlugRecipeRepository(recipe), new FakeCurrentUser("admin-1", isAdmin: true), Factory(signer));

        var dto = await handler.Handle(new GetRecipeBySlugQuery(recipe.Slug), CancellationToken.None);

        Assert.Equal("https://storage.local/presigned", Assert.Single(dto.Images).PresignedUrl);
    }

    [Fact]
    public async Task Published_detail_never_includes_presigned_url_even_for_owner()
    {
        var recipe = NewRecipe(RecipeStatus.Published);
        recipe.AddImage("recipes/r1/a.jpg", "Ảnh");
        var signer = new FakeObjectStorageUrlSigner();
        var handler = new GetRecipeBySlugHandler(
            new FakeSlugRecipeRepository(recipe), new FakeCurrentUser("author-1"), Factory(signer));

        var dto = await handler.Handle(new GetRecipeBySlugQuery(recipe.Slug), CancellationToken.None);

        // Ảnh Published đã phục vụ công khai -> ký là rải token thừa ra response.
        Assert.Null(Assert.Single(dto.Images).PresignedUrl);
        Assert.Empty(signer.SignedKeys);
    }

    [Fact]
    public async Task Anonymous_viewer_of_published_recipe_gets_no_presigned_url()
    {
        var recipe = NewRecipe(RecipeStatus.Published);
        recipe.AddImage("recipes/r1/a.jpg", "Ảnh");
        var signer = new FakeObjectStorageUrlSigner();
        var handler = new GetRecipeBySlugHandler(
            new FakeSlugRecipeRepository(recipe), new FakeCurrentUser(""), Factory(signer));

        var dto = await handler.Handle(new GetRecipeBySlugQuery(recipe.Slug), CancellationToken.None);

        Assert.Null(Assert.Single(dto.Images).PresignedUrl);
        Assert.Empty(signer.SignedKeys);
    }

    [Fact]
    public async Task Other_member_cannot_read_draft_detail_at_all()
    {
        var recipe = NewRecipe(RecipeStatus.Draft);
        recipe.AddImage("recipes/r1/a.jpg", "Ảnh");
        var signer = new FakeObjectStorageUrlSigner();
        var handler = new GetRecipeBySlugHandler(
            new FakeSlugRecipeRepository(recipe), new FakeCurrentUser("author-2"), Factory(signer));

        // 404 (không 403) để không tiết lộ sự tồn tại của Draft — D12.
        var ex = await Assert.ThrowsAsync<AppException>(
            () => handler.Handle(new GetRecipeBySlugQuery(recipe.Slug), CancellationToken.None));
        Assert.Equal(404, ex.Status);
        Assert.Equal("recipe.not_found", ex.Code);
        Assert.Empty(signer.SignedKeys);
    }

    [Fact]
    public async Task Summaries_keep_metadata_and_stay_ordered()
    {
        var recipe = NewRecipe(RecipeStatus.Draft);
        var a = recipe.AddImage("recipes/r1/a.jpg", "A");
        var b = recipe.AddImage("recipes/r1/b.jpg", "B");
        recipe.SetPrimaryImage(a.Id);
        recipe.UpdateImageMetadata(b.Id, "B đổi mô tả", 0);   // OrderIndex 0 đẩy a xuống 1
        var signer = new FakeObjectStorageUrlSigner();
        var handler = new GetRecipeBySlugHandler(
            new FakeSlugRecipeRepository(recipe), new FakeCurrentUser("author-1"), Factory(signer));

        var dto = await handler.Handle(new GetRecipeBySlugQuery(recipe.Slug), CancellationToken.None);

        Assert.Equal(2, dto.Images.Count);
        // Thứ tự ảnh phải bám OrderIndex của entity (không phụ thuộc domain có re-normalize hay không).
        Assert.Equal(
            recipe.Images.OrderBy(i => i.OrderIndex).Select(i => i.Id).ToArray(),
            dto.Images.Select(i => i.Id).ToArray());
        Assert.Equal("B đổi mô tả", dto.Images.Single(i => i.Id == b.Id).AltText);
        Assert.Equal("https://storage.local/presigned", dto.Images.Single(i => i.Id == b.Id).PresignedUrl);
        Assert.Equal("https://storage.local/presigned", dto.Images.Single(i => i.Id == a.Id).PresignedUrl);
        // Mỗi ảnh đúng một lần được ký -> không ký thừa, không bỏ sót.
        Assert.Equal(2, signer.SignedKeys.Count);
        Assert.Equal(signer.SignedKeys.Count, signer.SignedKeys.Distinct().Count());
    }
}
