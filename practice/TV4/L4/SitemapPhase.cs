using System.Xml;
using System.Xml.Linq;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Practice.Lab4;

public sealed record SitemapEntry(string Location, DateTime LastModified);

/// <summary>
/// Phase XML (L4): sinh sitemap.xml chỉ từ recipe Published (đọc DB, không ghi) — cùng nguyên tắc D26 mà
/// sản phẩm dùng cho `/sitemap`, ở đây chạy tay để quan sát định dạng XML + validate parse lại.
/// </summary>
public static class SitemapPhase
{
    public const string BaseUrl = "https://culinary.local";

    public static async Task<PhaseResult> RunAsync()
    {
        var logger = LabLog.For("LAB/xml");
        var checks = new List<Check>();
        logger.LogInformation("=== PHASE XML (L4) === sitemap từ recipe Published (DB={Db})",
            LabConfig.AppConnection is null ? "fixture" : "culinary_test (chỉ đọc)");

        List<SitemapEntry> entries;
        var fromDb = false;
        if (LabConfig.AppConnection is { } connection)
        {
            try
            {
                await using var db = CreateDb(connection);
                entries = await db.Recipes.AsNoTracking()
                    .Where(r => r.Status == RecipeStatus.Published)
                    .OrderBy(r => r.Slug)
                    .Select(r => new SitemapEntry($"{BaseUrl}/recipes/{r.Slug}", r.PublishedAt ?? DateTime.MinValue))
                    .ToListAsync();
                fromDb = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Đọc DB thất bại ({Error}) — dùng dữ liệu fixture cho lab.", ex.Message);
                entries = [];
            }
        }
        else
        {
            entries = [];
        }

        if (!fromDb)
        {
            entries =
            [
                new($"{BaseUrl}/recipes/pho-bo", new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc)),
                new($"{BaseUrl}/recipes/banh-mi-thit-nuong", new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc)),
                new($"{BaseUrl}/recipes/che-dau-den", new DateTime(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc))
            ];
        }

        var path = Path.Combine(LabConfig.OutDir, "sitemap.xml");
        Directory.CreateDirectory(LabConfig.OutDir);
        var bytes = Build(entries);
        await File.WriteAllBytesAsync(path, bytes);

        checks.Add(new Check("sitemap có URL", entries.Count > 0,
            $"{entries.Count} url, nguồn={(fromDb ? "DB culinary_test" : "fixture")}"));
        checks.Add(new Check("sitemap parse lại được (XML hợp lệ)", TryParse(path, out var count) && count == entries.Count,
            $"parse ra {count} phần tử <url>"));

        if (fromDb)
        {
            try
            {
                await using var db = CreateDb(LabConfig.AppConnection!);
                var published = await db.Recipes.AsNoTracking().CountAsync(r => r.Status == RecipeStatus.Published);
                var all = await db.Recipes.AsNoTracking().CountAsync();
                checks.Add(new Check("chỉ lấy Published", entries.Count == published,
                    $"published={published} / tổng {all} (Draft/Archived/Deleted bị loại)"));
            }
            catch (Exception ex)
            {
                checks.Add(new Check("đối chiếu số URL với DB", false, ex.Message));
            }
        }

        logger.LogInformation("WROTE {Path} ({Bytes} bytes)", path, bytes.Length);
        return PhaseResult.From("XML", checks);
    }

    private static AuthDbContext CreateDb(string connection) =>
        new(new DbContextOptionsBuilder<AuthDbContext>().UseNpgsql(connection).Options);

    public static byte[] Build(IReadOnlyCollection<SitemapEntry> entries)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urlset = new XElement(ns + "urlset");
        foreach (var entry in entries)
        {
            var element = new XElement(ns + "url", new XElement(ns + "loc", entry.Location));
            if (entry.LastModified > DateTime.MinValue)
                element.Add(new XElement(ns + "lastmod", entry.LastModified.ToString("yyyy-MM-dd")));
            urlset.Add(element);
        }

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), urlset);
        using var buffer = new MemoryStream();
        using (var writer = XmlWriter.Create(buffer, new XmlWriterSettings { Indent = true, Encoding = System.Text.Encoding.UTF8 }))
        {
            doc.Save(writer);
        }

        return buffer.ToArray();
    }

    private static bool TryParse(string path, out int count)
    {
        try
        {
            var doc = XDocument.Load(path);
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            count = doc.Root?.Elements(ns + "url").Count() ?? 0;
            return doc.Root is not null;
        }
        catch (Exception)
        {
            count = 0;
            return false;
        }
    }
}
