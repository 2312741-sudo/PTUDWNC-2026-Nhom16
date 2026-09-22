using CulinaryBlog.Application;
using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure;

/// <summary>
/// Query Recipe kèm collection Images để aggregate (AddImage/SetPrimaryImage/RemoveImage)
/// hoạt động trên bất biến của nó (D17/D19). Load qua Include ở Infrastructure để
/// Application không phụ thuộc EF Core (D18).
/// </summary>
public sealed class RecipeImageRepository(AuthDbContext db) : IRecipeImageRepository
{
    public Task<Recipe?> GetRecipeWithImagesAsync(Guid recipeId, CancellationToken ct) =>
        db.Recipes.Include(r => r.Images).FirstOrDefaultAsync(r => r.Id == recipeId, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}