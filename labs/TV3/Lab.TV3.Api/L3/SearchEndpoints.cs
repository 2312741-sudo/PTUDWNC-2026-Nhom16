using System.Globalization;
using System.Text;
using Dapper;
using Lab.TV3.Api.L20;
using Microsoft.AspNetCore.OutputCaching;

namespace Lab.TV3.Api.L3;

public sealed record LabRecipeInput(string Title, string? Description, string? Ingredients, string? Category, string Status);
public sealed record SearchHit(Guid Id, string Slug, string Title, string? Category, float Rank);
public sealed record SearchPage(IReadOnlyList<SearchHit> Items, long TotalCount, int Page, int PageSize);

public sealed class SearchRow
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Category { get; set; }
    public float Rank { get; set; }
    public long Total { get; set; }
}

public sealed class LabRecipe
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Ingredients { get; set; }
    public string? Category { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
}

/// <summary>LAB L3 — FTS trigger/GIN/rank/AND/page (K11) + Redis cache/OutputCache/invalidation/fallback (K12).</summary>
public static class SearchEndpoints
{
    private const string Columns = "id, slug, title, description, ingredients, category, status, created_at";

    // plainto_tsquery = AND giữa các từ; unaccent -> "pho" khớp "Phở"; chỉ Published (không lộ Draft)
    public const string SearchSql = """
        SELECT r.id, r.slug, r.title, r.category, ts_rank(r.search_vector, query) AS rank, count(*) OVER () AS total
        FROM lab_recipes r, plainto_tsquery('simple', lab_unaccent(@q)) AS query
        WHERE r.status = 'Published' AND r.search_vector @@ query AND (@cat::text IS NULL OR r.category = @cat)
        ORDER BY rank DESC, r.created_at DESC, r.id
        LIMIT @size OFFSET @off
        """;

    public static void MapL3Search(this WebApplication app)
    {
        var g = app.MapGroup("/lab/l3");

        g.MapPost("/recipes", async (LabRecipeInput r, LabDb db, RecipeCache cache, IOutputCacheStore output, CancellationToken ct) =>
        {
            if (Validate(r) is { } err) return err;
            await using var c = await db.OpenAsync(ct);
            var recipe = await c.QuerySingleAsync<LabRecipe>($"""
                INSERT INTO lab_recipes (id, slug, title, description, ingredients, category, status)
                VALUES (@id, @slug, @Title, @Description, @Ingredients, @Category, @Status)
                RETURNING {Columns}
                """, new { id = Guid.NewGuid(), slug = Slugify(r.Title), r.Title, r.Description, r.Ingredients, r.Category, r.Status });
            await Invalidate(cache, output, recipe.Slug, ct);
            return Results.Created($"/lab/l3/recipes/{recipe.Slug}", new { data = recipe });
        }).RequireAuthorization();

        g.MapPut("/recipes/{id:guid}", async (Guid id, LabRecipeInput r, LabDb db, RecipeCache cache, IOutputCacheStore output, CancellationToken ct) =>
        {
            if (Validate(r) is { } err) return err;
            await using var c = await db.OpenAsync(ct);
            var recipe = await c.QuerySingleOrDefaultAsync<LabRecipe>($"""
                UPDATE lab_recipes SET title = @Title, description = @Description, ingredients = @Ingredients,
                       category = @Category, status = @Status
                WHERE id = @id RETURNING {Columns}
                """, new { id, r.Title, r.Description, r.Ingredients, r.Category, r.Status });
            if (recipe is null) return Http.Err(404, "RECIPE_NOT_FOUND", "Không tìm thấy công thức");
            await Invalidate(cache, output, recipe.Slug, ct); // sửa xong -> xoá cache ngay
            return Results.Ok(new { data = recipe });
        }).RequireAuthorization();

        g.MapGet("/search", async (string? q, string? category, int? page, int? pageSize, LabDb db, RecipeCache cache,
            HttpContext http, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(q)) return Http.Err(400, "VALIDATION", "Thiếu từ khoá q");
            LabMetrics.SearchRequests.Add(1, new KeyValuePair<string, object?>("page", "api"));
            var p = Math.Max(1, page ?? 1);
            var size = Math.Clamp(pageSize ?? 10, 1, 50);
            var key = await cache.SearchKeyAsync(q.Trim().ToLowerInvariant(), category, p, size);
            var (result, status) = await cache.GetOrLoadAsync(key, () => SearchDb(db, q.Trim(), category, p, size, ct), TimeSpan.FromMinutes(2));
            http.Response.Headers["X-Cache"] = status.ToString().ToUpperInvariant();
            return Results.Ok(new { data = result });
        });

        g.MapGet("/recipes/{slug}", async (string slug, LabDb db, RecipeCache cache, HttpContext http, CancellationToken ct) =>
        {
            var (recipe, status) = await cache.GetOrLoadAsync(RecipeCache.DetailKey(slug), async () =>
            {
                await using var c = await db.OpenAsync(ct);
                return await c.QuerySingleOrDefaultAsync<LabRecipe>(
                    $"SELECT {Columns} FROM lab_recipes WHERE slug = @slug AND status = 'Published'", new { slug });
            }, TimeSpan.FromMinutes(5));
            http.Response.Headers["X-Cache"] = status.ToString().ToUpperInvariant();
            return recipe is null ? Http.Err(404, "RECIPE_NOT_FOUND", "Không tìm thấy công thức") : Results.Ok(new { data = recipe });
        }).CacheOutput("lab-detail");

        // Minh chứng EXPLAIN (K11/PERF-004). forceIndex=true tắt seqscan để thấy GIN được dùng khi bảng còn nhỏ.
        g.MapGet("/explain", async (string q, bool? forceIndex, LabDb db, CancellationToken ct) =>
        {
            await using var c = await db.OpenAsync(ct);
            await using var tx = await c.BeginTransactionAsync(ct);
            if (forceIndex == true) await c.ExecuteAsync("SET LOCAL enable_seqscan = off", transaction: tx);
            var plan = (await c.QueryAsync<string>("EXPLAIN (ANALYZE, BUFFERS) " + SearchSql,
                new { q, cat = (string?)null, size = 10, off = 0 }, tx)).ToList();
            await tx.RollbackAsync(ct);
            return Results.Ok(new { data = plan });
        });
    }

    internal static async Task<SearchPage> SearchDb(LabDb db, string q, string? category, int page, int size, CancellationToken ct)
    {
        await using var c = await db.OpenAsync(ct);
        var rows = (await c.QueryAsync<SearchRow>(SearchSql,
            new { q, cat = string.IsNullOrWhiteSpace(category) ? null : category, size, off = (page - 1) * size })).ToList();
        return new SearchPage(rows.Select(r => new SearchHit(r.Id, r.Slug, r.Title, r.Category, r.Rank)).ToList(),
            rows.FirstOrDefault()?.Total ?? 0, page, size);
    }

    private static async Task Invalidate(RecipeCache cache, IOutputCacheStore output, string slug, CancellationToken ct)
    {
        await cache.InvalidateAsync(slug);
        await output.EvictByTagAsync(RecipeCache.OutputTag, ct);
    }

    private static IResult? Validate(LabRecipeInput r)
    {
        if (string.IsNullOrWhiteSpace(r.Title) || r.Title.Trim().Length is < 5 or > 200)
            return Http.Err(400, "VALIDATION", "Tiêu đề 5–200 ký tự");
        if (r.Status is not ("Draft" or "Published"))
            return Http.Err(400, "VALIDATION", "Status phải là Draft hoặc Published");
        return null;
    }

    /// <summary>"Phở bò Hà Nội" -> "pho-bo-ha-noi-3f2a1b".</summary>
    public static string Slugify(string title)
    {
        var s = title.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in s)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsAsciiLetterOrDigit(ch) ? ch : '-');
        }
        var slug = string.Join('-', sb.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        if (slug.Length > 60) slug = slug[..60].Trim('-');
        if (slug.Length == 0) slug = "cong-thuc";
        return $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";
    }
}
