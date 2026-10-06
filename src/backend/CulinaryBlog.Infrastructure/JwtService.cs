using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CulinaryBlog.Application;
using Microsoft.IdentityModel.Tokens;

namespace CulinaryBlog.Infrastructure;

public sealed class JwtSettings
{
    /// <summary>
    /// QD3-3b: khoá dev đã bị commit lịch sử trong appsettings*.json — không được dùng lại.
    /// Danh sách chặn để app fail-fast thay vì âm thầm ký token bằng khoá đã lộ.
    /// </summary>
    private static readonly string[] RevokedSigningKeys =
    [
        "development-secret-key-that-is-at-least-64-bytes-long-for-jwt-signing-256-bits-security",
    ];

    public string Issuer { get; set; } = "culinary-blog";
    public string Audience { get; set; } = "culinary-blog-client";
    public string SigningKey { get; set; } = "";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SigningKey) || Encoding.UTF8.GetByteCount(SigningKey) < 64
            || string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException(
                "Thieu/yeu Jwt:SigningKey (it nhat 64 byte UTF-8), Issuer hoac Audience. "
                + "Dat bien moi truong Jwt__SigningKey hoac dong trong .env (khong commit). "
                + "Sinh khoa ngau nhien bang: openssl rand -base64 48");

        if (RevokedSigningKeys.Any(k => string.Equals(k, SigningKey, StringComparison.Ordinal)))
            throw new InvalidOperationException(
                "Jwt:SigningKey la khoa dev da bi commit vao repo (bi thu hoi) — hay doi sang khoa moi. "
                + "Sinh khoa ngau nhien bang: openssl rand -base64 48");
    }
}

public sealed class JwtService(JwtSettings settings, TimeProvider clock)
{
    public AuthResponse Issue(UserDto user, string? refreshToken = null)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var expiresAtUtc = now.AddMinutes(15);
        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, user.Id), new("userId", user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        claims.AddRange(user.Roles.Select(role => new Claim("role", role)));
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, now, expiresAtUtc,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)), SecurityAlgorithms.HmacSha256));

        return new AuthResponse(
            AccessToken: new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken: refreshToken,
            TokenType: "Bearer",
            ExpiresIn: 900,
            ExpiresAt: new DateTimeOffset(expiresAtUtc),
            User: user);
    }
}
