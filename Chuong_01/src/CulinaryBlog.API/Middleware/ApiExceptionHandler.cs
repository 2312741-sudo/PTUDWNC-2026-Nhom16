using CulinaryBlog.Application.Common;
using CulinaryBlog.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
namespace CulinaryBlog.API.Middleware;
public sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            DomainException or RequestValidationException => 400,
            BadHttpRequestException bad => bad.StatusCode,
            NotFoundException => 404,
            ConflictException => 409,
            _ => 500
        };
        if (status == 500) logger.LogError(exception, "Lỗi xử lý API");
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new()
            {
                Status = status,
                Title = status switch { 400 => "Dữ liệu không hợp lệ", 404 => "Không tìm thấy", 409 => "Xung đột dữ liệu", _ => "Không xử lý được yêu cầu" },
                Detail = status >= 500 ? "Có lỗi máy chủ khi xử lý yêu cầu." : exception.Message,
                Instance = context.Request.Path
            }
        });
    }
}
