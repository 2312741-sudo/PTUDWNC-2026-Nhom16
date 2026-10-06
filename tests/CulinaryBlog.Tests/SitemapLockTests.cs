using System.Xml.Linq;
using CulinaryBlog.API;
using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// N1-6 (tuần 4): sitemap có lịch chạy 02:00 UTC và khoá phân tán Redis.
///
/// Test cốt lõi là <see cref="Only_one_instance_generates_when_both_run_the_cron"/>: mô phỏng hai
/// API instance cùng nhận kích hoạt từ một cron. Trước khi có lock, cả hai đều ghi sitemap;
/// giờ chỉ một thắng và instance còn lại nhận Acquired = false kèm lý do.
///
/// Cần Redis thật nên theo quy ước của repo: bỏ qua nếu chưa dựng.
/// </summary>
public sealed class SitemapLockTests : IAsyncLifetime
{
    private const string Instance = "sitemap-test";
    private ConnectionMultiplexer? _redis;

    private static bool RedisIsReachable()
    {
        try
        {
            var host = EnvFileLoader.Get("REDIS_HOST", "127.0.0.1");
            var port = int.Parse(EnvFileLoader.Get("REDIS_PORT", "6379"));
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            using var mux = ConnectionMultiplexer.Connect(RecipeCacheService.BuildConfiguration(
                new RedisOptions { Host = host, Port = port }));
            mux.GetDatabase().Ping();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task InitializeAsync()
    {
        if (!RedisIsReachable()) return;
        var host = EnvFileLoader.Get("REDIS_HOST", "127.0.0.1");
        var port = int.Parse(EnvFileLoader.Get("REDIS_PORT", "6379"));
        _redis = await ConnectionMultiplexer.ConnectAsync(RecipeCacheService.BuildConfiguration(
            new RedisOptions { Host = host, Port = port, Instance = Instance }));
    }

    public async Task DisposeAsync()
    {
        if (_redis is null) return;
        await _redis.GetDatabase().KeyDeleteAsync(
        [
            $"{Instance}:sitemap:xml", $"{Instance}:lock:sitemap"
        ]);
        await _redis.CloseAsync();
        _redis.Dispose();
    }

    private SitemapGenerator NewGenerator(IRecipeDiscoveryRepository repository) => new(
        _redis!,
        Options.Create(new RedisOptions { Instance = Instance, LockSeconds = 30 }),
        Options.Create(new SitemapOptions { BaseUrl = "https://culinary.example" }),
        repository,
        NullLogger<SitemapGenerator>.Instance);

    [Fact]
    public async Task Only_one_instance_generates_when_both_run_the_cron()
    {
        if (_redis is null) return;

        var repositoryA = new FakeRepository();
        var repositoryB = new FakeRepository();
        var generatorA = NewGenerator(repositoryA);
        var generatorB = NewGenerator(repositoryB);

        // Hai instance chạy đồng thời, đúng như Hangfire kích hoạt cron trên cả hai server.
        var results = await Task.WhenAll(generatorA.GenerateAsync(), generatorB.GenerateAsync());

        Assert.Equal(1, results.Count(r => r.Acquired));
        Assert.Equal(1, results.Count(r => !r.Acquired));
        Assert.All(results.Where(r => !r.Acquired),
            r => Assert.False(string.IsNullOrWhiteSpace(r.Reason)));

        // Chỉ một instance được gọi xuống DB; instance thua không query DB lãng phí.
        Assert.Equal(1, repositoryA.Calls + repositoryB.Calls);
    }

    [Fact]
    public async Task Generated_sitemap_is_stored_in_redis_and_contains_published_recipes()
    {
        if (_redis is null) return;

        var repository = new FakeRepository();
        var generator = NewGenerator(repository);

        var result = await generator.GenerateAsync();
        Assert.True(result.Acquired);
        Assert.Equal(2, result.UrlCount);

        // Instance khác đọc được đúng bản vừa sinh — đây là lý do lưu Redis chứ không lưu đĩa.
        var other = NewGenerator(new FakeRepository());
        var xml = await other.GetCachedXmlAsync();
        Assert.NotNull(xml);

        XDocument doc;
        try
        {
            doc = XDocument.Parse(xml);
        }
        catch (System.Xml.XmlException ex)
        {
            throw new Xunit.Sdk.XunitException($"sitemap phải là XML hợp lệ nhưng parse lỗi: {ex.Message}\n{xml}");
        }

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var locs = doc.Root!.Elements(ns + "url").Select(u => u.Element(ns + "loc")!.Value).ToList();
        Assert.Contains("https://culinary.example/recipes/pho-bo", locs);
        Assert.Contains("https://culinary.example/recipes/bun-bo", locs);
        Assert.Contains("https://culinary.example/recipes", locs);
    }

    [Fact]
    public async Task Lock_is_released_after_generation_so_the_next_cron_can_run()
    {
        if (_redis is null) return;

        var generator = NewGenerator(new FakeRepository());
        Assert.True((await generator.GenerateAsync()).Acquired);
        // Nếu lock không được nhả, lần chạy kế tiếp sẽ bị bỏ qua — đúng thứ ta không muốn.
        Assert.True((await generator.GenerateAsync()).Acquired);
    }

    private sealed class FakeRepository : IRecipeDiscoveryRepository
    {
        public int Calls;

        public Task<PagedResult<RecipeSummaryDto>> GetPublishedRecipesAsync(GetRecipesQuery query, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<PagedResult<RecipeSummaryDto>> SearchPublishedRecipesAsync(SearchRecipesQuery query, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<List<SitemapRecipeDto>> GetPublishedForSitemapAsync(CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(new List<SitemapRecipeDto>
            {
                new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "pho-bo", new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)),
                new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "bun-bo", null)
            });
        }
    }
}
