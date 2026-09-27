using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Practice.Lab4;

/// <summary>
/// Lab L4 (TV4) — "Media &amp; jobs": upload/delete 4 MIME theo nội dung + kích thước thực, resize 2 size,
/// gửi mail SMTP qua Mailhog, sinh sitemap XML, và 4 kiểu Hangfire (fire-and-forget, delayed + restart worker,
/// retry, recurring). Chạy: dotnet run --project practice/TV4/L4 -- [media|email|xml|jobs|all]
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var phase = args.FirstOrDefault()?.Trim().ToLowerInvariant() ?? "all";
        Directory.CreateDirectory(LabConfig.OutDir);

        var logger = LabLog.For("LAB/run");
        logger.LogInformation("=== LAB L4 (TV4) bắt đầu — phase={Phase} run={RunId} ===", phase, LabConfig.RunId);
        logger.LogInformation("MinIO {Endpoint} bucket {Bucket} · SMTP {Smtp}:{Port} · DB lab {LabDb} · app DB {AppDb}",
            LabConfig.MinioEndpoint, LabConfig.StorageBucket, LabConfig.SmtpHost, LabConfig.SmtpPort,
            LabConfig.LabDatabase, LabConfig.AppConnection is null ? "(không có — dùng fixture)" : "culinary_test");
        logger.LogInformation("Log ghi tại {LogFile}", LabConfig.LogFile);

        await using var services = BuildServices();
        try
        {
            if (phase is "db") return await DbEvidencePhase.RunAsync(logger);
            if (phase is "purge") return await PurgePhase.RunAsync(logger);
            if (phase is "media" or "all") LabResults.Add(await MediaPhase.RunAsync(services));
            if (phase is "email" or "all") LabResults.Add(await EmailPhase.RunAsync());
            if (phase is "xml" or "all") LabResults.Add(await SitemapPhase.RunAsync());
            if (phase is "jobs" or "all") LabResults.Add(await JobsPhase.RunAsync(services));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Phase {Phase} lỗi: {Message}", phase, ex.Message);
            return 1;
        }

        if (phase is "media" or "email" or "xml" or "jobs")
        {
            var result = LabResults.Last;
            logger.LogInformation("KẾT THÚC phase {Phase}: {Status}", phase, result?.Passed == true ? "PASS" : "FAIL");
            return result?.Passed == true ? 0 : 1;
        }

        return LabResults.WriteSummary() ? 0 : 1;
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LabLog.Factory);
        services.AddLogging();
        services.AddSingleton<IOptions<MinioOptions>>(Options.Create(new MinioOptions
        {
            Endpoint = LabConfig.MinioEndpoint,
            AccessKey = Environment.GetEnvironmentVariable("Minio__AccessKey") ?? "",
            SecretKey = Environment.GetEnvironmentVariable("Minio__SecretKey") ?? "",
            Bucket = LabConfig.StorageBucket
        }));
        services.AddScoped<MinioStorageService>();
        services.AddScoped<IFileStorageService>(sp => sp.GetRequiredService<MinioStorageService>());
        services.AddScoped<IObjectStorageReader>(sp => sp.GetRequiredService<MinioStorageService>());
        services.AddScoped<IObjectStorageWriter>(sp => sp.GetRequiredService<MinioStorageService>());
        return services.BuildServiceProvider();
    }
}
