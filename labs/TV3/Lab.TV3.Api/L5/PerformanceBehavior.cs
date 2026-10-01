using MediatR;

namespace Lab.TV3.Api.L5;

public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
    TimeProvider clock,
    PerformanceOptions options) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var start = clock.GetTimestamp();
        try
        {
            return await next(cancellationToken);
        }
        finally
        {
            var elapsed = clock.GetElapsedTime(start);
            if (elapsed > options.Threshold)
                logger.LogWarning("{Request} chạy chậm: {ElapsedMs} ms", typeof(TRequest).Name, elapsed.TotalMilliseconds);
        }
    }
}
