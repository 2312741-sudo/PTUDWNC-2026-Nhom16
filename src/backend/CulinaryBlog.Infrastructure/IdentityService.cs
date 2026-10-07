using System.Net;
using System.Security.Cryptography;
using System.Text;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

namespace CulinaryBlog.Infrastructure;

public sealed class IdentityService(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    AuthDbContext db,
    JwtService jwt,
    IWelcomeEmailQueue welcome,
    IEmailService emailService,
    IMemoryCache cache) : IIdentityService
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

        var token = (await db.RefreshTokens
            .FromSqlRaw("""SELECT * FROM "RefreshTokens" WHERE "TokenHash" = {0} FOR UPDATE""", tokenHash)
            .ToListAsync(ct)).FirstOrDefault();

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

    public async Task RequestChangePasswordCodeAsync(string userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(userId) ?? throw new AppException(404, "auth.user_not_found", "Không tìm thấy tài khoản.");
        if (!user.IsActive) throw new AppException(403, "auth.inactive", "Tài khoản không khả dụng.");

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var cacheKey = $"change_pwd_otp_{userId}";
        cache.Set(cacheKey, code, TimeSpan.FromMinutes(10));

        var html = $@"
<div style=""font-family: Arial, sans-serif; max-width: 500px; margin: 0 auto; padding: 24px; border: 1px solid #e5e7eb; border-radius: 16px; background-color: #ffffff;"">
    <div style=""text-align: center; margin-bottom: 20px;"">
        <h2 style=""color: #059669; margin: 0; font-size: 22px;"">Culinary Blog</h2>
        <p style=""color: #6b7280; font-size: 14px; margin-top: 4px;"">Xác thực yêu cầu đổi mật khẩu</p>
    </div>
    <p style=""font-size: 15px; color: #374151;"">Xin chào <strong>{WebUtility.HtmlEncode(user.DisplayName)}</strong>,</p>
    <p style=""font-size: 14px; color: #4b5563; line-height: 1.5;"">Bạn vừa yêu cầu mã xác nhận để đổi mật khẩu tài khoản Culinary Blog. Mã OTP xác thực của bạn là:</p>
    <div style=""background-color: #ecfdf5; border: 1px solid #a7f3d0; border-radius: 12px; padding: 18px; text-align: center; margin: 24px 0;"">
        <span style=""font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #047857;"">{code}</span>
    </div>
    <p style=""font-size: 13px; color: #6b7280; line-height: 1.5;"">Mã này có hiệu lực trong vòng <strong>10 phút</strong>. Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email hoặc đổi mật khẩu ngay để bảo vệ tài khoản.</p>
    <div style=""border-top: 1px solid #f3f4f6; margin-top: 20px; padding-top: 16px; text-align: center; font-size: 12px; color: #9ca3af;"">
        Culinary Blog - Nền tảng chia sẻ công thức ẩm thực
    </div>
</div>";

        await emailService.SendEmailAsync(user.Email!, "[Culinary Blog] Mã OTP xác nhận đổi mật khẩu", html, ct);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordCommand command, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId) ?? throw new AppException(404, "auth.user_not_found", "Không tìm thấy tài khoản.");
        if (!user.IsActive) throw new AppException(403, "auth.inactive", "Tài khoản không khả dụng.");

        var cacheKey = $"change_pwd_otp_{userId}";
        if (cache.TryGetValue(cacheKey, out string? expectedCode))
        {
            if (string.IsNullOrWhiteSpace(command.Code) || !string.Equals(command.Code.Trim(), expectedCode, StringComparison.Ordinal))
            {
                throw new AppException(400, "auth.invalid_verification_code", "Mã xác thực email không đúng hoặc đã hết hạn.");
            }
            cache.Remove(cacheKey);
        }
        else if (!string.IsNullOrWhiteSpace(command.Code))
        {
            throw new AppException(400, "auth.invalid_verification_code", "Mã xác thực email đã hết hạn hoặc không hợp lệ. Vui lòng bấm nhận mã mới.");
        }

        var hasPassword = await users.HasPasswordAsync(user);
        IdentityResult result;
        if (!hasPassword)
        {
            result = await users.AddPasswordAsync(user, command.NewPassword);
        }
        else
        {
            result = await users.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);
        }

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == "PasswordMismatch"))
                throw new AppException(400, "auth.wrong_current_password", "Mật khẩu hiện tại không chính xác.");

            var msg = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new AppException(400, "auth.invalid_password", msg);
        }

        await users.UpdateSecurityStampAsync(user);
        var activeTokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in activeTokens)
        {
            t.Revoke(DateTime.UtcNow);
        }
        await db.SaveChangesAsync(ct);

        var confirmHtml = $@"
<div style=""font-family: Arial, sans-serif; max-width: 500px; margin: 0 auto; padding: 24px; border: 1px solid #e5e7eb; border-radius: 16px; background-color: #ffffff;"">
    <h2 style=""color: #059669; margin-top: 0; font-size: 20px;"">Đổi mật khẩu thành công</h2>
    <p style=""font-size: 15px; color: #374151;"">Xin chào <strong>{WebUtility.HtmlEncode(user.DisplayName)}</strong>,</p>
    <p style=""font-size: 14px; color: #4b5563; line-height: 1.5;"">Mật khẩu tài khoản Culinary Blog của bạn vừa được cập nhật thành công lúc {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC.</p>
    <p style=""font-size: 13px; color: #6b7280; line-height: 1.5;"">Nếu bạn không thực hiện thay đổi này, hãy liên hệ ngay với chúng tôi để bảo vệ tài khoản của mình.</p>
</div>";
        await emailService.SendEmailAsync(user.Email!, "[Culinary Blog] Mật khẩu đã được thay đổi thành công", confirmHtml, ct);
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(email))
            throw new AppException(400, "auth.invalid_email", "Email không được để trống.");

        var user = await users.FindByEmailAsync(email.Trim());
        if (user is null)
            throw new AppException(404, "auth.user_not_found", "Không tìm thấy tài khoản với email này trong hệ thống.");

        if (!user.IsActive)
            throw new AppException(403, "auth.inactive", "Tài khoản của bạn đã bị vô hiệu hóa.");

        var randomDigits = RandomNumberGenerator.GetInt32(1000, 10000);
        var randomSuffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(2)).ToLowerInvariant();
        var tempPassword = $"Chef@{randomDigits}{randomSuffix}";

        var hasPassword = await users.HasPasswordAsync(user);
        if (hasPassword)
        {
            await users.RemovePasswordAsync(user);
        }
        var addResult = await users.AddPasswordAsync(user, tempPassword);
        if (!addResult.Succeeded)
        {
            var msg = string.Join("; ", addResult.Errors.Select(e => e.Description));
            throw new AppException(400, "auth.invalid_password", msg);
        }

        await users.UpdateSecurityStampAsync(user);
        var activeTokens = await db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in activeTokens)
        {
            t.Revoke(DateTime.UtcNow);
        }
        await db.SaveChangesAsync(ct);

        var html = $@"
<div style=""font-family: Arial, sans-serif; max-width: 500px; margin: 0 auto; padding: 24px; border: 1px solid #e5e7eb; border-radius: 16px; background-color: #ffffff;"">
    <div style=""text-align: center; margin-bottom: 20px;"">
        <h2 style=""color: #059669; margin: 0; font-size: 22px;"">Culinary Blog</h2>
        <p style=""color: #6b7280; font-size: 14px; margin-top: 4px;"">Khôi phục mật khẩu tài khoản</p>
    </div>
    <p style=""font-size: 15px; color: #374151;"">Xin chào <strong>{WebUtility.HtmlEncode(user.DisplayName)}</strong>,</p>
    <p style=""font-size: 14px; color: #4b5563; line-height: 1.5;"">Hệ thống đã nhận được yêu cầu cấp lại mật khẩu cho tài khoản <strong>{WebUtility.HtmlEncode(user.Email)}</strong> của bạn.</p>
    
    <div style=""background-color: #ecfdf5; border: 1px solid #a7f3d0; border-radius: 12px; padding: 18px; text-align: center; margin: 24px 0;"">
        <div style=""font-size: 13px; font-weight: 600; color: #065f46; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 8px;"">Mật khẩu mới của bạn:</div>
        <div style=""font-size: 24px; font-family: monospace; font-weight: bold; color: #047857; letter-spacing: 2px; user-select: all;"">{tempPassword}</div>
    </div>

    <p style=""font-size: 13px; color: #6b7280; line-height: 1.6;"">
        💡 <strong>Lưu ý:</strong> Vui lòng sử dụng mật khẩu mới này để đăng nhập ngay và truy cập vào mục <strong>Hồ sơ & Tài khoản</strong> để đổi sang mật khẩu cá nhân của bạn.
    </p>
    <div style=""border-top: 1px solid #f3f4f6; margin-top: 20px; padding-top: 16px; text-align: center; font-size: 12px; color: #9ca3af;"">
        Culinary Blog - Nền tảng chia sẻ công thức ẩm thực
    </div>
</div>";

        await emailService.SendEmailAsync(user.Email!, "[Culinary Blog] Cấp lại mật khẩu mới cho tài khoản của bạn", html, ct);
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
