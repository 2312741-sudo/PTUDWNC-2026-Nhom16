# Báo cáo nghiệm thu Tuần 3 — TV1 (Nguyễn Thanh Tâm - 2312741)

- **Người thực hiện**: Nguyễn Thanh Tâm (MSSV: 2312741) — Nhóm trưởng (TV1)
- **Phần việc phụ trách**: Tài khoản, hồ sơ, bảo mật token, quan sát hệ thống & điều phối kỹ thuật
- **Mã task tuần 3**: A5, A6, A7
- **Nhánh Git**: `main`
- **Ngày hoàn thành**: 23/09/2026
- **Trạng thái**: Hoàn thành 100% mục tiêu Tuần 3 (90/90 tests pass, đạt mốc Cổng G3)

---

## 1. Bản ghi minh chứng theo mẫu quy định

### Bản ghi 1: Task A5 & C5 — Nền tảng Refresh Token Rotation, Family Reuse Revocation & Logout
- **Tuần / Người / Task**: 3 / Nguyễn Thanh Tâm (TV1) / A5, C5
- **FR/NFR/Kỹ năng**: FR-AUTH-004, FR-AUTH-005; NFR-SEC-001, NFR-SEC-002, NFR-SEC-003, NFR-SEC-004; K02, K04, K08
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Application/Auth.cs` (`RefreshTokenCommand`, `RefreshTokenHandler`, `RefreshTokenValidator`, mở rộng `IIdentityService`, hoàn thiện `LogoutHandler`).
  - `src/backend/CulinaryBlog.Infrastructure/IdentityModel.cs` (Map `DbSet<RefreshToken> RefreshTokens` vào `AuthDbContext` với cấu hình index duy nhất `IDX_RefreshToken_Hash`).
  - `src/backend/CulinaryBlog.Infrastructure/IdentityService.cs` (`RefreshTokenAsync`, `LogoutAsync`, `GenerateRefreshToken`, `HashToken`).
  - `src/backend/CulinaryBlog.Infrastructure/JwtService.cs` (Hỗ trợ cấp phát `RefreshToken` 512-bit cùng `AccessToken` 15 phút).
  - `src/backend/CulinaryBlog.API/Program.cs` (Endpoint `POST /api/v1/auth/refresh`).
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Cơ sở dữ liệu: PostgreSQL 16 (`culinary_test` & `culinary_blog`).
  - Test suite: `Week3AuthAndPersonalLabTests.cs` (xUnit).
  - Kết quả test:
    - `Register_and_login_return_valid_refresh_token`: Đăng ký và đăng nhập trả về Refresh Token 128 ký tự hex (512-bit ngẫu nhiên mật mã) -> **PASS**.
    - `Refresh_token_rotates_and_issues_new_token_pair`: Gửi Refresh Token hợp lệ lên `/api/v1/auth/refresh` thu hồi token cũ, cấp cặp Access Token và Refresh Token mới (Token Rotation). Dùng Access Token mới gọi `/api/v1/auth/me` thành công -> **PASS**.
    - `Refresh_token_reuse_triggers_family_revocation`: Kẻ tấn công cố tình sử dụng lại Refresh Token đã bị xoay vòng (Replay Attack) lập tức bị phát hiện; hệ thống thu hồi toàn bộ các token trong cùng phiên (Family Revocation) và vô hiệu hóa token hợp lệ mới cấp -> **PASS**.
    - `Logout_with_refresh_token_revokes_token`: Gọi `/api/v1/auth/logout` thu hồi Refresh Token trong CSDL; các yêu cầu refresh tiếp theo đều bị từ chối `401 Unauthorized` -> **PASS**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 23/09/2026.

---

### Bản ghi 2: Task A5 & K20 — Log Correlation, Redaction & Tracing OpenTelemetry / Health Checks
- **Tuần / Người / Task**: 3 / Nguyễn Thanh Tâm (TV1) / A5, K20
- **FR/NFR/Kỹ năng**: FR-OBS-001, FR-OBS-002; NFR-MAINT-001, NFR-SEC-004; K20
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.API/Program.cs`:
    - Middleware sinh và đính kèm `X-Correlation-ID` trên HTTP Header.
    - Đẩy `CorrelationId` và W3C `TraceId` vào Serilog `LogContext`.
    - Serilog Request Logging ghi nhận `UserId` (từ claim `sub`) hoặc `"anonymous"`.
    - Cấu hình Redaction: Tắt logging sensitive data từ EF Core (`LogEventLevel.Fatal`), tự động lọc bỏ Authorization Header, Refresh Token và Password.
    - Bộ 3 endpoints kiểm tra sức khỏe hệ thống:
      + `GET /health`: Tổng thể toàn bộ dịch vụ (Database, Redis, MinIO).
      + `GET /health/live`: Liveness probe dành cho container orchestrator.
      + `GET /health/ready`: Readiness probe kiểm tra kết nối CSDL và Redis sẵn sàng tiếp nhận traffic.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Kiểm tra `HealthTests.cs` và `AuthTests.cs`:
    - Header `X-Correlation-ID` xuất hiện trên mọi response.
    - JSON log không xuất hiện bất kỳ chuỗi `password`, `securityStamp`, hay `tokenHash`.
    - Lệnh gọi `/health`, `/health/live`, `/health/ready` trả về JSON theo chuẩn RFC và HTTP Status 200 -> **PASS**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 23/09/2026.

---

### Bản ghi 3: Task A6 — Lab cá nhân TV1: FTS Cache Invalidation & SEO Recipe Schema.org JSON-LD
- **Tuần / Người / Task**: 3 / Nguyễn Thanh Tâm (TV1) / A6
- **FR/NFR/Kỹ năng**: FR-SRCH-001, NFR-SEO-001, NFR-PERF-001, NFR-REL-002; K11, K12, K19, K21
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.Application/RecipeJsonLd.cs`: `RecipeJsonLdBuilder` sinh đối tượng JSON-LD tuân thủ nghiêm ngặt Schema.org Recipe và hướng dẫn Google Rich Results (tên món, mô tả, ảnh, tác giả, thời gian chuẩn bị/nấu, khẩu phần, danh mục, nguyên liệu, các bước chi tiết, thông tin dinh dưỡng 6 chỉ số; không tạo rating giả theo đúng quyết định SRS v1.1.1).
  - `src/backend/CulinaryBlog.Infrastructure/RecipeCacheService.cs`: Cài đặt `IRecipeCacheService` hiện thực chiến lược Cache-Aside, Invalidation khi dữ liệu thay đổi, và Resilient Fallback (khi dịch vụ Cache gặp sự cố, hệ thống tự động fallback về gọi trực tiếp CSDL mà không làm gián đoạn người dùng).
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Chạy `Week3AuthAndPersonalLabTests.cs`:
    - `Recipe_json_ld_builder_generates_schema_org_compliant_json`: Kiểm tra cấu trúc Schema.org hợp lệ, các trường `prepTime`, `cookTime` định dạng chuẩn ISO 8601 Duration (`PT30M`, `PT240M`), không chứa rating giả mạo -> **PASS**.
    - `Cache_service_cache_aside_and_invalidation_and_fallback_resilience`: Đo lường Cache MISS ở lần đầu, Cache HIT ở lần 2 (không gọi DB), làm mới dữ liệu sau `InvalidateAsync`, và kiểm chứng cơ chế Fallback thành công khi giả lập Cache Server down -> **PASS**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 23/09/2026.

---

### Bản ghi 4: Task A7 & K24 — Trách nhiệm Nhóm trưởng: Nghiệm thu G3, Giải quyết xung đột merge & Điều phối kiểm thử tích hợp
- **Tuần / Người / Task**: 3 / Nguyễn Thanh Tâm (TV1) / A7, K24
- **FR/NFR/Kỹ năng**: K21, K23, K24
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - Rà soát, nghiệm thu và merge PR #11 của TV2 (`feat/TV2-week2-discovery-search-google`) và PR #12 của TV4.
  - Ban hành tài liệu giải quyết xung đột: `docs/BAO_CAO_GIAI_QUYET_XUNG_DOT_MERGE_TV2_TUAN2.md`.
  - Khắc phục lỗi encoding migration C# và cấu hình đồng bộ locked-mode cho các dự án con.
  - Nghiệm thu Cổng G3: Toàn bộ 34 FR được tích hợp trên nhánh `main`, toàn bộ 90/90 tests vượt qua 100% trên môi trường CI/Dev.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - `dotnet test CulinaryBlog.sln`:
    ```
    Passed! - Failed: 0, Passed:  5, Skipped: 0, Total:  5 (ConcurrencySpike.dll)
    Passed! - Failed: 0, Passed: 85, Skipped: 0, Total: 85 (CulinaryBlog.Tests.dll)
    Total: 90/90 tests passed (100% Green).
    ```
  - `dotnet format CulinaryBlog.sln --verify-no-changes`: Passed 100% không phát sinh cảnh báo vi phạm chuẩn mã nguồn.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 23/09/2026.

---

## 2. Tổng kết tiến độ Tuần 3

| Nhóm nội dung | Kế hoạch tuần 3 | Thực tế hoàn thành | Đánh giá |
|---|---|---|:---:|
| **Xác thực & Bảo mật (Task A5/C5)** | Refresh Token rotation, hash SHA-256, family reuse revocation, logout | Hoàn thành trọn vẹn API, CSDL, logic nghiệp vụ và 4 automated tests | **100%** |
| **Quan sát & Hạ tầng (Task A5/K20)** | W3C Tracing, CorrelationId, Serilog redaction, Health checks | Hoàn thành middleware, lọc dữ liệu nhạy cảm, 3 endpoint probes | **100%** |
| **Lab cá nhân (Task A6/K11,K12,K19)** | FTS cache invalidation, fallback, Recipe Schema.org JSON-LD | Hoàn thành `RecipeJsonLdBuilder` và `RecipeCacheService` kèm tests | **100%** |
| **Trách nhiệm Nhóm trưởng (Task A7/K24)** | Review PRs, giải quyết xung đột merge, nghiệm thu Cổng G3 | Tích hợp TV2 & TV4, xử lý xung đột, 90/90 tests pass trên CI/Dev | **100%** |
