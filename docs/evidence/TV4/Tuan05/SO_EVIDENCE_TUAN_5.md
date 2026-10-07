# SỔ EVIDENCE TUẦN 5 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Reviewer nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Nhánh**: `2312739_NHTSon_D5-D7` (từ `origin/main` = `262201b`) · **Lab**: `practice/TV4/L4`, `practice/TV4/L5`
> **Trạng thái 07/10**: mọi mục bắt đầu ở **Chưa làm**. Ô K chỉ chuyển "đủ bằng chứng" khi có
> **code/config + test + kết quả chạy thật**, và chỉ chốt khi **reviewer Tâm xác nhận + ghi ngày**.
> Ô nào thiếu ghi **Chưa làm**, không làm tròn số, không tự đánh dấu ✅.

> [!IMPORTANT]
> 1. **Không dùng số của tuần 4 làm số của tuần 5.** Số 426/426, 96.31%, 26/26, k6 0.00% là số đo
>    trên nhánh/điều kiện tuần 4 (`fd90572`) — tuần 5 phải đo lại trên `262201b` (W5-1) và trên
>    **staging** (W5-7, W5-8).
> 2. **Không lấy 4 file `Lab 04` ở `docs/evidence/TV4/` làm minh chứng của TV4** (do TV1 tạo ở
>    commit `80b2c0e`, quyết định nhóm 30/09).
> 3. **`docs/evidence/TV4/Report/` là báo cáo phi chính thống của TV4** — không dùng làm căn cứ
>    nghiệm thu cho bất kỳ ai.
> 4. Việc của thành viên khác (TV1/TV2/TV3) được ghi ở mục 5 để đối chiếu **phụ thuộc**, không tính
>    vào tiến độ của TV4.

---

## 1. Baseline tuần 5 — ⬜ CHƯA ĐO (việc W5-1)

> Lệnh và môi trường y hệt cách đo tuần 4 (`../Tuan04/SO_EVIDENCE_TUAN_4.md` §1) để so sánh được.
> Điền bảng này sau khi chạy xong trên `262201b`. **Không** copy số tuần 4 vào đây.

| Hạng mục | Lệnh | Kết quả | Ngày | Log |
|---|---|---|---|---|
| Build backend | `dotnet build CulinaryBlog.sln -c Release` | ⬜ | — | `logs/baseline_build.log` |
| Format | `dotnet format CulinaryBlog.sln --verify-no-changes` | ⬜ | — | `logs/baseline_format.log` |
| Test | `dotnet test CulinaryBlog.sln -c Release` | ⬜ | — | `logs/baseline_test.log` |
| Coverage `CulinaryBlog.Application` | `bash deploy/check-coverage.sh` | ⬜ | — | `logs/baseline_coverage.log` |
| Frontend typecheck | `npx tsc --noEmit` | ⬜ | — | `logs/baseline_frontend.log` |
| Frontend lint | `npm run lint` | ⬜ | — | `logs/baseline_frontend.log` |
| Frontend build | `npm run build` | ⬜ | — | `logs/baseline_frontend.log` |
| Secret scan | `bash deploy/scan-secrets.sh` (hoặc `git grep` nếu máy thiếu bash thật) | ⬜ | — | — |
| CI `Backend week 1` + `Frontend CI` | GitHub Actions | ⬜ | — | link run |

---

## 2. Bảng 24 ô kỹ năng — trạng thái kế thừa 07/10 (chưa ô nào được Tâm xác nhận)

> Nguồn: `../Tuan04/TRANG_THAI_THUC_HIEN_TUAN_4.md` §7 (cập nhật 05/10). Tuần 5 chỉ **thêm bằng chứng
> mới** vào các ô W5-x mở lại (K04, K05, K06, K10, K11, K12, K13, K17, K19, K22, K23, K24), không tự đổi
> trạng thái ô khác. *(`07/10` — bổ sung K04/K06/K11/K12/K13 sau rà soát nợ, xem §3.10.)*

| Ô | Trạng thái kế thừa | Việc tuần 5 có bổ sung bằng chứng |
|---|---|---|
| K01 Phân tích SRS/FR-NFR/ADR/contract | 🟡 có nền — mapping đã lập, chờ Tâm | Nộp lại mapping |
| K02 .NET10 Minimal API/REST/Scalar/RFC7807 | 🟢 gần đạt | — |
| K03 Clean Architecture/DI | 🟢 gần đạt | — |
| K04 CQRS/MediatR behaviors | 🟡 có nền | ✅ **W5-9** (phase `cqrs-behavior` — L5 tuần 4 chưa chạy) |
| K05 FluentValidation/Zod/RHF | 🟡 có nền | ✅ **W5-6**, **W5-9** (Zod/RHF FE) |
| K06 EF Core/PG16 migration/index | 🟡 có nền | ✅ **W5-8** (EXPLAIN re-run — `N2-6` bị cắt) |
| K07 UoW/transaction/concurrency | 🟢 gần đạt | — |
| K08 Identity/PBKDF2/JWT/refresh/logout | 🟢 đủ bằng chứng | — |
| K09 Google OAuth2/PKCE | ⬜ thiếu thật (chờ credentials) | W5-9 — vẫn ghi "còn chờ" nếu chưa có credentials |
| K10 RBAC/rate limit/HTTPS/CORS/secrets | 🟢 gần đạt | ✅ **W5-4** (HTTPS/HSTS/CORS) · **W5-10** (dọn secret A1) |
| K11 FTS/GIN/ts_rank | 🟡 có nền | ✅ **W5-8** (EXPLAIN FTS — phụ thuộc TV2 sửa `to_tsquery`) |
| K12 Redis cache-aside/invalidation/fallback | 🟢 gần đạt | ✅ W5-3 (cache chung ở staging) · **W5-8** (cache-hit ratio) |
| K13 MinIO/S3 upload/delete/magic bytes | 🟢 đủ bằng chứng | ✅ **W5-10** (test path traversal `../`) |
| K14 Hangfire/retry/persistence | 🟢 gần đạt | ✅ **W5-3** (số Hangfire server khi 2 API) |
| K15 SMTP/resize/sitemap XML | 🟢 gần đạt | — |
| K16 Next.js App Router/SSR/ISR/CSR | 🟡 có nền | W5-9 (image-opt/ISR của Lab L5) |
| K17 TanStack Query/next/image/progress | 🟡 có nền | ✅ **W5-6** (progress upload, nút trạng thái + Xóa) |
| K18 Responsive/WCAG | ⬜ thiếu thật — **thuộc TV2** | — |
| K19 SEO metadata/OG/JSON-LD/robots/sitemap | 🟢 đủ bằng chứng | ✅ **W5-8** (SEO + 301 trên staging) |
| K20 Serilog/Seq/OTEL/health | 🟢 đủ bằng chứng | ✅ **W5-8** (metrics scrape thật — `FR-OBS-003`) |
| K21 xUnit/API/Jest/Playwright | 🟢 đủ bằng chứng | ✅ **W5-7** (5 luồng G6) |
| K22 k6/p95/p99/EXPLAIN/CWV | 🟡 có nền | ✅ **W5-8** (k6 trên staging ≥100 VU + EXPLAIN) |
| K23 Docker/Compose/Nginx/volumes/backup/restore | 🟢 gần đạt | ✅ **W5-2, W5-3, W5-5** |
| K24 Git/PR/review/CI/secret scan/docs | 🟢 đủ bằng chứng | ✅ **W5-1** (baseline + PR) · **W5-10** (PR `practice/TV4/L4`, scan-secrets mở rộng) |

**Tổng đầu tuần 5**: 🟢 đủ 6 · 🟢 gần đạt 8 · 🟡 có nền 8 · ⬜ thiếu thật 2 — **0/24 được Tâm xác nhận**.

---

## 3. Bằng chứng mới của tuần 5 (điền khi làm)

> Mỗi bản ghi dùng mẫu ở mục 4. Không có bằng chứng thì **để trống**, không viết "đã làm".

### 3.1. W5-1 — Baseline + PR

| Ngày | Việc | Kết quả | Log/commit/PR |
|---|---|---|---|
| | | | |

### 3.2. W5-2 — Deploy staging (2 lần)

| Ngày | Việc | Kết quả | Log |
|---|---|---|---|
| | | | |

### 3.3. W5-3 — 2 API instance trên staging

| Ngày | Việc | Kết quả | Log |
|---|---|---|---|
| | | | |

### 3.4. W5-4 — HTTPS / HSTS / CORS / volumes

| Ngày | Việc | Kết quả | Cấu hình + header đo được |
|---|---|---|---|
| | | | |

### 3.5. W5-5 — Runbook `docs/RUNBOOK.md`

| Ngày | Việc | Kết quả | Đường dẫn |
|---|---|---|---|
| | | | |

### 3.6. W5-6 — D4-UI: progress upload + Unpublish/Archive/**Xóa**

| Ngày | Việc | Kết quả | Test/E2E |
|---|---|---|---|
| | | | |

### 3.7. W5-7 — 5 E2E flows trên staging

| Luồng | Spec | Do ai viết | Kết quả trên staging |
|---|---|---|---|
| register | `create-recipe.spec.ts` | TV4 (cần xác nhận) | ⬜ |
| login | `create-recipe.spec.ts` | TV4 (cần xác nhận) | ⬜ |
| category | chưa có | **cần TV2 xác nhận** | ⬜ |
| draft / create-recipe | `create-recipe.spec.ts`, `wizard-week4.spec.ts` | TV4/TV3 | ⬜ |
| publish | `recipe-publish.spec.ts` | TV4 | ⬜ |

### 3.8. W5-8 — Số đo load/SEO + số đo bị cắt tuần 4

| Ngày | Hạng mục | Số đo | Ghi giới hạn |
|---|---|---|---|
| | k6 ≥100 VU (p50/p95/p99, lỗi%) | | |
| | SEO (sitemap/robots/metadata/JSON-LD/301) | | |
| | EXPLAIN re-run (`N2-6`) | | |
| | Cache-hit ratio (K12) | | |
| | Metrics scrape thật (`FR-OBS-003`: count/duration/error) | | |

### 3.9. W5-9 — Lab còn thiếu + nợ kỹ thuật

| Việc | Kết quả | Bằng chứng |
|---|---|---|
| `N3-A3` Zod/RHF FE | ⬜ | |
| `N3-A5` Google OAuth | ⬜ (chờ credentials) | |
| **4** phase Lab L5 (`isr-detail`, `image-opt`, `search-ssr`, `query-rollback`) | ⬜ | |
| Phase `cqrs-behavior` (K04) | ⬜ | |
| `npm audit` | ⬜ | |

### 3.10. W5-10 — Đóng `BUG-W4-01/02/03` + nợ nhỏ P1/P2 *(bổ sung 07/10)*

| Việc | Kết quả | Bằng chứng |
|---|---|---|
| Đối chiếu + chốt `BUG-W4-01` (`ValueGeneratedNever` đã thấy ở `main` — `c624b9f`) | ⬜ | |
| Dọn secret còn lại: `appsettings*.json` (`Password=postgres`, `minioadmin`) + `.github/workflows/backend.yml` | ⬜ | |
| Mở rộng `deploy/scan-secrets.sh` (quét appsettings + `env:` workflow) + test | ⬜ | |
| `BUG-W4-03` — sửa trực tiếp trong W5-10 (chưa từng có bản vá trong dự án; `Program.cs` của TV1 → PR riêng) | ⬜ | |
| Mở PR cho `practice/TV4/L4` | ⬜ | |
| ADR soft-delete `Recipe` vs `Category` + đổi tên test (`BUG-W4-06`) | ⬜ | |
| Header `X-Sitemap-Generated` (`BUG-W4-09`) | ⬜ | |
| Test path traversal `../` (`NFR-SEC-004`) | ⬜ | |

---

## 4. Mẫu bản ghi evidence (giữ nguyên format các tuần trước)

```text
Tuần / Người / Task: 5 / Nguyễn Hữu Trung Sơn (TV4) / W5-x
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
| Luồng E2E `category` | TV2 | W5-7 — thiếu thì G6 chưa đủ 5 luồng |
| Checklist WCAG/responsive | TV2 | G6 có mục a11y — số đo do TV2, TV4 ghi dẫn chiếu |
| Jest/RTL frontend unit test | TV1 | Không chặn việc nào của TV4 |
| Rate limit phân tán (`BUG-W4-03` = `UseForwardedHeaders`) — TV4 sửa trực tiếp W5-10, cần Tâm review PR (đụng `Program.cs`) | Tâm (review) | Ảnh hưởng số k6 (W5-8) — chưa sửa xong thì phải ghi giới hạn |
| Chốt `BUG-W4-01` C1/C2 (đụng schema TV3) | Tâm + TV3 | W5-10 — đối chiếu trạng thái trên `main` trước khi chốt |
| `query-rollback` (RowVersion): cần account E2E **sở hữu** công thức / `GetRecipesQuery` filter chủ sở hữu | TV3/Tâm | W5-9 — thiếu thì phase thứ 4 của L5 không chạy được |
| K11 EXPLAIN FTS phụ thuộc sửa `to_tsquery` (ADR 0003) | TV2 | W5-8 — phần EXPLAIN FTS chưa đo được |
| Chốt lịch backup / kho 30 ngày / rotate JWT | Tâm + TV2 | W5-5 phải ghi đúng giới hạn hiện tại (backup giữ 7 ngày) |
| Google credentials | Nhóm | K09 / `N3-A5` không đạt được |

---

## 6. Quy tắc khi ghi sổ tuần 5

1. **Không copy số tuần 4** vào mục 1 hoặc mục 3 — số cũ chỉ được trích kèm nguồn + ngày đo.
2. Bằng chứng phải **tái lập được**: ghi đúng lệnh, môi trường, commit.
3. Không ghi secret, token, khoá vào sổ (test `JwtSigningKeyNotCommittedTests` quét cả tài liệu `.md`).
4. Việc chưa làm để **⬜**, không ghi "hoàn thành một phần" trừ khi ghi rõ phần nào xong, phần nào thiếu.
5. Ô K chỉ đổi trạng thái khi có **cả 3**: code/config · test/log chạy thật · **Tâm xác nhận + ngày**.
