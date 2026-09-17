# SỔ EVIDENCE TUẦN 2 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Reviewer nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Trạng thái**: Tất cả bắt đầu ở **Chưa làm**; chỉ đóng khi có code/test/demo + reviewer Tâm xác nhận.

---

## 1. Bảng trạng thái kỹ năng tuần 2

| K | Loại | Kỹ thuật con | Sẽ chứng minh ở | Tuần 2 | Trạng thái |
|---|---|---|---|---|---|
| K01 | SP | SRS/FR-NFR/ADR/API contract | ADR-TV4-001 (D17/D21/D22/D23/D27) + mapping FR | D1, D2, D3 | Chưa làm |
| K02 | SP | .NET10 Minimal APIs, REST/version, Scalar/RFC7807 | Image/status endpoint group + 422 publish | D1, D3 | Chưa làm |
| K03 | SP | Clean Architecture, interface, DI, value object | `MinioStorageService` qua interface + status domain methods | D1 | Chưa làm |
| K04 | SP+LAB | CQRS/MediatR + logging/validation/caching behaviors | Publish/unpublish handlers + LAB behavior | D3 | Chưa làm |
| K05 | SP | FluentValidation + sanitization | Validator upload MIME/size/altText + status form nền | D1, D4 | Chưa làm |
| K06 | SP | EF Core Code First, migration/config/seed, LINQ/index | RecipeImage config/index (đã có TV3) + mọi migration mới | D1 | Chưa làm |
| K07 | SP+LAB | UoW/transaction/audit/soft delete/RowVersion | Primary transaction + race test 2 writer | D1 | Chưa làm |
| K10 | SP | RBAC/ownership/policy/rate limit/secrets | Ownership publish/image; LAB VerifiedAuthor/limit | D1, D3 | Chưa làm |
| K12 | SP+LAB | Redis cache-aside, OutputCache, invalidation | Invalidation khi unpublish/xoá ảnh | D3 | Chưa làm |
| K13 | SP+LAB | MinIO upload/delete, magic bytes, MIME, GUID path | SP upload/delete đầy đủ + boundary 4 MIME + LAB L4 | D1 + D6 | Chưa làm |
| K14 | SP+LAB | Hangfire fire-and-forget/delayed/recurring/retry | SP resize job + LAB delayed/restart | D2 + D6 | Chưa làm |
| K15 | LAB | SMTP/MailKit, resize 300×300/800×600, sitemap XML | LAB L4 Mailhog + resize + XML | D6 | Chưa làm |
| K16 | SP | Next.js App Router/TS/Tailwind, SSR/ISR/CSR | Uploader UI nền (D4) | D4 | Chưa làm |
| K17 | SP | TanStack Query, optimistic rollback, next/image | Uploader progress/gallery/primary optimistic | D4 | Chưa làm |
| K18 | SP | Responsive, WCAG2.1 AA, keyboard/loading/error | Upload/gallery/status checklist | D4 | Chưa làm |
| K19 | SP | SEO metadata/OG/canonical/robots/JSON-LD | Sitemap/robots nền; JSON-LD tuần 3 | D4 | Chưa làm |
| K20 | SP | Serilog/Seq/correlation, OTEL, metrics, health | Log redacted khi MinIO lỗi + OTEL nền | D1, D3 | Chưa làm |
| K22 | SP | k6/EXPLAIN/cache hit/CWV | EXPLAIN publish query + đo upload | D3 | Chưa làm |
| K23 | SP | Docker/Compose/Nginx/volumes/backup-restore | Vận hành stack + CI pass mỗi task | D1–D4 | Chưa làm |
| K24 | SP | Git/PR/review/CI/static analysis/secret scan/docs | PR nhỏ từng task + review Tâm | Tất cả | Chưa làm |

> Các ô còn lại (K08 tái sử dụng Tuần 1; K09/K11/K21/K16...) sẽ có minh chứng bổ sung từ D2–D7/description lab sau tuần 2.

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

## 3. Evidence chi tiết tuần 2

### TV4-K01 — Phân tích SRS, ADR, API contract

```text
Evidence: TV4-K01 (FR-FILE-001/002, FR-RCP-005/006/008; ADR D17/D21/D22/D23/D27)
Tuần 2 / TV4 / D1, D2, D3
Đường dẫn: docs/adr/ADR-TV4-001-van-hanh-storage-logout-tuan-1.md, docs/IMAGE_CONTRACT.md
Nhánh/PR: 2312739_NHTSon_D1-D3-D5-D6
Test/lệnh: [chờ triển khai]
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K02 — Minimal APIs, REST/version, Scalar/RFC7807

```text
Evidence: TV4-K02 (FR-FILE-001, FR-RCP-008)
Tuần 2 / TV4 / D1, D3
Đường dẫn: src/backend/CulinaryBlog.API/Program.cs (image group, publish)
Nhánh/PR: 2312739_NHTSon_D1-D3-D5-D6
Test/lệnh: curl POST /api/v1/recipes/{id}/images → 201/400; PATCH /recipes/{id}/publish → 200/422
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K03 — Clean Architecture, interface, DI

```text
Evidence: TV4-K03 (FR-FILE-001/002)
Tuần 2 / TV4 / D1
Đường dẫn: src/backend/CulinaryBlog.Infrastructure/MinioStorageService.cs, Program.cs (DI)
Nhánh/PR: 2312739_NHTSon_D1-D3-D5-D6
Test/lệnh: dotnet build; upload qua IFileStorageService hoạt động với MinIO docker
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K07 — UoW, transaction, RowVersion (primary race)

```text
Evidence: TV4-K07 (FR-RCP-008; D19)
Tuần 2 / TV4 / D1
Đường dẫn: src/backend/CulinaryBlog.Application/... (SetPrimaryCommand), tests
Nhánh/PR: 2312739_NHTSon_D1-D3-D5-D6
Test/lệnh: xunit test 2 writer set primary đồng thời → đúng 1 primary
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K13 — MinIO upload/delete + boundary

```text
Evidence: TV4-K13 (FR-FILE-001/002)
Tuần 2 / TV4 / D1
Đường dẫn: src/backend/CulinaryBlog.Infrastructure/MinioStorageService.cs, tests
Nhánh/PR: 2312739_NHTSon_D1-D3-D5-D6
Test/lệnh: upload JPEG/PNG/WebP/AVIF ≤5MiB OK; >5MiB/fake header → 400; delete xoá object
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: MinIO down → lỗi rõ ràng + log redacted
```

### TV4-K14 — Hangfire resize job (SP phần D2)

```text
Evidence: TV4-K14 (FR-JOB-002/003; D23)
Tuần 2 / TV4 / D2
Đường dẫn: src/backend/CulinaryBlog.Infrastructure/... (ResizeJob), tests
Nhánh/PR: 2312739_NHTSon_D1-D3-D5-D6
Test/lệnh: upload → 3 kích thước sinh ra; retry idempotent; restart worker không mất job
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K23 — Docker, Compose, volumes, CI

```text
Evidence: TV4-K23 (NFR-SCALE-001/003)
Tuần 2 / TV4 / Tất cả
Đường dẫn: docker-compose.dev.yml, .github/workflows/backend.yml
Nhánh/PR: 2312739_NHTSon_D1-D3-D5-D6
Test/lệnh: CI pipeline pass từng task; docker compose ps → services running
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

### TV4-K24 — Git, PR, review, CI, docs

```text
Evidence: TV4-K24 (FR-FILE-001, FR-RCP-005/006/008)
Tuần 2 / TV4 / Tất cả
Đường dẫn: .github/workflows/, docs/adr/, docs/evidence/TV4/Tuan02/
Nhánh/PR: 2312739_NHTSon_D1-D3-D5-D6 / PR #[số]
Test/lệnh: CI pipeline pass; PR được reviewer Tâm approve
Kết quả: [chưa có]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [chờ triển khai]
```

---

## 4. Checklist cá nhân tuần 2 (chốt G3)

- [ ] `MinioStorageService` implement + test MinIO down → thất bại rõ ràng + log redacted.
- [ ] Upload API: 4 MIME hợp lệ, ≤5MiB, magic bytes, path `recipes/{recipeId}/{uuid}.{ext}`, ảnh đầu tiên primary.
- [ ] PATCH primary/altText/orderIndex (D17) + race test 2 writer → đúng 1 primary.
- [ ] DELETE image xoá object MinIO, không orphan.
- [ ] Resize original/300×300/800×600 + URLs DB + original fallback + job retry.
- [ ] Publish: thiếu ingredient/step → 422 `RECIPE_PUBLISH_INCOMPLETE`; đủ → Published; non-owner → 403.
- [ ] Unpublish: ẩn public ngay, không cache response cũ.
- [ ] Logout revoke refresh family (khi TV3 C5 bàn giao); 204 idempotent.
- [ ] `IMAGE_CONTRACT.md` cho TV3 review giữa tuần.
- [ ] UI uploader/progress/primary + status button nền (D4).
- [ ] Sitemap XML/robots nền, Published-only.
- [ ] ADR TV4-001 cập nhật (D17/D21/D23); D27 gửi xác nhận nhóm.
- [ ] Nhánh lab `practice/TV4/L4` có commit + sổ evidence K cập nhật.
- [ ] CI pass sau mỗi task; không commit secret/token/password.