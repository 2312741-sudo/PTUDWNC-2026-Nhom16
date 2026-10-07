# T5 — Số đo hiệu năng (trên DB sạch 100 recipes, `cb_w5_seed`)

Ngày chạy: 07/10/2026. API chạy riêng (`dotnet run --no-auto-migrate`, không kèm frontend để tiết kiệm RAM — máy chỉ còn 0,24–0,45 GB trống trong lúc đo). Mọi số dưới đây **chạy thật**.

## 1. EXPLAIN (ANALYZE, BUFFERS) — các câu lệnh chính

| Câu lệnh | Kế hoạch thật | Execution Time |
|---|---|---|
| Chi tiết 1 recipe (theo slug) | `Index Scan` trên `IDX_Recipe_Slug` | 1,09 ms |
| Danh sách dashboard (Published, phân trang 20) | `Index Scan Backward` trên `IDX_Recipe_PublishedAt`, filter `Status=1` | 0,13 ms |
| Nguyên liệu của 1 recipe | `Bitmap Index Scan` trên `IX_RecipeIngredients_RecipeId_OrderIndex` | 1,06 ms |
| Bước của 1 recipe | `Bitmap Index Scan` trên `IX_RecipeSteps_RecipeId_StepNumber` | 0,22 ms |
| Tìm kiếm từ khoá (mẫu `ILIKE Title OR Description`, đúng cách `RecipeRepository.cs` viết) | **`Seq Scan`** — không dùng `IDX_Recipes_Title_Description_Trgm` | 0,79 ms |

### Phát hiện: chỉ mục GIN trigram tìm kiếm có thể không bao giờ được dùng bởi truy vấn thật

`IDX_Recipes_Title_Description_Trgm` được tạo trên **biểu thức** `("Title" || ' ' || "Description")` (migration `20260929060455_AddFtsAndGinIndex`, TV2). Nhưng `RecipeRepository.cs:86-91` viết truy vấn bằng `EF.Functions.ILike(r.Title, ...) || EF.Functions.ILike(r.Description, ...)` — **hai cột riêng, nối bằng OR**, không phải biểu thức nối chuỗi. PostgreSQL chỉ dùng được index biểu thức khi câu lệnh khớp **đúng** biểu thức đó, nên chỉ mục này **không khớp** với cách truy vấn thật đang viết — đã kiểm bằng `EXPLAIN` ở trên (ra `Seq Scan`, không phải `Bitmap Index Scan` trên GIN). Ở quy mô 100–110 dòng, `Seq Scan` vẫn nhanh (0,79 ms) nên **chưa gây chậm thấy được**, nhưng khi dữ liệu lớn lên chỉ mục này vẫn sẽ không được chọn — là góc hiệu năng ẩn, thuộc phần code của TV2 (search), **không sửa**, chỉ ghi nhận để TV2/Tâm biết.

## 2. k6 — API chi tiết công thức, 20 VU / 30 giây (đúng yêu cầu)

Lệnh (k6 chạy trong Docker, API chạy trên máy host):
```bash
docker run --rm -i -e API=http://host.docker.internal:5080/api/v1 -e SLUG=ca-tai-tuong-chien-xu -e VUS=20 -e DURATION=30s grafana/k6 run --quiet -
```
(script rút gọn chỉ gọi `GET /api/v1/recipes/{slug}`, không kèm trang Next.js vì frontend đang tắt để tiết kiệm RAM — xem mục "Hạn chế" dưới)

| Chỉ số | Giá trị thật |
|---|---|
| Tổng request | 23.292 |
| Lỗi (`http_req_failed`) | **0,00%** |
| Thông lượng | ~776 req/s |
| p50 (med) | **23,73 ms** |
| p90 | 35,59 ms |
| p95 | **40,72 ms** |
| p99 | **53,71 ms** |
| max | 331,81 ms |
| `SLOW_SQL` (log `SlowQueryInterceptor`, ngưỡng 100 ms) trong suốt 30s | **0** |

**So với số Tuần 4** (`Tuan04/K22_hieu_nang_recipe.md`): p95 cũ **11,35 ms** (API, điều kiện đo không ghi rõ số VU) — số Tuần 5 (p95 40,72 ms) **cao hơn ~3,6 lần**, nhưng đo ở tải thật 20 VU liên tục (Tuần 4 không ghi rõ VU, nhiều khả năng đo ở tải thấp hơn hoặc 1 VU). **Không kết luận "chậm đi"** — hai điều kiện đo khác nhau (số VU, có thể khác cả phần cứng do máy đang chạy nhiều tiến trình khác lúc đo Tuần 5). Ghi nhận số thật của từng lần, không suy diễn xu hướng.

### Hạn chế của lần đo này (thành thật, không giấu)
- **Chỉ đo được `api`, không đo được `page`** (trang Next.js `/recipes/{slug}`) vì phải tắt frontend để nhường RAM — máy chỉ còn 0,24–0,45 GB trống xuyên suốt phiên làm việc (xem `KIEM_TRA_TUAN4.md` mục 5 và nhật ký RAM trong `BAO_CAO_TUAN5.md`). CWV/Lighthouse đã ghi là việc của Trung (ngoài phạm vi Claude Code) theo đúng phân công gốc.
- Chỉ đo 1 lần (không lấy trung vị 3 lần như T5 yêu cầu) — RAM không cho phép chạy thêm lần nữa an toàn ngay sau đó trong phiên này; số liệu trên là **một lần chạy thật**, không phải trung vị.

## 3. Số câu SQL (đối chiếu Tuần 4, không đo lại — đã có log thật)

Không đo lại số câu SQL cho "thêm/xoá nguyên liệu/bước" trong phiên này (cần bật SQL logging riêng, tốn thêm một lượt chạy — RAM không cho phép thêm một tác vụ nặng). Dùng lại số đã xác nhận ở `Tuan04/K22_hieu_nang_recipe.md`: chi tiết 18 dòng (không N+1), nạp để ghi 17 dòng. Đề nghị đo lại ở Tuần 6 khi máy có nhiều RAM trống hơn.
