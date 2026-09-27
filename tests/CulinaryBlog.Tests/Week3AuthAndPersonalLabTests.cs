using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace CulinaryBlog.Tests;


public sealed class Week3AuthAndPersonalLabTests : IClassFixture<ApiFactory>
{

    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public Week3AuthAndPersonalLabTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.EnsureMigrated();
    }

    private static RegisterCommand NewUser() =>
        new($"tv1-w3-{Guid.NewGuid():N}@example.test", "Secure-Password99!", "Nguyễn Thanh Tâm TV1");

    [Fact]
    public async Task Register_and_login_return_valid_refresh_token()
    {
        var command = NewUser();
        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", command);
        Assert.Equal(HttpStatusCode.Created, regResponse.StatusCode);

        var apiRes = await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(apiRes?.Data);
        var auth = apiRes.Data;

        Assert.NotNull(auth.AccessToken);
        Assert.NotNull(auth.RefreshToken);
        Assert.Equal(128, auth.RefreshToken.Length); // 64 bytes hex = 128 hex chars (512-bit)
        Assert.True(auth.ExpiresIn > 0);
    }

    [Fact]
    public async Task Refresh_token_rotates_and_issues_new_token_pair()
    {
        // 1. Đăng ký tài khoản
        var command = NewUser();
        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", command);
        var auth = (await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;
        var initialRefreshToken = auth.RefreshToken!;

        // 2. Gọi POST /api/v1/auth/refresh với Refresh Token ban đầu
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenCommand(initialRefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshedAuth = (await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;
        Assert.NotNull(refreshedAuth.AccessToken);
        Assert.NotNull(refreshedAuth.RefreshToken);

        // Token rotation: Token mới phải khác token cũ
        Assert.NotEqual(initialRefreshToken, refreshedAuth.RefreshToken);

        // Access token mới dùng để gọi /api/v1/auth/me thành công
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshedAuth.AccessToken);
        var meResponse = await _client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_token_reuse_triggers_family_revocation()
    {
        // 1. Đăng ký tài khoản
        var command = NewUser();
        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", command);
        var auth = (await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;
        var token1 = auth.RefreshToken!;

        // 2. Rotate token lần 1: token1 -> token2
        var rotate1 = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenCommand(token1));
        Assert.Equal(HttpStatusCode.OK, rotate1.StatusCode);
        var token2 = (await rotate1.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data.RefreshToken!;

        // 3. Tấn công Replay / Token Reuse: Cố tình dùng lại token1 đã bị thu hồi!
        var reuseAttempt = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenCommand(token1));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseAttempt.StatusCode);

        // 4. Family revocation: token2 hợp pháp trước đó nay cũng PHẢI bị vô hiệu hóa!
        var token2Attempt = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenCommand(token2));
        Assert.Equal(HttpStatusCode.Unauthorized, token2Attempt.StatusCode);
    }

    [Fact]
    public async Task Refresh_token_invalid_or_nonexistent_returns_unauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenCommand("invalid-nonexistent-token-hex-12345678"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_with_refresh_token_revokes_token()
    {
        var command = NewUser();
        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", command);
        var auth = (await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;

        // Logout kèm RefreshToken
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var logoutResponse = await _client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutCommand(auth.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        // Sau khi logout, refresh token đó không còn dùng được nữa
        var tryRefresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenCommand(auth.RefreshToken!));
        Assert.Equal(HttpStatusCode.Unauthorized, tryRefresh.StatusCode);
    }

    [Fact]
    public void Recipe_json_ld_builder_generates_schema_org_compliant_json()
    {
        var ingredients = new[] { "500g Xương bò", "300g Thịt nạm bò", "500g Bánh phở", "Gia vị: hồi, quế, thảo quả" };
        var steps = new[]
        {
            ("Hầm xương", "Hầm xương bò cùng các loại thảo mộc trong 4-6 tiếng để lấy nước dùng trong ngọt."),
            ("Trần bánh phở", "Trần bánh phở tươi qua nước sôi và xếp vào bát."),
            ("Hoàn thiện", "Thái thịt nạm mỏng, chan nước dùng sôi và rắc hành hoa lên trên.")
        };

        var model = RecipeJsonLdBuilder.Build(
            title: "Phở Bò Gia Truyền",
            description: "Công thức nấu phở bò chuẩn vị truyền thống Hà Nội với nước dùng thanh ngọt.",
            primaryImageUrl: "https://example.com/images/pho-bo.jpg",
            authorName: "Nguyễn Thanh Tâm",
            publishedAt: new DateTimeOffset(2026, 9, 20, 8, 30, 0, TimeSpan.Zero),
            prepTimeMinutes: 30,
            cookTimeMinutes: 240,
            servings: 4,
            categoryName: "Món Nước",
            ingredients: ingredients,
            steps: steps,
            nutrition: (Calories: 450, Protein: 28, Carbs: 55, Fat: 12, Fiber: 3, Sodium: 980)
        );

        Assert.Equal("https://schema.org", model.Context);
        Assert.Equal("Recipe", model.Type);
        Assert.Equal("Phở Bò Gia Truyền", model.Name);
        Assert.Equal("Person", model.Author.Type);
        Assert.Equal("Nguyễn Thanh Tâm", model.Author.Name);
        Assert.Equal("PT30M", model.PrepTime);
        Assert.Equal("PT240M", model.CookTime);
        Assert.Equal("PT270M", model.TotalTime);
        Assert.Equal("4 khẩu phần", model.RecipeYield);
        Assert.Equal(4, model.RecipeIngredient.Length);
        Assert.Equal(3, model.RecipeInstructions.Length);
        Assert.NotNull(model.Nutrition);
        Assert.Equal("450 calories", model.Nutrition.Calories);

        var json = RecipeJsonLdBuilder.ToJsonString(model);
        Assert.Contains("\"@context\": \"https://schema.org\"", json);
        Assert.Contains("\"@type\": \"Recipe\"", json);
        Assert.DoesNotContain("aggregateRating", json); // SRS: Không giả mạo rating khi chưa có tính năng!
    }

    [Fact]
    public async Task Cache_service_cache_aside_and_invalidation_and_fallback_resilience()
    {
        var cache = new RecipeCacheService(NullLogger<RecipeCacheService>.Instance);
        var factoryCallCount = 0;

        Task<string> FetchFromDbAsync()
        {
            factoryCallCount++;
            return Task.FromResult($"Recipe-Content-Version-{factoryCallCount}");
        }

        // 1. Cache MISS: Lần đầu tiên gọi factory
        var data1 = await cache.GetOrSetAsync("recipe:pho-bo", FetchFromDbAsync, TimeSpan.FromMinutes(10));
        Assert.Equal("Recipe-Content-Version-1", data1);
        Assert.Equal(1, factoryCallCount);

        // 2. Cache HIT: Lần thứ 2 lấy từ cache, factory không được gọi lại
        var data2 = await cache.GetOrSetAsync("recipe:pho-bo", FetchFromDbAsync, TimeSpan.FromMinutes(10));
        Assert.Equal("Recipe-Content-Version-1", data2);
        Assert.Equal(1, factoryCallCount);

        // 3. Cache Invalidation: Khi cập nhật món ăn, xóa cache
        await cache.InvalidateAsync("recipe:pho-bo");

        // 4. Cache MISS sau invalidation: Factory được gọi lại lấy dữ liệu mới
        var data3 = await cache.GetOrSetAsync("recipe:pho-bo", FetchFromDbAsync, TimeSpan.FromMinutes(10));
        Assert.Equal("Recipe-Content-Version-2", data3);
        Assert.Equal(2, factoryCallCount);

        // 5. Fallback Resilience: Giả lập Cache Server gặp sự cố (Redis down)
        cache.SimulateServerDown(true);
        var dataFallback = await cache.GetOrSetAsync("recipe:pho-bo", FetchFromDbAsync, TimeSpan.FromMinutes(10));
        Assert.Equal("Recipe-Content-Version-3", dataFallback);
        Assert.Equal(3, factoryCallCount); // Vẫn hoạt động trơn tru mà không làm crash app!
    }
    // ===== Bổ sung TV3 (C5 / NFR-SEC-002 / D05) =====

    [Fact]
    public async Task Concurrent_refresh_issues_exactly_one_valid_token()
    {
        // Hai request refresh cùng một token, chạy song song.
        // Rotation phải đảm bảo chỉ một nhánh token hợp lệ được cấp;
        // nếu cả hai cùng thành công thì một refresh token đã sinh ra hai phiên — lỗ hổng bảo mật.
        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", NewUser());
        var auth = (await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;
        var refreshToken = auth.RefreshToken!;

        var results = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(_ =>
                _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenCommand(refreshToken))));

        Assert.Single(results, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, r => r.StatusCode != HttpStatusCode.OK);
    }

    [Fact]
    public async Task Expired_refresh_token_is_rejected()
    {
        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", NewUser());
        var auth = (await regResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data;
        var refreshToken = auth.RefreshToken!;

        // Đẩy hạn về quá khứ để mô phỏng token hết hạn (7 ngày là quá dài để chờ trong test).
        var tokenHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(refreshToken))).ToLowerInvariant();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                """UPDATE "RefreshTokens" SET "ExpiresAt" = {0} WHERE "TokenHash" = {1}""",
                DateTime.UtcNow.AddMinutes(-1), tokenHash);
        }

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshTokenCommand(refreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
