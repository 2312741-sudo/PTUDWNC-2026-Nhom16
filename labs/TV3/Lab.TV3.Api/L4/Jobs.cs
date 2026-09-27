using System.Xml.Linq;
using Dapper;
using Hangfire;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Lab.TV3.Api.L4;

public sealed class LabImage
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public string ObjectKey { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string? MediumKey { get; set; }
    public string? ThumbKey { get; set; }
}

/// <summary>Gửi email qua SMTP (Mailhog localhost:1025). Lỗi -> Hangfire retry (K15).</summary>
public sealed class EmailJob(IConfiguration cfg, ILogger<EmailJob> log)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task SendAsync(string to, string subject, string html, CancellationToken ct)
    {
        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse(cfg["Smtp:From"] ?? "lab-tv3@culinary.local"));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new TextPart("html") { Text = html };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(cfg["Smtp:Host"] ?? "localhost", int.Parse(cfg["Smtp:Port"] ?? "1025"), SecureSocketOptions.None, ct);
        await smtp.SendAsync(msg, ct);
        await smtp.DisconnectAsync(true, ct);
        log.LogInformation("Đã gửi email tới {To}", to);
    }
}

/// <summary>Resize 300×300 (crop) + 800×600 (fit) sau upload (K14/K15). Ảnh bị xoá giữa chừng -> dọn biến thể, không tái sinh file.</summary>
public sealed class ResizeJob(ObjectStorage storage, LabDb db, ILogger<ResizeJob> log)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task RunAsync(Guid imageId, CancellationToken ct)
    {
        await using var c = await db.OpenAsync(ct);
        var img = await c.QuerySingleOrDefaultAsync<LabImage>("SELECT * FROM lab_images WHERE id = @imageId", new { imageId });
        if (img is null) { log.LogInformation("Ảnh {Id} đã bị xoá — bỏ qua resize", imageId); return; }
        if (img.ContentType == "image/avif") { log.LogInformation("AVIF: ImageSharp chưa decode được — giữ bản gốc"); return; }

        await using var src = await storage.GetAsync(img.ObjectKey, ct);
        using var image = await Image.LoadAsync(src, ct);
        var baseKey = img.ObjectKey[..img.ObjectKey.LastIndexOf('.')];
        var thumb = await SaveVariantAsync(image, 300, 300, ResizeMode.Crop, $"{baseKey}_300x300.jpg", ct);
        var medium = await SaveVariantAsync(image, 800, 600, ResizeMode.Max, $"{baseKey}_800x600.jpg", ct);

        var n = await c.ExecuteAsync("UPDATE lab_images SET thumb_key = @thumb, medium_key = @medium WHERE id = @imageId",
            new { thumb, medium, imageId });
        if (n == 0) // bị xoá trong lúc resize (race xoá-vs-resize)
        {
            await storage.DeleteAsync(thumb, ct);
            await storage.DeleteAsync(medium, ct);
        }
    }

    private async Task<string> SaveVariantAsync(Image image, int w, int h, ResizeMode mode, string key, CancellationToken ct)
    {
        using var clone = image.Clone(x => x.Resize(new ResizeOptions { Size = new Size(w, h), Mode = mode }));
        await using var ms = new MemoryStream();
        await clone.SaveAsJpegAsync(ms, ct);
        ms.Position = 0;
        await storage.PutAsync(key, ms, "image/jpeg", ct);
        return key;
    }
}

/// <summary>Recurring: sitemap XML chỉ gồm công thức Published (K14/K15, K19).</summary>
public sealed class SitemapJob(LabDb db, IConfiguration cfg)
{
    public const string RecurringId = "lab-tv3-sitemap";
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    public async Task<int> RunAsync(CancellationToken ct)
    {
        await using var c = await db.OpenAsync(ct);
        var rows = (await c.QueryAsync<(string Slug, DateTime CreatedAt)>(
            "SELECT slug, created_at FROM lab_recipes WHERE status = 'Published' ORDER BY created_at DESC LIMIT 50000")).ToList();
        var site = (cfg["Site:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null),
            new XElement(Ns + "urlset", rows.Select(r => new XElement(Ns + "url",
                new XElement(Ns + "loc", $"{site}/recipes/{r.Slug}"),
                new XElement(Ns + "lastmod", r.CreatedAt.ToString("yyyy-MM-dd"))))));
        await c.ExecuteAsync("INSERT INTO lab_sitemaps (url_count, xml) VALUES (@n, @xml)",
            new { n = rows.Count, xml = doc.Declaration + Environment.NewLine + doc });
        return rows.Count;
    }
}