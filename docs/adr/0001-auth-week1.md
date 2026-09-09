# ADR 0001 — auth nền tuần 1

Ngày: 09/09/2026. Người thực hiện: Nguyễn Thanh Tâm (2312741).
Trạng thái: **Đề xuất đã triển khai để kiểm thử, chưa có xác nhận giảng viên/nhóm**. Không xem lựa chọn trong code là phê duyệt các mâu thuẫn SRS.

- D02: validation 400, duplicate email 409. Concurrency recipe chưa thuộc triển khai này.
- D03: displayName; UserName=email; register auto-login trả user + accessToken/tokenType/expiresIn.
- D18: Domain không phụ thuộc Identity/EF/MediatR. ApplicationUser đặt Infrastructure, lớp Application gọi IIdentityService/ICurrentUser.
- D24: Identity user Id string; các module recipe dùng FK string tương ứng. Hai role có ID seed ổn định, không seed tài khoản đặc quyền.
- D20: chưa áp dụng VerifiedAuthor để tránh khóa Author mới khi chưa có luồng xác minh email. Không tự gán EmailConfirmed=true.
- D05/D06: refresh token và logout chưa triển khai; tránh tạo raw refresh token không có rotation/revoke. Handoff rõ cho TV3/TV4.

Đăng ký dùng transaction bao gồm tạo user + gán Author. Unique normalized email ở DB xử lý cả race; không chỉ kiểm tra trước khi insert. PBKDF2 Identity V3 SHA512 100.000 iterations, JWT HS256 15 phút. Key lấy từ cấu hình bên ngoài và kiểm tra độ dài lúc startup.

MediatR 12.5.0 được khóa để dùng API đã kiểm thử; dependency versions và transitives có packages.lock.json. Microsoft.OpenApi khóa 2.7.5 để xử lý [GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc). Cấu hình bearer theo [Microsoft Minimal APIs security](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security?view=aspnetcore-10.0).

Chưa có email welcome outbox/job, lockout và rate limit (tuần 2). Chưa tuyên bố hoàn thành toàn bộ FR-AUTH-001/002 hoặc NFR-SEC. Khi bổ sung refresh, TV3 phải mở rộng response, persistence và test transaction nhất quán trước merge.
