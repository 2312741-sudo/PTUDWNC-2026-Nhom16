# K18 — Checklist a11y WCAG 2.1 AA + kịch bản NVDA (TV3, kiểm tay)

> Phạm vi: phần của TV3 trên SP `D:\CulinaryBlog`, nhánh `2312786_HuynhQuocTrung_C7-frontend-tests`:
> wizard soạn công thức (`/dashboard/recipes/new`, `/dashboard/recipes/{id}/edit`) và danh sách `/dashboard/recipes`.
> **NVDA:** đã nghe một đợt ngày 05/10/2026 (Chrome, bản build, đã đăng nhập), nguyên văn ở SP `docs/evidence/TV3/Tuan04/K18_nvda_speech_log.txt` (commit `8bf59fc`).
> Tìm ra 3 lỗi, đã sửa trong code (`1ae6ce4`, `08e64ce`, `548e4a7`), **chưa nghe lại sau khi sửa (quyết định của Trung)**.
> **320/768/1200:** chỉ có kiểm tự động bằng Playwright (ca h2, không cuộn ngang); chưa kiểm tay.
> Quy ước ô: "chưa làm" = cần kiểm/nghe tay nhưng chưa làm; "chưa kiểm" = mục NVDA đã biết là chưa nghe
> (lỗi khi để trống tiêu đề, alt ảnh, sắp xếp cột, bước 5). Không ô nào ghi đạt nếu không có bằng chứng ghi kèm.

## 0. Đã sửa trong code (commit `64e8f8b`) và kiểm tự động bằng Jest

| # | Vấn đề tìm thấy khi rà code | WCAG | Sửa | Test tự động |
|---|---|---|---|---|
| 1 | Bấm "Tiếp"/"Lưu & tiếp": nội dung bước đổi nhưng focus ở lại nút cũ, trình đọc màn hình không biết đã sang bước mới | 2.4.3, 4.1.3 | `<h2 tabIndex={-1} class="sr-only">Bước n/5: …</h2>`, focus vào đó khi đổi bước (không cướp focus lúc mở trang) | `WizardA11y.test.tsx` test 1 |
| 2 | Trạng thái "đang lưu" chỉ thể hiện bằng nút mờ / chữ nghiêng | 4.1.3 | `<p role="status" class="sr-only">Đang lưu…</p>` | test 4 |
| 3 | Bảng nguyên liệu / danh sách bước có nhiều nút cùng tên "Sửa", "Xoá" | 2.4.6, 2.4.9 (tham khảo) | Thêm phần chữ ẩn: "Sửa nguyên liệu Cá lóc", "Xoá bước 1: Sơ chế", "Xoá ảnh 2: …" | test 2, 3 |
| 4 | Cột sắp xếp ở `/dashboard/recipes` chỉ báo bằng mũi tên ↓↑ | 1.3.1 | `aria-sort="ascending/descending"` trên `<th>` | (kiểm tay mục 2.6) |
| — | Đã có từ trước: ô lỗi `aria-invalid` + `aria-describedby` trỏ tới thông báo lỗi; banner lỗi `role="alert"`; thanh bước `aria-current="step"`; nút ▲▼ có `aria-label` | 3.3.1, 4.1.2 | — | test 5 |

Chạy lại: `cd D:\CulinaryBlog\src\frontend; npx jest` (35/35 xanh lúc viết file này).

Cập nhật 05/10: cách làm ở dòng 3 (span `sr-only` ghép sau chữ nhìn thấy) làm NVDA đọc dính "Sửanguyên liệu cá viên" (lỗi 2, mục 3a);
đã thay bằng `aria-label` ở `08e64ce`.

## 1. Chuẩn bị

- [x] Postgres + API `http://localhost:5080` + frontend `http://localhost:3000`; đăng nhập tài khoản Author.
  Đợt NVDA 05/10: "bản build, đã đăng nhập" (theo log; log không ghi cổng và tài khoản).
- [x] NVDA (chế độ duyệt: `NVDA+Space` bật/tắt chế độ focus). Trình duyệt: Chrome (theo log 05/10). Phiên bản NVDA: không ghi trong log.
- [ ] Chrome DevTools > Device toolbar để đặt chiều rộng 320 / 768 / 1200 px. Zoom trình duyệt 200% cho mục 1.4.4. — chưa làm.
- [ ] (Tuỳ chọn) Lighthouse > Accessibility hoặc axe DevTools, chép điểm vào mục 4. — chưa làm cho trang wizard/dashboard (xem mục 4).

## 2. Checklist WCAG 2.1 AA (điền Đạt / Không đạt / Không áp dụng + ghi chú)

Ca h2 chỉ đo cuộn ngang nên kết quả Playwright chỉ ghi ở dòng 1.4.10. Các dòng khác cần kiểm tay ở từng độ rộng và chưa làm;
cột Ghi chú nêu bằng chứng tự động hoặc NVDA đã có (không gắn với độ rộng nào).

| Mã | Tiêu chí | Cách kiểm | 320 px | 768 px | 1200 px | Ghi chú |
|---|---|---|---|---|---|---|
| 1.1.1 | Ảnh có alt | Bước 4: ảnh đã tải lên đọc alt hoặc tên công thức; ảnh trang trí ở dashboard `alt=""` | chưa làm | chưa làm | chưa làm | NVDA: alt ảnh chưa kiểm. jest-axe 0 vi phạm, đột biến bỏ alt -> `image-alt` đỏ (`98f2c5f`). |
| 1.3.1 | Cấu trúc / nhãn | Mỗi ô nhập có nhãn; bảng nguyên liệu có `th`; danh sách bước là `ol`; `aria-sort` ở cột sắp xếp | chưa làm | chưa làm | chưa làm | NVDA 05/10: ô nhập đọc nhãn ("Tiêu đề *  edit", "Tên nguyên liệu (dòng 1)  edit"); bảng "table  with 2 rows and 5 columns". Sắp xếp cột (`aria-sort`): chưa kiểm. |
| 1.3.2 | Thứ tự đọc hợp lý | Đọc tuần tự (NVDA phím mũi tên) khớp thứ tự nhìn | chưa làm | chưa làm | chưa làm | |
| 1.4.3 | Tương phản ≥ 4.5:1 | DevTools > Inspect > Contrast: chữ xám `text-gray-400/500`, nút cam `bg-orange-500` chữ trắng, chữ đỏ lỗi | chưa làm | chưa làm | chưa làm | `fb2dc56`: chữ trắng trên nền cam (nút "Sửa công thức", số bước tròn) đổi sang `bg-orange-700`, 3.56:1 -> 5.18:1 (test tính từ mã màu Tailwind). Đo tay các màu còn lại: chưa làm. |
| 1.4.4 | Zoom 200% không mất nội dung | Ctrl + (+) tới 200% ở 1200 px | — | — | chưa làm | |
| 1.4.10 | Reflow 320 px không cuộn ngang (trừ bảng) | Thanh bước, form bước 1 (lưới 3 cột), dòng nguyên liệu (lưới 12 cột) | Đạt (kiểm tự động bằng Playwright, e2e/wizard-week4.spec.ts, ca h2: scrollWidth <= innerWidth ở cả 5 bước; chưa kiểm tay) | Đạt (kiểm tự động bằng Playwright, e2e/wizard-week4.spec.ts, ca h2: scrollWidth <= innerWidth ở cả 5 bước; chưa kiểm tay) | Đạt (kiểm tự động bằng Playwright, e2e/wizard-week4.spec.ts, ca h2: scrollWidth <= innerWidth ở cả 5 bước; chưa kiểm tay) | Ca h2: SP `src/frontend/e2e/wizard-week4.spec.ts:343`, vòng 320/768/1200 × bước 1–5, so `document.documentElement.scrollWidth` với `window.innerWidth`. Playwright 11/11 ngày 05/10 (SP `BANG_MINH_CHUNG_K01_K24.md`). |
| 1.4.11 | Tương phản thành phần UI ≥ 3:1 | Viền ô nhập, viền focus | chưa làm | chưa làm | chưa làm | |
| 1.4.13 | Nội dung hiện khi hover/focus | Không có tooltip tự viết -> ghi "Không áp dụng" nếu đúng | chưa làm | chưa làm | chưa làm | |
| 2.1.1 | Dùng được bằng bàn phím | Làm hết luồng tạo công thức chỉ bằng Tab / Shift+Tab / Enter / Space | chưa làm | chưa làm | chưa làm | |
| 2.1.2 | Không bẫy bàn phím | Hộp `confirm()` xoá: Enter / Esc thoát được | chưa làm | chưa làm | chưa làm | |
| 2.4.3 | Thứ tự focus | Sau "Tiếp": focus nằm ở tiêu đề bước (vô hình), Tab tiếp vào ô đầu tiên của bước | chưa làm | chưa làm | chưa làm | Jest `WizardA11y.test.tsx`: "luc mo wizard khong cuop focus; chuyen buoc thi focus toi tieu de buoc moi". NVDA 05/10: sau "Lưu & tiếp" đọc "Bước 2/5: Nguyên liệu  heading  level 2". |
| 2.4.6 | Tiêu đề / nhãn mô tả | Tên nút Sửa/Xoá có ngữ cảnh khi đọc bằng NVDA | chưa làm | chưa làm | chưa làm | NVDA 05/10: lỗi 2 và lỗi 3 (mục 3a), đã sửa `08e64ce`, `548e4a7`, Jest xanh; chưa nghe lại sau khi sửa (quyết định của Trung). |
| 2.4.7 | Focus nhìn thấy được | Mọi nút/ô có viền focus rõ (đặc biệt nút thanh bước, nút Sửa/Xoá chữ màu) | chưa làm | chưa làm | chưa làm | |
| 3.2.2 | Nhập liệu không tự đổi ngữ cảnh | Chọn danh mục / gõ không tự chuyển trang | chưa làm | chưa làm | chưa làm | |
| 3.3.1 | Báo lỗi | Bỏ trống tiêu đề -> lỗi hiện dưới ô, NVDA đọc khi focus ô | chưa làm | chưa làm | chưa làm | NVDA: lỗi khi để trống tiêu đề chưa kiểm. Jest: "o loi co aria-invalid va aria-describedby tro toi thong bao loi". |
| 3.3.2 | Nhãn / hướng dẫn | Dấu * bắt buộc, gợi ý định dạng ảnh (JPEG, PNG… 5 MiB) | chưa làm | chưa làm | chưa làm | NVDA 05/10: "Tiêu đề *", "Danh mục *" đọc kèm dấu *. |
| 3.3.3 | Gợi ý sửa lỗi | Thông báo nói rõ cần sửa gì ("Tiêu đề phải từ 5 đến 200 ký tự") | chưa làm | chưa làm | chưa làm | |
| 4.1.2 | Tên, vai trò, giá trị | Thanh bước đọc "hiện tại" ở bước đang mở; nút bị vô hiệu đọc "không khả dụng" | chưa làm | chưa làm | chưa làm | NVDA 05/10: "2. Nguyên liệu  button  current step"; "Độ khó  combo box  Trung bình  collapsed"; "Đang lưu…  unavailable". Lỗi 1 (tên ô Mô tả chứa bộ đếm): mục 3a. |
| 4.1.3 | Thông báo trạng thái | Lưu nguyên liệu: NVDA đọc "Đang lưu…"; lỗi server: đọc banner lỗi | chưa làm | chưa làm | chưa làm | NVDA 05/10: lưu bước 1 đọc "Đang lưu...". Lưu nguyên liệu và banner lỗi server: chưa làm. Jest: "dang luu duoc thong bao qua role=status, xong thi xoa thong bao". |

## 3. Kịch bản NVDA (ghi đúng câu NVDA đọc)

Câu ở cột "NVDA đọc thực tế" chép nguyên văn từ log 05/10, đợt nghe **trước** ba commit sửa. NVDA đọc vai trò bằng tiếng Anh
("button", "heading level 2") vì không có giọng tiếng Việt ("Vietnamese (not supported)"); đó không phải lỗi của ứng dụng.

| # | Thao tác | Mong đợi | NVDA đọc thực tế | Đạt? |
|---|---|---|---|---|
| N1 | Mở `/dashboard/recipes/new`, nhấn `H` | Nghe "Tạo công thức mới, tiêu đề cấp 1" | (không có trong log) | chưa làm |
| N2 | `D` (landmark) / `NVDA+F7` danh sách mốc | Có "Các bước soạn công thức, điều hướng" | "Các bước soạn công thức  navigation landmark  list  with 5 items" (log không ghi phím đã dùng) | Đạt (log 05/10) |
| N3 | Tab tới thanh bước | "1. Thông tin cơ bản, nút, hiện tại" ; các bước sau "không khả dụng" | Ở bước 2: "2. Nguyên liệu  button  current step". Bước 1 và các bước bị khoá: không có trong log | Một phần ("current step" có); phần còn lại chưa làm |
| N4 | Tab vào ô Tiêu đề, để trống, Tab tới "Lưu & tiếp", Enter | Focus về ô Tiêu đề, đọc "không hợp lệ" + "Tiêu đề phải từ 5 đến 200 ký tự" | — | chưa kiểm |
| N5 | Điền đủ bước 1, "Lưu & tiếp" | Đọc "Bước 2/5: Nguyên liệu, tiêu đề cấp 2" (focus chuyển tự động) | "Lưu & tiếp →  button" -> "Đang lưu…  unavailable" -> "Đang lưu..." -> "Bước 2/5: Nguyên liệu  heading  level 2" | Đạt (log 05/10) |
| N6 | Nhập 1 nguyên liệu, Tab tới "Lưu", Enter | Đọc "Đang lưu…" rồi bảng có dòng mới | (không có trong log; log chỉ có bảng đã có dòng "cá viên") | chưa làm |
| N7 | Bảng: `T` tới bảng, `Ctrl+Alt+mũi tên` qua ô | Đọc tiêu đề cột "Tên / Số lượng / Đơn vị / Ghi chú" | Chỉ có ô cột 5: "table  with 2 rows and 5 columns  row 2  Thao tác  column 5 …"; các cột khác không có trong log | chưa làm |
| N8 | Tab tới nút Sửa của dòng | "Sửa nguyên liệu <tên>, nút" | "Sửanguyên liệu cá viên  button", "Xoánguyên liệu cá viên  button" | Không đạt lúc nghe (lỗi 2) -> sửa `08e64ce`; chưa nghe lại sau khi sửa (quyết định của Trung) |
| N9 | Xoá nguyên liệu: hộp xác nhận | NVDA đọc nội dung hộp "Xoá nguyên liệu …?"; Esc huỷ, Enter đồng ý | (không có trong log) | chưa làm |
| N10 | Gõ dòng nháp rồi bấm "Tiếp" | Banner lỗi được đọc ngay ("Còn 1 dòng nguyên liệu chưa lưu…") | (không có trong log) | chưa làm |
| N11 | Bước 3: nút ▲▼ | "Đưa bước 1 xuống, nút"; sau khi bấm, thứ tự đọc đúng | (không có trong log; log bước 3 chỉ có "Chưa có bước nào." và các ô nhập bước) | chưa làm |
| N12 | Bước 4: ô chọn ảnh, chọn tệp .txt | Đọc "Chọn ảnh"; lỗi "Chỉ nhận ảnh JPEG, PNG, WebP hoặc AVIF" | "Chọn ảnh: No file chosen  button"; lỗi khi chọn .txt: không có trong log | Một phần (nhãn "Chọn ảnh" có); lỗi tệp chưa làm; alt ảnh chưa kiểm |
| N13 | Bước 5 "Xem lại" | Đọc tiêu đề công thức (cấp 2), danh sách nguyên liệu, các dấu ✓ | — | chưa kiểm |
| N14 | `/dashboard/recipes`: tới cột "Cập nhật" | "Cập nhật ↓, sắp xếp giảm dần" (hoặc tương đương) | — | chưa kiểm |
| N15 | Tab lọc trạng thái | "Tất cả, nút bật" (aria-pressed) | "Lọc theo trạng thái  navigation landmark  toggle button  pressed  Tất cả 5"; "toggle button  not pressed  Bản nháp 1" | Đạt (log 05/10) |
| N16 (thêm) | Bước 1: gõ vào ô Mô tả | Tên ô là "Mô tả", không đọc lại nhãn mỗi phím | "Mô tả (1/2000)", "Mô tả (2/2000)", "Mô tả (3/2000)"… lặp lại mỗi ký tự | Không đạt lúc nghe (lỗi 1) -> sửa `1ae6ce4`; chưa nghe lại sau khi sửa (quyết định của Trung) |
| N17 (thêm) | `/dashboard/recipes`: Tab tới liên kết Sửa trong bảng | "Sửa công thức <tiêu đề>, liên kết" | "table  with 7 rows and 5 columns  row 2  Thao tác  column 5  Sửa  visited  link" | Không đạt lúc nghe (lỗi 3) -> sửa `548e4a7`; chưa nghe lại sau khi sửa (quyết định của Trung) |

### 3a. Ba lỗi NVDA đã nghe (05/10/2026) và trạng thái

| # | NVDA đọc (nguyên văn log) | Nguyên nhân | Commit sửa (SP) | Test chứng minh (Jest đỏ trước / xanh sau, theo commit) | Nghe lại |
|---|---|---|---|---|---|
| 1 | "Mô tả (1/2000)", "Mô tả (2/2000)"… mỗi phím | Bộ đếm `(N/2000)` nằm trong thẻ label của ô Mô tả | `1ae6ce4` | `WizardA11y.test.tsx`, describe "bo dem o Mo ta nam ngoai nhan": "ten truy cap luon la 'Mo ta' khi go; bo dem noi bang aria-describedby, khong aria-live", "chi thong bao polite khi con duoi 100 ky tu va khi cham gioi han, khong doi theo tung phim" — đỏ 2/2 -> xanh | chưa nghe lại sau khi sửa (quyết định của Trung) |
| 2 | "Sửanguyên liệu cá viên  button", "Xoánguyên liệu cá viên  button" (dính chữ) | Span `sr-only` ghép sát chữ nhìn thấy; bước Các bước và Ảnh cùng kiểu viết (NVDA chưa nghe hai bước này), sửa cùng mẫu | `08e64ce` | `WizardA11y.test.tsx`, describe "nut lap lai dung aria-label, khong dung span sr-only": "buoc Nguyen lieu", "buoc Cac buoc", "buoc Anh: kem so thu tu, kem alt neu co" — đỏ 3/3 -> xanh | chưa nghe lại sau khi sửa (quyết định của Trung) |
| 3 | "Sửa  visited  link" ở bảng dashboard, không kèm tên công thức | Liên kết Xem/Sửa và nút Xoá chỉ có chữ ngắn | `548e4a7` | `DashboardA11y.test.tsx`: "lien ket Xem, Sua va nut Xoa trong hang kem ten cong thuc", "dang xoa: ten nut doi theo chu nhin thay 'Dang xoa…'" — đỏ 2/2 -> xanh | chưa nghe lại sau khi sửa (quyết định của Trung) |

## 4. Kết quả tổng hợp (điền sau khi làm)

- Ngày kiểm: 05/10/2026 (NVDA, một đợt, theo log). Người kiểm: Huỳnh Quốc Trung. Trình duyệt + NVDA: Chrome, bản build; phiên bản NVDA không ghi trong log.
  Kiểm tay 320/768/1200 và zoom 200%: chưa làm.
- Lighthouse Accessibility (1200 px) cho wizard/dashboard: chưa làm. Tham khảo, không phải trang wizard: trang công khai `/recipes/aaaaab` và `/recipes/bbbbb`
  đạt Accessibility 96 (SP `TV3_BAN_GIAO_TUAN4.md` mục F; ảnh `docs/evidence/TV3/Tuan04/K22_lighthouse_*.png`).
  axe DevTools: chưa làm. jest-axe (jsdom; 5 bước wizard, trạng thái lỗi, dashboard): 0 vi phạm (`98f2c5f`, `fb2dc56`).
- Lỗi còn lại (mô tả, mức độ, cách tái hiện):
  1. Ba lỗi NVDA ở mục 3a: đã sửa trong code, Jest xanh, chưa nghe lại sau khi sửa (quyết định của Trung).
  2. `Footer.tsx` (TV2): Lighthouse báo ba dòng chân trang thiếu tương phản (`text-gray-500` trên `bg-gray-900`); TV3 không sửa (SP `TV3_BAN_GIAO_TUAN4.md` mục D).
  3. NVDA chưa kiểm: lỗi khi để trống tiêu đề, alt ảnh, sắp xếp cột, bước 5. Chưa làm: các ô "chưa làm" ở mục 2 và mục 3.
- Ảnh chụp / ghi âm lưu tại: `docs/evidence/TV3/k18/` chưa tạo. Bằng chứng chữ: SP `docs/evidence/TV3/Tuan04/K18_nvda_speech_log.txt`.

## 5. Rủi ro đã biết (chưa sửa, cần kiểm tay để xác nhận)

- Màu `text-gray-400` (nhãn "(0/2000)", chữ nghiêng dòng đang lưu) có thể dưới 4.5:1 trên nền trắng — **đoán**, cần đo. Đo tay: chưa làm.
- Nút chữ màu (Sửa xanh, Xoá đỏ) không có `focus-visible` riêng; dựa vào viền focus mặc định của trình duyệt. Kiểm tay: chưa làm.
- Lưới 12 cột của dòng nguyên liệu ở 320 px có thể quá hẹp (ô "SL"/"Đơn vị") — **đoán**, cần xem thật.
  Playwright h2 chỉ xác nhận không cuộn ngang; ô có đủ rộng để đọc/nhập hay không: chưa làm.
- Sau khi xoá một dòng, nút vừa bấm biến mất nên focus rơi về `body`; NVDA có thể đọc lại từ đầu trang. Nghe tay: chưa làm.
