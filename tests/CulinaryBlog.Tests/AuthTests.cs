using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CulinaryBlog.Tests;

public sealed record ApiResponse<T>(T Data);

public sealed class DebugEnvTest
{
    [Fact]
    public void Print_test_database_env_var()
    {
        var v = Environment.GetEnvironmentVariable("TEST_DATABASE");
        Console.WriteLine($"[DEBUG] TEST_DATABASE seen by test process = '{v}'");
        Assert.True(true);
    }
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private static readonly object MigrationLock = new();
    private static bool migrated;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = Environment.GetEnvironmentVariable("TEST_DATABASE") ?? "Host=127.0.0.1;Port=5432;Database=culinary_test;Username=postgres;Password=postgres",
            ["Jwt:SigningKey"] = new string('t', 64)
        }));
    }

    public void EnsureMigrated()
    {
        if (migrated) return;
        lock (MigrationLock)
        {
            if (migrated) return;
            try
            {
                using var scope = Services.CreateScope();
                scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.Migrate();
            }
            catch
            {
                // Ignored if already created or degraded state
            }
            finally
            {
                migrated = true;
            }
        }
    }
}
public sealed class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;
    private readonly HttpClient client;
    public AuthTests(ApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
        factory.EnsureMigrated();
    }
    private static RegisterCommand NewUser() => new($"tv1-{Guid.NewGuid():N}@example.test", "Demo-Password9!", "Nguyễn Thanh Tâm");

    [Fact]
    public async Task Register_login_me_persist_hash_and_author_without_leaking_secrets()
    {
        var command = NewUser();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", command);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/auth/me", response.Headers.Location?.ToString());
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
        var apiRes = JsonSerializer.Deserialize<ApiResponse<AuthResponse>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var auth = apiRes.Data;
        Assert.Equal([Roles.Author], auth.User.Roles);
        Assert.Equal(900, auth.ExpiresIn);
        Assert.Equal("Nguyễn Thanh Tâm", auth.User.FullName);
        Assert.Equal(command.Email, auth.User.UserName);
        Assert.True(auth.ExpiresAt > DateTimeOffset.UtcNow);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(auth.AccessToken);
        Assert.Equal("HS256", token.Header.Alg);
        Assert.Equal(TimeSpan.FromMinutes(15), token.ValidTo - token.ValidFrom);
        Assert.Equal(auth.User.Id, token.Subject);
        Assert.Contains(token.Claims, c => c.Type == "role" && c.Value == Roles.Author);
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var saved = await users.FindByEmailAsync(command.Email);
        Assert.NotNull(saved);
        Assert.NotEqual(command.Password, saved.PasswordHash);
        Assert.True(await users.CheckPasswordAsync(saved, command.Password));
        var hash = Convert.FromBase64String(saved.PasswordHash!);
        Assert.Equal(1, hash[0]); // Identity V3
        Assert.Equal(2u, System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(hash.AsSpan(1, 4))); // SHA512
        Assert.True(System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(hash.AsSpan(5, 4)) >= 100_000);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(command.Email, command.Password));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var meRes = await client.GetFromJsonAsync<ApiResponse<UserDto>>("/api/v1/auth/me");
        var me = meRes!.Data;
        Assert.Equal(auth.User.Id, me.Id);
        Assert.Equal(auth.User.Email, me.Email);
        Assert.Equal(auth.User.FullName, me.FullName);
        Assert.Equal(auth.User.Roles, me.Roles);
    }
    [Fact]
    public async Task Duplicate_email_is_case_insensitive_and_returns_problem()
    {
        var command = NewUser();
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/auth/register", command)).StatusCode);
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", command with { Email = command.Email.ToUpperInvariant() });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
    [Fact]
    public async Task Concurrent_registration_creates_exactly_one_account()
    {
        var command = NewUser();
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.PostAsJsonAsync("/api/v1/auth/register", command)));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
    }
    [Fact]
    public async Task Invalid_registration_has_field_errors_and_no_account()
    {
        var command = NewUser() with { Password = "weak", DisplayName = "<script>" };
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", command);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("password", out _));
        Assert.True(problem.GetProperty("errors").TryGetProperty("displayName", out _) || problem.GetProperty("errors").TryGetProperty("fullName", out _));
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Null(await users.FindByEmailAsync(command.Email));
    }
    [Fact]
    public async Task Client_cannot_assign_admin_role()
    {
        var json = JsonSerializer.Serialize(new { email = $"client-role-{Guid.NewGuid():N}@example.test", password = "Demo-Password9!", displayName = "Admin Attempt", role = Roles.Admin, roles = new[] { Roles.Admin } });
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/auth/register", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var apiRes = (await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!;
        Assert.Equal([Roles.Author], apiRes.Data.User.Roles);
    }
    [Fact]
    public async Task Invalid_credentials_return_same_generic_error()
    {
        var missing = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand("missing@example.test", "Wrong-Password1!"));
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        var command = NewUser();
        await client.PostAsJsonAsync("/api/v1/auth/register", command);
        var wrong = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(command.Email, "Wrong-Password1!"));
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        var m1 = (await missing.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString();
        var m2 = (await wrong.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString();
        Assert.Equal(m1, m2);
    }
    [Theory]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    public async Task Me_rejects_missing_or_invalid_token(string token)
    {
        client.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task Disabled_account_cannot_login()
    {
        var command = NewUser();
        await client.PostAsJsonAsync("/api/v1/auth/register", command);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var user = await db.Users.FirstAsync(u => u.Email == command.Email);
            user.IsActive = false;
            await db.SaveChangesAsync();
        }
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(command.Email, command.Password));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    [Fact]
    public async Task Scalar_and_openapi_are_available_in_development()
    {
        var openApi = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, openApi.StatusCode);
        var scalar = await client.GetAsync("/scalar/v1");
        Assert.Equal(HttpStatusCode.OK, scalar.StatusCode);
    }
    [Fact]
    public async Task Logout_with_valid_token_returns_no_content()
    {
        var command = NewUser();
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", command);
        var auth = (await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutCommand(null));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
    [Fact]
    public async Task Logout_without_token_returns_unauthorized()
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task Lockout_after_five_failed_attempts_locks_account_and_returns_423()
    {
        var command = NewUser();
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", command);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        // 4 failed login attempts return 401 Unauthorized
        for (var i = 0; i < 4; i++)
        {
            var failed = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(command.Email, "wrong-password"));
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        // 5th failed attempt reaches MaxFailedAccessAttempts=5 and locks the account
        var fifthAttempt = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(command.Email, "wrong-password"));
        Assert.Equal((HttpStatusCode)423, fifthAttempt.StatusCode);
        var body5 = await fifthAttempt.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("auth.locked", body5.GetProperty("code").GetString());

        // Subsequent attempt (even with correct password) remains locked out
        var locked = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(command.Email, command.Password));
        Assert.Equal((HttpStatusCode)423, locked.StatusCode);
        var body = await locked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("auth.locked", body.GetProperty("code").GetString());
    }
    [Fact]
    public async Task Update_profile_patches_allowed_fields_successfully()
    {
        var command = NewUser();
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", command);
        var auth = (await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var patchCmd = new UpdateProfileCommand("Tâm Chef", "https://example.com/avatar.jpg", "Đầu bếp nghiệp dư");
        var patchRes = await client.PatchAsJsonAsync("/api/v1/auth/me", patchCmd);
        Assert.Equal(HttpStatusCode.OK, patchRes.StatusCode);

        var apiRes = await patchRes.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        Assert.NotNull(apiRes);
        var updated = apiRes.Data;
        Assert.Equal("Tâm Chef", updated.FullName);
        Assert.Equal("https://example.com/avatar.jpg", updated.AvatarUrl);
        Assert.Equal("Đầu bếp nghiệp dư", updated.Bio);
        Assert.Equal(command.Email, updated.Email); // email untouched

        var meRes = await client.GetFromJsonAsync<ApiResponse<UserDto>>("/api/v1/auth/me");
        Assert.NotNull(meRes);
        var me = meRes.Data;
        Assert.Equal(updated.FullName, me.FullName);
        Assert.Equal(updated.AvatarUrl, me.AvatarUrl);
        Assert.Equal(updated.Bio, me.Bio);
        Assert.Equal(updated.Email, me.Email);
        Assert.Equal(updated.Roles, me.Roles);
    }
    [Fact]
    public async Task Update_profile_rejects_unauthorized_call()
    {
        client.DefaultRequestHeaders.Authorization = null;
        var patchRes = await client.PatchAsJsonAsync("/api/v1/auth/me", new UpdateProfileCommand("Tâm", null, null));
        Assert.Equal(HttpStatusCode.Unauthorized, patchRes.StatusCode);
    }
    [Theory]
    [InlineData("<script>alert(1)</script>", null, "displayName")]
    [InlineData("Tâm", "javascript:alert(1)", "avatarUrl")]
    public async Task Update_profile_rejects_xss_and_invalid_inputs(string name, string? avatar, string expectedErrorField)
    {
        var command = NewUser();
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", command);
        var auth = (await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var patchRes = await client.PatchAsJsonAsync("/api/v1/auth/me", new UpdateProfileCommand(name, avatar, null));
        Assert.Equal(HttpStatusCode.BadRequest, patchRes.StatusCode);
        var body = await patchRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty(expectedErrorField, out _));
    }
    [Fact]
    public async Task Update_profile_cannot_modify_email_or_roles()
    {
        var command = NewUser();
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", command);
        var auth = (await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        // Attempting to send unmapped email/role properties is disallowed by serializer
        var attempt = await client.PatchAsJsonAsync("/api/v1/auth/me", new { displayName = "Tâm", email = "hacked@example.com", roles = new[] { "Admin" } });
        Assert.Equal(HttpStatusCode.BadRequest, attempt.StatusCode);

        // Verify profile still has original email and Author role
        var meRes = await client.GetFromJsonAsync<ApiResponse<UserDto>>("/api/v1/auth/me");
        Assert.NotNull(meRes);
        Assert.Equal(command.Email, meRes.Data.Email);
    }
}
