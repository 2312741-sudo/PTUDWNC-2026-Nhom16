using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// Regression guard cho bug 500 của POST /api/v1/recipes/{id}/images (báo cáo
/// docs/evidence/TV4/Tuan03/Report/BAO_CAO_LOI_UPLOAD_ANH_500.md): appsettings.Development.json
/// không có section Minio nên AccessKey/SecretKey rỗng, RustFS trả 401, MinioException
/// rơi vào nhánh generic của ApiExceptionHandler → 500 server.error.
/// <para>
/// A1 / BUG-W4-02 (tuần 5): mật khẩu DB và storage credential đã bị gỡ khỏi
/// <c>appsettings*.json</c> (tracked) — giá trị thật nằm ở <c>.env</c> (gitignore). Vì vậy parity
/// được neo lại ở chỗ mới: appsettings giữ phần phi-secret (Endpoint/Bucket/UseSsl) và không
/// được chứa credential; credential thì so lệch giữa <c>.env.example</c> (mẫu người dùng copy)
/// và <c>docker-compose.dev.yml</c> (default hạ tầng dev) — lệch hai chỗ này chính là nguyên
/// nhân đã khiến cấu hình dev và hạ tầng dev không khớp nhau.
/// </para>
/// </summary>
public sealed class DevConfigParityTests
{
    [Fact]
    public void Tracked_appsettings_hold_no_hardcoded_credentials()
    {
        foreach (var name in new[] { "appsettings.json", "appsettings.Development.json" })
        {
            var root = JsonDocument.Parse(File.ReadAllText(ApiSettingsPath(name))).RootElement.Clone();
            Assert.False(root.TryGetProperty("ConnectionStrings", out _),
                $"{name}: không được chứa ConnectionStrings (mật khẩu DB) trong repo — giá trị thật nằm ở .env (đã gitignore), thiếu thì app fail-fast với thông báo cấu hình.");
        }

        var minio = DevSettings().GetProperty("Minio");
        Assert.False(minio.TryGetProperty("AccessKey", out _),
            "appsettings.Development.json: không được chứa Minio:AccessKey trong repo — lấy từ .env (Minio__AccessKey).");
        Assert.False(minio.TryGetProperty("SecretKey", out _),
            "appsettings.Development.json: không được chứa Minio:SecretKey trong repo — lấy từ .env (Minio__SecretKey).");
    }

    [Fact]
    public void Development_storage_endpoint_matches_docker_compose()
    {
        // Giữ guard của bug 500 ở lớp phi-secret: section Minio phải tồn tại và trỏ đúng hạ tầng.
        // Credential rỗng vì thiếu .env thì fail-fast ở MinioOptionsValidator (issue #21), không còn 500 âm thầm.
        var minio = DevSettings().GetProperty("Minio");
        var compose = ComposeText();

        foreach (var key in new[] { "Endpoint", "Bucket" })
        {
            var value = minio.GetProperty(key).GetString();
            Assert.False(string.IsNullOrWhiteSpace(value), $"appsettings.Development.json: Minio:{key} phải có giá trị.");
        }
        Assert.False(minio.GetProperty("UseSsl").GetBoolean());
        Assert.Equal(EndpointFromCompose(compose), minio.GetProperty("Endpoint").GetString());
    }

    [Fact]
    public void Env_example_credentials_match_docker_compose()
    {
        var compose = ComposeText();
        var env = EnvExampleSettings();

        Assert.Equal(ComposeEnvDefault(compose, "POSTGRES_PASSWORD"), DbPassword(env["ConnectionStrings__Database"]));
        Assert.Equal(ComposeEnvDefault(compose, "RUSTFS_ACCESS_KEY"), env["RUSTFS_ACCESS_KEY"]);
        Assert.Equal(ComposeEnvDefault(compose, "RUSTFS_SECRET_KEY"), env["RUSTFS_SECRET_KEY"]);
        Assert.Equal(ComposeEnvDefault(compose, "RUSTFS_ACCESS_KEY"), env["Minio__AccessKey"]);
        Assert.Equal(ComposeEnvDefault(compose, "RUSTFS_SECRET_KEY"), env["Minio__SecretKey"]);
    }

    private static string? DbPassword(string connectionString) =>
        Regex.Match(connectionString, @"Password=([^;]+)").Groups[1].Value is { Length: > 0 } p ? p : null;

    private static string EndpointFromCompose(string compose)
    {
        var port = Regex.Match(compose, @"9000:9000").Success ? "9000" : throw new InvalidOperationException("docker-compose.dev.yml không còn publish cổng 9000 cho object storage.");
        return $"localhost:{port}";
    }

    private static string ComposeEnvDefault(string compose, string key)
    {
        var match = Regex.Match(compose, $@"^\s*{key}:\s*\$\{{{key}:-(?<value>[^}}]+)\}}\s*$", RegexOptions.Multiline);
        if (match.Success) return match.Groups["value"].Value;

        match = Regex.Match(compose, $@"^\s*{key}:\s*(?<value>\S+)\s*$", RegexOptions.Multiline);
        Assert.True(match.Success, $"docker-compose.dev.yml không còn khai báo {key} — cập nhật test hoặc cấu hình.");
        return match.Groups["value"].Value.Trim('\'', '"');
    }

    private static Dictionary<string, string> EnvExampleSettings()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(Path.Combine(RepoRoot(), ".env.example")))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
            var eq = trimmed.IndexOf('=');
            if (eq <= 0) continue;
            result[trimmed[..eq]] = trimmed[(eq + 1)..];
        }
        return result;
    }

    private static string ComposeText() => File.ReadAllText(Path.Combine(RepoRoot(), "docker-compose.dev.yml"));

    private static JsonElement DevSettings()
    {
        return JsonDocument.Parse(File.ReadAllText(ApiSettingsPath("appsettings.Development.json"))).RootElement.Clone();
    }

    private static string ApiSettingsPath(string fileName) =>
        Path.Combine(RepoRoot(), "src", "backend", "CulinaryBlog.API", fileName);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "docker-compose.dev.yml")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
