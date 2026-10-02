# LAB K16 — TV3: SSR search (L5) — nhánh practice/TV3/labs, KHÔNG merge

## Giả định (của TV3, không phải yêu cầu giảng viên)
Repo không có đề chi tiết cho phần LAB "SSR search". Tôi chọn phương án nhỏ nhất: trang `GET /lab/l16/search?q=&page=&pageSize=`
do chính Lab.TV3.Api (cổng 5090) render HTML phía server, dùng lại câu FTS của L3 (chỉ Published). Không dựng thêm Next.js trong lab;
phần Next.js App Router (CSR editor, ISR detail) đã có ở SP.

## Mô tả
- Server chạy truy vấn FTS (`SearchEndpoints.SearchDb`) rồi ghép HTML hoàn chỉnh → kết quả có sẵn trong HTML, không cần JavaScript.
- Encode HTML bằng `HtmlEncoder.Create(UnicodeRanges.All)`: giữ nguyên chữ tiếng Việt, nhưng `< > & " '` bị encode → chống XSS (cả tiêu đề lẫn từ khoá in lại).
- `<meta name="robots" content="noindex,follow">`: trang kết quả tìm kiếm không vào index, crawler vẫn đi theo link sang trang chi tiết.
- Phân trang bằng link `<a href>` thường (không cần JS), link chi tiết trỏ `/lab/l19/recipes/{slug}` (trang của K19).
- File: `labs/TV3/Lab.TV3.Api/L16/SsrSearchEndpoints.cs` (mới), `L3/SearchEndpoints.cs` (`SearchDb` private → internal), `Program.cs` (`MapL16SsrSearch`).
- Test: `labs/TV3/Lab.TV3.Tests/L16SsrSearchTests.cs` (5 test).

## Phần SP (không viết lại)
- ISR detail: `src/frontend/src/app/recipes/[slug]/page.tsx` (`export const revalidate = 300`), CSR editor: `dashboard/recipes/_wizard/*` (`"use client"`),
  nhánh SP `2312786_HuynhQuocTrung_C4-recipe-ui` / `..._C7-frontend-tests`.

## Cách chạy
```powershell
. .\.secrets.local.ps1
$env:LAB_PG = "Host=localhost;Port=5432;Username=postgres;Password=$env:LAB_PG_PASSWORD"
dotnet test labs/TV3/Lab.TV3.Tests --filter "FullyQualifiedName~L16" -m:1
# Xem tay: dotnet run --project labs/TV3/Lab.TV3.Api  ->  http://localhost:5090/lab/l16/search?q=pho  (tắt JS vẫn thấy kết quả)
```

## Kết quả thật
- Commit đỏ: `a567013` — 5/5 FAIL (`GET /lab/l16/search -> 404`) — `LAB_K16_do.txt`.
- Commit xanh: xem `BAO_CAO_TUAN4.md` — 5/5 PASS — `LAB_K16_xanh.txt`; cả bộ lab (Redis tắt, bỏ L3SearchTests) 60/60; L3SearchTests với Redis 6399: 8/8.
- `dotnet format --verify-no-changes --severity error` cho 2 csproj lab: exit 0.
- Giới hạn: chưa đo TTFB/Lighthouse cho trang này (xem K22); không cache trang SSR (mỗi request đều chạy SQL).

## 4 câu tự kiểm tra
1. **SSR khác CSR ở điểm nào trong lab này?** — SSR: server chạy SQL và trả HTML có sẵn kết quả; CSR: trình duyệt tải JS rồi mới gọi API và vẽ. Test chứng minh bằng cách đọc HTML thô (HttpClient không chạy JS) vẫn thấy tiêu đề công thức.
2. **Vì sao dùng `HtmlEncoder.Create(UnicodeRanges.All)` mà không dùng `HtmlEncoder.Default`?** — Default chỉ cho phép Basic Latin nên "Phở" thành `Ph&#x1EDF;`; All giữ chữ Việt nhưng vẫn encode ký tự nguy hiểm `< > & " '`.
3. **Vì sao trang tìm kiếm để `noindex,follow`?** — Trang kết quả có vô số tổ hợp `q`, nội dung trùng lặp; không index nhưng vẫn cho bot theo link đến trang chi tiết (trang đó mới cần index, có trong sitemap K19).
4. **Làm sao chắc Draft không lộ ra trang SSR?** — Câu SQL dùng chung `SearchSql` có `WHERE r.status = 'Published'`; test `Ban_nhap_khong_xuat_hien_trong_trang` tạo Draft có marker riêng và kiểm 0 kết quả.
