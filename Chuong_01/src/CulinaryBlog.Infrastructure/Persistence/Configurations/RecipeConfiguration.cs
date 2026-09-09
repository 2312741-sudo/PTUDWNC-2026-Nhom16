using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CulinaryBlog.Infrastructure.Persistence.Configurations;
public sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> b)
    {
        b.ToTable("Recipes", t => {
            t.HasCheckConstraint("CK_Recipes_Times", "\"PrepTimeMinutes\" >= 0 AND \"CookTimeMinutes\" >= 0");
            t.HasCheckConstraint("CK_Recipes_Servings", "\"Servings\" > 0");
            t.HasCheckConstraint("CK_Recipes_Difficulty", "\"Difficulty\" IN (0, 1, 2)");
        });
        b.HasKey(r => r.Id);
        b.Property(r => r.Title).HasMaxLength(200).IsRequired();
        b.Property(r => r.Description).HasMaxLength(4000);
        b.Property(r => r.Instructions).HasMaxLength(20000).IsRequired();
        b.HasOne(r => r.Category).WithMany().HasForeignKey(r => r.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(r => new { r.CategoryId, r.Difficulty });
        b.HasIndex(r => new { r.CreatedAt, r.Id });
        b.HasIndex(r => r.CookTimeMinutes);
    }
}
