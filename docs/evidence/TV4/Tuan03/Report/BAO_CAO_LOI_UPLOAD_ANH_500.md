# Báo cáo lỗi: `POST /api/v1/recipes/{id}/images` trả **500 server.error** — thiếu cấu hình MinIO

> **Người phát hiện & phân tích**: Nguyễn Hữu Trung Sơn (2312739 — TV4)
> **Ngày phát hiện**: 28/09/2026
> **Ngày xác minh/kết luận**: 28/09/2026
> **Mức độ**: **Blocker** — chặn toàn bộ luồng upload ảnh công thức (D1.3) ở môi trường dev local
> **Phân loại**: **Lỗi cấu hình môi trường (config)**, *không* phải lỗi logic code
> **Phạm vi**: D1.3 (API upload/PATCH/DELETE ảnh recipe) — `docs/IMAGE_CONTRACT.md`
> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026) — FR-FILE-001, FR-FILE-002, FR-RCP-008
> **Reviewer theo contract**: TV3 (Recipe) — tích hợp editor ảnh D4/C4

> ⚠️ **ĐÂY LÀ BÁO CÁO TRẠNG THÁI *TRƯỚC KHI SỬA*.** Toàn bộ mô tả bên dưới (kể cả "app không nạp
> `.env`", "phải export biến thủ công", "giá trị mặc định trong compose") ghi lại **đúng trạng thái
> ngày 28/09/2026 lúc phát hiện lỗi** và được giữ nguyên làm bằng chứng gốc — đừng dùng làm
> hướng dẫn cấu hình hiện hành.
>
> **Trạng thái sau khi sửa:** xem [`BAO_CAO_LOI_UPLOAD_ANH_500_DA_SUA.md`](./BAO_CAO_LOI_UPLOAD_ANH_500_DA_SUA.md)
> (§3.1 thêm section `Minio`, §3.1b đổi mô hình default + `.env`) và
> [`TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md`](./TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md).
> Riêng câu "app không nạp `.env`" **đã hết hiệu lực**: nhóm đã thêm `DotNetEnv` + `EnvFileLoader` —
> xem [`DE_XUAT_03_NAP_FILE_DOT_ENV.md`](./DE_XUAT_03_NAP_FILE_DOT_ENV.md) §8.

---

## 0. Tóm tắt một nén

| Hạng mục | Kết quả |
|---|---|
| Triệu chứng | Chọn file ảnh ở `http://localhost:3000/dashboard/recipes/{id}/edit?slug=...` → bấm **"Tải lên"** → `HTTP 500` + UI hiện *"Lỗi hệ thống. Vui lòng thử lại."* |
| API liên quan | `POST http://localhost:5080/api/v1/recipes/{id}/images` (`Program.cs:475-491`) |
| Mã lỗi trả về | `500` · `code: "server.error"` · `title: "Có lỗi hệ thống. Vui lòng thử lại."` |
| **Nguyên nhân gốc** | **`MinioOptions` không được cấu hình → `AccessKey = ""`, `SecretKey = ""` (chuỗi rỗng)**. Object storage trả `401 UnauthorizedAccess: "Your account is not signed up"` → `MinioException` lan ra → rơi vào nhánh generic của `ApiExceptionHandler` → 500 |
| Xác nhận | Request S3 ký tay (SigV4) trực tiếp vào `localhost:9000`: cred rỗng → **401**; `minioadmin/minioadmin` → **200 OK** |
| Lỗi code frontend? | **Không** — `recipe-editor.ts:164-177` đúng hợp đồng multipart (cố ý không set `Content-Type`) |
| Loại trừ | **Không phải** Hangfire enqueue (`Program.cs:121-142`) — `images[]` vẫn rỗng sau 500 ⇒ ghi DB chưa từng chạy |
| Cách sửa | Set 4 biến môi trường `Minio__*` rồi **restart API** (xem §5) |
| Lệch phát hiện | `/health` báo `minio: Healthy` vì health check chỉ là **TCP probe**, không xác thực credential |

---

## 1. Mô tả lỗi

### 1.1 Bước tái hiện

| # | Thao tác | Kết quả mong đợi | Kết quả thực tế |
|---|---|---|---|
| 1 | `POST /api/v1/auth/register` (tài khoản mới → tự có role `Author`) | `201` | ✅ `201` |
| 2 | `POST /api/v1/recipes` (tạo draft) | `201` | ✅ `201` |
| 3 | Mở `http://localhost:3000/dashboard/recipes/{id}/edit?slug=...` → **Bước ảnh** → chọn file PNG hợp lệ | — | ✅ file được chọn, hiển thị trước khi upload |
| 4 | Bấm nút **"Tải lên"** | `201 Created` | ❌ **`500 Internal Server Error`** |
| 5 | Quan sát Network tab | `201` | `{"title":"Có lỗi hệ thống. Vui lòng thử lại.","status":500,"code":"server.error"}` |
| 6 | `GET /api/v1/recipes/{slug}` lại | `images` không đổi | ⚠️ `images: []` — **không có dòng nào được ghi** |

> **Điểm mấu chốt ở bước 6**: ảnh không hề được ghi vào DB ⇒ lỗi xảy ra **trước** `SaveChangesAsync`, tức là ở bước ghi object lên storage — không phải ở bước enqueue job resize.

### 1.2 Response thực tế (reproduce bằng `curl`)

```
POST /api/v1/recipes/b0df1614-052d-45eb-a439-8f0c7bbf537c/images
Authorization: Bearer <JWT>
Content-Type: multipart/form-data; boundary=----WebKitFormBoundary... (do browser tự sinh)
------WebKitFormBoundary...
Content-Disposition: form-data; name="file"; filename="t.png"
Content-Type: image/png
<PNG bytes>
------WebKitFormBoundary--


HTTP/1.1 500 Internal Server Error
Content-Type: application/problem+json
Server: Kestrel

{"type":"https://tools.ietf.org/html/rfc9110#section-15.6.1",
 "title":"Có lỗi hệ thống. Vui lòng thử lại.",
 "status":500,
 "instance":"/api/v1/recipes/b0df1614-052d-45eb-a439-8f0c7bbf537c/images",
 "code":"server.error",
 "traceId":"00-e17f2abbbf6c001a6a2a991467b778b7-585ca2ecf751337a-01"}
```

---

## 2. Truy vết lời gọi (frontend → backend)

```
ImagesStep.tsx (bước "ảnh" của wizard sửa công thức)
  └─> recipe-editor.ts:164  uploadImage(id, file, altText)
        └─> recipe-editor.ts:170  fetch(`${API}/recipes/${id}/images`, { method:"POST", body: FormData })
              └─> Program.cs:475      recipes.MapPost("/{id:guid}/images", ...)
                    └─> RecipeImages.cs:98   UploadRecipeImageHandler.Handle()
                          1) GetRecipeWithImagesAsync   ✅ 200 (chứng minh: GET /recipes/{slug} trả images:[])
                          2) RecipeImageAccess.EnsureCanManage  ✅ 403 nếu sai → không phải
                          3) ImageUploadValidator.Validate     ✅ 400 nếu sai format/size → không phải
                          4) storage.UploadAsync(...)          ❌ THẤT BẠI  <-- thủ phạm
                          5) recipe.AddImage + SaveChanges     (chưa tới)
                          6) resizeQueue.EnqueueAsync          (chưa tới)
```

### 2.1 Đường lỗi chi tiết ở bước 4

```csharp
// RecipeImages.cs:113
var stored = await storage.UploadAsync(request.Content, fileName, contentType, $"recipes/{recipe.Id}", ct);
```

```csharp
// MinioStorageService.cs:54-58  — build client từ MinioOptions
_client = new MinioClient()
    .WithEndpoint(_options.Endpoint)          // "localhost:9000"  (default, ĐÚNG)
    .WithCredentials(_options.AccessKey,      // ""               (SAI — rỗng)
                     _options.SecretKey)      // ""               (SAI — rỗng)
    .WithSSL(_options.UseSsl)                 // false            (default, ĐÚNG)
    .Build();
```

```csharp
// MinioStorageService.cs:74-89  — exception KHÔNG bị nuốt (thiết kế có chủ ý)
var bucketExists = await _client.BucketExistsAsync(...);   // <-- ném MinioException
```

> Comment tại `MinioStorageService.cs:42` ghi rõ ý đồ:
> *"Không nuốt lỗi im lặng: exception MinIO lan ra để handler ánh xạ 5xx/4xx phù hợp."*
> → Vì vậy lỗi hạ tầng bị đẩy thẳng lên tầng API thay vì thành lỗi nghiệp vụ có mã rõ ràng.

Exception không khớp bất kỳ nhánh nào trong `ApiExceptionHandler.cs` (`ValidationException` / `AppException` / `DbUpdateException` / `DomainException` / `BadHttpRequestException`) nên rơi vào nhánh generic `ApiExceptionHandler.cs:29-33`:

```csharp
else
{
    logger.LogError(exception, "Request failed with {ExceptionType}: {Message}", ...);
    details = new() { Status = 500, Title = "Có lỗi hệ thống. Vui lòng thử lại.",
                      Extensions = { ["code"] = "server.error" } };
}
```

---

## 3. Nguyên nhân gốc

### 3.1 Chuỗi cấu hình bị đứt

| Bước | Kiểm tra | Kết quả |
|---|---|---|
| 1 | Section `Minio` trong `src/backend/CulinaryBlog.API/appsettings.json` | ❌ **không có** |
| 2 | Section `Minio` trong `appsettings.Development.json` | ❌ **không có** |
| 3 | File `.env` ở gốc repo (đọc được `Minio__*`) | ❌ **không tồn tại** — chỉ có `.env.example` (template, đúng thiết kế: không commit secret) |
| 4 | Biến môi trường `Minio__*` ở scope Process / User / Machine | ❌ **không có biến nào** |
| 5 | `Program.cs:29` dùng `WebApplication.CreateBuilder(args)` — có nạp `.env` không? | ❌ **không** (không có package `DotNetEnv`; chỉ đọc appsettings + env + args) |
| 6 | Lệnh chạy thực tế | `dotnet run --project src/backend/CulinaryBlog.API -- --urls http://localhost:5080` — không kèm biến môi trường nào |

⇒ `builder.Services.Configure<MinioOptions>(builder.Configuration.GetSection("Minio"))` (`Program.cs:113`) bind vào **section rỗng**, nên `MinioOptions` giữ nguyên **giá trị default trong code**:

```csharp
// src/backend/CulinaryBlog.Infrastructure/MinioOptions.cs
public sealed class MinioOptions
{
    public string Endpoint  { get; set; } = "localhost:9000";   // ✅ trùng docker-compose
    public string AccessKey { get; set; } = "";                 // ❌ RỖNG
    public string SecretKey { get; set; } = "";                 // ❌ RỖNG
    public string Bucket    { get; set; } = "culinary-blog";     // ✅ trùng docker-compose
    public bool   UseSsl    { get; set; }                       // ✅ false
}
```

Lưu ý: `Endpoint` và `Bucket` **có default khớp** nên container RustFS vẫn kết nối được ở tầng TCP — chỉ riêng **credential** là rỗng. Đây là lý do lỗi "tinh vi": mọi thứ *trông như* đã cấu hình đúng.

### 3.2 Bằng chứng quyết định — probe S3 ký tay (AWS SigV4)

Gửi request ký đúng chuẩn SigV4 trực tiếp vào `http://localhost:9000` (container `culinaryblog-s3` — image `rustfs/rustfs:1.0.0`), so sánh credential rỗng với credential đúng:

| # | Thao tác | AccessKey / SecretKey | Kết quả |
|---|---|---|---|
| A | `GET /` (ListBuckets) | `""` / `""` | ❌ **HTTP 401** — `<Code>UnauthorizedAccess</Code><Message>Your account is not signed up</Message>` |
| B | `GET /` (ListBuckets) | `minioadmin` / `minioadmin` | ✅ **HTTP 200** — trả về bucket `culinary-blog` |
| C | `PUT /culinary-blog/probe.png` | `""` / `""` | ❌ **HTTP 401** — `UnauthorizedAccess: Your account is not signed up` |
| D | `PUT /culinary-blog/probe.png` | `minioadmin` / `minioadmin` | ✅ **HTTP 200** |

Giá trị `minioadmin` / `minioadmin` lấy từ `docker-compose.dev.yml` và `.env.example` (`RUSTFS_ACCESS_KEY` / `RUSTFS_SECRET_KEY`), và xác nhận đúng qua `docker exec culinaryblog-s3 env`:

```
RUSTFS_ACCESS_KEY=minioadmin
RUSTFS_SECRET_KEY=minioadmin
```

**Kết luận**: credential rỗng bị RustFS từ chối ở **tầng xác thực** (401) — đúng loại lỗi mà `MinioStorageService` không bắt được và để lọt ra thành 500. (Object `probe.png` dùng để probe đã được xoá lại: `DELETE` → `204`.)

### 3.3 Vì sao `/health` không phát hiện ra

```csharp
// src/backend/CulinaryBlog.API/Health.cs:36-39
private readonly string _host = cfg["HealthChecks:Minio:Host"] ?? "localhost";
private readonly int _port  = int.Parse(cfg["HealthChecks:Minio:Port"] ?? "9000");
```

Health check chỉ **TCP probe** (`TcpClient.Connect`) — không đăng nhập, không xác thực credential. Kết quả thực tế lúc đang lỗi:

```json
{"status":"Healthy","checks":{...,"minio":{"status":"Healthy","description":"localhost:9000 reachable.","durationMs":9.9132}}}
```

⇒ **Không được dùng `/health` để kết luận storage cấu hình đúng.**

---

## 4. Loại trừ các giả thuyết khác

| Giả thuyết | Cách loại trừ | Kết luận |
|---|---|---|
| Lỗi ở `recipe-editor.ts:170` (thiếu/sai `Content-Type`) | `curl` với đúng boundary + đúng PNG 1×1 vẫn trả 500. Ngoài ra dòng 169 **cố ý** bỏ `Content-Type` để trình duyệt tự sinh `boundary` — đúng hợp đồng `multipart/form-data` | ❌ Loại trừ |
| Thiếu header `Authorization` / token hết hạn | `curl` dùng JWT hợp lệ vẫn 500 (401 sẽ trả `UnauthorizedError` tại `recipe-editor.ts:173`) | ❌ Loại trừ |
| Ảnh vượt 5 MiB / sai định dạng / magic bytes lệch `Content-Type` | `ImageUploadValidator.Validate` (`ImageUpload.cs:67-85`) trả `400` với mã `file.too_large` / `file.invalid_type` / `file.empty` | ❌ Loại trừ (lỗi này ra 400, không ra 500) |
| Recipe không tồn tại / không thuộc owner | `AppException(404 "recipe.not_found")` / `AppException(403 "recipe.forbidden")` tại `RecipeImages.cs:101-102`, `RecipeImageAccess.EnsureCanManage` | ❌ Loại trừ (ra 404/403) |
| `DbUpdateException` khi `SaveChangesAsync` | `ApiExceptionHandler.cs:18-23` map thành **422** `recipe.version_conflict`, không phải 500. Hơn nữa `images[]` sau lỗi vẫn rỗng ⇒ dòng DB chưa từng được tạo | ❌ Loại trừ |
| **Hangfire `BackgroundJob.Enqueue` lỗi** (`RecipeImages.cs:119`, `HangfireImageResizeQueue`) | Hợp lý về mặt triển khai (Development dùng `HangfireImageResizeQueue`, `Program.cs:142`; Testing dùng inline, `Program.cs:124`). **Nhưng bị loại trừ bằng thứ tự thực thi**: MinIO (bước 4) đã chết trước, nên `AddImage`/`SaveChanges` (bước 5) và `EnqueueAsync` (bước 6) **chưa từng chạy**. Bằng chứng: `GET /recipes/{slug}` sau lỗi trả `images: []` ⇒ dòng `RecipeImage` không tồn tại ⇒ không thể đã tới bước 6 | ❌ Loại trừ (nguyên nhân gốc là MinIO) |
| Object storage không chạy | `GET /minio/health/live` → `200`; container `culinaryblog-s3` trạng thái `Up (healthy)`; `/health` TCP probe pass | ❌ Loại trừ — chỉ sai **credential** |
| Lỗi `ResizeImageJob` (ImageSharp không decode được) | Job chạy **ngoài request** qua Hangfire; `ResizeImageJob.cs:76-81` còn cố ý bắt lỗi decode và giữ `original` (không ném ra ngoài) | ❌ Loại trừ |

---

## 5. Cách khắc phục

### 5.1 Cách nhanh — set biến môi trường rồi restart API

```powershell
# Giá trị lấy từ docker-compose.dev.yml / .env.example (RUSTFS_*)
$env:Minio__Endpoint  = "localhost:9000"
$env:Minio__Bucket    = "culinary-blog"
$env:Minio__AccessKey = "minioadmin"
$env:Minio__SecretKey = "minioadmin"
$env:Minio__UseSsl    = "false"

# BẮT BUỘC restart API — IConfiguration đọc env lúc khởi động, MinioStorageService bind 1 lần
dotnet run --project src/backend/CulinaryBlog.API -- --urls http://localhost:5080
```

### 5.2 Cách bền vững — thêm section `Minio` vào `appsettings.Development.json`

```jsonc
{
  "Minio": {
    "Endpoint":  "localhost:9000",
    "Bucket":    "culinary-blog",
    "AccessKey": "minioadmin",   // giá trị dev; KHÔNG đưa vào appsettings.json (production)
    "SecretKey": "minioadmin",
    "UseSsl":    false
  }
}
```

> ⚠️ **Lưu ý quan trọng**: `.env` hiện **không được code nạp** (thiếu `DotNetEnv`). Dù `.env.example` có liệt kê sẵn `Minio__*`, chỉ copy thành `.env` là **không có tác dụng** với `WebApplication.CreateBuilder(args)`. Muốn dùng `.env` thật thì phải thêm package `DotNetEnv` + `builder.AddDotNetEnv()` vào `Program.cs`, hoặc để shell/CI export biến thật.
> Giá trị production **không được** commit vào `appsettings.json` — theo nguyên tắc đã ghi ở `.env.example`.

### 5.3 Kiểm tra sau khi sửa

```powershell
# 1) Upload thử — kỳ vọng 201 Created
curl.exe -i -X POST "http://localhost:5080/api/v1/recipes/$RID/images" `
  -H "Authorization: Bearer $TOKEN" -F "file=@t.png;type=image/png"

# 2) Ảnh đã ghi DB chưa (quan trọng — bước 6 ở §1.1)
curl.exe -s "http://localhost:5080/api/v1/recipes/$SLUG" -H "Authorization: Bearer $TOKEN"
#    -> data.images phải có 1 phần tử (mediumUrl/thumbnailUrl sẽ null ngay, Hangfire job sau mới điền)

# 3) Job resize chạy chưa
#    GET /hangfire (Admin) -> job state = Succeeded
#    hoặc đọc log Serilog: "Resized ... -> ..._300x300.jpg (300x300)."
```

---

## 6. Đề xuất cải thiện (ngoài phạm vi fix hiện tại)

| # | Đề xuất | Lý do | Ưu tiên |
|---|---|---|---|
| 1 | **`MinioOptions` validate lúc khởi động** — throw `InvalidOptionException` nếu `AccessKey`/`SecretKey`/`Endpoint` rỗng, hoặc fallback sang `Minio__*` từ `RUSTFS_*`/`AWS_*` | Lỗi 500 mơ hồ khi start vẫn "thành công"; fail-fast sẽ ra lỗi rõ ngay khi khởi động thay vì khi user bấm nút | 🔴 Cao |
| 2 | **Bọc lỗi storage thành `AppException` có mã** — ví dụ `AppException(503, "storage.unavailable", "Dịch vụ lưu trữ ảnh tạm thời không khả dụng.")` khi MinIO lỗi 5xx/401 | Người dùng thấy thông báo có nghĩa thay vì *"lỗi hệ thống"*; đồng thời tách được lỗi hạ tầng (nên 503 + retry) khỏi lỗi nghiệp vụ | 🔴 Cao |
| 3 | **Bật auth cho `MinIOHealthCheck`** hoặc thêm 1 check "storage authenticated" (gọi `BucketExistsAsync` bằng `IObjectStorageReader`) | Health check hiện báo `Healthy` khi credential sai → gây hiểu lầm khi debug | 🟡 TB |
| 4 | **Cập nhật `docs/HUONG_DAN_CHAY_*.md` + `README`**: nêu rõ `.env` **không** được nạp, phải export biến `Minio__*` thật trước khi `dotnet run` | Nguyên nhân gốc của lỗi này là hiểu nhầm giữa "có `.env.example`" và "app đọc `.env`" | 🟡 TB |
| 5 | **Bổ sung test tích hợp** khẳng định `POST /recipes/{id}/images` **không** trả 5xx khi storage misconfig — chỉ trả 503 + mã lỗi | Khoá lại hành vi lỗi mong muốn | 🟢 Thấp |
| 6 | Hiển thị `traceId` trong toast lỗi ở UI | Giúp tra nhanh trong log Serilog (hiện chỉ có trong response, UI không hiện) | 🟢 Thấp |

---

## 7. Phụ trách & phối hợp

| Hạng mục | Người phụ trách | Ghi chú |
|---|---|---|
| API upload/PATCH/DELETE ảnh (D1.3) · `RecipeImages.cs` · `MinioStorageService.cs` · `IMAGE_CONTRACT.md` · `ResizeImageJob.cs` | **TV4 — Nguyễn Hữu Trung Sơn (2312739)** | `docs/IMAGE_CONTRACT.md:1,4` ghi rõ *"Tuần 2 (TV4…)"* / *"Phụ trách: Nguyễn Hữu Trung Sơn (2312739 — TV4)"*; `Program.cs:474` `// D1.3 (TV4)`; git history 100% `Nguyen Huu Trung Son <2312739@dlu.edu.vn>` |
| UI wizard bước ảnh · `recipe-editor.ts:145-187` · `ImagesStep.tsx` | **TV3 — Huỳnh Quốc Trung (2312786)** | Viết theo commit `15be516` (`feat(C4)`) — vai trò *consumer* tích hợp API của TV4. Bàn giao có chủ ý, ghi tại `IMAGE_CONTRACT.md:5` (*"Reviewer: TV3"*) |
| MinIO → RustFS đổi image + CI storage (28/09) | **TV4** | `docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md`; commit `cd72b27`, `d78e25c` |
| Hạ tầng dev (`docker-compose.dev.yml`, `.env.example`, `HUONG_DAN_CHAY_*.md`) | **TV4** (có review từ TV1) | `.env.example` đã liệt kê `Minio__*` nhưng thiếu bước "app không đọc `.env`" |

> **Kết luận về lỗi**: đây là **lỗi cấu hình môi trường**, thuộc phạm vi TV4. Không có thay đổi code nào của TV3 (frontend) bị nghi vấn — `recipe-editor.ts:164-177` hoạt động đúng hợp đồng. Sửa `appsettings`/biến môi trường nên trao đổi với Sơn; nếu chọn hướng nâng thành `503` + mã lỗi như §6 mục 2 thì đó là thay đổi **contract lỗi** → cần thống nhất với nhóm.

---

## 8. Tài liệu tham chiếu

| File | Nội dung liên quan |
|---|---|
| `src/backend/CulinaryBlog.API/Program.cs:475-491` | Ánh xạ `POST /api/v1/recipes/{id}/images` + `DisableAntiforgery()` + `RequireAuthorization()` |
| `src/backend/CulinaryBlog.API/Program.cs:113-124, 142` | Bind `MinioOptions`; chọn `Inline` vs `Hangfire` resize queue |
| `src/backend/CulinaryBlog.Infrastructure/MinioOptions.cs:3-10` | Default `AccessKey`/`SecretKey` **rỗng** — nguồn gốc lỗi |
| `src/backend/CulinaryBlog.Infrastructure/MinioStorageService.cs:42, 54-58, 74-89` | Không nuốt lỗi; build client; `BucketExistsAsync` ném exception |
| `src/backend/CulinaryBlog.Application/RecipeImages.cs:98-122` | `UploadRecipeImageHandler` — thứ tự bước 1→6 |
| `src/backend/CulinaryBlog.Application/ImageUpload.cs:65-85` | Validator trả **400** (không phải 500) cho size/format |
| `src/backend/CulinaryBlog.API/ApiExceptionHandler.cs:29-33` | Nhánh generic sinh `500 server.error` |
| `src/backend/CulinaryBlog.API/Health.cs:36-39` | `MinIOHealthCheck` chỉ TCP probe |
| `src/frontend/src/lib/recipe-editor.ts:164-177` | `uploadImage` — multipart đúng, **không** phải thủ phạm |
| `docs/IMAGE_CONTRACT.md` | Hợp đồng D1.3 D27; ghi rõ phụ trách TV4 |
| `docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md` | Quyết định dùng `rustfs/rustfs:1.0.0` |
| `.env.example` | Liệt kê `Minio__Endpoint/Bucket/AccessKey/SecretKey` + `RUSTFS_*` |
| `docker-compose.dev.yml` | Khai báo `RUSTFS_ACCESS_KEY/SECRET_KEY=minioadmin` |

---

## 9. Nhật ký xử lý

| Thời điểm | Hành động | Kết quả |
|---|---|---|
| 28/09/2026 | Tái hiện lỗi qua UI (`/dashboard/recipes/{id}/edit` → "Tải lên") | 500 `server.error` |
| 28/09/2026 | Tái hiện lại bằng `curl` (register → create recipe → upload PNG 1×1 hợp lệ) | 500 `server.error` — xác nhận không phải lỗi UI |
| 28/09/2026 | Kiểm tra `appsettings*.json`, `.env`, biến `Minio__*` (Process/User/Machine) | Không có nguồn cấu hình nào ⇒ về default |
| 28/09/2026 | Đọc chuỗi gọi: `recipe-editor.ts` → `Program.cs:475` → `RecipeImages.cs:98` | Xác định điểm gãy tại `storage.UploadAsync` (bước 4) |
| 28/09/2026 | Probe SigV4 trực tiếp vào `localhost:9000` (ListBuckets + PutObject) | Cred rỗng → **401**; `minioadmin` → **200** ⇒ xác nhận nguyên nhân |
| 28/09/2026 | Kiểm tra `images[]` qua `GET /recipes/{slug}` sau lỗi | `[]` ⇒ ghi DB chưa chạy ⇒ **loại trừ** Hangfire/DB |
| 28/09/2026 | Đối chiếu credential qua `docker exec culinaryblog-s3 env` | `RUSTFS_ACCESS_KEY/SECRET_KEY=minioadmin` |
| 28/09/2026 | Dọn object probe `culinary-blog/probe.png` | `DELETE` → 204 |
| 28/09/2026 | Ghi báo cáo | Tài liệu này |

---

*Báo cáo lỗi cấu hình môi trường — D1.3 Recipe Images (TV4). Tài liệu dùng để bàn giao và tra cứu; không phải blocker đối với code đã merge.*
