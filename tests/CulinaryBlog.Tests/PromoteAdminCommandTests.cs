using System.Net.Sockets;
using CulinaryBlog.Domain;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// B6 (TV4, PA-2) — lệnh CLI <c>--promote-admin</c> nâng tài khoản đã tồn tại lên role Admin.
/// PA-A: idempotent, chỉ Development, KHÔNG sửa DbSeeder / không sinh mật khẩu.
/// Phần logic thuần chạy luôn; phần DB bỏ qua nếu PostgreSQL chưa có.
/// </summary>
public sealed class PromoteAdminCommandTests
{
    // ---------- Parse argv ----------

    [Fact]
    public void Parses_switch_with_separate_value()
        => Assert.Equal("admin@local.test", PromoteAdminCommand.ExtractEmail(["--promote-admin", "admin@local.test"]));

    [Fact]
    public void Parses_switch_with_equals_form()
        => Assert.Equal("admin@local.test", PromoteAdminCommand.ExtractEmail(["--promote-admin=admin@local.test"]));

    [Fact]
    public void Switch_is_case_insensitive()
        => Assert.Equal("admin@local.test", PromoteAdminCommand.ExtractEmail(["--PROMOTE-ADMIN", "admin@local.test"]));

    [Fact]
    public void Trims_surrounding_whitespace()
        => Assert.Equal("admin@local.test", PromoteAdminCommand.ExtractEmail(["--promote-admin", "  admin@local.test "]));

    [Fact]
    public void Returns_null_when_email_absent()
    {
        var cases = new[]
        {
            System.Array.Empty<string>(),                    // không có tham số
            ["--migrate"],                                   // không có switch
            ["--promote-admin"],                             // switch ở cuối -> thiếu giá trị
            ["--promote-admin", "--seed"],                   // giá trị lại là switch khác
        };

        foreach (var args in cases)
            Assert.Null(PromoteAdminCommand.ExtractEmail(args));
    }

    [Fact]
    public void Missing_switch_returns_null()
        => Assert.Null(PromoteAdminCommand.ExtractEmail(["--migrate", "--seed"]));

    // ---------- Chốt môi trường (bảo mật) ----------

    [Theory]
    [InlineData("Testing")]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("development-2")]   // chỉ khớp CHÍNH XÁC "Development"
    [InlineData("")]
    public void Refuses_outside_development(string environment)
    {
        var result = PromoteAdminCommand.Decide("admin@local.test", environment);

        Assert.Equal(PromoteAdminOutcome.EnvironmentNotAllowed, result.Outcome);
        Assert.True(result.IsUsageError);
        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("development")]
    [InlineData("DEVELOPMENT")]
    public void Allows_development_case_insensitively(string environment)
        => Assert.Equal(PromoteAdminOutcome.Allowed,
            PromoteAdminCommand.Decide("admin@local.test", environment).Outcome);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Requires_email(string? email)
    {
        var result = PromoteAdminCommand.Decide(email, "Development");

        Assert.Equal(PromoteAdminOutcome.MissingEmail, result.Outcome);
        Assert.True(result.IsUsageError);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@local.test")]
    [InlineData("admin@")]
    public void Rejects_malformed_email(string email)
    {
        var result = PromoteAdminCommand.Decide(email, "Development");

        Assert.Equal(PromoteAdminOutcome.InvalidEmail, result.Outcome);
        Assert.True(result.IsUsageError);
    }

    [Fact]
    public void Email_is_checked_before_touching_db_but_environment_guard_wins()
    {
        // Môi trường sai phải bị chặn kể cả khi email sai -> không lộ chi tiết validate.
        var result = PromoteAdminCommand.Decide("khong-hop-le", "Production");
        Assert.Equal(PromoteAdminOutcome.EnvironmentNotAllowed, result.Outcome);
    }

    [Fact]
    public void Normalize_email_trims_and_uppercases_like_identity()
    {
        Assert.Equal("ADMIN@LOCAL.TEST", PromoteAdminCommand.NormalizeEmail("  Admin@Local.Test "));
    }

    [Theory]
    [InlineData("a@b")]                  // EmailAddressAttribute chấp nhận (dạng rút gọn hợp lệ)
    [InlineData("admin @local.test")]    // chấp nhận, nhưng không khớp user nào -> UserNotFound
    public void Lenient_emails_pass_validation_and_fail_later_as_user_not_found(string email)
        => Assert.Equal(PromoteAdminOutcome.Allowed, PromoteAdminCommand.Decide(email, "Development").Outcome);

    // ---------- Phần DB ----------

    [Fact]
    public async Task Promote_is_idempotent_and_never_duplicates_the_join()
    {
        if (!await PostgresIsReachableAsync()) return;

        using var factory = new ApiFactoryWithMinio();
        factory.EnsureMigrated();

        var email = $"tv4-b6-{Guid.NewGuid():N}@example.test";
        Guid userId;
        string roleId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email,
                NormalizedUserName = PromoteAdminCommand.NormalizeEmail(email),
                Email = email,
                NormalizedEmail = PromoteAdminCommand.NormalizeEmail(email),
                EmailConfirmed = true,
                DisplayName = "TV4 B6"
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = Guid.Parse(user.Id);

            // Role Admin do HasData/seed tạo — tra theo Name vì Id có thể là GUID hoặc "role-admin".
            roleId = (await db.Roles.FirstAsync(r => r.Name == Roles.Admin)).Id;

            var first = await PromoteAdminCommand.ExecuteAsync(db, email);
            Assert.Equal(PromoteAdminOutcome.Promoted, first.Outcome);
            Assert.True(first.Succeeded);

            // Idempotent: chạy lại không tạo bản ghi thứ hai.
            var second = await PromoteAdminCommand.ExecuteAsync(db, email);
            Assert.Equal(PromoteAdminOutcome.AlreadyAdmin, second.Outcome);
            Assert.True(second.Succeeded);

            var third = await PromoteAdminCommand.ExecuteAsync(db, email.ToUpperInvariant());
            Assert.Equal(PromoteAdminOutcome.AlreadyAdmin, third.Outcome);

            var joins = await db.UserRoles.CountAsync(ur => ur.UserId == userId.ToString() && ur.RoleId == roleId);
            Assert.Equal(1, joins);
        }

        // Dọn dẹp: bỏ role vừa cấp để không làm bẩn DB test dùng chung.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            db.UserRoles.RemoveRange(db.UserRoles.Where(ur => ur.UserId == userId.ToString()));
            db.Users.RemoveRange(db.Users.Where(u => u.Id == userId.ToString()));
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Unknown_email_fails_without_creating_anything()
    {
        if (!await PostgresIsReachableAsync()) return;

        using var factory = new ApiFactoryWithMinio();
        factory.EnsureMigrated();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var email = $"khong-ton-tai-{Guid.NewGuid():N}@example.test";

        var result = await PromoteAdminCommand.ExecuteAsync(db, email);

        Assert.Equal(PromoteAdminOutcome.UserNotFound, result.Outcome);
        Assert.False(result.Succeeded);
        Assert.False(result.IsUsageError);   // lỗi nghiệp vụ -> exit 1, không phải 2
        Assert.False(await db.Users.AnyAsync(u => u.NormalizedEmail == PromoteAdminCommand.NormalizeEmail(email)));
    }

    private static async Task<bool> PostgresIsReachableAsync()
    {
        try
        {
            using var tcp = new TcpClient();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await tcp.ConnectAsync("127.0.0.1", 5432, timeout.Token);
            return tcp.Connected;
        }
        catch
        {
            return false;
        }
    }
}
