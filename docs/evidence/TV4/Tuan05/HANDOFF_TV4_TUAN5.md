# HANDOFF TV4 — sau tuần 5 (W5-1 → W5-10)

- **TV4**: Nguyễn Hữu Trung Sơn (2312739) · **Reviewer**: Nguyễn Thanh Tâm
- **Nhánh**: `2312739_NHTSon_D5-D7` · **SRS**: v1.1.1
- **Ngày bàn giao**: 11/10/2026
- **Phạm vi bàn giao**: **9/10 khối xong** — W5-1, W5-2 (6/6), W5-3, W5-4, W5-5, W5-6, W5-8 (6/6),
  W5-10 (8/8); W5-7 **4/5**, W5-9 **2/5**. Còn lại 4 việc đều **blocked** (xem §4).

---

## 1. Trạng thái kiểm định lúc bàn giao

| Hạng mục | Kết quả |
|---|---|
| `dotnet build -c Release` | ✅ 0 warning / 0 error |
| `dotnet test -c Release` (toàn bộ) | ✅ **430/430** `CulinaryBlog.Tests` + **5/5** `ConcurrencySpike`, Failed 0, Skipped 0 |
| `dotnet format --verify-no-changes` (API) | ✅ exit 0 |
| `npx tsc --noEmit` (frontend) | ✅ exit 0 |
| `npm run lint` (frontend) | ✅ warning-only (pre-existing `no-img-element`) |
| Jest (frontend) | ✅ 86/86 |
| Playwright (E2E staging) | ✅ 40/40 (chạy lặp 2 lần) |
| CI GitHub Actions | ⏳ xác nhận run cho commit chốt sổ |

## 2. Những gì đã giao (theo mã việc)

| Mã | Nội dung | Bằng chứng |
|---|---|---|
| W5-1 | Baseline đo lại `0a9b1a5`: 426/426, coverage 97.07%, build/format/FE xanh + PR #32 | `SO_EVIDENCE_TUAN_5.md` §1 |
| W5-2 | Deploy lặp lại staging (run1+run2) + drill backup→restore 14 bảng + trace HTTP→EFCore→Postgres | `docs/RUNBOOK.md`, `logs/` |
| W5-3 | 2 API instance + nginx upstream (`zone` + `resolve`) + failover FR-OBS-001 | `SO_EVIDENCE_TUAN_5.md` §3.3 |
| W5-4 | HTTPS/HSTS/CORS + volumes (HTTPS/HSTS, `UseForwardedHeaders`) | `logs/w54_tls_cors.log` |
| W5-5 | Runbook 7 mục + mục Giới hạn | `docs/RUNBOOK.md` (commit `92e34be`) |
| W5-6 | Progress upload + Unpublish/Archive/**Xóa** + E2E | `SO_EVIDENCE` §3.6 |
| W5-7 | 4/5 E2E flows trên staging (còn `category` chờ TV2) | commit `9f3dd4c`; `w57_*` |
| W5-8.1 | k6 **102 VU**, p50 **82.56ms** / p95 **148.56ms** / p99 187.28ms, **0% lỗi** | `logs/w58_k6_vus100.log` |
| W5-8.2 | SEO staging: sitemap 96 URL, robots, JSON-LD, canonical/OG, 301 http→https | `logs/w58_seo_staging.log` |
| W5-8.3 | EXPLAIN re-run (list/category/search FTS/detail-by-slug) | `logs/w58_explain_staging.log` |
| W5-8.4 | Cache-hit ratio (mixed 70.31%; cached-only 100%) | `logs/w58_redis_cachehit.log` |
| W5-8.5 | `/metrics` Prometheus text (count/duration/error) + `RequestMetrics` | `logs/w58_metrics_header.log` |
| W5-8.6 | Ghi giới hạn số đo | `SO_EVIDENCE_TUAN_5.md` §3.8 |
| W5-10 | Đối chiếu `BUG-W4-01` + test hồi quy; `BUG-W4-02` scan-secrets; `BUG-W4-03` XFF; PR `practice/TV4/L4` #33; ADR-TV4-003; path traversal; header `X-Sitemap-Generated` | `logs/w58_metrics_header.log`, `SO_EVIDENCE_TUAN_5.md` §3.10 |

## 3. Việc reviewer cần xác nhận

1. **PR `Program.cs` (của TV1) — chờ Tâm review.** Ngoài `afdb4a7` (`BUG-W4-03`), đợt này thêm 2 thay đổi:
   - header `X-Sitemap-Generated` cho `/sitemap.xml` (`BUG-W4-09`);
   - endpoint **`/metrics`** + middleware `RequestMetrics` (FR-OBS-003).
   → Đề nghị gộp **một PR riêng** để tách khỏi nhánh tuần 5.
2. **`BUG-W4-01` C1/C2** (đụng schema TV3): TV4 chỉ **đối chiếu** + thêm test hồi quy, **chờ Tâm + TV3**.
3. **K11 EXPLAIN FTS**: chưa sửa `to_tsquery` (ADR 0003, chờ TV2) → phần FTS chưa đo được.
4. **`NFR-SEO-004`** (301 khi đổi slug): chưa có trong dự án → nợ **TV3**.
5. **`npm audit`** còn 44 vuln (**critical = 0**); các mục còn lại cần nâng **major** (`next@16`,
   `tailwindcss` 4.3.3, `jest` 30.x) → **đề xuất nhóm chốt**, không tự nâng major.

## 4. Giới hạn / nợ kỹ thuật (blocked)

| # | Việc | Ai / điều kiện | Ghi chú |
|---|---|---|---|
| 4.1 | W5-7 luồng E2E `category` | chờ **TV2** | 4/5 luồng còn lại đã xanh (40/40) |
| 4.2 | W5-9 Google OAuth | chờ **credentials** | không tự sinh client-id/secret |
| 4.3 | `/metrics` chưa có pipeline metrics OTLP | để tuần 6 | `RequestMetrics` giữ số **in-memory** (reset khi restart), **không** thêm package exporter để tránh đổi lock-file/CI locked-mode; muốn export thật cần thêm `OpenTelemetry.Exporter.Prometheus.AspNetCore` + pipeline metrics ở `deploy/otel-collector-config.yaml` |
| 4.4 | Lab L5 `image-opt`, `query-rollback` + `cqrs-behavior` | để W5-13/W5-14 | source ở branch `practice/TV4/L5`, ngoài working tree |

## 5. Giới hạn số đo (không phải khiếm khuyết mới)

- k6/EXPLAIN/cache-hit/metrics **đo trên staging**; chi tiết giới hạn ở `SO_EVIDENCE_TUAN_5.md` §3.8
  (planner seq scan do `Recipes` chỉ 167 dòng; cache-hit 70% là số **trộn** có scenario `list_uncached` cố ý).
- TLS staging **tự ký**; `NEXT_PUBLIC_SITE_URL=http://localhost`; SLA chưa đo; backup cục bộ 30 ngày.

## 6. Bước tiếp theo (tuần 6)

1. Tâm review PR `Program.cs` (header sitemap + `/metrics`).
2. Hoàn tất W5-7 `category` khi TV2 giao → chốt cổng **G6**.
3. Chuyển Lab L5 còn lại (`image-opt`, `query-rollback`) + `cqrs-behavior`.
4. Cân nhắc pipeline metrics OTLP/Prometheus exporter thật thay bộ đếm in-memory.
