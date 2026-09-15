using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Common.Interfaces;

/// <summary>
/// Cổng truy cập dữ liệu cho lớp Application (CQRS handler dùng interface này thay vì DbContext cụ thể).
/// LƯU Ý TÍCH HỢP: đây là hợp đồng CHUNG - TV1 bổ sung DbSet&lt;ApplicationUser&gt;/RefreshToken,
/// TV2 bổ sung DbSet&lt;Category&gt;. Phần dưới là những DbSet do TV3 sở hữu (Recipe aggregate).
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Recipe> Recipes { get; }
    DbSet<RecipeIngredient> RecipeIngredients { get; }
    DbSet<RecipeStep> RecipeSteps { get; }
    DbSet<RecipeImage> RecipeImages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
