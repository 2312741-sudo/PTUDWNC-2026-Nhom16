# Lab L4 (TV4) — Media & Jobs

Console app độc lập để **quan sát bằng chứng thật** cho các yêu cầu L4: upload/xoá theo 4 định dạng
MIME, resize 2 kích thước, gửi mail qua SMTP, sinh sitemap XML, và 4 kiểu job Hangfire.

Lab **không thêm tính năng mới vào sản phẩm**. Nó tham chiếu `CulinaryBlog.Application` +
`CulinaryBlog.Infrastructure` để dùng đúng đường code thật (`MinioStorageService`, `RecipeImageKeys`,
`AuthDbContext`) — điểm cần chứng minh là ranh giới interface của N4 được tôn trọng trong môi trường lab.

## Yêu cầu môi trường

| Hạng mục | Cần có |
|---|---|
| PostgreSQL | Có sẵn từ N1–N4; lab tự tạo database riêng `culinary_lab` |
| MinIO | Có sẵn từ N2 (`127.0.0.1:9000`, bucket `culinary-blog`) |
| Mailhog | Chưa có sẵn — phải bật khi chạy phase `email`/`jobs` |

```bash
docker run -d --name lab-mailhog -p 1025:1025 -p 8025:8025 mailhog/mailhog:v1.0.1
```

## Biến môi trường

Không có secret nào được hard-code trong mã nguồn; tất cả đọc từ biến môi trường của máy dev.

| Biến | Bắt buộc | Mặc định / ghi chú |
|---|---|---|
| `TEST_DATABASE` | Có (khi `LAB_POSTGRES` chưa đặt) | Dùng cho cả DB lab và DB ứng dụng (chỉ đọc) |
| `LAB_POSTGRES` | Không | Connection string maintenance DB, dùng để `CREATE DATABASE culinary_lab` |
| `LAB_DB_NAME` | Không | `culinary_lab` |
| `Minio__Endpoint` | Không | `127.0.0.1:9000` |
| `Minio__AccessKey` / `Minio__SecretKey` | Có | Không ghi vào log/evidence |
| `Minio__Bucket` | Không | `culinary-blog` |
| `LAB_SMTP_HOST` / `LAB_SMTP_PORT` | Không | `127.0.0.1` / `1025` |
| `LAB_SMTP_API` | Không | `http://127.0.0.1:8025` (Mailhog API) |
| `LAB_SMTP_FROM` | Không | `tv4-lab@culinary.local` |

## Chạy

```bash
# 4 phase yêu cầu, chạy tuần tự, kết thúc bằng tổng kết
dotnet run --project practice/TV4/L4 -- all

# chạy lẻ từng phase
dotnet run --project practice/TV4/L4 -- media
dotnet run --project practice/TV4/L4 -- email
dotnet run --project practice/TV4/L4 -- xml
dotnet run --project practice/TV4/L4 -- jobs

# phase chẩn đoán (KHÔNG tính vào 4 phase yêu cầu)
dotnet run --project practice/TV4/L4 -- db      # dump trạng thái bảng Hangfire -> out/lab_hangfire_db.txt
dotnet run --project practice/TV4/L4 -- purge   # xoá job Enqueued/Scheduled còn sót trong DB lab
```

Mã thoát `0` = PASS, `1` = FAIL (tiện dùng trong CI hoặc chạy tay).
Log của mỗi lần chạy nằm ở `out/lab-<RunId>.log`; output sinh ra nằm ở `out/` (đã được `.gitignore`).

## 4 phase yêu cầu

| Phase | Nội dung | Kết quả kỳ vọng |
|---|---|---|
| `media` | Validate theo **nội dung file** (magic bytes) cho JPEG/PNG/WebP/AVIF, upload + đọc lại, resize 2 size, idempotent, xoá original + 2 biến thể | 25/25 |
| `email` | MailKit gửi 2 mail (plain + HTML) qua SMTP Mailhog, đối chiếu số mail và subject qua Mailhog API | 3/3 |
| `xml` | Sinh `sitemap.xml` chỉ từ recipe `Published`, parse lại để kiểm tra XML hợp lệ | 3/3 |
| `jobs` | Fire-and-forget, delayed + restart worker, retry, recurring — tất cả trên Hangfire/PostgreSQL | 8/8 |

### Ghi chú trung thực về fixture AVIF

Fixture `sample.avif` là file ISOBMFF hợp lệ (có box `ftypavif`) để **validate MIME theo nội dung** chấp nhận
định dạng này, nhưng cố tình **không mã hoá được** (ảnh thật cần encoder AVIF). Vì vậy:
- `media` kiểm tra upload/đọc lại/xoá AVIF → PASS;
- resize AVIF → fallback giữ original, đúng hành vi đã chốt ở N4 (không để job resize hỏng upload).

Đây là giới hạn của bộ fixture, **không phải** bằng chứng rằng pipeline resize AVIF đã được kiểm thử.

### Kích thước sau resize

Resize dùng `ResizeMode.Max` (giữ tỉ lệ, không crop) đúng như sản phẩm. Ảnh fixture 1200×800 cho ra:
`300×200` và `800×533` — tên object vẫn theo hợp đồng `..._300x300.jpg` / `..._800x600.jpg`
(đặt tên là kích thước *giới hạn*, không phải kích thước thực).

## Điều kiện trước khi chạy `jobs` và `email`

`jobs` cần Mailhog vì job `delayed` gửi ping qua SMTP thật. Thiếu Mailhog thì `email` báo `SKIP` còn
`jobs` sẽ FAIL ở job ping — cần bật Mailhog trước.

## Lỗi đã gặp trong quá trình làm lab (đã sửa, ghi lại để tránh lặp)

1. **API obsolete của Hangfire.PostgreSql 1.21** — `UsePostgreSqlStorage(string, ...)` và
   `PostgreSqlStorage(string, options)` đã obsolete, mà repo bật `TreatWarningsAsErrors`.
   Sửa: khởi tạo `new PostgreSqlStorage(new NpgsqlConnectionFactory(cs, options), options)` rồi gán
   `JobStorage.Current = storage` (đây là nơi `BackgroundJob`/`RecurringJob` tìm thấy storage).
2. **Queue mismatch (quan trọng)** — `BackgroundJob.Enqueue<T>(...)` không tham số queue sẽ vào queue
   `default`, còn worker của lab chỉ nghe queue `lab`. Job nằm im `Enqueued` mãi. Sửa: truyền queue
   tường minh `BackgroundJob.Enqueue<LabResizeJob>("lab", ...)`; với recurring dùng overload
   `RecurringJob.AddOrUpdate<T>(id, queue, methodCall, cron, options)` (lưu ý `RecurringJobOptions`
   **không** có thuộc tính `Queue`, queue là tham số riêng).
3. **Subject Mailhog nằm ở header** — Mailhog v2 trả subject tại `items[].Content.Headers.Subject`
   (mảng), không phải `items[].Subject`. Sửa parser.
4. **Xoá object không qua `IObjectStorageWriter`** — writer chỉ có `ExistsAsync`/`UploadAsync`;
   xoá phải dùng `IFileStorageService.DeleteAsync` (đúng ranh giới đã chốt ở N4).
5. **Bẫy encoding khi sửa file bằng PowerShell** — `Get-Content`/`Set-Content` mặc định của
   PowerShell 5.1 đọc/ghi UTF-8 không BOM theo ANSI, làm hỏng tiếng Việt trong mã nguồn và trong file
   `.ps1` (script không chạy được). Sửa: dùng `[System.IO.File]::ReadAllText/WriteAllText` với
   `UTF8Encoding`, hoặc chạy `pwsh`/tool soạn thảo văn bản; file `.ps1` nên lưu kèm BOM.

## Cấu trúc mã

| File | Vai trò |
|---|---|
| `Program.cs` | Dispatcher phase + DI (MinIO, logger) |
| `LabConfig.cs` | Đọc cấu hình từ env, dựng connection string lab |
| `LabLog.cs` | Console + file logger |
| `PhaseResult.cs` | Kết quả/check từng phase, tổng kết |
| `Fixtures.cs` | Sinh ảnh JPEG/PNG/WebP/AVIF + ảnh vượt giới hạn + file MIME giả |
| `MediaPhase.cs`, `LabImageScaler.cs` | Kiểm tra MIME theo nội dung, upload/đọc/xoá, resize |
| `EmailPhase.cs` | MailKit + Mailhog API |
| `SitemapPhase.cs` | Sinh và validate sitemap XML |
| `LabJobs.cs`, `JobsPhase.cs` | 4 job Hangfire và kịch bản kiểm thử |
| `DbEvidencePhase.cs`, `PurgePhase.cs` | Hai phase chẩn đoán (dump DB, dọn job sót) |
