# TV3 - Bàn giao và tóm tắt tuần 4 (05/10/2026)

Người viết: Huỳnh Quốc Trung (TV3). Văn bản này mô tả vấn đề và đề xuất, không nhằm đánh giá cá nhân.
Chỗ nào là suy đoán hoặc chưa kiểm lại thì ghi rõ. Mọi con số lấy từ lần chạy thật của TV3 (xem mục G).

## A. Tóm tắt tuần 4

- Lab (nhánh `practice/TV3/labs`, thư mục `labs/TV3`, ngoài `CulinaryBlog.sln`): K04, K10, K23, K16, K19, K20, K22.
  82 test (Redis tắt) và 8 `L3SearchTests` (Redis bật ở cổng 6399). K23 chạy thật ngày 05/10: build image, 2 API sau Nginx,
  sao lưu/khôi phục file (`docs/evidence/TV3/LAB_K23_tuan4_chay_that.md` trên nhánh lab).
- Sản phẩm (nhánh `2312786_HuynhQuocTrung_C7-frontend-tests`): backend 281 test + 5 (ConcurrencySpike), Jest 85, Playwright 11.
  Coverage tầng Application 95,45% (đo trước các thay đổi a11y cuối, chưa đo lại).
- Lỗi đã sửa có test đỏ trước, xanh sau: xóa ảnh rồi tải lại bị 422; xóa ảnh chính khi còn ảnh khác; thêm ảnh vào công thức có sẵn;
  đổi tiêu đề bản nháp làm slug đổi nên tab kia 404 (nay nạp theo mã công thức); wizard tự nhảy về bước 1;
  ảnh bản nháp vỡ trong trình soạn (nay tải bằng `fetch` kèm token rồi hiển thị qua blob URL).
- K19: JSON-LD có ảnh tuyệt đối; validator.schema.org 0 lỗi, 0 cảnh báo; meta description dự phòng khi mô tả rỗng.
- K18: sửa độ tương phản nút "Sửa công thức"; NVDA tìm ra 3 lỗi, đã sửa (bộ đếm trong nhãn, tên nút dính chữ, liên kết dashboard thiếu tên công thức).

## B. File chung TV3 đã chạm

| File | Thay đổi | Tương thích ngược |
|---|---|---|
| `API/Program.cs` | Đăng ký `SlowQueryInterceptor`, `IRecipeDisplayNameReader`; nối thêm interceptor vào dòng `AddInterceptors`; `AddMeter(RecipeMetrics.MeterName)` qua `ConfigureOpenTelemetryMeterProvider` (không sửa khối `WithMetrics` của TV4) | Có |
| `Application/Recipes.cs` | `RecipeDetailDto` thêm `AuthorName`, `CategoryName` (mặc định null); `GET /recipes/{key}` nhận mã công thức hoặc slug; kiểm quyền bản nháp giữ nguyên | Có |
| `Application/RecipeImages.cs` (TV4) | `DeleteRecipeImageHandler`: thêm `IUnitOfWork? uow = null`; xóa mềm ảnh trước rồi đôn ảnh còn lại lên chính trong cùng transaction | Có (test cũ của TV4 không đổi) |
| `Domain/Entities/Recipe.cs` | `RemoveImage(imageId, promoteNext = true)` và `EnsurePrimaryImage()`; ảnh bị gỡ bỏ cờ chính | Có |
| `Infrastructure/RecipeRepository.cs`, `IRecipeRepository` | `FindByIdAsync` (thành viên mặc định, trả null); `AsSplitQuery` cho truy vấn chi tiết và nạp để ghi | Có |
| `RecipeImageConfiguration.cs` + migrations | Chỉ mục `ux_recipe_images_one_primary` lọc thêm `IsDeleted = false` (migration `RecipeImageOnePrimaryIgnoresSoftDeleted`); `ValueGeneratedNever` cho `RecipeImage.Id` (migration rỗng) | Có. Chỉ mục thuộc IMAGE_CONTRACT §4 |
| Frontend: `package.json`, `package-lock.json` | Thêm Jest, Testing Library, jest-axe, Playwright, `@tanstack/react-query`; script `test`, `e2e` | Có |
| Frontend: `recipes/[slug]/page.tsx`, `recipe-jsonld.ts`, `OwnerEditButton.tsx`, wizard, dashboard recipes | Meta description dự phòng, JSON-LD, màu nút, `aria-label` các nút | Có |

## C. Việc liên quan TV1 (Tâm)

1. `DbSeeder.cs` dòng ~22 (commit `4515021`, 24/09): điều kiện `!img.OriginalUrl.StartsWith("/images/recipes/")` khớp cả khóa ảnh người dùng
   (`recipes/{id}/{uuid}.ext`), nên mỗi lần API khởi động URL ảnh bị đổi thành `/images/recipes/{slug}.jpg`. Ảnh vỡ ở trang chi tiết.
   Đề xuất: chỉ ghi đè ảnh mẫu (`photo-...`). Cách né tạm: chạy API với `--no-auto-migrate`.
2. `dotnet run ... -- --migrate` in "Database migrations applied successfully" nhưng thất bại trên DB dev
   (`relation "AspNetRoles" already exists`, lịch sử migration lệch). Đoạn này trong `Program.cs`. TV3 chưa sửa.
3. Đã ghi nhận từ các lần kiểm trước (chưa kiểm lại): nút Google gửi token cứng `demo_token`; frontend không có
   `/api/auth/callback/google` (kết luận 404 suy ra từ cấu trúc thư mục, chưa chạy trên trình duyệt);
   user seed không có mật khẩu (không còn chặn E2E vì test tự đăng ký user); nhãn form đăng nhập không gắn với ô nhập (a11y).
4. Câu hỏi: NVDA của K18 tính cho cả bốn người hay chỉ ô TV1? Thứ tự PR: `C4-wizard-rhf-zod` trước hay gộp một PR?
   Lab TV3 giữ ở nhánh riêng hay đưa vào `main`?

## D. Việc liên quan TV2 (Trường Vĩ)

1. `Footer.tsx`: chữ `text-gray-500` trên nền `bg-gray-900` thiếu độ tương phản (Lighthouse Accessibility 96, ba dòng chân trang).
   Gợi ý: `text-gray-400`. Không gấp.
2. Header hiển thị liên kết "Quản trị DM" và "Hồ sơ (A3)" cho cả khách chưa đăng nhập (thấy trong HTML của trang công khai).
   Theo phiên kiểm trước, backend vẫn chặn bằng quyền Admin, nên mức độ thấp.

## E. Việc liên quan TV4 (Sơn)

1. `DeleteRecipeImageHandler` đã được TV3 sửa (mục B): lưu hai pha trong một transaction. Nếu có ý kiến khác, trao đổi ở PR.
2. Lỗi riêng, có từ trước bản sửa: xóa ảnh đúng lúc job resize vừa ghi xong có thể trả 422 (job đổi `RowVersion` của dòng ảnh).
   Ca Playwright mới chờ resize xong rồi mới xóa. Chưa có test riêng cho trường hợp này.
3. `IMAGE_CONTRACT.md` §4: chỉ mục ảnh chính nay lọc `IsDeleted = false`.

## F. Hạn chế đã biết của TV3

- Sửa nguyên liệu hoặc bước không đổi `RowVersion` của công thức; hai tab cùng sửa một nguyên liệu thì bản lưu sau thắng (ADR-0002).
- `AsSplitQuery`: các câu SQL không cùng snapshot (ADR-0002). Độ trễ gần như không đổi, lợi ích là không nhân dòng.
- Rich Results Test: với `localhost` báo 2 lỗi "URL trong trường image không hợp lệ"; đổi sang tên miền công khai thì hợp lệ.
  Các cảnh báo còn lại là trường khuyến nghị. Thiếu `aggregateRating` là cố ý (NFR-SEO-001, không bịa đánh giá).
  Ảnh chụp kết quả Rich Results Test không nằm trong repo (TV3 chạy tay).
- Lighthouse (bản build, máy RAM thấp, chỉ tham khảo): trang `aaaaab` 81/96/100/92 (trước), trang `bbbbb` 86/96/100/100 (sau).
  Hai lần đo khác trang nên không so sánh trực tiếp. SEO 100 ở trang `bbbbb` có mô tả thật, nên không chứng minh mô tả dự phòng.
  Bằng chứng cho mô tả dự phòng: test `metadata.test.ts` và HTML của `/recipes/aaaaab` sau khi sửa
  (`<meta name="description" content="aaaaabc — tổng 20 phút, 1 khẩu phần. Nguyên liệu: a."/>`, cùng `og:description`, `twitter:description`).
- K18: NVDA đã nghe một đợt (`docs/evidence/TV3/Tuan04/K18_nvda_speech_log.txt`), ba lỗi đã sửa nhưng chưa nghe lại.
  Chưa kiểm: lỗi khi để trống tiêu đề, chữ mô tả (alt) của ảnh, sắp xếp cột, bước 5.
  320/768/1200: kiểm tự động bằng Playwright (không cuộn ngang), chưa kiểm tay.
- DB dev `culinary_blog`: lệnh `--migrate` thất bại (mục C.2) nên chưa áp migration chỉ mục ảnh; công thức cũ có ảnh bị xóa mềm trước đó có thể vẫn kẹt.
  Công thức mới không bị ảnh hưởng.
- K24: chờ review của Tâm trên PR.

## G. Lệnh chạy lại

- Backend: `dotnet test CulinaryBlog.sln` (đặt `TEST_DATABASE` trỏ DB test riêng, tắt API trước).
- Frontend: trong `src/frontend`, `npm test`, `npx tsc --noEmit`, `npm run build`.
- E2E: bật API `dotnet run --project src/backend/CulinaryBlog.API -- --urls http://localhost:5080 --no-auto-migrate`,
  `npm run build` rồi `npm start`, sau đó `npx playwright test`.
- Lab: `dotnet test labs/TV3/Lab.TV3.Tests --filter "FullyQualifiedName!~L3SearchTests"`.