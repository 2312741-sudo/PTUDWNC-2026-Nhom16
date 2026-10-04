namespace CulinaryBlog.Application;

public interface IFileStorageService
{
    Task<StoredFile> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}

public sealed record StoredFile(string Key, string Url, string ContentType, long SizeBytes);

/// <summary>
/// B5 (issue #24): cấp URL có chữ ký (presigned) cho ảnh PRIVATE để thẻ &lt;img&gt; tải được.
/// Proxy D27 (PA-2) yêu cầu header Authorization — thẻ img không gửi được header đó nên ảnh recipe
/// Draft luôn 403 trong wizard. Presigned URL mang chữ ký trong chính query string nên trình duyệt
/// tải được không cần header.
/// TÁCH khỏi <see cref="IFileStorageService"/> và <c>IObjectStorageReader</c> để không phá contract bàn giao TV3.
/// </summary>
/// <remarks>
/// Hạn ngắn, mặc định 10 phút (Minio:PresignedUrlExpiryMinutes, tối đa 15): URL là bearer token,
/// rò ra access log / lịch sử trình duyệt / header Referer là lộ ảnh riêng tư.
/// Lỗi hạ tầng trả <c>null</c> (KHÔNG ném) — luồng upload đã thành công thì không được phá vỡ vì URL.
/// </remarks>
public interface IObjectStorageUrlSigner
{
    /// <summary>URL đã ký cho <paramref name="key"/>, hoặc <c>null</c> nếu không ký được.</summary>
    Task<string?> CreatePresignedUrlAsync(string key, CancellationToken ct = default);
}
