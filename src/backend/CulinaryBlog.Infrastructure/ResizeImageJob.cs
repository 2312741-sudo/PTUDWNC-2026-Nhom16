using CulinaryBlog.Application;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace CulinaryBlog.Infrastructure;

/// <summary>
/// D23 PA-1 (TV4): resize ảnh original thành 300×300 (thumbnail) + 800×600 (medium) bằng SixLabors.ImageSharp,
/// ghi object phái sinh {uuid}_300x300.{ext} / {uuid}_800x600.{ext} qua IObjectStorageWriter (key chủ động — nằm NGOÀI
/// IFileStorageService/StoredFile, đúng HANDOFF 5.1), rồi cập nhật RecipeImage.MediumUrl/ThumbnailUrl.
/// Bất biến (D2/FR-JOB-002/003):
/// - Idempotent: object phái sinh đã tồn tại → bỏ qua upload, vẫn đảm bảo URL trong DB.
/// - Delete-vs-resize: ảnh đã xoá → không tái sinh object.
/// - Original fallback: ảnh hỏng/không decode (vd AVIF, JPEG giả) hoặc encode lỗi → MediumUrl/ThumbnailUrl giữ null,
///   không ném exception ra ngoài (upload vẫn 201, FE fallback original qua imageSrc()).
/// - Retry: [AutomaticRetry(Attempts = 3)] — Hangfire (HangfireImageResizeQueue).
/// </summary>
public sealed class ResizeImageJob(
    AuthDbContext db,
    IObjectStorageReader reader,
    IObjectStorageWriter writer,
    ILogger<ResizeImageJob> logger)
{
    private const int ThumbnailMax = 300;
    private const int MediumWidth = 800;
    private const int MediumHeight = 600;

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(Guid recipeId, Guid imageId, string originalKey, CancellationToken ct = default)
    {
        // Boundary: ảnh đã xoá (row biến mất) → không tái sinh object phái sinh.
        var imageExists = await db.RecipeImages.AsNoTracking()
            .AnyAsync(i => i.Id == imageId && i.RecipeId == recipeId, ct);
        if (!imageExists)
        {
            logger.LogWarning("Resize skip: image {ImageId} của recipe {RecipeId} không còn tồn tại.", imageId, recipeId);
            return;
        }

        var keys = RecipeImageKeys.ResizedKeys(originalKey);
        if (keys is null)
        {
            logger.LogWarning("Resize skip: {Key} là định dạng chưa hỗ trợ resize (AVIF) — giữ original.", originalKey);
            return;
        }

        MediaContent? original;
        try
        {
            original = await reader.ReadAsync(originalKey, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Resize skip: đọc original {Key} thất bại.", originalKey);
            return;
        }
        if (original is null)
        {
            logger.LogWarning("Resize skip: original {Key} không tồn tại trên MinIO.", originalKey);
            return;
        }

        var (contentType, save) = EncoderFor(originalKey);
        var allDerivedReady = true;
        Image? source = null;
        try
        {
            source = await Image.LoadAsync(original.Stream, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // FR-JOB-003: ảnh hỏng/không decode (JPEG giả, file cắt dở) → giữ original, KHÔNG lỗi request upload.
            allDerivedReady = false;
            logger.LogWarning(ex, "Resize skip: {Key} không decode được — giữ original (FE fallback imageSrc).", originalKey);
        }

        if (source is not null)
        {
            using (source)
            {
                foreach (var (key, width, height) in new[]
                {
                    (keys.Value.ThumbnailKey, ThumbnailMax, ThumbnailMax),
                    (keys.Value.MediumKey, MediumWidth, MediumHeight)
                })
                {
                    if (await writer.ExistsAsync(key, ct))
                    {
                        logger.LogInformation("Resize idempotent: {Key} đã tồn tại — bỏ qua upload.", key);
                        continue;
                    }

                    try
                    {
                        using var resized = source.Clone(ctx => ctx.Resize(new ResizeOptions
                        {
                            Size = new Size(width, height),
                            Mode = ResizeMode.Max
                        }));
                        using var output = new MemoryStream();
                        await save(resized, output, ct);
                        output.Position = 0;
                        await writer.UploadAsync(key, output, contentType, output.Length, ct);
                        logger.LogInformation("Resized {Original} -> {Key} ({Width}x{Height}).", originalKey, key, width, height);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        allDerivedReady = false;
                        logger.LogWarning(ex, "Resize {Key} thất bại — không cập nhật URL resize.", key);
                    }
                }
            }
        }

        original.Stream.Dispose();
        if (!allDerivedReady)
            return;   // còn URL cũ (null) -> FE dùng original, không trỏ vào object không tồn tại

        // Cập nhật URL phái sinh (chỉ khi ảnh vẫn còn; concurrency → log, Hangfire retry idempotent).
        var image = await db.RecipeImages.FirstOrDefaultAsync(i => i.Id == imageId && i.RecipeId == recipeId, ct);
        if (image is null)
            return;
        if (image.MediumUrl == keys.Value.MediumKey && image.ThumbnailUrl == keys.Value.ThumbnailKey)
            return;

        image.SetResizedUrls(keys.Value.MediumKey, keys.Value.ThumbnailKey);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Resize cập nhật URL bị xung đột — bỏ qua (job idempotent).");
        }
    }

    private static (string ContentType, Func<Image, Stream, CancellationToken, Task> Save) EncoderFor(string key)
    {
        // Bản 3.1.x chỉ có SaveAsync(stream, encoder, ct); giữ nguyên định dạng gốc để extension khớp bytes/content-type proxy.
        return Path.GetExtension(key)?.ToLowerInvariant() switch
        {
            ".png" => ("image/png", (img, s, ct) => img.SaveAsync(s, new PngEncoder(), ct)),
            ".webp" => ("image/webp", (img, s, ct) => img.SaveAsync(s, new WebpEncoder(), ct)),
            _ => ("image/jpeg", (img, s, ct) => img.SaveAsync(s, new JpegEncoder(), ct))
        };
    }
}
