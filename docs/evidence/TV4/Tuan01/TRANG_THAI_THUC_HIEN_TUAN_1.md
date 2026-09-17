# TRẠNG THÁI THỰC HIỆN TUẦN 1 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Nhánh Git**: `feat/TV4-week1-deploy-storage`
> **Lab nhánh**: `practice/TV4/L4`
> **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Cập nhật lần cuối**: 16/09/2026

> File này ghi lại trạng thái thực hiện các task tuần 1, các điểm cần bàn luận và lý do.
> Chi tiết kế hoạch xem `KE_HOACH_TUAN_1_TV4.md`.

---

## 1. Đã hoàn thành

> Tất cả task bắt đầu ở trạng thái **Chưa làm**. Bảng này cập nhật khi có kết quả.

| Task | Nội dung | Nơi triển khai | Trạng thái |
|---|---|---|---|
| — | Chưa có task nào hoàn thành | — | Chưa làm |

---

## 2. Đang làm / Chưa thực hiện

| Task | Nội dung | Lý do chưa xong | Cần gì để xong |
|---|---|---|---|
| D5 | Mở rộng `docker-compose.dev.yml`: thêm Redis 7, MinIO, Mailhog, Seq, Nginx | Chưa bắt đầu | Bật Docker, chạy compose |
| D5 | Health endpoints: `/health`, `/health/live`, `/health/ready` | Chưa bắt đầu | Triển khai + test |
| D5 | `.env.example` đầy đủ placeholder | Chưa bắt đầu | — |
| D1 (nền) | `IFileStorageService` contract ở Application layer | Chưa bắt đầu | — |
| D1 (nền) | `MinioStorageService` + `MinioOptions` + DI | Chưa bắt đầu | Docker compose chạy được |
| D3 | `POST /auth/logout` 204/401 | Chưa bắt đầu | Auth endpoint từ TV1 đã có |
| D3 | Cập nhật `AUTH_CONTRACT.md` | Chưa bắt đầu | — |
| D6 (nền) | Tạo nhánh `practice/TV4/L4` | Chưa bắt đầu | — |
| D6 (nền) | Mở sổ evidence TV4-Kxx | Chưa bắt đầu | — |

---

## 3. Bị block (phụ thuộc người khác)

| Task | Nội dung | Block bởi | Thời điểm dự kiến gỡ |
|---|---|---|---|
| D3 | Publish/archive/delete thực sự (logic nghiệp vụ món ăn) | TV3 — merge entity Recipe (C1/C2) | Tuần 2 |
| D3 | Refresh token revoke trong logout | TV3 — C5 refresh token rotation | Tuần 2 |
| D1 | `MinioStorageService` triển khai đầy đủ + boundary tests | Tuần 2; cần xác nhận bucket policy (ADR D27) | Tuần 2 |
| D5 | CI chạy docker compose / integration test đầy đủ | Docker daemon local hoặc CI GitHub Actions | Khi Docker sẵn sàng |

---

## 4. Cần bàn luận / cần CR (SRS v1.1.1)

| # | Nội dung | Mô tả | Quyết định dự kiến |
|---|---|---|---|
| CR-1 | Bucket policy MinIO | SRS 2.4.1 nêu bucket `public-read`, kế hoạch TV4 giữ **private** (tải ảnh qua API/proxy có auth). Chưa chốt ảnh hưởng đến frontend (img tag hay fetch?) | Cần nghiệm thu với nhóm + giảng viên |
| CR-2 | Publish yêu cầu ≥1 ingredient + ≥1 step | FR-RCP-005 ghi "ít nhất 1 bước"; Phụ lục B yêu cầu "ít nhất 1 nguyên liệu và 1 bước". Tuần 1 chọn theo **Phụ lục B** (nghiêm hơn, theo C02) | Đã chốt trong SRS v1.1.1 (C02): ≥1 ingredient VÀ ≥1 step |
| CR-3 | Soft delete vs hard delete recipe | FR-RCP-007 ghi xóa cứng, NFR-REL-003 yêu cầu soft delete. Tuần 1 chọn **soft delete** | Đã chốt trong SRS v1.1.1 (C01): Soft Delete |
| CR-4 | Response format | SRS v1.1.1 (C08): Toàn bộ response thành công wrap trong `{ "data": ... }` | Đã chuẩn hóa |
| CR-5 | Category unique constraint | SRS v1.1.1 (C09): `Name` UNIQUE + `Slug` UNIQUE | Đã chuẩn hóa |

---

## 5. Ghi chú kỹ thuật cho review

1. **HealthCheck không thêm NuGet package mới** — tránh phá `restore --locked-mode` của CI:
   - DB: `AuthDbContext.Database.CanConnectAsync`
   - Redis / MinIO: TCP probe thuần (`TcpClient`) với timeout 3s vào cổng cấu hình từ `HealthChecks:Redis` / `HealthChecks:Minio`

2. **`IFileStorageService` nhận/trả `Stream`** — giữ Application layer không phụ thuộc ASP.NET Core Web.

3. **Logout tuần 1**: Bearer bắt buộc (401 nếu thiếu), body `{ refreshToken }` là tuỳ chọn (seam). Khi TV3 C5 bàn giao sẽ tích hợp revoke refresh token.

4. **Response format** theo SRS v1.1.1 (C08): `{ "data": ... }` cho mọi response thành công.

5. **`dotnet format --verify-no-changes`** có thể báo ENDOFLINE (CRLF) trên toàn repo — trạng thái có sẵn, không phát sinh từ thay đổi của TV4.

---

## 6. Cổng hoàn thành tuần 1

| Cổng | Tiêu chí | Trạng thái |
|---|---|---|
| **G0 giữa tuần** | Stack Compose lên được (Postgres + Redis + MinIO + API); `/health` 200; `.env.example` đầy đủ placeholder; Không secret | Chưa đạt |
| **G1 cuối tuần** | `IFileStorageService` merged (contract); logout 204/401 theo AUTH_CONTRACT cập nhật; CI backend/build pass; sổ skill mở, lab nền bắt đầu | Chưa đạt |
