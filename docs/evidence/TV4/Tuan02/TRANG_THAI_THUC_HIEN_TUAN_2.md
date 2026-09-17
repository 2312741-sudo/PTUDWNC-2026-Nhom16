# TRẠNG THÁI THỰC HIỆN TUẦN 2 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)
> **Nhánh Git**: `2312739_NHTSon_D1-D3-D5-D6` (tv4/week2)
> **Lab nhánh**: `practice/TV4/L4`
> **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
> **Cập nhật lần cuối**: 17/09/2026

> File này ghi lại trạng thái thực hiện các task tuần 2 (D1, D2, D3, D4 nền, D6 tiếp), các điểm cần bàn luận và lý do.
> Chi tiết kế hoạch xem `KE_HOACH_TUAN_2_TV4.md`.

---

## 1. Đã hoàn thành

> Tất cả task bắt đầu ở trạng thái **Chưa làm**. Bảng này cập nhật khi có kết quả.

| Task | Nội dung | Nơi triển khai | Trạng thái |
|---|---|---|---|
| — | Chưa có task nào hoàn thành (bắt đầu triển khai sau khi được confirm) | — | Chưa làm |

---

## 2. Đang làm / Chưa thực hiện

| Task | Nội dung | Lý do chưa xong | Cần gì để xong |
|---|---|---|---|
| D1 | `MinioStorageService` + API upload/metadata/primary/delete + validator | Chưa bắt đầu | Xác nhận D27 bucket policy + bắt đầu triển khai |
| D2 | Resize original/300×300/800×600 + job nền | Chưa bắt đầu | Chốt queue (Hangfire/BackgroundService) theo nhóm |
| D3 | Publish/unpublish CQRS + 422 + ownership | Chưa bắt đầu | Recipe entity + ingredient/step fixture từ TV3 |
| D3 | Logout revoke refresh family | Chưa bắt đầu | TV3 C5 refresh token merge |
| D4 (nền) | Uploader UI + status button + sitemap/robots nền | Chưa bắt đầu | Chốt D27 ảnh hiển thị (presigned/proxy) |
| D6 (tiếp) | Lab `practice/TV4/L4` + sổ evidence K | Chưa bắt đầu | G1 đã đóng; tạo nhánh lab |

---

## 3. Bị block (phụ thuộc người khác / quyết định)

| Task | Nội dung | Block bởi | Thời điểm dự kiến gỡ |
|---|---|---|---|
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
6. Điều kiện xác minh trên máy local: chỉ có .NET 9 SDK → chủ yếu dựa CI GitHub Actions (.NET 10) để build/test, giống Tuần 1.

---

## 6. Cổng hoàn thành tuần 2

| Cổng | Tiêu chí | Trạng thái |
|---|---|---|
| **G2 giữa tuần** | Upload ảnh hợp lệ → `StoredFile` URLs; set primary đúng 1; DELETE xoá object không orphan | Chưa đạt |
| **G3 cuối tuần** | Draft → thêm ingredient/step + ảnh → publish thành công; unpublish ẩn public; Draft/Archived không lộ; 4 MIME ≤5MiB; resize 3 kích thước; CI pass | Chưa đạt |