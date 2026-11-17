using CulinaryBlog.Application;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure;

public sealed class GoogleAuthService(
    IConfiguration configuration,
    ILogger<GoogleAuthService> logger,
    IHostEnvironment? environment = null) : IGoogleAuthService
{
    private static bool IsMockToken(string idToken) =>
        idToken.StartsWith("dev_google:", StringComparison.OrdinalIgnoreCase) ||
        idToken.StartsWith("mock_google:", StringComparison.OrdinalIgnoreCase) ||
        idToken == "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.demo_token" ||
        idToken == "valid-token";

    public async Task<GoogleUserPayload> ValidateIdTokenAsync(string idToken, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(idToken))
            throw new AppException(400, "auth.google_token_invalid", "Google ID Token không được để trống.");

        var isProduction = environment?.IsProduction() == true;
        var allowDevTokens = !isProduction && (
            environment?.IsDevelopment() == true ||
            configuration.GetValue<bool>("Authentication:Google:AllowDevTokens"));

        if (IsMockToken(idToken))
        {
            if (!allowDevTokens)
            {
                logger.LogWarning(
                    "Từ chối token Google thử nghiệm trong môi trường {Environment}",
                    environment?.EnvironmentName ?? "Unknown");
                throw new AppException(400, "auth.google_token_invalid", "Token Google thử nghiệm không được hỗ trợ trong môi trường hiện tại.");
            }

            // Token mô phỏng định dạng dev_google:<email>:<tên> hoặc mock_google:<email>:<tên>
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

            // Demo token mặc định (cho kiểm thử ở môi trường dev)
            var demoEmail = configuration["Authentication:Google:DefaultDevEmail"] ?? "demo.author@culinaryblog.vn";
            return new GoogleUserPayload(
                Subject: "google-demo-author-001",
                Email: demoEmail,
                EmailVerified: true,
                Name: "Google Author Demo",
                Picture: "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150");
        }

        var clientId = configuration["Authentication:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            logger.LogError("Thiếu cấu hình Authentication:Google:ClientId; từ chối xác thực Google ID Token để tránh bỏ qua kiểm tra Audience.");
            throw new AppException(503, "auth.google_not_configured", "Đăng nhập Google chưa được cấu hình Client ID trên máy chủ.");
        }

        var validationSettings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [clientId]
        };

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                logger.LogWarning("Google ID Token không chứa thông tin email");
                throw new AppException(400, "auth.google_token_invalid", "Token Google không chứa địa chỉ email.");
            }

            if (string.IsNullOrWhiteSpace(payload.Subject))
            {
                logger.LogWarning("Google ID Token không chứa thông tin subject");
                throw new AppException(400, "auth.google_token_invalid", "Token Google không hợp lệ (thiếu subject).");
            }

            return new GoogleUserPayload(
                Subject: payload.Subject,
                Email: payload.Email.ToLowerInvariant(),
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
