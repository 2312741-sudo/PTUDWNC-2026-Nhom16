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
| K02 | .NET 10 Minimal APIs, REST/version, Scalar/RFC7807 | ❌ | Health check mới, mã lỗi `storage.unavailable` (B1 nếu duyệt), mã lỗi từ chối file (N1-1, N1-7, N2-4) | TV4-K02 | ⬜ Chưa làm |
| K03 | Clean Architecture, interface, DI, value object | ❌ | `ObjectStorageHealthCheck` đúng tầng; dùng lại `IObjectStorageReader` (N1-1) | TV4-K03 | ⬜ Chưa làm |
| K04 | CQRS/MediatR + behavior logging/validation/caching | ❌ | Lab L5 tự viết behavior tối thiểu (N3-2) | TV4-K04 | ⬜ Chưa làm |
| K05 | FluentValidation + sanitization + Zod/RHF | ✅ | Bổ sung form status + form RHF/Zod trong lab (N2-8, N3-1) | TV4-K05 | 🟡 Có nền · tuần 4 bổ sung |
| K06 | EF/PG16 Code First, migration/config/seed, index | ❌ | Index phục vụ sitemap + EXPLAIN lại (N1-6, N2-6) | TV4-K06 | ⬜ Chưa làm |
| K07 | UoW/transaction/audit/soft delete/RowVersion | ❌ | 2 instance không mất dữ liệu; race sitemap; LAB RowVersion/audit (N1-5) | TV4-K07 | ⬜ Chưa làm |
| K08 | Identity/PBKDF2, JWT, refresh rotation/reuse/logout | ✅ | Bù lab mục 2 (refresh hash/rotation/reuse) (N3-1) | TV4-K08 | 🟡 Có nền · tuần 4 bổ sung lab |
| K09 | Google OAuth2/PKCE, Auth.js, ID token verify/link | ❌ | Lab mục 2; **thiếu credentials ⇒ ghi "còn chờ", không tính hoàn thành** (N3-1) | TV4-K09 | ⬜ Chưa làm |
| K10 | RBAC/ownership/policy/rate limit/secrets/HTTPS/CORS | ❌ | Bỏ secret hardcode khỏi `render.yaml` + secret scan CI; quyền upload (N1-8, N2-4) | TV4-K10 | ⬜ Chưa làm |
| K11 | FTS tsvector/unaccent/pg_trgm/GIN/ts_rank, filter/sort/page | ✅ | EXPLAIN lại + đo sau thay đổi (N2-6) | TV4-K11 | 🟡 Có nền · tuần 4 đo lại |
| K12 | Redis cache-aside, OutputCache, invalidation, fallback | ✅ | Redis thật thay cache in-process (hoặc ADR ghi giới hạn) + tắt Redis để test fallback (N1-5, N2-5) | TV4-K12 | 🟡 Có nền · **đây là gap thật** (chưa có Redis) |
| K13 | MinIO/S3 upload/delete, stream/MIME/magic bytes/GUID | ✅ | Kịch bản tấn công file ở mức E2E (N2-4) | TV4-K13 | 🟡 Có nền · bổ sung E2E tấn công |
| K14 | Hangfire fire-and-forget/delayed/recurring, retry, persistence, dashboard | ❌ | Sitemap recurring cron 02:00 UTC + distributed lock; nhiều worker chung queue (N1-5, N1-6) | TV4-K14 | ⬜ Chưa làm |
| K15 | SMTP/MailKit, resize 300×300/800×600, sitemap XML | ❌ | Backup volume file; sitemap XML sinh theo lịch (N1-4, N1-6) | TV4-K15 | ⬜ Chưa làm |
| K16 | Next.js App Router/TS/Tailwind, SSR/ISR/CSR | ❌ | Hạ tầng CI build FE + lab L5 (`search-ssr`, `isr-detail`) (N2-3, N3-2) | TV4-K16 | ⬜ Chưa làm |
| K17 | TanStack Query/server state, optimistic rollback, next/image | ❌ | Progress upload + rollback; lab `query-rollback`, `image-opt` (N2-8, N3-2) | TV4-K17 | ⬜ Chưa làm |
| K18 | Responsive, WCAG 2.1 AA, keyboard, loading/error | ❌ | Checklist 320/768/1200 px + focus/aria (N2-8) | TV4-K18 | ⬜ Chưa làm |
| K19 | SEO metadata/OG/Twitter/canonical/301/robots/JSON-LD | ❌ | Sitemap theo lịch; lab `seo` đủ metadata/robots/redirect (N1-6, N3-2) | TV4-K19 | ⬜ Chưa làm |
| K20 | Serilog/Seq/correlation, OTEL HTTP/DB/metrics, health probes | ✅ (cấu hình) | **Trace thật** + Seq sink + health failure test (N1-2, N1-3) | TV4-K20 | 🟡 Cấu hình có · **bằng chứng trace còn thiếu** |
| K21 | xUnit/unit ≥80%, API happy+error, Jest/RTL, Playwright | ❌ | Playwright thật + 5 luồng; ngưỡng coverage trong CI (N2-1, N2-2, N2-7) | TV4-K21 | ⬜ Chưa làm |
| K22 | k6 p50/p95/p99, EXPLAIN/N+1/cache hit, CWV/Lighthouse | ❌ | Commit script k6 tái lập được + đo p95/p99; số đo resilience (N2-5, N2-6) | TV4-K22 | ⬜ Chưa làm |
| K23 | Docker multi-stage/Compose/Nginx/env/volumes/backup-restore/scaling | ✅ (CI + stack) | **Backup/restore drill + 2 API instance + compose prod** (N1-4, N1-5) | TV4-K23 | 🟡 Có nền · tuần 4 làm phần còn thiếu |
| K24 | Git/PR/review/CI/static analysis/architecture test/secret scan/docs | ✅ | Secret scan CI; cổng CI frontend; mở PR cho nhánh lab; runbook (N1-8, N2-3, N3-4, N4) | TV4-K24 | 🟡 Có nền · bổ sung |

**Đếm**: 9 ô đã có · **15 ô cần bù trong tuần 4** (K02, K03, K04, K06, K07, K09, K10, K14, K15,
K16, K17, K18, K19, K21, K22). Ô nào cuối tuần vẫn thiếu thì ghi rõ ở
`TRANG_THAI_THUC_HIEN_TUAN_4.md`, **không** đánh dấu đạt.

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
Đường dẫn: docker-compose.dev.yml (service otel-collector), Program.cs (AddOtlpExporter),
           Serilog sink Seq, .env.example
Lệnh chạy: [điền]
Kết quả: [điền — phải thấy span HTTP → span SQL cho 1 request cụ thể]
Reviewer + ngày: ⬜
```

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
Đường dẫn: deploy/backup.sh, deploy/restore.sh, docker-compose.dev.yml, ADR mới
Lệnh chạy: [điền]
Kết quả: [điền — số bảng/row/object sau restore; số job sitemap khi 2 worker chạy]
Giới hạn: [ghi rõ giới hạn hạ tầng, không ngụ ý đã đạt SLA]
Reviewer + ngày: ⬜
```

### TV4-K14 (N1-6) — Sitemap cron 02:00 UTC + distributed lock

```text
Evidence: TV4-K14
Tuần / Người / Task: 4 / TV4 / N1-6
Đường dẫn: [điền — recurring job + lock]
Lệnh chạy: [điền]
Kết quả: [điền — 1 chu kỳ, N worker, đếm số lần job thực thi]
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
