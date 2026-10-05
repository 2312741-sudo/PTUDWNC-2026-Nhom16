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
/// C7 / 2d — Xoá bước trên Postgres thật (ApiFactory + JWT thật).
/// Test unit DeleteStep_middle_step_renumbers... chạy trên Fake nên không đụng tới interceptor xoá mềm
/// và unique index (RecipeId, StepNumber). Hai test dưới đây khẳng định hành vi ĐÚNG theo D16:
/// sau khi xoá, các bước còn lại liên tục 1..N và thêm bước mới ra số kế tiếp.
/// </summary>
public sealed class RecipeStepDeletionTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly ApiFactory _factory;

    public RecipeStepDeletionTests(ApiFactory factory)
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

    private async Task<(HttpClient Client, string Email, string Password)> NewAuthorClient()
    {
        var client = _factory.CreateClient();
        var cmd = new RegisterCommand($"tv3-stepdel-{Guid.NewGuid():N}@example.test", "Recipe-Flow9!", "Huỳnh Quốc Trung TV3");
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

        var ctor = typeof(CreateCategoryCommand).GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var args = ctor.GetParameters().Select(p => p.Name switch
        {
            "Name" => (object?)$"Danh mục test {Guid.NewGuid():N}"[..30],
            "Description" => "Tạo bởi RecipeStepDeletionTests",
            _ => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null,
        }).ToArray();
        var created = await client.PostAsJsonAsync("/api/v1/categories", (CreateCategoryCommand)ctor.Invoke(args));
        await AssertStatus(HttpStatusCode.Created, created);
        return (await DataOf(created)).GetProperty("id").GetGuid();
    }

    /// <summary>Tạo recipe Draft kèm 3 bước qua API; trả về id recipe và id các bước theo thứ tự 1..3.</summary>
    private async Task<(HttpClient Client, Guid RecipeId, List<Guid> StepIds)> RecipeWithThreeSteps()
    {
        var categoryId = await AnyCategoryId();
        var (client, _, _) = await NewAuthorClient();

        var createRes = await client.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeCommand(
            $"Bún riêu cua {Guid.NewGuid():N}"[..30], "Món bún riêu", "Nấu theo các bước bên dưới",
            15, 20, 4, RecipeDifficulty.Easy, categoryId, null));
        await AssertStatus(HttpStatusCode.Created, createRes);
        var recipeId = (await DataOf(createRes)).GetProperty("id").GetGuid();

        var stepIds = new List<Guid>();
        for (var i = 1; i <= 3; i++)
        {
            var res = await client.PostAsJsonAsync($"/api/v1/recipes/{recipeId}/steps",
                new StepBody($"Bước {i}", $"Mô tả bước {i}", 5, null));
            await AssertStatus(HttpStatusCode.Created, res);
            stepIds.Add((await DataOf(res)).GetProperty("id").GetGuid());
        }
        return (client, recipeId, stepIds);
    }

    /// <summary>Đọc thẳng DB, kể cả dòng đã xoá mềm, để thấy StepNumber thật của từng dòng.</summary>
    private async Task<List<(Guid Id, int StepNumber, bool IsDeleted)>> StepsInDb(Guid recipeId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var rows = await db.RecipeSteps.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.RecipeId == recipeId)
            .OrderBy(s => s.StepNumber)
            .Select(s => new { s.Id, s.StepNumber, s.IsDeleted })
            .ToListAsync();
        return rows.Select(r => (r.Id, r.StepNumber, r.IsDeleted)).ToList();
    }

    // ------------------------------------------------------------------ tests

    [Fact]
    public async Task Deleting_middle_step_returns_204_and_renumbers_remaining_steps_1_to_N()
    {
        var (client, recipeId, steps) = await RecipeWithThreeSteps();

        var delete = await client.DeleteAsync($"/api/v1/recipes/{recipeId}/steps/{steps[1]}");
        await AssertStatus(HttpStatusCode.NoContent, delete);

        var live = (await StepsInDb(recipeId)).Where(s => !s.IsDeleted).ToList();
        Assert.Equal([steps[0], steps[2]], live.Select(s => s.Id));
        Assert.Equal([1, 2], live.Select(s => s.StepNumber));
    }

    [Fact]
    public async Task Deleting_last_step_then_adding_a_step_gives_the_next_number()
    {
        var (client, recipeId, steps) = await RecipeWithThreeSteps();

        var delete = await client.DeleteAsync($"/api/v1/recipes/{recipeId}/steps/{steps[2]}");
        await AssertStatus(HttpStatusCode.NoContent, delete);

        var add = await client.PostAsJsonAsync($"/api/v1/recipes/{recipeId}/steps",
            new StepBody("Bước mới", "Thêm sau khi xoá bước cuối", null, null));
        await AssertStatus(HttpStatusCode.Created, add);
        var newId = (await DataOf(add)).GetProperty("id").GetGuid();

        var live = (await StepsInDb(recipeId)).Where(s => !s.IsDeleted).ToList();
        Assert.Equal([steps[0], steps[1], newId], live.Select(s => s.Id));
        Assert.Equal([1, 2, 3], live.Select(s => s.StepNumber));
    }

    [Fact]
    public async Task Deleting_last_step_and_adding_again_twice_allows_two_soft_deleted_rows_with_same_number()
    {
        var (client, recipeId, steps) = await RecipeWithThreeSteps();
        var lastId = steps[2];
        var removed = new List<Guid>();

        for (var round = 1; round <= 2; round++)
        {
            var delete = await client.DeleteAsync($"/api/v1/recipes/{recipeId}/steps/{lastId}");
            await AssertStatus(HttpStatusCode.NoContent, delete);
            removed.Add(lastId);

            var add = await client.PostAsJsonAsync($"/api/v1/recipes/{recipeId}/steps",
                new StepBody($"Bước thay thế {round}", "Thêm sau khi xoá bước cuối", null, null));
            await AssertStatus(HttpStatusCode.Created, add);
            lastId = (await DataOf(add)).GetProperty("id").GetGuid();
        }

        var rows = await StepsInDb(recipeId);
        var live = rows.Where(s => !s.IsDeleted).ToList();
        Assert.Equal([steps[0], steps[1], lastId], live.Select(s => s.Id));
        Assert.Equal([1, 2, 3], live.Select(s => s.StepNumber));

        // Hai dòng xoá mềm cùng giữ StepNumber = 3: index partial phải cho phép.
        var deleted = rows.Where(s => s.IsDeleted).ToList();
        Assert.Equal(removed.Order(), deleted.Select(s => s.Id).Order());
        Assert.All(deleted, s => Assert.Equal(3, s.StepNumber));
    }
}
