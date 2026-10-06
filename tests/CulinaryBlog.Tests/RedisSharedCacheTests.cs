using System.Net.Sockets;
using CulinaryBlog.API;
using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// N1-5 (tuần 4): chứng minh cache dùng chung THẬT SỰ qua Redis, không phải trong bộ nhớ tiến trình.
///
/// Test quan trọng nhất là <see cref="Cache_written_by_one_service_is_read_by_another"/>. Bản cache
/// cũ dùng ConcurrentDictionary nên mỗi tiến trình có một bản riêng: cùng một key, service A ghi thì
/// service B vẫn phải gọi factory. Test này sẽ ĐỎ với bản cũ và XANH với bản Redis — đó là bằng
/// chứng cho yêu cầu "cache phải dùng chung cho mọi instance".
///
/// Cần Redis thật (docker compose -f docker-compose.dev.yml up -d redis, hoặc service redis trong CI)
/// nên theo đúng quy ước của repo: bỏ qua nếu chưa dựng.
/// </summary>
public sealed class RedisSharedCacheTests : IAsyncLifetime
{
    private readonly string _runId = Guid.NewGuid().ToString("N");
    private readonly List<RecipeCacheService> _services = new();

    private static RedisOptions OptionsFor(string instance) => new()
    {
        Host = EnvFileLoader.Get("REDIS_HOST", "127.0.0.1"),
        Port = int.TryParse(EnvFileLoader.Get("REDIS_PORT", "6379"), out var p) ? p : 6379,
        // Instance riêng cho mỗi lần chạy test để không đụng cache của dev hay của test khác.
        Instance = instance
    };

    private static bool RedisIsReachable()
    {
        try
        {
            using var tcp = new TcpClient();
            var options = OptionsFor("probe");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            tcp.ConnectAsync(options.Host, options.Port, cts.Token).GetAwaiter().GetResult();
            return tcp.Connected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Mỗi "instance API" là một đối tượng service riêng với connection riêng tới cùng Redis.</summary>
    private RecipeCacheService NewInstance()
    {
        var options = OptionsFor("test-" + _runId);
        var service = RecipeCacheService.Create(NullLogger<RecipeCacheService>.Instance, options);
        _services.Add(service);
        return service;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        foreach (var s in _services) s.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Cache_written_by_one_service_is_read_by_another()
    {
        if (!RedisIsReachable()) return;

        var first = NewInstance();
        var second = NewInstance();
        var key = "shared:" + _runId;
        var calls = 0;

        var firstResult = await first.GetOrSetAsync(
            key,
            () => { calls++; return Task.FromResult("gia-tri"); },
            TimeSpan.FromMinutes(5));

        var secondResult = await second.GetOrSetAsync(
            key,
            () => { calls++; return Task.FromResult("gia-tri-khac-neu-miss"); },
            TimeSpan.FromMinutes(5));

        Assert.Equal("gia-tri", firstResult);
        Assert.Equal("gia-tri", secondResult);
        // Factory chỉ được gọi đúng 1 lần: instance thứ hai phải đọc được từ Redis, không tính lại.
        Assert.Equal(1, calls);

        // Dọn dẹp key thật để không để lại rác trong Redis của máy/CI.
        await first.InvalidateAsync(key);
    }

    [Fact]
    public async Task Paged_result_survives_json_round_trip_in_redis()
    {
        if (!RedisIsReachable()) return;

        var first = NewInstance();
        var second = NewInstance();
        var key = "paged:" + _runId;

        var original = new PagedResult<SampleDto>(
            Array.Empty<SampleDto>(),
            PaginationMeta.Create(1, 10, 42));
        await first.GetOrSetAsync(key, () => Task.FromResult(original), TimeSpan.FromMinutes(5));
        var restored = await second.GetOrSetAsync<PagedResult<SampleDto>>(
            key,
            () => throw new InvalidOperationException("phai la cache hit"),
            TimeSpan.FromMinutes(5));

        Assert.Equal(42, restored.Meta.Total);
        Assert.Equal(1, restored.Meta.Page);
        Assert.Equal(original.Meta.TotalPages, restored.Meta.TotalPages);

        await first.InvalidateAsync(key);
    }

    [Fact]
    public async Task Entry_disappears_after_ttl()
    {
        if (!RedisIsReachable()) return;

        var service = NewInstance();
        var key = "ttl:" + _runId;
        var calls = 0;

        await service.GetOrSetAsync(key, () => { calls++; return Task.FromResult(1); }, TimeSpan.FromMilliseconds(300));
        await service.GetOrSetAsync(key, () => { calls++; return Task.FromResult(1); }, TimeSpan.FromMilliseconds(300));
        Assert.Equal(1, calls);

        await Task.Delay(600);
        await service.GetOrSetAsync(key, () => { calls++; return Task.FromResult(1); }, TimeSpan.FromMilliseconds(300));
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Invalidate_prefix_removes_matching_keys_and_keeps_others()
    {
        if (!RedisIsReachable()) return;

        var first = NewInstance();
        var second = NewInstance();
        var prefix = "p:" + _runId + ":";
        var matching = prefix + "a";
        var other = "keep:" + _runId;

        await first.GetOrSetAsync(matching, () => Task.FromResult("x"), TimeSpan.FromMinutes(5));
        await first.GetOrSetAsync(other, () => Task.FromResult("y"), TimeSpan.FromMinutes(5));

        // Instance khác gọi invalidate: phải xoá được key do instance kia ghi.
        await second.InvalidatePrefixAsync(prefix);

        var calls = 0;
        var afterInvalidate = await second.GetOrSetAsync(
            matching, () => { calls++; return Task.FromResult("x-moi"); }, TimeSpan.FromMinutes(5));
        Assert.Equal("x-moi", afterInvalidate);

        await first.GetOrSetAsync(other, () => { calls++; return Task.FromResult("y"); }, TimeSpan.FromMinutes(5));
        Assert.Equal(1, calls); // key không cùng prefix phải còn nguyên

        await first.InvalidateAsync(matching);
        await first.InvalidateAsync(other);
    }

    [Fact]
    public async Task Falls_back_to_factory_when_redis_is_unreachable()
    {
        // Cổng 1 không có gì lắng nghe ⇒ lỗi kết nối, không phụ thuộc Docker.
        var options = new RedisOptions { Host = "127.0.0.1", Port = 1, Instance = "test-down-" + _runId };
        using var service = RecipeCacheService.Create(NullLogger<RecipeCacheService>.Instance, options);

        var calls = 0;
        var result = await service.GetOrSetAsync(
            "k",
            () => { calls++; return Task.FromResult("ket-qua-tu-db"); },
            TimeSpan.FromMinutes(5));

        // Redis chết KHÔNG được làm hỏng request: vẫn trả kết quả từ factory.
        Assert.Equal("ket-qua-tu-db", result);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void BuildConfiguration_does_not_abort_when_redis_is_down()
    {
        // AbortOnConnectFail=false là điều kiện để app vẫn khởi động được khi Redis chết
        // (nếu không thì ConnectionMultiplexer.Connect ném ngay lúc DI resolve).
        var config = RecipeCacheService.BuildConfiguration(new RedisOptions { Host = "127.0.0.1", Port = 1 });
        Assert.False(config.AbortOnConnectFail);
        // "127.0.0.1" được phân giải thành IP nên StackExchange tạo IPEndPoint chứ không phải DnsEndPoint;
        // kiểm tra theo chuỗi để không phụ thuộc loại endpoint mà thư viện chọn.
        var endpoint = Assert.Single(config.EndPoints);
        Assert.EndsWith(":1", endpoint.ToString());
    }

    private sealed record SampleDto(int Id, string Name);
}
