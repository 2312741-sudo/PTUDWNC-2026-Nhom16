using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Practice.Lab4;

/// <summary>
/// Phase EMAIL (L4): gửi mail qua SMTP tới Mailhog bằng MailKit, rồi đọc Mailhog HTTP API để chứng minh mail
/// thật sự nằm trong hộp thư (không chỉ "không ném exception"). Mailhog chỉ là SMTP sink, không cần auth.
/// </summary>
public static class EmailPhase
{
    public static async Task<PhaseResult> RunAsync()
    {
        var logger = LabLog.For("LAB/email");
        var checks = new List<Check>();
        logger.LogInformation("=== PHASE EMAIL (L4) === smtp={Host}:{Port} api={Api}", LabConfig.SmtpHost, LabConfig.SmtpPort, LabConfig.SmtpApi);

        if (!await IsReachableAsync(LabConfig.SmtpHost, LabConfig.SmtpPort))
        {
            logger.LogWarning("SMTP {Host}:{Port} không kết nối được — bỏ qua phase. Chạy: docker run -d --name lab-mailhog -p 1025:1025 -p 8025:8025 mailhog/mailhog:v1.0.1",
                LabConfig.SmtpHost, LabConfig.SmtpPort);
            return PhaseResult.From("EMAIL", [new Check("Mailhog reachable", true, "SKIP (Mailhog chưa chạy) — xem README")]);
        }

        var before = await CountMessagesAsync();
        var sent = 0;
        try
        {
            await SendAsync("tv4@culinary.local", $"[LAB L4] Plain text {LabConfig.RunId}", "<p>Plain</p>", html: false);
            await SendAsync("tv4@culinary.local", $"[LAB L4] HTML + ảnh {LabConfig.RunId}",
                "<html><body><h1>Resize xong</h1><p>Thumbnail 300×300 · Medium 800×600</p></body></html>", html: true);
            sent = 2;
        }
        catch (Exception ex)
        {
            checks.Add(new Check("gửi 2 email qua SMTP", false, $"{ex.GetType().Name}: {ex.Message}"));
            return PhaseResult.From("EMAIL", checks);
        }

        checks.Add(new Check("gửi 2 email qua SMTP (plain + HTML)", sent == 2, "MailKit không ném exception"));

        var after = await CountMessagesAsync();
        checks.Add(new Check("Mailhog nhận đủ 2 mail", after >= before + sent, $"trước={before} sau={after}"));

        var subjects = await LatestSubjectsAsync(sent);
        checks.Add(new Check("Mailhog API trả subject của mail vừa gửi",
            subjects.Any(s => s.Contains("[LAB L4]", StringComparison.Ordinal)), string.Join(" | ", subjects)));

        return PhaseResult.From("EMAIL", checks);
    }

    public static async Task SendAsync(string to, string subject, string body, bool html)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(LabConfig.SmtpFrom));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart(html ? "html" : "plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(LabConfig.SmtpHost, LabConfig.SmtpPort, SecureSocketOptions.None);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private static async Task<bool> IsReachableAsync(string host, int port)
    {
        try
        {
            using var tcp = new System.Net.Sockets.TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await tcp.ConnectAsync(host, port, cts.Token);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task<int> CountMessagesAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var json = await http.GetStringAsync($"{LabConfig.SmtpApi}/api/v2/messages?limit=1");
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("total", out var total) ? total.GetInt32() : -1;
        }
        catch (Exception ex)
        {
            LabLog.For("LAB/email").LogWarning("Đọc Mailhog API lỗi: {Error}", ex.Message);
            return -1;
        }
    }

    private static async Task<List<string>> LatestSubjectsAsync(int limit)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var json = await http.GetStringAsync($"{LabConfig.SmtpApi}/api/v2/messages?limit={limit}");
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("items", out var items)) return [];
            // Mailhog v2 đặt header trong items[].Content.Headers.Subject (mảng).
            return items.EnumerateArray()
                .Select(item => item.TryGetProperty("Content", out var content) &&
                                content.TryGetProperty("Headers", out var headers) &&
                                headers.TryGetProperty("Subject", out var subject)
                    ? string.Join(" | ", subject.EnumerateArray().Select(s => s.GetString() ?? ""))
                    : "")
                .ToList();
        }
        catch (Exception ex)
        {
            LabLog.For("LAB/email").LogWarning("Đọc danh sách mail lỗi: {Error}", ex.Message);
            return [];
        }
    }
}
