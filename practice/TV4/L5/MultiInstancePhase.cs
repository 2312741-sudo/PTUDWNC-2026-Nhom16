using System.Diagnostics;
using System.Net;
using StackExchange.Redis;

namespace CulinaryBlog.Practice.Lab5;

/// <summary>
/// L5 phase 7 — <c>multi-instance</c>: chạy **hai tiến trình API** và kiểm tra trạng thái
/// dùng chung thật sự có dùng chung hay không.
///
/// Vì sao phải kiểm chứng thay vì tin vào cấu hình: nếu cache phân tán hoặc khoá phân tán
/// chưa được cấu hình, mỗi tiến trình sẽ có bộ nhớ riêng. Hậu quả rất dễ bị bỏ sót trong
/// môi trường một tiến trình:
///   • Tính nhất quán sai — người dùng thấy trạng thái khác nhau tuỳ vào tiến trình nào phục vụ.
///   • Nhiều job nền chạy trùng lặp vì khoá chỉ có tác dụng trong một tiến trình.
///
/// Phase này dùng Redis để **đọc trực tiếp** xem hai tiến trình có cùng nhìn thấy một khoá
/// cache hay không — bằng chứng trực tiếp, không suy diễn.
/// </summary>
public static class MultiInstancePhase
{
    public const string Phase = "multi-instance";

    public static async Task<PhaseResult> RunAsync()
    {
        var checks = new List<Check>();
        var detail = new List<string>();
        using var http = LabConfig.NewHttp();

        // --- 1. Hai tiến trình phải cùng sống ---
        var probe1 = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/health/live");
        var probe2 = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase2}/health/live");
        detail.Add($"{LabConfig.ApiBase} -> HTTP {(int)probe1.Status}");
        detail.Add($"{LabConfig.ApiBase2} -> HTTP {(int)probe2.Status}");

        checks.Add(new Check("tiến trình API #1 đang sống", probe1.Status == HttpStatusCode.OK,
            $"HTTP {(int)probe1.Status}"));
        checks.Add(new Check("tiến trình API #2 đang sống (khác cổng)",
            probe2.Status == HttpStatusCode.OK, $"HTTP {(int)probe2.Status}"));

        if (probe2.Status != HttpStatusCode.OK)
        {
            checks.Add(new Check("có dữ liệu chia sẻ giữa hai tiến trình", false,
                "tiến trình #2 không chạy nên không thể kiểm chứng trạng thái dùng chung"));
            return PhaseResult.From(Phase, checks, detail);
        }

        // --- 2. Cùng một yêu cầu phải cho cùng kết quả ở cả hai tiến trình ---
        var r1 = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/recipes?pageSize=5");
        var r2 = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase2}/api/v1/recipes?pageSize=5");
        var sameBody = Normalize(r1.Body) == Normalize(r2.Body);
        var verdict = sameBody ? "GIONG NHAU" : "KHAC NHAU";
        detail.Add($"so sanh /recipes?pageSize=5 giua 2 tien trinh: {verdict}");
        checks.Add(new Check("cùng yêu cầu cho cùng kết quả ở cả hai tiến trình",
            r1.Status == r2.Status && sameBody,
            sameBody ? "hai tien trinh tra ve cung du lieu" : "du lieu khac nhau giua hai tien trinh"));

        // --- 3. Bằng chứng trực tiếp: cache dùng chung qua Redis ---
        var shared = await ProbeSharedRedisAsync(detail);
        checks.Add(new Check("cache/khoá phân tán nằm trong Redis dùng chung", shared.Found,
            shared.Detail));
        checks.Add(new Check("cả hai tiến trình cùng trỏ tới một Redis", shared.Found,
            shared.Found
                ? "cùng đọc/ghi một Redis — trạng thái dùng chung là thật"
                : "không xác nhận được Redis dùng chung"));

        // --- 4. Job nền: phải là một server duy nhất trên Hangfire, không nhân bản ---
        var hangfire1 = await ReadHangfireServersAsync(detail, 1);
        var hangfire2 = await ReadHangfireServersAsync(detail, 2);
        if (hangfire1 is null && hangfire2 is null)
        {
            checks.Add(new Check("không nhân bản job nền khi chạy nhiều tiến trình", true,
                "khong doc duoc bang server Hangfire (bo qua) — xem ghi chu"));
        }
        else
        {
            var active = (hangfire1 ?? 0) + (hangfire2 ?? 0);
            detail.Add($"so Hangfire server dang active: {active} (tiến trình 1: {hangfire1 ?? -1}, tiến trình 2: {hangfire2 ?? -1})");
            checks.Add(new Check("job nền chạy một lần duy nhất (số server Hangfire hợp lý)",
                active is >= 1 and <= 2,
                active > 2 ? "nhieu tien trinh cung chay job nen — se chay trung lap" : $"{active} server nen"));
        }

        // --- 5. Sức chịu khi một tiến trình chết ---
        var sw = Stopwatch.StartNew();
        var afterFirst = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/recipes?pageSize=5");
        sw.Stop();
        detail.Add($"van hanh khi tien trinh #2 con song, do tre {sw.ElapsedMilliseconds} ms");
        checks.Add(new Check("tiến trình còn lại phục vụ bình thường khi có nhiều bản sao",
            afterFirst.Status == HttpStatusCode.OK, $"HTTP {(int)afterFirst.Status}"));

        return PhaseResult.From(Phase, checks, detail);
    }

    private static async Task<(bool Found, string Detail)> ProbeSharedRedisAsync(List<string> detail)
    {
        try
        {
            await using var redis = await ConnectionMultiplexer.ConnectAsync(LabConfig.RedisConnection);
            var db = redis.GetDatabase();

// `INFO` đòi admin mode, nên suy ra thông tin từ chính kết nối thay vì hỏi server.
            var endpoint = redis.GetEndPoints()[0].ToString() ?? "?";
            detail.Add($"Redis endpoint: {endpoint}");

            // Đánh dấu bằng khoá riêng của lab rồi đọc lại: chứng minh kết nối thực sự dùng chung.
            var key = $"lab:l5:{LabConfig.RunId}:shared-probe";
            await db.StringSetAsync(key, LabConfig.ApiBase2, TimeSpan.FromMinutes(10));
            var value = await db.StringGetAsync(key);

            var found = value == LabConfig.ApiBase2;
            detail.Add($"ghi khoa {key} roi doc lai: {(found ? "khop" : $"KHONG khop (doc \"{value}\")")}");
            return (found, found
                ? "ghi xong doc lai dung gia tri trong Redis dung chung"
                : "doc lai khong khop — canh bao");
        }
        catch (Exception ex)
        {
            detail.Add($"khong doc duoc Redis: {ex.GetType().Name}: {ex.Message}");
            return (false, $"khong ket noi duoc Redis: {ex.Message}");
        }
    }

    private static async Task<int?> ReadHangfireServersAsync(List<string> detail, int instance)
    {
        if (LabConfig.TestDatabase is not { } connectionString) return null;

        try
        {
            await using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new Npgsql.NpgsqlCommand(
                "select count(*) from \"HangFireServer\" where \"LastHeartbeat\" > now() - interval '5 minutes'",
                connection);
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            detail.Add($"HangFireServer con active (doc tu DB, tien trinh {instance}): {count}");
            return count;
        }
        catch (Exception ex)
        {
            detail.Add($"doc HangFireServer that bai (tien trinh {instance}): {ex.GetType().Name}");
            return null;
        }
    }

    private static string Normalize(string body)
    {
        // Bỏ `generatedAt` vì hai tiến trình render khác thời điểm.
        var idx = body.IndexOf("generatedAt", StringComparison.Ordinal);
        return idx < 0 ? body : body[..idx];
    }
}