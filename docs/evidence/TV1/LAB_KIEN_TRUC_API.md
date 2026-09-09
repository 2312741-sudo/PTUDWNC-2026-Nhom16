# Lab nền kiến trúc và API — A6 tuần 1

Phần nền đã có trong sản phẩm được tái sử dụng làm minh chứng theo mục 9.1 kế hoạch, không sao chép backend thứ hai. Nhánh: `feat/TV1-week1-auth`. Chưa xác nhận hoàn thành toàn bộ K01–K24.

| Kỹ năng | Minh chứng thực thi | Demo |
|---|---|---|
| K01 | ADR 0001 + AUTH_CONTRACT | Giải thích D02/D03/D18 và phần chờ xác nhận |
| K02 | Minimal API + Problem Details + Scalar | register 201, invalid 400, duplicate 409, me 401 |
| K03 | Domain DisplayName + Application interfaces | ArchitectureTests kiểm tra lớp trong không reference EF/Web/Infrastructure |
| K04 (nền) | Register/Login/GetMe handlers, Logging/Validation behaviors | Test invalid request không gọi handler; cache behavior để lab sau |
| K06 (auth) | Identity migration/unique email/role seeds | Test database PG16 thật, đăng ký đồng thời |
| K08 (register/login) | Identity + JwtService | Decode claims, kiểm tra PBKDF2 bytes; rotation/reuse/logout chưa làm |
| K21 (nền) | xUnit unit/API/architecture tests | Chạy lệnh README, đọc TRX/coverage |

Thực hành lỗi: thử thêm field role=Admin vào register; API phải trả 400. Thử tạo hai request cùng email; đúng một 201 và một 409. Thử token giả; /me phải 401. Thử sửa Domain tham chiếu Infrastructure trong nhánh riêng khi demo, giải thích vì sao phá hướng phụ thuộc (không commit thay đổi sai vào sản phẩm).

Cần cá nhân tự chạy/demo và reviewer xác nhận; mã nguồn do công cụ hỗ trợ tạo không thay cho việc giải thích của sinh viên. Các LAB FTS/cache/media/SEO/concurrency theo tuần sau chưa có minh chứng.
