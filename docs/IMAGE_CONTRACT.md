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

## 5. Bucket policy (D27 — chờ xác nhận nhóm)

- Mặc định bucket **private**. Draft/Archived không được phục vụ public.
- Cách phục vụ ảnh public (3 phương án, chờ CR/ADR chốt):
  1. **Presigned URL** (vd MinIO `PresignedGetObjectArgs`) có TTL — phù hợp private bucket.
  2. **Proxy có auth** trong API (`GET /resources/images/{key}`) — kiểm tra quyền xem object.
  3. Public bucket cho ảnh **đã publish** — trái với SRS 2.4.1 `public-read`, cần CR trước khi áp dụng.
- `IMAGE_CONTRACT.md` cập nhật lại khi nhóm chốt D27.

---

## 6. Tích hợp cho TV3 (Recipe editor)

- TV3 gọi `POST /recipes/{id}/images` với `multipart/form-data` (field `file`, tùy chọn `altText`) → nhận `RecipeImageDto`.
- Sau upload, dùng `PATCH .../{imageId}` để set `isPrimary` (ảnh đại diện) hoặc sửa `altText`.
- `DELETE .../{imageId}` → `204`; UI xoá khỏi gallery và gọi đồng bộ.
- Ảnh đầu tiên tự thành primary — editor chỉ cần set primary khi có ≥ 2 ảnh.