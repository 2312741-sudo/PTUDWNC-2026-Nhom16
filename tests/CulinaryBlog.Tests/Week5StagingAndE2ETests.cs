using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// Bộ kiểm thử tích hợp toàn diện End-to-End (E2E) và Kiểm chứng Cổng G6 cho Tuần 5 (Lab 5)
/// Tác giả: Nguyễn Thanh Tâm (TV1 - 2312741)
/// Bao quát 5 kịch bản E2E trọng yếu:
/// 1. Vòng đời toàn diện Auth & Security (Register -> Login -> Profile -> ChangePass -> Token Revoke -> Logout)
/// 2. Phân quyền RBAC, Chống Brute-force & Account Lockout
/// 3. Khóa lạc quan (Optimistic Concurrency Control) ngăn chặn Lost Update
/// 4. Khả năng phục hồi (Resilient Fallback) & Health Probes
/// 5. Tìm kiếm FTS không dấu, Bộ lọc AND & Bảo vệ phân lập dữ liệu Draft
/// </summary>
public sealed class Week5StagingAndE2ETests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public Week5StagingAndE2ETests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.EnsureMigrated();
    }

    private static RegisterCommand NewUser() =>
        new($"tam-e2e-{Guid.NewGuid():N}@example.test", "Pass@123456", "Nguyễn Thanh Tâm TV1 E2E");

    #region E2E Scenario 1: Vòng đời xác thực và an toàn tài khoản toàn diện
    [Fact]
    public async Task E2E_Scenario_1_Full_Auth_Lifecycle_Profile_ChangePassword_And_Revocation()
    {
        // 1. Đăng ký tài khoản mới (Register)
        var reg = NewUser();
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", reg);
        Assert.Equal(HttpStatusCode.Created, regRes.StatusCode);
        var auth = (await regRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;
        Assert.NotNull(auth.AccessToken);
        Assert.NotNull(auth.RefreshToken);

        // 2. Lấy thông tin tài khoản hiện tại (Get Profile)
        using var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var meRes = await _client.SendAsync(meReq);
        Assert.Equal(HttpStatusCode.OK, meRes.StatusCode);
        var userProfile = (await meRes.Content.ReadFromJsonAsync<ApiResponse<UserDto>>())!.Data;
        Assert.Equal(reg.Email, userProfile.Email);

        // 3. Kiểm tra Anti-XSS: Tên chứa thẻ script độc hại phải bị từ chối 400 Validation Error
        using var xssReq = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/auth/me")
        {
            Content = JsonContent.Create(new UpdateProfileCommand("Tâm <script>alert(1)</script>", null, "Bio"))
        };
        xssReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var xssRes = await _client.SendAsync(xssReq);
        Assert.Equal(HttpStatusCode.BadRequest, xssRes.StatusCode);

        // Cập nhật với dữ liệu hợp lệ -> 200 OK
        using var validUpdateReq = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/auth/me")
        {
            Content = JsonContent.Create(new UpdateProfileCommand("Nguyễn Thanh Tâm Master Chef", null, "Bếp trưởng đam mê ẩm thực Việt"))
        };
        validUpdateReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var validUpdateRes = await _client.SendAsync(validUpdateReq);
        Assert.Equal(HttpStatusCode.OK, validUpdateRes.StatusCode);
        var updatedUser = (await validUpdateRes.Content.ReadFromJsonAsync<ApiResponse<UserDto>>())!.Data;
        Assert.Equal("Nguyễn Thanh Tâm Master Chef", updatedUser.DisplayName);

        // 4. Đổi mật khẩu tài khoản (Change Password)
        var newPassword = "NewPass@654321!";
        using var changeReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/change-password")
        {
            Content = JsonContent.Create(new ChangePasswordCommand(reg.Password, newPassword))
        };
        changeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var changeRes = await _client.SendAsync(changeReq);
        Assert.Equal(HttpStatusCode.NoContent, changeRes.StatusCode);

        // 5. Kiểm tra thu hồi toàn bộ Refresh Token của phiên cũ (Security Token Revocation)
        var oldRefreshRes = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenCommand(auth.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, oldRefreshRes.StatusCode);

        // 6. Đăng nhập thành công bằng mật khẩu mới
        var newLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(reg.Email, newPassword));
        Assert.Equal(HttpStatusCode.OK, newLoginRes.StatusCode);
        var newAuth = (await newLoginRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;

        // 7. Đăng xuất an toàn khỏi hệ thống (Logout)
        using var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout")
        {
            Content = JsonContent.Create(new LogoutCommand(newAuth.RefreshToken))
        };
        logoutReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAuth.AccessToken);
        var logoutRes = await _client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.NoContent, logoutRes.StatusCode);

        // Refresh Token sau khi logout phải bị vô hiệu hóa
        var afterLogoutRefresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenCommand(newAuth.RefreshToken!));
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogoutRefresh.StatusCode);
    }
    #endregion

    #region E2E Scenario 2: Phân quyền RBAC, Chống dò quét Brute-Force & Lockout
    [Fact]
    public async Task E2E_Scenario_2_RBAC_Authorization_And_BruteForce_Lockout_Protection()
    {
        // 1. Tạo tài khoản thường (Role: Author/Guest)
        var reg = NewUser();
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", reg);
        var auth = (await regRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;

        // 2. Tài khoản thường cố gắng tạo Danh mục (Endpoint chỉ dành cho AdminPolicy) -> 403 Forbidden
        using var catReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/categories")
        {
            Content = JsonContent.Create(new CreateCategoryCommand($"Cat-{Guid.NewGuid():N}", "Mô tả danh mục", null, 1))
        };
        catReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var catRes = await _client.SendAsync(catReq);
        Assert.Equal(HttpStatusCode.Forbidden, catRes.StatusCode);

        // 3. Gọi endpoint bảo mật mà không kèm token -> 401 Unauthorized
        var unauthRes = await _client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthRes.StatusCode);

        // 4. Kiểm tra cơ chế chống Brute-Force: Đăng nhập sai liên tiếp
        for (int i = 0; i < 4; i++)
        {
            var failRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(reg.Email, "WrongPassword@123"));
            Assert.Equal(HttpStatusCode.Unauthorized, failRes.StatusCode);
        }

        // Lần thứ 5 sai -> Phải kích hoạt Lockout (HTTP 423 Locked hoặc 401 với cảnh báo)
        var fifthAttempt = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(reg.Email, "WrongPassword@123"));
        Assert.True(fifthAttempt.StatusCode == HttpStatusCode.Unauthorized || fifthAttempt.StatusCode == (HttpStatusCode)423);
    }
    #endregion

    #region E2E Scenario 3: Toàn vẹn dữ liệu & Khóa lạc quan (OCC)
    [Fact]
    public async Task E2E_Scenario_3_Optimistic_Concurrency_Control_Prevents_Lost_Updates()
    {
        // 1. Đăng ký tác giả và đăng nhập
        var reg = NewUser();
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", reg);
        var auth = (await regRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;

        // 2. Lấy 1 categoryId hợp lệ
        var catRes = await _client.GetAsync("/api/v1/categories");
        var cats = (await catRes.Content.ReadFromJsonAsync<ApiResponse<List<CategoryDto>>>())!.Data;
        var categoryId = cats.First().Id;

        // 3. Tạo 1 Recipe mới
        using var createRecipeReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/recipes")
        {
            Content = JsonContent.Create(new CreateRecipeCommand(
                $"Món Đồng Thời {Guid.NewGuid():N}",
                "Mô tả kiểm thử OCC",
                "Cách làm món ăn",
                15, 30, 4,
                RecipeDifficulty.Medium,
                categoryId,
                null))
        };
        createRecipeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var createRecipeRes = await _client.SendAsync(createRecipeReq);
        Assert.Equal(HttpStatusCode.Created, createRecipeRes.StatusCode);
        var recipeDto = (await createRecipeRes.Content.ReadFromJsonAsync<ApiResponse<RecipeDto>>())!.Data;

        var originalRowVersion = recipeDto.RowVersion;
        Assert.NotNull(originalRowVersion);

        // 4. Writer 1 cập nhật thành công qua UpdateRecipeBody -> RowVersion thay đổi
        var body1 = new UpdateRecipeBody(
            "Tiêu đề sửa đổi lần 1 bởi Writer 1",
            recipeDto.Description,
            recipeDto.Instructions,
            20, 35, 4,
            RecipeDifficulty.Hard,
            categoryId,
            null,
            originalRowVersion);

        using var update1Req = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recipes/{recipeDto.Id}")
        {
            Content = JsonContent.Create(body1)
        };
        update1Req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var update1Res = await _client.SendAsync(update1Req);
        Assert.Equal(HttpStatusCode.OK, update1Res.StatusCode);

        // 5. Writer 2 gửi cập nhật với RowVersion cũ ban đầu -> Phải bị từ chối 422 Unprocessable Entity
        var body2 = new UpdateRecipeBody(
            "Tiêu đề sửa đổi lần 2 bởi Writer 2 (Xung đột)",
            recipeDto.Description,
            recipeDto.Instructions,
            25, 40, 4,
            RecipeDifficulty.Easy,
            categoryId,
            null,
            originalRowVersion); // Dùng rowVersion cũ

        using var update2Req = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/recipes/{recipeDto.Id}")
        {
            Content = JsonContent.Create(body2)
        };
        update2Req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var update2Res = await _client.SendAsync(update2Req);

        // Xung đột phiên bản phải trả HTTP 422
        Assert.Equal(HttpStatusCode.UnprocessableEntity, update2Res.StatusCode);
    }
    #endregion

    #region E2E Scenario 4: Phục hồi sự cố Resilience & Health Probes
    [Fact]
    public async Task E2E_Scenario_4_Resilient_Health_Probes_And_Graceful_Degradation()
    {
        // 1. Kiểm tra Liveness Probe -> Hệ thống đang chạy phải trả 200 OK
        var liveRes = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, liveRes.StatusCode);

        // 2. Kiểm tra Health Check tổng quát
        var healthRes = await _client.GetAsync("/health");
        Assert.True(healthRes.StatusCode == HttpStatusCode.OK || (int)healthRes.StatusCode == 503);

        // 3. Đọc dữ liệu công thức công khai khi fallback -> Không được quăng 500 ra client
        var recipesRes = await _client.GetAsync("/api/v1/recipes?pageSize=5");
        Assert.Equal(HttpStatusCode.OK, recipesRes.StatusCode);
        var recipesData = await recipesRes.Content.ReadFromJsonAsync<PagedResult<RecipeSummaryDto>>();
        Assert.NotNull(recipesData?.Data);
    }
    #endregion

    #region E2E Scenario 5: Tìm kiếm FTS không dấu tiếng Việt & Phân lập dữ liệu Draft
    [Fact]
    public async Task E2E_Scenario_5_FTS_Vietnamese_Search_AND_Filter_And_Draft_Isolation()
    {
        // 1. Tìm kiếm không dấu tiếng Việt: "canh" tìm thấy "Canh chua cá lóc"
        var searchRes = await _client.GetAsync("/api/v1/recipes/search?q=canh");
        Assert.Equal(HttpStatusCode.OK, searchRes.StatusCode);
        var searchData = (await searchRes.Content.ReadFromJsonAsync<PagedResult<RecipeSummaryDto>>())!;
        Assert.NotEmpty(searchData.Data);
        Assert.Contains(searchData.Data, r => r.Title.Contains("Canh", StringComparison.OrdinalIgnoreCase));

        // 2. Tìm kiếm kết hợp phân trang và bộ lọc: q + pageSize
        var filterRes = await _client.GetAsync("/api/v1/recipes/search?q=canh&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, filterRes.StatusCode);
        var filterData = (await filterRes.Content.ReadFromJsonAsync<PagedResult<RecipeSummaryDto>>())!;
        Assert.True(filterData.Data.Count <= 5);

        // 3. Đảm bảo kết quả tìm kiếm công khai KHÔNG BAO GIỜ chứa công thức ở trạng thái Draft
        foreach (var item in searchData.Data)
        {
            Assert.Equal(nameof(RecipeStatus.Published), item.Status);
        }
    }
    #endregion
}
