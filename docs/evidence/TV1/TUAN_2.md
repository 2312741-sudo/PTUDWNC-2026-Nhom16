# Báo cáo nghiệm thu Tuần 2 — TV1 (Nguyễn Thanh Tâm - 2312741)

- **Người thực hiện**: Nguyễn Thanh Tâm (MSSV: 2312741) — Nhóm trưởng (TV1)
- **Phần việc phụ trách**: Tài khoản, hồ sơ và nền tảng auth/logging/CI
- **Mã task tuần 2**: A2 (hoàn thiện), A3, A4, A7 (kiểm thử & nghiệm thu auth)
- **Nhánh Git**: `main` (commit `cb7d242` và các commits hoàn thiện kiểm thử, UI)
- **Ngày hoàn thành**: 16/09/2026
- **Trạng thái**: Hoàn thành toàn bộ mục tiêu Tuần 2 (100% tests pass, frontend dashboard/profile tích hợp)

---

## 1. Bản ghi minh chứng theo mẫu quy định

### Bản ghi 1: Task A2 (hoàn thiện) — Identity Lockout & Auth Rate Limiting
- **Tuần / Người / Task**: 2 / Nguyễn Thanh Tâm (TV1) / A2
- **FR/NFR/Kỹ năng**: FR-AUTH-002, FR-AUTH-006; NFR-SEC-004, NFR-SEC-005; K02, K04, K08, K10
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.API/Program.cs` (`options.Lockout`, `builder.Services.AddRateLimiter`, chính sách "auth" 10 req/phút/IP)
  - `src/backend/CulinaryBlog.Infrastructure/IdentityService.cs` (gọi `signIn.CheckPasswordSignInAsync(..., lockoutOnFailure: true)`, mã lỗi 423 `auth.locked`)
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Database: PostgreSQL 16 (`culinary_test`).
  - Lệnh test: `dotnet test tests/CulinaryBlog.Tests/CulinaryBlog.Tests.csproj --filter "FullyQualifiedName~Lockout"` -> **PASS**.
  - Kiểm thử lockout: Đăng nhập sai 4 lần trả lời `401 Unauthorized`. Lần thứ 5 sai đạt ngưỡng `MaxFailedAccessAttempts = 5` và kích hoạt khóa tài khoản 15 phút, trả về HTTP `423 Locked` cùng thông báo generic chống dò thông tin. Lần thử tiếp theo (dù đúng mật khẩu) vẫn bị chặn trả về `423 Locked` với mã Problem Details `code: "auth.locked"`.
  - Rate limiter: Áp dụng trên cả `/api/v1/auth/register` và `/api/v1/auth/login`, giới hạn 10 request/phút theo IP remote. Khi vượt ngưỡng trả về `429 Too Many Requests` và header chuẩn `Retry-After: 60`.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 16/09/2026.

---

### Bản ghi 2: Task A3 — Update Profile API & Dashboard UI Form
- **Tuần / Người / Task**: 2 / Nguyễn Thanh Tâm (TV1) / A3
- **FR/NFR/Kỹ năng**: FR-AUTH-007; NFR-SEC-006; K02, K03, K05, K16, K17
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Application/Auth.cs` (`UpdateProfileCommand`, `UpdateProfileHandler`, `UpdateProfileValidator`, `UserDto` mở rộng)
  - `src/backend/CulinaryBlog.Infrastructure/IdentityService.cs` (`UpdateAsync`, `ToDto` ánh xạ `AvatarUrl` và `Bio`)
  - `src/backend/CulinaryBlog.API/Program.cs` (`PATCH /api/v1/auth/me`)
  - `src/frontend/src/app/dashboard/profile/page.tsx` (Trang hồ sơ cá nhân với form React Hook Form + Zod, inline validation, preview avatar, email/roles read-only)
  - `src/frontend/src/lib/api.ts` (`getMe`, `updateProfile`, `logout`)
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Test tự động tích hợp:
    - `AuthTests.Update_profile_patches_allowed_fields_successfully`: Cập nhật hợp lệ `displayName`, `avatarUrl`, `bio`, lấy lại qua `GET /me` -> **PASS**.
    - `AuthTests.Update_profile_rejects_unauthorized_call`: Không có JWT Bearer trả về `401 Unauthorized` -> **PASS**.
    - `AuthTests.Update_profile_rejects_xss_and_invalid_inputs`: Thử tiêm `<script>` hoặc URL avatar `javascript:alert(1)` bị chặn bởi validator -> **PASS**.
    - `AuthTests.Update_profile_cannot_modify_email_or_roles`: Gửi thêm `email` hay `roles` bị từ chối và dữ liệu không bị thay đổi -> **PASS**.
  - Unit tests:
    - `ArchitectureTests.Update_profile_validator_accepts_valid_inputs` -> **PASS**.
    - `ArchitectureTests.Update_profile_validator_rejects_invalid_display_name` -> **PASS**.
    - `ArchitectureTests.Update_profile_validator_rejects_invalid_avatar_url` -> **PASS**.
    - `ArchitectureTests.Update_profile_validator_rejects_excessive_lengths` -> **PASS**.
  - Frontend Next.js build: Lệnh `npm run build` trong `src/frontend` thành công, route `/dashboard/profile` sinh ra sạch sẽ.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 16/09/2026.

---

### Bản ghi 3: Task A4 — Welcome Email Queue & Worker Retry
- **Tuần / Người / Task**: 2 / Nguyễn Thanh Tâm (TV1) / A4
- **FR/NFR/Kỹ năng**: FR-JOB-001; NFR-MAINT-001; K14, K15
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Infrastructure/WelcomeEmail.cs` (`WelcomeEmailQueue`, `IWelcomeEmailQueue`, `WelcomeEmailWorker`)
  - `src/backend/CulinaryBlog.Infrastructure/IdentityService.cs` (`await welcome.EnqueueAsync(...)` sau transaction commit)
  - `src/backend/CulinaryBlog.API/Program.cs` (Đăng ký BackgroundService `WelcomeEmailWorker` và Channel queue)
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Bất đồng bộ hóa qua Channel: Không làm chậm luồng response đăng ký tài khoản (User nhận ngay 201 Created mà không phải chờ gửi mail SMTP).
  - An toàn dữ liệu: Enqueue chỉ xảy ra sau khi transaction đăng ký cơ sở dữ liệu commit thành công. Lỗi gửi mail không làm rollback tài khoản đã đăng ký.
  - HTML Encode: Tên người dùng được encode qua `WebUtility.HtmlEncode` trước khi chèn vào template HTML để chống tấn công Email HTML Injection.
  - Cơ chế Retry: Worker thử lại tự động theo 4 mốc thời gian: `TimeSpan.Zero`, `1 phút`, `5 phút`, `30 phút`. Ghi log cảnh báo khi retry và chỉ log lỗi nếu thất bại toàn bộ. Không bao giờ ghi thông tin credential/secret vào log.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 16/09/2026.

---

### Bản ghi 4: Task A7 — Kiểm thử hồi quy, bảo mật Auth & Tích hợp hạ tầng
- **Tuần / Người / Task**: 2 / Nguyễn Thanh Tâm (TV1) / A7
- **FR/NFR/Kỹ năng**: K21, K23, K24
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - Tích hợp hạ tầng dev từ TV4: `docker-compose.dev.yml` (PostgreSQL 16, Redis 7, MinIO, MailHog, Seq, Nginx) và `nginx/nginx.dev.conf`.
  - Tích hợp contracts & endpoints: `src/backend/CulinaryBlog.API/Health.cs`, `src/backend/CulinaryBlog.Application/Storage.cs`, `src/backend/CulinaryBlog.Infrastructure/MinioOptions.cs`.
  - Dọn dẹp cấu trúc route trong `Program.cs`: gom các nhóm `/api/v1/auth`, `/api/v1/categories`, `/health`.
  - `tests/CulinaryBlog.Tests/AuthTests.cs`, `ArchitectureTests.cs`, `HealthTests.cs`.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Toàn bộ solution:
    ```bash
    dotnet build CulinaryBlog.sln
    # Kết quả: 0 Warning(s), 0 Error(s)
    ```
  - Chạy toàn bộ test suite backend:
    ```bash
    dotnet test tests/CulinaryBlog.Tests/CulinaryBlog.Tests.csproj
    # Kết quả: Passed! - Failed: 0, Passed: 50, Skipped: 0, Total: 50
    ```
  - Chạy kiểm thử tương tranh (Concurrency Spike):
    ```bash
    dotnet test tests/concurrency-spike/ConcurrencySpike.csproj
    # Kết quả: Passed! - Failed: 0, Passed: 4, Skipped: 0, Total: 4
    ```
  - Tổng số automated tests đạt **54/54 tests pass 100%**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 16/09/2026.

---

## 2. Kết quả kiểm thử tự động chi tiết

```text
Test run for tests/CulinaryBlog.Tests/bin/Debug/net10.0/CulinaryBlog.Tests.dll (.NETCoreApp,Version=v10.0)
Passed!  - Failed: 0, Passed: 50, Skipped: 0, Total: 50, Duration: 2 s
```

### Danh mục 50 tests của `CulinaryBlog.Tests`:
1. **ArchitectureTests (18 tests)**:
   - `Inner_layers_do_not_reference_infrastructure_or_web`
   - `Display_name_rejects_invalid_values` (4 cases)
   - `Display_name_trims_text`
   - `Validation_pipeline_stops_handler_on_invalid_password`
   - `Update_profile_validator_accepts_valid_inputs` (2 cases)
   - `Update_profile_validator_rejects_invalid_display_name` (4 cases)
   - `Update_profile_validator_rejects_invalid_avatar_url` (3 cases)
   - `Update_profile_validator_rejects_excessive_lengths`
   - `Logout_validator_checks_refresh_token_length`
2. **AuthTests (18 tests)**:
   - `Register_login_me_persist_hash_and_author_without_leaking_secrets`
   - `Duplicate_email_is_case_insensitive_and_returns_problem`
   - `Concurrent_registration_creates_exactly_one_account`
   - `Invalid_registration_has_field_errors_and_no_account`
   - `Client_cannot_assign_admin_role`
   - `Invalid_credentials_return_same_generic_error`
   - `Me_rejects_missing_or_invalid_token` (2 cases)
   - `Disabled_account_cannot_login`
   - `Scalar_and_openapi_are_available_in_development`
   - `Logout_with_valid_token_returns_no_content`
   - `Logout_without_token_returns_unauthorized`
   - `Lockout_after_five_failed_attempts_locks_account_and_returns_423`
   - `Update_profile_patches_allowed_fields_successfully`
   - `Update_profile_rejects_unauthorized_call`
   - `Update_profile_rejects_xss_and_invalid_inputs` (2 cases)
   - `Update_profile_cannot_modify_email_or_roles`
3. **CategoryTests (12 tests)**:
   - Toàn bộ 12 test danh mục (domain model, slug generation, CQRS create/update/delete/get).
4. **HealthTests (2 tests)**:
   - `Liveness_always_reports_healthy`
   - `Ready_and_full_report_have_expected_checks`

---

## 3. Cách thức Demo & Nghiệm thu

1. **Khởi động môi trường dev**:
   ```bash
   docker compose -f docker-compose.dev.yml up -d
   ```
2. **Chạy Backend API**:
   ```bash
   dotnet run --project src/backend/CulinaryBlog.API -- --migrate
   dotnet run --project src/backend/CulinaryBlog.API -- --urls http://localhost:5000
   ```
3. **Khởi động Frontend Next.js**:
   ```bash
   cd src/frontend && npm run dev
   ```
4. **Kịch bản nghiệm thu**:
   - Truy cập **http://localhost:3000/dashboard/profile**: xem giao diện hồ sơ, thử nhập tên chứa thẻ `<script>` để thấy inline error chặn XSS; lưu thay đổi và thấy toast thông báo thành công.
   - Gọi `POST /api/v1/auth/login` với sai mật khẩu 5 lần để nhận HTTP `423 Locked`.
   - Gửi 11 request auth trong vòng 1 phút để nhận HTTP `429 Too Many Requests` kèm header `Retry-After: 60`.
   - Đăng ký tài khoản mới và kiểm tra MailHog tại **http://localhost:8025** để thấy email chào mừng có tên được mã hóa HTML an toàn.
   - Kiểm tra `/health`, `/health/live`, `/health/ready` qua trình duyệt hoặc curl.
