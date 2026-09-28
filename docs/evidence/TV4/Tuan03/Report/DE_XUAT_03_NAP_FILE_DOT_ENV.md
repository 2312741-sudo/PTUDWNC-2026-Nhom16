# Đề xuất gỡ block B3 — App không nạp `.env` dù `.env.example` liệt kê đầy đủ biến

> **Block**: [`TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md`](./TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md) §2 B3
> **Mức**: 🟡 Trung bình · **Nguồn**: §5.2 và §6 mục 4 của [`BAO_CAO_LOI_UPLOAD_ANH_500.md`](./BAO_CAO_LOI_UPLOAD_ANH_500.md)
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

1. Chọn A (sửa tài liệu), B (thêm `DotNetEnv`) hay C (bỏ `.env.example`)?
2. Nếu chọn B: có chấp nhận thêm dependency + rủi ro `.env` bị nạp nhầm không?
3. `Minio__*` trong `.env.example` nên giữ (để override khi chạy Docker/CI) hay bỏ (vì đã có trong appsettings dev)?
