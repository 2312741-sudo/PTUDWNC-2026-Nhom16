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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var email in queue.ReadAllAsync(stoppingToken))
        {
            var delays = new[] { TimeSpan.Zero, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30) };
            for (var attempt = 0; attempt < delays.Length; attempt++)
            {
                try
                {
                    if (delays[attempt] > TimeSpan.Zero) await Task.Delay(delays[attempt], stoppingToken);
                    await SendAsync(email, stoppingToken);
                    logger.LogInformation("Welcome email sent to {Email}", email.Email);
                    break;
                }
                catch (Exception ex) when (attempt < delays.Length - 1)
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
