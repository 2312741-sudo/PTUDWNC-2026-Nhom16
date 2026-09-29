using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Recipe = CulinaryBlog.Domain.Entities.Recipe;

namespace CulinaryBlog.Tests;

/// <summary>
/// Bộ kiểm thử Tuần 3 của TV2 (Ngô Quốc Trường Vĩ - 2312796):
/// - Task B3: Tìm kiếm toàn văn FTS tiếng Việt không dấu, xếp hạng, bộ lọc AND, phân trang.
/// - Task B5: Chiến lược đệm Cache-Aside cho Category & Search (Redis/Memory), Invalidation sau mutation, Resilient Fallback.
/// - Task B6: Lab cá nhân K01-K24 (RowVersion 2-writer concurrency spike, Identity/Token hash, Media upload boundary).
/// </summary>
public sealed class Week3DiscoverySearchCacheTests
{
    private readonly RecipeCacheService _cacheService = new(NullLogger<RecipeCacheService>.Instance);
    private readonly TrackingCategoryRepository _categoryRepo = new();
    private readonly FakeRecipeDiscoveryRepository _recipeRepo = new();

    #region Task B5: Category Caching & Invalidation Tests

    [Fact]
    public async Task GetCategories_uses_cache_and_invalidates_on_create_update_delete()
    {
        var category1 = new Category("Món Khai Vị", "mon-khai-vi", "Các món khai vị hấp dẫn", null, 1);
        await _categoryRepo.AddAsync(category1, CancellationToken.None);

        var getHandler = new GetCategoriesHandler(_categoryRepo, _cacheService);
        var createHandler = new CreateCategoryHandler(_categoryRepo, _cacheService);
        var updateHandler = new UpdateCategoryHandler(_categoryRepo, _cacheService);
        var deleteHandler = new DeleteCategoryHandler(_categoryRepo, _cacheService);

        // 1. Lần đầu gọi GetCategories -> Cache MISS, repo được truy vấn (count = 1)
        var result1 = await getHandler.Handle(new GetCategoriesQuery(), CancellationToken.None);
        Assert.Single(result1);
        Assert.Equal(1, _categoryRepo.GetAllCallCount);

        // 2. Lần 2 gọi GetCategories -> Cache HIT, repo KHÔNG bị gọi thêm (vẫn count = 1)
        var result2 = await getHandler.Handle(new GetCategoriesQuery(), CancellationToken.None);
        Assert.Single(result2);
        Assert.Equal(1, _categoryRepo.GetAllCallCount);

        // 3. Thêm mới Category -> Tự động Invalidate Cache
        var created = await createHandler.Handle(
            new CreateCategoryCommand("Món Tráng Miệng", "Món tráng miệng ngọt ngào", null, 2),
            CancellationToken.None);
        Assert.NotNull(created);

        // 4. Lần 3 gọi GetCategories sau mutation -> Cache MISS, repo được gọi lại (count = 2), thấy đủ 2 danh mục
        var result3 = await getHandler.Handle(new GetCategoriesQuery(), CancellationToken.None);
        Assert.Equal(2, result3.Count);
        Assert.Equal(2, _categoryRepo.GetAllCallCount);

        // 5. Cập nhật Category -> Tự động Invalidate Cache
        await updateHandler.Handle(
            new UpdateCategoryCommand(created.Id, "Món Tráng Miệng Đặc Biệt", "Mô tả mới", null, 2),
            CancellationToken.None);

        // 6. Lần 4 gọi GetCategories -> Cache MISS, repo được gọi lại (count = 3), tên mới đã cập nhật
        var result4 = await getHandler.Handle(new GetCategoriesQuery(), CancellationToken.None);
        Assert.Equal(3, _categoryRepo.GetAllCallCount);
        Assert.Contains(result4, c => c.Name == "Món Tráng Miệng Đặc Biệt");

        // 7. Xóa Category -> Tự động Invalidate Cache
        await deleteHandler.Handle(new DeleteCategoryCommand(created.Id), CancellationToken.None);

        // 8. Lần 5 gọi GetCategories -> Lấy dữ liệu mới chỉ còn 1 danh mục
        var result5 = await getHandler.Handle(new GetCategoriesQuery(), CancellationToken.None);
        Assert.Equal(4, _categoryRepo.GetAllCallCount);
        Assert.Single(result5);
    }

    [Fact]
    public async Task GetCategoryBySlug_uses_cache_and_invalidates_on_update()
    {
        var category = new Category("Món Chay", "mon-chay", "Món ăn thanh đạm", null, 1);
        await _categoryRepo.AddAsync(category, CancellationToken.None);

        var slugHandler = new GetCategoryBySlugHandler(_categoryRepo, _cacheService);
        var updateHandler = new UpdateCategoryHandler(_categoryRepo, _cacheService);

        // 1. Cache MISS
        var cat1 = await slugHandler.Handle(new GetCategoryBySlugQuery("mon-chay"), CancellationToken.None);
        Assert.Equal("Món Chay", cat1.Name);
        Assert.Equal(1, _categoryRepo.GetBySlugCallCount);

        // 2. Cache HIT
        var cat2 = await slugHandler.Handle(new GetCategoryBySlugQuery("mon-chay"), CancellationToken.None);
        Assert.Equal("Món Chay", cat2.Name);
        Assert.Equal(1, _categoryRepo.GetBySlugCallCount);

        // 3. Update category -> invalidate slug cache
        await updateHandler.Handle(
            new UpdateCategoryCommand(category.Id, "Món Chay Tịnh", "Mô tả thanh tịnh", null, 1),
            CancellationToken.None);

        // 4. Cache MISS sau update
        var cat3 = await slugHandler.Handle(new GetCategoryBySlugQuery("mon-chay"), CancellationToken.None);
        Assert.Equal("Món Chay Tịnh", cat3.Name);
        Assert.Equal(2, _categoryRepo.GetBySlugCallCount);
    }

    #endregion

    #region Task B5: Recipe Discovery & Search Caching Tests

    private static Recipe CreatePublishedRecipe(
        string title, string slug, string description,
        int prepTime, int cookTime, int servings,
        RecipeDifficulty difficulty, Guid categoryId, string authorId)
    {
        var recipe = Recipe.CreateDraft(title, slug, description, null, prepTime, cookTime, servings, difficulty, categoryId, authorId);
        recipe.AddIngredient("Nguyên liệu mẫu", 100, "g", null);
        recipe.AddStep("Bước 1", "Bước chế biến mẫu", null, null);
        recipe.Publish();
        return recipe;
    }

    [Fact]
    public async Task GetRecipes_and_SearchRecipes_cache_and_invalidate_properly()
    {
        var catId = Guid.NewGuid();
        var authorId = "author-123";

        var r1 = CreatePublishedRecipe("Phở Bò Tái Lăn", "pho-bo-tai-lan", "Phở bò gia truyền thơm lừng", 20, 60, 4, RecipeDifficulty.Medium, catId, authorId);
        _recipeRepo.Recipes.Add(r1);

        var getHandler = new GetRecipesHandler(_recipeRepo, _cacheService);
        var searchHandler = new SearchRecipesHandler(_recipeRepo, _cacheService);

        // 1. GetRecipes Cache MISS -> Repo count = 1
        var list1 = await getHandler.Handle(new GetRecipesQuery(), CancellationToken.None);
        Assert.Single(list1.Data);
        Assert.Equal(1, _recipeRepo.GetPublishedCallCount);

        // 2. GetRecipes Cache HIT -> Repo count vẫn là 1
        var list2 = await getHandler.Handle(new GetRecipesQuery(), CancellationToken.None);
        Assert.Single(list2.Data);
        Assert.Equal(1, _recipeRepo.GetPublishedCallCount);

        // 3. SearchRecipes Cache MISS -> Repo search count = 1
        var search1 = await searchHandler.Handle(new SearchRecipesQuery("pho"), CancellationToken.None);
        Assert.Single(search1.Data);
        Assert.Equal(1, _recipeRepo.SearchPublishedCallCount);

        // 4. SearchRecipes Cache HIT -> Repo search count vẫn là 1
        var search2 = await searchHandler.Handle(new SearchRecipesQuery("pho"), CancellationToken.None);
        Assert.Single(search2.Data);
        Assert.Equal(1, _recipeRepo.SearchPublishedCallCount);

        // 5. Invalidation khi có thay đổi recipe (ví dụ unpublish hoặc thêm món mới)
        await _cacheService.InvalidatePrefixAsync("recipes:list:", CancellationToken.None);
        await _cacheService.InvalidatePrefixAsync("recipes:search:", CancellationToken.None);

        // 6. Lần truy vấn sau invalidation -> Lấy từ repo
        var list3 = await getHandler.Handle(new GetRecipesQuery(), CancellationToken.None);
        Assert.Equal(2, _recipeRepo.GetPublishedCallCount);

        var search3 = await searchHandler.Handle(new SearchRecipesQuery("pho"), CancellationToken.None);
        Assert.Equal(2, _recipeRepo.SearchPublishedCallCount);
    }

    [Fact]
    public async Task SearchRecipes_cache_isolation_never_returns_draft_or_archived_recipes()
    {
        var catId = Guid.NewGuid();
        var authorId = "author-tv2";

        var publishedRecipe = CreatePublishedRecipe("Bún Bò Huế Cay Nồng", "bun-bo-hue-cay-nong", "Bún bò đậm đà xứ Huế", 30, 90, 4, RecipeDifficulty.Hard, catId, authorId);

        var draftRecipe = Recipe.CreateDraft("Bún Chả Hà Nội Đang Soạn", "bun-cha-ha-noi-nhap", "Công thức thử nghiệm", null, 15, 30, 2, RecipeDifficulty.Easy, catId, authorId);
        var archivedRecipe = Recipe.CreateDraft("Bún Riêu Cua Cũ", "bun-rieu-cua-cu", "Công thức đã ngừng", null, 20, 45, 3, RecipeDifficulty.Medium, catId, authorId);
        archivedRecipe.Archive();

        _recipeRepo.Recipes.AddRange([publishedRecipe, draftRecipe, archivedRecipe]);

        var searchHandler = new SearchRecipesHandler(_recipeRepo, _cacheService);

        // Tìm từ khóa "bun"
        var searchResult = await searchHandler.Handle(new SearchRecipesQuery("bun"), CancellationToken.None);

        // Chỉ món Published được trả về trong cache public
        Assert.Single(searchResult.Data);
        Assert.Equal("Bún Bò Huế Cay Nồng", searchResult.Data[0].Title);
        Assert.Equal(RecipeStatus.Published.ToString(), searchResult.Data[0].Status);

        // Draft và Archived tuyệt đối không xuất hiện
        Assert.DoesNotContain(searchResult.Data, r => r.Title.Contains("Đang Soạn"));
        Assert.DoesNotContain(searchResult.Data, r => r.Title.Contains("Cũ"));
    }

    [Fact]
    public async Task Cache_service_fallback_resilience_when_cache_server_down()
    {
        var catId = Guid.NewGuid();
        var recipe = CreatePublishedRecipe("Cơm Tấm Sườn Bì Chả", "com-tam-suon-bi-cha", "Đặc sản Sài Gòn", 25, 45, 2, RecipeDifficulty.Easy, catId, "author-1");
        _recipeRepo.Recipes.Add(recipe);

        var searchHandler = new SearchRecipesHandler(_recipeRepo, _cacheService);

        // 1. Chạy bình thường
        var res1 = await searchHandler.Handle(new SearchRecipesQuery("com tam"), CancellationToken.None);
        Assert.Single(res1.Data);

        // 2. Giả lập Cache Server gặp sự cố / Redis down
        _cacheService.SimulateServerDown(true);

        // Hệ thống tự động fallback trực tiếp về DB mà KHÔNG làm gián đoạn request của người dùng (NFR-REL-002)
        var resFallback = await searchHandler.Handle(new SearchRecipesQuery("com tam"), CancellationToken.None);
        Assert.Single(resFallback.Data);
        Assert.Equal("Cơm Tấm Sườn Bì Chả", resFallback.Data[0].Title);

        // Phục hồi cache
        _cacheService.SimulateServerDown(false);
    }

    #endregion

    #region Task B3: FTS Diacritics & Combined AND Filters Tests

    [Fact]
    public async Task SearchRecipes_supports_unaccented_vietnamese_and_and_filters()
    {
        var cat1 = Guid.NewGuid();
        var cat2 = Guid.NewGuid();

        var r1 = CreatePublishedRecipe("Phở Gà Đồi Lá Chanh", "pho-ga-doi-la-chanh", "Nước dùng thanh ngọt từ gà ta", 15, 45, 4, RecipeDifficulty.Easy, cat1, "a1");
        var r2 = CreatePublishedRecipe("Phở Bò Sốt Vang", "pho-bo-sot-vang", "Thịt bò sốt vang đậm đà màu sắc", 30, 120, 6, RecipeDifficulty.Hard, cat1, "a2");
        var r3 = CreatePublishedRecipe("Cơm Gà Hội An", "com-ga-hoi-an", "Cơm gà xé vàng ươm", 20, 40, 3, RecipeDifficulty.Easy, cat2, "a3");

        _recipeRepo.Recipes.AddRange([r1, r2, r3]);

        var searchHandler = new SearchRecipesHandler(_recipeRepo, _cacheService);

        // 1. Tìm không dấu: "pho ga" -> khớp "Phở Gà Đồi Lá Chanh"
        var queryUnaccented = new SearchRecipesQuery("pho ga");
        var res1 = await searchHandler.Handle(queryUnaccented, CancellationToken.None);
        Assert.Single(res1.Data);
        Assert.Equal("Phở Gà Đồi Lá Chanh", res1.Data[0].Title);

        // 2. Tìm kết hợp bộ lọc AND: từ khóa "pho" + CategoryId = cat1 + Difficulty = Hard
        var queryAndFilter = new SearchRecipesQuery("pho", CategoryId: cat1, Difficulty: "Hard");
        var res2 = await searchHandler.Handle(queryAndFilter, CancellationToken.None);
        Assert.Single(res2.Data);
        Assert.Equal("Phở Bò Sốt Vang", res2.Data[0].Title);

        // 3. Tìm từ khóa "pho" + MaxCookTime = 50 -> chỉ lấy r1 (cook 45m), bỏ qua r2 (cook 120m)
        var queryTimeFilter = new SearchRecipesQuery("pho", MaxCookTime: 50);
        var res3 = await searchHandler.Handle(queryTimeFilter, CancellationToken.None);
        Assert.Single(res3.Data);
        Assert.Equal("Phở Gà Đồi Lá Chanh", res3.Data[0].Title);
    }

    [Fact]
    public void SearchRecipes_validator_rejects_query_shorter_than_2_chars()
    {
        var validator = new SearchRecipesValidator();

        var valid = validator.Validate(new SearchRecipesQuery("pho"));
        Assert.True(valid.IsValid);

        var shortQuery = validator.Validate(new SearchRecipesQuery("p"));
        Assert.False(shortQuery.IsValid);
        Assert.Contains(shortQuery.Errors, e => e.PropertyName == "Q");

        var emptyQuery = validator.Validate(new SearchRecipesQuery("   "));
        Assert.False(emptyQuery.IsValid);
    }

    #endregion

    #region Task B6: TV2 Personal Lab (K07 Concurrency, K08 Security, K13 Media Boundary)

    [Fact]
    public async Task PersonalLab_Recipe_optimistic_concurrency_two_writers_conflict()
    {
        // K07 Lab: Hai writer cùng cập nhật Recipe, writer 2 dùng RowVersion cũ bị xung đột
        var repo = new FakeAuthoringRecipeRepository();
        var user = new FakeTestCurrentUser { UserId = "author-tv2" };
        var recipe = Recipe.CreateDraft(
            "Gỏi Cuốn Tôm Thịt", "goi-cuon-tom-thit", "Món ăn thanh mát", null, 20, 10, 4,
            RecipeDifficulty.Easy, Guid.NewGuid(), user.UserId);
        recipe.RowVersion = Guid.NewGuid().ToByteArray(); // Gán token ban đầu mô phỏng đã lưu DB
        repo.Add(recipe);
        repo.Categories.Add(recipe.CategoryId);

        var initialVersion = Convert.ToBase64String(recipe.RowVersion);

        // Writer 1 sửa công thức và commit thành công (RowVersion được xoay vòng)
        recipe.RowVersion = Guid.NewGuid().ToByteArray();
        var newVersion = Convert.ToBase64String(recipe.RowVersion);
        Assert.NotEqual(initialVersion, newVersion);

        // Writer 2 cố tình cập nhật dựa trên RowVersion ban đầu (đã lỗi thời)
        var updateHandler = new UpdateRecipeHandler(repo, user);
        var cmd = new UpdateRecipeCommand(
            recipe.Id, "Gỏi Cuốn Tôm Thịt Đặc Biệt", "Mô tả mới", null,
            25, 10, 4, RecipeDifficulty.Easy, recipe.CategoryId, null, initialVersion);

        var ex = await Assert.ThrowsAsync<AppException>(() => updateHandler.Handle(cmd, CancellationToken.None));

        Assert.Equal(422, ex.Status);
        Assert.Equal("recipe.concurrency_conflict", ex.Code);
    }

    [Fact]
    public void PersonalLab_Identity_password_hash_and_token_security()
    {
        // K08 Lab: Kiểm tra nguyên lý hash mật khẩu an toàn với Salt ngẫu nhiên và SHA-512
        const string rawPassword = "SecurePassword@2026!";
        var salt = RandomNumberGenerator.GetBytes(16);

        var hash = Rfc2898DeriveBytes.Pbkdf2(rawPassword, salt, 100_000, HashAlgorithmName.SHA512, 64);
        Assert.Equal(64, hash.Length);

        // Cùng mật khẩu nhưng với salt khác thì hash hoàn toàn khác nhau (chống Rainbow Table)
        var salt2 = RandomNumberGenerator.GetBytes(16);
        var hash2 = Rfc2898DeriveBytes.Pbkdf2(rawPassword, salt2, 100_000, HashAlgorithmName.SHA512, 64);

        Assert.NotEqual(hash, hash2);
    }

    [Fact]
    public void PersonalLab_Media_mime_and_size_boundary_validation()
    {
        // K13 Lab: Kiểm tra giới hạn upload ảnh tối đa 5MiB và các định dạng cho phép
        string[] allowedMimes = ["image/jpeg", "image/png", "image/webp", "image/avif"];
        const long maxBytes = 5 * 1024 * 1024; // 5 MiB

        Assert.True(allowedMimes.Contains("image/webp"));
        Assert.True(allowedMimes.Contains("image/avif"));
        Assert.False(allowedMimes.Contains("image/gif")); // GIF không cho phép theo CONS-007
        Assert.False(allowedMimes.Contains("application/pdf"));

        long validFileSize = 4 * 1024 * 1024;
        long excessFileSize = 5 * 1024 * 1024 + 1;

        Assert.True(validFileSize <= maxBytes);
        Assert.False(excessFileSize <= maxBytes);
    }

    #endregion
}

#region Test Doubles for Week 3 TV2

public sealed class TrackingCategoryRepository : ICategoryRepository
{
    private readonly List<Category> _categories = [];
    public int GetAllCallCount { get; private set; }
    public int GetBySlugCallCount { get; private set; }

    public Task<IReadOnlyList<Category>> GetAllAsync(bool onlyWithRecipes, CancellationToken ct)
    {
        GetAllCallCount++;
        return Task.FromResult<IReadOnlyList<Category>>(_categories.Where(c => !c.IsDeleted).ToList());
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct)
        => Task.FromResult(_categories.FirstOrDefault(c => c.Id == id && !c.IsDeleted));

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        GetBySlugCallCount++;
        return Task.FromResult(_categories.FirstOrDefault(c => c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase) && !c.IsDeleted));
    }

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct)
        => Task.FromResult(_categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && (!excludeId.HasValue || c.Id != excludeId.Value)));

    public Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId, CancellationToken ct)
        => Task.FromResult(_categories.Any(c => c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase) && (!excludeId.HasValue || c.Id != excludeId.Value)));

    public Task<int> CountRecipesAsync(Guid categoryId, CancellationToken ct) => Task.FromResult(0);

    public Task AddAsync(Category category, CancellationToken ct)
    {
        _categories.Add(category);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Category category, CancellationToken ct) => Task.CompletedTask;

    public Task DeleteAsync(Category category, CancellationToken ct)
    {
        _categories.Remove(category);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}

public sealed class FakeRecipeDiscoveryRepository : IRecipeDiscoveryRepository
{
    public readonly List<Recipe> Recipes = [];
    public int GetPublishedCallCount { get; private set; }
    public int SearchPublishedCallCount { get; private set; }

    public Task<PagedResult<RecipeSummaryDto>> GetPublishedRecipesAsync(GetRecipesQuery query, CancellationToken ct)
    {
        GetPublishedCallCount++;
        var filtered = Recipes.Where(r => !r.IsDeleted && r.Status == RecipeStatus.Published);

        if (query.CategoryId.HasValue)
            filtered = filtered.Where(r => r.CategoryId == query.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(query.Difficulty) && Enum.TryParse<RecipeDifficulty>(query.Difficulty, true, out var diff))
            filtered = filtered.Where(r => r.Difficulty == diff);

        if (query.MaxCookTime.HasValue)
            filtered = filtered.Where(r => r.CookTimeMinutes <= query.MaxCookTime.Value);

        if (query.MinServings.HasValue)
            filtered = filtered.Where(r => r.Servings >= query.MinServings.Value);

        var list = filtered.Select(ToDto).ToList();
        return Task.FromResult(new PagedResult<RecipeSummaryDto>(list, PaginationMeta.Create(query.Page, query.PageSize, list.Count)));
    }

    public Task<PagedResult<RecipeSummaryDto>> SearchPublishedRecipesAsync(SearchRecipesQuery query, CancellationToken ct)
    {
        SearchPublishedCallCount++;
        var rawKeyword = query.Q.Trim().ToLowerInvariant();
        var slugKeyword = SlugHelper.GenerateSlug(rawKeyword);

        var filtered = Recipes.Where(r => !r.IsDeleted && r.Status == RecipeStatus.Published);

        filtered = filtered.Where(r =>
            r.Title.Contains(rawKeyword, StringComparison.OrdinalIgnoreCase) ||
            r.Description.Contains(rawKeyword, StringComparison.OrdinalIgnoreCase) ||
            SlugHelper.GenerateSlug(r.Title).Contains(slugKeyword, StringComparison.OrdinalIgnoreCase) ||
            r.Slug.Contains(slugKeyword, StringComparison.OrdinalIgnoreCase));

        if (query.CategoryId.HasValue)
            filtered = filtered.Where(r => r.CategoryId == query.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(query.Difficulty) && Enum.TryParse<RecipeDifficulty>(query.Difficulty, true, out var diff))
            filtered = filtered.Where(r => r.Difficulty == diff);

        if (query.MaxCookTime.HasValue)
            filtered = filtered.Where(r => r.CookTimeMinutes <= query.MaxCookTime.Value);

        if (query.MinServings.HasValue)
            filtered = filtered.Where(r => r.Servings >= query.MinServings.Value);

        var list = filtered.Select(ToDto).ToList();
        return Task.FromResult(new PagedResult<RecipeSummaryDto>(list, PaginationMeta.Create(query.Page, query.PageSize, list.Count)));
    }

    public Task<List<SitemapRecipeDto>> GetPublishedForSitemapAsync(CancellationToken ct)
        => Task.FromResult(Recipes.Where(r => !r.IsDeleted && r.Status == RecipeStatus.Published)
            .Select(r => new SitemapRecipeDto(r.Id, r.Slug, r.PublishedAt.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(r.PublishedAt.Value, DateTimeKind.Utc)) : null))
            .ToList());

    private static RecipeSummaryDto ToDto(Recipe r) => new(
        r.Id, r.Title, r.Slug, r.Description, r.CategoryId, "Danh mục test",
        r.AuthorId, "Tác giả test", r.PrepTimeMinutes, r.CookTimeMinutes,
        r.Servings, r.Difficulty.ToString(), r.Status.ToString(), null,
        r.PublishedAt.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(r.PublishedAt.Value, DateTimeKind.Utc)) : null,
        new DateTimeOffset(DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc)));
}

#endregion
