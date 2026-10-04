using System.Text;
using CulinaryBlog.Application;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// N2-E6 — magic bytes phải đủ, không được chỉ kiểm 4 byte đầu.
///
/// Vì sao cần: `Content-Type` do client gửi lên không đáng tin. Nếu chỉ so 4 byte đầu thì một file
/// `.exe` (4D 5A …) được đổi tên `.jpg`, hoặc file HTML/JS kèm header giả, sẽ lọt. Yêu cầu của
/// báo cáo là test phải **đỏ** khi cắt ngắn magic bytes — nên test ở đây cố tình cắt signature ở
/// từng độ dài và đòi hàm phát hiện phải thất bại.
///
/// Đây là test thuần tuý (không cần DB/S3) nên chạy nhanh và luôn chạy được.
/// </summary>
public sealed class ImageMagicBytesE6Tests
{
    // ------------------------------------------------------------------ helpers

    /// <summary>Dựng header đúng chuẩn của từng định dạng, dài đủ để có 12 byte đầu.</summary>
    private static byte[] ValidHeader(string mime, int length = 16)
    {
        byte[] head = mime switch
        {
            "image/jpeg" => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01],
            "image/png" => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D],
            // RIFF <vsize:4 byte> WEBP
            "image/webp" =>
            [
                0x52, 0x49, 0x46, 0x46, 0x1A, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50,
            ],
            // <vsize:4 byte> ftyp avif
            "image/avif" =>
            [
                0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70, 0x61, 0x76, 0x69, 0x66,
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(mime), mime, "Định dạng không hỗ trợ"),
        };

        var padded = new byte[Math.Max(head.Length, length)];
        head.CopyTo(padded, 0);
        return padded;
    }

    public static TheoryData<string> SupportedMimes => new()
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/avif",
    };

    // ------------------------------------------------------------------ E6.1
    [Theory]
    [MemberData(nameof(SupportedMimes))]
    public void E6_Valid_header_of_each_allowed_format_is_detected(string mime)
    {
        Assert.Equal(mime, ImageFormats.DetectMimeType(ValidHeader(mime)));
    }

    // ------------------------------------------------------------------ E6.2
    /// <summary>
    /// Byte đầu phải đủ cho toàn bộ chữ ký, không phải 4 byte. File chỉ có 4 byte dù đúng không
    /// được coi là ảnh hợp lệ — đây là bảo đảm trực tiếp cho yêu cầu "không chỉ 4 byte đầu".
    /// </summary>
    [Theory]
    [MemberData(nameof(SupportedMimes))]
    public void E6_Truncated_to_4_bytes_is_not_accepted(string mime)
    {
        var header = ValidHeader(mime);
        var only4 = header.AsSpan(0, 4).ToArray();

        // Với JPEG, 4 byte là `FF D8 FF E0` — chưa đủ chữ ký 3 byte? Không: 3 byte là đã đủ, nên
        // JPEG là ngoại lệ hợp lệ của danh sách này. Khẳng định đúng thực tế thay vì ép điều sai.
        if (mime == "image/jpeg")
        {
            Assert.Equal("image/jpeg", ImageFormats.DetectMimeType(only4));
            return;
        }

        Assert.Null(ImageFormats.DetectMimeType(only4));
    }

    // ------------------------------------------------------------------ E6.3
    /// <summary>
    /// Cắt ngắn signature ở *mọi* độ dài: file phải bị từ chối. Với WebP/AVIF chữ ký dài 12 byte,
    /// nên cắt ở 8 byte (chỉ còn `RIFF`/`ftyp`) phải không khớp.
    /// </summary>
    [Theory]
    [InlineData("image/webp", 5)]
    [InlineData("image/webp", 8)]
    [InlineData("image/webp", 11)]
    [InlineData("image/avif", 5)]
    [InlineData("image/avif", 8)]
    [InlineData("image/avif", 11)]
    [InlineData("image/png", 4)]
    [InlineData("image/png", 7)]
    public void E6_Signature_cut_short_at_any_length_is_rejected(string mime, int length)
    {
        var header = ValidHeader(mime).AsSpan(0, length).ToArray();

        // Chữ ký khai trong `ImageFormats.Allowed` dài hơn `length` byte nên phải không khớp.
        // Với PNG (8 byte) các độ dài 4 và 7 đều ngắu hơn chữ ký.
        Assert.True(
            ImageFormats.Allowed
                .Where(f => f.MimeType == mime)
                .SelectMany(f => f.MagicSignatures)
                .All(sig => sig.Length > length),
            $"Test thiếu: chữ ký của {mime} không dài hơn {length} byte nên case này vô nghĩa.");

        Assert.Null(ImageFormats.DetectMimeType(header));
    }

    // ------------------------------------------------------------------ E6.4
    [Theory]
    [InlineData("MZ")]              // .exe / PE
    [InlineData("#!/bin/sh")]       // script
    [InlineData("<html>")]          // HTML — XSS
    [InlineData("PK")]              // .zip
    public void E6_Non_image_payloads_are_rejected(string prefix)
    {
        var bytes = Encoding.ASCII.GetBytes(prefix.PadRight(16, 'A'));
        Assert.Null(ImageFormats.DetectMimeType(bytes));
    }

    // ------------------------------------------------------------------ E6.5
    [Fact]
    public void E6_Executable_renamed_to_jpg_is_rejected_because_magic_bytes_win()
    {
        // Kịch bản N2-B3: đổi tên `.exe` → `.jpg` và khai Content-Type là image/jpeg.
        var exe = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF };
        var declaredMime = "image/jpeg";

        // Content-Type khai là ảnh hợp lệ…
        Assert.True(ImageFormats.IsAllowedMime(declaredMime));
        // …nhưng nội dung thật không khớp → phải bị chặn.
        Assert.Null(ImageFormats.DetectMimeType(exe));
    }

    // ------------------------------------------------------------------ E6.6
    [Fact]
    public void E6_Webp_signature_is_checked_at_byte_8_to_11_not_just_riff()
    {
        // `RIFF` + kích thước, nhưng 4 byte cuối là `WAVE` (RIFF của wav) chứ không phải `WEBP`.
        var riffWave = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45 };
        Assert.Null(ImageFormats.DetectMimeType(riffWave));

        // Bỏ 4 byte cuối đi (chỉ còn `RIFF` + 4 byte kích thước) thì cũng không được coi là WebP.
        var riffOnly = riffWave.AsSpan(0, 8).ToArray();
        Assert.Null(ImageFormats.DetectMimeType(riffOnly));
    }

    // ------------------------------------------------------------------ E6.7
    [Fact]
    public void E6_Avif_brand_avif_and_avis_are_both_accepted_but_other_brands_are_not()
    {
        byte[] WithBrand(string brand) =>
        [
            0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70,
            .. Encoding.ASCII.GetBytes(brand.PadRight(4, ' ')),
        ];

        Assert.Equal("image/avif", ImageFormats.DetectMimeType(WithBrand("avif")));
        Assert.Equal("image/avif", ImageFormats.DetectMimeType(WithBrand("avis")));
        // `ftyp` đúng nhưng brand là `mp42` (MPEG-4) thì không phải ảnh được phép.
        Assert.Null(ImageFormats.DetectMimeType(WithBrand("mp42")));
    }

    // ------------------------------------------------------------------ E6.8
    [Fact]
    public void E6_Empty_and_too_short_inputs_return_null_instead_of_throwing()
    {
        Assert.Null(ImageFormats.DetectMimeType(ReadOnlySpan<byte>.Empty));
        Assert.Null(ImageFormats.DetectMimeType([0xFF]));
        Assert.Null(ImageFormats.DetectMimeType([0x89, 0x50]));
    }

    // ------------------------------------------------------------------ E6.9
    [Fact]
    public void E6_Every_allowed_format_declares_its_expected_extension()
    {
        Assert.Equal(".jpg", ImageFormats.ExtensionFor("image/jpeg"));
        Assert.Equal(".png", ImageFormats.ExtensionFor("image/png"));
        Assert.Equal(".webp", ImageFormats.ExtensionFor("image/webp"));
        Assert.Equal(".avif", ImageFormats.ExtensionFor("image/avif"));
    }

    // ------------------------------------------------------------------ E6.10
    // Phát hiện khi làm N2-B3 (kịch bản tấn công file): chữ ký JPEG chỉ dài 3 byte nên một file
    // **3 byte** khớp chữ ký và lọt qua `DetectMimeType`. Không có ngưỡng kích thước tối thiểu thì
    // file rỗng bị cắt cụt được nhận ở biên API rồi mới chết ở job resize — lỗi nằm ngoài request
    // nên người dùng không nhận được mã lỗi có nghĩa.
    [Fact]
    public void E6_File_too_short_to_be_an_image_is_rejected_with_file_too_small()
    {
        // 3 byte `FF D8 FF` — khớp chữ ký JPEG nhưng không thể là ảnh.
        var truncatedJpeg = new MemoryStream([0xFF, 0xD8, 0xFF]);

        var (ok, code, _) = ImageUploadValidator.Validate(
            truncatedJpeg, truncatedJpeg.Length, "image/jpeg");

        Assert.False(ok);
        Assert.Equal("file.too_small", code);
    }

    [Fact]
    public void E6_Just_under_minimum_size_is_rejected_even_with_a_valid_signature()
    {
        // 63 byte: PNG signature hợp lệ + padding, nhưng nhỏ hơn `MinBytes` (64).
        var almostPng = new MemoryStream(
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. new byte[55]]);

        var (ok, code, _) = ImageUploadValidator.Validate(
            almostPng, almostPng.Length, "image/png");

        Assert.False(ok);
        Assert.Equal("file.too_small", code);
    }

    [Fact]
    public void E6_A_real_png_at_the_size_boundary_is_still_accepted()
    {
        // PNG 1×1 hợp lệ (67 byte) — lớn hơn `MinBytes` nên phải qua.
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        Assert.True(png.Length >= ImageFormats.MinBytes);

        using var content = new MemoryStream(png);
        var (ok, code, _) = ImageUploadValidator.Validate(content, content.Length, "image/png");

        Assert.True(ok);
        Assert.Null(code);
    }
}
