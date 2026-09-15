# Báo cáo Thực hành Cá nhân (LAB) — TV2 (Ngô Quốc Trường Vĩ)
**Kỹ năng thực hành:** K01, K02, K03, K04, K08 (Cơ chế Identity, Password Hash & Token Claims)  
**Nhánh thực hành:** `practice/TV2/L1-auth-category-spike`  
**Người thực hiện:** Ngô Quốc Trường Vĩ (2312796 — TV2)  
**Reviewer:** Nguyễn Thanh Tâm (Nhóm trưởng — TV1)

---

## 1. Mục tiêu bài LAB cá nhân
Theo nguyên tắc phân công dự án: mọi thành viên phải có minh chứng độc lập cho tất cả 24 nhóm kỹ năng K01–K24. Dù phần sản phẩm chính (SP) của TV2 là Danh mục và Tìm kiếm, TV2 vẫn phải tự thực hành, hiểu sâu và giải thích được cơ chế Bảo mật & Identity (K08) đã được xây dựng bởi TV1.

Nội dung nghiên cứu & thực hành:
1. **PBKDF2 Password Hashing:** Cấu trúc byte array Identity V3 (header byte `0x01`, 4 bytes key derivation PRF `0x02` = SHA512, 4 bytes iteration count $\ge 100.000$, salt 128-bit, subkey 256-bit).
2. **JWT Claims & Lifetime:** Giải mã cấu trúc token gồm `sub`, `userId`, `email`, `role`, `jti`, `exp` (900s), xác minh cơ chế clock skew = 0.
3. **Phân quyền Role-based:** Cơ chế bảo vệ endpoint Category bằng policy `AdminPolicy` (`RequireRole("Admin")`), đảm bảo tài khoản Author thường và Guest không thể can thiệp dữ liệu danh mục.

---

## 2. Các bước thực hiện & Kết quả kiểm chứng

### 2.1. Thử nghiệm thuật toán băm mật khẩu PBKDF2
Thực hiện tạo tài khoản thử nghiệm với chuỗi mật khẩu phức tạp:
```csharp
var password = "ViPassword@2026";
var hasher = new PasswordHasher<ApplicationUser>(Options.Create(new PasswordHasherOptions { IterationCount = 100_000 }));
var hash = hasher.HashPassword(user, password);
```
*Kết quả kiểm tra:*
* Định dạng byte: Hash trả về chuỗi Base64 dài 84 ký tự.
* Byte đầu tiên là `0x01` (Identity Version 3).
* 4 bytes tiếp theo là `0x00000002` (HMAC-SHA512).
* 4 bytes tiếp theo là số vòng lặp `100,000` (đáp ứng tiêu chuẩn NFR-SEC-001).

### 2.2. Kiểm tra phân quyền Admin trên API Danh mục
Kiểm thử trực tiếp gọi `POST /api/v1/categories`:
* **Trường hợp 1 (Chưa đăng nhập):** Trả về `401 Unauthorized`.
* **Trường hợp 2 (Đăng nhập với tài khoản Author thường):** Trả về `403 Forbidden` do endpoint yêu cầu `AdminPolicy`.
* **Trường hợp 3 (Đăng nhập với tài khoản Admin seed sẵn):** Trả về `201 Created` kèm header `Location: /api/v1/categories/{slug}`.

---

## 3. Kết luận
TV2 đã nắm vững cách thức hoạt động của hệ thống xác thực, token claims và phân quyền trong ASP.NET Core Identity, sẵn sàng cho việc tích hợp module Đăng nhập Google (Task B4) vào tuần 2.
