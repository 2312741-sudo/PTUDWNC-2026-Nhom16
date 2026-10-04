# TRẠNG THÁI THỰC HIỆN TUẦN 4 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

- **SRS**: v1.1.1 (Approved 16/09/2026) · **Reviewer**: Nguyễn Thanh Tâm (Nhóm trưởng)
- **Nhánh**: `2312739_NHTSon_D5-D6-D7` từ `origin/main` = `7fe8fc2`
- **Kế hoạch**: [`KE_HOACH_TUAN_4_TV4.md`](KE_HOACH_TUAN_4_TV4.md) · **Mô tả việc**: [`MO_TA_CONG_VIEC_TUAN_4.md`](MO_TA_CONG_VIEC_TUAN_4.md) · **Sổ evidence**: [`SO_EVIDENCE_TUAN_4.md`](SO_EVIDENCE_TUAN_4.md)
- **Cổng nghiệm thu**: **G4** 24/24 ô kỹ năng K01–K24 · **G5** coverage ≥ 80% + sửa lỗi chặn/bảo mật + CI xanh

---

## 📌 Cập nhật 03/10/2026 — đọc trước kể cả bảng bên dưới

| Mục | Ghi cũ trong file | **Thực tế hiện tại** |
|---|---|---|
| Baseline nhánh | từ `origin/main` = `7fe8fc2` | Đã merge `origin/main` (`3d0695d`); HEAD = **`55b4c2b`** |
| Kiểm định | — | `210/210` pass (`205` + `5`), build 0 warning, `dotnet format` exit 0, `tsc`/`next build` exit 0 — **đã cũ**, xem dòng dưới |
| **Kiểm định mới nhất (03/10, sau B5/B6/QD3)** | — | ✅ `dotnet build CulinaryBlog.sln` **0 warning / 0 error**; `dotnet format --verify-no-changes` **exit 0**; `dotnet test` **265/265 pass**, Failed 0, **Skipped 0** (5 m 16 s); `npx tsc --noEmit` exit 0; `npm run build` ✅ `Compiled successfully`. ⚠️ Hai caveat: `deploy/scan-secrets.sh` **không chạy được** trên máy TV4 (thiếu bash/WSL: `execvpe(/bin/bash) failed`), đã quét tay bằng `git grep` — sạch; và `next build` in `fetch failed`/`ECONNREFUSED` do API không chạy lúc static fetch nhưng **exit 0**. ⚠️ `TracingObservabilityTests.…http_span_with_child_db_span` chạy song song toàn bộ thỉnh thoảng fail (assertion phụ thuộc thời gian), chạy lại riêng và chạy lại full đều xanh — flake có sẵn, **không** do thay đổi này |
| B1 / B2 / B4 | xem ở đề xuất | ✅ **Đã có code + test** (`9e786e7`, `a1311fa`) — ⛔ chờ reviewer duyệt `#20`/`#21`/`#22` |
| **B5** | ⏳ chờ quyết định | ✅ **Đã xong B5-1…B5-7** (PA-A): `IObjectStorageUrlSigner` + `MinioStorageService` presign (MinIO 7, hạn 10′/trần 15′), `PresignedUrl` ở `RecipeImageDto`/`RecipeImageSummaryDto` qua factory, `GetRecipeBySlugHandler` chỉ ký cho Draft/Archived của chủ hoặc Admin, `recipe-editor.ts` + `ImagesStep.tsx` (`no-referrer`, cảnh báo hết hạn), `docs/IMAGE_CONTRACT.md` §3/§7b, test `RecipeImagePresignedB5Tests` + `RecipeDetailPresignedB5Tests` — recipe **Published không bao giờ** trả URL ký |
| **B6** | ⏳ chờ quyết định | ✅ **Đã xong B6-1…B6-4** (PA-A): `PromoteAdminCommand` + `--promote-admin <email>` (`= <email>` cũng nhận, không phân biệt hoa/thường), **chỉ chạy ở `Development`**, idempotent, không đụng `DbSeeder`; `Program.cs` trả exit `2` usage/môi trường, `1` lỗi nghiệp vụ, `0` thành công; test `PromoteAdminCommandTests` + kiểm chứng tay từ chối ở `Testing`/`Production`. ⚠️ kiểm chứng tay đã promote `masterchef@culinary.local` và `trung.huynh@culinary.local` thành `Admin` trong DB local |
| **QD3** | ⏳ chờ quyết định | ✅ **Đã xong 3a/3b/3c trong repo**: xoá khoá khỏi cả `appsettings*.json`, `JwtSettings.Validate()` fail-fast + **từ chối khoá dev đã thu hồi**, `JwtSigningKeyNotCommittedTests` (6 test, đã cấy khoá thật để chứng minh bắt lỗi) chặn tái phát, `.env.example`/`README.md`/`HUONG_DAN_TEST_APP.md`/`HUONG_DAN_CAI_DAT_VA_CHAY_CHUONG_TRINH.md` hướng dẫn `openssl rand -base64 48`. ⚠️ khoá `.env` local đã đổi ⇒ **phiên đăng nhập cũ mất hiệu lực** |
| Block còn mở | B1, B2, B4, B5, B6 | **B5/B6/QD3-3a/3b/3c đã gỡ** và đã có code (03/10, PA-A). Còn lại **3 việc hạ tầng ngoài dự án**, mỗi việc 1 đề xuất: [`DE_XUAT_07`](../../../proposal/DE_XUAT_07_NOI_DAT_LICH_BACKUP.md) · [`DE_XUAT_08`](../../../proposal/DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md) · [`DE_XUAT_09`](../../../proposal/DE_XUAT_09_ROTATE_KHOA_JWT_DA_LO.md) — ⛔ TV4 **không** tự quyết (đề xuất 09: phần trong repo đã xong, phần rotate/xoá history nằm ngoài repo) |
| N2 / N3 / N4 | 0% | **Vẫn 0% — chưa thực thi.** Đã lập kế hoạch 3 giai đoạn, xem [`PLAN_TRIEN_KHAI_TV4_TUAN4.md`](PLAN_TRIEN_KHAI_TV4_TUAN4.md) |
| Phạm vi N4 | Runbook + release + bàn giao (4 việc) | ⛔ Đã lược: **runbook đầy đủ + deploy staging + TLS thuộc tuần 5**. Tuần 4 chỉ còn tài liệu + evidence. Xem [`PLAN_GIAI_DOAN_1_N2_N4.md`](PLAN_GIAI_DOAN_1_N2_N4.md) §0.2 |

> ⚠️ **Số `214/214` là của nhánh lab** `lab/TV4-audit-tuan4` (thêm 4 test hồi quy), ⛔ chưa merge.
> Baseline của nhánh này là **`210/210`**. Bảng dưới giữ nguyên số liệu gốc để làm căn cứ đối chiếu.

---

## 1. Tóm tắt tiến độ

| Nhóm | Task | Kế hoạch | Đã xong | Đang làm | Còn lại | Tỷ lệ |
|---|---|---|---|---|---|---|
| N0 | Mở đầu tuần 4 (baseline, bằng chứng 500, merge main, mapping K01) | 6 | 6 | 0 | 0 | 100% |
| N1 | D5 - Health/observability/backup/multi-instance | 8 | 8 | 0 | 0 | 100% |
| N2 | D7 - E2E, tinh công file, CI, sơ đồ | 8 | 0 | 0 | 8 | 0% |
| N3 | D6 - Lab L5 + bản mẫu 2 L4 | 4 | 0 | 0 | 4 | 0% |
| N4 | Runbook, release, bàn giao | 4 | 0 | 0 | 4 | 0% |
| **Tổng** | | **30** | **14** | **0** | **16** | **47%** |

> Tỷ lệ tính theo **số việc đã có bằng chứng**, không tính "đã lên kế hoạch".

---

## 2. N0 — Mở đầu tuần 4

| # | Việc | Trạng thái | Bằng chứng / commit |
|---|---|---|---|
| 1 | Tạo nhánh `2312739_NHTSon_D5-D6-D7` từ `origin/main` (`7fe8fc2`) | ✅ Xong | Nhánh có trên remote; commit `a4fc8d8` |
| 2 | Tạo `docs/evidence/TV4/Tuan04/` với 4 tài liệu | ✅ Xong | `KE_HOACH_TUAN_4_TV4.md`, `MO_TA_CONG_VIEC_TUAN_4.md`, `SO_EVIDENCE_TUAN_4.md`, `TRANG_THAI_THUC_HIEN_TUAN_4.md` |
| 3 | Commit `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md` vào nhánh này | ✅ Xong | `a4fc8d8` (nội dung **không sửa**) |
| 4 | Chạy lại baseline (build / format / test / coverage) và ghi số liệu thật | ✅ Xong 30/09 | **Build 0 warning/0 error** · **format exit 0** · **Test 178/178** (173 + 5, `Skipped=0`) · **Coverage `Application` line 83.37%**. Log: `Tuan04/logs/baseline_{build,format,test,coverage}.log` |
| 5 | Xác nhận lỗi `500` `/search` đã có bản sửa trên `main` (**không tự gỡ lỗi**) | ✅ Xong 30/09 | `e523579` đã là ancestor của `origin/main`; `page.tsx` còn **0** `onChange`; `SearchFilterSelect.tsx` có `'use client'`. Frontend build xanh: `npx tsc --noEmit` **exit 0**, `npm run build` **exit 0**. Log: `Tuan04/logs/baseline_frontend.log`. Chi tiết: `SO_EVIDENCE_TUAN_4.md` §1.1 |
| 6 | Merge `main` vào nhánh tuần 4 | ✅ Xong 30/09 | Merge `4770602` — kéo `1492b39` (TV2: sửa hiển thị ảnh + ô tìm kiếm ở `/recipes`) |
| 7 | Bảng mapping K01: FR/NFR ↔ ADR ↔ đường dẫn evidence cho 24 ô | ✅ Xong 30/09 | `docs/evidence/TV4/MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` (đặt ở gốc `TV4/`) — 24 dòng K01–K24, mỗi dòng có FR/NFR · ADR · đường dẫn evidence · commit · lệnh kiểm chứng · kết quả đo · reviewer. Kết luận: **0/24 ô đủ bằng chứng**, 9 ô có nền, 15 ô còn thiếu. **Chờ Tâm xác nhận** cột FR/NFR và ngày |

**N0 đã xong 6/6.** Phần chạy thật TC1–TC4 để lấy bằng chứng `/search` đã được chuyển sang làm cùng
**N2-2 (E2E luồng "tìm kiếm")** — vì bản sửa đã có trên `main`, chạy tay một lần không tạo ra bằng chứng
lặp lại được cho cổng CI.

**Ghi chú bổ sung (30/09)** — nằm ở **tài liệu trạng thái này**, không sửa báo cáo gốc: lỗi
`/search` đã được **TV2 sửa trên `main`** bằng commit `e523579` *"fix(frontend): extract
SearchFilterSelect to client component"* (đúng **Phương án B** trong báo cáo). File
`docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md` được **giữ nguyên 100%** như quyết định nhóm, để
giữ nguyên giá trị làm bằng chứng. Theo chỉ đạo 30/09, **không tự gỡ lỗi và không dựng app để
test tay** — bản sửa đã có trên `main`; việc lấy bằng chứng `/search` sẽ làm bằng E2E ở **N2-2**
để có kết quả lặp lại được trên CI.

---

## 3. N1 — D5: Health, observability, backup/restore, multi-instance

| # | Việc | Trạng thái | Bằng chứng | Ghi chú |
|---|---|---|---|---|
| 1 | `ObjectStorageHealthCheck` xác thực credential (B4) | 🟢 Xong 30/09 | `ObjectStorageCredentialProbe.cs`, `HealthTests` 18/18 | Probe `StatObject` phân biệt `AccessDenied` / `BucketNotFound` / `ObjectNotFound` |
| 2 | Test `/health/ready` → 503 khi Redis chết (thay dòng "200 hoặc 503" ở `HealthTests.cs:41`) | 🟢 Xong 30/09 | `HealthTests` 5/5 | Viết lại để kỳ vọng **dứt khoát** thay vì chấp nhận cả 200 lẫn 503 |
| 3 | OTEL collector + Serilog→Seq + **trace HTTP→DB thật** | 🟢 Xong 30/09 | `logs/seq_trace_recipes.log`, `TracingObservabilityTests` 2/2 | Sửa 2 lỗi làm app không khởi động được (xem handoff mục 5) |
| 4 | Script backup `pg_dump` 03:00 giữ 30 ngày + backup file + **drill restore** | 🟢 Xong 30/09 | `deploy/backup.sh`, `deploy/restore.sh`, drill 14 bảng | Lịch `0 20 * * *` UTC trong `.github/workflows/backup.yml`; cần secret `DATABASE_URL` |
| 5 | 2 API instance dùng chung cache/queue (hoặc ADR ghi giới hạn) | 🟢 Xong 30/09 | `logs/multi_instance_two_api.log` | 2 process thật sau nginx: 5/5 mỗi instance, cache dùng chung qua Redis |
| 6 | Sitemap cron 02:00 UTC + distributed lock (CR-7) | 🟢 Xong 30/09 | `SitemapLockTests` 3/3, `/sitemap.xml` 200 | Lock Redis chặn sinh trùng; phải đăng lịch **sau** `builder.Build()` |
| 7 | B1/B2 (500 → 503; fail-fast) | 🟢 Xong 30/09 — **chờ duyệt** | `StorageFailureContractTests` | B1: `503 storage.unavailable`, không retry. B2: `ValidateOnStart()` bỏ qua `Testing` |
| 8 | Bỏ secret hardcode khỏi `render.yaml` + secret scan CI | 🟢 Xong 30/09 | `deploy/scan-secrets.sh` pass | **Khoá JWT cũ còn trong git history, phải rotate ngoài repo** |

---

## 4. N2 — D7: E2E, tấn công file, CI, số đo

| # | Việc | Trạng thái | Bằng chứng | Ghi chú |
|---|---|---|---|---|
| 1 | Playwright thật (`playwright.config.ts` + `@playwright/test`) | ⬜ Chưa làm | — | Điều kiện cho 4 ô K |
| 2 | 5 luồng E2E (TV4 viết **publish** + **search**) | ⬜ Chưa làm | — | Search phủ TC1–TC12 của báo cáo 500 |
| 3 | Cổng CI frontend (`npm ci` → `tsc` → `next build` → `lint`) | ⬜ Chưa làm | — | Nguyên nhân gốc lỗi 500 lọt CI |
| 4 | Kịch bản tấn công file (size / MIME giả / hỏng / rate limit / ownership) | ⬜ Chưa làm | — | Hiện chỉ có unit test |
| 5 | Resilience có số liệu (DB/Redis/storage/worker/2 instance) | ✅ Xong 04/10 — `deploy/outage-drill.ps1` | K22, K24 | Redis/S3 dừng thật bằng `docker stop`; DB dùng instance trỏ port chết (thiếu quyền admin để `Stop-Service`) |
| 6 | Commit script k6 + EXPLAIN lại | ✅ Xong 04/10 — `tests/performance/read-load.js`; EXPLAIN thì **không** chạy lại (không đổi index) | K22 | 3 lần × ~3607 req, 0.00% lỗi |
| 7 | Ngưỡng coverage `Application` ≥ 80% trong CI | ⬜ Chưa làm | — | |
| 8 | D4-UI còn treo: progress upload + nút Unpublish/Archive + WCAG | ⬜ Chưa làm | — | Nợ từ tuần 3 |

---

## 5. N3 — D6: Lab

| # | Việc | Trạng thái | Bằng chứng | Ghi chú |
|---|---|---|---|---|
| A1 | Identity/PBKDF2 | 🟢 Có trong sản phẩm + test | `AuthTests.cs:108-111` | Assert **V3 / SHA512 / ≥100 000 vòng**. Theo quy tắc plan: dùng PR sản phẩm làm minh chứng, không viết lại lab cho đủ số file |
| A2 | Refresh hash/rotation/reuse/logout | 🟢 8/8 test | `Week3AuthAndPersonalLabTests.cs` | Rotation, **reuse ⇒ thu hồi cả family**, logout thu hồi, token hết hạn, refresh đồng thời |
| A4 | FTS tsvector/GIN/`ts_rank`/AND/phân trang | 🟡 Có trong sản phẩm + test | `RecipeConfiguration.cs`, `DiscoveryAndSearchTests.cs` | ⬜ EXPLAIN **đo lại tuần 4 chưa chạy** (index không đổi ⇒ `N2-C5` đã loại) |
| **A3** | **Zod/RHF phía FE** | ⬜ **Chưa làm** | `grep` `src/frontend` | `zod` có trong `package.json` nhưng **`src/` không import chỗ nào**; FE chỉ dùng `react-hook-form`. Phía BE đã có FluentValidation |
| **A5** | **Google OAuth2/PKCE** | ⬜ **Còn chờ credentials** | — | Không có mock nào được tính là hoàn thành |
| B1 | Lab L5 — `search-ssr` | ⬜ Chưa làm | — | Sản phẩm **đã là** Next.js App Router với `revalidate` (ISR), nhưng chưa có lab |
| B2 | Lab L5 — `isr-detail` | ⬜ Chưa làm | — | idem |
| B3 | Lab L5 — `query-rollback` | ⬜ Chưa làm | — | `useQuery`/`useOptimistic`: **0 file** trong `src/frontend` |
| B4 | Lab L5 — `image-opt` | ⬜ Chưa làm | — | `next/image`: **0 file** |
| B5 | Lab L5 — `seo` | ⬜ Chưa làm | — | Sản phẩm đã có `export const metadata`, sitemap 02:00 UTC, `robots.txt`, JSON-LD |
| B6 | Lab L5 — `observability` | ⬜ Chưa làm | — | Sản phẩm có Serilog + OTEL + Seq + health probes (đã kiểm ở N1/N2-C) |
| B7 | Lab L5 — `multi-instance` | ⬜ Chưa làm | — | Đã đo thủ công 2 API qua Nginx ở N1, chưa có compose profile |
| — | Nhánh `practice/TV4/L5` | ⬜ **Chưa tạo** | — | Đã **dời sang tuần 5** theo quy tắc ưu tiên trong `PLAN_GIAI_DOAN_1_N2_N4.md` mục 4: *"Nếu thiếu thời gian thì dời N3-B xuống tuần 5 trước"*. Lý do: `N2-C` vừa phát hiện và sửa **2 lỗi hạ tầng thật**, ưu tiên sửa lỗi sản phẩm trước khi làm lab |
| 3 | `SOK_LAB_L5.md` | ⬜ Chưa làm | — | Không tạo vì N3-B chưa có |
| 4 | Mở PR cho nhánh lab | ⬜ Chưa làm | — | `practice/TV4/L4` hiện chỉ có branch, chưa có PR → tuần 5 |
| **C3** | **Đính chính bảng 24 ô theo quy tắc code + test + log** | ✅ **Xong 04/10** | `SO_EVIDENCE_TUAN_4.md` mục 2 | Bảng cũ còn ghi "⬜ Chưa làm" cho K21/K22 dù đã có bằng chứng tuần 4 |

---

## 6. N4 — Runbook, release, bàn giao

| # | Việc | Trạng thái | Bằng chứng | Ghi chú |
|---|---|---|---|---|
| 1 | `docs/RUNBOOK.md` có số liệu thật | ⛔ **Thuộc tuần 5** | — | `N4-A` đã loại khỏi phạm vi GĐ1 (6-tuần L88: "load/SEO/**runbook**") |
| 2 | Deploy lặp lại được (compose prod hoặc checklist Render) | ⛔ **Thuộc tuần 5** | — | `N4-B` đã loại khỏi phạm vi GĐ1 |
| 3 | Cập nhật `HUONG_DAN_CHAY_TV4.md` + `README.md` + `CHANGELOG.md` | ✅ **Xong 04/10** | commit docs | HUONG_DAN: 154 → **311**, thêm lệnh coverage/E2E/k6/outage + bài học từ lỗi CI · README §4.5: 172 → **316/316** · CHANGELOG thêm `0.4.0` và `0.3.0` |
| 4 | Chốt sổ 24/24 K + nộp review Tâm | ✅ **Xong 04/10** (chờ Tâm xác nhận) | [`BAO_CAO_GIAI_DOAN_1_N2_N4.md`](BAO_CAO_GIAI_DOAN_1_N2_N4.md) | Báo cáo ghi rõ **3 ô còn thiếu thật** (K09/K17/K18) và **8 ô có nền nhưng thiếu phần lab/đo lại**. Không ô nào tự đánh dấu đạt |

---

## 7. Kỹ năng

Cập nhật 04/10/2026 theo quy tắc N3-C3 — chỉ tính ô khi có **code + test + log**:

| Mức | Số ô | Ô |
|---|---|---|
| 🟢 **đủ bằng chứng** | **3** | K08, K13, K21 |
| 🟢 **gần đạt, còn 1 việc nhỏ** | **10** | K02, K03, K07, K10, K12, K14, K15, K20, K23, K24 |
| 🟡 **có nền sản phẩm, thiếu phần lab/đo lại** | **8** | K01, K04, K05, K06, K11, K16, K19, K22 |
| ⬜ **còn thiếu thật** | **3** | K09 (chờ credentials), K17 (thiếu TanStack Query/next/image), K18 (thuộc TV2) |

- Ô **còn thiếu thật** giảm từ **15 → 3** so với đầu tuần 4.
- ⛔ **Không ô nào tự đánh dấu đạt** — toàn bộ 24 ô đang chờ **Nguyễn Thanh Tâm** xác nhận và ghi ngày.
- Chi tiết từng ô: `SO_EVIDENCE_TUAN_4.md` mục 2 · mapping 24 dòng: [`../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md`](../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md).

---

## 8. Rủi ro đang theo dõi

| Rủi ro | Mức độ | Xử lý |
|---|---|---|
| Không có hạ tầng test frontend nào (0 jest, 0 playwright, CI không build FE) | 🔴 Cao | Làm N2-1 và N2-3 trước các E2E khác; ưu tiên luồng `search` |
| Chưa có backup/restore | 🔴 Cao | ✅ Đã xong N1-4 — script chạy được, drill 14 bảng. ⛔ Còn thiếu: kho giữ **30 ngày** (artifact GitHub chỉ **7 ngày**) và job canh lịch. Xem [`DE_XUAT_08`](../../../proposal/DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md) |
| Cache in-process ⇒ số đo 2 instance không đáng tin | 🟡 | ✅ N1 đã dùng Redis shared; lab phát hiện **rate limit nhân theo số instance** + thiếu `UseForwardedHeaders` → phải ghi hạn chế khi đo |
| B1/B2/B4 chưa có quyết định nhóm | 🟡 | ✅ Đề xuất 01/02/04 đã có code + test; ⏳ chờ duyệt `#20`/`#21`/`#22` |
| B5/B6 chưa có quyết định nhóm | — | ✅ **Đã gỡ 03/10** — TV4 tự chốt PA-A và **tự thực hiện**, kể cả phần việc của TV3/TV1 (báo trước, không chờ). Xem [`QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md`](QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md) |
| 3 việc hạ tầng (lịch backup, kho 30 ngày, rotate JWT) chưa có quyết định | 🔴 Cao | 🔴 **Không tự quyết** — ngoài repo, có chi phí thật. Đã tách 3 đề xuất 07/08/09, chờ Tâm (+TV2). ⏳ TV4 làm được 3a/3b/3c trong repo |
| Chưa có tài khoản Admin để test `/hangfire`, DevConfigParityTests | 🟡 | ⏳ Chờ **B6** triển khai CLI `--promote-admin`; ⛔ không tự tạo admin để làm xanh |
| 15 ô kỹ năng trong 1 tuần | 🟡 | N2 và N3 làm song song; ô thiếu thì ghi thiếu, không đánh dấu đủ |
| `main` có 4 commit mới | 🟡 | Commit nhỏ + rebase `origin/main` trước khi mở PR |

---

## 9. Ghi chú nguồn gốc tài liệu `Lab 04` trong `docs/evidence/TV4/`

> [!NOTE]
> 4 file `TUAN_4.md`, `BAO_CAO_LAB_04.md`, `Lab04_2312739_NguyenHuuTrungSon.docx`,
> `Lab4_2312739_NguyenHuuTrungSon.docx` nằm trực tiếp trong `docs/evidence/TV4/` **không do TV4
> tạo**; chúng được tạo trong commit `80b2c0e` của **Nguyễn Thanh Tâm (TV1 — nhóm trưởng)**
> (cùng một commit sinh bộ 3 file tương ứng cho TV2 và TV3).
> Theo quyết định nhóm ngày 30/09: **giữ nguyên, không sửa, không xoá**, chỉ ghi chú nguồn gốc;
> **không dùng làm minh chứng cá nhân của TV4**. Tài liệu do chính TV4 viết: bộ 4 file trong
> `Tuan04/` này và `docs/evidence/TV4/HUONG_DAN_CHAY_TV4.md` (commit `c5361eb`).

---

## 10. Việc làm tiếp theo (thứ tự ưu tiên)

> ⭐ **Đính chính 03/10:** các mục 3–7 dưới đây **đã xong** trong N1 (xem §3). Thứ tự thực thi mới
> của GĐ1 là **B5 → B6 → QD3-3a/3b/3c → N2 → N3 → N4-C** — xem
> [`QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md`](QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md) §4.1 và
> [`PLAN_GIAI_DOAN_1_N2_N4.md`](PLAN_GIAI_DOAN_1_N2_N4.md) §4.

0. ⭐ **Trước tiên: B5-1…B5-7 → B6-1…B6-4 → QD3-3a/3b/3c.** Đây là **điều kiện tiên quyết** của N2:
   B6 mở khoá N2-E7, B5 mở khoá xem ảnh Draft trong wizard. ⛔ Không cắt để đổi — nếu thiếu thời gian
   thì **dời N3-B** xuống tuần 5.
1. ~~Bảng mapping K01: FR/NFR ↔ ADR ↔ đường dẫn evidence cho 24 ô~~ — **xong 30/09** (`docs/evidence/TV4/MAPPING_K01_FR_NFR_ADR_EVIDENCE.md`); phần còn lại là **Tâm xác nhận** cột FR/NFR và ngày cho từng ô.
2. ~~Dựng Playwright + cổng CI frontend (N2-A1, N2-A2)~~ — **xong 04/10**: Playwright + 12 test
   search E2E chạy thật (**12/12 pass**); ESLint 9 flat config + `npm run lint` trong CI
   (`.github/workflows/frontend.yml`), `npx eslint .` exit 0 và probe lỗi hook bị chặn (exit 1).
   ~~chạy `npm run build` + `tsc --noEmit` để có số liệu frontend cho baseline~~ — **đã chạy 30/09,
   exit 0** (`logs/baseline_frontend.log`); giờ cần đưa 2 lệnh này vào CI để có số liệu lặp lại mỗi
   lần merge. 🟡 Còn: CI **chưa** chạy Playwright (cần backend + Postgres + S3 trong job).
3. ~~B4 health check + test 503 khi Redis chết (N1-1, N1-2)~~ — **xong 30/09** (`a1311fa`).
4. ~~Backup/restore script + drill (N1-4)~~ — **xong 30/09** (drill 14 bảng). ⏳ Còn: kho 30 ngày +
   job canh lịch → đề xuất 07/08.
5. ~~OTEL collector + Seq + chụp trace thật (N1-3)~~ — **xong 30/09**.
6. ~~Sitemap cron 02:00 UTC + distributed lock (N1-6)~~ — **xong 30/09**.
7. Lab L5 + bù mục 2 L4; mở PR cho nhánh lab (N3).
8. ~~Kịch bản tấn công file (N2-E6)~~ — **xong 04/10**: `ImageMagicBytesE6Tests` **25/25**.
   ~~Race/idempotent/phân quyền ảnh (N2-E3, E4, E5, E7)~~ — **xong 04/10**:
   `ImageConcurrencyE7Tests` **5/5**, chạy lại 3 lần không flaky. 🟢 Phát hiện và **đã sửa 2 bug thật**
   (xoá ảnh không xoá dòng DB; `23505` unique index `ux_recipe_images_one_primary` gây 422) —
   chi tiết ở `SO_EVIDENCE_TUAN_4.md` §"Kiểm định N2". ✅ **E7 đã đóng 04/10**: kiểm chứng tay ở
   `Development` — anonymous **401** / Author **403** / Admin **200** (tài khoản probe tạm
    `e7.probe@culinary.local`, cần xoá trước khi bảo vệ). ✅ **N2-C xong 04/10** — xem mục 11.
9. ~~Ngưỡng coverage CI (N2-C6)~~ — **xong 04/10**: thêm `deploy/check-coverage.sh` + bước gate trong
   `.github/workflows/backend.yml`. `CulinaryBlog.Application` đang **84.35%** ≥ ngưỡng 80%.
   Probe âm (ngưỡng 90 → exit 1) và thiếu report (exit 1) đều đã kiểm chứng. ⏳ Còn: D4-UI (N2-D),
   CI chưa chạy Playwright (thiếu backend + Postgres + S3 trong job).
10. ~~Kịch bản tấn công file + quyền upload ảnh (N2-B3, N2-B4)~~ — **xong 04/10**:
   `src/frontend/e2e/upload-security.spec.ts` **10/10** (B3-1…B3-7, B4-1…B4-3), upload thật lên MinIO;
   toàn bộ Playwright **22/22**. 🟢 Phát hiện và **đã sửa 1 lỗ hổng thật**: JPEG 3 byte khớp trọn vẹn
   chữ ký nên được nhận `201` → thêm `ImageFormats.MinBytes = 64` (PNG hợp lệ nhỏ nhất 67 byte) + mã lỗi
   `file.too_small`, khoá bằng 4 test mới. 🟢 Sửa lần 2 lỗi `firstCategoryId()` dò **không giới hạn** →
   429 bị hiểu nhầm thành "danh mục hỏng" → `10/10 skipped` (xanh giả); nay giới hạn 12 lần dò và **ném
   lỗi 429** thay vì `skip` im lặng. Chi tiết ở `SO_EVIDENCE_TUAN_4.md` §"N2-B3/N2-B4".
11. ~~Resilience + k6 (N2-C1, C1b, C1c, C2, C3, C4)~~ — **xong 04/10**:
   - **C1/C2** `deploy/outage-drill.ps1` dừng lần lượt Redis → S3 → DB rồi đo trạng thái + thời gian
     phục hồi: Redis tắt đọc vẫn **200** (fallback in-process) + `/health/ready` **503**; S3 tắt
     media **404 → 503**; DB không truy cập được mọi endpoint **503 `database.unavailable`**.
     Phục hồi: Redis **0.2s** · S3 **0.5s** · API **3.5s**. Kill worker ⇒ connection refused (SPOF).
   - 🟢 Phát hiện và **đã sửa 2 lỗi thật**: (1) DB chết trả **500** vì `ApiExceptionHandler` không dò
     chuỗi `InnerException` mà `EfUnitOfWork` bọc Npgsql lại qua execution strategy → nay map
     `Npgsql`/`Socket`/`Timeout` thành **503**; (2) DB chết lúc khởi động làm `AddOrUpdate` lịch
     sitemap ném exception ra khỏi `Main` và **giết cả tiến trình** → nay bọc try/catch.
   - **C1c** sitemap **trước đây không có** `[AutomaticRetry]` ⇒ Hangfire không retry lần nào
     (mất sitemap cả ngày nếu DB chậm lúc 02:00 UTC). Nay sitemap **2**, resize **3**, xoá ảnh
     **3**, welcome **3** lần (lịch 0/1/5/30 phút) — khoá bằng `BackgroundJobRetryContractTests` 4/4.
   - **C3/C4** `tests/performance/read-load.js` **đã commit** (trước đây chỉ heredoc nên không ai tái lập
     được) + `README.md` runbook. Chạy **3 lần**, đều `FLUSHALL` trước: **~3607 req ≈ 120 req/s,
     0.00% lỗi**, p50 **6.23ms** / p95 **14.14ms** / p99 **25.06ms** (số của lần giữa).
   - ⚠️ **Hạn chế đã ghi rõ**: không dừng được PostgreSQL thật (thiếu quyền admin) nên phần DB dùng
     instance trỏ port chết; đo trên một node đơn nên "failover" thực chất là "mất dịch vụ".
   Chi tiết ở `SO_EVIDENCE_TUAN_4.md` §"TV4-K22" và §"TV4-K24".
11b. ~~Luồng E2E publish của TV4 (N2-B1)~~ — **xong 04/10**:
   `src/frontend/e2e/recipe-publish.spec.ts` **4/4**, toàn bộ Playwright **26/26**
   (publish 4 + search 12 + upload 10). `--retries=0` chạy **3 lần liên tiếp đều 26/26**,
   `recipe-publish --repeat-each=4` → **16/16**.
   🟢 **Tìm ra và sửa 1 lỗi sản phẩm thật**: wizard **mất bước khi lưu công thức mới** — `saveBasic`
   đổi URL sang `/dashboard/recipes/{id}/edit`, Next render lại nên `EditRecipeClient` dựng wizard mới
   với `step: 0`, người dùng bị **quay ngược về bước cơ bản** sau khi bấm "Lưu & tiếp" (và mất sạch
   phần đang gõ nếu đã sang bước sau). Sửa bằng cách mang bước trên URL `?step=` + `parseStepParam`;
   B1-3 giờ khẳng định wizard **tự** sang bước 2 sau lần lưu đầu. Bằng chứng trước khi sửa:
   `--repeat-each=6` → **5/6 đỏ**.
   🟢 `playwright.config.ts` giờ **tự bật backend** (chờ `/health/ready`) thay vì bắt mở terminal
   khác — vì API bị dọn giữa lúc chạy làm **14/26 test** đỏ với `ECONNREFUSED`, tức lỗi hạ tầng bị
   quy nhầm thành lỗi sản phẩm. CI dùng `E2E_START_BACKEND=0`.
   ⚠️ **Còn thiếu**: nút **Unpublish/Archive chưa có trong UI** (API đã có ở `Program.cs:620,627`) →
   gỡ xuất bản mới chỉ kiểm được ở tầng API; `register/login` (TV1), `category` (TV2),
   `create-recipe` (TV3) thuộc **G6 tuần 5**. Chi tiết: `SO_EVIDENCE_TUAN_4.md` §"TV4-K21".
12. Tài liệu + evidence + chốt sổ 24/24 K, nộp review Tâm (N4-C). ⛔ Runbook đầy đủ, deploy staging,
       TLS/HSTS, 5 E2E flows, responsive/a11y, Jest/RTL → **tuần 5**.

**Trạng thái build sau N2-B1 (04/10):** backend **311 + 5 = 316/316** ✅ · `dotnet format --verify-no-changes`
exit `0` ✅ · `npx tsc --noEmit` exit `0` ✅ · `npm run lint` exit `0` ✅ (chỉ cảnh báo `<img>` có sẵn từ trước) ·
Playwright **26/26**, 3 lần xanh ✅ · coverage `Application` **84.38%** ≥ 80% ✅.

**Trạng thái build sau N2-C (04/10):** backend **311 + 5 = 316/316** ✅ (thêm 12 test so với 304/304 trước đó) ·
`dotnet build` 0 warning/0 error ✅ · coverage gate `Application` **84.38%** ≥ 80% ✅ ·
frontend `tsc`/`lint`/`build` exit `0` ✅ · Playwright **26/26** ✅ ·
k6 **3/3 lần xanh**, `http_req_failed` **0.00%** ✅.
**Backend cũng 311/311 khi dựng lại đúng điều kiện CI** (không `Minio__*`, `Redis__Instance=ci`) ✅ —
đây là cách kiểm chứng lỗi do CI bắt, xem mục "lỗi thứ ba" trong `SO_EVIDENCE_TUAN_4.md` §TV4-K24.
**CI thật đã xanh** sau commit `8d9d62b`: `Backend week 1` run `37213966752` ✅ (`311/311` + `5/5`,
coverage gate pass) và `Frontend CI` run `37213966761` ✅.

**Trạng thái build sau N2-B3/B4 (04/10):** backend **299 + 5 = 304/304** ✅ · `dotnet format --verify-no-changes` exit `0` ✅ ·
`npx tsc --noEmit` exit `0` ✅ · `npm run lint` exit `0` ✅ (2 cảnh báo `<img>` có sẵn từ trước) ·
`npm run build` exit `0` ✅ · Playwright **22/22**, chạy **3 lần liên tiếp** đều xanh ✅ ·
coverage gate `Application` **84.38%** ≥ 80%, exit `0` ✅ · full backend suite **7/7 lần xanh** sau khi sửa race ✅.

🟢 Ngoài ra đã sửa **2 lỗi hạ tầng test** phát hiện khi chạy lặp (chi tiết ở `SO_EVIDENCE_TUAN_4.md`):
`dotnet test` ghi đè cache `dev:cache:` của môi trường Development (thiếu `appsettings.Testing.json`
nên test kế thừa `Redis.Instance = "dev"`) — đã tách sang `test:cache:`, cache dev được bảo chứng không
còn bị test đụng (25 → 25 sau full suite); và test tracing đỏ ngẫu nhiên
do `foreach` duyệt `List<Activity>` **ngoài `lock`** — đã chụp snapshot trong lock (trước 2/10 lần đỏ, sau **7/7 xanh**).
