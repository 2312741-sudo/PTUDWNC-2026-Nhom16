using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

// SRS 7.8. RefreshToken KHÔNG phải BaseEntity (D24). Placeholder — logic rotation ở C5.
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(t => t.Id);
        b.Property(t => t.UserId).HasMaxLength(450).IsRequired();
        b.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        b.Property(t => t.ExpiresAt).IsRequired();
        b.Property(t => t.RevokedAt);
        b.Property(t => t.ReplacedByTokenHash).HasMaxLength(64);
        b.Property(t => t.CreatedAt).IsRequired();
        b.Property(t => t.CreatedByIp).HasMaxLength(45);

        b.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("IDX_RefreshToken_Hash");

        b.HasOne<RecipeAuthorUser>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);           // SRS 7.8
    }
}
