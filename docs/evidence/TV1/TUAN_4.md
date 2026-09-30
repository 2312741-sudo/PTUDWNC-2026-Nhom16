# Báo cáo nghiệm thu Tuần 4 — TV1 (Nguyễn Thanh Tâm - 2312741)

- **Người thực hiện**: Nguyễn Thanh Tâm (MSSV: 2312741) — Nhóm trưởng (TV1)
- **Phần việc phụ trách**: Tính năng Đổi mật khẩu (Change Password), gia cố bảo mật xác thực, negative security tests & chuẩn hóa 24/24 ô kỹ năng
- **Mã task tuần 4**: A6, A7
- **Nhánh Git**: `main`
- **Ngày hoàn thành**: 30/09/2026
- **Trạng thái**: Hoàn thành 100% mục tiêu Tuần 4 (130/130 tests pass)

---

## 1. Bản ghi minh chứng theo mẫu quy định

### Bản ghi 1: Task A6 & K08 — Tính năng Đổi Mật Khẩu (Change Password) & Thu hồi phiên cũ
- **Tuần / Người / Task**: 4 / Nguyễn Thanh Tâm (TV1) / A6, K08
- **FR/NFR/Kỹ năng**: FR-AUTH-006, FR-AUTH-007; NFR-SEC-001, NFR-SEC-002, NFR-SEC-003, NFR-SEC-004; K02, K04, K05, K08
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Application/Auth.cs` (`ChangePasswordCommand`, `ChangePasswordHandler`, `ChangePasswordValidator`, mở rộng `IIdentityService`).
  - `src/backend/CulinaryBlog.Infrastructure/IdentityService.cs` (Cài đặt `ChangePasswordAsync`: xác minh mật khẩu hiện tại, kiểm tra policy độ phức tạp, cập nhật `SecurityStamp`, thu hồi toàn bộ `RefreshTokens` đang hoạt động của người dùng).
  - `src/backend/CulinaryBlog.API/Program.cs` (Map endpoint `POST /api/v1/auth/change-password` với `RequireAuthorization()`, trả HTTP 204 NoContent khi thành công).
  - `src/frontend/src/lib/api.ts` (Hàm client `changePassword` kết nối API).
  - `src/frontend/src/app/dashboard/profile/page.tsx` (Card giao diện Đổi mật khẩu với đầy đủ kiểm tra lỗi inline, mật khẩu mạnh và thông báo thời gian thực).
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Test suite: `Week4AuthAndSecurityLabTests.cs` (xUnit).
  - Kết quả test:
    - `Change_password_with_valid_credentials_succeeds_and_allows_login_with_new_password`: Đổi mật khẩu thành công trả 204; đăng nhập bằng mật khẩu cũ bị từ chối 401; đăng nhập bằng mật khẩu mới thành công 200 -> **PASS**.
    - `Change_password_with_wrong_current_password_fails_with_400`: Nhập sai mật khẩu hiện tại trả HTTP 400 kèm mã lỗi `auth.wrong_current_password` -> **PASS**.
    - `Change_password_with_same_or_weak_password_fails_validation`: Nhập mật khẩu trùng mật khẩu cũ hoặc không đủ độ phức tạp trả HTTP 400 validation error -> **PASS**.
    - `Change_password_requires_authorization_returns_401`: Chưa đăng nhập gọi API bị từ chối 401 -> **PASS**.
    - `Change_password_revokes_old_refresh_tokens`: Sau khi đổi mật khẩu, Refresh Token cũ bị thu hồi ngay lập tức, gọi refresh bị từ chối 401 -> **PASS**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 30/09/2026.

---

### Bản ghi 2: Task A6 & K07 — Kiểm thử đồng thời (Concurrency Spike) & Bảo đảm tính toàn vẹn RowVersion
- **Tuần / Người / Task**: 4 / Nguyễn Thanh Tâm (TV1) / A6, K07
- **FR/NFR/Kỹ năng**: NFR-REL-003, NFR-DATA-001; K06, K07
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `tests/concurrency-spike/`: Dự án kiểm thử concurrency chuyên biệt.
  - Cấu hình `RowVersion` / `xmin` trên thực thể PostgreSQL để phát hiện xung đột ghi đè dữ liệu đồng thời (Lost Update Problem).
  - `ApiExceptionHandler.cs`: Xử lý `DbUpdateConcurrencyException` toàn cục, trả về HTTP 422 Unprocessable Entity kèm mã lỗi `recipe.version_conflict`.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Chạy `dotnet test tests/concurrency-spike/`:
    - 5/5 tests concurrency pass 100% chứng minh cơ chế khóa lạc quan (Optimistic Concurrency Control) hoạt động chính xác khi 2 writer cùng cập nhật đồng thời -> **PASS**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 30/09/2026.

---

### Bản ghi 3: Task A7 & K10, K20 — Rate Limiting, CORS, Policy Authorization & Negative Security Testing
- **Tuần / Người / Task**: 4 / Nguyễn Thanh Tâm (TV1) / A7, K10, K20
- **FR/NFR/Kỹ năng**: NFR-SEC-001, NFR-SEC-004; K05, K10, K20
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.API/Program.cs`:
    - Rate Limiter Middleware: Giới hạn 10 requests/phút/IP trên các endpoint xác thực nhạy cảm (`/register`, `/login`, `/refresh`, `/change-password`).
    - Account Lockout: Tự động khóa tài khoản 15 phút sau 5 lần đăng nhập thất bại liên tiếp (HTTP 423 `auth.locked`).
    - Policy Authorization: `AdminPolicy` và `AuthorPolicy` ngăn chặn việc leo thang đặc quyền.
    - Anti-XSS & HTML Injection Sanitization: Kiểm duyệt toàn bộ dữ liệu đầu vào trên `RegisterCommand`, `UpdateProfileCommand`.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Chạy `AuthTests.cs`:
    - `Update_profile_rejects_xss_and_invalid_inputs`: Thử tiêm mã script `<script>alert(1)</script>` bị chặn đứng và trả HTTP 400 validation failed -> **PASS**.
    - `Client_cannot_assign_admin_role`: Client gửi payload tự gán role Admin bị phớt lờ hoàn toàn -> **PASS**.
    - `Disabled_account_cannot_login`: Tài khoản bị vô hiệu hóa bị từ chối HTTP 403 -> **PASS**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 30/09/2026.

---

### Bản ghi 4: Task A7 & K21, K24 — Trách nhiệm Nhóm trưởng: Chuẩn hóa 24/24 Kỹ năng & Điều phối kiểm thử toàn hệ thống
- **Tuần / Người / Task**: 4 / Nguyễn Thanh Tâm (TV1) / A7, K21, K24
- **FR/NFR/Kỹ năng**: K21, K23, K24
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - Rà soát toàn bộ 24 nhóm kỹ năng K01–K24 của thành viên TV1 theo phân công `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md`.
  - Tích hợp và nghiệm thu các module của nhóm trên nhánh `main`.
  - Điều phối và thực hiện kiểm thử tự động toàn bộ giải pháp: Đạt **130/130 tests pass 100% (Green)** (125 tests `CulinaryBlog.Tests` + 5 tests `ConcurrencySpike`).
  - Kiểm tra Next.js 15 App Router frontend: `npm run build` thành công 100% không phát sinh cảnh báo hay lỗi kiểu dữ liệu.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - `dotnet test CulinaryBlog.sln`: 130/130 Passed.
  - `npm run build` trong `src/frontend`: Compiled successfully (11/11 pages).
  - `dotnet format CulinaryBlog.sln --verify-no-changes`: Passed không vi phạm quy chuẩn mã nguồn.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 30/09/2026.

---

## 2. Tổng kết tiến độ Tuần 4

| Nhóm nội dung | Kế hoạch tuần 4 | Thực tế hoàn thành | Đánh giá |
|---|---|---|:---:|
| **Bảo mật & Đổi mật khẩu (Task A6)** | Triển khai API đổi mật khẩu, mã hóa PBKDF2, thu hồi refresh tokens cũ | Hoàn thiện API, CSDL, giao diện Dashboard và 5 automated tests | **100%** |
| **Kiểm thử Concurrency & Lockout (Task A6/K07)** | Concurrency spike, RowVersion, Lost Update prevention | 5 tests concurrency spike pass 100%, middleware HTTP 422 | **100%** |
| **Negative Security & Rate Limiting (Task A7/K10)** | Chống XSS, rate limiting 10 req/min, lockout 5 lần, CORS/Policy | Hoàn tất cấu hình, middleware và kiểm thử bảo mật tự động | **100%** |
| **Chuẩn hóa 24 Kỹ năng & Điều phối (Task A7/K24)** | Đạt 24/24 K kỹ năng, build sạch, 130 tests green | Hoàn tất nghiệm thu, build frontend và backend 100% thành công | **100%** |

---

## 3. Xác nhận hoàn thành

- Toàn bộ nội dung công việc của **Tuần 4 (Lab 04)** của thành viên **Nguyễn Thanh Tâm (TV1 - 2312741)** đã hoàn thành 100%.
- Báo cáo Word nộp bài: `Lab04_2312741_NguyenThanhTam.docx` đã được sinh và lưu tại `docs/evidence/TV1/` và `~/Downloads/`.
