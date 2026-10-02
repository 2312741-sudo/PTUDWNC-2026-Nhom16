namespace CulinaryBlog.Application;

/// <summary>
/// Dịch vụ lưu trữ đệm (Cache-Aside & Invalidation) cho Category, Discovery và Search (Task B5 - TV2).
/// Hỗ trợ fallback linh hoạt khi hệ thống cache gặp sự cố (Resilience).
/// </summary>
public interface IRecipeCacheService
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default);
    Task InvalidateAsync(string key, CancellationToken ct = default);
    Task InvalidatePrefixAsync(string prefix, CancellationToken ct = default);
}
