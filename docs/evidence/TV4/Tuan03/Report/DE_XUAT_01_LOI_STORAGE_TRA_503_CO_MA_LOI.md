# Đề xuất gỡ block B1 — Trả `503 storage.unavailable` thay vì `500 server.error` khi lỗi object storage

> **Block**: [`TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md`](./TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md) §2 B1
> **Mức**: 🔴 Cao · **Nguồn**: §6 mục 2 của [`BAO_CAO_LOI_UPLOAD_ANH_500.md`](./BAO_CAO_LOI_UPLOAD_ANH_500.md)
> **Cần ai quyết**: nhóm (thống nhất contract lỗi) + TV3 (consumer của API)

---

## 1. Thực trạng

| Mặt | Hiện tại | Người dùng thấy |
|---|---|---|
| Storage sai credential / down / bucket không tạo được | `500` + `code: "server.error"` | "Có lỗi hệ thống. Vui lòng thử lại." |
| Storage chậm/quá tải | `500` + `code: "server.error"` | như trên — **không retry được** vì FE không biết đây là lỗi tạm thời |
| Lỗi nghiệp vụ thật (recipe không tồn tại, sai quyền) | `404`/`403` + mã rõ ràng | thông báo đúng ý nghĩa |
| Lỗi validation đầu vào | `400 validation.failed` | thông báo đúng ý nghĩa |

Cơ chế hiện tại: `MinioStorageService` **cố tình không nuốt exception** (`MinioStorageService.cs:42`),
`MinioException` lan lên tầng API, không khớp nhánh nào trong `ApiExceptionHandler.cs:12-28`
(`ValidationException` / `AppException` / `DbUpdate*` / `DomainException` / `BadHttpRequestException`)
⇒ rơi vào nhánh generic `ApiExceptionHandler.cs:29-32`.

## 2. Nguyên nhân

1. **Thiếu nhánh ánh xạ lỗi hạ tầng.** Handler chỉ biết lỗi nghiệp vụ; lỗi I/O hạ tầng không có mã riêng.
2. **`MinioException` là loại lỗi của SDK**, không phải loại mà tầng Application được phép phụ thuộc
   ⇒ không thể map thẳng ở handler mà không tạo phụ thuộc ngược (kiến trúc đang cấm: `ArchitectureTests.cs`).
3. **Chưa có abstraction lỗi storage** ở tầng Application (`IFileStorageService` trả `StoredFile` hoặc ném exception thô).

## 3. Giải pháp đề xuất

### Phương án A (khuyến nghị) — bọc lỗi ở tầng Infrastructure + mã lỗi ở handler

1. Thêm exception ở tầng Application (đã có sẵn `AppException`): dùng `AppException(503, "storage.unavailable", ...)`.
2. `MinioStorageService` bọc các lệnh SDK bằng một `try/catch` **có chủ đích**:

```csharp
// MinioStorageService.cs — bọc MinioException thành AppException 503
catch (MinioException ex)
{
    _logger.LogError(ex, "Object storage thao tac that bai: {Op} {Bucket}", op, _options.Bucket);
    throw new AppException(503, "storage.unavailable",
        "Dich vu luu tru anh tam thoi khong kha dung. Vui long thu lai sau.");
}
```

3. `ApiExceptionHandler` **không cần sửa** — `AppException` đã được map thành đúng status + `code`
   (`ApiExceptionHandler.cs:16-17`).
4. FE (`recipe-editor.ts:175` + `ImagesStep.tsx`) hiện đã đọc `body?.code` và hiển thị
   `errorMessage(body, res.status)` ⇒ **chỉ cần thêm thông điệp tiếng Việt cho mã mới**, không
   phải sửa luồng xử lý lỗi.

### Phương án B — không đụng code, chỉ sửa tài liệu lỗi

Giữ `500` nhưng bổ sung `Detail` kèm tên biến cấu hình còn thiếu (rẻ hơn, nhưng người dùng
vẫn thấy thông báo chung chung và FE vẫn không retry được).

### Có thể gộp thêm (nếu nhóm đồng ý)

Cho phép client gửi `Retry-After` và cân nhắc retry có backoff ở FE cho mã `storage.unavailable`
(lỗi hạ tầng tạm thời, khác hẳn lỗi nghiệp vụ).

## 4. Rủi ro

| Rủi ro | Khả năng | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| Đổi 500 → 503 làm test hiện có đỏ | Cao | CI đỏ, mất thời gian | Có 1 test E2E `MinioE2ETests` có thể kỳ vọng 5xx; grep `server.error` trong test trước khi sửa |
| FE chưa có thông điệp cho mã `storage.unavailable` ⇒ hiện text thô | Cao | UX xấu | Thêm message trong FE **cùng PR**; hoặc đặt `Title`/`Detail` tiếng Việt sẵn ở API để FE hiện được |
| `AppException` 503 bị nuốt ở tầng dưới (Hangfire job resize) | Trung bình | Job retry 3 lần rồi fail, log khó đọc | Bọc lỗi riêng trong `ResizeImageJob` (đã có `catch` decode) hoặc để `MinioException` lan ra như cũ ở job |
| `AppException` ở tầng Infrastructure ⇒ vi phạm kiến trúc nếu Application không cho phép | Trung bình | `ArchitectureTests` đỏ | Kiểm tra `AppException` đang ở đâu; nếu ở Application thì Infrastructure **được** phép dùng (hướng phụ thuộc đúng) |
| Báo 503 khi storage chậm nhưng vẫn ghi được ⇒ thông báo sai | Thấp | Nhỏ | Chỉ bọc `MinioException`/`BucketNotFound`/`AccessDenied`, không bọc `OperationCanceledException` |

## 5. Test case

| # | Test case | Kỳ vọng sau khi sửa |
|---|---|---|
| 1 | Upload ảnh khi `Minio:AccessKey` sai (`Minio__AccessKey=wrong`) | `503` + `code: "storage.unavailable"` (**không** còn 500) |
| 2 | `GET /api/v1/resources/images/{key}` khi storage down | `503` + mã rõ ràng |
| 3 | Upload ảnh bình thường (storage OK) | `201`, hành vi **không** đổi |
| 4 | Ảnh > 5 MiB khi storage down | `400 file.too_large` (validator chạy **trước** storage — thứ tự ở `RecipeImages.cs` phải giữ) |
| 5 | Recipe không tồn tại + storage down | `404 recipe.not_found` (điều kiện DB kiểm trước) |
| 6 | Job resize ảnh khi storage down | Job `Failed` sau số lần retry, log có mã lỗi rõ |
| 7 | `GET /health/ready` khi storage down | Không phụ thuộc storage (xem block 04) |
| 8 | UI: bấm "Tậi lên" khi storage down | Thông báo *"Dịch vụ lưu trữ ảnh tạm thời không khả dụng"*, không phải *"Lỗi hệ thống"* |

## 6. Chuỗi lỗi liên quan (nếu có)

```
FE uploadImage (recipe-editor.ts:170)
  └─> POST /api/v1/recipes/{id}/images            -> 503 storage.unavailable   (sau khi sửa)
      └─> RecipeImages.cs:113 storage.UploadAsync
          └─> MinioStorageService.BucketExistsAsync / PutObjectAsync
              └─> RustFS 401 / 403 / 5xx / timeout
                  └─> MinioException
                      └─> [hiện tại] rơi vào nhánh generic -> 500 server.error
                          └─> [đề xuất] bọc -> AppException(503, "storage.unavailable")
```

Cùng chuỗi này hiện đang áp dụng cho **cả** proxy ảnh D27 (`Program.cs:540 storage.ReadAsync`) và
job resize D23 — sửa ở `MinioStorageService` là đủ cho cả ba.

## 7. Quyết định cần chốt

1. Có đồng ý đổi contract lỗi (thêm mã `storage.unavailable`, 500 → 503) không?
2. FE có được sửa cùng PR để có thông điệp riêng cho mã này không (TV3 phụ trách)?
3. Có cho phép FE retry tự động với mã này không (cần chốt số lần/backoff)?
