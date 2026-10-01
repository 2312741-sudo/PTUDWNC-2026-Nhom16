using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Xunit;
using static Lab.TV3.Tests.LabHttp;

namespace Lab.TV3.Tests;

/// <summary>Một "instance" API lab có tên riêng nhưng dùng chung DB lab_tv3_test (mô phỏng 2 container sau Nginx).</summary>
public sealed class NamedInstanceFactory(string name) : LabFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Instance:Name", name);
    }
}

/// <summary>
/// LAB L23 — K23 scaling: API phải stateless để chạy 2 instance sau Nginx.
/// Kiểm ở mức app: header X-Instance cho biết ai trả lời; token/refresh phát ở instance A dùng được ở instance B.
/// Bằng chứng Docker/Nginx thật nằm ở labs/TV3/deploy + docs/evidence/TV3/LAB_K23*.
/// </summary>
[Collection("lab")]
public sealed class L23ScalingTests
{
    [Fact]
    public async Task Moi_response_co_header_X_Instance_dung_ten_instance()
    {
        await using var a = new NamedInstanceFactory("lab-tv3-api1");
        var res = await a.CreateClient().GetAsync("/lab/health");

        await Expect(HttpStatusCode.OK, res);
        Assert.Equal("lab-tv3-api1", res.Headers.GetValues("X-Instance").Single());
    }

    [Fact]
    public async Task Token_va_refresh_phat_o_instance_A_dung_duoc_o_instance_B()
    {
        await using var a = new NamedInstanceFactory("lab-tv3-api1");
        await using var b = new NamedInstanceFactory("lab-tv3-api2");

        var (clientA, auth, _) = await AuthorAsync(a);
        var onA = await clientA.GetAsync("/lab/l1/me");
        await Expect(HttpStatusCode.OK, onA);
        Assert.Equal("lab-tv3-api1", onA.Headers.GetValues("X-Instance").Single());

        // Access token: B kiểm chữ ký bằng cùng khóa, không cần session trong RAM của A
        var clientB = b.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());
        var onB = await clientB.GetAsync("/lab/l1/me");
        await Expect(HttpStatusCode.OK, onB);
        Assert.Equal("lab-tv3-api2", onB.Headers.GetValues("X-Instance").Single());

        // Refresh token: lưu trong DB chung nên B xoay vòng được token do A phát
        var refreshed = await b.CreateClient().PostAsJsonAsync("/lab/l1/refresh",
            new { refreshToken = auth.GetProperty("refreshToken").GetString() });
        await Expect(HttpStatusCode.OK, refreshed);
    }
}
