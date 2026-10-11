# Báo cáo K18 + Tuần 6 — TV3 (Huỳnh Quốc Trung, 2312786)

Ngày làm: 08/10/2026. Nhánh: `2312786_HuynhQuocTrung_C9-tuan6` (tạo từ đỉnh `2312786_HuynhQuocTrung_C8-tuan5`, lý do: cần kế thừa 13 commit Tuần 5 + 4 commit K18 chưa merge vào `main`). PR Tuần 5 (`C8-tuan5`) vẫn **chưa merge**.

## Phần 0 — Trạng thái đầu phiên
Nhánh đúng `C8-tuan5` (trước khi tạo `C9-tuan6`), git status sạch, không cherry-pick/merge dở, không tiến trình API/FE/dotnet sót (chỉ PID 26000 có từ trước — VS Code), RAM trống **0,53 GB**. `git fetch` xác nhận `origin/main` không đổi so với lần kiểm trước.

## Phần A — K18 (tên truy cập thật qua Playwright, không chỉ đọc code)

| Việc | Kết quả thật |
|---|---|
| A1 Build lại production, bật API+FE | `.next` xoá, build lại, 15/15 trang; API `--no-auto-migrate`, FE `npm start` |
| A2 Spec mới `k18-a11y-names.spec.ts` | **5/5 PASS** — tên truy cập thật in ra: `"Mô tả"` (không `/2000`), `"Sửa nguyên liệu Nguyên liệu 1"` / `"Xoá nguyên liệu Nguyên liệu 1"` (đúng dấu cách), `"Sửa bước 1: Bước 1"`, `"Sửa công thức {tên}"` (dashboard, kèm tên) |
| A3 Sửa phần tử còn sai | **Không có phần tử nào sai trong code hiện tại** — 3 lỗi NVDA cũ (Tuần 4) đã sửa đúng từ trước (`1ae6ce4`/`08e64ce`/`548e4a7`), có Jest `WizardA11y`/`DashboardA11y` (12/12) đã xanh từ trước, nay thêm Playwright xác nhận lần nữa. Log NVDA của Trung nhiều khả năng ghi trên bản build **cũ** (trước 3 commit sửa) |
| A4 Chụp 320/768/1200 cho 5 bước wizard + dashboard | **18 ảnh** + `SO_DO_SCROLLWIDTH.txt` tại `docs/evidence/TV3/Tuan04/K18_reflow/`. **17/18 mốc OK**; riêng **dashboard ở 320px cuộn ngang THẬT** (`scrollWidth=662` > `innerWidth=320`, xác nhận bằng `window.scrollTo` thực nghiệm — không phải chỉ đọc chỉ số). Đã loại trừ nguyên nhân ở file TV3 (thử `overflow-x-hidden` trong `page.tsx`, không có tác dụng, đã revert) — nghi do `layout.tsx` (TV1, flex `<body>`/`<main>`) — `handoff_TV1_layout_flex_scroll_320px.md`, không sửa |
| A5 Cập nhật checklist + sổ minh chứng | `lab:LAB_K18_checklist.md` (commit lab `aebbac3`), `BANG_MINH_CHUNG_K01_K24.md` (commit `e8aeb9c`) — K18 ghi **"CHƯA (MỘT PHẦN — còn nghe lại NVDA 3 điểm)"**, không tự nhận XONG |

**K18 vẫn KHÔNG được đánh dấu XONG** — còn thiếu: nghe lại NVDA ở N8/N16/N17 sau 3 bản sửa (chỉ Trung làm được), kiểm tay zoom 200%, NVDA các điểm chưa kiểm (tiêu đề trống, alt ảnh, sắp xếp cột, bước 5).

## Phần B — Tuần 6

### B1. Mục tuần 6 của TV3 (đối chiếu `origin/main`)
`PHAN_CHIA_CONG_VIEC_6_TUAN.md` mục 3.3 dòng "6 — C7": *Regression, demo tạo/sửa/nguyên liệu/bước và lab; chốt evidence* — đầu ra: *Reviewer Nguyễn Thanh Tâm xác nhận; tự giải thích aggregate/UoW/version/owned data; bàn giao test và DB docs*. `README.md` dòng tuần 6 khớp: "Regression testing Soạn thảo món; Chốt toàn bộ Evidence cá nhân; Demo phân hệ Soạn thảo & Concurrency; Nghiệm thu & Bàn giao đồ án". Cổng cuối tuần 6 (mục 6): *"Regression pass, evidence hoàn chỉnh, reviewer Tâm xác nhận và demo độc lập; release/docs được bàn giao"*. **Không có ngày hạn cụ thể** — tài liệu gốc tự ghi "chưa có ngày bắt đầu cụ thể nên không tự gán lịch ngày tháng", không bịa thêm.

### B2. Nhánh
`2312786_HuynhQuocTrung_C9-tuan6`, tạo từ đỉnh `C8-tuan5` (không phải `origin/main`) — vì regression Tuần 6 cần chạy đúng trên code đã có 13 commit Tuần 5 + 4 commit K18 (chưa merge vào `main`).

### B3. Bảng W1–W7

| W | Việc | Nhãn | Trạng thái | Số thật |
|---|---|---|---|---|
| W1 | Đọc & ghi mục tuần 6 | [không tốn RAM] | ✅ XONG | Trên |
| W2 | Chốt lại sổ minh chứng | [không tốn RAM] | ✅ XONG | 22 XONG / 2 CHƯA (K18, K24) — không đổi, đã nhất quán sau cập nhật Phần A |
| Thử lại T1 (tuần 5) | [tốn RAM] | ❌ **BLOCKED RAM** | RAM đo trước khi quyết định: 0,56 GB < mốc 1 GB |
| Thử lại T8 (tuần 5) | [tốn RAM] | ❌ **BLOCKED RAM** | Cùng lần đo, < mốc 1,5 GB |
| W3 | Regression toàn bộ | [tốn RAM] | ✅ **XONG, không hồi quy** | Backend **421/421 + 5/5**; `dotnet format --verify-no-changes` exit 0; `tsc --noEmit` exit 0; Jest **85/85**; Playwright (4 spec TV3) **17/17 PASS** |
| W4 | (đã làm ở trên, cùng mục T1/T8) | — | — | — |
| W5 | Tài liệu tự giải thích aggregate/UoW/version/owned data | [không tốn RAM] | ✅ XONG | `docs/evidence/TV3/Tuan06/GIAI_THICH_AGGREGATE_DEMO.md`, commit `233be3f` |
| W6 | Bàn giao test + DB docs | [không tốn RAM] | ✅ XONG | `docs/evidence/TV3/Tuan06/BAN_GIAO_TEST_VA_DB.md`, commit `351d0ee` |
| W7 | Báo cáo + giải thích Tuần 6 | [không tốn RAM] | ✅ XONG | File này + `GIAI_THICH_TUAN6.md` |

## Giả định / suy đoán (ghi rõ)

- Log NVDA của Trung ghi trên bản build cũ — **suy đoán** (không có cách xác minh ngược thời gian build nào đã dùng).
- Cơ chế CSS chính xác gây cuộn ngang dashboard 320px (giả thuyết: flex automatic-minimum-size ở `layout.tsx`) — **suy đoán**, đã loại trừ nguyên nhân ở file TV3 bằng thực nghiệm nhưng chưa tự sửa `layout.tsx` để xác nhận 100%.

## VIỆC CỦA TRUNG (không tự làm được)

- [ ] K18: nghe lại NVDA ở N8, N16, N17 (và N4/N13/N14 nếu muốn đủ) sau 3 bản sửa; kiểm tay zoom 200%; nghe NVDA các điểm chưa kiểm (tiêu đề trống, alt ảnh, sắp xếp cột, bước 5).
- [ ] K24: PR #27 đã merge không qua review Tâm — hỏi Tâm có cần review hồi tố.
- [ ] Gửi tin nhóm: báo TV1 (`handoff_TV1_layout_flex_scroll_320px.md` — dashboard cuộn ngang 320px), TV4 (3 handoff còn mở — xem `BAN_GIAO_TEST_VA_DB.md` mục 3).
- [ ] Mở PR cho nhánh `C9-tuan6` (sau khi merge/đóng PR `C8-tuan5` trước, hoặc gộp — Trung quyết định thứ tự PR).
- [ ] Demo trước thầy/Tâm: dùng `GIAI_THICH_AGGREGATE_DEMO.md` để trả lời câu hỏi về aggregate/UoW/RowVersion.

## Lệnh chạy lại
Xem `docs/evidence/TV3/Tuan06/BAN_GIAO_TEST_VA_DB.md` mục 1.
