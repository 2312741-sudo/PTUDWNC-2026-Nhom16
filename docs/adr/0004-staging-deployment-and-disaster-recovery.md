# ADR 0004: Chiến Lược Triển Khai Staging Đa Container, Sao Lưu & Phục Hồi Dữ Liệu Tự Động (BCP/DR)

- **Trạng thái**: Đã phê duyệt (Accepted)
- **Người đề xuất & Phê duyệt**: Nguyễn Thanh Tâm (TV1 - 2312741, Nhóm trưởng)
- **Ngày quyết định**: 05/10/2026 (Tuần 5)
- **Phạm vi ảnh hưởng**: Toàn bộ hệ thống Backend, Frontend, Cơ sở dữ liệu PostgreSQL, Nginx Gateway, Quy trình vận hành & sao lưu dữ liệu

---

## 1. Bối cảnh (Context)

Bước vào **Tuần 5 (Lab 5)**, theo bản [Kế hoạch phân chia công việc 6 tuần](../PHAN_CHIA_CONG_VIEC_6_TUAN.md), hệ thống CulinaryBlog cần vượt qua **Cổng G6**:
1. Độc lập triển khai trên môi trường **Staging / Multi-container** với Nginx Gateway làm reverse proxy định tuyến thống nhất.
2. Thiết lập quy trình **Sao lưu và Phục hồi sau thảm họa (Disaster Recovery & Business Continuity)** cho cả CSDL quan hệ PostgreSQL và kho lưu trữ hình ảnh/media.
3. Đáp ứng chỉ tiêu **5 Critical E2E Test Scenarios**: Đảm bảo vòng đời Auth, phân quyền RBAC, kiểm soát tranh chấp đồng thời (OCC), khả năng chịu lỗi phục hồi (Resilient Fallback) và tìm kiếm FTS tiếng Việt không dấu.
4. Rà soát, đánh giá và nghiệm thu các đề xuất kỹ thuật B1, B2, B3 từ thành viên TV4 (Nguyễn Hữu Trung Sơn) về xử lý lỗi storage, cấu hình fail-fast và nạp file môi trường `.env`.

---

## 2. Quyết định kiến trúc (Decisions)

### 2.1. Cấu hình Staging Đa Container & Nginx Gateway
- Xây dựng file cấu hình triển khai `docker-compose.staging.yml` và cấu hình Gateway `nginx/nginx.staging.conf` bao gồm 6 dịch vụ phối hợp:
  - `culinary-db`: PostgreSQL 16 Alpine với volume mount dữ liệu độc lập.
  - `culinary-redis`: Redis 7 Alpine cơ chế lưu trữ AOF (Append-Only File).
  - `culinary-s3`: RustFS 1.0.0 (chuẩn giao thức S3, Apache-2.0) lưu trữ tệp ảnh công thức.
  - `culinary-api`: .NET 10 Minimal API container hóa, cấu hình cổng nội bộ 8080.
  - `culinary-frontend`: Next.js 15 App Router container hóa nhiều tầng (multi-stage build).
  - `culinary-nginx`: Nginx Alpine định tuyến duy nhất qua cổng HTTP 80 (chuẩn bị sẵn sàng cấu hình HTTPS 443).
- **Quy tắc định tuyến Nginx**:
  - `/api/` ➔ Chuyển tiếp tới `culinary-api:8080/api/` (kèm Header `X-Correlation-ID`, `X-Forwarded-For`).
  - `/health` ➔ Chuyển tiếp tới probe kiểm tra sức khỏe của API.
  - `/scalar/` & `/openapi/` ➔ Chuyển tiếp tài liệu API tương tác.
  - `/` ➔ Chuyển tiếp tới ứng dụng web Next.js `culinary-frontend:3000`.

### 2.2. Chiến lược Sao Lưu & Khôi Phục CSDL (Disaster Recovery / BCP)
- Xây dựng 2 script tự động hóa chuẩn:
  - `scripts/backup-db.sh`: Sử dụng `pg_dump` trích xuất schema & dữ liệu, nén trực tiếp qua gzip (`.sql.gz`), đồng thời tự động tạo mã băm kiểm tra tính toàn vẹn **SHA-256** (`.sha256`).
  - `scripts/restore-db.sh`: Tự động kiểm tra đối chiếu mã SHA-256 của file nén trước khi nạp vào CSDL; thực hiện phục hồi nguyên vẹn và kiểm tra xác thực số lượng bản ghi `Categories` và `Recipes` sau phục hồi.
  - `scripts/backup-storage.sh`: Đóng gói và sao lưu toàn bộ hình ảnh media công thức (`tar.gz`).
- **Chỉ tiêu vận hành**:
  - Thời gian phục hồi mục tiêu (**RTO - Recovery Time Objective**): < 2 phút.
  - Điểm phục hồi mục tiêu (**RPO - Recovery Point Objective**): 0 (không mất mát dữ liệu đối với bản sao lưu hợp lệ).

### 2.3. Nghiệm thu đề xuất kỹ thuật từ TV4 (Trung Sơn)
- **Nghiệm thu Đề xuất B3**: Tích hợp `DotNetEnv` để nạp tự động file `.env` khi chạy local dev, chống việc commit secret lên git.
- **Nghiệm thu Đề xuất B1**: Chuẩn hóa mã lỗi khi dịch vụ S3 / Storage gặp sự cố: trả về HTTP `503 storage.unavailable` thay vì để lọt lỗi không kiểm soát `500 server.error`.
- **Nghiệm thu Đề xuất B2**: Áp dụng cơ chế fail-fast khi khởi động nếu thiếu các cấu hình thiết yếu trong môi trường Production/Staging.

---

## 3. Hệ quả & Lợi ích (Consequences)

- ✅ **Tính toàn vẹn & Độc lập**: Toàn bộ hệ thống có thể được khởi động sạch (`clean checkout`) từ kho lưu trữ và chạy hoàn chỉnh chỉ bằng một lệnh `docker compose -f docker-compose.staging.yml up -d`.
- ✅ **Bảo đảm an toàn dữ liệu**: Đã chạy thử nghiệm thực tế kịch bản Disaster Recovery Drill: backup 100 món ăn và 25 danh mục, xóa và restore thành công 100% khớp mã SHA-256.
- ✅ **Bảo đảm chất lượng Cổng G6**: Toàn bộ **183/183 bài test tự động** (bao gồm 5 bài test E2E Tuần 5 chuyên biệt) đều vượt qua 100% (Green).
- ✅ **Hiệu năng vượt trội**: Kết quả đo lường tải thực tế: API Discovery đạt **477 RPS**, Search FTS đạt **1,039 RPS** với độ trễ p95 chỉ **6 - 7ms**.
