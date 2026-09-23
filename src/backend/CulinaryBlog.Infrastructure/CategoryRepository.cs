using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure;

public sealed class CategoryRepository(AuthDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(bool onlyWithRecipes, CancellationToken ct)
    {
        var query = db.Categories.AsNoTracking().Where(c => !c.IsDeleted);

        if (onlyWithRecipes)
        {
            query = query.Where(c => db.Recipes.Any(r => r.CategoryId == c.Id && r.Status == Domain.Enums.RecipeStatus.Published && !r.IsDeleted));
        }

        // Query ordered by OrderIndex ascending, then Name ascending (as per D29)
        return await query
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken ct) =>
        db.Categories.FirstOrDefaultAsync(c => c.Slug == slug && !c.IsDeleted, ct);

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct) =>
        db.Categories.AnyAsync(c => !c.IsDeleted && c.Name.ToLower() == name.ToLower() && (!excludeId.HasValue || c.Id != excludeId.Value), ct);

    public Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId, CancellationToken ct) =>
        db.Categories.AnyAsync(c => !c.IsDeleted && c.Slug == slug && (!excludeId.HasValue || c.Id != excludeId.Value), ct);

    public Task<int> CountRecipesAsync(Guid categoryId, CancellationToken ct) =>
        db.Recipes.CountAsync(r => r.CategoryId == categoryId && r.Status == Domain.Enums.RecipeStatus.Published && !r.IsDeleted, ct);

    public async Task AddAsync(Category category, CancellationToken ct) =>
        await db.Categories.AddAsync(category, ct);

    public Task UpdateAsync(Category category, CancellationToken ct)
    {
        db.Categories.Update(category);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Category category, CancellationToken ct)
    {
        db.Categories.Remove(category);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}
