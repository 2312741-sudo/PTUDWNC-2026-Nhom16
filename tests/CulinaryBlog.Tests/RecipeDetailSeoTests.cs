using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// K19 (TV3) — NFR-SEO-001: JSON-LD Recipe cần author (Person.name) và nên có recipeCategory.
/// Trang chi tiết dựng JSON-LD từ GET /api/v1/recipes/{slug}, nên DTO chi tiết phải trả tên hiển thị tác giả và tên danh mục
/// (chỉ tên công khai, không email/id nội bộ thêm).
/// </summary>
public sealed class RecipeDetailSeoTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly ApiFactory _factory;

    public RecipeDetailSeoTests(ApiFactory factory)
    {
        _factory = factory;
        factory.EnsureMigrated();
    }

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

    private async Task<(HttpClient Client, string Email, string Password)> NewAuthorClient(string displayName)
    {
        var client = _factory.CreateClient();
        var cmd = new RegisterCommand($"tv3-seo-{Guid.NewGuid():N}@example.test", "Recipe-Flow9!", displayName);
        var res = await client.PostAsJsonAsync("/api/v1/auth/register", cmd);
        await AssertStatus(HttpStatusCode.Created, res);
        var auth = (await res.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, cmd.Email, cmd.Password);
    }

    /// <summary>Lấy 1 danh mục có sẵn; DB rỗng (CI) thì tạo bằng tài khoản được nâng Admin.</summary>
    private async Task<(Guid Id, string Name)> AnyCategory()
    {
        var list = await DataOf(await _factory.CreateClient().GetAsync("/api/v1/categories"));
        var items = list.ValueKind == JsonValueKind.Array ? list
            : list.TryGetProperty("items", out var it) ? it : default;
        if (items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
            return (items[0].GetProperty("id").GetGuid(), items[0].GetProperty("name").GetString()!);

        var (client, email, password) = await NewAuthorClient("Quản trị SEO");
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var added = await users.AddToRoleAsync((await users.FindByEmailAsync(email))!, Roles.Admin);
            Assert.True(added.Succeeded, string.Join("; ", added.Errors.Select(e => e.Description)));
        }
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        await AssertStatus(HttpStatusCode.OK, login);
        var token = (await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var name = $"Danh mục SEO {Guid.NewGuid():N}"[..28];
        var ctor = typeof(CreateCategoryCommand).GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var args = ctor.GetParameters().Select(p => p.Name switch
        {
            "Name" => (object?)name,
            "Description" => "Tạo bởi RecipeDetailSeoTests",
            _ => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null,
        }).ToArray();
        var created = await client.PostAsJsonAsync("/api/v1/categories", (CreateCategoryCommand)ctor.Invoke(args));
        await AssertStatus(HttpStatusCode.Created, created);
        var data = await DataOf(created);
        return (data.GetProperty("id").GetGuid(), data.GetProperty("name").GetString()!);
    }

    /// <summary>Danh mục riêng cho test (tạo thẳng trong DB) để có thể xoá mềm mà không ảnh hưởng test khác.</summary>
    private async Task<(Guid Id, string Name)> OwnCategory()
    {
        var name = $"SEO riêng {Guid.NewGuid():N}"[..24];
        var category = new Category(name, $"seo-rieng-{Guid.NewGuid():N}");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return (category.Id, name);
    }

    private static async Task<string> PublishedRecipe(HttpClient author, Guid categoryId)
    {
        var create = await author.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeCommand(
            $"Canh chua SEO {Guid.NewGuid():N}"[..30], "Canh chua.", null, 10, 20, 2, RecipeDifficulty.Easy, categoryId, null));
        await AssertStatus(HttpStatusCode.Created, create);
        var recipe = await DataOf(create);
        var id = recipe.GetProperty("id").GetGuid();
        await AssertStatus(HttpStatusCode.Created, await author.PostAsJsonAsync($"/api/v1/recipes/{id}/ingredients", new IngredientBody("Cá lóc", 500m, "g", null)));
        await AssertStatus(HttpStatusCode.Created, await author.PostAsJsonAsync($"/api/v1/recipes/{id}/steps", new StepBody("Sơ chế", "Làm sạch cá.", null, null)));
        await AssertStatus(HttpStatusCode.OK, await author.PatchAsync($"/api/v1/recipes/{id}/publish", null));
        return recipe.GetProperty("slug").GetString()!;
    }

    private async Task<JsonElement> AnonymousDetail(string slug)
    {
        var res = await _factory.CreateClient().GetAsync($"/api/v1/recipes/{slug}");
        await AssertStatus(HttpStatusCode.OK, res);
        return await DataOf(res);
    }

    [Fact]
    public async Task Blank_display_name_gives_null_authorName_so_json_ld_omits_author()
    {
        var (categoryId, _) = await OwnCategory();
        var (author, email, _) = await NewAuthorClient("Tên tạm");
        var slug = await PublishedRecipe(author, categoryId);
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByEmailAsync(email))!;
            user.DisplayName = "   ";
            Assert.True((await users.UpdateAsync(user)).Succeeded);
        }

        var detail = await AnonymousDetail(slug);

        Assert.Equal(JsonValueKind.Null, detail.GetProperty("authorName").ValueKind);
    }

    [Fact]
    public async Task Soft_deleted_category_still_returns_author_name_and_null_category_name()
    {
        var (categoryId, _) = await OwnCategory();
        var (author, _, _) = await NewAuthorClient("Bếp Danh Mục Xoá");
        var slug = await PublishedRecipe(author, categoryId);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var category = await db.Categories.SingleAsync(c => c.Id == categoryId);
            category.MarkDeleted();
            await db.SaveChangesAsync();
        }

        var detail = await AnonymousDetail(slug);

        Assert.Equal("Bếp Danh Mục Xoá", detail.GetProperty("authorName").GetString());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("categoryName").ValueKind);
    }

    [Fact]
    public async Task Published_detail_returns_author_display_name_and_category_name_for_json_ld()
    {
        var (categoryId, categoryName) = await AnyCategory();
        var (author, _, _) = await NewAuthorClient("Bếp Nhà Trung TV3");

        var create = await author.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeCommand(
            $"Gà lắc phô mai {Guid.NewGuid():N}"[..30], "Gà chiên giòn lắc phô mai.", null,
            20, 25, 2, RecipeDifficulty.Easy, categoryId, null));
        await AssertStatus(HttpStatusCode.Created, create);
        var recipe = await DataOf(create);
        var id = recipe.GetProperty("id").GetGuid();
        await AssertStatus(HttpStatusCode.Created, await author.PostAsJsonAsync($"/api/v1/recipes/{id}/ingredients", new IngredientBody("Đùi gà", 500m, "g", null)));
        await AssertStatus(HttpStatusCode.Created, await author.PostAsJsonAsync($"/api/v1/recipes/{id}/steps", new StepBody("Ướp", "Ướp gà 30 phút.", null, null)));
        await AssertStatus(HttpStatusCode.OK, await author.PatchAsync($"/api/v1/recipes/{id}/publish", null));

        // Khách (không đăng nhập) như trang ISR của Next.js
        var res = await _factory.CreateClient().GetAsync($"/api/v1/recipes/{recipe.GetProperty("slug").GetString()}");
        await AssertStatus(HttpStatusCode.OK, res);
        var detail = await DataOf(res);

        Assert.True(detail.TryGetProperty("authorName", out var authorName), "thiếu authorName trong DTO chi tiết");
        Assert.Equal("Bếp Nhà Trung TV3", authorName.GetString());
        Assert.True(detail.TryGetProperty("categoryName", out var category), "thiếu categoryName trong DTO chi tiết");
        Assert.Equal(categoryName, category.GetString());
        Assert.False(detail.TryGetProperty("authorEmail", out _));
    }
}
