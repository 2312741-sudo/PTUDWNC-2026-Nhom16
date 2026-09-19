using CulinaryBlog.Domain;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace ConcurrencySpike;

/// <summary>
/// Dựng schema trên một database Postgres RIÊNG, seed sẵn 1 user + 1 category để FK hợp lệ.
///
/// Thứ tự chọn chuỗi kết nối:
///   1. SPIKE_DB      - biến riêng khi chạy ở máy dev.
///   2. TEST_DATABASE - biến CI đặt sẵn; giữ host/user/password nhưng ĐỔI tên database
///                      sang "culinary_spike" để không đụng database của test khác.
///   3. Mặc định localhost.
///
/// Fixture gọi EnsureDeleted/EnsureCreated nên BẮT BUỘC dùng database riêng.
/// </summary>
public sealed class SpikeDbFixture : IAsyncLifetime
{
    public string ConnectionString { get; } = ResolveConnectionString();

    public string AuthorId { get; } = Guid.NewGuid().ToString();

    public Guid CategoryId { get; private set; }

    private static string ResolveConnectionString()
    {
        var spike = Environment.GetEnvironmentVariable("SPIKE_DB");
        if (!string.IsNullOrWhiteSpace(spike))
        {
            return spike;
        }

        var shared = Environment.GetEnvironmentVariable("TEST_DATABASE");
        if (!string.IsNullOrWhiteSpace(shared))
        {
            var builder = new NpgsqlConnectionStringBuilder(shared)
            {
                Database = "culinary_spike"
            };
            return builder.ConnectionString;
        }

        return "Host=localhost;Port=5432;Database=culinary_spike;Username=postgres;Password=postgres";
    }

    public AuthDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(ConnectionString)
            .AddInterceptors(new AuditableEntityInterceptor(TimeProvider.System))
            .Options;

        return new AuthDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var ctx = NewContext();
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.EnsureCreatedAsync();

        ctx.Users.Add(new ApplicationUser
        {
            Id = AuthorId,
            UserName = "author@spike.local",
            NormalizedUserName = "AUTHOR@SPIKE.LOCAL",
            Email = "author@spike.local",
            NormalizedEmail = "AUTHOR@SPIKE.LOCAL",
            EmailConfirmed = true,
            DisplayName = "Spike Author",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        });

        var cat = new Category("Spike", "spike", "danh muc spike");
        ctx.Categories.Add(cat);

        await ctx.SaveChangesAsync();
        CategoryId = cat.Id;
    }

    public async Task DisposeAsync()
    {
        await using var ctx = NewContext();
        await ctx.Database.EnsureDeletedAsync();
    }
}
