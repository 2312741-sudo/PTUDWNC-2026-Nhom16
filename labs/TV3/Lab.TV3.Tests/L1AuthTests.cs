using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dapper;
using Lab.TV3.Api;
using Lab.TV3.Api.L1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Lab.TV3.Tests.LabHttp;

namespace Lab.TV3.Tests;

/// <summary>LAB L1 — K08 register/login/PBKDF2/refresh/logout, K09 Google verify/link.</summary>
[Collection("lab")]
public sealed class L1AuthTests(LabFactory f)
{
    [Fact]
    public async Task Register_hashes_password_with_PBKDF2_and_login_me_work()
    {
        var (client, auth, email) = await AuthorAsync(f);
        Assert.Equal(900, auth.GetProperty("expiresIn").GetInt32());
        Assert.Equal(128, auth.GetProperty("refreshToken").GetString()!.Length); // 64 byte = 512 bit

        await using (var c = await f.Services.GetRequiredService<LabDb>().OpenAsync())
        {
            var hash = await c.QuerySingleAsync<string>("SELECT password_hash FROM lab_users WHERE normalized_email = @n",
                new { n = email.ToUpperInvariant() });
            Assert.NotEqual(Password, hash);
            var bytes = Convert.FromBase64String(hash);
            Assert.Equal(0x01, bytes[0]);                                                   // Identity V3
            Assert.True(BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(5, 4)) >= 100_000); // số vòng PBKDF2
            var stored = await c.QuerySingleAsync<string>("SELECT token_hash FROM lab_refresh_tokens rt JOIN lab_users u ON u.id = rt.user_id WHERE u.normalized_email = @n",
                new { n = email.ToUpperInvariant() });
            Assert.NotEqual(auth.GetProperty("refreshToken").GetString(), stored);          // chỉ lưu hash
        }

        var login = await f.CreateClient().PostAsJsonAsync("/lab/l1/login", new { email = email.ToUpperInvariant(), password = Password });
        await Expect(HttpStatusCode.OK, login);
        var me = await client.GetAsync("/lab/l1/me");
        await Expect(HttpStatusCode.OK, me);
        Assert.Equal(email, (await Data(me)).GetProperty("email").GetString());
    }

    [Fact]
    public async Task Duplicate_email_is_case_insensitive_409()
    {
        var (_, _, email) = await AuthorAsync(f);
        var res = await f.CreateClient().PostAsJsonAsync("/lab/l1/register",
            new { email = email.ToUpperInvariant(), password = Password, displayName = "Trùng" });
        await Expect(HttpStatusCode.Conflict, res);
        Assert.Equal("EMAIL_TAKEN", await Code(res));
    }

    [Theory]
    [InlineData("short1A")]
    [InlineData("alllowercase1")]
    [InlineData("NoDigitsHere")]
    public async Task Weak_password_rejected_400(string password)
    {
        var res = await f.CreateClient().PostAsJsonAsync("/lab/l1/register", new { email = NewEmail(), password, displayName = "Yếu" });
        await Expect(HttpStatusCode.BadRequest, res);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_give_same_generic_401()
    {
        var (_, _, email) = await AuthorAsync(f);
        var client = f.CreateClient();
        var wrong = await client.PostAsJsonAsync("/lab/l1/login", new { email, password = "Sai-Pass9x" });
        var unknown = await client.PostAsJsonAsync("/lab/l1/login", new { email = NewEmail(), password = Password });
        await Expect(HttpStatusCode.Unauthorized, wrong);
        await Expect(HttpStatusCode.Unauthorized, unknown);
        // Cùng mã + cùng thông báo -> không dò được email nào đã đăng ký
        Assert.Equal(await Code(wrong), await Code(unknown));
        Assert.Equal(await Title(wrong), await Title(unknown));
    }

    [Fact]
    public async Task Refresh_rotates_and_old_token_is_rejected()
    {
        var (_, auth, _) = await AuthorAsync(f);
        var client = f.CreateClient();
        var old = auth.GetProperty("refreshToken").GetString();
        var first = await client.PostAsJsonAsync("/lab/l1/refresh", new { refreshToken = old });
        await Expect(HttpStatusCode.OK, first);
        Assert.NotEqual(old, (await Data(first)).GetProperty("refreshToken").GetString());
        await Expect(HttpStatusCode.Unauthorized, await client.PostAsJsonAsync("/lab/l1/refresh", new { refreshToken = old }));
    }

    [Fact]
    public async Task Logout_revokes_refresh_token()
    {
        var (client, auth, _) = await AuthorAsync(f);
        var rt = auth.GetProperty("refreshToken").GetString();
        await Expect(HttpStatusCode.NoContent, await client.PostAsJsonAsync("/lab/l1/logout", new { refreshToken = rt }));
        await Expect(HttpStatusCode.NoContent, await client.PostAsJsonAsync("/lab/l1/logout", new { refreshToken = rt })); // idempotent
        await Expect(HttpStatusCode.Unauthorized, await f.CreateClient().PostAsJsonAsync("/lab/l1/refresh", new { refreshToken = rt }));
    }

    [Fact]
    public async Task Protected_endpoints_require_token()
    {
        var client = f.CreateClient();
        await Expect(HttpStatusCode.Unauthorized, await client.GetAsync("/lab/l1/me"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "khong.phai.jwt");
        await Expect(HttpStatusCode.Unauthorized, await client.GetAsync("/lab/l1/me"));
    }

    // ---------------------------------------------------------------- K09 Google

    [Fact]
    public async Task Google_creates_new_user_then_signs_in_same_user()
    {
        var sub = Guid.NewGuid().ToString("N");
        var email = NewEmail("g");
        var client = f.CreateClient();
        var created = await client.PostAsJsonAsync("/lab/l1/google", new { idToken = $"ok|{sub}|{email}|true" });
        await Expect(HttpStatusCode.Created, created);
        var d1 = await Data(created);
        Assert.Equal("created", d1.GetProperty("action").GetString());
        Assert.False(d1.GetProperty("auth").GetProperty("user").GetProperty("hasPassword").GetBoolean());

        var again = await client.PostAsJsonAsync("/lab/l1/google", new { idToken = $"ok|{sub}|{email}|true" });
        await Expect(HttpStatusCode.OK, again);
        var d2 = await Data(again);
        Assert.Equal("signed_in", d2.GetProperty("action").GetString());
        Assert.Equal(d1.GetProperty("auth").GetProperty("user").GetProperty("id").GetGuid(),
                     d2.GetProperty("auth").GetProperty("user").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Google_links_existing_password_account_by_verified_email()
    {
        var (_, auth, email) = await AuthorAsync(f);
        var userId = auth.GetProperty("user").GetProperty("id").GetGuid();

        var res = await f.CreateClient().PostAsJsonAsync("/lab/l1/google", new { idToken = $"ok|{Guid.NewGuid():N}|{email}|true" });
        await Expect(HttpStatusCode.OK, res);
        var d = await Data(res);
        Assert.Equal("linked", d.GetProperty("action").GetString());
        var user = d.GetProperty("auth").GetProperty("user");
        Assert.Equal(userId, user.GetProperty("id").GetGuid());
        Assert.True(user.GetProperty("googleLinked").GetBoolean());

        // Mật khẩu cũ vẫn dùng được sau khi liên kết
        await Expect(HttpStatusCode.OK, await f.CreateClient().PostAsJsonAsync("/lab/l1/login", new { email, password = Password }));
    }

    [Fact]
    public async Task Google_second_account_cannot_take_over_linked_email_409()
    {
        var (_, _, email) = await AuthorAsync(f);
        var client = f.CreateClient();
        await Expect(HttpStatusCode.OK, await client.PostAsJsonAsync("/lab/l1/google", new { idToken = $"ok|{Guid.NewGuid():N}|{email}|true" }));
        var other = await client.PostAsJsonAsync("/lab/l1/google", new { idToken = $"ok|{Guid.NewGuid():N}|{email}|true" });
        await Expect(HttpStatusCode.Conflict, other);
        Assert.Equal("GOOGLE_ACCOUNT_CONFLICT", await Code(other));
    }

    [Fact]
    public async Task Google_invalid_token_401_and_unverified_email_403()
    {
        var client = f.CreateClient();
        var bad = await client.PostAsJsonAsync("/lab/l1/google", new { idToken = "gia-mao" });
        await Expect(HttpStatusCode.Unauthorized, bad);
        Assert.Equal("GOOGLE_TOKEN_INVALID", await Code(bad));
        var unverified = await client.PostAsJsonAsync("/lab/l1/google", new { idToken = $"ok|{Guid.NewGuid():N}|{NewEmail()}|false" });
        await Expect(HttpStatusCode.Forbidden, unverified);
    }

    [Fact]
    public async Task Real_google_verifier_requires_client_id_and_rejects_malformed_token()
    {
        IConfiguration Cfg(string? id) => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Google:ClientId"] = id }).Build();

        var none = new GoogleTokenVerifier(Cfg(null), NullLogger<GoogleTokenVerifier>.Instance);
        await Assert.ThrowsAsync<GoogleNotConfiguredException>(() => none.VerifyAsync("x", default));

        var real = new GoogleTokenVerifier(Cfg("lab-test-client"), NullLogger<GoogleTokenVerifier>.Instance);
        Assert.Null(await real.VerifyAsync("khong-phai-jwt", default)); // bị từ chối trước khi gọi mạng
    }

    private static async Task<string?> Title(HttpResponseMessage res)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("title").GetString();
    }
}