using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Practice.Lab4;

/// <summary>Phase MEDIA (L4): upload/delete 4 MIME theo nội dung + kích thước thực, resize 2 size, xoá dọn.</summary>
public static class MediaPhase
{
    public static async Task<PhaseResult> RunAsync(IServiceProvider services)
    {
        var logger = LabLog.For("LAB/media");
        var reader = services.GetRequiredService<IObjectStorageReader>();
        var writer = services.GetRequiredService<IObjectStorageWriter>();
        var storage = services.GetRequiredService<IFileStorageService>();
        var scaler = new LabImageScaler(reader, writer, LabLog.For<LabImageScaler>());
        var checks = new List<Check>();

        logger.LogInformation("=== PHASE MEDIA (L4) === bucket={Bucket} endpoint={Endpoint} prefix={Prefix}",
            LabConfig.StorageBucket, LabConfig.MinioEndpoint, LabConfig.LabKeyPrefix);

        var fixtures = await Fixtures.CreateAsync();
        logger.LogInformation("Sinh {Count} fixture: {Names}", fixtures.Count,
            string.Join(", ", fixtures.Select(f => $"{f.Name} ({f.Bytes.Length} bytes)")));

        // 1) Validator: 4 MIME hợp lệ + 2 ca lỗi (quá 5 MiB, nội dung không khớp).
        foreach (var fixture in fixtures.Where(f => f.Name.StartsWith("sample.", StringComparison.Ordinal)))
        {
            using var stream = new MemoryStream(fixture.Bytes);
            var (ok, code, _) = ImageUploadValidator.Validate(stream, fixture.Bytes.Length, fixture.DeclaredMime);
            checks.Add(new Check($"validate {fixture.Name} ({fixture.DeclaredMime})", ok, ok ? "OK" : $"bị từ chối: {code}"));
        }

        foreach (var fixture in fixtures.Where(f => f.Name is "oversize.jpg" or "fake.jpg"))
        {
            using var stream = new MemoryStream(fixture.Bytes);
            var (ok, code, _) = ImageUploadValidator.Validate(stream, fixture.Bytes.Length, fixture.DeclaredMime);
            var expected = fixture.Name == "oversize.jpg" ? "file.too_large" : "file.invalid_type";
            checks.Add(new Check($"validate {fixture.Name} -> {expected}", !ok && code == expected,
                ok ? "KHÔNG MONG ĐỢI: bị chấp nhận" : $"bị từ chối đúng: {code}"));
        }

        // 2) Upload 4 MIME lên MinIO rồi đọc lại, so byte length.
        var uploaded = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var fixture in fixtures.Where(f => f.Name.StartsWith("sample.", StringComparison.Ordinal)))
        {
            var key = $"{LabConfig.LabKeyPrefix}/{fixture.Name}";
            using (var content = new MemoryStream(fixture.Bytes))
            {
                await writer.UploadAsync(key, content, fixture.DeclaredMime, content.Length);
            }

            using var read = await reader.ReadAsync(key);
            var sameLength = read is not null && read.Length == fixture.Bytes.Length;
            checks.Add(new Check($"upload + readback {fixture.Name}", sameLength,
                read is null ? "missing" : $"{read.Length}/{fixture.Bytes.Length} bytes, ct={read.ContentType}"));
            uploaded[fixture.Name] = key;
            logger.LogInformation("UPLOAD {Key} ({DeclaredMime}, {Bytes} bytes) -> đọc lại {Actual} bytes",
                key, fixture.DeclaredMime, fixture.Bytes.Length, read?.Length ?? -1);
        }

        // 3) Resize 2 size cho 3 định dạng decode được; AVIF phải fallback original.
        foreach (var name in new[] { "sample.jpg", "sample.png", "sample.webp" })
        {
            var variants = await scaler.ResizeAsync(uploaded[name]);
            checks.Add(new Check($"resize {name} -> 2 biến thể", variants.Count == 2,
                string.Join(" | ", variants.Select(v => v.Key))));
            foreach (var variant in variants)
            {
                var measured = await LabImageScaler.MeasureAsync(reader, variant.Key);
                var limit = variant.Key.Contains(RecipeImageKeys.ThumbnailSuffix) ? LabImageScaler.ThumbnailMax : LabImageScaler.MediumWidth;
                checks.Add(new Check($"measure {Path.GetFileName(variant.Key)}", variant.Width <= limit, measured));
            }
        }

        var avifVariants = await scaler.ResizeAsync(uploaded["sample.avif"]);
        checks.Add(new Check("resize sample.avif -> fallback original", avifVariants.Count == 0, "0 biến thể, giữ original"));

        // 4) Idempotent: chạy lại resize trên JPEG → không ghi đè.
        var secondRun = await scaler.ResizeAsync(uploaded["sample.jpg"]);
        checks.Add(new Check("resize idempotent (chạy 2 lần)", secondRun.Count == 2 && secondRun.All(v => v.Existed),
            string.Join(" | ", secondRun.Select(v => $"{Path.GetFileName(v.Key)} existed={v.Existed}"))));

        // 5) Xoá original + phái sinh → không còn object.
        foreach (var name in new[] { "sample.jpg", "sample.png", "sample.webp" })
        {
            var key = uploaded[name];
            var variants = RecipeImageKeys.ResizedKeys(key);
            await storage.DeleteAsync(key);
            if (variants is not null)
            {
                await storage.DeleteAsync(variants.Value.ThumbnailKey);
                await storage.DeleteAsync(variants.Value.MediumKey);
            }

            var gone = !await writer.ExistsAsync(key) && (variants is null ||
                (!await writer.ExistsAsync(variants.Value.ThumbnailKey) && !await writer.ExistsAsync(variants.Value.MediumKey)));
            checks.Add(new Check($"delete {name} + 2 biến thể", gone, "không còn object"));
        }

        await storage.DeleteAsync(uploaded["sample.avif"]);
        checks.Add(new Check("delete sample.avif", !await writer.ExistsAsync(uploaded["sample.avif"]), "không còn object"));

        return PhaseResult.From("MEDIA", checks);
    }
}
