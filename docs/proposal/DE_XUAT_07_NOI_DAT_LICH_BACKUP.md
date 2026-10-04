# Đề xuất gỡ block — Nơi đặt lịch backup 03:00

> **Block**: [`HANDOFF_TV4_TUAN4_N1.md`](../evidence/TV4/Tuan04/HANDOFF_TV4_TUAN4_N1.md) §4.2 · [`KE_HOACH_DU_AN.md`](../KE_HOACH_DU_AN.md) mục 8 (ghi chú dòng 339: *"timezone backup cần nhóm chốt"*)
> **Mức**: 🟠 Trung bình — ảnh hưởng trực tiếp `NFR-REL-003` (backup/restore)
> **Người lập**: Nguyễn Hữu Trung Sơn (2312739 — TV4) · **Ngày**: 03/10/2026
> **Cần ai quyết**: Nguyễn Thanh Tâm (TV1 — Nhóm trưởng), vì đây là **quyết định hạ tầng ngoài phạm vi repo**
> **Trạng thái**: 🔴 **CHỜ QUYẾT ĐỊNH — không tự sửa.** TV4 không tự chọn vì hệ quả vận hành nằm ngoài dự án.

---

## 1. Thực trạng

Lịch backup **đã có trong repo** và đúng múi giờ:

| Mục | Giá trị |
|---|---|
| Nơi đặt | `.github/workflows/backup.yml` |
| Cron | `0 20 * * *` (UTC) = **03:00 ngày hôm sau theo Asia/Ho_Chi_Minh** |
| `BACKUP_KEEP_DAYS` | `'30'` (dọn file cũ hơn 30 ngày **trên runner**) |
| Secret cần | `DATABASE_URL` trong repo settings |
| Có chạy tay | ✅ `workflow_dispatch` |

**Ba rủi ro đã xác định khi kiểm chứng:**

| # | Rủi ro | Bằng chứng |
|---|---|---|
| R1 | **GitHub có thể trễ hoặc bỏ qua** job theo lịch khi repo lâu không có commit | Hành vi đã biết của GitHub Actions scheduler. Tuần 4 này repo có commit nên chưa lộ, nhưng **không đảm bảo** cho tương lai |
| R2 | Job cần secret `DATABASE_URL`; **thiếu secret ⇒ workflow đỏ** | Đã cố tình thiếu để kiểm tra thông báo rõ ràng |
| R3 | Múi giờ dễ sai khi người khác sửa cron | Comment trong YAML đã cảnh báo: `0 3 * * *` sẽ chạy **10:00** giờ VN |

> ⚠️ **Điểm dễ hiểu nhầm:** `BACKUP_KEEP_DAYS=30` **không** có nghĩa giữ 30 ngày thật.
> Xem [`DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md`](./DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md).

## 2. Vì sao đây là block thật, không phải việc TV4 tự quyết

`NFR-REL-001` yêu cầu uptime ≥ 99,5% ⇒ phải có backup **tin cậy**. Nhưng:

1. Chọn nơi đặt lịch = chọn **hạ tầng vận hành** (GitHub Actions miễn phí vs Render/host có phí).
2. Chi phí và phần cấu hình nằm **ngoài repo** ⇒ không thể kiểm chứng bằng test trong project.
3. Ảnh hưởng tới **chi phí nhóm** và **cam kết SLA** ⇒ thuộc quyết định của Nhóm trưởng.

## 3. Phương án

### Phương án A — Giữ GitHub Actions, thêm job canh (khuyến nghị)

Giữ `backup.yml`, thêm **workflow thứ hai** chạy mỗi ngày lúc `06:00 ICT` kiểm tra: "bản backup hôm qua có tồn tại không?". Nếu không có ⇒ cảnh báo.

| Ưu | Nhược |
|---|---|
| **0 đồng** chi phí | Vẫn phụ thuộc scheduler của GitHub (giảm, chưa loại bỏ) |
| Không cần hạ tầng mới | Cần secret `ALERT_WEBHOOK` (email/Slack/Teams) |
| Phát hiện hỏng trong ≤ 24h | Chỉ phát hiện, **không** backup được khi GitHub không chạy |

### Phương án B — Cron trên Render (đã có blueprint `render.yaml`)

Render có **cron job** miễn phí cho blueprint. Đặt `pg_dump` 03:00 ICT ngay trên Render, dump vào disk/ RustFS.

| Ưu | Nhược |
|---|---|
| Scheduler **không** bỏ qua job | Plan miễn phí của Render có thể **sleep** ⇒ cron không chắc chạy |
| Đúng 03:00 ICT theo giờ địa phương | Cần `render.yaml` sửa thêm (ngoài repo hiện tại) |
| Không phụ thuộc commit gần đây | Cần secret `DATABASE_URL` + bucket đích (xem đề xuất 08) |

### Phương án C — Cron trên máy/VPS của nhóm

Dùng cron thật trên Linux (host Render, VPS, hoặc máy cá nhân có bật 24/7).

| Ưu | Nhược |
|---|---|
| **Tin cậy nhất** — cron thật không bị scheduler bỏ qua | Phụ thuộc máy **luôn bật**; máy cá nhân tắt là mất backup |
| `pg_dump` chạy trực tiếp, không qua secret CI | Không ai giám sát nếu máy sập |

## 4. So sánh

| Tiêu chí | A · GitHub + canh | B · Render cron | C · Cron thật |
|---|---|---|---|
| Chi phí | 0 | 0 (có thể) | 0 – có phí |
| Không bị scheduler bỏ qua | ❌ (chỉ giảm rủi ro) | ⚠️ (có thể sleep) | ✅ |
| Đúng 03:00 ICT | ✅ (cron UTC cố định) | ✅ | ✅ |
| Cần hạ tầng ngoài repo | ❌ | ⚠️ có | ✅ có |
| Giám sát tự động | ✅ (job canh) | ❌ | ❌ |

## 5. Rủi ro

| Rủi ro | Khả năng | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| Chọn A mà repo lâu không commit ⇒ scheduler bỏ ⇒ **không có** backup mà không ai biết | Trung bình | Mất dữ liệu khi sự cố | ⛔ Bắt buộc kèm job canh; **không** chọn A một mình |
| Chọn B/C mà kho backup sai cấu hình ⇒ có file nhưng hỏng | Trung bình | Restore thất bại lúc cần | Xem [`DE_XUAT_08`](./DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md); phải có **drill restore thật** |
| Nhiều nơi cùng chạy backup ⇒ tốn tài nguyên, dễ nhầm bản nào đúng | Thấp | Rối khi sự cố | Chọn **một** nơi chính, nơi còn lại chỉ để dự phòng |
| Nghĩ đã có backup nhưng **chưa** drill restore lần nào | **Cao** | Mất dữ liệu khi sự cố | N1-4 đã drill 14 bảng ✅ — phải **làm lại** sau khi đổi nơi lưu |

## 6. Test case nghiệm thu

| # | Test case | Kỳ vọng |
|---|---|---|
| 1 | Đợi tới đúng giờ lịch đã chọn | Có bản backup mới, timestamp đúng |
| 2 | Cố tình xoá secret `DATABASE_URL` | Workflow **đỏ với thông báo rõ ràng**, không phải lỗi mơ hồ |
| 3 | Job canh (nếu chọn A) khi không có bản backup hôm qua | Cảnh báo gửi đi thật |
| 4 | `deploy/restore.sh` chạy với bản backup mới nhất | Restore thành công, đủ **14 bảng** như drill N1-4 |
| 5 | Đổi múi giờ máy chủ / đổi timezone runner | Vẫn chạy **03:00 ICT** |
| 6 | Repo 30 ngày không có commit mới (nếu chọn A) | Backup vẫn chạy **hoặc** job canh báo đỏ |

## 7. Quyết định cần chốt

1. Chọn **A** (GitHub + job canh), **B** (Render cron) hay **C** (cron thật)?
2. Ai cấu hình phần nằm ngoài repo (secret, blueprint, cron trên host)?
3. Có chấp nhận rủi ro scheduler của GitHub (nếu A) để đổi lấy chi phí 0?

> 📌 **TV4 không tự quyết mục này.** Lý do: quyết định nằm ngoài phạm vi dự án và có chi phí thật.
> Trong lúc chờ, backup vẫn chạy theo lịch hiện có (`0 20 * * *`) — **không dừng** vì chờ quyết định.
