# KẾ HOẠCH TUẦN 4 — TV3 Huỳnh Quốc Trung (2312786) — LAB K16/K19/K20/K22 + C7 Playwright

> Lập ngày 02/10/2026. Phần LAB: worktree `D:\CulinaryBlog-lab`, nhánh `practice/TV3/labs`, code `labs/TV3` (ngoài CulinaryBlog.sln, KHÔNG merge vào main).
> Phần SP: `D:\CulinaryBlog`, nhánh `2312786_HuynhQuocTrung_C7-frontend-tests`.

## Môi trường (không nhạy cảm)

| Mục | Giá trị |
|---|---|
| App lab | `http://localhost:5090`, DB `lab_tv3` (chạy) / `lab_tv3_test` (test), schema Hangfire `lab_tv3_hangfire` |
| Redis cho test lab | mặc định cổng đóng `localhost:6399` → kiểm fallback; test `Infra=docker` cần `LAB_REDIS` |
| App chính | API `http://localhost:5080` (DB `culinary_blog`), frontend `http://localhost:3000` |
| Email seed E2E | `trung.huynh@culinary.local` (DbSeeder.cs) |
| Bí mật | `.secrets.local.ps1` ở gốc mỗi thư mục (đã thêm vào `.git/info/exclude`), nạp bằng `. .\.secrets.local.ps1` |

Lệnh test lab (Redis tắt):

```powershell
. .\.secrets.local.ps1
$env:LAB_PG = "Host=localhost;Port=5432;Username=postgres;Password=$env:LAB_PG_PASSWORD"
dotnet test labs/TV3/Lab.TV3.Tests --filter "FullyQualifiedName!~L3SearchTests" -m:1
```

## Giả định (của TV3, KHÔNG phải yêu cầu giảng viên — repo không có đề chi tiết cho phần LAB)

- **K16 (L5 SSR search):** trang `GET /lab/l16/search?q=` do Lab.TV3.Api render HTML phía server (không cần JS), dùng lại câu FTS của L3.
- **K19:** `GET /sitemap.xml` (chỉ Published), `GET /robots.txt` (có dòng `Sitemap:`), trang chi tiết `GET /lab/l19/recipes/{slug}`;
  đổi tiêu đề khi còn Draft → slug mới + slug cũ trả **301** về slug mới; sau Publish slug giữ nguyên (D14, NFR-SEO-004).
- **K20:** Serilog (Console + File, Seq khi đặt `Seq:Url`), correlation id `X-Correlation-ID` trong log, OpenTelemetry trace/metric
  (exporter Console bật bằng cấu hình), `/health/live` và `/health/ready` (ready kiểm DB + Redis).
- **K22:** k6 20 VU / 30 s, ngưỡng p95 < 500 ms (giả định nhóm); đếm số câu SQL mỗi request để chứng minh không N+1;
  cảnh báo query > 100 ms (NFR-PERF-004). Lighthouse/CWV chỉ đo nếu có công cụ sẵn.
- Phần SP (JSON-LD, metric recipe, concurrency/query) → trỏ PR SP, không viết lại.

## Thứ tự

| # | Việc | Kết quả mong đợi |
|---|---|---|
| 0 | Recon + kế hoạch này | commit riêng |
| 1 | Hồi quy K04/K10/K23 (Redis tắt, bỏ L3SearchTests) | 55 pass |
| 2 | K16 → K19 → K20 → K22, mỗi lab: commit test ĐỎ (`LAB_Kxx_do.txt`) rồi commit XANH (`LAB_Kxx_xanh.txt`) + `LAB_Kxx.md` | test xanh thật |
| 3 | C7 Playwright (D:\CulinaryBlog): config + luồng soạn công thức 3 bước | chạy nếu có `E2E_PASSWORD`, không thì BLOCKED |
| 4 | Kiểm tra trạng thái K05/K17/K18/K20 trên SP | ghi vào báo cáo |
| 5 | `BAO_CAO_TUAN4.md` + `GIAI_THICH_TUAN4.md` | |

## Rủi ro đã biết lúc lập kế hoạch

- `DbSeeder.cs` tạo user seed **không có mật khẩu** (không có PasswordHash) → `E2E_PASSWORD` không xác định được → Playwright BLOCKED (không đoán).
- Máy không có `k6`, `lighthouse`, `psql` trong PATH; k6 sẽ thử chạy qua image Docker `grafana/k6`. RAM trống ~0.5 GB → chạy tuần tự.
- Push: CLAUDE.local.md quy định không tự push → mọi commit để local, chờ Trung xác nhận trước khi push.
