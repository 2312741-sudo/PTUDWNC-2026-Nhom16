using System.Net;
using System.Text.Json;

namespace CulinaryBlog.Practice.Lab5;

/// <summary>
/// L5 phase 2 — <c>isr-detail</c>: kiểm chứng ISR (Incremental Static Regeneration) thật sự chạy.
///
/// ISR = render ở server **một lần**, cache lại, các request sau dùng bản cache cho tới khi hết hạn.
/// Dấu hiệu quan sát được do Next.js phát ra ở response header:
///   • <c>x-nextjs-cache</c>: <c>MISS</c> (render mới) → <c>STALE</c> (đã hết hạn, đang render nền) → <c>HIT</c> (dùng cache).
///   • <c>x-nextjs-revalidate</c>: số giây của <c>export const revalidate</c>.
///
/// Lưu ý quan trọng của phase này: phải chạy <c>next start</c> (production build).
/// Ở <c>next dev</c> Next.js bỏ qua hoàn toàn cache ISR, nên mọi kết luận về ISR
/// thu được khi chạy dev server là vô giá trị.
/// </summary>
public static class IsrDetailPhase
{
    public const string Phase = "isr-detail";

    public static async Task<PhaseResult> RunAsync()
    {
        var checks = new List<Check>();
        var detail = new List<string>();
        using var http = LabConfig.NewHttp();

        var slug = await PickPublishedSlugAsync(http);
        detail.Add($"slug dùng để kiểm chứng: {slug ?? "(không tìm thấy công thức Published)"}");

        if (slug is null)
        {
            checks.Add(new Check("có công thức Published để kiểm chứng ISR", false,
                "API không trả về công thức nào có status=Published — cần dữ liệu mẫu"));
            return PhaseResult.From(Phase, checks, detail);
        }

        var url = $"{LabConfig.WebBase}/recipes/{slug}";

        // --- Hợp đồng của môi trường: phải là production build ---
        var homeProbe = await HttpProbe.GetAsync(http, LabConfig.WebBase);
        var poweredBy = homeProbe.Header("X-Powered-By");
        var looksLikeDev = homeProbe.Body.Contains("next-dev", StringComparison.Ordinal);
        detail.Add($"X-Powered-By: {poweredBy ?? "(không có)"}");
        detail.Add($"buildId trong HTML: {(homeProbe.Body.Contains("\"buildId\"") ? "có" : "không")}");

        checks.Add(new Check("chạy production server (không phải next dev)",
                !looksLikeDev && poweredBy?.Contains("Next.js") == true,
                looksLikeDev ? "phát hiện next dev — ISR sẽ không hoạt động"
                             : $"X-Powered-By={poweredBy ?? "(không có)"}"));

        // --- Gọi 3 lần, đọc x-nextjs-cache để thấy chuyển trạng thái ---
        var cacheStates = new List<string>();
        HttpProbe? firstProbe = null;

        for (var i = 1; i <= 3; i++)
        {
            var probe = await HttpProbe.GetAsync(http, url);
            firstProbe ??= probe;
            var cacheState = probe.Header("x-nextjs-cache") ?? "(không có)";
            cacheStates.Add($"lần {i}: HTTP {(int)probe.Status}, x-nextjs-cache={cacheState}");

            // Route phải render thành công ở cả 3 lần.
            checks.Add(new Check($"lần {i} render trang chi tiết OK", probe.Status == HttpStatusCode.OK,
                $"HTTP {(int)probe.Status}, {probe.Body.Length} byte"));

            if (i == 1) await Task.Delay(400);
        }

        detail.AddRange(cacheStates);

        // --- ISR thật phải có chuyển trạng thái cache, không phải luôn MISS ---
        var observedCaching = cacheStates.Any(s => s.Contains("HIT") || s.Contains("STALE"));
        checks.Add(new Check("có bằng chứng cache ISR hoạt động (HIT/STALE)", observedCaching,
            observedCaching
                ? "response báo HIT/STALE → ISR đang cache và tái dụng"
                : "mọi request đều MISS/không có header → ISR KHÔNG hoạt động"));

        // --- Kiểm tra revalidate khớp với khai báo 300 giây ---
        var revalidateHeader = firstProbe!.Header("x-nextjs-revalidate");
        detail.Add($"x-nextjs-revalidate: {revalidateHeader ?? "(không có)"}");
        if (int.TryParse(revalidateHeader, out var seconds))
            checks.Add(new Check("revalidate khớp khai báo 300s", seconds == 300,
                $"header={seconds}s, khai báo export const revalidate = 300"));
        else
            checks.Add(new Check("revalidate khớp khai báo 300s", false,
                $"không đọc được x-nextjs-revalidate (nhận \"{revalidateHeader ?? "không có"}\")"));

        // --- Nội dung phải render ở server (không phải CSR) ---
        checks.Add(new Check("trang chi tiết server-rendered", firstProbe.Body.Contains("self.__next_f"),
            firstProbe.Body.Contains("self.__next_f")
                ? "HTML có RSC payload — render sẵn ở server"
                : "KHÔNG có RSC payload — có thể render client-side"));

        // --- Vòng đời cache: sau khi hết hạn phải revalidate, không phải trả cache vĩnh viễn ---
        detail.Add("Ghi chú: kiểm tra hết hạn 300s cần chờ đủ 5 phút — xem check 'Hết hạn' trong phần dưới.");
        checks.Add(new Check("cache không bị ghim vĩnh viễn (có Age/X-Cache-Miss)",
            firstProbe.Header("Age") is not null || observedCaching,
            $"Age: {firstProbe.Header("Age") ?? "(không có)"}; observedCaching={observedCaching}"));

        return PhaseResult.From(Phase, checks, detail);
    }

    private static async Task<string?> PickPublishedSlugAsync(HttpClient http)
    {
        var probe = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/recipes?pageSize=20");
        foreach (var item in probe.DataItems)
        {
            var slug = item.TryGetProperty("slug", out var s) ? s.GetString() : null;
            if (!string.IsNullOrWhiteSpace(slug)) return slug;
        }
        return null;
    }
}