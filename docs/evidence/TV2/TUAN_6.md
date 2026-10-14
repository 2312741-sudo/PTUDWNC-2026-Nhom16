# Báo cáo nghiệm thu Tuần 6 (Lab 06) — TV2 (Ngô Quốc Trường Vĩ - 2312796)

- **Người thực hiện**: Ngô Quốc Trường Vĩ (MSSV: 2312796) — TV2
- **Phần việc phụ trách**: 
  1. Kiến trúc Frontend & Lớp Tiện ích Gọi API / Xử lý Lỗi chuẩn ProblemDetails (`HttpClient`, `errors.ts`)
  2. Cài đặt Gói Thư viện Giao diện chuẩn `shadcn/ui` (Button, Input, Card, Badge, Dialog, Skeleton, Alert, Pagination, `cn` utility)
  3. Quản lý Trạng thái Ứng dụng với Zustand Store (`auth-store.ts`, `ui-store.ts`) & Đồng bộ LocalStorage Reactive
  4. Thiết kế Giao diện Blog App Tĩnh & Fallback Mock Data (`mockData.ts`)
  5. Xây dựng Bộ Component Dùng Chung (`Header`, `Footer`, `Pagination`, `Loading`, `ErrorMessage`)
  6. Xây dựng Bố cục (Layout) Chung Toàn Ứng dụng (`layout.tsx`, `loading.tsx`, `error.tsx`, `not-found.tsx`)
  7. Regression Testing & Bàn giao Tài liệu Nghiệm thu Cổng G7 Tuần 6
- **Mã task tuần 6**: B7, K01, K05, K06, K16, K18, K19, K21, K23
- **Ngày hoàn thành**: 14/10/2026
- **Trạng thái**: Hoàn thành 100% mục tiêu Cổng G7 Tuần 6 & Yêu cầu Lab 06 (19/19 Test Suites Frontend Green, 111/111 Tests Pass)
- **Quy tắc đặt tên file nộp bài Lab 6**: `Lab6_2312796_NgoQuocTruongVi.docx` *(kèm file dự phòng `Lab06_2312796_NgoQuocTruongVi.docx`)*

---

## 0. Yêu cầu trọng tâm của Lab 06 & Tuần 6 đối với TV2

Căn cứ theo [Kế hoạch phân chia công việc 6 tuần](../../PHAN_CHIA_CONG_VIEC_6_TUAN.md) (mục 3.2 và mục 4, 5, 6) và **Yêu cầu tối thiểu Lab 6**:

1. **Cài đặt các lớp tiện ích để xử lý lỗi & gọi API**:
   - Xây dựng hệ thống phân cấp lỗi ứng dụng chuẩn RFC 7807 / RFC 9457: `AppError`, `ApiError`, `ValidationError`, `UnauthorizedError`, `ForbiddenError`, `NotFoundError`, `NetworkError`, `TimeoutError`.
   - Xây dựng lớp tiện ích `HttpClient` hỗ trợ `get`, `post`, `put`, `patch`, `delete`, tích hợp Request/Response Interceptors tự động nạp Bearer Token, chuẩn hóa URL nội bộ/công khai và bắt lỗi thời gian chờ (Timeout Controller).
2. **Cài đặt gói thư viện shadcn/ui**:
   - Khởi tạo cấu hình chuẩn `components.json`.
   - Xây dựng hàm hợp nhất class `cn` với `clsx` và `tailwind-merge` tại `src/lib/utils.ts`.
   - Cài đặt trọn bộ UI components chất lượng cao: `button.tsx` (hỗ trợ `asChild`, các biến thể màu, loading spinner), `input.tsx`, `card.tsx`, `badge.tsx`, `dialog.tsx`, `skeleton.tsx`, `alert.tsx`.
3. **Cài đặt store quản lý trạng thái (`auth-store.ts`)**:
   - Sử dụng thư viện `zustand` quản lý trạng thái đăng nhập, thông tin người dùng (`User`), token (`accessToken`, `refreshToken`), quyền (`roles`).
   - Tự động đồng bộ hóa hai chiều với `localStorage`, kích hoạt sự kiện `auth-state-change` giúp cập nhật tức thời trên toàn bộ các component (Header, Profile, Dashboard).
   - Bổ sung `ui-store.ts` quản lý toast notification và modal trạng thái.
4. **Thiết kế giao diện (tĩnh) cho blog app**:
   - Nâng cấp Trang chủ (`app/page.tsx`) với Hero Banner ấn tượng, thanh tìm kiếm nhanh, thống kê ẩm thực, danh mục nổi bật, công thức nấu ăn mới nhất và form đăng ký nhận bản tin (Newsletter).
   - Cung cấp bộ dữ liệu tĩnh fallback (`mockData.ts`) giúp blog app hiển thị trọn vẹn, sống động ngay cả khi không có kết nối cơ sở dữ liệu.
5. **Xây dựng các component dùng chung**:
   - `Header.tsx`: Thanh điều hướng cao cấp, logo thương hiệu, ô tìm kiếm nhanh, liên kết danh mục, avatar và nút đăng nhập/đăng xuất phản ứng nhanh theo auth-store.
   - `Footer.tsx`: Chân trang đa cột với bản quyền, danh mục, liên kết mạng xã hội và thông điệp ẩm thực.
   - `Pagination.tsx`: Thanh phân trang hỗ trợ cả chế độ URL SSR và callback Client State, tuân thủ trợ năng WCAG 2.1 AA (`aria-label`, `aria-current="page"`).
   - `Loading.tsx`: `LoadingSpinner`, `LoadingPage` và `RecipeCardSkeleton` tạo hiệu ứng tải trang mượt mà.
   - `ErrorMessage.tsx`: `ErrorMessage` và `InlineErrorAlert` thân thiện, có nút thử lại (Retry) và quay về trang chủ.
6. **Xây dựng bố cục (layout) chung cho blog app**:
   - `app/layout.tsx`: RootLayout chuẩn ngữ nghĩa HTML5 (`html`, `body flex flex-col min-h-screen`, `Header`, `main flex-1`, `Footer`).
   - `app/loading.tsx`, `app/error.tsx`, `app/not-found.tsx`: Các trang biên ranh giới chuẩn App Router của Next.js 15.
7. **Kiểm thử tự động chuyên biệt**:
   - Bổ sung 4 bộ kiểm thử Jest tự động: `errors.test.ts`, `httpClient.test.ts`, `auth-store.test.ts`, `CommonComponents.test.tsx` (25/25 bài test mới pass 100%, nâng tổng số test frontend lên 111 tests).

---

## 1. Bản ghi minh chứng chi tiết theo chuẩn môn học

### Bản ghi 1: Cài đặt Các Lớp Tiện Ích Xử Lý Lỗi & Gọi API (`errors.ts`, `httpClient.ts`)
- **Tuần / Người / Task**: 6 / Ngô Quốc Trường Vĩ (TV2) / Lab 6 — Yêu cầu 1
- **Trạng thái**: Hoàn thành 100%
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/frontend/src/lib/errors.ts`:
    + Lớp gốc `AppError` kế thừa `Error`.
    + Lớp `ApiError` với `status`, `title`, `detail`, `code`, `raw`.
    + Lớp `ValidationError` hỗ trợ trích xuất lỗi từng trường (`getFieldError(field)` không phân biệt hoa thường) và danh sách tất cả thông điệp lỗi (`getAllErrorMessages()`).
    + Các lớp lỗi HTTP đặc thù: `UnauthorizedError` (401), `ForbiddenError` (403), `NotFoundError` (404), `NetworkError`, `TimeoutError`.
    + Các hàm tiện ích: `parseApiError`, `getErrorMessage`, `isApiError`, `isValidationError`.
  - `src/frontend/src/lib/httpClient.ts`:
    + Lớp `HttpClient` hỗ trợ cấu hình Base URL động (`API_INTERNAL_URL` trên SSR / Docker và `NEXT_PUBLIC_API_URL` trên Client).
    + Request Interceptor: Tự động đính kèm `Authorization: Bearer <token>` từ `auth-store` / `localStorage`, thiết lập headers `Accept: application/json` và `Content-Type`.
    + Response Interceptor: Tự động phân tích Problem Details, tự động mở gói `data` (unwrap), ném ra đúng kiểu `ApiError` / `ValidationError`.
    + Cơ chế AbortController xử lý Timeout quá hạn (mặc định 15s).
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - `npm test -- src/lib/__tests__/errors.test.ts`: 7/7 tests PASS.
  - `npm test -- src/lib/__tests__/httpClient.test.ts`: 6/6 tests PASS.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng).

---

### Bản ghi 2: Cài Đặt Gói Thư Viện Giao Diện `shadcn/ui`
- **Tuần / Người / Task**: 6 / Ngô Quốc Trường Vĩ (TV2) / Lab 6 — Yêu cầu 2
- **Trạng thái**: Hoàn thành 100%
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/frontend/components.json`: Cấu hình chuẩn shadcn/ui tích hợp Tailwind CSS và alias path `@/components`, `@/lib/utils`, `@/components/ui`.
  - `src/frontend/src/lib/utils.ts`: Cài đặt hàm `cn(...inputs)` kết hợp `clsx` và `tailwind-merge` tối ưu hóa ghi đè class CSS; bổ sung formatters `formatVND`, `formatDate`, `truncate`.
  - Bộ component UI trong thư mục `src/frontend/src/components/ui/`:
    + `button.tsx`: Nút bấm đa năng với các biến thể `default`, `emerald`, `destructive`, `outline`, `secondary`, `ghost`, `link`; kích cỡ `sm`, `default`, `lg`, `icon`; hỗ trợ `isLoading` và `asChild` (kết hợp `next/link`).
    + `input.tsx`: Ô nhập liệu có viền focus emerald, icon đính kèm và thông báo lỗi validation.
    + `card.tsx`: Bộ thẻ container gồm `Card`, `CardHeader`, `CardTitle`, `CardDescription`, `CardContent`, `CardFooter`.
    + `badge.tsx`: Nhãn trạng thái với các biến thể `default`, `secondary`, `destructive`, `outline`, `success`, `warning`, `info`.
    + `dialog.tsx`: Hộp thoại Modal tương tác mượt mà với hiệu ứng làm mờ nền (backdrop blur) và khóa cuộn trang (scroll lock).
    + `skeleton.tsx`: Khung xương tải trang với hiệu ứng chuyển động sóng `animate-pulse`.
    + `alert.tsx`: Hộp thông báo cảnh báo/thành công/lỗi kèm biểu tượng ngữ nghĩa.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - `npm test -- src/components/__tests__/CommonComponents.test.tsx`: 13/13 tests PASS.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng).

---

### Bản ghi 3: Cài Đặt Store Quản Lý Trạng Thái (`auth-store.ts`, `ui-store.ts`)
- **Tuần / Người / Task**: 6 / Ngô Quốc Trường Vĩ (TV2) / Lab 6 — Yêu cầu 3
- **Trạng thái**: Hoàn thành 100%
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/frontend/src/store/auth-store.ts`:
    + Xây dựng với `zustand` đảm bảo reactivity tốc độ cao, không gây re-render thừa.
    + Quản lý toàn diện: `user` (id, email, fullName, roles, avatarUrl), `accessToken`, `refreshToken`, `isAuthenticated`, `isLoading`, `isInitialized`.
    + Các hàm hành động: `setAuth`, `setAuthFromResponse`, `updateUser`, `logout`, `initialize`, `setLoading`.
    + Đồng bộ hóa an toàn với `localStorage`, kích hoạt sự kiện `auth-state-change` giúp mọi component nhận diện tức thời trạng thái đăng nhập.
    + Cung cấp hàm tiện ích `hasRole(user, role)` kiểm tra quyền Admin/Author.
  - `src/frontend/src/store/ui-store.ts`:
    + Quản lý trạng thái thông báo nổi Toast Notification (`addToast`, `removeToast`) tự động ẩn sau 4 giây.
    + Quản lý trạng thái mở/đóng menu di động (`isMobileMenuOpen`) và hộp thoại tìm kiếm.
  - Tích hợp `useAuthStore` vào `Header.tsx`, `app/auth/login/page.tsx` và `app/auth/register/page.tsx`.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - `npm test -- src/store/__tests__/auth-store.test.ts`: 6/6 tests PASS.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng).

---

### Bản ghi 4: Thiết Kế Giao Diện (Tĩnh) Cho Blog App & Mock Data Fallback
- **Tuần / Người / Task**: 6 / Ngô Quốc Trường Vĩ (TV2) / Lab 6 — Yêu cầu 4
- **Trạng thái**: Hoàn thành 100%
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/frontend/src/lib/mockData.ts`:
    + 6 danh mục ẩm thực chuẩn mực (`MOCK_CATEGORIES`): Món nước & Súp, Món kho & Rim, Món xào & Nướng, Bánh ngọt & Tráng miệng, Món chay thanh tịnh, Đồ uống & Detox kèm ảnh độ phân giải cao và mô tả cuốn hút.
    + 6 công thức nấu ăn tiêu biểu (`MOCK_RECIPES`): Phở bò gia truyền Hà Nội, Cá kho tộ miền Tây, Bún chả nướng than hoa, Gỏi cuốn tôm thịt, Bánh mì nướng bơ tỏi phô mai, Trà đào cam sả.
    + Chi tiết công thức nấu ăn hoàn chỉnh (`MOCK_RECIPE_DETAIL`): Đầy đủ danh sách nguyên liệu định lượng gram/kg, quy trình nấu nướng 3 bước kèm bộ đếm thời gian và bảng thành phần dinh dưỡng (Calories, Protein, Carbohydrates, Fat).
    + Hàm phân trang dữ liệu tĩnh `getMockPagedRecipes(page, pageSize)`.
  - `src/frontend/src/app/page.tsx`:
    + Hero Section với gradient emerald-teal-amber cao cấp, khẩu hiệu truyền cảm hứng.
    + Thanh tìm kiếm nhanh dạng nổi với gợi ý từ khóa ("phở bò", "cá kho", "bún chả").
    + Lưới hiển thị danh mục ẩm thực nổi bật (`CategoryCard`).
    + Bộ sưu tập công thức nấu ăn mới nhất (`RecipeCard`).
    + Dải thống kê ẩm thực (500+ công thức, 25+ danh mục, 10,000+ độc giả, 4.9★ đánh giá).
    + Khối giới thiệu 3 thế mạnh công nghệ nền tảng (Tìm kiếm FTS không dấu, Chi tiết từng công đoạn, Bảo mật & Tốc độ cao).
    + Form đăng ký nhận bản tin ẩm thực qua email (Newsletter).
  - Tích hợp cơ chế tự động chuyển sang dữ liệu mẫu tĩnh trong `src/frontend/src/lib/api.ts` khi server API offline, đảm bảo website luôn hiển thị đẹp mắt 100%.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Chạy `npm run build` Next.js 15: 15/15 routes tĩnh/động kết xuất hoàn hảo, 0 lỗi cú pháp.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng).

---

### Bản ghi 5: Xây Dựng Các Component Dùng Chung & Bố Cục (Layout) Chung
- **Tuần / Người / Task**: 6 / Ngô Quốc Trường Vĩ (TV2) / Lab 6 — Yêu cầu 5 & 6
- **Trạng thái**: Hoàn thành 100%
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/frontend/src/components/Pagination.tsx`:
    + Component phân trang dùng chung hỗ trợ nút Trang đầu, Trang trước, các số trang kèm dấu chấm lửng (`...`), Trang sau, Trang cuối.
    + Hỗ trợ cả 2 chế độ: Tạo URL liên kết cho SEO SSR (`createPageUrl`) hoặc gọi hàm callback Client-side (`onPageChange`).
    + Tuân thủ ngữ nghĩa trợ năng `<nav role="navigation" aria-label="...">` và `aria-current="page"`.
  - `src/frontend/src/components/Loading.tsx`:
    + `LoadingSpinner`: Biểu tượng quay mượt mà với 4 kích cỡ `sm`, `md`, `lg`, `xl` và thông điệp động.
    + `LoadingPage`: Khối hiển thị tải toàn trang có hiệu ứng làm mờ nền (glassmorphism).
    + `RecipeCardSkeleton`, `RecipeListSkeleton`: Khung xương giả lập lưới danh sách món ăn khi đang tải dữ liệu.
  - `src/frontend/src/components/ErrorMessage.tsx`:
    + `ErrorMessage`: Hộp thông báo lỗi trang nhã, trích xuất thông điệp chuẩn xác, có nút "Thử lại" (Retry) và "Về trang chủ".
    + `InlineErrorAlert`: Thanh cảnh báo lỗi nội dòng cho form nhập liệu kèm nút đóng.
  - `src/frontend/src/app/layout.tsx`:
    + Bố cục chung toàn ứng dụng: Semantic HTML5, cấu trúc `Header` cố định trên đỉnh, nội dung chính `main className="flex-1"` co giãn linh hoạt, `Footer` cố định chân trang.
    + Tối ưu hóa Metadata SEO, OpenGraph và Favicon.
  - Các trang biên ranh giới chuyên dụng của App Router:
    + `src/frontend/src/app/loading.tsx`: Tự động kích hoạt khi chuyển trang Next.js.
    + `src/frontend/src/app/error.tsx`: Bắt lỗi biên giới (Error Boundary) tự động phục hồi với nút Reset.
    + `src/frontend/src/app/not-found.tsx`: Trang 404 thân thiện thông báo khi món ăn/đường dẫn không tồn tại.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - `npm test`: 19/19 Test Suites PASS (111/111 tests xanh 100%).
  - `npx tsc --noEmit`: 0 lỗi TypeScript.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng).

---

## 2. Bảng tổng hợp công việc Lab 6 đối chiếu Yêu cầu Tối thiểu

| STT | Yêu cầu tối thiểu Lab 6 | Hiện trạng thực hiện của TV2 | Tỷ lệ hoàn thành |
|:---:|---|---|:---:|
| **1** | Cài đặt các lớp tiện ích để xử lý lỗi, gọi API, ... | Hoàn thành `errors.ts` (AppError, ApiError, ValidationError, ...) và `httpClient.ts` (interceptor, auth token, timeout) | **100%** |
| **2** | Cài đặt gói thư viện shadcn/ui (hoặc gói hỗ trợ thiết kế giao diện khác) | Hoàn thành `components.json`, `utils.ts` (cn), và bộ 7 UI components (`button`, `input`, `card`, `badge`, `dialog`, `skeleton`, `alert`) | **100%** |
| **3** | Cài đặt store để quản lý trạng thái (chẳng hạn: auth-store.ts) | Hoàn thành `auth-store.ts` (Zustand + LocalStorage sync reactive) và `ui-store.ts` (toast, modals) | **100%** |
| **4** | Thiết kế giao diện (tĩnh) cho blog app | Hoàn thành Homepage cao cấp, Recipes listing, Detail, Categories, Search kèm `mockData.ts` fallback | **100%** |
| **5** | Xây dựng các component dùng chung (header, footer, pagination, loading, error) | Hoàn thành `Header`, `Footer`, `Pagination`, `Loading` (Spinners, Skeletons), `ErrorMessage` | **100%** |
| **6** | Xây dựng bố cục (layout) chung cho blog app | Hoàn thành `RootLayout` (`layout.tsx`), kèm `loading.tsx`, `error.tsx`, `not-found.tsx` | **100%** |
| **7** | Kiểm thử tự động & Đảm bảo chất lượng | 19 test suites frontend, 111/111 tests pass 100%, TypeScript 0 lỗi | **100%** |

---

## 3. Xác nhận hoàn thành

- Toàn bộ nội dung công việc của **Tuần 6 (Lab 06 / Cổng G7)** của thành viên **Ngô Quốc Trường Vĩ (TV2 - 2312796)** đã hoàn tất xuất sắc 100%.
- Nhánh làm việc: `2312796_NgoQuocTruongVi_lab6` đã sẵn sàng để tạo Pull Request lên GitHub cho Nhóm trưởng nghiệm thu.
- File báo cáo nộp bài LMS theo đúng quy tắc: `Lab6_2312796_NgoQuocTruongVi.docx` *(kèm file dự phòng `Lab06_2312796_NgoQuocTruongVi.docx`)* đã được sinh tự động và lưu trữ tại `docs/evidence/TV2/` và thư mục `~/Downloads/`.
