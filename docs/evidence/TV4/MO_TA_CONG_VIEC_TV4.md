# MÔ TẢ CÔNG VIỆC TV4 — Nguyễn Hữu Trung Sơn (2312739)

> Tài liệu này giải thích bằng lời đơn giản, ít thuật ngữ kỹ thuật, về **TV4 làm gì**, **công việc chạy theo thứ tự nào**, **bị giới hạn ở đâu** và **chuyển giao cho ai**.
> Chi tiết kỹ thuật đầy đủ nằm ở: `KE_HOACH_DU_AN.md` (mục 8), `PHAN_CHIA_CONG_VIEC_6_TUAN.md`, `SO_EVIDENCE_TV4.md`, `KE_HOACH_TUAN_1_TV4.md`.

---

## 1. TV4 là ai và làm gì?

**Nguyễn Hữu Trung Sơn (MSSV 2312739)** — thành viên thứ 4 của nhóm.

TV4 phụ trách phần: **Xuất bản công thức, quản lý hình ảnh, làm cho website dễ tìm trên Google (SEO), và vận hành hệ thống** — nghĩa là phần "đưa công thức ra công chúng và giữ cho hệ thống chạy ổn định".

Các mã công việc chính của TV4 (gọi là **D1 đến D7**):

| Mã | Tên gọi đơn giản | Làm gì cụ thể |
|---|---|---|
| D1 | Lưu trữ hình ảnh | Đăng tải, xóa ảnh món ăn; kiểm tra định dạng (JPEG/PNG/WebP/AVIF), kích thước tối đa 5MB; đặt tên file ngẫu nhiên để tránh trùng |
| D2 | Thu nhỏ hình ảnh | Tự động tạo ảnh kích thước nhỏ (300×300 và 800×600) để trang tải nhanh hơn |
| D3 | Xuất bản công thức | Cho phép tác giả đăng (publish), gỡ (unpublish), lưu trữ (archive), xóa công thức; và chức năng đăng xuất (logout) |
| D4 | Giao diện ảnh + SEO | Màn hình tải ảnh có thanh tiến trình %, sắp xếp ảnh; viết thẻ mô tả cho Google; tạo sitemap, robots |
| D5 | Vận hành hệ thống | Cài đặt môi trường chạy (Compose), Nginx, kiểm tra sức khỏe (health), lưu dự phòng (backup/restore), HTTPS |
| D6 | Bài thực hành cá nhân | Điền đủ 24 nhóm kỹ năng (K01–K24) bằng bài làm cá nhân trên nhánh riêng |
| D7 | Kiểm thử và bàn giao | Viết test, chạy kiểm thử tải/lỗi, soạn tài liệu vận hành, release |

Tổng cộng 100 điểm công việc dự kiến. **Lưu ý: điểm này là ước lượng, không phải điểm chấm.**

---

## 2. Công việc chạy theo thứ tự nào? (Flow)

### 2.1. Thứ tự 6 tuần của TV4

| Tuần | TV4 làm gì | Kết quả mong đợi |
|---|---|---|
| 1 | Dựng môi trường chạy (Postgres + Redis + MinIO + Nginx), viết hợp đồng lưu ảnh, chức năng đăng xuất, kiểm tra sức khỏe | Hệ thống chạy được bằng 1 lệnh; có khung lưu ảnh; đăng xuất hoạt động |
| 2 | Làm đầy đủ chức năng ảnh (upload, đặt ảnh chính, xóa ảnh, thu nhỏ), chức năng đăng/gỡ công thức | Công thức có ảnh đầy đủ; đăng/gỡ công thức hoạt động đúng |
| 3 | Lưu trữ/xóa công thức theo quyết định nhóm; sitemap, robots, thẻ SEO; giám sát hệ thống | Website dễ tìm trên Google; hệ thống được theo dõi |
| 4 | Hoàn tất bài thực hành cá nhân; kiểm thử lỗi hệ thống, phục hồi dữ liệu, tải | Đủ 24/24 nhóm kỹ năng cá nhân |
| 5 | Tự dựng & phục hồi hệ thống; kiểm tra HTTPS, bảo mật, tải; soạn tài liệu vận hành | Mọi thứ chạy trên môi trường thử nghiệm; có số đo hiệu năng |
| 6 | Kiểm tra lại toàn bộ, demo và bàn giao | Trình bày được phần của mình; tài liệu đầy đủ |

### 2.2. Luồng làm việc từng ngày (hàng tuần)

```
Đầu tuần: xác nhận việc cần làm + phụ thuộc với các thành viên
  → Giữa tuần: gộp code (PR) để các thành viên khác dùng được
  → Cuối tuần: demo sản phẩm + cập nhật sổ minh chứng cá nhân
  → Lặp lại tuần sau
```

### 2.3. Luồng một công thức đi qua tay TV4

```
TV3 tạo công thức nháp (Draft) + thêm nguyên liệu, bước làm
  → TV4 xây chức năng: đăng tải ảnh lên, chọn ảnh chính (ảnh đầu tiên tự động là ảnh chính)
  → TV4 xây chức năng "Đăng" (Publish) → công thức hiện ra cho mọi người xem
  → TV4 xây chức năng "Gỡ" (Unpublish) hoặc "Lưu trữ" (Archive) → khuất khỏi trang chủ
  → TV2: công thức Published lọt vào tìm kiếm
```

> **Chú ý:** các bước trên là **chức năng TV4 xây dựng** để tác giả dùng; TV4 không thao tác hộ người dùng.
> Để bấm "Đăng" được, công thức phải có **ít nhất 1 nguyên liệu và 1 bước làm** (theo Phụ lục B `RECIPE_PUBLISH_INCOMPLETE` — quyết định D07, tuần 2 chốt). Ảnh không bắt buộc để đăng.

### 2.4. Quy tắc nhỏ khi code (Definition of Done)

Mỗi task chỉ được tính "xong" khi:

- [ ] Đúng theo yêu cầu và các quyết định nhóm đã chốt (FR/ADR)
- [ ] Có giao diện nếu người dùng thao tác
- [ ] Có kiểm tra quyền: ai được làm, ai không
- [ ] Có test (kiểm thử) chạy đạt
- [ ] Có nhánh Git + PR được **Nguyễn Thanh Tâm (nhóm trưởng)** review
- [ ] Cập nhật sổ minh chứng cá nhân

---

## 3. Hạn chế của TV4 (không được làm gì / làm đến đâu)

TV4 có một số **giới hạn** để tránh làm thay việc người khác hoặc làm sai quyết định nhóm:

1. **Không tự quyết các mâu thuẫn trong đề** — các điểm mâu thuẫn (gọi là D-ghi chú) phải được chốt qua ADR và xác nhận với nhóm trưởng/giảng viên. TV4 phụ trách các ADR: **D06** (đăng xuất), **D08** (xóa công thức), **D17** (sửa ảnh), **D21** (chỉ số thời gian hoạt động), **D22** (kiểm tra sức khỏe), **D23** (tác vụ nền), **D27** (ảnh công khai hay riêng tư).

2. **Không tự đặt bucket ảnh công khai** — SRS 2.4.1/FR-FILE-001 ghi bucket để `public-read`, nhưng TV4 theo D27 mặc định ảnh để **chế độ riêng tư** (private) để không lộ ảnh công thức nháp. Muốn công khai phải có thay đổi yêu cầu (CR) được duyệt.

3. **Không tự ý đổi vị trí/lịch backup** — thời gian sao lưu dữ liệu (03:00) và múi giờ phải được nhóm chốt, không tự suy ra.

4. **Không nhận vơ phần của người khác để "cho đủ kỹ năng"** — nếu chưa có cơ hội thực hành kỹ năng nào trong phần chính, TV4 phải tự làm bài thực hành trên nhánh `practice/TV4/...`, không coi việc "đã đọc/đã họp/đã xem" là hoàn thành.

5. **Không đưa mật khẩu, token, thông tin thật vào tài liệu/ảnh chụp** (evidence) — mọi thứ phải che/gỡ.

6. **Trả lỗi đúng quy cách** — khi hệ thống lỗi phải trả mã và thông điệp theo quy định chung (kiểu `400/401/403/404/409/422/429/500`), không tự bịa mã.

7. **Tuần 1 tạm thời chưa có chức năng thu hồi phiên đăng nhập cũ** (refresh token) — việc đó thuộc **TV3**, đến tuần 2 mới có. Vì vậy tuần 1 đăng xuất chỉ đánh dấu phiên làm việc hiện tại.

---

## 4. Chuyển giao (nhận từ ai, bàn giao cho ai)

### 4.1. TV4 cần nhận từ các bạn

| Từ | Nhận gì | Khi nào |
|---|---|---|
| **TV1 (Thanh Tâm)** | Hợp đồng đăng nhập/đăng ký (`AUTH_CONTRACT`), cách lấy thông tin user hiện tại | Đã có (tuần 1) |
| **TV2 (Trường Vĩ)** | Danh sách dữ liệu danh mục (`CategoryDto`), để trang chủ hoạt động | Tuần 1–2 |
| **TV3 (Quốc Trung)** | Cấu trúc công thức (`Recipe` entity/DTO), dữ liệu mẫu có nguyên liệu + bước | Tuần 1–2 (để TV4 thử chức năng đăng/gỡ) |

### 4.2. TV4 phải bàn giao cho các bạn

| Bàn giao cho | Gì | Khi nào |
|---|---|---|
| **Cả nhóm** | Môi trường chạy 1 lệnh (Compose), kiểm tra sức khỏe, file cấu hình mẫu `.env.example` | Giữa tuần 1 (G0) |
| **TV3** | Hợp đồng lưu ảnh (`IFileStorageService`) để tích hợp vào màn hình soạn công thức | Tuần 1–2 |
| **TV1/TV3** | Chức năng đăng xuất (logout) + cập nhật hợp đồng đăng nhập | Cuối tuần 1 |
| **TV2** | Ảnh + URL để hiển thị trong danh sách/trang chi tiết | Tuần 2–3 |

### 4.3. Nguyên tắc phối hợp

- Mọi PR do **Nguyễn Thanh Tâm (nhóm trưởng)** review và duyệt.
- Một file dùng chung chỉ **một người sửa tại một thời điểm**.
- Đổi hợp đồng API/DTO phải báo người tích hợp trước.
- TV4 phụ trách khung vận hành, **nhưng cả 4 người đều phải tự build/deploy/giải thích** — không để TV4 làm thay hết.

---

## 5. Những quyết định quan trọng TV4 cần chốt (ADR)

| Mã ADR | Nội dung ngắn gọn | Trạng thái |
|---|---|---|
| D22 | Kiểm tra sức khỏe: `/health` (DB + Redis + MinIO), `/live` (chỉ check tiến trình), `/ready` (chỉ check DB + Redis — không gồm MinIO, lỗi trả 503) | Đã ghi ADR-TV4-001, chờ xác nhận |
| D27 | Ảnh để **private**, dùng URL tạm cho người có quyền xem | Đã ghi, chờ xác nhận |
| D06 | Đăng xuất phải gửi kèm mã Bearer, trả 204 | Đã ghi, chờ xác nhận |
| D05 | Thu hồi token cũ — thuộc TV3 (tuần 2) | Tham chiếu, chờ TV3 |
| D25 | Khóa phiên bản công cụ (Redis 7, MinIO, .NET 10...) | Cả nhóm cùng làm |

Tài liệu đầy đủ: `docs/adr/ADR-TV4-001-van-hanh-storage-logout-tuan-1.md`.

---

## 6. Điểm cần lưu ý khi vận hành (với khách xem)

- **Ảnh công thức nháp/đã lưu trữ** không được xuất hiện ở nơi công khai (trang chủ, tìm kiếm...).
- Khi một công thức **gỡ (unpublish)** hoặc **xóa**, nó phải biến mất khỏi nơi công khai ngay, kể cả trong bộ nhớ đệm (cache) và tìm kiếm.
- **Sitemap** (bản đồ trang cho Google) chỉ chứa công thức đã **Đăng (Published)**.
- Nếu máy chủ lưu ảnh (MinIO) bị sập, hệ thống phải báo rõ ràng chứ không im lặng (tải ảnh lên trả lỗi 503 như SRS FR-RCP-008).
- Sao lưu dữ liệu mỗi ngày lúc 03:00, giữ 30 ngày; cần được phục hồi thử thành công.

---

## 7. Minh chứng cá nhân (evidence) — cách ghi

TV4 cần có **24/24 nhóm kỹ năng K01–K24** được xác nhận. Với mỗi ô Kxx:

- Ghi rõ tên kỹ năng, FR/NFR liên quan
- Đường dẫn code hoặc cấu hình
- PR/commit, test đã chạy, kết quả thực tế
- Ảnh/log/video (không chứa thông tin bí mật)
- Reviewer **Nguyễn Thanh Tâm** xác nhận + ngày

Nếu ô có nhiều kỹ năng nhỏ, phải liệt kê từng kỹ năng đã chứng minh; thiếu một phần thì ô chưa hoàn thành.

Sổ đầy đủ: `SO_EVIDENCE_TV4.md`.

---

## 8. Tóm tắt đơn giản nhất

> **TV4 = "người đưa món ăn lên trang và giữ cho hệ thống chạy tốt".**
> Làm ảnh, đăng/gỡ công thức, làm SEO, cài môi trường, kiểm tra hệ thống, viết bài học cá nhân.
> Mọi việc phải đúng quyết định nhóm (ADR), có người duyệt (Tâm), có minh chứng cá nhân, và đúng thời hạn từng tuần.