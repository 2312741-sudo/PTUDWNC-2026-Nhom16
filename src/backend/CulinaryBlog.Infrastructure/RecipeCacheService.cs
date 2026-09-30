using System.Collections.Concurrent;
using System.Text.Json;
using CulinaryBlog.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CulinaryBlog.Infrastructure;

/// <summary>
/// N1-5 (tuần 4): cache dùng chung giữa nhiều API instance bằng Redis.
///
/// Vì sao phải đổi: bản cũ dùng <see cref="ConcurrentDictionary"/> — sống trong bộ nhớ của một
/// tiến trình, nên khi chạy 2 API instance thì instance này ghi còn instance kia không thấy
/// (và ngược lại). Hệ quả là cache "có" nhưng không bao giờ được dùng chung, còn yêu cầu
/// "cache phải dùng chung cho mọi instance" thì chỉ nằm trên giấy trong ADR.
///
/// Cơ chế: cache-aside đọc từ Redis, ghi cùng TTL. Nếu Redis không sẵn sàng thì KHÔNG ném lỗi ra
/// ngoài mà chạy factory thẳng (cache là tối ưu hoá, không được biến thành điểm chết của request),
/// đồng thời ghi cảnh báo để /health/ready báo Unhealthy và cảnh báo được nhìn thấy trên Seq.
///
/// Vẫn giữ một tầng bộ nhớ trong tiến trình: khi Redis chết, các request lặp lại cùng key vẫn
/// được phục vụ từ tầng này thay vì đâm thẳng vào PostgreSQL. Nhánh này cũng là nơi các unit
/// test cũ (Week3DiscoverySearchCacheTests) tiếp tục kiểm tra hành vi fallback.
///
/// <para><b>Lưu ý serialization:</b> giá trị đi qua JSON nên mọi loại đưa vào cache phải là
/// DTO/record có thể round-trip qua System.Text.Json. Nếu cache một entity có navigation loop
/// thì sẽ ném JsonException — xem <see cref="GetOrSetAsync{T}"/> đã bắt và fallback.</para>
/// </summary>
public sealed class RecipeCacheService : IRecipeCacheService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<RecipeCacheService> _logger;
    private readonly string _keyPrefix;
    private readonly IConnectionMultiplexer? _redis;
    private bool _simulateDown;
    private bool _ownsConnection;

    // Tầng dự phòng trong tiến trình: dùng khi Redis không có/không sống, và là nơi unit test cũ chạy.
    private readonly ConcurrentDictionary<string, (object Value, DateTime ExpiresAt)> _local = new();

    /// <param name="redis">
    /// Cho phép <c>null</c> — khi đó cache chạy thuần in-process (giữ được hành vi cho unit test
    /// cũ và cho trường hợp cố ý tắt Redis qua cấu hình).
    /// </param>
    public RecipeCacheService(
        ILogger<RecipeCacheService> logger,
        IConnectionMultiplexer? redis = null,
        IOptions<RedisOptions>? options = null)
    {
        _logger = logger;
        _redis = redis;
        _keyPrefix = $"{options?.Value.Instance ?? "dev"}:cache:";
    }

    /// <summary>Cấu hình chuẩn, tự tạo connection nếu DI chưa cấp (dùng cho test và cho tiến trình đơn).</summary>
    public static RecipeCacheService Create(ILogger<RecipeCacheService> logger, RedisOptions options)
    {
        var service = new RecipeCacheService(
            logger,
            ConnectionMultiplexer.Connect(BuildConfiguration(options)),
            Options.Create(options));
        service._ownsConnection = true;
        return service;
    }

    public static ConfigurationOptions BuildConfiguration(RedisOptions options)
    {
        var config = new ConfigurationOptions
        {
            AbortOnConnectFail = false,   // Không ném lúc khởi động: Redis chết thì app vẫn lên, /health/ready báo 503.
            ConnectRetry = 2,
            ConnectTimeout = 3000,
            SyncTimeout = 3000,
            DefaultDatabase = 0
        };
        config.EndPoints.Add(options.Host, options.Port);
        if (!string.IsNullOrWhiteSpace(options.Password)) config.Password = options.Password;
        return config;
    }

    private IDatabase? Db => _redis?.GetDatabase();

    public void SimulateServerDown(bool isDown) => _simulateDown = isDown;

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // Kiểm thử khả năng Fallback khi Cache Server gặp sự cố (Redis down).
        if (_simulateDown)
        {
            _logger.LogWarning("Cache service unavailable or degraded. Fallback to direct factory invocation for key: {CacheKey}", key);
            return await factory();
        }

        var db = Db;
        if (db is null) return await GetOrSetLocalAsync(key, factory, ttl);

        try
        {
            var redisKey = _keyPrefix + key;
            var cached = await db.StringGetAsync(redisKey).ConfigureAwait(false);
            if (cached.HasValue)
            {
                _logger.LogDebug("Cache HIT (redis) for key: {CacheKey}", key);
                return JsonSerializer.Deserialize<T>((string)cached!, JsonOptions)!;
            }

            _logger.LogDebug("Cache MISS (redis) for key: {CacheKey}", key);
            return await SetAndReturnAsync(db, redisKey, key, factory, ttl);
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException or RedisServerException)
        {
            // Redis hỏng KHÔNG được làm hỏng request: đọc tầng trong tiến trình rồi tính lại nếu cần.
            _logger.LogError(ex, "Redis unavailable, falling back to in-process cache for key: {CacheKey}", key);
            return await GetOrSetLocalAsync(key, factory, ttl);
        }
        catch (JsonException ex)
        {
            // Giá trị trong Redis không đọc được (đổi shape DTO giữa các lần deploy) — coi như cache miss
            // và ghi đè bằng giá trị mới, thay vì ném lỗi ra ngoài cho mọi request dính key đó.
            _logger.LogWarning(ex, "Cached value for key {CacheKey} is not deserializable; treating as miss.", key);
            return await SetAndReturnAsync(db, _keyPrefix + key, key, factory, ttl);
        }
    }

    private async Task<T> SetAndReturnAsync<T>(
        IDatabase db, string redisKey, string key, Func<Task<T>> factory, TimeSpan ttl)
    {
        var result = await factory().ConfigureAwait(false);
        if (result is not null)
        {
            var json = JsonSerializer.Serialize(result, JsonOptions);
            await db.StringSetAsync(redisKey, json, ttl).ConfigureAwait(false);
        }
        return result;
    }

    private async Task<T> GetOrSetLocalAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
    {
        if (_local.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow < entry.ExpiresAt)
            {
                _logger.LogDebug("Cache HIT (in-process) for key: {CacheKey}", key);
                return (T)entry.Value;
            }
            _local.TryRemove(key, out _);
        }

        try
        {
            _logger.LogDebug("Cache MISS (in-process) for key: {CacheKey}", key);
            var result = await factory();
            if (result is not null) _local[key] = (result, DateTime.UtcNow.Add(ttl));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache operation failed for key: {CacheKey}. Falling back to factory execution.", key);
            return await factory();
        }
    }

    public async Task InvalidateAsync(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _local.TryRemove(key, out _);
        var db = Db;
        if (db is not null)
        {
            try
            {
                await db.KeyDeleteAsync(_keyPrefix + key).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is RedisException or JsonException)
            {
                _logger.LogError(ex, "Redis invalidation failed for key: {CacheKey}", key);
            }
        }
        _logger.LogInformation("Cache invalidated for key: {CacheKey}", key);
    }

    public async Task InvalidatePrefixAsync(string prefix, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // Bộ nhớ trong tiến trình: lọc theo tiền tố.
        var keysToRemove = _local.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var k in keysToRemove) _local.TryRemove(k, out _);

        var db = Db;
        var removed = keysToRemove.Count;
        if (db is not null && _redis is not null)
        {
            try
            {
                // Redis không có "xoá theo mẫu" nên phải duyệt bằng SCAN (KeysAsync) — KHÔNG dùng
                // KEYS vì lệnh đó chạy đồng bộ và chặn server. pageSize 256 để không giữ nhiều key trong RAM.
                const int pageSize = 256;
                var pattern = (RedisValue)(_keyPrefix + prefix + "*");
                var batch = new List<RedisKey>(pageSize);
                foreach (var server in _redis.GetServers())
                {
                    if (!server.IsConnected || server.IsReplica) continue;
                    await foreach (var k in server.KeysAsync(db.Database, pattern, pageSize)
                        .WithCancellation(ct).ConfigureAwait(false))
                    {
                        batch.Add(k);
                        if (batch.Count < pageSize) continue;
                        removed += (int)await db.KeyDeleteAsync(batch.ToArray()).ConfigureAwait(false);
                        batch.Clear();
                    }
                }
                if (batch.Count > 0) removed += (int)await db.KeyDeleteAsync(batch.ToArray()).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is RedisException or JsonException)
            {
                _logger.LogError(ex, "Redis prefix invalidation failed for prefix: {Prefix}", prefix);
            }
        }

        _logger.LogInformation("Cache invalidated {Count} keys with prefix: {Prefix}", removed, prefix);
    }

    public void Dispose()
    {
        if (_ownsConnection) _redis?.Dispose();
    }
}
