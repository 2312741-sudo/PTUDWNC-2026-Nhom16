using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>
/// Design-time factory để chạy `dotnet ef migrations add` / `database update` mà không cần startup host.
/// Đọc chuỗi kết nối từ biến môi trường CONNECTIONSTRINGS__DEFAULT, mặc định trỏ Postgres compose.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULT")
                   ?? "Host=localhost;Port=5432;Database=culinaryblog;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContextFactory).Assembly.FullName))
            .Options;

        return new ApplicationDbContext(options);
    }
}
