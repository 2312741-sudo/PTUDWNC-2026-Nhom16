# SỔ EVIDENCE TUẦN 4 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Reviewer nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Nhánh**: `2312739_NHTSon_D5-D6-D7` (từ `origin/main` = `7fe8fc2`) · **Lab**: `practice/TV4/L4`, `practice/TV4/L5`
> **Trạng thái**: tất cả bắt đầu ở trạng thái **Chưa làm**; ô K chuyển sang "có minh chứng" khi có
> **code/config + test + kết quả thật + reviewer Tâm xác nhận**. Ô nào chưa đủ thì ghi **Chưa làm**,
> không làm tròn số.

> [!IMPORTANT]
> **Không lấy 4 file `Lab 04` ở `docs/evidence/TV4/` làm minh chứng của sổ này.**
> `TUAN_4.md`, `BAO_CAO_LAB_04.md`, `Lab04_2312739_NguyenHuuTrungSon.docx`,
> `Lab4_2312739_NguyenHuuTrungSon.docx` do commit `80b2c0e` của **Nguyễn Thanh Tâm (TV1)** tạo
> (sinh cùng bộ cho TV2/TV3). Theo quyết định nhóm 30/09: giữ nguyên, không sửa, không dùng để
> chấm điểm kỹ năng cá nhân của TV4.

---

## 1. Baseline đầu tuần 4 — **đã đo thật 30/09/2026**

> Môi trường đo: Windows, **SDK .NET 10.0.401** (khớp `global.json` và CI), Docker Desktop 29.6.2 /
> Compose v5.3.1, hạ tầng lấy từ `docker-compose.dev.yml` (`postgres` 16-alpine, `redis` 7-alpine,
> `s3` RustFS 1.0.0, `mailhog`) với cấu hình thật trong `.env` cục bộ (`.env` **không** commit).
> Đo tại commit `a4fc8d8` (bằm `origin/main` = `7fe8fc2` + bộ tài liệu Tuan04), **trước** khi merge
> `1492b39` — commit đó chỉ đụng frontend nên **không làm thay đổi** kết quả build/test backend.
> Log gốc: `Tuan04/logs/baseline_build.log`, `baseline_format.log`, `baseline_test.log`,
> `baseline_coverage.log`.

| Hạng mục | Lệnh | Kết quả | Ngày |
|---|---|---|---|
| Build backend | `dotnet build CulinaryBlog.sln --no-restore -c Release` | ✅ **0 warning / 0 error** (11.66 s) | 30/09 |
| Format | `dotnet format CulinaryBlog.sln --verify-no-changes --no-restore` | ✅ **exit 0** (không có diff) | 30/09 |
| Test | `dotnet test CulinaryBlog.sln --no-build -c Release --collect "XPlat Code Coverage"` | ✅ **178/178 pass** — `CulinaryBlog.Tests` **173/173** (2 m 39 s) + `ConcurrencySpike` **5/5**; Failed 0, **Skipped 0** | 30/09 |
| Coverage `CulinaryBlog.Application` | đọc `coverage.cobertura.xml` | ✅ **line 83.37%** / branch 67.44% — **đã vượt ngưỡng G5 80%** | 30/09 |
| Coverage tổng | cùng trên | 31.29% (2446/7817 dòng) — thấp vì `Infrastructure` chỉ 11.29% | 30/09 |
| Hạ tầng test | `docker compose -f docker-compose.dev.yml up -d postgres redis s3 mailhog` | ✅ 4 container lên; test E2E storage **thật** (không skip — `Skipped=0`) | 30/09 |
| Frontend typecheck | `npx tsc --noEmit` | ⬜ Chưa chạy | — |
| Frontend build | `npm run build` (`src/frontend`) | ⬜ Chưa chạy (nằm ở N2-3 khi dựng cổng CI) | — |
| Verify `/search` hết lỗi 500 | TC1–TC3 của `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md` | 🟡 **Xác nhận bằng tĩnh, chưa chạy runtime** — xem mục 1.1 | 30/09 |

> So với tuần 3 (`80b2c0e`: 172/172) ⇒ **178/178, +6 test**, không có test nào bị skip.

### 1.1 Xác nhận lỗi `500` ở `/search` — không tự sửa, lấy bản sửa từ `main`

Theo chỉ đạo 30/09: **không tự gỡ lỗi**, dùng bản sửa đã có trên `main`. Đã xác nhận:

| # | Kiểm tra | Kết quả |
|---|---|---|
| 1 | Commit sửa trên `main` | ✅ `e523579` *"fix(frontend): extract SearchFilterSelect to client component for search page SSR"* (TV2, 30/09 11:22) — **Phương án B** trong báo cáo |
| 2 | `e523579` đã nằm trong lịch sử nhánh tuần 4 | ✅ `git merge-base --is-ancestor e523579 origin/main` ⇒ có (nên không cần merge thêm cho lỗi này) |
| 3 | `src/frontend/src/app/search/page.tsx` còn `onChange` không | ✅ **0 kết quả**; file vẫn là Server Component (đúng — không thêm `'use client'`) |
| 4 | File mới `src/frontend/src/components/SearchFilterSelect.tsx` | ✅ có `'use client'`; `onChange` nằm trong Client Component; props truyền vào (`name`, `defaultValue`, `options`) **đều serialize được** |
| 5 | Đã merge `main` vào nhánh tuần 4 | ✅ merge `4770602`, kéo thêm `1492b39` (TV2: sửa hiển thị ảnh + thêm ô tìm kiếm ở trang `/recipes`) |
| 6 | Chạy thật TC1–TC4 (`/search`, `?q=a`, `?q=gà`, `?q=pho`) | ⬜ **Chưa chạy** — dừng theo chỉ đạo, không tự dựng app để test vì việc sửa đã ở trên `main`; chuyển sang làm cùng **N2-2 (E2E luồng search)** để có bằng chứng lặp lại được |
| 7 | Frontend build sau khi merge `main` | ✅ `npx tsc --noEmit` **exit 0**; `npm run build` **exit 0** (log: `Tuan04/logs/baseline_frontend.log`) |

> **Lưu ý về ý nghĩa của `next build`**: build xanh **không** phủ được lỗi `500` của `/search` — route
> `/search` được Next.js đánh dấu là **dynamic** (`ƒ /search`), lỗi RSC "Event handlers cannot be
> passed to Client Component props" chỉ nổ lúc render runtime. Vì vậy mục #7 chỉ chứng minh
> *TypeScript + build không hỏng*, còn bằng chứng lỗi đã hết vẫn phải lấy bằng E2E ở **N2-2**.

> Báo cáo `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md` **giữ nguyên 100%** (không sửa, không xoá)
> theo quyết định trước đó; trạng thái "đã có bản sửa trên `main`" được ghi ở đây và ở
> `TRANG_THAI_THUC_HIEN_TUAN_4.md` thay vì sửa báo cáo gốc.

---

## 2. Bảng 24 ô kỹ năng của TV4

> Cột **Đã có (trước tuần 4)** = 9 ô đã được `PHAN_CHIA_CONG_VIEC_6_TUAN.md` ghi nhận.
> Cột **Tuần 4** = việc sẽ bù; chi tiết ở `KE_HOACH_TUAN_4_TV4.md` mục 5.
> Bản **mapping đầy đủ 24 dòng** (FR/NFR ↔ ADR ↔ đường dẫn evidence ↔ lệnh kiểm chứng ↔ kết quả đo
> ↔ reviewer) nằm ở **`docs/evidence/TV4/MAPPING_K01_FR_NFR_ADR_EVIDENCE.md`** (đặt ở gốc `TV4/` vì
> phục vụ cả 4 tuần) — lập 30/09, chờ Tâm xác nhận.
> Bảng dưới đây là bản rút gọn theo nhóm người làm, **không** tự nâng trạng thái ô lên ✅.

| K | Kỹ thuật con cần chứng minh | Đã có (trước tuần 4) | Việc tuần 4 | Evidence key | Trạng thái |
|---|---|---|---|---|---|
| K01 | SRS/FR-NFR/ADR/API contract | ✅ (ADR media/vận hành) | Bảng mapping FR ↔ ADR ↔ evidence 24 dòng (N0-5) | `../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` | 🟡 **Mapping xong 30/09** · 24 dòng K01–K24 · chờ Tâm xác nhận |
| K02 | .NET 10 Minimal APIs, REST/version, Scalar/RFC7807 | 🟡 | `/health` + `/health/ready`; lỗi storage trả `503 storage.unavailable`; `/sitemap.xml` (N2-4 mã lỗi từ chối file chưa làm) | `Health.cs`, `Program.cs`, `StorageFailureContractTests` | 🟢 Đã làm 30/09 · chờ Tâm duyệt |
| K03 | Clean Architecture, interface, DI, value object | 🟡 | `ObjectStorageHealthCheck` (API) gọi `IObjectStorageReader` (Infrastructure) qua DI; probe S3 thật | `Health.cs`, `ObjectStorageCredentialProbe.cs` | 🟢 Đã làm 30/09 · chờ Tâm duyệt |
| K04 | CQRS/MediatR + behavior logging/validation/caching | ❌ | Lab L5 tự viết behavior tối thiểu (N3-2) | TV4-K04 | ⬜ Chưa làm |
| K05 | FluentValidation + sanitization + Zod/RHF | ✅ | Bổ sung form status + form RHF/Zod trong lab (N2-8, N3-1) | TV4-K05 | 🟡 Có nền · tuần 4 bổ sung |
| K06 | EF/PG16 Code First, migration/config/seed, index | ❌ | Index phục vụ sitemap + EXPLAIN lại (N1-6, N2-6) | TV4-K06 | ⬜ Chưa làm |
| K07 | UoW/transaction/audit/soft delete/RowVersion | 🟡 | Race sitemap chặn bằng Redis lock (1/1 thắng); audit/RowVersion còn ở phần lab | `SitemapGenerator.cs`, `SitemapLockTests` | 🟢 Lock đã làm 30/09 · lab chưa |
| K08 | Identity/PBKDF2, JWT, refresh rotation/reuse/logout | ✅ | Bù lab mục 2 (refresh hash/rotation/reuse) (N3-1) | TV4-K08 | 🟡 Có nền · tuần 4 bổ sung lab |
| K09 | Google OAuth2/PKCE, Auth.js, ID token verify/link | ❌ | Lab mục 2; **thiếu credentials ⇒ ghi "còn chờ", không tính hoàn thành** (N3-1) | TV4-K09 | ⬜ Chưa làm |
| K10 | RBAC/ownership/policy/rate limit/secrets/HTTPS/CORS | 🟡 | Bỏ secret khỏi `render.yaml` (`sync:false` + `fromService`); CI chạy `deploy/scan-secrets.sh` (bắt được JWT hardcode khi thử) | `render.yaml`, `deploy/scan-secrets.sh`, `backend.yml` | 🟢 Đã làm 30/09 · **còn rotate key thật** |
| K11 | FTS tsvector/unaccent/pg_trgm/GIN/ts_rank, filter/sort/page | ✅ | EXPLAIN lại + đo sau thay đổi (N2-6) | TV4-K11 | 🟡 Có nền · tuần 4 đo lại |
| K12 | Redis cache-aside, OutputCache, invalidation, fallback | 🟡 | `RecipeCacheService` dùng Redis thật + JSON + xoá theo prefix + fallback local khi Redis chết; 2 service dùng chung key (6/6 test) | `RecipeCacheService.cs`, `RedisSharedCacheTests` | 🟢 Gap đã lấp 30/09 · k6 cache-hit đo ở N2-5 |
| K13 | MinIO/S3 upload/delete, stream/MIME/magic bytes/GUID | ✅ | Kịch bản tấn công file ở mức E2E (N2-4) | TV4-K13 | 🟡 Có nền · bổ sung E2E tấn công |
| K14 | Hangfire fire-and-forget/delayed/recurring, retry, persistence, dashboard | 🟡 | Recurring "sitemap-daily" `0 2 * * *` UTC qua `IRecurringJobManager`; storage PostgreSQL dùng chung cho mọi worker | `Program.cs`, `SitemapGenerator.cs` | 🟢 Đã làm 30/09 · chờ Tâm duyệt |
| K15 | SMTP/MailKit, resize 300×300/800×600, sitemap XML | 🟡 | `deploy/backup.sh` + `deploy/restore.sh` (drill 14 bảng); sitemap XML sinh theo lịch, `/sitemap.xml` trả 200 | `deploy/backup.sh`, `deploy/restore.sh`, `SitemapGenerator.cs` | 🟢 Đã làm 30/09 · lịch 03:00 ICT chờ đặt ở host |
| K16 | Next.js App Router/TS/Tailwind, SSR/ISR/CSR | ❌ | Hạ tầng CI build FE + lab L5 (`search-ssr`, `isr-detail`) (N2-3, N3-2) | TV4-K16 | ⬜ Chưa làm |
| K17 | TanStack Query/server state, optimistic rollback, next/image | ❌ | Progress upload + rollback; lab `query-rollback`, `image-opt` (N2-8, N3-2) | TV4-K17 | ⬜ Chưa làm |
| K18 | Responsive, WCAG 2.1 AA, keyboard, loading/error | ❌ | Checklist 320/768/1200 px + focus/aria (N2-8) | TV4-K18 | ⬜ Chưa làm |
| K19 | SEO metadata/OG/Twitter/canonical/301/robots/JSON-LD | ❌ | Sitemap theo lịch; lab `seo` đủ metadata/robots/redirect (N1-6, N3-2) | TV4-K19 | ⬜ Chưa làm |
| K20 | Serilog/Seq/correlation, OTEL HTTP/DB/metrics, health probes | 🟡 | Trace thật qua collector: span HTTP + span `db.system=postgresql` cùng TraceId; log Serilog cùng TraceId; health probe credential S3 thật | `logs/seq_trace_recipes.log`, `TracingObservabilityTests`, `HealthTests` | 🟢 Đã làm 30/09 · chờ Tâm duyệt |
| K21 | xUnit/unit ≥80%, API happy+error, Jest/RTL, Playwright | ❌ | Playwright thật + 5 luồng; ngưỡng coverage trong CI (N2-1, N2-2, N2-7) | TV4-K21 | ⬜ Chưa làm |
| K22 | k6 p50/p95/p99, EXPLAIN/N+1/cache hit, CWV/Lighthouse | ❌ | Commit script k6 tái lập được + đo p95/p99; số đo resilience (N2-5, N2-6) | TV4-K22 | ⬜ Chưa làm |
| K23 | Docker multi-stage/Compose/Nginx/env/volumes/backup-restore/scaling | 🟡 | Backup/restore drill thật + OTEL collector trong Compose + Redis shared; **chưa dựng 2 process API qua Nginx** | `deploy/backup.sh`, `deploy/restore.sh`, `docker-compose.dev.yml` | 🟡 Phần đa xong 30/09 · thiếu 2 tiến trình API |
| K24 | Git/PR/review/CI/static analysis/architecture test/secret scan/docs | 🟡 | Secret scan trong CI + Redis service cho test; CI frontend, PR lab, runbook để N2/N3/N4 | `backend.yml`, `deploy/scan-secrets.sh` | 🟢 Secret scan xong 30/09 · phần còn lại để sau |

**Đếm (sau N1, cập nhật 30/09)**: 9 ô đã có + **8 ô vừa có bằng chứng trong N1** (K02, K03, K07, K10, K12,
K14, K15, K20) — tất cả ở trạng thái 🟢/🟡 **"chờ Tâm xác nhận"**, chưa ô nào tự đánh dấu đạt.
**Còn thiếu thật, chuyển sang N2/N3**: K04, K06 (index + EXPLAIN lại), K09 (Google OAuth — còn chờ
credentials), K16, K17, K18, K19, K21, K22. Ô nào cuối tuần vẫn thiếu thì ghi rõ ở
`TRANG_THAI_THUC_HIEN_TUAN_4.md`, **không** đánh dấu đạt.

> Lưu ý trung thực cho K23: phần "2 API instance" mới chứng minh được bằng **hai đối tượng dùng chung
> Redis/lock**, chưa phải hai tiến trình API sau Nginx. Chưa được tính là đạt.

---

## 3. Mẫu bản ghi evidence

```text
Evidence: TV4-Kxx (FR/NFR: ...)
Tuần / Người / Task: 4 / Nguyễn Hữu Trung Sơn (TV4) / [task]
Đường dẫn code/config: [đường dẫn cụ thể trong repo]
Nhánh / PR / commit: [nhánh] / PR #[số] (review: Nguyễn Thanh Tâm)
Test/lệnh chạy + môi trường: [lệnh cụ thể]
Kết quả thực tế (ảnh/log/coverage): [chụp log/ảnh, không có secret]
Minh chứng demo: [ảnh/video khi có]
Reviewer + ngày xác nhận: Nguyễn Thanh Tâm / [ngày]
Lỗi còn lại / ảnh hưởng: [ghi rõ nếu có]
```

---

## 4. Evidence tuần 4 (điền sau khi thực hiện)

> Các mục dưới đây **trống là chưa làm**. Không điền trước nội dung rồi chạy sau.
> File log dự kiến lưu tại `Tuan04/logs/`: `search_500_verify.log`, `health_ready_503.log`,
> `seq_trace_recipes.log`, `backup_restore_drill.log`, `resilience_matrix.log`, `k6_week4*.log`,
> `explain_week4_*.txt`, `e2e_playwright.log`, `ci_frontend.log`.

### TV4-K20 (N1-3) — Trace HTTP→DB thật vào Seq/OTLP

```text
Evidence: TV4-K20
Tuần / Người / Task: 4 / TV4 / N1-3
Đường dẫn: docker-compose.dev.yml (service otel-collector), deploy/otel-collector-config.yaml,
           Program.cs (AddOtlpExporter + Serilog sink Seq), CulinaryBlog.API.csproj,
           tests/CulinaryBlog.Tests/TracingObservabilityTests.cs
Lệnh chạy: docker compose -f docker-compose.dev.yml up -d seq otel-collector
           # API thật: OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317, Seq__Url=http://localhost:5341
           Invoke-WebRequest http://127.0.0.1:5198/api/v1/recipes?page=106\&pageSize=5
           Invoke-WebRequest http://127.0.0.1:5198/health/ready
           docker logs culinaryblog-otel --since 5m
           # Seq: http://localhost:5341/api/events?filter=Application%20%3D%20'CulinaryBlog.API'
           dotnet test --filter FullyQualifiedName~TracingObservabilityTests
Kết quả: 4 endpoint trả 200 (live / recipes x2 / ready). Collector nhận 2 lô span
         ({"resource spans": 1, "spans": 15} rồi {"spans": 3}). Cùng TraceId b19ee91b6746a491fbf8d79640262639
         có span HTTP "GET /api/v1/recipes/" VÀ span con "db.system=postgresql" (peer.service=127.0.0.1)
         => trace HTTP→DB thật, không phải trace giả lập. attributes/redact đã xoá db.statement.
         Seq nhận log Serilog của app: "HTTP GET /api/v1/recipes responded 200" với
         TraceId=a52518d1826ff3d3c833b0c229e22f60 — khớp span HTTP của chính request đó
         => log và trace liên kết được bằng TraceId. Test tự động 2/2 pass.
         Log gốc: Tuan04/logs/seq_trace_recipes.log
Reviewer + ngày: ⬜
```

> [!NOTE]
> **Hai lỗi thật đã phát hiện và sửa trong lúc làm N1-3** — ghi lại vì đều là lỗi mà test tự động bỏ sót:
> 1. `Program.cs` đăng lịch sitemap bằng static API `RecurringJob.AddOrUpdate` ngay trong lúc đăng ký DI.
>    Điều này ném `InvalidOperationException: Current JobStorage instance has not been initialized yet`
>    và làm **app không khởi động được ở mọi môi trường thật** (Development/Production). Test không bắt được vì
>    môi trường `Testing` không bật Hangfire. Đã sửa: đăng lịch **sau** `builder.Build()` qua
>    `IRecurringJobManager` lấy từ DI.
> 2. Sink Seq được gán vào `Log.Logger` *sau* `builder.Host.UseSerilog(...)` — mà `UseSerilog` lập tức thay thế
>    logger đó, nên **Seq nhận 0 event của app** (chỉ có span từ OTLP). Đã sửa: gộp `.WriteTo.Seq(url)` vào
>    đúng `LoggerConfiguration` của `UseSerilog`, bọc try/catch để Seq chết không làm app không khởi động.

### TV4-K21 (N2-1, N2-2) — Playwright + 5 luồng E2E

```text
Evidence: TV4-K21
Tuần / Người / Task: 4 / TV4 / N2-1, N2-2 (luồng publish + search của TV4)
Đường dẫn: src/frontend/playwright.config.ts, tests e2e, package.json scripts
Lệnh chạy: [điền]
Kết quả: [điền — số luồng pass/fail; search phải phủ TC1–TC12 của báo cáo lỗi 500]
Reviewer + ngày: ⬜
```

### TV4-K23 (N1-4, N1-5) — Backup/restore drill + 2 API instance

```text
Evidence: TV4-K23
Tuần / Người / Task: 4 / TV4 / N1-4, N1-5
Đường dẫn: deploy/backup.sh, deploy/restore.sh, deploy/otel-collector-config.yaml,
           docker-compose.dev.yml, src/backend/CulinaryBlog.Infrastructure/RecipeCacheService.cs,
           src/backend/CulinaryBlog.Infrastructure/RedisOptions.cs,
           tests/CulinaryBlog.Tests/RedisSharedCacheTests.cs,
           docs/adr/0003-category-and-search-caching-week3.md (quyết định cache-aside + fallback)
Lệnh chạy: # backup trên PostgreSQL native 18 (E:\PostgreSQL\bin)
           DATABASE_URL=... bash deploy/backup.sh
           # restore vào DB mới
           DATABASE_URL=... TARGET_DB=culinary_restore_drill bash deploy/restore.sh
           # cache dùng chung: 2 service/2 connection Redis
           dotnet test --filter FullyQualifiedName~RedisSharedCacheTests
Kết quả: Backup culinary_20260930T113019Z.dump = 728653 byte. Restore vào DB sạch
         culinary_restore_drill → 14 bảng, dữ liệu khớp nguồn (1344 users, 495 recipes, 25 categories).
         Restore lần 2 vào DB đã tồn tại → script TỪ CHỐI (exit 1), không ghi đè.
         Cache: 6/6 test pass — ghi ở service A thì service B (2 connection riêng) đọc trúng key,
         TTL có hiệu lực, JSON deserialize đúng, xoá theo prefix Recipe: xoá hết entry cũ,
         tắt Redis (SimulateServerDown) → tự fallback local, app vẫn 200.
         2 sitemap generator cùng tranh lock → đúng 1 thắng (xem TV4-K14).
         HAI TIẾN TRÌNH THẬT (tầng HTTP): chạy 2 process dotnet API (5080, 5081) + nginx round-robin
         (:8088, `nginx/nginx.multiinstance.conf`). 10 request qua nginx → api-1: 5, api-2: 5.
         api-1 làm nóng cache page 7; api-2 đọc lại CÙNG page đó trong 13 ms và đọc trúng key
         `multiinstance:cache:recipes:list:7:5:createdAt:desc::::` mà api-1 đã ghi ⇒ cache thuộc về Redis,
         không thuộc process. Tắt Redis: cả hai vẫn trả 200 từ DB, cả hai `/health/ready` trả 503.
         Log gốc: Tuan04/logs/multi_instance_two_api.log
Giới hạn: Hai tiến trình API đã chạy thật trên máy (5080/5081) sau nginx round-robin, nhưng
         chưa đưa vào docker-compose như một profile sẵn dùng cho cả nhóm, và chưa đo trên hạ tầng
         Render thật. Không có số đo RPO/RTO trên production.
         Lịch 03:00 Asia/Ho_Chi_Minh đã có trong repo: .github/workflows/backup.yml với cron
         '0 20 * * *' UTC (= 20:00 UTC hôm trước). Cần secret DATABASE_URL trong repository
         settings. Artifact của GitHub chỉ giữ 7 ngày nên 30 ngày phải chạy trên host có ổ đĩa riêng.
Reviewer + ngày: ⬜
```

### TV4-K14 (N1-6) — Sitemap cron 02:00 UTC + distributed lock

```text
Evidence: TV4-K14
Tuần / Người / Task: 4 / TV4 / N1-6
Đường dẫn: src/backend/CulinaryBlog.Infrastructure/SitemapGenerator.cs (LockTakeAsync/LockReleaseAsync +
           cache XML trong Redis), Program.cs (đăng lịch qua IRecurringJobManager sau builder.Build()),
           tests/CulinaryBlog.Tests/SitemapLockTests.cs
Lệnh chạy: dotnet test --filter FullyQualifiedName~SitemapLockTests
           # app thật: GET /sitemap.xml
Kết quả: 3/3 test pass. 2 generator cùng cố sinh sitemap trên cùng Redis → ĐÚNG 1 generator thắng lock,
         generator còn lại nhận "đã có người sinh" và không ghi đè; sau khi khoá được giải phóng thì
         lần sau sinh lại bình thường (lock có TTL nên job treo không chặn vĩnh viễn).
         Lịch: recurring job "sitemap-daily", cron "0 2 * * *", TimeZoneInfo.Utc
         = 02:00 UTC = 09:00 Asia/Ho_Chi_Minh.
         GET /sitemap.xml trên app thật trả 200 với XML hợp lệ (trang tĩnh + trang công thức).
Ghi chú: /sitemap.xml trả 200 với XML rỗng khi generator khác đang giữ lock — đây là hành vi có chủ đích
         (không phát sinh tải), KHÔNG phải lỗi; giá đổi là lần crawl kế tiếp lấy dữ liệu mới.
Reviewer + ngày: ⬜
```

### TV4-K22 (N2-5, N2-6) — k6 tái lập được + số đo resilience

```text
Evidence: TV4-K22
Tuần / Người / Task: 4 / TV4 / N2-5, N2-6
Đường dẫn: tests/performance/*.js (script k6 đã commit)
Lệnh chạy: [điền]
Kết quả: [điền — p50/p95/p99, req/s, % lỗi; bảng failover từng dependency]
Reviewer + ngày: ⬜
```

---

## 5. Sổ kỹ năng lab (D6)

| Lab | Nhánh | Phase | Check | Kỹ thuật con | Sổ chi tiết | Trạng thái |
|---|---|---|---|---|---|---|
| L4 | `practice/TV4/L4` | `media` 25 · `email` 3 · `xml` 3 · `jobs` 8 | **39/39 PASS** (28/09) | K13, K14, K15 | `Tuan03/SOK_LAB_L4.md` ⚠️ *chỉ có trên nhánh `origin/practice/TV4/L4`, chưa có trong `main`* | ✅ Xong tuần 3 (chưa mở PR) |
| L4 mục 2 | `practice/TV4/L4` | Identity/Google/refresh/forms/FTS | ⬜ | K08, K09, K10, K11 | `Tuan04/SOK_LAB_L4_MUC2.md` | ⬜ Chưa làm (bù nợ tuần 3) |
| L5 | `practice/TV4/L5` | 8 phase (mục 2.3.2) | ⬜ | K04, K05, K16, K17, K19, K23 | `Tuan04/SOK_LAB_L5.md` | ⬜ Chưa làm |

> Quy tắc: mock chỉ dùng cho unit/error test, **không** thay thế integration thật (DB/Redis/
> storage/provider). Thiếu Google credentials ⇒ ghi "integration ngoài còn chờ".

---

## 6. Nguyên tắc khi đóng ô kỹ năng

1. Ô có **nhiều kỹ thuật con** thì đánh dấu **từng kỹ thuật con**; thiếu một phần ⇒ ô chưa hoàn thành.
2. Một PR được tham chiếu nhiều ô khi nó thật sự chứa các kỹ năng đó.
3. "Đã đọc / đã họp / đã review code người khác / đã chạy lại demo nhóm" **không** tính.
4. Kết quả đo phải ghi **thiết bị, mạng, dữ liệu, số mẫu, công cụ, thời điểm, trạng thái cache**.
5. Không đưa secret hay tài khoản thật vào evidence; log đã redact.
6. Chỉ công nhận hoàn thành khi **reviewer Tâm xác nhận và ghi ngày**.
