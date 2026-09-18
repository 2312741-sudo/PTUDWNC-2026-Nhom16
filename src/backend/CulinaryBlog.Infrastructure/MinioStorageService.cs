using CulinaryBlog.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CulinaryBlog.Infrastructure;

/// <summary>
/// Triển khai IFileStorageService bằng MinIO SDK (D1/W2).
/// Key theo FR-RCP-008: {folder}/{uuid}.{ext} — ví dụ recipes/{recipeId}/{uuid}.{ext}.
/// Bucket private mặc định (D27); Url trả về là key (path tương đối), việc phục vụ ảnh
/// qua presigned/proxy được làm rõ trong IMAGE_CONTRACT.md theo quyết định D27.
/// Không nuốt lỗi im lặng: exception MinIO lan ra để handler ánh xạ 5xx/4xx phù hợp.
/// </summary>
public sealed class MinioStorageService : IFileStorageService
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
}
