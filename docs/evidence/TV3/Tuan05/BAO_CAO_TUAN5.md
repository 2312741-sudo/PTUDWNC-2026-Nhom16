# Báo cáo Tuần 5 — TV3 (Huỳnh Quốc Trung, 2312786)

Ngày làm: 07–08/10/2026. **Cập nhật 08/10**: 8 commit dưới đây ban đầu làm trên nhánh `2312786_HuynhQuocTrung_C7-frontend-tests` (lúc đó chưa biết nhánh này đã merge vào `main` từ 05/10 — xem Phần 0 mục 5). Sau khi xác nhận `66cd33e` (tip cũ) đã là tổ tiên thật của `origin/main` (`git merge-base --is-ancestor`, PR #27 merge thường không phải squash, 75 commit), đã tạo nhánh mới **`2312786_HuynhQuocTrung_C8-tuan5`** từ `origin/main` và cherry-pick sạch cả 8 commit (không xung đột — 2 file sửa đổi không bị ai khác đụng giữa `66cd33e` và `origin/main`). Nhánh làm việc hiện tại: `2312786_HuynhQuocTrung_C8-tuan5`. Commit (không Co-Authored-By theo yêu cầu):

| Commit (trên `C8-tuan5`, cherry-pick từ `C7-frontend-tests`) | Nội dung |
|---|---|
| `8c1da15` | Phần 0 — kiểm lại Tuần 4 độc lập |
| `10532a9` | T3 — rà soát tính nhất quán dữ liệu |
| `d727331` | T4 — SCHEMA_RECIPE.md |
| `368babf` | T7 — review kiểm thử TV2 |
| `19a6bf1` | T5 — EXPLAIN + k6 (lần 1) |
| `94b6ad8` | T1 + T6 — backup/restore + E2E (lần 1) |
| `9d49fbe` | Cập nhật K24 + bàn giao theo phát hiện Phần 0 |
| `0c88ce5` | T9 — BAO_CAO_TUAN5.md + GIAI_THICH_TUAN5.md |

**Commit mới thêm 08/10** (sau khi chạy lại Playwright/k6 thật trên nhánh `C8-tuan5`): `9c66559`, `0711e30` (lần trước), `1daddbf` (K17 — xác nhận B5 có chủ đích), `7da131f` (fix assertion `wizard-week4.spec.ts`).

## Bảng Phần 0

| # | Việc | Trạng thái | Số thật |
|---|---|---|---|
| 1 | Đối chiếu PHAN_CHIA_CONG_VIEC_6_TUAN.md + README trên `origin/main` | ✅ XONG | G4/G5 (PHAN_CHIA) ứng tuần 4, G6 ứng tuần 5; README gọi khác (G5 cho tuần 5) — đã ghi cả hai |
| 2 | Kiểm 66 hash / 25 file / 15 tên test trong sổ minh chứng | ✅ XONG | 100% có thật, không bịa |
| 3 | Chạy lại độc lập (restore/format/test+coverage/npm/build/Playwright) | ✅ XONG (1 phần BLOCKED) | Backend 281/281+5/5, coverage Application **94,23%**; Jest 85/85; build 15/15; Playwright 8/11 (3 fail do Docker tắt, đã xác nhận hết khi bật lại — xem T6) |
| 4 | Kiểm GitHub Actions qua REST API | ✅ XONG | Không cần `gh`; Backend CI + Frontend CI xanh cho PR #27 trên nhánh riêng; main sau merge từng đỏ 4 lần (đã có lời giải trong README), nay xanh; "Backup PostgreSQL" đang đỏ trên `main` (không phải việc TV3) |
| 5 | Phát hiện quan trọng | ✅ Đã ghi | **PR #27 đã mở VÀ đã merge** 05/10 bởi TV4 — khác giả định "chưa mở PR"; DbSeeder + `--migrate` nuốt lỗi **đã được TV4 sửa trên `main`** (nhánh TV3 chưa có); tự tái hiện lỗi `--migrate` giả thành công thật trên máy (vô tình nhắm nhầm `culinary_blog` do gõ sai biến môi trường — đã kiểm không có thiệt hại) |
| 6 | Ghi kết quả vào `KIEM_TRA_TUAN4.md` | ✅ XONG | Commit `62a1c06` |

## Bảng T1–T9

| # | Việc | Trạng thái | Chi tiết thật |
|---|---|---|---|
| T1 | Tự deploy/restore | ⚠️ **XONG một phần, BLOCKED RAM phần clone mới (lần 3, 08/10)** | Backup/restore thật trên `cb_w5_seed`: 5 bảng khớp 100% trước/sau. Checkout `D:\tmp\cb-clean` hoàn toàn mới: **vẫn BLOCKED RAM** — RAM đo ngay trước lúc quyết định (sau khi đã chạy xong T6, tắt API/FE, dọn MSBuild): **0,33–0,56 GB**, dưới mốc 1 GB yêu cầu — không cố |
| T2 | Migration sạch + nâng cấp lặp | ✅ XONG | 8/8 migration khớp model (`has-pending-model-changes` = không); `--migrate` ×2 idempotent; dữ liệu bẩn (ảnh xoá mềm `IsPrimary=true` + ảnh sống `IsPrimary=true`) **không tạo được** trước khi nâng cấp (chỉ mục cũ tự chặn) — khác giả thuyết ban đầu; **Down** của migration `20261004155213` lỗi thật `23505` nếu dữ liệu bẩn được tạo **sau** khi đã nâng cấp — rollback sạch, không hỏng state. Chi tiết: `SCHEMA_RECIPE.md` mục 6 |
| T3 | Nhất quán dữ liệu (SELECT-only) | ✅ XONG | `cb_w5_seed`: 0 lỗi mọi tiêu chí. `culinary_blog` (dev, chỉ đọc): 4 ảnh xoá mềm còn `IsPrimary=true`; **6 công thức Published rỗng hoàn toàn** (phát hiện mới, vi phạm C02 dữ liệu dev) — không sửa |
| T4 | Schema (ERD/index/RowVersion/migration/SRS) | ✅ XONG | `SCHEMA_RECIPE.md`: ERD Mermaid từ DB sạch thật; RowVersion không đổi khi sửa con — xác nhận bằng code + test đã xanh (không suy đoán); 4 chỗ lệch SRS/DB |
| T5 | Số đo (EXPLAIN + k6) | ✅ XONG (1 phần BLOCKED) | EXPLAIN dùng đúng index trừ tìm kiếm (phát hiện: GIN trigram có thể không bao giờ được chọn cho truy vấn thật của TV2); k6 **20 VU/30s, 2 lần chạy thật** trên nhánh `C8-tuan5`: lần 1 p50=23,73ms/p95=40,72ms/p99=53,71ms (0 SLOW_SQL); **lần 2 (08/10) p50=27,43ms/p95=49,5ms/p99=70,2ms** (0% lỗi, 8 cảnh báo SLOW_SQL — toàn bộ ở giây đầu lúc API/DB vừa khởi động, không lặp lại suốt 30s còn lại). Hai lần đo gần nhau (~20–30ms p50), chưa đủ 3 lần lấy trung vị như yêu cầu — BLOCKED: không đo được trang Next.js (phải tắt frontend để tiết kiệm RAM) |
| T6 | E2E 5 luồng quan trọng | ✅ **XONG (08/10 sáng)** | Đã tự xác định được nhánh (a)/(b) (không cần hỏi TV4): đọc `docs/IMAGE_CONTRACT.md` §7b — *"PA-3 — ✅ chốt 28/09: presigned URL"*, PA-2 (`blob:`) **bị loại tường minh** ("không vẽ được thumbnail sau khi reload wizard") → **đổi `blob:`→presigned URL là CÓ CHỦ ĐÍCH**, không phải hồi quy. Quyền truy cập xác nhận đúng bằng test đã xanh (`RecipeImagePresignedB5Tests`): `Other_member_cannot_read_draft_detail_at_all` → 404 (không lộ Draft), `Anonymous_viewer_of_published_recipe_gets_no_presigned_url` → xem công khai được. → **Nhánh (a)**: đã sửa 2 assertion lỗi thời trong `e2e/wizard-week4.spec.ts` (file của TV3, commit `7da131f`), chạy lại **(d)/(d2)/(g) 3/3 PASS**, sau đó **toàn bộ 11/11 Playwright của TV3 PASS thật** (gồm cả 5 luồng: đăng ký, đăng nhập, tạo công thức, thêm nguyên liệu/bước/ảnh, xuất bản → trang công khai). Giữa chừng phát hiện thêm: `GET /categories` từng trả ID cũ từ DB tạm trước đó do **Redis dùng chung giữa các lần test chưa flush** — đã `FLUSHALL` để sửa, ghi nhận để lần sau tự flush trước khi seed DB mới. Phát hiện phụ: file `e2e/recipe-publish.spec.ts` (không phải của TV3, tác giả TV4/`c37a476`) có 3 ca fail vì `getByLabel` không strict với aria-label "(dòng N)" của TV3 — viết `handoff_TV4_recipe-publish-spec_strict-mode.md`, không sửa |
| T7 | Review kiểm thử TV2 | ✅ XONG | `REVIEW_TEST_TV2.md`: 32 test backend đã PASS; 0 Jest/Playwright frontend của TV2; 5 gợi ý ca còn thiếu (đáng chú ý: không có test HTTP xác nhận `AdminPolicy` chặn non-Admin ở Category) |
| T8 | Staging (compose của TV4) | ⚠️ XONG phần validate cấu hình; **BLOCKED RAM phần build/run thật (08/10)** | `docker compose -f docker-compose.staging.yml config` exit 0; route `/api/` của nginx generic, đủ cho mọi endpoint recipe. **08/10**: mốc cần ≥1,5 GB để build+run thật 2 image Docker (API+frontend) — RAM đo được cao nhất trong phiên chỉ 0,8 GB (lúc chưa chạy Playwright), không đạt mốc — giữ BLOCKED RAM, chưa chứng minh được staging thật chạy được |
| T9 | Cập nhật sổ minh chứng + báo cáo + bàn giao | ✅ XONG | K24 sửa lại ("đã mở+merge, thiếu review" thay vì "chưa mở"); `TV3_BAN_GIAO_TUAN4.md` mục C/F cập nhật; file này |

## Giả định / điều chưa chắc (ghi rõ theo luật "chưa chắc thì ghi suy đoán")

- Coverage Application 94,23% (hôm nay) khác 95,45% (báo cáo 05/10) — **chưa rõ nguyên nhân chênh lệch**, ghi nhận cả hai số, dùng số hôm nay làm chính thức.
- Nguyên nhân log seed in "25 categories" nhưng DB thật 24 — **chưa rõ**, không phải code TV3, không điều tra sâu thêm.
- Tỷ lệ PASS thật của 3 ca Playwright ảnh khi storage đã lên — **suy đoán cao** (dựa trên curl 201) nhưng **chưa đo bằng đúng công cụ**, không tự nhận XONG.

## VIỆC CỦA TRUNG (cần người, không tự làm được)

- [ ] K18: nghe lại NVDA sau 3 bản sửa (N4, N8, N13, N14, N16, N17...) + điền tay cột 320/768/1200 + zoom 200% trong `lab:LAB_K18_checklist.md`.
- [ ] K24: PR #27 đã merge 05/10 **không qua review của Tâm** — hỏi Tâm có cần review hồi tố không, hoặc nhóm quyết định bỏ qua.
- [ ] Hỏi nhóm: ai cho phép TV4 tự merge PR #27 của TV3? Có cần siết quy trình review cho các PR còn lại của Tuần 5?
- [x] ~~Merge `origin/main` để lấy bản sửa DbSeeder/`--migrate`~~ — đã làm 08/10 bằng cách khác tốt hơn: tạo nhánh mới `C8-tuan5` thẳng từ `origin/main` + cherry-pick, không cần merge ngược.
- [x] ~~Quyết định cùng TV4 về B5/blob preview~~ — **08/10 sáng: tự xác định được, không cần hỏi**. Đọc `IMAGE_CONTRACT.md` §7b xác nhận PA-3 (presigned URL) là quyết định đã chốt 28/09, PA-2 (`blob:`) bị loại tường minh; quyền truy cập đúng (test backend đã xanh). Đã sửa 2 assertion trong `wizard-week4.spec.ts` (file của TV3, commit `7da131f`); 11/11 Playwright TV3 PASS. `handoff_TV4_ImagesStep_blob_preview_bi_mat.md` giữ lại làm hồ sơ (đã có kết luận, không cần TV4 trả lời nữa).
- [ ] **Mới 08/10**: `handoff_TV4_recipe-publish-spec_strict-mode.md` — file `e2e/recipe-publish.spec.ts` của TV4 (3 ca) đỏ vì `getByLabel` không strict với aria-label "(dòng N)" của TV3; đề nghị TV4 tự sửa locator trong file của mình.
- [ ] Lighthouse 3 lần lấy trung vị + CWV (ngoài phạm vi Claude Code theo phân công gốc).
- [ ] Khi máy có nhiều RAM hơn (đóng VS Code/Chrome/Zalo tạm, **và tắt các tiến trình MSBuild "node reuse" rác sau mỗi lệnh dotnet** — phát hiện 08/10: 10 tiến trình rác từng chiếm ~1,8 GB; `$env:MSBUILDDISABLENODEREUSE=1` + `dotnet build-server shutdown` đã áp dụng từ 08/10 sáng để hạn chế): chạy lại T1 (clone sạch `D:\tmp\cb-clean`, cần ≥1GB trống), T8 (build+run thật `docker-compose.staging.yml`, cần ≥1,5GB), T5 (k6 kèm trang Next.js, cần lần đo thứ 3 mới đủ trung vị).
- [ ] Báo với Tâm/Vĩ/Sơn về 2 phát hiện dữ liệu dev: 6 công thức Published rỗng hoàn toàn, 4 ảnh xoá mềm còn `IsPrimary=true` trên `culinary_blog` (quyết định có cần dọn hay không — TV3 không tự sửa DB dev).
- [ ] CI "Backup PostgreSQL" đỏ trên `main` — xem `handoff_TV4_backup_postgres.md` (mới viết 08/10), báo Sơn.
- [ ] Nhắc TV4: Redis dùng chung giữa các lần test tạo DB tạm khác nhau có thể trả dữ liệu cache cũ (category ID của DB đã xoá) — nên `FLUSHALL` (hoặc đặt TTL ngắn/khoá cache theo tên DB) trước mỗi lần seed DB mới để tránh nhầm lẫn khi debug.

## Lệnh chạy lại (tóm tắt)

```powershell
# Backend, DB rong moi (doi ten khac culinary_test/culinary_blog)
$env:ConnectionStrings__Database="Host=127.0.0.1;Port=5432;Database=<ten_db_tam>;Username=postgres;Password=postgres"
dotnet run --project src/backend/CulinaryBlog.API -- --migrate
dotnet run --project src/backend/CulinaryBlog.API -- --seed
$env:TEST_DATABASE="Host=127.0.0.1;Port=5432;Database=culinary_test;Username=postgres;Password=postgres"
dotnet test CulinaryBlog.sln --collect:"XPlat Code Coverage"

# Frontend
cd src/frontend; npm ci; npx tsc --noEmit; npm test; npm run build; npm start

# k6 (Docker)
docker run --rm -i -e API=http://host.docker.internal:5080/api/v1 -e SLUG=<slug_that> -e VUS=20 -e DURATION=30s grafana/k6 run --quiet - < tests/k6/recipe-detail.js
```
