using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.API;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// N1-2 (tuần 4): /health/ready phải có kỳ vọng DỨT KHOÁT.
///
/// Trước đây HealthTests chấp nhận cả 200 lẫn 503 (Assert.Contains trên
/// { OK, ServiceUnavailable }) vì môi trường test không bảo đảm Redis có sống hay không —
/// nên test luôn xanh dù health check hỏng hoàn toàn, tức là test không bảo vệ được gì.
///
/// Nay mỗi tình huống được dựng thành một factory riêng với cấu hình tường minh:
///   - Redis chết        => bắt buộc 503 + checks.redis = Unhealthy
///   - Credential storage sai => bắt buộc 503 + checks.object-storage = Unhealthy (B4, #22)
///   - Mọi thứ khoẻ      => bắt buộc 200
/// Hai test cuối cùng cần dịch vụ thật nên theo đúng quy ước sẵn có của repo: bỏ qua (return)
/// khi dịch vụ chưa dựng, thay vì đổi kỳ vọng cho vừa mọi môi trường.
/// </summary>
public sealed class HealthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;
    private readonly HttpClient client;
    public HealthTests(ApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
        factory.EnsureMigrated();
    }

    private static async Task<JsonElement> ReadChecksAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body;
    }

    private static string StatusOf(JsonElement checks, string name)
        => checks.GetProperty(name).GetProperty("status").GetString() ?? "";

    private static string DescriptionOf(JsonElement checks, string name)
    {
        var check = checks.GetProperty(name);
        return check.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
    }

    [Fact]
    public async Task Liveness_always_reports_healthy()
    {
        var response = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
        Assert.True(body.GetProperty("checks").TryGetProperty("liveness", out _));
    }

    /// <summary>
    /// /health/ready phải chứa cả check credential storage (B4), còn /health (all) mới chứa
    /// thêm TCP probe "minio". Trước N1-1, "minio" nằm trong tag all nên readiness báo Healthy
    /// dù AccessKey sai — đúng lỗ hổng B4 chỉ ra.
    /// </summary>
    [Fact]
    public async Task Ready_includes_credential_storage_check_and_all_adds_tcp_probe()
    {
        using var healthy = new HealthyDependenciesApiFactory();
        using var healthyClient = healthy.CreateClient();

        var ready = await ReadChecksAsync(healthyClient, "/health/ready");
        var readyChecks = ready.GetProperty("checks");
        Assert.True(readyChecks.TryGetProperty("object-storage", out _),
            "/health/ready phải có check 'object-storage' (credential thật)");
        Assert.False(readyChecks.TryGetProperty("minio", out _),
            "TCP probe 'minio' là check phụ, chỉ nên xuất hiện ở /health (tag all)");

        var all = await ReadChecksAsync(healthyClient, "/health");
        var allChecks = all.GetProperty("checks");
        Assert.True(allChecks.TryGetProperty("minio", out _), "/health phải giữ TCP probe 'minio' làm chẩn đoán");
    }

    /// <summary>N1-2: Redis chết => /health/ready bắt buộc 503 và nêu đích danh check redis.</summary>
    [Fact]
    public async Task Ready_is_503_when_redis_is_down()
    {
        using var redisDown = new RedisDownApiFactory();
        using var redisDownClient = redisDown.CreateClient();

        var response = await redisDownClient.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var checks = body.GetProperty("checks");
        Assert.Equal("Unhealthy", StatusOf(checks, "redis"));
        // Liveness phải VẪN xanh: Redis chết thì chỉ bị loại khỏi vòng nhận traffic, không phải restart loop.
        var live = await redisDownClient.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }

    /// <summary>
    /// B4 (issue #22): cổng storage MỞ nhưng AccessKey sai => /health/ready bắt buộc 503.
    /// Đây chính là tình huống mà TCP probe cũ báo Healthy và chỉ lộ ra lúc người dùng bấm "Tải lên".
    /// Cần storage thật (TCP tới được) nên theo quy ước của repo: bỏ qua nếu chưa dựng.
    /// </summary>
    [Fact]
    public async Task Ready_is_503_when_object_storage_credential_is_wrong()
    {
        if (!await ApiFactoryWithMinio.MinioIsReachableAsync()) return;

        using var badCred = new BadCredentialApiFactory();
        using var badCredClient = badCred.CreateClient();

        var response = await badCredClient.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var checks = body.GetProperty("checks");
        Assert.Equal("Unhealthy", StatusOf(checks, "object-storage"));
        // Thông điệp phải chỉ ra nguyên nhân cấu hình để người trực xử lý biết sửa AccessKey/SecretKey.
        Assert.Contains("AccessKey", DescriptionOf(checks, "object-storage"), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Mọi phụ thuộc khoẻ => 200. Bỏ qua nếu máy/CI chưa dựng Redis hoặc storage.</summary>
    [Fact]
    public async Task Ready_is_200_when_all_dependencies_are_healthy()
    {
        if (!await ApiFactoryWithMinio.MinioIsReachableAsync()) return;
        if (!await RedisHealthCheckDefaults.TcpReachableAsync(RedisHealthCheckDefaults.Host, RedisHealthCheckDefaults.Port)) return;

        using var healthy = new HealthyDependenciesApiFactory();
        using var healthyClient = healthy.CreateClient();

        // Runner CI (và máy mới) khởi động RustFS với volume rỗng, chưa có bucket. Probe
        // credential cố ý phân biệt BucketNotFound (→ Unhealthy) với ObjectNotFound, nên
        // nếu để thiếu bucket thì test "khoẻ" sẽ đỏ một cách vô nghĩa.
        await EnsureBucketViaAppAsync(healthy);

        var response = await healthyClient.GetAsync("/health/ready");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var checks = body.GetProperty("checks");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", StatusOf(checks, "database"));
        Assert.Equal("Healthy", StatusOf(checks, "redis"));
        Assert.Equal("Healthy", StatusOf(checks, "object-storage"));
    }

    /// <summary>
    /// Đẩy một object nhỏ qua chính IObjectStorageWriter của app: MinioStorageService tự
    /// BucketExists + MakeBucket trước khi ghi, nên sau bước này bucket chắc chắn tồn tại.
    /// Dùng đường đi của app thay vì gọi Minio SDK trực tiếp để test không lệ thuộc package.
    /// </summary>
    private static async Task EnsureBucketViaAppAsync(HealthyDependenciesApiFactory factory)
    {
        try
        {
            using var scope = factory.Services.CreateScope();
            var writer = scope.ServiceProvider.GetRequiredService<IObjectStorageWriter>();
            using var stream = new MemoryStream([1, 2, 3]);
            await writer.UploadAsync(".healthcheck/bucket-bootstrap", stream, "application/octet-stream", 3);
        }
        catch
        {
            // Không tạo được thì để assert bên dưới báo rõ nguyên nhân, không che lỗi.
        }
    }
}

// ---------------------------------------------------------------------------
// Factory riêng cho từng tình huống health.
// ---------------------------------------------------------------------------

internal static class RedisHealthCheckDefaults
{
    public const string Host = "localhost";
    public const int Port = 6379;

    /// <summary>Port không có gì lắng nghe ⇒ nối bị từ chối ngay, không treo timeout.</summary>
    public const string DeadHost = "127.0.0.1";
    public const int DeadPort = 1;

    public static async Task<bool> TcpReachableAsync(string host, int port)
    {
        try
        {
            using var tcp = new System.Net.Sockets.TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await tcp.ConnectAsync(host, port, cts.Token);
            return tcp.Connected;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>Base chung: Testing + DB test + JWT test key, cấu hình thêm do lớp con ghép vào.</summary>
public abstract class HealthScenarioApiFactory : WebApplicationFactory<Program>
{
    protected abstract Dictionary<string, string?> ExtraConfig { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        EnvFileLoader.Load();
        var testDb = Environment.GetEnvironmentVariable("TEST_DATABASE");
        if (!string.IsNullOrWhiteSpace(testDb))
            builder.UseSetting("ConnectionStrings:Database", testDb);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = EnvFileLoader.Get("TEST_DATABASE",
                    "Host=127.0.0.1;Port=5432;Database=culinary_test;Username=postgres;Password=postgres"),
                ["Jwt:SigningKey"] = new string('t', 64)
            };
            foreach (var kv in ExtraConfig) settings[kv.Key] = kv.Value;
            config.AddInMemoryCollection(settings);
        });
    }
}

/// <summary>Redis trỏ vào cổng chết ⇒ check redis phải Unhealthy.</summary>
public sealed class RedisDownApiFactory : HealthScenarioApiFactory
{
    protected override Dictionary<string, string?> ExtraConfig => new()
    {
        ["HealthChecks:Redis:Host"] = RedisHealthCheckDefaults.DeadHost,
        ["HealthChecks:Redis:Port"] = RedisHealthCheckDefaults.DeadPort.ToString()
    };
}

/// <summary>DB + Redis + storage đều dùng cấu hình thật ⇒ mọi check khoẻ.</summary>
public sealed class HealthyDependenciesApiFactory : HealthScenarioApiFactory
{
    protected override Dictionary<string, string?> ExtraConfig => new()
    {
        ["HealthChecks:Redis:Host"] = RedisHealthCheckDefaults.Host,
        ["HealthChecks:Redis:Port"] = RedisHealthCheckDefaults.Port.ToString(),
        ["Minio:Endpoint"] = ApiFactoryWithMinio.MinioEndpoint,
        ["Minio:AccessKey"] = ApiFactoryWithMinio.MinioAccess,
        ["Minio:SecretKey"] = ApiFactoryWithMinio.MinioSecret,
        ["Minio:Bucket"] = ApiFactoryWithMinio.MinioBucket,
        ["Minio:UseSsl"] = "false"
    };
}
