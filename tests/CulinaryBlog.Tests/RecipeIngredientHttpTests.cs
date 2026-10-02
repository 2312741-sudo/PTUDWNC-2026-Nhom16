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
/// C7 (TV3) — nguyên liệu/bước qua HTTP trên Postgres thật (ApiFactory + migration thật), bổ sung cho test mức handler:
///  (1) thêm, sửa, xoá nguyên liệu: status, OrderIndex liên tục 0..N-1 trong DB, dòng bị xoá chỉ xoá mềm;
///  (2) tác giả khác thêm/sửa/xoá nguyên liệu hoặc bước của công thức người khác -> 403, ẩn danh -> 401, DB không đổi;
///  (3) rowVersion sai khi sửa/xoá công thức -> 422 recipe.concurrency_conflict, công thức còn nguyên;
///  (4) hành vi hiện tại: đổi nguyên liệu KHÔNG đổi RowVersion của recipe (rowVersion wizard giữ vẫn hợp lệ).
/// </summary>
public sealed class RecipeIngredientHttpTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly ApiFactory _factory;

    public RecipeIngredientHttpTests(ApiFactory factory)
    {
        _factory = factory;
        factory.EnsureMigrated();
    }

    // ------------------------------------------------------------------ helpers

    private sealed record IngredientRow(Guid Id, string Name, decimal? Quantity, int OrderIndex, bool IsDeleted);

    private static async Task<JsonElement> DataOf(HttpResponseMessage res)
    {
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    private static async Task<string?> ErrorCodeOf(HttpResponseMessage res)
    {
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private static async Task AssertStatus(HttpStatusCode expected, HttpResponseMessage res)
    {
        if (res.StatusCode != expected)
            Assert.Fail($"{res.RequestMessage?.Method} {res.RequestMessage?.RequestUri} -> {(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
    }

    private async Task<(HttpClient Client, string Email, string Password)> NewAuthorClient()
    {
        var client = _factory.CreateClient();
        var cmd = new RegisterCommand($"tv3-ing-{Guid.NewGuid():N}@example.test", "Recipe-Flow9!", "Huỳnh Quốc Trung TV3");
        var res = await client.PostAsJsonAsync("/api/v1/auth/register", cmd);
        await AssertStatus(HttpStatusCode.Created, res);
        var auth = (await res.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, cmd.Email, cmd.Password);
    }

    /// <summary>Lấy 1 danh mục có sẵn; DB rỗng (CI) thì tạo bằng tài khoản được nâng Admin.</summary>
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
        var token = (await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ctor = typeof(CreateCategoryCommand).GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var args = ctor.GetParameters().Select(p => p.Name switch
        {
            "Name" => (object?)$"Danh mục test {Guid.NewGuid():N}"[..30],
            "Description" => "Tạo bởi RecipeIngredientHttpTests",
            _ => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null,
        }).ToArray();
        var created = await client.PostAsJsonAsync("/api/v1/categories", (CreateCategoryCommand)ctor.Invoke(args));
        await AssertStatus(HttpStatusCode.Created, created);
        return (await DataOf(created)).GetProperty("id").GetGuid();
    }

    private static async Task<(Guid Id, string Slug, string RowVersion)> CreateRecipe(HttpClient client, Guid categoryId)
    {
        var res = await client.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeCommand(
            $"Canh chua {Guid.NewGuid():N}"[..30], "Canh chua cá lóc", "Nấu theo các bước",
            15, 20, 4, RecipeDifficulty.Easy, categoryId, null));
        await AssertStatus(HttpStatusCode.Created, res);
        var data = await DataOf(res);
        return (data.GetProperty("id").GetGuid(), data.GetProperty("slug").GetString()!, data.GetProperty("rowVersion").GetString()!);
    }

    private static async Task<Guid> AddIngredient(HttpClient client, Guid recipeId, string name, decimal? quantity)
    {
        var res = await client.PostAsJsonAsync($"/api/v1/recipes/{recipeId}/ingredients", new IngredientBody(name, quantity, "g", null));
        await AssertStatus(HttpStatusCode.Created, res);
        return (await DataOf(res)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> AddStep(HttpClient client, Guid recipeId, string title)
    {
        var res = await client.PostAsJsonAsync($"/api/v1/recipes/{recipeId}/steps", new StepBody(title, $"Mô tả {title}", 5, null));
        await AssertStatus(HttpStatusCode.Created, res);
        return (await DataOf(res)).GetProperty("id").GetGuid();
    }

    /// <summary>Đọc thẳng DB bằng scope mới, kể cả dòng đã xoá mềm.</summary>
    private async Task<List<IngredientRow>> IngredientsInDb(Guid recipeId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await db.RecipeIngredients.IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.RecipeId == recipeId)
            .OrderBy(i => i.IsDeleted).ThenBy(i => i.OrderIndex)
            .Select(i => new IngredientRow(i.Id, i.Name, i.Quantity, i.OrderIndex, i.IsDeleted))
            .ToListAsync();
    }

    private async Task<string> RecipeRowVersionInDb(Guid recipeId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var rv = await db.Recipes.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.Id == recipeId).Select(r => r.RowVersion).SingleAsync();
        return Convert.ToBase64String(rv);
    }

    // ------------------------------------------------------------------ (1) thêm, sửa, xoá

    [Fact]
    public async Task Add_update_delete_ingredient_returns_201_200_204_and_keeps_order_index_contiguous_in_db()
    {
        var (client, _, _) = await NewAuthorClient();
        var (recipeId, slug, _) = await CreateRecipe(client, await AnyCategoryId());
        var ca = await AddIngredient(client, recipeId, "Cá lóc", 500m);
        var me = await AddIngredient(client, recipeId, "Me chua", 50m);
        var caChua = await AddIngredient(client, recipeId, "Cà chua", 2m);

        var put = await client.PutAsJsonAsync($"/api/v1/recipes/{recipeId}/ingredients/{me}", new IngredientBody("Me chín", 60m, "g", "dầm lấy nước"));
        await AssertStatus(HttpStatusCode.OK, put);
        var updated = await DataOf(put);
        Assert.Equal("Me chín", updated.GetProperty("name").GetString());
        Assert.Equal(1, updated.GetProperty("orderIndex").GetInt32());   // sửa không đổi vị trí

        await AssertStatus(HttpStatusCode.NoContent, await client.DeleteAsync($"/api/v1/recipes/{recipeId}/ingredients/{ca}"));

        var rows = await IngredientsInDb(recipeId);
        Assert.Equal(
            [new IngredientRow(me, "Me chín", 60m, 0, false), new IngredientRow(caChua, "Cà chua", 2m, 1, false)],
            rows.Where(r => !r.IsDeleted).ToList());
        Assert.Equal(ca, Assert.Single(rows, r => r.IsDeleted).Id);   // xoá mềm, không xoá cứng

        var detail = await DataOf(await client.GetAsync($"/api/v1/recipes/{slug}"));
        Assert.Equal(["Me chín", "Cà chua"],
            detail.GetProperty("ingredients").EnumerateArray().Select(i => i.GetProperty("name").GetString()!).ToList());

        // Xoá lần hai cùng id -> 404 (đã xoá mềm, không còn trong aggregate)
        var again = await client.DeleteAsync($"/api/v1/recipes/{recipeId}/ingredients/{ca}");
        await AssertStatus(HttpStatusCode.NotFound, again);
        Assert.Equal("ingredient.not_found", await ErrorCodeOf(again));
    }

    // ------------------------------------------------------------------ (2) quyền sở hữu

    [Fact]
    public async Task Other_author_cannot_add_update_or_delete_children_of_someone_elses_recipe_403_and_db_unchanged()
    {
        var categoryId = await AnyCategoryId();
        var (owner, _, _) = await NewAuthorClient();
        var (stranger, _, _) = await NewAuthorClient();
        var (recipeId, slug, _) = await CreateRecipe(owner, categoryId);
        var ing = await AddIngredient(owner, recipeId, "Cá lóc", 500m);
        var step = await AddStep(owner, recipeId, "Sơ chế");
        var before = await IngredientsInDb(recipeId);

        var attempts = new[]
        {
            await stranger.PostAsJsonAsync($"/api/v1/recipes/{recipeId}/ingredients", new IngredientBody("Ớt", 1m, null, null)),
            await stranger.PutAsJsonAsync($"/api/v1/recipes/{recipeId}/ingredients/{ing}", new IngredientBody("Bị sửa", 1m, null, null)),
            await stranger.DeleteAsync($"/api/v1/recipes/{recipeId}/ingredients/{ing}"),
            await stranger.PostAsJsonAsync($"/api/v1/recipes/{recipeId}/steps", new StepBody("Lạ", "Không được thêm", null, null)),
            await stranger.PutAsJsonAsync($"/api/v1/recipes/{recipeId}/steps/{step}", new StepBody("Bị sửa", "Không được sửa", null, null)),
            await stranger.DeleteAsync($"/api/v1/recipes/{recipeId}/steps/{step}"),
        };
        foreach (var res in attempts)
        {
            await AssertStatus(HttpStatusCode.Forbidden, res);
            Assert.Equal("recipe.forbidden", await ErrorCodeOf(res));
        }

        var anonymous = _factory.CreateClient();
        await AssertStatus(HttpStatusCode.Unauthorized, await anonymous.DeleteAsync($"/api/v1/recipes/{recipeId}/ingredients/{ing}"));

        Assert.Equal(before, await IngredientsInDb(recipeId));
        var detail = await DataOf(await owner.GetAsync($"/api/v1/recipes/{slug}"));
        Assert.Equal("Sơ chế", Assert.Single(detail.GetProperty("steps").EnumerateArray()).GetProperty("title").GetString());
    }

    // ------------------------------------------------------------------ (3) rowVersion sai

    [Fact]
    public async Task Wrong_rowVersion_on_update_or_delete_recipe_returns_422_and_recipe_stays()
    {
        var categoryId = await AnyCategoryId();
        var (client, _, _) = await NewAuthorClient();
        var (recipeId, slug, rowVersion) = await CreateRecipe(client, categoryId);
        var wrong = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        var put = await client.PutAsJsonAsync($"/api/v1/recipes/{recipeId}", new UpdateRecipeBody(
            "Tiêu đề không được lưu", "Mô tả", null, 15, 20, 4, RecipeDifficulty.Easy, categoryId, null, wrong));
        await AssertStatus(HttpStatusCode.UnprocessableEntity, put);
        Assert.Equal("recipe.concurrency_conflict", await ErrorCodeOf(put));

        using var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/recipes/{recipeId}");
        delete.Headers.TryAddWithoutValidation("If-Match", $"\"{wrong}\"");
        var del = await client.SendAsync(delete);
        await AssertStatus(HttpStatusCode.UnprocessableEntity, del);
        Assert.Equal("recipe.concurrency_conflict", await ErrorCodeOf(del));

        var detail = await DataOf(await client.GetAsync($"/api/v1/recipes/{slug}"));
        Assert.NotEqual("Tiêu đề không được lưu", detail.GetProperty("title").GetString());
        Assert.Equal(rowVersion, detail.GetProperty("rowVersion").GetString());
    }

    // ------------------------------------------------------------------ (4) hành vi hiện tại

    [Fact]
    public async Task CurrentBehavior_changing_ingredients_does_not_change_recipe_rowVersion_so_wizard_can_still_save_step_1()
    {
        var categoryId = await AnyCategoryId();
        var (client, _, _) = await NewAuthorClient();
        var (recipeId, _, rowVersion) = await CreateRecipe(client, categoryId);

        var ing = await AddIngredient(client, recipeId, "Cá lóc", 500m);
        await AssertStatus(HttpStatusCode.OK, await client.PutAsJsonAsync(
            $"/api/v1/recipes/{recipeId}/ingredients/{ing}", new IngredientBody("Cá lóc đồng", 600m, "g", null)));
        await AssertStatus(HttpStatusCode.NoContent, await client.DeleteAsync($"/api/v1/recipes/{recipeId}/ingredients/{ing}"));

        // RowVersion nằm trên từng entity; recipe gốc không Modified nên token giữ nguyên.
        Assert.Equal(rowVersion, await RecipeRowVersionInDb(recipeId));
        var put = await client.PutAsJsonAsync($"/api/v1/recipes/{recipeId}", new UpdateRecipeBody(
            "Canh chua cá lóc đã sửa", "Mô tả", null, 15, 20, 4, RecipeDifficulty.Easy, categoryId, null, rowVersion));
        await AssertStatus(HttpStatusCode.OK, put);
    }
}
