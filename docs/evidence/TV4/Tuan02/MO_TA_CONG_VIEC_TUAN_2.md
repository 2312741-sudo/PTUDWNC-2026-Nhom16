# MÔ TẢ CÔNG VIỆC TUẦN 2 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> Tài liệu mô tả chi tiết **công việc cần làm trong tuần 2** dựa trên SRS v1.1.1.
> Xem thêm: `KE_HOACH_DU_AN.md` (mục 8), `PHAN_CHIA_CONG_VIEC_6_TUAN.md`, `MO_TA_CONG_VIEC_TV4.md`, `KE_HOACH_TUAN_2_TV4.md`.

---

## 1. Tổng quan tuần 2

TV4 phụ trách 5 nhóm công việc chính trong tuần 2:

| Nhóm | Task | Mô tả | Điểm |
|---|---|---|---|
| Lưu trữ ảnh | D1 | `MinioStorageService` thật + API upload/metadata/primary/delete | 15đ |
| Xử lý ảnh | D2 | Resize original/300×300/800×600, jobs nền, retry | 10đ |
| Xuất bản | D3 | Publish/unpublish CQRS + ownership + logout revoke refresh | 13đ |
| UI + SEO | D4 (nền) | Image uploader/progress/primary + status button + sitemap/robots nền | 12đ |
| Kỹ năng cá nhân | D6 (tiếp) | Lab L4 + sổ evidence K | 25đ |

---

## 2. Chi tiết từng nhóm công việc

### 2.1. D1 — Upload/metadata/primary/delete (+ MinioStorageService thật)

**Mục tiêu**: editor có thể upload ảnh recipe, set ảnh chính, sửa metadata, xoá ảnh; không lộ object không đúng phạm vi.

#### 2.1.1. `MinioStorageService` (Infrastructure)

- Implement `IFileStorageService` bằng MinIO SDK (phiên bản đã chốt D25, có lockfile).
- Đọc config từ `MinioOptions` (endpoint/accessKey/secretKey/bucket/useSSL) — Tuần 1 đã đăng ký DI.
- `UploadAsync(Stream, fileName, contentType, folder, ct)` → tạo key `recipes/{recipeId}/{uuid}.{ext}` (FR-RCP-008), trả `StoredFile(Key, Url, ContentType, SizeBytes)`.
- `DeleteAsync(key, ct)` → xoá object, không nuốt lỗi im lặng.

```csharp
// Application/Storage.cs (Tuần 1 đã có)
public interface IFileStorageService
{
    Task<StoredFile> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
```

#### 2.1.2. Validator upload (FR-FILE-001/002)

| Tiêu chí | Giá trị |
|---|---|
| Định dạng | JPEG, PNG, WebP, AVIF (4 MIME theo L4) |
| Kích thước | ≤ 5 MiB |
| Xác thực nội dung | **Magic bytes** đối chiếu với MIME khai báo (không tin header) |
| Đường dẫn | `recipes/{recipeId}/{uuid}.{ext}` |
| Ảnh đầu tiên | Tự động thành `IsPrimary = true` |

Lỗi trả **400** với code chuẩn (vd `request.invalid`, `file.invalid_type`, `file.too_large`).

#### 2.1.3. Endpoints (FR-RCP-008, D17)

| Method + Path | Auth | Body | Response | Lỗi |
|---|---|---|---|---|
| `POST /recipes/{id}/images` | Author/Admin | multipart file | 201 `{ "data": RecipeImageDto }` | 400 file, 401, 403, 404, 422 |
| `PATCH /recipes/{id}/images/{imageId}` | owner/Admin | `{ isPrimary?, altText?, orderIndex? }` | 200 `{ "data": RecipeImageDto }` | 400, 401, 403, 404, 422 (race) |
| `DELETE /recipes/{id}/images/{imageId}` | owner/Admin | — | 204 | 401, 403, 404 |

- Mọi `imageId` phải thuộc `recipeId` trên URL (không để però cross-recipe).
- Non-owner → **403**; không tin `authorId` từ client (D12).
- Set primary bằng transaction + RowVersion (D19) — test 2 writer không tạo 2 primary.
- DELETE xoá object MinIO tương ứng, không để orphan.

#### 2.1.4. Bucket policy (D27)

- Mặc định **private**; URL ảnh qua presigned hoặc proxy có auth — KHÔNG dùng URL public làm cơ chế bảo mật Draft.
- SRS 2.4.1 ghi `public-read` → cần CR/ADR xác nhận trước khi đổi; ghi rõ vào ADR.

#### 2.1.5. Contract cho TV3

- Tạo `docs/IMAGE_CONTRACT.md`: DTO `RecipeImageDto`, định dạng response `{ "data": ... }` (C08), ví dụ các lỗi trên.
- TV3 review giữa tuần để tích hợp editor image (D4/C4).

---

### 2.2. D2 — Resize original/300×300/800×600 + jobs nền

**Mục tiêu**: mỗi ảnh upload có 3 kích thước, URL lưu DB, xử lý bất đồng bộ an toàn.

| # | Việc làm | Kết quả |
|---|---|---|
| 1 | Resize tạo `{uuid}_original.{ext}`, `{uuid}_300x300.{ext}`, `{uuid}_800x600.{ext}` | 3 object/bản ghi URL trong RecipeImage |
| 2 | Resize chạy qua job queue **persistent** (Hangfire/BackgroundService — chốt theo nhóm D23) | Lỗi enqueue không mất; retry idempotent; restart worker không mất job |
| 3 | Original fallback: nếu resize lỗi → vẫn dùng original | Không mất ảnh gốc |
| 4 | Kích thước đọc từ config, không hard-code | Dễ thay đổi |

**Boundary tests**: 4 MIME hợp lệ; file >5MiB bị từ chối; fake header bị magic bytes chặn; delete vs resize không tái sinh ảnh đã xóa.

---

### 2.3. D3 — Publish/unpublish CQRS + ownership + logout revoke refresh

**Mục tiêu**: công thức Draft đủ thành phần → Publish; đổi lại → Unpublish ẩn public ngay.

#### 2.3.1. `PATCH /recipes/{id}/publish`

- Điều kiện: **≥1 ingredient VÀ ≥1 step** (SRS v1.1.1 C02, nghiêm hơn FR-RCP-005).
- Thiếu thành phần → **422** code `recipe.publish.incomplete` (`RECIPE_PUBLISH_INCOMPLETE`).
- Đủ → `Published`, trả status + slug ổn định.
- **Ownership**: chỉ author của recipe hoặc Admin (403 non-owner).

#### 2.3.2. `PATCH /recipes/{id}/unpublish`

- `Published → Draft` ngay; public ẩn ngay (invalidation API/Redis/ISR — K12, D13).
- Không để cache cũ phục vụ response Draft.

#### 2.3.3. Logout revoke refresh (fr C5 TV3)

- Tuần 1: seam Bearer-only 204. Tuần 2: khi TV3 bàn giao C5 refresh → revoke family token, giữ 204 idempotent.

#### 2.3.4. Test cases

| Case | Expected |
|---|---|
| Publish đủ ingredient + step | 200 Published |
| Publish thiếu ingredient (0) | 422 RECIPE_PUBLISH_INCOMPLETE |
| Publish thiếu step (0) | 422 RECIPE_PUBLISH_INCOMPLETE |
| Publish bởi non-owner | 403 |
| Unpublish public ẩn ngay | 200; GET public không thấy |
| Logout kèm refresh (khi có C5) | 204; refresh cũ không còn dùng được |

---

### 2.4. D4 (nền) — Image uploader/progress + status UI + SEO/sitemap

**Mục tiêu nền tuần này**: editor có thể upload/xem/xoá ảnh, đổi trạng thái; nền SEO.

| # | Việc làm | Nền tuần 2 / đủ tuần 3 |
|---|---|---|
| 1 | Uploader UI (Next.js): progress thanh, rollback khi lỗi, gallery + thumbnail, chọn primary (TanStack Query optimistic) | Nền: upload + gallery + primary |
| 2 | Status action button Draft → Published/Unpublished | Nền: gọi API publish/unpublish |
| 3 | Sitemap XML (Published-only) + robots + canonical; cron 02:00 UTC | Nền: dựng cấu trúc; đủ tuần 3 |
| 4 | Ảnh display qua presigned/proxy (D27) | Tuỳ quyết định bucket |

---

### 2.5. D6 (tiếp) — Lab L4 + sổ skill

- Tạo nhánh `practice/TV4/L4` từ skeleton G1.
- Lab: upload/delete **4 MIME** (JPEG/PNG/WebP/AVIF) + resize **300×300/800×600** + Mailhog email + Hangfire delayed/restart.
- Cập nhật sổ evidence `TV4-Kxx` trạng thái thực tế (K04/K13/K14/K15 ...).

---

## 3. Thứ tự thực hiện đề xuất

```
Tuần 2 — Thứ tự làm việc:

Ngày 1: Chốt D27 bucket policy; khởi động nhánh lab practice/TV4/L4.
Ngày 1–2: MinioStorageService + test MinIO down → lỗi rõ ràng + log redacted.
Ngày 2–3: API upload ảnh + validator MIME/magic bytes/size → IMAGE_CONTRACT.md.
Ngày 3–4: PATCH primary + DELETE (transaction/RowVersion) + test race 2 writer.
Ngày 4–5: Resize 300×300/800×600 + job nền + retry; URLs DB.
Ngày 5: Publish/unpublish CQRS + 422 + ownership; test end-to-end.
Ngày 5–6: UI uploader/status nền + sitemap/robots nền.
Ngày 6: Sổ evidence + lab commit + CI pass; push PR nhỏ từng task.
```

---

## 4. Lưu ý kỹ thuật cho reviewer

1. **MinIO SDK** thêm mới → phải khóa phiên bản trong `packages.lock.json` (D25) nếu không CI `restore --locked-mode` fail. Nếu muốn tránh phụ thuộc thư viện mới, có thể viết HTTP-based `MinioStorageService` (S3 API) — cân nhắc rủi ro, ưu tiên SDK chính thức.
2. **Magic bytes**: không tin `Content-Type` từ client; đối chiếu signature thật.
3. **Primary đúng 1**: transaction + concurrency check (RowVersion D19); test cả 2 writer.
4. **Response `{ "data": ... }`** theo C08; draft/metadata không lộ public.
5. **Orphan object**: delete image → xoá object; soft delete recipe → thống nhất handling ảnh trong ADR.
6. **Logout**: tiếp tục seam; không tự triển khai refresh rotation trước khi TV3 C5 merge.