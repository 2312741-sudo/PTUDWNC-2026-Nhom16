using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure;

public sealed class RecipeRepository(AuthDbContext db) : IRecipeRepository
{
    public async Task<PagedResult<RecipeSummaryDto>> GetPublishedRecipesAsync(GetRecipesQuery query, CancellationToken ct)
    {
        var baseQuery = db.Recipes
            .AsNoTracking()
            .Where(r => r.Status == RecipeStatusValues.Published);

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
                CategoryName = r.Category != null ? r.Category.Name : "Khác",
                AuthorDisplayName = db.Users.Where(u => u.Id == r.AuthorId).Select(u => u.DisplayName).FirstOrDefault() ?? "Đầu bếp"
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
            Difficulty: x.Recipe.Difficulty,
            Status: x.Recipe.Status,
            PrimaryImageUrl: x.Recipe.PrimaryImageUrl,
            PublishedAt: x.Recipe.PublishedAt,
            CreatedAt: x.Recipe.CreatedAt
        )).ToList();

        return new PagedResult<RecipeSummaryDto>(items, PaginationMeta.Create(page, pageSize, total));
    }

    public async Task<PagedResult<RecipeSummaryDto>> SearchPublishedRecipesAsync(SearchRecipesQuery query, CancellationToken ct)
    {
        var normalizedQuery = query.Q.Trim().ToLowerInvariant();
        var slugSearch = SlugHelper.GenerateSlug(normalizedQuery);

        var baseQuery = db.Recipes
            .AsNoTracking()
            .Where(r => r.Status == RecipeStatusValues.Published);

        baseQuery = ApplyFilters(baseQuery, query.CategoryId, query.Difficulty, query.MaxCookTime, query.MinServings);

        // Search in Title or Description using EF.Functions.ILike or unaccent search
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
                CategoryName = r.Category != null ? r.Category.Name : "Khác",
                AuthorDisplayName = db.Users.Where(u => u.Id == r.AuthorId).Select(u => u.DisplayName).FirstOrDefault() ?? "Đầu bếp"
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
            Difficulty: x.Recipe.Difficulty,
            Status: x.Recipe.Status,
            PrimaryImageUrl: x.Recipe.PrimaryImageUrl,
            PublishedAt: x.Recipe.PublishedAt,
            CreatedAt: x.Recipe.CreatedAt
        )).ToList();

        return new PagedResult<RecipeSummaryDto>(items, PaginationMeta.Create(page, pageSize, total));
    }

    public async Task AddAsync(Recipe recipe, CancellationToken ct) =>
        await db.Recipes.AddAsync(recipe, ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);

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

        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            var diff = difficulty.Trim();
            query = query.Where(r => r.Difficulty.ToLower() == diff.ToLower());
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
