using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CulinaryBlog.Practice.Lab5;

/// <summary>
/// L5 phase 5 — <c>seo</c>: kiểm chứng tín hiệu SEO mà **crawler đọc được**.
///
/// Với Next.js App Router, metadata có thể khai báo tĩnh bằng `export const metadata`,
/// sinh động bằng `generateMetadata`, hoặc thuộc chỉnh qua `sitemap.ts`/`robots.ts`.
/// Có một điểm dễ sai thường gặp: khai báo metadata đúng trong code nhưng **không xuất
/// hiện trong HTML thực tế** (thường do metadata bị đặt sai vị trí so với segment),
/// hoặc JSON-LD không khớp với nội dung đang hiển thị.
///
/// Phase này chỉ tính là đạt khi thấy thẻ trong HTML **tĩnh trả về**, không tính vào
/// việc đọc file nguồn.
/// </summary>
public static class SeoPhase
{
    public const string Phase = "seo";

    public static async Task<PhaseResult> RunAsync()
    {
        var checks = new List<Check>();
        var detail = new List<string>();
        using var http = LabConfig.NewHttp();

        // --- 1. Trang chi tiết công thức ---
        var slug = await PickPublishedSlugAsync(http);
        if (slug is null)
        {
            checks.Add(new Check("lay duoc cong thuc Published de kiem chung SEO", false,
                "API khong tra ve cong thuc Published"));
            return PhaseResult.From(Phase, checks, detail);
        }

        var url = $"{LabConfig.WebBase}/recipes/{slug}";
        var page = await HttpProbe.GetAsync(http, url);
        detail.Add($"trang: /recipes/{slug} -> HTTP {(int)page.Status}, {page.Body.Length} byte");

        checks.Add(new Check("trang cong thuc tra 200", page.Status == HttpStatusCode.OK,
            $"HTTP {(int)page.Status}"));

        // <title> không phải thẻ <meta> nên phải so riêng.
        var title = FindTitle(page.Body);
        detail.Add($"<title>: {title ?? "(khong co)"}");
        checks.Add(new Check("co the <title> trong HTML tinh", !string.IsNullOrWhiteSpace(title),
            title is null ? "thieu <title>" : $"\"{Trim(title)}\""));

        var description = FindMetaContent(page.Body, "description");
        detail.Add($"meta description: {(description is null ? "(khong co)" : $"\"{Trim(description)}\"")}");
        checks.Add(new Check("co meta description", !string.IsNullOrWhiteSpace(description),
            description is null ? "thieu meta description" : $"do dai {description.Length} ky tu"));

        var ogTitle = FindMetaProperty(page.Body, "og:title");
        var ogImage = FindMetaProperty(page.Body, "og:image");
        detail.Add($"og:title: {(ogTitle is null ? "(khong co)" : "\"" + Trim(ogTitle) + "\"")}");
        detail.Add($"og:image: {(ogImage is null ? "(khong co)" : "\"" + Trim(ogImage) + "\"")}");

        checks.Add(new Check("co the OG:title cho chia se mxh", !string.IsNullOrWhiteSpace(ogTitle),
            ogTitle is null ? "thieu og:title" : $"\"{Trim(ogTitle)}\""));
        checks.Add(new Check("co OG:image de hien thi anh khi chia se", !string.IsNullOrWhiteSpace(ogImage),
            ogImage is null ? "thieu og:image" : $"\"{Trim(ogImage)}\""));

        var canonical = FindLinkRel(page.Body, "canonical");
        detail.Add($"canonical: {canonical ?? "(khong co)"}");
        checks.Add(new Check("co canonical de chong trung lap noi dung", canonical is not null,
            canonical is null ? "thieu canonical" : canonical));

        // --- 2. JSON-LD: phai parse duoc va khop noi dung ---
        var jsonLdBlocks = Regex.Matches(page.Body,
            "<script[^>]*type=[\"']application/ld\\+json[\"'][^>]*>(.*?)</script>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        detail.Add($"so khoi JSON-LD: {jsonLdBlocks.Count}");

        var parseable = 0;
        var hasRecipeLd = false;
        foreach (Match match in jsonLdBlocks)
        {
            var raw = match.Groups[1].Value.Trim();
            try
            {
                using var doc = JsonDocument.Parse(raw);
                parseable++;
                var text = raw;
                if (text.Contains("\"Recipe\"", StringComparison.Ordinal)) hasRecipeLd = true;
            }
            catch
            {
                detail.Add("  JSON-LD khong parse duoc");
            }
        }

        checks.Add(new Check("mo khoi JSON-LD phai parse duoc (khong nen sai JSON)",
            jsonLdBlocks.Count > 0 && parseable == jsonLdBlocks.Count,
            $"{parseable}/{jsonLdBlocks.Count} khoi hop le"));

        checks.Add(new Check("co JSON-LD kieu Recipe cho Google rich result",
            hasRecipeLd,
            hasRecipeLd ? "tim thay schema.org Recipe" : "khong co schema Recipe"));

        // --- 3. robots.txt va sitemap.xml ---
        var robots = await HttpProbe.GetAsync(http, $"{LabConfig.WebBase}/robots.txt");
        detail.Add($"GET /robots.txt -> HTTP {(int)robots.Status}, len={robots.Body.Length}");
        checks.Add(new Check("robots.txt phuc vu duoc", robots.Status == HttpStatusCode.OK,
            $"HTTP {(int)robots.Status}"));

        checks.Add(new Check("robots.txt cho phep crawler doc",
            robots.Body.Contains("Allow: /", StringComparison.OrdinalIgnoreCase) ||
            !robots.Body.Contains("Disallow: /", StringComparison.Ordinal),
            $"noi dung: {Trim(robots.Body).Replace("\n", " | ")}"));

        var sitemap = await HttpProbe.GetAsync(http, $"{LabConfig.WebBase}/sitemap.xml");
        detail.Add($"GET /sitemap.xml -> HTTP {(int)sitemap.Status}, len={sitemap.Body.Length}");
        checks.Add(new Check("sitemap.xml phuc vu duoc", sitemap.Status == HttpStatusCode.OK,
            $"HTTP {(int)sitemap.Status}"));

        checks.Add(new Check("sitemap.xml co du lieu <loc> khong rong",
            sitemap.Body.Contains("<loc>", StringComparison.Ordinal) && sitemap.Body.Length > 100,
            $"do dai {sitemap.Body.Length} byte"));

        var locCount = Regex.Matches(sitemap.Body, "<loc>", RegexOptions.IgnoreCase).Count;
        detail.Add($"so <loc> trong sitemap: {locCount}");
        checks.Add(new Check("sitemap.xml co it nhat mot URL", locCount >= 1, $"{locCount} URL"));

        // --- 4. Tin dung voi trang dang xem ---
        var home = await HttpProbe.GetAsync(http, LabConfig.WebBase);
        var homeTitle = FindTitle(home.Body);
        detail.Add($"<title> trang chu: {homeTitle ?? "(khong co)"}");
        checks.Add(new Check("moi trang co title rieng (khong trung nhau)",
            !string.IsNullOrWhiteSpace(homeTitle) && homeTitle != title,
            homeTitle == title ? "trang chu va trang cong thuc cung title" : $"\"{Trim(homeTitle ?? "(khong co)")}\""));

        // --- 5. Khai bao xung dot trong code: nhac nho cho reviewer ---
        if (LabConfig.SourceRoot is { } src)
        {
            var conflicts = FileScanner.Search(src, "generateMetadata", "export const metadata");
            detail.Add($"khai bao metadata tim thay: {conflicts.Count} cho");
            checks.Add(new Check("co khai bao metadata trong ma nguon", conflicts.Count > 0,
                $"{conflicts.Count} cho khai bao"));
        }

        return PhaseResult.From(Phase, checks, detail);
    }

    private static string? FindTitle(string html)
    {
        var match = Regex.Match(html, "<title[^>]*>(.*?)</title>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value.Trim()) : null;
    }

    /// <summary>
    /// Tìm thẻ meta theo cặp (attribute, key). Thứ tự attribute do Next.js sinh ra không
    /// cố định, nên phải thử cả hai chiều thay vì chỉ một mẫu.
    /// </summary>
    private static string? FindMetaContent(string html, string key, string attribute = "name")
    {
        string[] patterns =
        [
            "<meta[^>]*content=[\"']([^\"']*)[\"'][^>]*" + Regex.Escape(attribute) + "=[\"']" + Regex.Escape(key) + "[\"']",
            "<meta[^>]*" + Regex.Escape(attribute) + "=[\"']" + Regex.Escape(key) + "[\"'][^>]*content=[\"']([^\"']*)[\"']"
        ];

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
                return System.Net.WebUtility.HtmlDecode(match.Groups[1].Value);
        }
        return null;
    }

    private static string? FindMetaProperty(string html, string property) =>
        FindMetaContent(html, property, "property");

    private static string? FindLinkRel(string html, string rel)
    {
        var match = Regex.Match(html, "<link[^>]*rel=[\"']" + rel + "[\"'][^>]*href=[\"']([^\"']+)[\"']",
            RegexOptions.IgnoreCase);
        if (match.Success) return System.Net.WebUtility.HtmlDecode(match.Groups[1].Value);

        var reversed = Regex.Match(html,
            "<link[^>]*href=[\"']([^\"']+)[\"'][^>]*rel=[\"']" + rel + "[\"']", RegexOptions.IgnoreCase);
        return reversed.Success ? System.Net.WebUtility.HtmlDecode(reversed.Groups[1].Value) : null;
    }

    private static async Task<string?> PickPublishedSlugAsync(HttpClient http)
    {
        var probe = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/recipes?pageSize=20");
        foreach (var item in probe.DataItems)
        {
            var slug = item.TryGetProperty("slug", out var s) ? s.GetString() : null;
            if (!string.IsNullOrWhiteSpace(slug)) return slug;
        }
        return null;
    }

    private static string Trim(string value) => value.Length <= 80 ? value : value[..80] + "...";
}