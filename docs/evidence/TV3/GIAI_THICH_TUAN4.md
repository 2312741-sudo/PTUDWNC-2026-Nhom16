# GIẢI THÍCH TUẦN 4 — để Trung hiểu code và trả lời thầy

> Mỗi mục: 5–8 dòng giải thích code + 4 câu thầy có thể hỏi (kèm đáp án ngắn). Code lab ở `labs/TV3/Lab.TV3.Api/L16|L19|L20|L22`.

## K16 — SSR search (`L16/SsrSearchEndpoints.cs`)

- `MapGet("/lab/l16/search")` đọc `q`, `page`, `pageSize` (cắt `q` tối đa 200 ký tự, `pageSize` 1–50).
- Có `q` thì gọi `SearchEndpoints.SearchDb` (câu FTS của L3, chỉ `status = 'Published'`), không có thì chỉ vẽ form.
- `Render()` ghép chuỗi HTML bằng `StringBuilder`; mọi dữ liệu người dùng/DB đi qua `Html.Encode` (`HtmlEncoder.Create(UnicodeRanges.All)`).
- Trả `Results.Content(html, "text/html; charset=utf-8")` → trình duyệt nhận trang hoàn chỉnh, không cần JS.
- `<meta name="robots" content="noindex,follow">`: không index trang kết quả nhưng vẫn theo link sang trang chi tiết.
- Phân trang là link `<a href>`; dấu `&` trong href được encode thành `&amp;` (HTML hợp lệ).

Câu hỏi:
1. *SSR/ISR/CSR khác nhau?* — SSR render mỗi request trên server; ISR render sẵn rồi làm mới theo chu kỳ (SP: `revalidate = 300`); CSR render ở trình duyệt bằng JS (SP: wizard `"use client"`).
2. *Sao test biết là "không cần JS"?* — HttpClient trong test không chạy JS; nó đọc HTML thô mà vẫn thấy tiêu đề công thức.
3. *XSS chặn ở đâu?* — `Html.Encode` mọi chuỗi in ra (tiêu đề, từ khoá, slug, danh mục); test tạo tiêu đề `<script>` và từ khoá `"><img ...>`, kiểm thấy `&lt;script&gt;`.
4. *Sao không cache trang SSR?* — Lab giữ đơn giản; JSON L3 đã có Redis. Có thể thêm OutputCache theo `q` nếu cần (đánh đổi: dữ liệu cũ tới khi hết hạn).

## K19 — sitemap / robots / 301 (`L19/SeoEndpoints.cs`)

- `/robots.txt`: `Disallow: /lab/` nhưng `Allow` trang chi tiết + SSR search, dòng `Sitemap:` lấy từ `Seo:BaseUrl`.
- `/sitemap.xml`: `XDocument` với namespace `sitemaps.org/0.9`, mỗi công thức Published một `<url><loc><lastmod>`.
- `/lab/l19/recipes/{slug}`: tìm theo slug Published → HTML có `canonical`, `og:*`, `twitter:card`; không thấy thì tra `lab_slug_redirects` → 301 về slug hiện tại; không có nữa → 404.
- `PUT /lab/l19/recipes/{id}/title`: transaction + `SELECT ... FOR UPDATE`; Draft → slug mới + lưu slug cũ (UPSERT); Published → giữ slug.
- Bảng redirect lưu `recipe_id` (không lưu slug đích) → đổi nhiều lần vẫn 301 một bước.
- `/recipe/{slug}` (URL cũ số ít) → `Results.Redirect(..., permanent: true)` = 301.

Câu hỏi:
1. *301 vs 302 vs 308?* — 301/308 vĩnh viễn (308 giữ nguyên method POST), 302/307 tạm thời. SEO dùng 301 để chuyển "sức mạnh" link sang URL mới.
2. *Canonical để làm gì?* — Báo Google URL chuẩn khi một nội dung có nhiều URL (query string, http/https...), tránh trùng lặp.
3. *Sao `FOR UPDATE`?* — Khoá dòng công thức trong transaction; hai request đổi tiêu đề cùng lúc sẽ chạy tuần tự, không mất slug cũ.
4. *Sitemap lớn thì sao?* — Giới hạn 50 000 URL/file (code đã `LIMIT 50000`); lớn hơn thì chia nhiều file + sitemap index.

## K20 — Serilog / correlation / OTel / health (`L20/Observability.cs`)

- `AddSerilog((sp, lc) => ...)`: đọc mục `Serilog` trong appsettings (Console), sink từ DI (test), `Enrich.FromLogContext`; `LabLog:FilePath` → File, `Seq:Url` → Seq.
- Middleware đầu pipeline: nhận `X-Correlation-ID` nếu khớp `^[A-Za-z0-9._-]{1,64}$`, không thì tự sinh GUID; trả header + `X-Trace-Id`; `LogContext.PushProperty("CorrelationId", ...)`.
- `UseSerilogRequestLogging` ghi 1 dòng/request; truyền `o.Logger` của chính app (fix `a3e26a8`: mặc định nó dùng `Log.Logger` tĩnh → nhiều app trong 1 process ghi nhầm).
- OTel: tracing `AddAspNetCoreInstrumentation` + `AddSource("Npgsql")` (span SQL là con của span HTTP), metrics + `AddMeter("Lab.TV3")`; exporter Console bật bằng `Otel:ConsoleExporter`.
- Health: `/health/live` `Predicate = _ => false` (không kiểm gì), `/health/ready` kiểm `db` (`SELECT 1`) + `redis` (`IsConnected` + PING), có lỗi → 503, JSON từng thành phần.
- Counter `lab.search.requests` (tag `page`) tăng ở trang SSR và API search.

Câu hỏi:
1. *Ba trụ observability?* — Logs (sự kiện), metrics (số đếm/đo theo thời gian), traces (đường đi một request qua nhiều thành phần). Lab có đủ ba.
2. *`traceparent` gồm gì?* — `version-traceId(32 hex)-parentSpanId(16 hex)-flags`; ASP.NET Core đọc header này nên trace id của server = trace id client gửi (đã thấy trong `LAB_K20_chay_that.txt`).
3. *Sao chặn correlation id "xấu"?* — Chuỗi có xuống dòng/ký tự lạ có thể giả mạo dòng log (log injection) hoặc làm log quá dài.
4. *Health ready trả 503 khi Redis chết, nhưng app vẫn phục vụ được (fallback DB) — có mâu thuẫn?* — Đây là lựa chọn của lab: coi Redis là phụ thuộc bắt buộc. Nếu muốn chạy tiếp khi Redis chết thì cho Redis trả `Degraded` thay vì `Unhealthy`.

## K22 — k6 / EXPLAIN / N+1 / SLOW_SQL (`L22/PerformanceEndpoints.cs`, `k6/search.js`)

- `SqlMonitor`: một `ActivityListener` tĩnh nghe nguồn `Npgsql`; khi Activity dừng, nếu có `SqlScope` (AsyncLocal) của request thì `Count++`, và log `SLOW_SQL` nếu vượt `Perf:SlowQueryMs`.
- Middleware tạo `SqlScope` cho mỗi request và gắn `X-Sql-Count` bằng `Response.OnStarting` (lúc đó các câu SQL đã chạy xong).
- `ListSql`: CTE lấy trang công thức → `LEFT JOIN lab_images` → `json_agg(...) FILTER (WHERE x.id IS NOT NULL)` + `GROUP BY` → 1 câu cho cả danh sách + ảnh.
- `mode=naive`: cố ý vòng `foreach` gọi 1 câu ảnh cho mỗi công thức → `X-Sql-Count = 1 + N` (đo thật: 21 với take 20).
- `/lab/l22/explain`: `EXPLAIN (ANALYZE, BUFFERS)` → thấy `Bitmap Index Scan on ix_lab_images_recipe`, Execution Time 0.239 ms.
- k6: 20 VU / 30 s, 3 nhóm tag `api`/`ssr`/`list`, ngưỡng p95 < 500 ms → đo thật p95 15.8 ms, 0% lỗi, 61 422 request.

Câu hỏi:
1. *Sửa N+1 trong EF Core thế nào?* — `Include`/`ThenInclude` (eager loading), hoặc projection `Select` sang DTO; với nhiều collection thì `AsSplitQuery` (số câu cố định, không theo N).
2. *`Nested Loop` 20 vòng trong EXPLAIN có phải N+1 không?* — Không: đó là vòng lặp bên trong PostgreSQL của 1 câu SQL, không có 20 lần gọi qua mạng từ app.
3. *Sao k6 chạy trong Docker lại gọi `host.docker.internal`?* — Trong container, `localhost` là chính container; `host.docker.internal` trỏ về máy Windows nơi app đang chạy ở 5090.
4. *Số k6 này có tin được không?* — Tương đối: k6 và app chạy chung máy (RAM ~0.5 GB), dữ liệu nhỏ (203 công thức), Redis cache làm API nhanh. Đo nghiêm túc phải tách máy, dữ liệu gần thật, warm-up.

## C7 — Playwright (`src/frontend/playwright.config.ts`, `e2e/create-recipe.spec.ts`, nhánh SP)

- Config: `testDir ./e2e`, `baseURL` lấy từ `E2E_BASE_URL` (mặc định 3000), 1 worker, trace/screenshot khi lỗi; `webServer` chỉ bật khi `E2E_START_SERVER=1`.
- Jest bỏ qua `/e2e/` (đã có trong `jest.config.mjs`) → hai bộ test tách riêng: `npx jest` / `npm run e2e`.
- Test: đăng nhập bằng placeholder (label form đăng nhập không gắn input), bước 1 điền tiêu đề/thời gian/khẩu phần/danh mục → "Lưu & tiếp", bước 2 điền dòng nguyên liệu → "Lưu" → chờ ô bảng đúng tên (hết "(đang lưu…)"), bước 3 thêm bước.
- `test.skip(!password)` → thiếu `E2E_PASSWORD` thì skip, không đoán mật khẩu. Hiện BLOCKED vì user seed không có mật khẩu.

Câu hỏi:
1. *Unit / integration / E2E khác nhau?* — Unit: 1 hàm/lớp với fake; integration: nhiều lớp + DB thật (WebApplicationFactory); E2E: trình duyệt thật đi qua frontend → API → DB như người dùng.
2. *Sao chọn `getByRole`/`getByLabel` thay vì CSS class?* — Bền hơn khi đổi giao diện và kiểm luôn a11y (phần tử phải có tên truy cập được).
3. *Sao test chờ "(đang lưu…)" biến mất?* — Wizard hiện dòng tạm (optimistic) trước; hết chữ đó nghĩa là server đã trả về thành công, tránh test xanh giả.
4. *Test để lại dữ liệu rác?* — Có (Draft tên `E2E ... <timestamp>`); nên chạy trên DB test riêng hoặc thêm bước xoá trong `afterEach`.

## K20 SP — metric recipe (`CulinaryBlog.Application/RecipeMetrics.cs`, nhánh SP)

- `Meter("CulinaryBlog.Recipes")` + 2 `Counter<long>`: `culinary.recipes.created`, `culinary.recipes.updated`.
- Gọi `.Add(1)` **sau** `SaveChangesAsync` trong `CreateRecipeHandler`/`UpdateRecipeHandler` → lỗi (401/403/422) không được đếm.
- Chỉ dùng `System.Diagnostics.Metrics` (BCL) nên Application vẫn không phụ thuộc OpenTelemetry/EF (giữ architecture test).
- Test `RecipeMetricsTests` dùng `MeterListener`, chạy trong collection `DisableParallelization` để số đếm chính xác. Chưa `AddMeter` ở API (chờ OTel trên main).

Câu hỏi:
1. *Counter vs Histogram vs Gauge?* — Counter chỉ tăng (số công thức tạo); Histogram phân bố giá trị (thời gian xử lý → p95); Gauge giá trị tức thời (số job đang chờ).
2. *Sao không gắn tag authorId?* — High cardinality: mỗi user một chuỗi thời gian, làm nổ bộ nhớ hệ thống metric.
3. *Sao đếm sau SaveChanges?* — Để metric phản ánh việc đã thành công thật; đếm trước sẽ tính cả lần rollback/xung đột.
4. *Sao test phải chạy không song song?* — `MeterListener` nghe cả process; test khác tạo công thức cùng lúc sẽ làm số đếm sai.
