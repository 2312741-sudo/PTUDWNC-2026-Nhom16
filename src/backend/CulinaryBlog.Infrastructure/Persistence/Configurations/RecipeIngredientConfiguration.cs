using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public sealed class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> b)
    {
        b.ToTable("RecipeIngredients");
        b.HasKey(i => i.Id);

        b.Property(i => i.Name).HasMaxLength(200).IsRequired();          // D15: 1..200
        b.Property(i => i.Quantity).HasColumnType("numeric(10,3)");     // nullable, >0 nếu có
        b.Property(i => i.Unit).HasMaxLength(50);                       // nullable
        b.Property(i => i.Notes).HasMaxLength(500);
        b.Property(i => i.OrderIndex).IsRequired();
        b.Property(i => i.RowVersion).HasColumnType("bytea").IsConcurrencyToken().IsRequired();

        b.HasIndex(i => new { i.RecipeId, i.OrderIndex });
    }
}
