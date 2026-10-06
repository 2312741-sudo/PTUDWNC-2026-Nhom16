# BÁO CÁO TIẾN ĐỘ 2 TUẦN — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **Kỳ báo cáo**: Tuần 1 – Tuần 2 (17/09/2026 – 22/09/2026)
> **Tài liệu yêu cầu**: SRS Culinary Blog v1.1.1 (Approved 16/09/2026)
> **Phạm vi phụ trách**: Xuất bản công thức, quản lý ảnh, SEO, vận hành hệ thống và đăng xuất
> **Reviewer**: Nguyễn Thanh Tâm (Nhóm trưởng)

---

## Phần 1 — Các yêu cầu chức năng (FR) và yêu cầu phi chức năng (NFR) được phân công

### 1.1. Yêu cầu chức năng (FR)

| Mã FR | Yêu cầu theo SRS §3.1 / §8 |
|:---|:---|
| **FR-RCP-005** | Cập nhật trạng thái xuất bản công thức: `PATCH /recipes/{id}/publish` và `PATCH /recipes/{id}/unpublish`. Publish yêu cầu ≥1 nguyên liệu VÀ ≥1 bước (C02), thiếu trả HTTP 422 `RECIPE_PUBLISH_INCOMPLETE`. |
| **FR-RCP-006** | Lưu trữ công thức: `PATCH /recipes/{id}/archive`. Ẩn khỏi danh sách công khai, giữ nguyên dữ liệu. Chỉ owner/Admin. |
| **FR-RCP-007** | Xóa công thức: `DELETE /recipes/{id}`. Theo C01 là **Soft Delete** (`IsDeleted = true`), file ảnh trên MinIO giữ nguyên. Không lộ trong tìm kiếm/cache sau khi xóa. |
| **FR-RCP-008** | Quản lý ảnh công thức: `POST /recipes/{id}/images` (upload), `PATCH /recipes/{id}/images/{imageId}` (đặt primary/altText/orderIndex), `DELETE /recipes/{id}/images/{imageId}`. Đúng 1 primary khi có ảnh. |
| **FR-FILE-001** | `IFileStorageService.UploadAsync`: upload file lên MinIO/S3, kiểm tra kích thước ≤5 MiB, 4 định dạng (JPEG/PNG/WebP/AVIF), xác minh magic bytes (không tin Content-Type client), lưu GUID path. |
| **FR-FILE-002** | `IFileStorageService.DeleteAsync`: xóa object MinIO idempotent (xóa file không tồn tại vẫn thành công), retry 3 lần, ghi log khi thất bại. |
| **FR-JOB-002** | `ImageResizeJob`: sinh ảnh 300×300 và 800×600 từ original, cập nhật URLs vào DB, fallback về original nếu resize thất bại, retry idempotent. |
| **FR-JOB-003** | `SitemapGenerationJob`: cron `0 2 * * * UTC`, sinh sitemap XML chứa trang Published/categories/static, retry 2 lần, distributed lock khi chạy nhiều worker. |
| **FR-OBS-001** | `GET /health` (kiểm tra DB + Redis + MinIO), `GET /health/live` (process sống), `GET /health/ready` (DB + Redis; ready trả 200/503). |
| **FR-OBS-003** | OpenTelemetry tracing (HTTP → EF Core) và metrics (request count/duration/error + recipe created/published). |
| **FR-AUTH-005** | `POST /auth/logout`: yêu cầu Bearer token, revoke refresh token, trả HTTP 204 idempotent. |

### 1.2. Yêu cầu phi chức năng (NFR) liên quan

| Mã NFR | Yêu cầu theo SRS §4 |
|:---|:---|
| **NFR-SEC-004** | Validation bằng FluentValidation; validate file size trước khi buffer; kiểm tra nội dung file thật bằng magic bytes; GUID path chống path traversal. |
| **NFR-SEC-005** | TLS ≥1.2; HTTP redirect sang HTTPS; HSTS max-age; CORS explicit origins. |
| **NFR-REL-001** | Uptime mục tiêu ≥99,5%; readiness probe timeout 10s. |
| **NFR-REL-002** | Global exception → HTTP 500 không lộ stack; DB reconnect/timeout 30s; Redis fallback; Hangfire retry theo cấu hình job. |
| **NFR-REL-003** | pg_dump hàng ngày, giữ 30 ngày; volume MinIO/DB bền qua restart; refresh token sống sót qua restart ứng dụng. |
| **NFR-USE-004** | Skeleton/empty state/toast; optimistic update có rollback; upload hiển thị tiến độ %; mọi async có feedback người dùng. |
| **NFR-SCALE-001** | JWT stateless; Redis chia sẻ cache; distributed lock cho sitemap; nhiều Hangfire worker dùng chung PostgreSQL queue; test 2 API instance. |
| **NFR-SCALE-003** | Container tách service (API/DB/Redis/MinIO/Nginx); Nginx upstream nhiều backend; hồ sơ scale MinIO/CDN. |
| **NFR-SEO-003** | Sitemap XML đầy đủ `loc/lastmod/changefreq/priority`; robots.txt khai báo URL; cron 02:00 UTC. |
| **NFR-MAINT-003** | README khởi động <5 phút; OpenAPI/Scalar; ADR; CHANGELOG. |

---

## Phần 2 — Hiện trạng thực hiện theo yêu cầu SRS

> Quy ước trạng thái:
> - **Đã hoàn thành**: công việc đã làm xong trong 2 tuần qua và có bằng chứng (code/test/CI).
> - **Chưa làm**: công việc nằm đúng kế hoạch giai đoạn này nhưng chưa bắt đầu.
> - **Chưa đến lúc làm**: công việc thuộc kế hoạch tuần sau hoặc đang chờ phụ thuộc/quyết định (nêu rõ lý do).

---

### FR-RCP-005 — Xuất bản / Hủy xuất bản công thức

**Mô tả:** Cài đặt 2 endpoints `PATCH /recipes/{id}/publish` và `PATCH /recipes/{id}/unpublish` kèm kiểm tra điều kiện tối thiểu ≥1 nguyên liệu VÀ ≥1 bước theo C02 (thiếu trả 422 `RECIPE_PUBLISH_INCOMPLETE`) và kiểm tra quyền sở hữu.

**Đã hoàn thành:**
- Đã phân tích và xác nhận quyết định SRS v1.1.1 cho tính năng này: điều kiện publish ≥1 nguyên liệu VÀ ≥1 bước (C02), response wrap `{ "data": ... }` (C08) — ghi nhận trong báo cáo tuần 1.

**Chưa làm:**
- Chưa cài đặt các lớp *Command, *Validator, *Handler cho PublishRecipe và UnpublishRecipe.
- Chưa cài đặt 2 endpoints `PATCH /recipes/{id}/publish` và `/unpublish`.
- Chưa cài đặt kiểm tra quyền owner/Admin và invalidation cache sau khi đổi trạng thái.

**Chưa đến lúc làm:**
- Việc cài đặt handler cần Recipe entity và dữ liệu Ingredient/Step từ TV3 (C1/C2). Entity Recipe mới được nối vào API host từ 22/09 (PR #10) nhưng recipe CRUD endpoints chưa bàn giao, nên chưa có fixture để viết test publish. Dự kiến thực hiện từ tuần 3.

**Tiến độ (tự đánh giá): 0%**

---

### FR-RCP-006 — Lưu trữ công thức (Archive)

**Mô tả:** Cài đặt endpoint `PATCH /recipes/{id}/archive` để ẩn công thức khỏi public nhưng giữ nguyên dữ liệu, chỉ owner/Admin được thao tác.

**Đã hoàn thành:**
- Đã phân tích yêu cầu FR-RCP-006 và thống nhất hành vi: ẩn công thức khỏi danh sách/tìm kiếm công khai, giữ dữ liệu.

**Chưa làm:**
- Chưa cài đặt các lớp *Command, *Validator, *Handler cho ArchiveRecipe và endpoint `PATCH /recipes/{id}/archive`.
- Chưa cài đặt kiểm tra quyền owner/Admin và loại công thức Archived khỏi public list/search.

**Chưa đến lúc làm:**
- Theo kế hoạch 6 tuần, Archive nằm ở tuần 3 (sau khi publish hoàn tất) và cần Recipe entity từ TV3 như mô tả ở FR-RCP-005.

**Tiến độ (tự đánh giá): 0%**

---

### FR-RCP-007 — Xóa công thức (Soft Delete theo C01)

**Mô tả:** Cài đặt endpoint `DELETE /recipes/{id}` theo chiến lược Soft Delete đã chốt (C01): đặt `IsDeleted = true`, filter mọi query, giữ nguyên file ảnh trên MinIO, không lộ công thức đã xóa.

**Đã hoàn thành:**
- Đã phân tích và chốt chiến lược Soft Delete theo C01 (`IsDeleted = true`, file ảnh trên MinIO được giữ nguyên để phục vụ khôi phục) — ghi nhận trong báo cáo tuần 1.

**Chưa làm:**
- Chưa cài đặt các lớp *Command, *Validator, *Handler cho DeleteRecipe.
- Chưa cài đặt Global Query Filter `!IsDeleted` và logic loại công thức đã xóa khỏi search/cache/sitemap.

**Chưa đến lúc làm:**
- Theo kế hoạch 6 tuần, Soft Delete Recipe nằm ở tuần 3 (sau publish) và cần Recipe entity từ TV3.

**Tiến độ (tự đánh giá): 0%**

---

### FR-RCP-008 — Quản lý ảnh công thức

**Mô tả:** Cài đặt 3 endpoints `POST /recipes/{id}/images`, `PATCH /recipes/{id}/images/{imageId}`, `DELETE /recipes/{id}/images/{imageId}`; đảm bảo luôn đúng 1 ảnh primary khi có ảnh.

**Đã hoàn thành:**
- Đã cài đặt các domain rule cho RecipeImage: ảnh đầu tiên tự động trở thành primary; luôn có đúng 1 primary khi có ảnh; tự động promote ảnh kế tiếp khi xóa primary — 9 test pass trong `RecipeImageDomainTests` (tuần 2).
- Đã cài đặt validator upload ảnh: 4 định dạng MIME (JPEG/PNG/WebP/AVIF) + dung lượng ≤5 MiB — 16 test pass (tuần 2).
- Đã cài đặt cả 3 endpoints upload/PATCH metadata + primary/DELETE image — hoàn tất 22/09, 88/88 test pass, build 0 warning.
- Đã cài đặt race test 2 writer cùng đặt primary → không tạo 2 primary (RowVersion + unique partial index trong transaction).
- Đã viết `docs/IMAGE_CONTRACT.md` bàn giao TV3 về DTO, endpoints, mã lỗi (tuần 2).

**Chưa làm:**
- Không có mục nào thuộc giai đoạn này còn bỏ trống; phần còn lại nằm ở mục "chưa đến lúc" bên dưới.

**Chưa đến lúc làm:**
- Test E2E trên MinIO thật chưa chạy được vì cần một công thức có sẵn để gắn ảnh (chờ TV3 bàn giao recipe CRUD endpoints).
- Tích hợp uploader UI hiển thị tiến độ % thuộc NFR-USE-004, cần chốt quyết định bucket D27 trước.

**Tiến độ (tự đánh giá): 75%**

---

### FR-FILE-001 — Upload file lên MinIO

**Mô tả:** Cài đặt `IFileStorageService.UploadAsync`: kiểm tra kích thước ≤5 MiB, 4 định dạng, xác minh magic bytes nội dung thật, lưu theo GUID path.

**Đã hoàn thành:**
- Đã cài đặt interface `IFileStorageService` (UploadAsync/DeleteAsync) và record `StoredFile` ở Application layer (tuần 1).
- Đã cài đặt lớp `MinioOptions` (endpoint, accessKey, secretKey, bucket, useSSL) và đăng ký DI (tuần 1).
- Đã cấu hình service `minio-init` trong `docker-compose.dev.yml` tạo bucket `culinary-blog` với policy private (tuần 1).
- Đã cài đặt `MinioStorageService` triển khai bằng MinIO SDK 7.0.0 + khóa phiên bản trong `packages.lock.json` (tuần 2).
- Đã cài đặt validator đọc magic bytes từ nội dung file thật (không tin Content-Type client), whitelist 4 MIME và giới hạn ≤5 MiB — 16 test pass (tuần 2).

**Chưa làm:**
- Không có mục nào thuộc giai đoạn này còn bỏ trống.

**Chưa đến lúc làm:**
- Test MinIO down (lỗi rõ ràng + log redacted) và boundary tests trên MinIO thật cần chạy integration với MinIO local; dự kiến làm cùng đợt E2E khi có recipe fixture từ TV3.

**Tiến độ (tự đánh giá): 85%**

---

### FR-FILE-002 — Xóa file trên MinIO

**Mô tả:** Cài đặt `IFileStorageService.DeleteAsync` idempotent, retry 3 lần khi thất bại và ghi log để theo dõi.

**Đã hoàn thành:**
- Đã đưa hàm `DeleteAsync` vào interface `IFileStorageService` với ngữ nghĩa idempotent (xóa file không tồn tại vẫn thành công) — contract thiết kế ở tuần 1.

**Chưa làm:**
- Chưa cài đặt logic retry 3 lần và ghi log khi xóa object thất bại trong `MinioStorageService`.

**Chưa đến lúc làm:**
- Test tích hợp xóa object thật trên MinIO sẽ chạy cùng đợt integration (cần MinIO local + recipe fixture từ TV3).

**Tiến độ (tự đánh giá): 40%**

---

### FR-JOB-002 — Resize ảnh nền (ImageResizeJob)

**Mô tả:** Cài đặt job sinh ảnh 300×300 và 800×600 từ original, cập nhật URLs về DB, fallback về original, retry idempotent.

**Đã hoàn thành:**
- Nền tảng để triển khai job đã sẵn sàng: upload nguyên bản tới MinIO (FR-FILE-001) và lưu URLs vào DB (FR-RCP-008) đã hoàn thành trong 2 tuần.

**Chưa làm:**
- Chưa cài đặt job resize 300×300 + 800×600 và cập nhật URLs 3 kích thước vào DB.
- Chưa cài đặt fallback original và retry idempotent khi resize thất bại.
- Chưa cài đặt xử lý race giữa job xóa ảnh và job resize (không tái sinh ảnh đã xóa).

**Chưa đến lúc làm:**
- Cơ chế queue (Hangfire/BackgroundService) chưa được chốt với nhóm (quyết định D23); chờ chốt xong sẽ triển khai job trong tuần 3.

**Tiến độ (tự đánh giá): 0%**

---

### FR-JOB-003 — Sinh sitemap (SitemapGenerationJob)

**Mô tả:** Cài đặt job cron `0 2 * * * UTC` sinh sitemap XML chỉ gồm Published/categories/static, retry 2 lần, distributed lock.

**Đã hoàn thành:**
- Chưa đến giai đoạn triển khai nên không phát sinh công việc nào trong 2 tuần này.

**Chưa làm:**
- Chưa có (job chưa bắt đầu).

**Chưa đến lúc làm:**
- Theo kế hoạch 6 tuần, sitemap nằm ở tuần 3 (cùng NFR-SEO-003), phải chạy sau khi có dữ liệu Published thật từ FR-RCP-005.

**Tiến độ (tự đánh giá): 0%**

---

### FR-OBS-001 — Health endpoints

**Mô tả:** Cài đặt `/health` (DB + Redis + MinIO), `/health/live`, `/health/ready` (trả 200/503).

**Đã hoàn thành:**
- Đã cài đặt 3 endpoints `/health`, `/health/live`, `/health/ready` trong `Program.cs` — hoàn tất tuần 1.
- Đã cài đặt cơ chế probe không cần NuGet package mới (chống phá `restore --locked-mode` của CI): DB dùng `DbContext.CanConnectAsync`, Redis/MinIO dùng TCP probe 3 giây.
- Đã kiểm thử hành vi trong `HealthTests` và CI pass.

**Chưa làm:**
- Không có mục nào thuộc giai đoạn này còn bỏ trống.

**Chưa đến lúc làm:**
- Tích hợp kết quả `/health/ready` với Nginx upstream (tạm ngưng nhận traffic khi chưa ready) thuộc công việc Nginx production tuần 5.

**Tiến độ (tự đánh giá): 85%**

---

### FR-OBS-003 — OpenTelemetry tracing và metrics

**Mô tả:** Cài đặt OTEL trace HTTP → EF Core và metrics request count/duration/error + recipe created/published.

**Đã hoàn thành:**
- Nền tảng phục vụ tracing/metrics đã có: dịch vụ Seq và healthcheck được đưa vào stack Compose từ tuần 1.

**Chưa làm:**
- Chưa cài đặt OTEL trace nối HTTP request đến EF Core query.
- Chưa cài đặt metrics request count/duration/error và custom counter recipe created/published.

**Chưa đến lúc làm:**
- Theo kế hoạch 6 tuần, OTEL metric nằm ở tuần 3 (triển khai khi pipeline và dịch vụ nền đã ổn định).

**Tiến độ (tự đánh giá): 0%**

---

### FR-AUTH-005 — Đăng xuất (Logout)

**Mô tả:** Cài đặt endpoint `POST /auth/logout`: yêu cầu Bearer token (thiếu trả 401), revoke refresh token, trả 204 idempotent.

**Đã hoàn thành:**
- Đã cài đặt endpoint `POST /api/v1/auth/logout` yêu cầu Bearer bắt buộc, trả 204 No Content idempotent (tuần 1).
- Đã chấp nhận body `{ refreshToken }` tùy chọn làm seam để tích hợp revoke refresh token sau.
- Đã cập nhật `docs/AUTH_CONTRACT.md` theo SRS v1.1.1 (logout 204, Bearer bắt buộc).
- Đã kiểm thử: 401 thiếu token, 204 hợp lệ, validator refresh token trong `AuthTests` — CI pass.

**Chưa làm:**
- Không có mục nào thuộc giai đoạn này còn bỏ trống.

**Chưa đến lúc làm:**
- Revoke refresh token family khi logout cần FR-AUTH-004 refresh token rotation do TV3 (C5) bàn giao; dự kiến tích hợp cuối tuần 2 / tuần 3.

**Tiến độ (tự đánh giá): 60%**

---

### NFR-SEC-004 — Validation file an toàn

**Mô tả:** Validate kích thước trước khi buffer, kiểm tra magic bytes nội dung thật, GUID path chống path traversal.

**Đã hoàn thành:**
- Đã cài đặt validator đọc magic bytes từ nội dung file thật, giới hạn ≤5 MiB kiểm tra trước khi nạp buffer, whitelist 4 định dạng — 16 test pass (tuần 2).

**Chưa làm:**
- Chưa chạy test path traversal cụ thể (tên file chứa `../`) để xác nhận đường dẫn GUID chặn được truy cập ngoài bucket/prefix.

**Chưa đến lúc làm:**
- Test path traversal được bổ sung cùng batch test bảo mật tích hợp tuần 3–4.

**Tiến độ (tự đánh giá): 80%**

---

### NFR-SEC-005 — TLS / HTTPS / CORS

**Mô tả:** TLS ≥1.2, redirect HTTP → HTTPS, HSTS, CORS explicit origins.

**Đã hoàn thành:**
- Chưa đến giai đoạn triển khai nên không phát sinh công việc nào trong 2 tuần này.

**Chưa làm:**
- Chưa có (chưa bắt đầu).

**Chưa đến lúc làm:**
- Toàn bộ thuộc công việc Nginx production tuần 5; đồng thời cần chốt quyết định D27 (bucket private → ảnh truy cập qua presigned/proxy) vì ảnh hưởng luồng serve ảnh phía trước.

**Tiến độ (tự đánh giá): 10%**

---

### NFR-REL-001 — Độ sẵn sàng (uptime ≥99,5%)

**Mô tả:** Health probes, readiness timeout 10s, cảnh báo khi down >1 phút.

**Đã hoàn thành:**
- Đã cài đặt health probes `/health/live` và `/health/ready` phục vụ giám sát uptime và vòng đời container (tuần 1).

**Chưa làm:**
- Không có mục nào thuộc giai đoạn này còn bỏ trống.

**Chưa đến lúc làm:**
- Cảnh báo down >1 phút và đo uptime thực tế thuộc giai đoạn vận hành tuần 4–5, khi đã có môi trường staging chạy liên tục.

**Tiến độ (tự đánh giá): 40%**

---

### NFR-REL-002 — Khả năng chịu lỗi (Resilience)

**Mô tả:** Global exception không lộ stack, DB reconnect/timeout 30s, Redis fallback, Hangfire retry theo job.

**Đã hoàn thành:**
- Đã cài đặt 3 healthcheck cho DB/Redis/MinIO để phát hiện sớm dependency lỗi (tuần 1).
- Đã ghi nhận nền exception handling RFC 7807 (`ApiExceptionHandler`) có mặt trong pipeline sau bản tích hợp 22/09 (PR #10 của TV3).

**Chưa làm:**
- Chưa cài đặt DB reconnect và timeout 30 giây, chưa cài Redis fallback.
- Chưa cấu hình Hangfire retry cho từng loại job (liên quan quyết định D23).

**Chưa đến lúc làm:**
- Phần resilience phụ thuộc cơ chế queue (D23) và thường được kiểm chứng bằng test cắt mạng ở tuần 4 (resilience/restore drill).

**Tiến độ (tự đánh giá): 35%**

---

### NFR-REL-003 — Backup / Restore

**Mô tả:** pg_dump hàng ngày giữ 30 ngày, volume MinIO/DB bền, refresh token sống qua restart, restore drill.

**Đã hoàn thành:**
- Chưa đến giai đoạn triển khai nên không phát sinh công việc nào trong 2 tuần này.

**Chưa làm:**
- Chưa có (chưa bắt đầu).

**Chưa đến lúc làm:**
- Theo kế hoạch, backup/restore nằm ở tuần 4–5; cần nhóm chốt timezone cho lịch backup trước khi cài job pg_dump.

**Tiến độ (tự đánh giá): 0%**

---

### NFR-USE-004 — Feedback người dùng khi upload

**Mô tả:** Uploader hiển thị tiến độ %, gallery/primary UI, optimistic update có rollback, toast/inline error.

**Đã hoàn thành:**
- Chưa đến giai đoạn triển khai nên không phát sinh công việc nào trong 2 tuần này (UI thuộc phần frontend, triển khai song song backend).

**Chưa làm:**
- Chưa có (chưa bắt đầu).

**Chưa đến lúc làm:**
- Uploader UI phụ thuộc quyết định D27 (bucket private → dùng presigned URL hay proxy để hiển thị ảnh) và chờ contract ảnh từ `IMAGE_CONTRACT.md` được TV3 review xong.

**Tiến độ (tự đánh giá): 0%**

---

### NFR-SCALE-001 — Stateless / nhiều worker

**Mô tả:** Distributed lock cho sitemap, nhiều Hangfire worker, có test 2 API instance.

**Đã hoàn thành:**
- Nền tảng stateless đã được xác lập: bộ healthcheck và stack Compose tách service (JWT stateless do TV1/TV3 triển khai, Redis/MinIO là tầng chia sẻ) sẵn sàng từ tuần 1.

**Chưa làm:**
- Chưa cài đặt distributed lock cho sitemap, chưa kiểm tra nhiều Hangfire worker chia sẻ PostgreSQL queue, chưa chạy test 2 API instance.

**Chưa đến lúc làm:**
- Nội dung này được kiểm chứng ở tuần 4–5 (test multi-instance dùng chung Redis/jobs) sau khi job sitemap và queue được triển khai.

**Tiến độ (tự đánh giá): 10%**

---

### NFR-SCALE-003 — Container hóa / Nginx

**Mô tả:** Container tách service, Nginx upstream nhiều backend, hồ sơ scale MinIO/CDN.

**Đã hoàn thành:**
- Đã cài đặt `docker-compose.dev.yml` với 7 dịch vụ: PostgreSQL 16, Redis 7, MinIO, Mailhog, Seq, Nginx dev và API (tuần 1).
- Đã cấu hình volume + healthcheck cho từng dịch vụ; chạy `docker compose up -d` trên máy local: toàn bộ stack lên, Postgres/MinIO healthy, bucket `culinary-blog` tạo và set private (xác minh 17/09).

**Chưa làm:**
- Không có mục nào thuộc giai đoạn này còn bỏ trống.

**Chưa đến lúc làm:**
- Nginx upstream multi-API, `docker-compose.prod.yml` và hồ sơ scale MinIO 4+ nodes/CDN thuộc kế hoạch tuần 5; nếu phần scale chỉ có thiết kế do hạ tầng hạn chế thì sẽ ghi rõ chưa nghiệm thu triển khai.

**Tiến độ (tự đánh giá): 60%**

---

### NFR-SEO-003 — Sitemap / robots

**Mô tả:** Sitemap XML đầy đủ `loc/lastmod/changefreq/priority`, robots.txt, cron 02:00 UTC.

**Đã hoàn thành:**
- Chưa đến giai đoạn triển khai nên không phát sinh công việc nào trong 2 tuần này.

**Chưa làm:**
- Chưa có (chưa bắt đầu).

**Chưa đến lúc làm:**
- Triển khai cùng FR-JOB-003 ở tuần 3, sau khi có dữ liệu Published thật.

**Tiến độ (tự đánh giá): 0%**

---

### NFR-MAINT-003 — Tài liệu bàn giao

**Mô tả:** README khởi động <5 phút, OpenAPI/Scalar, ADR, CHANGELOG, không commit secret.

**Đã hoàn thành:**
- Đã cài đặt `.env.example` đầy đủ placeholder (JWT là `REPLACE_WITH_RANDOM_SECRET`), không commit secret thật (tuần 1).
- Đã viết `docs/IMAGE_CONTRACT.md` bàn giao TV3 (DTO, endpoints, mã lỗi, quyết định D27) — tuần 2.
- Đã cập nhật `docs/AUTH_CONTRACT.md` theo SRS v1.1.1 (tuần 1).

**Chưa làm:**
- Chưa ghi ADR cho quyết định D27 (bucket private vs public) — đang chờ chốt với nhóm/giảng viên.

**Chưa đến lúc làm:**
- README khởi động <5 phút và CHANGELOG được chốt ở giai đoạn bàn giao tuần 5–6.

**Tiến độ (tự đánh giá): 40%**

---

## Tổng kết

| Chỉ số | Giá trị |
|:---|:---:|
| FR có tiến độ hoàn thành rõ rệt (≥50%) | 4/11: FR-RCP-008, FR-FILE-001, FR-OBS-001, FR-AUTH-005 |
| FR hoàn thành một phần | 1/11: FR-FILE-002 (40%) |
| FR chưa đến lúc làm (chờ phụ thuộc / theo kế hoạch tuần sau) | 6/11: FR-RCP-005, FR-RCP-006, FR-RCP-007, FR-JOB-002, FR-JOB-003, FR-OBS-003 |
| **Tiến độ FR tổng thể (tự đánh giá)** | **~30%** |
| **Tiến độ NFR tổng thể (tự đánh giá)** | **~25%** |

### Blockers

| Yêu cầu bị chặn | Lý do | Dự kiến gỡ |
|:---|:---|:---|
| FR-RCP-005/006/007 | Cần Recipe entity + dữ liệu Ingredient/Step (TV3 C1/C2); recipe CRUD endpoints chưa bàn giao | Tuần 3 |
| FR-RCP-008 (test E2E) | Cần recipe CRUD endpoints để seed công thức gắn ảnh (TV3) | Tuần 3 |
| FR-JOB-002 | Cần chốt cơ chế queue Hangfire/BackgroundService (D23) | Tuần 3 |
| FR-AUTH-005 (revoke refresh) | Cần FR-AUTH-004 refresh token rotation (TV3 C5) | Tuần 3 |
| NFR-SEC-005, NFR-USE-004, NFR-MAINT-003 (ADR) | Cần chốt quyết định D27 (bucket private/public) với nhóm + giảng viên | Tuần 3 |
| NFR-REL-003 | Cần nhóm chốt timezone cho lịch backup | Tuần 4 |