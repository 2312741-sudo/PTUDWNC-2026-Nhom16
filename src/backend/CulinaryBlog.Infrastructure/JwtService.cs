using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CulinaryBlog.Application;
using Microsoft.IdentityModel.Tokens;

namespace CulinaryBlog.Infrastructure;

public sealed class JwtSettings
{
    public string Issuer { get; set; } = "culinary-blog";
    public string Audience { get; set; } = "culinary-blog-client";
    public string SigningKey { get; set; } = "";
    public void Validate()
    {
        if (Encoding.UTF8.GetByteCount(SigningKey) < 64 || string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Configure Jwt:SigningKey (at least 64 UTF-8 bytes), Issuer and Audience through secrets/environment.");
    }
}
public sealed class JwtService(JwtSettings settings, TimeProvider clock)
{
    public AuthResponse Issue(UserDto user)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, user.Id), new("userId", user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        claims.AddRange(user.Roles.Select(role => new Claim("role", role)));
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, now, now.AddMinutes(15),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", 900, user);
    }
}
