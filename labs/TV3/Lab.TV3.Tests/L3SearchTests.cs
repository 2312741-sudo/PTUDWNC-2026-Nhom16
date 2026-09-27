using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using static Lab.TV3.Tests.LabHttp;

namespace Lab.TV3.Tests;

/// <summary>LAB L3 — K11 FTS (unaccent, AND, rank, paging, không lộ Draft), K12 cache/invalidation/fallback.</summary>
[Collection("lab")]
public sealed class L3SearchTests(LabFactory f)
{
    private static string Marker() => "zq" + Guid.NewGuid().ToString("N")[..10];

    private static async Task<JsonElement> Create(HttpClient c, string title, string? description = null,
        string? ingredients = null, string status = "Published", string? category = null)
    {
        var res = await c.PostAsJsonAsync("/lab/l3/recipes", new { title, description, ingredients, category, status });
        await Expect(HttpStatusCode.Created, res);
        return await Data(res);
    }

    private static async Task<(JsonElement Page, string? Cache)> Search(HttpClient c, string q, int page = 1, int size = 10)
    {
        var res = await c.GetAsync($"/lab/l3/search?q={Uri.EscapeDataString(q)}&page={page}&pageSize={size}");
        await Expect(HttpStatusCode.OK, res);
        return (await Data(res), res.Headers.TryGetValues("X-Cache", out var v) ? v.First() : null);
    }

    private static List<string> Titles(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString()!).ToList();

    [Fact]
    public async Task Unaccented_query_finds_vietnamese_title()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        await Create(c, $"Phở bò Hà Nội {m}");
        var (page, _) = await Search(c, $"pho bo {m}");
        Assert.Contains($"Phở bò Hà Nội {m}", Titles(page));
    }

    [Fact]
    public async Task Multiple_words_use_AND_semantics()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        await Create(c, $"Phở gà {m}");
        var (page, _) = await Search(c, $"pho bo {m}");
        Assert.Empty(Titles(page));
    }

    [Fact]
    public async Task Draft_is_never_returned()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        await Create(c, $"Bản nháp bí mật {m}", status: "Draft");
        var (page, _) = await Search(c, m);
        Assert.Equal(0, page.GetProperty("totalCount").GetInt64());
    }

    [Fact]
    public async Task Title_match_ranks_above_description_match()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        await Create(c, "Món chỉ nhắc trong mô tả", description: $"Có từ khoá {m} ở mô tả");
        await Create(c, $"Món có từ khoá {m} ở tiêu đề");
        var (page, _) = await Search(c, m);
        Assert.Equal($"Món có từ khoá {m} ở tiêu đề", Titles(page)[0]);
    }

    [Fact]
    public async Task Paging_returns_total_count_and_correct_slices()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        for (var i = 1; i <= 3; i++) await Create(c, $"Bún chả số {i} {m}");
        var (p1, _) = await Search(c, m, page: 1, size: 2);
        var (p2, _) = await Search(c, m, page: 2, size: 2);
        Assert.Equal(3, p1.GetProperty("totalCount").GetInt64());
        Assert.Equal(2, Titles(p1).Count);
        Assert.Single(Titles(p2));
        Assert.Empty(Titles(p1).Intersect(Titles(p2)));
    }

    [Fact]
    public async Task Explain_shows_GIN_index_is_used()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        await Create(c, $"Cơm tấm {m}");
        var res = await c.GetAsync($"/lab/l3/explain?q={m}&forceIndex=true");
        await Expect(HttpStatusCode.OK, res);
        var plan = string.Join('\n', (await Data(res)).EnumerateArray().Select(l => l.GetString()));
        Assert.Contains("ix_lab_recipes_search", plan);
    }

    [Fact]
    public async Task Create_and_update_require_authentication()
    {
        var anon = f.CreateClient();
        await Expect(HttpStatusCode.Unauthorized, await anon.PostAsJsonAsync("/lab/l3/recipes",
            new { title = "Không có token", status = "Published" }));
    }

    // ---------------------------------------------------------------- cần Redis thật (docker)

    [Fact]
    [Trait("Infra", "docker")]
    public async Task Search_is_cached_then_invalidated_after_update()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = Marker();
        var recipe = await Create(c, $"Gỏi cuốn {m}");
        Assert.Equal("MISS", (await Search(c, m)).Cache);
        Assert.Equal("HIT", (await Search(c, m)).Cache);

        var upd = await c.PutAsJsonAsync($"/lab/l3/recipes/{recipe.GetProperty("id").GetGuid()}",
            new { title = $"Gỏi cuốn tôm thịt {m}", status = "Published" });
        await Expect(HttpStatusCode.OK, upd);

        var (after, cache) = await Search(c, m);
        Assert.Equal("MISS", cache); // version tăng -> key cũ hết hiệu lực
        Assert.Contains($"Gỏi cuốn tôm thịt {m}", Titles(after));
    }
}

/// <summary>Redis không chạy -> hệ thống vẫn trả kết quả từ PostgreSQL (X-Cache: BYPASS), không 500.</summary>
[Collection("lab")]
public sealed class L3RedisFallbackTests(RedisDownFactory down) : IClassFixture<RedisDownFactory>
{
    [Fact]
    public async Task Redis_down_falls_back_to_database()
    {
        var (c, _, _) = await AuthorAsync(down);
        var m = "zq" + Guid.NewGuid().ToString("N")[..10];
        var create = await c.PostAsJsonAsync("/lab/l3/recipes", new { title = $"Canh chua {m}", status = "Published" });
        await Expect(HttpStatusCode.Created, create); // invalidation lỗi Redis cũng không chặn ghi

        var res = await c.GetAsync($"/lab/l3/search?q={m}");
        await Expect(HttpStatusCode.OK, res);
        Assert.Equal("BYPASS", res.Headers.GetValues("X-Cache").First());
        Assert.Equal(1, (await Data(res)).GetProperty("totalCount").GetInt64());
    }
}
