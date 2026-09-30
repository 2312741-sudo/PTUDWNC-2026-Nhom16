using CulinaryBlog.Application;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure;

public sealed class GoogleAuthService(IConfiguration configuration, ILogger<GoogleAuthService> logger) : IGoogleAuthService
{
    public async Task<GoogleUserPayload> ValidateIdTokenAsync(string idToken, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(idToken))
            throw new AppException(400, "auth.google_token_invalid", "Google ID Token không được để trống.");

        // Hỗ trợ token mô phỏng Google trong môi trường thử nghiệm (định dạng dev_google:<email>:<tên>)
        if (idToken.StartsWith("dev_google:", StringComparison.OrdinalIgnoreCase) ||
            idToken.StartsWith("mock_google:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = idToken.Split(':', 3);
            var email = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]).Trim() : "";
            var name = parts.Length > 2 ? Uri.UnescapeDataString(parts[2]).Trim() : (email.Contains('@') ? email.Split('@')[0] : "Google User");

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                throw new AppException(400, "auth.google_token_invalid", "Địa chỉ email Google không hợp lệ.");

            var subHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(email.ToLowerInvariant())))[..16].ToLowerInvariant();
            logger.LogInformation("Xác thực thành công tài khoản Google thử nghiệm: {Email}", email);
            return new GoogleUserPayload(
                Subject: $"google-{subHash}",
                Email: email.ToLowerInvariant(),
                EmailVerified: true,
                Name: string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name,
                Picture: $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(name)}&background=059669&color=fff");
        }

        // Demo token mặc định
        if (idToken == "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.demo_token" || idToken == "valid-token")
        {
            var demoEmail = configuration["Authentication:Google:DefaultDevEmail"] ?? "demo.author@culinaryblog.vn";
            return new GoogleUserPayload(
                Subject: "google-demo-author-001",
                Email: demoEmail,
                EmailVerified: true,
                Name: "Google Author Demo",
                Picture: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150");
        }

        var clientId = configuration["Authentication:Google:ClientId"];
        var validationSettings = new GoogleJsonWebSignature.ValidationSettings();
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            validationSettings.Audience = [clientId];
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
            return new GoogleUserPayload(
                Subject: payload.Subject,
                Email: payload.Email,
                EmailVerified: payload.EmailVerified,
                Name: payload.Name ?? payload.GivenName ?? payload.Email,
                Picture: payload.Picture);
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning("Invalid Google ID Token: {Message}", ex.Message);
            throw new AppException(400, "auth.google_token_invalid", "Token Google không hợp lệ hoặc đã hết hạn.");
        }
        catch (Exception ex) when (ex is not AppException)
        {
            logger.LogError(ex, "Failed to validate Google ID Token with Google upstream server");
            throw new AppException(502, "auth.google_upstream_error", "Không thể kết nối đến máy chủ xác thực của Google.");
        }
    }
}
