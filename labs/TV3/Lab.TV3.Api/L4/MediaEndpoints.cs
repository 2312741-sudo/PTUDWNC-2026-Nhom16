using Dapper;
using Hangfire;
using MimeKit;

namespace Lab.TV3.Api.L4;

public sealed record WelcomeRequest(string Email, string Name);
public sealed record ReminderRequest(string Email, int DelaySeconds);

/// <summary>LAB L4 — upload/delete 4 MIME + biên 5 MiB (K13), Hangfire fire-and-forget/delayed/recurring (K14), SMTP/resize/XML (K15).</summary>
public static class MediaEndpoints
{
    public static void MapL4Media(this WebApplication app)
    {
        var g = app.MapGroup("/lab/l4");

        g.MapPost("/recipes/{recipeId:guid}/images", async (Guid recipeId, IFormFile file, ObjectStorage storage, LabDb db,
            IBackgroundJobClient jobs, CancellationToken ct) =>
        {
            if (!ImageValidator.SizeOk(file.Length))
                return Http.Err(400, "FILE_SIZE_INVALID", "Ảnh phải lớn hơn 0 và tối đa 5 MiB");

            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            var kind = ImageValidator.Detect(ms.GetBuffer().AsSpan(0, (int)Math.Min(ms.Length, 16)));
            if (kind is null)
                return Http.Err(422, "FILE_TYPE_INVALID", "Nội dung file không phải JPEG, PNG, WebP hoặc AVIF");

            var id = Guid.NewGuid();
            var key = $"recipes/{recipeId}/{id:N}{kind.Value.Ext}"; // tên theo GUID, đuôi theo nội dung thật
            ms.Position = 0;
            await storage.PutAsync(key, ms, kind.Value.Mime, ct);
            try
            {
                await using var c = await db.OpenAsync(ct);
                await c.ExecuteAsync("""
                    INSERT INTO lab_images (id, recipe_id, object_key, content_type, size_bytes)
                    VALUES (@id, @recipeId, @key, @mime, @size)
                    """, new { id, recipeId, key, mime = kind.Value.Mime, size = file.Length });
            }
            catch
            {
                await storage.DeleteAsync(key, CancellationToken.None); // DB lỗi -> không để object mồ côi
                throw;
            }
            var jobId = jobs.Enqueue<ResizeJob>(j => j.RunAsync(id, CancellationToken.None));
            return Results.Created($"/lab/l4/images/{id}", new
            {
                data = new { id, recipeId, key, contentType = kind.Value.Mime, size = file.Length, resizeJobId = jobId },
            });
        }).DisableAntiforgery().RequireAuthorization();

        g.MapDelete("/images/{id:guid}", async (Guid id, ObjectStorage storage, LabDb db, CancellationToken ct) =>
        {
            await using var c = await db.OpenAsync(ct);
            var img = await c.QuerySingleOrDefaultAsync<LabImage>("SELECT * FROM lab_images WHERE id = @id", new { id });
            if (img is null) return Http.Err(404, "IMAGE_NOT_FOUND", "Không tìm thấy ảnh");
            // Xoá object trước; lỗi thì metadata còn nguyên để xoá lại -> không có object mồ côi
            foreach (var key in new[] { img.ObjectKey, img.MediumKey, img.ThumbKey }.OfType<string>())
                await storage.DeleteAsync(key, ct);
            await c.ExecuteAsync("DELETE FROM lab_images WHERE id = @id", new { id });
            return Results.NoContent();
        }).RequireAuthorization();

        g.MapPost("/welcome", (WelcomeRequest r, IBackgroundJobClient jobs) =>
        {
            if (!MailboxAddress.TryParse(r.Email, out _)) return Http.Err(400, "VALIDATION", "Email không hợp lệ");
            var html = $"<h1>Xin chào {System.Net.WebUtility.HtmlEncode(r.Name)}!</h1><p>Chào mừng bạn đến Culinary Blog (LAB TV3).</p>";
            var jobId = jobs.Enqueue<EmailJob>(j => j.SendAsync(r.Email, "Chào mừng đến Culinary Blog", html, CancellationToken.None));
            return Results.Accepted($"/lab/hangfire/jobs/details/{jobId}", new { data = new { jobId } });
        });

        g.MapPost("/reminder", (ReminderRequest r, IBackgroundJobClient jobs) =>
        {
            if (!MailboxAddress.TryParse(r.Email, out _)) return Http.Err(400, "VALIDATION", "Email không hợp lệ");
            if (r.DelaySeconds is < 1 or > 7 * 24 * 3600) return Http.Err(400, "VALIDATION", "DelaySeconds 1..604800");
            var jobId = jobs.Schedule<EmailJob>(j => j.SendAsync(r.Email, "Nhắc: hoàn thiện công thức nháp",
                "<p>Bạn còn công thức nháp chưa xuất bản.</p>", CancellationToken.None), TimeSpan.FromSeconds(r.DelaySeconds));
            return Results.Accepted($"/lab/hangfire/jobs/details/{jobId}", new { data = new { jobId } });
        });

        g.MapPost("/sitemap/run", (IRecurringJobManager recurring) =>
        {
            recurring.Trigger(SitemapJob.RecurringId);
            return Results.Accepted();
        });

        g.MapGet("/sitemap.xml", async (LabDb db, CancellationToken ct) =>
        {
            await using var c = await db.OpenAsync(ct);
            var xml = await c.QuerySingleOrDefaultAsync<string>("SELECT xml FROM lab_sitemaps ORDER BY id DESC LIMIT 1");
            return xml is null ? Http.Err(404, "SITEMAP_NOT_READY", "Chưa sinh sitemap") : Results.Content(xml, "application/xml; charset=utf-8");
        });
    }
}