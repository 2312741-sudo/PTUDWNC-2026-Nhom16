# Mô tả PR soạn sẵn — TV3 Huỳnh Quốc Trung (chưa mở PR)

> Reviewer: Nguyễn Thanh Tâm (TV1). Thứ tự đề nghị: PR 1 (C4) merge trước, sau đó mở PR 2 (C7-frontend-tests) vào `main`
> — nhánh C7 mọc từ C4 nên khi C4 đã vào `main`, GitHub chỉ còn hiện các commit mới của C7. Không rebase, không force-push.
> Số test ghi dưới đây là lần chạy thật ngày 02/10/2026 trên đầu nhánh C7 (`1d64d66`, chứa toàn bộ commit của C4).
> Chưa chạy riêng trên đầu nhánh C4 (`abe2352`) — reviewer chạy lại theo mục "Cách kiểm".

---

## PR 1 — `2312786_HuynhQuocTrung_C4-wizard-rhf-zod` → `main`

**Tiêu đề:** `feat(C4): wizard soạn công thức dùng React Hook Form + Zod (K05); test C7 backend; fix C3 xoá bước`

### Tóm tắt
- **K05**: 4 bước wizard (`src/frontend/src/app/dashboard/recipes/_wizard/`) chuyển sang React Hook Form + Zod.
  Schema dùng chung ở `src/lib/recipe-schemas.ts`, khớp validator backend; output của resolver khớp đúng record backend
  (JSON strict, có kiểm tra kiểu lúc biên dịch `BasicInfoSchemaMatches`…). Bước 2/3 dùng `useFieldArray`, chặn chuyển bước khi còn dòng nháp chưa lưu.
- **C7 backend**: test `GetMyRecipesValidator`/MyRecipes, nhánh lỗi và quyền của Recipes, NetArchTest quy tắc phụ thuộc tầng (file mới,
  không sửa `ArchitectureTests.cs`), concurrency 2 writer + rollback transaction + child chéo recipe trên Postgres thật.
- **Fix C3**: xoá bước không còn 422 — unique `(RecipeId, StepNumber)` thành partial index `WHERE "IsDeleted" = false` (có migration
  `20260930113035_RecipeStepNumberUniqueIgnoresSoftDeleted`).

### Commit (9)
`519d6e6`, `04e01fa`, `784459c`, `58bdfa9`, `d46ea08`, `f2eb582`, `22654aa`, `6c5f3da`, `abe2352`.

### Ảnh hưởng
- Có **migration mới** (partial unique index) → `dotnet ef database update` trên DB đã có dữ liệu bước bị xoá mềm trùng số vẫn chạy được.
- Frontend thêm `react-hook-form`, `@hookform/resolvers`, `zod` (đã có trong `package-lock.json`).
- Không đổi hợp đồng API.

### Cách kiểm
```powershell
$env:TEST_DATABASE="Host=127.0.0.1;Port=5432;Database=culinary_test;Username=postgres;Password=$env:LAB_PG_PASSWORD"
dotnet format CulinaryBlog.sln --verify-no-changes --severity error; dotnet test CulinaryBlog.sln
cd src\frontend; npm ci; npx tsc --noEmit
```
Tay: `/dashboard/recipes/new` → bỏ trống tiêu đề → lỗi dưới ô; nhập dòng nguyên liệu chưa lưu rồi "Tiếp" → bị chặn.

### Checklist
- [x] Application không phụ thuộc EF Core (NetArchTest)
- [x] JSON body khớp record backend (không thừa/thiếu field)
- [x] Không có bí mật trong commit

---

## PR 2 — `2312786_HuynhQuocTrung_C7-frontend-tests` → `main` (mở sau khi PR 1 merge)

**Tiêu đề:** `test(C7): Jest/RTL + Playwright E2E wizard; K17 TanStack Query; K18 a11y; K19 JSON-LD đủ NFR-SEO-001; K22 SLOW_SQL + không N+1; fix wizard nhảy về bước 1; metric K20`

### Tóm tắt
- **Jest + RTL** (`jest.config.mjs`, bỏ qua `/e2e/`): schema Zod, RecipeWizard, IngredientsStep (sửa/xoá + `confirm`, hoàn tác khi lỗi),
  StepsStep (thêm/sửa/xoá/sắp xếp lại + hoàn tác), WizardRefresh, WizardA11y. **35/35 xanh.**
- **Playwright E2E** (`e2e/create-recipe.spec.ts`, chạy riêng `npm run e2e`): tự đăng ký user qua `POST /auth/register`
  (mật khẩu sinh trong test, nhập bằng `evaluate` để không lộ trong báo cáo HTML; trace tắt), đăng nhập UI, bước 1 → thêm/sửa/xoá nguyên liệu
  (dialog `confirm` huỷ rồi đồng ý) → thêm bước → xem lại → đối chiếu server. **3/3 xanh (lặp 3 lần), 2/2 sau K17, 2/2 sau K18.**
- **Fix C4 — wizard nhảy về bước 1** sau "Lưu & tiếp": `router.refresh()` chạy sau khi `history.replaceState` đã đổi URL sang `/edit`
  → Next render route edit → `EditRecipeClient` mount lại. Sửa: chỉ refresh khi pathname còn trùng lúc mount. Test đỏ `fc34f87` → xanh `64b29c1`.
- **K17 TanStack Query** (3 commit): `QueryClientProvider` riêng trong wizard (không đụng `app/layout.tsx`), dữ liệu công thức qua `useQuery`
  (`["recipe-detail", id]`), thao tác con qua `useMutation` (`onMutate` snapshot + optimistic, `onError` hoàn tác). Không đổi chữ ký `RunFn`
  nên các bước con không sửa. Đã thử đột biến bỏ dòng hoàn tác → 4 test đỏ.
- **K18 a11y**: focus về tiêu đề bước khi đổi bước, `role="status"` báo đang lưu, nút Sửa/Xoá có ngữ cảnh sr-only, `aria-sort` cột sắp xếp dashboard.
- **Backend**: `RecipeIngredientHttpTests` (4 test, Postgres thật): thêm/sửa/xoá giữ OrderIndex liên tục, tác giả khác 403, rowVersion sai 422,
  đổi nguyên liệu không đổi RowVersion recipe. Metric `culinary.recipes.created/updated` (`RecipeMetrics`, BCL) + 3 test.
- **K19 JSON-LD Recipe (NFR-SEO-001)**: HTML thật trang chi tiết thiếu `image` và `author`. Test đỏ `5930a41` → xanh `f0cc239`:
  DTO chi tiết thêm `authorName`/`categoryName` (`IRecipeDisplayNameReader`, 1 câu SQL, chỉ tên công khai); ảnh đường dẫn tương đối → URL tuyệt đối.
  Sau sửa HTML thật đủ 12/12 thuộc tính, không có `aggregateRating`/`review` (`Tuan04/K19_jsonld_chi_tiet.md`).
- **K22 (NFR-PERF-004)**: `SlowQueryInterceptor` cảnh báo `SLOW_SQL` khi lệnh > `Perf:SlowQueryMs` (mặc định 100 ms, không ghi tham số) — đỏ `227fcb0` → xanh `cbfdc01`.
  `RecipeQueryPerformanceTests` chứng minh số lệnh SQL mỗi request không tăng theo N (chi tiết, dashboard, thêm/xoá nguyên liệu và bước), báo cáo EXPLAIN khi đặt `K22_REPORT`;
  k6 `tests/k6/recipe-detail.js` (API p95 10.29 ms). Rủi ro ghi nhận: câu chi tiết bùng nổ tích Descartes (đề xuất `AsSplitQuery`, chưa làm).
- **Sổ minh chứng** `docs/evidence/TV3/BANG_MINH_CHUNG_K01_K24.md` (XONG 19 / CHƯA 5) + `Tuan04/TEST_THEO_LOP_chay_that.txt`.

### Commit (24, tính từ đầu C4)
`c2d349a`, `adbf808`, `ce2a057`, `af534c1`, `14022ef`, `b2c83f5`, `34324e0`, `fc34f87`, `64b29c1`, `0d4c368`, `e01c5f8`,
`b8a480b`, `e82cf0e`, `8f76f06`, `64e8f8b`, `1d64d66`, `5930a41`, `f0cc239`, `6050356`, `227fcb0`, `cbfdc01`, `b70f114`, `1b5c837`, `2b32e8c`.
(`ce2a057` là bản E2E cũ dùng `E2E_PASSWORD`, được `0d4c368` viết lại.)

### Ảnh hưởng / cần reviewer lưu ý
- `Program.cs` (file dùng chung): **thêm** đăng ký `IRecipeDisplayNameReader` và `SlowQueryInterceptor` (singleton, đọc `Perf:SlowQueryMs`), và gắn interceptor
  vào dòng `options.AddInterceptors(...)` của TV3; không sửa dòng của người khác.
- `RecipeDetailDto` thêm 2 trường cuối `authorName`, `categoryName` (mặc định null) — chỉ thêm, client cũ không ảnh hưởng.
- Thêm dependency `@tanstack/react-query` ^5.104 và devDependency Jest/RTL/Playwright → `package-lock.json` đổi; CI cần `npm ci`.
- **Chưa `AddMeter(RecipeMetrics.MeterName)`** trong API: chờ cấu hình OTel của TV4 trên `main`; sau khi merge thêm 1 dòng vào chỗ cấu hình metrics.
- E2E tạo user + 1 công thức Draft mỗi lần chạy trên DB đang dùng (không dọn tự động).
- Chưa có workflow CI cho frontend (cần hỏi Tâm trước khi thêm).

### Cách kiểm
```powershell
$env:TEST_DATABASE="Host=127.0.0.1;Port=5432;Database=culinary_test;Username=postgres;Password=$env:LAB_PG_PASSWORD"
dotnet format CulinaryBlog.sln --verify-no-changes --severity error; dotnet test CulinaryBlog.sln   # CulinaryBlog.Tests 214/214
cd src\frontend; npm ci; npx tsc --noEmit; npx jest                                                  # 43/43
# E2E: API (--migrate rồi --urls http://localhost:5080) + npm run dev, lần đầu npx playwright install chromium
npx playwright test
```
Kết quả đã chạy: DB rỗng `culinary_ci_check` 209/209 + 5/5 và coverage Application 90.9% (989/1088 dòng) — đo ở `1d64d66`;
sau các commit K19/K22 (HEAD `2b32e8c`): backend 214/214, Jest 43/43 (`Tuan04/TEST_THEO_LOP_chay_that.txt`). Chưa đo lại coverage và DB rỗng sau K19/K22.
Bằng chứng: `docs/evidence/TV3/Tuan04/` (C7_E2E_va_loi_wizard, K19_jsonld_chi_tiet, K22_*) trên nhánh SP.

### Checklist
- [x] Không sửa code thành viên khác (lỗi header "Quản trị DM" của TV2 → handoff)
- [x] Mật khẩu không nằm trong test, log, báo cáo Playwright
- [x] Jest và Playwright tách riêng
- [ ] NVDA kiểm tay (checklist `LAB_K18_checklist.md` trên nhánh lab, chưa làm)
- [ ] Validator ngoài cho JSON-LD (validator.schema.org, Rich Results Test) — Trung chạy tay
- [ ] Lighthouse trang chi tiết bản build — Trung đo tay
