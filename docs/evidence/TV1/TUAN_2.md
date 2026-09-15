# Evidence tuần 2 — Nguyễn Thanh Tâm (2312741)

Trạng thái: đã triển khai phần TV1; chờ review/tích hợp nhóm.

## Đã hoàn thành

- A2: Identity lockout với 5 lần sai trong 15 phút; đăng nhập dùng `SignInManager` với `lockoutOnFailure: true`, lỗi credential vẫn dùng thông báo chung.
- A2: rate limit riêng cho `/api/v1/auth/register` và `/api/v1/auth/login`, 10 request/phút/IP, trả 429 và `Retry-After: 60`.
- A3: `PATCH /api/v1/auth/me`, chỉ cập nhật `displayName`, `avatarUrl`, `bio`; validator chặn field rỗng, control character, HTML và URL không hợp lệ.
- A4: hàng đợi email chào mừng sau transaction đăng ký; HTML encode tên; SMTP/MailHog cấu hình qua `Smtp:*`; worker thử lại sau 1, 5 và 30 phút, không ghi secret vào log.
- A7: build/test hồi quy và cập nhật contract, README, CHANGELOG, ADR.

## Kiểm chứng

- `dotnet build CulinaryBlog.sln --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false`: đạt 0 lỗi, 0 cảnh báo.
- Bộ test hồi quy gồm 17 test: lần chạy cuối ngày 16/09/2026 đạt **17/17 pass** trên PostgreSQL 16 riêng tại `127.0.0.1:55432`. Một lần chạy trước đó bị 429 do test dùng chung IP; môi trường `Testing` hiện được cô lập permit limit, còn production vẫn giữ 10 request/phút/IP.
- `dotnet format CulinaryBlog.sln --verify-no-changes --no-restore`: đã chạy kiểm tra.

## Cách demo

Khởi động PostgreSQL và MailHog, đặt `ConnectionStrings__Database`, `Jwt__SigningKey` (ít nhất 64 byte), `Smtp__Host=localhost`, `Smtp__Port=1025`, sau đó chạy migration và API theo README. Gửi sai mật khẩu 5 lần để thấy 423; gửi hơn 10 request auth trong một phút để thấy 429; gọi PATCH `/api/v1/auth/me` với Bearer token; đăng ký user mới và kiểm tra MailHog.

## Giới hạn/chờ bàn giao

Frontend Next.js/RHF/Zod chưa có trong repository nên chưa thể tích hợp form UI. Refresh token, logout, Google, welcome email persistent/outbox và Hangfire thuộc phần bàn giao tiếp theo; worker tuần 2 hiện dùng queue trong bộ nhớ và bỏ qua gửi khi chưa cấu hình SMTP. Cần TV3/TV4 review contract trước khi merge vào nhánh tích hợp.
