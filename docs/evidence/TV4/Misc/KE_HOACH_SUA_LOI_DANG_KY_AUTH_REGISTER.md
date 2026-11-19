# Kế hoạch xử lý lỗi đăng ký (GSI lặp + 400 "Dữ liệu không hợp lệ")

> **Người thực hiện**: Nguyễn Hữu Trung Sơn (2312739 — TV4)
> **Ngày lập kế hoạch**: 19/11/2026
> **Branch làm việc**: `fix/TV4-auth-register-gsi-400` (tách từ `main` @ `3f3bcfe`)
> **Thư mục**: `docs/evidence/TV4/Misc/` — việc **ngoài kế hoạch TV4**, tách riêng để không lẫn evidence tuần
> **Báo cáo gốc**: [`docs/report/BAO_CAO_LOI_DANG_KY_GSI_VA_400.md`](../../../report/BAO_CAO_LOI_DANG_KY_GSI_VA_400.md)

---

## 1. Mục tiêu

Sửa hai lỗi trên trang `/auth/register` đã xác định nguyên nhân trong báo cáo gốc:

| Lỗi | Triệu chứng | Nguyên nhân |
|---|---|---|
| L1 | Gõ 1 ký tự → console lặp `[GSI_LOGGER] ... initialize() is called multiple times` + `FedCM get() rejects ... NotAllowedError` | Callback inline từ trang đăng ký làm effect khởi tạo GIS chạy lại sau mỗi re-render |
| L2 | Điền đủ form → `POST /auth/register` 400, UI chỉ hiện *"Dữ liệu không hợp lệ."* | Rule `[a-z]` không được label hướng dẫn + key lỗi camelCase bị frontend đọc nhầm PascalCase ⇒ mất lỗi field |

---

## 2. Phạm vi — QUAN TRỌNG

> Theo yêu cầu: **"giới hạn docker" = chỉ sửa và test ở mức local/dev**.
> Mọi lỗi có thể phát sinh sau khi deploy lên **Render (API)** / **Vercel (frontend)** đều
> **không thể lường trước ở local** ⇒ chỉ ghi nhận làm hạng mục cần kiểm tra sau deploy,
> **không** tự đoán hoặc tự sửa cấu hình production trong PR này.

| Được làm | Không làm |
|---|---|
| Sửa source frontend trên branch riêng | Đổi `render.yaml`, `docker-compose.staging.yml` hay env production |
| Verify bằng Docker local (PG/Redis/MinIO đã chạy) + 4 bước CI backend + `tsc`/`npm test`/`next build` | Khẳng định đã sửa xong lỗi trên production |
| Ghi rõ bằng chứng curl production vào báo cáo và PR body | Tự tạo tài khoản thật trên production để test |
| Cập nhật báo cáo `docs/report` với phần "cần re-check sau deploy" | Deploy/redeploy Render hay Vercel |

---

## 3. Các bước thực hiện

| # | Việc | File dự kiến | Trạng thái |
|---|---|---|---|
| 1 | Tạo branch `fix/TV4-auth-register-gsi-400` từ `main` | — | ✅ |
| 2 | Lập plan tại `docs/evidence/TV4/Misc/` | file này | ✅ |
| 3 | Lưu callback `onSuccess/onError` vào `useRef`; effect GIS chỉ phụ thuộc cấu hình; `prompt()` tối đa 1 lần/clientId | `src/frontend/src/components/GoogleSignInButton.tsx` | ✅ |
| 4 | Memoize `handleGoogleSuccess`/`handleGoogleError` ở trang đăng ký | `src/frontend/src/app/auth/register/page.tsx` | ✅ |
| 5 | Đọc lỗi validation cả camelCase lẫn PascalCase; label password thêm "1 chữ thường" | `src/frontend/src/lib/api.ts`, `src/frontend/src/app/auth/register/page.tsx` | ✅ |
| 6 | Cập nhật báo cáo: phần cảnh báo deploy Render/Vercel + trạng thái sau sửa | `docs/report/BAO_CAO_LOI_DANG_KY_GSI_VA_400.md` | ✅ |
| 7 | Verify local: 4 bước CI backend, `tsc --noEmit`, `npm test`, `npm run build`, `docker compose config`, secret scan | — | ✅ (jest: 1 fail có sẵn trên `main`, xem §7) |
| 8 | Commit → push origin → tạo PR (body có dẫn chứng Render) | — | ⬜ |

---

## 4. Kiểm tra sau deploy (ngoài phạm vi PR này)

Phải chạy **sau khi** bản sửa được deploy lên Vercel; local không kiểm chứng được.
**Đính chính:** nghi ngờ ban đầu "API Render trả generic" là do curl sai; gọi lại bằng
`--data-binary @file` (02:51 GMT) cho thấy **API Render đã khớp `main`** — xem báo cáo §4.3.

| # | Kiểm tra | Kỳ vọng | Trạng thái hiện tại |
|---|---|---|---|
| P1 | Mở frontend Vercel `/auth/register`, gõ 5 ký tự | Console **không** có `[GSI_LOGGER]` | Chưa có bản sửa trên Vercel |
| P2 | Vercel `/auth/register`, password `weak` | Hiện rõ lỗi cạnh ô "Mật khẩu" (production trả `errors.password` camelCase) | Chưa có bản sửa trên Vercel |
| P3 | `POST https://culinary-api-frvx.onrender.com/api/v1/auth/login` sai thông tin | `401` + `code:"auth.invalid_credentials"` | ✅ Đã đúng (02:51 GMT) |
| P4 | `POST .../auth/register` password `weak` | `400` + `code:"validation.failed"` + `errors.password` | ✅ Đã đúng (02:51 GMT) |
| P5 | Đối chiếu commit đang chạy trên Render/Vercel với commit PR | Phải là commit PR, không phải bản cũ | Chưa deploy |

---

## 5. Rủi ro & cách giảm thiểu

| Rủi ro | Cách xử lý |
|---|---|
| `useRef` cập nhật callback trong effect → callback tạm thời cũ trong 1 render | Chỉ đọc ref tại thời điểm event (Google callback), không dùng trong render |
| Thay đổi effect GIS có thể làm nút Google không render | Giữ nguyên điều kiện guard và `renderButton`; verify bằng test + build |
| Backend trả key lỗi dạng khác (vd. `Password` thay vì `password`) | Chuẩn hóa cả 2 chiều trong `api.ts` |
| Frontend deploy vẫn lỗi dù local xanh | Đã ghi P1/P2/P5 ở §4 và trong PR body — không tự sửa cấu hình production |

---

## 6. Tiêu chí hoàn thành

- [ ] Code sửa xong, branch đẩy lên `origin`, PR đã mở với evidence Render trong body. *(chờ push/PR — cập nhật ở bước 8)*
- [x] 4 bước backend CI xanh, **Failed = 0, Skipped = 0**.
- [x] `npx tsc --noEmit`, `npm run build` xanh; `npm test` có 1 fail có sẵn trên `main` (§7).
- [x] `docker compose -f docker-compose.dev.yml config --quiet` xanh.
- [x] Không secret/file build lọt vào commit.
- [x] Báo cáo `docs/report` ghi rõ hạng mục cần kiểm tra lại sau deploy (§8.2).

---

## 7. Kết quả verify local/dev (19/11/2026)

| Lệnh | Kết quả |
|---|---|
| `dotnet restore CulinaryBlog.sln --locked-mode` | ✅ OK |
| `dotnet build CulinaryBlog.sln --no-restore -c Release` | ✅ 0 warning, 0 error |
| `dotnet format CulinaryBlog.sln --verify-no-changes --no-restore` | ✅ OK |
| `dotnet test CulinaryBlog.sln --no-build -c Release` | ✅ **Failed 0, Skipped 0**, Passed **451** (446 `CulinaryBlog.Tests` + 5 `ConcurrencySpike`) |
| `npm ci` | ✅ 694 packages (sau khi tạm dừng dev server đang giữ file SWC; đã khởi động lại `npm run dev`) |
| `npx tsc --noEmit` | ✅ OK |
| `npm test --if-present` | ⚠️ **108 pass / 1 fail** — `HeaderAndGoogleSignIn.test.tsx` (NODE_ENV prod guard). **Đã chạy lại trên `main` gốc sau `git stash`: fail y hệt** ⇒ lỗi có sẵn, không phải regression của PR này |
| `npm run build` | ✅ Build thành công (chỉ warning `<img>`/`revalidate:0` có sẵn) |
| `docker compose -f docker-compose.dev.yml config --quiet` | ✅ OK |
| `docker compose -f docker-compose.staging.yml config --quiet` | ✅ OK |
| QA API local (`curl --data-binary @file`) | ✅ password `weak` → `400` + `errors.password` + `code:"validation.failed"`; password `Demo-Password9!` → `201` |

> **Bài học verify**: dùng `--data-binary @file` khi gọi `curl.exe` từ PowerShell. Escape kiểu
> bash (`--data "{\"email\":...}"`) gửi JSON sai → API trả `400 request.invalid`, dễ gây kết luận
> nhầm về bản deploy. Các lần curl production lúc đầu mắc lỗi này đã được **gọi lại bằng file JSON
> lúc 02:51 GMT → API Render khớp `main`** (§4.3 của báo cáo).

### 7.1 Bằng chứng production sau đính chính (19/11/2026 02:51 GMT)

| Request | Response |
|---|---|
| `POST .../auth/register` password `weak` | `400` `{"title":"Dữ liệu không hợp lệ.","errors":{"password":[...]},"code":"validation.failed"}` |
| `POST .../auth/login` sai thông tin | `401` `{"title":"Email hoặc mật khẩu không đúng.","code":"auth.invalid_credentials"}` |

→ Khẳng định lại nguyên nhân L2: production trả key `errors.password` (camelCase), đúng với
đoạn code frontend đọc `validationErrors.Password` (PascalCase) ⇒ mất lỗi field. Bản sửa
`normalizeValidationErrors()` + `errorOf()` xử lý đúng dạng này.
