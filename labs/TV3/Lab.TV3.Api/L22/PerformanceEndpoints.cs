using System.Diagnostics;
using System.Text.Json;
using Dapper;

namespace Lab.TV3.Api.L22;

/// <summary>Bộ đếm SQL của MỘT request (đặt vào AsyncLocal để chỉ đếm câu SQL chạy trong request đó).</summary>
public sealed class SqlScope(ILogger logger, double thresholdMs)
{
    public int Count;
    public ILogger Logger { get; } = logger;
    public double ThresholdMs { get; } = thresholdMs;
}

/// <summary>
/// LAB K22 — nghe Activity "Npgsql" (Npgsql tự phát 1 Activity cho mỗi câu lệnh): đếm số câu SQL mỗi request
/// (header X-Sql-Count) để chứng minh không N+1, và log Warning "SLOW_SQL" khi câu nào vượt ngưỡng (NFR-PERF-004, mặc định 100 ms).
/// </summary>
public static class SqlMonitor
{
    private static readonly AsyncLocal<SqlScope?> Current = new();

    // Một listener cho cả process (nhiều app test trong cùng process không bị đếm trùng); app nào dùng scope của app đó
    private static readonly ActivityListener Listener = CreateListener();

    private static ActivityListener CreateListener()
    {
        var l = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Npgsql",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = OnStopped,
        };
        ActivitySource.AddActivityListener(l);
        return l;
    }

    private static void OnStopped(Activity a)
    {
        if (Current.Value is not { } scope) return; // SQL ngoài request (startup, Hangfire) -> bỏ qua
        var sql = (a.GetTagItem("db.query.text") ?? a.GetTagItem("db.statement"))?.ToString();
        if (sql is null) return; // span không phải câu lệnh (vd. mở kết nối vật lý)
        Interlocked.Increment(ref scope.Count);
        var ms = a.Duration.TotalMilliseconds;
        if (ms > scope.ThresholdMs)
            scope.Logger.LogWarning("SLOW_SQL {ElapsedMs} ms (ngưỡng {ThresholdMs} ms): {Sql}",
                Math.Round(ms, 1), scope.ThresholdMs, sql.Length > 300 ? sql[..300] + "…" : sql);
    }

    public static void UseL22SqlMonitor(this WebApplication app)
    {
        _ = Listener;
        var threshold = app.Configuration.GetValue("Perf:SlowQueryMs", 100.0);
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Lab.TV3.Sql");
        app.Use(async (ctx, next) =>
        {
            var scope = new SqlScope(logger, threshold);
            Current.Value = scope;
            ctx.Response.OnStarting(() =>
            {
                ctx.Response.Headers["X-Sql-Count"] = scope.Count.ToString();
                return Task.CompletedTask;
            });
            await next(ctx);
        });
    }
}

public sealed class ListRow
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Images { get; set; } = "[]";
}

public sealed class ImageRow
{
    public Guid Id { get; set; }
    public string ObjectKey { get; set; } = "";
    public string ContentType { get; set; } = "";
}

public static class PerformanceEndpoints
{
    // 1 câu duy nhất: lấy trang công thức rồi JOIN + json_agg ảnh, GROUP BY theo công thức
    public const string ListSql = """
        WITH r AS (
            SELECT id, slug, title, created_at FROM lab_recipes
            WHERE status = 'Published' ORDER BY created_at DESC, id LIMIT @take)
        SELECT r.id, r.slug, r.title, r.created_at,
               COALESCE(json_agg(json_build_object('id', x.id, 'objectKey', x.object_key, 'contentType', x.content_type)
                        ORDER BY x.created_at) FILTER (WHERE x.id IS NOT NULL), '[]')::text AS images
        FROM r LEFT JOIN lab_images x ON x.recipe_id = r.id
        GROUP BY r.id, r.slug, r.title, r.created_at
        ORDER BY r.created_at DESC, r.id
        """;

    public static void MapL22Performance(this WebApplication app)
    {
        var g = app.MapGroup("/lab/l22");

        g.MapGet("/recipes", async (int? take, string? mode, LabDb db, CancellationToken ct) =>
        {
            var n = Math.Clamp(take ?? 20, 1, 100);
            await using var c = await db.OpenAsync(ct);
            if (mode == "naive")
            {
                // CỐ Ý N+1 để so sánh: 1 câu danh sách + 1 câu ảnh cho từng công thức
                var recipes = (await c.QueryAsync<ListRow>("""
                    SELECT id, slug, title, created_at FROM lab_recipes
                    WHERE status = 'Published' ORDER BY created_at DESC, id LIMIT @n
                    """, new { n })).ToList();
                var items = new List<object>();
                foreach (var r in recipes)
                {
                    var images = await c.QueryAsync<ImageRow>(
                        "SELECT id, object_key, content_type FROM lab_images WHERE recipe_id = @id ORDER BY created_at", new { id = r.Id });
                    items.Add(new { r.Id, r.Slug, r.Title, r.CreatedAt, images });
                }
                return Results.Ok(new { data = items });
            }

            var rows = await c.QueryAsync<ListRow>(ListSql, new { take = n });
            return Results.Ok(new
            {
                data = rows.Select(r => new { r.Id, r.Slug, r.Title, r.CreatedAt, images = JsonDocument.Parse(r.Images).RootElement.Clone() }),
            });
        });

        g.MapGet("/explain", async (int? take, LabDb db, CancellationToken ct) =>
        {
            await using var c = await db.OpenAsync(ct);
            var plan = await c.QueryAsync<string>("EXPLAIN (ANALYZE, BUFFERS) " + ListSql, new { take = Math.Clamp(take ?? 20, 1, 100) });
            return Results.Ok(new { data = plan });
        });
    }
}
