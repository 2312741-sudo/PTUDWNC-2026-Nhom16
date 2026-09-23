using System.Linq.Expressions;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using Recipe = CulinaryBlog.Domain.Entities.Recipe;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string FullName { get => DisplayName; set => DisplayName = value; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IApplicationDbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeImage> RecipeImages => Set<RecipeImage>();
    public DbSet<CulinaryBlog.Domain.Entities.RefreshToken> RefreshTokens => Set<CulinaryBlog.Domain.Entities.RefreshToken>();

    // Hiện thực tường minh IApplicationDbContext — Application chỉ thấy IQueryable (D18)
    IQueryable<Recipe> IApplicationDbContext.Recipes => Recipes;
    IQueryable<RecipeIngredient> IApplicationDbContext.RecipeIngredients => RecipeIngredients;
    IQueryable<RecipeStep> IApplicationDbContext.RecipeSteps => RecipeSteps;
    IQueryable<RecipeImage> IApplicationDbContext.RecipeImages => RecipeImages;
    IQueryable<Category> IApplicationDbContext.Categories => Categories;
    IQueryable<CulinaryBlog.Domain.Entities.RefreshToken> IApplicationDbContext.RefreshTokens => RefreshTokens;

    void IApplicationDbContext.Add<TEntity>(TEntity entity) => Add(entity);
    void IApplicationDbContext.Remove<TEntity>(TEntity entity) => Remove(entity);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            b.Property(x => x.AvatarUrl).HasMaxLength(500);
            b.HasIndex(x => x.NormalizedEmail).IsUnique();
        });
        builder.Entity<CulinaryBlog.Domain.Entities.RefreshToken>(b =>
        {
            b.ToTable("RefreshTokens");
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            b.Property(x => x.ExpiresAt).IsRequired();
            b.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);
            b.Property(x => x.CreatedByIp).HasMaxLength(45);
            b.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("IDX_RefreshToken_Hash");
            b.HasIndex(x => x.UserId);
        });
        builder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = "role-author", Name = CulinaryBlog.Domain.Roles.Author, NormalizedName = "AUTHOR", ConcurrencyStamp = "role-author-v1" },
            new IdentityRole { Id = "role-admin", Name = CulinaryBlog.Domain.Roles.Admin, NormalizedName = "ADMIN", ConcurrencyStamp = "role-admin-v1" });

        builder.Entity<Category>(b =>
        {
            b.ToTable("Categories");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500);
            b.Property(x => x.ImageUrl).HasMaxLength(500);
            b.Property(x => x.OrderIndex).HasDefaultValue(0);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired(false);
            b.Ignore(x => x.RecipesCount);

            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasIndex(x => x.Slug).IsUnique();
            b.HasIndex(x => x.Name);
        });

        // Nạp cấu hình Recipe aggregate (RecipeConfiguration, RecipeStepConfiguration...)
        builder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);

        // Soft delete đồng nhất cho mọi BaseEntity (D08 / C01)
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)) continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var body = Expression.Equal(
                Expression.Property(parameter, nameof(BaseEntity.IsDeleted)),
                Expression.Constant(false));
            builder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }
}
