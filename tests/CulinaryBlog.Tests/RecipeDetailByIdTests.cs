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
/// C1/C4 — GET /recipes/{key} nhận cả id (Guid) ngoài slug.
/// Lỗi tìm ra bằng E2E (e) tuần 4: sửa tiêu đề bản nháp đổi slug -> tab thứ hai (giữ slug cũ) bấm "Tải dữ liệu mới nhất"
/// gọi GET theo slug cũ -> 404, form không được nạp lại. Id không đổi nên wizard nạp lại theo id.
/// Phân quyền giữ nguyên: Draft chỉ chủ sở hữu/Admin xem được, người khác nhận 404 như tra theo slug.
/// </summary>
public sealed class RecipeDetailByIdTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly ApiFactory _factory;

    public RecipeDetailByIdTests(ApiFactory factory)
    {
        _factory = factory;
        factory.EnsureMigrated();
    }

    [Fact]
    public async Task Draft_title_change_changes_slug_owner_can_still_load_detail_by_id()
    {
        var (client, _, _) = await NewAuthorClient();
        var categoryId = await AnyCategoryId();
        var created = await CreateRecipe(client, categoryId, $"Canh chua {Guid.NewGuid():N}"[..30]);
        var oldSlug = created.GetProperty("slug").GetString()!;
        var id = created.GetProperty("id").GetGuid();

        var newTitle = $"Lẩu mắm {Guid.NewGuid():N}"[..30];
        var put = await client.PutAsJsonAsync($"/api/v1/recipes/{id}", new UpdateRecipeBody(
            newTitle, "Mô tả", null, 15, 20, 4, RecipeDifficulty.Easy, categoryId, null,
            created.GetProperty("rowVersion").GetString()));
        await AssertStatus(HttpStatusCode.OK, put);
        var newSlug = (await DataOf(put)).GetProperty("slug").GetString()!;
        Assert.NotEqual(oldSlug, newSlug);

        // Slug cũ không còn trỏ tới công thức
        await AssertStatus(HttpStatusCode.NotFound, await client.GetAsync($"/api/v1/recipes/{oldSlug}"));

        var byId = await client.GetAsync($"/api/v1/recipes/{id}");
        await AssertStatus(HttpStatusCode.OK, byId);
        var detail = await DataOf(byId);
        Assert.Equal(id, detail.GetProperty("id").GetGuid());
        Assert.Equal(newSlug, detail.GetProperty("slug").GetString());
        Assert.Equal(newTitle, detail.GetProperty("title").GetString());
        Assert.False(string.IsNullOrEmpty(detail.GetProperty("rowVersion").GetString()));
    }

    [Fact]
    public async Task Draft_by_id_is_404_for_anonymous_and_other_author()
    {
        var (owner, _, _) = await NewAuthorClient();
        var id = (await CreateRecipe(owner, await AnyCategoryId(), $"Bún bò {Guid.NewGuid():N}"[..30])).GetProperty("id").GetGuid();
        var (other, _, _) = await NewAuthorClient();

        await AssertStatus(HttpStatusCode.NotFound, await _factory.CreateClient().GetAsync($"/api/v1/recipes/{id}"));
        await AssertStatus(HttpStatusCode.NotFound, await other.GetAsync($"/api/v1/recipes/{id}"));
        await AssertStatus(HttpStatusCode.OK, await owner.GetAsync($"/api/v1/recipes/{id}"));
    }

    [Fact]
    public async Task Published_recipe_by_id_is_public()
    {
        var (client, _, _) = await NewAuthorClient();
        var created = await CreateRecipe(client, await AnyCategoryId(), $"Phở gà {Guid.NewGuid():N}"[..30]);
        var id = created.GetProperty("id").GetGuid();
        await AssertStatus(HttpStatusCode.Created, await client.PostAsJsonAsync($"/api/v1/recipes/{id}/ingredients",
            new IngredientBody("Gà ta", 1, "con", null)));
        await AssertStatus(HttpStatusCode.Created, await client.PostAsJsonAsync($"/api/v1/recipes/{id}/steps",
            new StepBody("Luộc gà", "Luộc gà với gừng.", null, null)));
        await AssertStatus(HttpStatusCode.OK, await client.PatchAsync($"/api/v1/recipes/{id}/publish", null));

        var anon = await _factory.CreateClient().GetAsync($"/api/v1/recipes/{id}");
        await AssertStatus(HttpStatusCode.OK, anon);
        Assert.Equal(created.GetProperty("slug").GetString(), (await DataOf(anon)).GetProperty("slug").GetString());
    }

    [Fact]
    public async Task Unknown_id_is_404()
    {
        var (client, _, _) = await NewAuthorClient();
        await AssertStatus(HttpStatusCode.NotFound, await client.GetAsync($"/api/v1/recipes/{Guid.NewGuid()}"));
    }

    private static async Task<JsonElement> CreateRecipe(HttpClient client, Guid categoryId, string title)
    {
        var res = await client.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeCommand(
            title, "Mô tả", null, 15, 20, 4, RecipeDifficulty.Easy, categoryId, null));
        await AssertStatus(HttpStatusCode.Created, res);
        return await DataOf(res);
    }

    private static async Task<JsonElement> DataOf(HttpResponseMessage res)
    {
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    private static async Task AssertStatus(HttpStatusCode expected, HttpResponseMessage res)
    {
        if (res.StatusCode != expected)
            Assert.Fail($"{res.RequestMessage?.Method} {res.RequestMessage?.RequestUri} -> {(int)res.StatusCode} (cần {(int)expected}): {await res.Content.ReadAsStringAsync()}");
    }

    private async Task<(HttpClient Client, string Email, string Password)> NewAuthorClient()
    {
        var client = _factory.CreateClient();
        var cmd = new RegisterCommand($"tv3-byid-{Guid.NewGuid():N}@example.test", "Recipe-Flow9!", "Huỳnh Quốc Trung TV3");
        var res = await client.PostAsJsonAsync("/api/v1/auth/register", cmd);
        await AssertStatus(HttpStatusCode.Created, res);
        var auth = (await res.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, cmd.Email, cmd.Password);
    }

    /// <summary>Lấy 1 danh mục có sẵn; DB test chưa seed thì tạo bằng tài khoản Admin.</summary>
    private async Task<Guid> AnyCategoryId()
    {
        var list = await DataOf(await _factory.CreateClient().GetAsync("/api/v1/categories"));
        var items = list.ValueKind == JsonValueKind.Array ? list
            : list.TryGetProperty("items", out var it) ? it : default;
        if (items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
            return items[0].GetProperty("id").GetGuid();

        var (client, email, password) = await NewAuthorClient();
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var added = await users.AddToRoleAsync((await users.FindByEmailAsync(email))!, Roles.Admin);
            Assert.True(added.Succeeded, string.Join("; ", added.Errors.Select(e => e.Description)));
        }
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        await AssertStatus(HttpStatusCode.OK, login);
        var adminToken = (await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var ctor = typeof(CreateCategoryCommand).GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var args = ctor.GetParameters().Select(p => p.Name switch
        {
            "Name" => (object?)$"Danh mục test {Guid.NewGuid():N}"[..30],
            "Description" => "Tạo bởi RecipeDetailByIdTests",
            _ => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null,
        }).ToArray();
        var created = await client.PostAsJsonAsync("/api/v1/categories", (CreateCategoryCommand)ctor.Invoke(args));
        await AssertStatus(HttpStatusCode.Created, created);
        return (await DataOf(created)).GetProperty("id").GetGuid();
    }
}
