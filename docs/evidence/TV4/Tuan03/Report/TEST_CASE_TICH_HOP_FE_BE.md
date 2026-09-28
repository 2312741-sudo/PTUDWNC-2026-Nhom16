# Test case soát lỗi tích hợp FE ↔ BE ↔ hạ tầng (TV4)

> **Mục đích**: săn bug chỉ xuất hiện khi **ghép** frontend + backend + hạ tầng thật — loại bug mà
> test backend và test frontend riêng lẽ **đều xanh**.
> **Nguồn**: phát sinh từ [`BAO_CAO_LOI_UPLOAD_ANH_500.md`](./BAO_CAO_LOI_UPLOAD_ANH_500.md).
> **Ngày chạy**: 28/09/2026 · **Kết quả**: **41/41 PASS** sau khi sửa (trước đó `D01` = 500).
> **Phạm vi**: toàn bộ luồng người dùng trên UI: đăng ký → tạo công thức → thêm nguyên liệu/bước →
> **upload ảnh** → publish → khám phá (list/search/sitemap) → sửa/xoá → vòng đời token.

---

## 0. Điều kiện tiên quyết

```powershell
# 1) Hạ tầng (6 container: pg, redis, s3/RustFS, mailhog, seq, nginx)
docker compose -f docker-compose.dev.yml up -d
docker compose -f docker-compose.dev.yml ps          # pg + s3 phải "healthy"

# 2) API — CỐT LÕI: KHÔNG set biến môi trường nào (đây là điều kiện để phát hiện bug cấu hình)
dotnet run --project src/backend/CulinaryBlog.API -- --urls http://localhost:5080

# 3) FE (terminal khác) — cần 2 biến, xem docs/HUONG_DAN_CHAY_TV4.md
$env:NEXT_PUBLIC_API_URL        = "http://localhost:5080/api/v1"
$env:NEXT_PUBLIC_MEDIA_URL      = "http://localhost:5080/api/v1/resources/images"
$env:NEXT_PUBLIC_SITE_URL       = "http://localhost:3000"
npm run dev --prefix src/frontend
```

> **Bài học từ lần chạy đầu**: nếu bạn set sẵn `ConnectionStrings__Database` / `Minio__*` trong
> shell thì bạn **che mất** đúng lớp bug này. Luôn chạy "sạch" (không biến môi trường) trước.

| Công cụ | Vai trò |
|---|---|
| Scalar | `http://localhost:5080/scalar` — danh sách endpoint để test tay (API dùng **Scalar**, không phải Swagger/FastAPI) |
| MailHog | `http://localhost:1025` — email chào mừng sau đăng ký |
| Seq | `http://localhost:5341` — log OTEL |
| RustFS | `http://localhost:9001` — console object storage (user `minioadmin`/`minioadmin`) |
| Hangfire | `http://localhost:5080/hangfire` — **chỉ Admin** (xem block 06) |

Dữ liệu thử (tạo sẵn, không tự tạo file): ảnh PNG 1×1 hợp lệ, ảnh PNG 6 MiB (vượt giới hạn), file `.txt` (không phải ảnh).

---

## 1. Nhóm A — Auth (6 case)

| ID | Test case | Input | Kỳ vọng | Kết quả |
|---|---|---|---|---|
| A01 | Đăng ký tài khoản mới | `{email, password, displayName}` | `201`; role tự gán `Author` | ✅ 201 |
| A02 | Đăng nhập | email + password | `200`, có `accessToken` + `refreshToken` | ✅ 200 |
| A03 | Lấy hồ sơ | `GET /auth/me` + Bearer | `200` | ✅ 200 |
| A04 | Sai mật khẩu | password sai | `401` `auth.invalid_credentials` | ✅ 401 |
| A05 | Không có token | `GET /me/recipes` không header | `401` | ✅ 401 |
| A06 | Token rác | Bearer `abc.def.ghi` | `401` | ✅ 401 |

⚠️ **Bẫy phát hiện lần đầu**: đăng ký **thiếu `displayName` sẽ trả 400** `validation.failed`
(`"Tên không được để trống."`) — `displayName` là bắt buộc dù type là optional.

## 2. Nhóm B — Category (3 case)

| ID | Test case | Kỳ vọng | Kết quả |
|---|---|---|---|
| B01 | `GET /categories` | `200`, `data[]` có phần tử | ✅ 200 |
| B02 | `GET /categories/{slug}` | `200` | ✅ 200 |
| B03 | Tạo category bằng tài khoản thường | `403` (chỉ Admin) | ✅ 403 |

## 3. Nhóm C — Tạo công thức (7 case)

| ID | Test case | Kỳ vọng | Kết quả |
|---|---|---|---|
| C01 | `POST /recipes` đủ trường | `201` + `id`, `slug`, `rowVersion` | ✅ 201 |
| C02 | `title` < 5 ký tự | `400` `validation.failed` | ✅ 400 |
| C03–C04 | Thêm 2 nguyên liệu | `201` mỗi cái | ✅ 201 ×2 |
| C05–C06 | Thêm 2 bước | `201` mỗi cái | ✅ 201 ×2 |
| C07 | `PATCH /steps/reorder` | `200`, thứ tự mới đúng | ✅ 200 |

⚠️ **Bẫy**: `POST /recipes` **không** nhận `ingredients`/`steps`/`slug` — contract là
`title, description, instructions, prepTimeMinutes, cookTimeMinutes, servings, difficulty, categoryId, nutrition?`
(`Program.cs:363`, `CreateRecipeCommand` tại `Recipes.cs:203`). Gửi thừa field ⇒ `400 request.invalid`.
`difficulty` là **enum số** `1=Easy, 2=Medium, 3=Hard, 4=Expert`.

## 4. Nhóm D — Ảnh (7 case) ⭐ nhóm của bug 500

| ID | Test case | Kỳ vọng | Kết quả |
|---|---|---|---|
| D01 | `POST /recipes/{id}/images` (multipart `file`) | `201`; `originalUrl` = key `recipes/{id}/{uuid}.png` | ✅ **201** (trước đây **500**) |
| D02 | `PATCH /recipes/{id}/images/{imageId}` `{altText, isPrimary}` | `200` | ✅ 200 |
| D03 | Proxy ảnh recipe **Draft**, khách | `403 image.forbidden` | ✅ 403 |
| D04 | Proxy ảnh recipe **Draft**, chủ sở hữu + Bearer | `200` | ✅ 200 |
| D05 | Proxy key sai định dạng | `404 image.not_found` | ✅ 404 |
| D06 | Upload ảnh 6 MiB | `400 file.too_large` (giới hạn 5 MiB) | ✅ 400 |
| D07 | Upload file `.txt` | `400 file.invalid_type` | ✅ 400 |

⚠️ **Bẫy**: DTO trả `originalUrl` / `mediumUrl` / `thumbnailUrl` ở dạng **key**, **không có host**
(`RecipeImageDto` tại `RecipeImages.cs:12-22`). Dùng `body.data.key` ⇒ rỗng ⇒ gọi
`/api/v1/resources/images/` ⇒ `400 request.invalid`.

### 4.1 Job resize ngoài request (D23 D2) — kiểm riêng

| Bước | Kỳ vọng | Kết quả |
|---|---|---|
| Ngay sau upload | `mediumUrl`/`thumbnailUrl` = `null` (job chưa xong) | ✅ đúng |
| Sau ~25 s | `mediumUrl` = `..._800x600.png`, `thumbnailUrl` = `..._300x300.png` | ✅ có |
| `GET /api/v1/resources/images/{mediumUrl}` | `200` | ✅ 200 |
| `GET /api/v1/resources/images/{thumbnailUrl}` | `200` | ✅ 200 |

## 5. Nhóm E — Publish & khám phá (8 case)

| ID | Test case | Kỳ vọng | Kết quả |
|---|---|---|---|
| E01 | `PATCH /recipes/{id}/publish` (đã có ≥1 nguyên liệu + ≥1 bước) | `200` | ✅ 200 |
| E02 | `GET /recipes/{slug}` khách (đã publish) | `200`, `status=Published`, `images` có 1 | ✅ 200 |
| E03 | Proxy ảnh **Published** + khách | `200` | ✅ 200 |
| E04 | `GET /recipes?page&pageSize` | `200` | ✅ 200 |
| E05 | `GET /recipes/search?q=` | `200` | ✅ 200 |
| E06 | `GET /recipes/sitemap` | `200` | ✅ 200 |
| E07 | `GET /me/recipes` | `200` | ✅ 200 |
| E08 | `GET /me/recipes/counts` | `200` + `{all, byStatus}` | ✅ 200 |

⚠️ **Bẫy**: phân trang nằm ở `meta` (`{page, pageSize, total, totalPages, hasNextPage, hasPreviousPage}` —
`Pagination.cs`), **không** phải `data.totalItems`. FE đọc đúng `json.meta` (`api.ts:207`).
Publish khi thiếu nguyên liệu/bước ⇒ `422 RECIPE_PUBLISH_INCOMPLETE`.

## 6. Nhóm F — Sửa & xoá (5 case)

| ID | Test case | Kỳ vọng | Kết quả |
|---|---|---|---|
| F01 | `PUT /recipes/{id}` với `rowVersion` **tươi** | `200` | ✅ 200 |
| F02 | `PUT` lại với `rowVersion` **cũ** | `422 recipe.version_conflict` | ✅ 422 |
| F03 | `DELETE /recipes/{id}` | `204` | ✅ 204 |
| F04 | `GET /recipes/{slug}` sau soft delete | `404` | ✅ 404 |
| F05 | Recipe đã xoá không còn trong list public | không xuất hiện | ✅ OK |

⚠️ **Bẫy (đã làm hỏng 1 lần khi soát)**: **mọi** lệnh đổi trạng thái đều làm đổi `rowVersion`
(publish, upload ảnh, sửa, xoá). Lấy `rowVersion` lúc `POST /recipes` rồi `publish` rồi `PUT`
⇒ `422` — đúng thiết kế, không phải bug. Luôn đọc lại `rowVersion` từ `GET /recipes/{slug}`
ngay trước khi `PUT`.

## 7. Nhóm G — Vòng đời token (3 case)

| ID | Test case | Kỳ vọng | Kết quả |
|---|---|---|---|
| G01 | `POST /auth/refresh` | `200`, access token mới | ✅ 200 |
| G02 | `POST /auth/logout` | `204` | ✅ 204 |
| G03 | Dùng lại refresh token sau logout | `401` | ✅ 401 |

## 8. Nhóm H — Health & tài liệu API (2 case)

| ID | Test case | Kỳ vọng | Kết quả |
|---|---|---|---|
| H01 | `GET /openapi/v1.json` | `200` (27 path / 35 operation) | ✅ 200 |
| H02 | `GET /scalar` | `200` (UI Scalar) | ✅ 200 |

⚠️ `GET /hangfire` bằng tài khoản thường ⇒ `403` (đúng). `/health` báo `minio=Healthy` **kể cả khi
credential sai** vì chỉ là TCP probe ⇒ **không dùng `/health` để kết luận storage đúng** (block 04).

---

## 9. Kết quả soát & bug tìm được

| # | Vấn đề | Mức | Trạng thái |
|---|---|---|---|
| 1 | `POST /recipes/{id}/images` → **500 `server.error`** vì thiếu `Minio:*` trong `appsettings.Development.json` | **Blocker** | ✅ **Đã sửa** + test `DevConfigParityTests` |
| 2 | Mật khẩu DB trong `appsettings.Development.json` (`postgres`) lệch `docker-compose.dev.yml` (`admin123`) → `28P01` mọi endpoint cần DB | **Blocker** | ✅ **Đã sửa** (cả hai về default `postgres`) + test parity |
| 3 | Hướng dẫn bảo để trống `NEXT_PUBLIC_MEDIA_URL` — thực tế UI **không** hiện ảnh | Trung bình | ✅ Đã sửa `HUONG_DAN_CHAY_TV4.md` |
| 4 | Xem ảnh recipe **Draft** trong wizard: `<img>` không gửi Bearer ⇒ proxy `403` | Trung bình | ⛔ Block 05 (cần quyết định) |
| 5 | Storage hỏng/credential sai vẫn trả `500` chung chung, không phân biệt lỗi hạ tầng | Trung bình | ⛔ Block 01 |
| 6 | Thiếu credential ⇒ API vẫn khởi động "thành công", lỗi lộ ra lúc user bấm nút | Trung bình | ⛔ Block 02 |
| 7 | `.env.example` liệt kê `Minio__*` nhưng app **không** nạp `.env` | Thấp | ✅ **Block 03 đã gỡ** (thêm `DotNetEnv` + `EnvFileLoader`) |
| 8 | `/health` kiểm storage bằng TCP ⇒ báo Healthy khi sai credential | Thấp | ⛔ Block 04 |
| 9 | Không có user Admin ⇒ không ai test được `/hangfire` và CRUD category | Thấp | ⛔ Block 06 |

Không phát hiện lỗi nào ở luồng auth, concurrency (RowVersion), soft-delete, proxy quyền truy cập
ảnh, hay job resize — 41/41 PASS.

> **Lưu ý khi chạy lại (sau 28/09/2026):** script QA và API giờ đọc `.env` ở thư mục gốc.
> Trên máy có volume `culinaryblog_pg_data` cũ (`admin123`) thì **phải có `.env`**, nếu không app rơi
> về default `postgres` và mọi case cần DB sẽ `FAIL` với `28P01`. Trên máy mới clone (volume mới) thì
> chạy được ngay mà không cần `.env`. Chi tiết: [`DE_XUAT_03_NAP_FILE_DOT_ENV.md`](./DE_XUAT_03_NAP_FILE_DOT_ENV.md) §8.

## 10. Cách chạy lại nhanh (không cần viết script tay)

Bộ test case trên được viết thành script PowerShell tạm ở
`%TEMP%\opencode\qa_flow.ps1` (dùng `System.Net.Http.HttpClient` vì PowerShell 5.1 **không có**
`Invoke-RestMethod -Form`). Script in đúng định dạng cột `PASS/FAIL` như trên và tự tạo dữ liệu
ngẫu nhiên mỗi lần chạy (email + slug có timestamp ⇒ chạy lại không đụng dữ liệu cũ).

Khi nhận bàn giao, script nên được chuyển thành **k6 script hoặc test .NET** để chạy trong CI —
hiện là script tạm, chưa commit vào repo.
