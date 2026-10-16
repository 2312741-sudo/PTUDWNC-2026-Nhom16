using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CulinaryBlog.Practice.Lab5;

/// <summary>Cấu hình lab L5 đọc từ env — không hard-code secret.</summary>
public static class LabConfig
{
    public static string RunId { get; } = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

    public static string OutDir { get; } = Path.Combine(Directory.GetCurrentDirectory(), "out");

    public static string LogFile => Path.Combine(OutDir, $"lab_l5_{RunId}.log");

    /// <summary>API sản phẩm (đang chạy) dùng cho các phase cần backend thật.</summary>
    public static string ApiBase { get; } = (Env("LAB_API") ?? "http://127.0.0.1:5080").TrimEnd('/');

    /// <summary>API thứ hai cho phase multi-instance (nếu chạy).</summary>
    public static string ApiBase2 { get; } = (Env("LAB_API2") ?? "http://127.0.0.1:5081").TrimEnd('/');

    /// <summary>Next.js production server (`next start`) — bắt buộc, vì ISR không hoạt động ở `next dev`.</summary>
    public static string WebBase { get; } = (Env("LAB_WEB") ?? "http://127.0.0.1:3000").TrimEnd('/');

    public static string RedisConnection { get; } = Env("LAB_REDIS") ?? "127.0.0.1:6379";

    public static string? TestDatabase
    {
        get
        {
            var raw = Env("LAB_APP_DB") ?? Env("TEST_DATABASE");
            return string.IsNullOrWhiteSpace(raw) ? null : raw;
        }
    }

    public static string SiteUrl { get; } = Env("LAB_SITE_URL") ?? WebBase;

    /// <summary>Gốc mã nguồn frontend — dùng cho các phase cần quét tĩnh (image-opt, query-rollback).</summary>
    public static string? SourceRoot
    {
        get
        {
            if (Env("LAB_SRC") is { } explicitRoot) return explicitRoot;
            // <repo>/practice/TV4/L5 -> <repo>
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (var i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "src", "frontend", "src");
                if (Directory.Exists(candidate)) return candidate;
            }
            return null;
        }
    }

    public static HttpClient NewHttp() => new() { Timeout = TimeSpan.FromSeconds(30) };

    public static string? Env(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

/// <summary>Tiện ích đọc response mà không nuốt lỗi (lab cần thấy cả case 4xx/5xx).</summary>
public sealed record HttpProbe(HttpStatusCode Status, string Body, Dictionary<string, string> Headers)
{
    public string? Header(string name)
    {
        var key = Headers.Keys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
        return key is null ? null : Headers[key];
    }

    public JsonElement? Json
    {
        get
        {
            try
            {
                return JsonDocument.Parse(Body).RootElement.Clone();
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// API trả list dưới vỏ <c>data</c>, có thể là mảng trực tiếp hoặc object chứa <c>items</c>.
    /// Chuẩn hoá về IEnumerable&lt;JsonElement&gt; để các phase không phải đoán cấu trúc.
    /// </summary>
    public IReadOnlyList<JsonElement> DataItems
    {
        get
        {
            if (Json is not { } root) return [];

            // { "data": [ ... ] }
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var data))
            {
                if (data.ValueKind == JsonValueKind.Array) return data.EnumerateArray().ToList();
                if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("items", out var inner) &&
                    inner.ValueKind == JsonValueKind.Array)
                    return inner.EnumerateArray().ToList();
            }

            // [ ... ]
            if (root.ValueKind == JsonValueKind.Array) return root.EnumerateArray().ToList();
            return [];
        }
    }

    public static async Task<HttpProbe> GetAsync(HttpClient http, string url, string? bearer = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        using var response = await http.SendAsync(request);
        return await ReadAsync(response);
    }

    public static async Task<HttpProbe> PostAsync(HttpClient http, string baseUrl,
        string path = "/api/v1/auth/login", object? jsonBody = null)
        => await SendAsync(http, HttpMethod.Post, baseUrl.TrimEnd('/') + path, jsonBody);

    public static async Task<HttpProbe> SendAsync(HttpClient http, HttpMethod method, string url,
        object? jsonBody = null, string? bearer = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        if (jsonBody is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(jsonBody), System.Text.Encoding.UTF8, "application/json");
        }
        using var response = await http.SendAsync(request);
        return await ReadAsync(response);
    }

    public static async Task<HttpProbe> SendRawAsync(HttpClient http, HttpMethod method, string url,
        ByteArrayContent? content, string contentType, string? bearer = null)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };
        request.Content!.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        using var response = await http.SendAsync(request);
        return await ReadAsync(response);
    }

    private static async Task<HttpProbe> ReadAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers) headers[header.Key] = string.Join(", ", header.Value);
        foreach (var header in response.Content.Headers) headers[header.Key] = string.Join(", ", header.Value);
        return new HttpProbe(response.StatusCode, body, headers);
    }
}