# KẾ HOẠCH TUẦN 4 — BẢN V2 (TV4 · Nguyễn Hữu Trung Sơn · 2312739)

> **Bản V2** = kế hoạch viết lại, lấy căn cứ là **báo cáo tự rà soát 3 tuần**
> (`docs/evidence/TV4/Report/BAO_CAO_TAM_RA_SOAT_TONG_HOP_3_TUAN.md`) **đối chiếu lại với repo thật**.
>
> **Bản gốc [`KE_HOACH_TUAN_4_TV4.md`](misc/KE_HOACH_TUAN_4_TV4.md) giữ nguyên, không sửa** — dùng để đối chiếu
> xem V2 đã đổi gì và vì sao. Hai bản cùng hiệu lực cho tới khi Tâm chốt chọn một.
>
> - **Ngày lập V2**: 03/10/2026 · **Baseline đo**: `origin/main` = `3d0695d`, nhánh `2312739_NHTSon_D5-D6-D7` = `55b4c2b`
> - **SRS**: v1.1.1 · **Reviewer**: Nguyễn Thanh Tâm · **Cổng**: G4 giữa tuần · G5 cuối tuần (06/10)
> - **Đã xong trước khi lập V2**: N0 (6/6) + N1 (8/8) — xem `HANDOFF_TV4_TUAN4_N1.md`
>
> **Nguyên tắc của V2**
> 1. Chỉ việc **của TV4**. Việc của thành viên khác nêu ở mục 7, **không tính** vào tiến độ tuần 4.
> 2. **Lỗi / bug không nằm trong kế hoạch này.** Mọi lỗi ghi ở [`BAO_CAO_LOI_TUAN_4_TV4.md`](report/BAO_CAO_LOI_TUAN_4_TV4.md).
> 3. Mỗi "đã làm" đều kèm `file:line` hoặc log. Không có "nên làm".
> 4. Ô kỹ năng chỉ chuyển ✅ khi **có code/config + test + kết quả thật + Tâm xác nhận và ghi ngày**.

> [!NOTE]
> **📌 Kế hoạch này đã được thực thi — đọc mục này trước khi dùng các tiêu đề "chưa làm" bên dưới.**
>
> Tài liệu này là **ảnh chụp kế hoạch lúc lập 03/10**. Tiêu đề mục §4.1/§4.2/§4.3 còn ghi
> *"chưa làm"* là **đúng tại thời điểm lập kế hoạch**, không phải trạng thái hiện tại.
>
> | Mục | Kế hoạch (03/10) | **Thực tế (05/10)** |
> |---|---|---|
> | §4.1 N2 — 8 việc | 8 việc chưa làm | 🟢 **7/8 xong** (Playwright **26/26**, CI frontend, tấn công file **25/25 + 10/10**, outage drill, k6 **0.00%**, coverage gate **84.13%**). ⬜ Dở dang **1**: `D1/D2/D3` — progress upload, UI unpublish/archive, WCAG |
> | §4.2 N3 — 4 việc | 4 việc chưa làm | 🟢 **3/4 xong** (Lab L5 **63 check / 3 phase PASS**, `SOK_LAB_L5.md`, PR #28). ⬜ Còn: bù mục 2 L4; N3-A3 (Zod/RHF), N3-A5 (Google OAuth — chờ credentials) |
> | §4.3 N4 — 4 việc | 4 việc chưa làm | 🟢 **N4-C xong** (HUONG_DAN, README, CHANGELOG, sổ 24 ô). ⛔ Còn: runbook đầy đủ, deploy staging, TLS/HSTS → tuần 5 |
>
> Nguồn sự thật hiện tại: [`BAO_CAO_GIAI_DOAN_1_N2_N4.md`](report/BAO_CAO_GIAI_DOAN_1_N2_N4.md) và
> [`TRANG_THAI_THUC_HIEN_TUAN_4.md`](TRANG_THAI_THUC_HIEN_TUAN_4.md).

---

## 🧪 CẬP NHẬT SAU KIỂM CHỨNG THỰC TẾ (03/10/2026)

Kế hoạch V2 ban đầu lập **chỉ bằng đọc code**. Sau đó đã chạy thật trên bản kiểm tra cục bộ của TV4
(local, không push, không thuộc dự án; 2 API instance + Nginx + PostgreSQL/Redis thật). Bằng chứng: [`BAO_CAO_LAB_TUAN4_V2.md`](report/BAO_CAO_LAB_TUAN4_V2.md).

**Việc nào trong kế hoạch đã được chứng minh bằng chạy thật:**

| Hạng mục | Kết quả kiểm chứng |
|---|---|
| **BUG-W4-01** (`422` khi thêm ảnh) | 🔴 Tái hiện được trên PostgreSQL thật; sửa 1 dòng ở `RecipeImageConfiguration.cs` — **07/10: đã vào `main`** (PR #29); số `214/214` không dùng (test hồi quy không có trong repo) |
| **BUG-W4-03** (rate limit chung bucket) | 🔴 Tái hiện rõ; sửa bằng `UseForwardedHeaders`; xác minh mỗi IP có bucket riêng — bản vá **chưa vào repo** → sửa trực tiếp tuần 5 (W5-10) |
| **BUG-W4-02** (scanner bỏ sót secret) | 🔴 Scanner cũ **bỏ sót** JWT key và secret literal; scanner mới bắt đúng 2 vị trí — bản vá **chưa vào repo** → sửa trực tiếp tuần 5 (W5-10) |
| **A3** (multi-instance + failover) | 🟠 Round-robin, failover, trả `502` khi cả 2 node chết, tự phục hồi sau `fail_timeout` — **đều đạt**. **TLS chưa bật** |
| **A6** (tài liệu) | 🟠 Số test thực đo: **`210/210`** (205 + 5). `CHANGELOG.md` bổ sung Tuần 2–4 |
| **A5** (`docker compose --profile`) | 🔴 **Chưa đạt** — compose không có `profiles:`, `container_name` cứng, **không có API service** |
| **A2** (code chết) | 🟡 Một phần — `Category.MarkDeleted` chỉ dùng trong test; **soft delete của `Recipe` đang chạy thật, không xoá** |

**Hai điểm phải đưa vào kế hoạch vì phát hiện mới khi chạy thật:**

1. 🔴 **Rate limit phân tán chưa đáp ứng NFR.** Bộ đếm in-memory **trên từng tiến trình** ⇒ hạn mức thực
   tế = `10 × số instance`. Qua Nginx với 2 node, 12 request cùng IP đều `401`. Cần quyết định: chấp nhận
   nhân hạn mức theo số node, hay đưa limiter ra Redis.
2. 🟡 **Cột `24/24 (K01–K24)` là số tự khai.** Vòng kiểm chứng chỉ xác nhận được build sạch, `210/210` test,
   `nginx -t` hợp lệ, scanner exit 0 — **không** có rubric máy đối chiếu. Cần Tâm đối chiếu rubric
   chính thức trước khi dùng làm căn cứ chấm điểm. Vì vậy nguyên tắc 4 ở trên **giữ nguyên**: ô kỹ năng
   chưa đủ căn cứ để chuyển ✅.

> **Trạng thái tích hợp (rà lại 07/10):** `BUG-W4-01` **đã vào `main`** (PR #29 `c624b9f`); `BUG-W4-02/03`
> **chưa có bản vá trong dự án** → sửa trực tiếp tuần 5 (W5-10). Con số `210/210` là baseline của **nhánh gốc**;
> `214/214` là số đo bản cục bộ (test hồi quy kèm theo không có trong repo) — không dùng.
> Vì nguyên tắc 4, các ô kỹ năng **giữ nguyên trạng thái** cho tới khi Tâm xác nhận.

---> [!IMPORTANT]
> **Vì sao cần bản V2** — báo cáo 3 tuần tìm ra 6 chỗ bản gốc lệch:
> (1) danh sách FR/NFR của TV4 lấy từ `KE_HOACH_DU_AN.md` nên **thừa `NFR-PERF-002`, thiếu `NFR-SCALE-003` + `NFR-MAINT-003`**;
> (2) mốc kỹ năng `9/24` của `PHAN_CHIA` **không khớp** bảng 24 ô trong `SO_EVIDENCE_TUAN_3.md:17-35` (báo cáo 3 tuần đếm được 15 ô mang dấu ✅, bảng này lên `main`);
> (3) bản gốc **hạ** N1 xuống "giống cũ" ở một số chỗ, trong khi N1 đã đo được thật (trace Seq, drill 14 bảng, 2 tiến trình API);
> (4) bản gốc **bỏ sót 5 hạng mục thuộc TV4** mà báo cáo 3 tuần chỉ ra (mục 5);
> (5) bản gốc **đặt mục tiêu 24/24 ô K** trong khi chính báo cáo 3 tuần chứng minh phần lớn ô thuộc người khác;
> (6) bản gốc **không có bước mở PR** cho nhánh tuần 4, dù DoD (`KE_HOACH_DU_AN.md` 12.3) yêu cầu "có PR được review".

---

## 1. Phạm vi: đúng 21 mã FR/NFR của TV4 (đã đính chính)

Theo SRS v1.1.1 — **11 FR + 10 NFR**:

| Nhóm | Mã |
|---|---|
| **Recipe vòng đời (D3)** | `FR-RCP-005` `FR-RCP-006` `FR-RCP-007` `FR-RCP-008` |
| **Media (D1)** | `FR-FILE-001` `FR-FILE-002` |
| **Jobs (D2)** | `FR-JOB-002` `FR-JOB-003` |
| **Observability (D5)** | `FR-OBS-001` `FR-OBS-003` |
| **Auth (D3)** | `FR-AUTH-005` |
| **Bảo mật** | `NFR-SEC-004` `NFR-SEC-005` |
| **Độ tin cậy** | `NFR-REL-001` `NFR-REL-002` `NFR-REL-003` |
| **Sử dụng** | `NFR-USE-004` |
| **Mở rộng** | `NFR-SCALE-001` `NFR-SCALE-003` |
| **SEO** | `NFR-SEO-003` |
| **Bảo trì** | `NFR-MAINT-003` |

> **Đính chính so với báo cáo 3 tuần mục 6.1a**: bản đầu lấy danh sách từ `KE_HOACH_DU_AN.md` nên **thừa
> `NFR-PERF-002`** (đã thuộc TV2) và **thiếu `NFR-SCALE-003` + `NFR-MAINT-003`**. Bản V2 dùng danh sách này.
>
> **Câu hỏi cần Tâm chốt (mục 10, Q1)**: `NFR-SEC-007` (*không commit secret + có secret scan*) **không** nằm
> trong 21 mã trên, nhưng `KE_HOACH_DU_AN.md` giao "bỏ secret hardcode + secret scan CI" cho TV4 và TV4 đã
> làm (N1-8). V2 vẫn giữ phần việc đó, đồng thời ghi nhận là **nợ chồng phân công — cần chốt mã hoặc chốt ngoại lệ**.
>
> **Không thuộc TV4** (nhiều tài liệu gán nhầm, đã xác nhận trên repo): `FR-RCP-011…014`, `FR-USR-001`,
> `FR-CMT-001` (không tồn tại entity/endpoint/migration/test); FTS migration và cache search (TV2);
> `Identity` / PBKDF2 / refresh token hash (TV1).

---

## 2. Đối chiếu trạng thái thật sau N0 + N1 (đo lại 03/10, không dùng lại số cũ)

| # | Việc N1 | Bằng chứng | Mã đóng được | % cuối tuần 3 (báo cáo 3 tuần) | Sau N1 |
|---|---|---|---|---:|---:|
| 1 | `ObjectStorageHealthCheck` xác thực credential (B4) | `ObjectStorageCredentialProbe.cs`, `HealthTests` 18/18 | `FR-OBS-001` | 80% | ~90% |
| 2 | `/health/ready` 503 khi Redis chết — kỳ vọng dứt khoát | `HealthTests.cs` 5/5 | `FR-OBS-001` | 80% | ~90% |
| 3 | OTEL collector + Serilog→Seq, **trace HTTP→DB thật** | `logs/seq_trace_recipes.log`, `TracingObservabilityTests` 2/2 | `FR-OBS-003` | 70% | ~90% |
| 4 | `pg_dump` 03:00 + **drill restore** (14 bảng) | `deploy/backup.sh`, `deploy/restore.sh` | `NFR-REL-001` `NFR-REL-003` | 45% / 10% | ~60% / ~55% |
| 5 | 2 tiến trình API thật + cache chung qua Redis | `logs/multi_instance_two_api.log`, `RedisSharedCacheTests` 6/6 | `NFR-SCALE-001` | 35% | ~65% |
| 6 | Sitemap cron `0 2 * * *` UTC + khoá phân tán Redis | `SitemapGenerator.cs`, `SitemapLockTests` 3/3 | `FR-JOB-003` `NFR-SEO-003` | 45% / 55% | ~85% / ~85% |
| 7 | B1 `503 storage.unavailable` + B2 fail-fast | `StorageFailureContractTests` | `FR-FILE-002` | 55% | ~60% |
| 8 | Bỏ secret khỏi `render.yaml` + secret scan CI | `render.yaml` (`sync: false`), `deploy/scan-secrets.sh` | *(ngoài 21 mã)* | — | 1 phần |

**Số đo chốt 03/10** (`55b4c2b`): build 0 warning · `dotnet format` exit 0 · test **210/210** (205 + 5) ·
`npx tsc --noEmit` exit 0 · `npm run build` exit 0 (17 route) · secret scan pass (đã thử chèn JWT hardcode → bị bắt) ·
CI `Backend` + `Frontend` xanh trên `ee5e78b`.

> **Ba điểm bản gốc ghi sai, V2 sửa lại:**
> - `KE_HOACH_TUAN_4_TV4.md:37` và `:119` yêu cầu chạy lại baseline để "không dùng lại 172/172 mơ hồ" — **đã làm** 30/09 (`178/178`) và cập nhật 03/10 (`210/210`). Trạng thái N0-4 là **xong**, không phải "việc làm ngay".
> - `KE_HOACH_TUAN_4_TV4.md:118` (N0-3) và `SO_EVIDENCE_TUAN_4.md:54` (mục 1.1 #6) yêu cầu chạy tay TC1–TC4 — **đã dời sang N2-2** và **đó là quyết định đúng**. Bản V2 giữ nguyên cách làm: lấy bằng chứng bằng E2E lặp lại được, không chạy tay một lần.
> - Bản gốc mục 2.3 ghi "⛔ **Chưa có PR** cho `practice/TV4/L4`" là đúng, nhưng bản gốc **không đưa việc mở PR cho chính nhánh tuần 4** vào bất kỳ mục nào → V2 bổ sung (mục 4.6).

---

## 3. Mốc kỹ năng: **7/24** kiểm chứng được, không phải 9/24

`PHAN_CHIA_CONG_VIEC_6_TUAN.md:180` ghi TV4 = **9/24** (K01, K05, K08, K11, K12, K13, K20, K23, K24).
Báo cáo 3 tuần mục 9 chỉ ra **3 ô trong 9 ô đó không đứng vững**:

| Ô | Vấn đề | Căn cứ | Xử lý trong V2 |
|---|---|---|---|
| **K08** Identity/PBKDF2/JWT/refresh | Là việc **TV1** (`IdentityService.cs`, `JwtService.cs`, `AuthTests.cs`) | báo cáo 3 tuần mục 9 | Chuyển sang mục 7 (đề xuất gửi nhóm). Phần **LAB** của TV4 vẫn giữ ở N3-1 |
| **K11** FTS `tsvector`/`unaccent`/`pg_trgm`/`ts_rank` | Là việc **TV2** (`20260929060455_AddFtsAndGinIndex.cs`) | báo cáo 3 tuần mục 9 | Chuyển sang mục 7. Phần **LAB** của TV4 vẫn giữ ở N3-2 |
| **K01** SRS/FR-NFR/ADR/API contract | `SO_EVIDENCE_TUAN_3.md:16` **tự ghi "⬜ Chưa làm"** nhưng vẫn được tính vào 9/24 | báo cáo 3 tuần mục 6.3 | Mapping **đã làm** 30/09 (`MAPPING_K01_FR_NFR_ADR_EVIDENCE.md`, 24 dòng) → K01 có nền thật, **chờ Tâm xác nhận** |

> **Phép tính để Tâm chốt:** `9 − K08 − K11 = 7`. Đó là con số `7/24` mà V2 dùng.
> Nếu Tâm đồng ý thêm với K01 (tự ghi chưa làm) thì xuống **6/24**.
> V2 **không** tự nâng và cũng **không** tự hạ thêm — con số 9/24 của nhóm vẫn nguyên trong `PHAN_CHIA`,
> V2 chỉ ghi rõ **chênh lệch 9 vs 7 và lý do từng ô**.

### 3.1 Bảng 24 ô sau khi áp dụng mốc 7/24

| Nhóm | Ô | Ghi chú |
|---|---|---|
| **Có bằng chứng — chờ Tâm duyệt (12 ô)** | K02 K03 K05 K07 K10 K12 K13 K14 K15 K20 K23 K24 | 6 ô có từ trước tuần 4 + 8 ô có bằng chứng mới trong N1 (`SO_EVIDENCE_TUAN_4.md:104-105`) |
| **Còn thiếu — thuộc TV4 (8 ô)** | K04 K06 K16 K17 K18 K19 K21 K22 | ← **mục tiêu thật của tuần 4** |
| **Còn thiếu — chờ người khác / ngoại lý (4 ô)** | K01 (chờ Tâm xác nhận) · K08 (Identity của TV1) · K09 (Google OAuth — **thiếu credentials**, không tính hoàn thành) · K11 (FTS của TV2) | ← mục 7 |

> **V2 không đặt mục tiêu 24/24.** Báo cáo 3 tuần mục 7 chỉ ra `K04`, `K06`, `K08`, `K09`, `K11` đòi hạ tầng
> hoặc tín hiệu của người khác (Google credentials, schema Recipe của TV3, migration FTS của TV2). Bản gốc
> mục 6 (G4) ghi "24/24 ô K có minh chứng" là **không khả thi trong tuần này** → V2 thay bằng mục 8.

---

## 4. Kế hoạch còn lại của tuần 4 — giữ nguyên khối N2 / N3 / N4, điều chỉnh nội dung

> **Giữ nguyên** khối lượng và thứ tự của bản gốc. V2 chỉ (a) thêm việc do báo cáo 3 tuần chỉ ra,
> (b) bỏ việc đã xong, (c) ghi rõ phần **không** làm được trong 3 ngày còn lại.
> Mọi lỗi/bug phát sinh khi làm N2–N4 ghi vào `BAO_CAO_LOI_TUAN_4_TV4.md`, **không** ghi vào đây.

### 4.1 N2 — D7: E2E, tấn công file, cổng CI, số đo (8 việc, chưa làm)

| # | Việc | Ô K | Mã | Điều chỉnh của V2 so với bản gốc |
|---|---|---|---|---|
| 1 | `playwright.config.ts` + `@playwright/test` + script `test` | K21 | `NFR-MAINT-002` | Giữ nguyên. Ưu tiên **cao nhất** — đang chặn K16/K17/K18/K21 |
| 2 | Luồng E2E **search** (phủ TC1–TC12 của báo cáo lỗi 500) + **publish** | K21 | `FR-RCP-007` | Giữ nguyên. Đây cũng là **bằng chứng thay cho N0-3** (TC1–TC4 chạy tay) |
| 3 | Cổng CI frontend: `npm ci` → `tsc --noEmit` → `next build` → `lint` | K16 K24 | `NFR-MAINT-002` | ⚠ **CI frontend đã có** (`.github/workflows/frontend.yml`, commit `adb1507` của TV2). V2: **verify + bổ sung `e2e` job** thay vì dựng mới — tránh làm trùng |
| 4 | Tấn công file ở mức E2E: > 5 MiB · `.exe`→`.jpg` · file rỗng/hỏng · nhiều request liên tiêp | K13 K21 | `FR-FILE-002` `NFR-SEC-004` | Giữ nguyên, chuyển từ "bổ sung" lên **ưu tiên** vì đây là hạng mục P1 của báo cáo 3 tuần |
| 5 | Resilience có số liệu: dừng lần lượt DB / Redis / S3 / worker rồi đo request lỗi, retry, thời gian phục hồi | K20 K22 | `FR-OBS-001` `NFR-REL-001` | Giữ. Bổ sung: đo **cache-hit ratio** (K12 gap còn lại) và **`/health/ready` trả gì khi từng phụ thuộc chết** |
| 6 | Commit script k6 + chạy lại có **p50/p95/p99**; EXPLAIN lại sau thay đổi index | K22 K06 K11 | `NFR-PERF-001` `NFR-PERF-002` | ⚠ **Đổi mốc đo**: bản gốc ghi "20 VU", báo cáo 3 tuần xếp `NFR-PERF-002` **không đạt** vì chưa đo p99 và chỉ 20 VU. V2: commit script ở `tests/performance/` + chạy **≥ 100 VU có p50/p95/p99**. `NFR-PERF-*` **không** nằm trong 21 mã của TV4 → ghi kết quả, **không** tự nhận mã |
| 7 | Ngưỡng coverage `CulinaryBlog.Application` ≥ 80% trong CI | K21 | `NFR-MAINT-002` | Giữ. Hiện line **83.37%** đã vượt ngưỡng nhưng **chưa có gate** — việc là thêm gate, không phải tăng coverage |
| 8 | **D4-UI còn treo từ tuần 3**: thanh **progress % upload** + nút **Unpublish/Archive** + checklist WCAG 320/768/1200 px | K05 K17 K18 | `NFR-USE-004` `FR-RCP-006` `FR-RCP-007` | ⚠ **Nâng lên bắt buộc.** Báo cáo 3 tuần xếp `NFR-USE-004` = **15%** (thấp nhất trong 10 NFR của TV4) và DoD ghi "thiếu UI nếu người dùng cần thao tác". **Đã kiểm chứng 03/10**: grep `unpublish\|archive` trong `src/frontend/src` chỉ còn **2 kết quả, đều là kiểu/nhãn** (`lib/recipe.ts:16,89`) — **chưa có nút nào** |

### 4.2 N3 — D6: Lab L5 + bù mục 2 của L4 (4 việc, chưa làm)

| # | Việc | Ô K | Điều chỉnh của V2 |
|---|---|---|---|
| 1 | Bù mục 2 L4 (Identity/refresh/forms/FTS) | K08 K09 K11 | ⚠ **Tách bạch**: K08/K11 là phần LAB của TV4 trên nền người khác viết SP. Ghi rõ trong sổ: phần nào là của TV4, phần nào mượn của TV1/TV2 |
| 2 | Lab L5 (`practice/TV4/L5`) — 8 phase, chấm kiểu L4 (check count rõ ràng) | K04 K16 K17 K19 | ⚠ **Ưu tiên 4 phase có giá trị nghiệm thu cao nhất**: `seo` (K19) · `query-rollback` + `image-opt` (K17) · `search-ssr` + `isr-detail` (K16) · `cqrs-behavior` (K04). 4 phase còn lại ghi rõ **để tuần 5** |
| 3 | `SOK_LAB_L5.md` — kỹ thuật con nào có/không, giới hạn, lỗi gặp | — | Giữ nguyên |
| 4 | **Mở PR cho `practice/TV4/L4`** (hiện chỉ có branch, chưa có PR) | K24 | ⚠ **Nâng lên bắt buộc.** Báo cáo 3 tuần mục 10 P1 #5 xếp việc này vào **P1**; DoD yêu cầu "có PR được review"; `K24` chưa đóng vì thiếu đúng thứ này |

### 4.3 N4 — D7: Runbook, release, bàn giao (4 việc, chưa làm)

| # | Việc | Ô K | Điều chỉnh của V2 |
|---|---|---|---|
| 1 | `docs/RUNBOOK.md` — dựng stack từ checkout sạch · backup/restore · failover từng dependency · 2 instance · lệnh k6 · tìm log/trace của một request | K23 K24 | ⚠ **Bắt buộc có số liệu thật**, lấy từ log N1/N2 đã có (`seq_trace_recipes.log`, `multi_instance_two_api.log`, `backup_restore_drill`). Bản gốc ghi "không viết 'nên làm'" — V2 giữ nguyên và thêm: **ghi rõ điều KHÔNG đạt** để người sau không hiểu nhầm |
| 2 | Deploy lặp lại được — `docker-compose.prod.yml` hoặc checklist Render; chạy 2 lần từ sạch để chứng minh idempotent | K23 | Giữ nguyên. ⚠ Phải nêu **giới hạn thật**: `render.yaml` để `plan: free`, **chưa đo trên hạ tầng thật** (handoff mục 4.1) → không ngụ ý đạt SLA 99,5% của `NFR-REL-001` |
| 3 | Cập nhật `HUONG_DAN_CHAY_TV4.md` + `README.md` + `CHANGELOG.md` | K24 K22 | ⚠ **Mở rộng bắt buộc** — xem A6 ở mục 5: `README.md` đang ghi **3 con số test khác nhau** (dòng 62: `177/177`; dòng 224 và 233: `172/172`; dòng 352: `172/172`) trong khi thực tế `210/210`. `CHANGELOG.md:3` đứng `0.1.0` từ 09/09 |
| 4 | Chốt `SO_EVIDENCE_TUAN_4.md` + `TRANG_THAI_THUC_HIEN_TUAN_4.md`, nộp review Tâm | K01 | ⚠ **Bổ sung bước 4b**: **mở PR cho nhánh `2312739_NHTSon_D5-D6-D7`** — hiện 8 commit N1 nằm trên nhánh, chưa có PR, nên **chưa thỏa DoD "có PR được review"** |

### 4.4 Thứ tự thực hiện đề xuất (còn 3 ngày, 03/10 → 06/10)

| Ngày | Việc | Lý do |
|---|---|---|
| **03/10** | A1 (dọn secret) · N2-1 Playwright · **4b mở PR** cho 8 commit N1 | PR càng sớm càng tốt — DoD đang bị vỡ vì nhánh chưa có PR |
| **04/10** | N2-3 verify CI frontend · N2-2 luồng search + publish · N2-4 tấn công file · A3 | Chặn 4 ô K; A3 là `NFR-SEC-005` (đang 20%) |
| **05/10** | N2-5 + N2-6 + N2-7 số đo · N3-1 + N3-4 lab/PR · A4 A5 A6 | Số đo cần thời gian chạy; A4/A5/A6 là nợ hạ tầng còn treo từ N1 |
| **06/10** | N3-2 lab L5 (4 phase ưu tiên) · N3-3 sổ lab · N4-1 runbook · N4-2/3 · N4-4 | Chốt G5 |

> **Cắt nếu trượt tiến độ** — theo thứ tự: (1) 4 phase lab L5 còn lại → tuần 5 · (2) N3-1 bù L4 → tuần 5 ·
> (3) N2-6 EXPLAIN lại → tuần 5. **Không cắt**: A1, A6, 4b, N2-1, N2-8, N4-1 — vì đây là những việc
> báo cáo 3 tuần đánh giá là **bỏ sót** và **vi phạm DoD**, không phải việc "thêm cho đủ".

---

## 5. Năm hạng mục thuộc TV4 mà bản gốc bỏ sót (báo cáo 3 tuần chỉ ra)

| Mã | Việc | Vì sao bản gốc không có | Bằng chứng hiện tại | Ưu tiên |
|---|---|---|---|---:|
| **A1** | **Dọn secret còn trong `appsettings*.json`** + **mở rộng `deploy/scan-secrets.sh`** để quét `appsettings*.json` và biến `env:` của workflow | N1-8 (30/09) chỉ xử lý `render.yaml`. Scanner hiện chỉ quét `render.yaml` + `docker-compose.dev.yml` + 7 pattern token nổi tiếng — **không** chạm JSON hay `env:` trong workflow | `src/backend/CulinaryBlog.API/appsettings.json:3` (mật khẩu DB), `:9` (JWT signing key 74 ký tự) · `appsettings.Development.json:3,6,14-15` (mật khẩu DB + `minioadmin`/`minioadmin`) · `.github/workflows/backend.yml:47,48,67,68` (`minioadmin` ×4) — **đã kiểm chứng 03/10** | **P0** |
| **A2** | Dọn dead-code soft-delete của Category | Báo cáo 3 tuần mục 11.1 ghi "nợ kỹ thuật" nhưng không ai đưa vào kế hoạch | `Recipe.cs:287,298` (`MarkDeleted`/`SoftDelete`), `BaseEntity.cs:18-19` (`IsDeleted`) còn sống, nhưng `CategoryRepository.DeleteAsync` **hard delete** → query filter `!IsDeleted` không còn tác dụng; tên test còn `..._and_soft_deletes_when_empty` | P2 |
| **A3** | **`ssl_certificate` + HSTS + `UseHttpsRedirection`** + `proxy_next_upstream` + `upstream` nhiều server | Bản gốc mục 2.1 có ghi "không có `ssl_certificate`" nhưng **không đưa vào danh sách việc**; `NFR-SEC-005` = **20%**, thấp thứ 3 của TV4 | `nginx/nginx.dev.conf:6-11` (`upstream` chỉ **1** server, `listen 80`, không `ssl`); grep `UseHsts\|UseHttpsRedirection` trong `src/backend` = **0** | **P1** |
| **A4** | **Giữ 30 ngày thật** cho backup (không dùng artifact GitHub 7 ngày) | N1-4 làm script + drill, nhưng handoff mục 4.2/4.3 ghi đây là *hạn chế*, **không** đưa thành việc | `deploy/backup.sh` có; `.github/workflows/backup.yml` cron `0 20 * * *` UTC; cần secret `DATABASE_URL`; artifact GitHub chỉ 7 ngày ⇒ `NFR-REL-003` mới ~55% | **P1** |
| **A5** | **Profile `docker-compose` 2 API instance** sẵn dùng cho cả nhóm | N1-5 mới chạy tay 2 process; handoff mục 4.1 ghi là hạn chế, **không** đưa thành việc | `nginx/nginx.multiinstance.conf` có `upstream` 2 server nhưng **không** có profile compose; `nginx.dev.conf` vẫn 1 server ⇒ `NFR-SCALE-001` ~65%, `NFR-SCALE-003` ~70% | **P1** |
| **A6** | **`CHANGELOG.md`** đứng `0.1.0` từ 09/09 + **`README.md`** ghi sai số test ở 4 chỗ | Bản gốc N4-3 chỉ ghi "cập nhật CHANGELOG" không nêu số sai; báo cáo 3 tuần mục 3.2 + 7 xếp `NFR-MAINT-003` là **vi phạm** của TV1, nhưng `NFR-MAINT-003` **thuộc TV4** theo danh sách đã đính chính | `CHANGELOG.md:3` = `## 0.1.0 — 2026-09-09`; `README.md:62,224,233,352` — đã kiểm chứng 03/10 | **P1** |

> **A1 là hạng mục quan trọng nhất trong nhóm này.** Báo cáo 3 tuần mục 1 xếp "secret trong repo" là
> **phát hiện nghiêm trọng nhất toàn dự án**. N1-8 đã sửa `render.yaml` — nhưng `appsettings.json` vẫn còn
> JWT signing key, và công cụ chống secret **có lỗ hổng**: nó báo "OK" trong khi repo vẫn còn secret.
> Đây là trường hợp "công cụ xanh nhưng việc chưa xong" — cần nói rõ trong báo cáo, không tính là đã xong.

---

## 6. Ma trận 21 mã — trạng thái V2 dự kiến khi hết tuần

| Mã | % cuối tuần 3 | Sau N1 | Sau V2 (dự kiến) | Việc V2 | Ghi chú trung thực |
|---|---:|---:|---:|---|---|
| `FR-FILE-001` | 95% | 95% | 95% | — | Đã khớp audit |
| `FR-FILE-002` | 55% | 60% | ~75% | N2-4 | Còn: retry 3 lần khi S3 lỗi |
| `FR-RCP-005` | 90% | 90% | 90% | N2-8 | Backend xong, UI còn thiếu |
| `FR-RCP-006` | 90% | 90% | ~95% | N2-8 | Nút Unpublish |
| `FR-RCP-007` | 90% | 90% | ~95% | N2-8, N2-2 | Nút Archive + E2E publish |
| `FR-RCP-008` | 90% | 90% | 90% | N2-8 | NFR-SEC-004 quyền trên ảnh đã có |
| `FR-JOB-002` | 95% | 95% | 95% | — | Đã khớp audit |
| `FR-JOB-003` | 45% | 85% | ~90% | N4-1 | Còn: ghi lịch vào runbook |
| `FR-OBS-001` | 80% | 90% | ~95% | N2-5 | Còn: `/health/ready` khi **từng** phụ thuộc chết |
| `FR-OBS-003` | 70% | 90% | ~95% | N2-5 | Còn: collector không đứng thì app có lỗi không |
| `FR-AUTH-005` | 95% | 95% | 95% | *(mục 7)* | `FamilyId` thuộc TV3 — không tính vào TV4 |
| `NFR-SEC-004` | 90% | 90% | ~95% | **A1** | Redaction log đã có; secret trong config chưa |
| `NFR-SEC-005` | **20%** | 20% | ~50% | **A3** | ⚠ **Thấp nhất trong 21 mã.** Chỉ đạt được nếu nhóm chốt TLS ở `render.yaml` |
| `NFR-REL-001` | 45% | 60% | ~70% | **A4** A5 N2-5 | ⚠ Chưa đo trên hạ tầng thật → **không** đạt 99,5% |
| `NFR-REL-002` | 50% | 50% | 50% | — | Đã đạt ở tuần 3 (refresh token sống qua restart) |
| `NFR-REL-003` | **10%** | 55% | ~70% | **A4** | Script + drill có; **còn** kho 30 ngày thật |
| `NFR-USE-004` | **15%** | 15% | ~60% | N2-8 | ⚠ **Thấp nhất trong 10 NFR.** Thanh progress chưa có |
| `NFR-SCALE-001` | 35% | 65% | ~80% | **A5** N2-5 | Cache chung qua Redis đã có; thiếu profile compose |
| `NFR-SCALE-003` | 65% | 70% | ~85% | **A3** A5 | `proxy_next_upstream` + upstream nhiều server |
| `NFR-SEO-003` | 55% | 85% | ~90% | N4-1 | Cron + lock có; còn ghi runbook |
| `NFR-MAINT-003` | 55% | 55% | ~80% | **A6** N4-3 | `CHANGELOG` + `README` còn đứng |

> **Ước tính 12/21 mã ≥ 80% khi hết tuần** (đầu tuần 4: 3/21). V2 **không** cam kết con số này — ghi để
> reviewer so sánh với số thực tế sau khi chạy. Mọi ô ghi "≈" đều là **ước tính theo việc có thể làm**, chưa đo.

---

## 7. Đề xuất gửi nhóm — **KHÔNG tính vào tiến độ tuần 4 của TV4**

| # | Vấn đề | Chủ sở hữu | Bằng chứng | Đề xuất |
|---|---|---|---|---|
| G1 | **Rate limit hỏng sau Nginx**: `GetFixedWindowLimiter` + không `UseForwardedHeaders`, khoá theo `RemoteIpAddress` ⇒ sau Nginx **toàn bộ người dùng dùng chung 1 bucket 10 req/phút**. `NFR-SEC-003` đạt **1/5** | **TV1** (`Program.cs`) — TV4 giữ phần nginx | `Program.cs:120-125` (chỉ `GetFixedWindowLimiter`, chỉ policy `auth`); `nginx/nginx.dev.conf:17-19` **đã** set `X-Forwarded-For`/`X-Real-IP` nhưng app không đọc | Đổi `AddSlidingWindowLimiter` + thêm policy API 100/phút, upload 5/phút + `UseForwardedHeaders` + `KnownProxies`. **TV4 sẵn sàng làm nếu được giao** — đây là P0 của báo cáo 3 tuần |
| G2 | `RateLimitPartition` cần `RemoteIpAddress` đúng ⇒ nếu G1 chưa xong, mọi số đo k6 của tuần 4 **không đáng tin** | TV1 | — | Chốt G1 **trước** N2-6, hoặc ghi rõ k6 chạy ở chế độ không qua proxy |
| G3 | `NFR-SEC-002` / `FR-AUTH-004` thiếu cột `FamilyId`; "family" hiện là *mọi token của user* | **TV3** (`IdentityService.cs:111-116`) | grep `FamilyId` trong `src/backend` = 0 | Phần này **không** phải của TV4 — nêu để TV3 biết trước nghiệm thu |
| G4 | FTS thiếu `tsvector`/trigger/`ts_rank`, vi phạm ADR `0003:43-44` của chính TV2; test "phở" chạy trên fake repo | **TV2** | `RecipeRepository` dùng `ILIKE`, không `to_tsquery` | `K11` của TV4 **không** đóng được nếu TV2 chưa sửa |
| G5 | `TUAN_4.md` của cả 4 người ghi "100% / 177-178 test" — **mâu thuẫn trực diện** với tài liệu gốc của TV4 (`TRANG_THAI_THUC_HIEN_TUAN_4.md:19` ghi "29 việc — 3 xong — ≈10%") | TV1 (người viết) | Đã đối chiếu; con số thật tại `3d0695d` là **210/210** | Hạ lại cho đúng số thật và tỷ lệ thật |
| G6 | **Toàn bộ PR do TV1 duyệt, kể cả PR của chính TV1**; PR #1/#4 không tồn tại | Nhóm | `git log --merges` | ⚠ Chưa có ADR. Rủi ro quản trị đã ghi nhận từ tuần 2, **3 tuần chưa xử lý**. Ảnh hưởng trực tiếp DoD của **TV4** (việc 4b) |
| G7 | Không có `Practice/TV2` branch và không có lab cho TV2 (`B6` = 8%) | **TV2** | `PHAN_CHIA` mục 3.4 | Ảnh hưởng cổng G4 **của nhóm**, không ảnh hưởng riêng TV4 |
| G8 | Mã `NFR-DATA-001` được khai nhưng **không tồn tại** trong SRS v1.1.1 | **TV3** | SRS chỉ có SEC/PERF/USE/REL/MAINT/SEO | Sửa báo cáo, không sửa SRS |

---

## 8. Cổng nghiệm thu G4 / G5 — nghỉnh lại

### G4 — giữa tuần (bản gốc ghi "24/24 ô K", V2 thay)

- [ ] **PR đã mở** cho `2312739_NHTSon_D5-D6-D7` (8 commit N1) và được Tâm review — *điều kiện tiên quyết, hiện đang vỡ*
- [ ] Bảng mapping K01 đã được Tâm **xác nhận cột FR/NFR + ngày** (file đã lập 30/09)
- [ ] 8 ô K mục tiêu của tuần 4 (**K04 K06 K16 K17 K18 K19 K21 K22**) có **≥ 5 ô** đạt đủ 4 điều kiện ở mục 4
- [ ] `playwright.config.ts` + `@playwright/test` đã cài, chạy được **≥ 1 luồng** (search)
- [ ] **A1** xong: `grep` không còn secret trong `appsettings*.json`; scanner bắt được secret **JSON** và **workflow `env:`**
- [ ] N2-8 xong: có nút Unpublish/Archive + thanh progress % upload
- [ ] `practice/TV4/L4` **có PR**; `practice/TV4/L5` có **≥ 1 phase** chấm kiểu L4
- [ ] Tâm xác nhận mốc kỹ năng: **7/24 hay 9/24** (mục 3) — và `NFR-SEC-007` có thuộc TV4 không (Q1)

### G5 — cuối tuần (bản gốc giữ, V2 thêm)

- [ ] `/health/ready` trả **503** khi Redis chết — **đã đạt** N1-2 (bản gốc còn để chưa làm)
- [ ] Health check storage **xác thực credential** — **đã đạt** N1-1
- [ ] **Trace HTTP→DB thật** trong Seq/OTLP + file bằng chứng — **đã đạt** N1-3
- [ ] Backup 03:00 + **drill restore** — **đã đạt** N1-4; **còn** kho 30 ngày thật (**A4**)
- [ ] 2 tiến trình API dùng chung cache/queue; sitemap không sinh trùng — **đã đạt** N1-5/N1-6; **còn** profile compose (**A5**)
- [ ] Sitemap cron `0 2 * * *` UTC + khoá phân tán — **đã đạt** N1-6
- [ ] **A6** xong: `CHANGELOG.md` có mục tuần 2/3/4; `README.md` **một** con số test duy nhất
- [ ] 5 luồng E2E Playwright pass (search + publish của TV4)
- [ ] Cổng CI frontend (`npm ci` → `tsc` → `next build` → `lint`) xanh **+ có job e2e**
- [ ] Kịch bản tấn công file (size/MIME giả/hỏng) bị chặn **đúng mã lỗi**
- [ ] Coverage `CulinaryBlog.Application` ≥ 80% **có ngưỡng trong CI** (đang 83,37%, thiếu gate)
- [ ] **k6 script commit** được, chạy **≥ 100 VU** có p50/p95/p99
- [ ] **Runbook** có số liệu thật **và** mục "chưa đạt" rõ ràng
- [ ] Không còn secret nào trong file tracked; CI chặn được cả JSON lẫn workflow `env:`
- [ ] Baseline `55b4c2b`: build 0 warning · `dotnet format` sạch · test xanh (**210/210**)
- [ ] Không commit secret; log bằng chứng đã redact
- [ ] **`SO_EVIDENCE_TUAN_4.md` + `TRANG_THAI_THUC_HIEN_TUAN_4.md` cập nhật; ô nào thiếu ghi rõ thiếu**

---

## 9. Rủi ro (rút gọn, chỉ rủi ro còn lại sau N1)

| Rủi ro | Mức | Xử lý trong V2 |
|---|---|---|
| **Nhánh 8 commit N1 chưa có PR** ⇒ vi phạm DoD, không được tính ô K nào | 🔴 | **4b** là việc đầu tiên 03/10 |
| `scan-secrets.sh` báo OK nhưng repo **vẫn còn** secret ở `appsettings*.json` + `backend.yml` | 🔴 | **A1** P0 |
| `NFR-SEC-005` = 20% cần quyết định TLS ở `render.yaml` (`plan: free`) | 🔴 | **A3** + hỏi nhóm (Q3); ghi rõ hạn chế nếu nhóm không chốt |
| CI frontend đã do TV2 dựng (`adb1507`) — V2 dựng lại sẽ **xung đột** | 🟡 | N2-3: **verify + bổ sung `e2e` job**, không tạo workflow mới |
| 3 ngày cho 16 việc + 6 việc A | 🟡 | Mục 4.4 có thứ tự cắt rõ; **không cắt** A1/A6/4b/N2-1/N2-8/N4-1 |
| Rate limit chưa sửa (G1) ⇒ **k6 đo không đáng tin** | 🟡 | G2: chốt G1 trước N2-6, hoặc đo ngoài proxy |
| 4 file `Lab 04` đã bị **TV1 xoá** ở `f85e1e6`, nhưng 3 tài liệu tuần 4 của TV4 vẫn ghi "giữ nguyên, không xoá" | 🟡 | **BUG-W4-08** — cập nhật 3 file tài liệu cho khớp thực tế |
| Một người duyệt tất cả PR (G6) — chưa có ADR sau 3 tuần | 🟡 | G6; không tự quyết, nhưng nó là điều kiện để 4b được duyệt công bằng |

---

## 10. Câu hỏi cần Tâm / nhóm chốt (chặn G4)

| # | Câu hỏi | Vì sao chặn | Chặn việc nào |
|---|---|---|---|
| **Q1** | `NFR-SEC-007` (no-secrets) **có thuộc TV4 không**? Nó không nằm trong 21 mã đã đính chính, nhưng `KE_HOACH_DU_AN.md` giao cho TV4 và N1-8 đã làm | Quyết định A1 tính vào mã nào | A1, K10, K24 |
| **Q2** | Mốc kỹ năng TV4 là **7/24** (báo cáo 3 tuần) hay **9/24** (`PHAN_CHIA`)? Nếu 9/24 thì K08/K11/K01 được tính vì lý do gì | Quyết định 12 ô có bằng chứng hay 12 ô còn thiếu | Toàn bộ mục 3, G4 |
| **Q3** | Có **TLS thật** ở môi trường deploy không (`render.yaml` đang `plan: free`)? Nếu không thì `NFR-SEC-005` ghi "chưa đạt — lý do hạ tầng" hay vẫn cố làm HSTS? | Quyết định A3 làm được tới đâu | A3, `NFR-SEC-005` |
| **Q4** | Ai **rotate JWT signing key** cũ (còn trong git history)? Việc này nằm ngoài repo | Không ai làm thì `NFR-SEC-004` vẫn hở | A1, hạn chế ghi trong sổ |
| **Q5** | Backup 30 ngày đặt ở **GitHub Actions** (artifact 7 ngày, có thể trễ/bỏ qua job) hay **cron trên host**? Kho lưu là RustFS/S3/NAS hay ổ đĩa host? | Quyết định `NFR-REL-003` có đạt được không | A4, `NFR-REL-003` |
| **Q6** | 2 API instance có cần đưa vào `docker-compose` thành profile **sẵn dùng cho cả nhóm**, hay để chạy tay như hiện tại? | Quyết định A5 có giá trị cho người khác không | A5, `NFR-SCALE-001/003` |
| **Q7** | `practice/TV4/L4` và `practice/TV4/L5` được phép **mở PR lên `main`** không, hay giữ nguyên quy ước "lab không merge"? Nếu giữ quy ước thì reviewer xem bằng cách nào | Quyết định K24 có đóng được không | N3-4, K24 |
| **Q8** | Bản kế hoạch nào là bản **chính thức** cho nghiệm thu G4/G5: `KE_HOACH_TUAN_4_TV4.md` hay file V2 này? | Hai bản đang cùng hiệu lực | Toàn bộ |

---

## 11. Tài liệu liên quan

| Tài liệu | Vai trò |
|---|---|
| [`KE_HOACH_TUAN_4_TV4.md`](misc/KE_HOACH_TUAN_4_TV4.md) | **Bản gốc**, giữ nguyên để đối chiếu — không sửa |
| [`BAO_CAO_LOI_TUAN_4_TV4.md`](report/BAO_CAO_LOI_TUAN_4_TV4.md) | **Lỗi/bug** — tách riêng, không nằm trong kế hoạch |
| `../Report/BAO_CAO_TAM_RA_SOAT_TONG_HOP_3_TUAN.md` | Nguồn đối chiếu của V2. **Tài liệu tạm của TV4, không dùng để chấm điểm**; mọi kết luận trong V2 đều dẫn lại `file:line` trong repo để kiểm chứng độc lập |
| [`HANDOFF_TV4_TUAN4_N1.md`](HANDOFF_TV4_TUAN4_N1.md) | Bàn giao N0+N1, số đo 210/210, các bẫy đã vấp |
| [`TRANG_THAI_THUC_HIEN_TUAN_4.md`](TRANG_THAI_THUC_HIEN_TUAN_4.md) · [`SO_EVIDENCE_TUAN_4.md`](SO_EVIDENCE_TUAN_4.md) · [`MO_TA_CONG_VIEC_TUAN_4.md`](MO_TA_CONG_VIEC_TUAN_4.md) | Bộ tài liệu tuần 4 chuẩn (dùng N4-4 để cập nhật theo V2) |
| `../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` | Mapping 24 dòng K01 — đã lập 30/09, chờ Tâm xác nhận |
| `docs/KE_HOACH_DU_AN.md` mục 8 (D1–D7) · mục 9 (K01–K24) · mục 12.3 (DoD) | Nguồn chuẩn cho task, kỹ năng, nghiệm thu |
| `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md` mục 3.4 · mục 6 (G4/G5) · dòng 180 (mốc 9/24) | Giao việc và cổng nghiệm thu |
| `docs/root/SRS_Culinary_Blog_v1.1.1.md` | SRS chuẩn (FR/NFR/CONS) |
| `docs/proposal/DE_XUAT_01..06` | 6 đề xuất B1–B6 (B1/B2/B4 đã triển khai trong N1, chờ duyệt) |
