# Sổ K — Lab L4 (TV4 Tuần 3)

Bằng chứng thực thi cho các yêu cầu L4, chạy bằng `practice/TV4/L4` (console app, không thay đổi sản phẩm).
Nguồn: `logs/lab_l4_run.log` (log đầy đủ của một lần chạy `all`) và `logs/lab_l4_db.txt`
(truy vấn trực tiếp database lab bằng Npgsql trong chính process của lab).

- Lần chạy: `20260927-183148`, môi trường `Testing` (không phải Production).
- Lệnh: `dotnet run --project practice/TV4/L4 -c Release -- all`
- Kết quả tổng: **4 phase, 39/39 check PASS**, mã thoát `0`.

## 1. Media — 25/25 PASS

| Nhóm check | Kết quả quan sát được |
|---|---|
| Nhận diện MIME theo **nội dung** file | JPEG, PNG, WebP, AVIF đều nhận đúng `Content-Type`; file JPEG giả (đuôi `.jpg` nhưng dữ liệu PNG rác) bị từ chối `file.invalid_type`; ảnh vượt giới hạn bị từ chối `file.too_large` |
| Upload + đọc lại | 4/4 định dạng: byte ghi ra bằng byte đọc lại (15613 / 5607 / 1702 / 28) |
| Resize 2 kích thước | JPEG `300×200` (1601 B) + `800×533` (7413 B); PNG `300×200` (733 B) + `800×533` (2839 B); WebP `300×200` (186 B) + `800×533` (814 B) |
| Resize AVIF | Fallback giữ original, không sinh biến thể, không làm hỏng upload |
| Idempotent | Chạy resize lần hai: hai object đã tồn tại, không ghi đè |
| Dọn dẹp | Xoá original + 2 biến thể của cả 3 định dạng → không còn object |

Ghi chú: kích thước thực là `300×200`/`800×533` vì dùng `ResizeMode.Max` (giữ tỉ lệ), khớp hợp đồng
đặt tên `..._300x300` / `..._800x600` đã chốt ở N4.

## 2. Email — 3/3 PASS

- MailKit gửi 2 mail (plain text + HTML) qua SMTP `127.0.0.1:1025` không ném exception.
- Mailhog nhận đủ: số mail tích luỹ tăng từ 37 lên 39 (đếm trước/sau qua API).
- Mailhog API trả đúng subject của mail vừa gửi:
  `[LAB L4] Plain text 20260927-183148` và `[LAB L4] HTML + =?utf-8?b?...?= 20260927-183148`
  (MailKit MIME-encode phần tiếng Việt trong subject; tiền tố ASCII vẫn khớp để đối chiếu).

## 3. XML — 3/3 PASS

- Sinh `sitemap.xml` từ DB thật `culinary_test` (chỉ đọc): **93 URL**.
- Parse lại bằng `XDocument` → XML hợp lệ, đúng 93 phần tử `<url>`.
- Đối chiếu: `Published = 93` trong tổng `300` recipe công khai; bảng `Recipes` có 307 dòng (`Draft 190`, `Archived 24`, `Published 93`) vì **7 dòng soft-deleted bị global filter loại** theo ADR D08.

## 4. Jobs — 8/8 PASS (Hangfire + PostgreSQL)

Worker chạy trong chính process lab, queue `lab`, storage PostgreSQL ở database riêng `culinary_lab`.

| Kịch bản | Bằng chứng |
|---|---|
| Fire-and-forget | Job `40` → `Succeeded`; job resize tạo đủ 2 object phái sinh |
| Tắt worker, job không mất | Job `41` còn `Scheduled` trong DB khi đã dừng worker |
| Delayed + restart worker | Job `41` → `Succeeded` sau khi khởi động lại worker (~23 s) |
| Retry | Job `42`: `Processing` 3 lần, `Failed` 2 lần, hai lần `Scheduled` với lý do `Retry attempt 1/2 of 5`, kết thúc `Succeeded` |
| Recurring | Job `43`, `44` với `Enqueued` có reason `Triggered by recurring job scheduler`; sitemap chạy 2 lần theo cron `*/5 * * * * *` |
| Dọn dẹp | `RemoveIfExists("lab-sitemap")` → bảng `hangfire.hash` trống; object lab đã xoá; `hangfire.jobqueue` còn 0 dòng |

Đối chiếu độc lập bằng truy vấn DB (`logs/lab_l4_db.txt`): 20 job đều `statename = Succeeded`,
`so_job_Failed_cuoi = 0`, `hangfire.jobqueue_con_lai = 0`, `hangfire.hash` trống.

Lưu ý khi đọc hash recurring: khoá `Queue` trong `hangfire.hash` hiển thị `default` (đây là hành vi
lưu trữ của Hangfire 1.8), **queue thật nằm trong JSON của job** và là `lab` — khớp với worker.

## 5. Điều kiện và giới hạn cần nói rõ

1. **AVIF chỉ mới được kiểm thử ở mức MIME/upload/xoá**, chưa kiểm thử được nhánh resize vì fixture
   AVIF là file hợp lệ về `ftyp` nhưng không mã hoá được (xem `practice/TV4/L4/README.md`).
   Nhánh resize AVIF **giữ nguyên theo thiết kế N4** — đã có test ở `ImageResizeD2Tests`, chưa có bằng chứng
   chạy tay trong lab này.
2. **Mailhog là hạ tầng phụ thuộc**, không phải hạ tầng sản phẩm. Phase `jobs` dùng SMTP thật cho job
   `delayed` nên bắt buộc có Mailhog; thiếu Mailhog thì `jobs` FAIL.
3. **Cron `*/5 * * * * *` (5 giây) chỉ dùng cho lab**. Sản phẩm giữ cron `02:00 UTC` theo D26.
4. **Job ID, số URL (93) và số mail tích luỹ phụ thuộc dữ liệu máy dev** — các lần chạy khác máy sẽ có
   số khác. Vì vậy sổ ghi kèm cả câu lệnh và log thay vì chỉ ghi con số.
5. **Worker chỉ nghe một queue** trong kịch bản restart; chưa kiểm thử trường hợp nhiều queue/worker
   cùng lúc ở mức lab (phần này do D23 và test đồng thời của sản phẩm phụ trách).
6. **Job `flaky` dùng `AutomaticRetry(Attempts = 5, DelaysInSeconds = [5])`** để quan sát được nhiều vòng
   retry trong thời gian chấp nhận được (log ghi `Retry attempt 1 of 5`); các job còn lại của lab và
   `ResizeImageJob` của sản phẩm đều dùng `Attempts = 3` như D23. Lab **không** đổi cấu hình retry
   của sản phẩm, chỉ chọn số lần khác nhau cho job cố tình hỏng.
