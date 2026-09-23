# TRẠNG THÁI THỰC HIỆN TUẦN 3 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Nhánh Git đề xuất**: `2312739_NHTSon_D3-D4-D5-D6` (tv4/week3), khởi động từ main đã cập nhật
> **Lab nhánh**: `practice/TV4/L4`
> **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Cập nhật lần cuối**: 23/09/2026 (thời điểm lập kế hoạch — tất cả bắt đầu ở trạng thái **Chưa làm**)

> Chi tiết kế hoạch xem `KE_HOACH_TUAN_3_TV4.md`; nền tảng trạng thái tuần 2 xem `docs/evidence/TV4/Tuan02/`.

---

## 0. Điểm mốc bước vào tuần 3 (ghi nhận 23/09)

| # | Sự kiện | Trạng thái |
|---|---|---|
| 1 | **Block 2.1 gỡ hoàn toàn** (merge C2/C3 TV3 `2086ee8`) — recipe CRUD + ingredient/step có trong API | ✅ Đã xong tuần 2 |
| 2 | **D3.1/D3.2 publish/unpublish CQRS** + 2 endpoint + 13 test (`0659d45`, amend `183c055`) | ✅ Đã xong tuần 2 |
| 3 | **PR #14 bị merge nhầm vào main** (`3eb6de3` + `c512943`) — D1.3 + D3.1/D3.2 + docs đã nằm trên main | ⚠️ Giữ nguyên theo quyết định nhóm; đưa việc rà soát/khắc phục vào tuần 3 |
| 4 | **CI main đỏ (pre-existing từ tuần 2)** — 16 test Auth/Week3 fail do duplicate migration `RefreshTokens` (2 migration cùng `CreateTable`) → DB CI thiếu `RefreshTokens` | 🔴 Fix ưu tiên #1 tuần 3 (N0) |
| 5 | Local branch TV4 hiện tại = `6d689bd` (bằng main); 120/120 test CulinaryBlog.Tests + 5/5 spike chạy đạt local | ✅ Cần tách nhánh tuần 3 từ main đã sửa CI |
| 6 | D2 resize / D1.1c MinIO-down / E2E D1.3 thật / D3.3 logout revoke (chờ TV3 C5) | 📋 Kế thừa vào tuần 3 |

---

## 1. Đã hoàn thành (đầu tuần 3)

> Tất cả task bắt đầu ở trạng thái **Chưa làm** ở tuần 3 này; các mục sau đã hoàn thành trong tuần 2 và là nền tảng.

| Task | Nội dung | Nơi triển khai | Trạng thái |
|---|---|---|---|
| D1.1 | `MinioStorageService` implement `IFileStorageService` bằng MinIO SDK 7.0.0 + DI + lockfile (D25) | `src/backend/CulinaryBlog.Infrastructure/MinioStorageService.cs`; `Program.cs` (DI) | Đã làm tuần 2 — còn D1.1c MinIO-down/log redacted |
| D1.2 | Validator upload: 4 MIME + ≤5MiB + magic bytes | `src/backend/CulinaryBlog.Application/ImageUpload.cs` | Đã làm tuần 2 — 16 test pass |
| D1.5 | Domain RecipeImage: primary invariant + race spike | `tests/CulinaryBlog.Tests/RecipeImageDomainTests.cs`, `tests/concurrency-spike/RecipeImagePrimaryConcurrencyTests.cs` | Đã làm tuần 2 |
| D1.6 | `docs/IMAGE_CONTRACT.md` bàn giao TV3 | `docs/IMAGE_CONTRACT.md` | Đã làm tuần 2 |
| D1.3 | API upload/PATCH primary/DELETE image (FR-RCP-008, D17) | `src/backend/CulinaryBlog.API/Program.cs` + `src/backend/CulinaryBlog.Application/RecipeImages.cs` | Đã làm tuần 2 — **trong main** (qua PR #14); còn E2E thật trên MinIO |
| D3.1/D3.2 | `PATCH /recipes/{id}/publish` & `/unpublish` (FR-RCP-005/006, C02, 422 `RECIPE_PUBLISH_INCOMPLETE`, ownership 403, idempotent) | `src/backend/CulinaryBlog.Application/Recipes.cs` (region D3.1/D3.2) + `Program.cs` + `tests/CulinaryBlog.Tests/RecipeLifecycleTests.cs` | Đã làm tuần 2 — **trong main** (qua PR #14); 13 test pass |
| 125 tests | Build Release 0 warning + 101→120 test (sau merge) + 5 spike | `tests/CulinaryBlog.Tests` | Đã làm tuần 2 — local chạy đạt (120 + 5) |

---

## 2. Đang làm / Chưa thực hiện (tuần 3)

| Task | Nội dung | Lý do chưa xong | Cần gì để xong |
|---|---|---|---|
| N0-1 | Fix duplicate migration `RefreshTokens` + CI xanh | Main chưa sửa; 2 migration trùng `CreateTable` | Sửa migration, build + format + test, push, CI xanh |
| N0-2 | Rà soát diff PR #14 so với main | PR đã merge nhầm, chưa review lại | Review nội dung + xác nhận nhóm |
| D3.1c | Archive `PATCH /recipes/{id}/archive` (FR-RCP-006) | Chưa có endpoint archive | ADR D08 + code + test |
| D3.2c | DELETE recipe soft theo D08 (FR-RCP-007) | Chưa làm | ADR D08 + code + test |
| D3.3 | Logout revoke refresh family (FR-AUTH-005) | Chưa bắt đầu | TV3 C5 refresh token merge |
| D3.4 | E2E D1.3 trên MinIO + D1.1c MinIO down/log redacted | Chưa chạy integration thật | Stack Compose + MinIO local |
| D2 | Resize original/300×300/800×600 + job nền (FR-JOB-002/003) | Chưa bắt đầu | Chốt D23 (Hangfire/BackgroundService) + ImageSharp |
| D4 | Uploader UI/progress/primary + status buttons + sitemap/robots/OG/JSON-LD | Chưa bắt đầu | Chốt D27 (presigned/proxy) + ghép TV3 C4 |
| D5 | OTEL/metrics/health/EXPLAIN/k6 | Chưa bắt đầu | — |
| D6 | Lab `practice/TV4/L4` (4 MIME + resize + Mailhog + Hangfire) + sổ K | Chưa bắt đầu | G1/G2 đã đóng; tạo nhánh lab |

---

## 3. Bị block (phụ thuộc người khác / quyết định)

| Task | Nội dung | Block bởi | Thời điểm dự kiến gỡ |
|---|---|---|---|
| D3.3 logout revoke | Cần refresh token family | TV3 — C5 | Sớm tuần 3 |
| D1/D4 ảnh display | Bucket private/public chưa chốt | Quyết định D27 + CR | Đầu tuần 3 |
| D2 resize | Queue chưa chốt | Quyết định D23 | Đầu tuần 3 |
| N0-1 CI xanh | Migration trùng `RefreshTokens` — cần phối hợp TV3 (migration thuộc Recipe cluster) nếu sửa ở phía khác | TV3 nếu migration dùng chung | Ngày 1–2 tuần 3 |
| PR #14 giữ/xoá | PR đã merge nhầm — cần thống nhất nhóm | Nhóm trưởng + nhóm | Đầu tuần 3 |

---

## 4. Cần bàn luận / cần CR (SRS v1.1.1)

| # | Nội dung | Mô tả | Quyết định dự kiến |
|---|---|---|---|
| CR-1 (từ tuần 1) | Bucket policy MinIO private vs public-read | Ảnh public theo SRS 2.4.1 nhưng Draft/Archived không lộ | Giữ private + presigned/proxy; xác nhận nhóm + giảng viên |
| CR-2 | Resize job Hangfire vs BackgroundService | SRS mô tả Hangfire persistent; phụ thuộc mới có thể phá lockfile | Chốt D23 với nhóm |
| CR-3 | Soft delete recipe → ảnh xử lý thế nào | Giữ hay xoá object khi recipe bị soft delete | Ghi vào ADR TV4-001/D08 |
| CR-4 (mới) | PR #14 merge nhầm vào main | Giữ nguyên và rà soát, hay revert? | Giữ nguyên theo quyết định nhóm 23/09; ghi ADR |

---

## 5. Ghi chú kỹ thuật cho review

1. **Fix migration phải giữ index** `IDX_RefreshToken_Hash` (unique) và `IX_RefreshTokens_UserId` — tránh tạo lại migration gây mất index hiện có.
2. **CI main đỏ là pre-existing** (main `29b171c` đã fail trước khi nhánh TV4 merge) — không phải do D3 gây ra; nhưng là gánh nặng chung cần xử lý đầu tuần.
3. **D3.1/D3.2 đã theo đúng ADR D07**: publish cần ≥1 ingredient VÀ ≥1 step; không bắt buộc ảnh.
4. **Mọi build** phải: Release 0 warning → `dotnet format` sạch → `dotnet test` đủ → mới push (CI check 4 bước).
5. **Không commit secret**: MinIO accessKey/secretKey, JWT keys chỉ trong `.env`/`MinioOptions`, CI dùng env test.