# Auth contract tuần 1 — TV1

Trạng thái: đã triển khai để tích hợp; các lựa chọn D02/D03/D18/D24 vẫn **chờ xác nhận** theo kế hoạch gốc. Prefix `/api/v1`, JSON camelCase. OpenAPI thực tế: `/openapi/v1.json` khi Development.

| Method/route | Request | Thành công | Lỗi chính |
|---|---|---|---|
| POST /auth/register | email, password, displayName | 201 + Location `/api/v1/auth/me`, AuthResponse | 400 validation/JSON, 409 email trùng |
| POST /auth/login | email, password | 200 AuthResponse | 400 validation, 401 thông tin sai, 403 inactive |
| GET /auth/me | Bearer JWT | 200 UserDto | 401 thiếu/sai/hết hạn JWT, 403 inactive, 404 user không còn |

`UserDto`: `{ id: string, email: string, displayName: string, roles: string[] }`.
`AuthResponse`: `{ accessToken: string, tokenType: "Bearer", expiresIn: 900, user: UserDto }`.
Chưa có `refreshToken`; TV3 cần bổ sung contract trước khi tích hợp rotation. TV4 chưa thể nghiệm thu revoke/logout với phiên bản access-token-only này. Google do TV2 bàn giao riêng.

Email được trim và normalize bằng Identity; unique index NormalizedEmail chống tranh chấp. DisplayName 1–100 ký tự văn bản, trim; password 8–128 ký tự, hoa/thường/số/đặc biệt. Field lạ bị từ chối, không chấp nhận role/userName từ client. Server ánh xạ UserName=email, role đăng ký luôn Author. EmailConfirmed mặc định false.

JWT HS256: `sub`, `userId`, `email`, `role` (một hoặc nhiều), `jti`, `iat`, `nbf`, `exp`, `iss`, `aud`. TTL 900 giây, không clock skew. Secret ≥64 byte UTF-8; cấu hình thiếu gây startup fail. `ICurrentUser.UserId` lấy từ `sub`; không lấy authorId từ body để cấp quyền. Policies `AuthorPolicy` cho Author/Admin, `AdminPolicy` chỉ Admin. Token chứa role tại thời điểm cấp; việc thay role chưa revoke token, hiệu lực còn lại tối đa 15 phút.

Response auth có `Cache-Control: no-store`; lỗi RFC7807 media type `application/problem+json`: status/title/type/instance khi có, `code`, `traceId`, và `errors` camelCase cho validation. `X-Correlation-ID` do server sinh; ghi mã này khi báo lỗi, không chụp token/password vào evidence.

Log chỉ metadata: request type, path, status, duration, UserId, CorrelationId, TraceId. Không log body, query string, header Authorization hoặc exception message có thể chứa SQL/password. Không bật EF sensitive logging. HTTPS/CORS/proxy integration do TV4 phối hợp; chưa dùng API này trực tiếp trên Internet trước khi hoàn tất tuần 2/bảo mật triển khai.
