using System.Collections.Concurrent;
using System.Data.Common;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// K22 / NFR-PERF-004 (TV3): không N+1 ở các query recipe chính — số lệnh SQL mỗi request KHÔNG tăng theo số nguyên liệu/bước/công thức.
/// So từng cặp nhỏ/lớn: chi tiết (1 vs 10 nguyên liệu), dashboard (1 vs 12 công thức), thêm/xoá nguyên liệu và bước.
/// Đặt biến môi trường K22_REPORT=&lt;đường dẫn .md&gt; để ghi báo cáo: số lệnh, thời gian từng lệnh, lệnh > 100 ms, EXPLAIN (ANALYZE, BUFFERS) các SELECT.
/// </summary>
public sealed class RecipeQueryPerformanceTests : IClassFixture<ApiFactory>, IDisposable
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly SqlCapture _capture = new();
    private readonly WebApplicationFactory<Program> _app;

    public RecipeQueryPerformanceTests(ApiFactory factory)
    {
        factory.EnsureMigrated();
        _app = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.ConfigureDbContext<AuthDbContext>(o => o.AddInterceptors(_capture))));
    }

    public void Dispose() => _app.Dispose();

    // ------------------------------------------------------------------ bắt lệnh SQL

    private sealed record SqlCommandRecord(string Text, IReadOnlyList<(string Name, NpgsqlDbType Type, object? Value)> Parameters, double Ms);

    private sealed class SqlCapture : DbCommandInterceptor
    {
        private readonly ConcurrentQueue<SqlCommandRecord> _items = new();
        private volatile bool _on;

        public void Start() { _items.Clear(); _on = true; }
        public List<SqlCommandRecord> Stop() { _on = false; return [.. _items]; }

        private void Add(DbCommand c, CommandExecutedEventData e)
        {
            if (!_on) return;
            var ps = c.Parameters.OfType<NpgsqlParameter>().Select(p => (p.ParameterName, p.NpgsqlDbType, p.Value)).ToList();
            _items.Enqueue(new SqlCommandRecord(c.CommandText, ps, e.Duration.TotalMilliseconds));
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand c, CommandExecutedEventData e, DbDataReader r, CancellationToken ct = default)
        { Add(c, e); return ValueTask.FromResult(r); }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand c, CommandExecutedEventData e, int r, CancellationToken ct = default)
        { Add(c, e); return ValueTask.FromResult(r); }

        public override ValueTask<object?> ScalarExecutedAsync(DbCommand c, CommandExecutedEventData e, object? r, CancellationToken ct = default)
        { Add(c, e); return ValueTask.FromResult(r); }
    }

    private async Task<(HttpResponseMessage Res, List<SqlCommandRecord> Sql)> Measure(Func<Task<HttpResponseMessage>> call)
    {
        _capture.Start();
        var res = await call();
        return (res, _capture.Stop());
    }

    // ------------------------------------------------------------------ dữ liệu

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
        var client = _app.CreateClient();
        var cmd = new RegisterCommand($"tv3-k22-{Guid.NewGuid():N}@example.test", "Recipe-Flow9!", "Bếp K22 TV3");
        var res = await client.PostAsJsonAsync("/api/v1/auth/register", cmd);
        await AssertStatus(HttpStatusCode.Created, res);
        var auth = (await res.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, cmd.Email, cmd.Password);
    }

    /// <summary>Lấy 1 danh mục có sẵn; DB rỗng (CI) thì tạo bằng tài khoản được nâng Admin.</summary>
    private async Task<Guid> AnyCategoryId()
    {
        var list = await DataOf(await _app.CreateClient().GetAsync("/api/v1/categories"));
        var items = list.ValueKind == JsonValueKind.Array ? list
            : list.TryGetProperty("items", out var it) ? it : default;
        if (items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
            return items[0].GetProperty("id").GetGuid();

        var (client, email, password) = await NewAuthorClient();
        using (var scope = _app.Services.CreateScope())
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
            "Name" => (object?)$"Danh mục K22 {Guid.NewGuid():N}"[..28],
            "Description" => "Tạo bởi RecipeQueryPerformanceTests",
            _ => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null,
        }).ToArray();
        var created = await client.PostAsJsonAsync("/api/v1/categories", (CreateCategoryCommand)ctor.Invoke(args));
        await AssertStatus(HttpStatusCode.Created, created);
        return (await DataOf(created)).GetProperty("id").GetGuid();
    }

    private sealed record Seeded(Guid Id, string Slug, List<Guid> IngredientIds, List<Guid> StepIds);

    private static async Task<Seeded> SeedRecipe(HttpClient client, Guid categoryId, int ingredients, int steps, bool publish)
    {
        var res = await client.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeCommand(
            $"K22 {ingredients}nl {Guid.NewGuid():N}"[..30], "Đo số câu SQL", null, 10, 20, 2, RecipeDifficulty.Easy, categoryId, null));
        await AssertStatus(HttpStatusCode.Created, res);
        var data = await DataOf(res);
        var id = data.GetProperty("id").GetGuid();
        var ingIds = new List<Guid>();
        for (var i = 1; i <= ingredients; i++)
        {
            var r = await client.PostAsJsonAsync($"/api/v1/recipes/{id}/ingredients", new IngredientBody($"Nguyên liệu {i}", i, "g", null));
            await AssertStatus(HttpStatusCode.Created, r);
            ingIds.Add((await DataOf(r)).GetProperty("id").GetGuid());
        }
        var stepIds = new List<Guid>();
        for (var i = 1; i <= steps; i++)
        {
            var r = await client.PostAsJsonAsync($"/api/v1/recipes/{id}/steps", new StepBody($"Bước {i}", $"Mô tả {i}", null, null));
            await AssertStatus(HttpStatusCode.Created, r);
            stepIds.Add((await DataOf(r)).GetProperty("id").GetGuid());
        }
        if (publish) await AssertStatus(HttpStatusCode.OK, await client.PatchAsync($"/api/v1/recipes/{id}/publish", null));
        return new Seeded(id, data.GetProperty("slug").GetString()!, ingIds, stepIds);
    }

    // ------------------------------------------------------------------ test

    [Fact]
    public async Task Sql_command_count_per_request_does_not_grow_with_children_or_recipe_count()
    {
        var categoryId = await AnyCategoryId();
        var (author, _, _) = await NewAuthorClient();
        var small = await SeedRecipe(author, categoryId, ingredients: 2, steps: 2, publish: true);
        var large = await SeedRecipe(author, categoryId, ingredients: 10, steps: 6, publish: true);
        var (oneRecipeAuthor, _, _) = await NewAuthorClient();
        await SeedRecipe(oneRecipeAuthor, categoryId, 1, 1, publish: false);
        var (manyRecipeAuthor, _, _) = await NewAuthorClient();
        for (var i = 0; i < 12; i++) await SeedRecipe(manyRecipeAuthor, categoryId, 1, 1, publish: false);

        var anonymous = _app.CreateClient();
        var report = new List<(string Name, HttpResponseMessage Res, List<SqlCommandRecord> Sql)>();
        async Task<List<SqlCommandRecord>> Run(string name, Func<Task<HttpResponseMessage>> call, HttpStatusCode expected)
        {
            var (res, sql) = await Measure(call);
            await AssertStatus(expected, res);
            report.Add((name, res, sql));
            return sql;
        }

        var detailSmall = await Run("GET chi tiết (2 nguyên liệu, 2 bước)", () => anonymous.GetAsync($"/api/v1/recipes/{small.Slug}"), HttpStatusCode.OK);
        var detailLarge = await Run("GET chi tiết (10 nguyên liệu, 6 bước)", () => anonymous.GetAsync($"/api/v1/recipes/{large.Slug}"), HttpStatusCode.OK);
        var listOne = await Run("GET dashboard /me/recipes (1 công thức)", () => oneRecipeAuthor.GetAsync("/api/v1/me/recipes?page=1&pageSize=20"), HttpStatusCode.OK);
        var listMany = await Run("GET dashboard /me/recipes (12 công thức)", () => manyRecipeAuthor.GetAsync("/api/v1/me/recipes?page=1&pageSize=20"), HttpStatusCode.OK);
        var countsOne = await Run("GET dashboard /me/recipes/counts (1 công thức)", () => oneRecipeAuthor.GetAsync("/api/v1/me/recipes/counts"), HttpStatusCode.OK);
        var countsMany = await Run("GET dashboard /me/recipes/counts (12 công thức)", () => manyRecipeAuthor.GetAsync("/api/v1/me/recipes/counts"), HttpStatusCode.OK);
        var addSmall = await Run("POST nguyên liệu (đang có 2)", () => author.PostAsJsonAsync($"/api/v1/recipes/{small.Id}/ingredients", new IngredientBody("Muối", 1m, "g", null)), HttpStatusCode.Created);
        var addLarge = await Run("POST nguyên liệu (đang có 10)", () => author.PostAsJsonAsync($"/api/v1/recipes/{large.Id}/ingredients", new IngredientBody("Muối", 1m, "g", null)), HttpStatusCode.Created);
        var delSmall = await Run("DELETE nguyên liệu đầu (còn 2 phải đánh lại thứ tự)", () => author.DeleteAsync($"/api/v1/recipes/{small.Id}/ingredients/{small.IngredientIds[0]}"), HttpStatusCode.NoContent);
        var delLarge = await Run("DELETE nguyên liệu đầu (còn 10 phải đánh lại thứ tự)", () => author.DeleteAsync($"/api/v1/recipes/{large.Id}/ingredients/{large.IngredientIds[0]}"), HttpStatusCode.NoContent);
        var stepSmall = await Run("POST bước (đang có 2)", () => author.PostAsJsonAsync($"/api/v1/recipes/{small.Id}/steps", new StepBody("Thêm", "Bước thêm", null, null)), HttpStatusCode.Created);
        var stepLarge = await Run("POST bước (đang có 6)", () => author.PostAsJsonAsync($"/api/v1/recipes/{large.Id}/steps", new StepBody("Thêm", "Bước thêm", null, null)), HttpStatusCode.Created);
        var delStepSmall = await Run("DELETE bước đầu (còn 2 phải đánh lại số)", () => author.DeleteAsync($"/api/v1/recipes/{small.Id}/steps/{small.StepIds[0]}"), HttpStatusCode.NoContent);
        var delStepLarge = await Run("DELETE bước đầu (còn 6 phải đánh lại số)", () => author.DeleteAsync($"/api/v1/recipes/{large.Id}/steps/{large.StepIds[0]}"), HttpStatusCode.NoContent);

        var path = Environment.GetEnvironmentVariable("K22_REPORT");
        if (!string.IsNullOrWhiteSpace(path)) await WriteReport(path, report);

        // Không N+1: số lệnh SQL (round-trip) bằng nhau giữa trường hợp nhỏ và lớn
        Assert.Equal(detailSmall.Count, detailLarge.Count);
        Assert.Equal(listOne.Count, listMany.Count);
        Assert.Equal(countsOne.Count, countsMany.Count);
        Assert.Equal(addSmall.Count, addLarge.Count);
        Assert.Equal(delSmall.Count, delLarge.Count);
        Assert.Equal(stepSmall.Count, stepLarge.Count);
        Assert.Equal(delStepSmall.Count, delStepLarge.Count);
        Assert.InRange(detailLarge.Count, 1, 5);   // split query: recipe + 3 collection + tên tác giả/danh mục
        Assert.InRange(listMany.Count, 1, 3);
    }

    [Fact]
    public async Task Detail_loads_each_child_collection_in_its_own_query_so_rows_are_not_ingredients_times_steps()
    {
        var categoryId = await AnyCategoryId();
        var (author, _, _) = await NewAuthorClient();
        var recipe = await SeedRecipe(author, categoryId, ingredients: 10, steps: 6, publish: true);

        var (res, sql) = await Measure(() => _app.CreateClient().GetAsync($"/api/v1/recipes/{recipe.Slug}"));
        await AssertStatus(HttpStatusCode.OK, res);

        // Một câu JOIN cả nguyên liệu lẫn bước trả về 10 x 6 = 60 dòng (bùng nổ tích Descartes, K22_sql_explain_chay_that.md)
        var texts = sql.Select(c => c.Text).ToList();
        Assert.DoesNotContain(texts, t => t.Contains("\"RecipeIngredients\"") && t.Contains("\"RecipeSteps\""));
        Assert.Single(texts, t => t.Contains("\"RecipeIngredients\""));
        Assert.Single(texts, t => t.Contains("\"RecipeSteps\""));
        Assert.Single(texts, t => t.Contains("\"RecipeImages\""));

        var detail = await DataOf(res);
        Assert.Equal(10, detail.GetProperty("ingredients").GetArrayLength());
        Assert.Equal(6, detail.GetProperty("steps").GetArrayLength());
    }

    [Fact]
    public async Task Write_path_loads_ingredients_and_steps_in_separate_queries_and_still_saves_correctly()
    {
        var categoryId = await AnyCategoryId();
        var (author, _, _) = await NewAuthorClient();
        var recipe = await SeedRecipe(author, categoryId, ingredients: 10, steps: 6, publish: false);

        // Nạp aggregate để ghi (FindForWriteAsync) rồi INSERT: câu nạp không được JOIN nguyên liệu x bước (10 x 6 = 60 dòng)
        var (res, sql) = await Measure(() => author.PostAsJsonAsync($"/api/v1/recipes/{recipe.Id}/ingredients", new IngredientBody("Muối", 1m, "g", null)));
        await AssertStatus(HttpStatusCode.Created, res);
        var selects = sql.Where(c => c.Text.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)).Select(c => c.Text).ToList();
        Assert.DoesNotContain(selects, t => t.Contains("\"RecipeIngredients\"") && t.Contains("\"RecipeSteps\""));
        Assert.Single(selects, t => t.Contains("\"RecipeIngredients\""));
        Assert.Single(selects, t => t.Contains("\"RecipeSteps\""));
        Assert.Equal(10, (await DataOf(res)).GetProperty("orderIndex").GetInt32());

        // Ghi vẫn đúng: xoá bước giữa đánh lại số 1..N, sửa nguyên liệu giữ vị trí
        await AssertStatus(HttpStatusCode.NoContent, await author.DeleteAsync($"/api/v1/recipes/{recipe.Id}/steps/{recipe.StepIds[2]}"));
        var put = await author.PutAsJsonAsync($"/api/v1/recipes/{recipe.Id}/ingredients/{recipe.IngredientIds[3]}", new IngredientBody("Đổi tên", 2m, "g", null));
        await AssertStatus(HttpStatusCode.OK, put);
        Assert.Equal(3, (await DataOf(put)).GetProperty("orderIndex").GetInt32());

        var detail = await DataOf(await author.GetAsync($"/api/v1/recipes/{recipe.Slug}"));
        Assert.Equal([1, 2, 3, 4, 5], detail.GetProperty("steps").EnumerateArray().Select(s => s.GetProperty("stepNumber").GetInt32()).ToArray());
        Assert.Equal(Enumerable.Range(0, 11).ToArray(), detail.GetProperty("ingredients").EnumerateArray().Select(i => i.GetProperty("orderIndex").GetInt32()).ToArray());
    }

    // ------------------------------------------------------------------ báo cáo (chỉ khi đặt K22_REPORT)

    private static string Statements(string sql) =>
        sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length.ToString(CultureInfo.InvariantCulture);

    private async Task WriteReport(string path, List<(string Name, HttpResponseMessage Res, List<SqlCommandRecord> Sql)> report)
    {
        string cs;
        using (var scope = _app.Services.CreateScope())
            cs = scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.GetConnectionString()!;
        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"# K22 — đếm câu SQL mỗi request + EXPLAIN (ANALYZE, BUFFERS) — {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        await using (var cmd = new NpgsqlCommand("""SELECT (SELECT count(*) FROM "Recipes"), (SELECT count(*) FROM "RecipeIngredients"), (SELECT count(*) FROM "RecipeSteps"), (SELECT count(*) FROM "AspNetUsers")""", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            await r.ReadAsync();
            sb.AppendLine(CultureInfo.InvariantCulture, $"Dữ liệu trong DB lúc đo: Recipes={r.GetInt64(0)}, RecipeIngredients={r.GetInt64(1)}, RecipeSteps={r.GetInt64(2)}, Users={r.GetInt64(3)}");
        }
        sb.AppendLine();
        sb.AppendLine("| Request | HTTP | Số lệnh SQL (round-trip) | Số câu trong các lệnh | Tổng ms SQL | Lệnh chậm nhất ms | > 100 ms |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var (name, res, sql) in report)
        {
            var slow = sql.Count(x => x.Ms > 100);
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"| {name} | {(int)res.StatusCode} | {sql.Count} | {string.Join(" + ", sql.Select(x => Statements(x.Text)))} | {sql.Sum(x => x.Ms):F1} | {(sql.Count == 0 ? 0 : sql.Max(x => x.Ms)):F1} | {slow} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Câu SQL và EXPLAIN (ANALYZE, BUFFERS) — bản lớn của mỗi cặp, chỉ các SELECT");
        var seen = new HashSet<string>();
        foreach (var (name, _, sql) in report.Where(x => x.Name.Contains("10") || x.Name.Contains("12") || x.Name.Contains("đang có 6") || x.Name.Contains("còn 6")))
        {
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"### {name}");
            foreach (var c in sql)
            {
                var text = c.Text.Trim();
                sb.AppendLine(CultureInfo.InvariantCulture, $"- {c.Ms:F1} ms, {Statements(text)} câu:");
                sb.AppendLine("```sql");
                sb.AppendLine(text.Length > 1500 ? text[..1500] + " …" : text);
                sb.AppendLine("```");
                if (!text.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) || text.Count(ch => ch == ';') > 1 || !seen.Add(text)) continue;
                await using var explain = new NpgsqlCommand("EXPLAIN (ANALYZE, BUFFERS) " + text.TrimEnd(';'), conn);
                foreach (var (pName, pType, pValue) in c.Parameters)
                    explain.Parameters.Add(new NpgsqlParameter(pName, pType) { Value = pValue ?? DBNull.Value });
                sb.AppendLine("```text");
                await using (var r = await explain.ExecuteReaderAsync())
                    while (await r.ReadAsync()) sb.AppendLine(r.GetString(0));
                sb.AppendLine("```");
            }
        }
        await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(false));
    }
}
