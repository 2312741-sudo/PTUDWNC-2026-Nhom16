using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// C7 — Integration test luồng soạn công thức trên Postgres thật + JWT thật.
/// Khoá 2 lỗi từng lọt qua test dùng Fake:
///  1. RoleClaimType: JWT mang claim "role" nhưng AuthorPolicy không đọc được -> 403.
///  2. ValueGeneratedNever: EF UPDATE thay vì INSERT khi thêm nguyên liệu/bước -> concurrency error.
/// </summary>
public sealed class RecipeAuthoringFlowTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly ApiFactory _factory;

    public RecipeAuthoringFlowTests(ApiFactory factory)
    {
        _factory = factory;
        factory.EnsureMigrated();
    }

    // ------------------------------------------------------------------ helpers

    private static async Task<JsonElement> DataOf(HttpResponseMessage res)
    {
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    private static async Task AssertStatus(HttpStatusCode expected, HttpResponseMessage res)
    {
        if (res.StatusCode != expected)
            Assert.Fail($"{res.RequestMessage?.Method} {res.RequestMessage?.RequestUri} -> {(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
    }

    /// <summary>Đăng ký user mới (mặc định role Author) và trả về client đã gắn Bearer token.</summary>
    private async Task<(HttpClient Client, string Email, string Password)> NewAuthorClient()
    {
        var client = _factory.CreateClient();
        var cmd = new RegisterCommand($"tv3-flow-{Guid.NewGuid():N}@example.test", "Recipe-Flow9!", "Huỳnh Quốc Trung TV3");
        var res = await client.PostAsJsonAsync("/api/v1/auth/register", cmd);
        await AssertStatus(HttpStatusCode.Created, res);
        var auth = (await res.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, cmd.Email, cmd.Password);
    }

    /// <summary>Lấy 1 danh mục có sẵn; DB test chưa seed thì tạo bằng tài khoản Admin.</summary>
    private async Task<Guid> AnyCategoryId()
    {
        var anon = _factory.CreateClient();
        var list = await DataOf(await anon.GetAsync("/api/v1/categories"));
        var items = list.ValueKind == JsonValueKind.Array ? list
            : list.TryGetProperty("items", out var it) ? it : default;
        if (items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
            return items[0].GetProperty("id").GetGuid();

        // Tạo admin: đăng ký -> gán role Admin qua UserManager -> đăng nhập lại để token có role
        var (client, email, password) = await NewAuthorClient();
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByEmailAsync(email))!;
            var added = await users.AddToRoleAsync(user, Roles.Admin);
            Assert.True(added.Succeeded, string.Join("; ", added.Errors.Select(e => e.Description)));
        }
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        await AssertStatus(HttpStatusCode.OK, login);
        var adminToken = (await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Dựng CreateCategoryCommand theo đúng constructor thật (không đoán số tham số)
        var ctor = typeof(CreateCategoryCommand).GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var args = ctor.GetParameters().Select(p => p.Name switch
        {
            "Name" => (object?)$"Danh mục test {Guid.NewGuid():N}"[..30],
            "Description" => "Tạo bởi RecipeAuthoringFlowTests",
            _ => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null,
        }).ToArray();
        var command = (CreateCategoryCommand)ctor.Invoke(args);

        var created = await client.PostAsJsonAsync("/api/v1/categories", command);
        await AssertStatus(HttpStatusCode.Created, created);
        return (await DataOf(created)).GetProperty("id").GetGuid();
    }

    private static CreateRecipeCommand NewRecipe(Guid categoryId) => new(
        $"Canh chua cá lóc {Guid.NewGuid():N}"[..30], "Món canh chua miền Tây", "Nấu theo các bước bên dưới",
        15, 20, 4, RecipeDifficulty.Easy, categoryId, null);

    // ------------------------------------------------------------------ tests

    [Fact]
    public async Task Author_creates_recipe_adds_children_reorders_and_publishes()
    {
        var categoryId = await AnyCategoryId();
        var (client, _, _) = await NewAuthorClient();

        // 1. Tạo recipe — 201, KHÔNG phải 403 (khoá lỗi RoleClaimType)
        var createRes = await client.PostAsJsonAsync("/api/v1/recipes", NewRecipe(categoryId));
        await AssertStatus(HttpStatusCode.Created, createRes);
        var recipe = await DataOf(createRes);
        var id = recipe.GetProperty("id").GetGuid();
        var slug = recipe.GetProperty("slug").GetString()!;

        // 2. Thêm 2 nguyên liệu + 2 bước liên tiếp — 201 (khoá lỗi ValueGeneratedNever)
        await AssertStatus(HttpStatusCode.Created, await client.PostAsJsonAsync($"/api/v1/recipes/{id}/ingredients",
            new IngredientBody("Cá lóc", 500m, "g", null)));
        await AssertStatus(HttpStatusCode.Created, await client.PostAsJsonAsync($"/api/v1/recipes/{id}/ingredients",
            new IngredientBody("Me chua", 50m, "g", "Dầm lấy nước")));
        var s1 = await client.PostAsJsonAsync($"/api/v1/recipes/{id}/steps", new StepBody("Sơ chế cá", "Làm sạch, cắt khúc", 10, null));
        await AssertStatus(HttpStatusCode.Created, s1);
        var s2 = await client.PostAsJsonAsync($"/api/v1/recipes/{id}/steps", new StepBody("Nấu canh", "Đun sôi nước me, cho cá vào", 15, null));
        await AssertStatus(HttpStatusCode.Created, s2);
        var step1 = (await DataOf(s1)).GetProperty("id").GetGuid();
        var step2 = (await DataOf(s2)).GetProperty("id").GetGuid();

        // 3. Đảo thứ tự bước
        var reorder = await client.PatchAsJsonAsync($"/api/v1/recipes/{id}/steps/reorder", new ReorderStepsBody([step2, step1]));
        Assert.True(reorder.IsSuccessStatusCode, await reorder.Content.ReadAsStringAsync());

        // 4. Xuất bản
        var publish = await client.PatchAsync($"/api/v1/recipes/{id}/publish", null);
        Assert.True(publish.IsSuccessStatusCode, await publish.Content.ReadAsStringAsync());

        // 5. Chi tiết: đủ 2 nguyên liệu, 2 bước, đúng thứ tự mới, đã Published
        var detailRes = await client.GetAsync($"/api/v1/recipes/{slug}");
        await AssertStatus(HttpStatusCode.OK, detailRes);
        var detail = await DataOf(detailRes);
        Assert.Equal(2, detail.GetProperty("ingredients").GetArrayLength());
        var steps = detail.GetProperty("steps").EnumerateArray()
            .OrderBy(s => s.GetProperty("stepNumber").GetInt32())
            .Select(s => s.GetProperty("id").GetGuid()).ToList();
        Assert.Equal([step2, step1], steps);
        Assert.Equal("Published", detail.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Publishing_recipe_without_ingredients_or_steps_returns_422()
    {
        var categoryId = await AnyCategoryId();
        var (client, _, _) = await NewAuthorClient();

        var createRes = await client.PostAsJsonAsync("/api/v1/recipes", NewRecipe(categoryId));
        await AssertStatus(HttpStatusCode.Created, createRes);
        var id = (await DataOf(createRes)).GetProperty("id").GetGuid();

        var publish = await client.PatchAsync($"/api/v1/recipes/{id}/publish", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, publish.StatusCode);
        Assert.Contains("RECIPE_PUBLISH_INCOMPLETE", await publish.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Anonymous_user_cannot_create_recipe()
    {
        var categoryId = await AnyCategoryId();
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/v1/recipes", NewRecipe(categoryId));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
