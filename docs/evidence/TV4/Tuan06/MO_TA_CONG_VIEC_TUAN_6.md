# MÔ TẢ CÔNG VIỆC TUẦN 6 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

- **SRS**: v1.1.1 · **Reviewer**: Nguyễn Thanh Tâm (Nhóm trưởng)
- **Nhánh**: `2312739_NHTSon_D7` (tạo từ `main` = `e93fbe8`, đã gồm tuần 5 qua PR #36)
- **Task**: **D7** — tuần 6 của TV4: "Regression/release, demo publish/media/ops và lab; chốt evidence"
- **Cổng**: **G7** — regression, release, tài liệu và minh chứng đủ 4 người
- **Kế hoạch**: [`KE_HOACH_TUAN_6_TV4.md`](KE_HOACH_TUAN_6_TV4.md)

> Tài liệu này mô tả **chi tiết việc cần làm trong tuần 6** và **Definition of Done** từng việc.
> Việc đã có sẵn một phần từ tuần 5 được ghi rõ phần nào **còn thiếu** — không viết lại phần đã xong.
> **Tuần 6 tự chứa**: mọi việc kế thừa từ tuần 5 đã được chép lại ở mục §2/§3, không cần mở lại `Tuan05/`.

---

## 1. Ma trận việc → FR/NFR/Kỹ năng

| Việc | FR | NFR | Ô K | Điểm theo kế hoạch tổng |
|---|---|---|---|---|
| W6-1 Baseline + PR tuần 6 | — | `NFR-MAINT-003` | K24 | DoD 12.3 |
| W6-2 Regression toàn hệ thống | — | `NFR-MAINT-002`, `NFR-REL-001` | K21, K24, K06 | D7 ("regression") |
| W6-3 Chốt 5/5 luồng G6 (luồng `category`) | `FR-CAT-*`, `FR-RCP-007` | `NFR-MAINT-002` | K21 | G6 (nợ W5-7) |
| W6-4 Lab L5 còn lại + `cqrs-behavior` | — | — | **K04**, K16, K07 | D6 (nợ W5-9) |
| W6-5 `/metrics` exporter thật | `FR-OBS-003` | — | K20 | D7/D5 (nợ HANDOFF §4.3) |
| W6-6 Release & bàn giao | — | `NFR-MAINT-003` | K24 | D7 ("release") |
| W6-7 24/24 ô K + mapping | — | — | **K01** | G7 ("minh chứng đủ 4 người") |
| W6-8 Demo publish/media/ops | `FR-RCP-006/007`, `FR-FILE-001/002`, `FR-OBS-001/003` | — | K17, K19, K20, K23 | D7 ("demo") |
| W6-9 Nợ nhỏ còn lại | — | `NFR-SEO-004` | K09, K24 | nợ P1/P2 tuần 5 |

---

## 2. Chi tiết từng việc

### W6-1 — Chạy lại baseline trên `main` mới và mở PR tuần 6

| # | Việc | Cách làm | Bằng chứng |
|---|---|---|---|
| 1 | Chạy lại toàn bộ kiểm định | `dotnet build -c Release` · `dotnet format --verify-no-changes` · `dotnet test -c Release` · `bash deploy/check-coverage.sh` · `npx tsc --noEmit` · `npm run lint` · `npm run build` · `npx jest --ci` | `Tuan06/logs/baseline_*.log`, số liệu ghi vào `TRANG_THAI_THUC_HIEN_TUAN_6.md` §2 |
| 2 | Xác nhận CI xanh cho `main` mới | Xem run `Backend week 1` + `Frontend CI` mới nhất sau PR #36 | Link run + trạng thái |
| 3 | Mở PR cho nhánh tuần 6 | PR `2312739_NHTSon_D7` → `main`, mô tả dẫn kế hoạch này; **nhờ Tâm review** (không tự merge như PR #36) | Link PR |

**DoD**: 8 lệnh chạy xanh (hoặc ghi rõ lệnh nào đỏ kèm lý do), CI có run xanh mới nhất được xác nhận, PR mở.

> ⚠️ **Bẫy đã vấp**: `deploy/scan-secrets.sh` cần bash/WSL. Nếu máy thiếu thì quét tay bằng `git grep` và **ghi rõ**, không ghi "secret scan pass".

---

### W6-2 — Regression toàn hệ thống (D7)

| # | Việc | Cách làm | Bằng chứng |
|---|---|---|---|
| 1 | Backend regression | `dotnet test CulinaryBlog.sln -c Release` — kỳ vọng **≥ 430** `CulinaryBlog.Tests` + **5** `ConcurrencySpike`, Failed 0, **Skipped=0** | `Tuan06/logs/regression_test.log` |
| 2 | Coverage gate | `bash deploy/check-coverage.sh` — `CulinaryBlog.Application` ≥ 80% (tuần 5: 97.07%) | `Tuan06/logs/regression_coverage.log` |
| 3 | Frontend regression | `tsc --noEmit` · `npm run lint` · `npm run build` · `npx jest --ci` (kỳ vọng 86/86) | `Tuan06/logs/regression_frontend.log` |
| 4 | **E2E full suite chống staging ×2** | `docker compose -f docker-compose.staging.yml up -d` → `npm run test:e2e` (`--retries=0`), chạy lặp **2 lần** (tuần 5: 40/40 ×2) | `Tuan06/logs/regression_e2e_run{1,2}.log` |
| 5 | Migration chạy lặp | DB sạch + nâng cấp từ bản trước (phối **TV3** điều phối migration) | Log `dotnet ef database update` |
| 6 | Đối chiếu không tụt | So với mốc §2.2 `TRANG_THAI_THUC_HIEN_TUAN_6.md` — không số nào thấp hơn tuần 5 (nếu tụt → mở ticket lỗi) | Bảng trong `TRANG_THAI_THUC_HIEN_TUAN_6.md` |

**DoD**: không hồi quy so với tuần 5; có 2 log E2E cho 2 lần chạy; nếu có tụt thì ghi rõ nguyên nhân + ticket, không giấu số.

---

### W6-3 — Chốt 5/5 luồng G6 (luồng `category`)

| Luồng G6 | Spec hiện có | Do ai viết | Việc tuần 6 |
|---|---|---|---|
| register | `create-recipe.spec.ts` | TV4 | Giữ xanh trong full suite |
| login | `create-recipe.spec.ts` | TV4 | Giữ xanh |
| **category** | `category.spec.ts` (**chưa có**) | **chờ TV2 xác nhận** | Kiểm tra TV2 đã giao chưa → tích hợp + chạy; **chưa có thì ghi ⬜ chờ TV2**, không tự viết thay |
| draft / create-recipe | `wizard-week4.spec.ts`, `create-recipe.spec.ts` | TV4/TV3 | Giữ xanh (tuần 5: 10/10) |
| publish | `recipe-publish.spec.ts` | TV4 | Giữ xanh (tuần 5: 4/4) |

**DoD**: `npx playwright test` trên staging chạy toàn bộ bộ test → **4/5 hoặc 5/5**; bảng "luồng nào do ai viết" ghi trong `SO_EVIDENCE_TUAN_6.md`. Nếu luồng `category` vẫn chưa có spec của TV2 thì **ghi ⬜ + lý do**, không tự nhận phần TV2.

---

### W6-4 — Lab L5 còn lại + phase `cqrs-behavior` (K04)

| # | Việc | Trạng thái đầu tuần 6 | Cách làm / Bằng chứng |
|---|---|---|---|
| 1 | `image-opt` (Lab L5) | ⬜ còn — tuần 4/5 ghi "9 file `<img>` thô, cần re-run" | Sửa theo khuyến nghị L5 (`next/image`), re-run harness `practice/TV4/L5` → log phase |
| 2 | `query-rollback` (Lab L5) | ⬜ còn — **cần tài khoản E2E sở hữu công thức** (`GetRecipesQuery` chưa filter chủ sở hữu) | Nếu có account (TV3 hỗ trợ) → chạy; nếu chưa → ghi ⬜ chờ TV3, **không tự sửa query của TV3** |
| 3 | Phase **`cqrs-behavior`** (K04) | ⬜ **chưa từng chạy** — L5 tuần 4 chỉ chạy 7 phase | Chạy phase trên `practice/TV4/L5`; đây là bằng chứng K04 |

**DoD**: mỗi phase có kết quả PASS/FAIL + commit trên `practice/TV4/L5`; ô nào chưa chạy được ghi rõ lý do.

---

### W6-5 — `/metrics` exporter thật (`FR-OBS-003`)

| # | Việc | Trạng thái đầu tuần 6 | Bằng chứng |
|---|---|---|---|
| 1 | Cân nhắc pipeline metrics OTLP/Prometheus exporter | Tuần 5: `RequestMetrics` giữ số **in-memory** (reset khi restart), **không** thêm package exporter để tránh đổi lock-file/CI locked-mode | `deploy/otel-collector-config.yaml` + `Program.cs` |
| 2 | Quyết định + ghi rõ | Nếu thêm `OpenTelemetry.Exporter.Prometheus.AspNetCore` không phá CI → làm; nếu phá → **quyết định giữ bộ đếm in-memory** + ghi lý do vào runbook (coi là giới hạn đã biết) | Mục "Giới hạn" `docs/RUNBOOK.md` |

**DoD**: có quyết định rõ ràng (làm hoặc không làm kèm lý do); `/metrics` vẫn 200 + số liệu scrape thật.

---

### W6-6 — Release & bàn giao (D7)

Checklist theo `KE_HOACH_DU_AN.md` §12.5 — mỗi mục có bằng chứng hoặc ghi "chưa đạt + lý do":

| # | Mục release | Cách kiểm ở tuần 6 |
|---|---|---|
| 1 | 34/34 FR có implementation + test + người chịu trách nhiệm | Bảng mapping FR→file/test |
| 2 | 30/30 NFR có kết quả đo hoặc ghi chưa đạt + lý do | Trích `SO_EVIDENCE_TUAN_6.md` |
| 3 | 10/10 CONS kiểm tra; ADR mâu thuẫn đã chốt | Đối chiếu ADR |
| 4 | 4 người × 24 K có minh chứng + demo + reviewer xác nhận | Chờ Tâm — W6-7 |
| 5 | 33 endpoint baseline + route ADR đối chiếu OpenAPI, có test | Đối chiếu Scalar/OpenAPI |
| 6 | 5 critical E2E pass; coverage ≥ 80%; không lỗi chặn luồng/lộ dữ liệu | W6-2/W6-3 |
| 7 | Compose/Nginx/HTTPS/secrets/volumes/backup/restore chạy thật | `docs/RUNBOOK.md` + `Tuan05/logs` |
| 8 | README, Scalar, ADR, **CHANGELOG**, báo cáo, **tài khoản demo** bàn giao | Bổ sung CHANGELOG/README |
| 9 | Ghi rõ production dependencies chưa cấu hình / giới hạn hạ tầng | Mục Giới hạn runbook |

**Script bàn giao**: lệnh dựng từ checkout sạch (A1 runbook) + lệnh backup/restore (A2) + **hướng dẫn secrets** (không commit secret; liệt kê biến môi trường cần đặt).

**DoD**: 9 mục có kết quả; CHANGELOG + README cập nhật; tài khoản demo (seed) sẵn; không có secret trong repo/evidence.

---

### W6-7 — 24/24 ô K + mapping + reviewer xác nhận (G7)

| # | Việc | Cách làm | Bằng chứng |
|---|---|---|---|
| 1 | Cập nhật mapping | `../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` thêm bằng chứng W6-* | File mapping |
| 2 | Nộp sổ evidence tuần 6 | `SO_EVIDENCE_TUAN_6.md` — mỗi ô K có `file:line`/commit/log | Sổ evidence |
| 3 | **Chờ Tâm xác nhận từng ô + ghi ngày** | Điều kiện G7; ô chưa được xác nhận ghi rõ "0/x" | Reply/xác nhận của Tâm |

**DoD**: mapping + sổ cập nhật; ghi trung thực số ô **được Tâm xác nhận** (không tự đánh dấu đạt).

---

### W6-8 — Demo publish/media/ops (D7)

Kịch bản 15–20 phút (§12.5):

1. **Tính năng chính (5–7')**: tạo Draft → upload ảnh (**thanh progress %**) → thêm nguyên liệu/bước → **publish** → xem detail công khai → **gỡ đăng / lưu trữ / xóa** (soft delete) → sitemap/robots cập nhật.
2. **Kỹ năng ngoài phần chính (5–7')**: OTEL/Seq trace HTTP→DB theo TraceId · `/metrics` scrape · 2 API round-robin + failover · TLS/HSTS.
3. **Còn lại**: giải thích architecture/schema (media, status, soft-delete ADR-TV4-003) · test (PathTraversal 7/7, RecipeChildIdGeneration 3) · xử lý 1 tình huống lỗi do Tâm chọn.
4. **Tự dựng app từ checkout sạch** theo README + **phục hồi 1 backup** + **tìm log/trace** của request vừa thao tác.

**DoD**: demo chạy độc lập, không cần sự trợ giúp của người khác; có bằng chứng (video/log).

---

### W6-9 — Nợ nhỏ còn lại (song song, cắt trước nếu trượt)

| # | Việc | Trạng thái đầu tuần 6 | Ai/điều kiện |
|---|---|---|---|
| 1 | `npm audit` nâng **major** | 🟡 Tuần 5: 46 → 44 vuln, **critical → 0**; còn lại đều cần nâng major (`next@16`, `tailwindcss` 4.3.3, `jest` 30.x) | **Chờ nhóm chốt** — không tự nâng |
| 2 | PR riêng `Program.cs` | 🟡 `afdb4a7` (`BUG-W4-03`) + header `X-Sitemap-Generated` + `/metrics` — `Program.cs` của TV1 | Tâm review |
| 3 | Google OAuth (K09) | ⬜ chờ credentials | Nhóm — không coi mock là đạt |
| 4 | `NFR-SEO-004` (301 đổi slug) | ⬜ chưa có trong dự án | Nợ **TV3** |
| 5 | K11 EXPLAIN FTS | 🟡 chưa sửa `to_tsquery` (ADR 0003) | Chờ **TV2** |
| 6 | **Tâm hậu kiểm PR #36** | 🟡 PR tuần 5 merge với `mergedBy=Tson-dev`, không qua Tâm | Tâm |

**DoD**: mỗi việc có kết quả ghi nhận (xong / chưa xong + lý do). Đây là nhóm **cắt trước** khi trượt tiến độ.

---

## 3. Quy ước làm việc trong tuần

1. Mỗi việc nhỏ **một commit**, tên commit ghi rõ mã việc (`W6-2`, `D7`…) + FR/NFR/K liên quan.
2. Lỗi phát sinh ghi vào báo cáo lỗi riêng của tuần, **không** sửa ngầm trong kế hoạch.
3. Việc phát sinh ngoài phạm vi (của TV1/TV2/TV3) → ghi mục §6 kế hoạch, **báo trước, không chờ trả lời**.
4. Không push thẳng `main`; mọi thay đổi qua PR được **Tâm** review.
5. Không commit secret; file `*.dump` và khoá JWT không được vào repo (đã có `JwtSigningKeyNotCommittedTests` chặn).

## 4. Thứ tự ưu tiên khi thiếu thời gian

```
W6-1 ──▶ W6-2 ──▶ W6-3 ──▶ W6-6 ──▶ W6-7 ──▶ W6-8
                     │
                     ├──▶ W6-4 (Lab L5 + cqrs-behavior)
                     │
                     ├──▶ W6-5 (/metrics exporter — cắt nếu trượt)
                     │
                     └──▶ W6-9 (npm audit major / nợ nhỏ — cắt đầu tiên)
```

- **Cắt thứ tự**: W6-9 (`npm audit` major → K11 FTS) → W6-5 (`/metrics` exporter — giữ bộ đếm in-memory + ghi lý do) → W6-4 (`image-opt`).
- **Không cắt**: W6-1, W6-2, W6-3 (điều kiện G6/G7), W6-6 (release bàn giao), W6-7 (điều kiện G7), W6-8 (demo).
