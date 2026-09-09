using CulinaryBlog.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
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
    }
}
