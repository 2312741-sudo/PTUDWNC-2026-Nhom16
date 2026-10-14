# HANDOFF TV4 — TUẦN 6 → TUẦN 7 / BÀN GIAO NGHIỆM THU

- **Người bàn giao**: Nguyễn Hữu Trung Sơn (TV4, 2312739)
- **Người nhận / nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng, reviewer)
- **Nhánh**: `2312739_NHTSon_D7` (từ `main` = `e93fbe8`, đã gồm tuần 5 qua **PR #36**)
- **Task tuần 6**: **D7** — regression/release, demo publish/media/ops, chốt evidence · **Cổng G7**
- **SRS**: v1.1.1 · **Ngày lập**: 14/10/2026

> **Cách đọc**: Tài liệu này **tự chứa** — mọi việc kế thừa tuần 5 đã được chép lại kèm nguồn, không cần mở
> `Tuan05/`. Điền các ô `⬜` khi kết thúc tuần (bàn giao thực tế). Trước khi điền, đọc lại
> [`TRANG_THAI_THUC_HIEN_TUAN_6.md`](TRANG_THAI_THUC_HIEN_TUAN_6.md) + [`SO_EVIDENCE_TUAN_6.md`](SO_EVIDENCE_TUAN_6.md).

---

## 1. Trạng thái kiểm định lúc bàn giao

> ⬜ **Chưa điền** — điền vào cuối tuần 6 sau khi chạy W6-1/W6-2 trên nhánh `2312739_NHTSon_D7`.
> Mốc tuần 5 (để so, **không** dùng làm số tuần 6) ở §5.

| Hạng mục | Lệnh | Kết quả | Ngày · môi trường | Nguồn |
|---|---|---|---|---|
| Build | `dotnet build CulinaryBlog.sln -c Release` | ⬜ | — | `logs/baseline_build.log` |
| Test backend | `dotnet test -c Release` | ⬜ | — | `logs/regression_test.log` |
| Coverage | `bash deploy/check-coverage.sh` | ⬜ | — | `logs/regression_coverage.log` |
| FE typecheck/lint/build | `npx tsc --noEmit` · `npm run lint` · `npm run build` | ⬜ | — | `logs/regression_frontend.log` |
| FE unit | `npx jest --ci` | ⬜ | — | `logs/regression_jest.log` |
| E2E | `npm run test:e2e` (×2, chống staging) | ⬜ | — | `logs/regression_e2e_run{1,2}.log` |
| k6 (nếu chạy lại) | `k6 run ...` trên staging | ⬜ | — | `logs/k6_*.log` |

**Kết luận tuần 6**: ⬜ *(đạt / đạt có ghi chú / chưa đạt — kèm lý do)*

---

## 2. Những gì cần bàn giao (theo mã việc)

| Việc | Nội dung bàn giao | Trạng thái | Bằng chứng |
|---|---|---|---|
| W6-1 | Baseline + PR `2312739_NHTSon_D7` → `main` đã mở, nhờ Tâm review | ⬜ | link PR + log baseline |
| W6-2 | Kết quả regression (không hồi quy so với tuần 5) | ⬜ | `SO_EVIDENCE_TUAN_6.md` §3.2 |
| W6-3 | Bảng 5 luồng G6 (luồng `category` do ai, kết quả) | ⬜ | §3.3 |
| W6-4 | Lab L5 (`image-opt`, `query-rollback`) + phase `cqrs-behavior` (K04) | ⬜ | §3.4 |
| W6-5 | Quyết định `/metrics` exporter (làm hoặc lý do không) | ⬜ | §3.5 |
| W6-6 | Release checklist §12.5 + CHANGELOG + README + tài khoản demo | ⬜ | §3.6 |
| W6-7 | Mapping + sổ evidence + **số ô K Tâm xác nhận** | ⬜ | §3.7 |
| W6-8 | Bằng chứng demo publish/media/ops | ⬜ | §3.8 |
| W6-9 | Nợ nhỏ (npm audit major, PR `Program.cs`, OAuth, SEO-004, FTS, hậu kiểm PR #36) | ⬜ | §3.9 |

---

## 3. Việc kế thừa từ tuần 5 và trạng thái bàn giao

Tuần 5 đóng **52/56 việc (93%)**; phần còn lại được xử lý hoặc ghi rõ phụ thuộc ở tuần 6:

| Việc | Xuất xứ | Bàn giao tuần 6 | Người/điều kiện |
|---|---|---|---|
| Luồng E2E `category` (G6 5/5) | nợ W5-7 | ⬜ | chờ **TV2** giao `category.spec.ts` |
| Lab L5 `image-opt`, `query-rollback`, `cqrs-behavior` | nợ W5-9 | ⬜ | `query-rollback` chờ **TV3** (account sở hữu công thức) |
| `/metrics` exporter thật | nợ HANDOFF tuần 5 §4.3 | ⬜ | TV4 (W6-5) |
| Google OAuth K09 | nợ từ tuần 3 | ⬜ | chờ **credentials** |
| `npm audit` nâng major | nợ W5-9 | ⬜ | chờ **nhóm chốt** |
| PR riêng `Program.cs` (sitemap header + `/metrics`) | nợ W5-10 | ⬜ | **Tâm** review |
| `NFR-SEO-004` (301 đổi slug) | nợ ngoài TV4 | ⬜ | **TV3** |
| K11 FTS `to_tsquery` (ADR 0003) | nợ W5-9 | ⬜ | chờ **TV2** |
| 24 ô K + mapping K01 | nợ nhiều tuần | ⬜ | **Tâm** xác nhận + ghi ngày |

---

## 4. Việc reviewer (Tâm) cần xác nhận

| # | Mục cần xác nhận | Trạng thái | Ngày xác nhận |
|---|---|---|---|
| 1 | Review **PR tuần 6** (`2312739_NHTSon_D7` → `main`) | ⬜ | — |
| 2 | **Hậu kiểm PR #36** (đã merge với `mergedBy=Tson-dev`, không qua Tâm — vi phạm DoD review) | ⬜ | — |
| 3 | Xác nhận **từng ô K01–K24** + ghi ngày (điều kiện G7) | ⬜ **0/24** | — |
| 4 | Chốt 3 việc hạ tầng: lịch backup (đề xuất 07) · kho 30 ngày (08) · rotate JWT (09) | ⬜ | — |
| 5 | Review PR riêng `Program.cs` (header `X-Sitemap-Generated` + `/metrics`) | ⬜ | — |
| 6 | Cập nhật `MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` + đối chiếu sổ evidence tuần 6 | ⬜ | — |

---

## 5. Mốc tuần 5 để đối chiếu (KHÔNG dùng làm số tuần 6)

| Hạng mục | Số cuối tuần 5 | Ngày/Commit | Nguồn |
|---|---|---|---|
| `dotnet test -c Release` | **430/430** + **5/5** `ConcurrencySpike`, Skipped 0 | 11/10 | `Tuan05/HANDOFF_TV4_TUAN5.md` §1 |
| Build / format | 0W · 0E · format exit 0 | 11/10 | `Tuan05/HANDOFF_TV4_TUAN5.md` §1 |
| Coverage `Application` | **97.07%** ≥ 80% | 07/10 (`0a9b1a5`) | `Tuan05/SO_EVIDENCE_TUAN_5.md` §1 |
| Frontend | Jest **86/86** · tsc/lint/build exit 0 | 11/10 | `Tuan05/HANDOFF_TV4_TUAN5.md` §1 |
| Playwright | **40/40** ×2 (chống staging) | 11/10 | `Tuan05/HANDOFF_TV4_TUAN5.md` §2 |
| k6 | 102 VU · p95 **148.56ms** · 0% lỗi | 11/10 | `Tuan05/logs/w58_k6_vus100.log` |
| PR tuần 5 | **PR #36 MERGED** (`D5-D7`→`main`, `mergedBy=Tson-dev`, 13/10); #32/#33 CLOSED | 13/10 | `gh pr view 32/33/36` |

---

## 6. Giới hạn số đo (kế thừa từ tuần 5 — dùng khi đọc số tuần 6)

| Số | Giới hạn đã biết | Ảnh hưởng | Ghi ở đâu |
|---|---|---|---|
| E2E `category` (5 luồng G6) | Chưa có spec của TV2 | G6/G7 chưa đủ 5/5 nếu TV2 chưa giao | §3, `TRANG_THAI` §3 |
| `image-opt` | Wave 2 (9 file `<img>` thô) | CWV tuần 6 vẫn có nợ | §3 |
| `query-rollback` | Không có account E2E sở hữu công thức | Phase rollback RowVersion không chạy | §3 |
| `/metrics` | Giữ bộ đếm in-memory (reset khi restart) nếu không thêm exporter | Số scrape mang tính thời điểm | §3 |
| k6 (nếu chạy) | Chạy trên 1 máy dev | VU/p95 là **proxy**, không phải load test sản xuất | §1 |
| Google OAuth | Chưa có credentials | K09 là **giới hạn đã biết**, không phải "đạt" | §3 |
| 24 ô K | Reviewer **chưa xác nhận** ô nào | Không tự đánh dấu đạt | §4 |

---

## 7. Bước tiếp theo sau tuần 6

1. Nếu PR tuần 6 + 24 ô K được Tâm duyệt → **G7 đạt**: regression, release, tài liệu và minh chứng đủ 4 người.
2. Nợ còn lại chuyển sang giai đoạn demo/bảo vệ: luồng `category` (TV2), `NFR-SEO-004` (TV3), Google OAuth, `npm audit` major.
3. Chốt **tài khoản demo** + script dựng từ checkout sạch (A1 runbook) + lệnh backup/restore (A2) cho buổi bảo vệ.
4. Cập nhật CHANGELOG/README bản cuối và đối chiếu OpenAPI/Scalar 33 endpoint trước khi nộp.

> **Ghi nhớ khi phản biện demo**: nêu đúng các **giới hạn số đo** ở §6, không che; nhấn mạnh **NFR đã đo thật**
> (coverage, E2E ×2, k6 p95) và **nguồn gốc từng số**.
