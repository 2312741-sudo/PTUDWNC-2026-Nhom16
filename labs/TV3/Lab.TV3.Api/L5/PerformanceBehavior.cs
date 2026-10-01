using MediatR;

namespace Lab.TV3.Api.L5;

public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
    TimeProvider clock,
    PerformanceOptions options) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // TODO 1: lấy mốc bắt đầu bằng clock.GetTimestamp() trước khi gọi next().
        // TODO 2: gọi next() và giữ response; dùng try/finally để vẫn đo khi next() ném lỗi (không nuốt exception).
        // TODO 3: tính thời gian đã trôi bằng clock.GetElapsedTime(mốcBắtĐầu).
        // TODO 4: nếu elapsed > options.Threshold (CHỈ khi vượt, bằng ngưỡng thì không) -> logger.LogWarning
        //         với tên request (typeof(TRequest).Name) và số ms; ngược lại không ghi Warning.
        // TODO 5: trả nguyên response của next().
        throw new NotImplementedException();
    }
}
