using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CulinaryBlog.Infrastructure;

public sealed class IdentityService(UserManager<ApplicationUser> users, AuthDbContext db, JwtService jwt) : IIdentityService
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
        if (user is null || !await users.CheckPasswordAsync(user, command.Password))
            throw new AppException(401, "auth.invalid_credentials", "Email hoặc mật khẩu không đúng.");
        if (!user.IsActive) throw new AppException(403, "auth.inactive", "Tài khoản không khả dụng.");
        return jwt.Issue(await ToDto(user));
    }
    public async Task<UserDto> GetAsync(string id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(id) ?? throw new AppException(404, "auth.user_not_found", "Không tìm thấy tài khoản.");
        if (!user.IsActive) throw new AppException(403, "auth.inactive", "Tài khoản không khả dụng.");
        return await ToDto(user);
    }
    private async Task<UserDto> ToDto(ApplicationUser user) => new(user.Id, user.Email!, user.DisplayName, (await users.GetRolesAsync(user)).ToArray());
}
