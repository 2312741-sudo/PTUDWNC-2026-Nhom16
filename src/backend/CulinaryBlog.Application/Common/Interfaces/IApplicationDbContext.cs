using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Entities;
using Recipe = CulinaryBlog.Domain.Entities.Recipe;
using RefreshToken = CulinaryBlog.Domain.Entities.RefreshToken;

namespace CulinaryBlog.Application.Common.Interfaces;

/// <summary>
/// Cổng truy cập dữ liệu cho lớp Application.
/// Dùng IQueryable (BCL) thay vì DbSet để Application không phụ thuộc EF Core — quyết định D18,
/// kiểm chứng bằng architecture test (NFR-MAINT-004).
/// </summary>
public interface IApplicationDbContext
{
    IQueryable<Recipe> Recipes { get; }
    IQueryable<RecipeIngredient> RecipeIngredients { get; }
    IQueryable<RecipeStep> RecipeSteps { get; }
    IQueryable<RecipeImage> RecipeImages { get; }
    IQueryable<Category> Categories { get; }
    IQueryable<RefreshToken> RefreshTokens { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
