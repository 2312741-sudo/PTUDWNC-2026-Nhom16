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
| Build | `dotnet build CulinaryBlog.sln -c Release` | **0W · 0E** | 14/10 · local | `logs/baseline_build.log` |
| Test backend | `dotnet test -c Release` | **446/446** (441 `CulinaryBlog.Tests` + 5 `ConcurrencySpike`), Failed 0 · Skipped 0 | 14/10 · local | `logs/regression_test.log` |
| Coverage | `bash deploy/check-coverage.sh` | **96.17%** ≥ 80% (Application) | 14/10 · local | `logs/regression_coverage.log` |
| FE typecheck/lint/build | `npx tsc --noEmit` · `npm run lint` · `npm run build` | exit 0 · exit 0 · exit 0 | 14/10 · local | `logs/regression_frontend.log` |
| FE unit | `npx jest --ci` | **86/86** | 14/10 · local | `logs/baseline_jest.log` |
| E2E | `npm run test:e2e` (×2, chống staging) | **44/46 ×2** — 2 lỗi = **drift spec/product có sẵn trên `main`** (xem §3.1) | 17/10 · staging + `next dev`:3000 | `logs/regression_e2e_run{1,2}.log` |
| k6 (nếu chạy lại) | `k6 run ...` trên staging | ⬜ *chưa chạy lại tuần 6* | — | `logs/k6_*.log` |

**Kết luận tuần 6**: ⬜ *(dự kiến **"đạt có ghi chú"**: backend 446/446 + FE 86/86 xanh; E2E 44/46 ×2 do 2 lỗi drift spec/product **không thuộc tuần 6** — §3.1; Playwright không nằm trong CI)*

---

## 2. Những gì cần bàn giao (theo mã việc)

| Việc | Nội dung bàn giao | Trạng thái | Bằng chứng |
|---|---|---|---|
| W6-1 | Baseline + **PR #37** `2312739_NHTSon_D7` → `main` đã mở, nhờ Tâm review | ✅ | [PR #37](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/pull/37) + `logs/baseline_*.log` |
| W6-2 | Regression backend/FE **không hồi quy** (446/446, jest 86/86); E2E **44/46 ×2** — 2 lỗi là drift spec/product ở `main`, **không do tuần 6** (§3.1) | ⚠️ | `SO_EVIDENCE_TUAN_6.md` §3.2 + `logs/regression_e2e_run{1,2}.log` |
| W6-3 | Bảng 5 luồng G6 (luồng `category` do ai, kết quả) | ⚠️ **blocked TV2** — không có `category.spec.ts` | §3.3, `SO_EVIDENCE` §3.3 |
| W6-4 | Lab L5 (`image-opt`, `query-rollback`) + phase `cqrs-behavior` (K04) | ⚠️ `cqrs-behavior` **✅ 5/5**; `image-opt` **còn nợ** (§3.3); `query-rollback` **chờ TV3** | §3.4, `logs/w64_lab_l5.log` |
| W6-5 | Quyết định `/metrics` exporter (làm hoặc lý do không) | ✅ **giữ in-memory, không thêm exporter** | §3.5, `logs/w65_metrics.log` |
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
| `/metrics` exporter thật | nợ HANDOFF tuần 5 §4.3 | ✅ chốt **in-memory** (W6-5, không thêm package) | TV4 (W6-5) |
| Google OAuth K09 | nợ từ tuần 3 | ⬜ | chờ **credentials** |
| `npm audit` nâng major | nợ W5-9 | ⬜ | chờ **nhóm chốt** |
| PR riêng `Program.cs` (sitemap header + `/metrics`) | nợ W5-10 | ⬜ | **Tâm** review |
| `NFR-SEO-004` (301 đổi slug) | nợ ngoài TV4 | ⬜ | **TV3** |
| K11 FTS `to_tsquery` (ADR 0003) | nợ W5-9 | ⬜ | chờ **TV2** |
| 24 ô K + mapping K01 | nợ nhiều tuần | ⬜ | **Tâm** xác nhận + ghi ngày |

### 3.1 Phát hiện ở W6-2 — lệch spec/product E2E `wizard-week4` (d)/(d2) *(chưa sửa — chờ nhóm/TV3 chốt)*

- **Hiện tượng**: `npm run test:e2e` chống staging (chạy **2 lần**): **44/46 pass**, 2 ca lỗi ổn định (không flaky):
  - `wizard-week4.spec.ts:103` ca **(d)** — kỳ vọng `getByRole('img', { name: 'Ảnh mới' })` có `src` khớp `/^https?:\/\/.+X-Amz-Signature=/`.
  - `wizard-week4.spec.ts:169` ca **(d2)** — cùng kỳ vọng tại dòng 207.
  - Thực tế `src = blob:http://127.0.0.1:3000/<uuid>` (locator thử 33 lần trong 15s, luôn ra `blob:`).
- **Nguyên nhân (đã truy vết — KHÔNG phải hồi quy tuần 6)**:
  - `src/frontend/src/lib/recipe-editor.ts:190` — `imageSrc(i) = mediaUrl(thumbnailUrl ?? mediumUrl ?? originalUrl ?? url)` ⇒ **luôn** trả đường dẫn **proxy** `${API}/resources/images/...`, **không bao giờ** trả `presignedUrl`.
  - `ImagesStep.tsx:60` render `<AuthImage src={proxy}>`; `AuthImage.tsx:33` fetch proxy kèm `Bearer` rồi `URL.createObjectURL(blob)` ⇒ `<img src="blob:...">` (hành vi **có chủ đích** theo D27/B5).
  - Lịch sử: `7da131f` (cũ) đổi spec (d)/(d2) sang kỳ vọng **presigned**; **`9f3dd4c` (W5-7, TV4)** đã **bỏ** UI presigned/`refreshUrl`/`isPresignedStale` khỏi `ImagesStep` và chuyển ảnh nháp sang **proxy→blob** (vì host ký `culinary-s3:9000` là host nội bộ, trình duyệt không mở được) — **nhưng không cập nhật 2 assertion presigned** ⇒ spec và product lệch nhau **có sẵn trên `main`**.
  - ⇒ Hai test này **không thể xanh trên `main` hiện tại**, bất kể môi trường/API (đúng cả khi chống API dev hay staging).
- **Đối chiếu tuần 5**: `Tuan05` ghi Playwright **40/40 ×2** (wizard-week4 10/10) — **mâu thuẫn** với code `main` hiện tại ⇒ nghi **drift evidence** tuần 5 (nhiều khả năng chạy trước `9f3dd4c`, hoặc chạy cấu hình khác). Đưa vào §4 để **Tâm** đối chiếu.
- **Ảnh hưởng CI**: Playwright **không** nằm trong `.github/workflows/backend.yml`/`frontend.yml` (CI chỉ restore/build/format/test + `tsc`/`lint`/`build`) ⇒ 2 lỗi này **không làm đỏ CI**; nhưng chặn việc tuyên bố **"E2E xanh 100%"** ở G6/G7.
- **Giải pháp đề xuất (chưa thực hiện) — sửa spec `src/frontend/e2e/wizard-week4.spec.ts`** (⚠️ file thuộc **TV3**, mã C7 — xuyên nhóm, phải TV3 đồng ý):
  - Ca (d) dòng ~103 và ca (d2) dòng ~207: đổi `await expect(img).toHaveAttribute("src", /^https?:\/\/.+X-Amz-Signature=/)` → khớp hành vi thật, ví dụ `await expect(img).toHaveAttribute("src", /^blob:http:\/\/127\.0\.0\.1:3000\//)`, **hoặc** chuyển sang kiểm "ảnh tải được thật": `await expect.poll(() => img.evaluate((el: HTMLImageElement) => el.complete && el.naturalWidth > 0)).toBe(true)` (dòng 208 đã có sẵn kiểu này). **Giữ nguyên** phần "ảnh đầu là ảnh chính" + "không 422".
  - **Tác động đến project (bắt buộc cân nhắc trước khi sửa)**:
    1. **Mất độ phủ PA-3 (IMAGE_CONTRACT §7b)**: bỏ assert presigned ⇒ hợp đồng `presignedUrl` **không còn ca E2E nào phủ**; các ô K liên quan ảnh nháp **giảm tính khả chứng** (chỉ còn unit test `ImagesStepPreview.test.tsx`). Phải ghi rõ ở `SO_EVIDENCE` + `MAPPING K`.
    2. **Đóng băng hành vi hiện tại**: ảnh nháp tiếp tục đi proxy→blob; UI **không còn** cảnh báo/gia hạn khi URL ký hết hạn (đã bỏ `isPresignedStale`/`refreshUrl` ở W5-7) ⇒ đây là **nợ UX** nếu sau này bật lại presigned.
    3. **Rủi ro quy trình**: sửa file TV3 mà không có sự đồng ý ⇒ lặp lại sự cố phân công như **PR #36** (merge không qua Tâm). Phải ghi vào §4 và chờ xác nhận.
    4. **Phương án thay thế (KHÔNG khuyến nghị)**: khôi phục PA-3 (dùng `presignedUrl` làm `src`) để giữ spec nguyên ⇒ **tái sinh** lỗi "ảnh không hiển thị" vì host ký `culinary-s3:9000` nội bộ — chính lý do W5-7 đã revert.
  - **Khuyến nghị**: nếu TV3 đồng ý → cập nhật spec theo phương án blob:/naturalWidth + ghi "PA-3 chỉ còn phủ bằng unit test"; nếu chưa kịp → **đánh dấu 2 ca là known-fail** và loại khỏi "điều kiện xanh" của G6/G7 (ghi minh bạch, không che).

### 3.2 Phát hiện ở W6-2 — model EF lệch migration (`PendingModelChangesWarning`) *(chưa sửa — việc điều phối migration của TV3)*

- **Bằng chứng** (`logs/w62_migration.log`):
  - **DB sạch** (localhost:5433, DB `culinary_migtest`): `dotnet run --project src/backend/CulinaryBlog.API -c Release -- --migrate` → **8/8** migration, `Database migrations applied successfully.`, **EXIT=0**.
  - **Chạy lặp idempotent**: chạy lại lệnh trên → **EXIT=0**, vẫn **8** dòng `__EFMigrationsHistory` (không áp lại).
  - **Drift**: `dotnet ef migrations has-pending-model-changes` → *"Changes have been made to the model since the last migration."*; `dotnet ef database update` (design-time) → ❌ `PendingModelChangesWarning`.
  - **Nội dung lệch** (probe `migrations add` rồi **revert sạch**): FK `FK_RecipeImages_Recipes_RecipeId` — model muốn **drop + add lại KHÔNG `onDelete: Cascade`**, còn migration cũ tạo **CÓ `ReferentialAction.Cascade`**.
- **Nguyên nhân**: `Program.cs:101` **cố ý** bỏ qua `PendingModelChangesWarning` (runtime migrate được, thinh lặng); `AuthDbContextFactory` (design-time) **không** bỏ qua ⇒ `dotnet ef database update` fail. Vì bị bỏ qua nên drift **không lộ ra** ở runtime → nguy hiểm vì DB thật đang là `ON DELETE CASCADE` cho `RecipeImages` trong khi model hiện tại không cascade (liên quan **soft-delete ADR-TV4-003**).
- **Tác động đến project**: (1) không thể dùng `dotnet ef database update`/kiểm tra migration nghiêm ngặt; (2) rủi ro **xóa Recipe → cascade xóa RecipeImages ở tầng DB** trái với ý định model; (3) K06 "EF Core/PG16 migration/index" **chưa thể coi là đạt**.
- **Đề xuất (chờ TV3/Tâm)**: TV3 xác nhận hành vi đúng cho `RecipeImages.RecipeId` (cascade hay không), rồi hoặc **bổ sung migration** mô tả đúng, hoặc **khôi phục `onDelete: Cascade` trong model** cho khớp DB; đồng thời cân nhắc thêm `ConfigureWarnings` tương ứng ở `AuthDbContextFactory` để tránh ẩn drift. **TV4 không tự thêm migration** (đổi schema là việc điều phối của TV3).

### 3.3 Phát hiện ở W6-4 — `image-opt` vẫn nợ; không thể đạt `rawImg == 0` nếu chưa đụng `AuthImage` *(chưa sửa — cần quyết định)*

- **Bằng chứng** (`logs/w64_lab_l5.log`): `image-opt` **FAIL 1/9** — **0 file** dùng `next/image`, **11 file** còn `<img>` thô; proxy ảnh `GET` trả **404**; không có biến thể `medium/large`; HTML runtime **không** có `/_next/image`; thiếu `loading/decoding` (CLS).
- **Vì sao TV4 không tự sửa**: (1) `src/frontend/src/app/dashboard/recipes/_wizard/AuthImage.tsx` **bắt buộc** dùng `<img>` thô — nó `fetch` proxy kèm `Bearer` rồi `URL.createObjectURL(blob)`, mà `next/image` **không tối ưu được `blob:`**; (2) chuyển ~10 component sang `next/image` là **thay đổi sản phẩm diện rộng** (ảnh hưởng ảnh chụp K18 của TV2, jest, cần `width/height`), rủi ro chéo giữa các thành viên; (3) check của lab L5 tự đếm `rawImg == 0` nên **không thể đạt** khi `AuthImage` còn `<img>`.
- **Tác động đến project**: CWV (`NFR-PERF-005` LCP/CLS) của trang công thức **còn nợ**; ảnh tải kích thước gốc thay vì resize theo màn hình; các ô K17/K18/K22 chưa thể đạt phần ảnh.
- **Đề xuất (chờ nhóm/Tâm quyết)**: **(A)** chuyển các component **không cần auth** (`components/Header|Footer|RecipeCard|RecipeDetailImage`, `app/dashboard/...`) sang `next/image`, giữ `AuthImage` `<img>` và **điều chỉnh check của lab** để loại trừ file auth; hoặc **(B)** giữ nguyên, ghi `image-opt` là **nợ đã biết** cho giai đoạn demo. **TV4 không tự đổi sản phẩm diện rộng khi chưa chốt.**

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
| 7 | Chốt xử lý lệch E2E (d)/(d2) §3.1: cho phép sửa spec `wizard-week4` (file **TV3**) hay giữ **known-fail** | ⬜ | — |
| 8 | Chốt hướng xử lý **drift model/migration** §3.2 (FK `RecipeImages` cascade): bổ sung migration (TV3) hay khôi phục cascade trong model — điều kiện **K06** | ⬜ | — |
| 9 | Chốt hướng xử lý **`image-opt`** §3.3: chuyển `next/image` (trừ `AuthImage`) + sửa check lab, hay giữ làm **nợ đã biết** | ⬜ | — |

---

## 5. Mốc tuần 5 để đối chiếu (KHÔNG dùng làm số tuần 6)

| Hạng mục | Số cuối tuần 5 | Ngày/Commit | Nguồn |
|---|---|---|---|
| `dotnet test -c Release` | **430/430** + **5/5** `ConcurrencySpike`, Skipped 0 | 11/10 | `Tuan05/HANDOFF_TV4_TUAN5.md` §1 |
| Build / format | 0W · 0E · format exit 0 | 11/10 | `Tuan05/HANDOFF_TV4_TUAN5.md` §1 |
| Coverage `Application` | **97.07%** ≥ 80% | 07/10 (`0a9b1a5`) | `Tuan05/SO_EVIDENCE_TUAN_5.md` §1 |
| Frontend | Jest **86/86** · tsc/lint/build exit 0 | 11/10 | `Tuan05/HANDOFF_TV4_TUAN5.md` §1 |
| Playwright | **40/40** ×2 (chống staging) — ⚠️ **không tái lập được**: chạy lại trên `main` hiện tại chỉ **44/46** (2 lỗi drift spec/product, xem §3.1) | 11/10 | `Tuan05/HANDOFF_TV4_TUAN5.md` §2 |
| k6 | 102 VU · p95 **148.56ms** · 0% lỗi | 11/10 | `Tuan05/logs/w58_k6_vus100.log` |
| PR tuần 5 | **PR #36 MERGED** (`D5-D7`→`main`, `mergedBy=Tson-dev`, 13/10); #32/#33 CLOSED | 13/10 | `gh pr view 32/33/36` |

---

## 6. Giới hạn số đo (kế thừa từ tuần 5 — dùng khi đọc số tuần 6)

| Số | Giới hạn đã biết | Ảnh hưởng | Ghi ở đâu |
|---|---|---|---|
| E2E `category` (5 luồng G6) | Chưa có spec của TV2 | G6/G7 chưa đủ 5/5 nếu TV2 chưa giao | §3, `TRANG_THAI` §3 |
| `image-opt` | Wave 2 (9 file `<img>` thô) | CWV tuần 6 vẫn có nợ | §3 |
| `query-rollback` | Không có account E2E sở hữu công thức | Phase rollback RowVersion không chạy | §3 |
| `/metrics` | **Đã chốt (W6-5):** giữ bộ đếm in-memory (reset khi restart), **không** thêm exporter (tránh đổi `packages.lock.json`/CI locked-mode) | Số scrape mang tính thời điểm; vẫn đủ FR-OBS-003 (200 + số liệu tăng thật) | §3.5, `SO_EVIDENCE §3.5` |
| k6 (nếu chạy) | Chạy trên 1 máy dev | VU/p95 là **proxy**, không phải load test sản xuất | §1 |
| Google OAuth | Chưa có credentials | K09 là **giới hạn đã biết**, không phải "đạt" | §3 |
| 24 ô K | Reviewer **chưa xác nhận** ô nào | Không tự đánh dấu đạt | §4 |
| E2E `wizard-week4` (d)/(d2) | **Drift spec/product có sẵn trên `main`**: spec kỳ vọng `src` presigned (`X-Amz-Signature`), product render proxy→blob (`imageSrc` không bao giờ trả `presignedUrl`) | Không thể tuyên bố "E2E 46/46"; G6/G7 phải ghi chú; xử lý §3.1 (đề xuất sửa spec — chờ TV3) | §3.1 |
| Model EF lệch migration (`PendingModelChangesWarning`) | `Program.cs:101` bỏ qua cảnh báo nên runtime không lộ; design-time `dotnet ef database update` fail; FK `RecipeImages→Recipes` model=no-cascade vs DB=Cascade | K06 chưa đạt; rủi ro cascade xóa ảnh; chờ TV3 (điều phối migration) | §3.2 |

---

## 7. Bước tiếp theo sau tuần 6
1. PR tuần 6 (#37) chờ Tâm review + 24 ô K được Tâm xác nhận (0/24) → điều kiện G7.
2. Lệch E2E (d)/(d2) §3.1: quyết định sửa spec wizard-week4 (TV3) hay giữ known-fail.
3. Lệch model/migration FK RecipeImages cascade §3.2: quyết định bổ sung migration hoặc khôi phục cascade (TV3) — K06.
4. image-opt §3.3: chọn (A) chuyển non-AuthImage sang 
ext/image + sửa check lab, hay (B) ghi nợ.
5. query-rollback Lab: chờ TV3 cung cấp account E2E sở hữu công thức.
6. Nợ nhóm: category.spec.ts (TV2), OAuth K09, 
pm audit major, NFR-SEO-004 (TV3), K11 FTS (TV2).

1. Nếu PR tuần 6 + 24 ô K được Tâm duyệt → **G7 đạt**: regression, release, tài liệu và minh chứng đủ 4 người.
2. Nợ còn lại chuyển sang giai đoạn demo/bảo vệ: luồng `category` (TV2), `NFR-SEO-004` (TV3), Google OAuth, `npm audit` major.
3. Chốt **tài khoản demo** + script dựng từ checkout sạch (A1 runbook) + lệnh backup/restore (A2) cho buổi bảo vệ.
4. Cập nhật CHANGELOG/README bản cuối và đối chiếu OpenAPI/Scalar 33 endpoint trước khi nộp.

> **Ghi nhớ khi phản biện demo**: nêu đúng các **giới hạn số đo** ở §6, không che; nhấn mạnh **NFR đã đo thật**
> (coverage, E2E ×2, k6 p95) và **nguồn gốc từng số**.
