using System.Net;
using System.Net.Mail;
using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure;

public sealed record WelcomeEmail(string Email, string DisplayName);
public interface IWelcomeEmailQueue
{
    ValueTask EnqueueAsync(WelcomeEmail email, CancellationToken ct);
}
public sealed class WelcomeEmailQueue : IWelcomeEmailQueue
{
    private readonly Channel<WelcomeEmail> channel = Channel.CreateUnbounded<WelcomeEmail>();
    public ValueTask EnqueueAsync(WelcomeEmail email, CancellationToken ct) => channel.Writer.WriteAsync(email, ct);
    public IAsyncEnumerable<WelcomeEmail> ReadAllAsync(CancellationToken ct) => channel.Reader.ReadAllAsync(ct);
}
public sealed class WelcomeEmailWorker(WelcomeEmailQueue queue, IConfiguration config, ILogger<WelcomeEmailWorker> logger) : BackgroundService
{
    /// <summary>
    /// Lịch thử lại gửi email chào mừng (N2-C1c: retry 3 lần tại 1 / 5 / 30 phút).
    /// Phần tử đầu là 0 vì lần gửi đầu tiên không chờ. Tổng cộng <see cref="RetryDelays"/> đây
    /// là 4 lần thử = 1 lần gửi + 3 lần retry.
    ///
    /// Tách ra khỏi thân <c>ExecuteAsync</c> để test được mà không phải chờ tới 36 phút thật:
    /// nếu để hằng nội tuyến thì cách duy nhất kiểm chứng là đọc IL hoặc chạy thật — cả hai đều tệ.
    /// </summary>
    public static readonly TimeSpan[] RetryDelays =
    {
        TimeSpan.Zero, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30)
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var email in queue.ReadAllAsync(stoppingToken))
        {
            for (var attempt = 0; attempt < RetryDelays.Length; attempt++)
            {
                try
                {
                    if (RetryDelays[attempt] > TimeSpan.Zero) await Task.Delay(RetryDelays[attempt], stoppingToken);
                    await SendAsync(email, stoppingToken);
                    logger.LogInformation("Welcome email sent to {Email}", email.Email);
                    break;
                }
                catch (Exception ex) when (attempt < RetryDelays.Length - 1)
                {
                    logger.LogWarning("Welcome email attempt {Attempt} failed for {Email}: {ErrorType}", attempt + 1, email.Email, ex.GetType().Name);
                }
                catch (Exception ex)
                {
                    logger.LogError("Welcome email permanently failed for {Email}: {ErrorType}", email.Email, ex.GetType().Name);
                }
            }
        }
    }
    private async Task SendAsync(WelcomeEmail email, CancellationToken ct)
    {
        var host = config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host)) return; // SMTP is optional in local runs; queue remains observable.
        using var client = new SmtpClient(host, config.GetValue("Smtp:Port", 1025))
        {
            EnableSsl = config.GetValue("Smtp:EnableSsl", false),
            Credentials = new NetworkCredential(config["Smtp:Username"], config["Smtp:Password"])
        };
        using var message = new MailMessage(config["Smtp:From"] ?? "no-reply@culinary.local", email.Email)
        {
            Subject = "Chào mừng bạn đến Culinary Blog",
            Body = $"<html><body><h1>Xin chào {WebUtility.HtmlEncode(email.DisplayName)}!</h1><p>Chào mừng bạn đến với Culinary Blog.</p></body></html>",
            IsBodyHtml = true
        };
        await client.SendMailAsync(message, ct);
    }
}
