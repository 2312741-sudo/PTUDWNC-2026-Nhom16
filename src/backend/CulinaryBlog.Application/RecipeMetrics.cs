using System.Diagnostics.Metrics;

namespace CulinaryBlog.Application;

/// <summary>
/// K20 (TV3) — metric nghiệp vụ công thức, chỉ dùng System.Diagnostics.Metrics (BCL) nên Application không phụ thuộc OpenTelemetry.
/// API bật xuất ra OTel bằng <c>.AddMeter(RecipeMetrics.MeterName)</c>. Chỉ đếm khi đã lưu DB thành công.
/// </summary>
public static class RecipeMetrics
{
    public const string MeterName = "CulinaryBlog.Recipes";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> Created = Meter.CreateCounter<long>(
        "culinary.recipes.created", unit: "{recipe}", description: "Số công thức (Draft) được tạo");

    public static readonly Counter<long> Updated = Meter.CreateCounter<long>(
        "culinary.recipes.updated", unit: "{recipe}", description: "Số lần cập nhật thông tin công thức thành công");
}
