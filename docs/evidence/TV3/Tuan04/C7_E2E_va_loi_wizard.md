# C7 tuần 4 — Playwright E2E "Create Recipe" + lỗi wizard nhảy về bước 1 (TV3)

Ngày chạy: 02/10/2026. Máy: Windows 11, Postgres 16 local (DB `culinary_blog`), Playwright 1.63.0, Chromium headless.
Đầu ra dưới đây chép nguyên văn từ terminal. Không có mật khẩu: test sinh mật khẩu ngẫu nhiên trong bộ nhớ, không in ra.

## 1. Môi trường

```powershell
cd D:\CulinaryBlog
dotnet run --project src/backend/CulinaryBlog.API --launch-profile http -- --migrate   # chạy migration rồi thoát
dotnet run --no-build --project src/backend/CulinaryBlog.API --launch-profile http -- --urls http://localhost:5080
cd src\frontend; npm run dev                                                            # http://localhost:3000
npx playwright install chromium                                                         # lần đầu
npm run e2e                                                                             # hoặc npx playwright test
```

Docker (redis/minio/mailhog) đã dừng trong lúc chạy E2E để đỡ RAM: API chỉ dùng Redis cho health check, MinIO cho ảnh (luồng E2E bỏ qua bước ảnh).
Bật lại: `docker start culinaryblog-redis culinaryblog-minio culinaryblog-mailhog`.

## 2. Luồng test (`src/frontend/e2e/create-recipe.spec.ts`)

1. `POST /api/v1/auth/register` tạo user mới (email `e2e.<thời gian><hex>@culinary.local`, mật khẩu `randomBytes`) -> 201.
2. Đăng nhập trên UI `/auth/login` -> `/dashboard/profile`.
3. Bước 1: điền tiêu đề, thời gian, khẩu phần, danh mục -> "Lưu & tiếp" (POST /recipes, Draft).
4. Bước 2: thêm "Cá lóc 500 g", "Me chua 50 g"; Sửa cá lóc 500 -> 600 (PUT); Xoá "Me chua": dialog `confirm()` lần 1 **dismiss** (dòng còn), lần 2 **accept** (dòng mất, nội dung dialog đúng `Xoá nguyên liệu "Me chua"?`).
5. Bước 3: thêm bước "Sơ chế cá" ("Lưu bước").
6. Bước 4 -> 5 "Xem lại": tiêu đề, `600 g Cá lóc`, `Sơ chế cá`, 2 dấu ✓, nút "Xuất bản" bật.
7. Đối chiếu server: `GET /recipes/{slug}` còn đúng `[["Cá lóc", 600]]` và 1 bước.

## 3. Kết quả chạy thật

Lặp 3 lần (`npx playwright test --repeat-each=3`, reporter mặc định list + html), trước khi đổi tiêu đề test:

```
Running 3 tests using 1 worker

  ok 1 [chromium] › e2e\create-recipe.spec.ts:30:7 › Soạn công thức qua wizard › đăng ký, đăng nhập, tạo công thức, thêm/sửa/xoá nguyên liệu và thêm bước (3.8s)
  ok 2 [chromium] › e2e\create-recipe.spec.ts:30:7 › Soạn công thức qua wizard › đăng ký, đăng nhập, tạo công thức, thêm/sửa/xoá nguyên liệu và thêm bước (2.3s)
  ok 3 [chromium] › e2e\create-recipe.spec.ts:30:7 › Soạn công thức qua wizard › đăng ký, đăng nhập, tạo công thức, thêm/sửa/xoá nguyên liệu và thêm bước (2.1s)

  3 passed (13.7s)
```

Lần cuối (tiêu đề test cuối cùng, `npx playwright test --reporter=list`):

```
Running 1 test using 1 worker

  ok 1 [chromium] › e2e\create-recipe.spec.ts:30:7 › Soạn công thức qua wizard › đăng ký, đăng nhập, tạo công thức, thêm/sửa/xoá nguyên liệu, thêm bước, xem lại (2.7s)

  1 passed (4.6s)
```

Kiểm tra lộ mật khẩu (giải nén dữ liệu nhúng trong `playwright-report/index.html`, quét chuỗi tiền tố mật khẩu):

```
html report co 'Aa1!': False
html report co buoc Evaluate: True
test-results co 'Aa1!': False
```

Trước khi sửa, báo cáo HTML có bước `Fill "Aa1!…"` (lộ mật khẩu) vì `locator.fill()` ghi giá trị vào tiêu đề bước.
Cách sửa: nhập mật khẩu bằng `locator.evaluate` (setter gốc + sự kiện `input`) và `trace: "off"` cho spec này.

## 4. Lỗi thật tìm ra nhờ E2E: wizard nhảy về bước 1 sau khi tạo mới (TV3, C4) — ĐÃ SỬA

Triệu chứng: ở `/dashboard/recipes/new`, bấm "Lưu & tiếp" -> sang bước 2, khoảng 1 giây sau tự quay về
"Sửa công thức / 1. Thông tin cơ bản" (mất bước đang làm). Lần chạy E2E thứ hai bị đỏ ở bước Xem lại vì lý do này (ảnh chụp
lúc lỗi: trang "Sửa công thức", bước 1 đang chọn).

Nguyên nhân (đã xác nhận bằng spec chẩn đoán tạm, đã xoá): `saveBasic` đổi URL sang `/{id}/edit` bằng `history.replaceState`
(Next 15 đồng bộ URL này vào router). Sau đó `refreshPublic` gọi `router.refresh()` -> Next tải RSC của route **edit**
-> `EditRecipeClient` mount -> khởi tạo wizard `step: 0`.

Log chẩn đoán TRƯỚC khi sửa:

```
 1183ms REQ POST /api/revalidate rsc=
 1183ms NAV http://localhost:3000/dashboard/recipes/ba38d0e0-…/edit?slug=diag-wizard-…
 1203ms h1="Tạo công thức mới" step="2. Nguyên liệu" url=/dashboard/recipes/ba38d0e0-…/edit
 1206ms REQ GET /dashboard/recipes/ba38d0e0-…/edit?slug=diag-wizard-…&_rsc=fpx6j rsc=1
 1469ms h1="Sửa công thức" step="2. Nguyên liệu" url=/dashboard/recipes/ba38d0e0-…/edit
 2324ms h1="Sửa công thức" step="1. Thông tin cơ bản" url=/dashboard/recipes/ba38d0e0-…/edit
```

SAU khi sửa (giữ bước 2 suốt 10 giây theo dõi, không còn request RSC):

```
 1460ms REQ POST /api/revalidate rsc=
 1460ms NAV http://localhost:3000/dashboard/recipes/17f2554a-…/edit?slug=diag-wizard-…
 1718ms h1="Sửa công thức" step="2. Nguyên liệu" url=/dashboard/recipes/17f2554a-…/edit
  1 passed (15.9s)
```

Sửa: `RecipeWizard` nhớ pathname lúc mount; chỉ `router.refresh()` khi pathname hiện tại còn trùng
(trang edit mở thẳng vẫn refresh như cũ). `/api/revalidate` (xoá cache ISR phía server) vẫn được gọi mọi lần.
Hạn chế còn lại: ở luồng /new, Router Cache phía trình duyệt không bị xoá trong phiên đó (trang công khai vẫn đúng
sau tối đa 5 phút hoặc khi tải lại trang).

Test Jest đỏ trước / xanh sau (`__tests__/WizardRefresh.test.tsx`):

```
TRƯỚC:
  × tao moi o /new: sau khi URL doi sang /edit thi KHONG goi router.refresh (tranh mount lai ve buoc 1) (621 ms)
  √ dang o trang /edit (URL khong doi pathname): van goi router.refresh de lam moi cache (231 ms)
  ● tao moi o /new: sau khi URL doi sang /edit thi KHONG goi router.refresh (tranh mount lai ve buoc 1)
    Expected number of calls: 0
    Received number of calls: 1
Tests:       1 failed, 1 passed, 2 total

SAU:
PASS src/app/dashboard/recipes/_wizard/__tests__/WizardRefresh.test.tsx
Tests:       30 passed, 30 total     (toàn bộ npx jest)
```

## 5. Ghi chú khác

- Next.js App Router luôn gắn một phần tử `role="alert"` rỗng (`next-route-announcer`) cuối trang -> test chỉ đếm alert có chữ.
- Mỗi lần chạy E2E tạo 1 user + 1 công thức Draft trong DB dev `culinary_blog` (không dọn tự động).
