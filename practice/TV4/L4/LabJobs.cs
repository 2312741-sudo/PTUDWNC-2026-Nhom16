using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace CulinaryBlog.Practice.Lab4;

/// <summary>Job resize chạy nền: dùng lại `RecipeImageKeys` + `IObjectStorageWriter` của sản phẩm.</summary>
public sealed class LabResizeJob(
    IObjectStorageReader reader,
    IObjectStorageWriter writer,
    ILogger<LabResizeJob> logger)
{
    [Hangfire.AutomaticRetry(Attempts = 3)]
    public async Task RunAsync(string originalKey)
    {
        var scaler = new LabImageScaler(reader, writer, LabLog.For<LabImageScaler>());
        var variants = await scaler.ResizeAsync(originalKey);
        logger.LogInformation("JOB resize xong {Key}: {Count} biến thể", originalKey, variants.Count);
    }
}

/// <summary>Job gửi mail (dùng cho fire-and-forget và delayed).</summary>
public sealed class LabEmailJob(ILogger<LabEmailJob> logger)
{
    [Hangfire.AutomaticRetry(Attempts = 3)]
    public async Task RunAsync(string to, string subject, bool html)
    {
        await EmailPhase.SendAsync(to, subject, html ? "<p>HTML từ Hangfire job</p>" : "plain từ Hangfire job", html);
        logger.LogInformation("JOB email xong: {Subject}", subject);
    }
}

/// <summary>
/// Job sinh sitemap (recurring) — minh hoạ tác vụ XML trong background. Số lần chạy đếm bằng file để
/// assert "chạy lặp" mà không phụ thuộc tên field nội bộ của Hangfire.
/// </summary>
public sealed class LabSitemapJob(ILogger<LabSitemapJob> logger)
{
    private static string CounterPath => Path.Combine(LabConfig.OutDir, $"sitemap-runs-{LabConfig.RunId}.txt");

    [Hangfire.AutomaticRetry(Attempts = 3)]
    public async Task RunAsync()
    {
        var result = await SitemapPhase.RunAsync();
        var runs = int.TryParse(File.Exists(CounterPath) ? File.ReadAllText(CounterPath).Trim() : "0", out var count) ? count : 0;
        runs++;
        Directory.CreateDirectory(LabConfig.OutDir);
        File.WriteAllText(CounterPath, runs.ToString());
        logger.LogInformation("JOB sitemap lần {Runs}: {Status} (file out/sitemap.xml)", runs, result.Passed ? "PASS" : "FAIL");
    }

    public static int GetRuns() => int.TryParse(File.Exists(CounterPath) ? File.ReadAllText(CounterPath).Trim() : "0", out var c) ? c : 0;

    public static async Task<int> WaitForRunsAsync(int expected, TimeSpan timeout, ILogger logger)
    {
        var deadline = DateTime.UtcNow + timeout;
        var runs = GetRuns();
        while (DateTime.UtcNow < deadline && runs < expected)
        {
            await Task.Delay(1000);
            runs = GetRuns();
        }

        logger.LogInformation("recurring 'lab-sitemap' đã chạy {Runs} lần", runs);
        return runs;
    }
}

/// <summary>
/// Job cố tình hỏng 2 lần đầu để minh hoạ retry của Hangfire: số lần đếm lưu trong file (worker dừng rồi chạy lại
/// vẫn giữ đúng số lần → chứng minh state job nằm trong DB, không phải bộ đếm trong RAM).
/// </summary>
public sealed class LabFlakyJob(ILogger<LabFlakyJob> logger)
{
    public const int FailTimes = 2;

    [Hangfire.AutomaticRetry(Attempts = 5, DelaysInSeconds = [5])]
    public Task RunAsync(string counterPath)
    {
        var count = File.Exists(counterPath) ? int.Parse(File.ReadAllText(counterPath).Trim()) : 0;
        count++;
        Directory.CreateDirectory(Path.GetDirectoryName(counterPath)!);
        File.WriteAllText(counterPath, count.ToString());
        logger.LogInformation("JOB flaky lần {Attempt}: {Decision}", count, count <= FailTimes ? "THROW (sẽ retry)" : "OK");
        if (count <= FailTimes)
            throw new InvalidOperationException($"Lab cố tình hỏng lần {count}/{FailTimes} để minh hoạ retry.");
        return Task.CompletedTask;
    }
}

/// <summary>Job kiểm tra kết nối SMTP (dùng cho delayed job sau khi restart worker).</summary>
public sealed class LabPingJob(ILogger<LabPingJob> logger)
{
    [Hangfire.AutomaticRetry(Attempts = 3)]
    public async Task RunAsync(string note)
    {
        using var client = new SmtpClient();
        await client.ConnectAsync(LabConfig.SmtpHost, LabConfig.SmtpPort, SecureSocketOptions.None);
        await client.DisconnectAsync(true);
        logger.LogInformation("JOB ping xong: {Note} (SMTP {Host}:{Port} OK)", note, LabConfig.SmtpHost, LabConfig.SmtpPort);
    }
}
