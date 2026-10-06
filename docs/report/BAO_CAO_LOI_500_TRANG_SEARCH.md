# Báo cáo lỗi: `GET /search?q=...` trả **500** — "Event handlers cannot be passed to Client Component props"

> **Người phát hiện & phân tích**: Nguyễn Hữu Trung Sơn (2312739 — TV4)
> **Ngày phát hiện**: 30/09/2026
> **Ngày xác minh/kết luận**: 30/09/2026
> **Mức độ**: **Blocker** — chặn toàn bộ trang kết quả tìm kiếm (`/search`) với từ khóa ≥ 2 ký tự
> **Phân loại**: **Lỗi kiến trúc React Server Component (RSC)**, *không* phải lỗi logic nghiệp vụ
> **Phạm vi**: `/search` — SSR kết quả FTS + filter/sort/pagination — B3/B5, FR-SRCH-001…004
> **Chủ sở hữu theo phân công**: **TV2 — Ngô Quốc Trường Vĩ (2312796)**
> **Reviewer theo contract**: TV1 — Nguyễn Thanh Tâm (nhóm trưởng)

> ⚠️ **ĐÂY LÀ BÁO CÁO CHỈ XÁC ĐỊNH NGUYÊN NHÂN — CHƯA SỬA.** Toàn bộ mô tả dưới đây ghi lại
> **đúng trạng thái ngày 30/09/2026** (commit `5ff64fc` đang nằm trên `main`) và được giữ nguyên
> làm bằng chứng gốc. §7 chỉ là **đề xuất**, chưa áp dụng.

---

## 0. Tóm tắt một nén

| Hạng mục | Kết quả |
|---|---|
| Triệu chứng | Mở `http://localhost:3000/search?q=gà` → `HTTP 500`, console Next.js in `Error: Event handlers cannot be passed to Client Component props.` kèm `digest: '3617035611'` |
| Request lỗi | `GET /search?q=g%C3%A0` → `500 in 1084ms` |
| **Nguyên nhân gốc** | **`src/frontend/src/app/search/page.tsx` là Server Component (async, không có `'use client'`) nhưng lại truyền `onChange={(e) => {...}}` — một arrow function — cho 3 thẻ `<select>`** (dòng 161, 184, 208). RSC không serialize được function qua ranh giới Server → Client ⇒ React ném lỗi ngay lúc render ⇒ Next.js trả 500 |
| File lỗi | `src/frontend/src/app/search/page.tsx` (đúng 1 file, 3 chỗ) |
| Commit đưa lỗi vào | `5ff64fc` — 29/09/2026 — `2312796-NgoQuocTruongVi` — *"feat(TV2): complete week 3 search SSR, caching, FTS migration and tests"* |
| Lỗi backend / API / DB? | **Không** — API `/recipes/search` không liên quan; lỗi xảy ra ở bước render React, sau khi đã gọi API xong |
| Điều kiện kích hoạt | Chỉ khi `q.length >= 2`. `/search` (không q) và `/search?q=a` vẫn **200 OK** |
| Phạm vi lan toả | Đã quét 21 file `.tsx` trong `app/`: **chỉ 1 file** mắc lỗi này. 9 file còn lại có handler đều đã có `'use client'` |
| Tình trạng trên `origin/main` | ⛔ **Còn nguyên** (đã kiểm tra `edb6841` — 30/09/2026). Chưa ai sửa |
| Có bị `next build` / CI bắt không? | ⛔ **Không** — xem §5 |

---

## 1. Thông điệp lỗi và cách đọc

```
⨯ Error: Event handlers cannot be passed to Client Component props.
  <select name="sortBy" defaultValue=... onChange={function onChange} className=... children=...>
                                                  ^^^^^^^^^^^^^^^^^^^
If you need interactivity, consider converting part of this to a Client Component.
    at stringify (<anonymous>) {
  digest: '3617035611'
}
 GET /search?q=g%C3%A0 500 in 1084ms
```

Ba dòng này nói đủ ý:

1. **`Event handlers cannot be passed to Client Component props`** — React đang ở bước
   `stringify` (serialize props để gửi từ server sang client) và gặp một **function** trong
   props. Function không serialize được trong Flight payload ⇒ lỗi.
2. **`onChange={function onChange}`** — đúng loại prop gây lỗi. `defaultValue` (chuỗi),
   `className` (chuỗi), `children` (mảng phần tử) đều serialize được; **chỉ `onChange` là
   function** nên lỗi nằm chỗ này.
3. **`GET /search?q=... 500`** — lỗi xảy ra trong lúc **server render** (không phải lúc
   hydration ở client), nên Next.js trả HTTP 500 cho request, không phải trang lỗi render
   đẹp.

> **Vì sao thông báo lại chỉ ra `name="sortBy"`?**
> `sortBy` là thẻ `<select>` **thứ ba** trong file (dòng 206). React duyệt props theo thứ tự
> và ném lỗi ở handler không serialize được **đầu tiên nó đi tới**; do đó con trỏ chỉ vào
> dòng 206. **Cả 3 thẻ đều cùng lỗi** — sửa mỗi thẻ một cái là chưa đủ. Xem bảng §3.

---

## 2. Nguyên nhân gốc (chi tiết kỹ thuật)

### 2.1 File nạn nhân là Server Component

`src/frontend/src/app/search/page.tsx`

| Dòng | Nội dung | Ý nghĩa |
|---|---|---|
| 1–5 | `import type { Metadata } from 'next';` … | **Không có chỉ thị `'use client'` ở dòng đầu** (đã grep: 0 kết quả) |
| 20 | `export async function generateMetadata(...)` | Hàm bất đồng bộ chỉ chạy được ở server |
| 37 | `export default async function SearchPage({ searchParams })` | `async` + dùng `await searchParams` ⇒ **bắt buộc là Server Component** |
| 48 | `const categories = await getCategories();` | Gọi API ngay trong render |

Vì là Server Component, React render nó thành **RSC payload** rồi gửi sang trình duyệt. Mọi
prop đi qua ranh giới Server → Client đều phải **serialize được** (JSON-ish).

### 2.2 Ba prop không serialize được

| Dòng | Thẻ | `name` | Handler | Dạng |
|---|---|---|---|---|
| 158–173 | `<select>` | `categoryId` | `onChange={(e) => { const form = e.target.form; if (form) form.requestSubmit(); }}` (dòng **161**) | arrow function |
| 181–196 | `<select>` | `difficulty` | cùng nội dung (dòng **184**) | arrow function |
| 205–217 | `<select>` | `sortBy` | cùng nội dung (dòng **208**) | arrow function |

Ví dụ đầy đủ (dòng 205–212):

```tsx
<select
  name="sortBy"
  defaultValue={sortBy}
  onChange={(e) => {                     // ← dòng 208: FUNCTION
    const form = e.target.form;
    if (form) form.requestSubmit();
  }}
  className="py-1.5 px-2.5 bg-white ..."
>
```

`<select>` là **host element** (phần tử DOM) — nó tồn tại ở phía client. Truyền `onChange`
(biến tương tác) cho nó từ Server Component chính là trường hợp mà React **cố tình từ chối**
thay vì âm thầm bỏ qua — vì nếu âm thầm bỏ qua thì bộ lọc sẽ "chết" mà không ai hiểu vì sao.

### 2.3 Chuỗi nhân quả đầy đủ

```
Commit 5ff64fc thêm 3 onChange vào search/page.tsx (Server Component)
        ↓
Request GET /search?q=gà  (q.length = 2  →  q.length >= 2  →  true  →  render filter bar)
        ↓
await getCategories() và await searchRecipes(...) chạy xong, bắt đầu dựng JSX
        ↓
React serialize props cho <select name="sortBy">  →  gặp onChange là function
        ↓
React throw: "Event handlers cannot be passed to Client Component props."  (digest 3617035611)
        ↓
Next.js: không render được → HTTP 500 cho GET /search?q=gà
```

**Kết luận:** lỗi **không nằm ở API `/recipes/search`, không nằm ở FTS, không nằm ở MinIO,
không nằm ở auth.** Nó là lỗi RSC thuần túy: dùng cú pháp Client Component trong
Server Component.

---

## 3. Bảng đối chiếu từng chỗ (đủ 3, không chỉ 1)

| # | Dòng `onChange` | `name` | `<form>` bao quanh | Trạng thái |
|---|---|---|---|---|
| 1 | **161** | `categoryId` | 155–174, `action="/search" method="GET"` | ⛔ Lỗi |
| 2 | **184** | `difficulty` | 178–196, `action="/search" method="GET"` | ⛔ Lỗi |
| 3 | **208** | `sortBy` | 201–218, `action="/search" method="GET"` | ⛔ Lỗi |
| — | — | `sortOrder` | — | ⚠️ **Không có** thẻ `<select name="sortOrder">`; tham số này chỉ đến từ hidden input dòng 119 nên **luôn luôn là `desc`** khi vào từ UI |

Lưu ý số 3 là **quan sát phụ**, không phải nguyên nhân lỗi 500: UI không cho đổi chiều sắp
xếp (tăng/giảm), dù API và `buildQueryUrl()` (dòng 80) đã hỗ trợ `sortOrder`.

---

## 4. Vì sao lỗi "mới xuất hiện" khi chạy web — 3 lý do cộng lại

Mỗi lý do riêng lẻ đủ để giấu lỗi; phải cả ba mới giải thích vì sao code sai lọt vào `main`
mà không ai thấy.

### 4.1 Lý do 1 — Code sai mới được thêm vào 29/09

| | |
|---|---|
| Commit | `5ff64fc` |
| Ngày | 29/09/2026 |
| Tác giả | `2312796-NgoQuocTruongVi` (TV2 — Ngô Quốc Trường Vĩ) |
| Subject | `feat(TV2): complete week 3 search SSR, caching, FTS migration and tests` |

Lịch sử của `src/frontend/src/app/search/page.tsx`:

| Commit | Ngày | Nội dung liên quan handler |
|---|---|---|
| `ae7de7e` | 16/09 | Trang search SSR cơ bản — **không có** event handler nào |
| `e9c1241` | 24/09 | Đổi màu giao diện — **không có** event handler nào |
| `5ff64fc` | **29/09** | Thêm filter bar + **3 `onChange`** + `generateMetadata` + phân trang |

Xác nhận bằng `git log -S "onChange"`:
```
5ff64fc  2026-09-29  2312796-NgoQuocTruongVi  feat(TV2): complete week 3 search SSR, caching, FTS migration and tests
```
→ `5ff64fc` là commit **duy nhất** từng chạm vào chuỗi `onChange` trong file này.
Trước đó file **không có khả năng phát sinh lỗi này**.

### 4.2 Lý do 2 — Route này **không** được prerender lúc build

`SearchPage` đọc `searchParams` (`await searchParams`, dòng 38) ⇒ App Router xem `/search` là
**dynamic route** và **bỏ qua bước static prerendering**. Hệ quả:

- `next build` **không** render trang này ⇒ không thấy lỗi lúc build.
- Handler chỉ được thực thi khi **có request thật** ⇒ lỗi chỉ lộ ra lúc mở trang trong trình duyệt.

Thêm nữa, CI của repo **không hề build frontend**:

`.github/workflows/backend.yml` chỉ chạy:
```
dotnet restore --locked-mode
dotnet build --configuration Release
dotnet format --verify-no-changes
dotnet test --blame-hang-timeout 10m --collect:"XPlat Code Coverage"
```
→ Không có `npm ci`, không có `next build`, không có `next lint`. **Toàn bộ frontend không
được kiểm tra tự động**, nên một lỗi 500 ở mọi request là hoàn toàn có thể merge.

> **Điểm cần xác minh khi sửa:** chạy `npm run build` trong `src/frontend` để biết chắc
> `next build` có bắt được lỗi này hay không. Khả năng cao là **vẫn xanh** (vì route
> dynamic không prerender), và đó chính là lý do CI cần một smoke test HTTP thay vì chỉ build.

### 4.3 Lý do 3 — Khối lỗi nằm trong điều kiện `q.length >= 2`

Dòng 138: `{q.length >= 2 && ( ...toàn bộ filter bar chứa 3 thẻ lỗi... )}`

| URL | `q` | `q.length >= 2` | Kết quả |
|---|---|---|---|
| `/search` | `''` | false | ✅ 200 — chỉ hiện ô nhập từ khóa |
| `/search?q=a` | `'a'` | false | ✅ 200 — hiện cảnh báo *"Từ khóa tìm kiếm phải có từ 2 ký tự trở lên."* (dòng 58) |
| `/search?q=gà` | `'gà'` | **true** | ⛔ **500** — đúng case đang gặp |
| `/search?q=pho` | `'pho'` | **true** | ⛔ **500** |

`g%C3%A0` trong log chính là `gà` (UTF-8) — **2 ký tự**. Vì vậy mọi lần vào trang tìm kiếm
từ thanh header đều lỗi, nhưng ai cũng thấy trang `/search` chạy bình thường nếu chỉ mở
trang trắng.

---

## 5. Vì sao lỗi lọt qua kiểm thử

| Cổng kiểm tra | Có bắt được không? | Lý do |
|---|---|---|
| `dotnet build` / `dotnet test` (CI) | ❌ Không | Không liên quan frontend |
| `npm run build` (`next build`) | ❌ Không *(theo phân tích ở §4.2 — cần xác minh)* | Route đọc `searchParams` ⇒ dynamic ⇒ không prerender |
| `next lint` | ⚠️ Không chắc | ESLint của Next không có rule cấm event handler trong Server Component; `react-hooks` rules chỉ áp dụng cho file có `'use client'` |
| TypeScript `tsc --noEmit` | ❌ Không | `onChange` trên `<select>` là prop **hợp lệ** theo `JSX.IntrinsicElements` — TS không phân biệt được Server/Client Component |
| Test E2E Playwright (5 luồng bắt buộc) | ❌ Chưa có | `KE_HOACH_DU_AN.md:68` yêu cầu *"Demo 5 luồng E2E bắt buộc: đăng ký, đăng nhập, tạo công thức, xuất bản, **tìm kiếm**"*; `PHAN_CHIA_CONG_VIEC_6_TUAN.md:188` ghi *"5 E2E flows Playwright chưa có"*. Luồng "tìm kiếm" là luồng **bắt buộc** và chính là luồng đang hỏng |
| Test unit backend của TV2 | ❌ Không | Bug thuần frontend, không có test FE nào |

> **Bài học cho nhóm:** lỗi này lọt vì (a) frontend không có cổng CI nào, (b) route dynamic
> không bị build kiểm tra, (c) luồng E2E "tìm kiếm" — luồng bắt buộc — chưa được viết.
> Cả ba đều là **gap kiểm thử**, không chỉ là lỗi của một người.

---

## 6. Phạm vi ảnh hưởng

### 6.1 Chức năng bị chặn

| Hạng mục | Trạng thái |
|---|---|
| `/search?q=<≥2 ký tự>` — mọi tìm kiếm thật | ⛔ **500**, không dùng được |
| Lọc theo danh mục (FR-SRCH-002) | ⛔ Không dùng được (UI 500) |
| Lọc theo độ khó (FR-SRCH-002) | ⛔ Không dùng được (UI 500) |
| Sắp xếp (FR-SRCH-003) | ⛔ Không dùng được (UI 500) |
| Phân trang (FR-SRCH-004) | ⛔ Không dùng được (UI 500) |
| `/recipes` (danh sách + filter/sort) | ✅ Vẫn chạy — dùng `<Link>`, không có handler |
| `/search` (trang trắng) | ✅ Vẫn chạy |
| Trang chủ, `/recipes/[slug]`, dashboard | ✅ Không liên quan |

### 6.2 Lỗi này có ở chỗ khác không? — Đã quét toàn bộ, **không**

Quét 21 file `.tsx` trong `src/frontend/src/app/`, phân loại theo `'use client'`:

| Phân loại | Số file | Có `on*` handler | Kết luận |
|---|---|---|---|
| Client Component (có `'use client'`) | 10 | 9 file (tổng 77 handler) | ✅ Hợp lệ |
| Server Component | 11 | **1 file** | ⛔ `app/search/page.tsx` (3 handler) |

⇒ **Đây là điểm lỗi cô lập**, không phải lỗi hệ thống lan rộng. Chỉ cần sửa một file.

---

## 7. Hướng sửa — **ĐỀ XUẤT, CHƯA THỰC HIỆN**

> Yêu cầu: *"chưa cần sửa, chỉ cần tìm ra nguyên nhân"*. Phần này ghi lại để nhóm chọn hướng.

### Phương án A — Bỏ `<select onChange>`, dùng `<Link>` (khuyến nghị)

Trang `src/frontend/src/app/recipes/page.tsx` **đã làm đúng** trong cùng dựng: cùng là Server
Component, cùng là bộ lọc/sắp xếp, nhưng dùng `<Link>` + hàm `buildUrl()`:

```tsx
// recipes/page.tsx:86-109 — CÁCH LÀM ĐÚNG, đã chạy được
<Link href={buildUrl({ sortBy: 'createdAt', sortOrder: 'desc', page: 1 })}>Mới nhất</Link>
<Link href={buildUrl({ sortBy: 'cookTimeMinutes', sortOrder: 'asc', page: 1 })}>Nhanh nhất</Link>
<Link href={buildUrl({ sortBy: 'title', sortOrder: 'asc', page: 1 })}>Tên A-Z</Link>
```

Áp dụng: bỏ 3 thẻ `<select>`, thay bằng 3 nhóm `<Link>` (dùng `buildQueryUrl()` sẵn có ở
dòng 72–92), ẩn luôn 3 `<form method="GET">` bao quanh.

| Ưu | Nhược |
|---|---|
| Giữ nguyên kiến trúc SSR, **không tăng JS** tới trình duyệt | Đổi UI: nút bấm thay dropdown |
| Đúng convention của trang `/recipes` | Cần chỉnh lại copy/ghi chú mô tả bộ lọc |
| Không phát sinh lỗi RSC | Tốn công sửa UI hơn 2 phương án còn lại |

### Phương án B — Tách 3 bộ lọc sang Client Component

Tạo file mới (vd. `src/frontend/src/app/search/SearchFilters.tsx`) có `'use client'`, nhận
`categories`, `defaultValues` qua props (đều serialize được), chứa 3 thẻ `<select onChange>`.

| Ưu | Nhược |
|---|---|
| Giữ nguyên UI dropdown, giữ trải nghiệm `requestSubmit()` | Tăng JS gửi xuống client |
| Đúng hướng dẫn trong chính thông báo lỗi | Phải cẩn thận: **không** truyền function nào từ Server sang Client |
| Tái dùng được cho `/recipes` sau này | Cần 2 file thay vì 1 |

### Phương án C — Bỏ handler, dùng `<select>` + nút "Áp dụng"

Bỏ `onChange`, thêm `<button type="submit">Áp dụng</button>` vào mỗi `<form>`; bấm nút thì
form GET submit → Next.js re-render server-side.

| Ưu | Nhược |
|---|---|
| Sửa ít dòng nhất, giữ gần như nguyên bản | Thêm 1 thao tác bấm cho mỗi lần đổi bộ lọc |

### So sánh nhanh

| | A — `<Link>` | B — Client Component | C — Nút "Áp dụng" |
|---|---|---|---|
| Sửa được lỗi 500 | ✅ | ✅ | ✅ |
| JS gửi tới client | Không tăng | Tăng | Không tăng |
| Trùng convention `/recipes` | ✅ | Không | Không |
| Số file cần sửa | 1 | 2 | 1 |
| Giữ UI dropdown | ❌ | ✅ | ✅ |

**Khuyến nghị: Phương án A** — sửa 1 file, không tăng JS, đúng convention đang chạy ổn ở
`/recipes`, và khớp yêu cầu SEO/SSR của B3. Nếu nhóm muốn giữ dropdown thì chọn B.

### Việc cần làm **ngoài** phạm vi sửa lỗi

| # | Việc | Lý do |
|---|---|---|
| V1 | Thêm `npm ci` + `next build` (hoặc `next lint`) vào CI | Không có cổng nào kiểm tra frontend (§5) |
| V2 | Viết E2E Playwright cho luồng "tìm kiếm" | Luồng bắt buộc theo `KE_HOACH_DU_AN.md:68`, hiện chưa có |
| V3 | Thêm rule lint cấm `on*` trong file không có `'use client'` | Chặn tái phát |
| V4 | Xác minh `next build` có bắt được lỗi này không (§4.2) | Quyết định V1 có đủ hay không |

---

## 8. Test case xác minh (dùng sau khi sửa)

| # | Bước | Kỳ vọng |
|---|---|---|
| TC1 | Mở `http://localhost:3000/search` | `200`, hiện ô nhập từ khóa |
| TC2 | Mở `/search?q=a` | `200`, hiện cảnh báo *"phải có từ 2 ký tự trở lên"* |
| TC3 | **Mở `/search?q=gà`** | **`200`, hiện danh sách kết quả — đây là case đang lỗi** |
| TC4 | Mở `/search?q=pho` (không dấu) | `200`, tìm được "Phở Bò…" (kiểm tra luôn unaccent) |
| TC5 | Từ `/search?q=gà` chọn danh mục | `200`, URL có `categoryId`, kết quả thu hẹp đúng |
| TC6 | Từ `/search?q=gà` chọn độ khó | `200`, URL có `difficulty` |
| TC7 | Từ `/search?q=gà` chọn "Tên món A-Z" | `200`, URL có `sortBy=title`, kết quả sort đúng |
| TC8 | Kết quả > 1 trang → bấm "Trang sau" | `200`, URL có `page=2`, giữ nguyên `q` và bộ lọc |
| TC9 | Console trình duyệt | **Không** có `Event handlers cannot be passed to Client Component props` |
| TC10 | Bấm nút "Tìm kiếm" ở header sau khi đã lọc | `q`, `categoryId`, `difficulty`, `sortBy` được giữ qua hidden input (dòng 116–119) |
| TC11 | Chạy `npm run build` trong `src/frontend` | Xác nhận kết quả (xem V4) |
| TC12 | Chạy E2E luồng "tìm kiếm" | **PASS** — đây là luồng E2E bắt buộc thứ 5 |

---

## 9. Đối chiếu phân công

| Nguồn | Nội dung |
|---|---|
| `KE_HOACH_DU_AN.md:55` | TV2 = 2312796 — Ngô Quốc Trường Vĩ |
| `KE_HOACH_DU_AN.md:229` | Route `/search` → **SSR kết quả FTS** → phụ trách **TV2** |
| `KE_HOACH_DU_AN.md:292` | B3 = "FTS migration unaccent/pg_trgm/config/trigger/GIN; search handler/SSR UI" |
| `KE_HOACH_DU_AN.md:423-426` | FR-SRCH-001…004 (search, filter, sort, page) → **TV2** |
| `PHAN_CHIA_CONG_VIEC_6_TUAN.md:23` | TV2 review bởi **Nguyễn Thanh Tâm (nhóm trưởng)** |
| `PHAN_CHIA_CONG_VIEC_6_TUAN.md:64` | Tuần 3 — **B3, B5, B6**: *"Search SSR hoàn chỉnh; Redis category/search, OutputCache/ISR isolation"* |
| `PHAN_CHIA_CONG_VIEC_6_TUAN.md:186` | *"TV2 hoàn tất Search & Cache Tuần 3 qua **PR #18** (đã merge)"* — chính PR này chứa `5ff64fc` |

⇒ **Chủ sở hữu: TV2 — Ngô Quốc Trường Vĩ (2312796).** Sửa và merge vào nhánh TV2, review bởi
TV1 — Nguyễn Thanh Tâm. TV4 chỉ phát hiện khi chạy thử và ghi báo cáo này.

---

## 10. Thông tin môi trường đã dùng để xác minh

| Thành phần | Phiên bản |
|---|---|
| `next` | 15.1.11 |
| `react` / `react-dom` | 19.3.0 |
| Node | theo `package.json` engines (chưa kiểm tra phiên bản cụ thể) |
| Backend | `.NET 10` — **không liên quan**, lỗi ở tầng render React |
| Commit của lỗi | `5ff64fc` (29/09/2026) |
| Branch | `main`, HEAD = `607ee14` (`main` **chậm hơn** `origin/main` 2 commit: `c65df67`, `edb6841` ngày 30/09) |
| Xác nhận còn lỗi trên `origin/main` | ✅ Đã kiểm tra — `5ff64fc` và 3 `onChange` vẫn còn nguyên |

---

**Kết luận:** `/search` 500 do `src/frontend/src/app/search/page.tsx` là Server Component
nhưng truyền `onChange` (function) cho 3 thẻ `<select>` tại dòng 161, 184, 208. Lỗi thuộc
phạm vi B3/B5 của **TV2**, phát sinh từ commit `5ff64fc` ngày 29/09/2026, chưa được sửa và
đã có mặt trên `origin/main`. **Chưa sửa** — chờ nhóm chọn hướng ở §7.
