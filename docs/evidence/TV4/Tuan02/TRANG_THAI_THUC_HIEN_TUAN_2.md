# TRẠNG THÁI THỰC HIỆN TUẦN 2 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Nhánh Git**: `2312739_NHTSon_D1-D2-D3-D4` (tv4/week2)
> **Lab nhánh**: `practice/TV4/L4`
> **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Cập nhật lần cuối**: 22/09/2026

> File này ghi lại trạng thái thực hiện các task tuần 2 (D1, D2, D3, D4 nền, D6 tiếp), các điểm cần bàn luận và lý do.
> Chi tiết kế hoạch xem `KE_HOACH_TUAN_2_TV4.md`.
> **Điều kiện gỡ block + hướng dẫn làm tiếp chi tiết**: xem `docs/HANDOFF_TV4_TUAN2_BLOCKED.md` (tài liệu tự túc khi TV4 vắng mặt).

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

---

## 2. Đang làm / Chưa thực hiện

| Task | Nội dung | Lý do chưa xong | Cần gì để xong |
|---|---|---|---|
| D1.1c | Test MinIO down → lỗi rõ ràng + log redacted | Cần chạy integration với MinIO local đang lên | Chạy test + ghi log evidence |
| D1.3 | API upload/metadata/primary/delete (FR-RCP-008, D17) | Recipe cluster được nối vào API host từ 22/09 (TV3 merge PR #10): `ApplicationDbContext` → gộp vào `AuthDbContext`, đã register `IApplicationDbContext`/`IUnitOfWork`. Còn thiếu condition 3 (recipe CRUD endpoints) để tạo recipe qua API | **Triển khai được ngay** D1.3 image endpoints (upload/PATCH/DELETE); test E2E cần seed recipe hoặc chờ TV3 bàn giao recipe CRUD |
| D2 | Resize original/300×300/800×600 + job nền | Chưa bắt đầu | Chốt queue (Hangfire/BackgroundService) theo nhóm |
| D3 | Publish/unpublish CQRS + 422 + ownership | Chưa bắt đầu | Recipe entity + ingredient/step fixture từ TV3 |
| D3 | Logout revoke refresh family | Chưa bắt đầu | TV3 C5 refresh token merge |
| D4 (nền) | Uploader UI + status button + sitemap/robots nền | Chưa bắt đầu | Chốt D27 ảnh hiển thị (presigned/proxy) |
| D6 (tiếp) | Lab `practice/TV4/L4` + sổ evidence K | Chưa bắt đầu | G1 đã đóng; tạo nhánh lab |

---

## 3. Bị block (phụ thuộc người khác / quyết định)

| Task | Nội dung | Block bởi | Thời điểm dự kiến gỡ |
|---|---|---|---|
| D1.3 image endpoints | Upload/primary/delete API — **đã gỡ wiring/migration từ 22/09**; test E2E cần recipe | TV3 — condition 3 recipe CRUD endpoints (để seed recipe) | Giữa tuần 2 — làm ngay phần code |
| D3 publish | Cần fixture Recipe có ingredient + step | TV3 — merge entity Recipe (C1/C2) | Giữa tuần 2 |
| D3 logout revoke | Cần refresh token family | TV3 — C5 | Cuối tuần 2 |
| D1/D4 ảnh display | Bucket private/public chưa chốt | Quyết định D27 + CR nếu cần | Đầu tuần 2 |

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
| **G2 giữa tuần** | Upload ảnh hợp lệ → `StoredFile` URLs; set primary đúng 1; DELETE xoá object không orphan | Đạt code: MinioStorageService + validator + domain/race tests đã có; **22/09** triển khai xong D1.3 (POST/PATCH/DELETE image + 422 race) — 88/88 test pass, build 0 warning. E2E trên MinIO cần seed recipe (chờ TV3 condition 3) |
| **G3 cuối tuần** | Draft → thêm ingredient/step + ảnh → publish thành công; unpublish ẩn public; Draft/Archived không lộ; 4 MIME ≤5MiB; resize 3 kích thước; CI pass | Chưa đạt |