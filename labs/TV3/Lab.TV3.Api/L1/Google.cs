using Google.Apis.Auth;

namespace Lab.TV3.Api.L1;

public sealed record GoogleIdentity(string Subject, string Email, bool EmailVerified, string? Name);

/// <summary>Tách interface để unit/error test thay bằng fake; integration thật cần Google:ClientId.</summary>
public interface IGoogleTokenVerifier
{
    Task<GoogleIdentity?> VerifyAsync(string idToken, CancellationToken ct);
}

public sealed class GoogleNotConfiguredException() : Exception("Chưa cấu hình Google:ClientId") { }

/// <summary>Xác thực ID token Google: chữ ký (JWKS của Google), iss, aud = ClientId, exp.</summary>
public sealed class GoogleTokenVerifier(IConfiguration cfg, ILogger<GoogleTokenVerifier> log) : IGoogleTokenVerifier
{
    public async Task<GoogleIdentity?> VerifyAsync(string idToken, CancellationToken ct)
    {
        var clientId = cfg["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId)) throw new GoogleNotConfiguredException();
        try
        {
            var p = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });
            return new GoogleIdentity(p.Subject, p.Email, p.EmailVerified, p.Name);
        }
        catch (InvalidJwtException ex)
        {
            log.LogWarning("Google ID token bị từ chối: {Reason}", ex.Message);
            return null;
        }
    }
}