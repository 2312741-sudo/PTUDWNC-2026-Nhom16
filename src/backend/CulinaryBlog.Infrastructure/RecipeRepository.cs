using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Recipe = CulinaryBlog.Domain.Entities.Recipe;

namespace CulinaryBlog.Infrastructure;

/// <summary>
/// Hiện thực IRecipeRepository (Authoring/CRUD TV3) và IRecipeDiscoveryRepository (Search/Discovery TV2) bằng EF Core.
/// </summary>
public sealed class RecipeRepository(AuthDbContext db) : IRecipeRepository, IRecipeDiscoveryRepository
{
    public async Task<PagedResult<RecipeSummaryDto>> GetPublishedRecipesAsync(GetRecipesQuery query, CancellationToken ct)
    {
        var baseQuery = db.Recipes
            .AsNoTracking()
            .Where(r => r.Status == RecipeStatus.Published && !r.IsDeleted);

        baseQuery = ApplyFilters(baseQuery, query.CategoryId, query.Difficulty, query.MaxCookTime, query.MinServings);

        var total = await baseQuery.CountAsync(ct);

        baseQuery = ApplySorting(baseQuery, query.SortBy, query.SortOrder);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var recipes = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                Recipe = r,
                CategoryName = db.Categories.Where(c => c.Id == r.CategoryId).Select(c => c.Name).FirstOrDefault() ?? "Khác",
                AuthorDisplayName = db.Users.Where(u => u.Id == r.AuthorId).Select(u => u.DisplayName).FirstOrDefault() ?? "Đầu bếp",
                PrimaryImageUrl = r.Images.Where(i => i.IsPrimary).Select(i => i.OriginalUrl).FirstOrDefault()
            })
            .ToListAsync(ct);

        var items = recipes.Select(x => new RecipeSummaryDto(
            Id: x.Recipe.Id,
            Title: x.Recipe.Title,
            Slug: x.Recipe.Slug,
            Description: x.Recipe.Description,
            CategoryId: x.Recipe.CategoryId,
            CategoryName: x.CategoryName,
            AuthorId: x.Recipe.AuthorId,
            AuthorDisplayName: x.AuthorDisplayName,
            PrepTimeMinutes: x.Recipe.PrepTimeMinutes,
            CookTimeMinutes: x.Recipe.CookTimeMinutes,
            Servings: x.Recipe.Servings,
            Difficulty: x.Recipe.Difficulty.ToString(),
            Status: x.Recipe.Status.ToString(),
            PrimaryImageUrl: x.PrimaryImageUrl,
            PublishedAt: x.Recipe.PublishedAt.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(x.Recipe.PublishedAt.Value, DateTimeKind.Utc)) : null,
            CreatedAt: new DateTimeOffset(DateTime.SpecifyKind(x.Recipe.CreatedAt, DateTimeKind.Utc))
        )).ToList();

        return new PagedResult<RecipeSummaryDto>(items, PaginationMeta.Create(page, pageSize, total));
    }

    public async Task<PagedResult<RecipeSummaryDto>> SearchPublishedRecipesAsync(SearchRecipesQuery query, CancellationToken ct)
    {
        var normalizedQuery = query.Q.Trim().ToLowerInvariant();
        var slugSearch = SlugHelper.GenerateSlug(normalizedQuery);

        var baseQuery = db.Recipes
            .AsNoTracking()
            .Where(r => r.Status == RecipeStatus.Published && !r.IsDeleted);

        baseQuery = ApplyFilters(baseQuery, query.CategoryId, query.Difficulty, query.MaxCookTime, query.MinServings);

        baseQuery = baseQuery.Where(r =>
            EF.Functions.ILike(r.Title, $"%{normalizedQuery}%") ||
            EF.Functions.ILike(r.Description, $"%{normalizedQuery}%") ||
            EF.Functions.ILike(r.Slug, $"%{slugSearch}%"));

        var total = await baseQuery.CountAsync(ct);

        baseQuery = ApplySorting(baseQuery, query.SortBy, query.SortOrder);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var recipes = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                Recipe = r,
                CategoryName = db.Categories.Where(c => c.Id == r.CategoryId).Select(c => c.Name).FirstOrDefault() ?? "Khác",
                AuthorDisplayName = db.Users.Where(u => u.Id == r.AuthorId).Select(u => u.DisplayName).FirstOrDefault() ?? "Đầu bếp",
                PrimaryImageUrl = r.Images.Where(i => i.IsPrimary).Select(i => i.OriginalUrl).FirstOrDefault()
            })
            .ToListAsync(ct);

        var items = recipes.Select(x => new RecipeSummaryDto(
            Id: x.Recipe.Id,
            Title: x.Recipe.Title,
            Slug: x.Recipe.Slug,
            Description: x.Recipe.Description,
            CategoryId: x.Recipe.CategoryId,
            CategoryName: x.CategoryName,
            AuthorId: x.Recipe.AuthorId,
            AuthorDisplayName: x.AuthorDisplayName,
            PrepTimeMinutes: x.Recipe.PrepTimeMinutes,
            CookTimeMinutes: x.Recipe.CookTimeMinutes,
            Servings: x.Recipe.Servings,
            Difficulty: x.Recipe.Difficulty.ToString(),
            Status: x.Recipe.Status.ToString(),
            PrimaryImageUrl: x.PrimaryImageUrl,
            PublishedAt: x.Recipe.PublishedAt.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(x.Recipe.PublishedAt.Value, DateTimeKind.Utc)) : null,
            CreatedAt: new DateTimeOffset(DateTime.SpecifyKind(x.Recipe.CreatedAt, DateTimeKind.Utc))
        )).ToList();

        return new PagedResult<RecipeSummaryDto>(items, PaginationMeta.Create(page, pageSize, total));
    }

    public Task<Recipe?> FindForWriteAsync(Guid id, CancellationToken ct) =>
        db.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Recipe?> FindBySlugAsync(string slug, CancellationToken ct) =>
        db.Recipes
            .AsNoTracking()
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Slug == slug, ct);

    public async Task<IReadOnlyList<string>> FindUsedSlugsAsync(
        string baseSlug, Guid? excludeRecipeId, CancellationToken ct) =>
        await db.Recipes
            .IgnoreQueryFilters()
            .Where(r => (r.Slug == baseSlug || r.Slug.StartsWith(baseSlug + "-"))
                        && (excludeRecipeId == null || r.Id != excludeRecipeId))
            .Select(r => r.Slug)
            .ToListAsync(ct);

    public Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken ct) =>
        db.Categories.AnyAsync(c => c.Id == categoryId, ct);

    public void Add(Recipe recipe) => db.Recipes.Add(recipe);

    public void RemoveIngredient(RecipeIngredient ingredient) => db.RecipeIngredients.Remove(ingredient);

    public void RemoveStep(RecipeStep step) => db.RecipeSteps.Remove(step);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    private static IQueryable<Recipe> ApplyFilters(
        IQueryable<Recipe> query,
        Guid? categoryId,
        string? difficulty,
        int? maxCookTime,
        int? minServings)
    {
        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
        {
            query = query.Where(r => r.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(difficulty) && Enum.TryParse<RecipeDifficulty>(difficulty.Trim(), true, out var diffEnum))
        {
            query = query.Where(r => r.Difficulty == diffEnum);
        }

        if (maxCookTime.HasValue)
        {
            query = query.Where(r => r.CookTimeMinutes <= maxCookTime.Value);
        }

        if (minServings.HasValue)
        {
            query = query.Where(r => r.Servings >= minServings.Value);
        }

        return query;
    }

    private static IQueryable<Recipe> ApplySorting(IQueryable<Recipe> query, string sortBy, string sortOrder)
    {
        var isAsc = string.Equals(sortOrder?.Trim(), "asc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "title" => isAsc ? query.OrderBy(r => r.Title) : query.OrderByDescending(r => r.Title),
            "cooktimeminutes" => isAsc ? query.OrderBy(r => r.CookTimeMinutes) : query.OrderByDescending(r => r.CookTimeMinutes),
            "preptimeminutes" => isAsc ? query.OrderBy(r => r.PrepTimeMinutes) : query.OrderByDescending(r => r.PrepTimeMinutes),
            _ => isAsc
                ? query.OrderBy(r => r.PublishedAt ?? r.CreatedAt)
                : query.OrderByDescending(r => r.PublishedAt ?? r.CreatedAt)
        };
    }
}
