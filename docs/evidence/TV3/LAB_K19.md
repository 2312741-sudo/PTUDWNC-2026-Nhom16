# LAB K19 — TV3: sitemap/robots/301 (L5) — nhánh practice/TV3/labs, KHÔNG merge

## Giả định (của TV3, không phải yêu cầu giảng viên)
Repo không có đề chi tiết. Tôi chọn: `GET /sitemap.xml` (chỉ Published), `GET /robots.txt` (có `Sitemap:`), trang chi tiết HTML
`GET /lab/l19/recipes/{slug}` có canonical + Open Graph; 301 khi (a) slug cũ sau khi đổi tiêu đề lúc còn Draft, (b) URL cũ dạng số ít `/recipe/{slug}`.
Sau khi Published slug giữ nguyên (theo D14). Base URL công khai lấy từ `Seo:BaseUrl` (mặc định `http://localhost:5090`).
Đổi tiêu đề chỉ yêu cầu đăng nhập, chưa kiểm chủ sở hữu (bảng `lab_recipes` của L3 không có owner).

## Mô tả
- `L19/SeoEndpoints.cs` (mới): robots.txt, sitemap.xml (XDocument, namespace sitemaps.org 0.9, `lastmod`), trang chi tiết, 301, `PUT /lab/l19/recipes/{id}/title`.
- Bảng `lab_slug_redirects(old_slug PK, recipe_id FK)` trong `LabDb.cs`: lưu **recipe_id** chứ không lưu slug đích → đổi slug nhiều lần thì slug đầu tiên vẫn 301 thẳng tới slug hiện tại (không có chuỗi 301→301).
- Đổi tiêu đề chạy trong transaction + `SELECT ... FOR UPDATE` (2 request đổi cùng lúc không đạp nhau), xong thì xoá cache Redis/OutputCache của L3.
- Slug: `SearchEndpoints.Slugify` (L3) — bỏ dấu (NFD + bỏ NonSpacingMark, `đ→d`), chữ thường, ký tự khác chữ/số → `-`, thêm hậu tố 6 hex chống trùng.
- Sửa: `Program.cs` (`MapL19Seo`), `appsettings.json` (`Seo:BaseUrl`), `LabDb.cs` (bảng redirect).
- Test `L19SeoTests.cs` (9 test). Test `Slug_khong_dau_chu_thuong_noi_gach_ngang` đã **xanh ngay ở commit đỏ** vì `Slugify` có từ L3 — giữ làm test bảo vệ NFR-SEO-004.

## Phần SP (không viết lại)
- JSON-LD Recipe trang chi tiết: `src/frontend/src/lib/recipe-jsonld.ts`, `app/recipes/[slug]/layout.tsx` (commit SP `15be516`, `a37d8f3`, nhánh C4).
- Sitemap/robots của sản phẩm thuộc TV4 (D4).

## Cách chạy
```powershell
. .\.secrets.local.ps1
$env:LAB_PG = "Host=localhost;Port=5432;Username=postgres;Password=$env:LAB_PG_PASSWORD"
dotnet test labs/TV3/Lab.TV3.Tests --filter "FullyQualifiedName~L19" -m:1
# Xem tay: dotnet run --project labs/TV3/Lab.TV3.Api ; curl.exe -i http://localhost:5090/robots.txt ; curl.exe -i http://localhost:5090/recipe/<slug>
```

## Kết quả thật
- Commit đỏ `32db275`: 8 FAIL (404) + 1 PASS (slug, lý do ở trên) — `LAB_K19_do.txt`.
- Commit xanh: `34220c5` — 9/9 PASS — `LAB_K19_xanh.txt`; cả bộ lab (Redis tắt, bỏ L3SearchTests) 69/69; format verify exit 0.
- Giới hạn: chưa validate sitemap bằng Google Search Console (chạy localhost); robots `Disallow: /lab/` + `Allow` cụ thể dựa vào quy tắc "luật dài nhất thắng" của Google.

## 4 câu tự kiểm tra
1. **Vì sao dùng 301 mà không phải 302?** — 301 là chuyển vĩnh viễn: Google chuyển tín hiệu xếp hạng sang URL mới và cập nhật index; 302 là tạm thời nên Google giữ URL cũ.
2. **Vì sao sau khi Published không đổi slug nữa?** — Link đã được chia sẻ/Google đã index; đổi slug làm gãy link và mất thứ hạng. Chỉ Draft (chưa ai thấy) mới đổi slug, và vẫn lưu slug cũ để 301 cho chắc.
3. **Sao lưu `recipe_id` trong bảng redirect?** — Để tra ra slug hiện tại tại thời điểm request: đổi A→B→C thì A vẫn 301 thẳng tới C, không phải A→B→C (chuỗi redirect làm chậm và bot có thể bỏ).
4. **Sitemap lọc Draft ở đâu, test kiểm thế nào?** — SQL `WHERE status = 'Published'`; test tạo 1 Published + 1 Draft cùng marker, parse XML bằng XDocument, kiểm loc có Published và không có Draft.
