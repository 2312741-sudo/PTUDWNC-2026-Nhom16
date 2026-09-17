using System.Text;
using CulinaryBlog.Application;
using Xunit;

namespace CulinaryBlog.Tests;

public sealed class ImageUploadValidatorTests
{
    private static Stream Stream(byte[] bytes) => new MemoryStream(bytes);

    [Theory]
    [InlineData("FFD8FF", ".jpg", "image/jpeg")]
    [InlineData("89504E470D0A1A0A", ".jpg", "image/png")]
    [InlineData("524946461000000057454250", ".jpg", "image/webp")]
    [InlineData("000000186674797061766966", ".jpg", "image/avif")]
    public void DetectMimeType_recognizes_allowed_formats(string hexHeader, string _, string expectedMime)
    {
        var bytes = Convert.FromHexString(hexHeader);
        var mime = ImageFormats.DetectMimeType(bytes);
        Assert.Equal(expectedMime, mime);
    }

    [Theory]
    [InlineData("474946383761")]          // GIF
    [InlineData("255044462D")]            // PDF
    [InlineData("000000006674797069736F6D")] // ISOM (MP4) - không phải image
    [InlineData("526172211A0700")]        // RAR
    public void DetectMimeType_rejects_non_images(string hexHeader)
    {
        Assert.Null(ImageFormats.DetectMimeType(Convert.FromHexString(hexHeader)));
    }

    [Fact]
    public void Validator_rejects_empty_file()
    {
        var (ok, code, _) = ImageUploadValidator.Validate(Stream([]), 0, "image/jpeg");
        Assert.False(ok);
        Assert.Equal("file.empty", code);
    }

    [Fact]
    public void Validator_rejects_file_larger_than_5MiB()
    {
        var head = new byte[12] { 0xFF, 0xD8, 0xFF, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        var (ok, code, _) = ImageUploadValidator.Validate(Stream(head), ImageFormats.MaxBytes + 1, "image/jpeg");
        Assert.False(ok);
        Assert.Equal("file.too_large", code);
    }

    [Fact]
    public void Validator_rejects_mismatched_content_type()
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var (ok, code, _) = ImageUploadValidator.Validate(Stream(bytes), bytes.Length, "image/png");
        Assert.False(ok);
        Assert.Equal("file.invalid_type", code);
    }

    [Fact]
    public void Validator_accepts_valid_jpeg_with_matching_mime()
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var (ok, code, _) = ImageUploadValidator.Validate(Stream(bytes), bytes.Length, "image/jpeg");
        Assert.True(ok);
        Assert.Null(code);
    }

    [Fact]
    public void Validator_accepts_without_declared_mime_and_detects_real_type()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };
        var (ok, code, _) = ImageUploadValidator.Validate(Stream(bytes), bytes.Length, null);
        Assert.True(ok);
        Assert.Equal("image/png", ImageFormats.DetectMimeType(bytes));
    }

    [Fact]
    public void ExtensionFor_maps_each_allowed_mime()
    {
        Assert.Equal(".jpg", ImageFormats.ExtensionFor("image/jpeg"));
        Assert.Equal(".png", ImageFormats.ExtensionFor("image/png"));
        Assert.Equal(".webp", ImageFormats.ExtensionFor("image/webp"));
        Assert.Equal(".avif", ImageFormats.ExtensionFor("image/avif"));
        Assert.True(ImageFormats.Allowed.Count == 4, "Chỉ 4 định dạng cho phép theo FR-FILE-001");
    }
}
