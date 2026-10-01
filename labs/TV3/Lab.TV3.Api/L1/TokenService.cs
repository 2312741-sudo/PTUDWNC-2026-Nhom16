using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.IdentityModel.Tokens;

namespace Lab.TV3.Api.L1;

/// <summary>JWT 15 phút + refresh token 512-bit ngẫu nhiên, DB chỉ giữ SHA-256 (K08, SEC-002).</summary>
public sealed class TokenService(IConfiguration cfg, LabDb db)
{
    public const int AccessMinutes = 15;
    public const int RefreshDays = 7;

    public static TokenValidationParameters ValidationParameters(IConfiguration cfg) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = cfg["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = cfg["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = Key(cfg),
        RoleClaimType = "role", // tránh lỗi 403 giống bản SP trước khi sửa
        NameClaimType = "sub",
    };

    private static SymmetricSecurityKey Key(IConfiguration cfg) =>
        new(Encoding.UTF8.GetBytes(cfg["Jwt:SigningKey"] ?? throw new InvalidOperationException("Thiếu Jwt:SigningKey")));

    public async Task<AuthResult> IssueAsync(LabUser u, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var jwt = new JwtSecurityToken(
            cfg["Jwt:Issuer"], cfg["Jwt:Audience"],
            [
                new Claim("sub", u.Id.ToString()),
                new Claim("email", u.Email),
                new Claim("role", u.Role),
                new Claim("verified_author", u.VerifiedAuthor ? "true" : "false"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ],
            now, now.AddMinutes(AccessMinutes),
            new SigningCredentials(Key(cfg), SecurityAlgorithms.HmacSha256));

        var refresh = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
        await using var c = await db.OpenAsync(ct);
        await c.ExecuteAsync(
            "INSERT INTO lab_refresh_tokens (id, user_id, token_hash, expires_at) VALUES (@id, @uid, @hash, @exp)",
            new { id = Guid.NewGuid(), uid = u.Id, hash = Hash(refresh), exp = now.AddDays(RefreshDays) });

        return new AuthResult(new JwtSecurityTokenHandler().WriteToken(jwt), AccessMinutes * 60, refresh, u.ToView());
    }

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
