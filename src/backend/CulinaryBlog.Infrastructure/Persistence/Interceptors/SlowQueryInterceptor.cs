using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Persistence.Interceptors;

/// <summary>
/// K22 / NFR-PERF-004 (TV3): cảnh báo SLOW_SQL khi một câu lệnh tới PostgreSQL chạy lâu hơn ngưỡng (mặc định 100 ms,
/// cấu hình <c>Perf:SlowQueryMs</c>). Chỉ ghi câu SQL (cắt 500 ký tự) và thời gian, KHÔNG ghi giá trị tham số (có thể chứa dữ liệu người dùng).
/// Thời gian là của EF (<see cref="CommandExecutedEventData.Duration"/>): với reader là tới lúc có dòng đầu, chưa gồm thời gian đọc hết.
/// </summary>
public sealed class SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger, int thresholdMs = 100) : DbCommandInterceptor
{
    public int ThresholdMs { get; } = thresholdMs;

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Check(command, eventData);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        Check(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Check(command, eventData);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Check(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        Check(command, eventData);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        Check(command, eventData);
        return ValueTask.FromResult(result);
    }

    private void Check(DbCommand command, CommandExecutedEventData eventData)
    {
        var elapsed = eventData.Duration.TotalMilliseconds;
        if (elapsed <= ThresholdMs) return;
        var sql = command.CommandText.Length > 500 ? command.CommandText[..500] + "…" : command.CommandText;
        logger.LogWarning("SLOW_SQL {ElapsedMs:F1} ms (> {ThresholdMs} ms): {Sql}", elapsed, ThresholdMs, sql);
    }
}
