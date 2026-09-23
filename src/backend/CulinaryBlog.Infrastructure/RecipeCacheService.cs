using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure;

public interface IRecipeCacheService
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default);
    Task InvalidateAsync(string key, CancellationToken ct = default);
    Task InvalidatePrefixAsync(string prefix, CancellationToken ct = default);
}

public sealed class RecipeCacheService : IRecipeCacheService
{
    private readonly ILogger<RecipeCacheService> _logger;
    private readonly ConcurrentDictionary<string, (object Value, DateTime ExpiresAt)> _cache = new();
    private bool _simulateDown;

    public RecipeCacheService(ILogger<RecipeCacheService> logger)
    {
        _logger = logger;
    }

    public void SimulateServerDown(bool isDown) => _simulateDown = isDown;

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // Kiểm thử khả năng Fallback khi Cache Server gặp sự cố (Redis / Memory down)
        if (_simulateDown)
        {
            _logger.LogWarning("Cache service unavailable or degraded. Fallback to direct factory invocation for key: {CacheKey}", key);
            return await factory();
        }

        try
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow < entry.ExpiresAt)
                {
                    _logger.LogDebug("Cache HIT for key: {CacheKey}", key);
                    return (T)entry.Value;
                }
                _cache.TryRemove(key, out _);
            }

            _logger.LogDebug("Cache MISS for key: {CacheKey}", key);
            var result = await factory();
            if (result is not null)
            {
                _cache[key] = (result, DateTime.UtcNow.Add(ttl));
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache operation failed for key: {CacheKey}. Falling back to factory execution.", key);
            return await factory();
        }
    }

    public Task InvalidateAsync(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _cache.TryRemove(key, out _);
        _logger.LogInformation("Cache invalidated for key: {CacheKey}", key);
        return Task.CompletedTask;
    }

    public Task InvalidatePrefixAsync(string prefix, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var k in keysToRemove)
        {
            _cache.TryRemove(k, out _);
        }
        _logger.LogInformation("Cache invalidated {Count} keys with prefix: {Prefix}", keysToRemove.Count, prefix);
        return Task.CompletedTask;
    }
}
