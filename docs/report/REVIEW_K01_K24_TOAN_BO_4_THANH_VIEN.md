# Review K01–K24 Toàn Bộ — Nhóm 16 Culinary Blog

> **Reviewer**: Nguyễn Thanh Tâm (TV1 — Nhóm trưởng)  
> **Ngày review**: 21/10/2026  
> **Căn cứ**: `docs/KE_HOACH_DU_AN.md` §9, `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md` §5–6, sổ evidence TV1–TV4, HANDOFF & báo cáo lab tuần 1–6.  
> **Trạng thái nghiệm thu cuối kỳ (Cổng G7)**: ✅ 4/4 thành viên đạt 24/24 K-skills — **Release v1.0.0, 14/10/2026**.

---

## Chú giải

| Ký hiệu | Nghĩa |
|:---:|---|
| ✅ | Đủ bằng chứng, code/test chạy thật, reviewer xác nhận |
| 🟡 | Có nền nhưng thiếu một kỹ thuật con (đã bổ sung trước G7) |
| 🔴 | Gap thật đã ghi nhận, cần bổ sung |
| ⬜ | Chưa làm (trong quá trình) |
| **SP** | Đóng góp vào sản phẩm chung |
| **LAB** | Bài thực hành trên nhánh `practice/TVn/...` |

---

## TV1 — Nguyễn Thanh Tâm (2312741) — Tài khoản & Hồ sơ

**Phần chính**: Auth / Identity / JWT / Profile / Logging / CI  
**Coverage Application**: 96.17%  
**Tests**: 446/446 backend pass, 86/86 Jest pass (G7)

| K | Kỹ năng | Yêu cầu TV1 | Trạng thái | Minh chứng chính |
|:---:|---|---|:---:|---|
| K01 | Phân tích SRS/FR/NFR/ADR/contract | SP ADR auth + test mapping | ✅ | `docs/adr/ADR-0004-*`, mapping FR-AUTH-* trong báo cáo tuần 1–6 |
| K02 | .NET10 Minimal APIs, REST, Scalar, RFC7807 | SP Auth group + lỗi | ✅ | `Program.cs` nhóm `/api/v1/auth/*`, Problem Details, Scalar tại `/scalar` |
| K03 | Clean Architecture, entity/value object, DI | SP Identity adapter/ICurrentUser + LAB domain value object | ✅ | `ICurrentUser`, `IdentityService`, LAB value object trên nhánh cá nhân |
| K04 | CQRS/MediatR + behaviors | SP auth handlers; LAB cache query + invalidation | ✅ | `LoginCommand`, `RegisterCommand`, LAB behavior cache+invalidation |
| K05 | FluentValidation + Zod/RHF | SP register/profile forms và validators | ✅ | `RegisterValidator`, `ProfileValidator`, form RHF `/auth/register`, `/auth/login` |
| K06 | EF Core/PG16, migration, seed, LINQ/index | SP User/Token migration; LAB Recipe query/index | ✅ | `UserConfiguration`, `RefreshTokenConfiguration`, migrations, LAB query/index |
| K07 | UoW/transaction/audit/soft delete/concurrency | LAB cập nhật Recipe 2 writer + UoW/audit/filter | ✅ | LAB 2-writer scenario, audit interceptor; BCP/DR drill |
| K08 | Identity/PBKDF2, JWT, refresh hash/rotation/logout | SP register/login; LAB rotation/reuse/logout | ✅ | PBKDF2 HMAC-SHA512, JWT HS512 15p, Refresh 512-bit/7 ngày, token revocation |
| K09 | Google OAuth2/PKCE, Auth.js, ID token verify | LAB Google callback/verify/link | ✅ | LAB Google callback trên app test (credentials có sẵn) |
| K10 | RBAC/policy/rate limit/secrets/HTTPS/CORS | SP policy + limit; LAB owner/VerifiedAuthor | ✅ | `AuthorPolicy`, rate limiter `/api/v1/auth`, XSS sanitizer, secret scan |
| K11 | FTS tsvector/tsquery/unaccent/GIN/ts_rank | LAB recipe search + trigger/index/rank/AND/page | ✅ | LAB FTS trên nhánh cá nhân, EXPLAIN GIN |
| K12 | Redis cache-aside, OutputCache, invalidation, fallback | LAB public recipe cache + output policy + invalidation + Redis down | ✅ | LAB Redis cache-aside, fallback khi Redis down |
| K13 | MinIO/S3 upload/delete, MIME/magic bytes | LAB upload/delete recipe image 4 MIME + boundary | ✅ | LAB 4 MIME (JPEG/PNG/WebP/AVIF), boundary test |
| K14 | Hangfire fire-and-forget/delayed/recurring | SP welcome; LAB resize+sitemap+delayed+restart | ✅ | `WelcomeEmailJob`, retry 1'/5'/30'; LAB resize+sitemap |
| K15 | SMTP/MailKit, resize 300×300/800×600, sitemap XML | SP SMTP; LAB resize/sitemap thật | ✅ | Mailhog integration, resize job, sitemap XML |
| K16 | Next.js App Router/TypeScript, SSR/ISR/CSR | SP CSR auth; LAB SSR search + ISR public | ✅ | `/auth/*` CSR, LAB SSR search, ISR public detail |
| K17 | TanStack Query, optimistic rollback, next/image, bundle | SP profile mutation; LAB image/list query/rollback | ✅ | profile `useMutation`, LAB query/rollback/bundle |
| K18 | Responsive 320/768/1200px, WCAG2.1 AA, screen reader | SP login/profile + VoiceOver/NVDA evidence | ✅ | Checklist 3 breakpoints, VoiceOver test, jest-axe 0 vi phạm |
| K19 | SEO: metadata/OG/canonical/301/robots/JSON-LD | LAB recipe page đủ SEO + sitemap/redirect/noindex | ✅ | LAB đủ metadata/OG/JSON-LD, 301 redirect, robots.txt |
| K20 | Serilog/Seq/correlation, OTEL, health probes | SP logs; LAB OTEL/metrics/health thật | ✅ | CorrelationId/redaction Serilog, OTEL trace HTTP→DB, `/health/*` |
| K21 | xUnit ≥80%, API happy+error, Jest/RTL, Playwright | SP auth + LAB recipe API/UI/E2E | ✅ | 441 backend tests (auth focus), 86 Jest, Playwright E2E register/login |
| K22 | k6/p95/p99, EXPLAIN/N+1/cache hit, CWV/Lighthouse | LAB tải/search query + đo trang của mình | ✅ | k6 smoke/load, EXPLAIN auth query, Lighthouse login/profile |
| K23 | Docker/Compose/Nginx/volumes/backup-restore/scaling | SP CI build; tự deploy/restore/test 2 API | ✅ | BCP/DR `restore-db.sh` SHA-256, 1.8s RTO, 6-container staging Nginx |
| K24 | Git/PR/review/CI/static/architecture/secret scan/docs | PR cá nhân + review TV2–TV4 + ADR/runbook + CI | ✅ | PR #31/#34/#35/#36 reviewed & merged, CI green, `scan-secrets.sh` 9/9, ADR-0004, Runbook |

**Kết quả: 24/24 ✅** — Đạt Cổng G7 ngày 14/10/2026.

---

## TV2 — Ngô Quốc Trường Vĩ (2312796) — Danh mục & Khám phá

**Phần chính**: Category CRUD / FTS tiếng Việt / Google OAuth / Cache  
**Coverage Application**: >85%  
**Tests**: 194/194 pass (Tuần 5) → 446/446 (G7, chung hệ thống)

| K | Kỹ năng | Yêu cầu TV2 | Trạng thái | Minh chứng chính |
|:---:|---|---|:---:|---|
| K01 | Phân tích SRS/FR/NFR/ADR/contract | SP ADR search/cache + mapping | ✅ | `ADR-0003-category-and-search-caching-week3.md`, mapping FR-CAT-*/FR-SRCH-* |
| K02 | .NET10 Minimal APIs, REST, Scalar, RFC7807 | SP Category/Search group + lỗi | ✅ | `/api/v1/categories/*`, `/api/v1/recipes/search`, Problem Details 404/409/422 |
| K03 | Clean Architecture, entity/value object, DI | SP Category/Slug/repository | ✅ | `Category` entity, `Slug` value object, `ICategoryRepository` |
| K04 | CQRS/MediatR + behaviors | SP category/search handlers; LAB behavior tối thiểu | ✅ | `GetCategoriesQuery`, `SearchRecipesQuery`, LAB behavior riêng |
| K05 | FluentValidation + Zod/RHF | SP category form/query params | ✅ | `CategoryValidator`, `SearchQueryValidator`, FTS sanitizer (loại `!&|()*:'"`)|
| K06 | EF Core/PG16, migration, seed, index | SP Category/FTS migration/query | ✅ | `CategoryConfiguration`, FTS trigger/GIN index, seed 25 categories |
| K07 | UoW/transaction/audit/soft delete/concurrency | LAB cập nhật Recipe 2 writer + UoW/audit/filter | ✅ | LAB 2-writer Recipe, audit filter, test `Week3AuthAndPersonalLabTests` |
| K08 | Identity/PBKDF2, JWT, refresh hash/rotation/logout | LAB register/login/refresh/reuse/logout tối thiểu | ✅ | LAB L1 đầy đủ: register/login PBKDF2, JWT, refresh rotation, logout |
| K09 | Google OAuth2/PKCE, Auth.js, ID token verify | SP Google callback/verify/link | ✅ | Google Code+PKCE/Auth.js, backend verify ID token, `POST /auth/google` |
| K10 | RBAC/policy/rate limit/secrets/HTTPS/CORS | SP Admin + LAB owner/VerifiedAuthor/limit | ✅ | `AdminPolicy` cho category CRUD, LAB owner/VerifiedAuthor/rate limit |
| K11 | FTS tsvector/tsquery/unaccent/GIN/ts_rank | SP full search + query analysis | ✅ | tsvector trigger, `unaccent`, GIN index, ts_rank, "phở"→"pho" AND filter, EXPLAIN, k6 |
| K12 | Redis cache-aside, OutputCache, invalidation, fallback | SP category/search Redis; LAB OutputCache/detail | ✅ | Redis cache `category:*`/`search:*` TTL 60'/30', invalidation sau mutation, fallback |
| K13 | MinIO/S3 upload/delete, MIME/magic bytes | LAB upload/delete 4 MIME + boundary | ✅ | LAB L4: 4 MIME, boundary test, magic bytes, delete idempotent |
| K14 | Hangfire fire-and-forget/delayed/recurring | LAB welcome+resize+sitemap+delayed+restart | ✅ | LAB: welcome email, resize, sitemap recurring, restart worker |
| K15 | SMTP/MailKit, resize 300×300/800×600, sitemap XML | LAB Mailhog email + resize + XML | ✅ | LAB L4: Mailhog SMTP, resize 2 kích thước, sitemap XML |
| K16 | Next.js App Router/TypeScript, SSR/ISR/CSR | SP SSR/ISR + CSR admin | ✅ | `/categories/[slug]` SSR+ISR `revalidate=3600`, CSR admin panel |
| K17 | TanStack Query, optimistic rollback, next/image, bundle | SP category mutation/query; LAB next/image tối ưu | ✅ | `useMutation` category CRUD, LAB `next/image` `sizes`/`priority` |
| K18 | Responsive 320/768/1200px, WCAG2.1 AA, screen reader | SP category/search checklist | ✅ | `aria-label` search, `aria-live="polite"` kết quả, `<nav aria-label>` phân trang, `aria-current="page"` |
| K19 | SEO: metadata/OG/canonical/301/robots/JSON-LD | SP public metadata; LAB full Recipe JSON-LD/301 | ✅ | `generateMetadata` tại `/recipes`, `/categories`, `/search`; `CollectionPage`+`ItemList`+`BreadcrumbList` JSON-LD; `canonical`; `robots: {index: !q}` |
| K20 | Serilog/Seq/correlation, OTEL, health probes | SP query trace; LAB log sink/metrics/health | ✅ | EF Core query trace, LAB log sink riêng, OTEL metrics, health check |
| K21 | xUnit ≥80%, API happy+error, Jest/RTL, Playwright | SP category/search API/UI/E2E | ✅ | `Week5DiscoverySeoAndA11yTests` 11/11, `Week3DiscoverySearchCacheTests`, Playwright search E2E |
| K22 | k6/p95/p99, EXPLAIN/N+1, CWV/Lighthouse | SP query/k6; đo trang mình | ✅ | k6: 2150 req/s categories, 1215 req/s search, p95 <10ms, EXPLAIN GIN, Lighthouse |
| K23 | Docker/Compose/Nginx/volumes/backup-restore/scaling | SP FE Docker/CI; tự deploy/restore/test 2 API | ✅ | `docker-compose.staging.yml` 6 containers, Nginx gateway, BCP/DR `restore-db.sh` 1.8s, 25 categories + 100 recipes khôi phục |
| K24 | Git/PR/review/CI/static/architecture/secret scan/docs | PR cá nhân + Tâm review + ADR/runbook + CI | ✅ | PR #31 merged (Tâm review), `ADR-0003`, `TUAN_1–5.md`, CI xanh, secret scan |

**Kết quả: 24/24 ✅** — Đạt Cổng G7 ngày 14/10/2026.

---

## TV3 — Huỳnh Quốc Trung (2312786) — Soạn thảo công thức

**Phần chính**: Recipe Aggregate / Ingredient/Step / Concurrency RowVersion / Refresh Token  
**Coverage Application**: 96.09% (HEAD `f691d31`) → >85% G7  
**Tests**: 281/281 backend (05/10), 85/85 Jest, 11/11 Playwright E2E (08/10)

| K | Kỹ năng | Yêu cầu TV3 | Trạng thái | Minh chứng chính |
|:---:|---|---|:---:|---|
| K01 | Phân tích SRS/FR/NFR/ADR/contract | SP ADR schema/version + mapping | ✅ | `ADR-0001-recipe-schema-va-concurrency.md`, `ADR-0002-recipe-version-schema.md`, mapping K→FR/NFR ở `BANG_MINH_CHUNG_K01_K24.md` |
| K02 | .NET10 Minimal APIs, REST, Scalar, RFC7807 | SP Recipe/child group + lỗi | ✅ | `/api/v1/recipes/*`, `/ingredients`, `/steps`; lỗi `recipe.forbidden`, `ingredient.not_found`, `recipe.concurrency_conflict` (201/200/204, 401/403/404/422) |
| K03 | Clean Architecture, entity/value object, DI | SP Recipe aggregate/Nutrition/UoW | ✅ | `Recipe`, `RecipeNutrition` (owned entity), UoW, `LayerDependencyRulesTests` 4/4 |
| K04 | CQRS/MediatR + behaviors | SP recipe/refresh handlers; LAB behavior tự viết | ✅ | `CreateRecipeCommand`, `AddIngredientCommand`; LAB `ValidationBehavior.cs`, `PerformanceBehavior.cs` (đỏ→xanh `81c967b`) |
| K05 | FluentValidation + Zod/RHF | SP wizard/nested form + validators | ✅ | `recipe-schemas.ts` Zod, wizard RHF `_wizard/*`, `MyRecipesValidatorTests` 32/32 |
| K06 | EF Core/PG16, migration, seed, LINQ/index | SP Recipe/Nutrition/Step migration/query | ✅ | `RecipeConfiguration` (partial unique index `slug`), `AsSplitQuery`, EXPLAIN, `K22_sql_explain_*.md`, `K22_index_check.txt` |
| K07 | UoW/transaction/audit/soft delete/concurrency | SP aggregate/RowVersion + tests | ✅ | `AuditableEntityInterceptor`, `RecipeGuard.EnsureVersion`, `RecipeConcurrencyAndTransactionTests` 8 test (2 writer đúng 1 thắng, rollback lồng nhau, child recipe khác bị từ chối) |
| K08 | Identity/PBKDF2, JWT, refresh hash/rotation/logout | SP refresh; LAB register/login/hash/logout | ✅ | SP `IdentityService` khoá hàng `FOR UPDATE` rotation; LAB L1: PBKDF2, JWT, rotation, logout; T2 xanh |
| K09 | Google OAuth2/PKCE, Auth.js, ID token verify | LAB Google callback/verify/link | ✅ | LAB L1 `Google.cs` (`GoogleJsonWebSignature`); Google thật tay: `01-google-created.png` (201, `googleLinked=true`) |
| K10 | RBAC/policy/rate limit/secrets/HTTPS/CORS | SP owner; LAB Admin/VerifiedAuthor/limit | ✅ | `RecipeGuard.LoadOwnedAsync`; LAB L10 Admin/VerifiedAuthor/limit (đỏ `4837af2`→xanh `f7da99f`) |
| K11 | FTS tsvector/tsquery/unaccent/GIN/ts_rank | LAB recipe search + trigger/index/rank/AND/page | ✅ | LAB L3: `LabDb.cs`, `L3/SearchEndpoints.cs`, `L3SearchTests` (unaccent/AND/rank/phân trang/Draft ẩn), EXPLAIN GIN |
| K12 | Redis cache-aside, OutputCache, invalidation, fallback | SP recipe detail invalidation; LAB cache/output/fallback | ✅ | SP `/api/revalidate` ISR, `WizardRefresh` 2 test; LAB L3 Redis thật `Search_is_cached_then_invalidated`, `Redis_down_falls_back_to_database` |
| K13 | MinIO/S3 upload/delete, MIME/magic bytes | LAB upload/delete 4 MIME + boundary | ✅ | LAB L4 `ImageValidator.cs`, `MediaEndpoints.cs`, `ObjectStorage.cs`; magic bytes 4 định dạng, biên 5MiB, MinIO thật không mồ côi |
| K14 | Hangfire fire-and-forget/delayed/recurring | LAB welcome+resize+sitemap+delayed+restart | ✅ | LAB L4 `Jobs.cs`: welcome enqueue, delayed, recurring sitemap; restart/retry ảnh `04–06` |
| K15 | SMTP/MailKit, resize 300×300/800×600, sitemap XML | LAB Mailhog + resize + XML | ✅ | LAB L4: `Sitemap_xml_lists_only_published_recipes`, resize MinIO, ảnh Mailhog `07-mailhog.png` |
| K16 | Next.js App Router/TypeScript, SSR/ISR/CSR | SP CSR editor/ISR detail; LAB SSR search | ✅ | `_wizard` (`"use client"`), `recipes/[slug]` `revalidate=300`; LAB L16 SSR 5/5 (đỏ→xanh `192e635`) |
| K17 | TanStack Query, optimistic rollback, next/image, bundle | SP ingredient/step mutations + image detail | ✅ | `RecipeWizard.tsx` `useQuery`/`useMutation` onMutate/onError rollback; Jest 43/43; E2E 2/2 |
| K18 | Responsive 320/768/1200px, WCAG2.1 AA, screen reader | SP wizard/editor checklist + NVDA | ✅ | 18 ảnh reflow `K18_reflow/` (wizard 15/15 OK); `K18_nvda_speech_log.txt` NVDA Chrome; 3 lỗi tìm+sửa (đỏ→xanh từng lỗi); jest-axe 0 vi phạm; Playwright `k18-a11y-names.spec.ts` 5/5; **Rủi ro tồn đọng thấp**: Backspace + mốc 1800/2000 ký tự N16 chưa nghe NVDA riêng (đã thiết kế đúng, Playwright xác nhận DOM) |
| K19 | SEO: metadata/OG/canonical/301/robots/JSON-LD | SP detail JSON-LD; LAB sitemap/robots/301 | ✅ | `recipe-jsonld.ts` đủ 12 thuộc tính; validator.schema.org 0 lỗi 0 cảnh báo; LAB L19 9/9 (sitemap/robots/301); `K19_validator.png`, `K19_jsonld_console.png` |
| K20 | Serilog/Seq/correlation, OTEL, health probes | SP recipe metric; LAB trace/log sink/health | ✅ | `RecipeMetrics.cs` OTEL: `culinary.recipes.created`/`updated` = 1 qua OTLP thật; LAB L20 8/8; `K20_metric_otel_chay_that.md` |
| K21 | xUnit ≥80%, API happy+error, Jest/RTL, Playwright | SP recipe API/UI/E2E | ✅ | Coverage Application **96.09%**; backend 272/272; Jest 55/55 → 85/85; Playwright 11/11 (`create-recipe.spec.ts`); đỏ→xanh L6 |
| K22 | k6/p95/p99, EXPLAIN/N+1, CWV/Lighthouse | SP concurrency/query; k6 + đo trang | ✅ | `K22_hieu_nang_recipe.md`: không N+1 (18 dòng thay 60), `AsSplitQuery`; k6 API p95 11.35ms; Lighthouse Performance 81→86, SEO 92→100 |
| K23 | Docker/Compose/Nginx/volumes/backup-restore/scaling | SP migration startup; tự deploy/restore/test 2 API | ✅ | LAB L23: 2 API instance (`5090`/`5091`), `smoke-scale.ps1`, restore 10 file SHA-256 khớp; `--migrate` startup |
| K24 | Git/PR/review/CI/static/architecture/secret scan/docs | PR cá nhân + Tâm review + ADR/runbook + CI | ✅ | PR #3/#13/#15/#27/#34/#35 (merge Tâm); `LayerDependencyRulesTests` 4/4; secret scan `K24_secret_scan.md` 205 commit sạch; `RUNBOOK_SOAN_THAO_CONG_THUC.md`; CI nhánh xanh 6/6 |

**Kết quả: 24/24 ✅** — Đạt Cổng G7 ngày 14/10/2026.

> [!NOTE]
> **K18 rủi ro tồn đọng**: Backspace và ngưỡng 1800/2000 ký tự (N16) chưa nghe NVDA riêng trong lần nghe cuối — thiết kế đúng (GOV.UK pattern), Playwright xác nhận DOM, không chặn XONG. TV3 ghi rõ trong `LAB_K18_checklist.md`.
>
> **K24 note**: PR #27 merge bởi TV4 (`Tson-dev`, 05/10) không qua review Tâm — đã xử lý hồi tố qua review PR #34/#35 (Tâm review trực tiếp).

---

## TV4 — Nguyễn Hữu Trung Sơn (2312739) — Xuất bản, Media & Vận hành

**Phần chính**: Docker/Nginx/Compose / RustFS S3 / Hangfire Resize / OTEL / Cloud Staging  
**Coverage Application**: 97.07% (Tuần 5) → 96.17% (G7 chung)  
**Tests**: 430/430 + 5/5 ConcurrencySpike (Tuần 5); Playwright 40/40 ×2 chống staging; k6 102 VU p95 148.56ms

| K | Kỹ năng | Yêu cầu TV4 | Trạng thái | Minh chứng chính |
|:---:|---|---|:---:|---|
| K01 | Phân tích SRS/FR/NFR/ADR/contract | SP ADR delete/media + mapping | ✅ | `ADR-TV4-001` (storage+logout), `ADR-TV4-002` (RustFS), `MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` |
| K02 | .NET10 Minimal APIs, REST, Scalar, RFC7807 | SP Image/status group + lỗi | ✅ | `/api/v1/recipes/{id}/images`, `/publish`, `/unpublish`, `/archive`; `503 storage.unavailable` (PR #20) |
| K03 | Clean Architecture, entity/value object, DI | SP file/job interfaces + status domain methods; LAB value object | ✅ | `IFileStorageService`, `ImageResizeQueue`, `RecipeImage` entity; LAB value object |
| K04 | CQRS/MediatR + behaviors | SP status/media handlers; LAB behavior tối thiểu | ✅ | `PublishRecipeCommand`, `UploadImageCommand`; LAB behavior (K04 hoàn thành tuần 6) |
| K05 | FluentValidation + Zod/RHF | SP media metadata/status form; LAB recipe form RHF/Zod | ✅ | `ImageUploadValidatorTests` 6 facts + 2 theories; LAB RHF/Zod recipe form |
| K06 | EF Core/PG16, migration, seed, index | SP Image config/index + LAB seed/query | ✅ | `RecipeImageConfiguration`, index/seed; LAB EXPLAIN truy vấn image/sitemap |
| K07 | UoW/transaction/audit/soft delete/concurrency | SP primary/status transaction; LAB RowVersion/filter/audit | ✅ | Transaction publish/primary; LAB RowVersion 2-writer, audit filter |
| K08 | Identity/PBKDF2, JWT, refresh hash/rotation/logout | SP logout; LAB register/login/refresh/reuse | ✅ | `POST /auth/logout` revoke idempotent 204; LAB L1: register/login/refresh/reuse rotation |
| K09 | Google OAuth2/PKCE, Auth.js, ID token verify | LAB Google callback/verify/link | ✅ | LAB L1 Google callback; credentials có/không — ghi đúng trạng thái |
| K10 | RBAC/policy/rate limit/secrets/HTTPS/CORS | SP owner/dashboard + LAB VerifiedAuthor/limit | ✅ | `AuthorPolicy`, rate limiter; secret scan CI (`render.yaml` đã xử lý JWT rotate — xem proposal `DE_XUAT_09`); LAB VerifiedAuthor/limit |
| K11 | FTS tsvector/tsquery/unaccent/GIN/ts_rank | LAB recipe search + trigger/index/rank/AND/page | ✅ | LAB L3 FTS riêng TV4: trigger/GIN/rank/AND/page |
| K12 | Redis cache-aside, OutputCache, invalidation, fallback | SP status/image invalidation; LAB cache/output/fallback | ✅ | Cache invalidation publish/archive; **multi-instance shared Redis** (PR #19, `N1`); LAB fallback |
| K13 | MinIO/S3 upload/delete, MIME/magic bytes | SP upload/delete đầy đủ + boundary tests | ✅ | `MinioE2ETests.cs` + `ImageUploadValidatorTests.cs` chạy RustFS thật; 4 MIME; boundary 5MiB; magic bytes |
| K14 | Hangfire fire-and-forget/delayed/recurring | SP resize/sitemap; LAB welcome+delayed+restart | ✅ | `ResizeImageJob`, `ImageResizeQueue`; sitemap cron 02:00 UTC + distributed lock (PR #22/#36); LAB welcome+delayed+restart |
| K15 | SMTP/MailKit, resize 300×300/800×600, sitemap XML | SP resize/XML; LAB Mailhog SMTP | ✅ | `ImageResizeD2Tests` 300×300/800×600 idempotent; sitemap `/sitemap.xml` dynamic; LAB Mailhog |
| K16 | Next.js App Router/TypeScript, SSR/ISR/CSR | SP CSR uploader/ISR media; LAB SSR search | ✅ | `ImagesStep.tsx` CSR uploader, `tsc --noEmit` exit 0, `next build` exit 0; LAB SSR search |
| K17 | TanStack Query, optimistic rollback, next/image, bundle | SP image mutation/progress; LAB query/rollback/bundle | ✅ | Image mutation/presigned URL; **thanh progress upload** (K17 hoàn thành tuần 6); LAB query/rollback/bundle |
| K18 | Responsive 320/768/1200px, WCAG2.1 AA, screen reader | SP upload/gallery/status checklist | ✅ | Checklist 320/768/1200px upload/gallery, focus trap, aria; `K18_nvda_speech_log.txt` (TV3 tuần 4 `Tuan04/`) |
| K19 | SEO: metadata/OG/canonical/301/robots/JSON-LD | SP SEO/sitemap/robots; LAB redirect/metadata | ✅ | `sitemap.ts`, `robots.ts`, `recipe-jsonld.ts` OG/JSON-LD; sitemap cron; LAB redirect/metadata đủ |
| K20 | Serilog/Seq/correlation, OTEL, health probes | SP OTEL/probes; LAB logging middleware | ✅ | OTEL HTTP→EF trace (PR #19 `N1`); `Health.cs` `/health`, `/health/live`, `/health/ready` credential probe; LAB logging middleware |
| K21 | xUnit ≥80%, API happy+error, Jest/RTL, Playwright | SP media/publish API/UI/E2E | ✅ | 430/430 backend (Tuần 5); Playwright **40/40 ×2** chống staging (publish/media/ops E2E); Jest 86/86 |
| K22 | k6/p95/p99, EXPLAIN/N+1, CWV/Lighthouse | SP metrics/load; EXPLAIN + đo trang | ✅ | k6 102 VU p95 **148.56ms** 0% lỗi; EXPLAIN publish/sitemap; Lighthouse (First Load JS 105KB < 200KB) |
| K23 | Docker/Compose/Nginx/volumes/backup-restore/scaling | SP Compose/Nginx; tự deploy/restore/test 2 API | ✅ | Cloud Staging Render; `docker-compose.staging.yml`; `backup-db.sh`/`restore-db.sh` SHA-256; 2 API instance shared Redis; `pg_dump` 14 bảng drill |
| K24 | Git/PR/review/CI/static/architecture/secret scan/docs | PR cá nhân + Tâm review + ADR/runbook + CI | ✅ | PR #16/#19/#20/#21/#22/#36 (Tâm review/merge); `ADR-TV4-001`, `ADR-TV4-002`; `RUNBOOK.md`; CI backend xanh; secret scan CI; CHANGELOG |

**Kết quả: 24/24 ✅** — Đạt Cổng G7 ngày 14/10/2026.

> [!NOTE]
> **K09 Google OAuth**: TV4 đánh dấu đúng trạng thái chờ credentials trong các tuần trước — đã hoàn thành LAB L1 Google callback ở tuần 6.  
> **K17 thanh progress**: Nợ từ tuần 3, hoàn thành tuần 6.  
> **PR #36**: Merge bởi `Tson-dev` (13/10), Tâm hậu kiểm xác nhận qua PR #37 regression 446/446 tests.

---

## Tổng hợp so sánh 4 thành viên

| K | Kỹ năng cốt lõi | TV1 Tâm | TV2 Trường Vĩ | TV3 Quốc Trung | TV4 Trung Sơn |
|:---:|---|:---:|:---:|:---:|:---:|
| K01 | SRS/ADR/Contract | ✅ | ✅ | ✅ | ✅ |
| K02 | Minimal APIs/REST | ✅ | ✅ | ✅ | ✅ |
| K03 | Clean Architecture | ✅ | ✅ | ✅ | ✅ |
| K04 | CQRS/MediatR/behaviors | ✅ | ✅ | ✅ | ✅ |
| K05 | FluentValidation/Zod/RHF | ✅ | ✅ | ✅ | ✅ |
| K06 | EF Core/migration/seed/index | ✅ | ✅ | ✅ | ✅ |
| K07 | UoW/audit/soft delete/concurrency | ✅ | ✅ | ✅ | ✅ |
| K08 | Identity/JWT/refresh/rotation | ✅ | ✅ | ✅ | ✅ |
| K09 | Google OAuth2/PKCE | ✅ | ✅ | ✅ | ✅ |
| K10 | RBAC/rate limit/secrets/HTTPS | ✅ | ✅ | ✅ | ✅ |
| K11 | FTS/tsvector/GIN/ts_rank | ✅ | ✅ | ✅ | ✅ |
| K12 | Redis/OutputCache/invalidation | ✅ | ✅ | ✅ | ✅ |
| K13 | MinIO/S3/MIME/magic bytes | ✅ | ✅ | ✅ | ✅ |
| K14 | Hangfire/fire-and-forget/recurring | ✅ | ✅ | ✅ | ✅ |
| K15 | SMTP/resize/sitemap XML | ✅ | ✅ | ✅ | ✅ |
| K16 | Next.js/SSR/ISR/CSR | ✅ | ✅ | ✅ | ✅ |
| K17 | TanStack Query/rollback/next/image | ✅ | ✅ | ✅ | ✅ |
| K18 | Responsive/WCAG2.1 AA/NVDA | ✅ | ✅ | ✅ | ✅ |
| K19 | SEO/OG/JSON-LD/canonical/301 | ✅ | ✅ | ✅ | ✅ |
| K20 | Serilog/OTEL/metrics/health | ✅ | ✅ | ✅ | ✅ |
| K21 | xUnit/Jest/Playwright/coverage≥80% | ✅ | ✅ | ✅ | ✅ |
| K22 | k6/EXPLAIN/CWV/Lighthouse | ✅ | ✅ | ✅ | ✅ |
| K23 | Docker/Nginx/backup-restore/scaling | ✅ | ✅ | ✅ | ✅ |
| K24 | Git/PR/review/CI/secret scan/docs | ✅ | ✅ | ✅ | ✅ |
| **Tổng** | | **24/24** | **24/24** | **24/24** | **24/24** |
| **Coverage** | | 96.17% | >85% | 96.09% | 97.07% |
| **Nổi bật** | | BCP/DR 1.8s; PBKDF2 HS512; CI/Secret scan | FTS tiếng Việt; Google OAuth PKCE; Redis k6 | Recipe Aggregate; RowVersion 2-writer; NVDA a11y | RustFS S3; Hangfire resize; Cloud Staging; OTEL trace |

---

## Số liệu kiểm chứng chung (G7 — 14/10/2026)

| Hạng mục | Kết quả |
|---|---|
| Backend tests (`CulinaryBlog.Tests` + `ConcurrencySpike`) | **446/446 PASS** (0 Failed, 0 Skipped) |
| Frontend tests (Jest, 15 suites) | **86/86 PASS** |
| `dotnet format --verify-no-changes` | Exit code **0** |
| `npm run build` Next.js 15 | **16/16 routes** thành công |
| Secret scan `deploy/scan-secrets.sh` | `OK: không phát hiện secret` |
| Secret scan `deploy/scan-secrets.test.sh` | **9/9 pass, 0 fail** |
| BCP/DR restore drill | **1.8s**, 25 categories + 100 recipes toàn vẹn |
| Application coverage | **96.17%** (ngưỡng yêu cầu: 80%) |
| Playwright E2E (TV4 staging) | **40/40 ×2** |
| k6 TV4 staging | 102 VU · p95 **148.56ms** · 0% lỗi |

---

## Kết luận

> [!IMPORTANT]
> **Cổng G7 ĐẠT** — Toàn bộ 4/4 thành viên hoàn thành **24/24 nhóm kỹ năng K01–K24** với bằng chứng code, test và kết quả thực tế. Dự án Culinary Blog — Nhóm 16 sẵn sàng bảo vệ trước Hội đồng Giảng viên.

Điểm nổi bật đáng ghi nhận:
- **TV3 Quốc Trung**: Quy trình debug NVDA (K18) rất nghiêm túc — tìm đúng nguyên nhân `aria-describedby` + GOV.UK pattern, sửa có test đỏ→xanh từng lỗi.
- **TV4 Trung Sơn**: Xử lý sự cố MinIO → RustFS (ADR-TV4-002), DevConfigParityTests 3 test chống hồi quy, và Cloud Staging Render thật.
- **TV2 Trường Vĩ**: FTS sanitizer loại bỏ toán tử nguy hiểm, Google PKCE end-to-end, k6 throughput >2000 req/s cho categories.
- **TV1 Tâm**: Hệ thống 446 test pass, BCP/DR drill dưới 2s, toàn bộ review cross-member.
