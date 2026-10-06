# BÁO CÁO TIẾN ĐỘ TUẦN 3 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **Kỳ báo cáo**: Tuần 3 (23/09/2026 – 28/09/2026)
> **Báo cáo liền kề**: `Tuan02/BAO_CAO_TIEN_DO_2_TUAN_TV4_SRS.md` (tuần 1–2)
> **Tài liệu yêu cầu**: SRS Culinary Blog v1.1.1 (Approved 16/09/2026)
> **Phạm vi phụ trách**: Xuất bản công thức, quản lý ảnh, SEO, vận hành hệ thống và đăng xuất
> **Reviewer**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Nhánh làm việc**: `2312739_NHTSon_D3-D4-D5-D6` — đã merge `origin/main` và **đã được merge vào main qua PR #16**
> **Nhánh lab**: `practice/TV4/L4` (commit `3642428`, đã push)
> **Cổng nghiệm thu trong kỳ**: G3 (đủ 34 FR chạy tích hợp) và G5 (media/status/jobs/health + CI xanh)

---

## I. Công việc

### 1. Bảng FR

| Mã FR | Mô tả |
|:---|:---|
| **FR-RCP-005** | Cập nhật trạng thái xuất bản công thức: `PATCH /recipes/{id}/publish` và `PATCH /recipes/{id}/unpublish`. Publish yêu cầu ≥1 nguyên liệu VÀ ≥1 bước (C02), thiếu trả HTTP 422 `RECIPE_PUBLISH_INCOMPLETE`. |
| **FR-RCP-006** | Lưu trữ công thức: `PATCH /recipes/{id}/archive`. Ẩn khỏi danh sách công khai, giữ nguyên dữ liệu. Chỉ owner/Admin. |
| **FR-RCP-007** | Xóa công thức: `DELETE /recipes/{id}`. Theo C01 là **Soft Delete** (`IsDeleted = true`), file ảnh trên object storage giữ nguyên. Không lộ trong tìm kiếm/cache sau khi xóa. |
| **FR-RCP-008** | Quản lý ảnh công thức: `POST /recipes/{id}/images` (upload), `PATCH /recipes/{id}/images/{imageId}` (đặt primary/altText/orderIndex), `DELETE /recipes/{id}/images/{imageId}`. Đúng 1 primary khi có ảnh. |
| **FR-FILE-001** | `IFileStorageService.UploadAsync`: upload file lên S3-compatible storage, kiểm tra kích thước ≤5 MiB, 4 định dạng (JPEG/PNG/WebP/AVIF), xác minh magic bytes (không tin Content-Type client), lưu GUID path. |
| **FR-FILE-002** | `IFileStorageService.DeleteAsync`: xóa object idempotent (xóa file không tồn tại vẫn thành công), retry 3 lần, ghi log khi thất bại. |
| **FR-JOB-002** | `ImageResizeJob`: sinh ảnh 300×300 và 800×600 từ original, cập nhật URLs vào DB, fallback về original nếu resize thất bại, retry idempotent. |
| **FR-JOB-003** | `SitemapGenerationJob`: cron `0 2 * * * UTC`, sinh sitemap XML chứa trang Published/categories/static, retry 2 lần, distributed lock khi chạy nhiều worker. |
| **FR-OBS-001** | `GET /health` (kiểm tra DB + Redis + object storage), `GET /health/live` (process sống), `GET /health/ready` (DB + Redis; ready trả 200/503). |
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
| **NFR-SCALE-003** | Container tách service (API/DB/Redis/object storage/Nginx); Nginx upstream nhiều backend; hồ sơ scale storage/CDN. |
| **NFR-SEO-003** | Sitemap XML đầy đủ `loc/lastmod/changefreq/priority`; robots.txt khai báo URL; cron 02:00 UTC. |
| **NFR-MAINT-003** | README khởi động <5 phút; OpenAPI/Scalar; ADR; CHANGELOG. |

### 3. Bảng hiện trạng

> Quy ước: **Hoàn thành** = đã có code + test/bằng chứng chạy thật trong kỳ; **Chưa hoàn thành** = còn thiếu hoặc đang chờ quyết định/phụ thuộc.
> Cột "Hoàn thành" ghi **phần làm thêm trong tuần 3**; phần đã có từ tuần 1–2 không lặp lại (xem báo cáo 2 tuần).

| STT | Mã | Hoàn thành | Chưa hoàn thành | Link github branch | Tiến độ |
|---:|:---|:---|:---|:---|:---:|
| 1 | **FR-RCP-005** | • Đã xác minh lại sau khi merge main `a651c8a`: `RecipeLifecycleHandlerTests` (file `RecipeLifecycleHandlerTests.cs`) **23/23 pass** trong đó có vòng publish → unpublish đủ.<br>• Đã kiểm chứng điều kiện C02 (≥1 nguyên liệu VÀ ≥1 bước) trả 422 `RECIPE_PUBLISH_INCOMPLETE` khi thiếu, 403 khi non-owner, lặp trạng thái trả 200 idempotent.<br>• Đã sửa lỗi EF do TV3 phát hiện: entity con mới bị đánh dấu `Modified` do Guid key khác rỗng → 422 giả; interceptor chuyển `Added` khi `RowVersion` gốc rỗng (fix `e3e8315`). | • Chưa có **nút Unpublish** trên UI dashboard (thuộc D4-UI, phải merge `main` trước để sửa `dashboard/recipes/**`). | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **90%** |
| 2 | **FR-RCP-006** | • 24/09: cài đặt `ArchiveRecipeCommand` + Validator + Handler (region D3 trong `Recipes.cs`) và endpoint `PATCH /recipes/{id}/archive`.<br>• Published/Draft → Archived, **ẩn khỏi public list ngay**, giữ nguyên dữ liệu, idempotent, 403 với non-owner, có test trong `RecipeLifecycleHandlerTests`. | • Chưa có **nút Archive** trên UI dashboard (D4-UI). | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **90%** |
| 3 | **FR-RCP-007** | • 24/09: cài đặt `DeleteRecipeCommand` + `Recipe.MarkDeleted()` — **soft delete theo C01/D08**, global query filter ẩn khỏi mọi truy vấn, **không xóa vật lý ảnh** cần restore.<br>• Đã kiểm chứng công thức đã xóa không xuất hiện trong public list, search và sitemap.<br>• Đã bổ sung test `Delete_stale_row_version_throws_422` (RowVersion cũ → 422 `recipe.version_conflict`). | • Chưa có **nút Xóa** trên UI dashboard (D4-UI).<br>• Chưa viết ADR TV4-001 về giữ/xóa object ảnh khi soft delete (CR-3 còn mở). | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **90%** |
| 4 | **FR-RCP-008** | • 24/09: **E2E thật trên object storage** — `MinioE2ETests` 3/3 pass, lặp nhiều lần ổn định: register → create recipe → upload JPEG → readback → PATCH primary → publish → unpublish → archive → delete → không còn trong public list.<br>• 27/09: **proxy ảnh D27 (PA-2)** — `GET /api/v1/resources/images/{**key}`; Published công khai + `Cache-Control: public, max-age=3600`; Draft/Archived chỉ owner/Admin, nếu không trả `403 image.forbidden`; key sai/ảnh không tồn tại/recipe đã soft-delete → `404 image.not_found`. Test `ImageProxyD27Tests` **6/6 pass**.<br>• 27/09: tách `IObjectStorageReader` (đọc) và `IObjectStorageWriter` (ghi key phái sinh) khỏi `IFileStorageService`/`StoredFile` — **không phá contract bàn giao TV3**.<br>• 27/09: xóa ảnh → xóa luôn **cả 2 object phái sinh**; delete-vs-resize không tái sinh ảnh đã xóa.<br>• Contract chốt tại `docs/IMAGE_CONTRACT.md §5` và `§7`. | • Chưa có **thanh tiến trình upload %** trên uploader UI (NFR-USE-004).<br>• Ảnh **Draft** trong wizard không xem được vì thẻ `<img>` không gửi Bearer → proxy 403 (block **B5**, cần chọn presigned/token query/cookie). | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **90%** |
| 5 | **FR-FILE-001** | • 24/09: CI bổ sung service object storage + env + bước chờ health → **E2E upload chạy thật trong CI** (không còn skip âm thầm).<br>• 28/09 (lab L4): phase `media` **25/25 check** — MIME nhận diện theo **magic bytes** đủ 4 định dạng; file khai báo MIME giả → `file.invalid_type`; ảnh vượt giới hạn → `file.too_large`; upload + đọc lại khớp byte; dọn dẹp sạch.<br>• 28/09: sửa lỗi 500 do thiếu cấu hình storage trong `appsettings.Development.json` + thêm 3 test `DevConfigParityTests` chặn tái diễn.<br>• 28/09: sửa lỗi đọc object cắt cụt (MinIO SDK `WithCallbackStream` nhận `Action<Stream>` nên `async` lambda biến thành **async void fire-and-forget**) → copy **đồng bộ** + kiểm tra `buffer.Length == stat.Size`. | • Chưa có test **path traversal** cụ thể (tên file chứa `../`) để chứng minh GUID path chặn truy cập ngoài bucket/prefix. | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) · [lab](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/practice/TV4/L4) | **95%** |
| 6 | **FR-FILE-002** | • 27/09: xóa ảnh xóa **luôn cả object 300×300 và 800×600** (không để orphan trong bucket).<br>• 27/09: xử lý race **delete-vs-resize** — nếu row ảnh đã biến mất thì job resize không ghi lại object (test: ảnh đã xoá → proxy trả 404, không tái sinh file).<br>• 28/09 (lab L4): xác minh xóa object phải đi qua `IFileStorageService` (`IObjectStorageWriter` chỉ `Exists`/`Upload`); dọn dẹp sạch sau mỗi phase. | • **`MinioStorageService.DeleteAsync` chưa có vòng retry 3 lần và chưa ghi log khi xóa object thất bại** (đọc code hiện tại: chỉ gọi `RemoveObjectAsync` 1 lần, không có `for/while` retry). | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **55%** |
| 7 | **FR-JOB-002** | • 27/09: gỡ block D23 — nhóm chốt **PA-1 Hangfire** (`docs/DE_XUAT_GIAI_QUYET_D23_D27.md`): queue persistent PostgreSQL + retry 3 + dashboard Admin.<br>• Cài đặt `ResizeImageJob` (300×300, 800×600, `ResizeMode.Max`) + `IImageResizeQueue` (Hangfire / Inline ở `Testing`) + `IObjectStorageWriter` ghi key `{base}_300x300.ext` / `{base}_800x600.ext`, cập nhật `MediumUrl`/`ThumbnailUrl`.<br>• Bất biến đã kiểm chứng: **idempotent** (`ExistsAsync`), **original fallback** (ảnh hỏng/AVIF → giữ `null`, không 5xx), **delete-vs-resize** không tái sinh, `[AutomaticRetry(Attempts = 3)]`.<br>• Dashboard `/hangfire` **chỉ Admin**: anon 401 / member 403 / Admin 200 (test thật).<br>• **Chạy thật**: API + storage + Postgres → upload JPEG 1200×800 → **Hangfire job `Succeeded` (~1s)** → proxy trả `300×200` và `800×533`; DB có `MediumUrl`/`ThumbnailUrl`.<br>• Test `ImageResizeD2Tests` **4/4** + 5 unit mới trong `RecipeImageTests`; suite **148/148 + 5/5 spike** (2 vòng).<br>• 28/09 (lab L4): phase `jobs` **8/8** — tắt worker → job vẫn còn `Scheduled` trong DB → **chạy lại khi restart**; retry quan sát được `Retry attempt 1,2 of 5` rồi `Succeeded`; recurring do scheduler kích hoạt chạy 2 lần.<br>• Ghi log thô: `Tuan03/logs/d2_resize_hangfire.log`, `d2_resize_hangfire_db.txt`. | • AVIF mới có bằng chứng ở mức MIME/upload/xóa; chưa chạy tay nhánh resize với AVIF thật (đã có test trong `ImageResizeD2Tests`). | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) · [lab](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/practice/TV4/L4) | **95%** |
| 8 | **FR-JOB-003** | • 24/09: `GetSitemapQuery` + `GetPublishedForSitemapAsync` + endpoint `GET /recipes/sitemap` — **chỉ lấy Published**, loại Draft/Archived/soft-deleted.<br>• 24/09: `src/frontend/src/app/sitemap.ts` sinh sitemap có đủ `loc` / `lastmod` / `changefreq` / `priority` cho trang tĩnh **và** từng công thức Published (93 URL sinh được từ DB thật trong lab, parse lại bằng `XDocument`).<br>• `robots.ts` khai báo URL sitemap.<br>• 28/09 (lab L4): phase `xml` **3/3 check** — sinh sitemap từ DB thật `culinary_test` (chỉ đọc) và parse lại thành công. | • **Chưa có job theo lịch `0 2 * * * UTC`** — hiện sitemap chỉ sinh on-demand theo request/ISR (block **CR-7**).<br>• **Chưa có distributed lock** khi nhiều worker cùng sinh sitemap (yêu cầu của đề).<br>• Sitemap chưa liệt kê **từng danh mục** (mới chỉ có URL tĩnh `/categories`); chưa có retry 2 lần. | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **45%** |
| 9 | **FR-OBS-001** | • Health check mở rộng phủ **3 dependency**: DB (EF `CanConnectAsync`), Redis và object storage (TCP probe 3s).<br>• 24/09: `/health`, `/health/live`, `/health/ready` chạy thật trong E2E và CI; không cần NuGet mới nên không phá `restore --locked-mode`.<br>• 28/09: health-cmd của service object storage trong CI/compose chỉnh lại theo đúng server đang chạy (RustFS phục vụ `/health`, **không** phục vụ `/minio/health/live` như MinIO). | • Chưa có bằng chứng **`/health/ready` trả 503 khi Redis down** (D22) — `PHAN_CHIA` xếp "health failure" ở tuần 4.<br>• `/health` hiện chỉ TCP-probe storage nên báo `Healthy` **dù credential sai** (block **B4**, cần nhóm chốt ngữ nghĩa). | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **80%** |
| 10 | **FR-OBS-003** | • 27/09: xác minh cấu hình OTEL còn nguyên (commit `2bbee0d`) — tracing ASP.NET/HttpClient/EF Core/OTLP; metrics ASP.NET/HttpClient/EF meter; lock 1.19.x.<br>• 27/09: **ghi số liệu đo thật** vào `Tuan03/logs/`: EXPLAIN publish list **0.339 ms** (12 rows), EXPLAIN count **0.044 ms**; k6 smoke 20 VU × 30s: **3310 request · 0% fail · check 100% · avg 81.58 ms · p90 193.45 ms · p95 225.63 ms (<250) · 109.42 req/s**.<br>• Log không chứa secret. | • **Chưa chụp được trace thật đi vào Seq** (HTTP → DB) — cần bật service `seq` trong compose + gửi OTLP rồi lưu bằng chứng (K20 còn thiếu). | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **70%** |
| 11 | **FR-AUTH-005** | • Xác minh 23/09: **C5 refresh token đã có trên `main`** (`IdentityService.RefreshTokenAsync` có rotation + family reuse revocation, `LogoutAsync` revoke theo hash **và** revoke toàn bộ token active của family), endpoint `POST /auth/refresh` + `POST /auth/logout` đã khai báo trong `Program.cs`.<br>• **7 test Week3 + 16 test Auth pass** ⇒ D3.3 logout revoke refresh family coi như hoàn thành (đóng block chờ TV3 từ đầu tuần 2). | • Không còn mục nào thuộc giai đoạn này còn bỏ trống (phần kiểm thử chi tiết token lifecycle đã gồm trong 41 test case tích hợp FE+BE, nhóm G). | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **95%** |
| 12 | **NFR-SEC-004** | • Lab L4 phase `media` chứng minh magic bytes thật cho 4 định dạng, file MIME giả bị chặn, ảnh vượt giới hạn bị chặn **trước khi nạp buffer**.<br>• Sửa lỗi đọc object: kiểm tra `buffer.Length == stat.Size`, thiếu → `IOException` (không trả về stream cắt cụt).<br>• 28/09: thêm 3 test `DevConfigParityTests` khoá parity cấu hình storage giữa `appsettings.Development.json` và `docker-compose.dev.yml` — ngăn lỗi "tưởng đã cấu hình".<br>• 28/09: `.env` bị chặn ở cả `.gitignore` **và** `.dockerignore`; không commit credential thật. | • Chưa có test **path traversal** cụ thể (tên file chứa `../`).<br>• Phần XSS sanitization + CSP do TV1 phụ trách, chưa tích hợp với uploader. | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) · [lab](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/practice/TV4/L4) | **90%** |
| 13 | **NFR-SEC-005** | • CORS explicit origins đã được cấu hình trên `main` (commit `a651c8a` kèm deploy config) và giữ nguyên khi merge PR #16.<br>• Endpoint ảnh đã tách quyền rõ ràng: ảnh Published công khai, ảnh Draft/Archived yêu cầu Bearer và trả `no-store` (không rò rỉ qua cache). | • **Chưa có** TLS ≥1.2, HTTP→HTTPS redirect, `Strict-Transport-Security` (thuộc Nginx production tuần 5). | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **20%** |
| 14 | **NFR-REL-001** | • Health probes `/health/live` + `/health/ready` phục vụ giám sát uptime và vòng đời container.<br>• CI chạy xanh thật 2 lần liên tiếp sau sự cố registry (run `36343309464`, `36344662570`) với **10/10 bước success**; dev stack dựng được trên máy mới (container `culinaryblog-s3` healthy). | • Chưa có cảnh báo khi down >1 phút và chưa đo uptime thực tế (cần staging chạy liên tục ở tuần 5).<br>• Chưa có quy ước quy đổi 99,5% ↔ số giờ theo ADR D21. | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **45%** |
| 15 | **NFR-REL-002** | • Cài đặt Hangfire retry 3 lần cho `ResizeImageJob` (`[AutomaticRetry(Attempts = 3)]`) — retry **idempotent** nhờ `ExistsAsync` trước khi ghi.<br>• **Job không mất khi worker chết**: lab L4 tắt worker → job vẫn còn `Scheduled` trong DB → chạy lại khi restart.<br>• Xử lý lỗi dependency an toàn: ảnh hỏng/AVIF không decode → **fallback original**, request upload vẫn trả 201 (không 5xx).<br>• Xóa ảnh và resize chạy song song không gây hậu quả (row biến mất → job bỏ qua). | • Chưa cấu hình **DB reconnect / timeout 30 giây** và **Redis fallback** khi Redis chết (chưa có test cắt dependency thật).<br>• Lỗi object storage hiện trả **500 `server.error` chung chung**, FE không phân biệt được → đề xuất thêm mã `storage.unavailable` và đổi 500 → 503 (block **B1**, cần nhóm thống nhất contract lỗi).<br>• API vẫn khởi động "thành công" dù thiếu credential storage (block **B2** — chưa chốt fail-fast/cảnh báo). | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **50%** |
| 16 | **NFR-REL-003** | • Volume `s3data` (đổi từ `miniodata`) bền qua restart container; app tự `BucketExists → MakeBucket` nên không phụ thuộc job `minio-init`.<br>• Xác minh refresh token sống qua restart ứng dụng (bảng `RefreshTokens` lưu trong PostgreSQL, đã test qua 16 test Auth + 7 test Week3). | • Chưa cài đặt **job `pg_dump` hàng ngày, giữ 30 ngày** và **chưa có restore drill** (thuộc tuần 4–5).<br>• Cần nhóm chốt **timezone cho lịch backup** (không suy ra UTC chỉ vì sitemap dùng UTC). | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **10%** |
| 17 | **NFR-USE-004** | • Rà `main`: `dashboard/recipes/_wizard/ImagesStep.tsx` (PR #15 của TV3) **đã có** upload/delete/set-primary + **optimistic rollback** + đã dùng `NEXT_PUBLIC_MEDIA_URL`; đã merge vào `main` trong PR #16 nên nhánh TV4 đã có đủ file để sửa tiếp.<br>• Đã sửa hướng dẫn `docs/HUONG_DAN_CHAY_TV4.md` (L9) vốn bảo để trống `NEXT_PUBLIC_MEDIA_URL` trong khi code trả `null` → không hiện ảnh; nay đã đặt đúng `http://localhost:5080/api/v1/resources/images`.<br>• 41 test case tích hợp FE+BE chạy qua API thật có phần D (ảnh) **7/7 PASS** (trước đây D01 = 500). | • **Chưa làm thanh tiến trình upload %** — phần lớn còn lại của NFR này.<br>• Chưa có checklist WCAG/keyboard cho upload + status button (K18). | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **15%** |
| 18 | **NFR-SCALE-001** | • Cài đặt `AddHangfireServer(WorkerCount = 4)` + storage PostgreSQL chung ⇒ **nhiều worker dùng chung một queue bền vững** (không phải queue in-memory).<br>• Job resize chạy thật `Succeeded`; job không mất khi restart worker (kiểm chứng ở lab L4).<br>• JWT stateless (`MapInboundClaims=false` + `RoleClaimType`/`NameClaimType` chuẩn) không lưu session server-side. | • **Chưa có distributed lock** cho sitemap job (phụ thuộc FR-JOB-003 chưa có job).<br>• **Chưa chạy test 2 API instance** dùng chung cache/jobs (thuộc tuần 4–5).<br>• Chưa có Redis chia sẻ cache cho recipe (xác minh hiện **không tồn tại** cache recipe nào — đóng tiêu chí "không phục vụ bằng cache cũ" theo mặc định). | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **35%** |
| 19 | **NFR-SCALE-003** | • 28/09: gỡ **sự cố CI 5 run đỏ** do image container object storage bị gỡ khỏi registry — thay bằng `rustfs/rustfs` (S3-compatible) cho **cả dev compose + CI**, ghim **tag + digest**; `MinioStorageService` **không đổi dòng nào**.<br>• Chốt CR-6 và ghi `docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md`; xử lý **7 conflict** khi merge `origin/main` (giữ RustFS, bỏ `minio-init`, giữ `If-Match`/`RowVersion` 422 của TV3, giữ `ArchiveRecipeCommand` của TV4).<br>• Verify: `docker compose config --quiet` exit 0, container healthy, `tsc --noEmit` exit 0, `next build` exit 0 (16/16 trang). | • Chưa có **Nginx upstream nhiều backend** và `docker-compose.prod.yml` (thuộc tuần 5).<br>• Hồ sơ scale storage 4+ nodes/CDN mới ở mức thiết kế — ghi rõ **chưa nghiệm thu triển khai**.<br>• **RustFS chỉ dùng cho dev + CI**; production bắt buộc dùng object storage có license. | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **65%** |
| 20 | **NFR-SEO-003** | • 24/09: `robots.ts` + `sitemap.ts` chạy thật; `next build` exit 0 và `robots.txt`/`sitemap.xml` có trong danh sách route.<br>• Sitemap **Published-only** — Draft/Archived/đã xóa không xuất hiện; có đủ `loc`/`lastmod`/`changefreq`/`priority`.<br>• 28/09 (lab L4): sinh sitemap từ **DB thật** (93 URL Published) và parse lại bằng `XDocument` — chứng minh XML hợp lệ, không phải dữ liệu giả. | • **Chưa có cron `0 2 * * * UTC`** — sitemap chỉ sinh on-demand/ISR, nên không cam kết được "cập nhật trong 24h" (block **CR-7**).<br>• Chưa liệt kê từng URL danh mục; chưa có retry 2 lần; chưa có distributed lock. | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **55%** |
| 21 | **NFR-MAINT-003** | • Ghi `docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md`; cập nhật bảng quyết định trong `README.md`; viết `docs/DE_XUAT_GIAI_QUYET_D23_D27.md` (D23 → PA-1, D27 → PA-2).<br>• Cập nhật `docs/IMAGE_CONTRACT.md §5` và `§7`; `docs/HUONG_DAN_CHAY_TV4.md`; `docs/HUONG_DAN_TEST_APP.md`.<br>• Viết 4 sổ tuần 3 (`KE_HOACH_TUAN_3_TV4.md`, `TRANG_THAI_THUC_HIEN_TUAN_3.md`, `SO_EVIDENCE_TUAN_3.md`, `HANDOFF_TV4_TUAN3.md`) + 4 báo cáo/đề xuất trong `Tuan03/Report/`.<br>• CI có 4 bước kiểm: restore `--locked-mode` → build (0 warning) → `dotnet format --verify-no-changes` → test. | • **CHANGELOG** chưa cập nhật cho các thay đổi tuần 3 (để dành bàn giao tuần 5–6).<br>• Chưa đo thời gian khởi động từ checkout sạch để chứng minh "<5 phút". | [2312739_NHTSon_D3-D4-D5-D6](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D3-D4-D5-D6) | **55%** |

---

## II. Tổng kết

### 2.1. FR đã làm trong tuần 3

| Kết quả | Chi tiết |
|:---|:---|
| **Hoàn thành (≥90%)** | **7/11 FR**: FR-RCP-005, FR-RCP-006, FR-RCP-007, FR-RCP-008, FR-FILE-001, FR-JOB-002, FR-AUTH-005 (kèm FR-FILE-001 và FR-JOB-002 ở 95%) |
| **Làm được một phần lớn** | **1/11 FR**: FR-FILE-002 (55% — còn thiếu retry 3 lần + log khi xóa object thất bại) |
| **Làm được một phần** | **2/11 FR**: FR-JOB-003 (45%), FR-OBS-003 (70%) |
| **Còn thiếu phần vận hành** | **1/11 FR**: FR-OBS-001 (80% — thiếu bằng chứng `/health/ready` 503 khi Redis down) |
| **Gỡ được toàn bộ block của tuần 2** | FR-RCP-005/006/007 (cần Recipe aggregate của TV3), FR-RCP-008 (E2E cần recipe fixture), FR-AUTH-005 (cần C5 refresh token), FR-JOB-002 (cần chốt queue D23) |

### 2.2. NFR đã làm trong tuần 3

| Kết quả | Chi tiết |
|:---|:---|
| **Tiến bộ rõ (≥50%)** | **4/10 NFR**: NFR-SEC-004 (90%), NFR-REL-002 (50%), NFR-SCALE-003 (65%), NFR-SEO-003 (55%) |
| **Tiến bộ một phần** | **4/10 NFR**: NFR-REL-001 (45%), NFR-SCALE-001 (35%), NFR-MAINT-003 (55%) |
| **Gần như không đổi** | **2/10 NFR**: NFR-SEC-005 (20% — chờ Nginx production tuần 5), NFR-REL-003 (10% — backup/restore thuộc tuần 4–5) |

### 2.3. Tiến độ tổng thể (tự đánh giá)

| Chỉ số | Tuần 1–2 | **Tuần 3** | So với tuần 2 |
|:---|:---:|:---:|:---|
| FR trung bình | ~30% | **~75%** | ▲ |
| NFR trung bình | ~25% | **~48%** | ▲ |
| Test backend (`CulinaryBlog.Tests`) | 120/120 | **157/157** (`Skipped=0`) | ▲ +37 test |
| Test concurrency spike | 5/5 | **5/5** | = |
| Test tích hợp FE+BE (chạy API thật) | chưa có | **41/41 PASS** | 🆕 |
| Lab cá nhân | chưa có | **4 phase · 39/39 check** | 🆕 |
| Số liệu hiệu năng | chưa có | EXPLAIN **0.339 / 0.044 ms**; k6 **3310 req · 0% fail · p95 225.63 ms · 109.42 req/s** | 🆕 |
| CI GitHub | 🟡 đỏ do migration trùng | 🟢 **xanh** (chạy xanh 2 lần liên tiếp sau khi gỡ sự cố image registry) | ▲ |
| Hệ số kỹ năng K đã có bằng chứng | 8/24 (cuối tuần 2) | **16/24** | ▲ +8 |

### 2.4. Cổng nghiệm thu trong kỳ

| Cổng | Tiêu chí | Kết quả |
|:---|:---|:---|
| **G3** | Đủ 34 FR chạy tích hợp + test ban đầu | ✅ Phần của TV4 đủ: media (upload/proxy/resize), status (publish/unpublish/archive/delete), jobs, health, SEO |
| **G5** | FR media/status/jobs/health; queue persistent; sitemap/OG/JSON-LD; CI xanh | ✅ **6/8 đạt** — thiếu (1) UI uploader + nút status, (2) sitemap job theo lịch 02:00 UTC + distributed lock, (3) bằng chứng trace thật vào Seq, (4) bằng chứng `/health/ready` 503 khi Redis down |

### 2.5. Nhận xét

- Tuần 3 tập trung vào **gỡ block** thay vì mở rộng phạm vi: 6 block của tuần 2 được gỡ hết (recipe aggregate, refresh token, queue D23, bucket D27, E2E storage, merge `main`).
- Ba lỗi hạ tầng gây ảnh hưởng **toàn nhóm** đã được phát hiện và gỡ ngay: image object storage bị gỡ khỏi registry, cấu hình mặc định DB lệch giữa `appsettings` và compose, và endpoint ảnh trả 500 do thiếu cấu hình.
- Bằng chứng đo đạc thay cho ước lượng: EXPLAIN, k6, số test, log job Hangfire, log lab đều lưu tại `docs/evidence/TV4/Tuan03/logs/`.
- Phần chưa làm lớn nhất của kỳ là **D4-UI** (progress upload + 3 nút trạng thái) và **lab Identity/Google/refresh/forms/FTS** — mục thứ hai là yêu cầu của **tuần 3** theo `PHAN_CHIA_CONG_VIEC_6_TUAN.md` mục 3.4, cần nhóm trưởng chốt cách xử lý.

---

## III. Block

### 3.1. Block do phụ thuộc người khác hoặc quyết định nhóm

| Yêu cầu bị block | Lý do | Dự kiến |
|:---|:---|:---|
| **D4-UI** — thanh progress upload + nút Unpublish/Archive (FR-RCP-005/006/007/008, NFR-USE-004, K16/K17/K18) | API đã sẵn sàng nhưng `main` đã có wizard của TV3 (PR #15) nên phải merge mới sửa được; đã merge trong PR #16, còn phần code UI phải viết mới. | Tuần 4 |
| **Lab Identity/Google/refresh/forms/FTS** (K08, K09, K11, K16) | Chưa làm trong tuần 3 | Đầu tuần 4 |
| **PR cho nhánh lab** `practice/TV4/L4` | Nhánh đã push nhưng chưa mở PR; `PHAN_CHIA` yêu cầu "có PR lab ngoài phần chính". | Tuần 4 |
| **Bảng mapping K01** (FR ↔ ADR ↔ evidence key) | Đã có ADR nhưng chưa có bảng mapping để reviewer nghiệm thu. | Tuần 4 |
| **NFR-REL-003** — backup/restore drill | Theo kế hoạch ở tuần 4–5. | Tuần 4–5 |
| **NFR-SEC-005** — TLS/HSTS/HTTPS | Thuộc Nginx production. | Tuần 5 |
| **NFR-SCALE-001** — test 2 API instance, distributed lock | Cần staging tích hợp đầy đủ. | Tuần 4–5 |

### 3.2. Block do cần quyết định contract (chưa ai làm tiếp được)

| Yêu cầu bị block | Lý do | Dự kiến |
|:---|:---|:---|
| **NFR-REL-002 / FR-OBS-001** — lỗi storage trả 500 chung chung (B1) | Muốn thêm mã `storage.unavailable` và đổi 500 → 503 ⇒ **đổi contract lỗi API**, FE phải biết mã mới. Cần nhóm thống nhất (TV3 là consumer). | Chờ nhóm chốt |
| **NFR-REL-002** — fail-fast khi thiếu cấu hình (B2) | Validate lúc startup có nghĩa là thành viên không bật được storage thì cũng không chạy được API ⇒ mất khả năng làm việc của nhóm khác. Cần nhóm chọn: fail-fast toàn cục / cảnh báo / chỉ dev. | Chờ nhóm chốt |
| **FR-OBS-001** — `/health` báo `Healthy` khi credential sai (B4) | Health chỉ TCP-probe storage nên báo sai hướng nguy hiểm; kết quả này dùng để quyết định "app sẵn sàng". Cần chốt ngữ nghĩa health check. | Chờ nhóm chốt |
| **FR-RCP-008 / NFR-USE-004** — xem ảnh Draft trong wizard (B5) | Proxy D27 yêu cầu auth cho ảnh Draft/Archived nhưng thẻ `<img>` không gửi Bearer ⇒ 403. Cần chọn: presigned URL / token trong query / cookie / chấp nhận không xem được. Đụng `IMAGE_CONTRACT` D27, cần nhóm + TV3 (chủ sở hữu UI wizard). | Chờ nhóm + TV3 |
| **FR-JOB-003 / NFR-SEO-003** — sitemap job 02:00 UTC + distributed lock (CR-7) | Đề yêu cầu cron `0 2 * * * UTC` và distributed lock khi nhiều worker; hiện chỉ sinh on-demand. Cần chốt: dùng Hangfire recurring (đã có sẵn storage + 4 worker) hay ghi rõ giới hạn trong sổ. | Chờ nhóm chốt |
| **Không có user Admin seed** (B6) | Không ai mở được `/hangfire`, không ai test được CRUD category. Tạo user trong seeder = dữ liệu/mật khẩu seed ⇒ cần chốt tài khoản và cách phân phối mật khẩu. Cần nhóm + TV1. | Chờ nhóm + TV1 |
| **NFR-SEC-004** — path traversal, XSS/CSP | Phần CSP/sanitization do TV1 phụ trách. | Tuần 4 |

---

## IV. Bug / Error

> Quy ước: chỉ ghi lỗi **tìm ra trong tuần 3**. Trạng thái: ✅ đã sửa + có test hoặc bằng chứng chạy lại · ⏳ đã ghi nhận, chờ nhóm chốt.

| # | Mã | Triệu chứng | Nguyên nhân gốc | Cách sửa | Bằng chứng | Trạng thái |
|---:|:---|:---|:---|:---|:---|:---:|
| 1 | — | **API trả 403 cho mọi request Author** khi gọi endpoint thật (role trong token là đúng), dù các test trước đó vẫn xanh | Đặt `MapInboundClaims=false` nhưng **thiếu** khai báo `TokenValidationParameters.RoleClaimType` / `NameClaimType` ⇒ claim `role` không khớp ⇒ policy luôn fail | Thêm `RoleClaimType = "role"`, `NameClaimType = "sub"` trong `Program.cs` | Phát hiện bởi E2E `MinioE2ETests`; đủ 3 endpoint upload/PATCH/DELETE chạy được thật | ✅ |
| 2 | — | **Ảnh đọc ra bị cắt cụt / hỏng** (~50% test resize fail) và **crash test host** bằng `ArgumentOutOfRangeException` trong `HttpConnection.CopyFromBufferAsync` | MinIO SDK `WithCallbackStream` nhận delegate `Action<Stream>`; truyền `async` lambda tạo **async void fire-and-forget** ⇒ `GetObjectAsync` có thể trả về khi copy chưa xong, lỗi nền không ai quan sát | Copy **đồng bộ** trong `MinioStorageService.ReadAsync` + kiểm tra `buffer.Length == stat.Size` (thiếu → `IOException`) | `ImageResizeD2Tests` 4/4 ổn định; `ImageProxyD27Tests` 6/6; suite 148/148 × 2 vòng | ✅ |
| 3 | — | Thêm ảnh / nguyên liệu / bước đầu tiên trả **422 version conflict giả** | EF coi entity con mới có Guid key khác rỗng là đã tồn tại ⇒ đánh dấu `Modified` ⇒ đụng RowVersion | Interceptor chuyển trạng thái `Added` khi `RowVersion` gốc rỗng (fix `e3e8315`) | E2E upload thật + unpublish/archive có đủ ingredient/step trước khi publish | ✅ |
| 4 | — | **CI đỏ 5 run liên tiếp**, job chết ở bước "Initialize containers", **mọi bước build/format/test đều bị skip**; đồng thời `docker-compose.dev.yml` không dựng được trên máy mới của các thành viên khác | Image `quay.io/minio/minio:latest` **không còn tồn tại** (quay.io → HTTP 401, Docker Hub `minio/minio` → HTTP 404); MinIO đã gỡ toàn bộ image public. **Không phải lỗi code** | Thay bằng `rustfs/rustfs` (S3-compatible, Apache-2.0) cho cả workflow lẫn dev compose, ghim **tag + digest**; bỏ `minio-init` vì app tự tạo bucket; `MinioStorageService` không đổi dòng nào. Harden thêm: SDK `10.0.401` khớp `global.json`, `timeout-minutes: 30`, `concurrency` + `cancel-in-progress`, `--blame-hang-timeout 10m` | CI đỏ `36338124928` → xanh `36343309464` → xanh `36344662570` (10/10 bước success); local **148/148 + 5/5 `Skipped=0`** trên cả hai đường, bucket/object có thật trong storage | ✅ |
| 5 | — | `POST /api/v1/recipes/{id}/images` trả **500 `server.error`** ngay cả khi đăng nhập và tạo recipe thành công | `appsettings.Development.json` **thiếu section `Minio`** ⇒ `MinioOptions` giữ `AccessKey`/`SecretKey` rỗng ⇒ storage trả `401 UnauthorizedAccess` ⇒ `MinioException` không khớp nhánh nào trong `ApiExceptionHandler` ⇒ rơi vào nhánh generic 500 | Thêm section `Minio` vào `appsettings.Development.json` (**dev-only**, lấy đúng credential đã có sẵn trong `docker-compose.dev.yml`, **không** đụng `appsettings.json` đọc ở production) | 41 test case FE+BE nhóm D (ảnh) **7/7 PASS**; trước đây `D01 = 500` | ✅ |
| 6 | — | `28P01 password authentication failed` ở **mọi** endpoint cần DB khi chạy `dotnet run` từ repo mới; **test vẫn xanh** vì mỗi tầng tự nạp config riêng | `appsettings.Development.json` khai `Password=postgres` còn `docker-compose.dev.yml` dùng `admin123` ⇒ cấu hình dev trong repo và cấu hình app thật không khớp | Đưa default trong repo về `postgres`; giá trị thật mỗi máy nằm ở `.env` (**gitignored**, chặn cả `.dockerignore`); thêm `EnvFileLoader.cs` nạp `.env` trước `CreateBuilder` với 2 chốt chặn (bỏ qua `Production`, không ghi đè biến đã có) | 3 test `DevConfigParityTests` mới — **negative control**: gỡ section `Minio` → 2 FAIL; đổi `POSTGRES_PASSWORD` trong compose → 1 FAIL; trả lại → 3/3 PASS | ✅ |
| 7 | — | Không sửa được `dashboard/recipes/**` vì file không có trong nhánh; khi merge phát sinh **7 conflict** vì nhánh dùng `rustfs/rustfs` còn `main` dùng `coollabsio/minio` (TV3 đổi ở `84dddd4`) | PR #15 (wizard ảnh của TV3) và commit đổi image dev của TV3 merge vào `main` **trong lúc** nhánh TV4 đang làm N4/N5 ⇒ nhánh lệch `main` 23 commit | Rà `origin/main` **trước khi** làm việc FE/hạ tầng; chốt CR-6 dùng `rustfs/rustfs` cho cả dev + CI; gỡ 7 conflict giữ đúng cả hai phía (giữ `ArchiveRecipeCommand` của TV4 + `DeleteRecipeCommand(id, rowVersion)` của TV3; giữ `If-Match`/RowVersion 422 + `NameClaimType`) | Build 0 warning · test `Skipped=0` · `tsc --noEmit` 0 · `next build` 0 (16/16 trang) · `docker compose config --quiet` 0 | ✅ |
| 8 | — | Build lab thất bại khi dùng `Hangfire.PostgreSql` 1.21 (dự án bật `TreatWarningsAsErrors`) | Constructor truyền `connectionString` đã obsolete | Chuyển sang `NpgsqlConnectionFactory` + `JobStorage.Current` | Lab build Release 0 warning, format sạch | ✅ |
| 9 | — | Job resize/enqueue trong lab **kẹt `Enqueued` mãi, không bao giờ chạy** | Queue **mismatch**: `BackgroundJob.Enqueue` mặc định đẩy vào queue `default` còn worker chỉ nghe queue `lab`; `RecurringJobOptions` **không có** thuộc tính `Queue` | Truyền queue tường minh khi enqueue; recurring để scheduler điều khiển | Phase `jobs` 8/8 — fire-and-forget `Succeeded`, recurring chạy 2 lần | ✅ |
| 10 | — | Đếm được email gửi qua Mailhog nhưng **không đối chiếu được subject** | Mailhog v2 đặt subject ở `items[].Content.Headers.Subject`, không phải `items[].Subject` | Sửa đường dẫn đọc; có bằng chứng MailKit MIME-encode tiếng Việt đúng | Phase `email` 3/3 | ✅ |
| 11 | — | Dọn dẹp object trong lab thất bại | `IObjectStorageWriter` chỉ có `Exists`/`Upload`, không có `Delete` | Xóa object phải đi qua `IFileStorageService` | Phase `media` dọn dẹp sạch | ✅ |
| 12 | — | Nội dung tiếng Việt trong file bị **hỏng ký tự** khi ghi bằng PowerShell | PowerShell 5.1 `Get-Content`/`Set-Content` dùng encoding mặc định không phải UTF-8 | Ghi file UTF-8 tường minh | Log `Tuan03/logs/lab_l4_run.log` đọc lại đúng tiếng Việt | ✅ |
| 13 | — | `SixLabors.ImageSharp` 4.x **bắt buộc license key thương mại** ⇒ build fail | Bản 4.x đã chuyển sang license thương mại | Khoanh phiên bản **3.1.11**; AVIF không decode ⇒ job đã fallback original | Build 0 warning; E2E resize 4/4 | ✅ |
| 14 | — | Ảnh **Draft** trong wizard không xem được (thẻ `<img>` trả **403** từ proxy) | Proxy D27 yêu cầu auth cho ảnh Draft/Archived, nhưng thẻ `<img>` không gửi header `Authorization` | **Chưa sửu** — cần chọn cơ chế (presigned URL / token trong query / cookie) | `Tuan03/Report/DE_XUAT_05_XEM_ANH_DRAFT_TRONG_WIZARD.md` | ⏳ Chờ nhóm + TV3 |
| 15 | — | Lỗi object storage trả **500 `server.error` chung chung**, FE không phân biệt được lỗi hạ tầng với lỗi ứng dụng | `ApiExceptionHandler` chưa có nhánh riêng cho lỗi storage ⇒ rơi vào generic 500 | Đề xuất: thêm mã `storage.unavailable`, đổi 500 → 503 | `Tuan03/Report/DE_XUAT_01_LOI_STORAGE_TRA_503_CO_MA_LOI.md` | ⏳ Chờ nhóm chốt contract lỗi |
| 16 | — | API **khởi động "thành công"** dù thiếu credential storage; lỗi chỉ lộ ra giữa lúc người dùng đang thao tác | Không có validate lúc khởi động | Cân nhắc fail-fast hoặc cảnh báo rõ khi khởi động | `Tuan03/Report/DE_XUAT_02_FAIL_FAST_KHI_THIEU_CAU_HINH.md` | ⏳ Chờ nhóm chốt |
| 17 | — | `/health` báo storage `Healthy` **dù credential sai** (chỉ TCP probe) | Health check chỉ kiểm tra khả năng kết nối, không xác thực | Cân nhắc probe có xác thực (stat object / list bucket) | `Tuan03/Report/DE_XUAT_04_HEALTH_CHECK_STORAGE_XAC_THUC.md` | ⏳ Chờ nhóm chốt |
| 18 | — | Không có user **Admin** trong dữ liệu seed ⇒ không ai mở được `/hangfire`, không ai test được CRUD category | Seeder chưa tạo tài khoản Admin | Đề xuất tạo user Admin trong seeder, kèm quy ước phân phối mật khẩu | `Tuan03/Report/DE_XUAT_06_TAI_TAO_USER_ADMIN_DE_SEED.md` | ⏳ Chờ nhóm + TV1 |

### Tài liệu báo cáo lỗi chi tiết

| File | Nội dung |
|:---|:---|
| `Tuan03/Report/BAO_CAO_LOI_UPLOAD_ANH_500.md` | Báo cáo gốc về lỗi 500 upload ảnh (chẩn đoán nguyên nhân thiếu credential storage) |
| `Tuan03/Report/BAO_CAO_LOI_UPLOAD_ANH_500_DA_SUA.md` | Bản sửa + nguyên nhân thứ hai cùng lớp (mật khẩu DB lệch) + 3 test parity + 41 test case + hồi quy 157/157 |
| `Tuan03/Report/TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md` | Tổng hợp 6 hạng mục còn lại (B1–B6) và thứ tự gỡ |
| `Tuan03/Report/TEST_CASE_TICH_HOP_FE_BE.md` | Bộ 41 test case tích hợp FE+BE dùng để tái hiện và soát |
