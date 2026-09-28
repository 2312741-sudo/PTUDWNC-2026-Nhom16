# Báo cáo sửa lỗi: `POST /api/v1/recipes/{id}/images` trả **500 server.error**

> **Báo cáo gốc**: [`BAO_CAO_LOI_UPLOAD_ANH_500.md`](./BAO_CAO_LOI_UPLOAD_ANH_500.md) (28/09/2026)
> **Người thực hiện**: Nguyễn Hữu Trung Sơn (2312739 — TV4)
> **Ngày sửa**: 28/09/2026
> **Nhánh**: `2312739_NHTSon_D3-D4-D5-D6`
> **Trạng thái**: **ĐÃ SỬA + ĐÃ THÊM TEST CHỐNG TÁI DIỄN** — 6 phần cải thiện còn lại thành block, xem [`TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md`](./TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md)

---

## 1. Kết luận 1 dòng

Báo cáo gốc chẩn đoán **đúng nguyên nhân gốc** (thiếu credential object storage). Bản sửa này bổ sung
**một nguyên nhân thứ hai cùng lớp** mà báo cáo gốc chưa nêu (mật khẩu PostgreSQL sai trong
`appsettings.Development.json`), và khóa lại cả lớp bug này bằng test parity.

---

## 2. Xác minh lại trên `main` (trước khi sửa)

Tái hiện đúng như báo cáo gốc, chạy API **không** truyền biến môi trường nào
(`dotnet run --project src/backend/CulinaryBlog.API` → chỉ đọc `appsettings*.json`):

```
POST /api/v1/recipes/{id}/images   ->  500
{"title":"Có lỗi hệ thống. Vui lòng thử lại.","status":500,"code":"server.error"}
```

Nguyên nhân: `appsettings.Development.json` **không có section `Minio`** ⇒ `MinioOptions`
giữ default `AccessKey = ""`, `SecretKey = ""` (`src/backend/CulinaryBlog.Infrastructure/MinioOptions.cs:6-7`)
⇒ RustFS trả `401 UnauthorizedAccess` ⇒ `MinioException` không khớp nhánh nào trong
`ApiExceptionHandler.cs:12-32` ⇒ rơi vào nhánh generic 500.

### 2.1 Phát hiện thêm: cùng lớp bug, khác biến — mật khẩu PostgreSQL

`appsettings.Development.json` cũng khai `Password=postgres`, trong khi `docker-compose.dev.yml:20`
dùng `${POSTGRES_PASSWORD:-admin123}`. Hai file mâu thuẫn nhau ⇒ máy mới clone repo chạy
`dotnet run` sẽ gặp `28P01` ở mọi endpoint cần DB (register/login/recipe), **trước cả** khi tới
bước upload ảnh. Đây là lý do báo cáo gốc buộc phải export `ConnectionStrings__Database` mới chạy
được — tức là "cấu hình chuẩn của repo" và "cấu hình app thật" không khớp.

### 2.2 Vì sao test backend vẫn xanh (điểm cốt lõi của câu hỏi "test riêng thì ổn")

| Tầng | Nguồn credential | Vì sao không bắt được bug |
|---|---|---|
| Test `AuthTests`/`Recipe*Tests` | `ApiFactoryWithMinio` nạp in-memory `Minio:AccessKey=minioadmin` (`tests/CulinaryBlog.Tests/MinioE2ETests.cs:44-48`) và `ConnectionStrings:Database` mặc định `admin123` (`AuthTests.cs:36`) | Test **không** đọc `appsettings.Development.json` ⇒ cấu hình dev lệch hoàn toàn mà test vẫn xanh |
| Test storage (`MinioE2ETests`…) | Tự nạp credential, và nếu storage down thì **`return` (skip im lặng)** | Lỗi cấu hình ≠ storage down ⇒ không vào nhánh skip |
| FE | `NEXT_PUBLIC_API_URL` mặc định `http://localhost:5080/api/v1`, upload đúng multipart | Không liên quan cấu hình storage |

⇒ Đây là mẫu bug **chỉ lộ ra khi ghép FE + BE + hạ tầng thật**: mỗi tầng tự test xanh vì tự
nạp cấu hình của riêng nó, không ai kiểm tra cấu hình mà app thật sẽ đọc.

---

## 3. Cách sửa

### 3.1 `src/backend/CulinaryBlog.API/appsettings.Development.json` (file dev-only)

```diff
   "ConnectionStrings": {
-    "Database": "Host=localhost;Port=5432;Database=culinary_blog;Username=postgres;Password=postgres"
+    "Database": "Host=localhost;Port=5432;Database=culinary_blog;Username=postgres;Password=admin123"
   },
+  "Minio": {
+    "Endpoint": "localhost:9000",
+    "Bucket": "culinary-blog",
+    "AccessKey": "minioadmin",
+    "SecretKey": "minioadmin",
+    "UseSsl": false
+  },
```

Giá trị lấy đúng từ `docker-compose.dev.yml:48-49` (`RUSTFS_ACCESS_KEY`/`RUSTFS_SECRET_KEY`) và
`.env.example:7` (mật khẩu DB). Đây là **cùng bộ credential dev đã nằm sẵn trong repo** ở
`docker-compose.dev.yml` — không thêm bí mật mới, và **không** đụng `appsettings.json` (file này
đọc ở production nên không được đặt credential dev vào).

Sau khi sửa, `dotnet run` trên máy mới hoạt động **không cần** export biến môi trường nào.

### 3.2 `tests/CulinaryBlog.Tests/DevConfigParityTests.cs` (test chống tái diễn)

Ba test khoá parity giữa `appsettings.Development.json` và `docker-compose.dev.yml`:

| Test | Chặn loại lệch nào |
|---|---|
| `Development_appsettings_provides_non_empty_storage_credentials` | Quay lại đúng bug 500: `Minio:AccessKey/SecretKey` bị xoá hoặc để rỗng |
| `Development_storage_credentials_match_docker_compose` | Đổi `RUSTFS_*` trong compose mà quên sửa appsettings (và ngược lại) |
| `Development_database_password_matches_docker_compose` | Lệch `POSTGRES_PASSWORD` giữa compose và appsettings (bug 2.1) |

Test đọc file trực tiếp, **không cần** database/object storage ⇒ chạy được ở mọi máy và trong CI.

---

## 4. Xác minh

### 4.1 Test bắt được bug (negative control)

Gỡ tạm section `Minio` khỏi `appsettings.Development.json` rồi chạy lại:

```
Failed  DevConfigParityTests.Development_appsettings_provides_non_empty_storage_credentials
Failed  DevConfigParityTests.Development_storage_credentials_match_docker_compose
Failed!  - Failed: 2, Passed: 1
```

Khôi phục fix → `Passed! - Failed: 0, Passed: 3`.

### 4.2 Flow thật qua HTTP (41 test case, xem `TEST_CASE_TICH_HOP_FE_BE.md`)

API chạy **không set biến môi trường nào**:

| Nhóm | Kết quả |
|---|---|
| A. Auth (6) | 6/6 PASS |
| B. Category (3) | 3/3 PASS |
| C. Recipe + ingredient + step (7) | 7/7 PASS |
| D. **Ảnh (7)** | **7/7 PASS — trước đây `D01` = 500** |
| E. Publish + discovery (8) | 8/8 PASS |
| F. Update + concurrency + soft delete (5) | 5/5 PASS |
| G. Token lifecycle (3) | 3/3 PASS |
| H. Health + docs (2) | 2/2 PASS |
| **Tổng** | **41/41 PASS, 0 FAIL** |

### 4.3 Job resize ảnh (D23 D2) chạy đúng ngoài request

```
upload -> 201  key=recipes/{id}/{uuid}.png
mediumUrl ngay sau upload = []            (job Hangfire chưa xong — đúng thiết kế)
sau 25s: mediumUrl = recipes/{id}/{uuid}_800x600.png
        thumbUrl  = recipes/{id}/{uuid}_300x300.png
GET /api/v1/resources/images/{mediumUrl} -> 200
GET /api/v1/resources/images/{thumbUrl}  -> 200
```

### 4.4 Hồi quy

```
dotnet build CulinaryBlog.sln -c Release   -> Build succeeded, 0 Warning, 0 Error
dotnet format --verify-no-changes          -> không đổi (sạch)
dotnet test CulinaryBlog.sln -c Release    -> 157/157 pass, Skipped=0   (154 cũ + 3 mới)
                                               5/5   pass, Skipped=0   (ConcurrencySpike)
```

> `157` = `154` test cũ + `3` test `DevConfigParityTests` mới. Không sửa test cũ, không xoá test nào.

---

## 5. Đồng thời sửa 1 chỗ hướng dẫn sai

`docs/HUONG_DAN_CHAY_TV4.md` (L9 và bảng biến môi trường) bảo *"để `NEXT_PUBLIC_MEDIA_URL`
**trống** để dùng proxy"*. Thực tế `mediaUrl()` trong `src/frontend/src/lib/recipe-editor.ts:157-161`
**trả `null`** khi biến trống, và `ImagesStep.tsx:52-57` hiện ô *"Chưa cấu hình
NEXT_PUBLIC_MEDIA_URL"* — tức **không** hiện ảnh. Đã sửa hướng dẫn thành
`NEXT_PUBLIC_MEDIA_URL=http://localhost:5080/api/v1/resources/images` và ghi rõ giới hạn xem
ảnh Draft (block 05).

---

## 6. Phần còn lại — đã tách thành block

Sáu hạng mục cải thiện còn lại (kể cả 4/6 mục trong §6 của báo cáo gốc) **không tự sửa** vì đụng
quyết định nhóm/contract. Xem [`TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md`](./TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md).

## 7. Tài liệu tham chiếu

| File | Vai trò |
|---|---|
| `src/backend/CulinaryBlog.API/appsettings.Development.json` | file sửa (cấu hình dev) |
| `tests/CulinaryBlog.Tests/DevConfigParityTests.cs` | test chống tái diễn |
| `docker-compose.dev.yml:20,48-49` | nguồn sự thật cho credential dev |
| `src/backend/CulinaryBlog.Infrastructure/MinioOptions.cs:6-7` | default rỗng = gốc rễ |
| `src/backend/CulinaryBlog.API/ApiExceptionHandler.cs:29-32` | nhánh generic sinh 500 |
| `docs/HUONG_DAN_CHAY_TV4.md` | hướng dẫn TV4 (đã sửa L9) |
| `TEST_CASE_TICH_HOP_FE_BE.md` | bộ test case dùng để tái hiện & soát |
