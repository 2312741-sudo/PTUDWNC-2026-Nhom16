using System.Net.Sockets;
using CulinaryBlog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.API;

public sealed class DatabaseHealthCheck(AuthDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        try
        {
            var ok = await db.Database.CanConnectAsync(ct);
            return ok
                ? HealthCheckResult.Healthy("PostgreSQL reachable.")
                : HealthCheckResult.Unhealthy("PostgreSQL unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL check failed.", ex);
        }
    }
}

public sealed class RedisHealthCheck(IConfiguration cfg) : IHealthCheck
{
    private readonly string _host = cfg["HealthChecks:Redis:Host"] ?? "localhost";
    private readonly int _port = int.Parse(cfg["HealthChecks:Redis:Port"] ?? "6379");

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
        => TcpHealthCheckHelper.CheckTcpAsync(_host, _port, ct);
}

public sealed class MinIOHealthCheck(IConfiguration cfg) : IHealthCheck
{
    private readonly string _host = cfg["HealthChecks:Minio:Host"] ?? "localhost";
    private readonly int _port = int.Parse(cfg["HealthChecks:Minio:Port"] ?? "9000");

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
        => TcpHealthCheckHelper.CheckTcpAsync(_host, _port, ct);
}

public sealed class LivenessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
        => Task.FromResult(HealthCheckResult.Healthy("Process alive."));
}

internal static class TcpHealthCheckHelper
{
    internal static async Task<HealthCheckResult> CheckTcpAsync(string host, int port, CancellationToken ct)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(host, port, cts.Token);
            return HealthCheckResult.Healthy($"{host}:{port} reachable.");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy($"{host}:{port} timed out.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"{host}:{port} unreachable.", ex);
        }
    }
}

public static class HealthReportWriter
{
    public static Task WriteJson(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    durationMs = e.Value.Duration.TotalMilliseconds
                })
        });
    }
}
