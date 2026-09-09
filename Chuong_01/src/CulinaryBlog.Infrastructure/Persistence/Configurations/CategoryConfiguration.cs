using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CulinaryBlog.Infrastructure.Persistence.Configurations;
public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories"); b.HasKey(c => c.Id);
        b.Property(c => c.Name).HasMaxLength(200).IsRequired();
        b.Property(c => c.Slug).HasMaxLength(200).IsRequired();
        b.HasIndex(c => c.Slug).IsUnique();
        b.Property(c => c.Description).HasMaxLength(2000);
    }
}
