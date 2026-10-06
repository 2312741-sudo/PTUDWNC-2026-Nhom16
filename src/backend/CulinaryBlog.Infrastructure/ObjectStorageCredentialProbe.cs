using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Infrastructure;

/// <summary>Kết quả probe object storage: chỉ nói "đúng/sai", không phụ thuộc ASP.NET health check.</summary>
public sealed record StorageProbeResult(bool Ok, string Message, Exception? Error = null)
{
    public static StorageProbeResult Healthy(string message) => new(true, message);
    public static StorageProbeResult Unhealthy(string message, Exception? error = null) => new(false, message, error);
}

/// <summary>
/// B4 (issue #22, N1-7): probe object storage kiểm tra CREDENTIAL THẬT, không chỉ mở cổng.
///
/// Lý do: health check cũ chỉ TCP-probe nên báo <c>Healthy</c> cả khi cổng 9000 mở nhưng
/// AccessKey sai — đúng cái loại lỗi chỉ lộ ra khi người dùng bấm "Tải lên" (bug 500 tuần 3).
///
/// Cách kiểm tra: một lệnh <c>HEAD object</c> (StatObject) trên một key chắc chắn không tồn tại.
/// Một request rẻ này phân biệt được bốn trạng thái mà TCP probe không phân biệt được:
/// <list type="bullet">
/// <item>credential sai / thiếu quyền ⇒ <c>AccessDeniedException</c> ⇒ Unhealthy (đây là B4 bắt)</item>
/// <item>bucket không tồn tại ⇒ <c>BucketNotFoundException</c> ⇒ Unhealthy</item>
/// <item>credential ĐÚNG, key vắng mặt ⇒ <c>ObjectNotFoundException</c> ⇒ Healthy</item>
/// <item>storage không truy cập được ⇒ lỗi mạng ⇒ Unhealthy</item>
/// </list>
///
/// Nếu chưa cấu hình credential thì báo <c>skipped</c> (Healthy kèm chú thích) chứ không giả vờ
/// khoẻ: ở môi trường thật, B2 đã chặn khởi động khi thiếu cấu hình nên nhánh này chỉ xảy ra
/// ở môi trường Testing (test host không nạp cấu hình storage thật).
/// </summary>
public sealed class ObjectStorageCredentialProbe(IOptions<MinioOptions> options, ILogger<ObjectStorageCredentialProbe> logger)
{
    /// <summary>Key chắc chắn không phải do ứng dụng tạo ra — chỉ dùng để hỏi storage một câu "ai đang hỏi?".</summary>
    private const string ProbeKey = ".healthcheck/credential-probe-does-not-exist";

    private readonly MinioOptions _options = options.Value;

    public async Task<StorageProbeResult> ProbeAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AccessKey) || string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            return StorageProbeResult.Healthy(
                "skipped: chưa cấu hình Minio AccessKey/SecretKey (chỉ xảy ra ở môi trường Testing — B2 chặn trường hợp này ở môi trường thật).");
        }

        try
        {
            using var client = new MinioClient()
                .WithEndpoint(_options.Endpoint)
                .WithCredentials(_options.AccessKey, _options.SecretKey)
                .WithSSL(_options.UseSsl)
                .Build();

            await client.StatObjectAsync(
                new StatObjectArgs().WithBucket(_options.Bucket).WithObject(ProbeKey), ct).ConfigureAwait(false);

            return StorageProbeResult.Healthy(
                $"Object storage OK: endpoint={_options.Endpoint}, bucket='{_options.Bucket}', credential hợp lệ.");
        }
        catch (ObjectNotFoundException)
        {
            // 404 chính là câu trả lời ĐÚNG: storage đã xác thực được credential rồi mới nói key không có.
            return StorageProbeResult.Healthy(
                $"Object storage OK: endpoint={_options.Endpoint}, bucket='{_options.Bucket}', credential hợp lệ.");
        }
        catch (BucketNotFoundException ex)
        {
            return StorageProbeResult.Unhealthy(
                $"Object storage không có bucket '{_options.Bucket}' tại {_options.Endpoint}. Cần tạo bucket hoặc sửa Minio:Bucket.",
                ex);
        }
        catch (AccessDeniedException ex)
        {
            // Đây CHÍNH LÀ tình huống B4 muốn bắt: cổng mở, credential sai.
            logger.LogWarning(ex, "Object storage từ chối truy cập (credential sai?) cho bucket {Bucket}", _options.Bucket);
            return StorageProbeResult.Unhealthy(
                "Object storage từ chối truy cập — AccessKey/SecretKey sai hoặc thiếu quyền. "
                + "Kiểm tra Minio:AccessKey / Minio:SecretKey.",
                ex);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return StorageProbeResult.Unhealthy(
                $"Object storage không truy cập được tại {_options.Endpoint} ({ex.GetType().Name}).",
                ex);
        }
    }
}
