# TRẠNG THÁI THỰC HIỆN TUẦN 5 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

- **SRS**: v1.1.1 · **Reviewer**: Nguyễn Thanh Tâm (Nhóm trưởng)
- **Nhánh**: `2312739_NHTSon_D5-D7` từ `origin/main` = `262201b` (merge PR #29 — đã đóng tuần 4)
- **Kế hoạch**: [`KE_HOACH_TUAN_5_TV4.md`](KE_HOACH_TUAN_5_TV4.md) · **Mô tả việc**: [`MO_TA_CONG_VIEC_TUAN_5.md`](MO_TA_CONG_VIEC_TUAN_5.md) · **Sổ evidence**: [`SO_EVIDENCE_TUAN_5.md`](SO_EVIDENCE_TUAN_5.md)
- **Cổng**: **G6** — staging hoàn chỉnh; performance/SEO/a11y có kết quả và giới hạn; 5 E2E pass

---

## 📌 Trạng thái ngày 07/10/2026 — ảnh chụp lúc bắt đầu tuần

> Đây là tài liệu **trạng thái**, cập nhật khi có việc đổi. Mọi ô ghi "0%" nghĩa là **chưa có bằng chứng
> chạy thật trong tuần 5** — kể cả việc thấy "có vẻ đã làm" ở tuần 4.

### 0.1. Tổng quan

| Mục | Trạng thái 07/10 |
|---|---|
| Nhánh | ✅ Tạo `2312739_NHTSon_D5-D7` từ `origin/main` = `262201b` |
| Tài liệu tuần 5 | ✅ Kế hoạch · Mô tả việc · Trạng thái · Sổ evidence (4 file trong `Tuan05/`) |
| Việc W5-1…W5-10 | 🟢 **9/10 khối xong** (cập nhật 11/10: W5-1, W5-2 **6/6**, W5-3, W5-4, W5-5, W5-6, W5-8 **6/6**, W5-10 **8/8**; W5-7 **4/5**, W5-9 **2/5**) |
| PR cho nhánh tuần 5 | ✅ PR #32 (`week-5` → `main`) + PR #33 (`practice/TV4/L4` → `main`) mở 07/10 |
| Baseline trên `262201b` | ✅ **Đã đo lại** trên commit `0a9b1a5` — 426/426, coverage 97.07%, build/format/FE exit 0, CI xanh (`SO_EVIDENCE_TUAN_5.md` §1) |

### 0.2. Số liệu kế thừa từ tuần 4 (chưa phải số của tuần 5)

| Hạng mục | Số (tuần 4, tại `fd90572`) | Trạng thái dùng cho tuần 5 |
|---|---|---|
| `dotnet test -c Release` | **426/426 pass** (421 + 5 `ConcurrencySpike`), Skipped 0 | ⏳ Phải đo lại trên `262201b` |
| Build / format | 0 warning · 0 error · `dotnet format` exit 0 | ⏳ Đo lại |
| Coverage gate | `CulinaryBlog.Application` **96.31%** ≥ 80% | ⏳ Đo lại |
| Frontend | Jest **85/85** · `tsc`/`lint`/`build` exit 0 | ⏳ Đo lại |
| Playwright | **26/26** (publish 4 + search 12 + upload 10) | ⏳ Chạy lại + mở rộng 5 luồng G6 |
| k6 | 3 lần, 0.00% lỗi, p50 6.23ms / p95 14.14ms / p99 25.06ms — **đo trên dev** | ⏳ Đo lại **trên staging** |
| CI | Xanh mới nhất được xác nhận: `8a585ef` (Backend + Frontend) | ⏳ Xác nhận run cho `main` sau PR #29 |

> ⚠️ Số tuần 4 **không** được dùng làm số chính của tuần 5. Việc W5-1 là đo lại toàn bộ.

---

## 1. Tóm tắt tiến độ

| # | Khối | Việc | Kế hoạch | Xong | Đang làm | Còn lại | Tỷ lệ |
|---|---|---|---|---|---|---|---|
| W5-1 | Mở đầu | Baseline + mở PR | 3 | 3 | 0 | 0 | 100% |
| W5-2 | Staging | Deploy lặp lại được + restore + trace | 6 | 6 | 0 | 0 | 100% |
| W5-3 | Staging | 2 API instance + Nginx upstream + health | 6 | 6 | 0 | 0 | 100% |
| W5-4 | Bảo mật | HTTPS / HSTS / CORS / volumes | 5 | 5 | 0 | 0 | 100% |
| W5-5 | Vận hành | Runbook 7 mục | 7 | 7 | 0 | 0 | 100% |
| W5-6 | UI | Progress upload + Unpublish/Archive/**Xóa** | 5 | 5 | 0 | 0 | 100% |
| W5-7 | Kiểm thử | 5 E2E flows trên staging | 5 | 4 | 0 | 1 | 80% |
| W5-8 | Số đo | k6 + SEO + EXPLAIN + cache-hit + metrics | 6 | 6 | 0 | 0 | 100% |
| W5-9 | Lab/nợ | Zod/RHF · OAuth · **4 phase L5** · `cqrs-behavior` · npm audit | 5 | 2 | 0 | 3 | 40% |
| W5-10 | Bản vá/nợ | `BUG-W4-01/02/03` · secret A1 · PR L4 · ADR · path traversal | 8 | 8 | 0 | 0 | 100% |
| **Tổng** | | | **56** | **52** | **0** | **4** | **93%** |

> Tỷ lệ tính theo **số việc có bằng chứng chạy thật trong tuần 5**.
> **07/10 — bổ sung W5-10 + mở rộng W5-2/3/6/8/9 sau rà soát nợ trong báo cáo tuần 4** → tổng **39 → 56**.

---

## 2. Việc đang làm / kế hoạch theo ngày

| Ngày | Việc dự kiến | Trạng thái |
|---|---|---|
| 07/10 | W5-1 baseline + mở PR #32/#33 · **W5-2 items 1–5** (run1+run2 sạch, drill backup→restore 14 bảng, trace HTTP→EFCore→Postgres) · **W5-10** (PR `practice/TV4/L4` + dọn secret A1, CI xanh) | ✅ Xong (mục 6 giới hạn → runbook W5-5 10/10) |
| 08/10 | W5-3 profile 2 API (default single + `--profile multi`) · W5-10 (mở rộng `scan-secrets.sh` + test) | ✅ Xong — W5-3 6/6, W5-10 scan mở rộng (3/8) |
| 09/10 | W5-4 HTTPS/HSTS/CORS · W5-10 (ADR + path traversal + BUG-W4-03) | ✅ W5-4 **5/5** + W5-10 (#4/#6/#8); W5-6 dời 10/10 |
| 10/10 | W5-5 runbook · W5-6 xong · W5-9 | ✅ **W5-5** runbook 7 mục + Giới hạn · **W5-6** code + E2E 3/3 + unit test progress + staging runtime verify · **W5-9** npm audit (**critical → 0**), Zod/RHF xác nhận đã dùng, `isr-detail` code fix (ISR thật) |
| 11/10 | W5-7 (5 E2E trên staging) · W5-8 số đo · W5-10 (đối chiếu `BUG-W4-01`) · chốt sổ, nộp review | ✅ **W5-7 4/5**: chặn gốc lỗi ảnh presigned `culinary-s3:9000` (mediaUrl proxy + `imageSrc` bỏ presigned, commit `9f3dd4c`), sửa spec E2E lỗi thời, **full suite 40/40** pass chống staging 2 lần liên tiếp; còn luồng `category` chờ TV2. ✅ **W5-8 6/6**: k6 **102 VU** 0% lỗi + SEO + EXPLAIN + cache-hit + `/metrics` (200 Prometheus) · **W5-10 8/8**: `BUG-W4-01` chốt + test hồi quy model-level, header `X-Sitemap-Generated`. ⬜ còn: nộp review | 

*(Ngày tham chiếu — nhóm chưa chốt ngày bắt đầu/kết thúc tuần 5 trong tài liệu chung.)*

---

## 3. Việc kế thừa từ tuần 4 (chưa xong, ghi ở đây để khỏi dò lại)

| Việc | Nguồn | Việc tuần 5 phải làm |
|---|---|---|
| `N2-D1/D2` — progress upload % + nút Unpublish/Archive/**Xóa** | `Tuan04/plan/PLAN_GIAI_DOAN_1_N2_N4.md` §7 + `Report/BAO_CAO_TIEN_DO_TUAN_4_TV4_SRS.md` | **W5-6** |
| `N4-A` runbook 7 mục | `PLAN_GIAI_DOAN_1_N2_N4.md` §7 | **W5-5** |
| `N4-B` deploy staging · 2 API · TLS/HSTS · volumes · **restore** · **trace HTTP→DB** · **`/health/ready` upstream** | `PLAN_GIAI_DOAN_1_N2_N4.md` §7 + 6-tuân L88 + `GĐ1` FR-OBS-001 | **W5-2, W5-3, W5-4** |
| 5 E2E flows (G6) — tuần 4 mới có 2 luồng của TV4 | `PLAN_GIAI_DOAN_1_N2_N4.md` §7 | **W5-7** |
| `N3-A3` Zod/RHF · `N3-A5` Google OAuth (chờ credentials) | `PLAN_GIAI_DOAN_1_N2_N4.md` §7 | **W5-9** |
| Số đo load/SEO trên staging | 6-tuần L88 | **W5-8** |
| **4** phase Lab L5 chưa PASS — thêm **`query-rollback`** (báo cáo viết 3, sổ ghi 4 phase FAIL) | `Tuan04/SOK_LAB_L5.md` §1 | **W5-9** (cắt trước) |
| Phase **`cqrs-behavior`** (K04) chưa từng chạy trong L5 | `Tuan04/SO_EVIDENCE_TUAN_4.md` K04 | **W5-9** |
| **EXPLAIN re-run** (`N2-6` bị cắt) + **cache-hit ratio** (K12 gap, `N2-5`) | `KE_HOACH_TUAN_4_TV4_V2.md` §4.4 + `GĐ1` §6.3–6.4 | **W5-8** |
| **Metrics scrape thật** (`FR-OBS-003` 90% — chưa có bằng chứng số liệu chạy thật) | `Report/BAO_CAO_TIEN_DO_TUAN_4_TV4_SRS.md` | **W5-8** |
| `npm audit` 10 vulnerability (9 high, 1 critical) | `Report/BAO_CAO_TIEN_DO_TUAN_4_TV4_SRS.md` | **W5-9** |
| **A1 secret còn lại** — `Password=postgres`/`minioadmin` trong `appsettings*.json` + `backend.yml`; `scan-secrets.sh` chưa quét appsettings/workflow | `report/BAO_CAO_LOI_TUAN_4_TV4.md` A1 | **W5-10** (P0, không cắt) |
| **PR cho `practice/TV4/L4`** chưa có (P1 "nâng lên bắt buộc") | `KE_HOACH_V2` §4.2 + §4.4 cut-list | **W5-10** (K24) |
| `BUG-W4-01/02/03` (đối chiếu + sửa trong W5-10) · `BUG-W4-06` ADR · `BUG-W4-09` header sitemap | `report/BAO_CAO_LOI_TUAN_4_TV4.md` | **W5-10** |
| **ADR soft-delete `Recipe` vs `Category`** + đổi tên test; test path traversal `../` (`NFR-SEC-004`) | `GĐ3` + `GĐ1` §7 | **W5-10** |
| Số Hangfire server khi 2 API chưa xác nhận | `Report/BAO_CAO_TIEN_DO_TUAN_4_TV4_SRS.md` | **W5-3** |
| 3 việc hạ tầng chờ chốt (lịch backup · kho 30 ngày · rotate JWT) | đề xuất 07/08/09 | ⛔ **Không tự quyết** — §5 |
| 24 ô K chờ Tâm xác nhận + mapping K01 | `../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` | Nộp lại + chờ xác nhận |

**Không thuộc TV4 tuần 5**: checklist WCAG/responsive (**TV2**), Jest/RTL frontend unit test (**TV1**).

---

## 4. Ô kỹ năng K01–K24 (giữ nguyên số của tuần 4 — chưa ô nào được Tâm xác nhận)

| Mức | Số ô | Ô | Ghi chú tuần 5 |
|---|---:|---|---|
| 🟢 đủ bằng chứng | 6 | K08, K13, K19, K20, K21, K24 | Chờ Tâm xác nhận và ghi ngày; K24 mở lại vì **PR `practice/TV4/L4` chưa có** |
| 🟢 gần đạt | 8 | K02, K03, K07, K10, K12, K14, K15, K23 | K10/K23 mở lại ở W5-4/W5-2; **K12** thêm đo cache-hit ratio (W5-8) |
| 🟡 có nền, thiếu lab/đo lại | 8 | K01, K04, K05, K06, K11, K16, K17, K22 | K05/K17 mở ở W5-6; K22 mở ở W5-8; **K04** thêm phase `cqrs-behavior`; **K06/K11** EXPLAIN re-run |
| ⬜ còn thiếu thật | 2 | K09 (Google credentials), K18 (thuộc TV2) | Không tự đánh dấu đạt |

---

## 5. Block / việc chờ nhóm

| Việc | Mức | Cần ai | Từ ngày |
|---|---|---|---|
| Chốt nơi đặt lịch backup (đề xuất 07) | 🔴 | Tâm + TV2 | 30/09 |
| Kho lưu backup 30 ngày (đề xuất 08) | 🔴 | Tâm + TV2 | 30/09 |
| Rotate/xoá khoá JWT ngoài repo (đề xuất 09) | 🟡 | Tâm | 03/10 |
| Google OAuth credentials (K09) | 🟡 | Nhóm | từ tuần 3 |
| Tâm xác nhận 24 ô K + mapping | 🟡 | Tâm | 30/09 |
| Chốt `BUG-W4-01` C1/C2 (đụng schema TV3) | 🟡 | Tâm + TV3 | 07/10 |
| `query-rollback` — cần tài khoản E2E **sở hữu** công thức (hoặc `GetRecipesQuery` filter chủ sở hữu) | 🟡 | TV3/Tâm | 07/10 |
| K11 EXPLAIN FTS phụ thuộc TV2 sửa `to_tsquery` | 🟡 | TV2 | 07/10 |
| Luồng E2E `category` do ai viết | 🟡 | TV2 | 07/10 |

---

## 6. Rủi ro đang theo dõi

| Rủi ro | Mức | Xử lý |
|---|---|---|
| 56 việc trong 5 ngày (07/10 tăng từ 39 sau rà soát nợ) | 🔴 | Thứ tự + thứ tự cắt ở `MO_TA_CONG_VIEC_TUAN_5.md` §4; không cắt A1 + PR L4 (điều kiện G6/DoD) |
| A1 secret còn lại + PR L4 là việc tuần 4 đã ghi "không cắt" nhưng chưa làm | 🔴 | W5-10 xếp từ ngày 07/10, song song với W5-1/2 |
| Staging chỉ có 1 API, `container_name` cứng, không TLS | 🔴 | W5-2/3/4 — đây chính là phần G6 đang thiếu |
| D4-UI dở dang từ tuần 3 → không E2E được luồng gỡ xuất bản | 🟡 | W5-6 làm cùng ngày với W5-2 |
| Số đo tuần 4 làm trên dev → không đại diện staging | 🟡 | W5-8 đo lại trên staging, ghi giới hạn |
| CI có thể đỏ do thay đổi compose/nginx | 🟡 | Chạy lại CI sau mỗi lần sửa cấu hình |
| Nhánh mới chưa có PR → vi phạm DoD | 🔴 | W5-1 mở PR ngay ngày đầu |

---

## 7. Nhật ký thay đổi

| Ngày | Nội dung |
|---|---|
| 07/10 | Tạo nhánh `2312739_NHTSon_D5-D7` từ `origin/main` = `262201b`; lập 4 tài liệu tuần 5; ghi trạng thái bắt đầu **0/9 khối việc**. Chưa chạy kiểm định trên nhánh mới. |
| 07/10 | **Rà soát nợ trong báo cáo tuần 4** (`GĐ1` §6–§7, `GĐ3`, báo cáo lỗi, báo cáo tiến độ SRS, `SOK_LAB_L5`, `KE_HOACH_V2` §4.4) + kiểm chứng trực tiếp trên code: bổ sung **W5-10** (A1 secret còn lại, PR `practice/TV4/L4`, `BUG-W4-01/02/03`, ADR, path traversal), mở rộng W5-2 (restore + trace), W5-3 (health/upstream + Hangfire), W5-6 (nút Xóa), W5-8 (EXPLAIN + cache-hit + metrics), W5-9 (4 phase + `cqrs-behavior`) → tổng **39 → 56 việc, 0/10 khối**. |
| 07/10 | **W5-1 xong**: baseline đo lại trên `0a9b1a5` (426/426, coverage 97.07%, build/format/FE/CI xanh) + mở PR #32. **W5-2**: run1 + run2 trên staging đều xanh (idempotent, `culinary-init`, fail-fast `JWT_SIGNING_KEY`), drill backup→restore 14 bảng = N1, trace HTTP→EFCore→Postgres qua Seq (3 span cùng CorrelationId); còn item 6 (giới hạn) chuyển vào runbook W5-5. **W5-10**: PR #33 + dọn secret A1 (`git grep` = 0), CI xanh. 2/10 khối bắt đầu. |
| 08/10 | **W5-3 xong 6/6**: profile `multi` + `culinary-api-2`, nginx upstream 2 server (`zone` + `resolve`) + failover FR-OBS-001, trải đều 50/50 (12req: 6/6), failover 0/8 non-200 khi stop/start api-2, Hangfire = 2 server, cache/queue Redis chung. **W5-10**: mở rộng `scan-secrets.sh` quét `appsettings*.json` + `env:` workflow (BUG-W4-02) + test 9 PASS/0 FAIL + CI thêm bước test. |
| 09/10 | **W5-4 xong 5/5**: TLS staging self-signed (`culinary-certs` → volume `staging_certs`, CN=localhost) + HTTP→HTTPS 301 + HSTS; CORS origin tường minh (`Cors:AllowedOrigins`) + `UseForwardedHeaders` (`KnownIPNetworks 172.16/12`) = BUG-W4-03 (XFF A 10×400+429, XFF B 400); volumes persistent (register→restart db→login 200; Redis appendonly còn sau restart); companion `/health` 503→200 (`HealthChecks__Minio__Host`). **W5-10**: 3 việc — `BUG-W4-03` (commit `afdb4a7`, PR riêng Program.cs), ADR-TV4-003 soft-delete + đổi tên test, test path traversal (7/7 pass). **Companion**: NuGetAudit chặn build → pin ImageSharp 3.1.12 + suppress 5 advisory (ADR-TV4-004, bản vá 4.1.2 là commercial). Build/test/format xanh; chờ push + CI. |
| 10/10 | **W5-5 xong 7/7**: `docs/RUNBOOK.md` 7 mục A1–A7 với số liệu thật (run2 24s tới ready, dump 287KB/14 bảng, failover, trace theo TraceId, Seq) + mục Giới hạn → commit `92e34be`; **W5-6 xong 5/5**: unit test progress (commit `2969f94`), E2E Gỡ đăng/Lưu trữ/Xóa 3/3 + CORS `127.0.0.1:3000` (commit `367b42e`); **W5-9 (2/5)**: `npm audit` critical → 0 (next 15.1.11→15.5.27), Zod/RHF xác nhận đã dùng sâu, `isr-detail` code fix (`generateStaticParams` → ISR thật, 104 trang) → commit `dfcbfa7`; evidence 10/10, CI xanh. |
| 11/10 | **W5-7 (4/5)**: sửa gốc lỗi ảnh nháp (`imageSrc` bỏ presigned `culinary-s3:9000` → luôn proxy `/resources/images` để `AuthImage` fetch-Bearer-blob; `ImagesStep` bỏ UI liên kết hết hạn) + cập nhật test `recipe-jsonld` + sửa spec E2E lỗi thời (`#err-title`/`#err-category`, placeholder dòng nháp, loại `__next-route-announcer__`) → commit `9f3dd4c`. Chạy E2E chống staging: **40/40 pass** (`recipe-publish` 4/4, `wizard-week4` 10/10, `create-recipe`, `search` 12, `upload-security` 10, `recipe-unpublish` 3); jest 86/86, `tsc`/`lint`/`build` exit 0. Luồng `category` còn ⬜ (chờ TV2). Evidence §3.7. |
