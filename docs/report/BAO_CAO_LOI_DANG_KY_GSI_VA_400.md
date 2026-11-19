# Báo cáo lỗi: Trang `/auth/register` — GSI initialize lặp khi gõ phím & `POST /auth/register` 400 "Dữ liệu không hợp lệ"

> **Người phát hiện & phân tích**: Nguyễn Hữu Trung Sơn (2312739 — TV4)
> **Ngày phát hiện**: 19/11/2026
> **Ngày xác minh/kết luận**: 19/11/2026
> **Mức độ**: **Blocker** — chặn luồng đăng ký bằng form (luồng E2E bắt buộc) và gây spam lỗi trên console
> **Phân loại**: Lỗi React effect dependency (frontend) + lỗi đồng bộ hợp đồng validate/hiển thị lỗi (frontend ↔ backend)
> **Phạm vi**: `/auth/register` — FR-AUTH-001/002, luồng E2E "đăng ký"
> **Branch / commit đối chiếu**: `main` — HEAD = `3f3bcfe` (19/11/2026)

> ⚠️ Phần §1–§4 ghi lại **đúng trạng thái branch `main` tại commit `3f3bcfe`** và được giữ nguyên
> làm bằng chứng gốc. **Trạng thái sau sửa: xem §8** — code đã được sửa trên branch
> `fix/TV4-auth-register-gsi-400` và chỉ verify ở mức **local/dev (Docker)**.

> 🔴 **CẦN KIỂM TRA LẠI SAU DEPLOY RENDER / VERCEL — BẮT BUỘC, KHÔNG THỂ LƯỜNG TRƯỚC Ở LOCAL.**
> Bản sửa chỉ được verify ở mức local/dev. Sau khi merge & deploy phải xác nhận (P1–P5, §8.2):
> (a) bản Vercel đã lấy commit PR → console `/auth/register` hết `[GSI_LOGGER]` và hiện rõ lỗi theo
> field; (b) API Render vẫn giữ đúng format Problem Details. **Không** tự sửa cấu hình production
> trong PR này.
>
> ✅ **Đính chính (quan trọng)**: nghi ngờ ban đầu "Render trả response generic, không khớp `main`"
> là **do lỗi escape JSON của `curl.exe` trong PowerShell**, không phải lỗi deploy. Gọi lại bằng
> `curl --data-binary @file` (§8.2) cho thấy API production **khớp chính xác** `main`: register invalid
> trả `400` + `errors` + `code:"validation.failed"`, login sai trả `401` + `code:"auth.invalid_credentials"`.

---

## 0. Tóm tắt một nén

| Hạng mục | Kết quả |
|---|---|
| Trang liên quan | `src/frontend/src/app/auth/register/page.tsx` → route `/auth/register` |
| Triệu chứng 1 | Gõ **một ký tự** vào bất kỳ ô input → console lặp liên tục: `[GSI_LOGGER]: google.accounts.id.initialize() is called multiple times...` và `[GSI_LOGGER]: FedCM get() rejects with NotAllowedError: Only one navigator.credentials.get request may be outstanding at one time.` (`client:89`) |
| **Nguyên nhân 1** | `onError={(err) => setErrorMessage(err)}` (inline) và `handleGoogleSuccess` (không memoize) truyền từ trang đăng ký → callback identity đổi sau mỗi lần gõ phím → effect khởi tạo GIS trong `GoogleSignInButton.tsx:136-169` chạy lại → gọi lại `initialize()` (dòng 142) **và** `prompt()` (dòng 165) |
| Triệu chứng 2 | Điền đủ form → bấm "Đăng ký ngay" → `POST https://culinary-api-frvx.onrender.com/api/v1/auth/register` → `400 Bad Request`, UI hiện *"Dữ liệu không hợp lệ."* |
| **Nguyên nhân 2** | Không phải validator từ chối payload hợp lệ — đã chạy thật `RegisterValidator`: payload y hệt frontend trả `VALID: True`. Lỗi là (a) rule mật khẩu yêu cầu **chữ thường `[a-z]`** trong khi label UI không hướng dẫn, và (b) key lỗi bị đổi sang camelCase ở backend nhưng frontend đọc PascalCase ⇒ **không hiển thị được lỗi cụ thể theo field**, người dùng chỉ thấy title chung chung |
| Xác nhận bằng thực chạy | `dotnet run` (SDK 10.0.404) với payload frontend → `VALID: True`; với password `PASSW0RD!` → `Password: 'Password' is not in the correct format.` → 400 |
| Phát hiện phụ | Ban đầu tưởng API production trả generic `400` do escape JSON sai trong PowerShell. Gọi lại đúng bằng `--data-binary @file`: response **khớp `main`** (`401` cho login sai, `400` + `errors` cho register invalid). Chi tiết §4 / §8.2 |
| Đã sửa chưa? | ✅ **Đã sửa** trên branch `fix/TV4-auth-register-gsi-400` — verify local/dev xong (§8). Chờ merge & kiểm tra lại sau deploy |

---

## 1. Mô tả lỗi và cách tái hiện

### 1.1 Lỗi 1 — GSI spam theo từng ký tự

| # | Thao tác | Kết quả mong đợi | Kết quả thực tế |
|---|---|---|---|
| 1 | Mở `/auth/register` trên môi trường có `NEXT_PUBLIC_GOOGLE_CLIENT_ID` (hoặc đã lưu Client ID trong dev modal) | Nút Google render 1 lần | ✅ Render 1 lần |
| 2 | Gõ 1 ký tự vào ô "Họ và tên" | Không có log mới | ❌ Console in 2 dòng `[GSI_LOGGER]` như trên |
| 3 | Gõ tiếp ký tự thứ 2 | Không có log mới | ❌ Log lặp lại thêm 1 chu kỳ |
| 4 | Quan sát Network / console | Không có request FedCM trùng | ❌ `FedCM get() rejects with NotAllowedError` |

> **Điều kiện kích hoạt**: `clientId` phải có giá trị. Khi `clientId` rỗng, component đi vào
> nhánh nút fallback (`GoogleSignInButton.tsx:254-293`) và **không** gọi GIS SDK — đó là lý do
> lỗi chỉ xuất hiện sau khi cấu hình Google Client ID (`render.yaml`, `docker-compose.staging.yml`
> tại commit `3f3bcfe`) hoặc sau khi nhập Client ID trong dev modal.

### 1.2 Lỗi 2 — 400 khi submit form

```http
POST https://culinary-api-frvx.onrender.com/api/v1/auth/register
Content-Type: application/json

{"fullName":"...","userName":"...","email":"...","password":"..."}
```

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
```

UI hiển thị: `Dữ liệu không hợp lệ.` — đây là `title` thật từ `ApiExceptionHandler` (đã đối chiếu
với production bằng curl đúng cách, xem §4 / §8.2). Lỗi là người dùng **không thấy lỗi cụ thể theo
field** do key camelCase/PascalCase không khớp.

---

## 2. Nguyên nhân lỗi 1 — chuỗi dependency của callback

### 2.1 Đoạn code gây lỗi

`src/frontend/src/app/auth/register/page.tsx`:

```tsx
// dòng 55 — KHÔNG được useCallback → identity đổi mỗi lần render
const handleGoogleSuccess = (auth: any) => { ... };

// dòng 231-236 — arrow function inline → identity đổi mỗi lần render
<GoogleSignInButton
  initialEmail={email}
  buttonText="Đăng ký nhanh bằng Google"
  onSuccess={handleGoogleSuccess}
  onError={(err) => setErrorMessage(err)}   // ← dòng 235
/>
```

`src/frontend/src/components/GoogleSignInButton.tsx`:

```tsx
// dòng 67  → useCallback(..., [onSuccess, onError])
const handleAuthPayload = useCallback(async (token: string) => { ... }, [onSuccess, onError]);

// dòng 98  → useCallback(..., [handleAuthPayload, onError])
const handleGoogleCredentialResponse = useCallback(..., [handleAuthPayload, onError]);

// dòng 136 → useEffect(..., [gsiLoaded, clientId, isRegister, handleGoogleCredentialResponse])
useEffect(() => {
  window.google.accounts.id.initialize({ ... });   // dòng 142
  ...
  window.google.accounts.id.renderButton(...);      // dòng 154
  window.google.accounts.id.prompt();               // dòng 165
}, [gsiLoaded, clientId, isRegister, handleGoogleCredentialResponse]);
```

### 2.2 Chuỗi nhân quả

```
Gõ 1 ký tự → setFullName(...) → RegisterPage re-render
   ↓
onError inline (dòng 235) + handleGoogleSuccess (dòng 55) được tạo lại
   ↓
handleAuthPayload đổi → handleGoogleCredentialResponse đổi
   ↓
useEffect khởi tạo GIS (dòng 136) chạy lại dù clientId/gsiLoaded không đổi
   ↓
google.accounts.id.initialize() gọi lại  → [GSI_LOGGER]: called multiple times
google.accounts.id.prompt()    gọi lại  → FedCM: Only one credentials.get outstanding
```

`prompt()` bị gọi lại trong khi request FedCM trước đó còn pending chính là nguồn của
`NotAllowedError`. Đây là lỗi **thuần frontend**, không liên quan tới backend hay API Google.

---

## 3. Nguyên nhân lỗi 2 — validator đúng, hợp đồng lỗi bị lệch

### 3.1 Bằng chứng: payload của frontend hợp lệ

Chạy một project console tạm tham chiếu `CulinaryBlog.Application.csproj`, deserialize JSON bằng
`PropertyNameCaseInsensitive = true` (đúng cấu hình mặc định của ASP.NET Core) rồi gọi
`new RegisterValidator().Validate(...)`:

```
=== DESERIALIZED ===
Email=test@example.test | Password=Demo-Password9! | FullName=Nguyễn Văn A | UserName=nguyenvana | DisplayName=<null>
=== VALID: True ===
```

⇒ Với dữ liệu đúng, `RegisterValidator` (`Auth.cs:137-157`) **không** trả lỗi. `RegisterCommand`
có `[JsonConstructor]` (`Auth.cs:38-46`) map đúng `fullName`/`userName` từ payload.

> **Lưu ý khi tự reproduce**: nếu dùng `JsonSerializer.Deserialize` thuần (console app) với
> options mặc định **case-sensitive**, mọi field sẽ về rỗng (`Email=`, `Password=`, `FullName=null`)
> và validator báo `Tên không được để trống.` — đây là **artifact của phép test**, không phải
> hành vi của ASP.NET Core. Phải set `PropertyNameCaseInsensitive = true`.

### 3.2 Nguyên nhân (a) — rule mật khẩu khớp với label không?

`src/backend/CulinaryBlog.Application/Auth.cs:142-143`:

```csharp
RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128)
    .Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]").Matches("[^a-zA-Z0-9]");
```

`src/frontend/src/app/auth/register/page.tsx:158`:

> `Mật khẩu (Tối thiểu 8 ký tự, 1 hoa, 1 số, 1 ký tự đặc biệt)`

**UI thiếu yêu cầu "1 chữ thường"**. Kết quả chạy thật:

```
=== CASE password khong co chu thuong ===
VALID: False
Password: 'Password' is not in the correct format.
```

Một người dùng nhập `PASSW0RD!` (9 ký tự, có hoa/số/đặc biệt) thấy mình đã "nhập đủ thông tin"
theo label, nhưng backend vẫn trả 400.

### 3.3 Nguyên nhân (b) — lỗi field bị nuốt ở frontend

`src/backend/CulinaryBlog.API/ApiExceptionHandler.cs:14-17`:

```csharp
if (exception is ValidationException validation)
    details = new HttpValidationProblemDetails(validation.Errors
        .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
    { Status = 400, Title = "Dữ liệu không hợp lệ.", Extensions = { ["code"] = "validation.failed" } };
```

Response trả về có dạng:

```json
{
  "title": "Dữ liệu không hợp lệ.",
  "status": 400,
  "code": "validation.failed",
  "errors": { "password": ["'Password' is not in the correct format."] }
}
```

`src/frontend/src/lib/api.ts:535-541` đọc `result.errors` → `validationErrors`, nhưng
`src/frontend/src/app/auth/register/page.tsx:108,129,150,178` lại đọc:

```tsx
validationErrors.FullName
validationErrors.UserName
validationErrors.Email
validationErrors.Password
```

| Backend trả (camelCase) | Frontend đọc (PascalCase) | Kết quả |
|---|---|---|
| `fullName` | `FullName` | `undefined` — không hiện |
| `userName` | `UserName` | `undefined` — không hiện |
| `email` | `Email` | `undefined` — không hiện |
| `password` | `Password` | `undefined` — không hiện |

Hệ quả: dù backend đã gửi lỗi cụ thể, giao diện **không hiển thị lỗi field nào**. Người dùng
chỉ thấy `res.error` lấy từ `result.title` (`api.ts:539`) = **"Dữ liệu không hợp lệ."** — chính
xác thông báo trong issue. Đây là lý do triệu chứng "nhập đủ rồi mà chỉ báo chung chung".

### 3.4 Có phải do backend từ chối username/email không?

Không phải trong trường hợp thông thường. `IdentityService.RegisterAsync`
(`IdentityService.cs:37-76`) chỉ trả `AppException(400, "auth.invalid_registration", "Thông tin đăng ký không hợp lệ.")`
khi `UserManager.CreateAsync` thất bại — ví dụ username chứa ký tự không được Identity cho phép
(mặc định chỉ `a-z A-Z 0-9 -._@+`). Trường hợp đó title sẽ là **"Thông tin đăng ký không hợp lệ."**,
khác với chuỗi "Dữ liệu không hợp lệ." trong issue, nên có thể loại trừ.

---

## 4. Phát hiện phụ: đối chiếu bản deploy production với `main` (đã đính chính)

### 4.1 Kết quả ban đầu (bị nhiễu)

Lần đầu gọi API production bằng `curl.exe` từ PowerShell với cú pháp
`--data "{\"email\":...}"` (escape kiểu bash) trả về response generic `400 {"title":"Bad Request"}`
cho cả register lẫn login, kể cả `/auth/me` → `401 {"title":"Unauthorized"}`.

### 4.2 Nguyên nhân của response generic — LỖI CURL, KHÔNG PHẢI DEPLOY

PowerShell **không** xử lý `\"` như bash; chuỗi JSON gửi đi chứa dấu `\` thừa ⇒ model binding của
API từ chối ở lớp `request.invalid` trước khi vào `ApiExceptionHandler`. Kiểm chứng: chạy đúng
cách trên **API local** (`curl --data "{\"...\"}"`) cũng trả `400 request.invalid`; chuyển sang
`--data-binary @file` thì trả đúng `validation.failed`.

### 4.3 Kết quả sau khi gọi lại đúng cách (`--data-binary @file`, 19/11/2026 ~02:51 GMT)

| Request | Kỳ vọng theo `main` | Response production thực tế | Khớp? |
|---|---|---|---|
| `POST /api/v1/auth/register` password `weak` | `400` + `title:"Dữ liệu không hợp lệ."` + `errors` | `400` + `title:"Dữ liệu không hợp lệ."` + `errors.password` + `code:"validation.failed"` | ✅ |
| `POST /api/v1/auth/login` sai email/password (`Program.cs:515` → 401) | `401` + `"Email hoặc mật khẩu không đúng."` | `401` + `title:"Email hoặc mật khẩu không đúng."` + `code:"auth.invalid_credentials"` | ✅ |
| `GET /api/v1/auth/me` không token | `401` | `401` (Problem Details) | ✅ |

⇒ **Kết luận đính chính:** bản Render đang chạy **khớp** `ApiExceptionHandler` của `main`. Không có
bằng chứng deploy sai; không đổi cấu hình production. Dữ liệu trên cũng **xác nhận trực tiếp** giả
thuyết §3: production trả key lỗi `errors.password` (camelCase) → đúng là nguyên nhân frontend đọc
nhầm `Password` (PascalCase) làm mất thông báo lỗi. Việc phải làm vẫn là kiểm tra lại sau khi
deploy bản sửa frontend (§8.2), không phải redeploy sửa backend.

---

## 5. Hướng sửa — **ĐỀ XUẤT, CHƯA THỰC HIỆN**

### 5.1 Lỗi GSI — chọn 1 trong 2

| Phương án | Cách làm | Ưu / Nhược |
|---|---|---|
| **A (tối thiểu)** | Bọc `handleGoogleSuccess` bằng `useCallback` và truyền `onError={useCallback(...)}` ở `register/page.tsx` | Sửa 1 file; vẫn giữ rủi ro nếu props khác đổi identity |
| **B (khuyến nghị)** | Trong `GoogleSignInButton.tsx`, lưu `onSuccess`/`onError` vào `useRef`, cập nhật ref mỗi render; effect khởi tạo chỉ phụ thuộc `[gsiLoaded, clientId, isRegister]`; tách riêng hiệu ứng gọi `prompt()` đúng **1 lần** | Triệt để — callback identity không còn ảnh hưởng tới SDK; tránh cảnh báo lẫn FedCM conflict |

Ngoài ra nên `cancel_on_tap_outside: true` và **không gọi `prompt()` khi component chỉ render lại**.

### 5.2 Lỗi 400

| # | Việc | File |
|---|---|---|
| 1 | Thêm "1 chữ thường" vào label password (hoặc bỏ `.Matches("[a-z]")` nếu muốn đúng label cũ) | `register/page.tsx:158` / `Auth.cs:143` |
| 2 | Chuẩn hóa key lỗi: đọc cả camelCase, ví dụ `validationErrors.password ?? validationErrors.Password` — hoặc bỏ camelCase ở `ApiExceptionHandler.cs:15` để trả nguyên PascalCase | `register/page.tsx:108-180` |
| 3 | Hiện toàn bộ `validationErrors` dạng danh sách để không field nào bị nuốt | `register/page.tsx` |
| 4 | (Đã làm) Đối chiếu API Render với `main` — xem §4.3: khớp, không cần redeploy backend | §4.3 / §8.2 |
| 5 | Thêm test validate payload frontend (`fullName` + `userName`, không có `DisplayName`) để chặn tái phát | `tests/CulinaryBlog.Tests/AuthTests.cs` |

### 5.3 Việc cần làm ngoài phạm vi sửa lỗi

| # | Việc | Lý do |
|---|---|---|
| V1 | Thêm E2E Playwright cho luồng đăng ký (luồng bắt buộc) | Hiện lỗi nằm đúng luồng bắt buộc nhưng không test nào bắt được |
| V2 | Thêm assertion format Problem Details (`title`, `code`, `errors`) trong test API | Chủ động phát hiện nếu sau này deploy trả sai format (bài học từ §4.1–4.2) |

---

## 6. Test case xác minh sau khi sửa

| # | Bước | Kỳ vọng |
|---|---|---|
| TC1 | Mở `/auth/register`, gõ 10 ký tự lần lượt vào các ô input | Console **không** có `[GSI_LOGGER]` nào |
| TC2 | Quan sát Network trong lúc gõ | Không có request `navigator.credentials.get` trùng |
| TC3 | Nhấn nút Google → đăng nhập bằng tài khoản thật | Callback chạy đúng 1 lần, `push('/dashboard/profile')` |
| TC4 | Đăng ký với password `Demo-Password9!` (có chữ thường) | `201`, đăng nhập được ngay |
| TC5 | Đăng ký với password `PASSW0RD!` | `400` + hiển thị rõ lỗi cạnh ô "Mật khẩu" |
| TC6 | Để trống họ tên, điền còn lại đúng | `400` + hiển thị lỗi cạnh ô "Họ và tên" (không chỉ title chung) |
| TC7 | Gọi `POST /api/v1/auth/login` sai thông tin | `401` + `"Email hoặc mật khẩu không đúng."` — kiểm tra luôn bản deploy |
| TC8 | Gọi `POST /api/v1/auth/register` password `weak` | `400` + `code: "validation.failed"` + `errors.password` |
| TC9 | Chạy `dotnet test` | Tất cả test xanh, `Skipped = 0` |
| TC10 | Chạy `npm run build` trong `src/frontend` | Xanh, không có warning hook dependency |

---

## 7. Thông tin môi trường đã dùng để xác minh

| Thành phần | Giá trị |
|---|---|
| Branch | `main` |
| HEAD | `3f3bcfe` — *chore(auth): configure public Google OAuth Web Client ID for Render, staging, and API defaults* (19/11/2026) |
| .NET SDK | `10.0.404` |
| Cách verify validator | Project console tạm ngoài repo, `ProjectReference` tới `CulinaryBlog.Application.csproj`, `PropertyNameCaseInsensitive = true` |
| Cách verify API production (đúng) | Ghi JSON vào file rồi `curl.exe -i --data-binary @file ...`; **không** dùng `--data "{\"...\"}"` trong PowerShell (xem §4.2) |
| Thời điểm gọi API production | 19/11/2026 ~02:13–02:14 GMT (lần đầu, nhiễu) và ~02:51 GMT (lần đính chính, đúng) |
| Trạng thái repo sau điều tra | Phát hiện ban đầu: sạch. Sau sửa: branch `fix/TV4-auth-register-gsi-400` có 3 file frontend + 2 file tài liệu (§8.1) |

---

## 8. Trạng thái sau sửa — local/dev only

### 8.1 Phạm vi đã sửa (branch `fix/TV4-auth-register-gsi-400`)

| File | Thay đổi |
|---|---|
| `src/frontend/src/components/GoogleSignInButton.tsx` | Lưu `onSuccess/onError` vào `useRef`; `handleAuthPayload`/`handleGoogleCredentialResponse` có dependency ổn định; effect khởi tạo GIS không còn phụ thuộc callback; `prompt()` tối đa 1 lần/`clientId` |
| `src/frontend/src/app/auth/register/page.tsx` | Memoize `handleGoogleSuccess`/`handleGoogleError`; đọc lỗi field cả camelCase lẫn PascalCase qua `errorOf()`; label password bổ sung "1 chữ thường" |
| `src/frontend/src/lib/api.ts` | `normalizeValidationErrors()` chuẩn hóa key lỗi từ `ApiExceptionHandler` sang cả 2 dạng |
| `docs/evidence/TV4/Misc/KE_HOACH_SUA_LOI_DANG_KY_AUTH_REGISTER.md` | Plan ngoài kế hoạch TV4 |
| Báo cáo này | Bổ sung cảnh báo deploy và mục §8 |

> Giới hạn theo yêu cầu: **chỉ sửa/test ở local/dev (Docker)**. Không đổi `render.yaml`,
> `docker-compose.staging.yml` hay bất kỳ env/cấu hình production nào.

### 8.2 🔴 CẦN KIỂM TRA LẠI SAU DEPLOY (Render API / Vercel frontend)

**Dẫn chứng production (đã gọi lại đúng cách, 19/11/2026 ~02:51 GMT):**

```powershell
# file register-prod.json: {"email":"debug-validation@example.test","password":"weak",
#                           "fullName":"Debug User","userName":"debuguser"}
curl.exe -i -X POST "https://culinary-api-frvx.onrender.com/api/v1/auth/register" `
  -H "Content-Type: application/json" `
  --data-binary "@C:\Users\admin\AppData\Local\Temp\opencode\register-prod.json"
# HTTP/1.1 400  Content-Type: application/problem+json
# {"title":"Dữ liệu không hợp lệ.","status":400,"instance":"/api/v1/auth/register",
#  "errors":{"password":["The length of 'Password' must be at least 8 characters...",
#                        "'Password' is not in the correct format."]},
#  "code":"validation.failed", ...}          ← KHỚP main, CÓ errors.password (camelCase)

# file login-prod.json: {"email":"nobody@example.test","password":"Wrong-Password9!"}
curl.exe -i -X POST "https://culinary-api-frvx.onrender.com/api/v1/auth/login" `
  -H "Content-Type: application/json" `
  --data-binary "@C:\Users\admin\AppData\Local\Temp\opencode\login-prod.json"
# HTTP/1.1 401  {"title":"Email hoặc mật khẩu không đúng.",
#                "code":"auth.invalid_credentials", ...}   ← KHỚP Program.cs:515
```

| Yêu cầu | Theo `main` | Production (curl đúng) |
|---|---|---|
| Register invalid | `400` + `title:"Dữ liệu không hợp lệ."` + `code:"validation.failed"` + `errors.password` | ✅ Khớp chính xác |
| Login sai | `401` + `title:"Email hoặc mật khẩu không đúng."` | ✅ Khớp chính xác |

> ⚠️ Lần đầu (02:13 GMT) dùng `--data "{\"...\"}"` → JSON hỏng → tưởng deploy sai. Đã đính
> chính ở §4.1–4.3. **Không có bằng chứng API Render sai**; API Render hiện đã đúng hợp đồng.

**Vì sao vẫn phải kiểm tra lại sau deploy?** Vì bản sửa nằm ở **frontend** (GSI + đọc lỗi field).
Local xanh chứng minh code đúng, nhưng chỉ bản deploy mới xác nhận được: (a) Vercel đã lấy commit
PR; (b) `errors.password` (camelCase — đã xác nhận production trả dạng này) được hiển thị thành
lỗi cạnh ô "Mật khẩu"; (c) console hết `[GSI_LOGGER]`. Đây là lỗi deploy **không thể lường trước ở
local** theo đúng yêu cầu giới hạn phạm vi.

| # | Bước kiểm tra SAU khi merge & deploy | Kỳ vọng | Hiện tại (trước deploy bản sửa) |
|---|---|---|---|
| P1 | Vercel `/auth/register`, gõ 5 ký tự | Console không còn `[GSI_LOGGER]` | Chưa có bản sửa trên Vercel |
| P2 | Vercel `/auth/register`, password `weak` | Hiện lỗi cạnh ô "Mật khẩu" | Chưa có bản sửa trên Vercel |
| P3 | `POST .../auth/register` password `weak` | `400` + `errors.password` + `code:"validation.failed"` | ✅ Đã đúng (xác nhận 02:51 GMT) |
| P4 | `POST .../auth/login` sai thông tin | `401` + `code:"auth.invalid_credentials"` | ✅ Đã đúng (xác nhận 02:51 GMT) |
| P5 | Đối chiếu commit Render/Vercel với commit PR | Khớp commit đã merge | Chưa deploy |

### 8.3 Kết quả verify local/dev (19/11/2026)

| Lệnh | Kết quả |
|---|---|
| `dotnet restore` / `build -c Release` / `format --verify-no-changes` | ✅ Xanh (0 warning, 0 error) |
| `dotnet test CulinaryBlog.sln --no-build -c Release` | ✅ **Failed 0, Skipped 0**, Passed **451** (446 + 5 ConcurrencySpike) |
| `npx tsc --noEmit` | ✅ Xanh |
| `npm test --if-present` | ⚠️ 108 pass / **1 fail** — `HeaderAndGoogleSignIn.test.tsx` prod guard. Đã stash code và chạy lại trên `main` gốc: **fail y hệt** ⇒ lỗi có sẵn, không phải regression |
| `npm run build` | ✅ Xanh (warning `<img>`, `DYNAMIC_SERVER_USAGE` có sẵn) |
| `docker compose ... config --quiet` (dev + staging) | ✅ Xanh |
| QA API local | ✅ `weak` → `400` + `errors.password` + `code:"validation.failed"`; `Demo-Password9!` → `201` |

> **Đính chính quan trọng**: các lần `curl.exe` đầu tiên dùng `--data "{\"email\":...}"` (escape kiểu
> bash) trong PowerShell → JSON gửi đi có dấu `\` thừa → API trả `400 request.invalid`. Gọi lại bằng
> `curl --data-binary @file` chứng minh **API production khớp `main`** (§4.3 / §8.2), nên nghi ngờ
> "deploy sai" ban đầu là sai. Hạng mục "kiểm tra lại render deploy" còn lại là xác nhận bản sửa
> frontend lên Vercel (P1–P2, P5), không phải sửa backend/production.

---

**Kết luận:** Lỗi 1 là effect dependency: callback inline từ trang đăng ký làm
`GoogleSignInButton` gọi lại `initialize()` + `prompt()` sau mỗi lần gõ phím. Lỗi 2 **không** phải
do `RegisterValidator` từ chối payload hợp lệ (đã chạy thật: `VALID: True`), mà do (a) rule
`[a-z]` chưa được hướng dẫn ở UI và (b) camelCase/PascalCase làm mất toàn bộ lỗi field — production
đã xác nhận trả `errors.password` dạng camelCase (§8.2). Cả hai đã được sửa trên branch riêng và
verify ở mức local/dev. API Render hiện **đã đúng hợp đồng**; riêng bản deploy frontend (Vercel)
phải được kiểm tra lại theo P1–P2/P5 §8.2 sau khi merge — local không thể lường trước lỗi deploy.
