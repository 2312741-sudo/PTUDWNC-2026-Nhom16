namespace CulinaryBlog.Application;

public interface IFileStorageService
{
    Task<StoredFile> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}

public sealed record StoredFile(string Key, string Url, string ContentType, long SizeBytes);
