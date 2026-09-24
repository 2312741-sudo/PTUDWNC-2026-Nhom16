using System.Security.Cryptography;
using System.Text;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CulinaryBlog.Infrastructure;

public sealed class IdentityService(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, AuthDbContext db, JwtService jwt, IWelcomeEmailQueue welcome) : IIdentityService
{
    private static (string rawToken, string tokenHash) GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        var rawToken = Convert.ToHexString(bytes).ToLowerInvariant();
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
        return (rawToken, tokenHash);
    }

    private static string HashToken(string rawToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
    }

    public async Task<AuthResponse> RegisterAsync(RegisterCommand command, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var resolvedName = command.ResolvedName;
        var resolvedUserName = command.ResolvedUserName;
        var user = new ApplicationUser
        {
            Email = command.Email.Trim(),
            UserName = resolvedUserName.Trim(),
            DisplayName = new DisplayName(resolvedName).Value,
            CreatedAt = DateTimeOffset.UtcNow
        };
        string rawRefreshToken;
        try
        {
            var result = await users.CreateAsync(user, command.Password);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(x => x.Code.StartsWith("Duplicate", StringComparison.Ordinal)))
                    throw new AppException(409, "auth.email_exists", "Email đã được sử dụng.");
                throw new AppException(400, "auth.invalid_registration", "Thông tin đăng ký không hợp lệ.");
            }
            var roleResult = await users.AddToRoleAsync(user, Roles.Author);
            if (!roleResult.Succeeded) throw new InvalidOperationException("Unable to assign Author role.");

            string tokenHash;
            (rawRefreshToken, tokenHash) = GenerateRefreshToken();
            var refreshToken = RefreshToken.Issue(user.Id, tokenHash, DateTime.UtcNow.AddDays(7), DateTime.UtcNow, null);
            db.RefreshTokens.Add(refreshToken);
            await db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
            await welcome.EnqueueAsync(new WelcomeEmail(user.Email!, user.DisplayName), ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new AppException(409, "auth.email_exists", "Email đã được sử dụng.");
        }
        return jwt.Issue(await ToDto(user), rawRefreshToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginCommand command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(command.Email.Trim());
        if (user is null) throw new AppException(401, "auth.invalid_credentials", "Email hoặc mật khẩu không đúng.");
        var check = await signIn.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: true);
        if (check.IsLockedOut) throw new AppException(423, "auth.locked", "Email hoặc mật khẩu không đúng.");
        if (!check.Succeeded)
            throw new AppException(401, "auth.invalid_credentials", "Email hoặc mật khẩu không đúng.");
        if (!user.IsActive) throw new AppException(403, "auth.inactive", "Tài khoản không khả dụng.");

        var (rawRefreshToken, tokenHash) = GenerateRefreshToken();
        var refreshToken = RefreshToken.Issue(user.Id, tokenHash, DateTime.UtcNow.AddDays(7), DateTime.UtcNow, null);
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(ct);

        return jwt.Issue(await ToDto(user), rawRefreshToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string rawRefreshToken, string? ipAddress, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            throw new AppException(401, "auth.invalid_refresh_token", "Refresh token không hợp lệ.");

        var tokenHash = HashToken(rawRefreshToken.Trim());
        // Rotation phải nguyên tử: khóa hàng để hai request đồng thời không cùng cấp token mới (D05).
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var token = await db.RefreshTokens
            .FromSqlRaw("""SELECT * FROM "RefreshTokens" WHERE "TokenHash" = {0} FOR UPDATE""", tokenHash)
            .FirstOrDefaultAsync(ct);

        if (token is null)
            throw new AppException(401, "auth.invalid_refresh_token", "Refresh token không tồn tại.");

        if (token.RevokedAt is not null)
        {
            // Token Reuse Detection: Nếu token đã bị thu hồi và được thay thế bằng token khác mà vẫn cố gửi lên
            if (!string.IsNullOrEmpty(token.ReplacedByTokenHash))
            {
                // Thu hồi toàn bộ token của user này (Family revocation)
                var activeTokens = await db.RefreshTokens
                    .Where(t => t.UserId == token.UserId && t.RevokedAt == null)
                    .ToListAsync(ct);
                foreach (var t in activeTokens)
                {
                    t.Revoke(DateTime.UtcNow, "compromised-reuse-detected");
                }
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            throw new AppException(401, "auth.token_reuse_detected", "Phiên đăng nhập không hợp lệ hoặc đã bị thu hồi.");
        }

        if (DateTime.UtcNow >= token.ExpiresAt)
            throw new AppException(401, "auth.token_expired", "Refresh token đã hết hạn.");

        var user = await users.FindByIdAsync(token.UserId);
        if (user is null || !user.IsActive)
            throw new AppException(401, "auth.user_inactive", "Tài khoản không tồn tại hoặc đã bị khóa.");

        // Token Rotation: Thu hồi token cũ và sinh token mới
        var (newRawToken, newTokenHash) = GenerateRefreshToken();
        token.Revoke(DateTime.UtcNow, newTokenHash);

        var newRefreshToken = RefreshToken.Issue(token.UserId, newTokenHash, DateTime.UtcNow.AddDays(7), DateTime.UtcNow, ipAddress);
        db.RefreshTokens.Add(newRefreshToken);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return jwt.Issue(await ToDto(user), newRawToken);
    }

    public async Task LogoutAsync(string? userId, string? rawRefreshToken, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            var tokenHash = HashToken(rawRefreshToken.Trim());
            var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
            if (token is not null && token.RevokedAt is null)
            {
                token.Revoke(DateTime.UtcNow);
                await db.SaveChangesAsync(ct);
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var activeTokens = await db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ToListAsync(ct);
            foreach (var t in activeTokens)
            {
                t.Revoke(DateTime.UtcNow);
            }
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<UserDto> UpdateAsync(string id, UpdateProfileCommand command, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id) ?? throw new AppException(404, "auth.user_not_found", "Không tìm thấy tài khoản.");
        if (!user.IsActive) throw new AppException(403, "auth.inactive", "Tài khoản không khả dụng.");
        user.DisplayName = new DisplayName(command.ResolvedName).Value;
        user.AvatarUrl = command.AvatarUrl;
        user.Bio = command.Bio;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) throw new InvalidOperationException("Unable to update profile.");
        return await ToDto(user);
    }

    public async Task<UserDto> GetAsync(string id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(id) ?? throw new AppException(404, "auth.user_not_found", "Không tìm thấy tài khoản.");
        if (!user.IsActive) throw new AppException(403, "auth.inactive", "Tài khoản không khả dụng.");
        return await ToDto(user);
    }

    public async Task<AuthResponse> LoginWithGoogleAsync(GoogleUserPayload payload, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!payload.EmailVerified)
            throw new AppException(401, "auth.google_email_unverified", "Email Google chưa được xác minh.");

        var email = payload.Email.Trim();
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            var rawName = string.IsNullOrWhiteSpace(payload.Name) ? email.Split('@')[0] : payload.Name.Trim();
            var displayName = rawName.Length > 100 ? rawName[..100] : rawName;
            user = new ApplicationUser
            {
                Email = email,
                UserName = email,
                DisplayName = displayName,
                AvatarUrl = payload.Picture,
                EmailConfirmed = true,
                IsActive = true
            };
            var createResult = await users.CreateAsync(user);
            if (!createResult.Succeeded)
                throw new AppException(400, "auth.invalid_registration", "Không thể tạo tài khoản từ Google.");

            await users.AddToRoleAsync(user, Roles.Author);
            await users.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));
        }
        else
        {
            if (!user.IsActive)
                throw new AppException(403, "auth.account_disabled", "Tài khoản của bạn đã bị vô hiệu hóa.");

            var logins = await users.GetLoginsAsync(user);
            if (!logins.Any(l => l.LoginProvider == "Google" && l.ProviderKey == payload.Subject))
            {
                await users.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));
            }
        }

        var (rawRefreshToken, tokenHash) = GenerateRefreshToken();
        var refreshToken = RefreshToken.Issue(user.Id, tokenHash, DateTime.UtcNow.AddDays(7), DateTime.UtcNow, null);
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(ct);

        return jwt.Issue(await ToDto(user), rawRefreshToken);
    }

    private async Task<UserDto> ToDto(ApplicationUser user) => new(
        user.Id,
        user.Email!,
        user.DisplayName,
        user.UserName ?? user.Email!,
        (await users.GetRolesAsync(user)).ToArray(),
        user.AvatarUrl,
        user.Bio,
        user.EmailConfirmed,
        user.CreatedAt,
        user.DisplayName);
}
