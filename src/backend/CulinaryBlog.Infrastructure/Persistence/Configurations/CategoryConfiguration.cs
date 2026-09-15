using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

// Placeholder cho C1 solo — TV2 (B1) thay bằng bản đầy đủ khi tích hợp.
public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories");
        b.HasKey(c => c.Id);
        b.Property(c => c.Name).HasMaxLength(100).IsRequired();
        b.Property(c => c.Slug).HasMaxLength(120).IsRequired();
        b.Property(c => c.Description);
        b.Property(c => c.ImageUrl).HasMaxLength(500);
        b.Property(c => c.OrderIndex).IsRequired();
        b.Property(c => c.RowVersion).HasColumnType("bytea").IsConcurrencyToken().IsRequired();

        b.HasIndex(c => c.Name).IsUnique();
        b.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("IDX_Category_Slug");
    }
}
