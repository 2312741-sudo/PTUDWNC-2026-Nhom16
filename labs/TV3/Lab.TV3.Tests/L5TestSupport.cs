using Lab.TV3.Api.L5;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lab.TV3.Tests;

// Đồ giả cho test K04: không DB, không LabFactory.

public sealed record SampleRequest(string Name, int WorkMs = 0) : IRequest<string>;

public sealed class SampleValidator(params string[] errors) : IValidator<SampleRequest>
{
    public IReadOnlyList<string> Validate(SampleRequest request) => errors;
}

/// <summary>Validator dùng cho test đầu-cuối: Name rỗng là lỗi.</summary>
public sealed class NameRequiredValidator : IValidator<SampleRequest>
{
    public IReadOnlyList<string> Validate(SampleRequest request) =>
        string.IsNullOrWhiteSpace(request.Name) ? ["Name là bắt buộc"] : [];
}

/// <summary>Đồng hồ giả: thời gian chỉ trôi khi gọi Advance.</summary>
public sealed class ManualTimeProvider : TimeProvider
{
    private long _ticks;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp() => _ticks;

    public void Advance(TimeSpan by) => _ticks += by.Ticks;
}

public sealed class ListLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, formatter(state, exception)));
}

/// <summary>Đếm số lần handler thật được gọi (test đầu-cuối).</summary>
public sealed class HandlerSpy
{
    public int Calls { get; set; }
}

public sealed class SampleHandler(HandlerSpy spy, ManualTimeProvider clock) : IRequestHandler<SampleRequest, string>
{
    public Task<string> Handle(SampleRequest request, CancellationToken cancellationToken)
    {
        spy.Calls++;
        clock.Advance(TimeSpan.FromMilliseconds(request.WorkMs));
        return Task.FromResult($"hello {request.Name}");
    }
}
