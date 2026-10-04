using System.Diagnostics;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace CulinaryBlog.Tests;

/// <summary>
/// N1-3 (tuần 4): chứng minh trace đi từ HTTP xuống DB thật, không phải chỉ cấu hình trong code.
///
/// Cách kiểm chứng: bật <see cref="ActivityListener"/> để thu span thật do instrumentation phát ra
/// khi gọi API, rồi kiểm tra cây trace: span ở tầng HTTP và span EF Core phải cùng TraceId.
///
/// Vì sao không chỉ kiểm tra "đã gọi AddEntityFrameworkCoreInstrumentation": cấu hình đúng nhưng
/// thiếu exporter/listener thì không ai thấy span nào, và đó đúng là tình trạng trước khi dựng
/// collector. Test này bắt đúng trường hợp đó — span không xuất hiện là test đỏ.
/// </summary>
public sealed class TracingObservabilityTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;
    private readonly ITestOutputHelper output;

    public TracingObservabilityTests(ApiFactory factory, ITestOutputHelper output)
    {
        this.factory = factory;
        this.output = output;
        factory.EnsureMigrated();
    }

    [Fact]
    public async Task Request_to_database_bearing_endpoint_produces_http_span_with_child_db_span()
    {
        // Nghe MỌI span của mọi nguồn: không lọc theo tên vì tên span đổi theo phiên bản thư viện,
        // lọc theo Source.Name mới ổn định.
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (activities) activities.Add(activity);
            }
        };
        ActivitySource.AddActivityListener(listener);

        using var client = factory.CreateClient();

        // Page NGẪU NHIÊN: cache discovery có TTL 15 phút và dùng chung Redis giữa các lần chạy
        // test, nên page cố định sẽ bị trả từ cache => không có truy vấn DB => không có span DB và
        // test chập chờn theo thứ tự chạy. Số ngẫu nhiên buộc phải cache miss, tức chắc chắn đi xuống DB.
        var randomPage = Random.Shared.Next(50, 1_000_000);
        var response = await client.GetAsync($"/api/v1/recipes?page={randomPage}&pageSize=5");

        Activity? httpSpan;
        List<Activity> dbSpans;
        List<Activity> snapshot;
        lock (activities)
        {
            httpSpan = activities.FirstOrDefault(a => a.Source.Name == "Microsoft.AspNetCore");
            dbSpans = activities
                .Where(a => a.Source.Name.Contains("EntityFrameworkCore", StringComparison.OrdinalIgnoreCase))
                .ToList();
            // Phải chụp bản sao trong `lock` rồi mới duyệt. Trước đây vòng `foreach` log bên dưới
            // duyệt thẳng `activities` **ngoài** lock, trong khi thread của HTTP server / EF Core vẫn
            // không ngừng `Add` ⇒ `InvalidOperationException: Collection was modified` ngẫu nhiên khi
            // chạy song song, và im lặng xanh khi chạy riêng test này.
            snapshot = activities.ToList();
        }

        foreach (var a in snapshot)
            output.WriteLine($"{a.Source.Name} :: {a.DisplayName} :: trace={a.TraceId} :: parent={a.ParentId}");

        Assert.True((int)response.StatusCode < 500, $"endpoint trả {response.StatusCode}, xem log test để biết nguyên nhân");

        // 1) Span tầng HTTP: instrumentation ASP.NET Core đang thật sự phát span.
        Assert.NotNull(httpSpan);

        // 2) Span DB: truy vấn PostgreSQL đã được instrumentation EF Core bắt.
        Assert.NotEmpty(dbSpans);

        // 3) Quan trọng nhất — "trace từ HTTP xuống DB": span DB phải nằm trong CÙNG trace với
        // span HTTP. Chỉ có span DB rời rạc thì chưa chứng minh được gì về việc truy vết xuyên suốt.
        Assert.Contains(dbSpans, a => a.TraceId == httpSpan!.TraceId);
    }

    [Fact]
    public async Task Redis_health_check_produces_a_client_span()
    {
        // Instrumentation HTTP client phải bắt được việc app nói chuyện với Redis — đây là
        // span cho thấy tracing không chỉ dừng ở DB mà còn theo được phụ thuộc ngoài.
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => { lock (activities) activities.Add(a); }
        };
        ActivitySource.AddActivityListener(listener);

        using var client = factory.CreateClient();
        await client.GetAsync("/health/ready");

        var deadline = DateTime.UtcNow.AddSeconds(5);
        List<Activity> redisSpans;
        do
        {
            lock (activities)
                redisSpans = activities.Where(a => a.DisplayName.Contains("6379", StringComparison.Ordinal)).ToList();
            if (redisSpans.Count == 0) await Task.Delay(100);
        }
        while (redisSpans.Count == 0 && DateTime.UtcNow < deadline);

        foreach (var a in redisSpans) output.WriteLine($"{a.Source.Name} :: {a.DisplayName}");
        Assert.NotEmpty(redisSpans);
    }
}
