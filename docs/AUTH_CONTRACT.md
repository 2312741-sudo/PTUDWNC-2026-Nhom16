# Auth Contract (Cập nhật theo SRS v1.1.1) — TV1

Trạng thái: Đã cập nhật khớp hoàn toàn với **SRS v1.1.1 (§3.1 & §8.1)** và giải quyết mâu thuẫn C08 (Response format wrap trong `{ data }`). Prefix `/api/v1`, JSON camelCase. OpenAPI: `/openapi/v1.json` khi Development.

| Method/route | Request Body | Thành công (2xx) | Lỗi chính (4xx/5xx) |
|---|---|---|---|
| `POST /auth/register` | `{ fullName, email, userName?, password }` | 201 + Location `/api/v1/auth/me`<br>`{ "data": { "accessToken", "refreshToken", "expiresAt", "user": { "id", "fullName", "email", "userName", "avatarUrl", "roles" } } }` | 400 validation/JSON, 409 email trùng |
| `POST /auth/login` | `{ email, password }` | 200 OK<br>`{ "data": { "accessToken", "refreshToken", "expiresAt", "user": { "id", "fullName", "email", "userName", "avatarUrl", "roles" } } }` | 400 validation, 401 thông tin sai, 423 locked |
| `GET /auth/me` | Bearer JWT | 200 OK<br>`{ "data": { "id", "fullName", "email", "userName", "avatarUrl", "roles", "emailConfirmed", "createdAt" } }` | 401 thiếu/sai/hết hạn JWT, 403 inactive, 404 user không còn |
| `PATCH /auth/me` | Bearer JWT + `{ fullName?, avatarUrl?, bio? }` | 200 OK<br>`{ "data": { "id", "fullName", "email", "userName", "avatarUrl", "roles", "emailConfirmed", "createdAt" } }` | 400 validation/JSON, 401 unauthorized, 403 inactive |
| `POST /auth/logout` | Bearer JWT + `{ "refreshToken"? }` | 204 NoContent | 401 thiếu/sai token |

### Mô hình dữ liệu chuẩn:
- **`UserDto`**: `{ "id": string, "email": string, "fullName": string, "userName": string, "roles": string[], "avatarUrl"?: string | null, "bio"?: string | null, "emailConfirmed": boolean, "createdAt": string }`.
- **`AuthResponse`**: `{ "accessToken": string, "refreshToken"?: string | null, "tokenType": "Bearer", "expiresIn": 900, "expiresAt": string (ISO 8601), "user": UserDto }`.
- **Response Wrapper (C08)**: Toàn bộ response thành công được wrap trong `{ "data": ... }`.

### Quy tắc bảo mật & xác thực:
- Email được trim và normalize; unique index NormalizedEmail chống trùng lặp.
- Mật khẩu 8–128 ký tự, có chữ hoa/thường/số/ký tự đặc biệt.
- Account Lockout: Khóa tài khoản sau 5 lần sai liên tiếp trong 15 phút (HTTP 423).
- Rate Limiting: 10 request/phút/IP (HTTP 429 kèm `Retry-After: 60`).
- Token JWT HS256: thời hạn 15 phút (900 giây).
