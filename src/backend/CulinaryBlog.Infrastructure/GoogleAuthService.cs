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
