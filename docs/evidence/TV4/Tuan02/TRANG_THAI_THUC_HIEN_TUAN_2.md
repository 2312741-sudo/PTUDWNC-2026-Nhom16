# TRẠNG THÁI THỰC HIỆN TUẦN 2 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Nhánh Git**: `2312739_NHTSon_D3-D4-D5-D6` (tv4/week3, chứa merge main `a651c8a`); tuần 2 cũ: `2312739_NHTSon_D1-D2-D3-D4` (giữ nguyên, HEAD `6d689bd`)
> **Lab nhánh**: `practice/TV4/L4`
> **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Cập nhật lần cuối**: 23/09/2026

> File này ghi lại trạng thái thực hiện các task tuần 2 (D1, D2, D3, D4 nền, D6 tiếp), các điểm cần bàn luận và lý do.
> Chi tiết kế hoạch xem `KE_HOACH_TUAN_2_TV4.md`.
> **Điều kiện gỡ block + hướng dẫn làm tiếp chi tiết**: xem `docs/HANDOFF_TV4_TUAN2_BLOCKED.md` (tài liệu tự túc khi TV4 vắng mặt).

> **Bản sửa đổi 23/09/2026 (T2) — kiểm chứng TỔNG THỂ sau khi merge main `a651c8a` + toàn bộ test lại**:
> Cập nhật từ kết quả test thật: build Release **0 warning/0 error**, `dotnet format` sạch,
> CulinaryBlog.Tests **120/120 pass** (gồm 26 test Auth+Week3 liên quan C5 — trước đây 16 fail giờ pass), spike **5/5 pass** (Postgres local `culinary_test`).
> **Phát hiện mới quan trọng — C5 refresh ĐÃ có trên main** (không còn block 2.2): `/auth/refresh` + `RefreshTokenAsync`
> (rotation + family reuse revocation `compromised-reuse-detected`) + `LogoutAsync` (revoke theo hash + revoke mọi token active của family) đều nằm trong
> `IdentityService.cs`, `POST /auth/refresh`, `POST /auth/logout` khai báo trong `Program.cs`. 23 test Auth + 7 test Week3 pass → **D3.3 logout revoke refresh family đã hoàn thành** (gián tiếp qua main, không phải TV4 tự làm).
> CI GitHub branch tuần 3 (sau merge fix migration `a651c8a`) = **success** (run `818522b`).

> **Bản sửa đổi 23/09/2026 — gỡ HOÀN TOÀN block 2.1 sau merge C2/C3 của TV3 (`2086ee8`)**:
> recipe CRUD endpoints (condition 3) đã có (TV3 C2/C3): `POST/PUT /recipes`, `GET /{slug}`, ingredient/step CRUD + reorder.
> → **Seed recipe qua API được**, D1.3 image endpoints test E2E được, fixtures ingredient/step cho D3 publish đã đủ.
> Còn block: C5 refresh (2.2), D27 (2.3), D23 (2.4); D3 publish cần TV3 bổ sung `Publish/Unpublish` command.

> **Bản sửa đổi 22/09/2026 — kiểm chứng block sau TV3 merge PR #10 (`5b36251`)**:
> block 2.1 được gỡ **một phần** — wiring DI + migration + envelope/RFC7807 đã có (`IApplicationDbContext`/`IUnitOfWork` từ `AuthDbContext`,
> migration `20260919061954_AddRecipeAggregate`, `ApiExceptionHandler`); **condition 3 (recipe CRUD endpoints) vẫn chưa**.
> Hệ quả: **D1.3 image endpoints làm được ngay**, test end-to-end cần seed recipe. C2/C5 refresh chưa gỡ. Chi tiết trong HANDOFF mục 0/2.1.

---

## 1. Đã hoàn thành

> Tất cả task bắt đầu ở trạng thái **Chưa làm**. Bảng này cập nhật khi có kết quả.
> **Đợt 1 (17/09/2026)**: triển khai các phần D1 không bị block trước khi TV3 bàn giao Recipe cluster.

| Task | Nội dung | Nơi triển khai | Trạng thái |
|---|---|---|---|
| D1.1 | `MinioStorageService` implement `IFileStorageService` bằng MinIO SDK 7.0.0 + DI + lockfile (D25) | `src/backend/CulinaryBlog.Infrastructure/MinioStorageService.cs`; `Program.cs` (DI); `packages.lock.json` (6 projects) | Đã làm — test MinIO down [chờ chạy integration trên local stack] |
| D1.2 | Validator upload: 4 MIME (JPEG/PNG/WebP/AVIF) + ≤5MiB + magic bytes (không tin Content-Type) | `src/backend/CulinaryBlog.Application/ImageUpload.cs` | Đã làm — 16 test unit pass |
| D1.5 | Domain RecipeImage tests: ảnh đầu tiên auto primary, đúng 1 primary, remove-promote | `tests/CulinaryBlog.Tests/RecipeImageDomainTests.cs` | Đã làm — 9 test pass |
| D1.5 | Race test 2 writer set primary → không tạo 2 primary (RowVersion + unique partial index) | `tests/concurrency-spike/RecipeImagePrimaryConcurrencyTests.cs` | Đã làm — chạy trên `culinary_spike` DB |
| D1.6 | `docs/IMAGE_CONTRACT.md` bàn giao TV3 (DTO, endpoints, lỗi, D27) | `docs/IMAGE_CONTRACT.md` | Đã làm — chờ TV3 review |
| D1.3 | API upload/metadata/primary/delete recipe image (FR-RCP-008, D17) | `Program.cs` (`POST/PATCH/DELETE /recipes/{id}/images`); `RecipeImages.cs` (Application) | Đã làm (22/09) — validate+test tổng thể 23/09: E2E chạy được qua seed `POST /recipes`; 120/120 + spike 5/5 pass |
| D3.1/D3.2 | Publish/Unpublish recipe (FR-RCP-005/006, 422 nếu thiếu ingredient/step, C02, D07) | `Recipes.cs` (region D3.1/D3.2); `PATCH /recipes/{id}/publish|unpublish`; domain `Recipe.Publish()/Unpublish()` | Đã làm (electron merge qua PR #14 + main `a651c8a`) — xác minh 23/09: 13 test `RecipeLifecycleTests` pass |
| D3.3 | Logout revoke refresh family | `IdentityService.LogoutAsync` (revoke theo hash + revoke family), `POST /auth/logout` | Đã làm — C5 refresh ĐÃ nằm trên main: `RefreshTokenAsync` rotation + family reuse revocation, 7 test Week3 + 16 test Auth pass |

---

## 2. Đang làm / Chưa thực hiện

| Task | Nội dung | Lý do chưa xong | Cần gì để xong |
|---|---|---|---|
| D1.1c | Test MinIO down → lỗi rõ ràng + log redacted | Cần chạy integration với MinIO local (Compose) đang lên | Chạy test + ghi log evidence (cần Docker local hoặc bổ sung service MinIO vào `backend.yml`) |
| D2 | Resize original/300×300/800×600 + job nền | Chưa bắt đầu — `RestAPI` không có ImageSharp/Hangfire/BackgroundService | Chốt queue (Hangfire/BackgroundService) theo nhóm (D23) |
| D4 (nền) | Uploader UI + status button + sitemap/robots nền | Chưa bắt đầu — FE chưa có trang dashboard/recipes/uploader | Chốt D27 ảnh hiển thị (presigned/proxy), ghép API D1.3 + D3.1/D3.2 |
| D5 | OTEL (trace HTTP→DB, filter 404/SDK4xx, payload omit) | Chưa bắt đầu — chỉ có Serilog/Seq + /health | Cài OpenTelemetry exporter; chạy nhất quán 4 tiêu chí tuần 3 |
| D6 (tiếp) | Lab `practice/TV4/L4` + sổ evidence K | Chưa bắt đầu | G1 đã đóng; tạo nhánh lab |

---

## 3. Bị block (phụ thuộc người khác / quyết định)

| Task | Nội dung | Block bởi | Thời điểm dự kiến gỡ |
|---|---|---|---|
| ~~D1.3 image endpoints~~ | ~~Upload/primary/delete API — gỡ hoàn toàn 23/09 (merge C2/C3, seed qua API)~~ | ✅ **Đã gỡ** (block 2.1 4/4) | Đã gỡ — chạy E2E trên MinIO |
| ~~D3 publish/unpublish~~ | ~~Chờ `Publish/Unpublish` command~~ | ✅ **Đã gỡ** — TV4 đã tự làm theo FR-RCP-005/006, merge qua main (`a651c8a`), 13 test pass | Đã gỡ (23/09) |
| ~~D3 logout revoke~~ | ~~Cần refresh token family~~ | ✅ **Đã gỡ** — C5 refresh token đã có sẵn trên main (`RefreshTokenAsync` + `LogoutAsync`), 7 test Week3 + 16 test Auth pass | Đã gỡ (23/09) |
| D1/D4 ảnh display | Bucket private/public chưa chốt | Quyết định D27 + CR nếu cần | Đầu tuần 3 |
| D1.1c / E2E MinIO | Test MinIO down + integration trên Compose | Thiếu Docker local / service MinIO trong CI | Sau khi bổ sung stack local |

---

## 4. Cần bàn luận / cần CR (SRS v1.1.1)

| # | Nội dung | Mô tả | Quyết định dự kiến |
|---|---|---|---|
| CR-1 (tuần 1) | Bucket policy MinIO private vs public-read | Ảnh recipe public theo SRS 2.4.1 nhưng Draft/Archived không được lộ; kế hoạch giữ private + presigned/proxy | Gửi nhóm + giảng viên xác nhận |
| CR-2 | Resize job dùng Hangfire hay BackgroundService | SRS mô tả Hangfire persistent; thêm phụ thuộc mới có thể phá `restore --locked-mode` | Chốt với nhóm theo D23 |
| CR-3 | Soft delete recipe → object ảnh xử lý thế nào | Giữ object hay xoá khi recipe bị soft delete; tránh orphan | Ghi vào ADR TV4-001/D27 |

---

## 5. Ghi chú kỹ thuật cho review

1. **MinIO SDK mới** → khóa phiên bản trong `packages.lock.json` (D25), chạy lại `dotnet restore --locked-mode` + CI.
2. **Magic bytes** xác thực nội dung file, không tin `Content-Type` từ client (D1).
3. **Primary duy nhất** bằng transaction + RowVersion (D19); test race 2 writer.
4. **Publish 422** theo SRS v1.1.1 C02 (≥1 ingredient VÀ ≥1 step).
5. **Response `{ "data": ... }`** theo C08; không lộ Draft/Archived ra public endpoint.
6. Máy local đã có .NET 10.0.401 → build/test chạy được local (kinh nghiệm Tuần 1); CI vẫn chạy song song sau mỗi commit.
7. CI hiện chỉ có PostgreSQL service; nếu test tích hợp cần MinIO → phải bổ sung service MinIO vào `.github/workflows/backend.yml` (đã nêu trong mục block).

---

## 6. Cổng hoàn thành tuần 2

| Cổng | Tiêu chí | Trạng thái |
|---|---|---|
| **G2 giữa tuần** | Upload ảnh hợp lệ → `StoredFile` URLs; set primary đúng 1; DELETE xoá object không orphan | 🟢 **Đạt** (MinioStorageService + validator + domain/race + D1.3 endpoints; 120/120 + spike 5/5 pass, build 0 warning, CI xanh). Còn: chạy/ghi evidence E2E thật trên Compose + D1.1c. |
| **G3 cuối tuần** | Draft → thêm ingredient/step + ảnh → publish thành công; unpublish ẩn public; Draft/Archived không lộ; 4 MIME ≤5MiB; resize 3 kích thước; CI pass | 🟡 **Phần bé đạt** — publish/unpublish + 422 + không lộ Draft (D3.1/D3.2, 13 test) + logout revoke (C5) đã xong. **Thiếu**: D2 resize (D23 chưa chốt), E2E thật trên MinIO, D1.1c. |