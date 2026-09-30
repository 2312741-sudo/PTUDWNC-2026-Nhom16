namespace CulinaryBlog.Infrastructure;

/// <summary>Cấu hình cache/lock dùng chung qua Redis (N1-5, N1-6 tuần 4).</summary>
public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>Host Redis. Rỗng thì bỏ qua Redis và chạy cache trong tiến trình.</summary>
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6379;
    public string Password { get; set; } = "";

    /// <summary>
    /// Tiền tố key. Bắt buộc khi chạy nhiều môi trường (dev/CI/prod) trên cùng một Redis
    /// để cache của môi trường này không đọc được dữ liệu của môi trường khác.
    /// </summary>
    public string Instance { get; set; } = "dev";

    /// <summary>Thời gian giữ khoá lock cho sitemap (N1-6) và cho thao tác ghi cache.</summary>
    public int LockSeconds { get; set; } = 300;
}
