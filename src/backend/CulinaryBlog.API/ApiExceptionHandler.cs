using CulinaryBlog.Application;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

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
            details = new() { Status = app.Status, Title = app.Message, Extensions = { ["code"] = app.Code } };
        else if (exception is BadHttpRequestException)
            details = new() { Status = 400, Title = "JSON hoặc yêu cầu không hợp lệ.", Extensions = { ["code"] = "request.invalid" } };
        else
        {
            // Exception messages can contain credentials, SQL parameters or request bodies.
            logger.LogError("Request failed with {ExceptionType}", exception.GetType().Name);
            details = new() { Status = 500, Title = "Có lỗi hệ thống. Vui lòng thử lại.", Extensions = { ["code"] = "server.error" } };
        }
        context.Response.StatusCode = details.Status!.Value;
        details.Instance = context.Request.Path;
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details });
        return true;
    }
}
