using System.ComponentModel.DataAnnotations;
using CulinaryBlog.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>B6 (TV4, PA-2) — kết quả của lệnh CLI <c>--promote-admin</c>.</summary>
public enum PromoteAdminOutcome
{
    Allowed,
    Promoted,
    AlreadyAdmin,
    UserNotFound,
    InvalidEmail,
    MissingEmail,
    EnvironmentNotAllowed,
    RoleMissing
}

/// <summary>B6 (TV4, PA-2) — kết quả + thông điệp tiếng Việt cho CLI.</summary>
public sealed record PromoteAdminResult(PromoteAdminOutcome Outcome, string Message)
{
    /// <summary>True khi lệnh đã đạt mục đích (kể cả khi user vốn đã là Admin).</summary>
    public bool Succeeded => Outcome is PromoteAdminOutcome.Promoted or PromoteAdminOutcome.AlreadyAdmin;

    /// <summary>True khi lệnh không chạy được do cấu hình/sai tham số — nên trả exit code khác 0.</summary>
    public bool IsUsageError => Outcome
        is PromoteAdminOutcome.MissingEmail
        or PromoteAdminOutcome.InvalidEmail
        or PromoteAdminOutcome.EnvironmentNotAllowed;
}

/// <summary>
/// B6 (TV4, PA-2) — nâng một tài khoản ĐÃ TỒN TẠI lên role Admin mà không cần mật khẩu.
/// <para>
/// PA-A: idempotent, chỉ chạy ở Development, KHÔNG đụng DbSeeder (không nhân bản account admin mặc định
/// và không thêm mật khẩu cứng). Role được tra theo <c>Name</c> vì Id do seeder sinh (GUID) khác với
/// HasData <c>role-admin</c>.
/// </para>
/// </summary>
public static class PromoteAdminCommand
{
    public const string SwitchName = "--promote-admin";

    /// <summary>
    /// Chuẩn hoá email giống Identity mặc định (<c>UpperInvariantLookupNormalizer</c>):
    /// trim + ToUpperInvariant. Tự viết lại vì lệnh CLI chỉ có DbContext, không có UserManager.
    /// </summary>
    public static string NormalizeEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return email.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Lấy email từ argv. Chấp nhận cả <c>--promote-admin a@b.c</c> lẫn <c>--promote-admin=a@b.c</c>.
    /// Trả null khi không có switch, thiếu giá trị, hoặc có thêm đối số lạ.
    /// </summary>
    public static string? ExtractEmail(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is null)
                continue;

            if (arg.StartsWith(SwitchName + "=", StringComparison.OrdinalIgnoreCase))
                return arg[(SwitchName.Length + 1)..].Trim();

            if (!arg.Equals(SwitchName, StringComparison.OrdinalIgnoreCase))
                continue;

            // Switch ở cuối argv, hoặc giá trị đi kèm lại là switch khác -> thiếu email.
            if (i == args.Length - 1)
                return null;

            var inline = args[i + 1];
            return inline is null || inline.StartsWith("--", StringComparison.Ordinal)
                ? null
                : inline.Trim();
        }

        return null;
    }

    /// <summary>
    /// Chốt quyết định trước khi chạm DB. Tách riêng để test được chốt bảo mật "chỉ Development"
    /// mà không cần dựng môi trường.
    /// </summary>
    public static PromoteAdminResult Decide(string? email, string environmentName)
    {
        if (string.IsNullOrWhiteSpace(email))
            return new(PromoteAdminOutcome.MissingEmail,
                $"Thieu email. Cach dung: dotnet run --project src/backend/CulinaryBlog.API -- {SwitchName} <email>");

        email = email.Trim();

        // Bảo mật: lệnh đụng quyền ưu tiên — chỉ cho phép ở Development.
        if (!string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
            return new(PromoteAdminOutcome.EnvironmentNotAllowed,
                $"Tu choi chay o moi truong '{environmentName}'. Chi chay o Development.");

        if (!new EmailAddressAttribute().IsValid(email))
            return new(PromoteAdminOutcome.InvalidEmail, $"Email khong hop le: '{email}'.");

        return new(PromoteAdminOutcome.Allowed, string.Empty);
    }

    /// <summary>
    /// Gán role Admin cho user đã tồn tại. Chạy lại nhiều lần vẫn giữ nguyên một bản ghi UserRole.
    /// </summary>
    public static async Task<PromoteAdminResult> ExecuteAsync(
        AuthDbContext db,
        string email,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        var normalized = NormalizeEmail(email);
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.NormalizedEmail == normalized || u.UserName == email, ct).ConfigureAwait(false);

        if (user is null)
            return new(PromoteAdminOutcome.UserNotFound,
                $"Khong tim thay tai khoan '{email}'. Hay dang ky truoc khi lenh.");

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Admin, ct).ConfigureAwait(false);
        if (adminRole is null)
            return new(PromoteAdminOutcome.RoleMissing,
                $"Khong co role '{Roles.Admin}' trong DB. Hay chay migration/seed truoc.");

        var already = await db.UserRoles.AnyAsync(
            ur => ur.UserId == user.Id && ur.RoleId == adminRole.Id, ct).ConfigureAwait(false);

        if (already)
            return new(PromoteAdminOutcome.AlreadyAdmin,
                $"'{user.UserName}' da co role {Roles.Admin}. Khong thay doi gi.");

        await db.UserRoles.AddAsync(
            new IdentityUserRole<string> { UserId = user.Id, RoleId = adminRole.Id }, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new(PromoteAdminOutcome.Promoted,
            $"Da them role {Roles.Admin} cho '{user.UserName}' ({user.Id}).");
    }
}
