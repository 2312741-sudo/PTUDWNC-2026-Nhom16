using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog.Core;
using Serilog.Events;
using Xunit;
using static Lab.TV3.Tests.LabHttp;

namespace Lab.TV3.Tests;

/// <summary>Ngưỡng query chậm = 0 ms -> mọi câu SQL đều bị cảnh báo, để test thấy được log SLOW_SQL.</summary>
public sealed class SlowSqlFactory : LabFactory
{
    public CollectingSink Sink { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Perf:SlowQueryMs", "0");
        builder.ConfigureTestServices(s => s.AddSingleton<ILogEventSink>(Sink));
    }
}

/// <summary>
/// LAB K22 — chứng minh không N+1 bằng cách đếm số câu SQL mỗi request (header X-Sql-Count),
/// so với cách làm N+1 cố ý; cảnh báo query chậm (NFR-PERF-004); EXPLAIN ANALYZE câu danh sách.
/// </summary>
[Collection("lab")]
public sealed class L22PerformanceTests(LabFactory f)
{
    private static async Task<Guid> CreatePublished(HttpClient c, string title)
    {
        var res = await c.PostAsJsonAsync("/lab/l3/recipes", new { title, description = (string?)null, ingredients = (string?)null, category = (string?)null, status = "Published" });
        await Expect(HttpStatusCode.Created, res);
        return (await Data(res)).GetProperty("id").GetGuid();
    }

    private static async Task AddImages(Guid recipeId, int count)
    {
        await using var c = new NpgsqlConnection($"{LabFactory.Pg};Database=lab_tv3_test");
        await c.OpenAsync();
        for (var i = 0; i < count; i++)
        {
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO lab_images (id, recipe_id, object_key, content_type, size_bytes) VALUES (@id, @r, @k, 'image/jpeg', 1024)", c);
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("r", recipeId);
            cmd.Parameters.AddWithValue("k", $"k22/{recipeId:N}/{i}.jpg");
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task<(int SqlCount, JsonElement Items)> List(HttpClient c, string query)
    {
        var res = await c.GetAsync($"/lab/l22/recipes{query}");
        await Expect(HttpStatusCode.OK, res);
        Assert.True(res.Headers.TryGetValues("X-Sql-Count", out var v), "Thiếu header X-Sql-Count");
        return (int.Parse(v!.Single()), await Data(res));
    }

    [Fact]
    public async Task Danh_sach_kem_anh_luon_1_cau_SQL_du_lay_5_hay_20_cong_thuc()
    {
        var (c, _, _) = await AuthorAsync(f);
        for (var i = 0; i < 5; i++) await CreatePublished(c, $"Món K22 số {i}");

        var (n5, items5) = await List(c, "?take=5");
        var (n20, _) = await List(c, "?take=20");

        Assert.Equal(5, items5.GetArrayLength());
        Assert.Equal(1, n5);
        Assert.Equal(1, n20); // không tăng theo số dòng -> không N+1
    }

    [Fact]
    public async Task Anh_duoc_gom_dung_vao_tung_cong_thuc()
    {
        var (c, _, _) = await AuthorAsync(f);
        var a = await CreatePublished(c, "Món K22 có hai ảnh");
        await AddImages(a, 2);
        var b = await CreatePublished(c, "Món K22 không ảnh");

        var (_, items) = await List(c, "?take=2"); // mới nhất trước: b rồi a
        var byId = items.EnumerateArray().ToDictionary(i => i.GetProperty("id").GetGuid());

        Assert.Equal(2, byId[a].GetProperty("images").GetArrayLength());
        Assert.Equal(0, byId[b].GetProperty("images").GetArrayLength());
    }

    [Fact]
    public async Task Cach_lam_N_cong_1_co_y_ton_dung_1_cong_N_cau()
    {
        var (c, _, _) = await AuthorAsync(f);
        for (var i = 0; i < 5; i++) await CreatePublished(c, $"Món K22 naive {i}");

        var (n, items) = await List(c, "?take=5&mode=naive");

        Assert.Equal(5, items.GetArrayLength());
        Assert.Equal(1 + 5, n); // 1 câu danh sách + 1 câu ảnh cho MỖI công thức
    }

    [Fact]
    public async Task Explain_analyze_cau_danh_sach_tra_ve_ke_hoach_co_thoi_gian_thuc_thi()
    {
        var res = await f.CreateClient().GetAsync("/lab/l22/explain?take=20");
        await Expect(HttpStatusCode.OK, res);
        var plan = string.Join('\n', (await Data(res)).EnumerateArray().Select(l => l.GetString()));

        Assert.Contains("Execution Time:", plan);
        Assert.Contains("lab_images", plan);
        Assert.DoesNotContain("SubPlan", plan); // không có subquery chạy lặp theo từng dòng
    }

    [Fact]
    public async Task Query_vuot_nguong_bi_ghi_canh_bao_SLOW_SQL()
    {
        using var slow = new SlowSqlFactory();
        var res = await slow.CreateClient().GetAsync("/lab/l22/recipes?take=1");
        await Expect(HttpStatusCode.OK, res);

        var warn = slow.Sink.Events.FirstOrDefault(e => e.Level == LogEventLevel.Warning && e.MessageTemplate.Text.StartsWith("SLOW_SQL"));
        Assert.NotNull(warn);
        Assert.True(warn!.Properties.ContainsKey("ElapsedMs"));
        Assert.True(warn.Properties.ContainsKey("Sql"));
    }
}
