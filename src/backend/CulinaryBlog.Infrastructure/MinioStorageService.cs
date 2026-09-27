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
public sealed record MediaContent(Stream Stream, string ContentType, long Length);

/// <summary>
/// Triển khai IFileStorageService bằng MinIO SDK (D1/W2).
/// Key theo FR-RCP-008: {folder}/{uuid}.{ext} — ví dụ recipes/{recipeId}/{uuid}.{ext}.
/// Bucket private mặc định (D27); Url trả về là key (path tương đối), việc phục vụ ảnh
/// qua presigned/proxy được làm rõ trong IMAGE_CONTRACT.md theo quyết định D27.
/// Không nuốt lỗi im lặng: exception MinIO lan ra để handler ánh xạ 5xx/4xx phù hợp.
/// </summary>
public sealed class MinioStorageService : IFileStorageService, IObjectStorageReader
{
    private readonly IMinioClient _client;
    private readonly MinioOptions _options;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(IOptions<MinioOptions> options, ILogger<MinioStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new MinioClient()
            .WithEndpoint(_options.Endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.UseSsl)
            .Build();
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

        var bucketExists = await _client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_options.Bucket), ct).ConfigureAwait(false);
        if (!bucketExists)
        {
            await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_options.Bucket), ct).ConfigureAwait(false);
            _logger.LogInformation("Created bucket {Bucket}", _options.Bucket);
        }

        await _client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(key)
                .WithStreamData(content)
                .WithObjectSize(content.Length)
                .WithContentType(contentType),
            ct).ConfigureAwait(false);

        return new StoredFile(key, key, contentType, content.Length);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await _client.RemoveObjectAsync(
            new RemoveObjectArgs().WithBucket(_options.Bucket).WithObject(key), ct).ConfigureAwait(false);
    }

    /// <summary>D27 proxy: stat rồi đọc object về MemoryStream. Object missing → null (404); MinIO down → exception lan ra (5xx).</summary>
    public async Task<MediaContent?> ReadAsync(string key, CancellationToken ct = default)
    {
        ObjectStat stat;
        try
        {
            stat = await _client.StatObjectAsync(
                new StatObjectArgs().WithBucket(_options.Bucket).WithObject(key), ct).ConfigureAwait(false);
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }

        var buffer = new MemoryStream();
        try
        {
            await _client.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_options.Bucket)
                    .WithObject(key)
                    .WithCallbackStream(async stream => { await stream.CopyToAsync(buffer, ct).ConfigureAwait(false); }),
                ct).ConfigureAwait(false);
        }
        catch (ObjectNotFoundException)
        {
            buffer.Dispose();
            return null;
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
}
