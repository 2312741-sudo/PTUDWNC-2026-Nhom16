using System.Net;
using System.Text.Json;

namespace CulinaryBlog.Practice.Lab5;

/// <summary>
/// L5 phase 8 — <c>cqrs-behavior</c> (K04): kiểm chứng các **MediatR pipeline behavior** tối thiểu
/// mà backend thật đang chạy, quan sát được **qua HTTP** (không chỉ đọc code).
///
/// Trong sản phẩm (`CulinaryBlog.Application/Pipeline.cs`) có hai open behavior:
/// <list type="bullet">
///   <item><c>ValidationBehavior</c> — chạy FluentValidation trước handler, ném <c>ValidationException</c>
///   khi request sai ⇒ API map thành <b>400</b>.</item>
///   <item><c>LoggingBehavior</c> — bọc handler, ghi thời gian xử lý ra log/OTLP.</item>
/// </list>
/// Điểm lab muốn làm rõ: <b>behavior chạy thật hay chỉ nằm trong DI</b>. Cách kiểm không thể là đọc code,
/// mà là gửi command sai (phải bị chặn 400 với lỗi có cấu trúc) và command hợp lệ (phải tới được handler).
///
/// Giới hạn đã biết (ghi rõ thay vì kết luận theo cảm tính): <c>LoggingBehavior</c> chỉ phát ra log/OTLP
/// nên **không quan sát trực tiếp qua HTTP** — phase ghi nhận điều này thay vì giả vờ "đã kiểm chứng".
/// </summary>
public static class CqrsBehaviorPhase
{
    public const string Phase = "cqrs-behavior";

    public static async Task<PhaseResult> RunAsync()
    {
        var checks = new List<Check>();
        var detail = new List<string>();
        using var http = LabConfig.NewHttp();

        // --- 1) Query đi qua MediatR tới handler không lỗi ---
        var recipes = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/recipes?pageSize=5");
        detail.Add($"GET /recipes -> HTTP {(int)recipes.Status}, {recipes.DataItems.Count} muc");
        checks.Add(new Check("query đi qua MediatR tới handler và trả dữ liệu",
            recipes.Status == HttpStatusCode.OK,
            $"HTTP {(int)recipes.Status}"));

        // --- 2) ValidationBehavior chặn command sai (400) ---
        var invalid = await HttpProbe.SendAsync(http, HttpMethod.Post,
            $"{LabConfig.ApiBase}/api/v1/auth/register",
            new { email = "khong-phai-email", password = "123" });
        detail.Add($"POST /auth/register (sai) -> HTTP {(int)invalid.Status}");
        checks.Add(new Check("ValidationBehavior chặn command sai bằng HTTP 400",
            invalid.Status == HttpStatusCode.BadRequest,
            $"HTTP {(int)invalid.Status}"));

        // --- 3) Lỗi trả về là lỗi validation FluentValidation có cấu trúc ---
        // Nếu chỉ có model-binding của ASP.NET, body sẽ khác. FluentValidation gắn kèm danh sách field lỗi.
        var body = invalid.Body;
        var hasStructured = body.Contains("\"errors\"", StringComparison.OrdinalIgnoreCase)
                            || body.Contains("password", StringComparison.OrdinalIgnoreCase)
                            || body.Contains("email", StringComparison.OrdinalIgnoreCase);
        detail.Add($"body lỗi (200 ky tu): {Truncate(body, 200)}");
        checks.Add(new Check("lỗi trả về có cấu trúc FluentValidation (email/password)",
            hasStructured, hasStructured ? "có nhắc tới email/password hoặc errors" : "body không có dấu hiệu validation"));

        // --- 4) Command hợp lệ đi tới handler (login sai ⇒ 401, không phải 400/500) ---
        // Email đúng định dạng + mật khẩu đúng định dạng nhưng không tồn tại ⇒ qua validation, tới handler.
        var login = await HttpProbe.SendAsync(http, HttpMethod.Post,
            $"{LabConfig.ApiBase}/api/v1/auth/login",
            new { email = $"khong-ton-tai+{LabConfig.RunId}@lab.local", password = "Wrong@12345" });
        detail.Add($"POST /auth/login (khong ton tai) -> HTTP {(int)login.Status}");
        checks.Add(new Check("command hợp lệ đi qua pipeline tới handler (401, không 400/500)",
            login.Status == HttpStatusCode.Unauthorized,
            $"HTTP {(int)login.Status}"));

        // --- 5) Đối chiếu tĩnh: pipeline có đúng các behavior mong đợi ---
        var pipeline = FindPipelineFile();
        if (pipeline is not null)
        {
            var text = await File.ReadAllTextAsync(pipeline);
            var behaviors = text.Split("IPipelineBehavior").Length - 1;
            var hasValidation = text.Contains("ValidationBehavior", StringComparison.Ordinal);
            var hasLogging = text.Contains("LoggingBehavior", StringComparison.Ordinal);
            var hasCaching = text.Contains("CachingBehavior", StringComparison.OrdinalIgnoreCase);
            detail.Add($"Pipeline.cs: {behaviors} tham chieu IPipelineBehavior (validation={hasValidation}, logging={hasLogging}, caching={hasCaching})");
            checks.Add(new Check("pipeline có behavior validation + logging tối thiểu",
                hasValidation && hasLogging, $"validation={hasValidation}, logging={hasLogging}"));
            detail.Add(hasCaching
                ? "co CachingBehavior trong pipeline"
                : "KHONG co CachingBehavior — K04 chi yeu cau toi thieu (logging/validation), caching la tuy chon");
        }
        else
        {
            checks.Add(new Check("tim thay Pipeline.cs de doi chieu behavior", false,
                "khong tim thay src/backend/CulinaryBlog.Application/Pipeline.cs"));
        }

        detail.Add("GIOI HAN: LoggingBehavior phat ra log/OTLP, KHONG quan sat truc tiep qua HTTP " +
                   "(chi suy ra duoc rang pipeline co chay khi command toi duoc handler).");

        return PhaseResult.From(Phase, checks, detail);
    }

    private static string? FindPipelineFile()
    {
        if (LabConfig.SourceRoot is not { } src) return null;
        // <repo>/src/frontend/src -> <repo>
        var repo = Path.GetFullPath(Path.Combine(src, "..", "..", ".."));
        var candidate = Path.Combine(repo, "src", "backend", "CulinaryBlog.Application", "Pipeline.cs");
        return File.Exists(candidate) ? candidate : null;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value.Replace('\n', ' ') : value[..max].Replace('\n', ' ') + "...";
}
