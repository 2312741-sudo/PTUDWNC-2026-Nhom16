using System.Security.Cryptography;
using System.Text.Json;
using CulinaryBlog.Infrastructure;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// QD3-3b/3c — khóa ký JWT KHÔNG được nằm trong file đã commit.
///
/// Bối cảnh: khóa dev từng nằm trong `appsettings*.json`, ai đọc repo cũng ký được token. Khóa đó đã bị
/// thu hồi (rotate ngoài repo) và giờ bị chặn ở `JwtSettings.Validate()`. Test này chặn tái phát:
/// file cấu hình được track phải để khoá rỗng/placeholder, và khóa cũ không được xuất hiện ở đâu ngoài
/// danh sách chặn trong mã nguồn.
/// </summary>
public sealed class JwtSigningKeyNotCommittedTests
{
    /// <summary>Khoá dev đã bị commit lịch sử và được thu hồi — không được tái sử dụng.</summary>
    private const string RevokedDevKey =
        "development-secret-key-that-is-at-least-64-bytes-long-for-jwt-signing-256-bits-security";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "docker-compose.dev.yml")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }

    /// <summary>Các file cấu hình backend được commit vào repo.</summary>
    public static TheoryData<string> TrackedSettingsFiles
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var name in new[] { "appsettings.json", "appsettings.Development.json" })
                data.Add(Path.Combine("src", "backend", "CulinaryBlog.API", name));
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(TrackedSettingsFiles))]
    public void Tracked_appsettings_never_holds_a_real_jwt_signing_key(string relativePath)
    {
        var path = Path.Combine(RepoRoot(), relativePath);
        Assert.True(File.Exists(path), $"{relativePath} không tồn tại.");

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        Assert.True(
            doc.RootElement.TryGetProperty("Jwt", out var jwt),
            $"{relativePath} phải giữ mục \"Jwt\" (kể cả khi rỗng) để không lệ phụ thuộc ẩn.");

        var hasKey = jwt.TryGetProperty("SigningKey", out var key);
        var value = hasKey ? (key.ValueKind == JsonValueKind.String ? key.GetString() ?? "" : key.GetRawText()) : "";

        // Rỗng, vắng mặt, hoặc placeholder đều được chấp nhận — app sẽ fail-fast và bắt nhập từ secret.
        var isPlaceholder = string.IsNullOrWhiteSpace(value)
            || value.Contains("REPLACE_WITH", StringComparison.OrdinalIgnoreCase)
            || value.Contains("your", StringComparison.OrdinalIgnoreCase)
            || value.Contains("changeme", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("${", StringComparison.Ordinal)
            || value.StartsWith("(", StringComparison.Ordinal);

        Assert.True(isPlaceholder,
            $"{relativePath}: Jwt:SigningKey phải rỗng/placeholder, KHÔNG gán khoá thật trong file đã commit. "
            + $"Sinh khoá ngẫu nhiên và đặt qua biến môi trường Jwt__SigningKey hoặc dòng trong .env.");
    }

    [Fact]
    public void Revoked_dev_key_is_never_reused_as_a_signing_key()
    {
        // JwtSettings.Validate() chặn khoá này; test này chặn việc ai đó "tiện tay" dán lại vào cấu hình.
        var settings = new JwtSettings { SigningKey = RevokedDevKey };
        var ex = Assert.Throws<InvalidOperationException>(settings.Validate);
        Assert.Contains("thu hoi", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Jwt_settings_reject_missing_or_short_signing_key()
    {
        Assert.Throws<InvalidOperationException>(() => new JwtSettings { SigningKey = "" }.Validate());
        Assert.Throws<InvalidOperationException>(() => new JwtSettings { SigningKey = "   " }.Validate());
        Assert.Throws<InvalidOperationException>(() => new JwtSettings { SigningKey = new string('k', 63) }.Validate());
        Assert.Throws<InvalidOperationException>(() => new JwtSettings
        {
            SigningKey = new string('k', 64),
            Issuer = "",
        }.Validate());
    }

    [Fact]
    public void Jwt_settings_accept_a_fresh_64_byte_key()
    {
        // 48 byte ngẫu nhiên base64 = 64 ký tự, đúng bằng openssl rand -base64 48.
        var generated = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        Assert.Equal(64, generated.Length);
        new JwtSettings { SigningKey = generated }.Validate();   // không ném
    }

    [Fact]
    public void Revoked_key_appears_only_in_the_blocklist()
    {
        // Ngoại lệ duy nhất được phép: danh sách chặn trong JwtService.cs và chính test này
        // (cần giá trị gốc để khẳng định Validate() chặn đúng khoá đó).
        var allowed = new[]
        {
            Path.Combine("src", "backend", "CulinaryBlog.Infrastructure", "JwtService.cs"),
            Path.Combine("tests", "CulinaryBlog.Tests", "JwtSigningKeyNotCommittedTests.cs"),
        };
        var offenders = new List<string>();

        foreach (var file in TrackedTextFilesUnderRepo())
        {
            var text = File.ReadAllText(file);
            if (!text.Contains(RevokedDevKey, StringComparison.Ordinal)) continue;
            if (allowed.Any(a => Path.GetFullPath(file).EndsWith(a, StringComparison.OrdinalIgnoreCase))) continue;
            offenders.Add(Path.GetRelativePath(RepoRoot(), file));
        }

        Assert.True(offenders.Count == 0,
            "Khoá dev đã thu hồi chỉ được xuất hiện trong JwtService.cs (danh sách chặn). "
            + $"Xuất hiện ngoài danh sách ở: {string.Join(", ", offenders)}");
    }

    /// <summary>Chỉ quét file văn bản của mã nguồn/tài liệu — bỏ qua bin, obj, node_modules, file nhị phân.</summary>
    private static readonly string[] TextExtensions =
        [".cs", ".ts", ".tsx", ".js", ".mjs", ".json", ".md", ".yml", ".yaml", ".sh", ".ps1", ".txt", ".env", ".example"];

    private static IEnumerable<string> TrackedTextFilesUnderRepo()
    {
        var root = RepoRoot();
        var skip = new[] { Path.Combine(root, "node_modules"), Path.Combine(root, ".git"), "bin", "obj", ".next" };
        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(f =>
            {
                if (skip.Any(s => f.Contains(s, StringComparison.OrdinalIgnoreCase))) return false;
                var ext = Path.GetExtension(f);
                return TextExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)
                    || Path.GetFileName(f).StartsWith(".env", StringComparison.Ordinal);
            });
    }
}
