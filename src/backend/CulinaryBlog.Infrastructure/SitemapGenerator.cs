using System.Xml.Linq;
using CulinaryBlog.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CulinaryBlog.Infrastructure;

/// <summary>Cấu hình sinh sitemap (N1-6).</summary>
public sealed class SitemapOptions
{
    public const string SectionName = "Sitemap";

    /// <summary>Origin của frontend — sitemap phải trỏ về domain thật, không phải localhost.</summary>
    public string BaseUrl { get; set; } = "http://localhost:3000";

    /// <summary>Cron UTC cho job sinh sitemap. Mặc định 02:00 UTC như yêu cầu tuần 4.</summary>
    public string Cron { get; set; } = "0 2 * * *";
}

public sealed record SitemapGenerateResult(bool Acquired, int UrlCount, string? Reason = null);

/// <summary>
/// N1-6 (tuần 4): sinh sitemap.xml theo lịch 02:00 UTC, có khoá phân tán qua Redis.
///
/// Vấn đề giải quyết: khi chạy NHIỀU API instance, mỗi instance đều có Hangfire server nên
/// cùng một cron sẽ kích hoạt job trên mọi instance. Trước đây không có gì chặn, kết quả là
/// nhiều tiến trình cùng ghi sitemap, dễ ghi đè lẫn nhau và nhân đôi log/nhân đôi tải DB.
/// LockTake của Redis đảm bảo chỉ một instance chạy; instance còn lại báo "skipped" có lý do.
///
/// Sitemap được lưu vào Redis (không phải đĩa) vì các container có hệ thống file riêng: ghi
/// xuống đĩa chỉ instance sinh ra có, các instance khác và cả lần chạy sau đều không thấy.
/// Lưu Redis thì mọi instance đọc chung một bản.
/// </summary>
public sealed class SitemapGenerator(
    IConnectionMultiplexer redis,
    IOptions<RedisOptions> redisOptions,
    IOptions<SitemapOptions> sitemapOptions,
    IRecipeDiscoveryRepository repository,
    ILogger<SitemapGenerator> logger)
{
    private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private string CacheKey => $"{redisOptions.Value.Instance}:sitemap:xml";
    private string LockKey => $"{redisOptions.Value.Instance}:lock:sitemap";

    /// <summary>
    /// Sinh sitemap nếu giữ được lock. Trả về <c>Acquired = false</c> khi instance khác đang chạy —
    /// đây là kết quả BÌNH THƯỜNG khi chạy nhiều instance, không phải lỗi.
    /// </summary>
    public async Task<SitemapGenerateResult> GenerateAsync(CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var token = Guid.NewGuid().ToString("N");
        var expiry = TimeSpan.FromSeconds(redisOptions.Value.LockSeconds);

        // LockTake = SET key token NX EX ttl: chỉ một client thắng, và khoá tự hết hạn nên không
        // kẹt vĩnh viễn nếu instance chết giữa chừng.
        if (!await db.LockTakeAsync(LockKey, token, expiry).ConfigureAwait(false))
        {
            logger.LogInformation("Bỏ qua sinh sitemap: một instance khác đang giữ lock {LockKey}", LockKey);
            return new SitemapGenerateResult(false, 0, "lock đang được instance khác giữ");
        }

        try
        {
            var recipes = await repository.GetPublishedForSitemapAsync(ct).ConfigureAwait(false);
            var xml = BuildXml(recipes);
            await db.StringSetAsync(CacheKey, xml).ConfigureAwait(false);
            logger.LogInformation("Đã sinh sitemap với {Count} URL lúc {Utc:O}", recipes.Count, DateTimeOffset.UtcNow);
            return new SitemapGenerateResult(true, recipes.Count);
        }
        finally
        {
            // Luôn nhả lock, kể cả khi sinh lỗi — nếu không thì sitemap sẽ không được sinh lại
            // cho tới hết ngày (tới khi TTL hết hạn).
            try
            {
                await db.LockReleaseAsync(LockKey, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Không nhả được lock sitemap {LockKey}; lock sẽ tự hết hạn sau {Expiry}", LockKey, expiry);
            }
        }
    }

    /// <summary>Sitemap đã lưu, hoặc <c>null</c> nếu chưa có bản nào (lúc mới deploy).</summary>
    public async Task<string?> GetCachedXmlAsync(CancellationToken ct = default)
    {
        try
        {
            var value = await redis.GetDatabase().StringGetAsync(CacheKey).ConfigureAwait(false);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex) when (ex is RedisException)
        {
            logger.LogWarning(ex, "Không đọc được sitemap từ Redis; sẽ sinh tại chỗ");
            return null;
        }
    }

    internal string BuildXml(List<SitemapRecipeDto> recipes)
    {
        var baseUrl = sitemapOptions.Value.BaseUrl.TrimEnd('/');
        var root = new XElement(SitemapNs + "urlset");

        // Trang tĩnh: không có PublishedAt nên dùng thời điểm sinh sitemap (Next.js cũng vậy).
        var generatedAt = DateTimeOffset.UtcNow;
        foreach (var path in new[] { "/", "/recipes", "/search", "/categories" })
        {
            root.Add(new XElement(SitemapNs + "url",
                new XElement(SitemapNs + "loc", baseUrl + path),
                new XElement(SitemapNs + "lastmod", generatedAt.ToString("yyyy-MM-dd")),
                new XElement(SitemapNs + "changefreq", path == "/" ? "daily" : "weekly"),
                new XElement(SitemapNs + "priority", path == "/" ? "1.0" : "0.8")));
        }

        foreach (var recipe in recipes)
        {
            root.Add(new XElement(SitemapNs + "url",
                new XElement(SitemapNs + "loc", $"{baseUrl}/recipes/{Uri.EscapeDataString(recipe.Slug)}"),
                new XElement(SitemapNs + "lastmod", (recipe.PublishedAt ?? generatedAt).ToString("yyyy-MM-dd")),
                new XElement(SitemapNs + "changefreq", "weekly"),
                new XElement(SitemapNs + "priority", "0.8")));
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString();
    }
}

/// <summary>Job Hangfire cho sitemap. Tên hàm phải public để Hangfire serialize được.</summary>
public sealed class SitemapGenerationJob(
    SitemapGenerator generator,
    IOptions<SitemapOptions> options,
    ILogger<SitemapGenerationJob> logger)
{
    public Task<SitemapGenerateResult> RunAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Bắt đầu job sitemap theo lịch {Cron}", options.Value.Cron);
        return generator.GenerateAsync(ct);
    }
}
