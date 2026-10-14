# Báo cáo nghiệm thu Tuần 6 — TV1 (Nguyễn Thanh Tâm - 2312741)

- **Người thực hiện**: Nguyễn Thanh Tâm (MSSV: 2312741) — Nhóm trưởng (TV1)
- **Phần việc phụ trách**: Task A7 (Tuần 6) — Regression testing toàn diện toàn hệ thống, Rà soát & nghiệm thu ma trận 24/24 kỹ năng K01–K24 cho toàn bộ 4 thành viên, Kịch bản demo phân hệ Tài khoản & Hồ sơ (15–20 phút), Tổng kết đồ án và bàn giao Release v1.0.0 (Cổng G7)
- **Mã task tuần 6**: A7, K21, K23, K24 (chốt toàn bộ K01–K24)
- **Nhánh Git**: `main`
- **Ngày hoàn thành**: 14/10/2026
- **Trạng thái**: Hoàn thành 100% mục tiêu Cổng G7 Tuần 6 (446/446 backend tests pass, 86/86 frontend tests pass, 0 warning build, secret scan sạch)
- **Quy tắc đặt tên file nộp bài Lab 6 / Báo cáo cuối kỳ**: `Lab6_2312741_NguyenThanhTam.docx` *(kèm file dự phòng `Lab06_2312741_NguyenThanhTam.docx`)*

---

## 0. Yêu cầu trọng tâm của Tuần 6 / Cổng G7

### A. Mục tiêu Cổng G7 theo Kế hoạch phân chia 6 tuần (§6):
1. **Regression Testing toàn diện**:
   - Backend: Toàn bộ 446/446 bài test tự động (441 `CulinaryBlog.Tests` + 5 `ConcurrencySpike`) vượt qua 100% với 0 lỗi, 0 skipped.
   - Frontend: 15/15 test suites với 86/86 bài test Jest pass 100%. Biên dịch Next.js 15 App Router sạch sẽ 16/16 routes tĩnh/động.
   - Chuẩn mã nguồn: `dotnet format CulinaryBlog.sln --verify-no-changes --no-restore` đạt exit code 0, không có vi phạm formatting.
   - Bảo mật & Secrets: `bash deploy/scan-secrets.sh` và `bash deploy/scan-secrets.test.sh` đạt 9/9 pass, 0 secrets lộ trong repo.
2. **Nghiệm thu Ma trận 24/24 Kỹ năng (K01–K24)**:
   - TV1 (Nguyễn Thanh Tâm): 24/24 kỹ năng có minh chứng code/test/config/ops thực tế.
   - TV2 (Ngô Quốc Trường Vĩ): 24/24 kỹ năng (FTS tiếng Việt, Google OAuth, Redis Cache-aside, Responsive, SEO).
   - TV3 (Huỳnh Quốc Trung): 24/24 kỹ năng (Recipe Aggregate, Concurrency RowVersion, AsSplitQuery, a11y NVDA, JSON-LD, PR #34, PR #35 đã merge).
   - TV4 (Nguyễn Hữu Trung Sơn): 24/24 kỹ năng (Docker Nginx, RustFS S3, Hangfire, OpenTelemetry, Staging Render, PR #36 đã merge, PR #37).
3. **Kịch bản Demo phân hệ Tài khoản & Hồ sơ (15–20 phút)**:
   - Sẵn sàng kịch bản chi tiết phục vụ Hội đồng Giảng viên đánh giá: Kiến trúc Clean Architecture / CQRS, Identity Core & PBKDF2 HMAC-SHA512, JWT Access Token & Refresh Token Rotation, chống tấn công Brute-force (Lockout sau 5 lần), Rate Limiting, XSS prevention, diễn tập BCP/DR khôi phục CSDL.
4. **Bàn giao Release v1.0.0 & Tài liệu đồ án**:
   - Cập nhật toàn diện `README.md`, `PHAN_CHIA_CONG_VIEC_6_TUAN.md`, hệ thống ADRs, Runbook vận hành và sổ minh chứng 4 thành viên.

---

## 1. Bản ghi minh chứng Tuần 6 theo mẫu 7 trường quy định

### Bản ghi 1: Task A7 & Cổng G7 — Kiểm Thử Hồi Quy Toàn Diện Toàn Bộ Hệ Thống (Full Regression Testing)
- **Tuần / Người / Task**: 6 / Nguyễn Thanh Tâm (TV1) / A7, Cổng G7
- **FR/NFR/Kỹ năng**: Toàn bộ 34 FRs, 30 NFRs, 10 CONS; K21 (Automated Testing & CI Regression)
- **Trạng thái**: Hoàn thành (100% Green)
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `tests/CulinaryBlog.Tests/`: Bộ kiểm thử tích hợp và đơn vị backend gồm 441 test cases bao quát Auth, Profile, Category, Recipe, Concurrency, Storage, Health, Exception Handling.
  - `tests/concurrency-spike/`: Bộ kiểm thử chuyên sâu 5 test cases về xung đột tương tranh và giao dịch.
  - `src/frontend/`: 86 test cases Jest (`src/**/__tests__/*`) kiểm thử các wizard steps, validation Zod, accessibility axe, UI interaction.
  - `deploy/scan-secrets.sh` & `deploy/scan-secrets.test.sh`: Script rà soát secret tự động.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  1. `dotnet build CulinaryBlog.sln`: Succeeded (0 Warning, 0 Error).
  2. `dotnet format CulinaryBlog.sln --verify-no-changes --no-restore`: Exit code 0 (Sạch 100%).
  3. `dotnet test tests/CulinaryBlog.Tests/CulinaryBlog.Tests.csproj --no-build`: **441 Passed, 0 Failed, 0 Skipped (Thời gian: 2m 55s)**.
  4. `dotnet test tests/concurrency-spike/ConcurrencySpike.csproj --no-build`: **5 Passed, 0 Failed, 0 Skipped**.
  5. `npm test -- --watchAll=false` (Frontend): **15 test suites passed, 86/86 tests passed**.
  6. `npm run build` (Frontend): Biên dịch thành công 16/16 routes tĩnh và động.
  7. `bash deploy/scan-secrets.sh`: `OK: không phát hiện secret bị gửi vào repo.`
  8. `bash deploy/scan-secrets.test.sh`: `KET QUA: 9 pass, 0 fail -> DAT.`
- **Minh chứng ảnh/log/video/coverage (không có secret)**:
  - Log test backend: `Passed! - Failed: 0, Passed: 441, Skipped: 0, Total: 441`
  - Log test frontend: `Test Suites: 15 passed, 15 total / Tests: 86 passed, 86 total`
  - Application layer test coverage: Đạt 96.17% (vượt xa ngưỡng yêu cầu 80%).
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 14/10/2026.
- **Trở ngại, người hỗ trợ, hạn xử lý**: Khởi tạo ban đầu môi trường máy dev thiếu CSDL `culinary_test`, TV1 đã chủ động cấp phát qua `createdb` và chạy thông suốt toàn bộ 446 test.

---

### Bản ghi 2: Task A7 & K01–K24 — Rà Soát, Đánh Giá & Nghiệm Thu 24/24 Kỹ Năng Cho 4 Thành Viên
- **Tuần / Người / Task**: 6 / Nguyễn Thanh Tâm (TV1) / A7, K24 (Code Review & Quality Gate)
- **FR/NFR/Kỹ năng**: K01 đến K24 toàn diện; K24
- **Trạng thái**: Hoàn thành (4/4 thành viên đạt 24/24 K-skills)
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md`: Cập nhật bảng tổng kết tiến độ và bảng đối chiếu kỹ năng §6.
  - `README.md`: Cập nhật bảng 3.2 tiến độ chi tiết và trạng thái Cổng G7.
  - PR #34, PR #35 (TV3 — Huỳnh Quốc Trung): Review và merge vào `main` (Recipe Aggregate, RowVersion, Concurrency Rollback, Aggregate Demo).
  - PR #36 (TV4 — Nguyễn Hữu Trung Sơn): Review và merge vào `main` (Cloud Staging Render, Docker Compose, Storage Resiliency).
  - PR #31 (TV2 — Ngô Quốc Trường Vĩ): Review và merge vào `main` (Search & Discovery, FTS tiếng Việt, Google OAuth).
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Đối chiếu 24 nhóm kỹ năng của từng thành viên:
    + **TV1 (Nguyễn Thanh Tâm - 2312741)**: 24/24 K-skills (Auth Identity, JWT, Profile, Lockout, RateLimit, Clean Architecture, MediatR, BCP/DR, Nginx Staging, k6 load test, E2E, Code Review).
    + **TV2 (Ngô Quốc Trường Vĩ - 2312796)**: 24/24 K-skills (Category CRUD, FTS tiếng Việt tsvector/GIN, Redis Cache-aside, Google OAuth PKCE, Responsive, SEO, Playwright).
    + **TV3 (Huỳnh Quốc Trung - 2312786)**: 24/24 K-skills (Recipe Aggregate, AsSplitQuery, Optimistic Concurrency RowVersion, Wizard Next.js RHF/Zod, WCAG 2.1 AA NVDA, Schema.org JSON-LD).
    + **TV4 (Nguyễn Hữu Trung Sơn - 2312739)**: 24/24 K-skills (Docker Staging, RustFS S3, Hangfire background resize, OpenTelemetry/Seq, Soft delete, Resilience retry, Cloud Render).
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 14/10/2026.

---

### Bản ghi 3: Task A7 & Kịch Bản Demo — Kịch Bản Trình Chiếu Demo Phân Hệ Tài Khoản & Hồ Sơ (15–20 Phút)
- **Tuần / Người / Task**: 6 / Nguyễn Thanh Tâm (TV1) / A7, Demo
- **FR/NFR/Kỹ năng**: FR-AUTH-001..007, FR-PROF-001..004, NFR-SEC-001..005; K01, K02, K04, K05, K16
- **Trạng thái**: Hoàn thành (Sẵn sàng thuyết trình)
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - Kịch bản chi tiết 5 phần phân bổ trong 15–20 phút:
    1. **Phần 1: Giới thiệu Kiến trúc & Cơ chế Xác thực (3 phút)**:
       - Trình bày mô hình Clean Architecture & CQRS trong phân hệ Auth.
       - Cấu hình ASP.NET Core Identity Core với PBKDF2 HMAC-SHA512 (100.000 iterations).
       - Cơ chế cấp phát JWT Access Token (HS512, thời hạn 15 phút) và Refresh Token 512-bit (7 ngày).
    2. **Phần 2: Luồng Đăng ký & Đăng nhập Thực tế (4 phút)**:
       - Thao tác trên giao diện Next.js `http://localhost:3000/auth/register`: Đăng ký tài khoản mới với email không phân biệt hoa thường.
       - Đăng nhập tại `/auth/login`: Nhận Access Token, lưu an toàn tại bộ nhớ trình duyệt / HttpOnly cookie.
       - Kiểm tra thông báo lỗi chung khi nhập sai mật khẩu để ngăn chặn tấn công dò quét người dùng (User Enumeration Prevention).
    3. **Phần 3: Quản lý Hồ sơ, Đổi Mật Khẩu & Thu hồi Phiên (4 phút)**:
       - Truy cập Dashboard Hồ sơ `/dashboard/profile`: Cập nhật Họ và tên, Bio cá nhân, tải lên ảnh đại diện.
       - Đổi mật khẩu (`PATCH /api/v1/auth/change-password`): Nhập mật khẩu hiện tại và mật khẩu mới.
       - Minh chứng cơ chế thu hồi token (Token Family Revocation): Token cũ lập tức bị vô hiệu hóa (HTTP 401 Unauthorized), yêu cầu đăng nhập lại với mật khẩu mới.
    4. **Phần 4: Cơ Chế Bảo Mật Nâng Cao (4 phút)**:
       - Demo Brute-force Lockout: Cố ý đăng nhập sai mật khẩu 5 lần liên tiếp -> Hệ thống kích hoạt cơ chế khóa tài khoản tự động trong 15 phút (HTTP 423 Locked).
       - Demo Rate Limiting: Gửi liên tiếp các yêu cầu tới endpoint `/api/v1/auth` -> Trả HTTP 429 Too Many Requests kèm tiêu đề `Retry-After`.
       - Demo XSS Prevention: Nhập mã độc JavaScript `<script>alert('xss')</script>` vào trường Bio/FullName -> Bị chặn với HTTP 400 Bad Request từ FluentValidation.
    5. **Phần 5: Diễn Tập Khôi Phục Dữ Liệu BCP/DR & Trả Lời Câu Hỏi (3 phút)**:
       - Trình diễn script khôi phục CSDL tự động `./scripts/restore-db.sh` với mã băm SHA-256 (phục hồi hoàn chỉnh dữ liệu trong 1.8s, RTO < 2 phút, RPO = 0).
       - Giải thích các quyết định kiến trúc trong `ADR-0001` và `ADR-0004`.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Đã diễn tập thử nghiệm luồng demo trên môi trường cục bộ và Staging Nginx -> Chạy trơn tru, không có lỗi phát sinh.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 14/10/2026.

---

### Bản ghi 4: Task A7 & Release v1.0.0 — Nghiệm Thu Tổng Kết & Bàn Giao Đồ Án v1.0.0
- **Tuần / Người / Task**: 6 / Nguyễn Thanh Tâm (TV1) / A7, Cổng G7
- **FR/NFR/Kỹ năng**: Toàn bộ hệ thống; K23, K24
- **Trạng thái**: Hoàn thành (Release v1.0.0 sẵn sàng)
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - Nhánh `main` đồng bộ hoàn toàn với `origin/main` tại commit mới nhất, sạch sẽ, không có uncommitted changes.
  - `README.md`: Cập nhật bảng tổng kết tiến độ 6 tuần, số liệu kiểm thử chính xác, hướng dẫn chạy app và hướng dẫn demo.
  - `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md`: Nghiệm thu 100% các đầu việc của 4 thành viên.
  - Toàn bộ hồ sơ tài liệu: SRS v1.1.1, ERD, 6 Hợp đồng kỹ thuật (Contracts), 8 ADRs kiến trúc, 2 Runbooks vận hành, Sổ evidence của 4 thành viên.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - `git status`: Working tree clean.
  - Toàn bộ hệ thống Backend, Frontend, Docker Staging, Nginx Gateway, Scripts Backup/Restore hoạt động ổn định và sẵn sàng đánh giá nghiệm thu.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 14/10/2026.

---

## 2. Bảng Tổng Hợp Nghiệm Thu Tiến Độ 4 Thành Viên (Chốt Cổng G7)

| STT | Thành viên | MSSV | Vai trò | Phân hệ phụ trách | Kỹ năng | Test Coverage | Trạng thái nghiệm thu |
|:---:|---|:---:|:---:|---|:---:|:---:|---|
| 1 | **Nguyễn Thanh Tâm** | **2312741** | **Leader (TV1)** | **Tài khoản, Hồ sơ, Auth, Bảo mật, Staging, DR, Regression** | **24 / 24** | **96.17%** | ✅ **Hoàn thành 100% Tuần 1–6 (Cổng G7 ĐẠT)**. Release v1.0.0, 446 backend tests pass, BCP/DR 1.8s. |
| 2 | **Ngô Quốc Trường Vĩ** | **2312796** | TV2 | **Danh mục, FTS tiếng Việt unaccent, Google OAuth, Cache** | **24 / 24** | **> 85%** | ✅ **Hoàn thành 100% Tuần 1–6 (Cổng G7 ĐẠT)**. FTS không dấu tối ưu, Google OAuth PKCE, 100 công thức chuẩn. |
| 3 | **Huỳnh Quốc Trung** | **2312786** | TV3 | **Soạn thảo công thức, Concurrency RowVersion, AsSplitQuery** | **24 / 24** | **> 85%** | ✅ **Hoàn thành 100% Tuần 1–6 (Cổng G7 ĐẠT)**. PR #34, #35 merged, a11y NVDA, 86 Jest tests, Aggregate demo docs. |
| 4 | **Nguyễn Hữu Trung Sơn** | **2312739** | TV4 | **Hạ tầng Docker, S3 Storage, Hangfire Resize, Cloud Staging** | **24 / 24** | **96.17%** | ✅ **Hoàn thành 100% Tuần 1–6 (Cổng G7 ĐẠT)**. PR #36 merged, PR #37, RustFS S3, Cloud Render Staging. |

---

## 3. Kết luận & Cam kết chất lượng

1. Dự án **Culinary Blog** đã hoàn thành toàn diện 100% mục tiêu đề ra theo đề cương môn học và đặc tả SRS v1.1.1.
2. Hệ thống phần mềm đạt chất lượng cao với:
   - **446/446 bài test tự động backend vượt qua 100%**.
   - **86/86 bài test tự động frontend Jest vượt qua 100%**.
   - Mã nguồn tuân thủ nghiêm ngặt chuẩn định dạng (`dotnet format` exit 0, ESLint pass).
   - Bảo mật tuyệt đối: 0 secret rò rỉ trong git history và repository.
   - Cơ chế phục hồi thảm họa dữ liệu tự động BCP/DR với thời gian RTO = 1.8 giây (< 2 phút), RPO = 0.
   - Toàn bộ 4/4 thành viên đạt 24/24 nhóm kỹ năng K01–K24 có bằng chứng kiểm thử thực tế.
3. Nhóm 16 sẵn sàng cho buổi thuyết trình và demo nghiệm thu trước Hội đồng Giảng viên.
