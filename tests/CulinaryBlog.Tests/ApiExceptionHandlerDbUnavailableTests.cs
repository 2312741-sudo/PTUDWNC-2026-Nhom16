using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Diagnostics;
using Npgsql;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// N2-C1 — lỗi hạ tầng PostgreSQL phải trả <b>503 database.unavailable</b>, không phải 500 server.error.
///
/// Bối cảnh: outage drill (deploy/outage-drill.ps1, log c1_outage_drill.log) chạy API với connection
/// string trỏ port không có gì lắng nghe. Cả 3 endpoint đọc trả <b>500</b>, đúng loại lỗi mà báo cáo
/// docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md đã đặt mục tiêu loại bỏ.
///
/// Nguyên nhân gốc: <c>EfUnitOfWork</c> chạy lệnh qua execution strategy của EF, nên NpgsqlException
/// gốc bị bọc thành <c>InvalidOperationException</c>. Test này bọc lại đúng cách đó — nếu ai đó đơn
/// giản hoá thành <c>exception is NpgsqlException</c> thì test sẽ đỏ.
///
/// Test cũng khoá ranh giới quan trọng: <see cref="DbUpdateConcurrencyException"/> là xung đột optimistic
/// concurrency (D19) và PHẢI trả 422 để client tải lại, không được thành 503.
/// </summary>
public sealed class ApiExceptionHandlerDbUnavailableTests
{
    private static async Task<(HttpContext Http, CapturingProblemDetailsService Problems)> HandleAsync(Exception exception)
    {
        var http = new DefaultHttpContext { RequestServices = new StubServiceProvider() };
        http.Request.Path = "/api/v1/recipes";
        http.Response.Body = new MemoryStream();

        var problems = new CapturingProblemDetailsService();
        var handler = new ApiExceptionHandler(problems, NullLogger<ApiExceptionHandler>.Instance);
        await handler.TryHandleAsync(http, exception, CancellationToken.None);
        return (http, problems);
    }

    private static void AssertUnavailable(HttpContext http, CapturingProblemDetailsService problems)
    {
        Assert.Equal(503, http.Response.StatusCode);
        Assert.Equal("database.unavailable", problems.Last!.Extensions["code"]!.ToString());
    }

    [Fact]
    public async Task NpgsqlException_truoc_tien_map_503()
    {
        var (http, problems) = await HandleAsync(new NpgsqlException("Failed to connect"));
        AssertUnavailable(http, problems);
    }

    [Fact]
    public async Task Npgsql_boc_trong_InvalidOperationException_cua_EF_van_503()
    {
        // Đúng hình dạng EF ném ra khi execution strategy không retry được lỗi hạ tầng.
        var wrapped = new InvalidOperationException(
            "An exception has been raised that is likely due to a transient failure.",
            new NpgsqlException("Failed to connect to 127.0.0.1:5499", new SocketException(10061)));

        var (http, problems) = await HandleAsync(wrapped);
        AssertUnavailable(http, problems);
    }

    [Fact]
    public async Task DbUpdateException_do_mat_ket_noi_503_chu_khong_422()
    {
        // Nếu nhánh 422 đứng trước, client nhận "dữ liệu đã thay đổi" và đổ thay đổi vô ích thay vì retry.
        var (http, problems) = await HandleAsync(
            new DbUpdateException("update failed", new NpgsqlException("connection reset")));
        AssertUnavailable(http, problems);
    }

    [Fact]
    public async Task DbUpdateConcurrencyException_van_422()
    {
        var (http, _) = await HandleAsync(new DbUpdateConcurrencyException("rowversion"));

        Assert.Equal(422, http.Response.StatusCode);
    }

    [Fact]
    public async Task TimeoutException_cua_Npgsql_503()
    {
        var (http, problems) = await HandleAsync(new TimeoutException("connection timeout"));
        AssertUnavailable(http, problems);
    }

    [Fact]
    public async Task Loi_khong_phai_ha_tang_van_500()
    {
        var (http, _) = await HandleAsync(new InvalidOperationException("logic bug không liên quan hạ tầng"));

        Assert.Equal(500, http.Response.StatusCode);
    }

    /// <summary>IProblemDetailsService tối giản: chỉ cần ghi lại ProblemDetails cuối cùng để assert.</summary>
    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetails? Last { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Last = context.ProblemDetails;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>DefaultHttpContext cần một provider rỗng để không ném khi log ghi ra HttpContext.</summary>
    private sealed class StubServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
