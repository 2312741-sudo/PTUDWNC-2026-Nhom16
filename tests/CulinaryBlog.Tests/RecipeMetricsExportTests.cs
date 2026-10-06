using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Domain;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Xunit;

namespace CulinaryBlog.Tests;

/// <summary>
/// K20 (TV3): metric nghiệp vụ <c>culinary.recipes.created/updated</c> phải đi qua MeterProvider OpenTelemetry thật của API
/// (cấu hình OTel của TV4), không chỉ đếm được bằng MeterListener. Gắn thêm một reader bắt metric vào provider của app, gọi API rồi ForceFlush.
/// </summary>
[Collection(nameof(RecipeMetricsExportTests))]
[CollectionDefinition(nameof(RecipeMetricsExportTests), DisableParallelization = true)]
public sealed class RecipeMetricsExportTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private sealed class CapturingExporter : BaseExporter<Metric>
    {
        public ConcurrentDictionary<string, long> Sums { get; } = new();

        public override ExportResult Export(in Batch<Metric> batch)
        {
            foreach (var metric in batch)
            {
                long sum = 0;
                foreach (ref readonly var point in metric.GetMetricPoints())
                    if (metric.MetricType == MetricType.LongSum) sum += point.GetSumLong();
                Sums[metric.Name] = sum;
            }
            return ExportResult.Success;
        }
    }

    private static async Task<JsonElement> DataOf(HttpResponseMessage res)
    {
        if (!res.IsSuccessStatusCode)
            Assert.Fail($"{res.RequestMessage?.Method} {res.RequestMessage?.RequestUri} -> {(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    [Fact]
    public async Task Recipe_created_and_updated_counters_are_exported_through_the_app_MeterProvider()
    {
        factory.EnsureMigrated();
        var exporter = new CapturingExporter();
        var reader = new BaseExportingMetricReader(exporter);
        using var app = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.ConfigureOpenTelemetryMeterProvider(m => m.AddReader(reader))));

        var client = app.CreateClient();
        var reg = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterCommand($"tv3-otel-{Guid.NewGuid():N}@example.test", "Recipe-Flow9!", "Bếp OTel TV3"));
        var auth = (await reg.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(Web))!.Data;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        Guid categoryId;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var category = new Category($"OTel {Guid.NewGuid():N}"[..20], $"otel-{Guid.NewGuid():N}");
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            categoryId = category.Id;
        }

        var created = await DataOf(await client.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeCommand(
            $"Canh chua OTel {Guid.NewGuid():N}"[..30], "Đo metric", null, 10, 20, 2, RecipeDifficulty.Easy, categoryId, null)));
        await DataOf(await client.PutAsJsonAsync($"/api/v1/recipes/{created.GetProperty("id").GetGuid()}", new UpdateRecipeBody(
            created.GetProperty("title").GetString()!, "Đo metric lần 2", null, 10, 20, 3, RecipeDifficulty.Easy, categoryId, null,
            created.GetProperty("rowVersion").GetString())));

        // Chỉ thu thập reader của test: ForceFlush của cả provider trả false khi exporter OTLP (localhost:4317) không có collector
        _ = app.Services.GetRequiredService<MeterProvider>();
        Assert.True(reader.Collect(5000), "Collect thất bại");

        Assert.True(exporter.Sums.TryGetValue("culinary.recipes.created", out var createdSum),
            "MeterProvider của app không xuất culinary.recipes.created (chưa AddMeter(RecipeMetrics.MeterName)?). Có: " + string.Join(", ", exporter.Sums.Keys));
        Assert.True(createdSum >= 1);
        Assert.True(exporter.Sums.TryGetValue("culinary.recipes.updated", out var updatedSum));
        Assert.True(updatedSum >= 1);
    }
}
