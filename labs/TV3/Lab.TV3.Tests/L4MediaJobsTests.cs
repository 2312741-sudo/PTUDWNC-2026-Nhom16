using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Xml.Linq;
using Hangfire;
using Hangfire.Storage;
using Lab.TV3.Api.L4;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using static Lab.TV3.Tests.LabHttp;

namespace Lab.TV3.Tests;

/// <summary>LAB L4 — K13 magic bytes/biên 5 MiB/upload-delete, K14 Hangfire, K15 SMTP/resize/XML.</summary>
[Collection("lab")]
public sealed class L4MediaJobsTests(LabFactory f)
{
    // ---------------------------------------------------------------- unit: magic bytes + biên kích thước
    public static TheoryData<string, byte[]> Valid => new()
    {
        { "image/jpeg", [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0, 1] },
        { "image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D] },
        { "image/webp", [.. "RIFF"u8, 0x24, 0, 0, 0, .. "WEBP"u8] },
        { "image/avif", [0, 0, 0, 0x20, .. "ftyp"u8, .. "avif"u8] },
    };

    [Theory]
    [MemberData(nameof(Valid))]
    public void Detects_all_four_formats_by_content(string mime, byte[] header) =>
        Assert.Equal(mime, ImageValidator.Detect(header)?.Mime);

    public static TheoryData<byte[]> Invalid => new()
    {
        { "Xin chao, day la file text"u8.ToArray() },
        { new byte[] { 0x89, 0x50, 0x4E, 0x47 } },               // PNG bị cắt cụt
        { [.. "RIFF"u8, 0x24, 0, 0, 0, .. "WAVE"u8] },            // RIFF nhưng là âm thanh
        { [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37] },     // %PDF-1.7
        { Array.Empty<byte>() },
    };

    [Theory]
    [MemberData(nameof(Invalid))]
    public void Rejects_non_images_and_truncated_headers(byte[] header) => Assert.Null(ImageValidator.Detect(header));

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5 * 1024 * 1024, true)]      // đúng 5 MiB: nhận
    [InlineData(5 * 1024 * 1024 + 1, false)] // vượt 1 byte: từ chối
    public void Size_boundary_is_five_mebibytes(long size, bool ok) => Assert.Equal(ok, ImageValidator.SizeOk(size));

    // ---------------------------------------------------------------- API: kiểm tra trước khi chạm MinIO

    private static MultipartFormDataContent Form(byte[] bytes, string fileName, string contentType)
    {
        var part = new ByteArrayContent(bytes);
        part.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { part, "file", fileName } };
    }

    [Fact]
    public async Task Upload_over_5MiB_is_rejected_400()
    {
        var (c, _, _) = await AuthorAsync(f);
        var big = new byte[5 * 1024 * 1024 + 1];
        big[0] = 0xFF; big[1] = 0xD8; big[2] = 0xFF;
        var res = await c.PostAsync($"/lab/l4/recipes/{Guid.NewGuid()}/images", Form(big, "big.jpg", "image/jpeg"));
        await Expect(HttpStatusCode.BadRequest, res);
        Assert.Equal("FILE_SIZE_INVALID", await Code(res));
    }

    [Fact]
    public async Task Renamed_text_file_with_image_content_type_is_rejected_422()
    {
        var (c, _, _) = await AuthorAsync(f);
        var res = await c.PostAsync($"/lab/l4/recipes/{Guid.NewGuid()}/images",
            Form("<script>alert(1)</script>"u8.ToArray(), "meo.png", "image/png"));
        await Expect(HttpStatusCode.UnprocessableEntity, res);
        Assert.Equal("FILE_TYPE_INVALID", await Code(res));
    }

    [Fact]
    public async Task Upload_and_delete_require_authentication()
    {
        var anon = f.CreateClient();
        await Expect(HttpStatusCode.Unauthorized, await anon.PostAsync($"/lab/l4/recipes/{Guid.NewGuid()}/images",
            Form([0xFF, 0xD8, 0xFF, 0xE0], "a.jpg", "image/jpeg")));
        await Expect(HttpStatusCode.Unauthorized, await anon.DeleteAsync($"/lab/l4/images/{Guid.NewGuid()}"));
    }

    // ---------------------------------------------------------------- Hangfire (lưu trên PostgreSQL)

    [Fact]
    public async Task Welcome_email_is_enqueued_and_persisted()
    {
        var res = await f.CreateClient().PostAsJsonAsync("/lab/l4/welcome", new { email = NewEmail("mail"), name = "Trung" });
        await Expect(HttpStatusCode.Accepted, res);
        var jobId = (await Data(res)).GetProperty("jobId").GetString()!;
        var details = f.Services.GetRequiredService<JobStorage>().GetMonitoringApi().JobDetails(jobId);
        Assert.NotNull(details);
        Assert.Equal(nameof(EmailJob.SendAsync), details.Job.Method.Name);
    }

    [Fact]
    public async Task Delayed_job_is_scheduled_not_run_immediately()
    {
        var res = await f.CreateClient().PostAsJsonAsync("/lab/l4/reminder", new { email = NewEmail("rem"), delaySeconds = 3600 });
        await Expect(HttpStatusCode.Accepted, res);
        var jobId = (await Data(res)).GetProperty("jobId").GetString()!;
        var details = f.Services.GetRequiredService<JobStorage>().GetMonitoringApi().JobDetails(jobId);
        Assert.Contains(details.History, h => h.StateName == "Scheduled");
        Assert.DoesNotContain(details.History, h => h.StateName is "Processing" or "Succeeded");
    }

    [Fact]
    public void Sitemap_recurring_job_is_registered()
    {
        using var conn = f.Services.GetRequiredService<JobStorage>().GetConnection();
        Assert.Contains(conn.GetRecurringJobs(), j => j.Id == SitemapJob.RecurringId);
    }

    [Fact]
    public async Task Sitemap_xml_lists_only_published_recipes()
    {
        var (c, _, _) = await AuthorAsync(f);
        var m = "zq" + Guid.NewGuid().ToString("N")[..10];
        var pub = await Data(await c.PostAsJsonAsync("/lab/l3/recipes", new { title = $"Bánh xèo {m}", status = "Published" }));
        var draft = await Data(await c.PostAsJsonAsync("/lab/l3/recipes", new { title = $"Nháp {m}", status = "Draft" }));

        using (var scope = f.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<SitemapJob>().RunAsync(CancellationToken.None);

        var res = await c.GetAsync("/lab/l4/sitemap.xml");
        await Expect(HttpStatusCode.OK, res);
        var xml = XDocument.Parse(await res.Content.ReadAsStringAsync());
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var locs = xml.Descendants(ns + "loc").Select(l => l.Value).ToList();
        Assert.Contains(locs, l => l.EndsWith("/recipes/" + pub.GetProperty("slug").GetString()));
        Assert.DoesNotContain(locs, l => l.EndsWith("/recipes/" + draft.GetProperty("slug").GetString()));
    }

    // ---------------------------------------------------------------- cần MinIO thật (docker)

    [Fact]
    [Trait("Infra", "docker")]
    public async Task Png_uploads_to_minio_resizes_and_delete_leaves_no_orphan()
    {
        var (c, _, _) = await AuthorAsync(f);
        using var ms = new MemoryStream();
        using (var img = new Image<Rgba32>(1200, 900)) await img.SaveAsPngAsync(ms);

        var res = await c.PostAsync($"/lab/l4/recipes/{Guid.NewGuid()}/images", Form(ms.ToArray(), "anh.jpg", "image/jpeg"));
        await Expect(HttpStatusCode.Created, res);
        var data = await Data(res);
        var key = data.GetProperty("key").GetString()!;
        Assert.EndsWith(".png", key); // đuôi theo nội dung thật, không theo tên "anh.jpg"
        var id = data.GetProperty("id").GetGuid();

        var storage = f.Services.GetRequiredService<ObjectStorage>();
        Assert.True(await storage.ExistsAsync(key, default));
        using (var scope = f.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ResizeJob>().RunAsync(id, default);
        var baseKey = key[..key.LastIndexOf('.')];
        Assert.True(await storage.ExistsAsync($"{baseKey}_300x300.jpg", default));
        Assert.True(await storage.ExistsAsync($"{baseKey}_800x600.jpg", default));

        await Expect(HttpStatusCode.NoContent, await c.DeleteAsync($"/lab/l4/images/{id}"));
        Assert.False(await storage.ExistsAsync(key, default));
        Assert.False(await storage.ExistsAsync($"{baseKey}_300x300.jpg", default));
        Assert.False(await storage.ExistsAsync($"{baseKey}_800x600.jpg", default));
    }
}
