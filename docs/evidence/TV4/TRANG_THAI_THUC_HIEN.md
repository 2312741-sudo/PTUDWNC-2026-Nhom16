# Trạng thái thực hiện — TV4 (Nguyễn Hữu Trung Sơn - 2312739)

- **Nhánh Git**: `2312739_NHTSon_Compose/Nginx/MinIO/Background_health-storage_contract-logout`
- **Người review/nghiệm thu**: TV1 Nguyễn Thanh Tâm (2312741)
- **Cập nhật lần cuối**: 16/09/2026

> File này ghi lại trạng thái thực hiện các task tuần 1 của TV4, các điểm cần bàn luận
> (cần nghiệm thu / cần CR) và lý do. Chi tiết kế hoạch xem `KE_HOACH_TUAN_1_TV4.md`.

---

## 1. Đã hoàn thành

| Task | Nội dung | Nơi triển khai | Trạng thái |
|---|---|---|---|
| D5 | Compose dev stack: PostgreSQL 16, Redis 7, MinIO (MINIO_TAG), mailhog, SEQ, nginx | `docker-compose.dev.yml`, `.env.example` | Hoàn thành (chưa khởi động được local do Docker daemon tắt) |
| D5 | Nginx dev config proxy `/api/` → host | `nginx/nginx.dev.conf` | Hoàn thành |
| D5 | Health checks: `/health`, `/health/live`, `/health/ready` | `src/backend/CulinaryBlog.API/Health.cs`, `Program.cs` | Hoàn thành (build OK) |
| D1 | Contract `IFileStorageService` + `StoredFile` (Stream-based, không phụ thuộc IFormFile) | `src/backend/CulinaryBlog.Application/Storage.cs` | Hoàn thành (contract tuần 1) |
| D1 | Config POCO `MinioOptions` + đăng ký `Configure<MinioOptions>` | `src/backend/CulinaryBlog.Infrastructure/MinioOptions.cs`, `Program.cs` | Hoàn thành |
| D3 | Logout endpoint `POST /api/v1/auth/logout` (204, có Bearer, seam cho refresh revoke) | `src/backend/CulinaryBlog.Application/Auth.cs`, `Program.cs` | Hoàn thành (build OK) |
| D6 | Soát SRS ↔ tài liệu TV4, sửa 3 file tài liệu | `KE_HOACH_TUAN_1_TV4.md`, `SO_EVIDENCE_TV4.md`, `MO_TA_CONG_VIEC_TV4.md` | Hoàn thành |
| D6 | Cập nhật `docs/AUTH_CONTRACT.md` thêm dòng logout | `docs/AUTH_CONTRACT.md` | Hoàn thành |

---

## 2. Đang làm / Chưa thực hiện

| Task | Nội dung | Lý do chưa xong | Cần gì để xong |
|---|---|---|---|
| D5 | Khởi động `docker compose up -d` và xác nhận stack khởi động, MinIO tạo bucket | Docker daemon trên máy đang tắt | Bật Docker Desktop / Docker Engine |
| D5 | Kiểm chứng health endpoints khi các service chạy (DB/Redis/MinIO) | Cần stack chạy thật | Chạy compose + gọi `/health`, `/health/ready` |
| D3 | Thực thi test logout + health (integration) | Postgres test cần chạy; Docker daemon tắt nên chưa chạy được CI trọn bộ | Bật Docker → `dotnet test` với `TEST_DATABASE` |

---

## 3. Bị block (phụ thuộc người khác)

| Task | Nội dung | Block bởi | Thời điểm dự kiến gỡ |
|---|---|---|---|
| D3 | Publish/archive/delete thực sự (logic nghiệp vụ món ăn) | TV3 — merge entity Recipe (C1/C2) | Tuần 2 (sau khi TV3 bàn giao) |
| D3 | Refresh token revoke trong logout | TV3 — C5 refresh token rotation | Tuần 2 |
| D1 | `MinIOStorageService` triển khai đầy đủ + package MinIO SDK | Chưa cần ở tuần 1; cần xác nhận bucket policy (xem mục 4) | Tuần 2 |
| D5 | CI chạy `docker compose` / integration test đầy đủ | Docker daemon local tắt | Khi bật Docker hoặc qua CI GitHub Actions |

---

## 4. Cần bàn luận / cần CR

| # | Nội dung | Mô tả | Quyết định dự kiến |
|---|---|---|---|
| CR-1 | Bucket policy MinIO | SRS mục 2.4.1 nêu bucket public-read, kế hoạch TV4 giữ private (tải ảnh qua API/proxy có auth). Chưa chốt ảnh hưởng đến việc lấy ảnh ở frontend (img tag hay fetch job?) | Cần nghiệm thu với nhóm + giảng viên |
| CR-2 | Publish yêu cầu ≥1 ingredient + ≥1 step | SRS FR-RCP-005 ghi "ít nhất 1 bước", Appendix B yêu cầu "ít nhất 1 nguyên liệu và 1 bước". Kế hoạch tuần 1 TV4 chọn theo Appendix B (chặt hơn). | Đã ghi bất đồng D07 vào kế hoạch, tuần 2 triển khai theo Appendix B |
| CR-3 | Hard delete vs soft delete recipe | FR-RCP-007 ghi xóa cứng, NFR-REL-003 yêu cầu soft delete (ẩn/khôi phục). Kế hoạch tuần 1 chọn soft delete. | Đã ghi bất đồng D08 vào kế hoạch |

---

## 5. Ghi chú kỹ thuật cho review

- `HealthCheck` không thêm NuGet package mới (tránh phá `restore --locked-mode` của CI):
  - DB: `AuthDbContext.Database.CanConnectAsync`
  - Redis / MinIO: TCP probe thuần (`TcpClient`) với timeout 3s vào cổng cấu hình từ `HealthChecks:Redis` / `HealthChecks:Minio`
- `IFileStorageService` nhận/trả `Stream` (không dùng `IFormFile`) để giữ Application layer không phụ thuộc ASP.NET Core Web.
- Logout tuần 1: Bearer bắt buộc (401 nếu thiếu), body `{ refreshToken }` là tuỳ chọn (seam). Khi TV3 C5 bàn giao sẽ tích hợp revoke refresh token và `AUTH_CONTRACT.md` đã có dòng tương ứng.
- `dotnet format --verify-no-changes` báo ENDOFLINE (CRLF) trên toàn repo — đây là trạng thái có sẵn của kho, không phát sinh từ thay đổi của TV4; build thành công 0 warning.