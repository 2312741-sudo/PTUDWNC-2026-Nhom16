using System.Net.Sockets;
using CulinaryBlog.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace CulinaryBlog.Infrastructure;

/// <summary>
/// Đọc object ảnh MinIO cho proxy D27 (TV4) — TÁCH khỏi IFileStorageService để không phá contract bàn giao TV3
/// (HANDOFF mục 5.1: không sửa IFileStorageService/StoredFile). Object không tồn tại → trả null (endpoint ánh xạ 404);
/// MinIO down/các lỗi khác lan ra để handler ánh xạ 5xx.
/// </summary>
public interface IObjectStorageReader
{
    Task<MediaContent?> ReadAsync(string key, CancellationToken ct = default);
}

/// <summary>Nội dung object đọc từ MinIO (stream đã buffer, Content-Type, kích thước).</summary>
public sealed record MediaContent(Stream Stream, string ContentType, long Length) : IDisposable
{
    public void Dispose() => Stream?.Dispose();
}

/// <summary>
/// Ghi object MinIO theo key CHỦ ĐỘNG (D23 D2 resize cần key phái sinh ổn định {uuid}_300x300.jpg / {uuid}_800x600.jpg).
/// TÁCH khỏi IFileStorageService (HANDOFF 5.1) — không sửa UploadAsync/DeleteAsync tự sinh UUID. ExistsAsync để job idempotent.
/// </summary>
public interface IObjectStorageWriter
{
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task UploadAsync(string key, Stream content, string contentType, long length, CancellationToken ct = default);
}

/// <summary>
/// Triển khai IFileStorageService bằng MinIO SDK (D1/W2).
/// Key theo FR-RCP-008: {folder}/{uuid}.{ext} — ví dụ recipes/{recipeId}/{uuid}.{ext}.
/// Bucket private mặc định (D27); Url trả về là key (path tương đối), việc phục vụ ảnh
/// qua presigned/proxy được làm rõ trong IMAGE_CONTRACT.md theo quyết định D27.
/// </summary>
/// <remarks>
/// B1 (issue #20, N1-7): lỗi hạ tầng của object storage KHÔNG được trả về 500 server.error.
/// Mọi <see cref="MinioException"/> (kể cả AccessDenied / BucketNotFound) đi qua
/// <see cref="GuardAsync"/> để thành <see cref="AppException"/> 503 + code <c>storage.unavailable</c>
/// ⇒ người dùng biết đây là lỗi tạm thời và retry được.
/// Cố ý KHÔNG bọc <see cref="OperationCanceledException"/> (storage chậm nhưng vẫn ghi được —
/// báo 503 nhầm sẽ khiến client retry một thao tác đang chạy dở).
/// <see cref="ObjectNotFoundException"/> giữ nguyên ngữ nghĩa cũ: object không tồn tại ⇒ null (404).
/// B5 (issue #24): <see cref="IObjectStorageUrlSigner"/> cấp URL có chữ ký cho thẻ &lt;img&gt; (PA-3) —
/// tách khỏi proxy D27 vì proxy cần header Bearer mà thẻ img không gửi được.
/// </remarks>
public sealed class MinioStorageService : IFileStorageService, IObjectStorageReader, IObjectStorageWriter, IObjectStorageUrlSigner
{
    private readonly Lazy<IMinioClient> _client;
    private readonly MinioOptions _options;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(IOptions<MinioOptions> options, ILogger<MinioStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        // N2-C1d: Build() LAZY — sửa lỗi thật do B5 (issue #24) lộ ra.
        //
        // Vì sao phải lazy: `MinioClient.Build()` NÉM `MinioException: User Access Credentials not
        // initialized` khi AccessKey/SecretKey rỗng. Trước B5, hậu quả chỉ là endpoint upload 500 —
        // chấp nhận được vì upload đúng là cần storage. Nhưng B5 cho `GetRecipeBySlugHandler` tiêm
        // `IRecipeImageDtoFactory` → `IObjectStorageUrlSigner` → chính service này, nên MỌI lần đọc
        // công thức công khai cũng dựng service ⇒ chỉ cần thiếu credential storage là
        // `GET /recipes/{slug}` trả 500, tức hỏng cả endpoint không liên quan gì tới ảnh.
        // CI bắt được đúng lỗi này: workflow không có file .env nên không có `Minio__*`, còn máy
        // dev có trong .env nên test local vẫn xanh và che mất lỗi.
        //
        // Build() giờ chạy lần đầu khi thật sự gọi storage, tức nằm trong `GuardAsync`/catch của
        // từng thao tác ⇒ `MinioException` bị `IsStorageFailure` bắt và thành **503
        // storage.unavailable** đúng như tài liệu mô tả. `CreatePresignedUrlAsync` cũng bắt và trả
        // null, vì ảnh đã upload xong không được đổi thành lỗi 5xx.
        //
        // Fail-fast lúc khởi động KHÔNG bị mất: `MinioOptionsValidator` + `ValidateOnStart()` vẫn
        // chạy ở mọi môi trường trừ Testing, nên thiếu credential vẫn chết ngay khi boot như thiết
        // kế B2/N1-7. Lazy chỉ gỡ đúng trường hợp "service bị DI dựng trên đường đọc".
        _client = new Lazy<IMinioClient>(() => new MinioClient()
            .WithEndpoint(_options.Endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.UseSsl)
            .Build());
    }

    public async Task<StoredFile> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension))
            extension = "." + contentType[(contentType.IndexOf('/') + 1)..];

        var key = $"{folder.Trim('/').TrimEnd('/')}/{Guid.NewGuid():N}{extension}";

        await GuardAsync("upload", ct, async () =>
        {
            var bucketExists = await _client.Value.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_options.Bucket), ct).ConfigureAwait(false);
            if (!bucketExists)
            {
                await _client.Value.MakeBucketAsync(new MakeBucketArgs().WithBucket(_options.Bucket), ct).ConfigureAwait(false);
                _logger.LogInformation("Created bucket {Bucket}", _options.Bucket);
            }

            await _client.Value.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_options.Bucket)
                    .WithObject(key)
                    .WithStreamData(content)
                    .WithObjectSize(content.Length)
                    .WithContentType(contentType),
                ct).ConfigureAwait(false);
        }).ConfigureAwait(false);

        return new StoredFile(key, key, contentType, content.Length);
    }

    /// <summary>
    /// Số lần thử tối đa cho thao tác **xoá** object (N2-C1c: "ảnh resize/delete retry 3 lần").
    ///
    /// Vì sao xoá cần retry còn upload thì không: upload sinh key UUID mới mỗi lần nên thử lại sẽ
    /// tạo rác — vì vậy upload cố ý fail-fast trả 503 để client tự quyết định. Xoá thì idempotent:
    /// S3/MinIO `DELETE` trên object không tồn tại vẫn trả 204, nên thử lại không có tác dụng phụ.
    /// </summary>
    public const int DeleteAttempts = 3;

    /// <summary>Nghỉ giữa các lần thử xoá: 200ms rồi 400ms — đủ để vượt lỗi mạng chớp nhoáng, không làm treo request.</summary>
    private static readonly TimeSpan DeleteRetryDelay = TimeSpan.FromMilliseconds(200);

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await _client.Value.RemoveObjectAsync(
                    new RemoveObjectArgs().WithBucket(_options.Bucket).WithObject(key), ct).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (IsStorageFailure(ex, ct) && attempt < DeleteAttempts)
            {
                _logger.LogWarning(
                    "Xoá object {Key} lần {Attempt}/{Attempts} thất bại ({ErrorType}), thử lại",
                    key, attempt, DeleteAttempts, ex.GetType().Name);
                await Task.Delay(DeleteRetryDelay * attempt, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsStorageFailure(ex, ct))
            {
                _logger.LogError(ex, "Xoá object {Key} thất bại sau {Attempts} lần thử", key, DeleteAttempts);
                throw StorageUnavailable(ex, "delete");
            }
        }
    }

    /// <summary>D27 proxy: stat rồi đọc object về MemoryStream. Object missing → null (404); MinIO down → 503 storage.unavailable.</summary>
    public async Task<MediaContent?> ReadAsync(string key, CancellationToken ct = default)
    {
        ObjectStat stat;
        try
        {
            stat = await _client.Value.StatObjectAsync(
                new StatObjectArgs().WithBucket(_options.Bucket).WithObject(key), ct).ConfigureAwait(false);
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
        catch (Exception ex) when (IsStorageFailure(ex, ct))
        {
            throw StorageUnavailable(ex, "stat");
        }

        var buffer = new MemoryStream();
        try
        {
            // Callback của MinIO SDK là Action<Stream> (đồng bộ) — PHẢI copy đồng bộ.
            // Nếu truyền async lambda thì C# tạo async void (fire-and-forget): GetObjectAsync có thể trả về
            // trước khi copy xong => buffer cắt cụt (ảnh không decode được), và lỗi nền không ai quan sát
            // (ArgumentOutOfRangeException từ HttpConnection.CopyFromBufferAsync làm crash test host).
            await _client.Value.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_options.Bucket)
                    .WithObject(key)
                    .WithCallbackStream(stream => stream.CopyTo(buffer)),
                ct).ConfigureAwait(false);
        }
        catch (ObjectNotFoundException)
        {
            buffer.Dispose();
            return null;
        }
        catch (Exception ex) when (IsStorageFailure(ex, ct))
        {
            buffer.Dispose();
            throw StorageUnavailable(ex, "read");
        }

        if (buffer.Length != stat.Size)
        {
            var copied = buffer.Length;
            buffer.Dispose();
            throw new IOException($"Đọc object '{key}' không đầy đủ: {copied}/{stat.Size} bytes.");
        }

        buffer.Position = 0;
        var contentType = string.IsNullOrWhiteSpace(stat.ContentType) ? LookupContentType(key) : stat.ContentType;
        return new MediaContent(buffer, contentType, stat.Size);
    }

    private static string LookupContentType(string key)
    {
        return Path.GetExtension(key)?.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".avif" => "image/avif",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// B5 (issue #24): URL có chữ ký cho ảnh private để thẻ &lt;img&gt; tải được (proxy D27 cần Bearer).
    /// MinIO SDK tính chữ ký NGAY TRÊN MÁY (SigV4 là HMAC cục bộ) nên không có round-trip ra storage.
    /// ⛔ TUYỆT ĐỐI KHÔNG ghi URL trả về vào log — query string chứa X-Amz-Signature là bearer token
    /// (rò qua access log / lịch sử trình duyệt / Referer). Chỉ log số ký tự và kết quả.
    /// Lỗi (kể cả storage down) trả null: ảnh đã upload xong không được đổi thành lỗi 5xx.
    /// </summary>
    public async Task<string?> CreatePresignedUrlAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var expirySeconds = (int)TimeSpan.FromMinutes(
            Math.Clamp(_options.PresignedUrlExpiryMinutes, 1, MinioOptions.MaxPresignedUrlExpiryMinutes)).TotalSeconds;
        try
        {
            // MinIO 7.0.0: PresignedGetObjectAsync KHÔNG nhận CancellationToken — hạn tối đa 7 ngày,
            // ta luôn truyền <= 15 phút nên không vướng trần đó.
            ct.ThrowIfCancellationRequested();

            var presigned = await _client.Value.PresignedGetObjectAsync(
                new PresignedGetObjectArgs()
                    .WithBucket(_options.Bucket)
                    .WithObject(key)
                    .WithExpiry(expirySeconds)).ConfigureAwait(false);

            _logger.LogDebug("Issued presigned URL for {Bucket}/{Key} ({HasUrl}, expiry {ExpiryMinutes} min)",
                _options.Bucket, key, presigned is not null, _options.PresignedUrlExpiryMinutes);
            return presigned;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsStorageFailure(ex, ct))
        {
            _logger.LogError(ex, "Khong cap duoc URL co chu ky: presign {Bucket}/{Key} ({ErrorType})",
                _options.Bucket, key, ex.GetType().Name);
            return null;
        }
    }

    /// <summary>D23: object phái sinh {uuid}_300x300/_800x600 đã tồn tại chưa (idempotent — không ghi đè).</summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _client.Value.StatObjectAsync(
                new StatObjectArgs().WithBucket(_options.Bucket).WithObject(key), ct).ConfigureAwait(false);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (Exception ex) when (IsStorageFailure(ex, ct))
        {
            throw StorageUnavailable(ex, "exists");
        }
    }

    /// <summary>D23: ghi object với key chỉ định (resize job), tạo bucket nếu thiếu.</summary>
    public async Task UploadAsync(string key, Stream content, string contentType, long length, CancellationToken ct = default)
    {
        await GuardAsync("resize-upload", ct, async () =>
        {
            var bucketExists = await _client.Value.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_options.Bucket), ct).ConfigureAwait(false);
            if (!bucketExists)
            {
                await _client.Value.MakeBucketAsync(new MakeBucketArgs().WithBucket(_options.Bucket), ct).ConfigureAwait(false);
                _logger.LogInformation("Created bucket {Bucket}", _options.Bucket);
            }

            await _client.Value.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_options.Bucket)
                    .WithObject(key)
                    .WithStreamData(content)
                    .WithObjectSize(length)
                    .WithContentType(contentType),
                ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// B1 (issue #20): chạy một lệnh MinIO và bọc lỗi hạ tầng thành 503 storage.unavailable.
    /// Bọc cả MinioException lẫn lỗi I/O của SDK (HttpRequestException / SocketException / IOException /
    /// TimeoutException / TaskCanceledException do SDK hết thời gian chờ) vì "storage down" và
    /// "credential sai" có thể biểu hiện bằng những loại khác nhau — tất cả đều là lỗi tạm thời, retry được.
    /// KHÔNG bọc OperationCanceledException khi cancellation token của lời gọi đã bị huỷ
    /// (client ngắt kết nối / shutdown) — đó không phải lỗi hạ tầng.
    /// ObjectNotFoundException để lọt: caller tự ánh xạ thành 404.
    /// </summary>
    private async Task GuardAsync(string op, CancellationToken ct, Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception ex) when (IsStorageFailure(ex, ct))
        {
            throw StorageUnavailable(ex, op);
        }
    }

    private static bool IsStorageFailure(Exception ex, CancellationToken ct)
    {
        if (ex is ObjectNotFoundException)
            return false;

        if (ex is OperationCanceledException)
            return !ct.IsCancellationRequested;

        return ex is MinioException
            or HttpRequestException
            or IOException
            or TimeoutException
            or SocketException;
    }

    private AppException StorageUnavailable(Exception ex, string op)
    {
        _logger.LogError(ex, "Object storage thao tac that bai: {Op} {Bucket} ({ErrorType})",
            op, _options.Bucket, ex.GetType().Name);
        return new AppException(
            503,
            "storage.unavailable",
            "Dịch vụ lưu trữ ảnh tạm thời không khả dụng. Vui lòng thử lại sau.");
    }
}
