using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.PostgreSql.Factories;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CulinaryBlog.Practice.Lab4;

/// <summary>
/// Phase JOBS (L4) — trọng tâm của Hangfire theo đề bài:
/// 1) fire-and-forget · 2) delayed + tắt/restart worker (job không bị mất) · 3) retry · 4) recurring.
/// Hàng đợi nằm trong DB riêng `culinary_lab` (schema Hangfire tự tạo) nên không đụng dữ liệu sản phẩm.
/// </summary>
public static class JobsPhase
{
    private const string Queue = "lab";

    public static async Task<PhaseResult> RunAsync(IServiceProvider services)
    {
        var logger = LabLog.For("LAB/jobs");
        var checks = new List<Check>();
        logger.LogInformation("=== PHASE JOBS (L4) === queue={Queue} storage=PostgreSQL db={Db}", Queue, LabConfig.LabDatabase);

        await EnsureLabDatabaseAsync(logger);
        // Hangfire.PostgreSql 1.21: ctor nhận IConnectionFactory (ctor (string, options) đã obsolete).
        var storageOptions = new PostgreSqlStorageOptions { PrepareSchemaIfNecessary = true };
        var storage = new PostgreSqlStorage(
            new NpgsqlConnectionFactory(LabConfig.LabConnection, storageOptions),
            storageOptions);
        // JobStorage.Current là nơi BackgroundJob/RecurringJob tìm storage.
        JobStorage.Current = storage;
        logger.LogInformation("Hangfire storage sẵn sàng (JobStorage.Current); schema được tạo tự động nếu thiếu.");

        var serverOptions = new BackgroundJobServerOptions
        {
            Queues = [Queue],
            WorkerCount = 1,
            SchedulePollingInterval = TimeSpan.FromSeconds(5)
        };

        // Hangfire 1.8: JobActivator.Current là điểm móc toàn cục cho worker (console không có ASP.NET Core activator).
        JobActivator.Current = new LabJobActivator(services);
        var server = new BackgroundJobServer(serverOptions, storage);
        var monitor = new LabJobMonitor(storage, logger);
        logger.LogInformation("Worker đã khởi động (queue={Queue}, workerCount={Workers})", Queue, serverOptions.WorkerCount);

        // 1) Fire-and-forget: resize ảnh lab.
        var key = $"{LabConfig.LabKeyPrefix}/jobs-sample.jpg";
        await UploadSampleAsync(services, key, logger);
        var resizeJob = BackgroundJob.Enqueue<LabResizeJob>(Queue, j => j.RunAsync(key));
        var resizeState = await monitor.WaitForFinalAsync(resizeJob);
        checks.Add(new Check("fire-and-forget: job resize", resizeState == "Succeeded", $"job={resizeJob} state={resizeState}"));
        var variants = RecipeImageKeys.ResizedKeys(key);
        if (variants is not null)
        {
            var writer = services.GetRequiredService<IObjectStorageWriter>();
            checks.Add(new Check("resize tạo đủ 2 object phái sinh",
                await writer.ExistsAsync(variants.Value.ThumbnailKey) && await writer.ExistsAsync(variants.Value.MediumKey),
                $"{variants.Value.ThumbnailKey} + {variants.Value.MediumKey}"));
        }

        // 2) Delayed job + restart worker: job phải sống sót khi không có worker.
        var delayedDelay = TimeSpan.FromSeconds(15);
        var delayedJob = BackgroundJob.Schedule<LabPingJob>(Queue, j => j.RunAsync("delayed sau khi restart worker"), delayedDelay);
        logger.LogInformation("Delayed job {Job} hẹn chạy sau {Seconds}s — tắt worker NGAY để job phải chờ trong DB.",
            delayedJob, (int)delayedDelay.TotalSeconds);
        server.SendStop();
        server.WaitForShutdown(TimeSpan.FromSeconds(20));
        logger.LogInformation("Worker đã dừng. Đợi {Seconds}s để delay trôi qua (không có worker xử lý).",
            (int)delayedDelay.TotalSeconds + 8);
        await Task.Delay(delayedDelay + TimeSpan.FromSeconds(8));
        var stateWhileDown = monitor.GetState(delayedJob);
        checks.Add(new Check("tắt worker: job vẫn còn trong DB (không mất)", stateWhileDown == "Scheduled",
            $"job={delayedJob} state={stateWhileDown}"));

        server = new BackgroundJobServer(serverOptions, storage);
        logger.LogInformation("Khởi động lại worker — job delayed phải tự chạy.");
        var delayedState = await monitor.WaitForFinalAsync(delayedJob, TimeSpan.FromSeconds(90));
        checks.Add(new Check("restart worker: job delayed chạy được", delayedState == "Succeeded",
            $"job={delayedJob} state={delayedState}"));

        // 3) Retry: job hỏng 2 lần đầu, Hangfire retry 5 lần (delay 5s) rồi thành công.
        var counterPath = Path.Combine(LabConfig.OutDir, $"flaky-{LabConfig.RunId}.txt");
        var flakyJob = BackgroundJob.Enqueue<LabFlakyJob>(Queue, j => j.RunAsync(counterPath));
        var flakyState = await monitor.WaitForFinalAsync(flakyJob, TimeSpan.FromSeconds(90));
        var attempts = File.Exists(counterPath) ? File.ReadAllText(counterPath).Trim() : "0";
        checks.Add(new Check("retry: job hỏng 2 lần rồi thành công", flakyState == "Succeeded",
            $"job={flakyJob} state={flakyState} attempts={attempts} (2 lần THROW trước đó theo log)"));
        checks.Add(new Check("retry: số lần chạy = số lần hỏng + 1", attempts == (LabFlakyJob.FailTimes + 1).ToString(),
            $"attempts={attempts} (kỳ vọng {LabFlakyJob.FailTimes + 1})"));

        // 4) Recurring: sitemap chạy lặp (lab dùng */5 giây; sản phẩm dùng cron 02:00 UTC theo D26).
        RecurringJob.AddOrUpdate<LabSitemapJob>("lab-sitemap", Queue, job => job.RunAsync(), "*/5 * * * * *",
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        logger.LogInformation("Đã đăng ký recurring job 'lab-sitemap' (cron */5 * * * * * UTC). Đợi 2 lần chạy...");
        var recurringRuns = await LabSitemapJob.WaitForRunsAsync(2, TimeSpan.FromSeconds(45), logger);
        var recurringHash = monitor.GetRecurringHash("lab-sitemap");
        checks.Add(new Check("recurring: sitemap chạy lặp nhiều lần", recurringRuns >= 2, $"số lần chạy={recurringRuns}"));
        checks.Add(new Check("recurring: Hangfire lưu hash trạng thái job định kỳ", recurringHash.Count > 0,
            string.Join(", ", recurringHash.Take(6).Select(kv => $"{kv.Key}={kv.Value}"))));

        // 5) Dọn dẹp: dừng worker + gỡ recurring job + xoá object lab.
        server.SendStop();
        server.WaitForShutdown(TimeSpan.FromSeconds(20));
        new RecurringJobManager(storage).RemoveIfExists("lab-sitemap");
        await DeleteLabObjectsAsync(services, key, logger);
        logger.LogInformation("Worker đã dừng, recurring job đã gỡ, object lab đã xoá.");

        return PhaseResult.From("JOBS", checks);
    }

    private static async Task EnsureLabDatabaseAsync(ILogger logger)
    {
        await using var connection = new NpgsqlConnection(LabConfig.AdminConnection);
        await connection.OpenAsync();
        await using var exists = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        exists.Parameters.AddWithValue("name", LabConfig.LabDatabase);
        if (await exists.ExecuteScalarAsync() is null)
        {
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{LabConfig.LabDatabase}\"", connection);
            await create.ExecuteNonQueryAsync();
            logger.LogInformation("Đã tạo database lab {Database}", LabConfig.LabDatabase);
        }
        else
        {
            logger.LogInformation("Dùng database lab có sẵn {Database}", LabConfig.LabDatabase);
        }
    }

    private static async Task UploadSampleAsync(IServiceProvider services, string key, ILogger logger)
    {
        var writer = services.GetRequiredService<IObjectStorageWriter>();
        if (await writer.ExistsAsync(key)) return;
        var fixture = (await Fixtures.CreateAsync()).First(f => f.Name == "sample.jpg");
        using var content = new MemoryStream(fixture.Bytes);
        await writer.UploadAsync(key, content, fixture.DeclaredMime, content.Length);
        logger.LogInformation("Đã upload ảnh lab {Key} ({Bytes} bytes)", key, fixture.Bytes.Length);
    }

    private static async Task DeleteLabObjectsAsync(IServiceProvider services, string key, ILogger logger)
    {
        // Xoá object dùng IFileStorageService (IObjectStorageWriter chỉ có Exists/Upload — đúng ranh giới đã chốt ở N4).
        var storage = services.GetRequiredService<IFileStorageService>();
        var variants = RecipeImageKeys.ResizedKeys(key);
        await storage.DeleteAsync(key);
        if (variants is not null)
        {
            await storage.DeleteAsync(variants.Value.ThumbnailKey);
            await storage.DeleteAsync(variants.Value.MediumKey);
        }

        logger.LogInformation("Đã xoá object lab của {Key}", key);
    }
}

/// <summary>
/// Job activator giải phụ thuộc qua DI (console không có ASP.NET Core JobActivator).
/// Hangfire 1.8 gọi BeginScope(PerformContext) rồi Resolve(job.Type) trong scope đó, nên DI scope sống
/// đúng bằng thời gian chạy job và được Hangfire dispose sau khi job kết thúc.
/// </summary>
internal sealed class LabJobActivator(IServiceProvider root) : JobActivator
{
    public override JobActivatorScope BeginScope(PerformContext context) => new LabJobActivatorScope(root);

    public override JobActivatorScope BeginScope(JobActivatorContext context) => new LabJobActivatorScope(root);
}

internal sealed class LabJobActivatorScope(IServiceProvider root) : JobActivatorScope
{
    private readonly IServiceScope _scope = root.CreateScope();

    public override object Resolve(Type type)
        => _scope.ServiceProvider.GetService(type) ?? ActivatorUtilities.CreateInstance(_scope.ServiceProvider, type);

    public override void DisposeScope() => _scope.Dispose();
}

/// <summary>Đọc state job qua Hangfire storage API để assert bằng chứng, không đoán theo log.</summary>
internal sealed class LabJobMonitor(JobStorage storage, ILogger logger)
{
    public string? GetState(string jobId)
    {
        using var connection = storage.GetConnection();
        return connection.GetJobData(jobId)?.State;
    }

    public async Task<string> WaitForFinalAsync(string jobId, TimeSpan? timeout = null)
    {
        var limit = timeout ?? TimeSpan.FromSeconds(60);
        var deadline = DateTime.UtcNow + limit;
        string? state = null;
        while (DateTime.UtcNow < deadline)
        {
            state = GetState(jobId);
            if (state is "Succeeded" or "Failed" or "Deleted") break;
            await Task.Delay(500);
        }

        logger.LogInformation("job {Job} -> state {State}", jobId, state ?? "(null)");
        return state ?? "Unknown";
    }

    public Dictionary<string, string> GetRecurringHash(string recurringJobId)
    {
        using var connection = storage.GetConnection();
        return connection.GetAllEntriesFromHash($"recurring-job:{recurringJobId}")
            ?.ToDictionary(kv => kv.Key, kv => kv.Value) ?? [];
    }
}
