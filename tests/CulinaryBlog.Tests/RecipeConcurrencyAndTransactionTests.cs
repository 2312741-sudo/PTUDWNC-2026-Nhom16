using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Recipe = CulinaryBlog.Domain.Entities.Recipe;

namespace CulinaryBlog.Tests;

/// <summary>
/// C7 — Concurrency và transaction trên Postgres thật (ApiFactory + EfUnitOfWork + interceptor + migration thật).
/// Các test unit hiện có chạy trên Fake (FakeUnitOfWork gọi thẳng action) nên không chứng minh được gì ở tầng DB.
///  (1) 1a/1b/1c: hai writer cùng RowVersion -> đúng một bên thắng, không lost update, không trộn trường.
///  (2) 2a/2b: lỗi giữa transaction (reorder hai pha, tạo aggregate kèm con) -> rollback trọn vẹn.
///      2c: MÔ TẢ HÀNH VI HIỆN TẠI của ExecuteInTransactionAsync lồng nhau (không hỗ trợ), không phải đặc tả mong muốn.
///  (3) id con (ingredient/step) thuộc recipe khác bị từ chối và recipe kia còn nguyên trong DB.
/// </summary>
public sealed class RecipeConcurrencyAndTransactionTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly ApiFactory _factory;

    public RecipeConcurrencyAndTransactionTests(ApiFactory factory)
    {
        _factory = factory;
        factory.EnsureMigrated();
    }

    // ------------------------------------------------------------------ helpers

    private sealed record StepRow(Guid Id, int StepNumber, bool IsDeleted, DateTime? UpdatedAt, string RowVersion);

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

    /// <summary>Khẳng định chính: bị từ chối (4xx). Status và mã lỗi cụ thể khẳng định riêng để dễ đổi khi chốt 400 hay 404.</summary>
    private static async Task AssertRejected(HttpStatusCode currentStatus, string currentCode, HttpResponseMessage res)
    {
        var what = $"{res.RequestMessage?.Method} {res.RequestMessage?.RequestUri} -> {(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}";
        Assert.True((int)res.StatusCode is >= 400 and < 500, what);

        Assert.Equal(currentStatus, res.StatusCode);
        Assert.Equal(currentCode, await ErrorCodeOf(res));
    }

    private async Task<(HttpClient Client, string Email, string Password, string UserId)> NewAuthorClient()
    {
        var client = _factory.CreateClient();
        var cmd = new RegisterCommand($"tv3-conc-{Guid.NewGuid():N}@example.test", "Recipe-Flow9!", "Huỳnh Quốc Trung TV3");
        var res = await client.PostAsJsonAsync("/api/v1/auth/register", cmd);
        await AssertStatus(HttpStatusCode.Created, res);
        var auth = (await res.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, cmd.Email, cmd.Password, auth.User.Id);
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

        var (client, email, password, _) = await NewAuthorClient();
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
            "Description" => "Tạo bởi RecipeConcurrencyAndTransactionTests",
            _ => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null,
        }).ToArray();
        var created = await client.PostAsJsonAsync("/api/v1/categories", (CreateCategoryCommand)ctor.Invoke(args));
        await AssertStatus(HttpStatusCode.Created, created);
        return (await DataOf(created)).GetProperty("id").GetGuid();
    }

    private static async Task<(Guid Id, string Slug, string RowVersion)> CreateRecipe(HttpClient client, Guid categoryId)
    {
        var res = await client.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeCommand(
            $"Lẩu mắm {Guid.NewGuid():N}"[..30], "Món lẩu mắm", "Nấu theo các bước bên dưới",
            15, 20, 4, RecipeDifficulty.Easy, categoryId, null));
        await AssertStatus(HttpStatusCode.Created, res);
        var data = await DataOf(res);
        return (data.GetProperty("id").GetGuid(), data.GetProperty("slug").GetString()!, data.GetProperty("rowVersion").GetString()!);
    }

    /// <summary>Tạo recipe Draft kèm 2 nguyên liệu và 3 bước qua API.</summary>
    private static async Task<(Guid RecipeId, List<Guid> IngredientIds, List<Guid> StepIds)> CreateRecipeWithChildren(
        HttpClient client, Guid categoryId)
    {
        var (recipeId, _, _) = await CreateRecipe(client, categoryId);

        var ingredientIds = new List<Guid>();
        for (var i = 1; i <= 2; i++)
        {
            var res = await client.PostAsJsonAsync($"/api/v1/recipes/{recipeId}/ingredients",
                new IngredientBody($"Nguyên liệu {i}", 100m * i, "g", null));
            await AssertStatus(HttpStatusCode.Created, res);
            ingredientIds.Add((await DataOf(res)).GetProperty("id").GetGuid());
        }

        var stepIds = new List<Guid>();
        for (var i = 1; i <= 3; i++)
        {
            var res = await client.PostAsJsonAsync($"/api/v1/recipes/{recipeId}/steps",
                new StepBody($"Bước {i}", $"Mô tả bước {i}", 5, null));
            await AssertStatus(HttpStatusCode.Created, res);
            stepIds.Add((await DataOf(res)).GetProperty("id").GetGuid());
        }
        return (recipeId, ingredientIds, stepIds);
    }

    private static UpdateRecipeBody Edit(string title, string description, int servings, Guid categoryId, string? rowVersion) =>
        new(title, description, "Nấu theo các bước bên dưới", 15, 20, servings, RecipeDifficulty.Easy, categoryId, null, rowVersion);

    private static Recipe NewDraftWithChildren(Guid categoryId, string authorId)
    {
        var recipe = Recipe.CreateDraft(
            "Công thức sẽ bị rollback", $"tv3-rollback-{Guid.NewGuid():N}", "Không được phép tồn tại sau test", null,
            10, 0, 2, RecipeDifficulty.Easy, categoryId, authorId);
        recipe.AddIngredient("Muối", 1m, "thìa", null);
        recipe.AddStep("Bước duy nhất", "Trộn đều", null, null);
        return recipe;
    }

    /// <summary>Đọc thẳng DB bằng scope mới, kể cả dòng đã xoá mềm.</summary>
    private async Task<List<StepRow>> StepsInDb(Guid recipeId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var rows = await db.RecipeSteps.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.RecipeId == recipeId)
            .OrderBy(s => s.StepNumber).ThenBy(s => s.Id)
            .Select(s => new { s.Id, s.StepNumber, s.IsDeleted, s.UpdatedAt, s.RowVersion })
            .ToListAsync();
        return rows.Select(r => new StepRow(r.Id, r.StepNumber, r.IsDeleted, r.UpdatedAt, Convert.ToBase64String(r.RowVersion))).ToList();
    }

    /// <summary>Chụp recipe và toàn bộ con (kể cả dòng xoá mềm) thành các dòng chữ để so trước/sau.</summary>
    private async Task<List<string>> SnapshotOf(Guid recipeId)
    {
        static string Line(params object?[] parts) =>
            string.Join(" | ", parts.Select(p => p switch
            {
                null => "<null>",
                byte[] bytes => Convert.ToBase64String(bytes),
                DateTime time => time.ToString("O", CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => p.ToString(),
            }));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var recipe = await db.Recipes.IgnoreQueryFilters().AsNoTracking().SingleAsync(r => r.Id == recipeId);
        var ingredients = await db.RecipeIngredients.IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.RecipeId == recipeId).OrderBy(i => i.OrderIndex).ThenBy(i => i.Id).ToListAsync();
        var steps = await db.RecipeSteps.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.RecipeId == recipeId).OrderBy(s => s.StepNumber).ThenBy(s => s.Id).ToListAsync();

        var lines = new List<string>
        {
            Line("recipe", recipe.Id, recipe.Title, recipe.Status, recipe.IsDeleted, recipe.UpdatedAt, recipe.RowVersion),
        };
        lines.AddRange(ingredients.Select(i => Line(
            "ingredient", i.Id, i.Name, i.Quantity, i.Unit, i.Notes, i.OrderIndex, i.IsDeleted, i.UpdatedAt, i.RowVersion)));
        lines.AddRange(steps.Select(s => Line(
            "step", s.Id, s.StepNumber, s.Title, s.Description, s.TimerMinutes, s.IsDeleted, s.UpdatedAt, s.RowVersion)));
        return lines;
    }

    private async Task<bool> AnyRowOfRecipeInDb(Guid recipeId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await db.Recipes.IgnoreQueryFilters().AnyAsync(r => r.Id == recipeId)
            || await db.RecipeIngredients.IgnoreQueryFilters().AnyAsync(i => i.RecipeId == recipeId)
            || await db.RecipeSteps.IgnoreQueryFilters().AnyAsync(s => s.RecipeId == recipeId);
    }

    // ------------------------------------------------------------------ (1) hai writer cùng RowVersion

    [Fact]
    public async Task Db_two_writers_loaded_with_same_RowVersion_exactly_one_saves_and_the_other_gets_concurrency_exception()
    {
        var (client, _, _, _) = await NewAuthorClient();
        var (recipeId, _, originalRowVersion) = await CreateRecipe(client, await AnyCategoryId());

        using var scopeA = _factory.Services.CreateScope();
        using var scopeB = _factory.Services.CreateScope();
        var repoA = scopeA.ServiceProvider.GetRequiredService<IRecipeRepository>();
        var repoB = scopeB.ServiceProvider.GetRequiredService<IRecipeRepository>();

        // Cả hai nạp xong rồi mới ghi -> cùng thấy một RowVersion.
        var a = (await repoA.FindForWriteAsync(recipeId, default))!;
        var b = (await repoB.FindForWriteAsync(recipeId, default))!;
        Assert.Equal(originalRowVersion, Convert.ToBase64String(a.RowVersion));
        Assert.Equal(originalRowVersion, Convert.ToBase64String(b.RowVersion));

        a.UpdateDetails("Tiêu đề của writer A", a.Slug, "Mô tả của writer A", a.Instructions,
            a.PrepTimeMinutes, a.CookTimeMinutes, 11, a.Difficulty, a.CategoryId);
        b.UpdateDetails("Tiêu đề của writer B", b.Slug, "Mô tả của writer B", b.Instructions,
            b.PrepTimeMinutes, b.CookTimeMinutes, 22, b.Difficulty, b.CategoryId);

        static Task<Exception?> TrySave(IRecipeRepository repo) => Task.Run(async () =>
        {
            try
            {
                await repo.SaveChangesAsync(default);
                return (Exception?)null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        });
        var errors = await Task.WhenAll(TrySave(repoA), TrySave(repoB));

        Assert.Single(errors, e => e is null);
        Assert.IsType<DbUpdateConcurrencyException>(errors.Single(e => e is not null));

        var winner = errors[0] is null ? a : b;
        using var verify = _factory.Services.CreateScope();
        var saved = await verify.ServiceProvider.GetRequiredService<AuthDbContext>()
            .Recipes.AsNoTracking().SingleAsync(r => r.Id == recipeId);

        // Không trộn: cả ba trường đều của bên thắng.
        Assert.Equal(winner.Title, saved.Title);
        Assert.Equal(winner.Description, saved.Description);
        Assert.Equal(winner.Servings, saved.Servings);
        Assert.Equal(Convert.ToBase64String(winner.RowVersion), Convert.ToBase64String(saved.RowVersion));
        Assert.NotEqual(originalRowVersion, Convert.ToBase64String(saved.RowVersion));
    }

    [Fact]
    public async Task Http_two_concurrent_PUTs_with_same_rowVersion_give_exactly_one_200_and_one_422()
    {
        var categoryId = await AnyCategoryId();
        var (clientA, _, _, _) = await NewAuthorClient();
        var (recipeId, _, originalRowVersion) = await CreateRecipe(clientA, categoryId);

        var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = clientA.DefaultRequestHeaders.Authorization;

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var responses = await Task.WhenAll(
            clientA.PutAsJsonAsync($"/api/v1/recipes/{recipeId}",
                Edit($"Writer A sửa qua HTTP {suffix}", "Mô tả của writer A", 11, categoryId, originalRowVersion)),
            clientB.PutAsJsonAsync($"/api/v1/recipes/{recipeId}",
                Edit($"Writer B sửa qua HTTP {suffix}", "Mô tả của writer B", 22, categoryId, originalRowVersion)));

        var summary = string.Join(" || ", await Task.WhenAll(
            responses.Select(async r => $"{(int)r.StatusCode}: {await r.Content.ReadAsStringAsync()}")));
        Assert.True(responses.Count(r => r.StatusCode == HttpStatusCode.OK) == 1, summary);
        Assert.True(responses.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity) == 1, summary);

        // Bên thua: kiểm trước (đã thấy bản mới) hoặc DB từ chối (nạp trước khi bên kia ghi) — tuỳ thời điểm.
        var loser = responses.Single(r => r.StatusCode == HttpStatusCode.UnprocessableEntity);
        Assert.Contains(await ErrorCodeOf(loser), new[] { "recipe.concurrency_conflict", "recipe.version_conflict" });

        var won = await DataOf(responses.Single(r => r.StatusCode == HttpStatusCode.OK));
        var detailRes = await clientA.GetAsync($"/api/v1/recipes/{won.GetProperty("slug").GetString()}");
        await AssertStatus(HttpStatusCode.OK, detailRes);
        var detail = await DataOf(detailRes);
        Assert.Equal(won.GetProperty("title").GetString(), detail.GetProperty("title").GetString());
        Assert.Equal(won.GetProperty("description").GetString(), detail.GetProperty("description").GetString());
        Assert.Equal(won.GetProperty("servings").GetInt32(), detail.GetProperty("servings").GetInt32());
        Assert.Equal(won.GetProperty("rowVersion").GetString(), detail.GetProperty("rowVersion").GetString());
        Assert.NotEqual(originalRowVersion, detail.GetProperty("rowVersion").GetString());
    }

    [Fact]
    public async Task Http_second_PUT_reusing_stale_rowVersion_returns_422_concurrency_conflict_and_keeps_first_write()
    {
        var categoryId = await AnyCategoryId();
        var (client, _, _, _) = await NewAuthorClient();
        var (recipeId, _, originalRowVersion) = await CreateRecipe(client, categoryId);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var first = await client.PutAsJsonAsync($"/api/v1/recipes/{recipeId}",
            Edit($"Lần sửa thứ nhất {suffix}", "Mô tả lần một", 6, categoryId, originalRowVersion));
        await AssertStatus(HttpStatusCode.OK, first);
        var afterFirst = await DataOf(first);

        var second = await client.PutAsJsonAsync($"/api/v1/recipes/{recipeId}",
            Edit($"Lần sửa thứ hai {suffix}", "Mô tả lần hai", 9, categoryId, originalRowVersion));
        await AssertStatus(HttpStatusCode.UnprocessableEntity, second);
        Assert.Equal("recipe.concurrency_conflict", await ErrorCodeOf(second));

        var detailRes = await client.GetAsync($"/api/v1/recipes/{afterFirst.GetProperty("slug").GetString()}");
        await AssertStatus(HttpStatusCode.OK, detailRes);
        var detail = await DataOf(detailRes);
        Assert.Equal($"Lần sửa thứ nhất {suffix}", detail.GetProperty("title").GetString());
        Assert.Equal("Mô tả lần một", detail.GetProperty("description").GetString());
        Assert.Equal(6, detail.GetProperty("servings").GetInt32());
        Assert.Equal(afterFirst.GetProperty("rowVersion").GetString(), detail.GetProperty("rowVersion").GetString());
    }

    // ------------------------------------------------------------------ (2) rollback trong transaction

    [Fact]
    public async Task Reorder_with_a_step_of_another_recipe_returns_400_and_rolls_back_the_temporary_renumbering()
    {
        var categoryId = await AnyCategoryId();
        var (client, _, _, _) = await NewAuthorClient();
        var (otherClient, _, _, _) = await NewAuthorClient();
        var (recipeId, _, steps) = await CreateRecipeWithChildren(client, categoryId);
        var (otherRecipeId, _, otherSteps) = await CreateRecipeWithChildren(otherClient, categoryId);
        var before = await StepsInDb(recipeId);
        var otherBefore = await StepsInDb(otherRecipeId);
        Assert.Equal(steps, before.Select(s => s.Id));
        Assert.Equal([1, 2, 3], before.Select(s => s.StepNumber));

        // Đủ số lượng, không trùng -> pha 1 (+10000) đã SaveChanges, tới id thứ ba ReorderSteps mới ném lỗi.
        var res = await client.PatchAsJsonAsync($"/api/v1/recipes/{recipeId}/steps/reorder",
            new ReorderStepsBody([steps[2], steps[0], otherSteps[0]]));
        await AssertRejected(HttpStatusCode.BadRequest, "STEP_NOT_FOUND", res);

        // So cả StepNumber, UpdatedAt và RowVersion: không còn giá trị +10000 hay số âm nào lọt xuống DB.
        Assert.Equal(before, await StepsInDb(recipeId));
        Assert.Equal(otherBefore, await StepsInDb(otherRecipeId));
    }

    [Fact]
    public async Task Reorder_with_a_duplicated_step_id_returns_400_and_rolls_back_the_temporary_renumbering()
    {
        var (client, _, _, _) = await NewAuthorClient();
        var (recipeId, _, steps) = await CreateRecipeWithChildren(client, await AnyCategoryId());
        var before = await StepsInDb(recipeId);
        Assert.Equal([1, 2, 3], before.Select(s => s.StepNumber));

        var res = await client.PatchAsJsonAsync($"/api/v1/recipes/{recipeId}/steps/reorder",
            new ReorderStepsBody([steps[2], steps[2], steps[0]]));
        await AssertRejected(HttpStatusCode.BadRequest, "STEP_ORDER_INVALID", res);

        Assert.Equal(before, await StepsInDb(recipeId));
    }

    [Fact]
    public async Task Transaction_failing_after_aggregate_with_children_was_saved_rolls_back_recipe_ingredient_and_step()
    {
        var categoryId = await AnyCategoryId();
        var (_, _, _, authorId) = await NewAuthorClient();
        var recipe = NewDraftWithChildren(categoryId, authorId);
        var boom = new InvalidOperationException("Lỗi giả lập sau khi đã SaveChanges");
        var visibleInsideTransaction = false;

        using (var scope = _factory.Services.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var repo = scope.ServiceProvider.GetRequiredService<IRecipeRepository>();
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => uow.ExecuteInTransactionAsync(async ct =>
            {
                repo.Add(recipe);
                await repo.SaveChangesAsync(ct);   // INSERT recipe + ingredient + step đã xuống DB, chưa commit
                visibleInsideTransaction = await db.RecipeSteps.AsNoTracking().AnyAsync(s => s.RecipeId == recipe.Id, ct);
                throw boom;
            }));
            Assert.Same(boom, thrown);
        }

        Assert.True(visibleInsideTransaction, "Dữ liệu phải đã xuống DB trong transaction thì rollback mới có ý nghĩa.");
        Assert.False(await AnyRowOfRecipeInDb(recipe.Id));
    }

    /// <summary>
    /// MÔ TẢ HÀNH VI HIỆN TẠI, không phải yêu cầu: EfUnitOfWork không hỗ trợ lồng transaction (không savepoint riêng,
    /// không dùng chung transaction). Production chưa có chỗ nào lồng. Nếu sau này hỗ trợ lồng thì test này PHẢI đổi.
    /// </summary>
    [Fact]
    public async Task CurrentBehavior_nested_ExecuteInTransactionAsync_is_not_supported_inner_call_throws_and_outer_rolls_back_everything()
    {
        var categoryId = await AnyCategoryId();
        var (_, _, _, authorId) = await NewAuthorClient();
        var recipe = NewDraftWithChildren(categoryId, authorId);
        var innerActionRan = false;

        using (var scope = _factory.Services.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var repo = scope.ServiceProvider.GetRequiredService<IRecipeRepository>();

            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.ExecuteInTransactionAsync(async ct =>
            {
                repo.Add(recipe);
                await repo.SaveChangesAsync(ct);
                await uow.ExecuteInTransactionAsync(_ =>
                {
                    innerActionRan = true;
                    return Task.CompletedTask;
                }, ct);
            }));
        }

        Assert.False(innerActionRan);
        Assert.False(await AnyRowOfRecipeInDb(recipe.Id));
    }

    // ------------------------------------------------------------------ (3) child của recipe khác

    [Fact]
    public async Task Child_ids_of_another_recipe_are_rejected_and_every_recipe_stays_untouched_in_db()
    {
        var categoryId = await AnyCategoryId();
        var (author1, _, _, _) = await NewAuthorClient();
        var (author2, _, _, _) = await NewAuthorClient();
        var a = await CreateRecipeWithChildren(author1, categoryId);
        var a2 = await CreateRecipeWithChildren(author1, categoryId);   // cùng tác giả, khác recipe
        var b = await CreateRecipeWithChildren(author2, categoryId);    // tác giả khác

        var beforeA = await SnapshotOf(a.RecipeId);
        var beforeA2 = await SnapshotOf(a2.RecipeId);
        var beforeB = await SnapshotOf(b.RecipeId);
        Assert.Equal(6, beforeB.Count);   // 1 recipe + 2 nguyên liệu + 3 bước

        foreach (var foreign in new[] { b, a2 })
        {
            var ingredientUrl = $"/api/v1/recipes/{a.RecipeId}/ingredients/{foreign.IngredientIds[0]}";
            var stepUrl = $"/api/v1/recipes/{a.RecipeId}/steps/{foreign.StepIds[1]}";

            await AssertRejected(HttpStatusCode.BadRequest, "INGREDIENT_NOT_FOUND",
                await author1.PutAsJsonAsync(ingredientUrl, new IngredientBody("Bị ghi đè", 9m, "kg", "không được phép")));
            await AssertRejected(HttpStatusCode.BadRequest, "STEP_NOT_FOUND",
                await author1.PutAsJsonAsync(stepUrl, new StepBody("Bị ghi đè", "không được phép", 99, null)));
            await AssertRejected(HttpStatusCode.NotFound, "ingredient.not_found", await author1.DeleteAsync(ingredientUrl));
            await AssertRejected(HttpStatusCode.NotFound, "step.not_found", await author1.DeleteAsync(stepUrl));
        }

        // Tên, số lượng, đơn vị, OrderIndex, StepNumber, IsDeleted, UpdatedAt, RowVersion của recipe lẫn từng con không đổi.
        Assert.Equal(beforeB, await SnapshotOf(b.RecipeId));
        Assert.Equal(beforeA2, await SnapshotOf(a2.RecipeId));
        Assert.Equal(beforeA, await SnapshotOf(a.RecipeId));
        Assert.Equal([1, 2, 3], (await StepsInDb(b.RecipeId)).Where(s => !s.IsDeleted).Select(s => s.StepNumber));
    }
}
