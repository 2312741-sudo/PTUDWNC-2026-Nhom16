# Changelog

## 0.4.0 — 2026-10-04 (Giai đoạn 1 — TV4, tuần 4)

Nhánh `2312739_NHTSon_D5-D6-D7`. Số đo đều lấy từ lần chạy thật, có lệnh và log trong
`docs/evidence/TV4/Tuan04/`.

### Sửa lỗi thật (kèm test hồi quy)

- **`GET /api/v1/recipes/{slug}` trả `500` khi thiếu credential object storage.** `MinioClient.Build()`
  ném `MinioException` khi credential rỗng mà `MinioStorageService` gọi nó ngay trong constructor;
  B5 khiến mọi lần đọc công thức đều dựng service này → hỏng cả endpoint không liên quan tới ảnh.
  Sửa bằng `Lazy<IMinioClient>`: `Build()` chạy lần đầu khi thật sự gọi storage, nên lỗi ra
  `503 storage.unavailable`. **Lỗi do CI bắt, không phải do test local** (máy dev có `Minio__*` trong
  `.env`, CI không có). Khoá bằng 2 test hồi quy; kiểm chứng bằng cách ép `Build()` ở constructor thì
  2 test đỏ.
- **DB không truy cập được làm 3 endpoint đọc trả `500`.** `EfUnitOfWork` chạy lệnh qua execution
  strategy của EF nên `NpgsqlException` gốc bị bọc thành `InvalidOperationException`, còn
  `ApiExceptionHandler` chỉ kiểm tra exception ngoài cùng. Sửa: dò cả chuỗi `InnerException`, map
  `Npgsql`/socket/timeout → `503 database.unavailable`, và đặt nhánh này **trước** nhánh 422 để
  `DbUpdateException` do mất kết nối không bị báo nhầm "dữ liệu đã thay đổi".
- **DB chết lúc khởi động giết cả tiến trình** vì `AddOrUpdate` của lịch sitemap ném `NpgsqlException`
  ra khỏi `Main`. Sửa: bọc try/catch + log; `/health/ready` vẫn 503 nên orchestrator restart pod khi
  DB trở lại và lịch được đăng ký lại.
- **Wizard mất bước sau lần lưu draft đầu tiên.** Lưu xong không chuyển bước, reload thì về bước 1.
  Sửa: truyền `?step=` khi lưu và đọc lại `step` từ URL khi mount (`RecipeWizard.tsx`,
  `EditRecipeClient.tsx`), kèm `settleStep()` trong E2E.

### Thêm

- Resilience N2-C1/C1b/C1c: sitemap retry **2**, resize retry **3**, xoá ảnh retry **3**, retry
  idempotent khi xoá file không tồn tại; cache dùng chung giữa nhiều instance qua Redis; sitemap theo
  lịch **02:00 UTC** với distributed lock.
- `tests/performance/read-load.js` (k6) + `README.md` — 3 log chuẩn, `http_req_failed` 0.00%.
- `deploy/outage-drill.ps1` — kịch bản dừng Redis/S3/DB/worker, đo thời gian phục hồi.
- Playwright E2E luồng publish: đăng ký → đăng nhập → wizard 5 bước → publish → tra cứu tìm kiếm.
- `ImageMagicBytesE6Tests`, `ImageConcurrencyE7Tests`, `BackgroundJobRetryContractTests`,
  `TracingObservabilityTests`.

### Số đo

- Backend **316/316** (311 + 5); coverage `CulinaryBlog.Application` **84.13%** ≥ 80%;
  `dotnet format --verify-no-changes` exit 0; build 0 warning / 0 error.
- Playwright **26/26**, 3 lần liên tiếp; `tsc`/`lint`/`build` exit 0.
- Cả hai job CI xanh trên commit `8d9d62b` (`Backend week 1` run `37213966752`, `Frontend CI` run
  `37213966761`).

### Chưa làm (chuyển tuần 5)

- N2-D3 checklist WCAG/responsive (thuộc TV2); 3 luồng E2E `register/login` (TV1), `category` (TV2),
  `create-recipe` (TV3).
- N4-A runbook đầy đủ và N4-B deploy staging + TLS/HSTS (cần chứng thư).
- Lab L5 và Google OAuth2/PKCE (thiếu credentials — không coi mock là hoàn thành).

## 0.3.0 — 2026-09-30 (TV4, tuần 3 — đã merge qua PR #16)

- D23 Hangfire queue persistent + dashboard Admin; D27 media proxy công khai / Published công khai;
  D2 resize 300×300/800×600 idempotent; D4 sitemap/robots/OG/JSON-LD; D5 EXPLAIN + k6.
- D6 Lab L4 (`practice/TV4/L4`): 4 phase, **39/39 check PASS**.
- PR #19: `503 storage.unavailable` + validate lúc khởi động; `DevConfigParityTests`; gỡ secret khỏi
  `render.yaml`.

## 0.1.0 — 2026-09-09

- TV1 tuần 1: .NET 10 Clean Architecture, Identity/PostgreSQL, Author register/login, JWT và ICurrentUser.
- FluentValidation/MediatR, Problem Details, correlation logging và Scalar.
- Migration auth, PostgreSQL integration tests, architecture lab, CI và auth handoff docs.
- Chưa tích hợp sản phẩm nhóm đầy đủ; xem evidence về kết quả thực tế.
