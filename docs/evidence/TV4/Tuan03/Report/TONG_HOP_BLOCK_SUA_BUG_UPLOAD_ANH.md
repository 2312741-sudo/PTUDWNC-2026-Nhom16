# Tổng hợp block khi sửa bug upload ảnh 500

> **Ngữ cảnh**: sửa [`BAO_CAO_LOI_UPLOAD_ANH_500.md`](./BAO_CAO_LOI_UPLOAD_ANH_500.md) và soát
> tích hợp ([`TEST_CASE_TICH_HOP_FE_BE.md`](./TEST_CASE_TICH_HOP_FE_BE.md)).
> **Phạm vi tài liệu này**: các hạng mục **đã xác định nguyên nhân + đã có đề xuất giải pháp +
> rủi ro + test case**, nhưng **cần quyết định của nhóm** mới sửa được. Không phải handoff.
> **Ngày lập**: 28/09/2026 · **Người lập**: Nguyễn Hữu Trung Sơn (2312739 — TV4)

---

## 1. Trạng thái: đã sửa được gì

| Hạng mục | Kết quả |
|---|---|
| Bug 500 `POST /recipes/{id}/images` | ✅ Đã sửa bằng cấu hình dev + khoá bằng test parity |
| Bug mật khẩu DB lệch compose (cùng lớp) | ✅ Đã sửa + test parity |
| Hướng dẫn `NEXT_PUBLIC_MEDIA_URL` sai | ✅ Đã sửa `docs/HUONG_DAN_CHAY_TV4.md` |
| 6 hạng mục còn lại | ⛔ **Block** — mỗi cái 1 file đề xuất bên dưới |

## 2. Danh sách block

| # | Block | Vì sao block | Loại quyết định | File đề xuất | Mức |
|---|---|---|---|---|---|
| B1 | Lỗi object storage trả `500 server.error` chung chung, FE không phân biệt được | Đổi **contract lỗi API** (thêm mã `storage.unavailable`, đổi 500 → 503) ⇒ FE phải biết mã mới, có thể cần thêm retry | Nhóm thống nhất contract lỗi (có TV3 là consumer) | [`DE_XUAT_01_LOI_STORAGE_TRA_503_CO_MA_LOI.md`](./DE_XUAT_01_LOI_STORAGE_TRA_503_CO_MA_LOI.md) | 🔴 Cao |
| B2 | API khởi động "thành công" dù thiếu credential storage; lỗi lộ ra giữa lúc user đang dùng | Bắt validate lúc startup ⇒ **thành viên không bật được storage cũng không chạy được API** (mất khả năng làm việc nhóm khác) | Nhóm chọn: fail-fast toàn cục / cảnh báo / chỉ dev | [`DE_XUAT_02_FAIL_FAST_KHI_THIEU_CAU_HINH.md`](./DE_XUAT_02_FAIL_FAST_KHI_THIEU_CAU_HINH.md) | 🔴 Cao |
| B3 | `.env.example` liệt kê `Minio__*` nhưng app **không** nạp `.env` ⇒ tưởng đã cấu hình | Thêm package `DotNetEnv` (dependency mới + `.env` có thể lọt vào CI/log) **hoặc** chỉ sửa tài liệu | Nhóm chọn hướng: dependency mới vs tài liệu | [`DE_XUAT_03_NAP_FILE_DOT_ENV.md`](./DE_XUAT_03_NAP_FILE_DOT_ENV.md) | 🟡 TB |
| B4 | `/health` báo `minio=Healthy` khi credential sai (chỉ TCP probe) | Đổi ngữ nghĩa `/health`; kết quả này được dùng để quyết định "app sẵn sàng" ⇒ báo sai hướng nguy hiểm | Nhóm chốt ngữ nghĩa health check | [`DE_XUAT_04_HEALTH_CHECK_STORAGE_XAC_THUC.md`](./DE_XUAT_04_HEALTH_CHECK_STORAGE_XAC_THUC.md) | 🟡 TB |
| B5 | Xem trước ảnh recipe **Draft** trong wizard: `<img>` không gửi Bearer ⇒ proxy `403` | Cần chọn cơ chế: presigned URL / token trong query / cookie / chấp nhận không xem được. **Đụng IMAGE_CONTRACT D27** | Nhóm + TV3 (chủ sở hữu UI wizard) | [`DE_XUAT_05_XEM_ANH_DRAFT_TRONG_WIZARD.md`](./DE_XUAT_05_XEM_ANH_DRAFT_TRONG_WIZARD.md) | 🟡 TB |
| B6 | Không có user **Admin** ⇒ không ai mở được `/hangfire`, không ai test được CRUD category | Tạo user Admin trong seeder = dữ liệu/mật khẩu seed ⇒ cần chốt tài khoản & cách phân phối mật khẩu | Nhóm + TV1 (quản trị dữ liệu seed) | [`DE_XUAT_06_TAI_TAO_USER_ADMIN_DE_SEED.md`](./DE_XUAT_06_TAI_TAO_USER_ADMIN_DE_SEED.md) | 🟢 Thấp |

## 3. Khuyến nghị thứ tự gỡ

1. **B1 + B2** (đi cặp): sửa contract lỗi + fail-fast ⇒ xử lý gốc rễ "lỗi hạ tầng hiện ra muộn và
   chung chung". Có thể gộp vào **một pull request** vì cùng đụng `ApiExceptionHandler`/`Program.cs`.
2. **B3 + B4**: tài liệu + health check, độc lập nhau, làm song song được.
3. **B5**: cần trao đổi với TV3 vì liên quan UI wizard.
4. **B6**: làm khi có nhu cầu demo/dùng Hangfire thật.

## 4. Điều **không** bị block (đã làm trong lần sửa này)

| Việc | Vì sao không cần hỏi |
|---|---|
| Thêm `Minio` + sửa mật khẩu DB trong `appsettings.Development.json` | File dev-only; giá trị **đã có sẵn** trong `docker-compose.dev.yml`/`.env.example`; không đụng production |
| Thêm `DevConfigParityTests` | Chỉ đọc file cấu hình, không đổi hành vi runtime, không cần hạ tầng |
| Sửa `HUONG_DAN_CHAY_TV4.md` L9 | Tài liệu đang sai so với code, sửa theo hành vi thực tế |
| Không thêm `DotNetEnv`, không validate startup, không đổi mã lỗi | Đều là thay đổi hành vi/contract ⇒ để quyết định ở B1–B3 |
