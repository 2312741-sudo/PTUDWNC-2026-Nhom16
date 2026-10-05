using System.Net;
using System.Text.Json;

namespace CulinaryBlog.Practice.Lab5;

/// <summary>
/// L5 phase 1 — <c>search-ssr</c>: chứng minh trang tìm kiếm được **render ở server**.
///
/// Cần phân biệt 3 thứ dễ nhầm:
///   • SSR/SSG: HTML trả về **đã có sẵn nội dung** — crawl được, không cần JS.
///   • CSR: HTML trả về rỗng, dữ liệu chỉ có sau khi JS chạy.
///   • ISR: render server nhưng **cache lại**, tái dùng cho các request sau.
///
/// Phase này kiểm tra HTML tĩnh trả về có nội dung thật, và kiểm tra route có bị
/// ép dynamic (nghĩa là mất ISR) hay không.
/// </summary>
public static class SearchSsrPhase
{
    public const string Phase = "search-ssr";

    public static async Task<PhaseResult> RunAsync()
    {
        var checks = new List<Check>();
        using var http = LabConfig.NewHttp();

        // Chọn từ khoá thật từ DB để trang tìm kiếm có kết quả — nếu không có kết quả
        // thì HTML rỗng và ta không phân biệt được SSR với CSR.
        var keyword = await PickPublishedKeywordAsync(http);

        var probes = new List<string>();
        var url = $"{LabConfig.WebBase}/search?q={Uri.EscapeDataString(keyword)}";

        var first = await HttpProbe.GetAsync(http, url);
        probes.Add($"GET {url} -> {(int)first.Status}, {first.Body.Length} byte");

        checks.Add(new Check("search trả 200", first.Status == HttpStatusCode.OK,
            $"HTTP {(int)first.Status}"));

        // --- Bằng chứng SSR: HTML tĩnh phải chứa nội dung, không phải vỏ rỗng ---
        var hasNextPayload = first.Body.Contains("self.__next_f", StringComparison.Ordinal);
        checks.Add(new Check("HTML chứa RSC payload của Next.js", hasNextPayload,
            hasNextPayload ? "có self.__next_f.push — App Router đã stream render" : "KHÔNG có payload RSC"));

        // `use client` / CSR thuần sẽ để lại dấu hiệu: HTML có id="__next" rỗng và không có
        // nội dung. Ta kiểm tra ngược lại: phải có text tiếng Việt thật trong HTML.
        var hasRealText = LooksLikeVietnameseContent(first.Body);
        checks.Add(new Check("HTML tĩnh đã có nội dung thật (không phải CSR shell)", hasRealText,
            hasRealText
                ? "HTML chứa text render sẵn — server-rendered"
                : "HTML không có nội dung → có thể là CSR, cần kiểm lại"));

        // Nếu đây là ISR, route KHÔNG được ép `no-store`. `no-store`/`private` trong
        // Cache-Control là dấu hiệu response không được cache lại.
        var cacheControl = first.Header("Cache-Control");
        var noStore = cacheControl?.Contains("no-store", StringComparison.OrdinalIgnoreCase) == true
                      || cacheControl?.Contains("private", StringComparison.OrdinalIgnoreCase) == true;
        checks.Add(new Check("response KHÔNG bị ép no-store (còn cơ hội cache)", !noStore,
            $"Cache-Control: {cacheControl ?? "(không có)"}"));

        // X-powered-by Next.js: xác nhận đúng server, không phải proxy/VPS khác.
        checks.Add(new Check("do Next.js phục vụ", first.Header("X-Powered-By")?.Contains("Next.js") == true,
            $"X-Powered-By: {first.Header("X-Powered-By") ?? "(không có)"}"));

        // --- Đo thời gian render để so sánh SSR vs độ trễ client ---
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await HttpProbe.GetAsync(http, url);
        sw.Stop();
        checks.Add(new Check("render server hoàn tất dưới 5s", sw.Elapsed.TotalSeconds < 5,
            $"{sw.ElapsedMilliseconds} ms"));

        var detail = probes.Concat([
            $"từ khoá: \"{keyword}\"",
            $"độ dài HTML: {first.Body.Length} byte",
            $"Cache-Control: {cacheControl ?? "(không có)"}",
            $"x-nextjs-cache: {first.Header("x-nextjs-cache") ?? "(không có)"}",
            $"x-nextjs-revalidate: {first.Header("x-nextjs-revalidate") ?? "(không có)"}"
        ]).ToList();

        return PhaseResult.From(Phase, checks, detail);
    }

    private static bool LooksLikeVietnameseContent(string html)
    {
        // Cụm từ chỉ xuất hiện khi server đã render nội dung thật.
        string[] markers =
        [
            "Kết quả", "Tìm kiếm", "Công thức", "Phở", "Món", "Không tìm thấy",
            "Khoảng", "phút", "Nguyên liệu"
        ];
        return markers.Any(m => html.Contains(m, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Lấy từ khoá từ tiêu đề công thức Published thật để tìm kiếm có kết quả.</summary>
    private static async Task<string> PickPublishedKeywordAsync(HttpClient http)
    {
var list = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/recipes?pageSize=1");
        var first = list.DataItems.FirstOrDefault();
        if (first.ValueKind == JsonValueKind.Object)
        {
            var title = first.TryGetProperty("title", out var t) ? t.GetString() : null;
            if (!string.IsNullOrWhiteSpace(title))
            {
                // Cắt 1 từ có dấu để truy vấn khớp AND ở mức đơn giản nhất.
                var word = title.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(w => w.Length > 2);
                if (!string.IsNullOrWhiteSpace(word)) return word;
            }
        }

        return "pho";
    }
}