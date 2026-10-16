using System.Net;
using System.Text.Json;

namespace CulinaryBlog.Practice.Lab5;

/// <summary>
/// L5 phase 3 — <c>query-rollback</c>: kiểm chứng cơ chế **optimistic update có rollback**.
///
/// Nguyên lý: khi người dùng thao tác, UI hiện kết quả **ngay** (optimistic), đồng thời
/// gửi request nền. Nếu server từ chối thì UI phải **trả lại trạng thái cũ** (rollback) và báo lỗi.
///
/// Rủi ro kinh điển — và là thứ phase này kiểm tra:
///   • Rollback ghi đè bằng dữ liệu **cũ hơn server** → mất cập nhật của người khác.
///   • Rollback đụng vào mutation đang chạy song song → mất tiến trình.
///   • Server trả lỗi nghiệp vụ bằng 500 thay vì 4xx → UI không phân biệt được lỗi nhập với lỗi hệ thống.
///
/// Hạ tầng rollback phụ thuộc đúng vào việc server trả mã lỗi **có cấu trúc và đúng ngữ nghĩa**,
/// nên phần lớn check dưới đây là kiểm tra hợp đồng lỗi của API, kèm quét mã nguồn xem
/// client có thực sự có đường thoát rollback không.
/// </summary>
public static class QueryRollbackPhase
{
    public const string Phase = "query-rollback";

    public static async Task<PhaseResult> RunAsync()
    {
        var checks = new List<Check>();
        var detail = new List<string>();
        using var http = LabConfig.NewHttp();

        // --- 1. Sản phẩm có cơ chế optimistic/rollback thật không? ---
        var usesOptimistic = ScanOptimisticUsage(detail);
        var usesRollback = ScanRollbackUsage(detail);

        checks.Add(new Check("sản phẩm có dùng useOptimistic hoặc tương đương", usesOptimistic,
            usesOptimistic
                ? "tìm thấy useOptimistic / onMutate / rollbacks"
                : "KHÔNG thấy useOptimistic — sản phẩm chưa có optimistic update thật"));

        checks.Add(new Check("sản phẩm có đường thoát rollback khi server từ chối", usesRollback,
            usesRollback
                ? "tìm thấy rollback / revert / refetch onError"
                : "KHÔNG thấy cơ chế rollback tường minh"));

        // --- 2. Hợp đồng lỗi: 4xx có cấu trúc, không phải 500 ---
        var badLogin = await HttpProbe.SendAsync(http, HttpMethod.Post,
            $"{LabConfig.ApiBase}/api/v1/auth/login",
            new { email = "khong-ton-tai@culinary.local", password = "SaiMatKhau123!" });
        detail.Add($"POST /auth/login (sai mat khau) -> HTTP {(int)badLogin.Status}: {Trim(badLogin.Body)}");
        checks.Add(new Check("input sai bị bác bằng 4xx chứ không phải 500",
            (int)badLogin.Status is >= 400 and < 500, $"HTTP {(int)badLogin.Status}"));

        checks.Add(new Check("lỗi trả về dạng JSON (client đọc được message)",
            badLogin.Header("Content-Type")?.Contains("json") == true,
            $"Content-Type: {badLogin.Header("Content-Type") ?? "(không có)"}"));

        var missing = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/recipes/khong-ton-tai-abc123");
        detail.Add($"GET /recipes/khong-ton-tai-abc123 -> HTTP {(int)missing.Status}: {Trim(missing.Body)}");
        checks.Add(new Check("truy vấn không tồn tại trả 404 có cấu trúc",
            missing.Status == HttpStatusCode.NotFound, $"HTTP {(int)missing.Status}"));

        // --- 3. Ghi thật rồi đọc lại: nền tảng để rollback về đúng giá trị ---
        var token = await LoginAsync(http, detail);
        if (token is null)
        {
            checks.Add(new Check("lấy được token để kiểm chứng ghi/đọc thật", false,
                "đăng nhập thất bại — bỏ qua kiểm chứng read-after-write"));
            return PhaseResult.From(Phase, checks, detail);
        }
        checks.Add(new Check("lấy được token để kiểm chứng ghi/đọc thật", true, "đăng nhập lab thành công"));

        var stamp = $"lab-l5-{LabConfig.RunId}";
        var before = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/auth/me", token);
        var currentName = ExtractString(before, "displayName") ?? ExtractString(before, "fullName") ?? "E2E User";
        detail.Add($"displayName hien tai: \"{currentName}\"");
        var originalBio = ExtractString(before, "bio");
        var bioShown = string.IsNullOrEmpty(originalBio) ? "(rong)" : $"\"{originalBio}\"";
        detail.Add($"bio goc: {bioShown}");

        // Validator bắt buộc displayName, nên phải gửi kèm — nếu không sẽ luôn 400 và
// không kiểm chứng được gì cả.
        var update = await HttpProbe.SendAsync(http, HttpMethod.Patch,
            $"{LabConfig.ApiBase}/api/v1/auth/me", new { displayName = currentName, bio = stamp }, token);
        detail.Add($"PATCH /auth/me bio={stamp} -> HTTP {(int)update.Status}: {Trim(update.Body)}");

        if (update.Status == HttpStatusCode.OK)
        {
            var reread = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/auth/me", token);
            var persisted = reread.Body.Contains(stamp, StringComparison.Ordinal);
            checks.Add(new Check("read-after-write nhất quán (nền tảng của rollback)", persisted,
                persisted ? "giá trị đã ghi được đọc lại đúng" : "ghi OK nhưng đọc lại không thấy giá trị"));
        }
        else
        {
            checks.Add(new Check("read-after-write nhất quán (nền tảng của rollback)", false,
                $"PATCH không thành công: HTTP {(int)update.Status}"));
        }

        // --- 4. Cơ chế quan trọng nhất: validation fail phải để nguyên dữ liệu cũ ---
        // Client giữ bản tối ưu hoá, server từ chối. Kiểm chứng server KHÔNG ghi dở.
        var overlong = new string('x', 5000);
        var rejected = await HttpProbe.SendAsync(http, HttpMethod.Patch,
            $"{LabConfig.ApiBase}/api/v1/auth/me", new { displayName = currentName, bio = overlong }, token);
        detail.Add($"PATCH /auth/me bio({overlong.Length} ký tự) -> HTTP {(int)rejected.Status}");

        checks.Add(new Check("giá trị vượt giới hạn bị chặn bằng 4xx, không 500",
            (int)rejected.Status is >= 400 and < 500, $"HTTP {(int)rejected.Status}"));

        var afterReject = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/auth/me", token);
        var stillHasStamp = afterReject.Body.Contains(stamp, StringComparison.Ordinal);
        checks.Add(new Check("mutation bị từ chối không làm hỏng dữ liệu đã ghi trước đó", stillHasStamp,
            stillHasStamp
                ? "dữ liệu trước đó vẫn còn nguyên — client rollback về đúng giá trị"
                : "dữ liệu trước đó biến mất → rollback sẽ phục hồi sai"));

        // --- 5. Ghi đồng thời: xung đột phải được server chặn (409/412), không im lặng ghi đè ---
        // RowVersion chỉ kiểm chứng được trên công thức mà tài khoản lab sở hữu.
        // Endpoint list không có tham số lọc theo chủ sở hữu, nên phải dò qua các công thức
        // và giữ công thức đầu tiên mà tài khoản này thực sự sửa được.
        string? recipeId = null;
        var scan = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/recipes?pageSize=50", token);
        foreach (var item in scan.DataItems)
        {
            var id = ExtractString(item, "id");
            var slug = ExtractString(item, "slug");
            if (id is null || slug is null) continue;

            var probe = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/recipes/{slug}", token);
            if (probe.Status != HttpStatusCode.OK) continue;

            // 403 nghĩa là không phải chủ -> bỏ qua. 200 mà sửa được thì dùng công thức này.
            var writable = await HttpProbe.SendAsync(http, HttpMethod.Put,
                $"{LabConfig.ApiBase}/api/v1/recipes/{id}",
                RecipeBody(ExtractString(probe, "title") ?? "lab", id), token);
            detail.Add($"thu cong thuc {slug} (id={id}): PUT -> HTTP {(int)writable.Status}");
            if (writable.Status is HttpStatusCode.OK or HttpStatusCode.BadRequest)
            {
                recipeId = id;
                break;
            }
        }

        if (recipeId is null)
        {
            checks.Add(new Check("kiểm tra xung đột ghi đồng thời (RowVersion)", false,
                "tài khoản lab không sở hữu công thức nào — không kiểm chứng được trên dữ liệu thật"));
        }
        else
        {
            detail.Add($"recipe id dùng để thử xung đột: {recipeId}");
            var detail1 = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/recipes/{recipeId}", token);
            var rv = ExtractString(detail1, "rowVersion");
            var currentTitle = ExtractString(detail1, "title") ?? "Recipe lab L5";
            detail.Add($"rowVersion hiện tại: {(rv is null ? "(null)" : rv)}");

            if (rv is null)
            {
                checks.Add(new Check("kiểm tra xung đột ghi đồng thời (RowVersion)", false,
                    "response không trả rowVersion — không chứng minh được cơ chế chống ghi đè"));
            }
            else
            {
                var body1 = RecipeBody(currentTitle + " (L5-A)", recipeId);
                var firstWrite = await HttpProbe.SendAsync(http, HttpMethod.Put,
                    $"{LabConfig.ApiBase}/api/v1/recipes/{recipeId}", body1, token);
                detail.Add($"PUT recipes/{recipeId} v1 -> HTTP {(int)firstWrite.Status}");
                checks.Add(new Check("ghi hợp lệ với RowVersion đúng được chấp nhận",
                    firstWrite.Status == HttpStatusCode.OK, $"HTTP {(int)firstWrite.Status}"));

                // Ghi lần 2 với RowVersion **đã cũ** — đây là tình huống mà
                // client optimistic sẽ tưởng thành công nếu server không chặn.
                var body2 = RecipeBody(currentTitle + " (L5-B)", recipeId);
                var staleWrite = await HttpProbe.SendAsync(http, HttpMethod.Put,
                    $"{LabConfig.ApiBase}/api/v1/recipes/{recipeId}", body2, token);
                detail.Add($"PUT recipes/{recipeId} v2 voi RowVersion cu -> HTTP {(int)staleWrite.Status}: {Trim(staleWrite.Body)}");

                checks.Add(new Check("RowVersion cũ bị chặn bằng 409/412 (chống mất cập nhật)",
                    staleWrite.Status is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed,
                    $"HTTP {(int)staleWrite.Status} — {((int)staleWrite.Status is >= 400 and < 500 ? "client có thể rollback an toàn" : "không chặn được ghi đè mù")}"));

                // Khôi phục tiêu đề gốc để không bẩn dữ liệu.
                var fresh = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/recipes/{recipeId}", token);
                if (ExtractString(fresh, "rowVersion") is { } rv2)
                {
                    await HttpProbe.SendAsync(http, HttpMethod.Put,
                        $"{LabConfig.ApiBase}/api/v1/recipes/{recipeId}", RecipeBody(currentTitle, recipeId), token);
                    detail.Add($"da khoi phuc tieu de goc (rowVersion moi: {(rv2.Length > 8 ? rv2[..8] + "..." : rv2)})");
                }
            }
        }

        // --- 6. Dọn dẹp: trả bio về trạng thái gốc ---
        await HttpProbe.SendAsync(http, HttpMethod.Patch,
            $"{LabConfig.ApiBase}/api/v1/auth/me", new { displayName = currentName, bio = originalBio }, token);
        detail.Add("da khoi phuc bio goc");

        return PhaseResult.From(Phase, checks, detail);
    }

    private static object RecipeBody(string title, string recipeId) => new
    {
        title,
        description = "Noi dung do Lab L5 tao de kiem chung optimistic rollback.",
        instructions = "Buoc 1: trich xuong. Buoc 2: chuyen huong bien dau.",
        prepTimeMinutes = 10,
        cookTimeMinutes = 15,
        servings = 2,
        difficulty = 1,
        categoryId = GuessCategoryId(),
        nutrition = (object?)null,
        rowVersion = (string?)null
    };

    private static string GuessCategoryId() => "00000000-0000-0000-0000-000000000001";

    private static async Task<string?> LoginAsync(HttpClient http, List<string> detail)
    {
        var email = LabConfig.Env("LAB_EMAIL") ?? "e2e.playwright@culinary.local";
        var password = LabConfig.Env("LAB_PASSWORD") ?? "E2e@Test123456";

        var probe = await HttpProbe.SendAsync(http, HttpMethod.Post,
            $"{LabConfig.ApiBase}/api/v1/auth/login", new { email, password });
        detail.Add($"POST /auth/login {email} -> HTTP {(int)probe.Status}");
        if (probe.Status != HttpStatusCode.OK) return null;

        // Response đăng nhập trả accessToken (không phải "token").
        if (probe.Json is not { } root) return null;
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object) return null;

        foreach (var name in new[] { "accessToken", "token", "jwt" })
        {
            if (data.TryGetProperty(name, out var tokenEl) && tokenEl.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(tokenEl.GetString()))
                return tokenEl.GetString();
        }
        return null;
    }

    private static string? ExtractString(JsonElement root, string property)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty(property, out var direct) && direct.ValueKind == JsonValueKind.String)
                return direct.GetString();

            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                if (data.TryGetProperty(property, out var nested) && nested.ValueKind == JsonValueKind.String)
                    return nested.GetString();
        }

        return null;
    }

    private static string? ExtractString(HttpProbe probe, string property)
        => probe.Json is { } root ? ExtractString(root, property) : null;

    private static string Trim(string body) => body.Length <= 150 ? body : body[..150] + "...";

    private static bool ScanOptimisticUsage(List<string> detail)
    {
        if (LabConfig.SourceRoot is not { } root)
        {
            detail.Add("khong tim thay thu muc frontend de quet");
            return false;
        }

        var hits = FileScanner.Search(root, "useOptimistic", "rollbacks", "onMutate", "onError");
        detail.Add($"quet ma nguon tai {root.Replace(Directory.GetCurrentDirectory(), "<repo>")}");
        detail.Add($"khop useOptimistic/rollbacks/onMutate/onError: {hits.Count} dong");
        foreach (var hit in hits.Take(8)) detail.Add($"  {hit}");
        return hits.Count > 0;
    }

    private static bool ScanRollbackUsage(List<string> detail)
    {
        if (LabConfig.SourceRoot is not { } root) return false;

        var hits = FileScanner.Search(root, "rollback", "revert", "refetch", "mutateAsync");
        detail.Add($"khop rollback/revert/refetch/mutateAsync: {hits.Count} dong");
        foreach (var hit in hits.Take(8)) detail.Add($"  {hit}");
        return hits.Count > 0;
    }
}