# GIÁO TRÌNH PHÁT TRIỂN ỨNG DỤNG WEB NÂNG CAO

Phiên bản V4 · .NET 10 + Next.js App Router

## TÀI LIỆU ĐẶC TẢ YÊU CẦU PHẦN MỀM

**Software Requirements Specification (SRS)**

Tiêu chuẩn IEEE 830 / ISO/IEC/IEEE 29148:2018

| Thuộc tính | Giá trị |
|---|---|
| Dự án | Blog Ẩm thực và Nấu ăn (Culinary Blog) |
| Phiên bản tài liệu | 1.1.1 |
| Ngày phát hành | 16/09/2026 |
| Trạng thái | Đã duyệt (Approved) |
| Công nghệ Backend | .NET 10 Minimal APIs, C# |
| Công nghệ Frontend | Next.js App Router, TypeScript |
| Cơ sở dữ liệu | PostgreSQL 16 |
| Object Storage | MinIO (S3-Compatible) |
| Cache | Redis 7 |

> **Ghi chú:** Tài liệu phiên bản 1.1.1 giải quyết các mâu thuẫn nội tại được phát hiện
> trong bản 1.0.0 (tham chiếu `SRS_Contradictions_Report.md`) và bổ sung sửa lỗi
> nhất quán giữa §8.1 (API Summary) và §3.1 (FR Chi tiết). Xem LỊCH SỬ THAY ĐỔI.

---

## LỊCH SỬ THAY ĐỔI TÀI LIỆU

| Phiên bản | Ngày | Tác giả / Vai trò | Trạng thái | Nội dung thay đổi |
|---|---|---|---|---|
| 1.1.1 | 16/09/2026 | Senior BA / Architect | Approved | Sửa lỗi nhất quán §8.1 Auth API Summary: response format (request body + response body + field names) cho tất cả 7 endpoints `/auth/*` khớp với FR chi tiết §3.1. Bổ sung `fullName`, `userName`, `emailConfirmed`, `createdAt` vào response; chuẩn hóa `expiresAt` (thay `expiresIn`); đồng bộ request body register (`fullName` thay `displayName`). |
| 1.1.0 | 16/09/2026 | Senior BA / Architect | Approved | Giải quyết 9 mâu thuẫn nội tại (C01–C09): Thống nhất chiến lược **soft delete** cho Recipe; điều kiện publish (≥1 ingredient VÀ ≥1 step); TTL cache danh mục = 60 phút; default pageSize = 12; chuẩn hóa field `TimerMinutes`/`OrderIndex`; xóa Category là **hard delete**; response luôn wrap trong `{ data }`; ràng buộc unique Name + Slug của Category. |
| 1.0.0 | 04/06/2026 | Senior BA / Architect | Approved | Phát hành lần đầu – bản hoàn chỉnh theo IEEE 830 / ISO 29148. |

---

## 9 MÂU THUẪN NỘI TẠI ĐÃ ĐƯỢC GIẢI QUYẾT (C01–C09)

1. **C01 — Xóa Recipe: Soft Delete**: Đặt `IsDeleted = true`, dùng Global Query Filter `!IsDeleted`. Giữ nguyên file ảnh trên MinIO (không xóa vật lý).
2. **C02 — Điều kiện Publish Recipe**: Cần ít nhất 1 nguyên liệu VÀ 1 bước thực hiện (≥1 ingredient AND ≥1 step). Thiếu sẽ trả về HTTP 422 `RECIPE_PUBLISH_INCOMPLETE`.
3. **C03 — TTL Cache Danh mục**: Thống nhất 60 phút (`IMemoryCache` / Redis).
4. **C04 — Default Page Size**: Thống nhất `pageSize = 12` cho cả công thức và tìm kiếm.
5. **C05 — Chuẩn hóa Field Name RecipeStep**: Dùng `TimerMinutes` (thay vì DurationMinutes).
6. **C06 — Chuẩn hóa Field Name RecipeIngredient**: Dùng `OrderIndex` (thay vì SortOrder).
7. **C07 — Xóa Category: Hard Delete**: Xóa vật lý khỏi database (DELETE). Kiểm tra nếu còn recipe thuộc category thì trả về HTTP 409 `CATEGORY_DELETE_HAS_RECIPES`.
8. **C08 — Response Wrapper**: **Toàn bộ response thành công của API đều wrap trong `{ "data": ... }`**.
9. **C09 — Ràng buộc Unique Category**: `Name` là duy nhất (UNIQUE constraint trong DB). `Slug` cũng là duy nhất (nếu trùng slug thì thêm suffix `-2`, `-3`...).

---

## TÓM TẮT CÁC ENDPOINT AUTHENTICATION (§8.1 v1.1.1)

- `POST /api/v1/auth/register` (body: `{ fullName, email, userName, password }`)
  → `201 Created`: `{ "data": { "accessToken", "refreshToken", "expiresAt", "user": { "id", "fullName", "email", "userName", "avatarUrl", "roles" } } }`
- `POST /api/v1/auth/login` (body: `{ email, password }`)
  → `200 OK`: `{ "data": { "accessToken", "refreshToken", "expiresAt", "user": { "id", "fullName", "email", "userName", "avatarUrl", "roles" } } }`
- `POST /api/v1/auth/google` (body: `{ idToken }`)
  → `200 OK`: `{ "data": { "accessToken", "refreshToken", "expiresAt", "user": { "id", "fullName", "email", "userName", "avatarUrl", "roles" } } }`
- `POST /api/v1/auth/refresh` (body: `{ refreshToken }`)
  → `200 OK`: `{ "data": { "accessToken", "refreshToken", "expiresAt", "user": { "id", "fullName", "email", "userName", "avatarUrl", "roles" } } }`
- `POST /api/v1/auth/logout` (body: `{ refreshToken? }`)
  → `204 No Content`
- `GET /api/v1/auth/me`
  → `200 OK`: `{ "data": { "id", "fullName", "email", "userName", "avatarUrl", "roles", "emailConfirmed", "createdAt" } }`
- `PATCH /api/v1/auth/me` (body: `{ fullName?, avatarUrl?, bio? }`)
  → `200 OK`: `{ "data": { "id", "fullName", "email", "userName", "avatarUrl", "roles", "emailConfirmed", "createdAt" } }`
