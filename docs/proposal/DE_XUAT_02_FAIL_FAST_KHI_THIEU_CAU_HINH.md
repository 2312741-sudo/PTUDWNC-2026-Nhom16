# Đề xuất gỡ block B2 — Fail-fast khi thiếu cấu hình object storage (và DB)

> **Block**: [`TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md`](../evidence/TV4/Tuan03/Report/TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md) §2 B2
> **Mức**: 🔴 Cao · **Nguồn**: §6 mục 1 của [`BAO_CAO_LOI_UPLOAD_ANH_500.md`](../report/BAO_CAO_LOI_UPLOAD_ANH_500.md)
> **Cần ai quyết**: nhóm (quyết định này ảnh hưởng tới mọi thành viên)

---

## 1. Thực trạng

Khi `Minio:AccessKey`/`SecretKey` rỗng (hoặc thiếu section `Minio`):

```
dotnet run --project src/backend/CulinaryBlog.API
  -> Build thanh cong, "Now listening on: http://localhost:5080", /health = Healthy
  -> Nguoi dung bam "Tai len" -> 500 server.error
```

`MinioOptions` có `AccessKey`/`SecretKey` mặc định là **chuỗi rỗng** và **không validate**
(`MinioOptions.cs:6-7`), `builder.Services.Configure<MinioOptions>(...)` không kèm validate
(`Program.cs:113`). Tệ hơn: `/health` vẫn `Healthy` vì health check chỉ TCP probe
(`Health.cs:36-43`) ⇒ **không có tín hiệu nào** báo app đang cấu hình sai.

Hệ quả thực tế đã xảy ra: bug 500 này chỉ lộ ra khi người dùng bấm nút, và phải mở code +
đọc log Serilog mới tìm ra nguyên nhân.

## 2. Nguyên nhân

1. Options class dùng default "trông hợp lý" (`Endpoint = "localhost:9000"`, `Bucket = "culinary-blog"`)
   ⇒ **cấu hình nửa vời**: phần đúng, phần sai, không ai thấy.
2. Cơ chế validate của ASP.NET (`ValidateDataAnnotations` / `ValidateOnStart`) chưa được dùng cho `MinioOptions`.
3. `JwtSettings` **đã** có pattern validate lúc DI resolve (`Program.cs:41-46` + `JwtSettings.Validate()`)
   ⇒ có tiền lệ trong codebase, chỉ là chưa áp cho storage.

## 3. Giải pháp đề xuất

### Phương án A (khuyến nghị) — validate lúc khởi động, chỉ ngoài môi trường Testing

```csharp
// MinioOptions.cs
public sealed class MinioOptions
{
    public const string SectionName = "Minio";
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string Bucket { get; set; } = "culinary-blog";
    public bool UseSsl { get; set; }

    /// <summary>Fail-fast: thiếu cấu hình thì báo lỗi ngay lúc khởi động, không đợi tới lúc user bấm nút.</summary>
    public void Validate()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(Endpoint)) missing.Add("Minio__Endpoint");
        if (string.IsNullOrWhiteSpace(AccessKey)) missing.Add("Minio__AccessKey");
        if (string.IsNullOrWhiteSpace(SecretKey)) missing.Add("Minio__SecretKey");
        if (string.IsNullOrWhiteSpace(Bucket)) missing.Add("Minio__Bucket");
        if (missing.Count > 0)
            throw new InvalidOperationException(
                "Thieu cau hinh object storage: " + string.Join(", ", missing) +
                ". Dat bien moi truong (xem .env.example) hoac them section \"Minio\" vao appsettings.Development.json.");
    }
}
```

```csharp
// Program.cs — cạnh dòng _ = app.Services.GetRequiredService<JwtSettings>(); (Program.cs:201)
if (!builder.Environment.IsEnvironment("Testing"))
    app.Services.GetRequiredService<IOptions<MinioOptions>>().Value.Validate();
```

Bọc `Validate` trong `try/catch` + `Log.Fatal` để thông báo lỗi hiện rõ trên console thay vì stack trace.

### Phương án B — chỉ cảnh báo, không chặn khởi động

Log `Warning` ngay khi khởi động + `Warning` mỗi lần gọi storage. Không chặn ai, nhưng người không
đọc log vẫn không biết.

### Phương án C — bật/tắt bằng biến môi trường

`STORAGE_FAIL_FAST=true|false` (mặc định `true` ở Development, `false` ở Production nếu storage
được cấu hình sau). Linh hoạt nhất nhưng thêm 1 khái niệm cấu hình.

## 4. Rủi ro

| Rủi ro | Khả năng | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| Thành viên không bật được `s3` thì **không chạy được API**, kể cả việc làm auth/category | **Cao** | Giảm năng suất làm việc của cả nhóm | Chỉ bật validate ở `Development`+`Production`, **không** ở `Testing`; hoặc dùng phương án C để tắt được |
| `IOptions<MinioOptions>.Value` ở `Testing` có thể lỗi với test factory không nạp `Minio` | Trung bình | Test đỏ hàng loạt | Guard `IsEnvironment("Testing")` (như trên) + chạy full suite để chắc |
| Deploy production quên set biến ⇒ **pod crash-loop** thay vì chạy được | Trung bình | Sự cố triển khai | Thông báo lỗi phải nêu **tên biến cần set**; kiểm tra trước ở pipeline/CI |
| Thêm validate cho DB connection string cũng theo ⇒ production phải set đủ | Trung bình | Sự cố triển khai | Chỉ validate storage trong đợt này; DB đã có `throw` sẵn ở `Program.cs:56-59` |
| Báo lỗi lúc khởi động khiến người mới rối vì không biết đọc tiếng Việt | Thấp | Trải nghiệm | Thông báo liệt kê chính xác biến cần đặt + link `.env.example` |

## 5. Test case

| # | Test case | Kỳ vọng sau khi sửa |
|---|---|---|
| 1 | Chạy API ở `Development` với `appsettings.Development.json` đầy đủ (sau lần sửa này) | Khởi động bình thường |
| 2 | Chạy API với `Minio__AccessKey=""` | **Khởi động thất bại**, log nêu rõ `Minio__AccessKey` (không còn 500 lúc runtime) |
| 3 | Chạy API ở `Testing` (test factory không nạp `Minio`) | Test chạy bình thường, **không** validate |
| 4 | `dotnet test CulinaryBlog.sln -c Release` | `Skipped=0`, xanh |
| 5 | Đặt `Minio__AccessKey=wrong` (có giá trị, sai) | Khởi động được (validate chỉ bắt **rỗng**) ⇒ lỗi nằm ở block 01 |
| 6 | CI workflow chạy `dotnet test` | Xanh, không bị validate chặn |

## 6. Chuỗi lỗi liên quan (nếu có)

```
dotnet run
  └─ builder.Services.Configure<MinioOptions>(...)          (Program.cs:113) — không validate
      └─ app.Services...MinioStorageService                  (Program.cs:114) — bind 1 lần
          └─ MinioClient.WithCredentials("", "")            (MinioStorageService.cs:56)
              └─ BucketExistsAsync -> 401 -> MinioException
                  └─ ApiExceptionHandler nhánh generic -> 500 server.error
                      └─ UI: "Lỗi hệ thống. Vui lòng thử lại."
```

Sau khi áp dụng phương án A, chuỗi bị cắt ở đầu:

```
dotnet run
  └─ MinioOptions.Validate() -> InvalidOperationException("Thieu cau hinh: Minio__AccessKey, ...")
      └─ app dừng ngay, thông báo chỉ đúng biến cần đặt
```

## 7. Quyết định cần chốt

1. Chọn phương án A (chặn ở non-Testing), B (chỉ cảnh báo) hay C (bật/tắt bằng biến)?
2. Có chấp nhận việc thành viên phải bật `s3` mới chạy được API không?
3. Có áp dụng cùng cơ chế validate cho `ConnectionStrings:Database` không?
