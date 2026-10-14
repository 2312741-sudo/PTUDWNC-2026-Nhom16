# SỔ EVIDENCE TUẦN 6 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Reviewer nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Nhánh**: `2312739_NHTSon_D7` (từ `main` = `e93fbe8`, + commit `5e84995`) · **Lab**: `practice/TV4/L5` (L4: `practice/TV4/L4`)
> **Trạng thái 14/10**: baseline backend+FE đã đo (§1) và ghi vào §3.1/§3.2; các mục khác bắt đầu ở **Chưa làm**.
> Ô K chỉ chuyển "đủ bằng chứng" khi có **code/config + test + kết quả chạy thật**, và chỉ chốt khi
> **reviewer Tâm xác nhận + ghi ngày**. Ô nào thiếu ghi **Chưa làm**, không làm tròn số, không tự đánh dấu ✅.
> **Tuần 6 tự chứa**: mốc số liệu tuần 5 được chép lại ở §1 kèm nguồn — không cần mở lại `Tuan05/`.

> [!IMPORTANT]
> 1. **Không dùng số của tuần 5 làm số của tuần 6.** Số 446/446, 86/86, 40/40 là số đo cuối tuần 5 —
>    tuần 6 phải đo lại trên nhánh `2312739_NHTSon_D7` (W6-1/W6-2) và trên **staging** (W6-2/W6-3).
> 2. **Không lấy file tuần 5 ở `docs/evidence/TV4/` làm minh chứng của tuần 6** — chỉ trích kèm nguồn + ngày.
> 3. **`docs/evidence/TV4/Report/` là báo cáo phi chính thống của TV4** — không dùng làm căn cứ nghiệm thu.
> 4. Việc của thành viên khác (TV1/TV2/TV3) ghi ở §5 để đối chiếu **phụ thuộc**, không tính vào tiến độ TV4.

---

## 1. Baseline tuần 6 — ✅ ĐÃ ĐO 14/10/2026 (N1 · W6-1/W6-2 backend+FE)

> Môi trường: máy TV4 (Windows), .NET SDK **10.0.401**, Node **v24.21.0**, docker-compose.dev
> (postgres/redis/s3/mailhog). Lệnh y hệt CI. Log: `Tuan06/logs/`.
> **Chưa gồm**: Playwright E2E ×2 + migration chạy lặp (thuộc N2).

| Hạng mục | Lệnh | Kết quả tuần 6 (14/10) | Log |
|---|---|---|---|
| Restore | `dotnet restore CulinaryBlog.sln --locked-mode` | ✅ exit 0 | `logs/baseline_build.log` |
| Build backend | `dotnet build CulinaryBlog.sln --no-restore -c Release` | ✅ **0 warning · 0 error** | `logs/baseline_build.log` |
| Format | `dotnet format CulinaryBlog.sln --verify-no-changes --no-restore` | ✅ exit 0 | `logs/baseline_format.log` |
| Test | `dotnet test CulinaryBlog.sln -c Release` (CI flags) | ✅ **441/441** + **5/5** `ConcurrencySpike` · Failed 0 · **Skipped 0** | `logs/regression_test.log` |
| Coverage `CulinaryBlog.Application` | `bash deploy/check-coverage.sh 80 TestResults` | ✅ **96.17%** ≥ 80% | `logs/regression_coverage.log` |
| Frontend typecheck | `npx tsc --noEmit` | ✅ exit 0 | `logs/regression_frontend.log` |
| Frontend lint | `npm run lint` | ✅ exit 0 (warning `no-img-element` như cũ) | `logs/regression_frontend.log` |
| Frontend build | `npm run build` | ✅ exit 0 (16 route); `ECONNREFUSED` sinh sitemap lúc build là không chặn (không có API) | `logs/baseline_frontend_build.log` |
| Frontend unit | `npx jest --ci` | ✅ **86/86** · 15 suite | `logs/baseline_jest.log` |
| CI `Backend week 1` + `Frontend CI` | GitHub Actions | ⬜ chưa xác nhận run mới (mở PR ở W6-1) | link run |

> **Lưu ý đối chiếu plan §2.2**: plan ghi mốc tuần 5 là 430+5 và coverage 97.07%. Đo thật trên nhánh
> tuần 6 = **441+5=446** (khớp báo cáo repo tại `400b49a` "dotnet 446/446") và coverage **96.17%**.
> ⇒ "430" là mốc giữa tuần 5 đã cũ; coverage 96.17% vẫn vượt cổng 80%.
> **Skipped=0** xác nhận không có test bị ẩn bằng skip.

### 1.1. Mốc tuần 5 để đối chiếu (KHÔNG dùng làm số tuần 6)

| Hạng mục | Số cuối tuần 5 | Ngày/Commit | Nguồn |
|---|---|---|---|
| `dotnet test -c Release` | **430/430** + **5/5** `ConcurrencySpike`, Skipped 0 | 11/10 | `Tuan05/HANDOFF_TV4_TUAN5.md` §1 |
| Build / format | 0W · 0E · format exit 0 | 11/10 | `Tuan05/HANDOFF_TV4_TUAN5.md` §1 |
| Coverage `Application` | **97.07%** ≥ 80% | 07/10 (`0a9b1a5`) | `Tuan05/SO_EVIDENCE_TUAN_5.md` §1 |
| Frontend | Jest **86/86** · tsc/lint/build exit 0 | 11/10 | `Tuan05/HANDOFF_TV4_TUAN5.md` §1 |
| Playwright | **40/40** ×2 (chống staging) | 11/10 | `Tuan05/HANDOFF_TV4_TUAN5.md` §2 |
| k6 | 102 VU · p95 **148.56ms** · 0% lỗi | 11/10 | `Tuan05/logs/w58_k6_vus100.log` |
| CI | Xanh (run `0a9b1a5`) | 07/10 | `Tuan05/SO_EVIDENCE_TUAN_5.md` §1 |
| PR tuần 5 | **PR #36 MERGED** (`D5-D7`→`main`, `mergedBy=Tson-dev`, 13/10); #32/#33 CLOSED | 13/10 | `gh pr view 32/33/36` |

> **Đính chính mốc 430**: dòng `430/430` ở bảng §1.1 chép nguyên từ plan §2.2. Đo thật trên nhánh tuần 6
> (14/10) ra **441/441** `CulinaryBlog.Tests` và báo cáo repo tại `400b49a` cũng ghi **446/446** (441+5).
> ⇒ "430" là mốc **giữa tuần 5**, đã bị thay bởi 446. Coverage tuần 5 ghi 97.07% (đo `0a9b1a5`); đo lại
> 14/10 = **96.17%** (vẫn vượt cổng 80%, không coi là hồi quy).

---

## 2. Bảng 24 ô kỹ năng — trạng thái kế thừa 14/10 (chưa ô nào được Tâm xác nhận)

> Nguồn: `Tuan05/SO_EVIDENCE_TUAN_5.md` §2 (cuối tuần 5). Tuần 6 chỉ **thêm bằng chứng mới** vào các ô
> mở lại (K04, K07, K16, K20, K21, K24), không tự đổi trạng thái ô khác.

| Ô | Trạng thái kế thừa | Việc tuần 6 có bổ sung bằng chứng |
|---|---|---|
| K01 Phân tích SRS/FR-NFR/ADR/contract | 🟡 có nền — mapping đã lập, chờ Tâm | W6-7 (nộp lại mapping + sổ) |
| K02 .NET10 Minimal API/REST/Scalar/RFC7807 | 🟢 gần đạt | — |
| K03 Clean Architecture/DI | 🟢 gần đạt | — |
| K04 CQRS/MediatR behaviors | 🟡 có nền | **W6-4** (phase `cqrs-behavior`) |
| K05 FluentValidation/Zod/RHF | 🟡 có nền (tuần 5 xác nhận Zod/RHF) | — |
| K06 EF Core/PG16 migration/index | 🟡 có nền | **W6-2** (migration chạy lặp) |
| K07 UoW/transaction/concurrency | 🟢 gần đạt | **W6-4** (`query-rollback` RowVersion) |
| K08 Identity/PBKDF2/JWT/refresh/logout | 🟢 đủ bằng chứng | — |
| K09 Google OAuth2/PKCE | ⬜ thiếu thật (chờ credentials) | W6-9 — vẫn ghi "còn chờ" |
| K10 RBAC/rate limit/HTTPS/CORS/secrets | 🟢 gần đạt | W6-6 (secrets instructions) |
| K11 FTS/GIN/ts_rank | 🟡 có nền | W6-9 (FTS chờ TV2) |
| K12 Redis cache-aside/invalidation/fallback | 🟢 gần đạt | — |
| K13 MinIO/S3 upload/delete/magic bytes | 🟢 đủ bằng chứng | — |
| K14 Hangfire/retry/persistence | 🟢 gần đạt | — |
| K15 SMTP/resize/sitemap XML | 🟢 gần đạt | — |
| K16 Next.js App Router/SSR/ISR/CSR | 🟡 có nền | **W6-4** (`image-opt`) |
| K17 TanStack Query/next/image/progress | 🟡 có nền | **W6-8** (demo media/progress) |
| K18 Responsive/WCAG | ⬜ thiếu thật — **thuộc TV2** | — |
| K19 SEO metadata/OG/JSON-LD/robots/sitemap | 🟢 đủ bằng chứng | W6-8 (demo SEO) |
| K20 Serilog/Seq/OTEL/health | 🟢 đủ bằng chứng | **W6-5** (`/metrics` exporter) |
| K21 xUnit/API/Jest/Playwright | 🟢 đủ bằng chứng | **W6-2** (regression), **W6-3** (5 luồng) |
| K22 k6/p95/p99/EXPLAIN/CWV | 🟡 có nền | — |
| K23 Docker/Compose/Nginx/volumes/backup/restore | 🟢 gần đạt | **W6-8** (demo ops) |
| K24 Git/PR/review/CI/secret scan/docs | 🟢 đủ bằng chứng | **W6-1** (baseline + PR), **W6-6** (release) |

**Tổng đầu tuần 6**: 🟢 đủ 6 · 🟢 gần đạt 8 · 🟡 có nền 8 · ⬜ thiếu thật 2 — **0/24 được Tâm xác nhận**.

---

## 3. Bằng chứng mới của tuần 6 (điền khi làm)

> Mỗi bản ghi dùng mẫu ở §4. Không có bằng chứng thì **để trống**, không viết "đã làm".

### 3.1. W6-1 — Baseline + PR tuần 6

| Ngày | Việc | Kết quả | Log/commit/PR |
|---|---|---|---|
| 14/10 | Baseline trên nhánh `2312739_NHTSon_D7` (`main`+`5e84995`) | ✅ đã đo — xem §1 | `logs/baseline_*.log` |
| 14/10 | Mở PR `2312739_NHTSon_D7` → `main` | ⬜ chưa mở (bước cuối W6-1) | link PR |

### 3.2. W6-2 — Regression toàn hệ thống

| Ngày | Việc | Kết quả | Log |
|---|---|---|---|
| 14/10 | Backend `dotnet test` | ✅ **441/441** + **5/5** `ConcurrencySpike`, Failed 0, Skipped 0 | `logs/regression_test.log` |
| 14/10 | Coverage gate | ✅ **96.17%** ≥ 80% (tuần 5 ghi 97.07%) | `logs/regression_coverage.log` |
| 14/10 | FE tsc/lint/build/jest | ✅ tsc/lint/build exit 0 · jest **86/86** | `logs/regression_frontend.log`, `logs/baseline_jest.log`, `logs/baseline_frontend_build.log` |
| — | E2E full suite chống staging ×2 (kỳ vọng ≥ 40/40) | ⬜ **để N2** | `logs/regression_e2e_run{1,2}.log` |
| — | Migration chạy lặp (DB sạch + nâng cấp) | ⬜ **để N2** | log `dotnet ef database update` |

### 3.3. W6-3 — Chốt 5/5 luồng G6

| Luồng | Spec | Do ai viết | Kết quả trên staging |
|---|---|---|---|
| register | `create-recipe.spec.ts` | TV4 | ⬜ |
| login | `create-recipe.spec.ts` | TV4 | ⬜ |
| category | `category.spec.ts` (**chưa có**) | **chờ TV2** | ⬜ (chờ TV2) |
| draft / create-recipe | `wizard-week4.spec.ts`, `create-recipe.spec.ts` | TV4/TV3 | ⬜ |
| publish | `recipe-publish.spec.ts` | TV4 | ⬜ |

### 3.4. W6-4 — Lab L5 còn lại + `cqrs-behavior`

| Việc | Kết quả | Bằng chứng |
|---|---|---|
| `image-opt` | ⬜ | log harness `practice/TV4/L5` |
| `query-rollback` | ⬜ (chờ TV3 account sở hữu công thức) | log harness |
| Phase `cqrs-behavior` (K04) | ⬜ | log harness |

### 3.5. W6-5 — `/metrics` exporter thật

| Việc | Kết quả | Bằng chứng |
|---|---|---|
| Quyết định exporter OTLP/Prometheus vs in-memory | ⬜ | `Program.cs` / `deploy/otel-collector-config.yaml` / runbook |
| `/metrics` 200 + số liệu scrape | ⬜ | `logs/w65_metrics.log` |

### 3.6. W6-6 — Release & bàn giao

| # | Mục §12.5 | Kết quả | Bằng chứng |
|---|---|---|---|
| 1 | 34/34 FR | ⬜ | bảng mapping |
| 2 | 30/30 NFR | ⬜ | `SO_EVIDENCE_TUAN_6.md` |
| 3 | 10/10 CONS + ADR | ⬜ | đối chiếu ADR |
| 4 | 4×24 K + reviewer | ⬜ (chờ Tâm) | W6-7 |
| 5 | 33 endpoint + OpenAPI | ⬜ | Scalar/OpenAPI |
| 6 | 5 E2E · coverage ≥ 80% | ⬜ | W6-2/W6-3 |
| 7 | Compose/Nginx/HTTPS/backup/restore | ⬜ | `docs/RUNBOOK.md` |
| 8 | README/Scalar/ADR/**CHANGELOG**/demo account | ⬜ | repo |
| 9 | Giới hạn hạ tầng | ⬜ | runbook |

### 3.7. W6-7 — 24 ô K + mapping + Tâm xác nhận

| Ngày | Việc | Kết quả | Bằng chứng |
|---|---|---|---|
| — | Cập nhật mapping + nộp sổ evidence | ⬜ | `../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` |
| — | **Tâm xác nhận từng ô + ghi ngày** | ⬜ **0/24** | reply/xác nhận Tâm |

### 3.8. W6-8 — Demo publish/media/ops

| Phần | Nội dung | Kết quả |
|---|---|---|
| Tính năng chính | draft → upload (progress) → nguyên liệu/bước → publish → unpublish/archive/delete | ⬜ |
| Kỹ năng phụ | trace Seq · `/metrics` · 2 API failover · TLS/HSTS | ⬜ |
| Giải thích | architecture/schema/test + xử lý lỗi Tâm chọn | ⬜ |
| Tự dựng | checkout sạch + restore backup + tìm trace | ⬜ |

### 3.9. W6-9 — Nợ nhỏ còn lại

| Việc | Kết quả | Bằng chứng |
|---|---|---|
| `npm audit` nâng major | ⬜ (chờ nhóm chốt) | `npm audit` |
| PR riêng `Program.cs` (sitemap header + `/metrics`) | ⬜ | link PR |
| Google OAuth K09 | ⬜ (chờ credentials) | — |
| `NFR-SEO-004` 301 đổi slug | ⬜ (nợ TV3) | — |
| K11 EXPLAIN FTS | ⬜ (chờ TV2) | — |
| Tâm hậu kiểm PR #36 | ⬜ | `gh pr view 36` |

---

## 4. Mẫu bản ghi evidence (giữ nguyên format các tuần trước)

```text
Tuần / Người / Task: 6 / Nguyễn Hữu Trung Sơn (TV4) / W6-x
FR/NFR/Kỹ năng: <mã FR/NFR>; K<...>
Trạng thái: Chưa làm | Đang làm | Chờ tích hợp | Chờ review | Hoàn thành
Đầu ra, đường dẫn code/config, PR/commit:
Test/lệnh chạy, môi trường, kết quả thực tế:
Minh chứng ảnh/log/video/coverage (không có secret):
Reviewer (Nguyễn Thanh Tâm) và ngày xác nhận:
Trở ngại, người hỗ trợ, hạn xử lý:
```

**Điều kiện một task hoàn thành** (DoD 12.3): đúng FR/ADR · UI/API/DB nối thật · quyền/validation/cache/job
phù hợp · test và CI pass · docs cập nhật · **Tâm duyệt** · có evidence trong sổ này.

---

## 5. Phụ thuộc thành viên khác (đối chiếu, không tính vào tiến độ TV4)

| Việc | Thành viên | Ảnh hưởng với TV4 |
|---|---|---|
| Luồng E2E `category` (spec + ai viết) | TV2 | W6-3 — thiếu thì G6 chưa đủ 5/5 |
| `query-rollback` — account E2E sở hữu công thức / `GetRecipesQuery` filter | TV3/Tâm | W6-4 — thiếu thì phase 4 L5 không chạy |
| Sửa `to_tsquery` (ADR 0003) | TV2 | W6-9 — EXPLAIN FTS chưa đo được |
| `NFR-SEO-004` (301 đổi slug) | TV3 | W6-9 — nợ ngoài TV4 |
| Google credentials | Nhóm | K09 không đạt được |
| Review PR riêng `Program.cs` + hậu kiểm PR #36 | Tâm | W6-9 — điều kiện DoD review |
| Chốt lịch backup / kho 30 ngày / rotate JWT | Tâm + TV2 | W6-6 ghi đúng giới hạn hiện tại |
| Xác nhận 24 ô K + mapping | Tâm | W6-7 — điều kiện G7 |

---

## 6. Quy tắc khi ghi sổ tuần 6

1. **Không copy số tuần 5** vào §1 hoặc §3 — số cũ chỉ được trích kèm nguồn + ngày đo (§1.1).
2. Bằng chứng phải **tái lập được**: ghi đúng lệnh, môi trường, commit.
3. Không ghi secret, token, khoá vào sổ (test `JwtSigningKeyNotCommittedTests` quét cả tài liệu `.md`).
4. Việc chưa làm để **⬜**, không ghi "hoàn thành một phần" trừ khi ghi rõ phần nào xong, phần nào thiếu.
5. Ô K chỉ đổi trạng thái khi có **cả 3**: code/config · test/log chạy thật · **Tâm xác nhận + ngày**.
