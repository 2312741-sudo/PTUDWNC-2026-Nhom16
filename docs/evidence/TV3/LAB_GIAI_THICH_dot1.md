# Giải thích code LAB đợt 1 — K10, K23 (TV3 Huỳnh Quốc Trung, nhánh practice/TV3/labs)

Code do AI viết theo yêu cầu; file này để mình đọc hiểu và tự kiểm tra trước khi nghiệm thu.

## K10 — Phân quyền và giới hạn tần suất (`labs/TV3/Lab.TV3.Api/L10/AuthorizationEndpoints.cs`)

**1. Claim trong JWT** (`L1/TokenService.cs`): token mang `role` (Author/Admin) và `verified_author` (true/false) đọc từ bảng `lab_users`.
Server chỉ tin claim vì token được ký bằng khóa bí mật. Hệ quả: đổi role trong DB thì phải **đăng nhập lại** mới có token mới
(test làm đúng như vậy). `RoleClaimType = "role"` giúp `IsInRole("Admin")` đọc đúng claim.

**2. Ba policy** (`AddL10Authorization`):
- `Admin` = đã đăng nhập + `RequireRole("Admin")` → `/lab/l10/admin/stats`.
- `VerifiedAuthor` = `RequireAssertion`: có claim `verified_author=true` **hoặc** là Admin (Admin vượt cấp) → `/publish`.
- `PostOwner` = requirement tùy biến, xử lý bởi `PostOwnerHandler`.

**3. Kiểm quyền chủ sở hữu (resource-based)**: policy thường chỉ nhìn user, không biết bài nào. Vì vậy endpoint `Update`/`Publish`
tải bài từ DB trước, rồi gọi `auth.AuthorizeAsync(user, post, "PostOwner")`. Handler so `sub` trong JWT với `post.OwnerId`
(không tin `ownerId` client gửi lên). Sai → `Results.Forbid()` = 403, và **không chạy câu UPDATE** (test kiểm DB không đổi).
`/publish` phải qua **cả hai**: policy VerifiedAuthor ở endpoint và kiểm chủ bài trong handler.

**4. 401 và 403 khác nhau**: `MapGroup(...).RequireAuthorization()` bắt buộc đăng nhập → khách không token bị 401 (chưa xác thực).
Đã đăng nhập nhưng thiếu quyền → 403 (bị cấm).

**5. Rate limit**: `AddRateLimiter` + `GetFixedWindowLimiter` — cửa sổ cố định 1 phút, 5 lượt, `QueueLimit = 0` (vượt là từ chối ngay).
Khóa phân vùng = claim `sub` (mỗi user một "xô" riêng, user khác không bị ảnh hưởng); khách dùng IP. `OnRejected` trả 429 dạng
Problem Details có `code = RATE_LIMITED` và header `Retry-After` (số giây chờ). `UseRateLimiter()` đặt **sau** `UseAuthorization()`
để khách bị 401 trước, không tốn lượt, và lúc đó `User` đã có claim `sub`.

**Tự kiểm tra K10**
1. Vì sao Admin vừa bị đổi role trong DB vẫn bị 403 cho đến khi đăng nhập lại?
2. Tại sao kiểm chủ sở hữu không làm được bằng policy `RequireClaim` thông thường mà cần `AuthorizeAsync(user, post, ...)`?
3. Nếu chạy 2 instance API sau Nginx thì giới hạn 5 bình luận/phút thực tế còn đúng không? Sửa thế nào (gợi ý: Redis)?
4. Nếu đặt `UseRateLimiter()` trước `UseAuthentication()` thì phân vùng theo user còn hoạt động không? Vì sao?

## K23 — Triển khai, 2 instance, backup/restore (`labs/TV3/deploy/`)

**1. Header `X-Instance`** (`Program.cs`): middleware gắn tên instance (`Instance:Name`, mặc định hostname container) vào mọi response
→ nhìn header biết Nginx đã chuyển request tới container nào. Test `L23ScalingTests` chạy 2 app cùng DB: token và refresh token
phát ở instance A dùng được ở B. Đây là điều kiện để scale ngang: API **stateless** (JWT ký chung khóa, refresh token nằm trong DB chung).

**2. Dockerfile multi-stage**: stage `build` dùng image SDK để `restore` (copy csproj trước → cache layer restore) và `publish`;
stage `runtime` chỉ copy thư mục publish sang image `aspnet` nhỏ, chạy bằng user không phải root (`USER $APP_UID`).
Build context là gốc repo vì cần `Directory.Build.props`/`global.json`; `Dockerfile.dockerignore` loại mọi thứ khác.

**3. Compose** (`docker-compose.lab.yml`): khối dùng chung `x-api` (anchor YAML) cho api1/api2: cùng image, cùng chuỗi kết nối
DB `lab_tv3` (Hangfire dùng schema `lab_tv3_hangfire` trong DB đó → 2 server Hangfire chia việc, không chạy trùng job),
cùng Redis `lab-tv3-redis`, cùng `Jwt__SigningKey`. Bí mật đọc từ biến môi trường `${LAB_PG_PASSWORD:?}` — thiếu là compose báo lỗi,
không có giá trị nào ghi trong file. `mem_limit` giới hạn RAM từng container vì máy chỉ 7,7 GB.

**4. Nginx** (`nginx.conf`): `upstream` round-robin 2 server; `max_fails=1 fail_timeout=10s` (passive health check);
`proxy_next_upstream error timeout http_502 http_503` → instance chết giữa chừng thì Nginx thử instance còn lại (chỉ với request
idempotent như GET; POST không tự gửi lại để tránh ghi trùng).

**5. Backup/restore DB** (`backup-restore-db.ps1`): `pg_dump -Fc` (định dạng custom, nén) → tạo DB tạm `lab_tv3_restore` →
`pg_restore --no-owner` → đếm `count(*)` từng bảng chính ở 2 DB. Chốt an toàn: chỉ nhận tên DB `lab_tv3*`. Lần chạy đầu lộ lỗi
script: bảng chưa tồn tại thì cả 2 phía rỗng mà vẫn in "KHỚP" → đã sửa (null/psql lỗi = LỆCH). Bài học: script kiểm tra cũng phải được kiểm.

**6. Backup/restore file** (`backup-restore-files.ps1`): tạo manifest `sha256sum` + `tar czf` volume `lab-tv3-files`, giải nén vào
volume mới rồi `sha256sum -c` → chứng minh từng byte khôi phục đúng, không chỉ đếm số file.

**Tự kiểm tra K23**
1. Vì sao image cuối không chứa SDK, và lợi ích về dung lượng/bảo mật là gì?
2. Nếu mỗi instance tự sinh khóa JWT riêng thì chuyện gì xảy ra khi Nginx chuyển request sang instance kia?
3. Tắt `lab-tv3-api2` thì request GET đang đi tới nó được xử lý thế nào? Còn POST thì sao?
4. Tại sao kiểm restore bằng so số dòng / sha256 thay vì chỉ xem lệnh `pg_restore` không báo lỗi?
