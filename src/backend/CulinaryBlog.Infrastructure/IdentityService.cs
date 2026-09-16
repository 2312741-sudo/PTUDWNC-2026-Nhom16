using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CulinaryBlog.Infrastructure;

public sealed class IdentityService(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, AuthDbContext db, JwtService jwt, IWelcomeEmailQueue welcome) : IIdentityService
{
    public async Task<AuthResponse> RegisterAsync(RegisterCommand command, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var user = new ApplicationUser { Email = command.Email.Trim(), UserName = command.Email.Trim(), DisplayName = new DisplayName(command.DisplayName).Value };
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
            await transaction.CommitAsync(ct);
            await welcome.EnqueueAsync(new WelcomeEmail(user.Email!, user.DisplayName), ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new AppException(409, "auth.email_exists", "Email đã được sử dụng.");
        }
        return jwt.Issue(await ToDto(user));
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
        return jwt.Issue(await ToDto(user));
    }
    public async Task<UserDto> UpdateAsync(string id, UpdateProfileCommand command, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id) ?? throw new AppException(404, "auth.user_not_found", "Không tìm thấy tài khoản.");
        if (!user.IsActive) throw new AppException(403, "auth.inactive", "Tài khoản không khả dụng.");
        user.DisplayName = new DisplayName(command.DisplayName).Value;
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

        return jwt.Issue(await ToDto(user));
    }
    private async Task<UserDto> ToDto(ApplicationUser user) => new(user.Id, user.Email!, user.DisplayName, (await users.GetRolesAsync(user)).ToArray(), user.AvatarUrl, user.Bio);
}
