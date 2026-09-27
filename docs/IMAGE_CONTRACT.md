# Image API Contract — Tuần 2 (TV4: Nguyễn Hữu Trung Sơn)

Trạng thái: **Được triển khai 22/09/2026 (block 2.1 gỡ một phần: wiring + migration + envelope; endpoints D1.3 làm ngay)** — cập nhật theo PR TV3 `5b36251`
Phụ trách: **Nguyễn Hữu Trung Sơn (2312739 — TV4)**
Reviewer: **TV3 (Recipe) — tích hợp editor ảnh D4/C4**
Prefix API: `/api/v1/recipes/{id}/images`
OpenAPI tag: `RecipeImages`

> Ghi chú bản sửa đổi 22/09/2026: contract giữ nguyên shape (DTO/endpoints/lỗi) — chỉ cập nhật trạng thái triển khai theo thay đổi wiring từ TV3. Các phiên bản trước lưu trong git.

---

## 1. Danh sách Endpoints

| Method | Route | Quyền truy cập | Thành công | Lỗi chính | Mô tả |
|---|---|---|---|---|---|
| `POST` | `/api/v1/recipes/{id}/images` | Owner / Admin | `201 Created` | `400`, `401`, `403`, `404`, `422` | Upload ảnh mới cho recipe. Ảnh đầu tiên tự động thành `isPrimary = true`. File qua validator magic bytes + size. |
| `PATCH` | `/api/v1/recipes/{id}/images/{imageId}` | Owner / Admin | `200 OK` | `400`, `401`, `403`, `404`, `422 (race)` | Cập nhật `isPrimary` / `altText` / `orderIndex`. Set primary qua transaction + RowVersion (D19). |
| `DELETE` | `/api/v1/recipes/{id}/images/{imageId}` | Owner / Admin | `204 NoContent` | `401`, `403`, `404` | Xoá metadata + object file trong MinIO (không để orphan). |

> **Ghi chú (SRS v1.1.1):**
> - Response thành công bọc trong `{ "data": ... }` (C08).
> - `imageId` luôn phải thuộc `recipeId` trên URL — không cho phép thao tác cross-recipe.
> - Non-owner → `403`; không tin `authorId` từ client (D12).
> - Draft/Archived metadata **không** lộ qua public API.

### Đường dẫn object trong MinIO (FR-RCP-008)

```
recipes/{recipeId}/{uuid}.{ext}
```

Ví dụ: `recipes/3fa85f64-5717-4562-b3fc-2c963f66afa6/9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d.jpg`

- `recipeId` = Guid của recipe sở hữu ảnh.
- `uuid` = `Guid.NewGuid():N` (không dấu gạch ngang).
- `ext` = `.jpg/.png/.webp/.avif` lấy từ định dạng thật (magic bytes), không lấy từ tên file client.

---

## 2. Quy tắc Upload (FR-FILE-001/002)

| Tiêu chí | Giá trị |
|---|---|
| Định dạng cho phép | JPEG, PNG, WebP, AVIF (4 MIME) |
| Kích thước tối đa | **≤ 5 MiB** |
| Xác thực nội dung | **Magic bytes** đối chiếu với nội dung file (**không tin** `Content-Type` header) |
| Ảnh đầu tiên | Tự động `isPrimary = true` |
| AltText | Tùy chọn, ≤ 200 ký tự |
| URL | ≤ 500 ký tự |

Magic bytes được nhận diện:
- JPEG: `FF D8 FF`
- PNG: `89 50 4E 47 0D 0A 1A 0A`
- WebP: `RIFF (size) WEBP`
- AVIF: `(size) ftypavif` / `(size) ftypavis`

---

## 3. Schema DTO

### `RecipeImageDto`
```json
{
  "id": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "recipeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "originalUrl": "recipes/3fa85f64-5717-4562-b3fc-2c963f66afa6/9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d.jpg",
  "mediumUrl": null,
  "thumbnailUrl": null,
  "altText": "Phở bò truyền thống",
  "isPrimary": true,
  "orderIndex": 0,
  "createdAt": "2026-09-17T01:00:00Z",
  "updatedAt": "2026-09-17T01:00:00Z"
}
```

*Ghi chú:* `originalUrl` chứa **key** (đường dẫn tương đối) của object. Cách biến key thành URL trình duyệt tùy quyết định bucket policy (D27) — xem mục 5.

### `PATCH /recipes/{id}/images/{imageId}` — Request Body
```json
{
  "altText": "Phở bò tái chín",
  "isPrimary": true
}
```
*Ràng buộc:*
- `altText`: Tùy chọn, ≤ 200 ký tự.
- `isPrimary`: Tùy chọn boolean; truyền `true` để chuyển ảnh này thành ảnh chính (tự bỏ primary của ảnh khác).
- `orderIndex`: Tùy chọn, số nguyên ≥ 0.

---

## 4. Quy ước Lỗi & Problem Details (RFC 7807)

Mọi phản hồi lỗi dùng `Content-Type: application/problem+json` và header `X-Correlation-ID`:

| HTTP Status | Extension `code` | Tình huống |
|---|---|---|
| `400 Bad Request` | `validation.failed` | Dữ liệu đầu vào vi phạm FluentValidation (altText > 200...). |
| `400 Bad Request` | `file.empty` | File rỗng. |
| `400 Bad Request` | `file.too_large` | File > 5 MiB (FR-FILE-002). |
| `400 Bad Request` | `file.invalid_type` | Không phải JPEG/PNG/WebP/AVIF hoặc `Content-Type` khai báo không khớp magic bytes. |
| `401 Unauthorized` | `http.401` | Thiếu/token JWT không hợp lệ. |
| `403 Forbidden` | `http.403` | Không phải author của recipe hoặc không phải Admin. |
| `404 Not Found` | `recipe.not_found` | Không tìm thấy recipe với `{id}`. |
| `404 Not Found` | `image.not_found` | Không tìm thấy ảnh; hoặc `imageId` không thuộc `recipeId` (cross-recipe). |
| `422 Unprocessable Entity` | `recipe.version_conflict` | Race khi set primary — RowVersion (D19) không khớp; trả về bản mới nhất để client reload. |

---

## 5. Phục vụ ảnh (D27 — ✅ chốt 27/09: PA-2 proxy có auth)

- Mặc định bucket **private**. Draft/Archived không được phục vụ public.

### Quyết định chốt: PA-2 — base media URL qua proxy có auth

- **Endpoint**: `GET /api/v1/resources/images/{key}` với `key` = đường dẫn object (vd `recipes/{recipeId}/{uuid}.jpg`).
- **Quy tắc truy cập**:
  | Trạng thái recipe | Khách chưa đăng nhập | Owner / Admin (Bearer) |
  |---|---|---|
  | **Published** | ✅ `200` + `Cache-Control: public, max-age=3600` | ✅ `200` |
  | **Draft / Archived / soft-deleted-unpublished** | ❌ `403 image.forbidden` | ✅ `200` + `Cache-Control: no-store` |
  | Recipe không tồn tại / key sai định dạng / recipe đã soft-delete | ❌ `404 image.not_found` | ❌ `404 image.not_found` |
- **Stream**: đọc object MinIO (stat → get) → trả về với `Content-Type` đúng (từ object stat, fallback theo extension `.jpg/.png/.webp/.avif`); không đổi bucket policy.
- **Triển khai**: `MinioStorageService` thêm `IObjectStorageReader.ReadAsync` (TÁCH khỏi `IFileStorageService`/`StoredFile` — giữ nguyên contract bàn giao TV3). Endpoint đặt tại `Program.cs` (`GET /api/v1/resources/images/{**key}`).
- **FE**: đặt `NEXT_PUBLIC_MEDIA_URL=https://<host>/api/v1/resources/images` → `mediaUrl(key)` của TV3 = `${NEXT_PUBLIC_MEDIA_URL}/${key}` hoạt động nguyên vẹn, không cần sửa code.
- **Kiểm thử** (E2E `ImageProxyD27Tests`, 6/6): Published public + cache-header; Draft/Archived → anonymous 403 / owner 200; non-owner member 403; invalid/unknown/deleted key → 404.

---

## 7. Resize ảnh (D2 — ✅ chốt 27/09: PA-1 Hangfire + ImageSharp)

- **Định dạng phái sinh**: thumbnail **300×300**, medium **800×600** (`ResizeMode.Max` → không phóng to ảnh nhỏ hơn).
- **Key object phái sinh** (giữ nguyên extension gốc): `recipes/{recipeId}/{uuid}_300x300.{ext}` và `recipes/{recipeId}/{uuid}_800x600.{ext}`.
- **Chạy ngoài request**: `POST /recipes/{id}/images` enqueue job Hangfire (queue PostgreSQL, retry 3) rồi trả `201` ngay → **`mediumUrl`/`thumbnailUrl` lúc upload luôn `null`**, FE đọc lại qua `GET /recipes/{slug}` (job xong mới có). Đây là hành vi mong muốn (upload không bị chặn bởi CPU encode).
- **Job** `ResizeImageJob` (Infrastructure): đọc original qua `IObjectStorageReader` → resize ImageSharp → ghi qua `IObjectStorageWriter` (interface mới, key chủ động; **không** sửa `IFileStorageService`/`StoredFile` — HANDOFF 5.1) → cập nhật `RecipeImage.MediumUrl/ThumbnailUrl`.
- **Bất biến**:
  | Tình huống | Hành vi |
  |---|---|
  | Object phái sinh đã tồn tại (retry / job chạy 2 lần) | Bỏ qua upload, URL trong DB vẫn đúng (idempotent, FR-JOB-002) |
  | Ảnh đã bị xoá khi job chạy | Không tái sinh object (delete-vs-resize) |
  | Ảnh hỏng/không decode (JPEG cắt dở), hoặc **AVIF** (ImageSharp 3.1 không decode AVIF) | `mediumUrl`/`thumbnailUrl` giữ `null` → FE fallback `originalUrl` (FR-JOB-003); **không** lỗi `5xx` cho request upload |
  | Xoá ảnh qua API | Xoá luôn original + 2 object phái sinh (`DeleteRecipeImageHandler`) |
- **Dashboard**: `GET /hangfire` chỉ Admin (`AdminDashboardAuthorizationFilter`): anonymous `401`, member `403`, Admin `200`. Không đăng ký Hangfire ở môi trường `Testing` (E2E chạy job inline qua `InlineImageResizeQueue` để assert deterministic).
- **Kiểm thử**: `ImageResizeD2Tests` (4/4) — resize đúng kích thước qua proxy, idempotent, fallback ảnh hỏng, xoá ảnh không tái sinh; unit test `ResizedKeys_*` + `Upload_*enqueue*` trong `RecipeImageTests`. Evidence chạy thật (Hangfire job `Succeeded`): `docs/evidence/TV4/Tuan03/logs/d2_resize_hangfire.log` + `d2_resize_hangfire_db.txt`.

---

## 8. Tích hợp cho TV3 (Recipe editor)

- TV3 gọi `POST /recipes/{id}/images` với `multipart/form-data` (field `file`, tùy chọn `altText`) → nhận `RecipeImageDto`.
- Sau upload, dùng `PATCH .../{imageId}` để set `isPrimary` (ảnh đại diện) hoặc sửa `altText`.
- `DELETE .../{imageId}` → `204`; UI xoá khỏi gallery và gọi đồng bộ.
- Ảnh đầu tiên tự thành primary — editor chỉ cần set primary khi có ≥ 2 ảnh.
- **Gallery sau upload**: poll/reload `GET /recipes/{slug}` để lấy `thumbnailUrl`/`mediumUrl` (job resize bất đồng bộ — xem §7). `imageSrc()` của FE đã tự fallback về `originalUrl` khi hai URL này còn `null`.