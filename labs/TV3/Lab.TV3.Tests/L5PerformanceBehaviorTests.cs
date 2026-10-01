using Lab.TV3.Api.L5;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Lab.TV3.Tests;

public class L5PerformanceBehaviorTests
{
    private static readonly SampleRequest Req = new("pho");
    private static readonly PerformanceOptions Opt = new(TimeSpan.FromMilliseconds(500));

    private readonly ManualTimeProvider _clock = new();
    private readonly ListLogger<PerformanceBehavior<SampleRequest, string>> _log = new();

    private PerformanceBehavior<SampleRequest, string> Make() => new(_log, _clock, Opt);

    /// <summary>Handler giả "chạy" mất đúng <paramref name="ms"/> ms trên đồng hồ giả.</summary>
    private RequestHandlerDelegate<string> Takes(int ms, Func<string>? result = null) => _ =>
    {
        _clock.Advance(TimeSpan.FromMilliseconds(ms));
        return Task.FromResult(result is null ? "ok" : result());
    };

    private int Warnings => _log.Entries.Count(e => e.Level == LogLevel.Warning);

    [Fact]
    public async Task Vuot_nguong_thi_ghi_Warning_kem_ten_request_va_so_ms()
    {
        await Make().Handle(Req, Takes(600), CancellationToken.None);

        var warning = Assert.Single(_log.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains("SampleRequest", warning.Message);
        Assert.Contains("600", warning.Message);
    }

    [Fact]
    public async Task Duoi_nguong_thi_khong_ghi_Warning()
    {
        await Make().Handle(Req, Takes(100), CancellationToken.None);

        Assert.Equal(0, Warnings);
    }

    /// <summary>
    /// Biên: đúng bằng ngưỡng (500ms) thì KHÔNG cảnh báo. Quy ước "chỉ cảnh báo khi VƯỢT ngưỡng" (elapsed > Threshold):
    /// ngưỡng là mức chấp nhận được, nên chạm đúng mức đó chưa phải chậm; so sánh >= sẽ gây cảnh báo giả
    /// ở request nằm đúng ngưỡng và làm test này đỏ.
    /// </summary>
    [Fact]
    public async Task Dung_bang_nguong_thi_khong_ghi_Warning()
    {
        await Make().Handle(Req, Takes(500), CancellationToken.None);

        Assert.Equal(0, Warnings);
    }

    [Fact]
    public async Task Tra_nguyen_response_cua_handler()
    {
        var result = await Make().Handle(Req, Takes(600, () => "ket-qua-goc"), CancellationToken.None);

        Assert.Equal("ket-qua-goc", result);
    }

    [Fact]
    public async Task Handler_nem_loi_thi_loi_van_lan_ra_va_van_do_Warning_neu_cham()
    {
        var next = Takes(600, () => throw new InvalidOperationException("handler hong"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Make().Handle(Req, next, CancellationToken.None));

        Assert.Equal("handler hong", ex.Message);
        Assert.Equal(1, Warnings);
    }
}
