namespace CulinaryBlog.Application;

/// <summary>
/// Danh sách MIME và magic bytes cho upload ảnh recipe (FR-FILE-001/002, W2 D1).
/// Không tin Content-Type từ client: đối chiếu signature thật của file.
/// </summary>
public sealed record AllowedImageFormat(string MimeType, string Extension, byte?[][] MagicSignatures);

public static class ImageFormats
{
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MiB (FR-FILE-002)

    public static readonly IReadOnlyList<AllowedImageFormat> Allowed =
    [
        new("image/jpeg", ".jpg", [[0xFF, 0xD8, 0xFF]]),
        new("image/png", ".png",
        [
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]
        ]),
        new("image/webp", ".webp",
        [
            [0x52, 0x49, 0x46, 0x46, null, null, null, null, 0x57, 0x45, 0x42, 0x50] // RIFF vsize WEBP
        ]),
        new("image/avif", ".avif",
        [
            [null, null, null, null, 0x66, 0x74, 0x79, 0x70, 0x61, 0x76, 0x69, 0x66], // vsize ftypavif
            [null, null, null, null, 0x66, 0x74, 0x79, 0x70, 0x61, 0x76, 0x69, 0x73]  // vsize ftypavis
        ])
    ];

    /// <summary>Lấy MIME chuẩn từ magic bytes (12 byte đầu). Trả null nếu không khớp định dạng cho phép.</summary>
    public static string? DetectMimeType(ReadOnlySpan<byte> head)
    {
        foreach (var format in Allowed)
        {
            foreach (var signature in format.MagicSignatures)
            {
                if (Matches(signature, head)) return format.MimeType;
            }
        }
        return null;
    }

    public static bool IsAllowedMime(string? mime)
        => mime is not null && Allowed.Any(f => string.Equals(f.MimeType, mime, StringComparison.OrdinalIgnoreCase));

    public static string ExtensionFor(string mimeType)
        => Allowed.First(f => string.Equals(f.MimeType, mimeType, StringComparison.OrdinalIgnoreCase)).Extension;

    private static bool Matches(byte?[] signature, ReadOnlySpan<byte> head)
    {
        if (head.Length < signature.Length) return false;
        for (var i = 0; i < signature.Length; i++)
        {
            if (signature[i] is { } expected && head[i] != expected) return false;
        }
        return true;
    }
}

/// <summary>
/// Kiểm tra file upload ảnh: size ≤5MiB + MIME hợp lệ + magic bytes khớp nội dung.
/// Trả về code lỗi chuẩn của validator; null nếu hợp lệ.
/// </summary>
public static class ImageUploadValidator
{
    public static (bool Ok, string? Code, string? Message) Validate(Stream content, long length, string? declaredMime)
    {
        if (length <= 0)
            return (false, "file.empty", "File rỗng không được chấp nhận.");
        if (length > ImageFormats.MaxBytes)
            return (false, "file.too_large", "File vượt quá giới hạn 5 MiB.");

        var head = new byte[12];
        var read = content.Read(head, 0, head.Length);
        content.Position = 0;

        var detected = ImageFormats.DetectMimeType(head.AsSpan(0, read));
        if (detected is null)
            return (false, "file.invalid_type", "Chỉ chấp nhận JPEG, PNG, WebP hoặc AVIF.");
        if (declaredMime is not null && !string.Equals(declaredMime.Trim(), detected, StringComparison.OrdinalIgnoreCase))
            return (false, "file.invalid_type", "Content-Type khai báo không khớp nội dung file.");

        return (true, null, null);
    }
}
