using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Context;
using StackExchange.Redis;

namespace Lab.TV3.Api.L20;

/// <summary>Custom metric của lab (Meter "Lab.TV3"), OpenTelemetry thu qua AddMeter.</summary>
public static class LabMetrics
{
    public const string MeterName = "Lab.TV3";
    private static readonly Meter Meter = new(MeterName);

    /// <summary>Số lần tìm kiếm, tag page = ssr (trang L16) | api (JSON L3).</summary>
    public static readonly Counter<long> SearchRequests = Meter.CreateCounter<long>("lab.search.requests", unit: "{request}",
        description: "Số request tìm kiếm công thức");
}

/// <summary>
/// LAB K20 (L5) — Serilog (sink đọc từ cấu hình: Console mặc định, File/Seq bật bằng cấu hình), correlation id,
/// OpenTelemetry trace (ASP.NET Core + Npgsql) và metric, health /health/live + /health/ready (DB + Redis).
/// </summary>
public static partial class Observability
{
    public const string CorrelationHeader = "X-Correlation-ID";
    public const string FileTemplate = "{Timestamp:o} [{Level:u3}] cid={CorrelationId} trace={TraceId} {Message:lj}{NewLine}{Exception}";

    public static void AddL20Observability(this WebApplicationBuilder builder)
    {
        builder.Services.AddSerilog((sp, lc) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            lc.ReadFrom.Configuration(cfg)   // MinimumLevel + WriteTo (Console/File/...) trong mục "Serilog"
              .ReadFrom.Services(sp)         // sink đăng ký qua DI (test dùng sink bộ nhớ)
              .Enrich.FromLogContext()       // lấy CorrelationId do middleware đẩy vào
              .Enrich.WithProperty("Application", "Lab.TV3.Api");
            // Sink bật/tắt bằng cấu hình (biến môi trường LabLog__FilePath, Seq__Url) — không cần sửa code
            if (cfg["LabLog:FilePath"] is { Length: > 0 } path)
                lc.WriteTo.File(path, outputTemplate: FileTemplate, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7);
            if (cfg["Seq:Url"] is { Length: > 0 } seq) lc.WriteTo.Seq(seq);
        });

        var console = builder.Configuration.GetValue<bool>("Otel:ConsoleExporter");
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("lab-tv3-api"))
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation().AddSource("Npgsql"); // Npgsql tự phát Activity cho mỗi câu SQL
                if (console) t.AddConsoleExporter();
            })
            .WithMetrics(m =>
            {
                m.AddAspNetCoreInstrumentation().AddMeter(LabMetrics.MeterName);
                if (console) m.AddConsoleExporter();
            });

        builder.Services.AddHealthChecks()
            .AddCheck<DbHealthCheck>("db", tags: ["ready"])
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
    }

    /// <summary>Gọi sớm nhất trong pipeline để mọi log của request (kể cả log tổng kết) đều có CorrelationId.</summary>
    public static void UseL20Observability(this WebApplication app)
    {
        app.Use(async (ctx, next) =>
        {
            var incoming = ctx.Request.Headers[CorrelationHeader].ToString();
            // Chỉ nhận id an toàn từ client (chống log injection/xuống dòng), còn lại server tự sinh
            var cid = SafeId().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N");
            ctx.Response.Headers[CorrelationHeader] = cid;
            if (Activity.Current is { } act) ctx.Response.Headers["X-Trace-Id"] = act.TraceId.ToHexString();
            using (LogContext.PushProperty("CorrelationId", cid))
                await next(ctx);
        });

        // Log 1 dòng/request: method, path (KHÔNG kèm query string), status, thời gian, user
        app.UseSerilogRequestLogging(o => o.EnrichDiagnosticContext = (d, http) =>
            d.Set("UserId", http.User.FindFirst("sub")?.Value ?? "anonymous"));
    }

    public static void MapL20Health(this WebApplication app)
    {
        // live: process còn sống, không gọi phụ thuộc (orchestrator không restart app chỉ vì Redis chết)
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false, ResponseWriter = WriteJson });
        // ready: có nhận traffic được không -> kiểm DB + Redis, lỗi thì 503
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready"), ResponseWriter = WriteJson });
    }

    private static Task WriteJson(HttpContext ctx, HealthReport report)
    {
        ctx.Response.ContentType = "application/json; charset=utf-8";
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            totalMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            entries = report.Entries.ToDictionary(e => e.Key, e => new
            {
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                ms = Math.Round(e.Value.Duration.TotalMilliseconds, 1),
            }),
        }));
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex SafeId();
}

public sealed class DbHealthCheck(LabDb db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await using var c = await db.OpenAsync(ct);
            await using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync(ct);
            return HealthCheckResult.Healthy("PostgreSQL OK");
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL lỗi: " + e.GetType().Name); // không trả chuỗi kết nối/mật khẩu
        }
    }
}

public sealed class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        if (!redis.IsConnected) return HealthCheckResult.Unhealthy("Redis chưa kết nối");
        try
        {
            var latency = await redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis ping {latency.TotalMilliseconds:0.0} ms");
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy("Redis lỗi: " + e.GetType().Name);
        }
    }
}
