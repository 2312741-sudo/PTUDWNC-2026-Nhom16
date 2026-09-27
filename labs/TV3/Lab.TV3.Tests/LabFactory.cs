using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Lab.TV3.Api.L1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lab.TV3.Tests;

/// <summary>
/// App lab thật trên PostgreSQL (DB lab_tv3_test). Redis mặc định trỏ cổng đóng (6399) -> kiểm fallback;
/// đặt LAB_REDIS=localhost:6379 khi có Redis để chạy test [Trait("Infra","docker")].
/// Google: fake verifier cho unit/error test (được phép theo quy định lab); integration thật dùng /lab/l1/google-demo.
/// </summary>
public class LabFactory : WebApplicationFactory<Program>
{
    public static string Pg => Environment.GetEnvironmentVariable("LAB_PG")
        ?? "Host=localhost;Port=5432;Username=postgres;Password=postgres";

    protected virtual string Redis => Environment.GetEnvironmentVariable("LAB_REDIS") ?? "localhost:6399";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Lab", $"{Pg};Database=lab_tv3_test");
        builder.UseSetting("Redis:Configuration", Redis);
        builder.UseSetting("Google:ClientId", "lab-test-client");
        builder.ConfigureTestServices(s => s.AddSingleton<IGoogleTokenVerifier, FakeGoogleVerifier>());
    }
}

/// <summary>Luôn trỏ Redis vào cổng đóng để kiểm fallback kể cả khi máy có Redis.</summary>
public sealed class RedisDownFactory : LabFactory
{
    protected override string Redis => "localhost:6399";
}

[CollectionDefinition("lab")]
public sealed class LabCollection : ICollectionFixture<LabFactory> { }

/// <summary>Token giả dạng "ok|sub|email|true|false" — chỉ dùng cho test lỗi/luồng, không thay integration Google.</summary>
public sealed class FakeGoogleVerifier : IGoogleTokenVerifier
{
    public Task<GoogleIdentity?> VerifyAsync(string idToken, CancellationToken ct) =>
        Task.FromResult<GoogleIdentity?>(idToken.Split('|') is ["ok", var sub, var email, var verified]
            ? new GoogleIdentity(sub, email, verified == "true", "Người dùng Google")
            : null);
}

public static class LabHttp
{
    public const string Password = "Lab-Pass9x";

    public static string NewEmail(string tag = "u") => $"tv3-{tag}-{Guid.NewGuid():N}@lab.test";

    public static async Task<JsonElement> Data(HttpResponseMessage res)
    {
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    public static async Task Expect(HttpStatusCode code, HttpResponseMessage res)
    {
        if (res.StatusCode != code)
            Assert.Fail($"{res.RequestMessage?.Method} {res.RequestMessage?.RequestUri} -> {(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
    }

    public static async Task<string> Code(HttpResponseMessage res)
    {
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("code").GetString()!;
    }

    /// <summary>Đăng ký user mới, trả về client đã gắn Bearer + payload auth.</summary>
    public static async Task<(HttpClient Client, JsonElement Auth, string Email)> AuthorAsync(WebApplicationFactory<Program> f)
    {
        var client = f.CreateClient();
        var email = NewEmail();
        var res = await client.PostAsJsonAsync("/lab/l1/register", new { email, password = Password, displayName = "Huỳnh Quốc Trung" });
        await Expect(HttpStatusCode.Created, res);
        var auth = await Data(res);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());
        return (client, auth, email);
    }
}