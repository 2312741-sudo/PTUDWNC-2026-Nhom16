using System.Net;
using System.Text.Json;

namespace CulinaryBlog.Practice.Lab5;

/// <summary>
/// L5 phase 4 — <c>image-opt</c>: kiểm chứng tối ưu ảnh ở **cả hai tầng**.
///
/// Tầng backend (MinIO/S3): resize, định dạng, phục vụ qua proxy. Đây là nơi tiết kiệm
/// băng thông thật vì ảnh gốc có thể vài MB.
///
/// Tầng frontend (Next.js): <c>next/image</c> sinh URL <c>/_next/image?url=...&amp;w=...&amp;q=...</c>
/// để trình duyệt tải đúng kích thước hiển thị. Nếu dùng thẻ <c>&lt;img&gt;</c> thô thì tầng này
/// không có tác dụng gì.
///
/// Điểm lab muốn làm rõ: ảnh nhỏ đã được backend resize thì tầng frontend có còn cần thiết không,
/// hay chỉ là dư thừa. Phase ghi rõ cả hai quan sát thay vì kết luận theo cảm tính.
/// </summary>
public static class ImageOptPhase
{
    public const string Phase = "image-opt";

    public static async Task<PhaseResult> RunAsync()
    {
        var checks = new List<Check>();
        var detail = new List<string>();
        using var http = LabConfig.NewHttp();

        // --- Tầng 1: backend có resize ảnh không? ---
        var recipes = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/recipes?pageSize=20");
        var images = await CollectImageKeysAsync(recipes, http);
        detail.Add($"tim thay {images.Count} khoa anh tren API");

        if (images.Count == 0)
        {
            checks.Add(new Check("co du lieu anh de kiem chung", false,
                "API khong tra ve anh nao — khong kiem chung duoc tang backend"));
        }
        else
        {
            foreach (var key in images.Take(3)) detail.Add($"  key: {key}");

// Proxy ảnh chỉ phục vụ được object key trong MinIO (dạng `recipes/xxx.jpg`).
            // Giá trị `/images/...` là đường dẫn tĩnh phía frontend, không phải object key —
            // nên gọi proxy với nó sẽ 404 là đúng hành vi, không phải lỗi.
            var firstKey = images[0];
            var isObjectKey = !firstKey.StartsWith('/') && !firstKey.StartsWith("http", StringComparison.OrdinalIgnoreCase);
            detail.Add($"khoa anh la object key cua MinIO: {isObjectKey} (gia tri: {firstKey})");

            if (!isObjectKey)
            {
                checks.Add(new Check("ảnh được lưu trong object storage (MinIO/S3)", false,
                    $"ảnh trỏ tới \"{firstKey}\" — đường dẫn tĩnh, không phải object key nên không resize/proxy được"));
            }

            var proxyUrl = $"{LabConfig.ApiBase}/api/v1/resources/images/{Uri.EscapeDataString(firstKey)}";
            var proxy = await HttpProbe.GetAsync(http, proxyUrl);
            detail.Add($"GET proxy anh -> HTTP {(int)proxy.Status}, len={proxy.Body.Length}");

if (isObjectKey)
            {
                checks.Add(new Check("proxy anh phuc vu duoc", (int)proxy.Status is >= 200 and < 400,
                    $"HTTP {(int)proxy.Status}"));

                var contentType = proxy.Header("Content-Type");
                checks.Add(new Check("proxy anh tra dung Content-Type anh",
                    contentType?.StartsWith("image/") == true,
                    $"Content-Type: {contentType ?? "(khong co)"}"));

                checks.Add(new Check("proxy anh co ETag/Last-Modified de trinh duyet cache lai",
                    proxy.Header("ETag") is not null || proxy.Header("Last-Modified") is not null,
                    $"ETag: {proxy.Header("ETag") ?? "(khong co)"}, Last-Modified: {proxy.Header("Last-Modified") ?? "(khong co)"}"));
            }
            else
            {
                detail.Add($"bo qua kiem tra proxy/resize vi key \"{firstKey}\" khong phai object key");
                detail.Add($"proxy tra HTTP {(int)proxy.Status} — dung ky vong vi key nay khong ton tai trong MinIO");
            }

            // Kích thước biến thể: nếu backend resize, phải có key dạng *-medium/*-large.
            var medium = images.FirstOrDefault(k => k.Contains("medium", StringComparison.OrdinalIgnoreCase));
            if (medium is not null)
            {
                var mediumProbe = await HttpProbe.GetAsync(http,
                    $"{LabConfig.ApiBase}/api/v1/resources/images/{Uri.EscapeDataString(medium)}");
                detail.Add($"bien the medium -> HTTP {(int)mediumProbe.Status}, len={mediumProbe.Body.Length}");
                checks.Add(new Check("bien the anh resize (medium) phuc vu duoc",
                    (int)mediumProbe.Status is >= 200 and < 400, $"HTTP {(int)mediumProbe.Status}"));
            }
            else
            {
                checks.Add(new Check("có biến thể ảnh resize (medium/large)", false,
                    "khong thay khoa anh nao co hau to medium/large"));
            }

            // Ảnh không tồn tại phải trả lỗi có kiểm soát, không 500.
            var notFoundImage = await HttpProbe.GetAsync(http,
                $"{LabConfig.ApiBase}/api/v1/resources/images/khong-co-that-lab-l5.jpg");
            detail.Add($"GET proxy anh sai -> HTTP {(int)notFoundImage.Status}");
            checks.Add(new Check("anh khong ton tai tra 404 chu khong phai 500",
                (int)notFoundImage.Status is 404 or 400, $"HTTP {(int)notFoundImage.Status}"));
        }

        // --- Tầng 2: frontend co dung next/image khong? ---
        if (LabConfig.SourceRoot is { } src)
        {
            var (nextImageFiles, rawImgFiles) = FileScanner.CountImageUsage(src);
            detail.Add($"frontend: {nextImageFiles} file dung next/image, {rawImgFiles} file dung the img tho");

            checks.Add(new Check("frontend co dung next/image de toi uu anh o tang client",
                nextImageFiles > 0,
                nextImageFiles > 0 ? $"{nextImageFiles} file dung next/image" : "KHONG file nao dung next/image"));

            checks.Add(new Check("khong con anh bi render bang the img tho khong toi uu",
                rawImgFiles == 0,
                rawImgFiles == 0 ? "khong con img tho" : $"{rawImgFiles} file van dung <img> tho"));
        }
        else
        {
            checks.Add(new Check("quet duoc ma nguon frontend", false,
                "khong tim thay thu muc src/frontend/src"));
        }

        // --- Kiểm chứng ở tầng thực thi: HTML đã sinh _next/image chưa? ---
        var recipeUrl = await PickPublishedRecipePathAsync(http);
        if (recipeUrl is not null)
        {
            var page = await HttpProbe.GetAsync(http, recipeUrl);
            var usesNextImageRuntime = page.Body.Contains("/_next/image", StringComparison.Ordinal);
            detail.Add($"HTML {recipeUrl.Replace(LabConfig.WebBase, "")} co duong dan /_next/image: {usesNextImageRuntime}");

            checks.Add(new Check("HTML sinh ra da dung duong dan /_next/image cua Next.js",
                usesNextImageRuntime,
                usesNextImageRuntime
                    ? "tim thay /_next/image — toi uu anh dang chay that"
                    : "khong thay /_next/image — anh duoc tai nguyen ban, khong duoc resize theo man hinh"));

            // Ảnh phải có width/height hoặc fill/aspect-ratio để tránh CLS.
            var hasImgAttrs = page.Body.Contains("decoding=\"async\"", StringComparison.Ordinal)
                              || page.Body.Contains("loading=\"lazy\"", StringComparison.Ordinal);
            detail.Add($"trang co thuoc tich loading/decoding tren anh: {hasImgAttrs}");
            checks.Add(new Check("anh duoc gan loading/decoding de khong lam doi layout (CLS)", hasImgAttrs,
                hasImgAttrs ? "co thuoc tich lazy-loading" : "khong thay thuoc tich lazy-loading/decoding"));
        }
        else
        {
            checks.Add(new Check("lay duoc trang cong thuc de kiem chung anh o runtime", false,
                "khong tim thay cong thuc Published de kiem chung"));
        }

        return PhaseResult.From(Phase, checks, detail);
    }

    private static async Task<List<string>> CollectImageKeysAsync(HttpProbe probe, HttpClient http)
    {
var keys = new List<string>();

        // Endpoint list KHÔNG trả mảng `images`; phải gọi endpoint chi tiết theo từng slug.
        foreach (var item in probe.DataItems)
        {
            var slug = item.TryGetProperty("slug", out var s) ? s.GetString() : null;
            if (string.IsNullOrWhiteSpace(slug)) continue;

            var detail = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/recipes/{slug}");
            if (detail.Json is not { } root) continue;

            // Ảnh có thể nằm ở data.images hoặc data.primaryImageUrl.
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Array)
            {
                foreach (var img in images.EnumerateArray())
                {
                    var key = img.TryGetProperty("originalUrl", out var o) && o.ValueKind == JsonValueKind.String
                        ? o.GetString()
                        : img.TryGetProperty("url", out var u) && u.ValueKind == JsonValueKind.String ? u.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(key)) keys.Add(key);
                }
            }
            else if (root.TryGetProperty("primaryImageUrl", out var primary) &&
                     primary.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(primary.GetString()))
            {
                keys.Add(primary.GetString()!);
            }

            if (keys.Count > 0) break;
        }
        return keys;
    }

    private static async Task<string?> PickPublishedRecipePathAsync(HttpClient http)
    {
        var probe = await HttpProbe.GetAsync(http, $"{LabConfig.ApiBase}/api/v1/recipes?pageSize=20");
foreach (var item in probe.DataItems)
        {
            var slug = item.TryGetProperty("slug", out var s) ? s.GetString() : null;
            if (!string.IsNullOrWhiteSpace(slug)) return $"{LabConfig.WebBase}/recipes/{slug}";
        }
        return null;
    }
}