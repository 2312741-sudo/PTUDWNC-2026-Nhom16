namespace Lab.TV3.Api.L4;

/// <summary>Nhận diện ảnh bằng magic bytes — KHÔNG tin Content-Type/đuôi file từ client (K13, FR-FILE-001).</summary>
public static class ImageValidator
{
    public const long MaxBytes = 5L * 1024 * 1024; // 5 MiB

    private static ReadOnlySpan<byte> Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static bool SizeOk(long length) => length > 0 && length <= MaxBytes;

    public static (string Mime, string Ext)? Detect(ReadOnlySpan<byte> h)
    {
        if (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ("image/jpeg", ".jpg");
        if (h.Length >= 8 && h[..8].SequenceEqual(Png)) return ("image/png", ".png");
        if (h.Length >= 12 && h[..4].SequenceEqual("RIFF"u8) && h[8..12].SequenceEqual("WEBP"u8)) return ("image/webp", ".webp");
        if (h.Length >= 12 && h[4..8].SequenceEqual("ftyp"u8) && (h[8..12].SequenceEqual("avif"u8) || h[8..12].SequenceEqual("avis"u8)))
            return ("image/avif", ".avif");
        return null;
    }
}