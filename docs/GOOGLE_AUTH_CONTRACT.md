# Google Authentication Contract (D04) — TV2 (Ngô Quốc Trường Vĩ)

Trạng thái: **Đã chốt phương án kiến trúc theo D04 để phối hợp với TV1**  
Chủ trì: **Ngô Quốc Trường Vĩ (TV2 — Discovery & Google Auth)**  
Phối hợp: **Nguyễn Thanh Tâm (TV1 — Leader & Identity System)**  
Nguồn SRS: Tr. 19–20, Tr. 47–50, Phụ lục Chương 8.

---

## 1. Kiến trúc luồng xác thực Google OAuth2 (Code Flow + PKCE)

Để đảm bảo an toàn tuyệt đối và giải quyết mâu thuẫn **D04**:
1. **Frontend (Next.js):** Sử dụng **Auth.js v5 (NextAuth)** với Google Provider, kích hoạt OAuth2 Authorization Code Flow kèm **PKCE (Proof Key for Code Exchange)**. Client tương tác trực tiếp với Google Accounts endpoint an toàn qua trình duyệt.
2. Sau khi xác thực với Google thành công, Auth.js nhận được Google credentials bao gồm **`id_token`** (JWT được ký bởi Google RS256).
3. Frontend gọi API Backend:
   ```http
   POST /api/v1/auth/google
   Content-Type: application/json

   {
     "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6..."
   }
   ```
4. **Backend (.NET 10):**
   * Sử dụng thư viện chính thức `Google.Apis.Auth` (`GoogleJsonWebSignature.ValidateAsync(idToken, settings)`).
   * Kiểm tra chữ ký với Google Public Keys, kiểm tra `Audience == GoogleClientId` và thời hạn `exp`.
   * **Tuyệt đối không nhận profile tự khai (email, name) từ client** để làm bằng chứng đăng nhập.
   * Lấy `email`, `sub` (Google User ID), `name`, `picture` từ token đã xác minh.
   * Nếu `email` chưa tồn tại: Tạo mới `ApplicationUser` với role `Author`, `EmailConfirmed = true`, `DisplayName = name ?? email`.
   * Nếu `email` đã tồn tại: Liên kết tài khoản với provider Google (`UserLoginInfo("Google", sub, "Google")`).
   * Trả về `AuthResponse` đồng bộ với luồng đăng nhập email chuẩn của TV1 (gồm `accessToken` 15 phút, `user`, và sau này TV3 bổ sung `refreshToken`).

---

## 2. Đặc tả Endpoint Backend

### `POST /api/v1/auth/google`

* **Request Body:**
```json
{
  "idToken": "string (bắt buộc, không được để trống)"
}
```

* **Phản hồi thành công (`200 OK`):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "user": {
    "id": "c1f7b492-911d-4089-9a72-f67e5bb68c01",
    "email": "truongvi.ngo@example.com",
    "displayName": "Ngô Quốc Trường Vĩ",
    "roles": ["Author"]
  }
}
```

* **Phản hồi lỗi (Problem Details RFC 7807):**
  * `400 Bad Request` (`code: "auth.google_token_invalid"`): Token Google không hợp lệ, sai chữ ký, hết hạn hoặc sai audience.
  * `401 Unauthorized` (`code: "auth.google_email_unverified"`): Email Google chưa được xác minh bởi Google.
  * `403 Forbidden` (`code: "auth.account_disabled"`): Tài khoản liên kết với email này đã bị vô hiệu hóa (`IsActive = false`).
  * `502 Bad Gateway` (`code: "auth.google_upstream_error"`): Lỗi kết nối đến máy chủ xác thực của Google.

---

## 3. Cấu hình Môi trường (Environment Variables)

* **Frontend (`.env.local`):**
  ```bash
  AUTH_SECRET=your_authjs_secret_key_32_chars
  AUTH_GOOGLE_ID=your_google_client_id.apps.googleusercontent.com
  AUTH_GOOGLE_SECRET=your_google_client_secret
  NEXT_PUBLIC_API_URL=http://localhost:5000/api/v1
  ```
* **Backend (`appsettings.json` / User Secrets):**
  ```json
  {
    "Authentication": {
      "Google": {
        "ClientId": "your_google_client_id.apps.googleusercontent.com"
      }
    }
  }
  ```
