# K18 — Checklist a11y WCAG 2.1 AA + kịch bản NVDA (TV3, kiểm tay)

> Phạm vi: phần của TV3 trên SP `D:\CulinaryBlog`, nhánh `2312786_HuynhQuocTrung_C7-frontend-tests`:
> wizard soạn công thức (`/dashboard/recipes/new`, `/dashboard/recipes/{id}/edit`) và danh sách `/dashboard/recipes`.
> **Chưa kiểm bằng NVDA.** Các ô "Kết quả" để trống cho Trung tự điền khi làm tay. Không đánh dấu đạt nếu chưa thử.

## 0. Đã sửa trong code (commit `64e8f8b`) và kiểm tự động bằng Jest

| # | Vấn đề tìm thấy khi rà code | WCAG | Sửa | Test tự động |
|---|---|---|---|---|
| 1 | Bấm "Tiếp"/"Lưu & tiếp": nội dung bước đổi nhưng focus ở lại nút cũ, trình đọc màn hình không biết đã sang bước mới | 2.4.3, 4.1.3 | `<h2 tabIndex={-1} class="sr-only">Bước n/5: …</h2>`, focus vào đó khi đổi bước (không cướp focus lúc mở trang) | `WizardA11y.test.tsx` test 1 |
| 2 | Trạng thái "đang lưu" chỉ thể hiện bằng nút mờ / chữ nghiêng | 4.1.3 | `<p role="status" class="sr-only">Đang lưu…</p>` | test 4 |
| 3 | Bảng nguyên liệu / danh sách bước có nhiều nút cùng tên "Sửa", "Xoá" | 2.4.6, 2.4.9 (tham khảo) | Thêm phần chữ ẩn: "Sửa nguyên liệu Cá lóc", "Xoá bước 1: Sơ chế", "Xoá ảnh 2: …" | test 2, 3 |
| 4 | Cột sắp xếp ở `/dashboard/recipes` chỉ báo bằng mũi tên ↓↑ | 1.3.1 | `aria-sort="ascending/descending"` trên `<th>` | (kiểm tay mục 2.6) |
| — | Đã có từ trước: ô lỗi `aria-invalid` + `aria-describedby` trỏ tới thông báo lỗi; banner lỗi `role="alert"`; thanh bước `aria-current="step"`; nút ▲▼ có `aria-label` | 3.3.1, 4.1.2 | — | test 5 |

Chạy lại: `cd D:\CulinaryBlog\src\frontend; npx jest` (35/35 xanh lúc viết file này).

## 1. Chuẩn bị

- [ ] Postgres + API `http://localhost:5080` + `npm run dev` (frontend `http://localhost:3000`); đăng nhập tài khoản Author.
- [ ] NVDA 2024+ (chế độ duyệt: `NVDA+Space` bật/tắt chế độ focus). Trình duyệt: Chrome hoặc Firefox (ghi rõ): ________
- [ ] Chrome DevTools > Device toolbar để đặt chiều rộng 320 / 768 / 1200 px. Zoom trình duyệt 200% cho mục 1.4.4.
- [ ] (Tuỳ chọn) Lighthouse > Accessibility hoặc axe DevTools, chép điểm vào mục 4.

## 2. Checklist WCAG 2.1 AA (điền Đạt / Không đạt / Không áp dụng + ghi chú)

| Mã | Tiêu chí | Cách kiểm | 320 px | 768 px | 1200 px | Ghi chú |
|---|---|---|---|---|---|---|
| 1.1.1 | Ảnh có alt | Bước 4: ảnh đã tải lên đọc alt hoặc tên công thức; ảnh trang trí ở dashboard `alt=""` | | | | |
| 1.3.1 | Cấu trúc / nhãn | Mỗi ô nhập có nhãn; bảng nguyên liệu có `th`; danh sách bước là `ol`; `aria-sort` ở cột sắp xếp | | | | |
| 1.3.2 | Thứ tự đọc hợp lý | Đọc tuần tự (NVDA phím mũi tên) khớp thứ tự nhìn | | | | |
| 1.4.3 | Tương phản ≥ 4.5:1 | DevTools > Inspect > Contrast: chữ xám `text-gray-400/500`, nút cam `bg-orange-500` chữ trắng, chữ đỏ lỗi | | | | |
| 1.4.4 | Zoom 200% không mất nội dung | Ctrl + (+) tới 200% ở 1200 px | — | — | | |
| 1.4.10 | Reflow 320 px không cuộn ngang (trừ bảng) | Thanh bước, form bước 1 (lưới 3 cột), dòng nguyên liệu (lưới 12 cột) | | — | — | |
| 1.4.11 | Tương phản thành phần UI ≥ 3:1 | Viền ô nhập, viền focus | | | | |
| 1.4.13 | Nội dung hiện khi hover/focus | Không có tooltip tự viết -> ghi "Không áp dụng" nếu đúng | | | | |
| 2.1.1 | Dùng được bằng bàn phím | Làm hết luồng tạo công thức chỉ bằng Tab / Shift+Tab / Enter / Space | | | | |
| 2.1.2 | Không bẫy bàn phím | Hộp `confirm()` xoá: Enter / Esc thoát được | | | | |
| 2.4.3 | Thứ tự focus | Sau "Tiếp": focus nằm ở tiêu đề bước (vô hình), Tab tiếp vào ô đầu tiên của bước | | | | |
| 2.4.6 | Tiêu đề / nhãn mô tả | Tên nút Sửa/Xoá có ngữ cảnh khi đọc bằng NVDA | | | | |
| 2.4.7 | Focus nhìn thấy được | Mọi nút/ô có viền focus rõ (đặc biệt nút thanh bước, nút Sửa/Xoá chữ màu) | | | | |
| 3.2.2 | Nhập liệu không tự đổi ngữ cảnh | Chọn danh mục / gõ không tự chuyển trang | | | | |
| 3.3.1 | Báo lỗi | Bỏ trống tiêu đề -> lỗi hiện dưới ô, NVDA đọc khi focus ô | | | | |
| 3.3.2 | Nhãn / hướng dẫn | Dấu * bắt buộc, gợi ý định dạng ảnh (JPEG, PNG… 5 MiB) | | | | |
| 3.3.3 | Gợi ý sửa lỗi | Thông báo nói rõ cần sửa gì ("Tiêu đề phải từ 5 đến 200 ký tự") | | | | |
| 4.1.2 | Tên, vai trò, giá trị | Thanh bước đọc "hiện tại" ở bước đang mở; nút bị vô hiệu đọc "không khả dụng" | | | | |
| 4.1.3 | Thông báo trạng thái | Lưu nguyên liệu: NVDA đọc "Đang lưu…"; lỗi server: đọc banner lỗi | | | | |

## 3. Kịch bản NVDA (ghi đúng câu NVDA đọc)

| # | Thao tác | Mong đợi | NVDA đọc thực tế | Đạt? |
|---|---|---|---|---|
| N1 | Mở `/dashboard/recipes/new`, nhấn `H` | Nghe "Tạo công thức mới, tiêu đề cấp 1" | | |
| N2 | `D` (landmark) / `NVDA+F7` danh sách mốc | Có "Các bước soạn công thức, điều hướng" | | |
| N3 | Tab tới thanh bước | "1. Thông tin cơ bản, nút, hiện tại" ; các bước sau "không khả dụng" | | |
| N4 | Tab vào ô Tiêu đề, để trống, Tab tới "Lưu & tiếp", Enter | Focus về ô Tiêu đề, đọc "không hợp lệ" + "Tiêu đề phải từ 5 đến 200 ký tự" | | |
| N5 | Điền đủ bước 1, "Lưu & tiếp" | Đọc "Bước 2/5: Nguyên liệu, tiêu đề cấp 2" (focus chuyển tự động) | | |
| N6 | Nhập 1 nguyên liệu, Tab tới "Lưu", Enter | Đọc "Đang lưu…" rồi bảng có dòng mới | | |
| N7 | Bảng: `T` tới bảng, `Ctrl+Alt+mũi tên` qua ô | Đọc tiêu đề cột "Tên / Số lượng / Đơn vị / Ghi chú" | | |
| N8 | Tab tới nút Sửa của dòng | "Sửa nguyên liệu <tên>, nút" | | |
| N9 | Xoá nguyên liệu: hộp xác nhận | NVDA đọc nội dung hộp "Xoá nguyên liệu …?"; Esc huỷ, Enter đồng ý | | |
| N10 | Gõ dòng nháp rồi bấm "Tiếp" | Banner lỗi được đọc ngay ("Còn 1 dòng nguyên liệu chưa lưu…") | | |
| N11 | Bước 3: nút ▲▼ | "Đưa bước 1 xuống, nút"; sau khi bấm, thứ tự đọc đúng | | |
| N12 | Bước 4: ô chọn ảnh, chọn tệp .txt | Đọc "Chọn ảnh"; lỗi "Chỉ nhận ảnh JPEG, PNG, WebP hoặc AVIF" | | |
| N13 | Bước 5 "Xem lại" | Đọc tiêu đề công thức (cấp 2), danh sách nguyên liệu, các dấu ✓ | | |
| N14 | `/dashboard/recipes`: tới cột "Cập nhật" | "Cập nhật ↓, sắp xếp giảm dần" (hoặc tương đương) | | |
| N15 | Tab lọc trạng thái | "Tất cả, nút bật" (aria-pressed) | | |

## 4. Kết quả tổng hợp (điền sau khi làm)

- Ngày kiểm: ________ Người kiểm: Huỳnh Quốc Trung. Trình duyệt + NVDA: ________
- Lighthouse Accessibility (1200 px): ____ / 100. axe: ____ lỗi nghiêm trọng.
- Lỗi còn lại (mô tả, mức độ, cách tái hiện):
  1. ________
  2. ________
- Ảnh chụp / ghi âm lưu tại: `docs/evidence/TV3/k18/` (tạo khi có).

## 5. Rủi ro đã biết (chưa sửa, cần kiểm tay để xác nhận)

- Màu `text-gray-400` (nhãn "(0/2000)", chữ nghiêng dòng đang lưu) có thể dưới 4.5:1 trên nền trắng — **đoán**, cần đo.
- Nút chữ màu (Sửa xanh, Xoá đỏ) không có `focus-visible` riêng; dựa vào viền focus mặc định của trình duyệt.
- Lưới 12 cột của dòng nguyên liệu ở 320 px có thể quá hẹp (ô "SL"/"Đơn vị") — **đoán**, cần xem thật.
- Sau khi xoá một dòng, nút vừa bấm biến mất nên focus rơi về `body`; NVDA có thể đọc lại từ đầu trang.
