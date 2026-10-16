# TRẠNG THÁI THỰC HIỆN TUẦN 6 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

- **SRS**: v1.1.1 · **Reviewer**: Nguyễn Thanh Tâm (Nhóm trưởng)
- **Nhánh**: `2312739_NHTSon_D7` từ `main` = `e93fbe8` (đã gồm tuần 5 qua **PR #36**, merge 13/10/2026)
- **Kế hoạch**: [`KE_HOACH_TUAN_6_TV4.md`](KE_HOACH_TUAN_6_TV4.md) · **Mô tả việc**: [`MO_TA_CONG_VIEC_TUAN_6.md`](MO_TA_CONG_VIEC_TUAN_6.md) · **Sổ evidence**: [`SO_EVIDENCE_TUAN_6.md`](SO_EVIDENCE_TUAN_6.md) · **Bàn giao**: [`HANDOFF_TV4_TUAN6.md`](HANDOFF_TV4_TUAN6.md)
- **Cổng**: **G7** — regression, release, tài liệu và minh chứng đủ 4 người

---

## 📌 Trạng thái ngày 14/10/2026 — ảnh chụp lúc bắt đầu tuần

> Đây là tài liệu **trạng thái**, cập nhật khi có việc đổi. Mọi ô ghi "0%" nghĩa là **chưa có bằng chứng
> chạy thật trong tuần 6** — kể cả việc thấy "có vẻ đã làm" ở tuần 5.
> **Tuần 6 tự chứa**: số liệu + nợ của tuần 5 được chép lại ngay trong file này (kèm nguồn), không cần mở lại `Tuan05/`.

### 0.1. Tổng quan

| Mục | Trạng thái 14/10 |
|---|---|
| Nhánh | ✅ `2312739_NHTSon_D7` từ `main` (`e93fbe8`) + commit `5e84995` (5 tài liệu tuần 6) đã push `origin/2312739_NHTSon_D7` |
| Tài liệu tuần 6 | ✅ Kế hoạch · Mô tả việc · Trạng thái · Sổ evidence · Bàn giao (5 file trong `Tuan06/`) |
| Việc W6-1…W6-9 | 🟡 **W6-1 ✅** · **W6-2 ✅** (backend+FE, E2E ×2 44/46, migration DB sạch+lặp; 2 drift spec + 1 finding model) · **W6-3 ⬜** (chờ TV2) · các khối còn lại chưa làm |
| PR cho nhánh tuần 6 | ✅ **[PR #37](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/pull/37)** → `main` (OPEN · MERGEABLE; chờ Tâm review, **không tự merge**) |
| Baseline tuần 6 | ✅ **Đã đo 14/10** trên nhánh tuần 6 — bảng đầy đủ ở §0.3 |

### 0.3. Kết quả baseline tuần 6 đo ngày 14/10 (N1 · W6-2 backend/FE)

> Môi trường: máy TV4 (Windows), .NET SDK **10.0.401**, Node **v24.21.0**, docker-compose.dev (postgres/redis/s3/mailhog).
> Log: `Tuan06/logs/`. Lệnh y hệt CI (`.github/workflows/backend.yml`, `frontend.yml`).

| Hạng mục | Lệnh | Kết quả 14/10 | So tuần 5 | Log |
|---|---|---|---|---|
| Restore | `dotnet restore CulinaryBlog.sln --locked-mode` | exit 0 | ✅ | `logs/baseline_build.log` |
| Build | `dotnet build CulinaryBlog.sln --no-restore -c Release` | **0 warning · 0 error** | ✅ bằng | `logs/baseline_build.log` |
| Format | `dotnet format --verify-no-changes --no-restore` | exit 0 | ✅ bằng | `logs/baseline_format.log` |
| Test backend | `dotnet test -c Release` (CI flags) | **441/441** `CulinaryBlog.Tests` + **5/5** `ConcurrencySpike` · Failed 0 · **Skipped 0** | ✅ hơn (441 vs 430) | `logs/regression_test.log` |
| Coverage gate | `bash deploy/check-coverage.sh 80 TestResults` | `CulinaryBlog.Application` = **96.17%** ≥ 80% | 🟡 thấp hơn 97.07% (vẫn đạt cổng ≥80%) | `logs/regression_coverage.log` |
| FE typecheck | `npx tsc --noEmit` | exit 0 | ✅ bằng | `logs/regression_frontend.log` |
| FE lint | `npm run lint` | exit 0 (warning `no-img-element` như cũ) | ✅ bằng | `logs/regression_frontend.log` |
| FE unit | `npx jest --ci` | **86/86** · 15 suite | ✅ bằng | `logs/baseline_jest.log` |
| FE build | `npm run build` | exit 0 (16 route) · `ECONNREFUSED` khi sinh sitemap là không chặn (không có API lúc build) | ✅ bằng | `logs/baseline_frontend_build.log` |

> **Đối chiếu plan §2.2**: plan ghi mốc tuần 5 là **430 + 5** và coverage **97.07%**.
> Đo thật trên nhánh tuần 6: **441 + 5 = 446** (khớp chính báo cáo của repo ở commit `400b49a` "dotnet 446/446")
> và coverage **96.17%**. ⇒ Số "430" trong plan là mốc **giữa tuần 5** đã cũ; số đúng cuối tuần 5 là 446.
> Coverage 96.17% vẫn **vượt cổng 80%**; xem `SO_EVIDENCE_TUAN_6.md` §1.
>
> **N2 (17/10)**: Playwright E2E full suite ×2 **đã chạy** chống staging → **44/46 ×2**
> (2 lỗi drift spec/product có sẵn trên `main`, xem `HANDOFF_TV4_TUAN6.md` §3.1). Migration chạy lặp còn ⬜.

### 0.2. Số liệu kế thừa từ tuần 5 (chưa phải số của tuần 6) — để khỏi mở lại `Tuan05/`

| Hạng mục | Số cuối tuần 5 | Nguồn (tuần 5) | Trạng thái dùng cho tuần 6 |
|---|---|---|---|
| `dotnet test -c Release` | **430/430** `CulinaryBlog.Tests` + **5/5** `ConcurrencySpike`, Failed 0, Skipped 0 | `Tuan05/HANDOFF_TV4_TUAN5.md` §1 | ⏳ Phải đo lại (W6-1/W6-2) |
| Build / format | 0 warning · 0 error · `dotnet format` exit 0 | `Tuan05/HANDOFF_TV4_TUAN5.md` §1 | ⏳ Đo lại |
| Coverage gate | `CulinaryBlog.Application` **97.07%** ≥ 80% | `Tuan05/SO_EVIDENCE_TUAN_5.md` §1 | ⏳ Đo lại |
| Frontend | Jest **86/86** · `tsc` exit 0 · `lint` warning-only (`no-img-element`) · `build` exit 0 | `Tuan05/HANDOFF_TV4_TUAN5.md` §1 | ⏳ Đo lại |
| Playwright | **40/40** (chống staging, chạy lặp 2 lần) | `Tuan05/HANDOFF_TV4_TUAN5.md` §2 | ⚠️ Chạy lại trên `main` hiện tại: **44/46 ×2** — **không tái lập 40/40** (drift spec/product, `HANDOFF` §3.1) |
| k6 | 102 VU · p50 82.56ms · p95 **148.56ms** · p99 187.28ms · **0% lỗi** | `Tuan05/logs/w58_k6_vus100.log` | Giữ nguyên (đo trên staging tuần 5), trích khi cần |
| PR tuần 5 | **PR #36 MERGED** (`D5-D7` → `main`, 13/10, `mergedBy=Tson-dev`); PR #32/#33 **CLOSED** (không merge) | `gh pr view 32/33/36` | W6-9 (Tâm hậu kiểm) |

> ⚠️ Số tuần 5 **không** được dùng làm số chính của tuần 6. Việc W6-1/W6-2 là đo lại toàn bộ trên nhánh tuần 6 → **đã đo 14/10 ở §0.3**.

---

## 1. Tóm tắt tiến độ

| # | Khối | Việc | Kế hoạch | Xong | Đang làm | Còn lại | Tỷ lệ |
|---|---|---|---|---|---|---|---|
| W6-1 | Mở đầu | Baseline ✅ + mở PR ✅ (#37) | 3 | 3 | 0 | 0 | 100% |
| W6-2 | Kiểm thử | Regression (backend+FE ✅; E2E ×2 ✅ **44/46**; migration DB sạch+lặp ✅) — nợ: 2 drift spec/product + 1 finding model/migration | 6 | 6 | 0 | 0 | 100% |
| W6-3 | Kiểm thử | Chốt 5/5 luồng G6 (luồng `category`) | 5 | 0 | 0 | 5 | 0% |
| W6-4 | Lab/n��� | Lab L5 image-opt/query-rollback + cqrs-behavior (cqrs-behavior PASS 5/5) | 3 | 1 | 0 | 2 | 33% |
| W6-5 | Quan sA�t | /metrics exporter th��-t — **giữ in-memory** | 2 | 2 | 0 | 0 | 100% |
| W6-6 | Release | Release checklist §12.5 + bàn giao | 9 | 0 | 0 | 9 | 0% |
| W6-7 | Minh chứng | 24/24 ô K + mapping + Tâm xác nhận | 3 | 0 | 0 | 3 | 0% |
| W6-8 | Demo | Demo publish/media/ops | 4 | 0 | 0 | 4 | 0% |
| W6-9 | Nợ nhỏ | `npm audit` major · PR `Program.cs` · OAuth · nợ TV2/TV3 · hậu kiểm PR #36 | 6 | 0 | 0 | 6 | 0% |
| **Tổng** | | | **41** | **9** | **0** | **32** | **~22%** |

> Tỷ lệ tính theo **số việc có bằng chứng chạy thật trong tuần 6**. Bảng cập nhật khi làm.

---

## 2. Việc đang làm / kế hoạch theo ngày

> Ngày tham chiếu từ 14/10 (tuần 5 = 07–11/10); nhóm chưa chốt ngày bắt đầu/kết thúc tuần 6 trong tài liệu chung.

| Ngày | Việc dự kiến | Trạng thái |
|---|---|---|
| 14/10 | **W6-1** baseline ✅ + mở **PR #37** ✅ · **W6-2** regression backend/FE ✅ (E2E để N2) | ✅ W6-1 xong · 🟡 W6-2 4/6 |
| 15/10 | **W6-2** E2E full suite ×2 · **W6-3** kiểm tra luồng `category` · bắt **W6-4** | 🟡 E2E ×2 ✅ **44/46** (2 lỗi drift spec/product — `HANDOFF` §3.1) · W6-3/W6-4 ⬜ |
| 16/10 | **W6-4** xong Lab L5 + `cqrs-behavior` · **W6-5** `/metrics` · bắt **W6-6** | ⬜ Chưa làm |
| 17/10 | **W6-6** release/CHANGELOG/bàn giao · **W6-7** nộp mapping + sổ evidence | ⬜ Chưa làm |
| 18/10 | **W6-8** demo publish/media/ops · **W6-9** nợ nhỏ · chốt sổ, nộp review Tâm | ⬜ Chưa làm |

---

## 3. Việc kế thừa từ tuần 5 (chưa xong — chép lại để khỏi dò `Tuan05/`)

Tuần 5 đóng **52/56 việc (93%)**. 4 việc còn lại **đều blocked** hoặc **nằm trong cut-list**:

| Việc | Ở tuần 5 | Việc tuần 6 phải làm | Ai/điều kiện |
|---|---|---|---|
| Luồng E2E **`category`** (W5-7 4/5 → G6 5/5) | 4/5 luồng xanh (40/40) | **W6-3** | Chờ **TV2** giao `category.spec.ts` |
| Lab L5 **`image-opt`, `query-rollback`** + phase **`cqrs-behavior`** (K04) | 2/5 việc W5-9 xong; 3 còn lại cắt tuần 6 | **W6-4** | `query-rollback` chờ **TV3** (account E2E sở hữu công thức) |
| `/metrics` chưa có pipeline OTLP/Prometheus exporter thật | `RequestMetrics` in-memory | **W6-5** | TV4 (quyết định + ghi lý do) |
| Google OAuth K09 (`N3-A5`) | vẫn chờ credentials | **W6-9** | Chờ **credentials** (nhóm) |
| `npm audit` nâng major | critical → 0; 44 vuln còn lại | **W6-9** | Chờ **nhóm chốt** |
| PR riêng `Program.cs` (header sitemap + `/metrics`) review | chưa review | **W6-9** | **Tâm** review |
| `NFR-SEO-004` (301 đổi slug) · K11 FTS `to_tsquery` | chưa có / chưa đo | **W6-9** | Nợ **TV3** / chờ **TV2** |
| 24 ô K chờ xác nhận + mapping K01 | 0/24 được Tâm xác nhận | **W6-7** | **Tâm** xác nhận + ghi ngày |
| 3 việc hạ tầng (lịch backup · kho 30 ngày · rotate JWT) | chờ chốt | ⛔ **Không tự quyết** — §5 | **Tâm + TV2** |

**Không thuộc TV4 tuần 6**: checklist WCAG/responsive (**TV2**), Jest/RTL frontend unit test (**TV1**), `to_tsquery` (**TV2**), `NFR-SEO-004` 301 (**TV3**).

---

## 4. Ô kỹ năng K01–K24 (kế thừa trạng thái cuối tuần 5 — **0/24 được Tâm xác nhận**)

| Mức | Số ô | Ô | Ghi chú tuần 6 |
|---|---:|---|---|
| 🟢 đủ bằng chứng | 6 | K08, K13, K19, K20, K21, K24 | Củng cố ở W6-2/W6-6/W6-8; chờ Tâm xác nhận + ngày |
| 🟢 gần đạt | 8 | K02, K03, K07, K10, K12, K14, K15, K23 | K07 mở lại ở W6-4 (`query-rollback`); K23 ở W6-8 (demo ops) |
| 🟡 có nền, thiếu lab/đo lại | 8 | K01, K04, K05, K06, K11, K16, K17, K22 | **K04** phase `cqrs-behavior` (W6-4); **K16** `image-opt` (W6-4); K11 FTS chờ TV2 |
| ⬜ còn thiếu thật | 2 | K09 (Google credentials), K18 (thuộc TV2) | Không tự đánh dấu đạt |

Nguồn: `Tuan05/SO_EVIDENCE_TUAN_5.md` §2 + `../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md`.

---

## 5. Block / việc chờ nhóm

| Việc | Mức | Cần ai | Từ ngày |
|---|---|---|---|
| Luồng E2E `category` do ai viết + `category.spec.ts` | 🟡 | TV2 | 11/10 |
| `query-rollback` — tài khoản E2E **sở hữu** công thức (hoặc `GetRecipesQuery` filter chủ sở hữu) | 🟡 | TV3/Tâm | 07/10 |
| Google OAuth credentials (K09) | 🟡 | Nhóm | từ tuần 3 |
| `npm audit` nâng major (next@16/tailwind 4.3.3/jest 30.x) | 🟡 | Nhóm | 10/10 |
| Sửa `to_tsquery` (ADR 0003) để đo EXPLAIN FTS (K11) | 🟡 | TV2 | 07/10 |
| `NFR-SEO-004` (301 đổi slug) | 🟡 | TV3 | 11/10 |
| Chốt nơi đặt lịch backup (đề xuất 07) | 🔴 | Tâm + TV2 | 30/09 |
| Kho lưu backup 30 ngày (đề xuất 08) | 🔴 | Tâm + TV2 | 30/09 |
| Rotate/xoá khoá JWT ngoài repo (đề xuất 09) | 🟡 | Tâm | 03/10 |
| Tâm xác nhận 24 ô K + mapping (K01) | ⏳ | Tâm | 30/09 |
| **Hậu kiểm PR #36** (`mergedBy=Tson-dev`, không qua Tâm) | 🟡 | Tâm | 13/10 |

---

## 6. Rủi ro đang theo dõi

| Rủi ro | Mức | Xử lý |
|---|---|---|
| 41 việc trong 5 ngày nhưng nhiều việc **blocked** (TV2/TV3/nhóm) | 🔴 | Thứ tự + cắt ở `MO_TA_CONG_VIEC_TUAN_6.md` §4; không cắt W6-2/3/6/7/8 |
| Luồng `category` chưa có spec → G6 chưa đủ 5/5 | 🟡 | W6-3 — chờ TV2; ghi rõ, không tự nhận phần TV2 |
| PR #36 merge không qua Tâm → vi phạm DoD review | 🟡 | W6-9 — đề nghị Tâm hậu kiểm |
| 24 ô K vẫn 0/24 được xác nhận → G7 chưa đạt | 🔴 | W6-7 — nộp đầy đủ + chờ Tâm; không tự đánh dấu |
| Release CHANGELOG/tài khoản demo có thể thiếu | 🟡 | W6-6 — checklist §12.5 |
| CI đỏ do thêm exporter `/metrics` (locked-mode) | 🟡 | W6-5 — nếu phá CI thì giữ in-memory + ghi lý do |
| E2E `wizard-week4` (d)/(d2) lệch spec/product **có sẵn trên `main`** → không thể tuyên bố E2E 46/46 | 🟡 | W6-2/`HANDOFF` §3.1 — đề xuất sửa spec (file **TV3**, chờ đồng ý) hoặc đánh known-fail |
| Model EF lệch migration (`PendingModelChangesWarning` bị `Program.cs:101` ẩn; FK `RecipeImages` cascade) | 🟡 | W6-2/`HANDOFF` §3.2 — chờ TV3 điều phối migration (**K06**) |

---

## 7. Nhật ký thay đổi

| Ngày | Nội dung |
|---|---|
| 14/10 | Tạo nhánh `2312739_NHTSon_D7` **từ `main`** (`e93fbe8`, đã gồm tuần 5 qua PR #36); lập 5 tài liệu tuần 6 (Kế hoạch · Mô tả việc · Trạng thái · Sổ evidence · Bàn giao); ghi trạng thái bắt đầu **0/9 khối**. Chưa chạy kiểm định trên nhánh tuần 6. |
| 14/10 | Người dùng commit 5 tài liệu tuần 6 → `5e84995` (push `origin/2312739_NHTSon_D7`). |
| 14/10 | **N1 · W6-2 backend+FE**: restore/build/format exit 0, `dotnet test` **441+5=446** (Failed 0, Skipped 0), coverage gate **96.17%** ≥ 80%, FE tsc/lint/build exit 0, jest **86/86**. Log ở `Tuan06/logs/`. Đối chiếu: mốc 430 cũ trong plan là mốc giữa tuần 5; số đúng là 446. E2E ×2 + migration để N2. |
| 14/10 | **W6-1 xong**: mở **PR #37** (`2312739_NHTSon_D7` → `main`), secret-scan CI pass, chờ Tâm review. |
| 17/10 | **N2 · W6-2 E2E ×2 chống staging**: dựng `docker-compose.staging.yml` (`logs/w62_staging_up.log`) + `next dev`:3000 (`E2E_START_BACKEND=0`, **không** đặt `E2E_API_URL`/`E2E_BASE_URL`) → **44/46 lần 1** · **44/46 lần 2 (3.5m)**. 2 ca `wizard-week4` (d)/(d2) fail **ổn định** = **drift spec/product có sẵn trên `main`** (spec kỳ vọng `src` presigned; product `imageSrc` proxy→blob từ W5-7 `9f3dd4c`) — không do tuần 6, Playwright không nằm trong CI. Chi tiết + **đề xuất sửa spec + tác động project**: `HANDOFF_TV4_TUAN6.md` §3.1. Log: `logs/regression_e2e_run{1,2}.log`. | |
| 17/10 | **N5**: đóng gói sổ evidence tuần 6, sao logs, cập nhật TRANG_THAI/HANDOFF/MAPPING §F; đề xuất quyết định cần Tâm. |
| 17/10 | **N2 · migration chạy lặp** (**K06**): DB sạch `culinary_migtest` (localhost:5433) → `dotnet run -- --migrate` **8/8** exit 0; chạy lặp idempotent (vẫn 8). **Phát hiện drift**: model có pending changes — FK `RecipeImages→Recipes` model=**no-cascade** vs DB=**Cascade**; `Program.cs:101` ẩn cảnh báo nên runtime không lộ, design-time `dotnet ef database update` **fail**. Probe `migrations add` xác định diff rồi **revert sạch**. Log `logs/w62_migration.log`; chi tiết `HANDOFF` §3.2 / `SO_EVIDENCE` §3.2.1. |
