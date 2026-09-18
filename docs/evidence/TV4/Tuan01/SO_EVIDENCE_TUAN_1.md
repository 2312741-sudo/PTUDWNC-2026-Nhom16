# SỔ EVIDENCE TUẦN 1 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Reviewer nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Trạng thái**: Tất cả bắt đầu ở **Chưa làm**; chỉ đóng khi có code/test/demo + reviewer Tâm xác nhận.

---

## 1. Bảng trạng thái kỹ năng tuần 1

| K | Loại | Kỹ thuật con | Sẽ chứng minh ở | Tuần 1 | Trạng thái |
|---|---|---|---|---|---|
| K01 | SP | SRS/FR-NFR/ADR/API contract | ADR-TV4-001 + mapping FR | D5 (D22/D25), D1 (D27), D3 (D06) | Chưa làm |
| K02 | SP | .NET10 Minimal APIs, REST/version, Scalar/RFC7807 | Health/storage/logout endpoints + lỗi | D5, D1, D3 | Chưa làm |
| K03 | SP | Clean Architecture, interface, DI, value object | `IFileStorageService` + status domain methods | D1 (nền) | Chưa làm |
| K04 | LAB | CQRS/MediatR + logging/validation/caching behaviors | LAB L4 tự viết behavior tối thiểu | D6 (nền) | Chưa làm |
| K06 | SP | EF Core Code First, migration/config/seed, LINQ/index | Image config/index + MinIO seed bucket | D1 (nền) | Chưa làm |
| K08 | SP | Identity/PBKDF2, JWT, refresh, logout | Logout endpoint (SP) | D3 | Chưa làm |
| K10 | SP | RBAC/ownership/policy/rate limit/secrets | Logout cần Bearer; owner policy | D3 | Chưa làm |
| K13 | SP+LAB | MinIO upload/delete, magic bytes, MIME, GUID path | SP D1 upload/delete nền + LAB L4 | D1 + D6 | Chưa làm |
| K14 | LAB | Hangfire fire-and-forget/delayed/recurring/retry | LAB L4 welcome+resize+restart | D6 (nền) | Chưa làm |
| K15 | LAB | SMTP/MailKit, resize 300×300/800×600, sitemap XML | LAB L4 Mailhog + resize + XML | D6 (nền) | Chưa làm |
| K20 | SP | Serilog/Seq/correlation, OTEL, custom metrics, health | Health probes + Serilog/OTEL nền | D5 | Chưa làm |
| K23 | SP | Docker multi-stage/Compose/Nginx/volumes/backup-restore | Compose/Nginx + CI nền | D5 | Chưa làm |
| K24 | SP | Git/PR/review/CI/static analysis/secret scan/docs | PR + review Tâm + ADR/runbook/CI | Tất cả | Chưa làm |

> Các ô còn lại (K05, K07, K09, K11, K12, K16, K17, K18, K19, K21, K22) sẽ có minh chứng từ D2–D7/description lab sau tuần 1.

---

## 2. Mẫu bản ghi evidence

```text
Evidence: TV4-Kxx (FR/NFR: ...)
Tuần / Người / Task: [tuần] / TV4 / [task]
Đường dẫn code/config: [đường dẫn cụ thể trong repo]
Nhánh / PR / commit: [nhánh] / PR #[số] (review: Nguyễn Thanh Tâm)
Test/lệnh chạy + môi trường: [lệnh cụ thể]
Kết quả thực tế (ảnh/log/coverage): [chụp log/ảnh, không có secret]
Minh chứng demo: [link ảnh/video khi có]
Reviewer + ngày xác nhận: Nguyễn Thanh Tâm / [ngày]
Lỗi còn lại / ảnh hưởng: [ghi rõ nếu có]
```

---

## 3. Evidence chi tiết tuần 1

### TV4-K01 — Phân tích SRS, ADR, API contract

```text
Evidence: TV4-K01 (FR-OBS-001, FR-FILE-001/002, FR-AUTH-005; ADR D22/D27/D06)
Tuần 1 / TV4 / D5, D1, D3
Đường dẫn: docs/adr/ADR-TV4-001-van-hanh-storage-logout-tuan-1.md
Nhánh/PR: feat/TV4-week1-deploy-storage
Test/lệnh: [chưa có — chờ triển khai]
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K02 — Minimal APIs, REST/version, Scalar/RFC7807

```text
Evidence: TV4-K02 (FR-OBS-001, FR-FILE-001, FR-AUTH-005)
Tuần 1 / TV4 / D5, D1, D3
Đường dẫn: src/backend/CulinaryBlog.API/Endpoints/ (health, storage, auth/logout)
Nhánh/PR: feat/TV4-week1-deploy-storage
Test/lệnh: curl http://localhost:8080/health → 200; curl -X POST /api/v1/auth/logout → 204/401
Kết quả: [chưa có — chờ triển khai]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K03 — Clean Architecture, interface, DI

```text
Evidence: TV4-K03 (FR-FILE-001/002)
Tuần 1 / TV4 / D1 (nền)
Đường dẫn: src/backend/CulinaryBlog.Application/Storage.cs (IFileStorageService)
Nhánh/PR: feat/TV4-week1-deploy-storage
Test/lệnh: dotnet build — interface tồn tại, không reference Infrastructure
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K06 — EF Core, migration, config, seed

```text
Evidence: TV4-K06 (FR-FILE-001)
Tuần 1 / TV4 / D1 (nền)
Đường dẫn: docker-compose.dev.yml (MinIO service), .env.example
Nhánh/PR: feat/TV4-week1-deploy-storage
Test/lệnh: docker compose up -d minio; curl http://localhost:9001 → MinIO Console
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K08 — Identity, JWT, logout

```text
Evidence: TV4-K08 (FR-AUTH-005)
Tuần 1 / TV4 / D3
Đường dẫn: src/backend/CulinaryBlog.API/Endpoints/AuthEndpoints.cs (logout)
Nhánh/PR: feat/TV4-week1-deploy-storage
Test/lệnh: curl -X POST http://localhost:8080/api/v1/auth/logout -H "Authorization: Bearer <token>" → 204
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: refresh revoke chờ TV3 C5 (tuần 2)
```

### TV4-K10 — RBAC, ownership, policy

```text
Evidence: TV4-K10 (FR-AUTH-005)
Tuần 1 / TV4 / D3
Đường dẫn: src/backend/CulinaryBlog.API/Endpoints/AuthEndpoints.cs
Nhánh/PR: feat/TV4-week1-deploy-storage
Test/lệnh: curl -X POST /api/v1/auth/logout (không Bearer) → 401; curl với token user A → 204
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K13 — MinIO upload/delete

```text
Evidence: TV4-K13 (FR-FILE-001/002)
Tuần 1 / TV4 / D1 (nền)
Đường dẫn: src/backend/CulinaryBlog.Infrastructure/MinioStorageService.cs
Nhánh/PR: feat/TV4-week1-deploy-storage
Test/lệnh: [chưa có — tuần 1 chỉ contract, upload test tuần 2]
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: boundary tests (4 MIME, ≤5MiB, magic bytes) sẽ làm tuần 2
```

### TV4-K20 — Serilog, OTEL, health probes

```text
Evidence: TV4-K20 (FR-OBS-001)
Tuần 1 / TV4 / D5
Đường dẫn: src/backend/CulinaryBlog.API/Health.cs, Program.cs
Nhánh/PR: feat/TV4-week1-deploy-storage
Test/lệnh: curl http://localhost:8080/health → 200; curl http://localhost:8080/health/live → 200; curl http://localhost:8080/health/ready → 200 (hoặc 503 khi Redis down)
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K23 — Docker, Compose, Nginx, volumes

```text
Evidence: TV4-K23 (NFR-SCALE-001/003, FR-OBS-001)
Tuần 1 / TV4 / D5
Đường dẫn: docker-compose.dev.yml, nginx/nginx.dev.conf, .env.example
Nhánh/PR: feat/TV4-week1-deploy-storage
Test/lệnh: docker compose -f docker-compose.dev.yml up -d; docker compose ps → tất cả services running
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K24 — Git, PR, review, CI, docs

```text
Evidence: TV4-K24 (FR-OBS-001, FR-FILE-001, FR-AUTH-005)
Tuần 1 / TV4 / Tất cả
Đường dẫn: .github/workflows/, docs/adr/, docs/evidence/TV4/Tuan01/
Nhánh/PR: feat/TV4-week1-deploy-storage / PR #[số]
Test/lệnh: CI pipeline pass; PR được reviewer Tâm approve
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

---

## 4. Checklist cá nhân tuần 1 (chốt G1)

- [ ] `docker-compose.dev.yml` mở rộng: Postgres + Redis + MinIO + Mailhog + Seq + Nginx chạy được.
- [ ] `.env.example` đầy đủ placeholder, không có secret.
- [ ] `/health`, `/health/live`, `/health/ready` theo D22 + test 503 khi Redis down.
- [ ] `IFileStorageService` contract ở Application layer, không phụ thuộc Infrastructure.
- [ ] `MinioStorageService` + `MinioOptions` + DI registration.
- [ ] MinIO bucket `culinary-blog` tự khởi tạo khi Compose up.
- [ ] `POST /auth/logout` 204/401 theo AUTH_CONTRACT.
- [ ] `AUTH_CONTRACT.md` cập nhật thêm logout.
- [ ] ADR-TV4-001 (D22/D27/D06/D05/D25) ghi vào sổ ADR nhóm.
- [ ] CI workflow nền (build/docker compose config).
- [ ] Nhánh lab `practice/TV4/L4` tạo ra.
- [ ] Sổ evidence TV4-Kxx mở với trạng thái cập nhật.
- [ ] PR nhỏ từng task: compose → health → storage → logout.
- [ ] Không commit secret/token/password vào repo.
