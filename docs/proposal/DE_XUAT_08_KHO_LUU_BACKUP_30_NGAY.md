# Đề xuất gỡ block — Kho lưu backup 30 ngày

> **Block**: [`HANDOFF_TV4_TUAN4_N1.md`](../evidence/TV4/Tuan04/HANDOFF_TV4_TUAN4_N1.md) §4.3 · [`KE_HOACH_DU_AN.md`](../KE_HOACH_DU_AN.md) mục 8 (dòng 339: *"giữ 30 ngày"*)
> **Mức**: 🔴 **Cao** — ảnh hưởng trực tiếp `NFR-REL-003` và khả năng khôi phục dữ liệu
> **Người lập**: Nguyễn Hữu Trung Sơn (2312739 — TV4) · **Ngày**: 03/10/2026
> **Cần ai quyết**: Nguyễn Thanh Tâm (TV1 — Nhóm trưởng) + **TV2** (đang giữ phần search/cache)
> **Trạng thái**: 🔴 **CHỜ QUYẾT ĐỊNH — không tự sửa.** Kho lưu trữ nằm ngoài repo.

---

## 1. Thực trạng — đây là chỗ dễ hiểu nhầm nhất

`backup.yml` có `BACKUP_KEEP_DAYS: '30'`. **Con số này KHÔNG có nghĩa là giữ được 30 ngày.**

| Mức | Giữ bao lâu | Nơi lưu | Đúng yêu cầu? |
|---|---|---|---|
| **Trong runner** | Dọn file > 30 ngày | `$HOME/culinary-backups` trên `ubuntu-latest` | ⚠️ Runner bị **xoá sạch** sau mỗi job |
| **Artifact GitHub** | **7 ngày** | GitHub Actions storage | ❌ **Không** đạt 30 ngày |
| **Kho bên ngoài** | — | RustFS / S3 / NAS | ✅ Đạt — nhưng **chưa có** |

⇒ **Thực tế hiện tại: chỉ giữ được 7 ngày.** Yêu cầu 30 ngày **chưa** thoả.

### Bằng chứng đã kiểm chứng

- `deploy/backup.sh` và `deploy/restore.sh` đã chạy thật, drill **14 bảng** thành công (N1-4).
- `*.dump` đã thêm vào `.gitignore` vì **dump chứa dữ liệu người dùng** ⇒ không thể commit dump vào repo
  để "giữ lâu".
- `deploy/scan-secrets.sh` sẽ bắt credential nếu nhét vào workflow ⇒ kho phải dùng **secret**, không
  dùng khoá cứng.

## 2. Nguyên nhân

1. `pg_dump` xuất file **không nén** và có thể vài trăm MB ⇒ không thể để trong git hay artifact
   dài hạn (vừa tốn phí vừa rò dữ liệu).
2. RustFS/S3 đã có sẵn trong stack nhưng **chưa cấu hình bucket riêng cho backup** và chưa có
   vòng đời (lifecycle) xoá theo số ngày.
3. Render có **disk tạm** — mất khi restart ⇒ không dùng làm kho backup được.

## 3. Phương án

### Phương án A — Bucket RustFS/S3 riêng + vòng đời 30 ngày (khuyến nghị)

Tạo bucket `culinary-backups` **riêng biệt**, bật lifecycle xoá sau 30 ngày, backup đẩy lên đó
bằng `mc` hoặc `aws s3 cp` (RustFS tương thích S3).

| Ưu | Nhược |
|---|---|
| ✅ Đạt đúng 30 ngày | Cần cấu hình bucket + secret (ngoài repo) |
| Tận dụng hạ tầng **đã có sẵn** | RustFS chạy trong dev stack ⇒ production cần bucket thật (Render/S3) |
| Không tốn phí nếu RustFS chạy trên Render đã trả tiền | ⚠️ Bucket backup **không được public** |
| Lifecycle xoá tự động ⇒ không lo quên dọn | Cần kiểm tra bucket **không** nằm trong cùng prefix ảnh |

### Phương án B — NAS / ổ đĩa ngoài

Cron `pg_dump` ghi thẳng ra ổ đĩa/NAS có sẵn ở trường hoặc nhà.

| Ưu | Nhược |
|---|---|
| Đơn giản nhất, dễ kiểm tra bằng mắt | Phụ thuộc máy ở nhà **luôn bật** và có mạng |
| Rẻ | Không có bản sao thứ hai ở nơi khác |

### Phương án C — Chấp nhận 7 ngày, ghi rõ giới hạn

Không làm gì thêm; **ghi rõ** vào tài liệu rằng giữ 7 ngày và đánh dấu `NFR-REL-003` **chưa đạt**.

| Ưu | Nhược |
|---|---|
| 0 công sức, 0 chi phí | ❌ **Không đạt yêu cầu 30 ngày** |
| Trung thực về năng lực thật | Phải nộp phần NFR chưa đạt |

## 4. So sánh

| Tiêu chí | A · RustFS/S3 | B · NAS | C · Chấp nhận 7 ngày |
|---|---|---|---|
| Đạt 30 ngày | ✅ | ✅ | ❌ |
| Dùng hạ tầng đã có | ✅ | ❌ | ✅ |
| Truy cập ngoài mạng nội bộ | ✅ | ⚠️ phải có VPN/tunnel | ✅ |
| Phụ thuộc máy vật lý | ❌ | ✅ | ❌ |
| Chi phí | 0 (nếu RustFS có sẵn) | có thể 0 | 0 |
| Trả lời được khi reviewer hỏi "30 ngày ở đâu" | ✅ | ✅ | ❌ (phải nói "chưa đạt") |

## 5. Rủi ro

| Rủi ro | Khả năng | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| ⛔ Bucket backup **cùng bucket** với ảnh người dùng, lifecycle xoá nhầm ảnh | Thấp | **Mất toàn bộ ảnh** | Bucket **riêng** + prefix riêng; kiểm tra policy trước khi bật lifecycle |
| Dump chứa dữ liệu người dùng nằm trong kho không mã hoá | Trung bình | Rò dữ liệu cá nhân | ⛔ Bucket **private**; cân nhắc mã hoá `pg_dump` (`-Fc` + mật khẩu) |
| Có file trong kho nhưng **không khôi phục được** | **Cao** nếu không drill | Mất dữ liệu khi sự cố | ⛔ **Bắt buộc** drill `restore.sh` từ kho mới **trước khi** coi là xong |
| Số 30 ngày ghi trong YAML nhưng thực tế 7 ⇒ báo cáo sai | **Đã xảy ra** | Mất uy tín khi bảo vệ | Ghi rõ giới hạn trong báo cáo; đổi tên biến `BACKUP_KEEP_DAYS` thành `RUNNER_PRUNE_DAYS` để không nhầm |

## 6. Test case nghiệm thu

| # | Test case | Kỳ vọng |
|---|---|---|
| 1 | Liệt kê object trong bucket backup | Có dump theo ngày, **tách biệt** với ảnh recipe |
| 2 | Đợi/chạy tay để có ≥ 2 bản | Bản mới nhất đúng ngày, kích thước > 0 |
| 3 | `deploy/restore.sh` từ **bản trong kho** (không phải file local) | Restore thành công, đủ **14 bảng** |
| 4 | Xem policy bucket | ⛔ Không public; không có `AllowAnyOrigin` |
| 5 | Kiểm tra lifecycle | Xoá đúng sau 30 ngày, **không** chạm bucket ảnh |
| 6 | Xoá bản backup cũ nhất bằng tay | Không ảnh hưởng ảnh recipe |
| 7 | `deploy/scan-secrets.sh` | exit 0 — không có secret hardcode trong workflow |

## 7. Quyết định cần chốt

1. Chọn **A** (RustFS/S3), **B** (NAS) hay **C** (chấp nhận 7 ngày, ghi giới hạn)?
2. Ai cấu hình bucket/lifecycle/secret phần ngoài repo?
3. Có chấp nhận `pg_dump -Fc` + mật khẩu (dump mã hoá) không?
4. Có cần giữ thêm bản sao ở **hai nơi** (kho chính + kho dự phòng) không?

> 📌 **TV4 không tự quyết mục này.** Lý do: kho lưu trữ và secret nằm ngoài repo.
> Trong lúc chờ, **backup vẫn chạy** và drill đã chứng minh `restore.sh` hoạt động — thiếu duy nhất là
> **thời hạn lưu 30 ngày**. Ghi rõ giới hạn này trong báo cáo, không ghi "đạt 30 ngày".
