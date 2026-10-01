using Lab.TV3.Api.L5;
using MediatR;
using Xunit;

namespace Lab.TV3.Tests;

public class L5ValidationBehaviorTests
{
    private static readonly SampleRequest Req = new("pho");

    private static ValidationBehavior<SampleRequest, string> Make(params IValidator<SampleRequest>[] validators) => new(validators);

    private sealed class Next(Func<string>? body = null)
    {
        public int Calls { get; private set; }

        public RequestHandlerDelegate<string> Delegate => _ =>
        {
            Calls++;
            return Task.FromResult(body is null ? "ok" : body());
        };
    }

    [Fact]
    public async Task Khong_co_validator_thi_goi_next_va_tra_response()
    {
        var next = new Next();

        var result = await Make().Handle(Req, next.Delegate, CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(1, next.Calls);
    }

    [Fact]
    public async Task Validator_hop_le_thi_goi_next_dung_1_lan()
    {
        var next = new Next();

        var result = await Make(new SampleValidator(), new SampleValidator()).Handle(Req, next.Delegate, CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(1, next.Calls);
    }

    [Fact]
    public async Task Co_loi_thi_nem_LabValidationException_va_khong_goi_next()
    {
        var next = new Next();

        var ex = await Assert.ThrowsAsync<LabValidationException>(
            () => Make(new SampleValidator("Name sai")).Handle(Req, next.Delegate, CancellationToken.None));

        Assert.Equal(["Name sai"], ex.Errors);
        Assert.Equal(0, next.Calls);
    }

    [Fact]
    public async Task Nhieu_validator_cung_loi_thi_exception_chua_du_loi_cua_ca_hai()
    {
        var next = new Next();

        var ex = await Assert.ThrowsAsync<LabValidationException>(
            () => Make(new SampleValidator("loi A"), new SampleValidator("loi B1", "loi B2")).Handle(Req, next.Delegate, CancellationToken.None));

        Assert.Equal(["loi A", "loi B1", "loi B2"], ex.Errors.OrderBy(e => e));
        Assert.Equal(0, next.Calls);
    }

    [Fact]
    public async Task Exception_cua_handler_khong_bi_nuot()
    {
        var next = new Next(() => throw new InvalidOperationException("handler hong"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Make(new SampleValidator()).Handle(Req, next.Delegate, CancellationToken.None));

        Assert.Equal("handler hong", ex.Message);
    }
}
