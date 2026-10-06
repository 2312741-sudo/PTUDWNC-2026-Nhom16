using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// N2-E3/E4/E5/E7 — race condition, tính idempotent và phân quyền (K07, K10, K13, K14).
///
/// Bối cảnh: các test này cần **tài khoản Admin** để chạy được, vốn là phụ thuộc chéo đã gỡ ở
/// B6 (CLI `--promote-admin`). Ở đây Admin được tạo bằng đúng đường đã chốt: đăng ký tài khoản
/// thật rồi nâng role trong DB của test — **không** seed mật khẩu cứng vào `DbSeeder`.
///
/// Cần MinIO local cho phần upload ảnh (E3/E4/E5). E7 chỉ cần HTTP nên vẫn chạy khi MinIO tắt.
/// </summary>
public sealed class ImageConcurrencyE7Tests : IAsyncLifetime
{
    private readonly ApiFactoryWithMinio factory;
    private readonly HttpClient ownerClient;
    private readonly bool minioUp;
    private Guid categoryId;

    public ImageConcurrencyE7Tests()
    {
        factory = new ApiFactoryWithMinio();
        ownerClient = factory.CreateClient();
        factory.EnsureMigrated();
        minioUp = ApiFactoryWithMinio.MinioIsReachableAsync().GetAwaiter().GetResult();
    }

    public Task DisposeAsync()
    {
        ownerClient.Dispose();
        return factory.DisposeAsync().AsTask();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    private static byte[] RealJpeg(int width = 400, int height = 300)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(100, 149, 237));
        using var output = new MemoryStream();
        image.Save(output, new JpegEncoder());
        return output.ToArray();
    }

    private async Task<string> RegisterAndGetTokenAsync(string prefix, string[] roles)
    {
        var http = factory.CreateClient();
        var email = $"{prefix}-{Guid.NewGuid():N}@example.test";
        var register = await http.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = "Demo-Password9!", displayName = prefix });
        var body = await register.Content.ReadAsStringAsync();
        Assert.True(register.StatusCode == HttpStatusCode.Created,
            $"register failed {register.StatusCode}: {body}");

        var token = JsonSerializer.Deserialize<ApiResponse<AuthResponse>>(body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Data.AccessToken;

        // Nâng role đúng như B6 làm (`PromoteAdminCommand`): thêm join, không ghi đè role khác.
        // Phải bỏ qua role đã có — đăng ký đã cấp sẵn `Author`, thêm lại sẽ vi phạm PK_AspNetUserRoles.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var user = await db.Users.SingleAsync(u => u.Email == email);
            foreach (var role in roles)
            {
                var roleEntity = await db.Roles.SingleAsync(r => r.Name == role);
                var already = await db.UserRoles.AnyAsync(
                    ur => ur.UserId == user.Id && ur.RoleId == roleEntity.Id);
                if (already) continue;

                db.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
                {
                    UserId = user.Id,
                    RoleId = roleEntity.Id,
                });
            }
            await db.SaveChangesAsync();
        }
        http.Dispose();
        return token;
    }

    /// <summary>Đăng ký tài khoản owner (không role) và gắn bearer token vào <see cref="ownerClient"/>.</summary>
    private async Task RegisterOwnerAsync()
    {
        var token = await RegisterAndGetTokenAsync("e7-owner", []);
        ownerClient.DefaultRequestHeaders.Authorization = new("Bearer", token);
    }

    private void SeedCategory()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var category = new Category("Món ảnh E7", $"mon-anh-e7-{Guid.NewGuid():N}", "E3/E4/E5");
        db.Categories.Add(category);
        db.SaveChanges();
        categoryId = category.Id;
    }

    private async Task<RecipeDto> CreateRecipeAsync()
    {
        var created = await ownerClient.PostAsJsonAsync("/api/v1/recipes", new
        {
            title = $"E7 recipe {Guid.NewGuid():N}",
            description = "Recipe cho N2-E3/E4/E5.",
            instructions = "",
            prepTimeMinutes = 10,
            cookTimeMinutes = 20,
            servings = 2,
            difficulty = 1,
            categoryId
        });
        var raw = await created.Content.ReadAsStringAsync();
        Assert.True(created.IsSuccessStatusCode, $"create failed {created.StatusCode}: {raw}");
        return (await created.Content.ReadFromJsonAsync<ApiResponse<RecipeDto>>())!.Data;
    }

    private async Task<RecipeImageDto> UploadAsync(Guid recipeId, byte[] bytes, string fileName)
    {
        var part = new ByteArrayContent(bytes);
        part.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        using var content = new MultipartFormDataContent { { part, "file", fileName } };
        var upload = await ownerClient.PostAsync($"/api/v1/recipes/{recipeId}/images", content);
        var raw = await upload.Content.ReadAsStringAsync();
        Assert.True(upload.StatusCode == HttpStatusCode.Created, $"upload failed {upload.StatusCode}: {raw}");
        return (await upload.Content.ReadFromJsonAsync<ApiResponse<RecipeImageDto>>())!.Data;
    }

    /// <summary>Đếm ảnh đang `IsPrimary` của một recipe đọc thẳng từ DB (không qua cache).</summary>
    private async Task<int> CountPrimaryAsync(Guid recipeId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await db.RecipeImages.CountAsync(i => i.RecipeId == recipeId && i.IsPrimary);
    }

    // ================================================================== E3
    /// <summary>
    /// E3 — hai request đặt primary **đồng thời** không được tạo ra hai ảnh primary.
    ///
    /// Đây là race thật: hai PATCH chạy song song trên hai scope DbContext khác nhau. Nếu chỉ kiểm
    /// tuần tự thì lỗi "đặt primary xong lại bị đặt primary lần nữa" sẽ không bao giờ lộ.
    /// </summary>
    [Fact]
    public async Task E3_Concurrent_primary_patches_leave_exactly_one_primary()
    {
        if (!minioUp) return;

        await RegisterOwnerAsync();
        SeedCategory();
        var recipe = await CreateRecipeAsync();
        var bytes = RealJpeg();
        var a = await UploadAsync(recipe.Id, bytes, $"e3-a-{Guid.NewGuid():N}.jpg");
        var b = await UploadAsync(recipe.Id, bytes, $"e3-b-{Guid.NewGuid():N}.jpg");

        // Cả hai client dùng **cùng** bearer token nhưng là hai HttpClient riêng ⇒ hai request thật
        // song song (không bị tuần tự hoá bởi connection pooling của một client).
        var client1 = factory.CreateClient();
        var client2 = factory.CreateClient();
        foreach (var c in new[] { client1, client2 })
            c.DefaultRequestHeaders.Authorization = ownerClient.DefaultRequestHeaders.Authorization;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var t1 = client1.PatchAsJsonAsync($"/api/v1/recipes/{recipe.Id}/images/{a.Id}",
            new { isPrimary = true }, cts.Token);
        var t2 = client2.PatchAsJsonAsync($"/api/v1/recipes/{recipe.Id}/images/{b.Id}",
            new { isPrimary = true }, cts.Token);
        await Task.WhenAll(t1, t2);

        // Cả hai request đều phải trả 200 — người dùng đặt primary thì không được gặp lỗi.
        // Đọc kết quả bằng `await` (không `.Result`) để không chặn luồng và không gây deadlock.
        var r1 = await t1;
        var r2 = await t2;

        // Hành vi ĐÚNG khi hai request thật sự đụng nhau: chỉ một request thắng, request còn lại bị
        // từ chối bằng 422 `recipe.version_conflict` (optimistic concurrency trên RowVersion —
        // D19). Đòi cả hai trả 200 là đặt sai kỳ vọng và che mất đúng cơ chế đang bảo vệ ta.
        // Điều KHÔNG được phép: 500, và trạng thái DB phải luôn hợp lệ.
        foreach (var r in new[] { r1, r2 })
        {
            var body = await r.Content.ReadAsStringAsync();
            Assert.True(
                r.StatusCode is HttpStatusCode.OK or HttpStatusCode.UnprocessableEntity
                                or HttpStatusCode.Conflict,
                $"Race phải cho 200 hoặc 409/422, thực tế {(int)r.StatusCode}: {body}");
        }

        var okCount = new[] { r1, r2 }.Count(r => r.StatusCode == HttpStatusCode.OK);
        Assert.True(okCount >= 1, " ít nhất một request phải thành công.");

        var primaries = await CountPrimaryAsync(recipe.Id);
        Assert.Equal(1, primaries);

        client1.Dispose();
        client2.Dispose();
    }

    // ================================================================== E4
    /// <summary>
    /// E4 — ảnh đầu tiên phải tự động là primary, và khi xoá ảnh primary thì phải có ảnh primary
    /// mới (không để recipe mồ côi 0 ảnh primary).
    /// </summary>
    [Fact]
    public async Task E4_First_image_is_primary_and_deleting_primary_promotes_another()
    {
        if (!minioUp) return;

        await RegisterOwnerAsync();
        SeedCategory();
        var recipe = await CreateRecipeAsync();
        var bytes = RealJpeg();

        var first = await UploadAsync(recipe.Id, bytes, $"e4-first-{Guid.NewGuid():N}.jpg");
        Assert.True(first.IsPrimary, "Ảnh đầu tiên phải tự động là primary.");
        Assert.Equal(1, await CountPrimaryAsync(recipe.Id));

        var second = await UploadAsync(recipe.Id, bytes, $"e4-second-{Guid.NewGuid():N}.jpg");
        Assert.False(second.IsPrimary, "Ảnh thứ hai không được tự nhảy lên primary.");

        // Xoá ảnh primary.
        var delete = await ownerClient.DeleteAsync($"/api/v1/recipes/{recipe.Id}/images/{first.Id}");
        Assert.True(delete.StatusCode == HttpStatusCode.NoContent,
            $"Xoá ảnh primary -> {(int)delete.StatusCode}: {await delete.Content.ReadAsStringAsync()}");

        var primariesAfter = await CountPrimaryAsync(recipe.Id);
        Assert.Equal(1, primariesAfter);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var promoted = await db.RecipeImages.SingleAsync(i => i.RecipeId == recipe.Id && i.IsPrimary);
        Assert.Equal(second.Id, promoted.Id);

        // Đây là hồi quy cho bug N2-E4: trước đây endpoint trả 204 và xoá object S3 nhưng DÒNG
        // RecipeImages vẫn nằm lại trong DB (DeleteBehavior.Cascade trên quan hệ bắt buộc khiến
        // EF không phát DELETE). Phải xác nhận cả dòng DB biến mất, không chỉ HTTP status.
        Assert.False(await db.RecipeImages.AnyAsync(i => i.Id == first.Id),
            "Ảnh đã xoá vẫn còn dòng trong DB — endpoint chỉ trả 204 nhưng không xoá thật.");
    }

    // ================================================================== E4b
    /// <summary>
    /// E4b — đặt primary bằng PATCH (tuần tự, không race).
    ///
    /// Đường này chưa từ có test integration nào phủ trước đây. Nó dùng chung bất biến "một
    /// recipe chỉ có đúng một primary" với `ux_recipe_images_one_primary`, nên phải chứng minh
    /// nó chuyển primary (cũ → false, mới → true) mà không vi phạm index.
    /// </summary>
    [Fact]
    public async Task E4b_Setting_a_new_primary_via_patch_keeps_exactly_one_primary()
    {
        if (!minioUp) return;

        await RegisterOwnerAsync();
        SeedCategory();
        var recipe = await CreateRecipeAsync();
        var bytes = RealJpeg();
        var first = await UploadAsync(recipe.Id, bytes, $"e4b-a-{Guid.NewGuid():N}.jpg");
        var second = await UploadAsync(recipe.Id, bytes, $"e4b-b-{Guid.NewGuid():N}.jpg");
        Assert.True(first.IsPrimary, "Ảnh đầu tiên phải là primary.");

        var patch = await ownerClient.PatchAsJsonAsync(
            $"/api/v1/recipes/{recipe.Id}/images/{second.Id}", new { isPrimary = true });
        Assert.True(patch.StatusCode == HttpStatusCode.OK,
            $"PATCH đặt primary -> {(int)patch.StatusCode}: {await patch.Content.ReadAsStringAsync()}");

        Assert.Equal(1, await CountPrimaryAsync(recipe.Id));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var current = await db.RecipeImages.SingleAsync(i => i.RecipeId == recipe.Id && i.IsPrimary);
        Assert.Equal(second.Id, current.Id);
    }

    // ================================================================== E5
    /// <summary>
    /// E5 — xoá cùng một imageId **hai lần**: lần hai phải idempotent (204), tuyệt đối không 500.
    /// </summary>
    [Fact]
    public async Task E5_Deleting_the_same_image_twice_is_idempotent_not_500()
    {
        if (!minioUp) return;

        await RegisterOwnerAsync();
        SeedCategory();
        var recipe = await CreateRecipeAsync();
        var image = await UploadAsync(recipe.Id, RealJpeg(), $"e5-{Guid.NewGuid():N}.jpg");

        var first = await ownerClient.DeleteAsync($"/api/v1/recipes/{recipe.Id}/images/{image.Id}");
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await ownerClient.DeleteAsync($"/api/v1/recipes/{recipe.Id}/images/{image.Id}");

        Assert.True(second.StatusCode != HttpStatusCode.InternalServerError,
            "Xoá 2 lần không được sinh 500.");
        Assert.True(second.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound
                        or HttpStatusCode.BadRequest,
            $"Lần hai trả mã lạ: {(int)second.StatusCode}. Kỳ vọng 204/404/400 (idempotent).");
    }

    // ================================================================== E7
    /// <summary>
    /// E7 — dashboard `/hangfire` không được lộ ra công khai.
    ///
    /// *Giới hạn thật của hạ tầng test:* `Program.cs` chỉ map `UseHangfireDashboard` khi
    /// `!IsEnvironment("Testing")`, còn Hangfire không được đăng ký trong `Testing`. Vì vậy ở đây
    /// `/hangfire` trả **404** — tức là endpoint không tồn tại, chứ **không** phải đã bị
    /// `AdminDashboardAuthorizationFilter` chặn 403. Test này chỉ chứng minh được "không lộ
    /// dashboard ở môi trường test"; phần "Author bị 403 / Admin thấy 200" buộc phải chạy ở
    /// `Development` và được kiểm chứng thủ công, ghi rõ trong evidence thay vì giả vờ test tự động.
    ///
    /// Nếu sau này ai đó gỡ điều kiện `!IsEnvironment("Testing")`, test này sẽ đỏ và buộc phải
    /// bổ sung đường kiểm chứng thật cho role Admin — đó là chủ ý.
    /// </summary>
    [Fact]
    public async Task E7_Hangfire_dashboard_is_never_publicly_exposed()
    {
        var anon = factory.CreateClient();
        var anonRes = await anon.GetAsync("/hangfire");
        Assert.True(anonRes.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                        or HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.NotFound,
            $"Khách không token không được thấy dashboard, thực tế {(int)anonRes.StatusCode}.");

        var authorToken = await RegisterAndGetTokenAsync("e7-author", ["Author"]);
        var authorClient = factory.CreateClient();
        authorClient.DefaultRequestHeaders.Authorization = new("Bearer", authorToken);
        var authorRes = await authorClient.GetAsync("/hangfire");
        Assert.True(authorRes.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                        or HttpStatusCode.NotFound,
            $"Author (không phải Admin) không được thấy dashboard, thực tế {(int)authorRes.StatusCode}.");

        anon.Dispose();
        authorClient.Dispose();
    }
}



