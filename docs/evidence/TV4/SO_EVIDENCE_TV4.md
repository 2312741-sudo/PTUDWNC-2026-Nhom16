# SỔ EVIDENCE KỸ NĂNG — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> Phần nghiệp vụ: Xuất bản, hình ảnh, SEO và vận hành — D1–D7.
> Reviewer nghiệm thu toàn bộ: Nguyễn Thanh Tâm (Nhóm trưởng).
> Quy ước: **SP** = đóng góp sản phẩm chung; **LAB** = nhánh `practice/TV4/Lx`, dùng stack thật, giữ minh chứng riêng.
> Trạng thái: Chưa làm · Đang làm · Chờ tích hợp · Chờ review · Hoàn thành. Mỗi ô bắt đầu **Chưa làm**; chỉ đóng khi có code/test/demo + reviewer Tâm xác nhận.
> Xem thêm [MO_TA_CONG_VIEC_TV4.md](MO_TA_CONG_VIEC_TV4.md) để nắm tổng quan công việc/flow/hạn chế/chuyển giao.

## 1. Bản đồ K01–K24 của TV4 — nơi chứng minh

| K | Loại | Kỹ thuật con | Sẽ chứng minh ở | Trạng thái |
|---|---|---|---|---|
| K01 | SP | SRS/FR-NFR/ADR/API contract | ADR-TV4-001 + mapping FR | Chưa làm |
| K02 | SP | .NET10 Minimal APIs, REST/version, Scalar/RFC7807 | Image/status endpoint group (D1/D3) | Chưa làm |
| K03 | SP | Clean Architecture, interface, DI, value object | `IFileStorageService`, status domain methods | Chưa làm |
| K04 | LAB | CQRS/MediatR + logging/validation/caching/invalidation/performance behaviors | LAB L2/L4 tự viết behavior tối thiểu | Chưa làm |
| K05 | LAB | FluentValidation/Zod/RHF | LAB recipe form RHF/Zod; media metadata form (SP D4) | Chưa làm |
| K06 | SP | EF Core Code First, migration/config/seed, LINQ/index | Image config/index + add migration (D1); LAB seed/query | Chưa làm |
| K07 | LAB | UoW/transaction/audit/soft delete/optimistic concurrency | LAB L2 RowVersion/filter/audit + primary/status transaction (SP) | Chưa làm |
| K08 | SP | Identity/PBKDF2, JWT, refresh, logout | Logout SP (N3); LAB register/login/refresh/reuse | Chưa làm |
| K09 | LAB | Google OAuth2/PKCE/Auth.js, verify/link | LAB L1 Google callback/verify/link | Chưa làm |
| K10 | SP+LAB | RBAC/ownership/policy/rate limit/secrets | Logout cần Bearer, owner policy D3; LAB VerifiedAuthor/limit | Chưa làm |
| K11 | LAB | FTS tsvector/tsquery/unaccent/pg_trgm/GIN/rank | LAB L3 recipe search + trigger/index/rank | Chưa làm |
| K12 | LAB | Redis cache-aside/OutputCache/invalidation/fallback | LAB L3 cache/output/fallback; SP status/image invalidation | Chưa làm |
| K13 | SP+LAB | MinIO upload/delete, magic bytes, MIME, GUID + path `recipes/{recipeId}/{uuid}.{ext}` | SP D1 upload/delete + boundary tests; LAB 4 MIME | Chưa làm |
| K14 | SP+LAB | Hangfire fire-and-forget/delayed/recurring/retry | SP resize/sitemap (D2/D4); LAB welcome+delayed+restart | Chưa làm |
| K15 | SP+LAB | SMTP/MailKit, resize 300×300/800×600, sitemap XML | SP resize/XML (D4); LAB Mailhog SMTP | Chưa làm |
| K16 | SP+LAB | Next.js App Router/TypeScript/Tailwind SSR/ISR/CSR | SP CSR uploader/ISR media; LAB SSR search | Chưa làm |
| K17 | SP+LAB | TanStack Query/rollback/next/image/bundle | SP image mutation + progress; LAB query/rollback/bundle | Chưa làm |
| K18 | SP | Responsive, WCAG2.1 AA, keyboard, screen reader | SP upload/gallery/status + checklist | Chưa làm |
| K19 | SP+LAB | SEO: metadata/OG/JSON-LD/canonical/sitemap/robots | SP SEO/sitemap/robots (D4); LAB redirect/metadata | Chưa làm |
| K20 | SP | Serilog/Seq/correlation, OTEL, custom metrics, health | SP health probes + OTEL nền (D5); LAB logging middleware | Chưa làm |
| K21 | SP | xUnit/API/Jest/RTL/Playwright | SP media/publish API/UI/E2E (D7) + unit | Chưa làm |
| K22 | SP+LAB | k6/p95/p99, EXPLAIN/N+1/cache hit, CWV | SP metrics/load; EXPLAIN + đo trang của mình | Chưa làm |
| K23 | SP | Docker multi-stage/Compose/Nginx/volumes/backup-restore/scaling | SP Compose/Nginx + tự deploy/restore/test 2 API (D5/D7) | Chưa làm |
| K24 | SP | Git/PR/review/CI/static analysis/secret scan/docs | PR + review Tâm + ADR/runbook/CI | Chưa làm |

> Mỗi kỹ thuật con trong ô phải được kiểm tra; thiếu một phần thì ô chưa hoàn thành (mục 5.2 PHAN_CHIA).

## 2. Mẫu bản ghi evidence (copy cho mỗi ô)

```text
Evidence: TV4-Kxx (FR/NFR: ...)
Tuần / Người / Task: [tuần] / TV4 / [task]
Đường dẫn code/config: [đường dẫn cụ thể trong repo]
Nhánh / PR / commit: [nhánh] / PR #[số] (review: Nguyễn Thanh Tâm)
Test/lệnh chạy + môi trường: [lệnh cụ thể, ví dụ docker compose up + curl /health → 200]
Kết quả thực tế (ảnh/log/coverage): [chụp log/ảnh, không có secret]
Minh chứng demo: [link ảnh/video khi có]
Reviewer + ngày xác nhận: Nguyễn Thanh Tâm / [ngày]
Lỗi còn lại / ảnh hưởng: [ghi rõ nếu có]
```

**Lưu ý khi điền evidence:**
- Mỗi ô Kxx cần có ÍT NHẤT 1 commit/PR, 1 test chạy được, 1 kết quả thực tế.
- Nếu ô có nhiều kỹ thuật con, liệt kê từng kỹ thuật đã chứng minh.
- Không ghi "hoàn thành" khi chưa có reviewer Tâm xác nhận.
- Nếu phần SP đã đáp ứng một nội dung LAB, dùng PR SP làm minh chứng.

## 3. Ví dụ điền sẵn — áp dụng cho tuần 1

```text
Evidence: TV4-K23 (NFR-SCALE-001/003, FR-OBS-001)
Tuần 1 / TV4 / D5
Đường dẫn: docker-compose.dev.yml, .env.example, Program.cs (health endpoints), .github/workflows/…
Nhánh/PR: feat/TV4-week1-deploy-storage
Test/lệnh: docker compose -f docker-compose.dev.yml up -d; curl http://localhost:8080/health
Kết quả: [chụp log các container up + curl 200]
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: [nếu có]
```

```text
Evidence: TV4-K08 (FR-AUTH-005 — Logout)
Tuần 1 / TV4 / D3 (logout part)
Đường dẫn: src/backend/CulinaryBlog.API/Endpoints/AuthEndpoints.cs, docs/AUTH_CONTRACT.md
Nhánh/PR: feat/TV4-week1-deploy-storage / PR #[số]
Test/lệnh: curl -X POST http://localhost:8080/api/v1/auth/logout -H "Authorization: Bearer <token>"
Kết quả: 204 khi token hợp lệ; 401 khi thiếu/sai token
Reviewer/ngày: Nguyễn Thanh Tâm / ___
Lỗi còn lại: refresh revoke chờ TV3 C5 (tuần 2)
```

## 4. Checklist cá nhân tuần 1 (chốt G1)

- [ ] `IFileStorageService` + DI + bucket MinIO private (D27) có PR review.
- [ ] `/health`, `/health/live`, `/health/ready` theo D22 + test 503 khi Redis down.
- [ ] `POST /auth/logout` 204/401 + AUTH_CONTRACT cập nhật.
- [ ] Compose đủ Postgres/Redis/MinIO chạy 1 lệnh; `.env.example` không secret.
- [ ] Nhánh lab `practice/TV4/L4` tạo ra; sổ K đổi trạng thái từng ô phù hợp.
- [ ] ADR-TV4-001 (D22/D27/D06/D05/D25) ghi vào sổ ADR nhóm, gửi confirm đầu tuần 2.
- [ ] Tạo nhánh `feat/TV4-week1-deploy-storage` với PR nhỏ từng mục: compose → health → logout → storage interface.
- [ ] Ghi evidence TV4-K01 (ADR), TV4-K02 (health/storage/logout endpoints), TV4-K08 (logout), TV4-K20 (health), TV4-K23 (Compose/CI).