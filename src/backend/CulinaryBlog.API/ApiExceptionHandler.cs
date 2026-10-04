using CulinaryBlog.Application;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net.Sockets;

public sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        ProblemDetails details;
        if (exception is ValidationException validation)
            details = new HttpValidationProblemDetails(validation.Errors.GroupBy(e => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
            { Status = 400, Title = "Dữ liệu không hợp lệ.", Extensions = { ["code"] = "validation.failed" } };
        else if (exception is AppException app)
            details = new() { Status = app.Status, Title = app.Message, Detail = app.Message, Extensions = { ["code"] = app.Code } };
        else if (IsInfrastructureFailure(exception))
        {
            // N2-C1: DB/PostgreSQL không truy cập được là LỖI TẠM THỜI, không phải bug ứng dụng.
            // Trước đây rơi vào nhánh else cuối và trả 500 server.error — đúng loại lỗi mà cả
            // B1 (storage.unavailable) và báo cáo lỗi 500 trang /search đã đặt mục tiêu loại bỏ.
            // Đặt TRƯỚC nhánh 422 vì DbUpdateException do mất kết nối cũng là DbUpdateException;
            // nếu không, client nhận 422 "dữ liệu đã thay đổi" và đổ thay đổi vô ích thay vì retry.
            logger.LogError(exception, "DB unreachable (503): {Message}", exception.Message);
            details = new() { Status = 503, Title = "Cơ sở dữ liệu tạm thời không khả dụng. Vui lòng thử lại sau.", Detail = "database.unavailable", Extensions = { ["code"] = "database.unavailable" } };
        }
        else if (exception is DbUpdateConcurrencyException or DbUpdateException)
        {
            // RowVersion (D19), partial unique index ux_recipe_images_one_primary (IMAGE_CONTRACT §4) — tất cả trả 422.
            logger.LogError(exception, "DB update failed (422): {Message}", exception.InnerException?.Message ?? exception.Message);
            details = new() { Status = 422, Title = "Dữ liệu đã thay đổi ở nơi khác. Vui lòng tải lại.", Extensions = { ["code"] = "recipe.version_conflict" } };
        }
        else if (exception is CulinaryBlog.Domain.Common.DomainException domain)
            // C02: publish thiếu nguyên liệu/bước trả 422, các vi phạm nghiệp vụ khác trả 400.
            details = new() { Status = domain.Code == "RECIPE_PUBLISH_INCOMPLETE" ? 422 : 400, Title = domain.Message, Detail = domain.Message, Extensions = { ["code"] = domain.Code } };
        else if (exception is BadHttpRequestException)
            details = new() { Status = 400, Title = "JSON hoặc yêu cầu không hợp lệ.", Extensions = { ["code"] = "request.invalid" } };
        else
        {
            logger.LogError(exception, "Request failed with {ExceptionType}: {Message}", exception.GetType().Name, exception.Message);
            details = new() { Status = 500, Title = "Có lỗi hệ thống. Vui lòng thử lại.", Extensions = { ["code"] = "server.error" } };
        }
        context.Response.StatusCode = details.Status!.Value;
        details.Instance = context.Request.Path;
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details });
        return true;
    }

    /// <summary>
    /// Lỗi hạ tầng tới PostgreSQL, phải dò theo CẢ chuỗi InnerException chứ không chỉ exception ngoài cùng.
    ///
    /// Lý do phải dò sâu: <c>EfUnitOfWork</c> chạy lệnh qua execution strategy của EF để retry an toàn
    /// với lỗi tạm thời, nên lỗi Npgsql gốc bị EF bọc lại thành
    /// <c>InvalidOperationException("...likely due to a transient failure")</c>. Nếu chỉ kiểm tra
    /// <c>exception is NpgsqlException</c> thì lỗi thật không bao giờ lộ ra và vẫn trả 500.
    ///
    /// Không tính <see cref="DbUpdateConcurrencyException"/> là lỗi hạ tầng: đó là xung đột optimistic
    /// concurrency (D19) và phải trả 422 để client tải lại.
    /// </summary>
    private static bool IsInfrastructureFailure(Exception exception)
    {
        if (exception is DbUpdateConcurrencyException)
            return false;

        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is NpgsqlException or SocketException or TimeoutException)
                return true;
        }

        return false;
    }
}
