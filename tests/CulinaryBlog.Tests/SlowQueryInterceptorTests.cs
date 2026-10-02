using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// K22 / NFR-PERF-004 (TV3): "Slow query log: cảnh báo khi query > 100ms". Chạy trên Postgres thật (chuỗi kết nối lấy từ app trong ApiFactory).
/// </summary>
public sealed class SlowQueryInterceptorTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private sealed class CapturingLogger : ILogger<SlowQueryInterceptor>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }

    private string ConnectionString()
    {
        factory.EnsureMigrated();
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.GetConnectionString()!;
    }

    private AuthDbContext NewContext(SlowQueryInterceptor interceptor) =>
        new(new DbContextOptionsBuilder<AuthDbContext>().UseNpgsql(ConnectionString()).AddInterceptors(interceptor).Options);

    [Fact]
    public async Task Query_slower_than_100ms_logs_one_SLOW_SQL_warning_with_sql_text_and_fast_query_logs_nothing()
    {
        var logger = new CapturingLogger();
        await using var db = NewContext(new SlowQueryInterceptor(logger, thresholdMs: 100));

        await db.Database.ExecuteSqlRawAsync("SELECT 1");
        await db.Recipes.AsNoTracking().Where(r => r.Id == Guid.NewGuid()).ToListAsync();
        Assert.Empty(logger.Entries);

        await db.Database.ExecuteSqlRawAsync("SELECT pg_sleep(0.15)");
        var (level, message) = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, level);
        Assert.Contains("SLOW_SQL", message);
        Assert.Contains("pg_sleep", message);
    }

    [Fact]
    public async Task Slow_reader_query_is_also_warned()
    {
        var logger = new CapturingLogger();
        await using var db = NewContext(new SlowQueryInterceptor(logger, thresholdMs: 100));

        await db.Database.SqlQueryRaw<int>("SELECT 1 AS \"Value\" FROM pg_sleep(0.15)").ToListAsync();

        Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("SLOW_SQL"));
    }

    [Fact]
    public void App_DbContext_registers_SlowQueryInterceptor_with_threshold_100ms()
    {
        factory.EnsureMigrated();
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<AuthDbContext>().GetService<IDbContextOptions>();
        var interceptors = options.FindExtension<CoreOptionsExtension>()?.Interceptors ?? [];

        var slow = Assert.Single(interceptors.OfType<SlowQueryInterceptor>());
        Assert.Equal(100, slow.ThresholdMs);
    }
}
