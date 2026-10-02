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

## A — Test tích hợp nguyên liệu/bước (`tests/CulinaryBlog.Tests/RecipeIngredientHttpTests.cs`, nhánh SP)

- Dùng `ApiFactory` (WebApplicationFactory) + Postgres thật + migration thật: request đi qua routing, auth JWT, MediatR, validator, EF, interceptor.
- Quyền sở hữu nằm ở một chỗ: `RecipeGuard.LoadOwnedAsync` (Recipes.cs) → chưa đăng nhập 401, không có recipe 404, không phải tác giả và không phải Admin 403.
- Test 1: thêm 3 nguyên liệu, sửa cái giữa (vị trí giữ nguyên), xoá cái đầu → đọc thẳng DB (`IgnoreQueryFilters`) thấy OrderIndex còn 0,1 và dòng bị xoá chỉ `IsDeleted = true`.
- Test 2: tác giả thứ hai POST/PUT/DELETE nguyên liệu và bước của công thức người khác → cả 6 lần 403 `recipe.forbidden`, DB không đổi; khách → 401.
- Test 3: PUT/DELETE recipe với rowVersion sai → 422 `recipe.concurrency_conflict`, công thức còn nguyên.
- Test 4 ("CurrentBehavior"): đổi nguyên liệu không làm đổi RowVersion của recipe — vì interceptor chỉ cấp token mới cho entity ở trạng thái Modified.

Câu hỏi:
1. *Sao trả 403 mà không 404 cho người lạ?* — Công thức tồn tại và người gọi đã đăng nhập; 403 nói rõ "không có quyền". Riêng xem Draft của người khác thì trả 404 (`GetRecipeBySlugHandler`) để không lộ sự tồn tại.
2. *Sao test đọc DB trực tiếp thay vì chỉ gọi GET?* — GET đã lọc dòng xoá mềm; muốn chứng minh "xoá mềm chứ không xoá cứng" và OrderIndex trong bảng thì phải nhìn bảng.
3. *Hai tab cùng sửa một nguyên liệu thì sao?* — Endpoint con không nhận rowVersion nên bản lưu sau thắng (last-write-wins). Muốn chặn thì gửi rowVersion của nguyên liệu (cột đã có `IsConcurrencyToken`).
4. *Unit vs integration ở đây?* — Test cũ (RecipeErrorBranchTests) gọi handler với repository giả → nhanh nhưng không kiểm routing/EF/DB; test mới chậm hơn nhưng kiểm cả đường đi thật.

## B — Jest/RTL cho bước nguyên liệu và các bước (`_wizard/__tests__/IngredientsStep.test.tsx`, `StepsStep.test.tsx`)

- Render cả `RecipeWizard` (không render riêng step) để kiểm luôn `run()`/hoàn tác ở wizard; mock module `@/lib/recipe-editor` bằng `jest.mock`, giữ phần thật bằng `requireActual`.
- Sửa: bấm "Sửa" → dòng nháp "dòng 2" nạp sẵn giá trị → gõ `"  Ca loc dong  "` → `updateIngredient("r1", "i1", { name: "Ca loc dong", quantity: 600, unit: "g", notes: null })` (Zod trim, rỗng → null).
- Xoá: `jest.spyOn(window, "confirm").mockReturnValue(false/true)` → false thì không gọi API, true thì gọi `deleteIngredient("r1","i1")` và dòng biến mất.
- Mở Sửa mà không đổi gì vẫn tính là dòng nháp (có `serverId`) → "Tiếp" bị chặn cho tới khi bấm "Bỏ".
- Lỗi server: mock reject `ApiError` → banner "… — đã hoàn tác thay đổi.", bảng trở lại như cũ, dòng nháp được trả lại để sửa tiếp.
- StepsStep: thêm/sửa/xoá + nút ▼ gọi `reorderSteps("r1", ["s2","s1"])`; reorder lỗi → thứ tự cũ quay lại.

Câu hỏi:
1. *Sao mock module API mà không mock `fetch`?* — Test kiểm hành vi wizard (gọi đúng hàm, đúng tham số, hoàn tác), không kiểm chi tiết HTTP; phần HTTP đã có test backend + E2E.
2. *`userEvent` khác `fireEvent`?* — `userEvent` mô phỏng chuỗi sự kiện như người thật (focus, keydown, input…), gần thực tế hơn `fireEvent` chỉ bắn 1 sự kiện.
3. *Sao biết test hoàn tác không xanh giả?* — Đã thử xoá dòng hoàn tác trong code: 4 test đỏ, khôi phục thì xanh.
4. *`confirm()` trong jsdom?* — jsdom không có hộp thoại thật; `spyOn(window, "confirm")` thay bằng giá trị cố định và kiểm cả nội dung câu hỏi.

## C — Playwright E2E + lỗi wizard nhảy về bước 1 (`e2e/create-recipe.spec.ts`, `RecipeWizard.tsx`)

- Test tự tạo user: `request.post("/auth/register")` với email `e2e.<thời gian><hex>@culinary.local`, mật khẩu `Aa1!` + `randomBytes(18)` (đủ hoa/thường/số/đặc biệt) → 201.
- Mật khẩu không lộ: `fill()` ghi giá trị vào tên bước trong báo cáo HTML (`Fill "…"`) → nhập bằng `locator.evaluate` (setter gốc + sự kiện `input`, React nhận được); trace tắt cho spec này. Đã giải nén báo cáo kiểm: không có.
- Dialog: `page.once("dialog", d => d.dismiss())` rồi bấm Xoá → dòng còn; lần hai `d.accept()` → dòng mất, kiểm nội dung `Xoá nguyên liệu "Me chua"?`.
- Cuối test gọi API lấy chi tiết để đối chiếu: đúng `[["Cá lóc", 600]]` và 1 bước (không chỉ tin giao diện).
- Lỗi tìm được: sau "Lưu & tiếp", `history.replaceState` đổi URL sang `/{id}/edit`; Next 15 đồng bộ URL này vào router; `router.refresh()` (gọi sau `/api/revalidate`) tải RSC của route **edit** → `EditRecipeClient` mount lại với `step: 0`.
- Sửa: nhớ `window.location.pathname` lúc wizard mount, chỉ `router.refresh()` khi pathname còn trùng. Test Jest đỏ trước (`fc34f87`) / xanh sau (`64b29c1`).

Câu hỏi:
1. *Sao lỗi này Jest cũ không bắt được?* — Jest mock `useRouter`, không có Next router thật nên `refresh()` không render lại route. Chỉ trình duyệt thật (E2E) mới thấy; sau đó viết test Jest mô tả điều kiện (không được gọi refresh).
2. *Sao test E2E lúc xanh lúc đỏ trước khi sửa?* — Phụ thuộc thời gian: `refresh()` chạy sau khi `fetch('/api/revalidate')` xong; nếu test đi nhanh hơn thì qua, chậm hơn thì bị đá về bước 1. Test chập chờn thường là dấu hiệu lỗi thật.
3. *Có `role="alert"` rỗng làm test đỏ?* — Next.js luôn gắn `next-route-announcer` (role alert) cuối trang; test chỉ đếm alert có chữ.
4. *Vì sao đăng ký qua API mà đăng nhập qua UI?* — Đăng ký chỉ là chuẩn bị dữ liệu (nhanh, ổn định); đăng nhập là một phần luồng người dùng cần kiểm.

## D — K17 TanStack Query trong wizard (`RecipeWizard.tsx`, 3 commit)

- `RecipeWizard` bọc `QueryClientProvider` với `QueryClient` riêng (tạo một lần bằng `useState`) → không sửa `app/layout.tsx` dùng chung; `staleTime: Infinity`, không refetch khi focus, không retry.
- Dữ liệu công thức: `useQuery({ queryKey: ["recipe-detail", recipeId], enabled: false, initialData })` — chỉ đọc cache; nạp lại bằng `queryClient.fetchQuery({ …, staleTime: 0 })` sau mỗi lần lưu.
- Khoá theo `recipeId` vì slug đổi khi sửa tiêu đề bản nháp.
- Thao tác con qua một `useMutation`: `onMutate` → `cancelQueries` + chụp `snapshot` + `setQueryData(optimistic(snapshot))`; `onError` → `setQueryData(snapshot)` + banner "đã hoàn tác"; `onSuccess` → làm mới trang công khai.
- Giữ nguyên `RunFn` (`run(fn, optimistic)`) nên IngredientsStep/StepsStep/ImagesStep/ReviewStep không phải sửa — mỗi commit Jest vẫn 30/30, E2E 2/2.

Câu hỏi:
1. *Optimistic update là gì, rủi ro?* — Cập nhật giao diện trước khi server trả lời để nhanh; rủi ro là server từ chối → phải hoàn tác (snapshot) và báo lỗi.
2. *Sao `cancelQueries` trong `onMutate`?* — Nếu đang có một lần fetch cũ chạy dở, kết quả của nó có thể ghi đè dữ liệu lạc quan vừa đặt.
3. *Sao `staleTime: Infinity` + `enabled: false`?* — Giữ hành vi cũ: chỉ nạp lại khi wizard yêu cầu; tránh refetch tự động lúc focus ghi đè trạng thái đang hoàn tác.
4. *Khác gì reducer tự viết?* — Logic snapshot/rollback giống, nhưng TanStack quản lý cache, trạng thái pending/error, huỷ request cũ và dễ dùng lại ở trang khác (vd. dashboard dùng cùng khoá).

## E — K18 a11y (`RecipeWizard.tsx`, các step, `dashboard/recipes/page.tsx`; checklist `LAB_K18_checklist.md`)

- Đổi bước: `<h2 tabIndex={-1} className="sr-only">Bước n/5: …</h2>` và `focus()` khi `step` đổi (so với bước đã hiện trước đó, nên lúc mở trang không cướp focus) — WCAG 2.4.3.
- `<p role="status" className="sr-only">` hiện "Đang lưu…" khi đang gọi API → trình đọc màn hình đọc mà không đổi giao diện — WCAG 4.1.3.
- Nút lặp lại có ngữ cảnh: `Sửa<span className="sr-only"> nguyên liệu Cá lóc</span>` → tên truy cập "Sửa nguyên liệu Cá lóc" — WCAG 2.4.6.
- Dashboard: `<th aria-sort="descending">` ở cột đang sắp xếp (mũi tên ↓ chỉ là hình) — WCAG 1.3.1.
- Có từ trước: `aria-invalid` + `aria-describedby` trỏ tới lỗi dưới ô, banner lỗi `role="alert"`, `aria-current="step"`.
- 5 test tự động trong `WizardA11y.test.tsx`; NVDA/zoom/tương phản là phần kiểm tay (chưa làm, có kịch bản N1–N15).

Câu hỏi:
1. *`role="status"` khác `role="alert"`?* — status = `aria-live="polite"` (đọc khi rảnh, cho thông báo bình thường); alert = `assertive` (ngắt lời ngay, cho lỗi).
2. *Sao dùng `sr-only` mà không `aria-label`?* — `aria-label` thay toàn bộ tên; `sr-only` giữ chữ "Sửa" nhìn thấy làm phần đầu tên (WCAG 2.5.3 "label in name": người dùng giọng nói nói "Sửa" vẫn khớp).
3. *`tabIndex={-1}` để làm gì?* — Cho phép `focus()` bằng code vào tiêu đề nhưng không thêm điểm dừng khi người dùng bấm Tab.
4. *Test tự động có thay được NVDA?* — Không: jsdom kiểm được thuộc tính (tên, aria, focus), không kiểm được cách NVDA thực sự đọc, tương phản màu hay bố cục 320 px.

## G — Dọn bí mật trong docs

- `C1-HOAN-THIEN.md` dòng 32, 41 và `LAB_K10.md` dòng 8 viết thẳng `Password=postgres`/`Password=...` → đổi thành `Password=$env:LAB_PG_PASSWORD` (khối lệnh chuyển sang PowerShell cho khớp Windows).
- `LAB_K20_chay_that.txt`: lần che trước thay mọi chữ "postgres" (không phân biệt hoa thường) thành `***` nên hỏng `PostgreSQL OK` và `postgresql` — đã trả lại; quy tắc đúng là che theo mẫu chuỗi kết nối (`Password=…`), không che theo từ.

Câu hỏi:
1. *Mật khẩu `postgres` máy local có cần giấu?* — Có: tài liệu bị chép sang môi trường khác; thói quen viết thẳng bí mật dẫn tới lộ thật. Secret scan (K24) cũng sẽ báo.
2. *Che theo từ khoá sai ở đâu?* — Phá nội dung không phải bí mật và vẫn có thể sót bí mật khác chuỗi; che theo mẫu (`Password=[^;"]+`) chính xác hơn.
3. *Bí mật để ở đâu?* — `.secrets.local.ps1` (đã exclude khỏi git), nạp bằng `. .\.secrets.local.ps1` trước khi chạy lệnh.
4. *Nếu đã lỡ commit bí mật?* — Coi như đã lộ: đổi mật khẩu; xoá khỏi lịch sử là việc phụ (người khác có thể đã clone).

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
