# Báo cáo nghiệm thu Tuần 1 — TV1 (Nguyễn Thanh Tâm - 2312741)

- **Người thực hiện**: Nguyễn Thanh Tâm (MSSV: 2312741) — Nhóm trưởng (TV1)
- **Phần việc phụ trách**: Tài khoản, hồ sơ và nền tảng auth/logging/CI
- **Mã task tuần 1**: A1, A2 (nền), A5 (nền), A6 (nền)
- **Nhánh Git**: `feat/TV1-week1-auth`
- **Ngày hoàn thành**: 09/09/2026
- **Trạng thái**: Hoàn thành toàn bộ mục tiêu Tuần 1 (Đã test pass 100%, sẵn sàng bàn giao cho TV2–TV4)

---

## 1. Bản ghi minh chứng theo mẫu quy định

### Bản ghi 1: Task A1 — Identity, Roles, Migration, PBKDF2 & JWT
- **Tuần / Người / Task**: 1 / Nguyễn Thanh Tâm (TV1) / A1
- **FR/NFR/Kỹ năng**: FR-AUTH-001/002/006; NFR-SEC-001/002/003/004; K01, K02, K03, K06, K08
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Domain/DisplayName.cs`, `Roles.cs`
  - `src/backend/CulinaryBlog.Infrastructure/IdentityModel.cs` (`ApplicationUser`, `AuthDbContext`)
  - `src/backend/CulinaryBlog.Infrastructure/Migrations/20260909134304_InitialIdentity.cs`
  - `src/backend/CulinaryBlog.Infrastructure/JwtService.cs`, `IdentityService.cs`
  - `docs/AUTH_CONTRACT.md`, `docs/adr/0001-auth-week1.md`
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Database: PostgreSQL 16 (Port 55432 / Docker 5432).
  - Lệnh migration: `dotnet run --project src/backend/CulinaryBlog.API -- --migrate` (Thành công, tạo 8 bảng Identity và seed 2 roles `Author`, `Admin`).
  - Unit/Integration test: `AuthTests.Register_login_me_persist_hash_and_author_without_leaking_secrets` -> PASS.
  - Kiểm tra thuật toán mật khẩu: PBKDF2 Identity V3 SHA512 với 100.000 iterations -> PASS.
  - JWT HS256: claims `sub`, `userId`, `email`, `role`, `jti`, `iat`, `exp` (900s / 15 phút), `iss`, `aud`, clockSkew = 0 -> PASS.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 09/09/2026.

---

### Bản ghi 2: Task A2 (nền) — Register, Login cơ bản & ICurrentUser
- **Tuần / Người / Task**: 1 / Nguyễn Thanh Tâm (TV1) / A2 (nền)
- **FR/NFR/Kỹ năng**: FR-AUTH-001, FR-AUTH-002, FR-AUTH-006; NFR-SEC-005; K02, K04, K05, K10
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Application/Auth.cs` (`RegisterCommand`, `LoginCommand`, `GetMeQuery`, `IIdentityService`, `ICurrentUser`)
  - `src/backend/CulinaryBlog.API/Program.cs` (Endpoints: `POST /api/v1/auth/register`, `POST /api/v1/auth/login`, `GET /api/v1/auth/me`)
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Đăng ký thành công: trả về `201 Created`, header `Location: /api/v1/auth/me`, `Cache-Control: no-store`, auto-login cấp `accessToken` và role `Author`.
  - Đăng ký trùng email: trả về `409 Conflict`, Problem Details `code: "auth.email_exists"`. Xử lý đồng thời (concurrent registration) bằng Unique Index `NormalizedEmail` -> đúng 1 request 201, 1 request 409.
  - Đăng nhập sai: trả về `401 Unauthorized`, thông báo chung generic ("Email hoặc mật khẩu không đúng").
  - Tài khoản inactive (`IsActive = false`): trả về `403 Forbidden`.
  - Client cố tình gửi field lạ (`role: "Admin"`): `JsonUnmappedMemberHandling.Disallow` trả về `400 Bad Request`.
  - `GET /api/v1/auth/me`: kiểm tra Bearer token hợp lệ trả `200 OK` chứa `UserDto`; token thiếu/hỏng trả `401 Unauthorized`.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 09/09/2026.

---

### Bản ghi 3: Task A5 (nền) — Problem Details, Logging, CorrelationId & CI
- **Tuần / Người / Task**: 1 / Nguyễn Thanh Tâm (TV1) / A5 (nền)
- **FR/NFR/Kỹ năng**: FR-OBS-002; NFR-MAINT-001; K02, K04, K20, K24
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.API/ApiExceptionHandler.cs` (RFC 7807 Problem Details với `code`, `traceId`, `errors`)
  - `src/backend/CulinaryBlog.API/Program.cs` (Middleware Serilog, Server-generated `X-Correlation-ID`, `TraceId`)
  - `src/backend/CulinaryBlog.Application/Pipeline.cs` (`LoggingBehavior`, `ValidationBehavior`)
  - `.github/workflows/backend.yml` (CI pipeline với GitHub Actions + PostgreSQL 16 container service)
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Log correlation: mỗi request sinh correlation ID duy nhất và trả về qua header `X-Correlation-ID`.
  - Redaction: mật khẩu, security stamp, token tuyệt đối không lọt vào response body hay log file. EF Core sensitive logging bị tắt (`Fatal` override).
  - Scalar & OpenAPI: kiểm thử truy cập `GET /scalar/v1` (200 OK) và `GET /openapi/v1.json` (có auth schema `Bearer`) -> PASS.
  - CI Workflow: chạy đầy đủ restore (locked-mode), build Release, `dotnet format --verify-no-changes`, `dotnet test` kèm code coverage và upload TRX.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 09/09/2026.

---

### Bản ghi 4: Task A6 (nền) — Lab Kiến trúc Clean Architecture & Domain Value Object
- **Tuần / Người / Task**: 1 / Nguyễn Thanh Tâm (TV1) / A6 (nền)
- **FR/NFR/Kỹ năng**: K01, K03, K04 (nền), K21 (nền)
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `docs/evidence/TV1/LAB_KIEN_TRUC_API.md`
  - `src/backend/CulinaryBlog.Domain/DisplayName.cs` (Value Object kiểm tra 1-100 ký tự, cấm HTML/XSS control characters, auto-trim)
  - `tests/CulinaryBlog.Tests/ArchitectureTests.cs`
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - `ArchitectureTests.Inner_layers_do_not_reference_infrastructure_or_web`: Kiểm tra tự động bằng reflection chứng minh Domain và Application độc lập hoàn toàn với Web/EF/Infrastructure -> PASS.
  - `ArchitectureTests.DisplayName_rejects_invalid_values` & `DisplayName_trims_text` -> PASS.
  - `ArchitectureTests.Validation_pipeline_stops_handler_on_invalid_password`: Pipeline MediatR chặn request trước khi vào handler nếu validate thất bại -> PASS.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 09/09/2026.

---

## 2. Kết quả kiểm thử tự động (Automated Test Suite)

### Kết quả chạy lệnh `dotnet test`
```
Test run for .../CulinaryBlog.Tests.dll (.NETCoreApp,Version=v10.0)
Passed! - Failed: 0, Passed: 17, Skipped: 0, Total: 17, Duration: 1 s
```

Danh sách 17 tests:
1. `AuthTests.Register_login_me_persist_hash_and_author_without_leaking_secrets`: PASS
2. `AuthTests.Duplicate_email_is_case_insensitive_and_returns_problem`: PASS
3. `AuthTests.Concurrent_registration_creates_exactly_one_account`: PASS
4. `AuthTests.Invalid_registration_has_field_errors_and_no_account`: PASS
5. `AuthTests.Client_cannot_assign_admin_role`: PASS
6. `AuthTests.Invalid_credentials_return_same_generic_error`: PASS
7. `AuthTests.Me_rejects_missing_or_invalid_token(token: null)`: PASS
8. `AuthTests.Me_rejects_missing_or_invalid_token(token: "invalid.token.value")`: PASS
9. `AuthTests.Disabled_account_cannot_login`: PASS
10. `AuthTests.Scalar_and_openapi_are_available_in_development`: PASS
11. `ArchitectureTests.Inner_layers_do_not_reference_infrastructure_or_web`: PASS
12. `ArchitectureTests.Display_name_rejects_invalid_values(value: "")`: PASS
13. `ArchitectureTests.Display_name_rejects_invalid_values(value: " ")`: PASS
14. `ArchitectureTests.Display_name_rejects_invalid_values(value: "<b>Tâm</b>")`: PASS
15. `ArchitectureTests.Display_name_rejects_invalid_values(value: "name\n")`: PASS
16. `ArchitectureTests.Display_name_trims_text`: PASS
17. `ArchitectureTests.Validation_pipeline_stops_handler_on_invalid_password`: PASS

### Đo lường Code Coverage (XPlat Code Coverage)
- **Package CulinaryBlog.Application**: **100.00%** (Vượt mốc chỉ tiêu yêu cầu >= 80% trong kế hoạch)
- **Package CulinaryBlog.Domain**: **100.00%**
- **Package CulinaryBlog.API**: 49.32%
- **Package CulinaryBlog.Infrastructure**: 41.45%
- **Overall Line Coverage**: 46.74%

### Kiểm tra định dạng mã nguồn (Code Formatting)
```sh
dotnet format CulinaryBlog.sln --verify-no-changes --no-restore
# Kết quả: 0 warnings, 0 errors, không có vi phạm format.
```

---

## 3. Bàn giao và ranh giới tuần 1 vs tuần 2

1. **Bàn giao cho TV2, TV3, TV4**:
   - `docs/AUTH_CONTRACT.md` đã chốt rõ schema, endpoints, DTOs, cơ chế xác thực JWT Bearer, error handling RFC 7807.
   - Các thành viên khác có thể chạy local API để lấy token thật hoặc dùng seed role `Author` / `Admin`.
2. **Các tính năng thuộc Tuần 2 (đã lên kế hoạch rõ trong ADR 0001 và PHAN_CHIA_CONG_VIEC_6_TUAN)**:
   - Account Lockout (5 lần thử sai -> khóa 15 phút).
   - Rate limiting middleware.
   - Welcome email job (Hangfire + MailKit/SMTP qua Mailhog).
   - Update Profile (chỉ sửa DisplayName, AvatarUrl, Bio; cấm đổi Email qua profile).
   - Frontend Auth (Next.js CSR forms với React Hook Form + Zod).
   - Refresh Token rotation (TV3) và Logout (TV4).
