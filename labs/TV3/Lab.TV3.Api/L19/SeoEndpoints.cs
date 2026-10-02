using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Xml.Linq;
using Dapper;
using Lab.TV3.Api.L3;
using Microsoft.AspNetCore.OutputCaching;

namespace Lab.TV3.Api.L19;

public sealed record RenameInput(string Title);

/// <summary>
/// LAB K19 (L5) — sitemap.xml / robots.txt / trang chi tiết có canonical + Open Graph, 301 cho slug cũ và URL cũ.
/// Quy tắc slug (D14, NFR-SEO-004): đổi tiêu đề khi còn Draft thì sinh slug mới và giữ slug cũ để 301;
/// đã Published thì slug giữ nguyên (link ngoài/Google đã lưu không bị gãy).
/// </summary>
public static class SeoEndpoints
{
    private static readonly XNamespace Sm = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly HtmlEncoder Html = HtmlEncoder.Create(UnicodeRanges.All);

    public const string DetailPath = "/lab/l19/recipes/";

    private static string BaseUrl(IConfiguration cfg) => (cfg["Seo:BaseUrl"] ?? "http://localhost:5090").TrimEnd('/');

    public static void MapL19Seo(this WebApplication app)
    {
        app.MapGet("/robots.txt", (IConfiguration cfg) => Results.Text(
            $"""
            User-agent: *
            Disallow: /lab/
            Allow: {DetailPath}
            Allow: /lab/l16/search
            Sitemap: {BaseUrl(cfg)}/sitemap.xml
            """, "text/plain; charset=utf-8"));

        app.MapGet("/sitemap.xml", async (LabDb db, IConfiguration cfg, CancellationToken ct) =>
        {
            await using var c = await db.OpenAsync(ct);
            var rows = await c.QueryAsync<(string Slug, DateTime CreatedAt)>(
                "SELECT slug, created_at FROM lab_recipes WHERE status = 'Published' ORDER BY created_at DESC LIMIT 50000");
            var site = BaseUrl(cfg);
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", null),
                new XElement(Sm + "urlset", rows.Select(r => new XElement(Sm + "url",
                    new XElement(Sm + "loc", $"{site}{DetailPath}{r.Slug}"),
                    new XElement(Sm + "lastmod", r.CreatedAt.ToString("yyyy-MM-dd"))))));
            return Results.Text(doc.Declaration + "\n" + doc.ToString(SaveOptions.DisableFormatting), "application/xml; charset=utf-8");
        });

        // URL cũ dạng số ít /recipe/{slug} -> 301 về URL chuẩn
        app.MapGet("/recipe/{slug}", (string slug) => Results.Redirect(DetailPath + Uri.EscapeDataString(slug), permanent: true));

        app.MapGet(DetailPath + "{slug}", async (string slug, LabDb db, IConfiguration cfg, CancellationToken ct) =>
        {
            await using var c = await db.OpenAsync(ct);
            var recipe = await c.QuerySingleOrDefaultAsync<LabRecipe>(
                "SELECT id, slug, title, description, category, status, created_at FROM lab_recipes WHERE slug = @slug AND status = 'Published'",
                new { slug });
            if (recipe is not null)
                return Results.Content(RenderDetail(recipe, BaseUrl(cfg)), "text/html; charset=utf-8");

            // Slug cũ: tra bảng redirect theo recipe_id -> luôn ra slug HIỆN TẠI (không tạo chuỗi 301 -> 301)
            var current = await c.QuerySingleOrDefaultAsync<string>("""
                SELECT r.slug FROM lab_slug_redirects s JOIN lab_recipes r ON r.id = s.recipe_id
                WHERE s.old_slug = @slug AND r.status = 'Published'
                """, new { slug });
            return current is null
                ? Http.Err(404, "RECIPE_NOT_FOUND", "Không tìm thấy công thức")
                : Results.Redirect(DetailPath + current, permanent: true);
        });

        app.MapPut("/lab/l19/recipes/{id:guid}/title", async (Guid id, RenameInput input, LabDb db, RecipeCache cache,
            IOutputCacheStore output, CancellationToken ct) =>
        {
            var title = input.Title?.Trim() ?? "";
            if (title.Length is < 5 or > 200) return Http.Err(400, "VALIDATION", "Tiêu đề 5–200 ký tự");

            await using var c = await db.OpenAsync(ct);
            await using var tx = await c.BeginTransactionAsync(ct);
            var row = await c.QuerySingleOrDefaultAsync<(string Slug, string Status)>(
                "SELECT slug, status FROM lab_recipes WHERE id = @id FOR UPDATE", new { id }, tx);
            if (row.Slug is null) return Http.Err(404, "RECIPE_NOT_FOUND", "Không tìm thấy công thức");

            var slug = row.Slug;
            if (row.Status == "Draft")
            {
                slug = SearchEndpoints.Slugify(title);
                await c.ExecuteAsync("""
                    INSERT INTO lab_slug_redirects (old_slug, recipe_id) VALUES (@old, @id)
                    ON CONFLICT (old_slug) DO UPDATE SET recipe_id = EXCLUDED.recipe_id;
                    DELETE FROM lab_slug_redirects WHERE old_slug = @slug;
                    """, new { old = row.Slug, id, slug }, tx);
            }
            await c.ExecuteAsync("UPDATE lab_recipes SET title = @title, slug = @slug WHERE id = @id", new { title, slug, id }, tx);
            await tx.CommitAsync(ct);

            await cache.InvalidateAsync(row.Slug);
            await output.EvictByTagAsync(RecipeCache.OutputTag, ct);
            return Results.Ok(new { data = new { id, slug, title, status = row.Status } });
        }).RequireAuthorization();
    }

    public static string RenderDetail(LabRecipe r, string site)
    {
        var title = Html.Encode(r.Title);
        var desc = Html.Encode(r.Description ?? r.Title);
        var url = Html.Encode($"{site}{DetailPath}{r.Slug}");
        return new StringBuilder()
            .Append("<!doctype html><html lang=\"vi\"><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")
            .Append("<title>").Append(title).Append(" – Culinary Lab</title>")
            .Append("<meta name=\"description\" content=\"").Append(desc).Append("\">")
            .Append("<link rel=\"canonical\" href=\"").Append(url).Append("\">")
            .Append("<meta property=\"og:type\" content=\"article\">")
            .Append("<meta property=\"og:title\" content=\"").Append(title).Append("\">")
            .Append("<meta property=\"og:description\" content=\"").Append(desc).Append("\">")
            .Append("<meta property=\"og:url\" content=\"").Append(url).Append("\">")
            .Append("<meta name=\"twitter:card\" content=\"summary\">")
            .Append("</head><body><main><article><h1>").Append(title).Append("</h1>")
            .Append("<p>").Append(desc).Append("</p></article></main></body></html>")
            .ToString();
    }
}
