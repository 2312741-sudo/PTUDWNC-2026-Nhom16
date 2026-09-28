# KẾ HOẠCH TUẦN 3 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

- **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026, giải quyết C01–C09)
- **Phần nghiệp vụ**: Xuất bản, hình ảnh, SEO và vận hành — tasks D3–D7 (theo `docs/KE_HOACH_DU_AN.md` mục 8 và `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md`).
- **Mã task tuần 3** (theo 6-tuần, dòng 3 TV4): D3 (archive/delete theo ADR D08), D4 (hoàn thiện uploader UI + status + SEO sitemap/robots/OG/JSON-LD), D5 (OTEL/metrics/health), D6 (lab L4 Identity/Google/refresh/forms/FTS). Cộng phần bàn giao thiếu tuần 2: D2 resize, D1.1c test MinIO down, E2E D1.3. (D3.3 logout revoke refresh family — ✅ đã xong vì C5 refresh đã có trên main.)
- **Nhánh Git đề xuất**: `2312739_NHTSon_D3-D4-D5-D6` (tv4/week3) — đã merge `origin/main` mới nhất (a651c8a + 6 commit deploy/Render/100 ảnh) → HEAD `4830e57`; lab: `practice/TV4/L4`.
> **Cập nhật 24/09**: working tree đã thêm D3 archive/delete + D4 SEO + D5 OTEL + E2E MinIO + CI MinIO service. **133/133 + 5/5 pass local, frontend build OK** — chưa commit/push.
> **Cập nhật 27/09 (mốc D23/D27)**: nhóm chốt hướng **D23 → Hangfire (PA-1)** + **D27 → base media URL proxy có auth (PA-2)** theo đề xuất `docs/DE_XUAT_GIAI_QUYET_D23_D27.md` (khớp FE TV3 PR #15 đã ghép `NEXT_PUBLIC_MEDIA_URL`). D2 resize + D1/D4 display chuyển từ "chờ quyết định" sang "có phương án được duyệt".
- **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng).
- **Cổng**: G4 giữa tuần (publish/unpublish/archive end-to-end + ảnh hiển thị được qua presigned theo D27; sitemap Published-only) → G5 cuối tuần (đủ FR media/status/jobs/health; queue persistent; sitemap/OG/JSON-LD; OTEL trace HTTP→DB; CI xanh).

> Tuần 3 tính từ ngày 23/09/2026 (kết thúc tuần 2: block 2.1 đã gỡ hoàn toàn, D3.1/D3.2 + D1.3 đã trong main; C5 refresh cũng đã có trên main → D3.3 xong).
> Tài liệu này là **kế hoạch thực thi + khung minh chứng**, mọi việc ban đầu ở trạng thái **Chưa làm** trừ phần đã được đánh dấu hoàn thành.

---

## 0. Bối cảnh bước vào tuần 3 (ghi nhận thực tế 23/09)

- **PR #14 (D1.3 image API + D3.1/D3.2 publish/unpublish)** đã được merge nhầm vào main (commit `3eb6de3` + `c512943`) trước khi hoàn tất nghiệm thu trên nhánh TV4. **Quyết định nhóm**: giữ nguyên trên main, đưa việc rà soát/khắc phục vào kế hoạch tuần 3. Việc này đồng nghĩa D1.3/D3.1/D3.2 đã "chính thức" mặt bằng main và TV4 cần tập trung phía sau.
- **CI main đỏ (pre-existing từ tuần 2) — ĐÃ ĐƯỢC FIX 23/09 T2**: main `a651c8a` ("loai bo migration trung lap") xoá 2 migration trùng (`AddRefreshTokens` 20260923104044 + `AddRecipeDiscoveryAndSearch` 20260916102353); bảng `RefreshTokens` giờ chỉ do `20260919061954_AddRecipeAggregate` tạo (kèm index `IDX_RefreshToken_Hash`). Commit cùng bật CORS + auto migrate/seed khi deploy. **TV4 đã verify**: build 0 warning, format sạch, **CulinaryBlog.Tests 120/120** (16 test Auth/Week3 trước đây fail giờ pass), **spike 5/5** → N0 coi như xong; **CI GitHub branch tuần 3 = success** (run `818522b` cho HEAD `28a87b3`).
- **C5 refresh token ĐÃ có trên main** (xác minh 23/09 trong kiểm chứng tổng thể tuần 2): `/auth/refresh` + `RefreshTokenAsync` (rotation + family reuse revocation `compromised-reuse-detected`) + `LogoutAsync` (revoke theo hash + revoke mọi token active của family) trong `IdentityService.cs`; 7 test Week3 + 16 test Auth pass → **D3.3 logout revoke refresh family đã hoàn thành** (không cần TV3 bàn giao nữa).
- **Merge mới từ main sau `a651c8a` (23/09 T3 — TV4 đã merge thành công `e026dc9`→`75a8bf5`→`e5b1bad`→`4830e57`)**: TV1/TV2 lan thêm 6 commit — deploy Render (`Dockerfile`, `render.yaml`, `.dockerignore`, hỗ trợ `DATABASE_URL` + `PORT` + `NormalizePostgreSqlConnectionString`), chỉnh `DbSeeder` seed **100 ảnh món ăn thực tế local `/images/recipes/{slug}.jpg`** + `RecipeImage.SetOriginalUrl`, `CategoryRepository` tính `recipesCount` theo Published, UI Emerald&Gold + logo. **Hệ quả sai lầm phát hiện & fix**: connection string đọc eager ở top-level khiến override `TEST_DATABASE` của test mất hiệu lực → 18 test Auth/Week3 fail `28P01`; TV4 chuyển đọc vào lambda `AddDbContext` (commit `4830e57`, local 120/120 + 5/5 lại xanh; **cần đề xuất đưa fix này lên main** vì CI main cũng dính).
- Block còn lại từ tuần 2: D27 bucket policy (ảnh hiển thị frontend cho ảnh UPLOAD qua API — 100 ảnh seed giờ là static path local), D23 queue resize (chưa chốt Hangfire/BackgroundService).

---

## 1. Mục tiêu tuần 3

| Tiêu chí | Bàn giao kỳ vọng |
|---|---|
| Nghiệp vụ | Archive/delete recipe theo ADR D08 (FR-RCP-006/007); hoàn thiện uploader UI + status/action buttons (FR-RCP-008, C4/TV3 editor); sitemap XML Published-only + robots + canonical + JSON-LD (FR-SEO, D26); OTEL/metrics/health thành phần (FR-OBS-001/003, D20/D21/D22); logout revoke refresh family nếu TV3 C5 bàn giao (FR-AUTH-005) |
| Nghiệm thu | Archive ẩn public ngay, giữ dữ liệu, owner/Admin; DELETE recipe soft theo D08, không lộ search/cache, không mất ảnh cần restore; uploader hiển thị ảnh bằng URL hợp lệ (D27); sitemap chỉ chứa Published, không chứa Draft/Archived; trace HTTP→DB có correlation; CI xanh toàn bộ |
| Skill | Chứng minh SP K01->K07/K10/K12/K13/K20-K24; LAB L4 K04/K12->15/K17/K19; chốt ADR D08/D17/D21->D23/D26/D27 |

---

## 2. Hiện trạng repo tại thời điểm lập kế hoạch

| Mảng | Trạng thái | Ảnh hưởng đến TV4 tuần 3 |
|---|---|---|
| Block 2.1 (recipe cluster) | ✅ **Gỡ hoàn toàn** 23/09 (4/4 điều kiện) | Recipe CRUD + ingredient/step + D1.3 images có trong API; seed qua `POST /recipes` |
| D1.3 + D3.1/D3.2 | ✅ Trong main (PR #14, merge nhầm) | Base phát triển D3 archive/D4 UI; cần CI xanh và E2E thật trên MinIO |
| CI | ✅ **Xanh** — main `a651c8a` đã fix duplicate migration; branch tuần 3 CI run `818522b` = success | Hết chặn; N0 hoàn tất |
| C5 refresh (TV3) | ✅ **Đã có trên main** (23/09 verify) | D3.3 logout revoke family ĐÃ xong (7 test Week3 + 16 test Auth pass) |
| Dockerize/Render | ✅ Mới trên main: `Dockerfile`, `render.yaml`, `DATABASE_URL`/`PORT`/normalize | D5 health/jobs có base deploy thật; TV4 cần chuyển fix connection string `4830e57` lên main |
| Seed 100 ảnh thật | ✅ Mới trên main: `/images/recipes/{slug}.jpg` + `SetOriginalUrl` + UI Emerald | Ảnh seed hiển thị được static path; D4 uploader focus ảnh UPLOAD qua API (D27) |
| D2 resize | ✅ **Xong 27/09 (N4)** — PA-1 Hangfire + ImageSharp, job thật `Succeeded`, E2E 4/4 | LAB delayed/restart còn lại → N5/D6 |
| D27 bucket policy | ✅ **Chốt + xong 27/09 (N2)** — PA-2 proxy có auth; FE TV3 chỉ set `NEXT_PUBLIC_MEDIA_URL` | Uploader UI (progress/rollback/gallery/primary) còn lại cho D4 |
| Frontend Next.js | ✅ UI Emerald & Gold + logo (main `e9c1241`) | D4 uploader/status ghép TV3 C4 + TV1 host/deploy |

---

## 3. Phân rã công việc tuần 3

### N0 — Mở đầu: fix CI main + rà soát PR #14 đã merge (ưu tiên cao) ⭐ ĐÃ XONG (23/09 T2)

**Skills**: K01, K23, K24 · **ADR**: D25 (lockfile)

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | ✅ **Fix duplicate migration `RefreshTokens`** — main `a651c8a` đã xoá `AddRefreshTokens` + `AddRecipeDiscoveryAndSearch`; TV4 verify lokal | `Migrate()` chạy được trên DB mới; build 0 warning; format sạch; CulinaryBlog.Tests **120/120** + spike **5/5** — **đạt** |
| 2 | ✅ Push branch tuần 3 → **CI GitHub success** (run `818522b`) + rà soát diff PR #14 so với main | CI xanh trên GitHub; không xung đột logic; không lộ secret; test vẫn pass |
| 3 | ⏳ **Mới**: chuyển fix connection string lazy `4830e57` lên main (main đang dính eager-read mất TEST_DATABASE) | CI main xanh với 6 commit mới (deploy Render/100 ảnh/UI) |
| 4 | Cập nhật docs trạng thái đã fix | TRANG_THAI/KE_HOACH/SO_EVIDENCE tuần 3 đã cập nhật 23/09 T2 + rà soát T3 |

### N1 — D3: Archive/delete + invalidation + D3.3 logout revoke (theo FR-RCP-006/007, D08)

**Skills**: K01, K02, K03, K04, K07, K10, K12 · **ADR**: D08 (soft delete), D13 (unpublish ẩn ngay), D06/D05 (logout refresh revoke)

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | `PATCH /recipes/{id}/archive`: Published/Draft → Archived, ẩn public ngay, giữ dữ liệu, owner/Admin (403 non-owner); idempotent | Mã lỗi chuẩn + test happy/403/404 |
| 2 | `DELETE /recipes/{id}` (tuỳ ADR D08): soft delete theo ADR, không xoá vật lý ảnh cần restore; không lộ trong search/cache | Test delete + không còn trong public list/search |
| 3 | ~~Invalidation khi archive/unpublish/delete: clear cache~~ — ✅ **Đóng 27/09 bằng xác minh**: không tồn tại cache recipe nào (backend không OutputCache/Redis-dữ-liệu; FE recipe luôn `no-store`; `RecipeCacheService` orphan chưa wire) → tiêu chí thỏa mặc định; ghi handoff TV2/TV3: nếu nhóm thêm ISR/output-cache cho recipe → cần revalidate hook | Draft/Archived không được phục vụ bởi cache cũ — ✅ xác minh: không có cache |
| 4 | ~~D3.3 logout revoke refresh family~~ — ✅ **Đã xong** (23/09): C5 refresh token đã có trên main (`RefreshTokenAsync` rotation + family revocation + `LogoutAsync`); 7 test Week3 + 16 test Auth pass | 204; refresh cũ không dùng được — cần xác nhận test logout revoke family chạy trong suite |
| 5 | Hoàn tất E2E D1.3 + D1.1c: chạy với MinIO local: upload/PATCH primary/DELETE, MinIO down → lỗi rõ ràng + log redacted | Evidence log thật, không commit secret |

### N2 — D4: Uploader/status UI hoàn thiện + SEO (FR-RCP-008, FR-SEO, D26)

**Skills**: K16, K17, K18, K19, K22 · **ADR**: D17 (PATCH images), D26 (sitemap), D27 (presigned/proxy)

> **Cập nhật 27/09 (mốc D23/D27)**: FE editor ảnh đã có sẵn trong PR #15 (TV3) `ImagesStep.tsx` (upload/delete/set-primary/gallery); D27 được chốt **PA-2 base media URL qua proxy** (đề xuất `docs/DE_XUAT_GIAI_QUYET_D23_D27.md`) → TV4 giảm phạm vi xuống: review + bổ sung progress/rollback + nối URL proxy, FE gần như chỉ cấu hình `NEXT_PUBLIC_MEDIA_URL`.

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | Review PR #15 `ImagesStep.tsx` + bổ sung progress upload thật + rollback khi lỗi | Upload hiển thị tiến trình; lỗi hiển thị + rollback; gallery đúng 1 primary |
| 2 | Status/action buttons: Publish/Unpublish/Archive từ dashboard edit (phối hợp TV3 C4) | Đổi trạng thái end-to-end, reload UI theo trạng thái |
| 3 | Ảnh hiển thị qua proxy D27 (`GET /resources/images/{key}`) theo đề xuất PA-2 | ✅ **Xong 27/09** — endpoint proxy + `IObjectStorageReader` + test E2E `ImageProxyD27Tests` 6/6 (Published public/cache; Draft/Archived owner/Admin else 403; 404 invalid/deleted); FE TV3 chỉ set `NEXT_PUBLIC_MEDIA_URL` |
| 4 | Sitemap XML **Published-only** + robots.txt + canonical + JSON-LD (dữ liệu có cấu trúc); cron 02:00 UTC (D26) | Sitemap không chứa Draft/Archived; Google-tested file hợp lệ |

### N3 — D5: OTEL/metrics/health (FR-OBS-001/003, D20/D21/D22)

**Skills**: K20, K22, K23 · **ADR**: D20 (OTEL trace HTTP→DB), D21 (uptime 99,5%), D22 (Redis down fallback vs readiness)

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | Serilog/Seq/OTEL: trace HTTP→DB có correlation id qua Nginx (phối hợp TV1) | Trace thấy request đi qua Nginx→API→DB |
| 2 | Metrics: request duration/count, DB query time; health endpoint thành phần (db/redis/minio) đúng D22 | `/health` phản ánh đúng; Redis down → API fallback nhưng readiness 503 |
| 3 | EXPLAIN + k6/load cho publish/list query (K22) | ✅ **Xong 27/09** — EXPLAIN `0.339ms/0.044ms`; k6 smoke 20 VU×30s: **3310 req 0% fail, p95 225.63ms**; log tại `Tuan03/logs/` |

### N4 — D2 (bàn giao thiếu tuần 2): Resize original/300×300/800×600 + job (FR-JOB-002/003, D23)

**Skills**: K13, K14, K15, K23 · **ADR**: D23 (queue persistent + retry) · **Block**: quyết định D23 của nhóm

> **Cập nhật 27/09 (mốc D23)**: nhóm chốt **PA-1 Hangfire** theo đề xuất `docs/DE_XUAT_GIAI_QUYET_D23_D27.md` (queue persistent PostgreSQL + dashboard Admin; retry 3; khớp SRS tr.38).
> **Kết quả 27/09 (tối)**: N4 **xong** — xem bảng trạng thái bên dưới. Phần LAB (delayed job/restart worker) chuyển sang **N5/D6**.

| # | Việc làm | Kết quả mong đợi | Trạng thái 27/09 |
|---|---|---|---|
| 1 | Chốt queue với nhóm — **Hangfire (PA-1)** theo `DE_XUAT_GIAI_QUYET_D23_D27.md`; regenerate `packages.lock.json` (không `--locked-mode` khi thêm) | Quyết định ghi ADR D23 + lockfile khớp CI `--locked-mode` | ✅ API `Hangfire.AspNetCore 1.8.25` + `Hangfire.PostgreSql 1.21.1`; Infra `Hangfire.Core 1.8.25` + `ImageSharp **3.1.11**` (4.x cần license thương mại) |
| 2 | Resize tạo `{base}_300x300` / `{base}_800x600`, cập nhật URL DB, enqueue sau upload; original fallback khi lỗi/AVIF | Đủ 2 kích thước phái sinh; fallback original; test unit + E2E | ✅ `ResizeImageJob` + `RecipeImageKeys` + hook enqueue; E2E `ImageResizeD2Tests` 4/4 + 5 unit |
| 3 | Job persistent: retry idempotent (3), restart worker không mất job; boundary delete vs resize; dashboard `/hangfire` chỉ Admin | Test restart/retry; không tái sinh ảnh đã xoá | ✅ retry 3 + `ExistsAsync` idempotent + delete-vs-resize; dashboard Admin-only (401/403/200); **chạy thật job `Succeeded`**; phần LAB delayed/restart để N5/D6 |

### N5 — D6: Lab L4 hoàn thiện

**Skills**: K04, K12, K13, K14, K15, K17, K19

> **Kết quả 28/09 (N5)**: mục 1 **xong** — commit `3642428` trên nhánh riêng `practice/TV4/L4`, `media` 25/25 · `email` 3/3 · `xml` 3/3 · `jobs` 8/8 = **4 phase · 39/39 check PASS**, sổ K `Tuan03/SOK_LAB_L4.md`. Mục 2 (Identity/Google/refresh/forms/FTS) **chưa làm**.

| # | Việc làm | Kết quả mong đợi | Trạng thái 28/09 |
|---|---|---|---|
| 1 | Nhánh `practice/TV4/L4`: upload/delete 4 MIME + resize 300×300/800×600 + Mailhog email + Hangfire delayed/restart | Commit lab + test | ✅ **Xong** — 39/39 check; thêm 2 phase chẩn đoán `db`/`purge` (không tính vào yêu cầu); log `Tuan03/logs/lab_l4_{run.log,db.txt}` |
| 2 | Lab theo PHAN_CHIA tuần 3 TV4: Identity/Google/refresh/forms/FTS (phần TV4 cần học) | PR lab ngoài phần chính, có sổ K | ⬜ **Chưa làm** — ⚠️ đối chiếu 28/09: `PHAN_CHIA` mục 3.4 + `KE_HOACH_DU_AN` mục 4.2 đều **xếp việc này vào tuần 3**, không phải tuần 4 → cần nhóm trưởng chốt (làm tối giản / ghi nhận dời tuần 4) thay vì tự ghi "để tuần 4" |
| 3 | *(phát sinh 28/09)* CI 5 run đỏ vì image MinIO bị gỡ khỏi registry — gỡ + harden action + sửa dev compose | CI xanh + dev stack dựng được trên máy mới | ✅ **Xong** — `cd72b27` + `d78e25c`; CI xanh run `36344662570` |

---

## 4. Phụ thuộc & bàn giao tuần 3

### TV4 cần nhận:
| Từ | Nhận gì | Khi nào |
|---|---|---|
| ~~TV3 (C5)~~ | ~~Refresh token family để logout revoke (D3.3)~~ — ✅ C5 đã có trên main, không cần | — |
| Cả nhóm | ✅ Quyết định D27 — **PA-2 base media URL proxy có auth** (`DE_XUAT_GIAI_QUYET_D23_D27.md`, 27/09) | ✅ Đầu tuần 3 |
| Cả nhóm | ✅ Quyết định D23 queue — **PA-1 Hangfire** (`DE_XUAT_GIAI_QUYET_D23_D27.md`, 27/09) | ✅ Đầu tuần 3 |
| ~~Nhóm trưởng~~ | ~~Xác nhận giữ nguyên PR #14 đã merge hay cần điều chỉnh~~ — ✅ PR #14 đã được nhóm chung tay sửa; thống nhất giữ nguyên (mọi task/CR từ PR #14 xem như hoàn thành) | — |
| Nhóm trưởng | Reviewer cho PR fix connection string `4830e57` → main | Ngày 1–2 |
| Nhóm trưởng | Review/duyệt đề xuất D23/D27 (`DE_XUAT_GIAI_QUYET_D23_D27.md`) + ghi ADR | Sau 27/09 |

### TV4 phải bàn giao sớm:
| Bàn giao cho | Gì | Khi nào |
|---|---|---|
| Cả nhóm | Fix duplicate migration `RefreshTokens` + CI xanh | Ngày 1–2 — ✅ đã xong (branch CI success) |
| Cả nhóm | Fix connection string lazy `4830e57` (CI main đang dính eager-read) | Ngày 1–2 |
| TV3 (C4) | Contract/status API archive + **image URL proxy D27** (`/resources/images/{key}`) cho editor | Giữa tuần 3 |
| Cả nhóm | E2E D1.3/D3 + D2 resize (Hangfire) + D5 metrics chạy thật | Cuối tuần 3 |

### Thứ tự ưu tiên:
1. ✅ Fix CI main (N0) — **đã xong** 23/09; bổ sung PR fix connection string `4830e57` lên main.
2. ✅ Chốt D27 (PA-2 proxy) + D23 (PA-1 Hangfire) — **đã chốt 27/09**; bắt đầu thực hiện.
3. ~~Archive/delete + invalidation (D3)~~ — ✅ **xong 24/09; đóng invalidation bằng xác minh 27/09 (không cache)** → proxy ảnh D27 → uploader UI ghép TV3 C4.
4. Resize Hangfire (D2 theo D23) → sitemap/JSON-LD/OTEL (D4/D5) → cuối tuần chốt G5.

---

## 5. Ma trận skill tuần 3 TV4

| K | Loại | Sẽ chứng minh ở | Evidence key |
|---|---|---|---|
| K01 | SP | ADR D08/D17/D21/D22/D23/D26/D27 + mapping FR + đề xuất `DE_XUAT_GIAI_QUYET_D23_D27.md` | TV4-K01 |
| K02 | SP | archive/delete status endpoints + RFC7807 | TV4-K02 |
| K03 | SP | presigned/proxy qua `IFileStorageService` + domain methods | TV4-K03 |
| K04 | SP+LAB | archive/delete CQRS handlers; LAB behavior | TV4-K04 |
| K05 | SP | validator status transition + sanitize UI input | TV4-K05 |
| K06 | SP | migration fix `RefreshTokens` + index mới | TV4-K06 |
| K07 | SP | concurrency khi đổi trạng thái/xoá (RowVersion) | TV4-K07 |
| K10 | SP | ownership archive/delete; 403 non-owner | TV4-K10 |
| K12 | SP+LAB | invalidation archive/unpublish/delete — 🟡 đóng bằng xác minh 27/09 (không cache recipe) + LAB cache L4 | TV4-K12 |
| K13 | SP+LAB | upload/delete + E2E MinIO + 4 MIME (lab) | TV4-K13 |
| K14 | SP+LAB | resize job + restart/retry (D23) | TV4-K14 |
| K15 | LAB | Mailhog + resize + sitemap (lab) | TV4-K15 |
| K16 | SP | uploader UI Next.js hoàn thiện | TV4-K16 |
| K17 | SP | TanStack Query optimistic + rollback | TV4-K17 |
| K18 | SP | WCAG2.1 AA + keyboard/loading/error checklist | TV4-K18 |
| K19 | SP | sitemap/robots/OG/JSON-LD Published-only | TV4-K19 |
| K20 | SP | OTEL trace + metric + health thành phần | TV4-K20 |
| K22 | SP | EXPLAIN + k6 + cache hit | TV4-K22 |
| K23 | SP | CI xanh + Compose/volumes + queue service | TV4-K23 |
| K24 | SP | PR nhỏ từng task + review Tâm | TV4-K24 |

---

## 6. Checklist cổng tuần 3

- [x] Fix duplicate migration `RefreshTokens` (main `a651c8a`) + **CI GitHub branch tuần 3 = success** (run `818522b`); local 120/120 + 5/5 pass.
- [x] Logout revoke refresh family — C5 refresh ĐÃ có trên main; 204 idempotent (7 test Week3 + 16 test Auth pass).
- [x] **Archive/DELETE (24/09)**: `PATCH /recipes/{id}/archive` (ẩn public ngay, owner/Admin, idempotent) + `DELETE /recipes/{id}` soft theo D08 (`MarkDeleted` + global filter, giữ ảnh restore); chỉnh núm 133/133 + 5/5.
- [x] **E2E D1.3 MinIO (24/09)**: `MinioE2ETests` upload/PATCH primary/publish/unpublish/archive/delete 3/3 pass; MinIO down → skip an toàn; không lộ secret; CI đã thêm service MinIO.
- [x] **Sitemap/robots/SEO (24/09)**: `GET /recipes/sitemap` Published-only + `sitemap.ts`/`robots.ts` + SEO metadata trang công thức; `next build` exit 0.
- [x] **OTEL/metrics/health (24/09)**: trace ASP.NET/Http/EF + metrics + health db/redis/minio; **27/09 hoàn tất EXPLAIN publish query + k6 smoke** (`logs/`) — N3 xong.
- [ ] PR fix connection string lazy `4830e57` → main (CI main 6 commit mới có thể dính 28P01).
- [x] **Proxy ảnh D27 (27/09)**: `GET /api/v1/resources/images/{**key}` + `IObjectStorageReader` (tách khỏi `IFileStorageService`); Published public + cache; Draft/Archived owner/Admin else `403 image.forbidden`; `404` invalid/unknown/soft-deleted; E2E `ImageProxyD27Tests` **6/6**; `IMAGE_CONTRACT.md §5` chốt PA-2.
- [ ] Uploader UI: review PR #15 `ImagesStep.tsx` + progress + rollback + gallery + primary; nối URL proxy (set `NEXT_PUBLIC_MEDIA_URL`). → **28/09: PR #15 đã merge vào main (`b591e74`)**; rà main thấy `ImagesStep.tsx` **đã có** upload/delete/set-primary + optimistic rollback + đã dùng `NEXT_PUBLIC_MEDIA_URL`; **còn thiếu thanh progress upload**. Nhánh này **chưa có** FE editor (behind main 23) → merge `origin/main` trước khi sửa.
- [ ] Status buttons Publish/Unpublish/Archive end-to-end với TV3 C4. → **28/09: Publish đã có** (`ReviewStep.tsx` gọi `publishRecipe`); **Unpublish/Archive chưa có nút nào** dù API đã sẵn sàng → làm sau khi merge main.
- [ ] **Sitemap theo lịch 02:00 UTC + distributed lock khi nhiều worker** (CR-7; `KE_HOACH_DU_AN` mục 8 dòng 339 + N2 item 4) — hiện chỉ có `GET /recipes/sitemap` on-demand + `sitemap.ts`/`robots.ts`.
- [ ] **Trace thật vào Seq** (K20 — G5 tuần 3 yêu cầu "OTEL trace HTTP→DB"): bật service `seq` trong compose + gửi OTLP + chụp log làm bằng chứng.
- [ ] **Bảng mapping K01** (FR ↔ ADR ↔ evidence key) — bảng kỹ năng vẫn ghi "Chưa làm"; reviewer cần để nghiệm thu.
- [x] **Resize D2 (27/09, N4)**: `ResizeImageJob` (300×300/800×600, idempotent, original fallback, delete-vs-resize, retry 3) + `IObjectStorageWriter` (key phái sinh chủ động, tách khỏi `IFileStorageService`) + `IImageResizeQueue` (Hangfire / Inline ở `Testing`) + dashboard `/hangfire` chỉ Admin; E2E `ImageResizeD2Tests` **4/4** + 5 unit; **job Hangfire thật `Succeeded`**, proxy trả 300×200, ảnh hỏng → fallback original; suite **148/148 + 5/5**; `IMAGE_CONTRACT.md §7`; log `Tuan03/logs/d2_resize_hangfire*.{log,txt}`.
- [x] **Invalidation cache archive/unpublish/delete (27/09 — đóng bằng xác minh)**: không có cache recipe nào để invalidate (backend không OutputCache/Redis-dữ-liệu; FE recipe `no-store`; `RecipeCacheService` orphan chưa wire) → handoff TV2/TV3 nếu nhóm thêm ISR/output-cache.
- [ ] ~~Lab `practice/TV4/L4` commit + sổ skill cập nhật; CI pass sau mỗi task; không commit secret.~~ → **xong 28/09 (N5)**: commit `3642428`, **4 phase · 39/39 check PASS**, sổ K `Tuan03/SOK_LAB_L4.md` + `practice/TV4/L4/README.md`, log `lab_l4_{run.log,db.txt}` không secret; CI pass sau mỗi task ✅; không commit secret ✅ (quét staged diff trước khi commit).
- [x] **CI 5 run đỏ do image MinIO bị gỡ khỏi registry (28/09)** — `quay.io/minio/minio` 401 + `minio/minio` Docker Hub 404 → job chết ở "Initialize containers", mọi bước khác bị skip. Thay bằng `rustfs/rustfs` (S3-compatible, `MinioStorageService` không đổi dòng) trong **cả** workflow lẫn dev compose; bỏ `minio-init` (app tự tạo bucket); harden CI (tag+digest, SDK pin, timeout 30', concurrency, blame-hang 10') → **CI xanh** run `36344662570`.
- [ ] N5 mục 2: lab Identity/Google/refresh/forms/FTS theo `PHAN_CHIA` tuần 3.

---

## 7. Trở ngại dự kiến & cách xử lý

| Rủi ro | Ảnh hưởng | Giải pháp |
|---|---|---|
| Main CI đỏ kéo dài (duplicate migration) | Chặn mọi thành viên | ✅ Đã fix (main `a651c8a`); branch tuần 3 CI success; còn chuyển fix connection string `4830e57` lên main |
| **Image container trong CI/compose bị gỡ khỏi registry** | Job chết ở "Initialize containers", **mọi** test bị skip → CI xanh giả trong khi không hề kiểm thử gì (xảy ra 28/09 với MinIO) | ✅ Đổi sang `rustfs/rustfs` (tag + digest) trong workflow lẫn dev compose; **gỡ bằng chứng thật** (148/148 `Skipped=0` + bucket/object có thật trong storage); ghim digest + SDK + timeout + blame-hang để lỗi tương tự lộ ra sớm hơn |
| ~~C5 TV3 chưa bàn giao~~ | ~~D3.3 logout chưa revoke family~~ | ✅ C5 đã có trên main — D3.3 xong |
| Eager-read connection string trong main phá override test | CI main fail 28P01 khi thêm commit deploy | TV4 fix `4830e57` (đọc trong lambda AddDbContext) → PR lên main ngay |
| ~~D27 chưa chốt~~ | ~~Uploader ảnh không hiển thị~~ | ✅ **Chốt 27/09 — PA-2 proxy** (`DE_XUAT_GIAI_QUYET_D23_D27.md`); FE TV3 chỉ set `NEXT_PUBLIC_MEDIA_URL`; làm proxy D27 |
| ~~D23 chưa chốt queue~~ | ~~Resize (D2) trễ~~ | ✅ **Xong 27/09** — PA-1 Hangfire đã implement (N4), job chạy thật `Succeeded` |
| ImageSharp 4.x bắt buộc license key thương mại | Build fail khi nâng version | Giữ **3.1.11**; AVIF không decode → job fallback original (đã xử lý) |
| MinIO SDK `WithCallbackStream` + `async` lambda | Ảnh cắt cụt/stream hỏng khi đọc đồng thời | Copy **đồng bộ** trong `ReadAsync` + kiểm tra `buffer.Length == stat.Size` (đã sửa, test ổn định) |
| D27 proxy qua API có thể tăng tải/đi qua app | Bandwidth/token khi phục vụ ảnh | Cache-Control hợp lý (Published cache); cân nhắc Nginx route thẳng MinIO sau khi chốt vị trí proxy với TV1 |
| PR #14 đã merge nhầm gây xung đột docs/số liệu | Doc nhầm trạng thái | Rà soát diff, note rõ trong sổ evidence; đưa vào báo cáo nhóm |
| **Nhánh lệch main 23 commit** (28/09) | D4 UI không sửa được (thiếu `dashboard/recipes/**` của PR #15); dễ mất thay đổi của TV1/TV2/TV3 | Merge `origin/main` trước khi làm D4; xử lý conflict `docker-compose.dev.yml` + `.github/workflows/backend.yml` |
| ~~**Hai image storage khác nhau trên main vs nhánh**~~ (28/09) | Nhánh `rustfs/rustfs` (digest) vs main `coollabsio/minio:RELEASE.2025-10-15T17-29-55Z` (TV3 `84dddd4`) → conflict khi merge; CI main chưa có service storage nên E2E vẫn **skip âm thầm** | ✅ **Đã gỡ (PR #16)**: CR-6 chốt `rustfs/rustfs` cho dev + CI, ghim tag+digest ở cả compose lẫn workflow; xem `docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md`. Production vẫn dùng storage có license |
| **Fix `4830e57` chưa lên main** (28/09) | CI main có nguy cơ 28P01 với các commit deploy mới | PR lên main (nhóm trưởng duyệt) |
| Sitemap chỉ on-demand, chưa có job lịch + distributed lock | Lệch yêu cầu đề (cron 02:00 UTC); nhiều worker có thể sinh trùng | CR-7: Hangfire recurring (đã có) hoặc ghi rõ giới hạn trong sổ |

---

## 8. Việc làm ngay khi được confirm

1. ✅ Fix duplicate migration `RefreshTokens` — **đã xong** (main `a651c8a`); CI branch tuần 3 success (`818522b`).
2. ✅ Merge main mới (deploy Render/100 ảnh/UI) — đã merge + fix connection string lazy `4830e57`.
3. **PR `4830e57` lên main** (CI main 6 commit mới có thể dính 28P01).
4. ✅ Chốt D27 (PA-2 proxy) + D23 (PA-1 Hangfire) — **27/09** theo `DE_XUAT_GIAI_QUYET_D23_D27.md`; ghi ADR.
5. ✅ Archive/delete CQRS — **xong 24/09**; invalidation — **đóng bằng xác minh 27/09** (không cache recipe; handoff TV2/TV3).
6. E2E D1.3 trên MinIO + D1.1c MinIO down/log redacted.
7. ✅ **Proxy ảnh D27** (`/resources/images/{key}`) — **xong 27/09**; còn: uploader UI + status buttons (ghép TV3 C4; FE set `NEXT_PUBLIC_MEDIA_URL`).
8. Sitemap/robots/JSON-LD + OTEL/metrics/health.
9. ✅ **Resize job (Hangfire — D23 chốt)** — xong 27/09 (N4).
10. ✅ **Lab L4 (N5/D6)** — xong 28/09: commit `3642428`, 39/39 check, sổ K.
11. ✅ **Gỡ sự cố CI + dev compose** (image storage bị gỡ khỏi registry) — xong 28/09: `cd72b27` + `d78e25c`.
12. ⬜ **N5 mục 2** — lab Identity/Google/refresh/forms/FTS (**yêu cầu của tuần 3** theo `PHAN_CHIA` mục 3.4 — cần nhóm trưởng chốt làm tối giản hay ghi nhận dời tuần 4).
13. ✅ **Merge `origin/main`** vào nhánh → gỡ 7 conflict (compose theo RustFS, `Program.cs`, `Recipes.cs`, `Recipe.cs`, 3 file frontend) trong PR #16; giữ cả D3 archive/delete của TV4 lẫn soft-delete có `RowVersion` của TV3; `dotnet test` **159/159, `Skipped=0`**, `tsc`/`next build` sạch.
14. ⬜ **Uploader UI + status buttons** (D4 — sau khi merge main): thanh progress upload + nút Unpublish/Archive (Publish đã có trên main).
15. ⬜ **Trace thật vào Seq** (K20) + **sitemap job 02:00 UTC/distributed lock** (CR-7) + **bảng mapping K01**.