# K20 (SP) — Metric nghiệp vụ recipe xuất qua OpenTelemetry thật — TV3, 02/10/2026

## Thay đổi
- Merge `origin/main` (3d0695d, có OTel của TV4 ở `Program.cs`) vào nhánh C7: `5b16601` (merge sạch, không xung đột).
- Test đỏ `d7a7c30` (`RecipeMetricsExportTests`): gắn thêm một reader bắt metric vào **MeterProvider của app** (`ConfigureOpenTelemetryMeterProvider`),
  tạo + sửa công thức qua HTTP, `Collect()` → provider chỉ có metric aspnetcore/efcore, **không có** `culinary.recipes.created`.
- Sửa `c8b7c64`: `builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddMeter(RecipeMetrics.MeterName));` — đăng ký riêng,
  không sửa khối `AddOpenTelemetry()` của TV4. Test xanh (cùng 3 test `RecipeMetricsTests` cũ: 4/4).
- Ghi chú: `ForceFlush()` của cả provider trả `false` khi exporter OTLP không có collector ở `localhost:4317` → test chỉ `Collect()` reader của nó.

## Chạy với exporter thật (OTLP → OpenTelemetry Collector)
- Collector `otel/opentelemetry-collector:0.115.0`, receiver OTLP gRPC `:4317`, exporter `debug` (verbosity detailed); container `tv3-otel-collector` (đã xoá sau khi đo).
- API bản Release của nhánh (`c8b7c64`) chạy ở `http://localhost:5081`, môi trường `Testing`, DB `culinary_test`, `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317`,
  `OTEL_METRIC_EXPORT_INTERVAL=5000`; đăng ký user mới, `POST /api/v1/recipes` rồi `PUT /api/v1/recipes/{id}`.
- Collector nhận scope `CulinaryBlog.Recipes` trong **5** lần export; trích lần cuối (nguyên văn log collector, đã lọc dòng):

```text
InstrumentationScope CulinaryBlog.Recipes
-> Name: culinary.recipes.created
-> Description: Số công thức (Draft) được tạo
-> Unit: {recipe}
-> DataType: Sum
-> IsMonotonic: true
-> AggregationTemporality: Cumulative
NumberDataPoints #0
StartTimestamp: 2026-10-02 12:59:27.8654929 +0000 UTC
Timestamp: 2026-10-02 12:59:52.2538737 +0000 UTC
Value: 1
-> Name: culinary.recipes.updated
-> Description: Số lần cập nhật thông tin công thức thành công
-> Unit: {recipe}
-> DataType: Sum
-> IsMonotonic: true
-> AggregationTemporality: Cumulative
NumberDataPoints #0
StartTimestamp: 2026-10-02 12:59:27.8658575 +0000 UTC
Timestamp: 2026-10-02 12:59:52.2538752 +0000 UTC
Value: 1
```