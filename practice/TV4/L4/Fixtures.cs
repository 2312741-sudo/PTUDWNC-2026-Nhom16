using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CulinaryBlog.Practice.Lab4;

public sealed record Fixture(string Name, string DeclaredMime, byte[] Bytes, string Note);

/// <summary>
/// Sinh file ảnh thật để lab kiểm tra "nội dung/kích thước thực" (L4):
/// JPEG/PNG/WebP encode bằng ImageSharp; AVIF dùng box `ftyp` chuẩn ISOBMFF vì ImageSharp 3.1 không encode AVIF
/// (đúng tình huống cần chứng minh resize fallback original). Ba fixture lỗi để kiểm tra validator.
/// </summary>
public static class Fixtures
{
    public static async Task<List<Fixture>> CreateAsync(int width = 1200, int height = 800)
    {
        var list = new List<Fixture>
        {
            new("sample.jpg", "image/jpeg", await EncodeAsync(new JpegEncoder { Quality = 88 }), $"{width}x{height} JPEG"),
            new("sample.png", "image/png", await EncodeAsync(new PngEncoder()), $"{width}x{height} PNG"),
            new("sample.webp", "image/webp", await EncodeAsync(new WebpEncoder { Quality = 88 }), $"{width}x{height} WebP"),
            new("sample.avif", "image/avif", CreateAvifFtyp(), "ftyp AVIF (ImageSharp 3.1 không decode được)"),
        };

        var oversize = new byte[5 * 1024 * 1024 + 1024];
        list[0].Bytes.CopyTo(oversize, 0);   // JPEG hợp lệ rồi pad > 5 MiB để chạm đúng nhánh giới hạn kích thước
        list.Add(new Fixture("oversize.jpg", "image/jpeg", oversize, "JPEG hợp lệ nhưng > 5 MiB (FR-FILE-002)"));

        list.Add(new Fixture("fake.jpg", "image/jpeg", System.Text.Encoding.ASCII.GetBytes(new string('X', 4096)),
            "Nội dung text, khai báo image/jpeg → magic bytes không khớp"));
        return list;
    }

    private static async Task<byte[]> EncodeAsync(IImageEncoder encoder)
    {
        using var image = new Image<Rgba32>(1200, 800);
        image.Mutate(ctx => ctx.BackgroundColor(Color.CornflowerBlue));
        using var output = new MemoryStream();
        await image.SaveAsync(output, encoder);
        return output.ToArray();
    }

    /// <summary>Box ftyp hợp lệ: size + 'ftyp' + major brand 'avif' + minor 0 + compatible brands.</summary>
    private static byte[] CreateAvifFtyp()
    {
        byte[] brands = [.. Ascii("avif"), .. new byte[4], .. Ascii("avif"), .. Ascii("mif1"), .. Ascii("miaf")];
        var box = new byte[8 + brands.Length];
        WriteBigEndian(box, 0, (uint)box.Length);
        Ascii("ftyp").CopyTo(box, 4);
        brands.CopyTo(box, 8);
        return box;
    }

    private static byte[] Ascii(string text) => System.Text.Encoding.ASCII.GetBytes(text);

    private static void WriteBigEndian(byte[] target, int offset, uint value)
    {
        target[offset] = (byte)(value >> 24);
        target[offset + 1] = (byte)(value >> 16);
        target[offset + 2] = (byte)(value >> 8);
        target[offset + 3] = (byte)value;
    }
}
