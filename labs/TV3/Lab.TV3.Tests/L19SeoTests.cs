using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using static Lab.TV3.Tests.LabHttp;

namespace Lab.TV3.Tests;

/// <summary>
/// LAB K19 (L5) — sitemap.xml chỉ nội dung public, robots.txt có Sitemap, trang chi tiết có canonical,
/// slug không dấu/chữ thường/gạch nối (NFR-SEO-004), đổi tiêu đề khi Draft -> 301 từ slug cũ, sau Publish slug ổn định (D14).
/// </summary>
[Collection("lab")]
public sealed class L19SeoTests(LabFactory f)
{
    private static readonly XNamespace Sm = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private static string Marker() => "seo" + Guid.NewGuid().ToString("N")[..10];

    private HttpClient NoRedirect() => f.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<JsonElement> Create(HttpClient c, string title, string status = "Published")
    {
        var res = await c.PostAsJsonAsync("/lab/l3/recipes", new { title, description = "Mô tả món", ingredients = (string?)null, category = (string?)null, status });
        await Expect(HttpStatusCode.Created, res);
        return await Data(res);
    }

    private static async Task<string> Rename(HttpClient c, JsonElement recipe, string title)
    {
        var res = await c.PutAsJsonAsync($"/lab/l19/recipes/{recipe.GetProperty("id").GetGuid()}/title", new { title });
        await Expect(HttpStatusCode.OK, res);
        return (await Data(res)).GetProperty("slug").GetString()!;
    }

    private static async Task Publish(HttpClient c, JsonElement recipe, string title)
    {
        var res = await c.PutAsJsonAsync($"/lab/l3/recipes/{recipe.GetProperty("id").GetGuid()}",
            new { title, description = "Mô tả món", ingredients = (string?)null, category = (string?)null, status = "Published" });
        await Expect(HttpStatusCode.OK, res);
    }

    [Fact]
    public async Task Slug_khong_dau_chu_thuong_noi_gach_ngang()
    {
        var (c, _, _) = await AuthorAsync(f);
        var slug = (await Create(c, "Bánh Xèo Đà Nẵng — Giòn Rụm!!")).GetProperty("slug").GetString()!;

        Assert.StartsWith("banh-xeo-da-nang-gion-rum-", slug);
        Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", slug);
    }

    [Fact]
    public async Task Sitemap_chi_chua_cong_thuc_Published()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        var pub = (await Create(c, $"Cơm tấm công khai {m}")).GetProperty("slug").GetString();
        var draft = (await Create(c, $"Cơm tấm nháp {m}", "Draft")).GetProperty("slug").GetString();

        var res = await f.CreateClient().GetAsync("/sitemap.xml");
        await Expect(HttpStatusCode.OK, res);
        Assert.Equal("application/xml", res.Content.Headers.ContentType?.MediaType);
        var locs = XDocument.Parse(await res.Content.ReadAsStringAsync()).Descendants(Sm + "loc").Select(l => l.Value).ToList();

        Assert.Contains($"http://localhost:5090/lab/l19/recipes/{pub}", locs);
        Assert.DoesNotContain(locs, l => l.EndsWith("/" + draft));
    }

    [Fact]
    public async Task Robots_cho_phep_trang_cong_khai_va_tro_Sitemap()
    {
        var res = await f.CreateClient().GetAsync("/robots.txt");
        await Expect(HttpStatusCode.OK, res);
        Assert.Equal("text/plain", res.Content.Headers.ContentType?.MediaType);
        var body = await res.Content.ReadAsStringAsync();

        Assert.Contains("User-agent: *", body);
        Assert.Contains("Disallow: /lab/", body);
        Assert.Contains("Allow: /lab/l19/recipes/", body);
        Assert.Contains("Sitemap: http://localhost:5090/sitemap.xml", body);
    }

    [Fact]
    public async Task Trang_chi_tiet_co_canonical_va_Draft_tra_404()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        var pub = (await Create(c, $"Bún bò Huế {m}")).GetProperty("slug").GetString();
        var draft = (await Create(c, $"Bún bò nháp {m}", "Draft")).GetProperty("slug").GetString();
        var anon = NoRedirect();

        var ok = await anon.GetAsync($"/lab/l19/recipes/{pub}");
        await Expect(HttpStatusCode.OK, ok);
        var html = await ok.Content.ReadAsStringAsync();
        Assert.Contains($"<link rel=\"canonical\" href=\"http://localhost:5090/lab/l19/recipes/{pub}\">", html);
        Assert.Contains($"<meta property=\"og:title\" content=\"Bún bò Huế {m}\">", html);
        Assert.Contains($"<h1>Bún bò Huế {m}</h1>", html);

        await Expect(HttpStatusCode.NotFound, await anon.GetAsync($"/lab/l19/recipes/{draft}"));
        await Expect(HttpStatusCode.NotFound, await anon.GetAsync($"/lab/l19/recipes/khong-ton-tai-{m}"));
    }

    [Fact]
    public async Task Doi_tieu_de_khi_Draft_tao_slug_moi_va_slug_cu_tra_301()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        var recipe = await Create(c, $"Canh chua {m}", "Draft");
        var oldSlug = recipe.GetProperty("slug").GetString()!;

        var newSlug = await Rename(c, recipe, $"Canh chua cá lóc {m}");
        Assert.NotEqual(oldSlug, newSlug);
        Assert.StartsWith("canh-chua-ca-loc-", newSlug);
        await Publish(c, recipe, $"Canh chua cá lóc {m}");

        var res = await NoRedirect().GetAsync($"/lab/l19/recipes/{oldSlug}");
        Assert.Equal(HttpStatusCode.MovedPermanently, res.StatusCode);
        Assert.Equal($"/lab/l19/recipes/{newSlug}", res.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Doi_slug_hai_lan_slug_dau_tien_301_thang_toi_slug_hien_tai()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        var recipe = await Create(c, $"Gà nướng {m}", "Draft");
        var first = recipe.GetProperty("slug").GetString()!;
        await Rename(c, recipe, $"Gà nướng mật ong {m}");
        var current = await Rename(c, recipe, $"Gà nướng mật ong lá chanh {m}");
        await Publish(c, recipe, $"Gà nướng mật ong lá chanh {m}");

        var res = await NoRedirect().GetAsync($"/lab/l19/recipes/{first}");
        Assert.Equal(HttpStatusCode.MovedPermanently, res.StatusCode);
        Assert.Equal($"/lab/l19/recipes/{current}", res.Headers.Location?.OriginalString); // không chuỗi 301 -> 301
    }

    [Fact]
    public async Task Sau_khi_Published_doi_tieu_de_khong_doi_slug()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        var recipe = await Create(c, $"Chả giò {m}");
        var slug = recipe.GetProperty("slug").GetString()!;

        Assert.Equal(slug, await Rename(c, recipe, $"Chả giò rế {m}"));
        await Expect(HttpStatusCode.OK, await NoRedirect().GetAsync($"/lab/l19/recipes/{slug}"));
    }

    [Fact]
    public async Task Url_cu_dang_so_it_301_ve_url_chuan()
    {
        var (c, _, _) = await AuthorAsync(f);
        var slug = (await Create(c, $"Bánh mì {Marker()}")).GetProperty("slug").GetString();

        var res = await NoRedirect().GetAsync($"/recipe/{slug}");
        Assert.Equal(HttpStatusCode.MovedPermanently, res.StatusCode);
        Assert.Equal($"/lab/l19/recipes/{slug}", res.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Doi_tieu_de_can_dang_nhap()
    {
        var res = await f.CreateClient().PutAsJsonAsync($"/lab/l19/recipes/{Guid.NewGuid()}/title", new { title = "Không có token" });
        await Expect(HttpStatusCode.Unauthorized, res);
    }
}
