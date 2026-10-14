# BÁO CÁO TIẾN ĐỘ TUẦN 5 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **Kỳ báo cáo**: Tuần 5 (07/10/2026 – 11/10/2026)
> **Báo cáo liền kề**: [`BAO_CAO_TIEN_DO_TUAN_4_TV4_SRS.md`](BAO_CAO_TIEN_DO_TUAN_4_TV4_SRS.md) (tuần 4) · [`BAO_CAO_TIEN_DO_TUAN_3_TV4_SRS.md`](BAO_CAO_TIEN_DO_TUAN_3_TV4_SRS.md) (tuần 3) · [`BAO_CAO_TIEN_DO_2_TUAN_TV4_SRS.md`](BAO_CAO_TIEN_DO_2_TUAN_TV4_SRS.md) (tuần 1–2)
> **Tài liệu yêu cầu**: SRS Culinary Blog v1.1.1 (Approved 16/09/2026)
> **Phạm vi phụ trách**: Xuất bản công thức, quản lý ảnh, SEO, vận hành hệ thống và đăng xuất
> **Reviewer**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Nhánh làm việc**: `2312739_NHTSon_D5-D7` (gốc `origin/main` = `262201b`) — [PR #32](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/pull/32) (`week-5` → `main`)
> **Nhánh lab**: `practice/TV4/L4` — [PR #33](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/pull/33) (mở 07/10, sau 2 tuần treo) · `practice/TV4/L5`
> **Cổng nghiệm thu trong kỳ**: **G6** cuối tuần 5 — staging hoàn chỉnh (deploy lặp lại được, 2 API, TLS/HSTS, backup/restore RTO < 2 phút / RPO = 0), **5/5 luồng E2E**, có số đo performance/SEO/a11y
> **Cách làm báo cáo**: tuần 5 chia 10 khối việc W5-1 → W5-10 (baseline + PR, deploy staging, 2 API, HTTPS/CORS, runbook, D4-UI, 5 luồng E2E, số đo load/SEO, lab còn thiếu, đóng `BUG-W4-01/02/03`). Báo cáo SRS này gộp kết quả cả 10 khối, dẫn chiếu sổ evidence [`../Tuan05/SO_EVIDENCE_TUAN_5.md`](../Tuan05/SO_EVIDENCE_TUAN_5.md) và bàn giao [`../Tuan05/HANDOFF_TV4_TUAN5.md`](../Tuan05/HANDOFF_TV4_TUAN5.md).

> [!IMPORTANT]
> **Phạm vi file này**: ghi **phần đã làm thêm trong tuần 5 so với tuần 4**. Việc đã xong tuần 4 (N1/N2 phần còn/N3-N-C, trace Seq, drill backup/restore 14 bảng trên dev, `NFR-REL-002`, đóng 5 issues B1–B6, E2E 26/26, coverage 96.31%) **không lặp lại** — xem [`BAO_CAO_TIEN_DO_TUAN_4_TV4_SRS.md`](BAO_CAO_TIEN_DO_TUAN_4_TV4_SRS.md).
> **Quy tắc giữ nguyên**: **không copy số tuần 4 làm số tuần 5**; số nào của tuần 5 đều đo lại trên `262201b`/`0a9b1a5` hoặc trên staging.

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

> Quy ước: **Hoàn thành** = có code + test/bằng chứng chạy thật trong kỳ. Cột "Hoàn thành trong tuần 5" ghi **phần làm thêm trong tuần 5**; phần đã có tuần 1–4 không lặp lại (xem 3 báo cáo liền kề).
> Cột **Tuần 4 → Tuần 5** ghi % tự đánh giá để thấy độ lệch; **không** ô K nào tự chuyển ✅ khi chưa được reviewer Tâm xác nhận.

| STT | Mã | Tuần 4 → 5 | Hoàn thành trong tuần 5 | Chưa hoàn thành | Nhánh | Tiến độ |
|---:|:---|:---:|:---|:---|:---|:---:|
| 1 | **FR-RCP-005** | 90 → **100%** | • **Nút "Gỡ đăng" trên dashboard** (`N2-D2`): `my-recipes.ts` gọi `PATCH /recipes/{id}/unpublish`; `page.tsx` (MyRecipes) thêm nút theo trạng thái + xác nhận 2 bước + state `actingId` (commit `d65cc17`).<br>• E2E thật chống staging `recipe-unpublish.spec.ts` **3/3** (Published→Bản nháp) — commit `367b42e`. | — | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **100%** |
| 2 | **FR-RCP-006** | 90 → **100%** | • **Nút "Lưu trữ" trên dashboard**: `PATCH /recipes/{id}/archive`; E2E `recipe-unpublish.spec.ts` (Draft→Lưu trữ, nút Lưu trữ biến mất) **3/3** — commit `367b42e`. | — | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **100%** |
| 3 | **FR-RCP-007** | 90 → **100%** | • **Nút "Xóa" trên dashboard** (soft delete): E2E Xóa→dòng biến mất khỏi danh sách **3/3**.<br>• **ADR-TV4-003** chốt quy ước: Recipe **soft delete** (interceptor + query filter) / Category **hard delete** (`CategoryRepository.cs:51`); `MarkDeleted` = dead code, KHÔNG xóa — commit `0084bb9`. | — | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **100%** |
| 4 | **FR-RCP-008** | 95 → **95%** | • **Sửa gốc lỗi ảnh nháp/vừa tải**: `imageSrc` bỏ presigned → luôn proxy `/api/v1/resources/images` + `AuthImage` fetch Bearer→blob (commit `9f3dd4c`); ảnh blob `^blob:` + JSON-LD ảnh tuyệt đối 200 khi chạy 40/40 E2E. | • Chưa chạy tay nhánh resize với AVIF thật (đã có test `ImageResizeD2Tests`). | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **95%** |
| 5 | **FR-FILE-001** | 95 → **100%** | • **Test path traversal** `PathTraversalSecurityTests` **7/7**: server đặt key `recipes/{id}/image{ext}` (FileName client **không** vào path); proxy 404 mọi `../`, không phục vụ ngoài prefix — commit `2d40fb0`. | — | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **100%** |
| 6 | **FR-FILE-002** | 85 → **85%** | • Không thay đổi — retry 3 + log + hợp đồng đã đủ từ tuần 4. | • Chưa có test riêng gọi thẳng `DeleteAsync` xác nhận "không xóa object ngoài bucket/prefix" (mới bảo vệ gián tiếp). | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **85%** |
| 7 | **FR-JOB-002** | 95 → **95%** | • Không thay đổi — không có việc W5 chạm `ImageResizeJob`. | • Lab L5 phase `image-opt` vẫn ⬜ (cắt tuần 6 — `next/image` 0 file ghi nhận từ tuần 4). | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **95%** |
| 8 | **FR-JOB-003** | 80 → **85%** | • **Sitemap production**: FE sitemap **96 URL** (92 Published + 4 tĩnh), backend `/sitemap.xml` 96 URL Published-only, robots 200 (`Sitemap:` khai báo), nginx 301 `http→https` (W5-8).<br>• Header `X-Sitemap-Generated` = `true` (cache nguội) / `false` (cache ấm) — verify `:5080` (W5-10, `BUG-W4-09`). | • Sitemap **chưa liệt kê URL từng danh mục** (mới `/categories` tĩnh) — vẫn như tuần 4. | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **85%** |
| 9 | **FR-OBS-001** | 95 → **100%** | • **Sửa `/health` 503 oan** (companion): thêm `HealthChecks:Minio:Host` vào `x-api-env` → `/health` **200** cả direct lẫn qua nginx (W5-4).<br>• **Failover 2 API instance trên staging** (FR-OBS-001): stop api-2 → 0/8 non-200, start lại → 0/8 non-200 (W5-3). | — | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **100%** |
| 10 | **FR-OBS-003** | 90 → **100%** | • **`/metrics` scrape thật**: `200 text/plain; version=0.0.4` — `http_requests_total`, `http_request_errors_total`, `http_request_duration_ms_sum`, `http_requests_by_class`; tự viết `RequestMetrics` in-memory (W5-8).<br>• Trace HTTP→EFCore→Postgres trên staging: 3 span cùng `CorrelationId` (W5-2). | — | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **100%** |
| 11 | **FR-AUTH-005** | 95 → **95%** | • Không thay đổi — revoke refresh family đã hoàn thành tuần 3; xác minh không hồi quy qua bộ test thật (430/430). | — | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **95%** |
| 12 | **NFR-SEC-004** | 90 → **100%** | • **Bổ sung biên** từ tuần 4 đã đủ; tuần 5 **đóng nốt** phần path traversal bằng `PathTraversalSecurityTests` **7/7** (`recipes/{id}/image{ext}`, proxy 404 `..`) — commit `2d40fb0`. | — | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **100%** |
| 13 | **NFR-SEC-005** | 20 → **95%** | • **TLS staging tự ký** (`service culinary-certs` sinh `CN=localhost`, không commit key) + `listen 443 ssl` + `TLSv1.2/1.3`.<br>• `return 301 https://...` trên port 80; header `Strict-Transport-Security: max-age=31536000; includeSubDomains`.<br>• CORS explicit origins thay `AllowAnyOrigin`; `UseForwardedHeaders` (`ForwardLimit=1`, `KnownIPNetworks 172.16/12`) trước rate limiter → mỗi IP một bucket (`BUG-W4-03`, commit `afdb4a7`). | • TLS **tự ký** (chưa chứng thư CA thật) — ghi rõ trong runbook mục Giới hạn. | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **95%** |
| 14 | **NFR-REL-001** | 60 → **70%** | • **2 API instance + failover trên staging**: nginx upstream (`zone 128k` + `resolve`), 12 request → api1=6 / api2=6 (50/50), stop api-2 → gánh được (W5-3).<br>• Runbook A3 ghi failover từng dependency (Redis 0.2s, S3 ~0.5s, API node = SPOF). | • Vẫn **1 node là SPOF** — không tuyên bố đạt 99,5%; chưa đo uptime dài hạn. | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **70%** |
| 15 | **NFR-REL-002** | 85 → **90%** | • Volumes persistent `staging_pgdata/s3data/redisdata`: register 201 → restart DB → login 200; Redis appendonly SET → restart → GET còn (W5-4).<br>• **Restart-qua-restart bền** kiểm trên staging (không chỉ dev). | • Chưa có test cắt dependency thật cho **DB reconnect** (drill DB chỉ mô phỏng port chết). | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **90%** |
| 16 | **NFR-REL-003** | 45 → **60%** | • **Drill backup→restore trên staging** (client PG16 trong container): `backup.sh` exit 0 → `DROP DATABASE ... (FORCE)` → `restore.sh` exit 0 → **14 bảng = N1** → ready 200 (W5-2).<br>• Volume DB/S3/Redis bền qua restart staging (W5-4). | • **CHƯA ĐẠT giữ 30 ngày** — `BACKUP_KEEP_DAYS=30` chỉ dọn file runner, artifact GitHub giữ 7 ngày → `DE_XUAT_08`.<br>• Chưa chốt nơi đặt lịch backup → `DE_XUAT_07`. | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **60%** |
| 17 | **NFR-USE-004** | 20 → **80%** | • **Thanh tiến trình upload %** (`N2-D1`): `uploadImage` chuyển sang **XMLHttpRequest + `onProgress`**; `ImagesStep` có `role="progressbar"` + `aria-valuenow` theo % thật (commit `d65cc17`, test `ImagesStepProgress.test.tsx` pass).<br>• Nút trạng thái + Xóa có xác nhận 2 bước + `actingId` (feedback người dùng). | • Chưa có checklist WCAG/keyboard cho upload + status button (K18, **thuộc TV2**). | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **80%** |
| 18 | **NFR-SCALE-001** | 70 → **95%** | • **2 API instance trên staging**: profile `multi` + service `culinary-api-2` (port 5081); nginx upstream 2 server (`zone 128k` + `resolve` + `resolver 127.0.0.11`) — 50/50.<br>• **Hangfire 2 server** cùng storage PostgreSQL; cache/queue Redis chung (`staging:cache:*`, `staging:sitemap:xml` redistribution lock) — W5-3. | • Redis cache cross-instance vẫn đo bằng lab + quan sát staging, chưa có test tự động riêng. | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **95%** |
| 19 | **NFR-SCALE-003** | 70 → **90%** | • `docker-compose.staging.yml` (6 container + profile `multi` + service `culinary-certs`), `nginx/nginx.staging.conf` upstream 2 backend + TLS/redirect; volumes `staging_pgdata/s3data/redisdata/certs` (W5-2/3/4). | • Hồ sơ scale 4+ nodes/CDN mới ở mức thiết kế — chưa nghiệm thu triển khai.<br>• Chưa có `docker-compose.prod.yml`. | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **90%** |
| 20 | **NFR-SEO-003** | 85 → **95%** | • **SEO staging đầy đủ**: FE sitemap 96 URL, robots 200, recipe detail JSON-LD (`@type:Recipe`, ingredients/instructions), canonical, 5 OG, `twitter:card`; backend Published-only 96 URL; nginx 301 `http→https` (W5-8). | • **Chưa liệt kê URL từng danh mục**.<br>• `NFR-SEO-004` (301 khi đổi slug) chưa có → **nợ TV3** (không phải việc W5). | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **95%** |
| 21 | **NFR-MAINT-003** | 70 → **90%** | • **`docs/RUNBOOK.md` đầy đủ 7 mục** (A1–A7) mỗi mục có số liệu/log thật + mục **Giới hạn** — commit `92e34be` (`N4-A`).<br>• ADR-TV4-003 (soft-delete) + **ADR-TV4-004** (ImageSharp 3.1.12 + NuGetAuditSuppress).<br>• `deploy/scan-secrets.sh` mở rộng quét appsettings + `env:` workflow, test **9 PASS/0 FAIL**; `npm audit` critical → 0. | • Chưa đo thời gian khởi động từ checkout sạch để chứng minh "<5 phút".<br>• `npm audit` còn 44 vuln cần nâng **major** (next@16, tailwindcss 4.3.3, jest 30.x) — chờ nhóm chốt. | [2312739_NHTSon_D5-D7](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/2312739_NHTSon_D5-D7) | **90%** |

---

## II. Tổng kết

### 2.1. FR đã làm trong tuần 5

| Kết quả | Chi tiết |
|:---|:---|
| **Đóng trọn bộ nợ UI (D4-UI)** | **3/11 FR đạt 100% trong tuần**: FR-RCP-005 (**90→100%**, nút Gỡ đăng + E2E 3/3), FR-RCP-006 (**90→100%**, nút Lưu trữ), FR-RCP-007 (**90→100%**, nút Xóa + ADR-TV4-003) — toàn bộ đều từ UI dashboard còn thiếu ở tuần 4 |
| **Tăng mạnh** | FR-FILE-001 (**95→100%**, test path traversal 7/7) · FR-OBS-001 (**95→100%**, sửa `/health` 503 + failover 2 API) · FR-OBS-003 (**90→100%**, `/metrics` scrape thật) · FR-JOB-003 (**80→85%**, sitemap staging 96 URL + header) |
| **Đứng yên** | **3/11 FR**: FR-FILE-002 (85% — chờ test `DeleteAsync` riêng), FR-JOB-002 (95% — không việc W5), FR-AUTH-005 (95% — đã xong từ tuần 3) |
| **Số FR đạt ≥ 90%** | **9/11 FR** (8 đạt 95–100%; 2 FR còn lại đều **85%**: FR-FILE-002, FR-JOB-003) |

### 2.2. NFR đã làm trong tuần 5

| Kết quả | Chi tiết |
|:---|:---|
| **Tiến bộ rõ (≥25 điểm %)** | **3/10 NFR**: NFR-SEC-005 (**20→95%**, TLS/HSTS/redirect/CORS + forwarded headers), NFR-USE-004 (**20→80%**, thanh tiến trình upload %), NFR-SCALE-001 (**70→95%**, 2 API + Hangfire 2 server) |
| **Tăng một phần** | **7/10 NFR**: NFR-SEC-004 (90→100%), NFR-REL-001 (60→70%), NFR-REL-002 (85→90%), NFR-REL-003 (45→60%), NFR-SCALE-003 (70→90%), NFR-MAINT-003 (70→90%), NFR-SEO-003 (85→95%) |
| **Đứng yên** | **0/10 NFR** — mọi NFR đều có bước tiến hoặc bằng chứng mới trong tuần 5 |

### 2.3. Tiến độ tổng thể (tự đánh giá)

| Chỉ số | Tuần 1–2 | Tuần 3 | Tuần 4 | **Tuần 5** | So với tuần 4 |
|:---|:---:|:---:|:---:|:---:|:---|
| FR trung bình | ~30% | ~75% | ~91% | **~96%** | ▲ |
| NFR trung bình | ~25% | ~48% | ~61% | **~87%** | ▲ |
| Test backend (`CulinaryBlog.Tests` + spike) | 120/120 | 157/157 | **426/426** (421 + 5 spike), `Skipped=0` | **430/430** `CulinaryBlog.Tests` + **5/5** `ConcurrencySpike`, Failed 0, `Skipped=0` | ▲ +4 (`RecipeChildIdGenerationTests` 3 + nhóm bảo mật) |
| Test mới TV4 thêm trong kỳ | — | +37 | Nhóm hồi quy lỗi tuần 4 | `RecipeChildIdGenerationTests` (3) · `PathTraversalSecurityTests` (7) · `ImagesStepProgress.test.tsx` · E2E `recipe-unpublish.spec.ts` (3/3) · `scan-secrets.test.sh` 9 PASS | ▲ |
| Coverage `CulinaryBlog.Application` | — | — | **96.31%** (05/10) | **97.07%** (`check-coverage.sh`, ngưỡng 80%, gate CI) | ▲ |
| Jest (frontend unit) | chưa có | chưa có | 85/85 (TV3) | **86/86** (+1 `ImagesStepProgress`) | ▲ (1 test TV4) |
| Playwright E2E | chưa có | 41/41 test tích hợp FE–BE | **26/26** ×3 (2/5 luồng) | **40/40 ×2** chống **staging** (4/5 luồng: publish 4/4 · wizard-week4 10/10 · create-recipe · search 12 · upload-security 10 · recipe-unpublish 3) | ▲ |
| Lab cá nhân | chưa có | 4 phase · 39/39 (L4) | **L5: 63 check · 41 đạt · 3/7 phase PASS** | **+2/5 việc W5-9** (`isr-detail` sửa code → ISR thật 104 trang; `npm audit` critical→0) · ⬜ `image-opt`/`query-rollback`/`cqrs-behavior` cắt tuần 6 | ▲ một phần |
| Số liệu hiệu năng | chưa có | EXPLAIN 0.339 ms; k6 smoke p95 225.63 ms | k6 dev ~120 req/s, p95 14.14 ms + outage drill | **k6 staging 102 VU** · p50 82.56 · p95 **148.56** · p99 187.28 ms · **0% lỗi** (~1150 req/s, threshold PASS); EXPLAIN list 0.32 / slug **Index Scan 0.032 ms**; cache-hit mixed 70.31% / cached-only 100% | 🆕 (số staging) |
| CI GitHub | 🟡 đỏ | 🟢 xanh | 🟡→🟢 `8a585ef` xanh | 🟢 xanh (run `0a9b1a5`; PR #32/#33 chờ review) | ▲ |
| Ô kỹ năng K | 8/24 (cuối tuần 2) | 16/24 | **9/24 đã duyệt** + bằng chứng mới | Bổ sung bằng chứng cho **K04, K05, K06, K10, K11, K12, K13, K17, K19, K20, K21, K22, K23, K24** — **vẫn 0/24 được Tâm xác nhận** | ▲ (chưa được duyệt) |

### 2.4. Cổng nghiệm thu trong kỳ

| Cổng | Tiêu chí | Kết quả |
|:---|:---|:---|
| **G6** (cuối tuần 5) | Staging hoàn chỉnh: deploy lặp lại được · 2 API · TLS/HSTS · backup/restore RTO < 2 phút / RPO = 0 · **5/5 luồng E2E** · số đo performance/SEO/a11y | 🟡 **Đạt về kỹ thuật, chờ nghiệm thu**: staging deploy **2 lần** (run1 + run2 volume sạch) + **2 API** + TLS/HSTS self-signed + **drill restore staging 14 bảng** (RPO = 0) · k6 102 VU 0% lỗi · SEO staging 96 URL · ⛔ **còn `category` là luồng E2E thứ 5 chờ TV2** (mới **4/5 luồng**) · ⛔ a11y/WCAG do TV2 · ⛔ Tâm chưa nghiệm thu PR #32/#33 và 24 ô K |

### 2.5. Nhận xét

- Tuần 5 **đóng toàn bộ nợ UI/hạ tầng còn lại của tuần 3–4** (D4-UI, `N4-A`, `N4-B`) và **chuyển TV4 từ "có API + số đo trên dev" sang "staging chạy được 2 lần + 2 API + TLS + E2E 40/40 + số đo có giới hạn"** — sát cổng G6.
- **Bằng chứng đo trên staging** thay vì dev: deploy idempotent (run1/run2), 12 request round-robin 50/50, failover 8/8, TLS 301→200, drill restore 14 bảng, k6 p95/p99, cache-hit, `/metrics`, trace 3 span cùng `CorrelationId` — log lưu tại [`../Tuan05/logs/`](../Tuan05/logs/).
- **Trung thực về giới hạn**: TLS **tự ký**; k6/EXPLAIN/cache-hit đo trên **staging cục bộ** (không phải SLA); planner seq scan vì `Recipes` chỉ 167 dòng; cache-hit 70.31% là số **trộn** có scenario `list_uncached` cố ý; `NFR-REL-001` vẫn **không** tuyên bố 99,5% (1 node SPOF).
- **Đóng trọn bộ `BUG-W4-01/02/03`** bằng commit + test hồi quy (không chỉ kiểm tra cục bộ): `BUG-W4-01` đối chiếu `main` + 3 test model-level; `BUG-W4-02` dọn secret + mở rộng `scan-secrets.sh`; `BUG-W4-03` `UseForwardedHeaders` (PR riêng).
- **Đóng 2 nợ P1 cũ nhất**: mở **PR #33** (`practice/TV4/L4`, treo 2 tuần) và secret scan; mở **PR #32** cho nhánh tuần 5.
- Phần còn lại của kỳ đều **blocked hoặc nằm trong cut-list có sẵn**: luồng `category` (TV2), Google OAuth (nhóm), 3 phase Lab L5 (cắt tuần 6), nâng major `npm audit` (chờ chốt) — TV4 không tự quyết.

---

## III. Block

### 3.1. Block do phụ thuộc người khác hoặc quyết định nhóm

| Yêu cầu bị block | Lý do | Dự kiến |
|:---|:---|:---|
| **W5-7 — luồng E2E `category`** (G6 cần 5/5 luồng) | Chưa có spec — sổ evidence §3.7 ghi "cần **TV2** xác nhận ai viết"; 4 luồng còn lại đã xanh (40/40) | Chờ TV2 |
| **K11 EXPLAIN FTS** (`to_tsquery`) | Phụ thuộc TV2 sửa `RecipeRepository` sang `to_tsquery` (vi phạm ADR 0003) — W5-8 mới đo được phần phi-FTS | Chờ TV2 |
| **`query-rollback` (RowVersion)** — phase thứ 4 Lab L5 | Cần account E2E **sở hữu** công thức hoặc `GetRecipesQuery` filter chủ sở hữu (TV3/Tâm) — thiếu nên phase không chạy | Chờ TV3/Tâm (tuần 6) |
| **`NFR-SEO-004`** (301 khi đổi slug) | Chưa có trong dự án → **nợ TV3**, không thuộc TV4 | Chờ TV3 |
| **K18 — checklist WCAG/a11y** (G6 có mục a11y) | Thuộc **TV2** theo phân chia; TV4 chỉ ghi dẫn chiếu | Chờ TV2 |
| **Google OAuth credentials** (`N3-A5`, K09) | **Thiếu credentials** — không tính mock là hoàn thành | Chờ nhóm |
| **Review `afdb4a7` + header `/sitemap.xml` + `/metrics`** (`Program.cs` của TV1) | TV4 sửa trực tiếp nhưng đụng file của TV1 → đề nghị gộp **một PR riêng** để Tâm review | Chờ Tâm |
| **Chốt `BUG-W4-01` C1/C2** (đụng schema TV3 `RecipeImageId`) | TV4 chỉ **đối chiếu `main`** + thêm test hồi quy | Chờ Tâm + TV3 |
| **Nghiệm thu PR #32 + #33** (điều kiện DoD "có PR được review") | Mở 07/10, cả 2 đang chờ review | Chờ Tâm |
| **24 ô K** | Tuần 5 bổ sung nhiều bằng chứng mới nhưng **vẫn 0/24 được Tâm xác nhận** | Chờ Tâm |

### 3.2. Block do cần quyết định / hợp đồng

| Yêu cầu bị block | Lý do | Dự kiến |
|:---|:---|:---|
| **NFR-REL-003 — nơi đặt lịch backup 03:00 ICT** | GH Actions có thể trễ/bỏ qua job khi repo lâu không commit | [`DE_XUAT_07`](../../../proposal/DE_XUAT_07_NOI_DAT_LICH_BACKUP.md) — chờ Tâm |
| **NFR-REL-003 — kho lưu 30 ngày** | Artifact GitHub giữ 7 ngày; `BACKUP_KEEP_DAYS=30` chỉ dọn file runner | [`DE_XUAT_08`](../../../proposal/DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md) — chờ Tâm |
| **K10 — rotate khoá JWT đã lộ trong git history** | Nằm ngoài repo, thao tác không thể hoàn tác | [`DE_XUAT_09`](../../../proposal/DE_XUAT_09_ROTATE_KHOA_JWT_DA_LO.md) — chờ Tâm |
| **`npm audit` nâng major** (next@16, tailwindcss 4.3.3, jest 30.x) | Còn 44 vuln (critical = 0) nhưng các mục còn lại **đều cần nâng major** → có thể breaking | Đề xuất nhóm chốt — không tự nâng major trong tuần |
| **NFR-REL-001 — uptime 99,5%** | Cần vận hành dài hạn trên môi trường có failover; drill ngắn không chứng minh được | Cần staging + dài hạn |
| **Pipeline metrics OTLP/Prometheus exporter thật** | `/metrics` tự viết, giữ số **in-memory** (reset khi restart); muốn export thật cần thêm package exporter → đổi lock-file/CI locked-mode | Cân nhắc tuần 6 |

---

## IV. Bug / Error

> Quy ước: chỉ ghi lỗi **tìm ra và sửa trong tuần 5**. ✅ = đã sửa + có test/bằng chứng chạy lại. Chi tiết đầy đủ ở [`BAO_CAO_TIEN_DO_TUAN_5_SO_VOI_TUAN_4.md`](BAO_CAO_TIEN_DO_TUAN_5_SO_VOI_TUAN_4.md) §3 và [`../Tuan05/SO_EVIDENCE_TUAN_5.md`](../Tuan05/SO_EVIDENCE_TUAN_5.md).

| # | Triệu chứng | Nguyên nhân gốc | Cách sửa | Bằng chứng | Trạng thái |
|---:|:---|:---|:---|:---|:---:|
| 1 | **CI backend đỏ do NuGetAudit** (NU1902/NU1903 ImageSharp 3.1.11) | Advisory flag cả dòng `<= 4.1.1`; bản vá `first_patched = 4.1.2` là **commercial** | Pin **3.1.12** (Apache-2.0, mới nhất miễn phí) + `NuGetAuditSuppress` đúng 5 URL (ADR-TV4-004); audit vẫn bật cho package khác | Build/test/format xanh; `Directory.Build.props` | ✅ |
| 2 | **Ảnh nháp/vừa tải vỡ trên staging** | `<img>` 403 / presigned `culinary-s3:9000` không trỏ được từ trình duyệt (host nội bộ container) | `mediaUrl` fallback proxy + `imageSrc` bỏ presigned → **luôn** `/api/v1/resources/images` + `AuthImage` Bearer-blob (`9f3dd4c`) | E2E 40/40; ảnh blob `^blob:` + JSON-LD ảnh tuyệt đối 200 | ✅ |
| 3 | **6 spec E2E cũ fail** khi chạy chống staging (10/10) | Selector viết theo UI cũ (banner zod → lỗi nay dưới ô; aria-label động → placeholder) | Cập nhật selector (`#err-*`, placeholder dòng nháp), bỏ `#__next-route-announcer__`, giữ title route sau `replaceState` (`9f3dd4c`) | Full suite **40/40 ×2** | ✅ |
| 4 | **`/health` trả 503 oan** (companion) | `MinIOHealthCheck` đọc `HealthChecks:Minio:Host` default `localhost:9000` trong container | Thêm `HealthChecks:Minio:Host` vào `x-api-env` → `/health` **200** cả direct lẫn qua nginx | `docker-compose.staging.yml`; W5-4 | ✅ |
| 5 | **CORS chặn Playwright** chống staging | `Cors:AllowedOrigins` chưa có origin của browser Playwright | Thêm `http://127.0.0.1:3000` vào staging compose + recreate api → OPTIONS preflight **204 + `Allow-Origin`** (`367b42e`) | E2E `recipe-unpublish` 3/3 | ✅ |
| 6 | **CI format đỏ** | newline cuối thiếu trong test mới | `dotnet format --verify-no-changes` → thêm newline (`bf98eee`, `34f82f4`) | CI xanh | ✅ |

### Lỗi còn mở (không sửa trong tuần 5)

| Mã | Lỗi | Vì sao còn mở |
|:---|:---|:---|
| `BUG-W4-08/09/10` | Chưa kiểm tra sâu · điều kiện cạnh tranh · migration no-op | ⏳ Mới xử lý phần `X-Sitemap-Generated` (`09`); còn lại **chưa xác minh** — không tự kết luận |
| — | RowVersion/optimistic concurrency E2E | ⛔ Tài khoản E2E không sở hữu công thức (`GetRecipesQuery` không filter chủ sở hữu) → phase `query-rollback` chưa chạy |
| — | Google OAuth thật | ⛔ Thiếu credentials |
| — | Uptime thật 99,5% | ⛔ Cần vận hành dài hạn, không sửa được bằng code |
| — | `NFR-SEO-004` (301 khi đổi slug) | ⛔ Chưa có trong dự án — nợ **TV3** |
| — | Pipeline metrics OTLP/Prometheus exporter | ⛔ `/metrics` giữ số in-memory; export thật cần thêm package (đổi lock-file) |

### Tài liệu báo cáo lỗi chi tiết

| File | Nội dung |
|:---|:---|
| [`BAO_CAO_TIEN_DO_TUAN_5_SO_VOI_TUAN_4.md`](BAO_CAO_TIEN_DO_TUAN_5_SO_VOI_TUAN_4.md) | Delta tuần 5 so với tuần 4 — 6 lỗi thật + số liệu kiểm chứng + điểm K thay đổi |
| [`../Tuan05/SO_EVIDENCE_TUAN_5.md`](../Tuan05/SO_EVIDENCE_TUAN_5.md) | Sổ evidence tuần 5 — baseline, bằng chứng W5-1→W5-10, giới hạn số đo §3.8 |
| [`../Tuan05/HANDOFF_TV4_TUAN5.md`](../Tuan05/HANDOFF_TV4_TUAN5.md) | Bàn giao tuần 5 — trạng thái kiểm định, việc reviewer cần xác nhận, nợ blocked |
| [`../Tuan05/TRANG_THAI_THUC_HIEN_TUAN_5.md`](../Tuan05/TRANG_THAI_THUC_HIEN_TUAN_5.md) | Trạng thái thực hiện + bảng 24 ô K cuối tuần 5 |
