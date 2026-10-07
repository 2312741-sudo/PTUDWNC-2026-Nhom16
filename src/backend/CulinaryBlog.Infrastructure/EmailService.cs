using System.Net;
using System.Net.Mail;
using CulinaryBlog.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure;

public sealed class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host = config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogWarning("SMTP is not configured. Email to {Email} with subject '{Subject}' was not sent.", toEmail, subject);
            return;
        }

        try
        {
            using var client = new SmtpClient(host, config.GetValue("Smtp:Port", 1025))
            {
                EnableSsl = config.GetValue("Smtp:EnableSsl", false)
            };

            var username = config["Smtp:Username"];
            var password = config["Smtp:Password"];
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            using var message = new MailMessage(config["Smtp:From"] ?? "no-reply@culinary.local", toEmail)
            {
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            await client.SendMailAsync(message, ct);
            logger.LogInformation("Email sent successfully to {Email} (Subject: {Subject})", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }
}
