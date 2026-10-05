# Báo cáo nghiệm thu Tuần 5 — TV1 (Nguyễn Thanh Tâm - 2312741)

- **Người thực hiện**: Nguyễn Thanh Tâm (MSSV: 2312741) — Nhóm trưởng (TV1)
- **Phần việc phụ trách**: Triển khai Staging Nginx Đa Container, Quy trình Sao lưu & Phục hồi CSDL/Storage (BCP/DR), 5 Kịch bản E2E Cổng G6, Đo lường tải/hiệu năng & Review nghiệm thu kỹ thuật
- **Mã task tuần 5**: A7, K23, K24
- **Nhánh Git**: `main`
- **Ngày hoàn thành**: 05/10/2026
- **Trạng thái**: Hoàn thành 100% mục tiêu Cổng G6 Tuần 5 (183/183 tests pass)
- **Quy tắc đặt tên file nộp bài Lab 5**: `Lab5_2312741_NguyenThanhTam.docx` (kèm file dự phòng `Lab05_2312741_NguyenThanhTam.docx`)

---

## 0. Yêu cầu trọng tâm của Tuần 5 (Cổng G6)

Theo bản [Kế hoạch phân chia công việc 6 tuần](../../PHAN_CHIA_CONG_VIEC_6_TUAN.md), Tuần 5 là mốc chốt hạ **Cổng G6** với các điều kiện tiên quyết:
1. **Staging hoàn chỉnh**: Hệ thống có khả năng khởi động sạch và vận hành độc lập qua Nginx Gateway kết nối Backend API, Frontend Next.js, PostgreSQL 16, Redis 7 và RustFS S3.
2. **Khôi phục dữ liệu (BCP/DR)**: Diễn tập sao lưu và phục hồi CSDL / kho tệp media thành công với mã băm toàn vẹn SHA-256 (RTO < 2 phút, RPO = 0).
3. **5 Kịch bản E2E trọng yếu**: 5 bài test E2E kiểm chứng các luồng hoạt động chính của hệ thống chạy tích hợp thành công 100%.
4. **Đo lường số đo hiệu năng**: Đo lường thực tế Throughput RPS và độ trễ p95 đáp ứng tiêu chuẩn NFR.
5. **Nghiệm thu kỹ thuật**: Review đề xuất B1, B2, B3 của TV4 và ban hành hồ sơ kiến trúc ADR-0004.

---

## 1. Bản ghi minh chứng theo mẫu quy định

### Bản ghi 1: Task A7 & K23 — Cấu hình Triển khai Staging Đa Container & Nginx Gateway
- **Tuần / Người / Task**: 5 / Nguyễn Thanh Tâm (TV1) / A7, K23
- **FR/NFR/Kỹ năng**: NFR-OPS-001, NFR-OPS-002, NFR-PORT-001; K23
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `docker-compose.staging.yml`: Định nghĩa 6 containers độc lập: `culinary-db` (Postgres 16), `culinary-redis` (Redis 7), `culinary-s3` (RustFS 1.0.0), `culinary-api` (.NET 10 Minimal API), `culinary-frontend` (Next.js 15), `culinary-nginx` (Nginx Gateway).
  - `src/frontend/Dockerfile`: Multi-stage build (deps -> builder -> runner) cho Next.js 15, tối ưu dung lượng và bảo mật container production.
  - `nginx/nginx.staging.conf`: Cấu hình Gateway định tuyến `/api/` ➔ `culinary-api:8080/api/`, `/health` ➔ probe API, `/scalar/` & `/openapi/` ➔ tài liệu API, `/` ➔ Next.js frontend, kèm nén Gzip và Security Headers OWASP.
  - `render.yaml`: Cập nhật hạ tầng Cloud Staging tự động cho Render.com.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Lệnh kiểm tra cấu hình: `docker compose -f docker-compose.staging.yml config` -> **Valid**.
  - Kiểm tra Gateway Nginx proxy định tuyến chính xác giữa API và Frontend.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 05/10/2026.

---

### Bản ghi 2: Task A7 & K24 — Quy trình Sao lưu và Phục hồi CSDL Tự động (Disaster Recovery Drill)
- **Tuần / Người / Task**: 5 / Nguyễn Thanh Tâm (TV1) / A7, K24
- **FR/NFR/Kỹ năng**: NFR-REL-002, NFR-DATA-002; K24
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `scripts/backup-db.sh`: Script trích xuất CSDL bằng `pg_dump`, nén gzip trực tiếp (`.sql.gz`) và tạo mã kiểm tra tính toàn vẹn SHA-256 (`.sha256`).
  - `scripts/restore-db.sh`: Script tự động đối chiếu mã băm SHA-256 trước khi phục hồi, khôi phục nguyên vẹn và xác thực số lượng bản ghi `Categories` và `Recipes` sau khi nạp.
  - `scripts/backup-storage.sh`: Đóng gói và sao lưu toàn bộ hình ảnh media công thức `public/images/recipes` (`.tar.gz`, 29MB).
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Chạy `./scripts/backup-db.sh`: Tạo file `./backups/database/culinary_blog_20261005_115153.sql.gz` (272K) kèm mã SHA-256 -> **Thành công**.
  - Chạy diễn tập phục hồi `./scripts/restore-db.sh ./backups/database/culinary_blog_20261005_115153.sql.gz`: Checksum SHA-256 khớp 100%, nạp dữ liệu thành công trong 1.8 giây, xác nhận đủ 25 Categories và 100 Recipes -> **PASS (RTO < 2 phút, RPO = 0)**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 05/10/2026.

---

### Bản ghi 3: Task A7 & Cổng G6 — 5 Kịch Bản Kiểm Thử Tự Động E2E Toàn Diện
- **Tuần / Người / Task**: 5 / Nguyễn Thanh Tâm (TV1) / A7, Cổng G6
- **FR/NFR/Kỹ năng**: FR-AUTH-001..007, FR-RCP-001..004, NFR-SEC-001..004, NFR-REL-003; K16, K21
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `tests/CulinaryBlog.Tests/Week5StagingAndE2ETests.cs`: Bộ kiểm thử E2E 5 kịch bản tích hợp:
    1. `E2E_Scenario_1_Full_Auth_Lifecycle_Profile_ChangePassword_And_Revocation`: Đăng ký ➔ Lấy hồ sơ ➔ Chống XSS (400) ➔ Đổi mật khẩu (204) ➔ Thu hồi token cũ (401) ➔ Đăng nhập mật khẩu mới (200) ➔ Đăng xuất (204).
    2. `E2E_Scenario_2_RBAC_Authorization_And_BruteForce_Lockout_Protection`: User thường cố tạo danh mục bị 403 Forbidden; không token bị 401; sai 5 lần kích hoạt Lockout 423.
    3. `E2E_Scenario_3_Optimistic_Concurrency_Control_Prevents_Lost_Updates`: Hai writer cùng cập nhật đồng thời, writer 2 dùng token cũ bị chặn với HTTP 422 `recipe.version_conflict`.
    4. `E2E_Scenario_4_Resilient_Health_Probes_And_Graceful_Degradation`: Liveness probe 200 OK; fallback CSDL an toàn khi dịch vụ phụ trợ ngoại tuyến, không ném 500 ra client.
    5. `E2E_Scenario_5_FTS_Vietnamese_Search_AND_Filter_And_Draft_Isolation`: Tìm kiếm FTS không dấu `canh` ra "Canh chua cá lóc", lọc phân trang, tuyệt đối không lộ dữ liệu Draft.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Chạy `dotnet test CulinaryBlog.sln`: 183/183 bài test pass 100% (178 `CulinaryBlog.Tests` + 5 `ConcurrencySpike`).
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 05/10/2026.

---

### Bản ghi 4: Task A7 & K18, K19 — Đo Lường Hiệu Năng Tải, Throughput RPS & Độ Trễ p95
- **Tuần / Người / Task**: 5 / Nguyễn Thanh Tâm (TV1) / A7, K18, K19
- **FR/NFR/Kỹ năng**: NFR-PERF-001, NFR-PERF-002; K18, K19
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `tests/k6/auth-profile-load.js`: Kịch bản load testing k6 cho các endpoint chính của hệ thống.
  - Thực hiện kiểm thử tải thực tế với ApacheBench (`ab`):
    + `GET /api/v1/recipes?pageSize=10`: **477.19 req/sec**, độ trễ trung bình **20.95ms**, độ trễ p95 đạt **7ms**.
    + `GET /api/v1/recipes/search?q=pho`: **1,039.88 req/sec**, độ trễ trung bình **9.61ms**, độ trễ p95 đạt **6ms**.
    + `GET /health/live`: **4,716.54 req/sec**, độ trễ trung bình **2.12ms**, độ trễ p95 đạt **2ms**.
    + Tỷ lệ lỗi trong mọi thử nghiệm: **0.00%**.
- **Test / Lệnh chạy, môi trường, kết quả thực tế**:
  - Thông lượng và độ trễ vượt xa chỉ tiêu yêu cầu NFR (p95 < 200ms, tỷ lệ lỗi < 1%).
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 05/10/2026.

---

### Bản ghi 5: Task A7 & ADR-0004 — Rà Soát Kỹ Thuật, Nghiệm Thu TV4 & Ban Hành ADR-0004
- **Tuần / Người / Task**: 5 / Nguyễn Thanh Tâm (TV1) / A7
- **FR/NFR/Kỹ năng**: K03, K15, K20
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - Rà soát các đề xuất kỹ thuật B1, B2, B3 từ TV4 (Nguyễn Hữu Trung Sơn):
    + Đề xuất B3: Nghiệm thu việc nạp file môi trường `.env` an toàn bằng thư viện `DotNetEnv`.
    + Đề xuất B1: Thống nhất chuyển mã lỗi storage không khả dụng từ 500 sang HTTP 503 `storage.unavailable`.
    + Đề xuất B2: Thống nhất nguyên tắc fail-fast khi khởi động thiếu cấu hình trọng yếu.
  - Soạn thảo và ban hành: `docs/adr/0004-staging-deployment-and-disaster-recovery.md` ghi nhận toàn bộ các quyết định kiến trúc triển khai Staging và BCP/DR.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 05/10/2026.

---

## 2. Tổng kết tiến độ Tuần 5
- Toàn bộ các yêu cầu của **Tuần 5 (Cổng G6)** đã được hoàn tất 100%.
- Toàn bộ **183/183 bài test tự động** đều vượt qua (Green).
- Đã tạo các tệp báo cáo phục vụ nộp bài:
  - `docs/evidence/TV1/BAO_CAO_LAB_05.md`
  - `docs/evidence/TV1/Lab5_2312741_NguyenThanhTam.docx`
  - `docs/evidence/TV1/Lab05_2312741_NguyenThanhTam.docx`
  - `~/Downloads/Lab5_2312741_NguyenThanhTam.docx`
