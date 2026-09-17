# TRẠNG THÁI THỰC HIỆN TUẦN 1 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Nhánh Git**: `2312739_NHTSon_D1-D3-D5-D6` (tv4/week1)
> **Lab nhánh**: `practice/TV4/L4`
> **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Cập nhật lần cuối**: 17/09/2026

> File này ghi lại trạng thái thực hiện các task tuần 1, các điểm cần bàn luận và lý do.
> Chi tiết kế hoạch xem `KE_HOACH_TUAN_1_TV4.md`.

---

## 1. Đã hoàn thành

| Task | Nội dung | Nơi triển khai | Trạng thái |
|---|---|---|---|
| D5 | Mở rộng `docker-compose.dev.yml`: Postgres 16 + Redis 7 + MinIO + Mailhog + Seq + Nginx dev, volume, healthcheck, `minio-init` tạo bucket `culinary-blog` private | `docker-compose.dev.yml` | ✅ Đã commit + `docker compose up -d` chạy cả stack trên máy local (Postgres healthy, MinIO healthy, bucket private) |
| D5 | Health endpoints: `/health` (DB+Redis+MinIO), `/health/live`, `/health/ready` (chỉ DB+Redis, không MinIO) | `src/backend/CulinaryBlog.API/Program.cs`, `Health.cs` | ✅ Đã test trong `HealthTests` + CI pass |
| D5 | `.env.example` đầy đủ placeholder, Không secrect thật (JWT vẫn là `REPLACE_WITH_RANDOM_SECRET`) | `.env.example` | ✅ |
| D5 | CI workflow nền cho build + test + format | `.github/workflows/backend.yml` | ✅ CI **pass** trên GitHub Actions (run id 35232056398, .NET 10.0.x) |
| D1 (nền) | `IFileStorageService` contract (`UploadAsync/DeleteAsync`) + `StoredFile` DTO ở Application | `src/backend/CulinaryBlog.Application/Storage.cs` | ✅ |
| D1 (nền) | `MinioOptions` (endpoint/accessKey/secretKey/bucket/useSSL) + đăng ký DI | `src/backend/CulinaryBlog.Infrastructure/MinioOptions.cs`, `Program.cs` | ✅ |
| D1 (nền) | Khởi tạo bucket `culinary-blog` khi compose up + policy private | `docker-compose.dev.yml` (service `minio-init`) | ✅ bucket đã tạo thành công, set private |
| D3 | `POST /api/v1/auth/logout` — Bearer bắt buộc, 204 idempotent, body `{ refreshToken }` tuỳ chọn (seam) | `src/backend/CulinaryBlog.API/Program.cs` | ✅ |
| D3 | Cập nhật `docs/AUTH_CONTRACT.md` theo SRS v1.1.1 (logout 204, Bearer) | `docs/AUTH_CONTRACT.md` | ✅ |
| D3 | Test: 401 thiếu token; 204 hợp lệ; validator refresh token | `tests/CulinaryBlog.Tests/AuthTests.cs`, `ArchitectureTests.cs` | ✅ CI pass |
| D4/D6 | PostgreSQL password thống nhất `admin123` cho môi trường dev (compose, appsettings.Development, test defaults) | nhiều file | ✅ |

---

## 2. Chưa thực hiện (đẩy sang tuần 2 theo kế hoạch)

| Task | Nội dung | Lý do chưa bắt đầu | Cần gì để xong |
|---|---|---|---|
| D1 | `MinioStorageService` triển khai đầy đủ (MinIO SDK) + boundary tests | Tuần 2 theo kế hoạch; bucket policy D27 cần xác nhận | Giao diện đã có, tuần 2 implement + test integration |
| D3 | Publish/unpublish/archive/delete thực sự | Cần Recipe entity từ TV3 (C1/C2) | Merge entity Recipe |
| D3 | Refresh token revoke trong logout | TV3 — C5 refresh token rotation | Tuần 2 |
| D6 | Tạo nhánh lab `practice/TV4/L4` + commit nền | Chưa tới thời điểm; làm khi bắt đầu Tuần 2 | G1 đóng |

---

## 3. Bị block (phụ thuộc người khác)

| Task | Nội dung | Block bởi | Thời điểm dự kiến gỡ |
|---|---|---|---|
| D3 | Publish/archive/delete thực sự (logic nghiệp vụ món ăn) | TV3 — merge entity Recipe (C1/C2) | Tuần 2 |
| D3 | Refresh token revoke trong logout | TV3 — C5 refresh token rotation | Tuần 2 |
| D1 | `MinioStorageService` triển khai đầy đủ + boundary tests | Tuần 2; cần xác nhận bucket policy (ADR D27) | Tuần 2 |
| D6 | Lab `practice/TV4/L4` | G1 chưa đóng (kéo theo decide bucket policy) | Tuần 2 |

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

5. **`.env.example` vẫn giữ JWT placeholder**; password Postgres dev mặc định `admin123` (đồng bộ với compose), không bị commit secret.

6. **Xác minh thực tế** (17/09/2026):
   - CI GitHub Actions run `35232056398` → **success** (restore/build/format/test trên .NET 10.0.x).
   - `docker compose -f docker-compose.dev.yml up -d` → toàn bộ 7 container chạy; Postgres healthy; MinIO healthy; `minio-init` tạo bucket `culinary-blog` private (Exited 0).
   - Push nhánh `2312739_NHTSon_D1-D3-D5-D6` lên origin → auto-trigger CI.

7. **Ghi chú môi trường**: máy local chỉ có .NET 9 SDK, không build được target `net10.0` — dựa vào CI (có .NET 10) để xác minh build/test. Nếu cần xác minh thủ công thì hạ target hoặc dùng máy khác có .NET 10.

---

## 6. Cổng hoàn thành tuần 1

| Cổng | Tiêu chí | Trạng thái |
|---|---|---|
| **G0 giữa tuần** | Stack Compose lên được (Postgres + Redis + MinIO + API); `/health` 200; `.env.example` đầy đủ placeholder; Không secret | ✅ Đạt (stack chạy; health behavior được test trong CI; `.env.example` placeholder) |
| **G1 cuối tuần** | `IFileStorageService` merged (contract); logout 204/401 theo AUTH_CONTRACT cập nhật; CI backend/build pass; sổ skill mở, lab nền bắt đầu | ✅ Đạt (contract + logout + CI pass; lab nền mở vào đầu Tuần 2) |
