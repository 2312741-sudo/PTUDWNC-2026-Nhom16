using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Persistence.Interceptors;

/// <summary>
/// K22 / NFR-PERF-004 (TV3): cảnh báo SLOW_SQL khi một câu lệnh tới PostgreSQL chạy lâu hơn ngưỡng (mặc định 100 ms).
/// </summary>
public sealed class SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger, int thresholdMs = 100) : DbCommandInterceptor
{
    public int ThresholdMs { get; } = thresholdMs;

    internal ILogger<SlowQueryInterceptor> Logger { get; } = logger;
}
