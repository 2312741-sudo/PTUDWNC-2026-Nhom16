using System.Diagnostics;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var errors = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
            errors.AddRange((await validator.ValidateAsync(request, ct)).Errors);
        if (errors.Count > 0) throw new ValidationException(errors);
        return await next();
    }
}
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var start = Stopwatch.GetTimestamp();
        try { return await next(); }
        finally { logger.LogInformation("Handled {RequestType} in {ElapsedMs} ms", typeof(TRequest).Name, Stopwatch.GetElapsedTime(start).TotalMilliseconds); }
    }
}
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(c =>
        {
            c.RegisterServicesFromAssemblyContaining<RegisterCommand>();
            c.AddOpenBehavior(typeof(LoggingBehavior<,>));
            c.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
        return services;
    }
}
