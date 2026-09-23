using CulinaryBlog.Application;
using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure;

/// <summary>
/// Hiện thực IRecipeRepository bằng EF Core. Đặt ở Infrastructure để lớp Application
/// không phụ thuộc EF Core (D18 — kiểm chứng bằng architecture test NFR-MAINT-004).
/// Theo đúng mẫu CategoryRepository của TV2.
/// </summary>
public sealed class RecipeRepository(AuthDbContext db) : IRecipeRepository
{
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

    /// <summary>
    /// IgnoreQueryFilters: bản ghi đã soft delete vẫn chiếm slug trong unique index,
    /// nên phải tính cả chúng khi sinh slug mới (D14).
    /// </summary>
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
}
