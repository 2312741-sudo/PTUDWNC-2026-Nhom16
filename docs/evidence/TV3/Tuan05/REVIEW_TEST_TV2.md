# T7 — Review kiểm thử của TV2 (Ngô Quốc Trường Vĩ), chỉ đọc

Theo `README.md` trên `origin/main` (bảng tuần 5, cột TV3): *"Review kiểm thử TV2 (Trường Vĩ)"*. Đây là review chéo kiểm thử giữa đồng nghiệp, **khác** với review PR chính thức (luôn là Nguyễn Thanh Tâm theo `PHAN_CHIA_CONG_VIEC_6_TUAN.md`). Không sửa file của TV2 — chỉ liệt kê, chạy, và nêu ca còn thiếu kèm gợi ý, giọng trung lập.

## 1. Kiểm kê test của TV2

### Backend (`tests/CulinaryBlog.Tests/`)

| File | Số test | Phạm vi |
|---|---|---|
| `CategoryTests.cs` | 9 | Domain Category (validate tên, slug tiếng Việt), CRUD handler, unique slug, chặn xoá khi còn recipe, soft delete khi rỗng, sắp xếp theo OrderIndex/Name, PaginationMeta |
| `DiscoveryAndSearchTests.cs` | 8 | `GetRecipes` chỉ trả Published + filter, `SearchRecipes` unaccent + từ khoá ngắn bị chặn, Google login tạo/link account, sitemap chỉ Published, phân trang, validator, filter+sort |
| `Week3DiscoverySearchCacheTests.cs` | 10 | Cache Redis cho categories/recipes (tạo/sửa/xoá thì invalidate), cách ly cache giữa Published/Draft, fallback khi Redis chết, FTS tiếng Việt có dấu, validator độ dài từ khoá, + 3 test "PersonalLab" (concurrency, hash, MIME — lab cá nhân, không thuộc phạm vi Category/Search) |
| `Week4DiscoveryAndPerformanceLabTests.cs` | 5 | Google token thật (hợp lệ/rỗng/sai dạng), word-boundary search tránh false positive, cache key ổn định |

**Tổng: 32 test**, tất cả đã PASS trong lần chạy đầy đủ 281/281 ở `KIEM_TRA_TUAN4.md` mục 3 (chạy chung, không tách riêng lại vì cùng `dotnet test CulinaryBlog.sln`).

### Frontend

- **Jest/RTL**: tìm trong `src/frontend/src` theo mẫu tên liên quan Category/Search/Discovery → **0 file**. Toàn bộ 14 suite Jest hiện có (85 test) đều thuộc wizard/dashboard/a11y của TV3.
- **Playwright E2E**: `src/frontend/e2e/` chỉ có `create-recipe.spec.ts` và `wizard-week4.spec.ts` (cả hai của TV3) — **0 file của TV2**.

## 2. Ca còn thiếu (nhận xét trung lập, không phải lỗi — là gợi ý)

1. **Không có test HTTP xác nhận `AdminPolicy` chặn người không phải Admin** trên `POST/PUT/DELETE /api/v1/categories`. Đọc `Program.cs:337-360`: cả 3 endpoint đều `.RequireAuthorization("AdminPolicy")`, nhưng `CategoryTests.cs` chỉ test ở mức handler (gọi trực tiếp `CreateCategoryHandler`, không qua pipeline HTTP/Authorization). Nếu policy bị gỡ nhầm hoặc đổi tên, test hiện tại vẫn xanh — không phát hiện được hồi quy ở lớp authorization. **Gợi ý**: thêm 1 test kiểu `RecipeIngredientHttpTests` của TV3 (qua `ApiFactory` + `HttpClient` thật) — Author thường gọi `POST /categories` → kỳ vọng `403`; khách ẩn danh → `401`.
2. **Không có Jest/RTL cho UI Category** (trang quản lý category, form tạo/sửa) dù theo `PHAN_CHIA_CONG_VIEC_6_TUAN.md` tuần 2 TV2 đã làm "UI Quản lý & Detail Category". Không rõ UI này có test riêng ở nơi khác hay chưa viết — cần hỏi TV2.
3. **Không có Playwright E2E cho luồng Category** (tạo category → gán vào recipe → lọc theo category ở trang khám phá) hay luồng **search** (nhập từ khoá → thấy kết quả → click vào recipe). README (mục kế hoạch tiếp theo) đã tự ghi nhận "3 luồng E2E còn lại: register/login (TV1), category (TV2), create-recipe (TV3)" — tức TV2 đã biết thiếu, không phải phát hiện mới, chỉ xác nhận lại ở đây.
4. **`Week3DiscoverySearchCacheTests.SearchRecipes_cache_isolation_never_returns_draft_or_archived_recipes`** test cách ly cache Draft/Archived tốt, nhưng không thấy test riêng cho trường hợp **recipe chuyển từ Published → Archived khi đang có cache** (tức sau khi archive, cache cũ của kết quả search còn chứa recipe đó có bị invalidate không, hay chỉ tự hết TTL). Có thể đã được `GetRecipes_and_SearchRecipes_cache_and_invalidate_properly` phủ một phần — cần hỏi TV2 để chắc.
5. **Google OAuth**: có test cho token hợp lệ/rỗng/sai dạng, nhưng không thấy test cho trường hợp **email Google đã tồn tại với tài khoản đăng ký thường (không phải Google)** — kịch bản "link account" có được test (`GoogleLogin_creates_author_or_links_account`), nhưng tên gộp 2 ca (creates OR links) trong 1 test — khó biết test có thật sự phủ cả hai nhánh assert riêng hay chỉ nhánh đầu. Gợi ý: tách thành 2 test riêng để rõ ràng hơn khi đọc báo cáo coverage.

## 3. Điểm làm tốt (ghi nhận, không chỉ nêu thiếu)

- `Cache_service_fallback_resilience_when_cache_server_down` — kiểm tra khi Redis chết hệ thống vẫn chạy được (fallback DB), đúng tinh thần NFR-REL.
- `SearchRecipes_supports_unaccented_vietnamese_and_and_filters` + `Word_boundary_search_helper_prevents_false_positives` — xử lý tiếng Việt không dấu và tránh match nhầm từ con, là hai lỗi tìm kiếm tiếng Việt thường gặp, TV2 đã chủ động test.
- `DeleteCategory_blocks_when_recipes_exist_and_soft_deletes_when_empty` — đúng quy tắc toàn vẹn tham chiếu (C09/FR-CAT-005), test rõ 2 nhánh.

## 4. Cách chạy lại (để Tâm/TV2 tự xác nhận)

```powershell
$env:TEST_DATABASE="Host=127.0.0.1;Port=5432;Database=culinary_test;Username=postgres;Password=postgres"
dotnet test CulinaryBlog.sln --filter "FullyQualifiedName~CategoryTests|FullyQualifiedName~DiscoveryAndSearchTests|FullyQualifiedName~Week3DiscoverySearchCacheTests|FullyQualifiedName~Week4DiscoveryAndPerformanceLabTests"
```
(Không tách chạy riêng trong phiên này — đã chạy chung với toàn bộ 281/281 ở `KIEM_TRA_TUAN4.md`, không lặp lại tốn RAM.)
