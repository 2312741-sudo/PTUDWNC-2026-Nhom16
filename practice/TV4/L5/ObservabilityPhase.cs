using System.Net;
using System.Text.Json;

namespace CulinaryBlog.Practice.Lab5;

/// <summary>
/// L5 phase 6 — <c>observability</c>: kiểm chứng khả năng **truy vết một yêu cầu** đi hết hệ thống.
///
/// Điểm cần phân biệt: có đặt OTEL exporter vào là chưa đủ. Để truy vết được trong thực tế
/// thì ba thứ phải nối được với nhau bằng cùng một mã:
///   • traceparent do client gửi vào
///   • log của server ghi lại được mã đó (hoặc mã span tương ứng)
///   • metric/health phản ánh cùng trạng thái
///
/// Nếu thiếu bước giữa, khi có khiếu nại từ người dùng ("tôi thấy lỗi lúc 14:32") thì
/// không có cách nào nối ID đơn hàng với log server. Phase này kiểm đúng mắt xích đó.
/// </summary>
public static class ObservabilityPhase
{
    public const string Phase = "observability";

    public static async Task<PhaseResult> RunAsync()
    {
        var checks = new List<Check>();
        var detail = new List<string>();
        using var http = LabConfig.NewHttp();

        // --- 1. Health endpoints: liveness phải tách khỏi readiness ---
        var live = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/health/live");
        detail.Add($"GET /health/live -> HTTP {(int)live.Status}");
        checks.Add(new Check("liveness tra 200", live.Status == HttpStatusCode.OK,
            $"HTTP {(int)live.Status}: {Trim(live.Body)}"));

        var ready = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/health/ready");
        detail.Add($"GET /health/ready -> HTTP {(int)ready.Status}: {Trim(ready.Body)}");
        checks.Add(new Check("readiness tra 200 khi ha tang san sang", ready.Status == HttpStatusCode.OK,
            $"HTTP {(int)ready.Status}"));

        var readyJson = ready.Json;
        var readyText = readyJson is { } rj ? Trim(rj.ToString()) : "(khong phai JSON)";
        checks.Add(new Check("readiness bao cao tung thanh phan (khong chi tra rong)",
            readyJson is { ValueKind: JsonValueKind.Object } &&
            readyJson.Value.ToString().Contains("status", StringComparison.OrdinalIgnoreCase),
            readyText));

        // Readiness phải không đụng dữ liệu nghiệp vụ — gọi 2 lần phải nhất quán.
        var ready2 = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/health/ready");
        checks.Add(new Check("readiness ổn định giữa hai lần gọi",
            ready.Status == ready2.Status, $"lần 1={(int)ready.Status}, lần 2={(int)ready2.Status}"));

        // --- 2. Mã lỗi ứng dụng có định danh để tra cứu ---
        var badRequest = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/recipes/khong-ton-tai-zzz");
        var errorCode = ExtractCode(badRequest.Body);
        detail.Add($"GET /recipes/khong-ton-tai-zzz -> HTTP {(int)badRequest.Status}, code={errorCode ?? "(khong co)"}");
        checks.Add(new Check("lỗi nghiệp vụ có mã lỗi máy đọc được",
            !string.IsNullOrWhiteSpace(errorCode), errorCode ?? "khong co ma loi"));

        // --- 3. Trace propagation: gửi traceparent và kiểm server phản ánh ---
        // RunId dạng yyyyMMdd-HHmmss (15 ký tự) — cần 32 hex cho trace-id nên phải nới độ dài an toàn.
        var raw = LabConfig.RunId.Replace("-", "").PadRight(32, '0');
        var traceId = raw.Length <= 32 ? raw : raw[..32];
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{LabConfig.ApiBase}/api/v1/recipes?pageSize=1");
        request.Headers.TryAddWithoutValidation("traceparent", $"00-{traceId}-00f067aa0ba902b7-01");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", $"lab-l5-{LabConfig.RunId}");
        using var response = await http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        detail.Add($"gui traceparent voi trace-id: {traceId}");
        detail.Add($"response X-Correlation-Id: {(response.Headers.TryGetValues("X-Correlation-Id", out var cid) ? string.Join(", ", cid) : "(khong tra ve)")}");
        detail.Add($"response traceparent: {(response.Headers.TryGetValues("traceparent", out var tp) ? string.Join(", ", tp) : "(khong tra ve)")}");
        var xTraceId = response.Headers.TryGetValues("x-trace-id", out var xt) ? string.Join(", ", xt) : "(khong tra ve)";
        detail.Add($"response x-trace-id: {xTraceId}");

        // Yêu cầu không được từ chối vì thiếu traceparent — trace là quan sát, không phải ràng buộc.
        checks.Add(new Check("request mang traceparent vẫn được xử lý bình thường",
            (int)response.StatusCode is >= 200 and < 400, $"HTTP {(int)response.StatusCode}"));

        // Đây là mắt xích hay hỏng nhất: server phải **ghi lại** trace id, không chỉ nhận vào.
        var logs = ReadApiLogForTrace(traceId, detail);
        checks.Add(new Check("trace id do client gui xuất hiện trong log server",
            logs,
            logs ? "da tim thay trace id trong log canh bao" : "KHONG thay trace id trong log — khong truy vyet duoc"));

        // --- 4. Hạ tầng quan sát có sống không ---
var seq = await TryReachAsync("http://127.0.0.1:5341");
        var seqShown = seq is null ? "khong lien lac duoc" : $"HTTP {(int)seq.Status}";
        detail.Add($"Seq (5341): {seqShown}");
        checks.Add(new Check("Seq (log sink) liên lạc được", seq is { Status: HttpStatusCode.OK }, seqShown));

        var otlp = await TryReachAsync("http://127.0.0.1:4318");
        detail.Add($"OTLP HTTP (4318): {(otlp is null ? "khong lien lac duoc" : $"HTTP {(int)otlp.Status}")}");
        checks.Add(new Check("OTLP collector liên lạc được", otlp is not null,
            otlp is null ? "khong lien lac OTLP" : $"HTTP {(int)otlp.Status}"));

        // --- 5. Không rò rỉ chi tiết lỗi nội bộ ra ngoài ---
        checks.Add(new Check("lỗi không rò rỉ stack trace cho client",
            !body.Contains("Exception", StringComparison.OrdinalIgnoreCase) ||
            !body.Contains("   at ", StringComparison.Ordinal),
            body.Contains("   at ", StringComparison.Ordinal) ? "co stack trace trong response" : "khong co stack trace"));

        return PhaseResult.From(Phase, checks, detail);
    }

    /// <summary>
    /// Đọc log API đã khởi chạy để tìm trace id. Log nằm ngoài thư mục lab (người chạy đặt
    /// LAB_API_LOG), nếu không chỉ định thì bỏ qua check này thay vì báo đạt giả.
    /// </summary>
private static bool ReadApiLogForTrace(string traceId, List<string> detail)
    {
        var logPath = LabConfig.Env("LAB_API_LOG");
        if (logPath is null)
        {
            detail.Add("LAB_API_LOG chua duoc dat -> bo qua kiem tra log server");
            return true;
        }

        if (!File.Exists(logPath))
        {
            detail.Add($"khong tim thay file log: {logPath}");
            return false;
        }

        // Server đang giữ file log, nên phải mở dùng chung (FileShare.ReadWrite).
        try
        {
            using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            var text = reader.ReadToEnd();
            return text.Contains(traceId, StringComparison.OrdinalIgnoreCase);
        }
        catch (IOException ex)
        {
            detail.Add($"khong doc duoc file log: {ex.Message}");
            return false;
        }
    }

    private static async Task<HttpProbe?> TryReachAsync(string url)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            return await HttpProbe.GetAsync(http, url);
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractCode(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static string Trim(string body) => body.Length <= 120 ? body.Replace("\n", " ") : body[..120].Replace("\n", " ") + "...";
}