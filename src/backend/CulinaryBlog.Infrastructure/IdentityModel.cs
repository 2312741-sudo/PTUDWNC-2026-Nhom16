using CulinaryBlog.Domain;
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
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Recipe> Recipes => Set<Recipe>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            b.Property(x => x.AvatarUrl).HasMaxLength(500);
            b.HasIndex(x => x.NormalizedEmail).IsUnique();
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
            b.Property(x => x.UpdatedAt).IsRequired();
            b.Ignore(x => x.RecipesCount);

            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasIndex(x => x.Slug).IsUnique();
            b.HasIndex(x => x.Name);
        });

        builder.Entity<Recipe>(b =>
        {
            b.ToTable("Recipes");
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(220).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            b.Property(x => x.Instructions).IsRequired();
            b.Property(x => x.PrepTimeMinutes).IsRequired();
            b.Property(x => x.CookTimeMinutes).IsRequired();
            b.Property(x => x.Servings).IsRequired();
            b.Property(x => x.Difficulty).HasMaxLength(50).IsRequired();
            b.Property(x => x.Status).HasMaxLength(50).IsRequired();
            b.Property(x => x.AuthorId).IsRequired();
            b.Property(x => x.PrimaryImageUrl).HasMaxLength(500);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();

            b.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasIndex(x => x.Slug).IsUnique();
            b.HasIndex(x => x.CategoryId);
            b.HasIndex(x => x.AuthorId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.Difficulty);
            b.HasIndex(x => x.PublishedAt);
            b.HasIndex(x => x.CreatedAt);
        });
    }
}
