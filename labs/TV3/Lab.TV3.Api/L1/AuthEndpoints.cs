using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace Lab.TV3.Api.L1;

public sealed class LabUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public string? PasswordHash { get; set; }
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "Author";
    public string? GoogleSub { get; set; }
    public bool EmailConfirmed { get; set; }
}

public sealed class RefreshRow
{
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record GoogleRequest(string IdToken);
public sealed record UserView(Guid Id, string Email, string DisplayName, string Role, bool HasPassword, bool GoogleLinked);
public sealed record AuthResult(string AccessToken, int ExpiresIn, string RefreshToken, UserView User);

/// <summary>LAB L1 — register/login/hash/logout (K08) + Google callback/verify/link (K09).</summary>
public static partial class AuthEndpoints
{
    private const string InvalidCredentials = "Email hoặc mật khẩu không đúng"; // thông báo chung, không lộ email tồn tại
    private static readonly LabUser Dummy = new();

    private const string InsertUser = """
        INSERT INTO lab_users (id, email, normalized_email, password_hash, display_name, role, google_sub, email_confirmed)
        VALUES (@Id, @Email, @NormalizedEmail, @PasswordHash, @DisplayName, @Role, @GoogleSub, @EmailConfirmed)
        """;

    public static UserView ToView(this LabUser u) =>
        new(u.Id, u.Email, u.DisplayName, u.Role, u.PasswordHash is not null, u.GoogleSub is not null);

    public static void MapL1Auth(this WebApplication app)
    {
        var g = app.MapGroup("/lab/l1");
        g.MapPost("/register", Register);
        g.MapPost("/login", Login);
        g.MapPost("/refresh", Refresh);
        g.MapPost("/logout", Logout).RequireAuthorization();
        g.MapGet("/me", Me).RequireAuthorization();
        g.MapPost("/google", GoogleSignIn);
        g.MapGet("/google-demo", GoogleDemo);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRx();

    private static string Norm(string? email) => (email ?? "").Trim().ToUpperInvariant();

    private static string? PasswordError(string? p) =>
        p is null || p.Length < 8 || !p.Any(char.IsUpper) || !p.Any(char.IsLower) || !p.Any(char.IsDigit)
            ? "Mật khẩu tối thiểu 8 ký tự, gồm chữ hoa, chữ thường và chữ số"
            : null;

    private static async Task<IResult> Register(RegisterRequest r, LabDb db, IPasswordHasher<LabUser> hasher,
        TokenService tokens, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Email) || !EmailRx().IsMatch(r.Email.Trim()))
            return Http.Err(400, "VALIDATION", "Email không hợp lệ");
        if (PasswordError(r.Password) is { } pe) return Http.Err(400, "VALIDATION", pe);
        if (string.IsNullOrWhiteSpace(r.DisplayName) || r.DisplayName.Length > 100)
            return Http.Err(400, "VALIDATION", "Tên hiển thị 1–100 ký tự");

        var u = new LabUser
        {
            Id = Guid.NewGuid(), Email = r.Email.Trim(), NormalizedEmail = Norm(r.Email), DisplayName = r.DisplayName.Trim(),
        };
        u.PasswordHash = hasher.HashPassword(u, r.Password); // Identity V3: PBKDF2-HMAC-SHA512, salt ngẫu nhiên

        await using var c = await db.OpenAsync(ct);
        try { await c.ExecuteAsync(InsertUser, u); }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Http.Err(409, "EMAIL_TAKEN", "Email đã được sử dụng");
        }
        return Results.Created("/lab/l1/me", new { data = await tokens.IssueAsync(u, ct) });
    }

    private static async Task<IResult> Login(LoginRequest r, LabDb db, IPasswordHasher<LabUser> hasher,
        TokenService tokens, CancellationToken ct)
    {
        await using var c = await db.OpenAsync(ct);
        var u = await c.QuerySingleOrDefaultAsync<LabUser>(
            "SELECT * FROM lab_users WHERE normalized_email = @n", new { n = Norm(r.Email) });
        if (u?.PasswordHash is null)
        {
            hasher.HashPassword(Dummy, r.Password ?? ""); // cân bằng thời gian phản hồi, chống dò email
            return Http.Err(401, "INVALID_CREDENTIALS", InvalidCredentials);
        }
        var result = hasher.VerifyHashedPassword(u, u.PasswordHash, r.Password ?? "");
        if (result == PasswordVerificationResult.Failed)
            return Http.Err(401, "INVALID_CREDENTIALS", InvalidCredentials);
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            await c.ExecuteAsync("UPDATE lab_users SET password_hash = @h WHERE id = @id",
                new { h = hasher.HashPassword(u, r.Password!), id = u.Id });
        return Results.Ok(new { data = await tokens.IssueAsync(u, ct) });
    }

    private static async Task<IResult> Refresh(RefreshRequest r, LabDb db, TokenService tokens, CancellationToken ct)
    {
        var hash = TokenService.Hash(r.RefreshToken ?? "");
        await using var c = await db.OpenAsync(ct);
        var row = await c.QuerySingleOrDefaultAsync<RefreshRow>(
            "SELECT user_id, expires_at, revoked_at FROM lab_refresh_tokens WHERE token_hash = @hash", new { hash });
        var invalid = Http.Err(401, "REFRESH_INVALID", "Refresh token không hợp lệ, hết hạn hoặc đã bị thu hồi");
        if (row is null || row.RevokedAt is not null || row.ExpiresAt <= DateTime.UtcNow) return invalid;

        // Rotation: thu hồi token cũ có điều kiện -> 2 request đồng thời chỉ 1 cái thắng
        var n = await c.ExecuteAsync(
            "UPDATE lab_refresh_tokens SET revoked_at = now() WHERE token_hash = @hash AND revoked_at IS NULL", new { hash });
        if (n == 0) return invalid;

        var u = await c.QuerySingleAsync<LabUser>("SELECT * FROM lab_users WHERE id = @id", new { id = row.UserId });
        return Results.Ok(new { data = await tokens.IssueAsync(u, ct) });
    }

    private static async Task<IResult> Logout(RefreshRequest r, ClaimsPrincipal user, LabDb db, CancellationToken ct)
    {
        await using var c = await db.OpenAsync(ct);
        // Chỉ thu hồi token thuộc chính user đang đăng nhập; idempotent -> luôn 204
        await c.ExecuteAsync(
            "UPDATE lab_refresh_tokens SET revoked_at = now() WHERE token_hash = @hash AND user_id = @uid AND revoked_at IS NULL",
            new { hash = TokenService.Hash(r.RefreshToken ?? ""), uid = UserId(user) });
        return Results.NoContent();
    }

    private static async Task<IResult> Me(ClaimsPrincipal user, LabDb db, CancellationToken ct)
    {
        await using var c = await db.OpenAsync(ct);
        var u = await c.QuerySingleOrDefaultAsync<LabUser>("SELECT * FROM lab_users WHERE id = @id", new { id = UserId(user) });
        return u is null ? Http.Err(404, "USER_NOT_FOUND", "Không tìm thấy người dùng") : Results.Ok(new { data = u.ToView() });
    }

    /// <summary>
    /// Callback Google: nhận ID token (từ Google Identity Services / Auth.js) -> verify -> đăng nhập, liên kết hoặc tạo mới.
    /// Liên kết chỉ khi Google xác nhận email (email_verified) để tránh chiếm tài khoản.
    /// </summary>
    private static async Task<IResult> GoogleSignIn(GoogleRequest r, IGoogleTokenVerifier verifier, LabDb db,
        TokenService tokens, CancellationToken ct)
    {
        GoogleIdentity? g;
        try { g = await verifier.VerifyAsync(r.IdToken ?? "", ct); }
        catch (GoogleNotConfiguredException) { return Http.Err(503, "GOOGLE_NOT_CONFIGURED", "Chưa cấu hình Google:ClientId"); }
        if (g is null) return Http.Err(401, "GOOGLE_TOKEN_INVALID", "Google ID token không hợp lệ");
        if (!g.EmailVerified) return Http.Err(403, "GOOGLE_EMAIL_UNVERIFIED", "Email Google chưa được xác minh");

        await using var c = await db.OpenAsync(ct);
        var bySub = await c.QuerySingleOrDefaultAsync<LabUser>("SELECT * FROM lab_users WHERE google_sub = @sub", new { sub = g.Subject });
        if (bySub is not null)
            return Results.Ok(new { data = new { action = "signed_in", auth = await tokens.IssueAsync(bySub, ct) } });

        var byEmail = await c.QuerySingleOrDefaultAsync<LabUser>(
            "SELECT * FROM lab_users WHERE normalized_email = @n", new { n = Norm(g.Email) });
        if (byEmail is not null)
        {
            var linked = await c.ExecuteAsync(
                "UPDATE lab_users SET google_sub = @sub, email_confirmed = true WHERE id = @id AND google_sub IS NULL",
                new { sub = g.Subject, id = byEmail.Id });
            if (linked == 0) return Http.Err(409, "GOOGLE_ACCOUNT_CONFLICT", "Email này đã liên kết với tài khoản Google khác");
            byEmail.GoogleSub = g.Subject;
            return Results.Ok(new { data = new { action = "linked", auth = await tokens.IssueAsync(byEmail, ct) } });
        }

        var u = new LabUser
        {
            Id = Guid.NewGuid(), Email = g.Email, NormalizedEmail = Norm(g.Email),
            DisplayName = string.IsNullOrWhiteSpace(g.Name) ? g.Email : g.Name!,
            GoogleSub = g.Subject, EmailConfirmed = true, PasswordHash = null,
        };
        await c.ExecuteAsync(InsertUser, u);
        return Results.Created("/lab/l1/me", new { data = new { action = "created", auth = await tokens.IssueAsync(u, ct) } });
    }

    /// <summary>Trang demo đăng nhập Google thật (cần Google:ClientId + origin http://localhost:5090 trong Google Console).</summary>
    private static IResult GoogleDemo(IConfiguration cfg)
    {
        var clientId = WebUtility.HtmlEncode(cfg["Google:ClientId"] ?? "");
        var html = $$"""
            <!doctype html><html lang="vi"><meta charset="utf-8"><title>LAB TV3 — Google</title>
            <body style="font-family:sans-serif;max-width:720px;margin:40px auto">
            <h1>LAB L1 — Đăng nhập Google</h1>
            <p>Client ID: <code>{{(clientId.Length > 0 ? clientId : "CHƯA CẤU HÌNH")}}</code></p>
            <script src="https://accounts.google.com/gsi/client" async></script>
            <div id="g_id_onload" data-client_id="{{clientId}}" data-callback="onCredential"></div>
            <div class="g_id_signin" data-type="standard"></div>
            <pre id="out" style="background:#f4f4f4;padding:12px;white-space:pre-wrap"></pre>
            <script>
            async function onCredential(r) {
              const res = await fetch('/lab/l1/google', { method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ idToken: r.credential }) });
              document.getElementById('out').textContent = res.status + '\n' + JSON.stringify(await res.json(), null, 2);
            }
            </script></body></html>
            """;
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static Guid UserId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue("sub")!);
}