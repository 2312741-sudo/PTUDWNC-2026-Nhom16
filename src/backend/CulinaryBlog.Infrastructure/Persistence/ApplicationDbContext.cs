using System.Linq.Expressions;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>
/// DbContext DUY NHẤT của hệ thống. Kế thừa IdentityDbContext để có bảng AspNetUsers/Roles... (SRS 7.7).
/// C1 (TV3) sở hữu cụm Recipe; Category/RecipeAuthorUser/RefreshToken là bản tối thiểu cho solo dev,
/// sẽ được TV2/TV1 mở rộng khi tích hợp (một context duy nhất — không tạo bản thứ hai).
///
/// IApplicationDbContext được hiện thực tường minh bằng IQueryable: lớp Application không phụ thuộc
/// EF Core (quyết định D18, kiểm chứng bằng architecture test - NFR-MAINT-004).
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<RecipeAuthorUser, IdentityRole, string>(options), IApplicationDbContext
{
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeImage> RecipeImages => Set<RecipeImage>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    IQueryable<Recipe> IApplicationDbContext.Recipes => Recipes;

    IQueryable<RecipeIngredient> IApplicationDbContext.RecipeIngredients => RecipeIngredients;

    IQueryable<RecipeStep> IApplicationDbContext.RecipeSteps => RecipeSteps;

    IQueryable<RecipeImage> IApplicationDbContext.RecipeImages => RecipeImages;

    void IApplicationDbContext.Add<TEntity>(TEntity entity) => Add(entity);

    void IApplicationDbContext.Remove<TEntity>(TEntity entity) => Remove(entity);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);   // BẮT BUỘC gọi trước: cấu hình Identity

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filter soft delete (D08 / SRS 7.1) cho mọi entity kế thừa BaseEntity.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)) continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var body = Expression.Equal(
                Expression.Property(parameter, nameof(BaseEntity.IsDeleted)),
                Expression.Constant(false));
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }
}
