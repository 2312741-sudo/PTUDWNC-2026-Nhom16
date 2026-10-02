# LAB K20 — TV3: log sink / correlation / trace / metric / health (L5) — nhánh practice/TV3/labs, KHÔNG merge

## Giả định (của TV3, không phải yêu cầu giảng viên)
Repo không có đề chi tiết. Tôi chọn: Serilog với sink đọc từ cấu hình (Console mặc định; File bật bằng `LabLog:FilePath`;
Seq bật bằng `Seq:Url` khi có server Seq — máy hiện **không chạy Seq**, nên Seq chưa kiểm thật), correlation id qua header
`X-Correlation-ID`, OpenTelemetry trace (ASP.NET Core + Npgsql) và metric (Meter `Lab.TV3`) với exporter Console bật bằng
`Otel:ConsoleExporter`, health `/health/live` (không kiểm phụ thuộc) và `/health/ready` (kiểm DB + Redis, lỗi → 503).

## Mô tả
- `L20/Observability.cs` (mới):
  - `AddL20Observability`: `AddSerilog` (ReadFrom.Configuration + ReadFrom.Services + Enrich.FromLogContext), OTel tracing/metrics, health checks.
  - `UseL20Observability`: middleware đầu pipeline nhận/sinh correlation id (chỉ nhận `^[A-Za-z0-9._-]{1,64}$`, còn lại tự sinh → chống log injection),
    trả `X-Correlation-ID` + `X-Trace-Id`, đẩy `CorrelationId` vào `LogContext`; `UseSerilogRequestLogging` ghi 1 dòng/request (path không kèm query string).
  - `DbHealthCheck` (`SELECT 1`), `RedisHealthCheck` (`IsConnected` + `PING`); mô tả lỗi chỉ ghi tên exception, không lộ chuỗi kết nối.
  - `LabMetrics.SearchRequests` (counter `lab.search.requests`, tag `page=ssr|api`) — gọi ở L16 và L3.
- Sửa: `Program.cs`, `appsettings.json` (mục `Serilog`, `LabLog`, `Seq`, `Otel`), `L16/SsrSearchEndpoints.cs`, `L3/SearchEndpoints.cs`, csproj (5 package: Serilog.AspNetCore 10.0.0, Serilog.Sinks.Seq 9.1.0, OpenTelemetry.* 1.19.x).
- Test `L20ObservabilityTests.cs` (8 test) với `ObservabilityFactory`: Redis luôn chết (6398), sink log trong bộ nhớ `CollectingSink` đăng ký qua DI.

## Ghi chú trung thực về TDD
- Commit đỏ `37985e4` đã gồm cả **thay đổi csproj** (thêm package) để test biên dịch được (test dùng kiểu `ILogEventSink` của Serilog); đỏ là đỏ lúc chạy (8/8 FAIL).
- Ở commit xanh, test `Sink_File...` được **sửa cách bật sink**: bản đỏ dùng `Serilog:WriteTo:9:*` qua UseSetting nhưng Serilog đọc ra mục WriteTo có `Name` rỗng
  (SelfLog: "Unable to find a method called ."). Nguyên nhân chính xác: **đoán** là do ghép mảng WriteTo giữa appsettings và key từ UseSetting. Đổi sang key riêng `LabLog:FilePath` (cùng kiểu với `Seq:Url`).

## Phần SP (không viết lại)
- Metric recipe / OTEL / probes của sản phẩm: TV1 (A5) và TV4 (D5) — xem `docs/evidence/TV1/TUAN_3.md`, commit SP `2bbee0d`. Metric riêng cho recipe của TV3 trên SP: xem `BAO_CAO_TUAN4.md` mục SP.

## Cách chạy
```powershell
. .\.secrets.local.ps1
$env:LAB_PG = "Host=localhost;Port=5432;Username=postgres;Password=$env:LAB_PG_PASSWORD"
dotnet test labs/TV3/Lab.TV3.Tests --filter "FullyQualifiedName~L20" -m:1
# Chạy thật:
$env:ConnectionStrings__Lab = "Host=localhost;Port=5432;Database=lab_tv3;Username=postgres;Password=$env:LAB_PG_PASSWORD"
$env:Jwt__SigningKey = $env:LAB_JWT_KEY; $env:LabLog__FilePath = "D:\lablog\lab-tv3-.log"; $env:Otel__ConsoleExporter = "true"
dotnet run --project labs/TV3/Lab.TV3.Api ; curl.exe -i http://localhost:5090/health/ready
# Seq (khi có): docker run -d -e ACCEPT_EULA=Y -p 5341:80 datalust/seq ; $env:Seq__Url = "http://localhost:5341"
```

## Kết quả thật
- Commit đỏ `37985e4`: 8/8 FAIL — `LAB_K20_do.txt`.
- Commit xanh: `ac33ac8` (+ fix logger `a3e26a8`) — 8/8 PASS — `LAB_K20_xanh.txt`; cả bộ lab (Redis tắt, bỏ L3SearchTests) 77/77; format verify exit 0.
- Chạy thật (`LAB_K20_chay_that.txt`): `/health/live` 200; `/health/ready` 200 với db + redis Healthy (Redis container 6379);
  request gửi `traceparent` → `X-Trace-Id` trùng trace id, OTel Console in span Server `GET /lab/l16/search` (ParentSpanId = span client gửi)
  và span con Npgsql chứa câu SQL; file log có `cid=tv3-demo-001 trace=4bf9...`.
  Lưu ý: chuỗi che `***` trong file đó là do che mật khẩu không phân biệt hoa thường, nên một phần chữ "PostgreSQL" cũng bị che.
- Chưa làm: Seq thật (không có server), dashboard metric (chỉ exporter Console).

## 4 câu tự kiểm tra
1. **Correlation id khác trace id thế nào?** — Correlation id là chuỗi do client/gateway đặt để tra log nghiệp vụ (đọc được, ví dụ `tv3-demo-001`); trace id là chuẩn W3C (`traceparent`) do OpenTelemetry dùng để nối các span HTTP → SQL giữa nhiều service. Lab trả cả hai trong header.
2. **Vì sao live không kiểm DB/Redis mà ready thì có?** — Live trả lời "process còn sống không" — nếu kiểm Redis thì Redis chết sẽ làm orchestrator restart app vô ích. Ready trả lời "có nhận traffic được không" → DB/Redis chết thì 503 để load balancer tạm ngừng gửi request.
3. **Làm sao chắc log không chứa mật khẩu/token?** — Request log chỉ ghi method/path/status/thời gian, không ghi body và không ghi query string (`IncludeQueryInRequestPath` mặc định false); test đăng ký với mật khẩu và gọi URL có `access_token=...` rồi kiểm toàn bộ log không chứa hai chuỗi đó.
4. **Vì sao middleware correlation phải đứng trước `UseSerilogRequestLogging`?** — Dòng log tổng kết request được ghi khi request đi ngược ra; middleware correlation bọc bên ngoài nên `LogContext` vẫn còn `CorrelationId` lúc dòng đó được ghi.
