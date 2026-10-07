# KẾ HOẠCH TUẦN 5 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

| Mục | Nội dung |
|---|---|
| Người lập | Nguyễn Hữu Trung Sơn (TV4 · 2312739) |
| Ngày lập | 07/10/2026 |
| Nhánh | **`2312739_NHTSon_D5-D7`** — tạo từ `origin/main` = `262201b` (merge PR #29, đã đóng tuần 4) |
| SRS | v1.1.1 (Approved 16/09/2026) |
| Task theo kế hoạch | **D5, D7** — "Tự deploy/restore, vận hành/HTTPS/backup review và số đo" |
| Reviewer / nghiệm thu | Nguyễn Thanh Tâm (Nhóm trưởng) — tuần 5 giao chính Tâm **review task TV4** |
| Cổng | **G6**: staging hoàn chỉnh · performance/SEO/a11y có kết quả **và giới hạn** · 5 E2E pass |

---

## 1. Căn cứ lập kế hoạch

| Nguồn | Nội dung lấy |
|---|---|
| [`PHAN_CHIA_CONG_VIEC_6_TUAN.md`](../../../PHAN_CHIA_CONG_VIEC_6_TUAN.md) mục 3.4 dòng tuần 5 | "Tự deploy/restore/test2 API; Nguyễn Thanh Tâm review; **HTTPS/CORS/volumes, load/SEO/runbook**" → đầu ra: "Staging đầy đủ; trace HTTP→DB; Nginx/health đúng; **số đo và giới hạn được ghi rõ**" |
| [`KE_HOACH_DU_AN.md`](../../../KE_HOACH_DU_AN.md) dòng 258 (G6) | "staging hoàn chỉnh; performance/SEO/a11y có kết quả và giới hạn; **5 E2E pass**" |
| `KE_HOACH_DU_AN.md` mục 8 (D5, D7) | D5: "Compose/Nginx, health/OTEL/metrics, persistent volumes, backup/restore/multi-instance" · D7: "Integration/UI/publish E2E, resilience/load/SEO, runbook và release" |
| `KE_HOACH_DU_AN.md` mục 12.3 (DoD) | Task chỉ xong khi có PR được review, test/CI pass, docs cập nhật, reviewer Tâm xác nhận |
| **Nợ chuyển tuần 5** của tuần 4 | [`PLAN_GIAI_DOAN_1_N2_N4.md`](../Tuan04/plan/PLAN_GIAI_DOAN_1_N2_N4.md) **§7** — danh sách đầy đủ, ghi lại ở §3 dưới đây để khỏi dò lại |

> **Nguyên tắc** (giữ nguyên của tuần 1–4):
> 1. Việc của thành viên khác nêu ở mục 6 — **không tính** vào tiến độ tuần 5 của TV4.
> 2. Lỗi/bug ghi vào báo cáo lỗi riêng, **không** ghi vào kế hoạch này.
> 3. Mỗi "đã làm" kèm `file:line` hoặc log. **Không** có mục "nên làm".
> 4. Ô kỹ năng chỉ chuyển ✅ khi có **code/config + test + kết quả chạy thật + Tâm xác nhận và ghi ngày**; không tự đánh dấu đạt.

---

## 2. Bối cảnh khi bắt đầu tuần 5 (đo 07/10, không dùng số cũ)

### 2.1. Việc của TV4 đã đóng ở tuần 4 (nguồn: `Tuan04/`)

| Khối | Kết quả |
|---|---|
| N0 + N1 | ✅ 14/14 — health probe credential thật, OTEL+Seq trace HTTP→DB, backup/restore drill 14 bảng, cache chung Redis, sitemap cron + khoá phân tán, B1/B2, bỏ secret khỏi `render.yaml` |
| N2 | 🟡 **7/8** — Playwright **26/26**, CI frontend, tấn công file `25/25` + `10/10`, outage drill, k6 0.00% lỗi, coverage gate `Application` **96.31%**. ⬜ Dở dang `N2-D1/D2` (**progress upload + nút Unpublish/Archive**) → **chuyển tuần 5** |
| N3 | 🟡 **3/4** — Lab L5 **63 check / 3 phase PASS**, `SOK_LAB_L5.md`. ⬜ Còn `A3` (Zod/RHF FE) và `A5` (Google OAuth — chờ credentials) → **chuyển tuần 5** |
| N4 | 🟡 **N4-C xong** (tài liệu + evidence + sổ 24 ô). ⛔ `N4-A` runbook đầy đủ + `N4-B` deploy staging/TLS → **đúng phạm vi tuần 5** |
| B5/B6/QD3 | ✅ Đã gỡ (presigned URL ảnh Draft, CLI `--promote-admin`, xoá khoá JWT khỏi repo + test chặn tái phát) |

### 2.2. Số liệu kiểm định làm baseline của tuần 5

| Hạng mục | Số liệu | Nguồn |
|---|---|---|
| `dotnet test -c Release` | **426/426 pass** (421 + 5 `ConcurrencySpike`), Skipped 0 | `Tuan04/TRANG_THAI_THUC_HIEN_TUAN_4.md` tại `fd90572` |
| Build / format | 0 warning · 0 error · `dotnet format --verify-no-changes` exit 0 | `Tuan04/TRANG_THAI_THUC_HIEN_TUAN_4.md` tại `fd90572` |
| Coverage gate | `CulinaryBlog.Application` **96.31%** ≥ 80% | `deploy/check-coverage.sh` + `backend.yml` |
| Frontend | Jest **85/85** · `tsc` / `lint` / `build` exit 0 | `Tuan04/TRANG_THAI_THUC_HIEN_TUAN_4.md` tại `fd90572` |
| Playwright | **26/26** (publish 4 + search 12 + upload 10), `--retries=0` 3 lần xanh | `src/frontend/e2e/` |
| k6 | 3 lần × ~3607 req, `http_req_failed` **0.00%**, p50 6.23ms · p95 14.14ms · p99 25.06ms | `Tuan04/logs/k6_c4_run{1,2,3}.log` |
| CI | Xanh mới nhất được xác nhận: `8a585ef` (Backend + Frontend) | `Tuan04/TRANG_THAI_THUC_HIEN_TUAN_4.md` §7 |

> ⚠️ **Việc làm ngay ngày đầu tuần**: chạy lại baseline trên `262201b` (sau merge PR #29) và xác nhận
> CI có run xanh cho `main` mới hay không — **không** được dùng số của `fd90572` làm số chính của tuần 5.

### 2.3. Ô kỹ năng (chưa ô nào được Tâm xác nhận — giữ nguyên để chờ)

| Mức | Số ô | Ô |
|---|---:|---|
| 🟢 đủ bằng chứng | 6 | K08, K13, K19, K20, K21, K24 |
| 🟢 gần đạt, còn 1 việc nhỏ | 8 | K02, K03, K07, K10, K12, K14, K15, K23 |
| 🟡 có nền sản phẩm, thiếu phần lab/đo lại | 8 | K01, K04, K05, K06, K11, K16, K17, K22 |
| ⬜ còn thiếu thật | 2 | K09 (Google OAuth — chờ credentials), K18 (thuộc TV2) |

Nguồn: `Tuan04/TRANG_THAI_THUC_HIEN_TUAN_4.md` §7 + `../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md`.

---

## 3. Danh sách việc tuần 5 (kế thừa đúng §7 của plan tuần 4 + mục 3.4 dòng tuần 5)

### 3.1. Bảng tổng hợp — 9 khối việc

| # | Khối | Việc | Mã việc tuần 4 | K | FR/NFR |
|---|---|---|---|---|---|
| **W5-1** | Chạy lại baseline + mở PR | Đo lại build/format/test/coverage/tsc/build trên `262201b`; mở PR cho nhánh tuần 5 | N0-4 (lặp lại) | K24 | `NFR-MAINT-003` |
| **W5-2** | **Deploy staging lặp lại được** | Chạy `docker-compose.staging.yml` 2 lần từ checkout sạch; đủ volumes/env/healthcheck; ghi giới hạn hạ tầng | `N4-B` | K23 | `NFR-SCALE-003`, `NFR-REL-001` |
| **W5-3** | **2 API instance trên staging** | Thêm `profiles:` + `culinary-api` thứ 2, upstream Nginx nhiều server, `proxy_next_upstream`, cache/queue dùng chung — lặp lại xác minh N1-5 ở staging | `N4-B3` (điều kiện G6 "test2 API") | K23 | `NFR-SCALE-001`, `NFR-SCALE-003` |
| **W5-4** | **HTTPS / HSTS / CORS / volumes** | TLS cho Nginx staging (chứng thư tự ký hoặc ghi rõ chưa có), `Strict-Transport-Security`, redirect HTTPS, `UseForwardedHeaders`, CORS origins explicit | `N4-B4` = **A3** tuần 4 | K10, K23 | **`NFR-SEC-005`** (đang 20% — thấp nhất trong 21 mã của TV4) |
| **W5-5** | **Runbook đầy đủ `docs/RUNBOOK.md`** | A1 dựng stack từ checkout sạch · A3 failover từng dependency · A5 lệnh k6 · A6 tìm log/trace theo TraceId · A7 tìm log Seq. A2/A4 đã có số liệu từ N1 → điền luôn | `N4-A` | K23, K24 | `NFR-REL-001`, `NFR-MAINT-003` |
| **W5-6** | **D4-UI còn treo từ tuần 3** | Thanh **progress % upload** + nút **Unpublish / Archive** trong dashboard (API đã có sẵn ở `Program.cs:620,627`; FE hiện **0 chỗ** có `onUploadProgress`, **0 nút** unpublish/archive) | `N2-D1`, `N2-D2` | K05, K17 | `NFR-USE-004` (đang 15%), `FR-RCP-006`, `FR-RCP-007` |
| **W5-7** | **5 E2E flows — cổng G6** | Đối chiếu 5 luồng (register · login · category · create-recipe · publish) với spec hiện có: `create-recipe.spec.ts` (đăng ký/đăng nhập/tạo công thức), `recipe-publish.spec.ts`, `search.spec.ts`; bổ sung luồng còn thiếu; chạy trọn bộ trên staging | `N2-B1` (nợ G6) | K21 | `FR-RCP-007`, `NFR-MAINT-002` |
| **W5-8** | **Số đo load/SEO trên staging** | k6 ≥100 VU có p50/p95/p99 **trên staging** (số tuần 4 đo ở dev); SEO: sitemap/robots/metadata/JSON-LD trên bản staging; ghi **giới hạn** (không tuyên bố SLA 99,5%) | D5/D7 (6-tuần L88 "load/SEO") | K22, K19 | `NFR-PERF-002`, `NFR-SEO-003`, `NFR-REL-001` |
| **W5-9** | **Lab còn thiếu + nợ kỹ thuật** | `N3-A3` Zod/RHF phía FE · `N3-A5` Google OAuth (**chờ credentials**) · sửa 3 phase Lab L5 chưa PASS (`isr-detail`, `image-opt`, `search-ssr`) · `npm audit` 10 vulnerability (9 high, 1 critical) | `N3-A3`, `N3-A5`, báo cáo tuần 4 | K05, K16, K17 | — |

> **Không thuộc TV4 trong tuần 5** (ghi rõ để không tính vào tiến độ): checklist WCAG/responsive (**TV2**),
> Jest/RTL frontend unit test (**TV1**), xoá lịch/kho backup 30 ngày và rotate khoá JWT (**Tâm + TV2**,
> xem đề xuất 07/08/09).

### 3.2. Chi tiết W5-1 → W5-9

#### W5-1 — Chạy lại baseline + mở PR (ngày 1)

| Việc | Cách làm | Bằng chứng | K/NFR |
|---|---|---|---|
| Đo lại toàn bộ trên `262201b` | `dotnet build` · `dotnet format --verify-no-changes` · `dotnet test` · `deploy/check-coverage.sh` · `tsc --noEmit` · `npm run lint` · `npm run build` | `Tuan05/logs/baseline_*.log` — số liệu ghi vào `TRANG_THAI_THUC_HIEN_TUAN_5.md` | K24 |
| Xác nhận CI trên `main` mới | Mở 2 workflow `Backend week 1` + `Frontend CI` của run mới nhất | Link run + trạng thái | K24 |
| Mở PR cho nhánh tuần 5 | PR `2312739_NHTSon_D5-D7` → `main`, mô tả dẫn kế hoạch này | Link PR (DoD 12.3) | K24 |

#### W5-2 + W5-3 — Deploy staging + 2 API instance (ngày 1–2)

| Việc | Cách làm | Bằng chứng | K/NFR |
|---|---|---|---|
| Chạy staging từ checkout sạch | `docker compose -f docker-compose.staging.yml up -d` → `/health/ready` 200, FE 200, `/sitemap.xml` 200 | Log lần 1 | K23 |
| **Idempotent 2 lần** | Dọn sạch volume → chạy lại lần 2 → cùng kết quả | Log lần 2 (2 file log, không được ghi đè) | K23 |
| Thêm profile 2 API | `profiles:` trong compose + `culinary-api-2`; `nginx.staging.conf` upstream ≥2 server + `proxy_next_upstream` | `nginx -t` hợp lệ; log chứng minh request trải đều | K23, `NFR-SCALE-001` |
| Cache/queue dùng chung ở staging | Redis shared giữa 2 API; khoá phân tán sitemap không sinh trùng | Log / test | K12, K23 |

> ⚠️ **Rủi ro đã biết từ tuần 4**: lab ghi nhận `container_name` cứng (không chạy song song 2 stack),
> rate limit **nhân theo số instance**, thiếu `UseForwardedHeaders`. Nếu W5-4 chưa xong thì khi đo
> **phải ghi rõ hạn chế**, không báo cáo số như "2 instance thật".

#### W5-4 — HTTPS / HSTS / CORS (ngày 2–3)

| Việc | Cách làm | Bằng chứng | K/NFR |
|---|---|---|---|
| TLS cho Nginx staging | Chứng thư tự ký (dev/staging) hoặc ghi rõ "chưa có chứng thư thật" | Cấu hình + log `curl -k https://` | `NFR-SEC-005` |
| HSTS + redirect HTTPS | `Strict-Transport-Security` + `return 301 https://$host` | Header trả về khi đo | `NFR-SEC-005` |
| CORS explicit origins + forwarded headers | `AllowedOrigins` từ env, `UseForwardedHeaders` + `KnownProxies` | Test/log | K10 |
| Ghi giới hạn thật | Không ngụ ý đạt TLS 1.2 production khi mới có self-signed | Mục "Giới hạn" trong runbook | `NFR-REL-001` |

#### W5-5 — Runbook `docs/RUNBOOK.md` (ngày 3–4)

7 mục theo `PLAN_GIAI_DOAN_1_N2_N4.md` N4-A: A1 dựng stack · A2 backup/restore (số drill 14 bảng) ·
A3 failover (số outage drill) · A4 2 API (`multi_instance_two_api.log`) · A5 lệnh k6 · A6 truy vết theo
TraceId · A7 tìm log Seq. **Mục nào chưa đo được ghi "chưa đo" kèm lý do** — không có mục "nên làm".

#### W5-6 — D4-UI: progress upload + Unpublish/Archive (ngày 3–4)

| Việc | Cách làm | Bằng chứng | K/NFR |
|---|---|---|---|
| Thanh progress % khi upload | Dùng `XMLHttpRequest`/`fetch` + `onUploadProgress` (hiện FE **0 chỗ** có) | Screenshot hoặc log % tăng dần | K17, `NFR-USE-004` |
| Nút Unpublish / Archive | API đã có `Program.cs:620,627` — chỉ thiếu UI ở dashboard | Test click → trạng thái đổi đúng; E2E được luồng gỡ xuất bản | K17, K19, `FR-RCP-006/007` |
| Không hồi quy | `recipe-publish.spec.ts` + `wizard-week4.spec.ts` vẫn xanh | `npx playwright test` | K21 |

#### W5-7 — 5 E2E flows (ngày 4–5)

| Luồng | Spec hiện có | Việc tuần 5 |
|---|---|---|
| register | `create-recipe.spec.ts` (mục "đăng ký, đăng nhập, tạo công thức…") | Xác nhận phủ đủ, tách/đánh dấu rõ |
| login | như trên | như trên |
| category | (thuộc TV2) | Đối chiếu với spec của TV2; thiếu thì ghi rõ **ai viết** |
| create-recipe / draft | `create-recipe.spec.ts`, `wizard-week4.spec.ts` | Giữ nguyên, chạy trên staging |
| publish | `recipe-publish.spec.ts` 4/4 | Chạy trên staging |

> Cổng G6 là **5 luồng pass** — TV4 chịu trách nhiệm **chạy toàn bộ bộ test trên staging** và báo đúng
> luồng nào do ai viết; không nhận phần của thành viên khác là của mình.

#### W5-8 — Số đo load/SEO (ngày 5)

- k6: `tests/performance/read-load.js` chạy trên **staging**, ≥100 VU, ghi p50/p95/p99 + `http_req_failed`.
- SEO: `sitemap.xml` (Published-only), `robots.txt`, metadata/canonical/JSON-LD trên trang staging.
- Ghi **giới hạn**: hạ tầng đo (máy nào, có qua proxy không), không tuyên bố `NFR-REL-001` 99,5%.

#### W5-9 — Lab còn thiếu + nợ kỹ thuật (song song, cắt trước nếu trượt tiến độ)

| Việc | Ghi chú |
|---|---|
| `N3-A3` Zod/RHF phía FE | `zod` có trong `package.json` nhưng `src/` chưa import chỗ nào |
| `N3-A5` Google OAuth/PKCE | ⛔ **Chờ credentials** — không coi mock là hoàn thành, ghi "còn chờ" |
| Sửa 3 phase Lab L5 chưa PASS | `isr-detail` (ISR không hoạt động), `image-opt` (9 file `<img>` thô), `search-ssr` (`no-store`) |
| `npm audit` | 10 vulnerability (9 high, 1 critical) — rà và ghi kết quả, phần cần nâng major thì ghi đề xuất |

---

## 4. Thứ tự và ngày dự kiến

> Nhóm **chưa chốt ngày bắt đầu/kết thúc tuần 5** trong tài liệu chung; bảng dưới dùng ngày dương lịch
> tham chiếu từ 07/10, nếu nhóm chốt lịch khác thì cập nhật lại.

| Ngày | Việc | Lý do xếp ở đây |
|---|---|---|
| **07/10** | W5-1 baseline + mở PR sớm · bắt W5-2 | PR càng sớm càng tốt (DoD 12.3); staging là điều kiện của mọi việc sau |
| **08/10** | W5-2 ×2 lần · W5-3 profile 2 API | Số liệu cần chạy lặp, làm sớm để có chỗ sửa |
| **09/10** | W5-4 HTTPS/HSTS/CORS · bắt W5-6 (D4-UI) | `NFR-SEC-005` đang 20%; D4-UI chặn luồng gỡ xuất bản |
| **10/10** | W5-5 runbook · W5-6 xong · W5-9 | Runbook lấy số liệu đã có của 07–09/10 |
| **11/10** | W5-7 5 E2E trên staging · W5-8 số đo · chốt sổ, nộp review Tâm | Cổng **G6** cuối tuần |

**Cắt nếu trượt tiến độ** — theo thứ tự: (1) W5-9 sửa 3 phase Lab L5 → tuần 6 · (2) W5-8 đo k6 nâng cao
(giữ 1 lần cơ bản) · (3) `npm audit`. **Không cắt**: W5-1, W5-2, W5-4, W5-5, W5-6, W5-7 — đây là các việc
tuần 4 đã ghi "chuyển tuần 5" và là điều kiện G6.

---

## 5. Cổng G6 — checklist nghiệm thu cuối tuần 5

- [ ] **Staging đầy đủ** chạy được từ checkout sạch, **2 lần liên tiếp**, có 2 log
- [ ] **2 API instance** trên staging qua Nginx, cache/queue dùng chung, `nginx -t` hợp lệ
- [ ] **HTTPS/HSTS/redirect** có cấu hình + bằng chứng đo; nếu chưa có chứng thư thật thì **ghi rõ giới hạn**
- [ ] **`docs/RUNBOOK.md` đủ 7 mục**, mỗi mục có số liệu thật hoặc ghi "chưa đo" + lý do
- [ ] **D4-UI**: thanh progress % upload + nút Unpublish/Archive hoạt động, có test/E2E
- [ ] **5 E2E flows pass** trên staging (register · login · category · create-recipe · publish)
- [ ] **Số đo load/SEO** có kết quả **và giới hạn** (k6 ≥100 VU p50/p95/p99 trên staging)
- [ ] **Baseline tuần 5**: build 0 warning · format exit 0 · test xanh · coverage ≥80% · `tsc`/`lint`/`build` exit 0
- [ ] **PR** của `2312739_NHTSon_D5-D7` đã mở và được **Tâm review**
- [ ] `TRANG_THAI_THUC_HIEN_TUAN_5.md` + `SO_EVIDENCE_TUAN_5.md` cập nhật, **ô nào thiếu ghi rõ thiếu**

---

## 6. Block / việc chờ nhóm (không tự quyết)

| Việc | Trạng thái | Cần ai | Nguồn |
|---|---|---|---|
| Nơi đặt lịch backup (cron host vs GitHub Actions) | 🔴 Chờ chốt | Tâm (+TV2) | [`DE_XUAT_07`](../../../proposal/DE_XUAT_07_NOI_DAT_LICH_BACKUP.md) |
| Kho lưu backup **30 ngày** (artifact GitHub chỉ 7 ngày) | 🔴 Chờ chốt | Tâm +TV2 | [`DE_XUAT_08`](../../../proposal/DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md) |
| Rotate/xoá khoá JWT đã lộ trong git history | 🟡 Trong repo đã xong 3a/3b/3c; phần ngoài repo chờ | Tâm | [`DE_XUAT_09`](../../../proposal/DE_XUAT_09_ROTATE_KHOA_JWT_DA_LO.md) |
| Google OAuth credentials (K09, `N3-A5`) | ⬜ Chờ credentials | Nhóm | `Tuan04/SO_EVIDENCE_TUAN_4.md` |
| Xác nhận 24 ô K + mapping (K01) | ⏳ Chờ Tâm xác nhận và ghi ngày | Tâm | `../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` |
| Rate limit phân tán sau Nginx (G1) — ảnh hưởng số đo k6 | 🔴 Việc của **TV1** (`Program.cs`); nếu chưa sửa phải ghi hạn chế khi đo | TV1 | `KE_HOACH_TUAN_4_TV4_V2.md` mục 7 |
| AI viết luồng E2E `category` | Cần TV2 xác nhận | TV2 | §3.2 W5-7 |

---

## 7. Tài liệu liên quan

| Tài liệu | Vai trò |
|---|---|
| [`TRANG_THAI_THUC_HIEN_TUAN_5.md`](TRANG_THAI_THUC_HIEN_TUAN_5.md) | Trạng thái thực hiện tuần 5 (cập nhật khi làm) |
| [`SO_EVIDENCE_TUAN_5.md`](SO_EVIDENCE_TUAN_5.md) | Sổ evidence tuần 5 |
| [`MO_TA_CONG_VIEC_TUAN_5.md`](MO_TA_CONG_VIEC_TUAN_5.md) | Mô tả chi tiết việc + DoD từng việc |
| [`../Tuan04/plan/PLAN_GIAI_DOAN_1_N2_N4.md`](../Tuan04/plan/PLAN_GIAI_DOAN_1_N2_N4.md) §7 | **Danh sách nợ chuyển tuần 5** — nguồn chính của kế hoạch này |
| [`../Tuan04/TRANG_THAI_THUC_HIEN_TUAN_4.md`](../Tuan04/TRANG_THAI_THUC_HIEN_TUAN_4.md) | Trạng thái đóng của tuần 4 + số liệu nền |
| [`../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md`](../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md) | Mapping 24 ô K — chờ Tâm xác nhận |
| [`../../../PHAN_CHIA_CONG_VIEC_6_TUAN.md`](../../../PHAN_CHIA_CONG_VIEC_6_TUAN.md) mục 3.4, dòng tuần 5 | Giao việc tuần 5 của TV4 |
| [`../../../adr/0004-staging-deployment-and-disaster-recovery.md`](../../../adr/0004-staging-deployment-and-disaster-recovery.md) | ADR staging/BCP-DR của TV1 (05/10) — căn cứ hạ tầng staging đã có |
