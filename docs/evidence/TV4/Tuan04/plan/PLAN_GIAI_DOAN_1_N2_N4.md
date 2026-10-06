# PLAN GIAI ĐOẠN 1 — LÀM CÔNG VIỆC TUẦN 4 (N2 → N3 → N4)

| Mục | Nội dung |
|---|---|
| Người lập | Nguyễn Hữu Trung Sơn (TV4 · 2312739) |
| Ngày lập | 03/10/2026 |
| Nhánh | `2312739_NHTSon_D5-D6-D7` — **không push `main`** |
| Nguồn | `KE_HOACH_TUAN_4_TV4.md` mục 3 (`N2`, `N3`, `N4`) + `KE_HOACH_TUAN_4_TV4_V2.md` |
| Trạng thái | 🟡 **Đã thực thi — đủ điều kiện chặn tiếp, còn `D1/D2/D3` + `N3-A3` + `N3-A5`** |
| Quyết định bổ sung | [`QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md`](../misc/QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md) (03/10) — B5 → **PA-A**, B6 → **PA-A**, 3 task lock hạ tầng → đề xuất 07/08/09 |
| Quy tắc phát sinh | [`PLAN_TRIEN_KHAI_TV4_TUAN4.md`](PLAN_TRIEN_KHAI_TV4_TUAN4.md) mục 2 |

---

## 0. Bối cảnh khi bắt đầu

| Việc | Trạng thái |
|---|---|
| **N0** (chốt baseline, verify lỗi 500 `/search`) | ✅ Xong |
| **N1** (health thật, OTEL+Seq, backup/restore, multi-instance, sitemap cron+lock, B1/B2/B4, no-secret) | ✅ Xong 8/8 — xem `HANDOFF_TV4_TUAN4_N1.md` |
| **N2 / N3 / N4** | ⬜ **Chưa làm** — đây là phạm vi của plan này *(lúc lập plan)* |
| Baseline đo được | build 0 warning · `dotnet test` **210/210** · `npx tsc --noEmit` 0 · `npm run build` OK |

> [!NOTE]
> **📌 Mục 0 là ảnh chụp bối cảnh lúc lập plan — cập nhật 05/10.**
>
> | Việc | Lúc lập plan (03/10) | **Thực tế 05/10** |
> |---|---|---|
> | **N2 / N3 / N4** | ⬜ Chưa làm | 🟡 **N2 7/8** (dở dang `D1/D2/D3` — progress upload, UI unpublish/archive, WCAG) · **N3 3/4** (còn `A3` Zod/RHF và `A5` Google OAuth — chờ credentials) · **N4-C xong** (runbook + deploy staging → tuần 5) |
>
> Nguồn: [`BAO_CAO_GIAI_DOAN_1_N2_N4.md`](../report/BAO_CAO_GIAI_DOAN_1_N2_N4.md) · [`TRANG_THAI_THUC_HIEN_TUAN_4.md`](../TRANG_THAI_THUC_HIEN_TUAN_4.md)

### 0.1. NFR của riêng TV4 — dùng để tự kiểm mỗi khối có phục vụ NFR không

| NFR | Nội dung rút gọn |
|---|---|
| `NFR-PERF-002` | ≥100 người dùng đồng thời; smoke → load → stress; tách bottleneck |
| `NFR-SEC-004` | FluentValidation, SQL parameterized, XSS sanitization + CSP, validate size **trước khi** buffer |
| `NFR-SEC-005` | TLS ≥ 1.2 · redirect HTTPS · HSTS · CORS explicit origins |
| `NFR-USE-004` | Skeleton/empty/toast · optimistic rollback · **upload progress %** |
| `NFR-REL-001` | Uptime ≥ 99,5% · readiness 10s · **không tuyên bố đạt SLA bằng test ngắn** |
| `NFR-REL-002` | Global exception → 500 · timeout 30s · Redis fallback · Hangfire retry |
| `NFR-SCALE-001` | Stateless JWT · Redis shared · lock singleton sitemap · **test 2 API** |
| `NFR-SCALE-003` | Container tách service · **Nginx upstream nhiều API** · ghi rõ đã triển khai hay mới thiết kế |
| `NFR-SEO-003` | Sitemap XML đủ `loc/lastmod/changefreq/priority` · cron 02:00 UTC · robots khai báo URL |

> Ô kỹ năng liên quan: **K02, K03, K04, K05, K06, K07, K09, K10, K14, K15, K16, K17, K18, K19,
> K21, K22, K23, K24** (18 ô chưa có bằng chứng đầy đủ theo `SO_EVIDENCE_TUAN_4.md`).

### 0.2. Đối chiếu phạm vi — đã lược bớt so với kế hoạch gốc

Đối chiếu 3 nguồn: `KE_HOACH_DU_AN.md` mục 4.2 + mục 8 · `PHAN_CHIA_CONG_VIEC_6_TUAN.md` mục 3.4 +
mục 6. Kết luận: plan này **chỉ giữ phần thuộc tuần 4**.

#### A. Đã LOẠI khỏi tuần 4 (thuộc tuần 5–6, không làm ở GĐ1)

| Mục | Đã loại | Thuộc tuần nào | Nguồn |
|---|---|---|---|
| **N4-A** | Runbook đầy đủ 7 mục | ⛔ Tuần **5** | 6-tuần L88: "load/SEO/**runbook**" |
| **N4-B** | Deploy lặp lại được, staging, 2 API trong prod compose, TLS/HSTS | ⛔ Tuần **5** | 6-tuần L88: "**Tự deploy/restore/test2 API**; HTTPS/CORS/volumes" |
| **N2-D3** | Checklist WCAG / responsive | ⛔ Tuần **5**, thuộc **TV2** | 6-tuần L257: TV2 "responsive/a11y" |
| **N2-B1** | 5 luồng E2E đầy đủ (register/login/category/draft) | 🟡 Phần của TV4 xong 04/10: **publish 4 test** qua wizard 5 bước + search 12 → **26/26**, 3 lần xanh | Còn **G6 tuần 5** (`register/login` của TV1, `category` của TV2, `create-recipe` của TV3) | 6-tuần mục 6: "Cuối tuần 5 … 5 critical E2E pass" |
| **N2-A3** | Dựng Jest/RTL cho frontend | ⛔ Thuộc **TV1** | 6-tuần L257: TV1 "security/frontend tests" |
| **N2-C5** | EXPLAIN lại toàn bộ | ⛔ Chỉ làm **nếu index đổi** | D5 đã xong tuần 3 (PR #16) |

> **Hai tài liệu gốc có mâu thuẫn về `multi-instance`** — đã xử lý như sau:
> `KE_HOACH_DU_AN.md` L257 xếp `multi-instance` vào **tuần 4**; `PHAN_CHIA_CONG_VIEC_6_TUAN.md` L88 xếp
> `test 2 API` vào **tuần 5**. Căn cứ thực tế: **N1 đã kiểm chứng 2 API trên dev stack** (xem handoff) →
> tuần 4 coi như đạt. Phần **tuần 5** là lặp lại trên **staging** sau khi deploy → nằm ở N4-B đã loại.

#### B. Đã BỔ SUNG — tuần 4 giao nhưng plan ban đầu bỏ sót

| Mục | Việc bổ sung | Nguồn |
|---|---|---|
| **N2-E** (mới) | **retry / race / security** — resize retry idempotent, delete-vs-resize không tái sinh ảnh đã xoá, 2 request đặt primary đồng thời, xoá file không tồn tại idempotent, không xoá object ngoài bucket/prefix | 6-tuần L87 "retry/race/security"; `KE_HOACH_DU_AN.md` L329, L337 |
| **N2-C1** (bổ sung) | **shared cache / multi-worker** — nhiều worker tranh cùng job sitemap phải có distributed lock | 6-tuần L87; `KE_HOACH_DU_AN.md` L339 |
| **N2-C1** (bổ sung) | Job sitemap retry **2 lần**; resize/delete retry **3 lần** | `KE_HOACH_DU_AN.md` L339 |

#### C. Còn lại trong phạm vi tuần 4 — hợp lệ

`N2-A1/A2` (Playwright + cổng CI, cần để có E2E) · `N2-B2/B3/B4` (publish E2E, file attack, quyền upload) ·
`N2-C2/C3/C4/C6` (Redis fallback, commit k6 script, p50/p95/p99, ngưỡng coverage 80% = cổng G5) ·
`N2-D1/D2` (progress, unpublish/archive — D4 dở dang từ tuần 3) · toàn bộ `N3` ("Hoàn tất LAB") ·
`N4-C` (tài liệu + evidence, G4 yêu cầu reviewer Tâm xác nhận).

---

## 1. N2 — D7: E2E, resilience, tấn công file, cổng CI frontend

**Kỹ năng**: K02, K03, K04, K06, K10, K16, K17, K18, K21, K24
**Phụ thuộc**: N1-1 (health), N1-3 (observability) — *đã xong*
**NFR**: `NFR-SEC-004`, `NFR-REL-002`, `NFR-PERF-002`, `NFR-USE-004`, `NFR-MAINT-002`

### N2-A — Hạ tầng test frontend + cổng CI *(làm trước, chặn 4 ô K)*

| # | Việc | Cách làm | Bằng chứng | K/NFR |
|---|---|---|---|---|
| A1 | Dựng Playwright thật | `playwright.config.ts` + `npm i -D @playwright/test` + script `test` trong `package.json` | `npx playwright test --list` ra danh sách luồng | K21, K16 |
| A2 | **Cổng CI frontend** | Workflow (mới hoặc thêm bước): `npm ci` → `tsc --noEmit` → `next build` → `next lint` | CI xanh; **cố tình làm hỏng 1 lần để chứng minh cổng chặn được** | K16, K24 |
| A3 | Test frontend (Jest/RTL) | ⛔ **LOẠI khỏi tuần 4** — thuộc TV1 (6-tuần L257). Chỉ dùng Playwright cho N2-B | — | (TV1) |

> ⚠️ **Đây là nguyên nhân gốc lỗi `500` toàn bộ trang `/search` lọt vào `main`** — không có cổng CI nào
> build frontend, route dynamic không prerender. Làm A2 **trước** các phần E2E khác.
> Bằng chứng gốc: `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md`.

### N2-B — Luồng E2E + kịch bản tấn công file

| # | Việc | Cách làm | Bằng chứng | K/NFR |
|---|---|---|---|---|
| B1 | **2 luồng của riêng TV4** | ✅ 04/10: **publish** 4/4 + **search** 12/12 → **26/26** (`--retries=0`, 3 lần xanh; `repeat-each=4` 16/16). 🟢 Tìm ra lỗi thật: wizard mất bước sau lần lưu đầu (URL đổi route → remount về `step: 0`) — đã sửa + khoá bằng B1-3. 5 luồng đầy đủ là cổng **G6 tuần 5**; `register/login` (TV1), `category` (TV2), `create-recipe` (TV3) do thành viên khác viết | 2 luồng chạy được, dùng **tài khoản seed** | K21 |
| B2 | `search` E2E = tự động hoá TC1–TC12 | Viết lại đúng 12 test case của báo cáo lỗi 500 | Bảng TC1–TC12 → trạng thái pass/fail | K21, K16 |
| B3 | **Kịch bản tấn công file** (E2E, không chỉ unit) | File > 5 MiB · đổi tên `.exe` → `.jpg` (MIME giả) · file rỗng/hỏng · nhiều request upload liên tiêp | Từ chối đúng mã `FILE_SIZE_EXCEEDED` / `FILE_MIME_INVALID`, **không lọt 500** | K02, K10, `NFR-SEC-004` |
| B4 | Quyền upload (khách/người lạ) | Test Guest và user khác role | 401/403 đúng mã | K10 |

> **Trạng thái 04/10: B3 ✅ 7/7, B4 ✅ 3/3** (`src/frontend/e2e/upload-security.spec.ts`, upload thật lên MinIO).
>
> ⚠️ **Cặp mã lỗi trong bảng trên là bản nháp, không khớp code thật.** API hiện trả:
> `file.too_large` · `file.invalid_type` · `file.empty` · và mới thêm **`file.too_small`**
> (do B3 phát hiện: JPEG 3 byte khớp trọn chữ ký nên được nhận `201` → thêm `ImageFormats.MinBytes = 64`).
> Cần sửa cột "Cách làm" cho khớp thực tế khi chốt báo cáo.
>
> 🟢 B3 còn phát hiện lỗi trong **hạ tầng test**: `firstCategoryId()` dò danh mục không giới hạn →
> 429 bị hiểu nhầm thành "danh mục hỏng" → 10/10 `skipped` (xanh giả). Đã giới hạn 12 lần dò và ném lỗi 429.

### N2-C — Đo đạc: resilience, k6, EXPLAIN, coverage

| # | Việc | Cách làm | Bằng chứng | K/NFR |
|---|---|---|---|---|
| C1 | **Resilience có số liệu** | Dừng lần lượt DB / Redis / S3 / worker; đo: request lỗi trả gì, có retry không, phục hồi sau bao lâu | Bảng số failover/retry trong `SO_EVIDENCE_TUAN_4.md` + log | K22, K24, `NFR-REL-002` |
| C1b | **Shared cache / multi-worker** | 2 worker cùng chạy sitemap → phải có distributed lock, **không** tạo trùng; cache dùng chung giữa 2 API | Log 2 worker, sitemap chỉ ghi 1 lần | K14, K23, `NFR-SCALE-001` |
| C1c | **Số lần retry đúng quy định** | sitemap retry **2** lần · resize/delete retry **3** lần · welcome **3** lần (lịch 1/5/30 phút) | `BackgroundJobRetryContractTests` 4/4 + `WelcomeEmailWorker.RetryDelays` | K14, K24 |
| C2 | Chứng minh fallback khi tắt Redis | Tắt Redis, gọi endpoint cache | Trả dữ liệu đúng, **không 500** — đọc 200 qua fallback in-process, `/health/ready` 503 | K12, K24, `NFR-REL-002` |
| C3 | **k6 script commit được** | Chuyển từ heredoc inline sang `tests/performance/` | `tests/performance/read-load.js` + `README.md` (runbook) | K22, `NFR-PERF-002` |
| C4 | Đo lại k6 có p50/p95/p99 | Chạy lại script đã commit | `logs/k6_c4_run{1,2,3}.log` — 3 lần × ~3607 req, 0% lỗi, p50/p95/p99 theo từng nhánh + hardware/cache state | K22 |
| C5 | EXPLAIN lại *(có điều kiện)* | ⛔ D5 đã xong tuần 3 (PR #16) → **chỉ chạy lại nếu GĐ1 có đổi index**. Không đổi index thì bỏ | `logs/explain_*.txt` | K06, `NFR-PERF-004` |
| C6 | **Coverage có ngưỡng** | Đọc `coverage.cobertura.xml` trong CI, đặt ngưỡng line ≥ 80% cho `CulinaryBlog.Application` | CI fail khi hạ ngưỡng · số liệu thật báo cáo | K21, `NFR-MAINT-002` |

### N2-D — D4-UI còn treo từ tuần 3

| # | Việc | Cách làm | Bằng chứng | K/NFR |
|---|---|---|---|---|
| D1 | Thanh **progress % upload** | API upload đã có sẵn | Screenshot hoặc log progress | K17, `NFR-USE-004` |
| D2 | Nút **Unpublish/Archive** | API đã có sẵn | Test click → trạng thái đúng | K17, K19 |
| D3 | Checklist WCAG | ⛔ **LOẠI khỏi tuần 4** — `responsive/a11y` là việc của **TV2 tuần 5** (6-tuần L257). Chỉ làm nếu TV2 không nhận | (TV2) | K18, `NFR-USE-002` |

### N2-E — retry / race / security *(bổ sung: tuần 4 giao nhưng plan cũ bỏ sót)*

**Kỹ năng**: K13, K14, K10, K07
**Nguồn**: 6-tuần L87 `retry/race/security/publish E2E` · `KE_HOACH_DU_AN.md` L329 + L337

| # | Việc | Cách làm | Bằng chứng | K |
|---|---|---|---|---|
| E1 | **Delete vs resize không tái sinh ảnh đã xoá** | Xoá ảnh khi job resize đang chạy | Ảnh không xuất hiện lại sau khi job xong | K13 |
| E2 | **Resize retry idempotent** | Cho job fail giữa chừng rồi retry | Không tạo object trùng, không xoá object ngoài bucket/prefix | K14, K13 |
| E3 | **2 request đặt primary đồng thời** | Gửi song song 2 PATCH primary | Chỉ 1 ảnh là primary | K07 |
| E4 | **Primary lần đầu và khi xoá primary** | Xoá ảnh primary → phải có ảnh primary mới | DB đúng | K07 |
| E5 | **Xoá file không tồn tại = idempotent** | DELETE 2 lần cùng 1 imageId | Lần 2 trả 204, không 500 | K13 |
| E6 | **Magic bytes WebP/AVIF** | Kiểm tra đủ magic bytes, **không chỉ 4 byte đầu** | Test đỏ khi cắt ngắn magic bytes | K10 |
| E7 | **Dashboard `/hangfire` chỉ Admin** | Gọi bằng token non-Admin | 401/403 | K14 |

> ✅ **Phụ thuộc chéo đã gỡ (03/10):** các test này cần **tài khoản Admin**. **B6 đã được quyết**
> theo phương án A — CLI `--promote-admin` xem
> [`QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md`](../misc/QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md) §2. Làm **B6 trước**,
> sau đó E3/E4/E7 chạy được. ⛔ Vẫn **không** tự tạo admin để làm xanh: phải nâng quyền tài khoản
> thật bằng lệnh đã chốt, rồi mới chạy test.

> **Ghi chú phụ thuộc chéo:** lab đã phát hiện **rate limit nhân theo số instance** và
> **`UseForwardedHeaders` còn thiếu**. Hai điểm này ảnh hưởng N2-C1 (đo resilience qua nhiều instance).
> Nếu GĐ3 chưa sửa thì **phải ghi rõ hạn chế** khi đo, không báo cáo số như 2 instance.

---

## 2. N3 — D6: Lab L5 + bù mục 2 của L4

**Kỹ năng**: K04, K05, K09, K15, K19
**Điều kiện**: nhánh `practice/TV4/L5`, DB/schema/bucket prefix riêng
**Quy ước LAB**: code lab **không merge** trùng với `main`

### N3-A — Bù mục 2 của L4

| # | Việc | Ghi chú quan trọng |
|---|---|---|
| A1 | Identity/PBKDF2 hash + verify | Dùng số iteration thật, ghi rõ |
| A2 | Refresh token: hash, rotation, reuse detection, logout | K08 |
| A3 | Form: Zod/RHF phía FE, FluentValidation phía BE | K05 |
| A4 | FTS: tsvector, trigger, index, `ts_rank`, AND, phân trang | K11 |
| A5 | Google OAuth2/PKCE | ⛔ **Thiếu credentials thì ghi rõ "còn chờ", KHÔNG coi mock là hoàn thành** |

### N3-B — Lab L5

| # | Phase | Nội dung |
|---|---|---|
| B1 | `search-ssr` | SSR search |
| B2 | `isr-detail` | ISR published detail |
| B3 | `query-rollback` | TanStack Query optimistic + rollback |
| B4 | `image-opt` | `next/image` |
| B5 | `seo` | metadata / JSON-LD / robots / redirect |
| B6 | `observability` | Serilog / OTEL / metrics / health |
| B7 | `multi-instance` | 2 API + shared cache/jobs |

> Mỗi phase có **check thật đếm được**, giống cách L4 chấm `39/39`, để reviewer đối chiếu được.

### N3-C — Sổ K + PR

| # | Việc |
|---|---|
| C1 | `Tuan04/report/SOK_LAB_L5.md` — kỹ thuật con nào có/không, giới hạn, lỗi gặp |
| C2 | Mở **PR cho nhánh lab** (`practice/TV4/L5`) — hiện L4 mới chỉ có branch, chưa có PR |
| C3 | Đính chính `SO_EVIDENCE_TUAN_4.md`: ô K chỉ tính khi có **code + test + log** |

---

## 3. N4 — D7: Bàn giao tài liệu + chốt evidence *(đã lược bớt)*

**Kỹ năng**: K01, K23, K24
**NFR**: `NFR-REL-001`, `NFR-SCALE-003`, `NFR-MAINT-003`

> ⛔ **Đã loại khỏi tuần 4**: runbook đầy đủ và deploy staging → **thuộc tuần 5**
> (6-tuần L88: "Tự deploy/restore/test2 API; HTTPS/CORS/volumes, load/SEO/**runbook**").
> Tuần 4 chỉ làm **N4-C** để đủ điều kiện cổng G4.

### N4-A — Runbook `docs/RUNBOOK.md` ⛔ CHUYỂN SANG TUẦN 5

| # | Mục | Yêu cầu |
|---|---|---|
| A1 | Dựng stack từ checkout sạch | Lệnh thật, đã chạy được |
| A2 | Backup / restore | Có **số liệu thật** từ drill |
| A3 | Failover từng dependency | Dùng số đo của N2-C1 |
| A4 | 2 API instance | Có log `multi_instance_two_api.log` |
| A5 | Lệnh k6 | Trỏ đúng script đã commit ở N2-C3 |
| A6 | Tìm log/trace của một request vừa thao tác | Theo TraceId |
| A7 | Cách tìm log Seq | Screenshot hoặc lệnh truy vấn |

> ⛔ **Không** có mục nào ghi "nên làm". Mục nào chưa đo được thì ghi "**chưa đo**" kèm lý do.
>
> 📌 **Tuần 4 chỉ làm phần khung**: tạo file + 2 mục đã có số liệu từ N1 (A2, A4). A1/A3/A5/A6/A7
> để nguyên cho tuần 5 — vì chúng cần số đo của N2-C và số đo staging.

### N4-B — Deploy lặp lại được ⛔ CHUYỂN SANG TUẦN 5

> ⛔ Toàn bộ khối này thuộc **tuần 5** (6-tuần L88). Giữ lại để tham chiếu, **không thực hiện ở GĐ1**.

| # | Việc | Ghi chú |
|---|---|---|
| B1 | `docker-compose.prod.yml` **hoặc** checklist Render | Chọn một, tùy quyết định #8/#9 trong plan tổng |
| B2 | Chạy **2 lần từ sạch** để chứng minh idempotent | Log 2 lần chạy |
| B3 | 2 API instance qua Nginx trong Compose | ⛔ Cần `profiles:` — lab đã xác nhận hiện chưa có |
| B4 | TLS + HSTS + redirect HTTPS | ⛔ Cần chứng thư; lab đã xác nhận hiện chỉ nằm trong comment |
| B5 | Ghi rõ **giới hạn hạ tầng** | Không ngụ ý đã đạt SLA (`NFR-REL-001`) |

### N4-C — Tài liệu + chốt evidence

| # | Việc |
|---|---|
| C1 | Cập nhật `docs/evidence/TV4/HUONG_DAN_CHAY_TV4.md` |
| C2 | Cập nhật `README.md` mục test/ops — **dùng số đo mới, không dùng số tuần trước** |
| C3 | Cập nhật `CHANGELOG.md` |
| C4 | Chốt `SO_EVIDENCE_TUAN_4.md` + `TRANG_THAI_THUC_HIEN_TUAN_4.md` |
| C5 | Nộp review Tâm — **ghi rõ ô nào còn thiếu**, không làm tròn số |

---

## 4. Thứ tự thực hiện đề xuất

```
B5 (7 task) ──▶ B6 (4 task) ──▶ QD3-3a/3b/3c      ⭐ chèn trước N2 (03/10)
   (ảnh Draft)     (Admin CLI)
        │
        ▼
N2-A  ──▶  N2-B  ──▶  N2-E  ──▶  N2-C  ──▶  N2-D
            (A chặn B; E độc lập với A, nhưng E cần B6 xong)
                          │
                          ▼
                 N3-A ──▶ N3-B ──▶ N3-C
                          (L5 song song được với N2 vì khác nhánh)
                                              │
                                              ▼
                                    N4-C   (N4-A/B ⛔ để tuần 5)
```

> ⭐ **Vì sao B5/B6 chèn trước N2** (quyết định 03/10, xem
> [`QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md`](../misc/QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md)):
> **B6** mở khoá **N2-E7**; **B5** là điều kiện để xem ảnh Draft trong wizard (N2-D1/D2) chạy được
> với dữ liệu thật. Nếu làm N2 trước thì các test này phải chờ hoặc bỏ, rồi làm lại.

> N3 có thể làm **song song** với N2 (khác nhánh, khác miền), nhưng theo thứ tự bàn giao vẫn là
> B5/B6 → N2 → N3 → N4. Không mở đồng thời quá 2 khối để tránh conflict `Program.cs`, compose, CI.
>
> **Ưu tiên khi thiếu thời gian** — cắt theo thứ tự sau, vì đây là thứ tuần 5/6 giao lại:
> 1. `N2-D1/D2` (UI progress/status — D4 dở dang)
> 2. `N2-C5` (EXPLAIN — có điều kiện, bỏ được)
> 3. `N3-C2` (PR lab — L4 cũng chưa có PR, nên để tuần 5)
> 4. `N2-E6/E7` (magic bytes AVIF, `/hangfire` Admin)
>
> ⛔ **Không** cắt `B5`/`B6`: chúng là **điều kiện tiên quyết**, bỏ thì N2-D1/D2 và N2-E7 phải làm
> lại. Nếu thiếu thời gian thì **dời** N3-B xuống tuần 5 trước.

---

## 5. Sổ ghi phát sinh (điền khi làm, không bỏ trống)

> Copy bảng này vào `TRANG_THAI_THUC_HIEN_TUAN_4.md` hoặc cuối báo cáo GĐ1. **Mỗi phát sinh 1 dòng.**

| # | Ngày | Khối | Mô tả | Mức | Loại | Đi đâu | Trạng thái |
|---|---|---|---|---|---|---|---|
| | | | | S1/S2/S3 | T1/T2/T3 | Báo lỗi / Handoff / Câu hỏi / Sửa ngay / File riêng | |

### 5.1. Câu hỏi cần TV1 trả lời (mở tại thời điểm xác định mức độ)

| # | Câu hỏi | Mở khi nào | Ảnh hưởng |
|---|---|---|---|
| | | | |

### 5.2. Block / việc chờ nhóm — cập nhật 03/10 theo thực tế đã commit

> ⚠️ **Đính chính so với bản 30/09:** B1, B2, B4 **đã có code và test trên nhánh này** rồi, chỉ còn
> chờ reviewer duyệt — **không** phải "chờ quyết định mới làm". Chi tiết ở `docs/proposal/DE_XUAT_0*`.

| Mã | Nội dung | Trạng thái thật | Cần ai | Từ ngày |
|---|---|---|---|---|
| B1 | `500` → `503 storage.unavailable` | ✅ Đã triển khai `9e786e7` — chờ duyệt | TV1 duyệt (`#20`) | 30/09 |
| B2 | Fail-fast khi thiếu cấu hình | ✅ Đã triển khai `9e786e7` — chờ duyệt | TV1 duyệt (`#21`) | 30/09 |
| B3 | Nạp `.env` | 🟢 **Đã xong** — PR #19 | — | 30/09 |
| B4 | `/health/ready` gồm probe credential | ✅ Đã triển khai `a1311fa` — chờ duyệt | TV1 xác nhận (`#22`) | 30/09 |
| **B5** | Xem ảnh Draft trong wizard | ✅ **Đã quyết PA-A** (presigned URL ≤ 10 phút) — TV4 tự làm **toàn bộ**, kể cả `ImagesStep.tsx` của TV3 · **B5-1…B5-7 xong**, `dotnet test` 265/265 xanh, không rò URL ký ở response Published | **TV4 tự thực hiện** — ✅ hoàn tất | 03/10 |
| **B6** | Tài khoản Admin | ✅ **Đã quyết PA-A** (CLI `--promote-admin`, không seed mật khẩu cứng) — mở khoá N2-E7 · **B6-1…B6-4 xong**, kiểm chứng tay từ chối ở `Testing`/`Production`, idempotent | **TV4 tự thực hiện** — ✅ hoàn tất | 03/10 |
| — | Nơi đặt lịch backup (cron host vs CI) | 🔴 **Đề xuất 07** — cron `0 20 * * *` đã có; GitHub có thể bỏ qua job → cần chốt nơi chính | Tâm chốt | 30/09 |
| — | Kho lưu backup 30 ngày | 🔴 **Đề xuất 08** — artifact GitHub chỉ giữ **7 ngày** ⇒ `BACKUP_KEEP_DAYS=30` chỉ dọn trên runner, **chưa** đạt | Tâm + TV2 | 30/09 |
| — | Rotate khoá JWT thật đã lộ trong git history | 🔴 **Đề xuất 09** — ngoài repo; **3a/3b/3c trong repo đã xong** (`JwtService` từ chối khoá dev đã thu hồi + `JwtSigningKeyNotCommittedTests` chặn tái phát + tài liệu sinh khoá ngẫu nhiên), ⛔ **không** tự rotate/xoá history | Tâm | 03/10 |

> ⛔ **Không tự quyết 3 việc trên** vì đều nằm ngoài hạ tầng dự án: không kiểm chứng bằng test trong
> repo, có chi phí thật. Trong lúc chờ, **ghi đúng giới hạn thật** (7 ngày, không chờ chốt nơi đặt)
> — không ghi "đã đạt". Chi tiết phương án và test case: `docs/proposal/DE_XUAT_07/08/09`.

---

## 6. Điều kiện hoàn thành GĐ1

- [ ] 9 khối **được giao tuần 4** đều có kết quả ghi rõ: `N2-A1/A2`, `N2-B2/B3/B4`, `N2-C1/C1b/C1c/C2/C3/C4/C6`, `N2-D1/D2`, `N2-E`, `N3-A/B/C`, `N4-C`
- [x] ⭐ **B5-1…B5-7** xong; `docs/IMAGE_CONTRACT.md` §3 + §5 mô tả PA-3 presigned (hạn ≤ 10 phút, bảng quyền,
      `Referrer-Policy: no-referrer`, cấm log query string) — sửa **cùng PR** với code B5
- [x] ⭐ **B6-1…B6-4** xong; `--promote-admin` chạy được và bị **từ chối** ở `Testing`
- [ ] ⭐ **TV3 đã được thông báo** về B5-6/B5-7 (ghi trong PR hoặc handoff) — báo để họ biết, **không** chờ trả lời
- [ ] ⭐ **N2-E7** chạy được; **QD3-3a/3b/3c** xong (3a/3b/3c ✅ — N2-E7 ✅ một phần: `/hangfire` không lộ
      ra; phần "Author 403 / Admin 200" cần chạy ở `Development` vì Hangfire không bật trong `Testing`)
- [x] ⛔ Kiểm tra rò rỉ: presigned **không** xuất hiện trong response public của recipe Published; log không chứa `X-Amz-Signature`
- [ ] Các mục **đã loại** (N2-A3, N2-B1 phần 5 luồng, N2-D3, N4-A đầy đủ, N4-B) được ghi vào báo cáo là
      **chuyển tuần 5**, không tính là "chưa làm"
- [ ] Mọi phát sinh đã vào mục 5 hoặc đi đúng nơi theo ma trận T1/T2/T3
- [ ] Không ô K nào đánh ✅ khi thiếu bằng chứng
- [ ] Build 0 warning · `dotnet format --verify-no-changes` exit 0 · `dotnet test` xanh (`Skipped=0`) ·
      `npx tsc --noEmit` exit 0 · `npm run build` xanh · `deploy/scan-secrets.sh` exit 0
      *(đã ✅ build/format/`dotnet test` 265/265 `Skipped=0`/tsc/Next — còn `scan-secrets.sh` chưa chạy được
      vì máy không có bash/WSL; quét tay bằng `git grep` đã sạch)*
- [ ] `SO_EVIDENCE_TUAN_4.md` + `TRANG_THAI_THUC_HIEN_TUAN_4.md` cập nhật
- [ ] Phần khung runbook (N4-A: A2 + A4) có số liệu thật, **không** có mục "nên làm"
- [ ] Báo cáo `BAO_CAO_GIAI_DOAN_1_N2_N4.md` viết xong
- [ ] ⭐ Ghi rõ giới hạn còn treo trong báo cáo: backup **7 ngày** (⛔ chưa đạt 30), lịch backup
      **chưa** có job canh — kèm link đề xuất 07/08.
      *(cập nhật: khoá JWT **đã** rotate ngoài repo và **đã** thu hồi khoá dev từng bị commit, xem đề xuất 09;
      khoá còn nằm trong lịch sử git nên xoá history vẫn là việc của Tâm/TV2)*

---

## 7. Phần chuyển sang tuần 5 — bàn giao lại

Ghi nguyên danh sách này vào cuối báo cáo GĐ1 để tuần 5 không phải dò lại:

| Mục | Nội dung | Nguồn giao tuần 5 |
|---|---|---|
| Runbook đầy đủ | A1, A3, A5, A6, A7 | 6-tuần L88 |
| Deploy staging | `docker-compose.prod.yml`, chạy 2 lần từ sạch | 6-tuần L88 |
| 2 API trên staging | Cần thêm `profiles:` vào compose | 6-tuần L88 |
| HTTPS/CORS/volumes | TLS + HSTS + redirect | 6-tuần L88 |
| 5 E2E flows | register · login · category · draft · publish | 6-tuần mục 6 (G6) |
| Responsive/a11y | Checklist WCAG — **TV2** | 6-tuần L257 |
| Frontend unit test | Jest/RTL — **TV1** | 6-tuần L257 |
| Coverage ≥ 80% thật | Số đo line coverage chưa có; mục C6 chỉ đặt ngưỡng | 6-tuần L188 |
