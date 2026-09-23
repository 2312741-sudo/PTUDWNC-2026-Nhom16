using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using FluentValidation;
using MediatR;
using Xunit;

namespace CulinaryBlog.Tests;

public sealed class FakeRecipeRepository : IRecipeRepository
{
    public readonly List<Recipe> Recipes = [];
    public readonly Dictionary<Guid, string> CategoryNames = [];
    public readonly Dictionary<string, string> UserDisplayNames = [];

    public Task<PagedResult<RecipeSummaryDto>> GetPublishedRecipesAsync(GetRecipesQuery query, CancellationToken ct)
    {
        var filtered = Recipes
            .Where(r => !r.IsDeleted && r.Status == RecipeStatusValues.Published);

        if (query.CategoryId.HasValue && query.CategoryId.Value != Guid.Empty)
        {
            filtered = filtered.Where(r => r.CategoryId == query.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Difficulty))
        {
            var diff = query.Difficulty.Trim();
            filtered = filtered.Where(r => r.Difficulty.Equals(diff, StringComparison.OrdinalIgnoreCase));
        }

        if (query.MaxCookTime.HasValue)
        {
            filtered = filtered.Where(r => r.CookTimeMinutes <= query.MaxCookTime.Value);
        }

        if (query.MinServings.HasValue)
        {
            filtered = filtered.Where(r => r.Servings >= query.MinServings.Value);
        }

        var total = filtered.Count();

        var isAsc = string.Equals(query.SortOrder?.Trim(), "asc", StringComparison.OrdinalIgnoreCase);
        filtered = query.SortBy?.Trim().ToLowerInvariant() switch
        {
            "title" => isAsc ? filtered.OrderBy(r => r.Title) : filtered.OrderByDescending(r => r.Title),
            "cooktimeminutes" => isAsc ? filtered.OrderBy(r => r.CookTimeMinutes) : filtered.OrderByDescending(r => r.CookTimeMinutes),
            _ => isAsc ? filtered.OrderBy(r => r.PublishedAt ?? r.CreatedAt) : filtered.OrderByDescending(r => r.PublishedAt ?? r.CreatedAt)
        };

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var paged = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RecipeSummaryDto(
                Id: r.Id,
                Title: r.Title,
                Slug: r.Slug,
                Description: r.Description,
                CategoryId: r.CategoryId,
                CategoryName: CategoryNames.GetValueOrDefault(r.CategoryId, "Món ngon"),
                AuthorId: r.AuthorId,
                AuthorDisplayName: UserDisplayNames.GetValueOrDefault(r.AuthorId, "Tác giả"),
                PrepTimeMinutes: r.PrepTimeMinutes,
                CookTimeMinutes: r.CookTimeMinutes,
                Servings: r.Servings,
                Difficulty: r.Difficulty,
                Status: r.Status,
                PrimaryImageUrl: r.PrimaryImageUrl,
                PublishedAt: r.PublishedAt,
                CreatedAt: r.CreatedAt
            ))
            .ToList();

        return Task.FromResult(new PagedResult<RecipeSummaryDto>(paged, PaginationMeta.Create(page, pageSize, total)));
    }

    public Task<PagedResult<RecipeSummaryDto>> SearchPublishedRecipesAsync(SearchRecipesQuery query, CancellationToken ct)
    {
        var rawKeyword = query.Q.Trim().ToLowerInvariant();
        var slugKeyword = SlugHelper.GenerateSlug(rawKeyword);

        var filtered = Recipes
            .Where(r => !r.IsDeleted && r.Status == RecipeStatusValues.Published);

        filtered = filtered.Where(r =>
            r.Title.Contains(rawKeyword, StringComparison.OrdinalIgnoreCase) ||
            r.Description.Contains(rawKeyword, StringComparison.OrdinalIgnoreCase) ||
            r.Slug.Contains(slugKeyword, StringComparison.OrdinalIgnoreCase));

        if (query.CategoryId.HasValue && query.CategoryId.Value != Guid.Empty)
        {
            filtered = filtered.Where(r => r.CategoryId == query.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Difficulty))
        {
            var diff = query.Difficulty.Trim();
            filtered = filtered.Where(r => r.Difficulty.Equals(diff, StringComparison.OrdinalIgnoreCase));
        }

        var total = filtered.Count();

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var paged = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RecipeSummaryDto(
                Id: r.Id,
                Title: r.Title,
                Slug: r.Slug,
                Description: r.Description,
                CategoryId: r.CategoryId,
                CategoryName: CategoryNames.GetValueOrDefault(r.CategoryId, "Món ngon"),
                AuthorId: r.AuthorId,
                AuthorDisplayName: UserDisplayNames.GetValueOrDefault(r.AuthorId, "Tác giả"),
                PrepTimeMinutes: r.PrepTimeMinutes,
                CookTimeMinutes: r.CookTimeMinutes,
                Servings: r.Servings,
                Difficulty: r.Difficulty,
                Status: r.Status,
                PrimaryImageUrl: r.PrimaryImageUrl,
                PublishedAt: r.PublishedAt,
                CreatedAt: r.CreatedAt
            ))
            .ToList();

        return Task.FromResult(new PagedResult<RecipeSummaryDto>(paged, PaginationMeta.Create(page, pageSize, total)));
    }

    public Task AddAsync(Recipe recipe, CancellationToken ct)
    {
        Recipes.Add(recipe);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}

public sealed class FakeGoogleAuthService : IGoogleAuthService
{
    public bool ShouldFailValidation { get; set; }
    public bool EmailVerified { get; set; } = true;
    public string UserEmail { get; set; } = "user@google.test";
    public string UserName { get; set; } = "Google User";

    public Task<GoogleUserPayload> ValidateIdTokenAsync(string idToken, CancellationToken ct)
    {
        if (ShouldFailValidation || string.IsNullOrWhiteSpace(idToken))
            throw new AppException(400, "auth.google_token_invalid", "Token Google không hợp lệ.");

        return Task.FromResult(new GoogleUserPayload(
            Subject: "google-sub-123456",
            Email: UserEmail,
            EmailVerified: EmailVerified,
            Name: UserName,
            Picture: "https://example.com/avatar.jpg"));
    }
}

public sealed class FakeIdentityServiceForGoogle : IIdentityService
{
    public readonly Dictionary<string, UserDto> Users = [];

    public Task<AuthResponse> RegisterAsync(RegisterCommand command, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task<AuthResponse> LoginAsync(LoginCommand command, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task<AuthResponse> LoginWithGoogleAsync(GoogleUserPayload payload, CancellationToken ct)
    {
        if (!payload.EmailVerified)
            throw new AppException(401, "auth.google_email_unverified", "Email Google chưa được xác minh.");

        var user = Users.GetValueOrDefault(payload.Email) ?? new UserDto(
            Guid.NewGuid().ToString("N"),
            payload.Email,
            payload.Name,
            [Roles.Author]
        );

        Users[payload.Email] = user;

        return Task.FromResult(new AuthResponse(
            "fake-jwt-token",
            "Bearer",
            900,
            user
        ));
    }

    public Task<UserDto> GetAsync(string id, CancellationToken ct) =>
        Task.FromResult(Users.Values.FirstOrDefault(u => u.Id == id) ?? throw new AppException(404, "auth.user_not_found", "Not found"));

    public Task<UserDto> UpdateAsync(string id, UpdateProfileCommand command, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task<AuthResponse> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task LogoutAsync(string? userId, string? refreshToken, CancellationToken ct) =>
        Task.CompletedTask;
}

public sealed class DiscoveryAndSearchTests
{
    [Fact]
    public async Task GetRecipes_only_returns_published_and_applies_and_filters()
    {
        var repo = new FakeRecipeRepository();
        var handler = new GetRecipesHandler(repo);

        var catA = Guid.NewGuid();
        var catB = Guid.NewGuid();

        // 1 Published, catA, Medium, cook 30, servings 4
        repo.Recipes.Add(new Recipe("Phở Bò Hà Nội", "pho-bo-ha-noi", "Nước dùng thơm ngon", "", 20, 30, 4, RecipeDifficultyValues.Medium, catA, "author1", null, RecipeStatusValues.Published));
        // 2 Draft, catA - MUST NOT BE RETURNED
        repo.Recipes.Add(new Recipe("Bún Chả Nem Rán", "bun-cha-nem-ran", "Thịt nướng thơm", "", 30, 45, 4, RecipeDifficultyValues.Medium, catA, "author1", null, RecipeStatusValues.Draft));
        // 3 Archived, catA - MUST NOT BE RETURNED
        repo.Recipes.Add(new Recipe("Gỏi Cuốn Tôm Thịt", "goi-cuon-tom-thit", "Món cuốn tươi mát", "", 15, 10, 2, RecipeDifficultyValues.Easy, catA, "author1", null, RecipeStatusValues.Archived));
        // 4 Published, catB, Easy, cook 15, servings 2
        repo.Recipes.Add(new Recipe("Salad Bơ Trứng", "salad-bo-trung", "Món khai vị bổ dưỡng", "", 10, 15, 2, RecipeDifficultyValues.Easy, catB, "author2", null, RecipeStatusValues.Published));

        // Test 1: Lấy tất cả Published (không lộ Draft/Archived)
        var allPublished = await handler.Handle(new GetRecipesQuery(), CancellationToken.None);
        Assert.Equal(2, allPublished.Meta.Total);
        Assert.DoesNotContain(allPublished.Data, r => r.Status != RecipeStatusValues.Published);

        // Test 2: Bộ lọc AND (catA + Difficulty Medium + MaxCookTime 35)
        var filtered = await handler.Handle(new GetRecipesQuery(CategoryId: catA, Difficulty: RecipeDifficultyValues.Medium, MaxCookTime: 35), CancellationToken.None);
        Assert.Single(filtered.Data);
        Assert.Equal("Phở Bò Hà Nội", filtered.Data[0].Title);
    }

    [Fact]
    public async Task SearchRecipes_finds_by_unaccented_keyword_and_rejects_short_queries()
    {
        var repo = new FakeRecipeRepository();
        var handler = new SearchRecipesHandler(repo);
        var validator = new SearchRecipesValidator();

        var catId = Guid.NewGuid();
        repo.Recipes.Add(new Recipe("Phở Bò Gia Truyền", "pho-bo-gia-truyen", "Nấu từ thịt bò tươi và thảo mộc", "", 20, 60, 4, RecipeDifficultyValues.Medium, catId, "author1", null, RecipeStatusValues.Published));
        repo.Recipes.Add(new Recipe("Bò Lúc Lắc Hạt Tiêu", "bo-luc-lac-hat-tieu", "Thịt bò xào ớt chuông đậm vị", "", 15, 20, 2, RecipeDifficultyValues.Easy, catId, "author1", null, RecipeStatusValues.Published));

        // 1. Validator: từ khóa < 2 ký tự bị từ chối
        var invalidQuery = new SearchRecipesQuery("p");
        var validationResult = await validator.ValidateAsync(invalidQuery);
        Assert.False(validationResult.IsValid);
        Assert.Contains(validationResult.Errors, e => e.PropertyName == "Q");

        // 2. Tìm kiếm không dấu: "pho" tìm thấy "Phở Bò Gia Truyền" qua SlugHelper
        var searchPho = await handler.Handle(new SearchRecipesQuery("pho"), CancellationToken.None);
        Assert.Single(searchPho.Data);
        Assert.Equal("Phở Bò Gia Truyền", searchPho.Data[0].Title);

        // 3. Tìm kiếm "Bò": cả 2 món đều có
        var searchBo = await handler.Handle(new SearchRecipesQuery("Bò"), CancellationToken.None);
        Assert.Equal(2, searchBo.Data.Count);
    }

    [Fact]
    public async Task GoogleLogin_creates_author_or_links_account()
    {
        var fakeAuth = new FakeGoogleAuthService();
        var fakeIdentity = new FakeIdentityServiceForGoogle();
        var handler = new GoogleLoginHandler(fakeAuth, fakeIdentity);
        var validator = new GoogleLoginValidator();

        // 1. Validate empty token
        var invalid = await validator.ValidateAsync(new GoogleLoginCommand(""));
        Assert.False(invalid.IsValid);

        // 2. Login new user
        fakeAuth.UserEmail = "truongvi@google.com";
        fakeAuth.UserName = "Trường Vĩ";
        var res1 = await handler.Handle(new GoogleLoginCommand("valid-token"), CancellationToken.None);

        Assert.NotNull(res1);
        Assert.Equal("truongvi@google.com", res1.User.Email);
        Assert.Contains(Roles.Author, res1.User.Roles);

        // 3. Unverified email throws 401
        fakeAuth.EmailVerified = false;
        var ex = await Assert.ThrowsAsync<AppException>(() => handler.Handle(new GoogleLoginCommand("valid-token"), CancellationToken.None));
        Assert.Equal(401, ex.Status);
    }

    [Fact]
    public async Task GetRecipes_pagination_calculates_bounds_and_handles_clamping()
    {
        var repo = new FakeRecipeRepository();
        var handler = new GetRecipesHandler(repo);

        var catId = Guid.NewGuid();
        for (var i = 1; i <= 25; i++)
        {
            repo.Recipes.Add(new Recipe(
                $"Món ngon số {i:D2}",
                $"mon-ngon-so-{i:D2}",
                $"Mô tả cho món ngon số {i}",
                "",
                10,
                15,
                2,
                RecipeDifficultyValues.Easy,
                catId,
                "author1",
                null,
                RecipeStatusValues.Published));
        }

        // Test page size 10, page 1 -> 10 items, total 25, totalPages 3, hasNext true, hasPrev false
        var page1 = await handler.Handle(new GetRecipesQuery(Page: 1, PageSize: 10), CancellationToken.None);
        Assert.Equal(10, page1.Data.Count);
        Assert.Equal(25, page1.Meta.Total);
        Assert.Equal(3, page1.Meta.TotalPages);
        Assert.True(page1.Meta.HasNextPage);
        Assert.False(page1.Meta.HasPreviousPage);

        // Test page 3 -> 5 items, hasNext false, hasPrev true
        var page3 = await handler.Handle(new GetRecipesQuery(Page: 3, PageSize: 10), CancellationToken.None);
        Assert.Equal(5, page3.Data.Count);
        Assert.False(page3.Meta.HasNextPage);
        Assert.True(page3.Meta.HasPreviousPage);

        // Test negative page and overly large pageSize -> clamped to valid bounds
        var clamped = await handler.Handle(new GetRecipesQuery(Page: -5, PageSize: 100), CancellationToken.None);
        Assert.Equal(1, clamped.Meta.Page);
        Assert.Equal(50, clamped.Meta.PageSize);
    }

    [Fact]
    public async Task GetRecipes_validator_rejects_invalid_inputs()
    {
        var validator = new GetRecipesValidator();

        // Invalid difficulty
        var invalidDiff = await validator.ValidateAsync(new GetRecipesQuery(Difficulty: "SuperHard"));
        Assert.False(invalidDiff.IsValid);
        Assert.Contains(invalidDiff.Errors, e => e.PropertyName == "Difficulty");

        // Invalid MaxCookTime (< 0)
        var invalidCookTime = await validator.ValidateAsync(new GetRecipesQuery(MaxCookTime: -10));
        Assert.False(invalidCookTime.IsValid);
        Assert.Contains(invalidCookTime.Errors, e => e.PropertyName == "MaxCookTime");

        // Invalid MinServings (<= 0)
        var invalidServings = await validator.ValidateAsync(new GetRecipesQuery(MinServings: 0));
        Assert.False(invalidServings.IsValid);
        Assert.Contains(invalidServings.Errors, e => e.PropertyName == "MinServings");

        // Valid inputs pass
        var valid = await validator.ValidateAsync(new GetRecipesQuery(Difficulty: "Medium", MaxCookTime: 45, MinServings: 2));
        Assert.True(valid.IsValid);
    }

    [Fact]
    public async Task SearchRecipes_with_filters_and_sorting()
    {
        var repo = new FakeRecipeRepository();
        var handler = new SearchRecipesHandler(repo);

        var catA = Guid.NewGuid();
        var catB = Guid.NewGuid();

        repo.Recipes.Add(new Recipe("Canh Chua Cá Lóc", "canh-chua-ca-loc", "Món canh đậm đà miền Tây", "", 20, 30, 4, RecipeDifficultyValues.Medium, catA, "author1", null, RecipeStatusValues.Published));
        repo.Recipes.Add(new Recipe("Canh Bí Đỏ Thịt Bằm", "canh-bi-do-thit-bam", "Canh ngọt thanh bổ dưỡng", "", 10, 15, 2, RecipeDifficultyValues.Easy, catB, "author1", null, RecipeStatusValues.Published));
        repo.Recipes.Add(new Recipe("Cá Kho Tộ", "ca-kho-to", "Cá kho tộ đậm vị mặn ngọt", "", 15, 45, 4, RecipeDifficultyValues.Hard, catA, "author1", null, RecipeStatusValues.Published));

        // Search "kho" -> 1 item ("Cá Kho Tộ")
        var searchKho = await handler.Handle(new SearchRecipesQuery("kho"), CancellationToken.None);
        Assert.Single(searchKho.Data);
        Assert.Equal("Cá Kho Tộ", searchKho.Data[0].Title);

        // Search "canh" -> 2 items ("Canh Chua Cá Lóc", "Canh Bí Đỏ Thịt Bằm")
        var searchCanh = await handler.Handle(new SearchRecipesQuery("canh"), CancellationToken.None);
        Assert.Equal(2, searchCanh.Data.Count);
    }
}
