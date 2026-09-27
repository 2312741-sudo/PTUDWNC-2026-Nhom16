using Npgsql;

namespace CulinaryBlog.Practice.Lab4;

/// <summary>Cấu hình lab đọc từ env (không hard-code secret).</summary>
public static class LabConfig
{
    public static string RunId { get; } = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

    public static string OutDir { get; } = Path.Combine(Directory.GetCurrentDirectory(), "out");

    public static string LogFile => Path.Combine(OutDir, $"lab-{RunId}.log");

    /// <summary>Connection maintenance DB (để CREATE DATABASE lab). Lấy từ LAB_POSTGRES, hoặc dựng từ TEST_DATABASE.</summary>
    public static string AdminConnection
    {
        get
        {
            var raw = Env("LAB_POSTGRES") ?? Env("TEST_DATABASE") ?? Env("ConnectionStrings__Database");
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException(
                    "Thiếu connection string Postgres. Đặt env LAB_POSTGRES (vd: \"Host=127.0.0.1;Port=5432;Database=postgres;Username=postgres;Password=***\") " +
                    "hoặc TEST_DATABASE trước khi chạy lab.");
            return new NpgsqlConnectionStringBuilder(raw) { Database = "postgres" }.ConnectionString;
        }
    }

    /// <summary>DB riêng cho lab (schema Hangfire riêng, không đụng DB sản phẩm).</summary>
    public static string LabDatabase { get; } = Env("LAB_DB_NAME") ?? "culinary_lab";

    public static string LabConnection
    {
        get
        {
            var builder = new NpgsqlConnectionStringBuilder(AdminConnection) { Database = LabDatabase };
            return builder.ConnectionString;
        }
    }

    /// <summary>DB ứng dụng (chỉ đọc) để sinh sitemap thật; null nếu không cấu hình.</summary>
    public static string? AppConnection
    {
        get
        {
            var raw = Env("LAB_APP_DB") ?? Env("TEST_DATABASE") ?? Env("ConnectionStrings__Database");
            if (string.IsNullOrWhiteSpace(raw)) return null;
            return new NpgsqlConnectionStringBuilder(raw) { Database = "culinary_test" }.ConnectionString;
        }
    }

    public static string SmtpHost => Env("LAB_SMTP_HOST") ?? "127.0.0.1";
    public static int SmtpPort => int.TryParse(Env("LAB_SMTP_PORT"), out var p) ? p : 1025;
    public static string SmtpApi => Env("LAB_SMTP_API") ?? "http://127.0.0.1:8025";
    public static string SmtpFrom => Env("LAB_SMTP_FROM") ?? "tv4-lab@culinary.local";
    public static string StorageBucket => Env("Minio__Bucket") ?? "culinary-blog";
    public static string MinioEndpoint => Env("Minio__Endpoint") ?? "127.0.0.1:9000";

    public static string LabKeyPrefix => $"lab/l4/{RunId}";

    private static string? Env(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
