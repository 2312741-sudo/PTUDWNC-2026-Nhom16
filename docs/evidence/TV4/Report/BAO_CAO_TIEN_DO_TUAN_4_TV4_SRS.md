# BÁO CÁO TIẾN ĐỘ TUẦN 4 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **Kỳ báo cáo**: Tuần 4 (29/09/2026 – 05/10/2026)
> **Báo cáo liền kề**: [`BAO_CAO_TIEN_DO_TUAN_3_TV4_SRS.md`](BAO_CAO_TIEN_DO_TUAN_3_TV4_SRS.md) (tuần 3) · [`BAO_CAO_TIEN_DO_2_TUAN_TV4_SRS.md`](BAO_CAO_TIEN_DO_2_TUAN_TV4_SRS.md) (tuần 1–2)
> **Tài liệu yêu cầu**: SRS Culinary Blog v1.1.1 (Approved 16/09/2026)
> **Phạm vi phụ trách**: Xuất bản công thức, quản lý ảnh, SEO, vận hành hệ thống và đăng xuất
> **Reviewer**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Nhánh làm việc**: `2312739_NHTSon_D5-D6-D7` — [PR #29](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/pull/29)
> **Nhánh lab**: `practice/TV4/L4`, `practice/TV4/L5`
> **Cổng nghiệm thu trong kỳ**: **G4** giữa tuần (24/24 ô K có minh chứng + reviewer xác nhận) · **G5** cuối tuần (06/10: coverage ≥ 80% có số đo thật, sửa xong lỗi chặn luồng/bảo mật, CI xanh)
> **Cách làm báo cáo**: tuần 4 chia 3 giai đoạn — GĐ1 (N2/N3/N4-C), GĐ2 (kiểm tra tích hợp FE–BE), GĐ3 (sửa lỗi + đóng issue). Báo cáo SRS này gộp kết quả cả 3 giai đoạn, dẫn chiếu chi tiết tại [`BAO_CAO_GIAI_DOAN_1_N2_N4.md`](../Tuan04/report/BAO_CAO_GIAI_DOAN_1_N2_N4.md), [`BAO_CAO_GIAI_DOAN_2_N3.md`](../Tuan04/report/BAO_CAO_GIAI_DOAN_2_N3.md), [`BAO_CAO_GIAI_DOAN_3_SUA_LOI.md`](../Tuan04/report/BAO_CAO_GIAI_DOAN_3_SUA_LOI.md).

---

## I. Công việc

### 1. Bảng FR

| Mã FR | Mô tả |
|:---|:---|
| **FR-RCP-005** | Cập nhật trạng thái xuất bản công thức: `PATCH /recipes/{id}/publish` và `PATCH /recipes/{id}/unpublish`. Publish yêu cầu ≥1 nguyên liệu VÀ ≥1 bước (C02), thiếu trả HTTP 422 `RECIPE_PUBLISH_INCOMPLETE`. |
| **FR-RCP-006** | Lưu trữ công thức: `PATCH /recipes/{id}/archive`. Ẩn khỏi danh sách công khai, giữ nguyên dữ liệu. Chỉ owner/Admin. |
| **FR-RCP-007** | Xóa công thức: `DELETE /recipes/{id}`. Theo C01 là **Soft Delete** (`IsDeleted = true`), file ảnh trên object storage giữ nguyên. Không lộ trong tìm kiếm/cache/sitemap sau khi xóa. |
| **FR-RCP-008** | Quản lý ảnh công thức: `POST /recipes/{id}/images` (upload), `PATCH /recipes/{id}/images/{imageId}` (đặt primary/altText/orderIndex), `DELETE /recipes/{id}/images/{imageId}`. Đúng 1 primary khi có ảnh. |
| **FR-FILE-001** | `IFileStorageService.UploadAsync`: upload file lên S3-compatible storage, kiểm tra kích thước ≤5 MiB, 4 định dạng (JPEG/PNG/WebP/AVIF), xác minh magic bytes (không tin Content-Type client), lưu GUID path. |
| **FR-FILE-002** | `IFileStorageService.DeleteAsync`: xóa object idempotent, **retry 3 lần**, ghi log khi thất bại. |
| **FR-JOB-002** | `ImageResizeJob`: sinh ảnh 300×300 và 800×600 từ original, cập nhật URLs vào DB, fallback về original nếu resize thất bại, retry idempotent. |
| **FR-JOB-003** | `SitemapGenerationJob`: cron `0 2 * * * UTC`, sinh sitemap XML chứa trang Published/categories/static, retry 2 lần, distributed lock khi chạy nhiều worker. |
| **FR-OBS-001** | `GET /health` (DB + Redis + object storage), `GET /health/live`, `GET /health/ready` (trả 200/503). |
| **FR-OBS-003** | OpenTelemetry tracing (HTTP → EF Core) và metrics (request count/duration/error + recipe created/published). |
| **FR-AUTH-005** | `POST /auth/logout`: yêu cầu Bearer token, revoke refresh token, trả HTTP 204 idempotent. |

### 2. Bảng NFR

| Mã NFR | Mô tả |
|:---|:---|
| **NFR-SEC-004** | Validation bằng FluentValidation; validate file size trước khi buffer; kiểm tra nội dung file thật bằng magic bytes; GUID path chống path traversal. |
| **NFR-SEC-005** | TLS ≥1.2; HTTP redirect sang HTTPS; HSTS max-age; CORS explicit origins. |
| **NFR-REL-001** | Uptime mục tiêu ≥99,5%; readiness probe timeout 10s. |
| **NFR-REL-002** | Global exception → HTTP 500 không lộ stack; DB reconnect/timeout 30s; Redis fallback; Hangfire retry theo cấu hình job. |
| **NFR-REL-003** | pg_dump hàng ngày, giữ 30 ngày; volume object storage/DB bền qua restart; refresh token sống sót qua restart ứng dụng. |
| **NFR-USE-004** | Skeleton/empty state/toast; optimistic update có rollback; upload hiển thị tiến độ %; mọi async có feedback người dùng. |
| **NFR-SCALE-001** | JWT stateless; Redis chia sẻ cache; distributed lock cho sitemap; nhiều Hangfire worker dùng chung PostgreSQL queue; test 2 API instance. |
| **NFR-SCALE-003** | Container tách service; Nginx upstream nhiều backend; hồ sơ scale storage/CDN. |
| **NFR-SEO-003** | Sitemap XML đầy đủ `loc/lastmod/changefreq/priority`; robots.txt khai báo URL; cron 02:00 UTC. |
| **NFR-MAINT-003** | README khởi động <5 phút; OpenAPI/Scalar; ADR; CHANGELOG. |

### 3. Bảng hiện trạng

> Quy ước: **Hoàn thành** = có code + test/bằng chứng chạy thật trong kỳ. Cột "Hoàn thành" ghi **phần làm thêm trong tuần 4**; phần đã có từ tuần 1–3 không lặp lại (xem 2 báo cáo liền kề).
> Cột **Tuần 3 → Tuần 4** ghi % tự đánh giá để thấy độ lệch; **không** ô K nào tự chuyển ✅ khi chưa được reviewer Tâm xác nhận.

| STT | Mã | Tuần 3 → 4 | Hoàn thành trong tuần 4 | Chưa hoàn thành | Nhánh | Tiến độ |
|---:|:---|:---:|:---|:---|:---|:---:|
| 1 | **FR-RCP-005** | 90 → **90%** | • E2E qua **UI thật**: `recipe-publish.spec.ts` B1-2/B1-3/B1-4 — nút "Xuất bản" `disabled` khi thiếu bước, publish → badge Draft→Published → trang công khai; `recipe-publish --repeat-each=4` **16/16**.<br>• API `PATCH /{id}/publish` + `/unpublish` giữ nguyên tại `Program.cs:620,627`; `RecipeLifecycleHandlerTests` **23/23**. | • **Nút Unpublish trên UI dashboard chưa có** (`N2-D2`) — gỡ xuất bản mới kiểm được ở tầng API. | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **90%** |
| 2 | **FR-RCP-006** | 90 → **90%** | • Không có thay đổi về code trong tuần 4 — `ArchiveRecipeCommand` + endpoint đã làm tuần 3. | • **Nút Archive trên UI chưa có** (`N2-D2`). | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **90%** |
| 3 | **FR-RCP-007** | 90 → **90%** | • Xác minh soft delete phủ cả **sitemap**: `GetPublishedForSitemapAsync` loại Draft/Archived/đã xóa (lab L5 phase `seo` 15/15). | • **Nút Xóa trên UI chưa có**.<br>• **ADR TV4-001** (giữ/xóa object ảnh khi soft delete, CR-3) chưa viết. | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **90%** |
| 4 | **FR-RCP-008** | 90 → **95%** | • **Gỡ B5** (issue [#23](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/issues/23) đóng 05/10): ảnh Draft xem được qua **presigned URL** (PA-3) — FE `imageSrc()` ưu tiên `presignedUrl`, `isPresignedStale()` → nút "Tải lại liên kết ảnh"; hợp đồng `IMAGE_CONTRACT.md §7b`.<br>• **10/10 E2E tấn công file** (`upload-security.spec.ts`): B3 file-size/MIME spoofing, B4 quyền upload — upload thật lên MinIO.<br>• Sửa **lỗ hổng JPEG 3 byte lọt `201`**: thêm `MinBytes = 64` + mã `file.too_small` (4 test hồi quy).<br>• GĐ3: `DbSeeder` không ghi đè URL ảnh người dùng — `DbSeederUserImageUrlTests` **2/2** (đỏ trước, xanh sau).<br>• Test E2E storage thật **44/44** (`MinioE2ETests` + resize + proxy + contract) với hạ tầng dev bật. | • **Thanh tiến trình upload % chưa có** (thuộc `N2-D1`, xem NFR-USE-004). | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **95%** |
| 5 | **FR-FILE-001** | 95 → **95%** | • `ImageMagicBytesE6Tests` **25/25** — chữ ký phải đủ chuỗi (WebP/AVIF kiểm tới byte 11), `.exe` đổi đuôi `.jpg` bị chặn, `RIFF…WAVE` không phải WebP, brand `mp42` không phải AVIF.<br>• Thêm biên `file.too_small` (64 byte) — PNG hợp lệ nhỏ nhất 67 byte, JPEG ~125 byte nên không loại ảnh thật. | • **Chưa có test path traversal** (tên file chứa `../`) chứng minh GUID path chặn truy cập ngoài prefix. | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **95%** |
| 6 | **FR-FILE-002** | 55 → **85%** | • **Đã có retry 3 lần + log** cho `MinioStorageService.DeleteAsync` (`MinioStorageService.cs:128-154`: `DeleteAttempts`, backoff `DeleteRetryDelay * attempt`, log `attempt/DeleteAttempts`).<br>• Hợp đồng khoá bằng test: `BackgroundJobRetryContractTests` **4/4** — "xoá ảnh retry 3" (cùng sitemap 2, resize 3, welcome 0/1/5/30 phút).<br>• Xóa ảnh xoá **cả object phái sinh** 300×300/800×600; race delete-vs-resize không tái sinh ảnh đã xóa. | • Chưa có test riêng gọi thẳng `DeleteAsync` xác nhận "không xoá object ngoài bucket/prefix" (mới bảo vệ gián tiếp qua key `recipes/{recipeId}/...` trong test D2/D27). | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **85%** |
| 7 | **FR-JOB-002** | 95 → **95%** | • Xác minh lại hợp đồng retry: `ResizeImageJob` `[AutomaticRetry(Attempts = 3)]` + idempotent qua `ExistsAsync` (test 4/4).<br>• Lab L5 phase `image-opt` đo được **ảnh chưa được tối ưu 2 tầng** (`next/image` 0 file) — ghi nhận, không tự sửa. | • Chưa chạy tay nhánh resize với **AVIF thật** (đã có test trong `ImageResizeD2Tests`, AVIF không decode → fallback original). | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **95%** |
| 8 | **FR-JOB-003** | 45 → **80%** | • **Đã có job theo lịch** — recurring `"sitemap-daily"`, cron `0 2 * * *`, `TimeZoneInfo.Utc` đăng qua `IRecurringJobManager` **sau** `builder.Build()` (`Program.cs:349`; sửa lỗi đăng lịch trước Build làm app không khởi động được ở môi trường thật).<br>• **Distributed lock** Redis (`SitemapGenerator.cs` `LockTakeAsync`/TTL) — `SitemapLockTests` **3/3**: 2 generator tranh lock → đúng 1 thắng, không ghi đè; lock có TTL nên job treo không chặn vĩnh viễn.<br>• `[AutomaticRetry(Attempts = 2)]` (hợp đồng N2-C1c).<br>• `GET /sitemap.xml` app thật trả 200 XML hợp lệ (trang tĩnh + công thức Published). | • Sitemap **chưa liệt kê URL từng danh mục** (mới có `/categories` tĩnh). | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **80%** |
| 9 | **FR-OBS-001** | 80 → **95%** | • **Gỡ B4** (issue [#22](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/issues/22) đóng 05/10): thêm `ObjectStorageCredentialProbe` — `StatObject` trên key probe, phân biệt `AccessDenied`/`BucketNotFound` → **Unhealthy**, `ObjectNotFound` (404 sau xác thực) → Healthy, lỗi mạng → Unhealthy.<br>• Outage drill xác nhận `/health/ready` **503** khi Redis/S3/DB chết, `/health/live` vẫn 200; phục hồi Redis 0.2s · S3 0.5s · API 3.5s.<br>• `HealthTests` pass. | • Tích hợp `/health/ready` với **Nginx upstream** (tạm ngưng traffic khi chưa ready) thuộc tuần 5. | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **95%** |
| 10 | **FR-OBS-003** | 70 → **90%** | • **Trace thật HTTP→DB vào Seq/OTLP** (evidence TV4-K20): cùng TraceId có span HTTP `GET /api/v1/recipes/` **và** span con `db.system=postgresql`; log Serilog trong Seq cùng TraceId ⇒ log–trace liên kết được. Bằng chứng: [`../Tuan04/logs/seq_trace_recipes.log`](../Tuan04/logs/seq_trace_recipes.log).<br>• Sửa 2 lỗi làm trace/metric không hoạt động thật: đăng lịch sitemap trước `Build()` → `JobStorage` chưa init; sink Seq gán **sau** `UseSerilog` → Seq nhận 0 event.<br>• Sửa race trong `TracingObservabilityTests` (snapshot trong `lock`): trước 2/10 lần đỏ → sau **7/7 xanh**.<br>• Lab L5 phase `observability` **10/10 PASS**. | • Metrics `request count/duration/error` + custom counter `recipe created/published` — đã cấu hình nhưng **chưa có bằng chứng chụp số liệu chạy thật** (mới có trace). | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **90%** |
| 11 | **FR-AUTH-005** | 95 → **95%** | • Không thay đổi — revoke refresh family đã hoàn thành tuần 3 (8/8 test `Week3AuthAndPersonalLabTests`).<br>• Tuần 4 chỉ xác minh không hồi quy qua E2E auth + `Week4AuthAndSecurityLabTests`. | • Không có mục nào thuộc giai đoạn này còn bỏ trống. | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **95%** |
| 12 | **NFR-SEC-004** | 90 → **90%** | • Thêm biên `file.too_small` **trước** khi nạp buffer đầy đủ + 4 test (E6.10, validator) — chặn file JPEG 3 byte lọt 201.<br>• `deploy/scan-secrets.sh` chạy được trong CI (probe âm đã thử: cố ý chèn khoá → exit 1). | • **Chưa có test path traversal** cụ thể (`../` trong tên file).<br>• XSS sanitization/CSP do TV1, chưa tích hợp uploader. | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **90%** |
| 13 | **NFR-SEC-005** | 20 → **20%** | • Không thay đổi — CORS explicit origins đã có từ tuần 3.<br>• Ảnh Published `Cache-Control: public, max-age=3600`; Draft/Archived `no-store`. | • **Chưa có** TLS ≥1.2, HTTP→HTTPS redirect, HSTS (thuộc Nginx production, `N4-B` tuần 5). | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **20%** |
| 14 | **NFR-REL-001** | 45 → **60%** | • **Outage drill thật** (`deploy/outage-drill.ps1`, log `c1_outage_drill.log`): Redis tắt → đọc vẫn 200 (fallback) + ready 503; S3 tắt → media 404→503; DB hỏng → mọi endpoint **503 `database.unavailable`, không 500**. Thời gian phục hồi Redis 0.2s · S3 0.5s · API 3.5s.<br>• Đã xác nhận startupProbe cần riêng (app mất ~95–120s mới LISTEN khi DB chết). | • **Một node là SPOF**, không failover ⇒ **không tuyên bố đạt 99,5%** bằng drill ngắn.<br>• Chưa đo uptime thực tế, chưa cảnh báo down >1 phút (cần staging).<br>• PostgreSQL native không dừng được (thiếu quyền Administrator) — drill DB chỉ là **mô phỏng** port chết. | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **60%** |
| 15 | **NFR-REL-002** | 50 → **85%** | • **Gỡ B1** (issue [#20](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/issues/20)): `MinioStorageService.GuardAsync` bọc lỗi storage → **503 `storage.unavailable`** (không bọc `ObjectNotFound` → 404, không bọc cancellation đang chạy).<br>• **Gỡ B2** (issue [#21](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/issues/21)): fail-fast `Program.cs:156` khi thiếu `AccessKey`/`SecretKey`.<br>• Lỗi DB → 503: dò **cả chuỗi `InnerException`** (`EfUnitOfWork` bọc `NpgsqlException` thành `InvalidOperationException`), map Npgsql/socket/timeout → 503 **trước** nhánh 422. `ApiExceptionHandlerDbUnavailableTests` **6/6**.<br>• `CommandTimeout(30)` cho DB (`Program.cs:92`); Redis fallback xác minh bằng drill + `RedisSharedCacheTests` (SimulateServerDown → fallback local, app vẫn 200).<br>• Hợp đồng retry khoá bằng `BackgroundJobRetryContractTests` **4/4**.<br>• `Lazy<IMinioClient>`: `GET /recipes/{slug}` không còn 500 khi thiếu credential storage (2 test hồi quy; lỗi này **do CI bắt**). | • Chưa có test cắt dependency thật cho **DB reconnect** (drill DB mô phỏng, xem NFR-REL-001).<br>• Khi DB chết mà Redis còn, list vẫn trả **200 bằng dữ liệu cũ** không có tín hiệu hạn dữ liệu — chưa thêm cờ. | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **85%** |
| 16 | **NFR-REL-003** | 10 → **45%** | • `deploy/backup.sh` + `deploy/restore.sh` drill thật: backup 728 653 byte → restore DB sạch **14 bảng** khớp (1344 users, 495 recipes, 25 categories); restore lần 2 vào DB tồn tại → **từ chối (exit 1)**.<br>• `.github/workflows/backup.yml` cron `0 20 * * *` UTC = **03:00 Asia/Ho_Chi_Minh** đã có trong repo (cần secret `DATABASE_URL`).<br>• Volume `s3data`/`pgdata` bền qua restart; refresh token sống qua restart (bảng `RefreshTokens` trong PostgreSQL). | • **CHƯA ĐẠT giữ 30 ngày** — `BACKUP_KEEP_DAYS=30` chỉ dọn file trên runner, artifact GitHub giữ 7 ngày → [`DE_XUAT_08`](../../../proposal/DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md).<br>• **Chưa chốt nơi đặt lịch** (GH Actions trễ/bỏ qua job khi lâu không commit) → [`DE_XUAT_07`](../../../proposal/DE_XUAT_07_NOI_DAT_LICH_BACKUP.md).<br>• Chưa drill restore trên staging. | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **45%** |
| 17 | **NFR-USE-004** | 15 → **20%** | • **Gỡ B5** (issue [#23](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/issues/23)): ảnh Draft hiển thị được + nút **"Tải lại liên kết ảnh"** khi presigned hết hạn (15 phút) — có feedback lỗi rõ thay vì vỡ ảnh im lặng.<br>• Lab L5 xác nhận **optimistic rollback có thật** (`RecipeWizard.tsx` dùng `useOptimistic` + rollbacks, rollback đúng). | • **Chưa có thanh tiến trình upload %** — việc lớn nhất của NFR này (`N2-D1`).<br>• Chưa có checklist WCAG/keyboard cho upload + status button (K18, thuộc TV2). | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **20%** |
| 18 | **NFR-SCALE-001** | 35 → **70%** | • **Distributed lock sitemap** kiểm chứng: 2 generator → đúng 1 thắng (`SitemapLockTests` 3/3).<br>• **2 API instance thật** (5080/5081) sau Nginx round-robin (`nginx/nginx.multiinstance.conf`): 10 request → api-1: 5 / api-2: 5; api-1 làm nóng cache, api-2 đọc trúng **cùng key Redis trong 13 ms** ⇒ cache thuộc Redis không thuộc process. Tắt Redis: cả hai vẫn 200 từ DB, cả hai ready 503. Bằng chứng [`../Tuan04/logs/multi_instance_two_api.log`](../Tuan04/logs/multi_instance_two_api.log).<br>• `RedisSharedCacheTests` **6/6** (2 connection, TTL, JSON, invalidation prefix, fallback).<br>• Hangfire storage PostgreSQL chung, `WorkerCount = 4`. | • **Chưa xác nhận số Hangfire server khi chạy 2 API** (thiếu `LAB_APP_DB`).<br>• Chưa có compose profile 2 API sẵn dùng cho nhóm.<br>• Redis cache recipe cross-instance chỉ đo bằng lab, chưa có test tự động. | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **70%** |
| 19 | **NFR-SCALE-003** | 65 → **70%** | • Thêm service `otel-collector` + `seq` vào `docker-compose.dev.yml` (đã chạy thật, port 4317/4318/5341).<br>• Nginx multi-instance conf + backup/restore scripts trong repo. | • **Chưa có** Nginx upstream production nhiều backend, `docker-compose.prod.yml`, TLS (thuộc `N4-B` tuần 5).<br>• Hồ sơ scale 4+ nodes/CDN mới ở mức thiết kế — **chưa nghiệm thu triển khai**.<br>• RustFS chỉ dev + CI; production bắt buộc storage có license. | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **70%** |
| 20 | **NFR-SEO-003** | 55 → **85%** | • **Đủ 3 yêu cầu cốt lõi**: cron `0 2 * * *` UTC (recurring `sitemap-daily`) · distributed lock · retry 2 lần.<br>• Sitemap **Published-only**, đủ `loc/lastmod/changefreq/priority`; `robots.ts` khai báo URL.<br>• Lab L5 phase `xml` **3/3**: sinh từ DB thật (`culinary_test`, 93 URL Published) parse lại bằng `XDocument` thành công; phase `seo` **15/15** (metadata, JSON-LD Recipe, robots, canonical). | • **Chưa liệt kê URL từng danh mục**.<br>• **301 redirect chưa kiểm chứng bằng test** (lab `seo` ghi nhận). | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **85%** |
| 21 | **NFR-MAINT-003** | 55 → **70%** | • `CHANGELOG.md`: thêm `0.3.0`, `0.4.0`, `0.4.1`.<br>• `README.md` mục test/ops cập nhật số liệu mới; `docs/evidence/TV4/HUONG_DAN_CHAY_TV4.md` cập nhật lệnh coverage/E2E/k6/outage + "bài học từ lỗi CI".<br>• ADR: `ADR-TV4-002` (RustFS) · hợp đồng `IMAGE_CONTRACT.md` bổ sung **§7b** (presigned) + **§7c** (unique index + soft-delete interceptor).<br>• Bảng mapping K01 24 dòng FR/NFR ↔ ADR ↔ evidence.<br>• CI có secret scan + coverage gate (`check-coverage.sh 80`, probe âm đã thử). | • **Runbook `docs/RUNBOOK.md` đầy đủ 7 mục** (`N4-A`) → tuần 5.<br>• Chưa đo thời gian khởi động từ checkout sạch để chứng minh "<5 phút".<br>• **10 vulnerability `npm audit`** (9 high, 1 critical) chưa xử lý. | [2312739_NHTSon_D5-D6-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D6-D7) | **70%** |

---

## II. Tổng kết

### 2.1. FR đã làm trong tuần 4

| Kết quả | Chi tiết |
|:---|:---|
| **Tiến bộ rõ (≥80%)** | **10/11 FR** — trong đó FR-FILE-002 tăng mạnh **55% → 85%** (đã có retry 3 + log), FR-JOB-003 **45% → 80%** (đủ cron + lock + retry), FR-OBS-001 **80% → 95%** (gỡ B4), FR-OBS-003 **70% → 90%** (trace thật vào Seq) |
| **Đứng yên** | **1/11 FR**: FR-RCP-006 (90% — API xong từ tuần 3, UI chưa làm) |
| **Vẫn thiếu vì phụ thuộc UI** | FR-RCP-005/006/007 dừng ở **90%** — cả 3 chỉ còn thiếu nút Unpublish/Archive/Xóa trên dashboard |
| **Gỡ block trong tuần** | 5/5 GitHub issues **đã sửa + test hồi quy + đóng**: [#20 B1](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/issues/20) (503 storage), [#21 B2](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/issues/21) (fail-fast), [#22 B4](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/issues/22) (probe credential), [#23 B5](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/issues/23) (ảnh Draft presigned), [#24 B6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/issues/24) (Admin seed + `--promote-admin`) — **đóng toàn bộ block B còn mở từ tuần 3** |

### 2.2. NFR đã làm trong tuần 4

| Kết quả | Chi tiết |
|:---|:---|
| **Tiến bộ rõ (≥50% so với đầu kỳ)** | **6/10 NFR**: NFR-REL-002 (50→**85%**), NFR-SEO-003 (55→**85%**), NFR-SCALE-001 (35→**70%**), NFR-REL-001 (45→**60%**), NFR-REL-003 (10→**45%**), NFR-MAINT-003 (55→**70%**) |
| **Tăng một phần** | **2/10 NFR**: NFR-USE-004 (15→20%), NFR-SCALE-003 (65→70%) |
| **Đứng yên** | **2/10 NFR**: NFR-SEC-004 (90% — còn path traversal), NFR-SEC-005 (20% — TLS/HSTS tuần 5) |

### 2.3. Tiến độ tổng thể (tự đánh giá)

| Chỉ số | Tuần 1–2 | Tuần 3 | **Tuần 4** | So với tuần 3 |
|:---|:---:|:---:|:---:|:---|
| FR trung bình | ~30% | ~75% | **~91%** | ▲ |
| NFR trung bình | ~25% | ~48% | **~61%** | ▲ |
| Test backend (`CulinaryBlog.Tests` + spike) | 120/120 | 157/157 | **426/426** (421 + 5), `Skipped=0` | ▲ +269 — *trong đó phần lớn do merge `origin/main` (test của TV3/TV1); baseline nhánh này ngày 30/09 là **178/178*** |
| Test mới TV4 thêm trong kỳ | — | +37 | Nhóm hồi quy lỗi tuần 4: `ApiExceptionHandlerDbUnavailableTests` 6, `BackgroundJobRetryContractTests` 4, `StorageFailureContractTests` 11, `ImageMagicBytesE6Tests` +4 (`file.too_small`), `DbSeederUserImageUrlTests` 2, `JwtSigningKeyNotCommittedTests` 6, `PromoteAdminCommandTests`, `RecipeImagePresignedB5Tests`… | ▲ |
| Coverage `CulinaryBlog.Application` | — | — | **83.37%** (30/09) → **96.31%** (05/10) — vượt ngưỡng G5 80%, gate chạy trong CI | 🆕 |
| Jest (frontend unit) | chưa có | chưa có | **85/85** (14 suites) — ⚠️ từ **PR #27 của TV3** (`C7-frontend tests`), merge về qua `origin/main`, **không phải test TV4 tự viết** | 🆕 (không tính của TV4) |
| Playwright E2E | chưa có | 41/41 test tích hợp FE–BE (khác Playwright) | **26/26** ×3 lần liên tiếp + `--repeat-each=4` **16/16**; phủ **2/5 luồng** (publish, search) | 🆕 |
| Lab cá nhân | chưa có | 4 phase · 39/39 (L4) | + **L5: 63 check · 41 đạt · 3/7 phase PASS** (`seo` 15/15, `observability` 10/10, `multi-instance` 7/7) | ▲ |
| Số liệu hiệu năng | chưa có | EXPLAIN 0.339 ms; k6 smoke p95 225.63 ms | k6 `read-load.js` **3 lần**: ~3606 req ≈ 120 req/s, `http_req_failed` **0.00%**, p95 **14.14 ms** (lấy lần giữa, không lấy lần đẹp nhất) + outage drill (0.2s/0.5s/3.5s) | 🆕 |
| CI GitHub | 🟡 đỏ | 🟢 xanh | 🟡→🟢 **đỏ nhiều run** (khoá JWT trong `.md` của TV4 · `ApiFactory` không seed · race span tracing) → **xanh `8a585ef` (321/321)**; PR #29 commit `ca97131` toàn bộ check xanh | ▼→▲ |
| Ô kỹ năng K | 8/24 (cuối tuần 2) | 16/24 | **9/24 đã duyệt** (theo `PHAN_CHIA`) + bằng chứng mới chờ Tâm: 6 ô đủ (`K08, K13, K19, K20, K21, K24`) · 8 ô gần đạt · 8 ô có nền sản phẩm · **còn thiếu thật 2 ô** (`K09`, `K18`) | ▲ (chưa được duyệt) |

### 2.4. Cổng nghiệm thu trong kỳ

| Cổng | Tiêu chí | Kết quả |
|:---|:---|:---|
| **G4** (giữa tuần 4) | 24/24 ô K có code/config + test + demo + **reviewer Tâm xác nhận** | ⛔ **CHƯA ĐẠT** — 9/24 ô đã duyệt từ trước; tuần 4 có thêm bằng chứng mới cho các ô còn lại nhưng **ô nào cũng chờ Tâm xác nhận và ghi ngày**, không tự đánh dấu. `K09` (Google OAuth — chờ credentials) và `K18` (WCAG — thuộc TV2) **còn thiếu thật** |
| **G5** (cuối tuần 4, 06/10) | Coverage ≥ 80% có số đo thật · sửa xong lỗi chặn luồng/bảo mật · CI xanh | 🟡 **Đạt về kỹ thuật, chờ nghiệm thu**: coverage `Application` **96.31%** (gate CI) · 5 issues B1/B2/B4/B5/B6 đóng + 3 lỗi GĐ1 sửa kèm test · CI xanh `8a585ef` và PR #29 |

### 2.5. Nhận xét

- Tuần 4 là tuần **đóng block + sửa lỗi**, không mở rộng phạm vi: toàn bộ **B1–B6** (block contract lỗi từ tuần 3) được sửa, khoá bằng test hồi quy và đóng trên GitHub; 3 lỗi hạ tầng thật (DB chết trả 500, DB chết lúc khởi động giết process, thiếu credential storage) tìm ra trong outage drill/CI và sửa ngay.
- **Làm theo 3 giai đoạn có kế hoạch** (GĐ1 → GĐ2 → GĐ3) thay vì gộp: GĐ1 đóng N2/N4-C, GĐ2 kiểm tra tích hợp FE–BE một cách độc lập, GĐ3 sửa lỗi + việc phát sinh. Chi tiết dẫn ở đầu báo cáo.
- **Bằng chứng đo đạc thay ước lượng**: k6 3 lần, outage drill, trace thật vào Seq, 2 API instance thật, backup/restore drill 14 bảng — đều lưu log tại [`../Tuan04/logs/`](../Tuan04/logs/).
- **Trung thực về test xanh**: 426/426 gồm test hợp nhất từ `origin/main`; Jest 85/85 là của TV3; E2E mới phủ 2/5 luồng; drill DB là mô phỏng (không có quyền Administrator). Không con số nào được làm tròn lên.
- **Phần lớn việc còn lại của kỳ là việc chưa tới lúc hoặc bị phụ thuộc**: UI dashboard (D1/D2/D3), lab L4 mục 2, Zod FE, Google OAuth, TLS/staging — tất cả đã ghi rõ ở mục III.

---

## III. Block

### 3.1. Block do phụ thuộc người khác hoặc quyết định nhóm

| Yêu cầu bị block | Lý do | Dự kiến |
|:---|:---|:---|
| **D4-UI** — nút Unpublish/Archive/Xóa + thanh tiến trình upload % (FR-RCP-005/006/007, NFR-USE-004, K16/K17/K18) | API đã đủ (`Program.cs:620,627`) nhưng UI dashboard chưa viết → 3 FR kẹt ở 90%, N2-D1/D2 dở dang | Tuần 5 |
| **Lab L4 mục 2** — Identity/Google/refresh/forms/FTS (K08/K09/K10/K11) | Bù nợ tuần 3, chưa làm | Tuần 5 |
| **N3-A5 Google OAuth2/PKCE** (K09) | **Thiếu credentials** — không tính mock là hoàn thành | Chờ credentials |
| **N3-A3 Zod/RHF phía FE** (K05) | `zod` có trong `package.json` nhưng `src/` không import chỗ nào | Tuần 5 |
| **N2-D3 checklist WCAG 320/768/1200 px** (K18) | Thuộc **TV2** theo phân chia công việc tuần 5 | Tuần 5 |
| **E2E 5 luồng** (K21) | Mới phủ 2/5 — `register/login` (TV1), `category` (TV2), `create-recipe` (TV3) | Tuần 5 (cổng G6) |
| **`BUG-W4-01/02/03`** | `01` **đã vào `main`** (PR #29 `c624b9f`) — còn test hồi quy + chốt C1/C2; `02`/`03` chưa có bản vá trong dự án → **sửa trực tiếp tuần 5** (W5-10) | TV4 (W5-10) + Tâm (review PR) |
| **`N4-A` runbook · `N4-B` deploy staging/TLS/HSTS** | Thuộc phạm vi tuần 5 | Tuần 5 |
| **K11 FTS EXPLAIN** | Phụ thuộc TV2 sửa `RecipeRepository` sang `to_tsquery` (vi phạm ADR 0003) — `K11` của TV4 không đóng được khi TV2 chưa sửa | Chờ TV2 |
| **`npm audit` 10 vulnerability** (9 high, 1 critical) | Chưa rà được trong kỳ | Tuần 5 |

### 3.2. Block do cần quyết định / hợp đồng

| Yêu cầu bị block | Lý do | Dự kiến |
|:---|:---|:---|
| **NFR-REL-003 — nơi đặt lịch backup 03:00 ICT** | GH Actions có thể trễ/bỏ qua job khi repo lâu không commit; cần chọn: giữ GH Actions + job canh hay host riêng | [`DE_XUAT_07`](../../../proposal/DE_XUAT_07_NOI_DAT_LICH_BACKUP.md) — chờ Tâm |
| **NFR-REL-003 — kho lưu 30 ngày** | Artifact GitHub giữ 7 ngày; `BACKUP_KEEP_DAYS=30` chỉ dọn file trên runner | [`DE_XUAT_08`](../../../proposal/DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md) — chờ Tâm |
| **K10 / NFR-SEC — rotate khoá JWT đã lộ trong git history** | Nằm ngoài repo, thao tác không thể hoàn tác | [`DE_XUAT_09`](../../../proposal/DE_XUAT_09_ROTATE_KHOA_JWT_DA_LO.md) — chờ Tâm |
| **G4 — nghiệm thu 24 ô K** | 6 ô đủ bằng chứng mới + 8 ô gần đạt đang chờ xác nhận; không ô nào tự đánh dấu | Chờ Tâm |
| **NFR-REL-001 — uptime 99,5%** | Cần vận hành dài hạn trên môi trường có failover; drill ngắn không chứng minh được | Cần staging + dài hạn |

---

## IV. Bug / Error

> Quy ước: chỉ ghi lỗi **tìm ra trong tuần 4**. ✅ = đã sửa + có test/bằng chứng chạy lại · ⏳ = đã ghi nhận, chờ quyết định/merge. Lỗi chi tiết hơn ở [`BAO_CAO_LOI_TUAN_4_TV4.md`](../Tuan04/report/BAO_CAO_LOI_TUAN_4_TV4.md).

| # | Triệu chứng | Nguyên nhân gốc | Cách sửa | Bằng chứng | Trạng thái |
|---:|:---|:---|:---|:---|:---:|
| 1 | Wizard **mất bước sau lần lưu draft đầu** — bị hiểu nhầm là test chập chờn | `saveBasic` đổi URL `/new` → `/{id}/edit` khiến Next remount `RecipeWizard` về `step: 0` | Truyền `?step=` khi lưu + đọc lại khi mount (`RecipeWizard.tsx`, `EditRecipeClient.tsx`), `settleStep()` trong spec | Trước: `--repeat-each=6` → **5/6 đỏ**; sau: 16/16 + 26/26 ×3 | ✅ |
| 2 | **DB chết ⇒ 3 endpoint đọc trả `500`** | `EfUnitOfWork` chạy qua execution strategy của EF → `NpgsqlException` bị bọc thành `InvalidOperationException`; `ApiExceptionHandler` chỉ kiểm tra exception ngoài cùng | Dò cả chuỗi `InnerException`, map Npgsql/socket/timeout → **503 `database.unavailable`**, đặt **trước** nhánh 422 | `ApiExceptionHandlerDbUnavailableTests` **6/6** | ✅ |
| 3 | **DB chết lúc khởi động ⇒ tiến trình chết** | `AddOrUpdate` lịch sitemap ném `NpgsqlException` ra khỏi `Main` | Bọc try/catch + log; `/health/ready` vẫn 503 → orchestrator restart | Drill: app tự phục hồi, lịch đăng lại khi DB trở lại | ✅ |
| 4 | `GET /api/v1/recipes/{slug}` trả **500 khi thiếu credential storage** — **do CI bắt, không do test local** | `MinioClient.Build()` ném ở **constructor**; B5 khiến mọi lần đọc công thức đều dựng service | `Lazy<IMinioClient>` — `Build()` chạy lúc gọi storage → 503 `storage.unavailable`; fail-fast `ValidateOnStart` giữ nguyên | 2 test hồi quy; chứng minh đỏ nếu ép Build ở constructor; CI `8d9d62b` xanh | ✅ |
| 5 | **File JPEG 3 byte được nhận `201`** | Magic bytes chỉ kiểm tiền tố; chữ ký JPEG đúng 3 byte (`FF D8 FF`) nên file rỗng lọt | Thêm `ImageFormats.MinBytes = 64` + mã `file.too_small` (400) — 64 < PNG 67 byte và JPEG ~125 byte nên không loại ảnh thật | `ImageMagicBytesE6Tests` 25/25 (4 test mới: 3 byte, 63 byte, PNG 1×1 67 byte vẫn nhận) | ✅ |
| 6 | **E2E `skip` im lặng** — 10/10 test "xanh" mà không test gì; trước đó TC8 bị skip | `firstCategoryId()` lấy `list[0]` từ cache 60 phút chứa id chết; dò không giới hạn → chạm rate limit **429** → hiểu nhầm "danh mục hỏng" → `null` → `test.skip` | Giới hạn 12 lần dò, **429 ném ra như lỗi hạ tầng**, hết số lần → ném lỗi kèm slug bị loại; verify qua `GET /categories/{slug}` (đọc DB, không cache) | Sau: 12/12 và 10/10 pass thật | ✅ |
| 7 | **`dotnet test` ghi đè cache Redis của môi trường Development** | Test dùng `Instance = "dev"` (không có `appsettings.Testing.json`) ⇒ ghi vào đúng namespace `dev:cache:` mà app dev dùng | Thêm `appsettings.Testing.json` đặt `Redis.Instance = "test"` | Nạp cache dev 25 slug → chạy full suite → key dev **vẫn 25** | ✅ |
| 8 | `TracingObservabilityTests` **đỏ ngẫu nhiên** khi chạy song song | Vòng `foreach` log duyệt `List<Activity>` **ngoài `lock`** trong khi thread khác vẫn `Add` | Chụp `snapshot = activities.ToList()` bên trong `lock` rồi duyệt bản sao | Trước: 2/10 lần đỏ → sau: **7/7 xanh** | ✅ |
| 9 | Chạy E2E lần 2 bị **429 ở `beforeAll`** (rate limit đăng nhập 60s) | Cache token ở RAM, Playwright dựng worker mới mỗi lần chạy | Chuyển cache xuống đĩa (`.playwright/token-cache.json`), đọc `exp` từ JWT, coi token hết hạn sớm 60s | 3 lần liên tiếp **26/26** | ✅ |
| 10 | `DELETE .../images/{imageId}` trả **204 nhưng dòng DB không biến mất** | `Recipe.Images` chỉ expose `IReadOnlyList` → EF không phát hiện orphan với cascade | `MarkImageDeleted` qua `DbSet.Remove` tường minh + `DeleteBehavior.ClientCascade` | `E4` kiểm cả 3 điều: 204 · đúng 1 primary thay thế · dòng **thực sự biến mất** | ✅ |
| 11 | `23505 duplicate key ux_recipe_images_one_primary` → API trả **422** khi đổi/xóa ảnh primary | Postgres kiểm unique partial **từng câu lệnh**, không deferrable; EF không bảo đảm thứ tự UPDATE | Lưu **nhiều lần trong 1 transaction** (hạ primary → bật thay thế → DELETE); ghi rõ vào `IMAGE_CONTRACT.md §7c` | Test chuyển primary/xóa primary; `E3` chấp nhận 422 cho request thua (hành vi đúng D19) | ✅ |
| 12 | **Khoá JWT đã thu hồi** nằm trong `docker-compose.staging.yml` (do merge `main` mang vào) | Commit `21aa722` của TV1 chép khoá dev mà `JwtSettings.Validate()` đã cấm | Thay bằng `${JWT_SIGNING_KEY:?}` không default | `JwtSigningKeyNotCommittedTests`: **1 fail/316 → 6/6** | ✅ |
| 13 | Mỗi lần API khởi động, **URL ảnh người dùng bị ghi đè** thành ảnh seed | `DbSeeder` dùng `!StartsWith("/images/recipes/")` khớp mọi URL không phải seed | Chỉ coi là ảnh mẫu khi `StartsWith("/images/") && !StartsWith("/images/recipes/")` hoặc Unsplash placeholder | `DbSeederUserImageUrlTests` **2/2** — chứng minh **đỏ 2/2** trên code cũ, xanh sau fix | ✅ |
| 14 | `--migrate` **báo thành công giả** khi migration thất bại | `try { ... } catch { }` rồi vẫn in "applied successfully" | Bỏ catch nuốt lỗi; in `Database migration FAILED: ...` + `ExitCode = 1` | Thiếu migration → exit 1 + thông báo lỗi | ✅ |
| 15 | `dotnet build` fail `MSB3027` | 2 tiến trình API lab đang giữ DLL | Dừng tiến trình rồi build lại | Build 0 warning/0 error | ✅ (không phải lỗi code) |

### Lỗi còn mở (không sửa trong tuần 4)

| Mã | Lỗi | Vì sao còn mở |
|:---|:---|:---|
| `BUG-W4-01` | `422 recipe.version_conflict` khi thêm ảnh vào recipe đã có ảnh | 🟠 Đã tái hiện + sửa cục bộ 03/10 — **07/10: đã vào `main`** (PR #29 `c624b9f`) — còn test hồi quy + chốt C1/C2 (W5-10, đụng schema của TV3) |
| `BUG-W4-02` | Secret còn trong file tracked, `scan-secrets.sh` không bắt được | 🔴 **Chưa có bản vá trong dự án** (chỉ kiểm tra cục bộ 03/10) — sửa trực tiếp tuần 5 (W5-10) |
| `BUG-W4-03` | 2 IP khác nhau dùng chung 1 bucket | 🔴 **Chưa có bản vá trong dự án** (chỉ kiểm tra cục bộ 03/10) — sửa trực tiếp tuần 5 (W5-10) |
| `BUG-W4-08/09/10` | Chưa kiểm tra sâu · điều kiện cạnh tranh · migration no-op | ⏳ Chưa xác minh — không tự kết luận |
| — | RowVersion/optimistic concurrency E2E | ⛔ Tài khoản E2E không sở hữu công thức (`GetRecipesQuery` không filter chủ sở hữu) |
| — | Google OAuth thật | ⛔ Thiếu credentials |
| — | Uptime thật 99,5% | ⛔ Cần vận hành dài hạn, không sửa được bằng code |

### Tài liệu báo cáo lỗi chi tiết

| File | Nội dung |
|:---|:---|
| [`../Tuan04/report/BAO_CAO_GIAI_DOAN_1_N2_N4.md`](../Tuan04/report/BAO_CAO_GIAI_DOAN_1_N2_N4.md) | GĐ1 — N2/N3/N4-C, 3 lỗi thật (mục 1), giới hạn 13 điểm, 13 phát sinh |
| [`../Tuan04/report/BAO_CAO_GIAI_DOAN_2_N3.md`](../Tuan04/report/BAO_CAO_GIAI_DOAN_2_N3.md) | GĐ2 — kiểm tra tích hợp tổng hợp FE–BE, 3 quan sát giao tiếp |
| [`../Tuan04/report/BAO_CAO_GIAI_DOAN_3_SUA_LOI.md`](../Tuan04/report/BAO_CAO_GIAI_DOAN_3_SUA_LOI.md) | GĐ3 — sửa lỗi + kiểm chứng báo cáo "Tuần 5" của TV1 + đóng 5 issues |
| [`../Tuan04/report/BAO_CAO_LOI_TUAN_4_TV4.md`](../Tuan04/report/BAO_CAO_LOI_TUAN_4_TV4.md) | Danh sách `BUG-W4-01…10` còn mở |
| [`../Tuan04/misc/KiemChung_Commit_Week5_TV1.md`](../Tuan04/misc/KiemChung_Commit_Week5_TV1.md) | Hồ sơ kiểm chứng 4 sai lệch báo cáo TV1 (việc cuối GĐ3) |
| [`../Tuan04/report/SOK_LAB_L5.md`](../Tuan04/report/SOK_LAB_L5.md) | Sổ K Lab L5 — 63 check, 3/7 phase PASS, 4 phase lộ vấn đề thật |
