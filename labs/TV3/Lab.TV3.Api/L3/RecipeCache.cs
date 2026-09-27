using System.Text.Json;
using StackExchange.Redis;

namespace Lab.TV3.Api.L3;

public enum CacheStatus { Hit, Miss, Bypass }

/// <summary>
/// Cache-aside trên Redis. Mọi lỗi Redis -> log cảnh báo và đọc thẳng PostgreSQL (Bypass), không trả 500.
/// Invalidation search dùng "version key": tăng version là toàn bộ key search cũ hết hiệu lực, không cần SCAN.
/// </summary>
public sealed class RecipeCache(IConnectionMultiplexer redis, ILogger<RecipeCache> log)
{
    public const string OutputTag = "lab-recipes";
    private const string VersionKey = "lab:tv3:search:v";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string DetailKey(string slug) => $"lab:tv3:recipe:{slug}";

    public async Task<string> SearchKeyAsync(string q, string? category, int page, int size)
    {
        var (ok, v) = await TryAsync(db => db.StringGetAsync(VersionKey), "GET version");
        var version = ok && v.HasValue ? v.ToString() : "0";
        return $"lab:tv3:search:{version}:{Uri.EscapeDataString(q)}:{category}:{page}:{size}";
    }

    public async Task<(T Value, CacheStatus Status)> GetOrLoadAsync<T>(string key, Func<Task<T>> load, TimeSpan ttl)
    {
        var (ok, cached) = await TryAsync(db => db.StringGetAsync(key), "GET");
        if (ok && cached.HasValue) return (JsonSerializer.Deserialize<T>(cached.ToString(), Json)!, CacheStatus.Hit);

        var value = await load();
        if (!ok) return (value, CacheStatus.Bypass);
        var (setOk, _) = await TryAsync(db => db.StringSetAsync(key, JsonSerializer.Serialize(value, Json), ttl, When.Always), "SET");
        return (value, setOk ? CacheStatus.Miss : CacheStatus.Bypass);
    }

    public async Task InvalidateAsync(string? slug)
    {
        await TryAsync(db => db.StringIncrementAsync(VersionKey), "INCR version");
        if (slug is not null) await TryAsync(db => db.KeyDeleteAsync(DetailKey(slug)), "DEL detail");
    }

    private async Task<(bool Ok, T? Value)> TryAsync<T>(Func<IDatabase, Task<T>> op, string what)
    {
        try { return (true, await op(redis.GetDatabase())); }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            log.LogWarning("Redis không khả dụng ({What}) — fallback PostgreSQL: {Error}", what, ex.Message);
            return (false, default);
        }
    }
}