using Lab.TV3.Api.L5;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Lab.TV3.Tests;

public class L5PipelineTests
{
    /// <summary>
    /// Đầu-cuối trong bộ nhớ: AddMediatR + AddOpenBehavior, gửi qua ISender (không DB, không LabFactory).
    /// Chứng minh cả hai behavior thật sự nằm trong pipeline: request sai bị chặn TRƯỚC handler,
    /// request hợp lệ nhưng chậm thì sinh Warning.
    /// </summary>
    [Fact]
    public async Task Hai_behavior_chay_trong_pipeline_MediatR()
    {
        var clock = new ManualTimeProvider();
        var spy = new HandlerSpy();
        var log = new ListLogger<PerformanceBehavior<SampleRequest, string>>();

        var services = new ServiceCollection();
        services.AddSingleton(clock);
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(spy);
        services.AddSingleton(new PerformanceOptions(TimeSpan.FromMilliseconds(500)));
        services.AddSingleton<ILogger<PerformanceBehavior<SampleRequest, string>>>(log);
        services.AddSingleton<IValidator<SampleRequest>, NameRequiredValidator>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<L5PipelineTests>();
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
        });
        await using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        // Request sai -> ValidationBehavior chặn, handler không chạy
        var ex = await Assert.ThrowsAsync<LabValidationException>(() => sender.Send(new SampleRequest("")));
        Assert.Contains("Name là bắt buộc", ex.Errors);
        Assert.Equal(0, spy.Calls);

        // Request đúng nhưng chậm 600ms (> 500ms) -> handler chạy, PerformanceBehavior ghi Warning
        var response = await sender.Send(new SampleRequest("pho", WorkMs: 600));
        Assert.Equal("hello pho", response);
        Assert.Equal(1, spy.Calls);
        Assert.Single(log.Entries, e => e.Level == LogLevel.Warning);
    }
}
