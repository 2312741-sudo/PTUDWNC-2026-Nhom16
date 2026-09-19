using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain;              
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CulinaryBlog.Domain.Enums;
namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> b)
    {
        b.ToTable("Recipes", t =>
        {
            t.HasCheckConstraint("CK_Recipes_PrepTime", "\"PrepTimeMinutes\" > 0");
            t.HasCheckConstraint("CK_Recipes_CookTime", "\"CookTimeMinutes\" >= 0");
            t.HasCheckConstraint("CK_Recipes_Servings", "\"Servings\" > 0");
        });
        b.HasKey(r => r.Id);

        b.Property(r => r.Title).HasMaxLength(200).IsRequired();
        b.Property(r => r.Slug).HasMaxLength(220).IsRequired();
        b.Property(r => r.Description).HasMaxLength(2000).IsRequired();
        b.Property(r => r.Instructions).IsRequired().HasDefaultValue(string.Empty); // D28
        b.Property(r => r.PrepTimeMinutes).IsRequired();
        b.Property(r => r.CookTimeMinutes).IsRequired();
        b.Property(r => r.Servings).IsRequired();
        b.Property(r => r.Difficulty).HasConversion<short>().IsRequired().HasDefaultValue(RecipeDifficulty.Easy);
        b.Property(r => r.Status).HasConversion<short>().IsRequired().HasDefaultValue(RecipeStatus.Draft);
        b.Property(r => r.PublishedAt);
        b.Property(r => r.CategoryId).IsRequired();
        b.Property(r => r.AuthorId).HasMaxLength(450).IsRequired();
        b.Property(r => r.RowVersion).HasColumnType("bytea").IsConcurrencyToken().IsRequired();

        // Owned Nutrition -> 6 cột Nutrition_* trong bảng Recipes (D28 / SRS 7.2.1)
        b.OwnsOne(r => r.Nutrition, n =>
        {
            n.Property(x => x.Calories).HasColumnName("Nutrition_Calories").HasColumnType("numeric(8,2)");
            n.Property(x => x.Protein).HasColumnName("Nutrition_Protein").HasColumnType("numeric(8,2)");
            n.Property(x => x.Carbohydrates).HasColumnName("Nutrition_Carbohydrates").HasColumnType("numeric(8,2)");
            n.Property(x => x.Fat).HasColumnName("Nutrition_Fat").HasColumnType("numeric(8,2)");
            n.Property(x => x.Fiber).HasColumnName("Nutrition_Fiber").HasColumnType("numeric(8,2)");
            n.Property(x => x.Sodium).HasColumnName("Nutrition_Sodium").HasColumnType("numeric(8,2)");
        });
        b.Navigation(r => r.Nutrition).IsRequired();

        // Child collections (aggregate quản lý qua backing field)
        b.HasMany(r => r.Ingredients).WithOne().HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(r => r.Steps).WithOne().HasForeignKey(s => s.RecipeId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(r => r.Images).WithOne().HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(r => r.Ingredients).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(r => r.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(r => r.Images).UsePropertyAccessMode(PropertyAccessMode.Field);

        // FK THẬT (SRS 6.4 / 7.2)
        b.HasOne<Category>()
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);          // chặn xóa category còn recipe (C07 → 409)

        b.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index theo SRS 7.2
        b.HasIndex(r => r.Slug).IsUnique().HasDatabaseName("IDX_Recipe_Slug");
        b.HasIndex(r => r.CategoryId).HasDatabaseName("IDX_Recipe_CategoryId");
        b.HasIndex(r => r.AuthorId).HasDatabaseName("IDX_Recipe_AuthorId");
        b.HasIndex(r => r.Status).HasDatabaseName("IDX_Recipe_Status");
        b.HasIndex(r => r.PublishedAt).HasDatabaseName("IDX_Recipe_PublishedAt");
        b.HasIndex(r => r.Difficulty).HasDatabaseName("IDX_Recipe_Difficulty");
        b.HasIndex(r => r.IsDeleted).HasDatabaseName("IDX_Recipe_IsDeleted").HasFilter("\"IsDeleted\" = false");
        // SearchVector (tsvector) + GIN: TV2 bổ sung bằng migration FTS riêng (D18). Không đặt ở Domain.
    }
}
