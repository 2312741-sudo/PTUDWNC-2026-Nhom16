# K22 (SP) — Hiệu năng query recipe: không N+1, EXPLAIN ANALYZE, cảnh báo > 100 ms, k6 trang chi tiết — TV3

Ngày đo: 02/10/2026. Nhánh `2312786_HuynhQuocTrung_C7-frontend-tests`. Yêu cầu: NFR-PERF-004 (không N+1; index cho WHERE/ORDER BY;
slow query log > 100 ms; EXPLAIN ANALYZE), NFR-PERF-001 (GET p50 ≤ 150 ms). Lighthouse/CWV: **chưa đo — Trung đo tay**.

| File | Nội dung |
|---|---|
| `K22_sql_explain_chay_that.md` | Báo cáo do test sinh ra (TRƯỚC `AsSplitQuery`): bảng đếm lệnh SQL, nguyên văn câu SQL, EXPLAIN (ANALYZE, BUFFERS) |
| `K22_sql_explain_sau_split.md` | Như trên, SAU `AsSplitQuery` + reader mới (mục 6) |
| `K22_index_check.txt` | EXPLAIN lọc trực tiếp theo `RecipeId` + danh sách index các bảng recipe |
| `K22_k6_chi_tiet.txt` / `K22_k6_chi_tiet_sau_split.txt` | Đầu ra k6 nguyên văn trước / sau |

## 1. Không N+1 — đếm lệnh SQL mỗi request (test `RecipeQueryPerformanceTests`, Postgres thật)
Cách đo: interceptor EF ghi mọi lệnh tới Postgres trong lúc gọi 1 request HTTP (gắn bằng `ConfigureDbContext` qua `WithWebHostBuilder`, không sửa `ApiFactory`).
Test so cặp nhỏ/lớn và **assert số lệnh bằng nhau**. Chạy lại: `$env:K22_REPORT="<file.md>"; dotnet test tests/CulinaryBlog.Tests --filter "FullyQualifiedName~RecipeQueryPerformanceTests"`.

| Request | Nhỏ | Lớn | Số lệnh (round-trip) | Chậm nhất |
|---|---|---|---|---|
| GET chi tiết `/recipes/{slug}` | 2 nguyên liệu, 2 bước | 10 nguyên liệu, 6 bước | **2 = 2** (recipe+con; tên tác giả/danh mục) | 2.4 ms |
| GET dashboard `/me/recipes` | 1 công thức | 12 công thức | **2 = 2** (COUNT; trang có projection) | 3.0 ms |
| GET `/me/recipes/counts` | 1 | 12 | **1 = 1** (GROUP BY Status) | 1.5 ms |
| POST nguyên liệu | đang có 2 | đang có 10 | **2 = 2** (nạp aggregate; INSERT) | 1.3 ms |
| DELETE nguyên liệu đầu | còn 2 | còn 10 | **2 = 2**, nhưng lệnh ghi chứa 1+2 / 1+10 câu | 1.3 ms |
| POST bước | đang có 2 | đang có 6 | **2 = 2** | 1.0 ms |
| DELETE bước đầu | còn 2 | còn 6 | **2 = 2**, lệnh ghi chứa 1+2 / 1+6 câu | 1.1 ms |

Dữ liệu DB lúc đo: Recipes=285, RecipeIngredients=266, RecipeSteps=496, Users=829. Test xanh; toàn bộ backend 214/214.
**Không lệnh nào > 100 ms** trong lần đo này (chậm nhất 3.0 ms).

Ghi chú trung thực:
- Xoá nguyên liệu/bước phải đánh lại `OrderIndex`/`StepNumber` cho các dòng sau → số câu UPDATE tăng theo N, nhưng EF gom vào **một** round-trip. Không phải N+1 query.
- Dashboard lấy tên danh mục, ảnh chính, số nguyên liệu/bước bằng subquery trong **một** câu (projection) — EXPLAIN thấy các subquery chạy theo từng dòng của trang
  (`loops=12`) nhưng đều Index Scan / Index Only Scan, trong cùng 1 câu SQL.

## 2. EXPLAIN (ANALYZE, BUFFERS) — trích (đầy đủ trong `K22_sql_explain_chay_that.md`)
| Query | Truy cập chính | Execution Time |
|---|---|---|
| Chi tiết theo slug | `Index Scan using "IDX_Recipe_Slug"`, bước: `Bitmap Index Scan on "IX_RecipeSteps_RecipeId_StepNumber"`, ảnh: `Index Scan using "IX_RecipeImages_RecipeId_OrderIndex"`, nguyên liệu: `Seq Scan` + Hash Join | 0.292 ms |
| Tên tác giả + danh mục | `Index Scan using "PK_AspNetUsers"`, `"PK_Categories"` | 0.026 ms |
| Dashboard trang | `Index Scan using "IDX_Recipe_AuthorId"` + Sort theo UpdatedAt | 0.191 ms |
| Dashboard COUNT / counts | `Index Scan using "IDX_Recipe_AuthorId"` | 0.052 / 0.030 ms |
| Nạp aggregate để ghi | `Index Scan using "PK_Recipes"`, bước Bitmap Index Scan, nguyên liệu `Seq Scan` + Hash Join | 0.177 ms |

`Seq Scan on "RecipeIngredients"`: bảng nhỏ (266 dòng) nên planner chọn quét + hash join. Lọc trực tiếp theo `RecipeId` thì dùng
`Bitmap Index Scan on "IX_RecipeIngredients_RecipeId_OrderIndex"` (`K22_index_check.txt`) → index có và dùng được. Với dữ liệu lớn hơn
planner sẽ đổi kế hoạch — **đoán**, chưa đo trên dữ liệu lớn.

### Rủi ro thấy được
1. **Bùng nổ tích Descartes ở câu chi tiết**: 3 `Include` collection trong một câu JOIN → 10 nguyên liệu × 6 bước = **60 dòng** trả về cho 1 công thức
   (EXPLAIN: `rows=60`). **ĐÃ SỬA cho trang chi tiết** (`FindBySlugAsync` + `AsSplitQuery`, đỏ `625b3fd` → xanh `b2789e4`) — số đo ở mục 6.
   `FindForWriteAsync` (nạp để ghi, chỉ nguyên liệu × bước) **chưa đổi** — đề xuất làm tiếp nếu công thức lớn.
2. Dashboard sắp xếp theo `UpdatedAt` không có index riêng; hiện lọc theo `AuthorId` trước nên chỉ sort vài dòng. Nếu một tác giả có rất nhiều công thức: index ghép
   `("AuthorId", "UpdatedAt")` — đề xuất.

## 3. Cảnh báo query > 100 ms (NFR-PERF-004)
`SlowQueryInterceptor` (commit đỏ `227fcb0` → xanh `cbfdc01`) ghi `SLOW_SQL {ms} ms (> 100 ms): {SQL}` (không ghi giá trị tham số), ngưỡng `Perf:SlowQueryMs`.
3 test: `pg_sleep(0.15)` → đúng 1 cảnh báo; `SELECT 1` và query nhanh → không cảnh báo; reader chậm cũng cảnh báo; DbContext của app đã gắn interceptor ngưỡng 100.
Khi chạy k6, log API thật có **5 dòng** `SLOW_SQL` (169.8–1358.4 ms), cùng giây 13:55:26 = lúc 5 VU gửi request đầu tiên, sau đó không còn — **đoán** do khởi động lạnh
(mở kết nối pool, JIT, lập kế hoạch lần đầu).

## 4. k6 trang chi tiết (`tests/k6/recipe-detail.js`, 5 VU × 20 s mỗi kịch bản, `ga-lac-pho-mai-cay`)
| Kịch bản | p50 | p95 | p99 | max | Ngưỡng |
|---|---|---|---|---|---|
| API `GET /api/v1/recipes/{slug}` | 6.42 ms | 10.29 ms | 26.15 ms | 1.83 s | p50<150, p95<500 — **đạt** |
| Trang Next `/recipes/{slug}` (`next dev`) | 107.74 ms | 821.04 ms | 1.05 s | 1.08 s | p95<2000 — đạt |
Tổng 12 431 request, 0% lỗi, 24 862 check đều đạt (có `authorName` trong API, có JSON-LD trong trang).
Hạn chế: chạy chung máy (RAM trống ~0.8 GB), API bản Debug qua `dotnet run`, trang chạy `next dev` (render lại mỗi request, không phải bản build/ISR) → số của trang
**không đại diện production**. Muốn số đúng: `npm run build; npm run start` rồi chạy lại cùng lệnh.

## 5. Trung đo tay
- Lighthouse (Performance/LCP/CLS/INP) trang `/recipes/ga-lac-pho-mai-cay` ở bản build (`npm run build; npm run start`), 3 lần lấy trung vị: ____
- (Tuỳ chọn) k6 lại trên bản build: ____

## 6. Sau `AsSplitQuery` (02/10/2026 18:2x, HEAD `085a25a`) — chạy lại đếm SQL, EXPLAIN, k6
Commit: test đỏ `625b3fd` (`Detail_loads_each_child_collection_in_its_own_query_so_rows_are_not_ingredients_times_steps`: không câu nào chứa cả
`RecipeIngredients` và `RecipeSteps`) → sửa `b2789e4`. Reader tên tác giả/danh mục đổi sang neo vào dòng công thức (`e46f951`, vẫn 1 câu).
Dữ liệu DB lúc đo: Recipes=510, RecipeIngredients=628, RecipeSteps=880, Users=1058.

| Chi tiết (10 nguyên liệu, 6 bước) | Trước | Sau |
|---|---|---|
| Số lệnh SQL | 2 (1 câu JOIN + tên) | **5** (recipe, nguyên liệu, bước, ảnh, tên) — vẫn bằng nhau giữa bản 2×2 và 10×6 |
| Số dòng Postgres trả về cho phần recipe + con | **60** (10 × 6) | **18** (1 + 10 + 6 + 0, + 1 dòng tên) |
| Truy cập `RecipeIngredients` | `Seq Scan` + Hash Join | `Bitmap Heap Scan` theo `IX_RecipeIngredients_RecipeId_OrderIndex` |
| Tổng Execution Time (EXPLAIN) | 0.292 ms | 0.050 + 0.048 + 0.040 + 0.026 + 0.048 = **0.212 ms** |
| Lệnh chậm nhất trong test | 2.4 ms | 0.9 ms; **không lệnh nào > 100 ms** |

k6 (cùng kịch bản 5 VU × 20 s, 0% lỗi, 21 918/21 918 check đạt):

| Kịch bản | p50 trước → sau | p95 trước → sau | p99 trước → sau |
|---|---|---|---|
| API `GET /api/v1/recipes/{slug}` | 6.42 → **6.54 ms** | 10.29 → **11.35 ms** | 26.15 → **16.73 ms** |
| Trang Next `next dev` | 107.74 → 103.18 ms | 821.04 → 829.74 ms | 1.05 → 1.07 s |

Nhận xét trung thực: với công thức cỡ này độ trễ API **gần như không đổi** (thêm 3 round-trip nhưng bớt dòng); lợi ích chính là số dòng không còn nhân
theo nguyên liệu × bước × ảnh khi công thức lớn. `SLOW_SQL` lần này: **2 dòng** (1645.4 ms, 157.5 ms) lúc 18:24:34–35 = ngay khi k6 bắt đầu 5 VU đồng thời,
sau đó không còn — **đoán** do pool mở thêm kết nối lúc tải đồng thời đầu tiên. Số trang Next vẫn là `next dev`, không đại diện production.