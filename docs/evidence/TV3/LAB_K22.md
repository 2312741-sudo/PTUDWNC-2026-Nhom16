# LAB K22 — TV3: k6 / EXPLAIN / N+1 / cảnh báo query chậm — nhánh practice/TV3/labs, KHÔNG merge

## Giả định (của TV3, không phải yêu cầu giảng viên)
Đề không ghi ngưỡng. Dùng giả định nhóm: k6 **20 VU, 30 s, p95 < 500 ms**, checks > 99%. Chứng minh "không N+1" bằng cách **đếm số câu SQL
mỗi request** (header `X-Sql-Count`), cảnh báo query > 100 ms (NFR-PERF-004, cấu hình `Perf:SlowQueryMs`). Lighthouse/CWV: máy **không có**
`lighthouse` → **chưa đo**.

## Mô tả
- `L22/PerformanceEndpoints.cs` (mới):
  - `SqlMonitor`: một `ActivityListener` cho cả process nghe nguồn `Npgsql` (Npgsql phát 1 Activity cho mỗi câu lệnh). Mỗi request có một
    `SqlScope` trong `AsyncLocal` → đếm câu SQL của đúng request đó, header `X-Sql-Count` gắn bằng `Response.OnStarting`; câu nào vượt ngưỡng → log
    Warning `SLOW_SQL {ElapsedMs} ms: {Sql}` (log có sẵn CorrelationId/TraceId của K20).
  - `GET /lab/l22/recipes?take=` — danh sách + ảnh bằng **1 câu** (CTE lấy trang → LEFT JOIN `lab_images` → `json_agg ... GROUP BY`).
  - `GET /lab/l22/recipes?take=&mode=naive` — **cố ý N+1** (1 câu danh sách + 1 câu ảnh/công thức) để so sánh.
  - `GET /lab/l22/explain?take=` — `EXPLAIN (ANALYZE, BUFFERS)` câu danh sách.
- `LabDb.cs`: index `ix_lab_images_recipe`. `Program.cs`: `UseL22SqlMonitor` ngay sau middleware K20, `MapL22Performance`. `appsettings.json`: `Perf:SlowQueryMs=100`.
- `k6/search.js`: ngưỡng p95 < 500 ms cho 3 nhóm `page:api` (JSON L3 + Redis), `page:ssr` (HTML L16), `page:list` (L22, check `X-Sql-Count == 1`).
- Test `L22PerformanceTests.cs` (5 test): 1 câu SQL dù take=5 hay 20; ảnh gom đúng công thức; naive = 1+N câu; EXPLAIN có `Execution Time`, không `SubPlan`; ngưỡng 0 ms → có log `SLOW_SQL`.
- Phát hiện khi chạy cả bộ: 4 test K20 đỏ vì `UseSerilogRequestLogging` mặc định ghi qua `Log.Logger` tĩnh (app test build sau ghi đè) → commit riêng `fix(K20)`.

## Phần SP (không viết lại)
- Concurrency/query SP (2 writer RowVersion, nested rollback, child khác recipe, EXPLAIN query recipe): nhánh SP C7 của TV3 (`2312786_HuynhQuocTrung_C7-tests-tuan4` / `..._C7-frontend-tests`).

## Cách chạy
```powershell
. .\.secrets.local.ps1
$env:LAB_PG = "Host=localhost;Port=5432;Username=postgres;Password=$env:LAB_PG_PASSWORD"
dotnet test labs/TV3/Lab.TV3.Tests --filter "FullyQualifiedName~L22" -m:1
# Đo tải (app chạy ở 5090; không cài k6 thì dùng Docker):
Get-Content -Raw labs/TV3/k6/search.js | docker run --rm -i grafana/k6 run --quiet -e BASE=http://host.docker.internal:5090 -
curl.exe -s -D - -o NUL "http://localhost:5090/lab/l22/recipes?take=20&mode=naive"   # X-Sql-Count: 21
```

## Kết quả thật
- Commit đỏ `60728f7`: 5/5 FAIL (404) — `LAB_K22_do.txt`. Commit xanh: `c4688b7` (sau fix K20 `a3e26a8`) — 5/5 PASS — `LAB_K22_xanh.txt`; cả bộ lab 82/82.
- `LAB_K22_explain.txt` (app thật, 203 Published): `X-Sql-Count` = 1 (take 5), 1 (take 20), **21** (naive take 20). EXPLAIN: `GroupAggregate` ←
  `Nested Loop Left Join` ← `Bitmap Index Scan on ix_lab_images_recipe`, **Execution Time 0.239 ms**. Ước lượng rows của planner lệch (2 so với 203)
  vì chưa `ANALYZE` sau khi seed — đoán, chưa kiểm.
- `LAB_K22_k6.txt`: 20 VU / 30 s, 61 422 request, **0% lỗi**, checks 100%; p95 tổng **15.8 ms** (api 18.68, ssr 11.94, list 15 ms), p99 24.54 ms,
  max 960 ms → đạt ngưỡng giả định. Có 5 dòng `SLOW_SQL` > 100 ms trong lúc tải (đoán do máy chỉ còn ~0.5 GB RAM, k6 chạy cùng máy).
- Chưa đo: Lighthouse/CWV (không có công cụ); k6 chạy chung máy với app nên số liệu chỉ mang tính tương đối.

## 4 câu tự kiểm tra
1. **N+1 là gì, lab chứng minh "không N+1" thế nào?** — 1 câu lấy N dòng cha rồi N câu lấy con cho từng dòng. Lab đếm câu SQL thật (Activity của Npgsql) mỗi request: bản JOIN luôn 1 câu dù take 5 hay 20; bản naive tăng theo N (take 20 → 21 câu).
2. **Vì sao dùng `AsyncLocal` để đếm?** — Nhiều request chạy song song trên cùng listener; `AsyncLocal` đi theo luồng async của từng request nên câu SQL của request nào chỉ cộng vào scope của request đó.
3. **Đọc EXPLAIN ANALYZE thấy gì?** — Thời gian thật (`actual time`, `Execution Time`), số dòng thật, cách quét (Seq Scan / Bitmap Index Scan), `loops`; `Buffers: shared hit` = đọc từ cache. Nested Loop 20 lần trong 1 câu SQL khác N+1 vì không có 20 vòng gọi qua mạng.
4. **p95 là gì, sao dùng p95 mà không dùng trung bình?** — 95% request nhanh hơn giá trị này. Trung bình che mất request chậm (max 960 ms nhưng avg chỉ 9.56 ms); p95/p99 phản ánh trải nghiệm của người bị chậm.
