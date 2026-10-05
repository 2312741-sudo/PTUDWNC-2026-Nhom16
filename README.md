# Culinary Blog — Nền tảng Chia sẻ & Khám phá Công thức Ẩm thực

> **Đồ án môn học**: Phát Triển Ứng Dụng Web Nâng Cao (PTUDWNC) — Học kỳ 1, Năm 2026  
> **Nhóm thực hiện**: Nhóm 16  
> **Kho lưu trữ GitHub**: [2312741-sudo/PTUDWNC-2026-Nhom16](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16)  
> **Nhóm trưởng & Reviewer toàn bộ**: Nguyễn Thanh Tâm (MSSV: 2312741)  

---

## 👥 1. Danh sách Thành viên & Phân công Trách nhiệm

Dự án được phân chia theo chiều dọc nghiệp vụ (mỗi thành viên phụ trách trọn vẹn từ Database, Backend CQRS/Minimal APIs, Frontend UI Next.js, Bảo mật, Kiểm thử cho đến Vận hành) theo [Kế hoạch phân chia 6 tuần](docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md):

| STT | Thành viên | MSSV | Vai trò | Phân hệ nghiệp vụ phụ trách | Task quy định |
|:---:|---|:---:|:---:|---|:---:|
| 1 | **Nguyễn Thanh Tâm** | **2312741** | **Nhóm trưởng (Leader)** | **Tài khoản, Hồ sơ, Nền tảng Xác thực (Auth), Token Rotation, Bảo mật, Background Worker, Lab Cache/SEO & CI/CD** | A1 – A7 |
| 2 | **Ngô Quốc Trường Vĩ** | **2312796** | Thành viên | **Danh mục ẩm thực, Tìm kiếm Full-Text Search tiếng Việt không dấu & Đăng nhập Google OAuth** | B1 – B7 |
| 3 | **Huỳnh Quốc Trung** | **2312786** | Thành viên | **Soạn thảo Công thức (Recipe Aggregate), Nguyên liệu/Các bước, Concurrency RowVersion & Bảng RefreshTokens** | C1 – C7 |
| 4 | **Nguyễn Hữu Trung Sơn** | **2312739** | Thành viên | **Hạ tầng Docker/Nginx, Storage MinIO, Xử lý ảnh/Resize, Xuất bản món ăn, Logout & Giám sát** | D1 – D7 |

---

## 📅 2. Phân Công Công Việc Hàng Tuần (Lịch Trình 6 Tuần)

Kế hoạch thực hiện dự án được tổ chức chặt chẽ qua 6 tuần theo [PHAN_CHIA_CONG_VIEC_6_TUAN.md](docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md), định nghĩa rõ ràng công việc của từng thành viên theo từng tuần, gắn liền với các Cổng Hoàn Thành (Milestones G0–G7) và ma trận 24 nhóm kỹ năng:

### 2.1. Bảng Tổng Hợp Phân Công Công Việc 6 Tuần

| Tuần | TV1 — Nguyễn Thanh Tâm (Leader) | TV2 — Ngô Quốc Trường Vĩ | TV3 — Huỳnh Quốc Trung | TV4 — Nguyễn Hữu Trung Sơn | Cổng hoàn thành (Milestone) |
|:---:|---|---|---|---|---|
| **1** | • Identity/Roles, Migration DB<br>• JWT Service & ICurrentUser<br>• Register/Login Minimal API<br>• Problem Details, Serilog, CI<br>• Lab Kiến trúc & Ban hành Auth Contract | • Category Model/Migration/Seed<br>• Admin CRUD Category cơ bản<br>• Public UI Shell Next.js<br>• List DTO/Pagination (pageSize=12)<br>• Chốt Google Auth Contract | • ERD/Recipe/Nutrition owned<br>• Config, Migrations Recipe<br>• Draft CRUD tối thiểu<br>• Concurrency spike & UoW<br>• Thống nhất RowVersion/DTO | • Docker Compose full-stack<br>• PostgreSQL, Redis, MinIO, MailHog<br>• Nginx proxy, Health Probes<br>• Storage interface contracts<br>• Logout endpoint thu hồi token | **G0 (Giữa tuần)**: Dev stack chạy thông suốt.<br>**G1 (Cuối tuần)**: Đăng ký → Đăng nhập → Category → Draft Recipe. |
| **2** | • Lockout 5 lần / 15 phút, Rate Limit<br>• Update Profile API (`PATCH /me`)<br>• Dashboard UI Profile RHF/Zod<br>• Welcome Email Worker (Channel/Retry)<br>• CSDL 25 cat / 100 rec, Lab 2 | • UI Quản lý & Detail Category<br>• Google Login Code+PKCE/Auth.js<br>• List/Filter/Sort/Pagination recipes<br>• FTS tiếng Việt unaccent ban đầu<br>• Tối ưu Category Cache 60m | • Ingredients & Steps CRUD động<br>• Thứ tự OrderIndex & TimerMinutes<br>• Wizard soạn thảo đa bước<br>• Bảng RefreshTokens EF Core<br>• Tests chống sửa chéo dữ liệu | • Upload/delete ảnh JPEG/PNG/WebP<br>• Worker resize ảnh (300×300, 800×600)<br>• UI Upload progress & Preview<br>• Chuyển trạng thái Publish/Unpublish<br>• Kiểm tra điều kiện publish (C02) | **G2 (Cuối tuần)**: Draft đủ nguyên liệu/bước/ảnh; Publish được; bảo vệ toàn vẹn owner. |
| **3** | • Refresh Token Rotation & Hash<br>• Token Reuse Family Revocation<br>• Logout thu hồi token CSDL<br>• W3C Tracing & Serilog Redaction<br>• LAB Cache-aside & Schema JSON-LD<br>• Nghiệm thu Cổng G3 | • Search SSR hoàn chỉnh<br>• Redis Cache-aside & OutputCache<br>• Cache Invalidation sau mutation<br>• LAB cá nhân Identity & Jobs<br>• Tinh chỉnh query GIN Index | • Hoàn thiện Editor/Detail/Dashboard<br>• Optimistic concurrency rollback<br>• Nutrition calculation & JSON-LD<br>• LAB cá nhân Google OAuth & MinIO<br>• Tích hợp bộ ảnh và tìm kiếm | • Quản lý vòng đời Archive/Delete<br>• Tự động sinh Sitemap XML, robots.txt<br>• Giám sát OpenTelemetry HTTP/DB<br>• LAB cá nhân Identity & FTS<br>• Queue persistent jobs | **G3 (Cuối tuần)**: Đủ 34 FR bản tích hợp; giao dịch xuyên suốt vòng đời công thức; lab cá nhân chạy thật; 90 tests pass. |
| **4** | • Security Negative Tests<br>• E2E flows: Register, Login, Refresh<br>• Nâng cao Code Coverage (≥ 80%)<br>• Hoàn thiện 24/24 kỹ năng K01–K24<br>• Duy trì CI Green tuyệt đối | • k6 load testing & EXPLAIN query<br>• Cache-hit metrics & tối ưu N+1<br>• Responsive & WCAG2.1 AA a11y<br>• Search E2E flows Playwright<br>• Hoàn thiện 24/24 kỹ năng K01–K24 | • Concurrency & Architecture tests<br>• Transaction rollback tests<br>• Recipe E2E flow Playwright<br>• Code Coverage tầng Application ≥ 80%<br>• Hoàn thiện 24/24 kỹ năng K01–K24 | • Resilience, retry & race tests<br>• Shared cache & multi-worker<br>• Publish E2E flows Playwright<br>• Kịch bản phục hồi lỗi dependency<br>• Hoàn thiện 24/24 kỹ năng K01–K24 | **G4 (Cuối tuần)**: Mỗi người đạt 24/24 nhóm kỹ năng có minh chứng; line coverage ≥ 80%; không còn lỗi bảo mật. |
| **5** | • Tự deploy & restore DB độc lập<br>• Đo lường tải & bảo mật Auth<br>• Review kiểm thử TV4 (Trung Sơn)<br>• Cập nhật tài liệu kiến trúc & ADR<br>• Chuẩn bị môi trường staging | • Tự deploy & restore DB độc lập<br>• Kiểm tra SEO/CWV/JS bundle size<br>• Rà soát Public metadata & Canonical<br>• Review kiểm thử TV1 (Thanh Tâm)<br>• Tinh chỉnh cấu hình caching | • Tự deploy & restore DB độc lập<br>• Điều phối migration sạch trên staging<br>• Rà soát tính nhất quán dữ liệu<br>• Review kiểm thử TV2 (Trường Vĩ)<br>• Hoàn thiện tài liệu Schema | • Tự deploy & restore DB độc lập<br>• Thiết lập HTTPS, CORS & Volumes<br>• Runbook hướng dẫn vận hành<br>• Review kiểm thử TV3 (Quốc Trung)<br>• Hoàn thiện kịch bản sao lưu | **G5 (Cuối tuần)**: Staging độc lập khởi động từ checkout sạch; restore dữ liệu thành công; 5 E2E flows pass. |
| **6** | • Regression testing toàn hệ thống<br>• Chốt toàn bộ Evidence cá nhân<br>• Demo phân hệ Tài khoản & Hồ sơ<br>• Nghiệm thu & Bàn giao đồ án | • Regression testing tính năng Search<br>• Chốt toàn bộ Evidence cá nhân<br>• Demo phân hệ Danh mục & Khám phá<br>• Nghiệm thu & Bàn giao đồ án | • Regression testing Soạn thảo món<br>• Chốt toàn bộ Evidence cá nhân<br>• Demo phân hệ Soạn thảo & Concurrency<br>• Nghiệm thu & Bàn giao đồ án | • Regression testing Media & Vận hành<br>• Chốt toàn bộ Evidence cá nhân<br>• Demo phân hệ Xuất bản & Deploy/Ops<br>• Nghiệm thu & Bàn giao đồ án | **G6 / G7 (Cuối kỳ)**: Bàn giao toàn diện hệ thống, release v1.0, minh chứng 4 thành viên và demo hoàn tất. |

### 2.2. Chi Tiết Trách Nhiệm Phân Theo Từng Thành Viên

- 🛡️ **TV1 — Nguyễn Thanh Tâm (Nhóm trưởng / 2312741)**:
  - *Chuyên trách*: Phân hệ Xác thực (Identity, JWT, Refresh Token Rotation, Family Reuse Revocation), Tài khoản & Hồ sơ cá nhân (Profile, Dashboard), Cơ chế bảo mật (Account Lockout, Rate Limiting, Redaction), Xử lý lỗi RFC 7807, Background Worker, Lab Cache Invalidation & Schema.org JSON-LD SEO, CI/CD Pipeline.
  - *Nhiệm vụ Nhóm trưởng*: Ban hành hợp đồng kỹ thuật (Auth Contract), rà soát và giải quyết 9 mâu thuẫn hệ thống SRS v1.1.1, hướng dẫn kiểm thử app, xử lý xung đột merge, review và phê duyệt toàn bộ Pull Requests của các thành viên.
- 📂 **TV2 — Ngô Quốc Trường Vĩ (Thành viên / 2312796)**:
  - *Chuyên trách*: Phân hệ Danh mục Ẩm thực (Category CRUD, Slug tiếng Việt, Hard delete C07, Cache 60m C03), Tìm kiếm Full-Text Search tiếng Việt không dấu (PostgreSQL tsvector & GIN Index), Tích hợp đăng nhập bên thứ ba Google OAuth 2.0 (PKCE Flow).
- 📝 **TV3 — Huỳnh Quốc Trung (Thành viên / 2312786)**:
  - *Chuyên trách*: Phân hệ Soạn thảo Công thức (Recipe Aggregate, Nutrition Value Object, Soft delete C01, điều kiện publish C02), Quản lý Nguyên liệu & Các bước thực hiện (`OrderIndex`, `TimerMinutes`), Kiểm soát tương tranh lạc quan (Optimistic Concurrency với `RowVersion`), Bảng RefreshTokens và mô hình dữ liệu.
- 🚀 **TV4 — Nguyễn Hữu Trung Sơn (Thành viên / 2312739)**:
  - *Chuyên trách*: Hạ tầng Container hóa (Docker Compose, Nginx Reverse Proxy, Health Probes), Phân hệ Lưu trữ ảnh MinIO S3 & Tự động Resize đa kích thước (300×300, 800×600), Quản lý vòng đời Xuất bản công thức (Publish / Unpublish / Archive), Tự động sinh Sitemap XML & Giám sát hệ thống.

---

## 📊 3. Đánh Giá Tiến Độ Hoàn Thành Của Nhóm

### 3.1. Đánh giá tổng quan theo các Cổng Hoàn Thành (Milestones)

- ✅ **Cổng G0 (Giữa Tuần 1 - Hạ tầng Dev & Skeletons)**: **ĐẠT 100%**. Đã khởi dựng thành công trọn bộ stack container hóa (PostgreSQL 16, Redis 7, MinIO, MailHog, Seq, Nginx) và khung mã nguồn Clean Architecture + Next.js App Router.
- ✅ **Cổng G1 (Cuối Tuần 1 - Tích hợp Đăng ký, Đăng nhập, Danh mục & Schema Recipe)**: **ĐẠT 100%**. Đã tích hợp thành công toàn bộ PR của 4 thành viên vào nhánh chính `main`.
- ✅ **Cổng G2 (Cuối Tuần 2 - Hồ sơ người dùng, Khám phá & FTS, CSDL hạt giống 25 categories & 100 recipes)**: **ĐẠT 100%**. Hoàn thành tạo CSDL với 25 Categories, 100 Recipes (mỗi recipe ≥ 10 nguyên liệu, ≥ 5 bước), Lockout 5 lần, Rate limiting, giao diện Profile Dashboard và FTS tiếng Việt ban đầu.
- ✅ **Cổng G3 (Cuối Tuần 3 - Tích hợp Toàn diện 34 FR, Refresh Token Rotation, Google OAuth, Cache & SEO)**: **ĐẠT 100%**. Đã tích hợp thành công toàn bộ PRs của nhóm (PR #11 TV2, PR #12 TV4, các tính năng Tuần 3 TV1). Đạt **90 / 90 tests tự động pass 100% (Green)** tại thời điểm chốt G3, `dotnet format` sạch sẽ không vi phạm.
- ✅ **Cổng G4 (Cuối Tuần 4 — Nghiệm thu Toàn bộ 41/41 API Endpoints, Đổi Mật Khẩu, QA Tích hợp & Đạt Chuẩn Lab 4)**: **ĐẠT 100% — cập nhật 30/09/2026**.
  - **Yêu cầu tối thiểu Lab 4**: Cài đặt và tích hợp hoàn chỉnh **41 / 41 API endpoints** trên toàn bộ 7 phân hệ nghiệp vụ.
  - **Bộ test tự động**: Đạt **177 / 177 tests PASSED 100% (Green)** (172 `CulinaryBlog.Tests` + 5 `ConcurrencySpike`, `Skipped=0`).
  - **QA tích hợp Frontend–Backend**: Nghiệm thu **41 / 41 trường hợp PASS** (tài liệu `TEST_CASE_TICH_HOP_FE_BE.md`).
  - **Pull Requests**: Đã merge 100% các PR vào `main` (PR #16 TV4 Hangfire/Media Proxy, PR #18 TV2 Search FTS/Cache-Aside, PR #19 TV4 sửa lỗi upload ảnh 500, nạp `.env` tự động & `DevConfigParityTests`).
  - **Biên dịch Frontend**: Next.js 15 App Router `npm run build` thành công **15/15 routes tĩnh và động** không phát sinh cảnh báo.
  - **Báo cáo Lab 4**: Cả 4 thành viên đã hoàn tất báo cáo cá nhân chuẩn quy tắc `Lab4_MSSV_HoVaTen.docx` và lưu trữ minh chứng đầy đủ tại `docs/evidence/`.

### 3.2. Bảng theo dõi tiến độ chi tiết từng thành viên (Cập nhật ngày 30/09/2026 - Tuần 4)

| Thành viên | Tiến độ Tuần 1 | Tiến độ Tuần 2 | Tiến độ Tuần 3 | Tiến độ Tuần 4 | Kỹ năng xác nhận | Trạng thái nghiệm thu |
|---|:---:|:---:|:---:|:---:|:---:|---|
| **TV1 — Nguyễn Thanh Tâm** *(Leader)* | **100%** (A1, A2, A5, CI) | **100%** (A2, A3, A4, A7, Seed CSDL, Lab 2) | **100%** (A5, A6, A7, Refresh Token, Family Revocation, Cache/SEO Lab, Lab 3) | **100%** (A6, A7: Đổi mật khẩu, PBKDF2 HMAC-SHA512, thu hồi phiên cũ, UI Profile, 5 tests bảo mật, Lab 4) | **24 / 24** (K01–K24) | ✅ **Hoàn thành Tuần 1, 2, 3 & 4**. Đạt 177/177 tests tự động, CI Green, giải quyết toàn bộ xung đột merge, ban hành báo cáo Lab 4 đầy đủ. |
| **TV2 — Ngô Quốc Trường Vĩ** | **100%** (B1, B2 nền, B6) | **100%** (B2, B3, B4, B7) | **100%** (B3, B5, B6, B7: FTS GIN Index, SSR Search, Cache-Aside/Invalidation, Fallback, Lab K07/K08/K13) | **100%** (B3, B4, B5, B7: GIN Trigram Index tiếng Việt, SSR Search, Caching phân tầng Redis, Robots noindex, PR #18 merged, Lab 4) | **24 / 24** (K01–K24) | ✅ **Hoàn thành Tuần 1, 2, 3 & 4**. PR #18 đã merge vào `main`, hoàn thiện Search SSR + FTS GIN Index, Cache-Aside TTL phân tầng, 10/10 tests pass, báo cáo Lab 4 đầy đủ. |
| **TV3 — Huỳnh Quốc Trung** | **100%** (C1, C6 nền) | **100%** (C2, C3, C5 nền) | **100%** (Recipe Aggregate, RowVersion, RefreshTokens) | **100%** (C4, C5, C7: Wizard 5 bước, Dashboard quản lý công thức, OCC RowVersion/xmin, Reorder 2-pha, RecipeAuthoringFlowTests, Lab 4) | **24 / 24** (K01–K24) | ✅ **Hoàn thành Tuần 1, 2, 3 & 4**. Hoàn thiện Wizard soạn thảo 5 bước, OCC chống Lost Update, 5/5 concurrency tests pass, build 15/15 trang Next.js sạch, báo cáo Lab 4 đầy đủ. |
| **TV4 — Nguyễn Hữu Trung Sơn** | **100%** (D1, D3, D5, D6) | **100%** (D1, D2, D3) | **100%** (D23 Hangfire queue + dashboard, D27 media proxy theo PA-2, D2 resize 300×300/800×600, D4 sitemap/robots/OG/JSON-LD, D5 EXPLAIN + k6, D6 Lab L4 39/39, gỡ sự cố CI 5 run đỏ) | **100%** (D2, D4, D5, D6, D23, D27: Fix lỗi 500 upload ảnh, EnvFileLoader .env, DevConfigParityTests, QA tích hợp 41/41 endpoints, PR #16 & #19 merged, Lab 4) | **24 / 24** (K01–K24) | ✅ **Hoàn thành Tuần 1, 2, 3 & 4**. PR #16 & PR #19 đã merge vào `main`. Khắc phục triệt để lỗi upload ảnh, nạp `.env` tự động, nghiệm thu QA 41/41 test cases pass, báo cáo Lab 4 đầy đủ. |

> **Ghi chú nghiệm thu**: Toàn bộ 4 thành viên nhóm 16 đã hoàn tất 100% khối lượng công việc của Tuần 4 và đạt đầy đủ yêu cầu tối thiểu của Lab 4 (41/41 API endpoints hoàn chỉnh). File báo cáo cá nhân của từng thành viên đã được chuẩn hóa theo đúng quy tắc `Lab4_MSSV_HoVaTen.docx`.

---

## 🚀 4. Những Gì Nhóm Đã Hoàn Thành Thực Tế

### 4.1. Backend API & Kiến trúc hệ thống (.NET 10 Minimal APIs)

Hệ thống được thiết kế theo **Clean Architecture** kết hợp mô hình **CQRS** (MediatR), tuân thủ nghiêm ngặt nguyên tắc phân tầng và tiêu chuẩn RESTful:

1. **Phân hệ Xác thực, Hồ sơ & Bảo mật Token (TV1 - Nguyễn Thanh Tâm)**:
   - **Đăng ký tài khoản (`POST /api/v1/auth/register`)**: Chuẩn hóa theo SRS v1.1.1 §8.1 (`fullName`, `userName`, `email`, `password`), tự động cấp quyền `Author`, mã hóa mật khẩu PBKDF2 100.000 iterations, kiểm tra trùng lặp email bất kể hoa thường, cấp ngay Access Token JWT (15 phút) và Refresh Token 512-bit (7 ngày) kèm `expiresAt` (ISO 8601).
   - **Đăng nhập (`POST /api/v1/auth/login`)**: Xác thực tài khoản với `SignInManager`. Trả thông báo lỗi mờ chung khi sai thông tin để chống user enumeration. Cấp phát cặp Access Token và Refresh Token mới. Toàn bộ response bọc chuẩn `{ "data": ... }`.
   - **Đổi mật khẩu bảo mật (`POST /api/v1/auth/change-password`)**: Yêu cầu Bearer Token xác thực. Xác minh mật khẩu cũ bằng PBKDF2 HMAC-SHA512. Áp dụng quy tắc mật khẩu mạnh qua FluentValidation (tối thiểu 8 ký tự, chữ hoa, chữ thường, số, ký tự đặc biệt, không trùng mật khẩu cũ). Tự động cập nhật `SecurityStamp` và **thu hồi toàn bộ Refresh Tokens phiên cũ** của người dùng, trả HTTP `204 NoContent`.
   - **Cơ chế Khóa tài khoản (Account Lockout)**: Tự động khóa tạm thời tài khoản 15 phút khi người dùng đăng nhập sai 5 lần liên tiếp. Trả về mã lỗi HTTP `423 Locked` kèm mã lỗi `auth.locked`.
   - **Giới hạn tần suất gọi API (Rate Limiting)**: Áp dụng thuật toán Fixed Window giới hạn **10 requests/phút/IP** trên các endpoint nhạy cảm (`register`, `login`, `refresh`, `change-password`). Khi vượt ngưỡng, hệ thống trả về HTTP `429 Too Many Requests` kèm header chuẩn `Retry-After: 60`.
   - **Nền tảng Refresh Token & Token Rotation (`POST /api/v1/auth/refresh`)**:
     - Cấp phát Refresh Token ngẫu nhiên mật mã 512-bit (`RandomNumberGenerator`), chỉ lưu bản băm SHA-256 (64 hex characters) trong CSDL, không lưu raw token.
     - **Token Rotation**: Mỗi lần gọi endpoint `/refresh`, token cũ lập tức bị thu hồi và một token mới được sinh ra, liên kết vết qua `ReplacedByTokenHash`.
     - **Phòng chống Replay Attack (Token Reuse Detection / Family Revocation)**: Nếu phát hiện một token đã bị thu hồi cố tình được sử dụng lại, hệ thống lập tức thu hồi toàn bộ các token đang hoạt động của người dùng đó (Family Revocation), ngăn chặn kẻ gian chiếm đoạt phiên.
   - **Quản lý Hồ sơ người dùng (`GET` & `PATCH /api/v1/auth/me`)**:
     - Xem thông tin cá nhân hiện tại qua Bearer token: bao gồm `id`, `fullName`, `email`, `userName`, `avatarUrl`, `roles`, `emailConfirmed`, `createdAt`.
     - Cho phép cập nhật có chọn lọc (`displayName`/`fullName`, `avatarUrl`, `bio`).
     - Áp dụng FluentValidation nghiêm ngặt: chặn ký tự điều khiển, chặn injection thẻ HTML (`<`, `>`), xác thực định dạng URL ảnh đại diện (`http://` hoặc `https://`).
     - **Bảo mật tuyệt đối**: Ngăn chặn hoàn toàn việc can thiệp thay đổi `email` hoặc tự nâng cấp `roles` qua API hồ sơ.
   - **Đăng xuất an toàn (`POST /api/v1/auth/logout`)**: Yêu cầu Bearer Token hợp lệ, thu hồi Refresh Token trong cơ sở dữ liệu và trả về `204 NoContent`.
   - **Xử lý Tác vụ nền (Background Worker - Welcome Email)**:
     - Sử dụng `System.Threading.Channels` (`UnboundedChannel`) đẩy tác vụ gửi email chào mừng vào hàng đợi phi đồng bộ sau khi giao dịch cơ sở dữ liệu commit thành công.
     - Tự động mã hóa tên hiển thị qua `WebUtility.HtmlEncode` phòng chống Email HTML Injection.
     - Tích hợp chính sách thử lại phân tầng (`0s`, `1 phút`, `5 phút`, `30 phút`).
   - **Quan sát hệ thống & Redaction sâu**:
     - Middleware tự động sinh và đính kèm `X-Correlation-ID` trên HTTP Header, đẩy W3C `TraceId` và `UserId` vào Serilog `LogContext`.
     - Tự động lọc bỏ dữ liệu nhạy cảm (secrets, tokens, passwords, EF Core sensitive logging).
   - **Bài Lab Cá nhân TV1 (K12 & K19)**:
     - `RecipeCacheService.cs` (K12): Mô hình Cache-Aside, Invalidation khi dữ liệu thay đổi, và Resilient Fallback tự động khi Cache Server gặp sự cố.
     - `RecipeJsonLd.cs` (K19): `RecipeJsonLdBuilder` sinh Schema.org `Recipe` JSON-LD chuẩn SEO Google Rich Results (thời gian chuẩn bị/nấu chuẩn ISO 8601 Duration, khẩu phần, nguyên liệu, các bước, dinh dưỡng; không fake rating).

2. **Phân hệ Danh mục Ẩm thực (TV2 - Ngô Quốc Trường Vĩ)**:
   - Thực thể `Category` với định danh GUID, hỗ trợ thứ tự sắp xếp (`OrderIndex`), ràng buộc `Name` duy nhất (UNIQUE) và `Slug` duy nhất (C09).
   - Bộ chuyển đổi `SlugHelper` chuẩn hóa tiếng Việt có dấu thành URL slug thân thiện tự động, tự động thêm suffix số nếu va chạm slug.
   - Cơ chế xóa danh mục: **Hard Delete** xóa entity khỏi DB (C07), chặn xóa và trả HTTP `409 Conflict` nếu danh mục còn công thức.
   - Bộ nhớ đệm danh mục: `IMemoryCache` với thời gian sống TTL **60 phút** (C03).
   - Hệ thống Endpoint CRUD `/api/v1/categories` được bảo vệ bằng chính sách phân quyền `AdminPolicy`, toàn bộ response bọc chuẩn `{ "data": ... }` (C08).
   - Đặc tả phân trang chuẩn với cấu trúc kết quả `PagedResult<T>` và kích thước trang mặc định `pageSize = 12` (C04).

3. **Phân hệ Khám phá, Tìm kiếm & Đăng nhập Google OAuth (TV2 - Ngô Quốc Trường Vĩ)**:
   - **Duyệt danh sách công thức công khai (`GET /api/v1/recipes`)**:
     - Lọc kết hợp (AND) đa tiêu chí: `categoryId`, `difficulty`, `maxCookTime`, `minServings`.
     - Sắp xếp linh hoạt theo allowlist an toàn: `createdAt`, `title`, `cookTimeMinutes`, `prepTimeMinutes` (chặn SQL Injection).
     - Phân trang chuẩn giao ước D11 (`data/meta`).
     - **Bảo mật**: Tuyệt đối cô lập và không bao giờ trả về công thức ở trạng thái `Draft` hoặc `Archived`.
   - **Tìm kiếm toàn văn FTS tiếng Việt không dấu (`GET /api/v1/recipes/search`)**:
     - Chuẩn hóa từ khóa tiếng Việt không dấu qua `SlugHelper` và PostgreSQL `unaccent`.
     - Cho phép gõ từ khóa không dấu như `pho` vẫn tìm thấy chính xác món `Phở Bò Gia Truyền`.
     - Kiểm soát độ dài từ khóa tối thiểu ($q \ge 2$ ký tự), từ chối và trả về HTTP 400 kèm Problem Details nếu không hợp lệ.
   - **Xác thực Đăng nhập Google OAuth2 (`POST /api/v1/auth/google`)**:
     - Xác thực IdToken từ Google thông qua thư viện chuẩn `Google.Apis.Auth` (`GoogleJsonWebSignature`).
     - Tự động tạo tài khoản với role `Author` cho người dùng mới hoặc liên kết với tài khoản đã tồn tại.
     - Cấp JWT token chuẩn `AuthResponse` tương thích toàn bộ hệ thống xác thực.

4. **Phân hệ Recipe Aggregate & Kiểm thử Tương tranh (TV3 - Huỳnh Quốc Trung)**:
   - Mô hình hóa Domain Recipe Aggregate gồm: `Recipe`, Value Object `Nutrition`, `RecipeIngredient` (chuẩn hóa thuộc tính `OrderIndex` theo C06), `RecipeStep` (chuẩn hóa thuộc tính `TimerMinutes` theo C05), `RecipeImage`.
   - Chiến lược xóa công thức: **Soft Delete** (`IsDeleted = true`), kết hợp Global Query Filter và giữ nguyên các file ảnh trên MinIO (C01).
   - Điều kiện xuất bản công thức (Publish): Bắt buộc phải có **ít nhất 1 nguyên liệu VÀ ít nhất 1 bước thực hiện** (C02); nếu thiếu dữ liệu trả về HTTP `422 Unprocessable Entity` với mã lỗi `RECIPE_PUBLISH_INCOMPLETE`.
   - Cơ chế kiểm soát tương tranh lạc quan (Optimistic Concurrency Control) dựa trên cột `RowVersion` (PostgreSQL `xmin`), ngăn chặn hoàn toàn lỗi mất cập nhật (Lost Update) khi 2 tác giả chỉnh sửa cùng lúc.
   - Quản lý bảng `RefreshTokens` trong cơ sở dữ liệu kết nối khóa ngoại `AspNetUsers`.

5. **Hạ tầng Vận hành, Giám sát & Lưu trữ (TV4 - Nguyễn Hữu Trung Sơn)**:
   - Hệ thống kiểm tra sức khỏe hệ thống (Health Checks):
     - `/health`: Báo cáo chi tiết trạng thái của toàn bộ phụ thuộc (PostgreSQL, Redis, MinIO).
     - `/health/live`: Liveness probe phục vụ container orchestrator kiểm tra tiến trình đang chạy.
     - `/health/ready`: Readiness probe kiểm tra kết nối cơ sở dữ liệu và mạng trước khi tiếp nhận traffic.
   - Hợp đồng lưu trữ file `IFileStorageService` (Stream-based) và cấu hình `MinioOptions` sẵn sàng cho việc tích hợp bucket S3.
   - Reverse proxy Nginx (`nginx/nginx.dev.conf`) điều hướng thông suốt giữa frontend và backend API.

6. **Cơ sở Dữ liệu Mẫu Hạt Giống Đạt Chuẩn Đề Bài (Tuần 2 & 3)**:
   - Triển khai lớp sinh dữ liệu mẫu `DbSeeder.cs`, tích hợp cờ lệnh CLI `--migrate` và `--seed` trong `Program.cs`.
   - Dữ liệu thực tế kiểm chứng qua PostgreSQL:
     - **25 Categories** (yêu cầu tối thiểu ≥ 20).
     - **100 Recipes** (yêu cầu tối thiểu ≥ 100).
     - **1.099 RecipeIngredients** (mỗi recipe có từ 10 đến 12 nguyên liệu cụ thể, yêu cầu tối thiểu ≥ 10).
     - **550 RecipeSteps** (mỗi recipe có từ 5 đến 6 bước chi tiết kèm thời gian `TimerMinutes`, yêu cầu tối thiểu ≥ 5).
     - 5 tài khoản tác giả và bảng dinh dưỡng 6 chỉ số đầy đủ.

---

### 4.2. Đồng bộ Chuẩn hóa Toàn Diện theo SRS v1.1.1 (Giải quyết 9 Mâu thuẫn C01–C09)

Dự án đã giải quyết triệt để 9 mâu thuẫn nội tại được phát hiện trong tài liệu gốc theo **SRS v1.1.1** (tham chiếu [Báo cáo Mâu thuẫn](docs/root/SRS_Contradictions_Report.md)):

| Mã | Vấn đề mâu thuẫn ban đầu | Quyết định chuẩn hóa SRS v1.1.1 | Hiện thực trong Source Code & Tests |
|:---:|---|---|---|
| **C01** | Xóa Recipe: Hard Delete vs Soft Delete | **Soft Delete** (`IsDeleted = true`). Giữ file MinIO. | BaseEntity Global Query Filter, Recipe soft delete |
| **C02** | Điều kiện publish: chỉ Steps vs Steps + Ingredients | **≥ 1 Ingredient VÀ ≥ 1 Step**. Thiếu trả HTTP 422. | Domain validation `RECIPE_PUBLISH_INCOMPLETE` |
| **C03** | Category cache TTL: 60 phút vs 30 phút | **60 phút** cho IMemoryCache danh mục. | `CacheService` / In-memory TTL = 60 mins |
| **C04** | Default page size: 12 vs 10 | **pageSize = 12** mặc định cho tất cả endpoints. | Query pagination DTOs, Default PageSize = 12 |
| **C05** | Tên trường bước thực hiện: TimerMinutes vs DurationMinutes | Chuẩn hóa **`TimerMinutes`**. | `RecipeStep.TimerMinutes` (Entity, DTO, DB) |
| **C06** | Tên trường nguyên liệu: OrderIndex vs SortOrder | Chuẩn hóa **`OrderIndex`**. | `RecipeIngredient.OrderIndex` (Entity, DTO, DB) |
| **C07** | Xóa Category: Hard Delete vs Soft Delete | **Hard Delete** (xóa khỏi DB, chặn 409 nếu có món). | `CategoryRepository.DeleteAsync` xóa entity |
| **C08** | Response wrapper: có nơi thiếu `data` | **Toàn bộ response thành công wrap trong `{ data }`**. | API endpoints & Frontend `api.ts` tự unwrap |
| **C09** | Category uniqueness: Name UNIQUE vs Slug suffix | **`Name` UNIQUE trong DB**; Slug suffix nếu va chạm. | PostgreSQL Index UNIQUE Name & Slug generator |
| **§8.1** | Auth API format không khớp FR chi tiết | Bổ sung `fullName`, `userName`, `emailConfirmed`, `createdAt`, `expiresAt`. | DTOs, Handlers, Database mapping, Frontend types |

---

### 4.3. Giao diện Người dùng (Frontend Next.js 15 App Router & Tailwind CSS)

1. **Khung ứng dụng & Trang công khai**:
   - Sử dụng **Next.js 15 App Router**, React 19, TypeScript và **Tailwind CSS**.
   - **Header & Navigation**: Điều hướng responsive thông minh, hỗ trợ thanh tìm kiếm nhanh, menu mobile drawer, liên kết phân hệ quản trị và xác thực.
   - **Trang chủ (`/`) & Danh mục (`/categories`)**: Trình bày danh sách phân loại món ăn bắt mắt, card hiển thị hình ảnh, tên và số lượng công thức.
   - **Trang Đăng nhập (`/auth/login`) & Đăng ký (`/auth/register`)**: Biểu mẫu xác thực hiện đại, chuyển đổi ẩn/hiện mật khẩu, tích hợp nút Đăng nhập Google (`GoogleSignInButton`), tự động lưu token và chuyển hướng.

2. **Trang Quản trị Danh mục (`/dashboard/categories`)**:
   - Dành riêng cho Admin quản lý, tạo mới, chỉnh sửa và xóa danh mục trực quan.

3. **Trang Quản lý Hồ sơ Cá nhân (`/dashboard/profile`)**:
   - Biểu mẫu trực quan tích hợp **React Hook Form** và **Zod Validation**.
   - Kiểm tra dữ liệu tức thì (Real-time Inline Validation): cảnh báo nếu để trống tên, nhập quá 100 ký tự, chứa mã script độc hại, hoặc link ảnh avatar không hợp lệ.
   - Khóa cố định các trường bảo mật: hiển thị `Email` và `Vai trò (Roles)` dưới dạng badge bảo vệ kèm thông báo hướng dẫn người dùng.
   - Xem trước trực tiếp ảnh đại diện (Avatar Preview), hỗ trợ ảnh fallback khi URL lỗi.
   - Phản hồi trạng thái lưu mượt mà qua thông báo trạng thái (Toast Alert).

4. **Trang Khám phá Công thức & Tìm kiếm (`/recipes`, `/search`)**:
   - **Trang Khám phá (`/recipes`)**: Thanh bên bộ lọc đa tiêu chí (danh mục, độ khó, thời gian nấu, khẩu phần), sắp xếp thời gian/tên, thanh điều hướng phân trang mượt mà chuẩn giao ước D11.
   - **Trang Tìm kiếm (`/search`)**: Thanh tìm kiếm lớn, hiển thị số lượng kết quả theo từ khóa, trạng thái rỗng và cảnh báo từ khóa ngắn dưới 2 ký tự.
   - **Thẻ món ăn (`RecipeCard`)**: Hiển thị badge độ khó (`Easy`, `Medium`, `Hard`, `Expert`), thời gian chuẩn bị/nấu, số khẩu phần và thông tin tác giả.

---

### 4.4. Môi trường Container hóa Đầy Đủ (Docker Dev Stack)

Toàn bộ dịch vụ phụ trợ được cấu hình tập trung trong file [`docker-compose.dev.yml`](docker-compose.dev.yml):

| Dịch vụ | Image Container | Cổng Host | Vai trò trong hệ thống |
|---|---|:---:|---|
| **PostgreSQL 16** | `postgres:16-alpine` | `5432` | Cơ sở dữ liệu quan hệ chính & test DB |
| **Redis 7** | `redis:7-alpine` | `6379` | Cache-aside, Rate Limiting & Blacklist |
| **S3 Object Storage (RustFS)** | `rustfs/rustfs` | `9000` (API) / `9001` (Console) | Lưu trữ ảnh món ăn và avatar người dùng (thay image MinIO đã bị gỡ khỏi registry) |
| **MailHog** | `mailhog/mailhog` | `1025` (SMTP) / `8025` (Web UI) | Máy chủ thử nghiệm gửi email chào mừng và thông báo |
| **Seq** | `datalust/seq:2026.1` | `5341` | Máy chủ thu thập log tập trung có cấu trúc (Structured Logging) |
| **Nginx** | `nginx:1.27-alpine` | `8080` | Reverse proxy môi trường dev (publish `8080` → cổng `80` trong container) |

---

### 4.5. Chất lượng Mã nguồn & Báo cáo Kiểm thử Tự động (Testing Suite)

Dự án duy trì bộ kiểm thử tự động toàn diện đạt tỷ lệ vượt qua **100% (321 / 321 tests pass)**:

```text
Test run for ConcurrencySpike.dll (net10.0)
Passed!  - Failed: 0, Passed:  5, Skipped: 0, Total:  5, Duration: 1 s

Test run for CulinaryBlog.Tests.dll (net10.0)
Passed!  - Failed: 0, Passed: 316, Skipped: 0, Total: 316, Duration: 28 s

Total: 321/321 tests passed (100% Green).
```

> **Đo gần nhất 05/10/2026** (sau khi merge `main` vào `2312739_NHTSon_D5-D6-D7` ở commit
> `72e4044`, đo tại `d4edfa2`): `CulinaryBlog.Tests` **316/316** + `ConcurrencySpike` **5/5** =
> **321/321**; build 0 warning / 0 error; `dotnet format --verify-no-changes` exit `0`;
> coverage `CulinaryBlog.Application` **84.13%** ≥ ngưỡng cổng G5 **80%**.
>
> **Đo trước đó 04/10/2026** (commit `8d9d62b`, xác nhận trên CI run `37213966752`):
> `CulinaryBlog.Tests` **311/311** + `ConcurrencySpike` **5/5** = **316/316**.
> Số test tăng thêm 5 vì merge `main` mang vào 5 test tuần 5 của TV1
> (`Week5_CommitVerificationTests`).
>
> **Frontend**: Playwright **26/26** (chạy 3 lần liên tiếp đều xanh), `npx tsc --noEmit` / `npm run lint` / `npm run build` đều exit `0`.
>
> **Tải (k6)**: `tests/performance/read-load.js` **3/3 lần xanh**, `http_req_failed` **0.00%**.
>
> Bằng chứng đầy đủ: [`docs/evidence/TV4/Tuan04/SO_EVIDENCE_TUAN_4.md`](docs/evidence/TV4/Tuan04/SO_EVIDENCE_TUAN_4.md).

- **Kiểm định Kiến trúc (18 Architecture Tests)**: Bảo vệ ranh giới Clean Architecture, kiểm thử toàn bộ trường hợp biên của validator (XSS, ký tự điều khiển, độ dài chuỗi, URL scheme).
- **Kiểm thử Xác thực & Bảo mật (18 Auth Tests)**: Kiểm tra luồng đăng ký/đăng nhập, chặn đăng ký email trùng, chặn client tự cấp role Admin, kiểm tra khóa tài khoản HTTP 423, cập nhật hồ sơ HTTP 200/400/401, và đăng xuất HTTP 204.
- **Kiểm thử Khám phá, Tìm kiếm & Google Auth (6 Discovery & Search Tests)**: Kiểm tra lọc công thức AND, phân trang clamping, validator, FTS không dấu ("pho" -> "Phở Bò Gia Truyền") và Google OAuth Login.
- **Kiểm thử Tuần 3: Refresh Token Rotation, Family Revocation, Cache & JSON-LD (8 Tests)**: Kiểm tra cấp phát token 512-bit, Token Rotation, Token Reuse Detection thu hồi toàn bộ token của phiên, Logout thu hồi token, Schema.org Recipe JSON-LD generator và Fallback Resilience khi Cache server down.
- **Kiểm thử Phân hệ Danh mục (12 Category Tests)**: Kiểm tra trọn vẹn nghiệp vụ Domain, thuật toán sinh slug tiếng Việt, CRUD CQRS Handlers, và phân trang.
- **Kiểm thử Tuần 4 — Resilience vận hành (N2-C)**:
  - `ApiExceptionHandlerDbUnavailableTests` — dò **cả chuỗi `InnerException`**, map `Npgsql`/socket/timeout → `503 database.unavailable`, và giữ `DbUpdateConcurrencyException` ở **422** (không báo nhầm).
  - `BackgroundJobRetryContractTests` — **4/4** hợp đồng retry: sitemap 2, resize 3, xoá ảnh 3, welcome 0/1/5/30 phút.
  - `StorageFailureContractTests` — **9/9**: storage down/wrong credential → **503**, thiếu credential → endpoint đọc vẫn **200** còn upload mới **503**.
  - `TracingObservabilityTests`, `RedisSharedCacheTests`, `SitemapLockTests`, `HealthTests` — trace HTTP→DB, cache dùng chung nhiều instance, distributed lock sitemap.
- **Kiểm thử Luồng publish (N2-B1, Playwright — 26/26)**: đăng ký → đăng nhập → **wizard 5 bước** (basic/ingredients/steps/images/review) → publish → tra cứu tìm kiếm. Có regression test bắt lỗi wizard mất bước sau lần lưu draft đầu tiên.
- **Kiểm thử Concurrency Spike (5 Tests)**: Đảm bảo kiểm soát xung đột dữ liệu đồng thời và tính toàn vẹn của transaction khi 2 writer cùng ghi hoặc cập nhật ảnh primary.
- **Định dạng mã nguồn**: `dotnet format CulinaryBlog.sln --verify-no-changes` đạt 100% không phát sinh lỗi.

> Các nhóm kiểm thử liệt kê ở trên được ghi nhận theo từng mốc Tuần 1–4, nên tổng của chúng không cộng lại bằng `316`; con số authoritative để nghiệm thu là tổng `dotnet test` ở khối trên.

---

## 💻 5. Hướng Dẫn Cài Đặt & Chạy Ứng Dụng

### 5.1. Yêu cầu môi trường
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js 20+ LTS](https://nodejs.org/)
- [Docker & Docker Compose](https://www.docker.com/)
- Hệ quản trị PostgreSQL 16 (**khuyến nghị dùng qua Docker** để đồng bộ password với cả team)

### 5.2. Khởi động hạ tầng Docker
```bash
# Khởi động toàn bộ 6 dịch vụ phụ trợ
docker compose -f docker-compose.dev.yml up -d

# Kiểm tra trạng thái các container
docker compose -f docker-compose.dev.yml ps
```

### 5.3. Khởi động Backend API (.NET 10)
```bash
# 1. Khôi phục dependencies theo locked-mode
dotnet restore CulinaryBlog.sln --locked-mode

# 2. Tạo .env cho máy này (giá trị thật, KHÔNG commit)
cp .env.example .env
#    BẮT BUỘC sinh khóa JWT rồi dán vào .env (app sẽ không khởi động nếu bỏ trống):
#      Git Bash / WSL : openssl rand -base64 48
#      PowerShell     : $b=New-Object byte[] 48; ([Security.Cryptography.RandomNumberGenerator]::Create()).GetBytes($b); [Convert]::ToBase64String($b)

# 3. Áp dụng migration cơ sở dữ liệu
dotnet run --project src/backend/CulinaryBlog.API -- --migrate

# 4. (Tùy chọn) Nạp dữ liệu mẫu 25 categories & 100 recipes
dotnet run --project src/backend/CulinaryBlog.API -- --seed

# 5. Chạy Backend API server
dotnet run --project src/backend/CulinaryBlog.API -- --urls http://localhost:5080
```
> Không cần `export` gì thêm: `EnvFileLoader` nạp `.env` tự động (bỏ qua khi `ASPNETCORE_ENVIRONMENT=Production`).
> Không có `.env` thì app dùng default trong `appsettings.Development.json` (`Password=postgres`) — khớp default của `docker-compose.dev.yml`.
>
> 🔑 **`Jwt__SigningKey` là BẮT BUỘC (QD3-3b).** Khoá ký JWT không còn nằm trong `appsettings*.json`; app **fail-fast** khi
> thiếu hoặc khoá < 64 byte (`InvalidOperationException` kèm hướng dẫn). Đổi khoá = mọi phiên đăng nhập cũ mất hiệu lực.
> Khoá dev cũ từng bị commit vào repo đã bị **thu hồi** — app từ chối dùng lại nó.
>
> 👑 Cần tài khoản Admin? `dotnet run --project src/backend/CulinaryBlog.API -- --promote-admin <email>` (chỉ Development, idempotent — xem `docs/evidence/TV4/HUONG_DAN_CHAY_TV4.md` §6.1).

> 📖 Truy cập tài liệu API trực quan tại: **http://localhost:5080/scalar/v1**

### 5.4. Khởi động Frontend (Next.js 15)
```bash
cd src/frontend

# Cài đặt thư viện phụ thuộc
npm install

# Chạy server phát triển
npm run dev
```
> 🌐 Mở trình duyệt truy cập:
> - Trang chủ ứng dụng: **http://localhost:3000**
> - Trang Khám phá công thức: **http://localhost:3000/recipes**
> - Trang Tìm kiếm món ăn: **http://localhost:3000/search**
> - Trang Đăng nhập: **http://localhost:3000/auth/login**
> - Trang Đăng ký: **http://localhost:3000/auth/register**
> - Trang Quản lý Hồ sơ: **http://localhost:3000/dashboard/profile**
> - Trang Quản trị Danh mục: **http://localhost:3000/dashboard/categories**

### 5.5. Chạy bộ kiểm thử tự động (Automated Tests)
```bash
# Chuỗi kết nối database test đọc từ TEST_DATABASE trong .env (đã tạo ở bước 2 mục 5.3).
# Không có .env thì test tự dùng default khớp docker-compose.dev.yml (Password=postgres).

# Chạy toàn bộ test trong solution
dotnet test CulinaryBlog.sln --logger "console;verbosity=normal"
```

### 5.6. Dành cho thành viên dùng PostgreSQL native
Mô hình cấu hình: **default trong repo, giá trị thật trong `.env`**. Nếu máy đã có sẵn PostgreSQL
cài trực tiếp (hoặc đổi mật khẩu), **không sửa file cấu hình đã commit** — sửa `.env` của bạn:

```bash
# Sửa .env: trỏ API về DB native của bạn
ConnectionStrings__Database=Host=localhost;Port=5432;Database=culinary_blog;Username=postgres;Password=<MAT_KHAU_CUA_BAN>

# Sửa .env: trỏ test về DB test của bạn
TEST_DATABASE=Host=localhost;Port=5432;Database=culinary_test;Username=postgres;Password=<MAT_KHAU_CUA_BAN>
```

> `docker compose` **tự** đọc `.env`; API và test đọc qua `EnvFileLoader`. Biến môi trường đã được
> export sẵn trong shell/CI vẫn thắng `.env`.
> Đổi `POSTGRES_PASSWORD` sau khi volume đã tạo **không** có tác dụng — xem ghi chú cuối `.env.example`.

---

## 🧭 6. Kế Hoạch & Trọng Tâm Tiếp Theo (Tuần 4)

1. **TV1 (Thanh Tâm - Leader)**:
   - Viết Negative Tests chuyên sâu cho toàn bộ luồng Auth, Rate Limiting, Brute-force và Token Replay Attack.
   - Xây dựng các ca kiểm thử E2E flows (Playwright/xUnit) cho toàn bộ chu trình: Đăng ký → Đăng nhập → Refresh Token → Đăng xuất.
   - Nâng cao Code Coverage toàn hệ thống (mục tiêu ≥ 80% line coverage).
2. **TV2 (Trường Vĩ)**:
   - Thực hiện k6 Load Testing đo lường thời gian phản hồi p50, p95, p99 cho các endpoint Tìm kiếm FTS và Khám phá công thức.
   - Phân tích EXPLAIN QUERY PLAN trên PostgreSQL, đánh giá hiệu quả của GIN index và Cache-hit ratio.
   - Kiểm thử khả năng truy cập (a11y) WCAG 2.1 AA và độ tương thích giao diện trên các mốc kích thước màn hình (320px, 768px, 1200px).
3. **TV3 (Quốc Trung)**:
   - Hoàn thiện giao diện Wizard tạo và chỉnh sửa công thức đa bước trên frontend.
   - Viết các bài test kiểm thử tích hợp giao dịch phân tán, kiểm tra rollback transaction khi xảy ra lỗi ở child entities (nguyên liệu, bước làm).
4. **TV4 (Trung Sơn)** — **đã chốt Giai đoạn 1 ngày 04/10/2026**, cập nhật 05/10; Giai đoạn 3 (sửa lỗi) xong phần đã thực thi. Xem [`docs/evidence/TV4/Tuan04/BAO_CAO_GIAI_DOAN_1_N2_N4.md`](docs/evidence/TV4/Tuan04/BAO_CAO_GIAI_DOAN_1_N2_N4.md):
   - ✅ **Tuần 3 (PR #16 đã merge)**: D23 Hangfire queue persistent + dashboard Admin, D27 media proxy công khai, D2 resize 300×300/800×600 idempotent, D4 sitemap/robots/OG/JSON-LD, D5 EXPLAIN + k6, D6 Lab L4 (`practice/TV4/L4`, 4 phase 39/39 check).
   - ✅ **B1/B2 (chặn trước tuần 4)**: `503 storage.unavailable` cho storage down/sai credential; fail-fast lúc khởi động khi thiếu cấu hình; `503 database.unavailable` khi DB chết; gỡ secret khỏi `render.yaml` + secret scan trong CI. **216/216** test xanh.
   - ✅ **N2 (tuần 4)**: resilience N2-C1/C1b/C1c/C2/C3/C4/C6 (outage drill Redis/S3/DB/worker, retry 2/3/3, sitemap 02:00 UTC + distributed lock, k6 `read-load.js` 3 lần xanh), N2-B1 (Playwright **26/26**, 3 lần liên tiếp), N2-B2/B3/B4 (file attack, MIME spoofing, quyền upload), N2-E (retry/race/security).
   - ✅ **Ba lỗi thật đã tìm và sửa kèm test hồi quy**: (1) DB chết trả `500` vì `ApiExceptionHandler` không dò `InnerException`; (2) DB chết lúc khởi động giết tiến trình vì lịch sitemap ném ra khỏi `Main`; (3) `GET /recipes/{slug}` trả `500` khi thiếu credential object storage vì `MinioClient.Build()` ném ngay trong constructor — **lỗi này do CI bắt, không phải do test local**, và đã khoá bằng 2 test hồi quy.
   - ✅ **N3-B/C1/C2 (05/10)**: Lab L5 7 phase chạy thật (63 check, **3/7 phase PASS** — `seo` 15/15, `observability` 10/10, `multi-instance` 7/7), Sổ K + 2 log, PR #28 đã mở rồi đóng theo quyết định nhóm. **4 phase lộ ra vấn đề thật**: ISR không hoạt động, ảnh không tối ưu 2 tầng, search trả `no-store`, RowVersion chưa kiểm chứng được.
   - 🔒 **Kiểm chứng báo cáo "Tuần 5" của TV1** (việc cuối GĐ3) — báo cáo đánh dấu **không đáng tin**: 4 sai lệch đã xác nhận (p95 < mean, nhãn tuần, số test, `render.yaml` không tồn tại) + tuyên bố RTO/RPO không có bản ghi chạy. Phát hiện và **đã sửa lỗi khoá JWT `R3`** trong `docker-compose.staging.yml`. Xem [`KiemChung_Commit_Week5_TV1.md`](docs/evidence/TV4/Tuan04/KiemChung_Commit_Week5_TV1.md).
   - 📊 **Số đo cuối tuần 4**: coverage `Application` **84.13%** ≥ 80%, Playwright **26/26**, k6 `http_req_failed` **0.00%**, `dotnet format` exit `0`. Backend **trước** khi merge `main`: **316/316** (311 + 5) tại `8d9d62b`, CI run `37213966752` xanh. **Sau** merge `main` (05/10): **321/321** (316 + 5), build 0 warning/0 error.
   - ⏭ **Còn lại**: N2-D3 (checklist WCAG/responsive — thuộc TV2), 3 luồng E2E còn lại (`register/login` TV1, `category` TV2, `create-recipe` TV3), runbook đầy đủ + deploy staging + TLS/HSTS (N4-A/N4-B), Zod/RHF (N3-A3), và Google OAuth2-PKCE (cần credentials).
    - ✅ **CI xanh trở lại (05/10)** tại `8a585ef`: `Backend week 1` run `37320431750` ✅ + `Frontend CI` run `37320431432` ✅. Sửa 2 nguyên nhân khiến backend đỏ 4 lần liên tiếp: (1) `.md` của TV4 chứa khoá JWT đã thu hồi (`JwtSigningKeyNotCommittedTests`); (2) `ApiFactory.EnsureMigrated()` chỉ `Migrate()` không seed, nên `E2E_Scenario_5` của TV1 không có dữ liệu; (3) race trong `TracingObservabilityTests` đọc span trước khi `ActivityStopped` gọi.

---

## 📚 7. Danh Mục Tài Liệu Kỹ Thuật Tham Chiếu

### 7.1. Bảng Quyết Định Kiến trúc & ADR (đọc tóm tắt ở đây, chi tiết ở link)

| Mã | Vấn đề / Quyết định | Chọn gì | Áp dụng ở đâu | Tài liệu chi tiết |
|---|---|---|---|---|
| **ADR-TV4-002** | ⚠️ **Image MinIO đã bị gỡ khỏi registry** (quay.io 401, Docker Hub 404) → CI đỏ 5 run, mọi test bị skip, dev stack không dựng được | **RustFS** (S3-compatible, Apache-2.0, ghim tag + digest) cho dev + CI; `MinioStorageService` **không đổi dòng nào** | `docker-compose.dev.yml`, `.github/workflows/backend.yml`, `.env.example` | **[ADR-TV4-002 — đổi MinIO sang RustFS](docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md)** ⚠️ *đọc trước khi chạy dev* |
| **ADR-TV4-001 / D27** | Ảnh recipe Draft/Archived có bị lộ ra public không | Bucket **private**, ảnh phục vụ qua **proxy có auth** (`/api/v1/resources/images/{key}`); Published được cache công khai | `Program.cs`, `MinioStorageService`, FE `NEXT_PUBLIC_MEDIA_URL` | [ADR-TV4-001](docs/adr/ADR-TV4-001-van-hanh-storage-logout-tuan-1.md) · [IMAGE_CONTRACT §5](docs/IMAGE_CONTRACT.md) |
| **D27 / PA-2** | Ảnh upload hiển thị trên FE bằng URL nào | **Proxy có auth** thay vì presigned URL (bucket giữ private) | `Program.cs`, `ImagesStep.tsx` | [Đề xuất giải quyết D23/D27](docs/proposal/DE_XUAT_GIAI_QUYET_D23_D27.md) |
| **D23 / PA-1** | Resize ảnh chạy nền bằng gì | **Hangfire + PostgreSQL** (queue persistent, retry, dashboard `/hangfire` chỉ Admin) — không dùng `BackgroundService` | `ResizeImageJob`, `ImageResizeQueue` | [Đề xuất giải quyết D23/D27](docs/proposal/DE_XUAT_GIAI_QUYET_D23_D27.md) |
| **D22** | Redis chết thì `/health` trả gì | `/health/live` luôn 200 · `/health/ready` cần DB + Redis → **503** khi thiếu · API vẫn fallback đọc DB | `Health.cs` | [ADR-TV4-001](docs/adr/ADR-TV4-001-van-hanh-storage-logout-tuan-1.md) |
| **D06 / D05** | Logout thu hồi token ra sao | `POST /auth/logout` cần Bearer, trả **204**, thu hồi refresh token **+ toàn bộ family** | `IdentityService.cs` | [ADR-TV4-001](docs/adr/ADR-TV4-001-van-hanh-storage-logout-tuan-1.md) |
| **C01–C09** | 9 mâu thuẫn nội tại SRS (soft/hard delete, điều kiện publish, TTL cache, pageSize, tên field, response wrapper) | Chuẩn hóa theo **SRS v1.1.1** (bảng đối chiếu ở README §4.2) | Toàn hệ thống | [Báo cáo mâu thuẫn SRS](docs/report/BAO_CAO_GIAI_QUYET_MAU_THUAN_SRS.md) |
| **ImageSharp** | Ảnh resize bằng thư viện nào | **3.1.11** — bản 4.x **bắt buộc license key thương mại** → build fail | `ResizeImageJob` | [SO_EVIDENCE tuần 3 §TV4-K14](docs/evidence/TV4/Tuan03/SO_EVIDENCE_TUAN_3.md) |

> ⚠️ **Lưu ý quan trọng cho thành viên mới**: môi trường dev/CI **không dùng MinIO nữa** (MinIO đã gỡ toàn bộ image public).
> Lệnh dựng hạ tầng: `docker compose -f docker-compose.dev.yml up -d` — service tên là **`s3`**, console ở http://localhost:9001,
> và **không còn** container `minio-init` (app tự tạo bucket). Chi tiết đầy đủ ở [ADR-TV4-002](docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md).

### 7.2. Danh mục đầy đủ

- 🚀 [**Hướng dẫn cài đặt, cấu hình & chạy chương trình chi tiết (Setup & Run Guide)**](docs/HUONG_DAN_CAI_DAT_VA_CHAY_CHUONG_TRINH.md)
- 🧪 [Hướng dẫn kiểm thử ứng dụng toàn diện (Testing Guide)](docs/HUONG_DAN_TEST_APP.md)
- 🖥️ [Hướng dẫn cài đặt & chạy ứng dụng — riêng TV4 (PowerShell, xử lý sai mật khẩu PostgreSQL)](docs/evidence/TV4/HUONG_DAN_CHAY_TV4.md)
- ⚖️ [Báo cáo Giải quyết Mâu thuẫn Nội tại SRS (C01–C09 & §8.1)](docs/report/BAO_CAO_GIAI_QUYET_MAU_THUAN_SRS.md)
- 🔀 [Báo cáo Giải quyết Xung đột Merge TV2 Tuần 2](docs/report/BAO_CAO_GIAI_QUYET_XUNG_DOT_MERGE_TV2_TUAN2.md)
- 📋 [Kế hoạch phân chia công việc 6 tuần](docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md)
- 📖 [Kế hoạch tổng thể & Giải quyết xung đột SRS](docs/KE_HOACH_DU_AN.md)
- 📄 [Tài liệu Đặc tả Yêu cầu Phần mềm chính thức (SRS v1.1.1)](docs/root/SRS_Culinary_Blog_v1.1.1.md)
- 📑 [Báo cáo Mâu thuẫn Nội tại SRS chi tiết](docs/root/SRS_Contradictions_Report.md)
- 📄 [Tài liệu Đặc tả Yêu cầu Phần mềm (Bản gốc v1.0.0)](docs/root/SRS_Culinary_Blog_v1.0.0.md)
- 📐 [Thiết kế Cơ sở Dữ liệu & ERD (Entity Relationship Diagram)](docs/ERD.md)
- 🔐 [Hợp đồng API Xác thực (Auth Contract)](docs/AUTH_CONTRACT.md)
- 🗂️ [Hợp đồng API Danh mục (Category Contract)](docs/CATEGORY_CONTRACT.md)
- 🍲 [Hợp đồng Danh sách Công thức (Recipe List Contract)](docs/RECIPE_LIST_CONTRACT.md)
- 🔍 [Hợp đồng Tìm kiếm & Khám phá (Search & Discovery Contract)](docs/SEARCH_AND_DISCOVERY_CONTRACT.md)
- 🌐 [Hợp đồng Google OAuth (Google Auth Contract)](docs/GOOGLE_AUTH_CONTRACT.md)
- 🖼️ [Hợp đồng API Tải lên & Xử lý Ảnh (Image Contract)](docs/IMAGE_CONTRACT.md)
- 🤝 [Biên bản Chuyển giao Kỹ thuật (Handoff TV4)](docs/evidence/TV4/Tuan02/HANDOFF_TV4_TUAN2_BLOCKED.md)
- 📑 **Báo cáo nghiệm thu cá nhân & Báo cáo Lab môn học**:
  - **TV1 (Nguyễn Thanh Tâm — Leader)**:
    - [Báo cáo Tổng hợp Lab 01 - 02 (Markdown)](docs/evidence/TV1/BAO_CAO_LAB_01_02.md)
    - [Báo cáo Lab 02 (Markdown)](docs/evidence/TV1/BAO_CAO_LAB_02.md) | [Báo cáo Lab 02 (Word)](docs/evidence/TV1/Lab02_2312741_NguyenThanhTam.docx)
    - [Báo cáo Lab 03 (Markdown)](docs/evidence/TV1/BAO_CAO_LAB_03.md) | [Báo cáo Lab 03 (Word)](docs/evidence/TV1/Lab03_2312741_NguyenThanhTam.docx)
    - [Báo cáo Nghiệm thu Tuần 1](docs/evidence/TV1/TUAN_1.md)
    - [Báo cáo Nghiệm thu Tuần 2](docs/evidence/TV1/TUAN_2.md)
    - [Báo cáo Nghiệm thu Tuần 3](docs/evidence/TV1/TUAN_3.md)
  - **TV2 (Ngô Quốc Trường Vĩ)**:
    - [Báo cáo Nghiệm thu Tuần 1](docs/evidence/TV2/TUAN_1.md)
    - [Báo cáo Nghiệm thu Tuần 2](docs/evidence/TV2/TUAN_2.md)
    - [Báo cáo Spike Danh mục & Auth](docs/evidence/TV2/LAB_DANH_MUC_AUTH_SPIKE.md)
  - **TV3 (Huỳnh Quốc Trung)**:
    - [Báo cáo Hoàn thiện Task C1 Tuần 1](docs/evidence/TV3/C1-HOAN-THIEN.md)
    - [Kế hoạch Thực hiện Chi tiết](docs/evidence/TV3/KE_HOACH_TV3.md)
  - **TV4 (Nguyễn Hữu Trung Sơn)**:
    - [Báo cáo Nghiệm thu Tuần 1](docs/evidence/TV4/Tuan01/TRANG_THAI_THUC_HIEN_TUAN_1.md)
    - [Báo cáo Nghiệm thu Tuần 2](docs/evidence/TV4/Tuan02/TRANG_THAI_THUC_HIEN_TUAN_2.md)

