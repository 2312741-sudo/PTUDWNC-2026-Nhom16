# Đề xuất giải quyết D23 (queue resize) và D27 (hiển thị ảnh)

- **Người soạn**: Nguyễn Hữu Trung Sơn (2312739 — TV4) · **Ngày 27/09/2026**
- **Trạng thái**: Đề xuất — chờ nhóm/giảng viên xác nhận theo quy trình §12.1 `KE_HOACH_DU_AN.md`.
- **Căn cứ**: `KE_HOACH_DU_AN.md` (D23/D27), `ADR-TV4-001` (D27 tạm), `IMAGE_CONTRACT.md`, rà soát **PR #15** (TV3 C4 — FE đã ghép `NEXT_PUBLIC_MEDIA_URL`).
- **Quyết định mục tiêu**:
  - **D23 → PA-1: Hangfire** (queue persistent PostgreSQL) + SixLabors.ImageSharp cho D2 resize.
  - **D27 → PA-2: base media URL qua proxy có auth** (URL ổn định) — khớp với FE TV3 đã code `mediaUrl()`/`NEXT_PUBLIC_MEDIA_URL`.

---

## Phần A — D23: Queue resize bằng Hangfire (PA-1)

### 1. Vấn đề

- D2 yêu cầu (FR-JOB-002/003): resize ảnh thành **original + 300×300 + 800×600**, URL lưu DB, original fallback khi lỗi, retry idempotent, delete-vs-resize không tái sinh ảnh đã xoá.
- SRS mâu thuẫn: tr. 38 mô tả **Hangfire persistent PostgreSQL**, tr. 16 nói fire-and-forget bị mất khi không khả dụng → cần chốt một hướng queue bền vững trước khi viết D2.
- Chưa có gì trong code: D2 chưa bắt đầu, chưa có package `Hangfire`/`ImageSharp` trong csproj (môi trường đang lockfile D25; CI dùng `--locked-mode`).

### 2. Hiện trạng

- Chưa có worker/job nền nào trong backend; `Recipes.cs` hiện xử lý trực tiếp trong request handler.
- `HANDOFF_TV4_TUAN3.md` đã cảnh báo: thêm package phải `dotnet restore` để regenerate `packages.lock.json` (không `--locked-mode` khi thêm), CI restore `--locked-mode` mới khớp.
- PR #15 (TV3) không động tới D23 — không có file resize/Hangfire/job trong PR.

### 3. Nên làm gì

- Chọn **Hangfire** là queue job:
  - Thêm packages: `Hangfire` (core + `Hangfire.AspNetCore`), `Hangfire.PostgreSql` (hoặc `Hangfire.PostgreSql.Npgsql`), `SixLabors.ImageSharp`.
  - Storage: Hangfire tự tạo schema `_HangFire` trong **chính PostgreSQL** (persistent), không dùng in-memory.
  - Cấu hình `GlobalConfiguration` + `AddHangfireServer`; Dashboard `/hangfire` chỉ **Admin** (đã có sẵn role).
  - Resize: `BackgroundJob.Enqueue(() => ...)` gọi sau khi upload ảnh thành công; retry mặc định (hoặc **3 lần** theo `KE_HOACH_DU_AN.md`); job idempotent (check URL đã tồn tại / object đã có trước khi ghi).
  - Resize tạo `{uuid}_original`, `{uuid}_300x300`, `{uuid}_800x600` (hoặc `{uuid}.jpg` + suffixed names) theo config kích thước **không hard-code**; update `RecipeImage.MediumUrl/ThumbnailUrl`; original fallback khi resize lỗi (giữ URL original).
  - Boundary: khi xoá ảnh (soft-delete metadata) job resize chạy trễ không được tái sinh object đã xoá (check recipe/image còn tồn tại + chưa deleted).
- Phụ thuộc mới: `Hangfire` tạo bảng trong DB (qua migrate khi startup). Không cần service riêng trong CI (chạy cùng process); test "restart worker không mất job" bằng job server thật trong integration.

### 4. Làm xong thì sẽ như nào

- Upload ảnh xong → trong ít giây sau có `MediumUrl`/`ThumbnailUrl` trong DB và DTO; FE dùng thumbnail cho gallery/list.
- Job **persistent**: worker restart không mất job; retry tự động; dashboard `/hangfire` (Admin) nhìn thấy job + trạng thái — đúng chứng minh K14/K15 và đặc tả SRS tr.38.
- Dockerfile/Compose không đổi đáng kể (server chạy cùng API process).
- CI xanh: `dotnet restore --locked-mode` khớp sau khi regenerate lockfile + thêm test resize (unit + E2E trên MinIO).

### 5. Ảnh hưởng đến từng thành viên

| Thành viên | Ảnh hưởng | Việc cần làm |
|---|---|---|
| **TV4 (Sơn)** | Thực hiện chính: packages, Hangfire server, job resize, update DTO, test | Implement theo đề xuất; regenerate lockfile; `dotnet format`; 4 bước build/format/test/CI |
| **TV1 (auth/ops)** | Dashboard `/hangfire` là Admin-only → cần role Admin đã seed; health/readiness có thể thêm check Hangfire; Compose giữ cùng DB | Xác nhận role Admin seed; coordination nếu thêm health component; chia sẻ storage PostgreSQL |
| **TV2 (list/search)** | List/detail sẽ nhận `ThumbnailUrl/MediumUrl` → hiển thị thumbnail ở list bằng URL này | Cập nhật UI list dùng `thumbnailUrl ?? mediumUrl ?? originalUrl` (khớp `imageSrc()` FE TV3) |
| **TV3 (recipe/C4)** | Editor/gallery nhận `MediumUrl/ThumbnailUrl` qua API → hiển thị ảnh resize ngay; contract DTO thêm URL (nhẹ) | Dùng URL có sẵn trong `RecipeImageDto`; không cần đổi upload flow |
| **Nhóm trưởng (Tâm)** | Review/accept đề xuất; chốt lockfile CI `--locked-mode` sau khi thêm package | Duyệt đề xuất; kiểm tra CI xanh |

---

## Phần B — D27: Hiển thị ảnh bằng base media URL (PA-2, proxy có auth)

### 1. Vấn đề

- Ảnh upload qua API (`POST /api/v1/recipes/{id}/images`) lưu thành công nhưng **không hiển thị được ở trình duyệt**: `RecipeImageDto.OriginalUrl` trả **key** MinIO (`recipes/{recipeId}/{uuid}.jpg`), không phải URL HTTP; bucket mặc định **private** (ADR-TV4-001).
- FE hiện dùng thẳng `originalUrl` làm `src` (detail `page.tsx`) hoặc ghép `NEXT_PUBLIC_MEDIA_URL` (PR #15) → thiếu điểm cuối URL hợp lệ.
- D27 chưa chốt → `IMAGE_CONTRACT.md §5` còn để 3 phương án mở; `HANDOFF` mục 5.1 cấm sửa `IFileStorageService`/`StoredFile` trước khi chốt.
- 100 ảnh seed hiển thị được **chỉ nhờ** static local `/images/recipes/{slug}.jpg` — không phải giải pháp cho ảnh upload thật.

### 2. Hiện trạng

- **FE TV3 (PR #15) đã định hướng sẵn**: `mediaUrl(key)` = `${NEXT_PUBLIC_MEDIA_URL}/${key}`; `imageSrc()` ưu tiên `thumbnailUrl ?? mediumUrl ?? originalUrl ?? url` → FE kỳ vọng một **base URL ổn định**, không trông chờ presigned thay từng request.
- **Backend chưa có gì cho điểm cuối URL**: không endpoint presigned, không proxy, không đổi policy. Đây là việc còn phải làm.

### 3. Nên làm gì

Chốt **PA-2: base media URL ổn định qua proxy có auth** trong API:

- Endpoint mới ví dụ `GET /api/v1/resources/images/{key}` (hoặc `/media/**`):
  - Parse `recipeId` từ key (`recipes/{recipeId}/{uuid}.ext`); truy vấn recipe theo Id.
  - Quy tắc truy cập:
    - Recipe **Published** → công khai (không cần token) — ảnh của bài public hiển thị bình thường trên `<img>`.
    - Recipe **Draft / Archived / Deleted** → chỉ **owner hoặc Admin** (Bearer) → else `403 image.forbidden`.
    - Không tìm thấy recipe/ảnh → `404`.
  - Stream object từ MinIO ra response với `Content-Type` đúng; set `Cache-Control` hợp lý (Published có thể cache).
- **Không sửa `IFileStorageService`/`StoredFile`** (giữ `UploadAsync/DeleteAsync`; `OriginalUrl` vẫn là key để delete/resize). Proxy dùng lại `MinioStorageService` (thêm method `GetAsync`/`OpenRead` nếu cần, hoặc chính storage trả stream — lưu ý đây vẫn nằm trong hợp đồng `IFileStorageService`, cần thống nhất nhẹ).
- FE: đặt `NEXT_PUBLIC_MEDIA_URL` trỏ endpoint proxy → **không cần đổi code FE TV3** (`mediaUrl()`/`imageSrc()` hoạt động nguyên vẹn).
- Cập nhật `IMAGE_CONTRACT.md §5`: chốt PA-2; ghi rõ rule quyền + cách stream.
- Kiểm thử: ảnh Published hiển thị public; Draft/Archived → owner OK, anonymous `403`; key không hợp lệ → `404`; MinIO down → lỗi rõ ràng (đã có pattern D1.1c).

### 4. Làm xong thì sẽ như nào

- `<img>` hiển thị được ảnh upload qua API cho bài **Published** (public); ảnh Draft/Archived **không lộ** (`403` khi không phải owner/Admin) — thỏa cả SRS lẫn an toàn.
- URL ổn định → dễ cache, SEO ổn; FE TV3 **không phải sửa code** (chỉ thêm env) tránh xung đột PR #15.
- `IFileStorageService` gần như giữ nguyên contract → không phá bàn giao cho TV3 như `HANDOFF` dặn.
- Nếu sau này nhóm muốn public-read (PA-3) vẫn phải **CR riêng + giảng viên** — đề xuất này không đổi bucket policy.

### 5. Ảnh hưởng đến từng thành viên

| Thành viên | Ảnh hưởng | Việc cần làm |
|---|---|---|
| **TV4 (Sơn)** | Thực hiện chính: endpoint proxy + quy tắc quyền + stream MinIO + test | Implement; cập nhật `IMAGE_CONTRACT.md`; test Published/Draft/Archived |
| **TV3 (recipe/C4)** | FE **không đổi code**; chỉ cấu hình `NEXT_PUBLIC_MEDIA_URL`; nhận URL ảnh hiển thị được ở wizard/detail | Set env; xác nhận wizard ảnh hiện ảnh upload sau khi D27 chốt |
| **TV1 (auth/ops/Nginx)** | Route `/resources/images/*` nếu đặt qua Nginx; cache control; JWT policy cho proxy (reuse `RoleClaimType="role"`) | Cấu hình Nginx (nếu cần); đảm bảo health/proxy không làm lộ Draft |
| **TV2 (list/search)** | List dùng URL ổn định từ proxy cho thumbnail (sau D2 resize) | Cập nhật list hiển thị thumbnail qua base URL khi `thumbnailUrl` có |
| **Nhóm trưởng (Tâm)** | Review/accept; quyết định điểm cuối URL (đặt ở Nginx hay qua API) | Duyệt đề xuất; chốt vị trí proxy |

---

## Phần C — Thứ tự thực hiện & theo dõi

1. **Chốt đề xuất này** bởi nhóm + giảng viên (D23/D27 thuộc D → cần ADR mới ghi quyết định chính thức thay/bổ sung ADR-TV4-001).
2. **D27 trước** (mở D1 hiển thị + D4 editor): viết proxy + rule quyền + test → cập nhật `IMAGE_CONTRACT.md` → chốt FE.
3. **D23 sau** (mở D2 resize): thêm Hangfire/ImageSharp → job resize + test restart/retry → regenerate lockfile → CI.
4. Cập nhật tài liệu tuần 3 (KE_HOACH/TRANG_THAI) theo mốc mới; ghi ADR D23/D27.