using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace CulinaryBlog.API;

/// <summary>
/// FR-OBS-003 (W5-8): số đo tối thiểu scrape được ở định dạng Prometheus text mà **không** cần
/// thêm package exporter — tránh đổi <c>packages.lock.json</c> và làm hỏng CI đang restore
/// locked-mode. Bổ sung cho trace/metrics OTLP đã có (W5-2). Chỉ số nằm trong bộ nhớ tiến trình
/// nên reset khi restart; đủ để chứng minh request count / duration / error được thu thập thật.
/// </summary>
public sealed class RequestMetrics
{
    private long _total;
    private long _errors;
    private long _durationMs;
    private readonly ConcurrentDictionary<string, long> _byClass = new(StringComparer.Ordinal);

    public void Record(string method, int status, double elapsedMs)
    {
        Interlocked.Increment(ref _total);
        Interlocked.Add(ref _durationMs, (long)elapsedMs);
        if (status >= 500)
        {
            Interlocked.Increment(ref _errors);
        }
        _byClass.AddOrUpdate($"{method} {status / 100}xx", 1, static (_, v) => v + 1);
    }

    public string Render(string service)
    {
        var total = Interlocked.Read(ref _total);
        var errors = Interlocked.Read(ref _errors);
        var durationMs = Interlocked.Read(ref _durationMs);

        var sb = new StringBuilder();
        AppendCounter(sb, "http_requests_total", "Tổng số request HTTP đã phục vụ.", service, total);
        AppendCounter(sb, "http_request_errors_total", "Số request trả 5xx.", service, errors);
        AppendCounter(sb, "http_request_duration_ms_sum", "Tổng thời gian xử lý (ms).", service, durationMs);

        sb.Append("# HELP http_requests_by_class Số request theo method và lớp status.\n");
        sb.Append("# TYPE http_requests_by_class counter\n");
        foreach (var (key, value) in _byClass.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var parts = key.Split(' ');
            sb.Append("http_requests_by_class{service=\"").Append(service)
              .Append("\",method=\"").Append(parts[0])
              .Append("\",status_class=\"").Append(parts[1]).Append("\"} ")
              .Append(value.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }
        return sb.ToString();
    }

    private static void AppendCounter(StringBuilder sb, string name, string help, string service, long value)
    {
        sb.Append("# HELP ").Append(name).Append(' ').Append(help).Append('\n');
        sb.Append("# TYPE ").Append(name).Append(" counter\n");
        sb.Append(name).Append("{service=\"").Append(service).Append("\"} ")
          .Append(value.ToString(CultureInfo.InvariantCulture)).Append('\n');
    }
}
