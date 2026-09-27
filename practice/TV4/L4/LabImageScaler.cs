using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace CulinaryBlog.Practice.Lab4;

public sealed record VariantResult(string Key, int Width, int Height, bool Existed);

/// <summary>
/// Resize 2 size (300×300 + 800×600) cho lab, dùng lại `RecipeImageKeys` + `IObjectStorageWriter` của sản phẩm.
/// Bản lab bỏ phần cập nhật DB (đã chứng minh ở N4 bằng `ResizeImageJob`) để job chạy được không cần dữ liệu recipe.
/// Bất biến: idempotent (bỏ qua object đã tồn tại) + fallback original khi ảnh không decode (AVIF) — FR-JOB-002/003.
/// </summary>
public sealed class LabImageScaler(
    IObjectStorageReader reader,
    IObjectStorageWriter writer,
    ILogger<LabImageScaler> logger)
{
    public const int ThumbnailMax = 300;
    public const int MediumWidth = 800;
    public const int MediumHeight = 600;

    public async Task<List<VariantResult>> ResizeAsync(string originalKey, CancellationToken ct = default)
    {
        var keys = RecipeImageKeys.ResizedKeys(originalKey);
        if (keys is null)
        {
            logger.LogWarning("SKIP resize {Key}: định dạng chưa hỗ trợ (AVIF) — giữ original.", originalKey);
            return [];
        }

        using var original = await reader.ReadAsync(originalKey, ct);
        if (original is null)
        {
            logger.LogWarning("SKIP resize {Key}: original không tồn tại.", originalKey);
            return [];
        }

        var results = new List<VariantResult>();
        Image? source = null;
        try
        {
            source = await Image.LoadAsync(original.Stream, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "SKIP resize {Key}: không decode được — giữ original.", originalKey);
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
                    var existed = await writer.ExistsAsync(key, ct);
                    if (existed)
                    {
                        logger.LogInformation("IDEMPOTENT {Key} đã tồn tại — không ghi đè.", key);
                        results.Add(new VariantResult(key, width, height, true));
                        continue;
                    }

                    using var resized = source.Clone(ctx => ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(width, height),
                        Mode = ResizeMode.Max
                    }));
                    using var output = new MemoryStream();
                    await EncodeAsync(resized, output, originalKey, ct);
                    output.Position = 0;
                    await writer.UploadAsync(key, output, ContentTypeFor(originalKey), output.Length, ct);
                    logger.LogInformation("RESIZED {Original} -> {Key} (max {Width}x{Height})", originalKey, key, width, height);
                    results.Add(new VariantResult(key, resized.Width, resized.Height, false));
                }
            }
        }

        return results;
    }

    /// <summary>Đọc lại object phái sinh để đo kích thước thật (bằng chứng resize đúng).</summary>
    public static async Task<string> MeasureAsync(IObjectStorageReader reader, string key, CancellationToken ct = default)
    {
        using var content = await reader.ReadAsync(key, ct);
        if (content is null) return "missing";
        var info = await Image.IdentifyAsync(content.Stream, ct);
        return $"{info?.Width}x{info?.Height} ({content.Length} bytes, {content.ContentType})";
    }

    private static Task EncodeAsync(Image image, Stream output, string key, CancellationToken ct)
    {
        // ImageSharp 3.1 chỉ có SaveAsync(stream, encoder, ct).
        return Path.GetExtension(key)?.ToLowerInvariant() switch
        {
            ".png" => image.SaveAsync(output, new PngEncoder(), ct),
            ".webp" => image.SaveAsync(output, new WebpEncoder(), ct),
            _ => image.SaveAsync(output, new JpegEncoder { Quality = 88 }, ct)
        };
    }

    private static string ContentTypeFor(string key) => Path.GetExtension(key)?.ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".avif" => "image/avif",
        _ => "image/jpeg"
    };
}
