# SỔ EVIDENCE TUẦN 3 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Reviewer nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Trạng thái**: Tất cả bắt đầu ở **Chưa làm**; chỉ đóng khi có code/test/demo + reviewer Tâm xác nhận.

---

## 1. Bảng trạng thái kỹ năng tuần 3

| K | Loại | Kỹ thuật con | Sẽ chứng minh ở | Tuần 3 | Trạng thái |
|---|---|---|---|---|---|
| K01 | SP | SRS/FR-NFR/ADR/API contract | ADR-TV4-001 (D08/D17/D21/D22/D23/D26/D27) + mapping FR | N0, D3, D4, D5 | Chưa làm |
| K02 | SP | .NET10 Minimal APIs, REST/version, Scalar/RFC7807 | Archive/delete endpoints + 422/403 | D3 | Chưa làm |
| K03 | SP | Clean Architecture, interface, DI, value object | Presigned/proxy qua `IFileStorageService` extension + domain methods | D3, D4 | Chưa làm |
| K04 | SP+LAB | CQRS/MediatR + logging/validation/caching behaviors | Archive/delete CQRS handlers + LAB behavior | D3 + D6 | Chưa làm |
| K05 | SP | FluentValidation + sanitization | Validator transition trạng thái + sanitize UI input | D3, D4 | Chưa làm |
| K06 | SP | EF Core Code First, migration/config/seed, LINQ/index | Fix migration `RefreshTokens` index + migration soft-delete | N0, D3 | Chưa làm |
| K07 | SP+LAB | UoW/transaction/audit/soft delete/RowVersion | Race đổi trạng thái + soft delete | D3 | Chưa làm |
| K10 | SP | RBAC/ownership/policy/rate limit/secrets | Ownership archive/delete + 403 non-owner | D3 | Chưa làm |
| K12 | SP+LAB | Redis cache-aside, OutputCache, invalidation | Invalidation archive/unpublish/delete + LAB | D3 + D6 | Chưa làm |
| K13 | SP+LAB | MinIO upload/delete, magic bytes, MIME, GUID path | E2E D1.3 trên MinIO + 4 MIME + SP upload/delete | D3 + D6 | Chưa làm |
| K14 | SP+LAB | Hangfire fire-and-forget/delayed/recurring/retry | SP resize job + LAB delayed/restart | D2 + D6 | Chưa làm |
| K15 | LAB | SMTP/MailKit, resize 300×300/800×600, sitemap XML | LAB L4 Mailhog + resize + XML | D6 | Chưa làm |
| K16 | SP | Next.js App Router/TS/Tailwind, SSR/ISR/CSR | Uploader UI hoàn thiện (D4) | D4 | Chưa làm |
| K17 | SP | TanStack Query, optimistic rollback, next/image | Uploader progress/gallery/primary optimistic | D4 | Chưa làm |
| K18 | SP | Responsive, WCAG2.1 AA, keyboard/loading/error | Upload/status checklist | D4 | Chưa làm |
| K19 | SP | SEO metadata/OG/canonical/robots/JSON-LD | Sitemap/robots/OG/JSON-LD Published-only | D4 | Chưa làm |
| K20 | SP | Serilog/Seq/correlation, OTEL, metrics, health | OTEL trace HTTP→DB + health thành phần | D5 | Chưa làm |
| K22 | SP | k6/EXPLAIN/cache hit/CWV | EXPLAIN publish query + k6 + cache hit | D5 | Chưa làm |
| K23 | SP | Docker/Compose/Nginx/volumes/backup-restore | CI xanh + stack vận hành + queue service | N0, D2 | Chưa làm |
| K24 | SP | Git/PR/review/CI/static analysis/secret scan/docs | PR nhỏ từng task + review Tâm + note PR #14 | N0, Tất cả | Chưa làm |

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

## 3. Evidence chi tiết tuần 3

> Các mục được bổ sung khi có kết quả triển khai (kèm lệnh chạy + bằng chứng thật). Không điền trước nội dung chưa có.

### TV4-K06 (N0 — fix duplicate migration `RefreshTokens` + CI xanh)

```text
Evidence: TV4-K06 (kỹ thuật migration; FR-AUTH liên quan bảng RefreshTokens)
Tuần 3 / TV4 / N0
Đường dẫn: src/backend/CulinaryBlog.Infrastructure/Migrations/20260919061954_AddRecipeAggregate.cs,
          20260923104044_AddRefreshTokens.cs
Nhánh/PR: [chưa có]
Test/lệnh: dotnet build CulinaryBlog.sln --no-restore --configuration Release; dotnet format; dotnet test CulinaryBlog.sln
Kết quả: [chờ triển khai — mục tiêu: Migrate() trên DB mới không lỗi, 16 test Auth/Week3 pass trên CI]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [ghi sau khi chạy]
```

### TV4-K02/K10 (D3 — Archive/delete + ownership)

```text
Evidence: TV4-K02/K10 (FR-RCP-006/007; D08)
Tuần 3 / TV4 / D3
Đường dẫn: src/backend/CulinaryBlog.Application/Recipes.cs (archive/delete region), tests/RecipeLifecycleTests.cs
Nhánh/PR: [chưa có]
Test/lệnh: dotnet test CulinaryBlog.Tests --filter ... ; curl PATCH /recipes/{id}/archive
Kết quả: [chờ triển khai]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K13 (D3.4 — E2E D1.3 trên MinIO + MinIO down)

```text
Evidence: TV4-K13 (FR-FILE-001/002; D1.1c)
Tuần 3 / TV4 / D3
Đường dẫn: src/backend/CulinaryBlog.Infrastructure/MinioStorageService.cs, Program.cs image endpoints
Nhánh/PR: [chưa có]
Test/lệnh: docker compose up (minio); curl upload/PATCH/DELETE; tắt MinIO → lỗi rõ ràng + log redacted
Kết quả: [chờ triển khai]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K16/K17/K19 (D4 — Uploader UI + status + SEO)

```text
Evidence: TV4-K16/K17/K19 (FR-RCP-008; FR-SEO; D26/D27)
Tuần 3 / TV4 / D4
Đường dẫn: frontend Next.js (uploader, dashboard status, sitemap/robots/OG/JSON-LD)
Nhánh/PR: [chưa có]
Test/lệnh: build FE + truy cập UI; curl /sitemap.xml (chỉ Published); test JSON-LD
Kết quả: [chờ triển khai]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K20/K22 (D5 — OTEL/metrics/health/k6)

```text
Evidence: TV4-K20/K22 (FR-OBS-001/003; D20/D21/D22)
Tuần 3 / TV4 / D5
Đường dẫn: Program.cs (OTEL/metrics/health), docs/huong-dan, k6 script
Nhánh/PR: [chưa có]
Test/lệnh: chạy stack → trace HTTP→DB; /health khi Redis down; k6/EXPLAIN
Kết quả: [chờ triển khai]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

---

## 4. Checklist cá nhân tuần 3 (chốt G5)

- [ ] Fix duplicate migration `RefreshTokens` → `dotnet test CulinaryBlog.sln` pass (120 + 5 spike) → CI xanh.
- [ ] Rà soát diff PR #14 đã merge (giữ nguyên theo quyết định nhóm).
- [ ] Archive `PATCH /recipes/{id}/archive` + ẩn public ngay + ownership test.
- [ ] DELETE soft theo ADR D08 + không lộ search/cache + không mất ảnh cần restore.
- [ ] Invalidation cache archive/unpublish/delete (phối hợp TV2/TV3).
- [ ] E2E D1.3 trên MinIO: upload/PATCH/DELETE thật; MinIO down → log redacted; không commit secret.
- [ ] Resize original/300×300/800×600 + queue persistent (tuỳ D23) + original fallback + restart/retry test.
- [ ] Uploader UI progress/rollback/gallery/primary + ảnh hiển thị qua presigned/proxy (D27).
- [ ] Status buttons Publish/Unpublish/Archive ghép TV3 C4.
- [ ] Sitemap XML Published-only + robots + canonical + JSON-LD; cron 02:00 UTC.
- [ ] OTEL trace HTTP→DB; health thành phần đúng D22; EXPLAIN/k6 có số liệu.
- [ ] Logout revoke refresh family khi TV3 C5 có; 204 idempotent.
- [ ] Lab `practice/TV4/L4` commit + sổ evidence K cập nhật.
- [ ] CI pass sau mỗi task; không commit secret/token/password.