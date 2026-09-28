using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// Regression guard cho bug 500 của POST /api/v1/recipes/{id}/images (báo cáo
/// docs/evidence/TV4/Tuan03/Report/BAO_CAO_LOI_UPLOAD_ANH_500.md): appsettings.Development.json
/// không có section Minio nên AccessKey/SecretKey rỗng, RustFS trả 401, MinioException
/// rơi vào nhánh generic của ApiExceptionHandler → 500 server.error.
/// Backend test không bắt được vì ApiFactoryWithMinio tự nạp credential qua in-memory config,
/// còn CI lại không hề chạy app với appsettings.Development.json → lệch cấu hình dev.
/// Test này khoá parity giữa appsettings.Development.json và docker-compose.dev.yml.
/// </summary>
public sealed class DevConfigParityTests
{
    [Fact]
    public void Development_appsettings_provides_non_empty_storage_credentials()
    {
        var minio = DevSettings().GetProperty("Minio");

        foreach (var key in new[] { "Endpoint", "Bucket", "AccessKey", "SecretKey" })
        {
            var value = minio.GetProperty(key).GetString();
            Assert.False(string.IsNullOrWhiteSpace(value), $"appsettings.Development.json: Minio:{key} phải có giá trị, nếu rỗng thì upload ảnh trả 500 server.error.");
        }
        Assert.False(minio.GetProperty("UseSsl").GetBoolean());
    }

    [Fact]
    public void Development_storage_credentials_match_docker_compose()
    {
        var minio = DevSettings().GetProperty("Minio");
        var compose = ComposeText();

        Assert.Equal(ComposeEnvDefault(compose, "RUSTFS_ACCESS_KEY"), minio.GetProperty("AccessKey").GetString());
        Assert.Equal(ComposeEnvDefault(compose, "RUSTFS_SECRET_KEY"), minio.GetProperty("SecretKey").GetString());
        Assert.Equal(EndpointFromCompose(compose), minio.GetProperty("Endpoint").GetString());
    }

    [Fact]
    public void Development_database_password_matches_docker_compose()
    {
        var password = DevSettings().GetProperty("ConnectionStrings").GetProperty("Database").GetString()!;
        var compose = ComposeText();

        Assert.Equal(ComposeEnvDefault(compose, "POSTGRES_PASSWORD"), DbPassword(password));
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

    private static string ComposeText() => File.ReadAllText(Path.Combine(RepoRoot(), "docker-compose.dev.yml"));

    private static JsonElement DevSettings()
    {
        var path = Path.Combine(RepoRoot(), "src", "backend", "CulinaryBlog.API", "appsettings.Development.json");
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement.Clone();
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "docker-compose.dev.yml")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
