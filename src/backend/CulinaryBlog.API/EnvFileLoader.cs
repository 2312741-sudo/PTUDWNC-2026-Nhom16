using DotNetEnv;

namespace CulinaryBlog.API;

/// <summary>
/// Nạp file <c>.env</c> của máy vào biến môi trường trước khi dựng host.
/// <para>
/// Lý do tồn tại: mô hình cấu hình của dự án là <b>default trong repo</b>
/// (<c>appsettings*.json</c>, <c>docker-compose.dev.yml</c>) và <b>giá trị thật của máy</b> trong
/// <c>.env</c> (đã gitignore). Không có lớp này thì <c>.env</c> chỉ có tác dụng với
/// <c>docker compose</c> — Docker tự nạp <c>.env</c> — còn API thì không, nên cấu hình trong
/// <c>.env</c> sẽ bị bỏ qua và app rơi về default (mất password đúng cho DB local).
/// </para>
/// <para>
/// An toàn: bỏ qua khi môi trường được khai báo tường minh là <c>Production</c>; chỉ tìm file ở
/// thư mục gốc repo trở lên; không ghi đè biến môi trường đã có sẵn (để CI/host vẫn ăn được giá
/// trị truyền vào). Gọi nhiều lần chỉ nạp một lần.
/// </para>
/// </summary>
public static class EnvFileLoader
{
    private const string FileName = ".env";
    private static int _loadAttempted;

    /// <summary>
    /// Nạp <c>.env</c> nếu tìm thấy. Trả về đường dẫn đã nạp, hoặc <c>null</c> nếu không nạp.
    /// Idempotent: lần gọi sau trả <c>null</c> và không nạp lại.
    /// </summary>
    public static string? Load()
    {
        if (Interlocked.Exchange(ref _loadAttempted, 1) == 1)
        {
            return null;
        }

        // Production tường minh (Render/Docker) không bao giờ đọc .env trên đĩa.
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var path = FindEnvFile();
        if (path is null)
        {
            return null;
        }

        // clobberExistingVars: false => biến đã set từ trước (CI secret, $env:...) thắng, .env chỉ bổ sung.
        // onlyExactPath: true => chỉ dùng đúng file đã tìm thấy, không dò tiếp thư mục cha (đã tự dò ở FindEnvFile).
        Env.Load(path: path, options: new LoadOptions(
            setEnvVars: true,
            clobberExistingVars: false,
            onlyExactPath: true));
        return path;
    }

    /// <summary>Đọc biến môi trường, nạp <c>.env</c> trước nếu chưa nạp, rồi trả <paramref name="fallback"/> nếu rỗng.</summary>
    public static string Get(string name, string fallback)
    {
        Load();
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string? FindEnvFile()
    {
        // `dotnet run --project ...` đặt CWD ở thư mục project nên phải dò ngược lên thư mục gốc repo.
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, FileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
