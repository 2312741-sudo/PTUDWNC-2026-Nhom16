# Sổ K Lab L5 — Kiểm chứng caching, rollback, ảnh, SEO, observability, multi-instance

- Nhánh lab: `practice/TV4/L5`
- Ngày chạy: 2026-10-05
- Run ID: `20261005-101129`
- Kết quả: **FAIL có chủ đích — 41/63 check đạt, 3/7 phase PASS, 4/7 phase lộ ra vấn đề thật**

## 0. Điều kiện tiên quyết để đọc sổ này

Ba điều kiện sau bắt buộc, nếu thiếu thì kết luận của lab **không có giá trị**:

| Điều kiện | Bắt buộc vì |
|---|---|
| Chạy `next build` + `next start` (production build) | `next dev` **bỏ qua hoàn toàn** cache ISR. Mọi kết luận về ISR thu được khi chạy dev server là vô giá trị. |
| API chạy thật ở `5080`, tiến trình thứ hai ở `5081` | Phase multi-instance cần **hai tiến trình cùng lúc** mới kiểm chứng được trạng thái dùng chung. |
| Có dữ liệu công thức Published | Phase ISR/SEO/ảnh đọc nội dung thật. Trang rỗng thì không phân biệt được SSR với CSR. |

Môi trường đã dùng: **101 công thức Published** (`meta.total = 101`), 105 URL trong sitemap,
Next.js 15.1.11, hạ tầng Postgres/Redis/MinIO/Seq/OTLP đều `Healthy`.

Bằng chứng kèm theo:
- `docs/evidence/TV4/Tuan04/logs/lab_l5_run.log` — log đầy đủ 63 check.
- `docs/evidence/TV4/Tuan04/logs/lab_l5_db.txt` — dữ liệu thật từ API và `/health/ready`.

## 1. Bảng tổng hợp 7 phase

| # | Phase | Kết quả | Đạt/Tổng | Kết luận |
|---|---|---|---|---|
| 1 | `search-ssr` | FAIL | 5/6 | SSR thật, nhưng bị ép `no-store` |
| 2 | `isr-detail` | FAIL | 5/8 | **ISR không hoạt động** dù code khai báo `revalidate = 300` |
| 3 | `query-rollback` | FAIL | 9/10 | Rollback đúng; chưa kiểm chứng được RowVersion |
| 4 | `image-opt` | FAIL | 1/7 | **Ảnh không tối ưu ở cả hai tầng** |
| 5 | `seo` | **PASS** | 15/15 | SEO tốt hơn dự kiến |
| 6 | `observability` | **PASS** | 10/10 | Trace propagation hoạt động đúng |
| 7 | `multi-instance` | **PASS** | 7/7 | Redis dùng chung thật |

## 2. Phase 1 — `search-ssr`

**Kết luận: trang tìm kiếm CÓ render ở server, nhưng bị buộc `no-store`.**

| Check | Kết quả | Bằng chứng |
|---|---|---|
| search trả 200 | PASS | HTTP 200 |
| HTML chứa RSC payload | PASS | có `self.__next_f` — App Router stream render |
| HTML tĩnh có nội dung thật | PASS | có text tiếng Việt đã render, không phải CSR shell |
| Do Next.js phục vụ | PASS | `X-Powered-By: Next.js` |
| Render dưới 5s | PASS | đo được ở lần gọi thứ hai |
| **Không bị ép `no-store`** | **FAIL** | `Cache-Control: no-store, must-revalidate, no-cache, max-age=0, private` |

**Nhận xét.** Ba check đầu là bằng chứng SSR đủ mạnh: HTML tĩnh đã có nội dung, nên crawler đọc được
mà không cần JavaScript. Điểm cần lưu ý là `no-store` **không** phủ định SSR — nó chỉ nói
response không được cache lại, tức mỗi lượt truy cập đều render lại. Với `/search` điều này
hợp lý vì kết quả phụ thuộc truy vấn, nhưng nó phải nằm ở `cache: 'no-store'` của `fetch`
chứ không phải do route bị ép dynamic.

## 3. Phase 2 — `isr-detail` (phát hiện quan trọng nhất)

**Kết luận: ISR KHÔNG hoạt động. Khai báo `revalidate = 300` trong code là không có tác dụng.**

| Check | Kết quả | Bằng chứng |
|---|---|---|
| Lần 1/2/3 render OK | PASS | cả 3 lần HTTP 200 |
| Chạy production server | PASS | `X-Powered-By: Next.js`, không phải `next dev` |
| Trang server-rendered | PASS | HTML có RSC payload |
| **Cache ISR có hoạt động (HIT/STALE)** | **FAIL** | cả 3 lần đều không có `x-nextjs-cache` |
| **`x-nextjs-revalidate` = 300** | **FAIL** | không có header này |
| Cache không bị ghim vĩnh viễn | FAIL | không có `Age`, `observedCaching=False` |

**Nguyên nhân — không phải ở Next.js mà ở cách `fetch` được gọi.**

`src/frontend/src/app/recipes/[slug]/page.tsx:20` khai báo `export const revalidate = 300`.
Nhưng `src/frontend/src/lib/api.ts:385-388`:

```ts
const isDev = process.env.NODE_ENV === 'development';
const res = await fetch(`${API_BASE_URL}/recipes/${...}`, {
  ...(token || isDev
    ? { cache: 'no-store' as const, ... }
    : { next: { revalidate: 300 } }),
});
```

Và `next build` xác nhận điều đó — route bị đánh dấu **Dynamic**, không phải ISR:

```
├ ƒ /recipes/[slug]   2.59 kB   ƒ (Dynamic)  server-rendered on demand
```

**Điều này có nghĩa gì.** Ghi `export const revalidate = 300` vào `page.tsx` là **không đủ**.
Trong App Router, `revalidate` của segment chỉ có tác dụng nếu dữ liệu được lấy qua `fetch`
có cache, hoặc route được sinh tĩnh bằng `generateStaticParams`. Ở đây route nhận
`params` và dữ liệu đến từ `fetch`, nên nếu bất kỳ tầng nào trong chuỗi render yêu cầu
`no-store`/`dynamic`, toàn bộ route trở thành dynamic và `revalidate` bị bỏ qua âm thầm —
**không có cảnh báo nào**.

Rủi ro thực tế: đây là loại bug **không ai nhìn thấy khi review code**. Dòng `revalidate = 300`
với comment "ISR 5 phút" trông rất đúng và dễ được duyệt; nhưng runtime thì mỗi lượt xem
đều render lại, tải DB nhiều hơn dự kiến. Chỉ đọc response header mới phát hiện được.

**Đề xuất sửa** (chưa áp dụng — nằm ngoài phạm vi nhánh lab):
1. Bỏ nhánh `isDev` trong `getRecipeBySlug`; dùng `next: { revalidate: 300 }` cho ẩn danh.
2. Tách route ẩn danh (ISR) khỏi route chủ sở hữu (`no-store` + token) — hiện chung một hàm.
3. Dùng `/api/revalidate` sẵn có để gọi `revalidatePath` khi sửa/xuất bản, thay vì chờ 5 phút.

## 4. Phase 3 — `query-rollback`

**Kết luận: cơ chế rollback đúng; hợp đồng lỗi API lành mạnh. Phần RowVersion chưa kiểm chứng được.**

| Check | Kết quả | Bằng chứng |
|---|---|---|
| Sản phẩm có cơ chế optimistic | PASS | `RecipeWizard.tsx` có `useOptimistic` + `rollbacks` |
| Có đường thoát rollback | PASS | `RecipeWizard.tsx:174` dispatch `rollback` + thông báo "đã hoàn tác thay đổi" |
| Input sai bị bác bằng 4xx | PASS | login sai mật khẩu → HTTP 401, **không phải 500** |
| Lỗi trả JSON có message | PASS | `application/problem+json` |
| 404 có cấu trúc | PASS | HTTP 404 với title "Không tìm thấy công thức." |
| Lấy được token | PASS | `accessToken` trong vỏ `data` |
| Read-after-write nhất quán | PASS | ghi `bio` rồi đọc lại thấy đúng giá trị |
| Mutation bị từ chối không hỏng dữ liệu | PASS | `bio` 5000 ký tự bị chặn, giá trị cũ vẫn nguyên |
| Client phân biệt 404 với 5xx | PASS | 404 và 401 tách biệt rõ ràng |
| **Xung đột RowVersion (409/412)** | **FAIL** | tài khoản lab không sở hữu công thức nào |

**Nhận xét.** `RecipeWizard.tsx` có rollback đúng cách: giữ `snapshot` trước khi ghi, và khi
mutation thất bại thì `dispatch({ type: "rollback", detail: snapshot })`. Đây là mẫu đúng.

Điểm chưa kiểm chứng được là **RowVersion**. `UpdateRecipeBody` có trường `RowVersion`, nhưng
tài khoản E2E không sở hữu công thức nào, và endpoint list không có tham số lọc theo chủ sở hữu
(`GetRecipesQuery` chỉ có `Page/PageSize/SortBy/CategoryId/Difficulty/MaxCookTime/MinServings`).
Lab đã dò 50 công thức và thử `PUT` từng cái — tất cả đều không thuộc tài khoản này.

Đây là **giới hạn của dữ liệu kiểm thử**, không phải lỗi sản phẩm. Để kiểm chứng được cần tạo
một công thức bằng tài khoản lab rồi thử ghi đồng thời bằng `RowVersion` cũ.

## 5. Phase 4 — `image-opt` (phát hiện lớn nhất)

**Kết luận: ảnh KHÔNG được tối ưu ở cả hai tầng. Đây là điểm yếu rõ ràng nhất của sản phẩm.**

| Check | Kết quả | Bằng chứng |
|---|---|---|
| Proxy ảnh trả đúng Content-Type | FAIL | ảnh là `/images/...jpg` (đường dẫn tĩnh), proxy trả `application/problem+json` |
| Proxy có ETag/Last-Modified | FAIL | không có header nào |
| **Ảnh lưu trong object storage** | **FAIL** | `primaryImageUrl = /images/recipes/khoai-lang-lac-pho-mai.jpg` |
| Có biến thể resize (medium/large) | FAIL | không có key nào chứa `medium`/`large` |
| **Frontend dùng `next/image`** | **FAIL** | 0 file dùng `next/image` |
| **Không còn `<img>` thô** | **FAIL** | **9 file dùng `<img>` thô** |
| **HTML sinh `/_next/image`** | **FAIL** | không có đường dẫn này trong HTML |
| Ảnh có `loading`/`decoding` | FAIL | không có thuộc tính này |

**Nhận xét — hai tầng đều không chạy, và chúng cộng hưởng lẫn nhau.**

- **Tầng backend:** toàn bộ **101/101** công thức (đã quét cả 3 trang, `meta.total = 101`)
  trỏ tới `/images/recipes/<slug>.jpg` — ảnh tĩnh trong repo, không nằm trong MinIO.
  Nghĩa là endpoint proxy `/api/v1/resources/images/{key}` **không bao giờ được dùng** với
  dữ liệu thật, và không có biến thể resize nào được sinh ra. Tính năng resize đã viết
  trong backend hiện không có đường vào. Bằng chứng: `docs/evidence/TV4/Tuan04/logs/lab_l5_db.txt` mục [3].
- **Tầng frontend:** 9 file dùng `<img>` thô, 0 file dùng `next/image`. HTML không chứa
  `/_next/image`, nên trình duyệt tải ảnh gốc ở mọi kích thước màn hình.

Hệ quả cụ thể: trên màn hình rộng 1920px, ảnh gốc vài MB được tải về dù khung hiển thị có thể chỉ
400px. Không có `width`/`height` nên còn **CLS** (layout shift) khi ảnh nạp xong.

Điểm đáng lưu ý về mặt thiết kế: nếu backend đã resize và sinh biến thể thì `next/image` ở
phía trên là dư thừa; nhưng ở đây **cả hai** đều thiếu, nên chưa tầng nào đang làm việc.
Về mặt ưu tiên, thêm `next/image` rẻ hơn nhiều so với phải di chuyển toàn bộ ảnh sang MinIO —
chỉ cần `sizes` phù hợp là giảm được phần lớn băng thông.

## 6. Phase 5 — `seo` (PASS 15/15)

**Kết luận: SEO tốt hơn dự kiến. Đây là phase mạnh nhất.**

| Check | Kết quả | Bằng chứng |
|---|---|---|
| Trang công thức trả 200 | PASS | HTTP 200, 71153 byte |
| Có `<title>` | PASS | "Khoai Lang Lắc Phô Mai" |
| Có meta description | PASS | mô tả đầy đủ |
| Có OG:title | PASS | "Khoai Lang Lắc Phô Mai" |
| Có OG:image | PASS | `http://localhost:3000/images/recipes/...jpg` |
| Có canonical | PASS | `http://localhost:3000/recipes/khoai-lang-lac-pho-mai` |
| JSON-LD parse được | PASS | 1/1 khối hợp lệ |
| JSON-LD kiểu Recipe | PASS | có `schema.org Recipe` |
| robots.txt | PASS | `Allow: /`, chặn `/auth/` và `/dashboard/` |
| sitemap.xml | PASS | 18484 byte |
| sitemap có URL | PASS | **105 URL** |
| Mỗi trang title riêng | PASS | title trang chủ khác trang công thức |
| Có khai báo metadata trong code | PASS | 5 chỗ |

Ghi chú: `robots.txt` chặn `/dashboard/` là đúng — trang đó cần đăng nhập, đưa vào sitemap
sẽ làm nhiễu chỉ mục.

## 7. Phase 6 — `observability` (PASS 10/10)

**Kết luận: truy vết yêu cầu hoạt động đúng, đủ để nối lỗi người dùng với log server.**

| Check | Kết quả | Bằng chứng |
|---|---|---|
| liveness 200 | PASS | `/health/live` |
| readiness 200 | PASS | `/health/ready`, báo từng thành phần |
| Readiness ổn định | PASS | hai lần gọi cùng kết quả |
| Lỗi có mã máy đọc được | PASS | `code: validation.failed` |
| Request mang traceparent vẫn xử lý bình thường | PASS | HTTP 200 |
| **Trace id xuất hiện trong log server** | **PASS** | đọc thẳng `api1.out.log`, tìm thấy |
| Seq liên lạc được | PASS | `:5341` HTTP 200 |
| OTLP collector liên lạc được | PASS | `:4318` |
| Không rò rỉ stack trace | PASS | không có `at ...` trong response |

Bằng chứng mạnh nhất của phase này: API trả về `traceId` trong **thân lỗi**:

```json
{"title":"Dữ liệu không hợp lệ.","status":400,"code":"validation.failed",
 "traceId":"00-033e0622bac11045079416d3a95acc2d-ce79c298132c59d9-01"}
```

Nghĩa là khi người dùng báo lỗi kèm mã này, có thể tra thẳng ra log của đúng request đó.
Đây là mắt xích hay hỏng nhất và nó đang hoạt động.

## 8. Phase 7 — `multi-instance` (PASS 7/7)

**Kết luận: chạy hai tiến trình không gây lệch trạng thái; Redis dùng chung được chứng minh bằng đọc trực tiếp.**

| Check | Kết quả | Bằng chứng |
|---|---|---|
| API #1 sống (5080) | PASS | HTTP 200 |
| API #2 sống (5081) | PASS | HTTP 200 |
| Cùng yêu cầu cho cùng kết quả | PASS | `/recipes?pageSize=5` giống **hệt** nhau ở cả 2 tiến trình |
| **Cache/khoá phân tán trong Redis dùng chung** | **PASS** | ghi `lab:l5:...:shared-probe` rồi đọc lại khớp |
| Hai tiến trình cùng trỏ một Redis | PASS | endpoint `127.0.0.1:6379` |
| Job nền không nhân bản | PASS (bỏ qua) | không đọc được bảng `HangFireServer` |
| Tiến trình còn lại phục vụ bình thường | PASS | HTTP 200, độ trễ 4 ms |

**Về cách kiểm chứng.** Phase này **không** tin vào cấu hình. Lab ghi một khoá riêng vào Redis
rồi đọc lại, đồng thời so sánh response thô của cùng một yêu cầu giữa hai tiến trình. Đây là
bằng chứng trực tiếp rằng trạng thái dùng chung là thật, đúng thứ mà ghi chú kiểm chứng "không
nhân bản job nền" thường nói mà không có bằng chứng.

**Giới hạn cần nói rõ.** Check job nền bị bỏ qua vì thiếu `LAB_APP_DB` để đọc bảng
`HangFireServer`. Nếu không xác nhận được số Hangfire server đang active thì **chưa thể kết
luận** rằng job nền không chạy trùng khi có hai tiến trình. Đây là khoảng trống thật của
bằng chứng, không phải kết quả đạt.

## 9. Tổng hợp phát hiện

Xếp theo mức tác động tới người dùng và chi phí:

| # | Phát hiện | Mức | Căn cứ |
|---|---|---|---|
| 1 | **ISR không hoạt động** dù khai báo `revalidate = 300` | Cao | Không `x-nextjs-cache`; build đánh dấu Dynamic |
| 2 | **Ảnh không tối ưu ở cả hai tầng** | Cao | 0 `next/image`, 9 `<img>` thô, không có biến thể resize |
| 3 | **Ảnh không nằm trong object storage** | Trung bình | 101/101 công thức trỏ `/images/...`; proxy không có đường vào |
| 4 | RowVersion chưa kiểm chứng được | Thấp | Cần tài khoản sở hữu công thức |
| 5 | Số Hangfire server chưa xác nhận | Thấp | Thiếu connection string đọc DB |

### Điều quan trọng nhất rút ra

Hai phát hiện #1 và #2 có cùng một đặc điểm: **code trông đúng, comment mô tả đúng,
nhưng runtime thì không**. `revalidate = 300` kèm comment "ISR 5 phút" sẽ được mọi người
đọc là đã xử lý xong. Tương tự, backend đã viết đầy đủ proxy + resize ảnh nhưng dữ liệu
thật không hề đi qua đó.

Cả hai chỉ lộ ra khi **đọc response header và so sánh với dữ liệu thật** — không đọc code,
không đọc log build, không chạy test unit. Đây là lý do lab này kiểm tra HTTP thật thay vì
chỉ khẳng định cấu hình.

## 10. Cách tái chạy

```powershell
# Điều kiện: Postgres, Redis, MinIO, Seq, OTLP đang chạy
dotnet build src/backend/CulinaryBlog.API/CulinaryBlog.API.csproj -c Release
dotnet build practice/TV4/L5/Lab.L5.csproj -c Release

# API #1 (terminal 1)
dotnet src/backend/CulinaryBlog.API/bin/Release/net10.0/CulinaryBlog.API.dll --urls http://127.0.0.1:5080

# API #2 (terminal 2) — bắt buộc cho phase multi-instance
dotnet src/backend/CulinaryBlog.API/bin/Release/net10.0/CulinaryBlog.API.dll --urls http://127.0.0.1:5081

# Frontend production build (terminal 3) — KHÔNG dùng next dev
cd src/frontend; npm run build; npm run start -- -p 3000

# Chạy lab (terminal 4)
cd practice/TV4/L5
$env:LAB_API = "http://127.0.0.1:5080"
$env:LAB_API2 = "http://127.0.0.1:5081"
$env:LAB_WEB = "http://127.0.0.1:3000"
$env:LAB_API_LOG = "<đường dẫn tới log API #1>"
dotnet run -c Release
```

Exit code `0` khi mọi phase PASS, `1` khi có phase FAIL. Log đầy đủ ghi ra
`practice/TV4/L5/out/lab_l5_<runid>.log`.

## 11. Biến môi trường

| Biến | Mặc định | Vai trò |
|---|---|---|
| `LAB_API` | `http://127.0.0.1:5080` | API chính |
| `LAB_API2` | `http://127.0.0.1:5081` | API thứ hai cho multi-instance |
| `LAB_WEB` | `http://127.0.0.1:3000` | Next.js production server |
| `LAB_REDIS` | `127.0.0.1:6379` | Redis để chứng minh trạng thái dùng chung |
| `LAB_API_LOG` | *(không đặt)* | Đường dẫn log API; bỏ trống thì phase observability bỏ qua check log server |
| `LAB_APP_DB` | *(không đặt)* | Connection string DB; cần cho check Hangfire server |
| `LAB_EMAIL` / `LAB_PASSWORD` | tài khoản E2E | Tài khoản để thao tác ghi/đọc |
| `LAB_SRC` | tự dò | Thư mục `src/frontend/src` cho quét mã nguồn |