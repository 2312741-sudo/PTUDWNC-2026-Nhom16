using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CulinaryBlog.Tests;

public sealed class HealthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;
    private readonly HttpClient client;
    public HealthTests(ApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.Migrate();
    }

    [Fact]
    public async Task Liveness_always_reports_healthy()
    {
        var response = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
        Assert.True(body.GetProperty("checks").TryGetProperty("liveness", out _));
    }

    [Fact]
    public async Task Ready_and_full_report_have_expected_checks()
    {
        foreach (var path in new[] { "/health", "/health/ready" })
        {
            var response = await client.GetAsync(path);
            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable });
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var checks = body.GetProperty("checks");
            foreach (var check in new[] { "database", "redis" })
                Assert.True(checks.TryGetProperty(check, out _), $"{path} must include {check}");
        }
    }
}