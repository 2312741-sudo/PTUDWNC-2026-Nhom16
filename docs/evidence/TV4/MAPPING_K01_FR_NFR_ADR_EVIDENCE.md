# MAPPING K01 — FR/NFR ↔ ADR ↔ evidence kỹ năng (TV4)

- **SRS**: v1.1.1 (Approved 16/09/2026) — bản chi tiết: `docs/root/SRS_Culinary_Blog_v1.1.1 Detail.md`
- **Nguồn yêu cầu K01**: `docs/KE_HOACH_DU_AN.md` mục 9 (hàng K01, **cột TV4** = *"SP ADR delete/media + mapping"*) và mục **9.2** (mỗi ô K phải có bản ghi: FR/NFR liên quan · đường dẫn code/config · commit/PR · test/lệnh chạy · kết quả thực tế · ảnh/log/video · reviewer + ngày).
- **Mục đích file này**: đáp ứng phần **"mapping"** của K01 và làm khung để reviewer đối chiếu 24 ô kỹ năng của TV4.
- **Trạng thái**: bản **đề xuất của TV4**, lấy từ nội dung SRS và trạng thái repo thật ở `2d7c992` (đã gồm `1492b39` từ `main`). **Chưa** có xác nhận của Nguyễn Thanh Tâm.
- **Ngày lập**: 30/09/2026 · **Nhánh**: `2312739_NHTSon_D5-D6-D7`

> [!IMPORTANT]
> Bảng này **không tự nâng trạng thái ô K lên "xong"**. Một ô chỉ chuyển sang ✅ khi có đủ
> *code/config + test + kết quả thật + reviewer Tâm xác nhận và ghi ngày* (theo `KE_HOACH_DU_AN.md` mục 9.2).
> Mapping FR/NFR ở cột C là **suy ra từ nội dung yêu cầu trong SRS**, không phải bảng tra
> chính thức của nhóm — cần Tâm xác nhận hoặc điều chỉnh.

**Chú giải trạng thái**: ✅ đủ bằng chứng · 🟡 mới có một phần · 🔴 đã có nền nhưng còn **gap thật** đã ghi nhận · ⬜ chưa làm.

---

## A. Bảng mapping 24 ô kỹ năng của TV4

| K | Kỹ thuật con TV4 phải chứng minh (mục 9) | FR / NFR liên quan | ADR / quyết định áp dụng | Trạng thái |
|---|---|---|---|---|
| K01 | SP ADR delete/media + mapping | FR-RCP-007, FR-RCP-008, FR-FILE-001/002, NFR-REL-003, NFR-SEC-007 | **ADR-TV4-001** (vận hành storage + logout), **ADR-TV4-002** (RustFS) | 🟡 Mapping xong 30/09 (file này) + 2 ADR; chờ Tâm xác nhận |
| K02 | SP Image/status group + lỗi | FR-RCP-005/006/008, FR-OBS-001, NFR-USE-003 | ADR-TV4-001 (quyền trên ảnh) | 🟡 Có nhóm endpoint image + publish/archive; thiếu mã lỗi `503 storage.unavailable` (**B1**) và test `/health/ready` 503 |
| K03 | SP file/job interfaces + status domain methods; **LAB** value object | NFR-MAINT-004, FR-FILE-001, FR-JOB-002 | — | ⬜ Chưa làm phần LAB (value object) |
| K04 | **LAB** tự viết behavior tối thiểu (logging/validation/caching) | NFR-MAINT-004, FR-OBS-002 | — | ⬜ Chưa làm (L5) |
| K05 | SP media metadata/status form; **LAB** recipe form RHF/Zod | NFR-SEC-004, NFR-USE-003, NFR-USE-004 | — | 🟡 Có validator backend; thiếu form status trong UI + RHF/Zod trong lab |
| K06 | SP Image config/index + **LAB** seed/query | NFR-PERF-004, NFR-SCALE-002 | — | ⬜ Chưa có EXPLAIN riêng cho truy vấn image/sitemap; chưa có lab |
| K07 | SP primary/status transaction; **LAB** RowVersion/filter/audit | NFR-REL-002, NFR-REL-003, FR-RCP-005, FR-RCP-008 | ADR-TV4-001 (delete recipe **giữ ảnh**) | ⬜ Chưa làm phần LAB; chưa có test 2 writer cho luồng status |
| K08 | SP logout; **LAB** register/login/refresh/reuse | NFR-SEC-001, NFR-SEC-002, FR-AUTH-005 | ADR-TV4-001 (quyết định về logout) | 🟡 Có endpoint `POST /auth/logout`; chưa có LAB rotation/reuse |
| K09 | **LAB** Google callback/verify/link | FR-AUTH-003 | — | ⬜ Chưa làm — cần Google credentials, nếu không ghi "integration ngoài còn chờ" |
| K10 | SP owner/dashboard + **LAB** VerifiedAuthor/limit | NFR-SEC-003, NFR-SEC-005, NFR-SEC-006, NFR-SEC-007, FR-RCP-008 | — | 🟡 Có `AuthorPolicy` + rate limiter; thiếu secret scan, HTTPS/HSTS, LAB |
| K11 | **LAB** recipe search + trigger/index/rank/AND/page | FR-SRCH-001…004, NFR-PERF-004 | ADR-0003 (TV2 — search/cache tuần 3) | ⬜ Chưa làm phần LAB của TV4 |
| K12 | SP status/image invalidation; **LAB** cache/output/fallback | NFR-PERF-003, NFR-REL-002, NFR-SCALE-001 | ADR-0003 | 🔴 **Gap thật**: `grep IDistributedCache\|StackExchange.Redis` trong `src/backend` = **0 kết quả**; cache đang là in-process ⇒ chưa có shared cache cho multi-instance |
| K13 | SP upload/delete đầy đủ + boundary tests | FR-FILE-001, FR-FILE-002, FR-RCP-008, NFR-SEC-004 | ADR-TV4-002 (RustFS thay MinIO) | 🟡 Có `ImageUploadValidatorTests` + `MinioE2ETests` chạy thật; thiếu kịch bản tấn công ở mức E2E |
| K14 | SP resize/sitemap; **LAB** welcome+delayed+restart | FR-JOB-002, FR-JOB-003, FR-JOB-001 | — | 🟡 Có `ResizeImageJob` + queue; thiếu recurring sitemap 02:00 UTC và distributed lock |
| K15 | SP resize/XML; **LAB** Mailhog SMTP | FR-JOB-001, FR-JOB-002, FR-JOB-003, NFR-SEO-003 | — | 🟡 Resize 300×300/800×600 đã có; sitemap mới chỉ on-demand (`GET /recipes/sitemap`) |
| K16 | SP CSR uploader/ISR media; **LAB** SSR search | FR-SRCH-001, FR-RCP-002 | — | 🟡 `tsc --noEmit` + `next build` xanh 30/09; **CI chưa build frontend** |
| K17 | SP image mutation/**progress**; **LAB** query/rollback/bundle | NFR-PERF-005, NFR-USE-004 | — | 🔴 **Thanh progress % upload chưa có** (nợ từ tuần 3) |
| K18 | SP upload/gallery/status checklist | NFR-USE-001, NFR-USE-002, NFR-USE-004 | — | ⬜ Chưa có checklist 320/768/1200 px, focus, aria |
| K19 | SP SEO/sitemap/robots; **LAB** redirect/metadata đủ | NFR-SEO-001…004, FR-JOB-003 | — | 🟡 Có JSON-LD/OG/robots/canonical; thiếu cron 02:00 UTC và 301 khi đổi slug |
| K20 | SP OTEL/probes; **LAB** logging middleware | FR-OBS-001, FR-OBS-002, FR-OBS-003, NFR-REL-001, NFR-MAINT-003 | — | 🔴 Đã cấu hình OTEL/Serilog nhưng **chưa có trace thật**; health check storage mới chỉ TCP probe |
| K21 | SP media/publish API/UI/E2E | NFR-MAINT-002 | — | 🔴 **0 Playwright / 0 Jest**; coverage `Application` **83.37%** nhưng **chưa có ngưỡng trong CI** |
| K22 | SP metrics/load; EXPLAIN + đo trang của mình | NFR-PERF-001, NFR-PERF-002, NFR-PERF-004, NFR-PERF-005 | — | 🟡 Có EXPLAIN + k6 (20 VU) nhưng **script k6 chưa commit**; chưa đo CWV/Lighthouse |
| K23 | SP Compose/Nginx; tự deploy/restore/test 2 API | NFR-REL-003, NFR-SCALE-001, NFR-SCALE-003, NFR-REL-001 | ADR-TV4-002 | 🟡 Có Compose + Nginx; thiếu backup/restore drill và 2 API instance |
| K24 | PR cá nhân + Tâm review + ADR/runbook + CI | NFR-MAINT-001, NFR-MAINT-003, NFR-SEC-007 | — | 🟡 Có PR #16/#19 + 2 ADR + CI backend; thiếu runbook, secret scan, cổng CI frontend |

**Tỷ lệ**: ✅ 0 · 🟡 12 · 🔴 4 · ⬜ 8 → **0/24 ô đủ bằng chứng** (khớp với `Tuan04/SO_EVIDENCE_TUAN_4.md` mục 2 và
`Tuan04/KE_HOACH_TUAN_4_TV4.md` mục 5: 9 ô đã có nền, 15 ô còn thiếu, **chưa ô nào được nghiệm thu**).

> **Vị trí file**: đặt ở gốc `docs/evidence/TV4/` (cạnh `TUAN_4.md`, `BAO_CAO_LAB_04.md`) vì phục vụ
> **cả 4 tuần** của TV4, không chỉ tuần 4. Tài liệu tuần 4 nằm ở `docs/evidence/TV4/Tuan04/`.

---

## B. Bằng chứng và lệnh kiểm chứng từng ô

| K | Đường dẫn evidence (đã tồn tại trong repo) | Commit / PR | Lệnh kiểm chứng | Kết quả đo được | Reviewer + ngày |
|---|---|---|---|---|---|
| K01 | `docs/adr/ADR-TV4-001-van-hanh-storage-logout-tuan-1.md`, `docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md`, file này | `c5361eb`, `d78e25c`/`cd72b27` | Đọc ADR + đối chiếu SRS | 2 ADR đã merge `main` | ⬜ Tâm — |
| K02 | `src/backend/CulinaryBlog.API/Program.cs:468-520` (publish/unpublish/archive/images) | PR #16 (`607ee14`), #19 (`208b7f7`) | `dotnet test` | Nằm trong 178/178 | ⬜ Tâm — |
| K03 | `src/backend/CulinaryBlog.Application/RecipeImages.cs`, `ImageUpload.cs` | — | `dotnet build` | Build sạch | ⬜ Tâm — |
| K04 | — | — | — | ⬜ Chưa có | ⬜ Tâm — |
| K05 | `RecipeImages.cs:202-231` (3 validator), `tests/CulinaryBlog.Tests/ImageUploadValidatorTests.cs` | PR #16 | `dotnet test` | 6 facts + 2 theories trong 178/178 | ⬜ Tâm — |
| K06 | `src/backend/CulinaryBlog.Infrastructure/Persistence/Configurations/RecipeImageConfiguration.cs` | — | `EXPLAIN` (chưa chạy cho image) | ⬜ Chưa có | ⬜ Tâm — |
| K07 | `src/backend/CulinaryBlog.Domain/Entities/RecipeImage.cs` | — | — | ⬜ Chưa có test 2 writer cho status | ⬜ Tâm — |
| K08 | `src/backend/CulinaryBlog.API/Program.cs:304-309`, `Application/Auth.cs:173-174` | PR #16 | `dotnet test` | Trong 178/178 | ⬜ Tâm — |
| K09 | — | — | — | ⬜ Chờ credentials | ⬜ Tâm — |
| K10 | `Program.cs:192` (`AuthorPolicy`), `Program.cs:97-103, 271` (rate limiter) | — | `dotnet test` | Trong 178/178 | ⬜ Tâm — |
| K11 | `tests/CulinaryBlog.Tests/Week3DiscoverySearchCacheTests.cs` (phía TV2) | PR #18 | `dotnet test` | Trong 178/178 — **phần LAB của TV4 chưa có** | ⬜ Tâm — |
| K12 | `src/backend/CulinaryBlog.Infrastructure/RecipeCacheService.cs` (in-process) | — | `git grep StackExchange.Redis` | **0 kết quả** ⇒ gap đã ghi nhận | ⬜ Tâm — |
| K13 | `tests/CulinaryBlog.Tests/MinioE2ETests.cs`, `ImageUploadValidatorTests.cs` | `cd72b27` (RustFS) | `dotnet test` | E2E storage chạy thật, `Skipped=0` | ⬜ Tâm — |
| K14 | `src/backend/CulinaryBlog.Infrastructure/ResizeImageJob.cs`, `ImageResizeQueue.cs` | PR #16 | `dotnet test` + `Tuan03/logs/d2_resize_hangfire.log` | Có log tuần 3 | ⬜ Tâm — |
| K15 | `tests/CulinaryBlog.Tests/ImageResizeD2Tests.cs`, `Program.cs:366-369` | PR #16 | `dotnet test` | Trong 178/178 | ⬜ Tâm — |
| K16 | `src/frontend/src/app/search/page.tsx`, `components/SearchFilterSelect.tsx` | `e523579`, `1492b39` | `npx tsc --noEmit`; `npm run build` | **exit 0 / exit 0** — log `Tuan04/logs/baseline_frontend.log` | ⬜ Tâm — |
| K17 | `src/frontend/src/lib/recipe-editor.ts` (media) | `1492b39` | — | ⬜ Thiếu progress % | ⬜ Tâm — |
| K18 | — | — | — | ⬜ Chưa có checklist | ⬜ Tâm — |
| K19 | `src/frontend/src/app/sitemap.ts`, `robots.ts`, `lib/recipe-jsonld.ts` | PR #16 | `npm run build` | Xanh; route `/sitemap.xml` là dynamic | ⬜ Tâm — |
| K20 | `src/backend/CulinaryBlog.API/Health.cs`, `Program.cs:41-44, 156-167` | PR #19 | `dotnet test` | `HealthTests` có 2 test và **chấp nhận cả 200/503** ⇒ chưa chứng minh được | ⬜ Tâm — |
| K21 | `tests/CulinaryBlog.Tests/*`, `.github/workflows/backend.yml` | — | `dotnet test --collect "XPlat Code Coverage"` | **178/178**; `Application` **83.37%**; **chưa** có Playwright/Jest/ngưỡng CI | ⬜ Tâm — |
| K22 | `Tuan03/logs/k6_smoke_recipes.log`, `Tuan03/logs/k6_smoke_summary.json`, `Tuan03/logs/explain_publish_culinary_test.txt` | PR #16 | `k6` (heredoc — **không commit script**) | 20 VU · 3310 req · 0% fail · p95 225.63 ms | ⬜ Tâm — |
| K23 | `docker-compose.dev.yml`, `nginx/nginx.dev.conf`, `Dockerfile`, `.github/workflows/backend.yml` | `d78e25c` | `docker compose -f docker-compose.dev.yml up -d` | 4 container lên 30/09; **chưa** có backup/restore, chưa có 2 instance | ⬜ Tâm — |
| K24 | PR #16, #19; `docs/evidence/TV4/Tuan01`–`Tuan03`; `CHANGELOG.md` | `607ee14`, `208b7f7` | `git log`, CI run | CI backend xanh; thiếu runbook + secret scan | ⬜ Tâm — |

> Cột "Kết quả đo được" chỉ ghi những gì **đã chạy thật** trong tuần 1–4. Ô `⬜` là chưa có bằng chứng,
> không suy diễn từ việc "đã đọc" hoặc "đã có trong PR của người khác".

---

## C. Yêu cầu của SRS mà TV4 liên quan nhưng chưa có bằng chứng

| Yêu cầu | Nội dung | Ô K phụ trách | Vì sao chưa có |
|---|---|---|---|
| NFR-REL-001 | Uptime ≥ 99.5%, probe `/health/ready` mỗi 10s, cảnh báo khi down > 1 phút | K20, K23 | Chỉ đo được ở môi trường thật; tuần 4 chỉ ghi được giới hạn, không ngụ ý đạt |
| NFR-REL-003 | `pg_dump` 03:00 giữ 30 ngày, volume file persistent, soft delete khôi phục được | K23, K07 | Chưa có script backup, chưa có drill restore |
| NFR-SCALE-001 | Distributed cache (không in-memory), distributed lock RedLock, Hangfire nhiều worker | K12, K14, K23 | Cache còn in-process; chưa có lock cho sitemap |
| NFR-SCALE-003 | Nginx upstream pool nhiều API instance, MinIO distributed mode | K23 | Chưa có 2 instance; Nginx chưa có `proxy_next_upstream` |
| NFR-SEC-005 | HTTPS + HSTS + CORS không wildcard | K10 | Nginx dev chỉ `listen 80`, chưa có `ssl_certificate` |
| NFR-SEC-007 | Không commit secret, có scanning (gitleaks/truffleHog) | K10, K24 | `render.yaml` còn JWT key hardcode; CI chưa có secret scan |
| NFR-USE-002 | WCAG 2.1 AA + kiểm thử NVDA/VoiceOver | K18 | Chưa có checklist và bằng chứng screen reader |
| NFR-USE-004 | Progress bar % realtime khi upload ảnh | K17, K18 | Thanh progress chưa làm (nợ tuần 3) |
| NFR-PERF-005 | CWV: LCP ≤ 2.5s, CLS ≤ 0.1, INP ≤ 200ms, First Load JS ≤ 200KB | K17, K22 | Chưa chạy Lighthouse; `next build` báo First Load JS shared **105 kB** (nằm trong ngưỡng, cần ghi kèm đo thật) |
| NFR-SEO-004 | 301 redirect khi slug đổi | K19 | Chưa có |
| FR-JOB-001 | Welcome email retry 1′/5′/30′ | K14, K15 | Phần SP thuộc TV1; TV4 có phase `email` trong lab L4 (39/39) |

---

## D. ADR liên quan

| ADR | Nội dung | Ảnh hưởng ô K của TV4 |
|---|---|---|
| `docs/adr/ADR-TV4-001-van-hanh-storage-logout-tuan-1.md` | Quyết định vận hành storage + logout (xóa recipe dùng soft delete nên **giữ ảnh**; xóa ảnh riêng lẻ mới xóa object) | K01, K02, K07, K08, K13 |
| `docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md` | Đổi MinIO → RustFS (Apache-2.0) vì image MinIO bị gỡ khỏi registry; ghim tag + digest | K13, K23 |
| `docs/adr/ADR-0003-category-and-search-caching-week3.md` (TV2) | Chiến lược cache search/category | K11, K12 (TV4 dùng chung, không tự viết lại) |
| `docs/adr/ADR-0001-recipe-schema-va-concurrency.md` (TV3) | Schema Recipe + RowVersion | K07 (test 2 writer dùng chung schema) |

> ADR mới cần tạo trong tuần 4 (nếu nhóm chốt hướng): **ADR về chia sẻ cache/multi-instance** và
> **ADR về chiến lược backup + timezone lịch 03:00** — cả hai đều là câu hỏi mà kế hoạch hiện để
> "chọn hướng A/B", chưa có quyết định.

---

## E. Cách reviewer dùng bảng này

1. Chọn ô K → xem hàng tương ứng ở **bảng A** (yêu cầu + FR/NFR) và **bảng B** (bằng chứng + lệnh).
2. Tự chạy lại **cột "Lệnh kiểm chứng"** và đối chiếu **cột "Kết quả đo được"**.
3. Với ô 🟡/🔴/⬜: kiểm tra xem phần thiếu có đúng là đã ghi ở `Tuan04/KE_HOACH_TUAN_4_TV4.md` mục 5 và
   `Tuan04/SO_EVIDENCE_TUAN_4.md` mục 2 không — nếu thiếu mà không được ghi, đó là lỗi tài liệu, sửa ngay.
4. Ghi **ngày xác nhận** vào cột cuối bảng B khi chấp nhận; ô chỉ chuyển ✅ khi **cả** các kỹ thuật con
   trong cột "Kỹ thuật con" đều có bằng chứng (mục 9.2: *thiếu một phần thì ô chưa hoàn thành*).
5. Nếu cột "FR/NFR liên quan" của TV4 gán sai, sửa cột đó và ghi lại — đây là điểm cần Tâm chốt,
   vì ảnh hưởng tới việc đối chiếu ma trận bao phủ ở `KE_HOACH_DU_AN.md` mục 10–11.
