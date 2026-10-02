using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Xunit;
using static Lab.TV3.Tests.LabHttp;

namespace Lab.TV3.Tests;

/// <summary>
/// LAB K16 (L5 SSR search) — trang tìm kiếm render HTML phía server: kết quả nằm sẵn trong HTML trả về,
/// không cần JavaScript; chỉ Published; chống XSS; phân trang bằng link thường.
/// </summary>
[Collection("lab")]
public sealed class L16SsrSearchTests(LabFactory f)
{
    private static string Marker() => "ssr" + Guid.NewGuid().ToString("N")[..10];

    private static async Task Create(HttpClient c, string title, string status = "Published")
    {
        var res = await c.PostAsJsonAsync("/lab/l3/recipes", new { title, description = (string?)null, ingredients = (string?)null, category = (string?)null, status });
        await Expect(HttpStatusCode.Created, res);
    }

    private static async Task<string> Page(HttpClient c, string query)
    {
        var res = await c.GetAsync($"/lab/l16/search{query}");
        await Expect(HttpStatusCode.OK, res);
        Assert.Equal("text/html", res.Content.Headers.ContentType?.MediaType);
        return await res.Content.ReadAsStringAsync();
    }

    private static int ResultCount(string html) => Regex.Matches(html, "<li class=\"hit\"").Count;

    [Fact]
    public async Task Html_tra_ve_da_chua_ket_qua_khong_can_JavaScript()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        await Create(c, $"Phở bò Hà Nội {m}");

        var html = await Page(c, $"?q={Uri.EscapeDataString("pho bo " + m)}");

        Assert.Contains($"Phở bò Hà Nội {m}", html);           // tiếng Việt giữ nguyên, không bị encode thành &#x...
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, ResultCount(html));
        Assert.Matches("href=\"/lab/l19/recipes/pho-bo-ha-noi-" + m + "-[0-9a-f]{6}\"", html);
    }

    [Fact]
    public async Task Ban_nhap_khong_xuat_hien_trong_trang()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        await Create(c, $"Bản nháp riêng tư {m}", status: "Draft");

        var html = await Page(c, $"?q={m}");

        Assert.DoesNotContain(m + "</a>", html);
        Assert.Equal(0, ResultCount(html));
        Assert.Contains("Không tìm thấy", html);
    }

    [Fact]
    public async Task Tieu_de_va_tu_khoa_duoc_HTML_encode_chong_XSS()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        await Create(c, $"<script>alert(1)</script> Gỏi {m}");

        var html = await Page(c, $"?q={Uri.EscapeDataString("goi " + m)}");
        Assert.DoesNotContain("<script>alert(1)", html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt; Gỏi " + m, html);

        // Từ khoá được in lại vào ô input/tiêu đề trang -> cũng phải encode
        var echo = await Page(c, $"?q={Uri.EscapeDataString("\"><img src=x onerror=alert(2)>")}");
        Assert.DoesNotContain("<img src=x", echo);
        Assert.Contains("&quot;&gt;&lt;img src=x onerror=alert(2)&gt;", echo);
    }

    [Fact]
    public async Task Khong_co_tu_khoa_chi_hien_form_va_trang_tim_kiem_noindex()
    {
        var html = await Page(f.CreateClient(), "");

        Assert.Contains("<form method=\"get\" action=\"/lab/l16/search\"", html);
        Assert.Contains("name=\"q\"", html);
        Assert.Contains("<meta name=\"robots\" content=\"noindex,follow\">", html);
        Assert.Equal(0, ResultCount(html));
    }

    [Fact]
    public async Task Phan_trang_bang_link_thuong()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        for (var i = 1; i <= 3; i++) await Create(c, $"Bún chả số {i} {m}");

        var p1 = await Page(c, $"?q={m}&pageSize=2");
        var p2 = await Page(c, $"?q={m}&pageSize=2&page=2");

        Assert.Equal(2, ResultCount(p1));
        Assert.Equal(1, ResultCount(p2));
        Assert.Contains($"href=\"/lab/l16/search?q={m}&amp;page=2&amp;pageSize=2\"", p1);
        Assert.Contains("3 kết quả", p1);
    }
}
