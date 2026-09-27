using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace Lab.TV3.Api.L4;

/// <summary>MinIO qua S3 API. Bucket riêng của lab (mặc định lab-tv3).</summary>
public sealed class ObjectStorage(IAmazonS3 s3, IConfiguration cfg)
{
    private volatile bool _bucketReady;

    public string Bucket => cfg["Minio:Bucket"] ?? "lab-tv3";

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (_bucketReady) return;
        if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3, Bucket)) await s3.PutBucketAsync(Bucket, ct);
        _bucketReady = true;
    }

    public async Task PutAsync(string key, Stream body, string contentType, CancellationToken ct)
    {
        await EnsureBucketAsync(ct);
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Bucket,
            Key = key,
            InputStream = body,
            ContentType = contentType,
            AutoCloseStream = false,
            UseChunkEncoding = false, // MinIO không cần chunk signing
        }, ct);
    }

    public async Task<MemoryStream> GetAsync(string key, CancellationToken ct)
    {
        using var res = await s3.GetObjectAsync(Bucket, key, ct);
        var ms = new MemoryStream();
        await res.ResponseStream.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }

    public Task DeleteAsync(string key, CancellationToken ct) => s3.DeleteObjectAsync(Bucket, key, ct);

    public async Task<bool> ExistsAsync(string key, CancellationToken ct)
    {
        try { await s3.GetObjectMetadataAsync(Bucket, key, ct); return true; }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound) { return false; }
    }
}
