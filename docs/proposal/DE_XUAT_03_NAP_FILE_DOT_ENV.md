# Đề xuất gỡ block B3 — App không nạp `.env` dù `.env.example` liệt kê đầy đủ biến

> **Block**: [`TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md`](../evidence/TV4/Tuan03/Report/TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md) §2 B3
> **Mức**: 🟡 Trung bình · **Nguồn**: §5.2 và §6 mục 4 của [`BAO_CAO_LOI_UPLOAD_ANH_500.md`](../report/BAO_CAO_LOI_UPLOAD_ANH_500.md)
> **Cần ai quyết**: nhóm (thêm dependency mới hay chỉ sửa tài liệu)

---

## 1. Thực trạng

| Mắt xích | Thực tế |
|---|---|
| `.env.example` | Liệt kê `Minio__Endpoint/AccessKey/SecretKey`, `ConnectionStrings__Database`, `HealthChecks__*`… |
| `.env` thật | Không tồn tại ở máy TV4 (đúng thiết kế: không commit secret) |
| `Program.cs:29` | `WebApplication.CreateBuilder(args)` — chỉ đọc `appsettings*.json` + biến môi trường + args |
| Package `DotNetEnv` | **Không có** trong `CulinaryBlog.API.csproj` |

⇒ Copy `.env.example` → `.env` rồi điền đầy đủ vẫn **không có tác dụng gì**. Đây chính là
hiểu lầm đã dẫn tới bug 500: tưởng "đã cấu hình" vì có file, thực tế `MinioOptions` rỗng.

Sau lần sửa này, `appsettings.Development.json` đã chứa đủ credential dev ⇒ máy mới chạy được
ngay mà không cần `.env`. Nhưng `.env.example` vẫn **gây hiểu nhầm** và vẫn là bẫy cho người đọc.

## 2. Nguyên nhân

1. `.env.example` được viết theo thói quen phổ biến của Node, nhưng dự án là .NET — nơi biến môi
   trường/launchSettings mới là cách chuẩn.
2. Dòng 1 của `.env.example` **đã** ghi *"dotnet không tự nạp file này"* ⇒ ý thức đã có, nhưng nằm ở
   dòng đầu nên dễ bỏ qua; phần thân file vẫn trông như bảng biến cần điền.

## 3. Giải pháp đề xuất

### Phương án A (khuyến nghị) — **không** thêm dependency, sửa tài liệu cho đúng

- `.env.example`: đổi câu chữ đầu file thành cảnh báo nổi bật + bỏ nhóm `Minio__*`/`ConnectionStrings__Database`
  (vì đã nằm trong `appsettings.Development.json`) hoặc ghi rõ "chỉ dùng khi chạy bằng Docker/CI".
- `README` + `docs/HUONG_DAN_CHAY_TV4.md`: thêm một dòng "PowerShell phải set `$env:...` trước khi
  `dotnet run`; `.env` **không** được nạp".
- Chi phí: 10 phút, không rủi ro.

### Phương án B — nạp thật `.env` bằng `DotNetEnv`

```csharp
// Program.cs, ngay sau WebApplication.CreateBuilder(args)
if (File.Exists(".env")) DotNetEnv.Load();
```

| Ưu | Nhược |
|---|---|
| Đúng trực giác của người đọc, copy file là chạy | Thêm package vào mọi project ⇒ cần `dotnet format`/lock file cập nhật |
| Ít lệnh phải nhớ khi debug | `.env` có thể bị nạp nhầm trong CI ⇒ **rủi ro bảo mật** (bật/tắt theo môi trường phải cẩn thận) |
| | Thêm 1 cách cấu hình ⇒ 2 nguồn sự thật (appsettings vs .env) dễ lệch nhau — chính là bug vừa gặp |

### Phương án C — bỏ hẳn `.env.example`

Gộp mọi giá trị dev vào `appsettings.Development.json` (đã làm ở lần sửa này) và xoá `.env.example`,
thay bằng bảng biến môi trường trong tài liệu. Ít file nhất, nhưng CI vẫn cần `.env`-style config
(CI đang truyền biến trực tiếp trong workflow, không đọc file) nên không mất gì.

## 4. Rủi ro

| Rủi ro | Khả năng | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| Chọn B ⇒ `.env` local lọt vào môi trường CI/production | Trung bình | Rò rỉ secret | Chỉ nạp khi `IsDevelopment()`; `.gitignore` đã phải có `.env` (kiểm tra lại) |
| Chọn B ⇒ phải `dotnet restore` lại, có thể phát sinh khác biệt `dotnet format`/lock file | Thấp | CI đỏ một lần | Chạy `dotnet format --verify-no-changes` sau khi thêm |
| Chọn A/C ⇒ người mới vẫn phải set biến thủ công khi cần override | Thấp | Nhỏ | Có sẵn bảng `$env:` trong `HUONG_DAN_CHAY_TV4.md` |
| Không làm gì ⇒ bẫy "đã cấu hình nhưng không có tác dụng" còn lại | **Cao** | Tái phát bug 500 | Tối thiểu phải chọn A |

## 5. Test case

| # | Test case | Kỳ vọng |
|---|---|---|
| 1 | Tạo `.env` với `Minio__AccessKey=xxx` rồi `dotnet run` (hiện tại) | **Không** có tác dụng (xác nhận lại để tránh tưởng tượng) |
| 2 | Đọc `.env.example` như một người mới | Không còn câu dẫn dắt sai (kiểm tra nội dung file sau khi sửa) |
| 3 | Sau khi đổi `docker-compose.dev.yml` (VD đổi mật khẩu DB) | `DevConfigParityTests` **bắt được** lệch (đã có) |
| 4 | Nếu chọn B: CI chạy `dotnet test` | Xanh; `TEST_DATABASE`/`MINIO_*` từ CI vẫn ưu tiên hơn `.env` |
| 5 | Nếu chọn B: build + `dotnet format --verify-no-changes` | Không sinh khác biệt |
| 6 | Thành viên mới clone repo, làm đúng hướng dẫn | `POST /recipes/{id}/images` trả `201` (đã xác nhận) |

## 6. Chuỗi lỗi liên quan (nếu có)

```
Nguoi dung: cp .env.example .env ; dien Minio__AccessKey=minioadmin
  └─ WebApplication.CreateBuilder(args)          (Program.cs:29) — bo qua .env
      └─ IConfiguration: appsettings + ENV + args — khong co ENV
          └─ Configure<MinioOptions>(section rong) (Program.cs:113)
              └─ AccessKey = "" -> 401 -> 500 server.error
```

## 7. Quyết định cần chốt

1. ~~Chọn A (sửa tài liệu), B (thêm `DotNetEnv`) hay C (bỏ `.env.example`)?~~ → **Đã chốt: B**
2. ~~Nếu chọn B: có chấp nhận thêm dependency + rủi ro `.env` bị nạp nhầm không?~~ → **Đã chốt: có, kèm 2 chốt chặn an toàn** (bỏ qua khi `Production`, không ghi đè biến đã tồn tại)
3. ~~`Minio__*` trong `.env.example` nên giữ hay bỏ?~~ → **Đã chốt: giữ**, viết lại thành mô hình default/override

---

## 8. TRẠNG THÁI: ĐÃ TRIỂN KHAI (Phương án B) — 28/09/2026

Team đã chốt **Phương án B**. Mục 1–6 ở trên giữ nguyên làm **khung phân tích gốc**;
phần này ghi nhận thay đổi đã thực sự nằm trong nhánh `2312739_NHTSon_D3-D4-D5-D6`.

### 8.1 Code đã thêm/sửa

| File | Thay đổi |
|---|---|
| `src/backend/CulinaryBlog.API/CulinaryBlog.API.csproj` | Thêm `DotNetEnv` **3.2.0** |
| `src/backend/CulinaryBlog.API/EnvFileLoader.cs` | **File mới** — loader `.env` (xem 8.2) |
| `src/backend/CulinaryBlog.API/Program.cs` | `EnvFileLoader.Load();` **trước** `WebApplication.CreateBuilder(args)` (dòng 31) |
| `tests/CulinaryBlog.Tests/AuthTests.cs`, `MinioE2ETests.cs` | Gọi loader / đọc qua `EnvFileLoader.Get`; fallback `Password=postgres` |
| `tests/concurrency-spike/SpikeDbFixture.cs` | Gọi loader; fallback `Password=postgres` |
| `tests/concurrency-spike/ConcurrencySpike.csproj` | Thêm `ProjectReference` tới API để dùng chung loader |
| `src/backend/CulinaryBlog.API/appsettings.Development.json` | Default DB → `Password=postgres` |
| `docker-compose.dev.yml` | `${POSTGRES_PASSWORD:-postgres}` |
| `.env.example` | Viết lại thành template **default/override** |
| `.gitignore` + `.dockerignore` | Chặn `.env` (`.dockerignore` chặn luôn lọt vào image) |

### 8.2 `EnvFileLoader` — 2 chốt chặn an toàn đã nêu ở mục 7.2

```csharp
// Env.Load(..., new LoadOptions(setEnvVars: true, clobberExistingVars: false, onlyExactPath: true))
```

| Chốt chặn | Cách thực hiện | Bằng chứng |
|---|---|---|
| **Bỏ qua khi Production** | `Load()` return sớm nếu `ASPNETCORE_ENVIRONMENT=Production` | Log `skip .env in Production` |
| **Không ghi đè biến đã có** | `clobberExistingVars: false` → biến CI/shell luôn thắng `.env` | Test L6b trong `docs/HUONG_DAN_CHAY_TV4.md` |
| Idempotent | `Interlocked.Exchange` chặn nạp 2 lần trong 1 tiến trình | `DevConfigParityTests` + test suite xanh |
| Tìm đúng file | Dò từ `Directory.GetCurrentDirectory()` rồi `AppContext.BaseDirectory` lên thư mục cha, `onlyExactPath: true` | Chạy được cả từ `dotnet run --project src/backend/...` |

### 8.3 Mô hình default/override sau khi sửa

| Tầng | Ở đâu | Giá trị | Commit? |
|---|---|---|:---:|
| **Default** | `appsettings.json`, `appsettings.Development.json`, `docker-compose.dev.yml`, fallback của test | `Password=postgres` | ✅ |
| **Thật của máy** | `.env` ở thư mục gốc (TV4: volume cũ) | `Password=admin123` | ❌ gitignored |
| **Mẫu** | `.env.example` | `Password=postgres` + hướng dẫn override | ✅ |

> ⚠️ Volume `culinaryblog_pg_data` của TV4 đã init với `admin123`; đổi `POSTGRES_PASSWORD` **không**
> đổi được password trong volume. Xem `docs/HUONG_DAN_CHAY_TV4.md` mục 2 (lỗi B) và mục 5.

### 8.4 Kết quả kiểm chứng (mục 5 — đã chạy thật)

| # | Test case | Kỳ vọng | Kết quả |
|---|---|---|---|
| 1 | Có `.env` → `dotnet run` API | API dùng DB/credential trong `.env` | ✅ `/health/ready` = `200`, DB `Healthy` |
| 1' | **Tạm đổi tên `.env`** → chạy lại API | Không được đọc `.env` ⇒ dùng default `postgres` | ✅ `28P01 password authentication failed for user "postgres"` |
| 1'' | Khôi phục `.env` | Health trở lại bình thường | ✅ `200` |
| 2 | Đọc `.env.example` như mô hình default/override | Không còn hướng dẫn sai | ✅ Đã viết lại |
| 3 | Đổi `docker-compose.dev.yml` lệch default | `DevConfigParityTests` **bắt được** | ✅ Negative control → `1 FAIL`; trả lại default → `3/3 PASS` |
| 4 | `dotnet test` với biến CI/shell đã set | Biến môi trường thắng `.env` | ✅ theo `clobberExistingVars: false` |
| 5 | `dotnet build` + `dotnet format --verify-no-changes` | Không sinh khác biệt | ✅ lock file đã cập nhật, format sạch |
| 6 | Thành viên mới clone repo, copy `.env.example` → `.env`, làm hướng dẫn | `POST /recipes/{id}/images` trả `201` | ✅ (xem `TEST_CASE_TICH_HOP_FE_BE.md`) |

> Test 1/1'/1'' là bằng chứng **trực tiếp** rằng loader có tác dụng: đổi tên đúng một file `.env`
> làm app đổi hành vi, và lỗi `28P01` chỉ xảy ra khi app rơi về default `postgres` trong khi volume
> vẫn là `admin123`.

### 8.5 Tài liệu đã đồng bộ

`README.md` §5, `docs/HUONG_DAN_CHAY_TV4.md` (mục 0, 5, 6, 7, 8, 11 L6/L6b, 12),
`docs/HUONG_DAN_TEST_APP.md`, `docs/GOOGLE_AUTH_CONTRACT.md`, comment trong chính file `.env`.

### 8.6 Còn lại (không thuộc B3)

- Frontend **vẫn** phải có `src/frontend/.env.local` riêng — Next.js không đọc `.env` ở thư mục gốc.
- Nhánh lab `practice/TV4/L4` chưa có loader ⇒ vẫn set `$env:` thủ công (đã ghi chú trong hướng dẫn mục 9).
