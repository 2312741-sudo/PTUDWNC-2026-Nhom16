using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public sealed class RecipeImageConfiguration : IEntityTypeConfiguration<RecipeImage>
{
    public void Configure(EntityTypeBuilder<RecipeImage> b)
    {
        b.ToTable("RecipeImages");
        b.HasKey(i => i.Id);

        b.Property(i => i.OriginalUrl).HasMaxLength(500).IsRequired();
        b.Property(i => i.MediumUrl).HasMaxLength(500);
        b.Property(i => i.ThumbnailUrl).HasMaxLength(500);
        b.Property(i => i.AltText).HasMaxLength(200);
        b.Property(i => i.IsPrimary).IsRequired();
        b.Property(i => i.OrderIndex).IsRequired();
        b.Property(i => i.RowVersion).HasColumnType("bytea").IsConcurrencyToken().IsRequired();

        b.HasIndex(i => new { i.RecipeId, i.OrderIndex });

        // Partial unique index: đúng 1 primary cho mỗi recipe (Postgres filtered index) - chống race 2 request set primary.
        b.HasIndex(i => i.RecipeId)
            .IsUnique()
            .HasDatabaseName("ux_recipe_images_one_primary")
            .HasFilter("\"IsPrimary\" = true");
    }
}
