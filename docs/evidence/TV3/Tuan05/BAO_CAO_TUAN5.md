# Báo cáo Tuần 5 — TV3 (Huỳnh Quốc Trung, 2312786)

Ngày làm: 07/10/2026. Nhánh: `2312786_HuynhQuocTrung_C7-frontend-tests`. Commit mới (7, không Co-Authored-By theo yêu cầu):

| Commit | Nội dung |
|---|---|
| `62a1c06` | Phần 0 — kiểm lại Tuần 4 độc lập |
| `d2510d6` | T3 — rà soát tính nhất quán dữ liệu |
| `a214675` | T4 — SCHEMA_RECIPE.md |
| `6988788` | T7 — review kiểm thử TV2 |
| `d594846` | T5 — EXPLAIN + k6 |
| `09d2a39` | T1 + T6 — backup/restore + E2E |
| `0c54207` | Cập nhật K24 + bàn giao theo phát hiện Phần 0 |

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
| T1 | Tự deploy/restore | ⚠️ **XONG một phần, BLOCKED RAM phần clone mới** | Backup/restore thật trên `cb_w5_seed`: 5 bảng khớp 100% trước/sau. Checkout `D:\tmp\cb-clean` hoàn toàn mới: BLOCKED RAM (0,24–0,45 GB trống xuyên suốt, không đủ chạy song song bộ build thứ hai) |
| T2 | Migration sạch + nâng cấp lặp | ✅ XONG | 8/8 migration khớp model (`has-pending-model-changes` = không); `--migrate` ×2 idempotent; dữ liệu bẩn (ảnh xoá mềm `IsPrimary=true` + ảnh sống `IsPrimary=true`) **không tạo được** trước khi nâng cấp (chỉ mục cũ tự chặn) — khác giả thuyết ban đầu; **Down** của migration `20261004155213` lỗi thật `23505` nếu dữ liệu bẩn được tạo **sau** khi đã nâng cấp — rollback sạch, không hỏng state. Chi tiết: `SCHEMA_RECIPE.md` mục 6 |
| T3 | Nhất quán dữ liệu (SELECT-only) | ✅ XONG | `cb_w5_seed`: 0 lỗi mọi tiêu chí. `culinary_blog` (dev, chỉ đọc): 4 ảnh xoá mềm còn `IsPrimary=true`; **6 công thức Published rỗng hoàn toàn** (phát hiện mới, vi phạm C02 dữ liệu dev) — không sửa |
| T4 | Schema (ERD/index/RowVersion/migration/SRS) | ✅ XONG | `SCHEMA_RECIPE.md`: ERD Mermaid từ DB sạch thật; RowVersion không đổi khi sửa con — xác nhận bằng code + test đã xanh (không suy đoán); 4 chỗ lệch SRS/DB |
| T5 | Số đo (EXPLAIN + k6) | ✅ XONG (1 phần BLOCKED) | EXPLAIN dùng đúng index trừ tìm kiếm (phát hiện: GIN trigram có thể không bao giờ được chọn cho truy vấn thật của TV2); k6 **20 VU/30s** (đúng yêu cầu): 0% lỗi, p50=23,73ms, p95=40,72ms, p99=53,71ms. BLOCKED: không đo được trang Next.js (phải tắt frontend để tiết kiệm RAM), chỉ 1 lần đo không phải trung vị 3 lần |
| T6 | E2E 5 luồng quan trọng | ⚠️ **3/5 xác nhận PASS, 2/5 xác nhận hết chặn nhưng chưa chạy lại bằng Playwright** | Đăng ký/đăng nhập/tạo công thức: PASS thật (Phần 0). Ảnh + publish-có-ảnh: 3 ca Playwright fail lúc Docker tắt; sau khi bật Docker, xác nhận bằng `curl` (nhẹ hơn) rằng upload ảnh giờ `201` — chưa tự nhận XONG vì chưa chạy lại đúng Playwright (RAM không đủ Chromium+frontend+API đồng thời) |
| T7 | Review kiểm thử TV2 | ✅ XONG | `REVIEW_TEST_TV2.md`: 32 test backend đã PASS; 0 Jest/Playwright frontend của TV2; 5 gợi ý ca còn thiếu (đáng chú ý: không có test HTTP xác nhận `AdminPolicy` chặn non-Admin ở Category) |
| T8 | Staging (compose của TV4) | ✅ XONG (chỉ validate cấu hình, không build/run) | `docker compose -f docker-compose.staging.yml config` exit 0 (file chỉ có trên `origin/main`, chưa merge vào nhánh TV3); route `/api/` của nginx generic, đủ cho mọi endpoint recipe; **không build/run thật** vì cần 2 image Docker build (API+frontend) — BLOCKED RAM, chỉ kiểm cấu hình |
| T9 | Cập nhật sổ minh chứng + báo cáo + bàn giao | ✅ XONG | K24 sửa lại ("đã mở+merge, thiếu review" thay vì "chưa mở"); `TV3_BAN_GIAO_TUAN4.md` mục C/F cập nhật; file này |

## Giả định / điều chưa chắc (ghi rõ theo luật "chưa chắc thì ghi suy đoán")

- Coverage Application 94,23% (hôm nay) khác 95,45% (báo cáo 05/10) — **chưa rõ nguyên nhân chênh lệch**, ghi nhận cả hai số, dùng số hôm nay làm chính thức.
- Nguyên nhân log seed in "25 categories" nhưng DB thật 24 — **chưa rõ**, không phải code TV3, không điều tra sâu thêm.
- Tỷ lệ PASS thật của 3 ca Playwright ảnh khi storage đã lên — **suy đoán cao** (dựa trên curl 201) nhưng **chưa đo bằng đúng công cụ**, không tự nhận XONG.

## VIỆC CỦA TRUNG (cần người, không tự làm được)

- [ ] K18: nghe lại NVDA sau 3 bản sửa (N4, N8, N13, N14, N16, N17...) + điền tay cột 320/768/1200 + zoom 200% trong `lab:LAB_K18_checklist.md`.
- [ ] K24: PR #27 đã merge 05/10 **không qua review của Tâm** — hỏi Tâm có cần review hồi tố không, hoặc nhóm quyết định bỏ qua.
- [ ] Hỏi nhóm: ai cho phép TV4 tự merge PR #27 của TV3? Có cần siết quy trình review cho các PR còn lại của Tuần 5?
- [ ] Merge `origin/main` mới nhất vào nhánh làm việc để lấy 2 bản sửa của TV4 (DbSeeder, `--migrate`) trước khi tiếp tục code Tuần 5.
- [ ] Lighthouse 3 lần lấy trung vị + CWV (ngoài phạm vi Claude Code theo phân công gốc).
- [ ] Khi máy có nhiều RAM hơn (đóng VS Code/Chrome/Zalo tạm): chạy lại đầy đủ T1 (clone sạch `D:\tmp\cb-clean`), T6 (Playwright đầy đủ 11/11 với Docker đã bật), T5 (k6 kèm trang Next.js, lấy trung vị 3 lần), T8 (build+run thật `docker-compose.staging.yml`).
- [ ] Báo với Tâm/Vĩ/Sơn về 2 phát hiện dữ liệu dev: 6 công thức Published rỗng hoàn toàn, 4 ảnh xoá mềm còn `IsPrimary=true` trên `culinary_blog` (quyết định có cần dọn hay không — TV3 không tự sửa DB dev).

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
