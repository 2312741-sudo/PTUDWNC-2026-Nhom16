using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;
using Xunit;
using static Lab.TV3.Tests.LabHttp;

namespace Lab.TV3.Tests;

/// <summary>Gom log Serilog trong bộ nhớ để test đọc lại (được đăng ký như một sink qua DI).</summary>
public sealed class CollectingSink : ILogEventSink
{
    public ConcurrentQueue<LogEvent> Events { get; } = new();
    public void Emit(LogEvent logEvent) => Events.Enqueue(logEvent);

    public static string? Prop(LogEvent e, string name) =>
        e.Properties.TryGetValue(name, out var v) && v is ScalarValue { Value: var s } ? s?.ToString() : null;
}

/// <summary>App lab với Redis luôn chết (cổng 6398), sink log trong bộ nhớ và sink File ghi ra file tạm.</summary>
public sealed class ObservabilityFactory : LabFactory
{
    public CollectingSink Sink { get; } = new();
    public string LogFile { get; } = Path.Combine(Path.GetTempPath(), $"lab-tv3-k20-{Guid.NewGuid():N}.log");

    protected override string Redis => "localhost:6398";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        // Thêm sink File bằng cấu hình (giống cách bật Seq/File ở môi trường thật) -> chứng minh sink "cấu hình được"
        builder.UseSetting("Serilog:WriteTo:9:Name", "File");
        builder.UseSetting("Serilog:WriteTo:9:Args:path", LogFile);
        builder.UseSetting("Serilog:WriteTo:9:Args:outputTemplate", "{Timestamp:o} [{Level:u3}] cid={CorrelationId} {Message:lj}{NewLine}");
        builder.ConfigureTestServices(s => s.AddSingleton<ILogEventSink>(Sink));
    }
}

/// <summary>
/// LAB K20 (L5) — Serilog sink cấu hình được, correlation id trong log, trace W3C (OpenTelemetry), custom metric,
/// health live/ready (ready kiểm DB + Redis), không log mật khẩu/token.
/// </summary>
public sealed class L20ObservabilityTests(ObservabilityFactory f) : IClassFixture<ObservabilityFactory>
{
    private List<LogEvent> RequestLogs(string path) =>
        f.Sink.Events.Where(e => CollectingSink.Prop(e, "RequestPath") == path).ToList();

    private static async Task<JsonElement> Json(HttpResponseMessage res)
    {
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task Live_tra_200_khong_phu_thuoc_DB_Redis()
    {
        var res = await f.CreateClient().GetAsync("/health/live");
        await Expect(HttpStatusCode.OK, res);
        Assert.Equal("Healthy", (await Json(res)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Ready_tra_503_khi_Redis_chet_va_bao_tung_thanh_phan()
    {
        var res = await f.CreateClient().GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, res.StatusCode);
        var body = await Json(res);
        Assert.Equal("Unhealthy", body.GetProperty("status").GetString());
        Assert.Equal("Healthy", body.GetProperty("entries").GetProperty("db").GetProperty("status").GetString());
        Assert.Equal("Unhealthy", body.GetProperty("entries").GetProperty("redis").GetProperty("status").GetString());
    }

    [Fact]
    public async Task Correlation_id_tu_client_duoc_tra_lai_va_gan_vao_log()
    {
        var client = f.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "tv3-k20-abc");
        var res = await client.GetAsync("/lab/health");
        await Expect(HttpStatusCode.OK, res);

        Assert.Equal("tv3-k20-abc", res.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Contains(RequestLogs("/lab/health"), e => CollectingSink.Prop(e, "CorrelationId") == "tv3-k20-abc");
    }

    [Fact]
    public async Task Khong_gui_hoac_gui_id_xau_thi_server_tu_sinh_id_moi()
    {
        var res1 = await f.CreateClient().GetAsync("/health/live");
        var generated = res1.Headers.GetValues("X-Correlation-ID").Single();
        Assert.Matches("^[0-9a-f]{32}$", generated);

        var client = f.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-ID", "bad id\twith\"chars" + new string('x', 100));
        var res2 = await client.GetAsync("/health/live");
        Assert.Matches("^[0-9a-f]{32}$", res2.Headers.GetValues("X-Correlation-ID").Single()); // chống log injection
    }

    [Fact]
    public async Task Traceparent_W3C_duoc_noi_tiep_vao_trace_va_log()
    {
        var traceId = Guid.NewGuid().ToString("N");
        var client = f.CreateClient();
        client.DefaultRequestHeaders.Add("traceparent", $"00-{traceId}-{Guid.NewGuid().ToString("N")[..16]}-01");
        var res = await client.GetAsync("/lab/l16/search");
        await Expect(HttpStatusCode.OK, res);

        Assert.Equal(traceId, res.Headers.GetValues("X-Trace-Id").Single());
        Assert.Contains(RequestLogs("/lab/l16/search"), e => e.TraceId?.ToHexString() == traceId);
    }

    [Fact]
    public async Task Sink_File_bat_bang_cau_hinh_ghi_log_co_correlation_id()
    {
        var client = f.CreateClient();
        var cid = "tv3-file-" + Guid.NewGuid().ToString("N")[..8];
        client.DefaultRequestHeaders.Add("X-Correlation-ID", cid);
        await Expect(HttpStatusCode.OK, await client.GetAsync("/lab/health"));

        Assert.True(File.Exists(f.LogFile), "Chưa có file log: " + f.LogFile);
        using var stream = new FileStream(f.LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var text = await new StreamReader(stream).ReadToEndAsync();
        Assert.Contains($"cid={cid}", text);
    }

    [Fact]
    public async Task Log_request_khong_chua_mat_khau_va_query_string()
    {
        var client = f.CreateClient();
        var email = NewEmail("k20");
        await Expect(HttpStatusCode.Created, await client.PostAsJsonAsync("/lab/l1/register", new { email, password = Password, displayName = "K20" }));
        await client.GetAsync("/lab/l16/search?q=pho&access_token=SECRET-TOKEN-K20");

        Assert.NotEmpty(RequestLogs("/lab/l1/register"));
        var all = string.Join('\n', f.Sink.Events.Select(e => e.RenderMessage() + " " + string.Join(' ', e.Properties.Values)));
        Assert.DoesNotContain(Password, all);
        Assert.DoesNotContain("SECRET-TOKEN-K20", all);
    }

    [Fact]
    public async Task Metric_tuy_bien_dem_so_lan_tim_kiem_SSR()
    {
        var hits = new ConcurrentBag<(long Value, string? Page)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (i, l) =>
        {
            if (i.Meter.Name == "Lab.TV3" && i.Name == "lab.search.requests") l.EnableMeasurementEvents(i);
        };
        listener.SetMeasurementEventCallback<long>((_, v, tags, _) =>
        {
            string? page = null;
            foreach (var t in tags) if (t.Key == "page") page = t.Value?.ToString();
            hits.Add((v, page));
        });
        listener.Start();

        await Expect(HttpStatusCode.OK, await f.CreateClient().GetAsync("/lab/l16/search?q=pho"));

        Assert.Contains(hits, h => h.Value == 1 && h.Page == "ssr");
    }
}
