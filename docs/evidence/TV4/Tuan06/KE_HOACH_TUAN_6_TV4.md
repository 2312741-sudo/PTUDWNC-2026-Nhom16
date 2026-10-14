# KẾ HOẠCH TUẦN 6 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

| Mục | Nội dung |
|---|---|
| Người lập | Nguyễn Hữu Trung Sơn (TV4 · 2312739) |
| Ngày lập | 14/10/2026 |
| Nhánh | **`2312739_NHTSon_D7`** — tạo từ **`main`** (`e93fbe8`, đã gồm tuần 5 qua **PR #36**, 13/10/2026) |
| SRS | v1.1.1 (Approved 16/09/2026) |
| Task theo kế hoạch | **D7** — "Integration/UI/publish E2E, resilience/load/SEO, runbook và **release**" (`KE_HOACH_DU_AN.md` §8) |
| Reviewer / nghiệm thu | Nguyễn Thanh Tâm (Nhóm trưởng) |
| Cổng | **G7**: regression, release, tài liệu và **minh chứng đủ 4 người** |

> **Về nhánh**: `2312739_NHTSon_D7` được tạo **từ `main`** (`e93fbe8`). `main` đã chứa toàn bộ thay đổi tuần 5 (W5-1→W5-10: staging 2 API, TLS/CORS, runbook, D4-UI, số đo k6/SEO, đóng `BUG-W4-01/02/03`, ADR-TV4-003/004) qua **PR #36** (13/10/2026) cùng các file tuần 5 của TV1. Vì vậy nhánh tuần 6 **kế thừa đầy đủ thay đổi tuần 5** và đang ở **trạng thái mới nhất của `main`** — không cần merge `main` khi tạo nhánh (chỉ cần đồng bộ định kỳ nếu `main` tiến thêm trong tuần).

---

## 1. Căn cứ lập kế hoạch

| Nguồn | Nội dung lấy |
|---|---|
| [`../../../PHAN_CHIA_CONG_VIEC_6_TUAN.md`](../../../PHAN_CHIA_CONG_VIEC_6_TUAN.md) §3.4 dòng tuần 6 | "Regression/release, demo publish/media/ops và lab; chốt evidence" → đầu ra: "Reviewer Tâm xác nhận; **5 E2E pass**; runbook, backup/restore và **secrets instructions bàn giao**" |
| [`../../../KE_HOACH_DU_AN.md`](../../../KE_HOACH_DU_AN.md) dòng 259 (G7) + §2 | "**G7: regression, release, tài liệu và minh chứng đủ 4 người**"; tuần 6 dành cho sửa lỗi còn lại, regression, demo và bàn giao |
| `KE_HOACH_DU_AN.md` §8 (D7) | D7: "Integration/UI/publish E2E, resilience/load/SEO, runbook và release — Failover/retry có số liệu; file-size attack test; deploy lặp lại được" |
| `KE_HOACH_DU_AN.md` §12.5 | **Checklist release** 9 mục (34 FR / 30 NFR / 10 CONS / 4×24 K / 33 endpoint / 5 E2E / Compose-Nginx-HTTPS-secrets / README-Scalar-ADR-CHANGELOG / giới hạn) + demo 15–20 phút |
| [`.opencode/skills/ra-soat-tien-do-tuan/SKILL.md`](../../../../.opencode/skills/ra-soat-tien-do-tuan/SKILL.md) | Mốc tuần nhóm: tuần 5 = 07–11/10 → **tuần 6 = 14–18/10/2026** (tham chiếu) |
| **Nợ chuyển tuần 6** từ tuần 5 | [`../Tuan05/TRANG_THAI_THUC_HIEN_TUAN_5.md`](../Tuan05/TRANG_THAI_THUC_HIEN_TUAN_5.md) §1 · [`../Tuan05/HANDOFF_TV4_TUAN5.md`](../Tuan05/HANDOFF_TV4_TUAN5.md) §4/§6 · [`../Report/BAO_CAO_TIEN_DO_TUAN_5_SO_VOI_TUAN_4.md`](../Report/BAO_CAO_TIEN_DO_TUAN_5_SO_VOI_TUAN_4.md) §6/§8 — ghi đầy đủ ở §3.3 |

> **Nguyên tắc** (giữ nguyên của tuần 1–5):
> 1. Việc của thành viên khác nêu ở §6 — **không tính** vào tiến độ tuần 6 của TV4.
> 2. Lỗi/bug ghi vào báo cáo lỗi riêng, **không** ghi vào kế hoạch này.
> 3. Mỗi "đã làm" kèm `file:line` hoặc log. **Không** có mục "nên làm".
> 4. Ô kỹ năng chỉ chuyển ✅ khi có **code/config + test + kết quả chạy thật + Tâm xác nhận và ghi ngày**; không tự đánh dấu đạt.

---

## 2. Bối cảnh khi bắt đầu tuần 6

### 2.1. Tuần 5 đã đóng gì (nguồn: `Tuan05/`, `Report/`)

| Khối | Kết quả cuối tuần 5 |
|---|---|
| W5-1 | ✅ Baseline `0a9b1a5`: 426/426, coverage **97.07%**, build 0W/0E, format exit 0, FE exit 0, CI xanh |
| W5-2/3/4 | ✅ Staging idempotent 2 lần + drill backup→restore 14 bảng + trace HTTP→DB · **2 API** (50/50, failover 0/8, Hangfire 2 server) · TLS self-signed + HSTS + 301 + CORS explicit + `UseForwardedHeaders` |
| W5-5 | ✅ `docs/RUNBOOK.md` 7 mục + mục Giới hạn (commit `92e34be`) |
| W5-6 | ✅ Progress upload + nút Gỡ đăng/Lưu trữ/**Xóa** + E2E 3/3 + jest 86/86 |
| W5-7 | 🟡 **4/5** E2E flows trên staging — **full suite 40/40 ×2**; còn luồng **`category`** |
| W5-8 | ✅ k6 **102 VU** p95 148.56ms · 0% lỗi · SEO (96 URL) · EXPLAIN · cache-hit 70.31% · `/metrics` 200 |
| W5-9 | 🟡 **2/5** — `npm audit` critical→0, Zod/RHF xác nhận, `isr-detail` fix; còn `image-opt`/`query-rollback`/`cqrs-behavior`, OAuth |
| W5-10 | ✅ **8/8** — `BUG-W4-01/02/03` + secret scan mở rộng + PR `practice/TV4/L4` + ADR-TV4-003 + path traversal 7/7 + header `X-Sitemap-Generated` |
| **Tổng** | **52/56 việc (93%)** — 4 việc còn lại **đều blocked** (xem §3.3) |

### 2.2. Số liệu kiểm định làm baseline của tuần 6

| Hạng mục | Số cuối tuần 5 | Nguồn |
|---|---|---|
| `dotnet test -c Release` | **430/430** `CulinaryBlog.Tests` + **5/5** `ConcurrencySpike`, Failed 0, Skipped 0 | `Tuan05/HANDOFF_TV4_TUAN5.md` §1 |
| Build / format | 0 warning · 0 error · `dotnet format --verify-no-changes` exit 0 | `Tuan05/HANDOFF_TV4_TUAN5.md` §1 |
| Coverage gate | `CulinaryBlog.Application` **97.07%** ≥ 80% | `Tuan05/SO_EVIDENCE_TUAN_5.md` §1 |
| Frontend | Jest **86/86** · `tsc` exit 0 · `lint` warning-only (pre-existing `no-img-element`) · `build` exit 0 | `Tuan05/HANDOFF_TV4_TUAN5.md` §1 |
| Playwright | **40/40** (chống staging, chạy lặp 2 lần) | `Tuan05/HANDOFF_TV4_TUAN5.md` §2 |
| k6 | 102 VU · p50 82.56 · p95 **148.56** · p99 187.28 · **0% lỗi** | `Tuan05/logs/w58_k6_vus100.log` |
| PR tuần 5 | **PR #36 MERGED** (`2312739_NHTSon_D5-D7` → `main`, 13/10/2026, `mergedBy=**Tson-dev**`) · PR #32/#33 **CLOSED** (không merge) | `gh pr view 32/33/36` |

> ⚠️ **Việc làm ngay ngày đầu tuần 6**: đo lại baseline trên `main` mới (sau merge PR #36 = `e93fbe8`) và trên nhánh `2312739_NHTSon_D7`; **không** dùng số `Tuan05` làm số chính của tuần 6 (W6-1).

### 2.3. Ô kỹ năng (chưa ô nào được Tâm xác nhận — giữ nguyên để chờ)

| Mức | Số ô | Ô |
|---|---:|---|
| 🟢 đủ bằng chứng | 6 | K08, K13, K19, K20, K21, K24 |
| 🟢 gần đạt, còn 1 việc nhỏ | 8 | K02, K03, K07, K10, K12, K14, K15, K23 |
| 🟡 có nền sản phẩm, thiếu phần lab/đo lại | 8 | K01, K04, K05, K06, K11, K16, K17, K22 |
| ⬜ còn thiếu thật | 2 | K09 (Google OAuth — chờ credentials), K18 (thuộc TV2) |

Nguồn: `Tuan05/TRANG_THAI_THUC_HIEN_TUAN_5.md` §4 + `../Tuan05/SO_EVIDENCE_TUAN_5.md` §2. **0/24 được Tâm xác nhận** — G7 đòi hỏi điều này ở cả 4 người.

---

## 3. Danh sách việc tuần 6

### 3.1. Bảng tổng hợp — 9 khối việc

| # | Khối | Việc | Nợ/mới | K | FR/NFR |
|---|---|---|---|---|---|
| **W6-1** | Mở đầu | Baseline trên `main` mới + nhánh `2312739_NHTSon_D7`; mở PR tuần 6; **merge `main`** vào nhánh | Lặp lại (W5-1) | K24 | `NFR-MAINT-003` |
| **W6-2** | **Regression toàn hệ thống** | `dotnet test` + coverage + FE + **Playwright full suite trên staging ×2**; đối chiếu không tụt so với tuần 5; kiểm migration chạy lặp DB sạch | D7 "regression" | K21, K24 | `NFR-MAINT-002`, `NFR-REL-001` |
| **W6-3** | **Chốt 5/5 luồng G6** | Luồng E2E **`category`** (nợ W5-7, chờ TV2) → tích hợp + chạy; nếu TV2 chưa giao thì ghi ⬜ rõ | Nợ G6 | K21 | `FR-CAT-*`, `NFR-MAINT-002` |
| **W6-4** | **Lab L5 còn lại + K04** | Re-run harness `practice/TV4/L5`: sửa `image-opt`, `query-rollback` (chờ TV3 account) + chạy phase **`cqrs-behavior`** (K04 — chưa từng chạy) | Nợ W5-9 | K04, K16, K17 | — |
| **W6-5** | **`/metrics` exporter thật** | Cân nhắc pipeline metrics OTLP/Prometheus exporter thật thay bộ đếm in-memory (`RequestMetrics`); nếu không làm thì ghi rõ lý do | Nợ HANDOFF §4.3 | K20 | `FR-OBS-003` |
| **W6-6** | **Release & bàn giao** | Checklist §12.5: README, Scalar, ADR, **CHANGELOG**, báo cáo, tài khoản demo; đối chiếu 34 FR / 30 NFR / 10 CONS / 33 endpoint; secrets/backup/restore instructions | D7 "release" | K24 | `NFR-MAINT-003` |
| **W6-7** | **24/24 ô K + mapping** | Cập nhật `MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` + sổ evidence tuần 6; **chờ Tâm xác nhận từng ô + ngày** | G7 "minh chứng đủ 4 người" | K01 | — |
| **W6-8** | **Demo publish/media/ops** | Kịch bản demo 15–20 phút (§12.5): publish/unpublish/archive/delete · media+progress · SEO/sitemap · ops (2 API, TLS, backup/restore, trace, metrics) | D7 "demo" | K17, K19, K20, K23 | `FR-RCP-*`, `FR-OBS-*` |
| **W6-9** | **Nợ nhỏ còn lại** (song song, cắt trước) | `npm audit` nâng **major** (chờ nhóm chốt) · PR riêng `Program.cs` (`X-Sitemap-Generated` + `/metrics`) review · Google OAuth K09 (chờ credentials) · `NFR-SEO-004` (nợ TV3) · K11 FTS (chờ TV2) · **Tâm hậu kiểm PR #36** (`mergedBy` không phải Tâm) | Nợ P1/P2 | K09, K24 | `NFR-SEO-004`, `NFR-SEC-*` |

> **Không thuộc TV4 tuần 6** (ghi rõ để không tính vào tiến độ): checklist WCAG/responsive (**TV2**),
> Jest/RTL frontend unit test (**TV1**), sửa `to_tsquery` (ADR 0003 — **TV2**), `NFR-SEO-004` 301 đổi slug (**TV3**).

### 3.2. Chi tiết W6-1 → W6-9

#### W6-1 — Baseline + mở PR tuần 6 (ngày 1)

| Việc | Cách làm | Bằng chứng | K/NFR |
|---|---|---|---|
| Đo lại toàn bộ trên `main` mới (`e93fbe8`) + nhánh `2312739_NHTSon_D7` | `dotnet build` · `dotnet format --verify-no-changes` · `dotnet test` · `deploy/check-coverage.sh` · `tsc --noEmit` · `npm run lint` · `npm run build` · `npx jest` | `Tuan06/logs/baseline_*.log` — số vào `TRANG_THAI_THUC_HIEN_TUAN_6.md` | K24 |
| Đồng bộ `main` định kỳ | Nhánh đã tạo từ `main` (`e93fbe8`); nếu `main` tiến thêm trong tuần thì `git fetch` + `git merge origin/main` | `git log --oneline --graph` + CI xanh | K24 |
| Mở PR cho nhánh tuần 6 | PR `2312739_NHTSon_D7` → `main`, mô tả dẫn kế hoạch này; **nhờ Tâm review** (không tự merge như PR #36) | Link PR (DoD 12.3) | K24 |

#### W6-2 — Regression toàn hệ thống (ngày 1–2)

| Việc | Cách làm | Bằng chứng | K/NFR |
|---|---|---|---|
| Backend regression | `dotnet test CulinaryBlog.sln -c Release` — kỳ vọng **≥ 430 + 5**, `Skipped=0`, Failed 0 | `Tuan06/logs/regression_test.log` | K21 |
| Coverage gate | `bash deploy/check-coverage.sh` — `Application` ≥ 80% | `Tuan06/logs/regression_coverage.log` | K21 |
| Frontend regression | `tsc` · `lint` · `build` · `npx jest --ci` (kỳ vọng 86/86) | `Tuan06/logs/regression_frontend.log` | K21 |
| **E2E full suite chống staging ×2** | `docker compose -f docker-compose.staging.yml up -d` + `npm run test:e2e` (kỳ vọng ≥ 40/40, `--retries=0`), chạy lặp 2 lần | `Tuan06/logs/regression_e2e_run{1,2}.log` | K21 |
| Migration chạy lặp | DB sạch + nâng cấp từ bản trước (phối TV3 điều phối migration) | Log `dotnet ef database update` | K06 |
| Đối chiếu không tụt | So bảng §2.2 — không số nào thấp hơn tuần 5 | Bảng trong `TRANG_THAI_THUC_HIEN_TUAN_6.md` | `NFR-MAINT-002` |

#### W6-3 — Chốt 5/5 luồng G6: luồng `category` (ngày 2–3)

| Luồng G6 | Spec | Do ai viết | Việc tuần 6 |
|---|---|---|---|
| register | `create-recipe.spec.ts` | TV4 | Giữ xanh trong full suite |
| login | `create-recipe.spec.ts` | TV4 | Giữ xanh |
| **category** | `category.spec.ts` (**chưa có**) | **chờ TV2 xác nhận** | Kiểm tra TV2 đã giao chưa → tích hợp + chạy; **chưa có thì ghi ⬜ chờ TV2**, không tự viết thay |
| draft / create-recipe | `wizard-week4.spec.ts`, `create-recipe.spec.ts` | TV4/TV3 | Giữ xanh (10/10) |
| publish | `recipe-publish.spec.ts` | TV4 | Giữ xanh (4/4) |

> Cổng G6 (đóng ở tuần 5) chỉ đủ 5/5 khi luồng `category` có spec và pass. TV4 chịu trách nhiệm **chạy toàn bộ bộ test trên staging** và **báo đúng luồng nào do ai viết**.

#### W6-4 — Lab L5 còn lại + phase `cqrs-behavior` (ngày 2–4)

| Việc | Ghi chú | K |
|---|---|---|
| `image-opt` (Lab L5) | Sửa theo khuyến nghị L5 (9 file `<img>` thô → `next/image`), re-run harness | K16 |
| `query-rollback` (Lab L5) | Cần tài khoản E2E **sở hữu** công thức (hoặc `GetRecipesQuery` filter chủ sở hữu — việc TV3) → nếu chưa có thì ghi ⬜ chờ TV3, không tự sửa query của TV3 | K07 |
| Phase **`cqrs-behavior`** | K04 ghi "phần tự viết behavior trong lab L5"; L5 tuần 4 chỉ chạy 7 phase, **thiếu phase này** — chạy trên `practice/TV4/L5` | **K04** |
| Ghi kết quả | Số phase PASS/FAIL + commit trên `practice/TV4/L5` | — |

#### W6-5 — `/metrics` exporter thật (ngày 3)

- Hiện `RequestMetrics` giữ số **in-memory** (reset khi restart), **không** thêm package exporter để tránh đổi lock-file/CI locked-mode (`Tuan05/HANDOFF_TV4_TUAN5.md` §4.3).
- Tuần 6: thử thêm `OpenTelemetry.Exporter.Prometheus.AspNetCore` + pipeline metrics ở `deploy/otel-collector-config.yaml`; nếu **không** làm được (rủi ro CI locked-mode) thì **quyết định + ghi rõ lý do** trong runbook, coi là giới hạn đã biết.
- Bằng chứng: `/metrics` 200 + số liệu scrape thật.

#### W6-6 — Release & bàn giao (ngày 3–4)

Checklist theo `KE_HOACH_DU_AN.md` §12.5 — mỗi mục có bằng chứng hoặc ghi "chưa đạt + lý do":

| # | Mục release | Cách kiểm ở tuần 6 |
|---|---|---|
| 1 | 34/34 FR có implementation + test + người chịu trách nhiệm | Bảng mapping FR→file/test |
| 2 | 30/30 NFR có kết quả đo hoặc ghi chưa đạt + lý do | Trích `SO_EVIDENCE_TUAN_5/6` |
| 3 | 10/10 CONS kiểm tra; ADR mâu thuẫn đã chốt | Đối chiếu ADR |
| 4 | 4 người × 24 K có minh chứng + demo + reviewer xác nhận | Chờ Tâm — W6-7 |
| 5 | 33 endpoint baseline + route ADR đối chiếu OpenAPI, có test | Đối chiếu Scalar/OpenAPI |
| 6 | 5 critical E2E pass; coverage ≥ 80%; không lỗi chặn luồng/lộ dữ liệu | W6-2/W6-3 |
| 7 | Compose/Nginx/HTTPS/secrets/volumes/backup/restore chạy thật | `docs/RUNBOOK.md` + `Tuan05/logs` |
| 8 | README, Scalar, ADR, **CHANGELOG**, báo cáo, **tài khoản demo** bàn giao | Bổ sung CHANGELOG/README |
| 9 | Ghi rõ production dependencies chưa cấu hình / giới hạn hạ tầng | Mục Giới hạn runbook |

- **Script bàn giao**: lệnh dựng từ checkout sạch (A1 runbook) + lệnh backup/restore (A2) + **hướng dẫn secrets** (không commit secret; biến môi trường cần đặt).

#### W6-7 — 24/24 ô K + mapping + reviewer xác nhận (ngày 4–5)

- Cập nhật `../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` với bằng chứng mới W6-*.
- Nộp sổ evidence tuần 6; **chờ Tâm xác nhận từng ô + ghi ngày** — điều kiện G7.
- Với ô chưa được xác nhận: ghi rõ "0/x được Tâm xác nhận".

#### W6-8 — Demo publish/media/ops (ngày 5)

Kịch bản 15–20 phút (§12.5):

1. **Tính năng chính (5–7')**: tạo Draft → upload ảnh (thanh progress %) → thêm nguyên liệu/bước → **publish** → xem detail công khai → **gỡ đăng / lưu trữ / xóa** (soft delete) → sitemap/robots cập nhật.
2. **Kỹ năng ngoài phần chính (5–7')**: OTEL/Seq trace HTTP→DB theo TraceId · `/metrics` scrape · 2 API round-robin + failover · TLS/HSTS.
3. **Còn lại**: giải thích architecture/schema (media, status, soft-delete ADR-TV4-003), test (PathTraversal 7/7, RecipeChildIdGeneration 3), xử lý 1 tình huống lỗi do Tâm chọn.
4. **Tự dựng app từ checkout sạch** theo README + **phục hồi 1 backup** + **tìm log/trace** của request vừa thao tác.

#### W6-9 — Nợ nhỏ còn lại (song song, cắt trước nếu trượt)

| Việc | Ghi chú | Ai/điều kiện |
|---|---|---|
| `npm audit` nâng major | 44 vuln còn lại đều cần nâng major (`next@16`, `tailwindcss` 4.3.3, `jest` 30.x) | **Chờ nhóm chốt** — không tự nâng tuần 5 |
| PR riêng `Program.cs` | `X-Sitemap-Generated` (`BUG-W4-09`) + endpoint `/metrics` (`FR-OBS-003`) tách khỏi nhánh tuần 5 để Tâm review | Tâm review |
| Google OAuth (K09) | Không coi mock là hoàn thành | Chờ credentials |
| `NFR-SEO-004` (301 đổi slug) | Chưa có trong dự án | Nợ **TV3** |
| K11 EXPLAIN FTS | Chưa sửa `to_tsquery` (ADR 0003) | Chờ **TV2** |
| **Tâm hậu kiểm PR #36** | PR tuần 5 đã merge nhưng `mergedBy=**Tson-dev**` (không qua Tâm) → đề nghị Tâm hậu kiểm để khớp quy tắc review (`KE_HOACH_DU_AN.md:484`) | Tâm |

### 3.3. Việc kế thừa từ tuần 5 (chưa xong — nguồn để khỏi dò lại)

| Việc | Nguồn | Việc tuần 6 phải làm |
|---|---|---|
| Luồng E2E **`category`** (W5-7 4/5 → G6 5/5) | `Tuan05/HANDOFF_TV4_TUAN5.md` §4.1 · `Tuan05/TRANG_THAI_THUC_HIEN_TUAN_5.md` §1 | **W6-3** |
| Lab L5 **`image-opt`, `query-rollback`** + phase **`cqrs-behavior`** (K04) | `Tuan05/HANDOFF_TV4_TUAN5.md` §4.4 · `Tuan05/SO_EVIDENCE_TUAN_5.md` §3.9 | **W6-4** |
| `/metrics` chưa có pipeline OTLP/Prometheus exporter thật | `Tuan05/HANDOFF_TV4_TUAN5.md` §4.3 | **W6-5** |
| Google OAuth K09 (`N3-A5`) | `Tuan05/HANDOFF_TV4_TUAN5.md` §4.2 | **W6-9** (chờ credentials) |
| `npm audit` nâng major | `Tuan05/HANDOFF_TV4_TUAN5.md` §3.5 | **W6-9** (chờ nhóm chốt) |
| PR riêng `Program.cs` (header sitemap + `/metrics`) review | `Tuan05/HANDOFF_TV4_TUAN5.md` §3.1 | **W6-9** |
| `NFR-SEO-004` (301 đổi slug) · K11 FTS `to_tsquery` | `Report/BAO_CAO_TIEN_DO_TUAN_5_SO_VOI_TUAN_4.md` §6 | **W6-9** (nợ TV3/TV2) |
| 24 ô K chờ xác nhận + mapping K01 | `Tuan05/TRANG_THAI_THUC_HIEN_TUAN_5.md` §4 · `Tuan05/HANDOFF_TV4_TUAN5.md` §3 | **W6-7** (điều kiện G7) |
| 3 việc hạ tầng (lịch backup · kho 30 ngày · rotate JWT) | đề xuất 07/08/09 | ⛔ **Không tự quyết** — §6 |

---

## 4. Thứ tự và ngày dự kiến — từng mục N

> Nhóm **chưa chốt ngày bắt đầu/kết thúc tuần 6** trong tài liệu chung; ngày dưới dùng dương lịch
> tham chiếu từ 14/10 (tuần 5 = 07–11/10) — nếu nhóm chốt lịch khác thì cập nhật lại. Làm lần lượt
> **N1 → N5**; mỗi mục N là một ngày, xong mới sang mục sau.

| Mục | Ngày dự kiến | Việc | Lý do xếp ở đây |
|---|---|---|---|
| **N1** | 14/10 | **W6-1** baseline + mở PR sớm · bắt **W6-2** regression backend/FE | PR càng sớm càng tốt (DoD 12.3); regression là nền của G7 |
| **N2** | 15/10 | **W6-2** E2E full suite ×2 · **W6-3** kiểm tra luồng `category` · bắt **W6-4** | E2E cần staging chạy lặp; luồng `category` phụ thuộc TV2 |
| **N3** | 16/10 | **W6-4** xong Lab L5 + `cqrs-behavior` · **W6-5** `/metrics` · bắt **W6-6** | Lab cần re-run; release cần CHANGELOG |
| **N4** | 17/10 | **W6-6** release/CHANGELOG/bàn giao · **W6-7** nộp mapping + sổ evidence | Lấy số liệu đã đo 14–16/10 |
| **N5** | 18/10 | **W6-8** demo publish/media/ops · **W6-9** nợ nhỏ · chốt sổ, nộp review Tâm | Cổng **G7** cuối tuần |

**Cắt nếu trượt tiến độ** — theo thứ tự: (1) W6-9 `npm audit` major → K11 FTS · (2) W6-5 `/metrics` exporter (giữ bộ đếm in-memory + ghi lý do) · (3) W6-4 `image-opt`.
**Không cắt**: W6-1, W6-2, W6-3 (điều kiện G6/G7), W6-6 (release bàn giao), W6-7 (điều kiện G7), W6-8 (demo).

---

## 5. Cổng G7 — checklist nghiệm thu cuối tuần 6

- [ ] **Regression pass**: backend ≥ 430 + 5 (`Skipped=0`), coverage ≥ 80%, FE `tsc`/`lint`/`build`/Jest sạch, **E2E full suite ×2 xanh**
- [ ] **5 E2E flows pass** trên staging (register · login · category · draft · publish) — hoặc ghi rõ luồng `category` ⬜ chờ TV2
- [ ] **Release checklist §12.5** đủ 9 mục (README · Scalar · ADR · **CHANGELOG** · tài khoản demo · 34 FR/30 NFR/10 CONS · 33 endpoint · Compose/Nginx/HTTPS/secrets/backup/restore)
- [ ] **Bàn giao**: runbook + hướng dẫn backup/restore + **secrets instructions** (không commit secret)
- [ ] **Lab L5**: `image-opt` + `query-rollback` xử lý hoặc ghi ⬜ rõ; phase **`cqrs-behavior`** (K04) đã chạy
- [ ] **24/24 ô K** có minh chứng + **reviewer Tâm xác nhận và ghi ngày** (điều kiện G7; đối chiếu `MAPPING_K01_...`)
- [ ] **Demo độc lập** publish/media/ops 15–20 phút (tự dựng từ checkout sạch + phục hồi backup + tìm trace)
- [ ] **PR tuần 6** đã mở và được **Tâm review** (không tự merge)
- [ ] `TRANG_THAI_THUC_HIEN_TUAN_6.md` + `SO_EVIDENCE_TUAN_6.md` + `HANDOFF_TV4_TUAN6.md` cập nhật, ô nào thiếu ghi rõ thiếu

---

## 6. Block / việc chờ nhóm (không tự quyết)

| Việc | Trạng thái | Cần ai | Nguồn |
|---|---|---|---|
| Luồng E2E `category` do ai viết + `category.spec.ts` | 🟡 Chờ | TV2 | `Tuan05/SO_EVIDENCE_TUAN_5.md` §5 |
| `query-rollback` — tài khoản E2E **sở hữu** công thức (hoặc `GetRecipesQuery` filter chủ sở hữu) | 🟡 Chờ | TV3/Tâm | `Tuan05/HANDOFF_TV4_TUAN5.md` §4.4 |
| Google OAuth credentials (K09, `N3-A5`) | ⬜ Chờ credentials | Nhóm | `Tuan05/HANDOFF_TV4_TUAN5.md` §4.2 |
| `npm audit` nâng major (next@16/tailwind 4.3.3/jest 30.x) | 🟡 Chờ chốt | Nhóm | `Tuan05/HANDOFF_TV4_TUAN5.md` §3.5 |
| Sửa `to_tsquery` (ADR 0003) để đo EXPLAIN FTS (K11) | 🟡 Chờ | TV2 | `Tuan05/SO_EVIDENCE_TUAN_5.md` §5 |
| `NFR-SEO-004` (301 đổi slug) | 🟡 Nợ | TV3 | `Report/BAO_CAO_TIEN_DO_TUAN_5_SO_VOI_TUAN_4.md` §6 |
| Nơi đặt lịch backup · kho lưu 30 ngày · rotate khoá JWT ngoài repo | 🔴/🟡 Chờ chốt | Tâm (+TV2) | đề xuất 07/08/09 |
| Xác nhận 24 ô K + mapping (K01) | ⏳ Chờ Tâm ghi ngày | Tâm | `../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` |
| **Hậu kiểm PR #36** (merged `2312739_NHTSon_D5-D7` với `mergedBy=Tson-dev`, không qua Tâm) | 🟡 Cần đối chiếu | Tâm | `gh pr view 36` |

---

## 7. Tài liệu liên quan

| Tài liệu | Vai trò |
|---|---|
| [`KE_HOACH_TUAN_5_TV4.md`](../Tuan05/KE_HOACH_TUAN_5_TV4.md) | Kế hoạch tuần 5 — tham chiếu format + danh sách W5-x |
| [`../Tuan05/TRANG_THAI_THUC_HIEN_TUAN_5.md`](../Tuan05/TRANG_THAI_THUC_HIEN_TUAN_5.md) | Trạng thái đóng tuần 5 (52/56) + 4 việc còn lại |
| [`../Tuan05/HANDOFF_TV4_TUAN5.md`](../Tuan05/HANDOFF_TV4_TUAN5.md) | Bàn giao tuần 5 + §4 nợ blocked + §6 bước tiếp theo |
| [`../Tuan05/SO_EVIDENCE_TUAN_5.md`](../Tuan05/SO_EVIDENCE_TUAN_5.md) | Sổ evidence tuần 5 + phụ thuộc thành viên khác |
| [`../Report/BAO_CAO_TIEN_DO_TUAN_5_SO_VOI_TUAN_4.md`](../Report/BAO_CAO_TIEN_DO_TUAN_5_SO_VOI_TUAN_4.md) · [`../Report/BAO_CAO_TIEN_DO_TUAN_5_TV4_SRS.md`](../Report/BAO_CAO_TIEN_DO_TUAN_5_TV4_SRS.md) | Báo cáo tiến độ tuần 5 (phi chính thống) |
| [`../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md`](../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md) | Mapping 24 ô K — chờ Tâm xác nhận |
| [`../../../PHAN_CHIA_CONG_VIEC_6_TUAN.md`](../../../PHAN_CHIA_CONG_VIEC_6_TUAN.md) §3.4 dòng tuần 6 + §2 (G7) | Giao việc tuần 6 của TV4 |
| [`../../../KE_HOACH_DU_AN.md`](../../../KE_HOACH_DU_AN.md) §8 (D7) · §12.3 (DoD) · §12.5 (release/demo) | Căn cứ task D7 + cổng G7 + checklist release |
| ADR liên quan | [`../../../adr/0004-staging-deployment-and-disaster-recovery.md`](../../../adr/0004-staging-deployment-and-disaster-recovery.md) · `docs/adr/ADR-TV4-003-quy-uoc-soft-delete.md` · `ADR-TV4-004` |
