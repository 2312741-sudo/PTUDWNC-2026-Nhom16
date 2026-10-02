using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using Xunit;

namespace CulinaryBlog.Tests;

public sealed class Week4AuthAndSecurityLabTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public Week4AuthAndSecurityLabTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
        factory.EnsureMigrated();
    }

    private static RegisterCommand NewUser() =>
        new($"tv1-w4-{Guid.NewGuid():N}@example.test", "Old-SecurePass123!", "Nguyễn Thanh Tâm TV1 W4");

    [Fact]
    public async Task Change_password_with_valid_credentials_succeeds_and_allows_login_with_new_password()
    {
        // 1. Đăng ký tài khoản với mật khẩu ban đầu
        var reg = NewUser();
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", reg);
        Assert.Equal(HttpStatusCode.Created, regRes.StatusCode);
        var auth = (await regRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;

        // 2. Đổi mật khẩu sang mật khẩu mới
        var changeCmd = new ChangePasswordCommand("Old-SecurePass123!", "New-SuperSecretPass456!");
        using var changeReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/change-password")
        {
            Content = JsonContent.Create(changeCmd)
        };
        changeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var changeRes = await _client.SendAsync(changeReq);
        Assert.Equal(HttpStatusCode.NoContent, changeRes.StatusCode);

        // 3. Đăng nhập bằng mật khẩu cũ phải bị từ chối 401
        var oldLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(reg.Email, "Old-SecurePass123!"));
        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginRes.StatusCode);

        // 4. Đăng nhập bằng mật khẩu mới phải thành công 200
        var newLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(reg.Email, "New-SuperSecretPass456!"));
        Assert.Equal(HttpStatusCode.OK, newLoginRes.StatusCode);

        var newAuth = (await newLoginRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;
        Assert.NotNull(newAuth.AccessToken);
        Assert.NotNull(newAuth.RefreshToken);
    }

    [Fact]
    public async Task Change_password_with_wrong_current_password_fails_with_400()
    {
        var reg = NewUser();
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", reg);
        var auth = (await regRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;

        // Gửi mật khẩu hiện tại sai
        var changeCmd = new ChangePasswordCommand("WrongCurrentPassword999!", "New-SuperSecretPass456!");
        using var changeReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/change-password")
        {
            Content = JsonContent.Create(changeCmd)
        };
        changeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var changeRes = await _client.SendAsync(changeReq);
        Assert.Equal(HttpStatusCode.BadRequest, changeRes.StatusCode);

        var problem = await changeRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("auth.wrong_current_password", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Change_password_with_same_or_weak_password_fails_validation()
    {
        var reg = NewUser();
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", reg);
        var auth = (await regRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;

        // Trùng mật khẩu cũ
        var sameCmd = new ChangePasswordCommand("Old-SecurePass123!", "Old-SecurePass123!");
        using var sameReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/change-password")
        {
            Content = JsonContent.Create(sameCmd)
        };
        sameReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var sameRes = await _client.SendAsync(sameReq);
        Assert.Equal(HttpStatusCode.BadRequest, sameRes.StatusCode);

        // Mật khẩu mới quá yếu (không có chữ hoa, số, ký tự đặc biệt)
        var weakCmd = new ChangePasswordCommand("Old-SecurePass123!", "weak");
        using var weakReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/change-password")
        {
            Content = JsonContent.Create(weakCmd)
        };
        weakReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var weakRes = await _client.SendAsync(weakReq);
        Assert.Equal(HttpStatusCode.BadRequest, weakRes.StatusCode);
    }

    [Fact]
    public async Task Change_password_requires_authorization_returns_401()
    {
        var changeCmd = new ChangePasswordCommand("Old-SecurePass123!", "New-SuperSecretPass456!");
        var res = await _client.PostAsJsonAsync("/api/v1/auth/change-password", changeCmd);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Change_password_revokes_old_refresh_tokens()
    {
        // 1. Đăng ký tài khoản
        var reg = NewUser();
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", reg);
        var auth = (await regRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;
        var oldRefreshToken = auth.RefreshToken!;

        // 2. Đổi mật khẩu
        var changeCmd = new ChangePasswordCommand("Old-SecurePass123!", "New-SuperSecretPass456!");
        using var changeReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/change-password")
        {
            Content = JsonContent.Create(changeCmd)
        };
        changeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var changeRes = await _client.SendAsync(changeReq);
        Assert.Equal(HttpStatusCode.NoContent, changeRes.StatusCode);

        // 3. Refresh token cũ trước khi đổi mật khẩu phải bị thu hồi ngay lập tức
        var refreshRes = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenCommand(oldRefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshRes.StatusCode);
    }
}
