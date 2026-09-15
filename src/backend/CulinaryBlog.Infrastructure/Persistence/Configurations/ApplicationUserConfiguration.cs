using CulinaryBlog.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

// Custom columns theo SRS 7.7 (bảng AspNetUsers do Identity tạo). Placeholder — TV1 (A1) mở rộng.
public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> b)
    {
        b.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        b.Property(u => u.AvatarUrl).HasMaxLength(500);
        b.Property(u => u.IsActive).IsRequired();
        b.Property(u => u.CreatedAt).IsRequired();
    }
}
