# MÔ TẢ CÔNG VIỆC TUẦN 1 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> Tài liệu mô tả chi tiết **công việc cần làm trong tuần 1** dựa trên SRS v1.1.1.
> Xem thêm: `KE_HOACH_DU_AN.md` (mục 8), `PHAN_CHIA_CONG_VIEC_6_TUAN.md`, `MO_TA_CONG_VIEC_TV4.md`.

---

## 1. Tổng quan tuần 1

TV4 phụ trách 4 nhóm công việc chính trong tuần 1:

| Nhóm | Task | Mô tả | Điểm |
|---|---|---|---|
| Vận hành hệ thống | D5 (nền) | Dựng môi trường chạy đầy đủ (Compose), health checks, Nginx | 12đ |
| Lưu trữ ảnh | D1 (nền) | Hợp đồng lưu ảnh (`IFileStorageService`), cấu hình MinIO | 15đ |
| Đăng xuất | D3 | Endpoint `POST /auth/logout`, cập nhật hợp đồng auth | 13đ |
| Kỹ năng cá nhân | D6 (nền) | Mở sổ evidence, bắt đầu lab L4 | 25đ |

---

## 2. Chi tiết từng nhóm công việc

### 2.1. D5 — Môi trường chạy + Health checks

**Mục tiêu**: `docker compose up -d` chạy được cả stack; health endpoints hoạt động.

#### 2.1.1. Mở rộng `docker-compose.dev.yml`

Thêm các dịch vụ sau vào file `docker-compose.dev.yml` hiện có (đang chỉ có PostgreSQL 16):

| Dịch vụ | Phiên bản | Port | Volume | Ghi chú |
|---|---|---|---|---|
| PostgreSQL 16 | `postgres:16` | 5432 | `pgdata` | Giữ nguyên |
| Redis 7 | `redis:7` | 6379 | `redisdata` | AOF enabled |
| MinIO | `minio/minio` | 9000 (API), 9001 (Console) | `miniodata` | Credentials từ env |
| Mailhog | `mailhog/mailhog` | 1025 (SMTP), 8025 (Web UI) | — | Dev email testing |
| Seq | `datalust/seq` | 5341 (ingest), 8081 (Web UI) | `seqdata` | Structured logs |
| Nginx | `nginx:alpine` | 80 (HTTP) | Config volume | Reverse proxy nền |

**Lưu ý kỹ thuật**:
- Giữ nguyên PostgreSQL + `pgdata` volume hiện tại.
- MinIO credentials đọc từ `.env` (không hardcode secret).
- Nginx config file đặt trong `nginx/nginx.dev.conf`.

#### 2.1.2. Health Endpoints (FR-OBS-001, SRS ch.8.7)

Ba endpoint health ở root:

| Endpoint | Kiểm tra | Khi nào fail |
|---|---|---|
| `GET /health` | DB + Redis + MinIO | Bất kỳ dịch vụ nào down |
| `GET /health/live` | Chỉ process alive | Luôn 200 trừ khi process chết |
| `GET /health/ready` | DB + Redis (KHÔNG gồm MinIO) | Redis hoặc DB down → 503 |

**Theo ADR D22**:
- `readiness` KHÔNG kiểm tra MinIO (MinIO down không chặn nhận traffic).
- `readiness` trả 503 khi Redis down (theo thiết kế NFR-REL-002).
- `liveness` chỉ check process, không check dependency.

**Triển khai**: Dùng `TcpClient` thuần (không thêm NuGet package mới) với timeout 3s.

#### 2.1.3. `.env.example`

File mẫu chứa tất cả biến môi trường cần thiết, KHÔNG có secret thật:

```
# PostgreSQL
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DB=culinary_blog
POSTGRES_USER=your_user_here
POSTGRES_PASSWORD=your_password_here

# Redis
REDIS_HOST=localhost
REDIS_PORT=6379

# MinIO
MINIO_ENDPOINT=localhost:9000
MINIO_ACCESS_KEY=your_minio_access_key
MINIO_SECRET_KEY=your_minio_secret_key
MINIO_BUCKET=culinary-blog
MINIO_USE_SSL=false

# Seq
SEQ_HOST=localhost
SEQ_PORT=5341
```

#### 2.1.4. CI Workflow Nền

Tạo `.github/workflows` cơ bản để build backend và validate docker compose config.

---

### 2.2. D1 (nền) — Storage Abstraction + MinIO Integration

**Mục tiêu**: Interface lưu ảnh độc lập, MinIO chạy được trong Compose, DTO sẵn sàng cho TV3.

#### 2.2.1. `IFileStorageService` (Application Layer)

```csharp
// File: src/backend/CulinaryBlog.Application/Storage.cs
public interface IFileStorageService
{
    Task<StoredFile> UploadAsync(Stream stream, string contentType, long size, string prefix, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    // ...
}

public record StoredFile(string Key, string Url, string ContentType, long Size);
```

**Quy tắc thiết kế**:
- Nhận/trả `Stream` (không dùng `IFormFile`) → giữ Application layer không phụ thuộc ASP.NET Core Web.
- Path ảnh theo FR-RCP-008: `recipes/{recipeId}/{uuid}.{ext}`.
- Ảnh đầu tiên tải lên tự động là ảnh chính (`IsPrimary = true`).

#### 2.2.2. `MinioStorageService` (Infrastructure Layer)

- Triển khai `IFileStorageService` bằng MinIO SDK.
- `MinioOptions`: endpoint, accessKey, secretKey, bucket, useSSL — đọc từ config.
- Đăng ký DI trong `Program.cs`.
- Khởi tạo bucket `culinary-blog` khi khởi động.

#### 2.2.3. Bucket Policy (ADR D27)

- **Mặc định**: Private bucket.
- Ảnh được truy cập qua API/proxy có auth hoặc presigned URL.
- **Lưu ý**: SRS 2.4.1 + FR-FILE-001 ghi `public-read` → nếu nhóm muốn giữ private phải có CR (Change Request).

#### 2.2.4. Acceptance Criteria tuần 1

- [ ] Interface `IFileStorageService` tồn tại ở Application layer.
- [ ] `MinioStorageService` trong Infrastructure, không leak vào Domain/Application.
- [ ] Config mẫu không secret.
- [ ] MinIO chạy được trong Compose.
- [ ] Test: MinIO down → upload thất bại rõ ràng + log redacted.
- [ ] DTO sẵn sàng cho TV3 review.

---

### 2.3. D3 — Logout Endpoint

**Mục tiêu**: `POST /api/v1/auth/logout` hoạt động theo AUTH_CONTRACT.

#### 2.3.1. Endpoint

```
POST /api/v1/auth/logout
Authorization: Bearer <access_token>
Body (tuỳ chọn): { "refreshToken": "..." }

Response:
- 204 No Content (hợp lệ, idempotent)
- 401 Unauthorized (thiếu/sai token)
```

**Theo SRS FR-AUTH-005**:
- Yêu cầu Bearer token (D06).
- Body `{ refreshToken }` là tuỳ chọn (tuần 1 chưa có refresh token → seam).
- Trả 204, không tạo side-effect với token khác user.
- **Tuần 2**: TV3 C5 bàn giao refresh token → gắn revoke family.

#### 2.3.2. Cập nhật AUTH_CONTRACT

Thêm dòng logout vào `docs/AUTH_CONTRACT.md`:
- Logout yêu cầu Bearer, trả 204.
- Trạng thái: "refresh chưa có → chỉ revoke access scope".

#### 2.3.3. Test Cases

| Case | Input | Expected |
|---|---|---|
| Thiếu token | Không có header | 401 |
| Token sai | `Bearer invalid_token` | 401 |
| Token hết hạn | `Bearer expired_token` | 401 |
| Happy path | Token hợp lệ | 204 |
| Idempotent | Gọi lại với token đã logout | 204 (không lỗi) |
| Cross-user | Token user A, request cho user B | 403 hoặc 401 |

---

### 2.4. D6 (nền) — Mở sổ skill + Lab nền

#### 2.4.1. Tạo nhánh lab

```
git checkout -b practice/TV4/L4
```

Bắt đầu từ skeleton G1, viết code nền cho upload/delete 4 MIME + resize 300×300/800×600.

#### 2.4.2. Mở sổ evidence TV4-Kxx

Tạo file `SO_EVIDENCE_TV4.md` với trạng thái ban đầu cho từng ô K01–K24:

| K | Trạng thái tuần 1 |
|---|---|
| K01 | Đang làm (ADR D22/D27/D06) |
| K02 | Đang làm (health/storage/logout endpoints) |
| K03 | Đang làm (IFileStorageService) |
| K04 | Chưa làm (lab L4 nền) |
| K06 | Đang làm (MinIO config/seed) |
| K08 | Đang làm (logout) |
| K10 | Đang làm (logout Bearer) |
| K13 | Đang làm (MinIO upload/delete nền) |
| K14 | Chưa làm (lab L4) |
| K15 | Chưa làm (lab L4) |
| K20 | Đang làm (health probes) |
| K23 | Đang làm (Compose/Nginx) |
| K24 | Đang làm (PR/review) |
| Còn lại | Chưa làm |

---

## 3. Thứ tự thực hiện đề xuất

```
Tuần 1 — Thứ tự làm việc:

Ngày 1–2: Mở rộng docker-compose.dev.yml (Redis/MinIO/Mailhog/Seq/Nginx)
  → Chạy `docker compose up -d` xác nhận stack hoạt động (G0)
  → Tạo .env.example

Ngày 2–3: Health endpoints
  → Triển khai /health, /health/live, /health/ready
  → Test: tất cả 200; readiness 503 khi Redis down

Ngày 3–4: Storage abstraction
  → IFileStorageService (Application layer)
  → MinioStorageService (Infrastructure)
  → MinIO config + DI registration
  → Test: MinIO down → thất bại rõ ràng

Ngày 4–5: Logout endpoint
  → POST /api/v1/auth/logout (204/401)
  → Cập nhật AUTH_CONTRACT.md
  → Test cases

Ngày 5: CI + Lab
  → Tạo .github/workflows nền
  → Tạo nhánh practice/TV4/L4
  → Mở sổ evidence TV4-Kxx
  → Push PR nhỏ từng task
```

---

## 4. Lưu ý kỹ thuật cho reviewer

1. **Health Check không thêm NuGet package mới** — dùng `TcpClient` thuần với timeout 3s, tránh phá `restore --locked-mode` của CI.
2. **`IFileStorageService` nhận/trả `Stream`** — giữ Application layer không phụ thuộc ASP.NET Core Web.
3. **Logout tuần 1**: Bearer bắt buộc (401 nếu thiếu), body `{ refreshToken }` là tuỳ chọn (seam). Khi TV3 C5 bàn giao sẽ tích hợp revoke refresh token.
4. **`dotnet format --verify-no-changes`** có thể báo ENDOFLINE (CRLF) trên toàn repo — đây là trạng thái có sẵn của kho.
5. **Response format** theo SRS v1.1.1 (C08): Toàn bộ response thành công wrap trong `{ "data": ... }`.
