using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;                                    
namespace ConcurrencySpike;

/// <summary>
/// Dựng schema đầy đủ (Identity + Recipe) trên Postgres nháp, seed sẵn 1 user + 1 category để FK hợp lệ.
///   export SPIKE_DB="Host=localhost;Port=5432;Database=culinary_spike;Username=postgres;Password=postgres"
/// Dùng đúng ApplicationDbContext + AuditableEntityInterceptor thật.
/// </summary>
public sealed class SpikeDbFixture : IAsyncLifetime
{
    public string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("SPIKE_DB")
        ?? "Host=localhost;Port=5432;Database=culinary_spike;Username=postgres;Password=postgres";

    public string AuthorId { get; } = Guid.NewGuid().ToString();
    public Guid CategoryId { get; private set; }

    public ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .AddInterceptors(new AuditableEntityInterceptor(TimeProvider.System))
            .Options;
        return new ApplicationDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var ctx = NewContext();
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.EnsureCreatedAsync();   // tạo cả AspNetUsers + Recipe tables

        ctx.Users.Add(new RecipeAuthorUser
        {
            Id = AuthorId, UserName = "author@spike.local", NormalizedUserName = "AUTHOR@SPIKE.LOCAL",
            Email = "author@spike.local", NormalizedEmail = "AUTHOR@SPIKE.LOCAL", EmailConfirmed = true,
            DisplayName = "Spike Author", IsActive = true, CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        });

        var cat = Category.Create("Spike", "spike", "danh mục spike");
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
