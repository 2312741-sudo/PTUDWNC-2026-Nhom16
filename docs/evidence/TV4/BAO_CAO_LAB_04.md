# BÁO CÁO LAB - MÔN PHÁT TRIỂN ỨNG DỤNG WEB NÂNG CAO

- **Lab**: **04 (Tuần 4 — Cài đặt Toàn bộ API Endpoints, Hangfire Resize ảnh, Media Proxy & Fix bug Upload 500)**
- **Từ ngày**: **23/09/2026** **đến ngày**: **30/09/2026**
- **MSSV**: **2312739**
- **Họ và tên**: **Nguyễn Hữu Trung Sơn**
- **Nhóm**: **16** (Thành viên — TV4)
- **Tên file nộp theo quy định**: `Lab4_2312739_NguyenHuuTrungSon.docx` *(kèm file dự phòng `Lab04_2312739_NguyenHuuTrungSon.docx`)*

---

### Bảng công việc thực hiện (Lab 04 / Tuần 4)

| STT | Công việc được giao | Liên kết đến github branch / PR / file | Tiến độ % |
|:---:|---|---|:---:|
| **1** | **[Yêu cầu trọng tâm Lab 4] Hoàn thành việc cài đặt TẤT CẢ các API endpoints theo hợp đồng và SRS (41 endpoints)**<br><br>_**Đã hoàn thành:**_<br>- Cùng nhóm hoàn thành và tích hợp 100% tất cả 41 API endpoints trên hệ thống, trong đó trực tiếp phụ trách các nhóm API Xuất bản, Quản lý ảnh và Giám sát hệ thống:<br>  + `PATCH /api/v1/recipes/{id}/publish`, `PATCH /api/v1/recipes/{id}/unpublish`, `PATCH /api/v1/recipes/{id}/archive`.<br>  + `POST /api/v1/recipes/{id}/images`, `PATCH /api/v1/recipes/{id}/images/{imgId}`, `DELETE /api/v1/recipes/{id}/images/{imgId}`.<br>  + `GET /api/v1/resources/images/{**key}`.<br>  + `GET /health`, `GET /health/live`, `GET /health/ready`.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% tất cả các API endpoints). | [Program.cs](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/backend/CulinaryBlog.API/Program.cs) | **100%** |
| **2** | **[Tác vụ nền Resize ảnh & Media Proxy (Task D2, D23, D27)] Tích hợp Hangfire Background Worker & Endpoint phục vụ ảnh an toàn**<br><br>_**Đã hoàn thành:**_<br>- Triển khai Hangfire xử lý tác vụ nền phi đồng bộ tự động tạo 2 phiên bản kích thước ảnh chuẩn (300×300 thumbnail và 800×600 detail).<br>- Cài đặt Media Proxy `GET /api/v1/resources/images/{**key}` (phương án PA-2) phục vụ ảnh an toàn trực tiếp từ Object Storage, khắc phục triệt để vấn đề CORS.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [PR #16](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/pull/16) | **100%** |
| **3** | **[Điều tra & Sửa lỗi Upload ảnh 500 (PR #19)] Chẩn đoán nguyên nhân gốc rễ và tự khắc phục lỗi 500 khi tải ảnh**<br><br>_**Đã hoàn thành:**_<br>- Điều tra nguyên nhân do thiếu cấu hình Object Storage và mật khẩu DB trên môi trường phát triển local.<br>- Bổ sung cấu hình an toàn, ghi log lỗi chuẩn RFC 7807 và kiểm tra tính nhất quán qua bộ test `DevConfigParityTests`.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [PR #19](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/pull/19) | **100%** |
| **4** | **[Nạp cấu hình môi trường tự động EnvFileLoader (PR #19)] Thiết kế module nạp file .env cho API và Tests**<br><br>_**Đã hoàn thành:**_<br>- Xây dựng `EnvFileLoader.cs` tự động nạp các biến từ file `.env` vào runtime mà không cần thao tác gán biến shell thủ công.<br>- Đảm bảo biến môi trường thật và cấu hình CI luôn có quyền ưu tiên cao nhất.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [EnvFileLoader.cs](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/backend/CulinaryBlog.API/EnvFileLoader.cs) | **100%** |
| **5** | **[Nghiệm thu QA Tích hợp Toàn diện 41/41 Endpoints (Task D6)] Kiểm thử tích hợp Frontend Next.js và Backend .NET**<br><br>_**Đã hoàn thành:**_<br>- Thực hiện kiểm thử toàn bộ 41 trường hợp tích hợp giữa Frontend và Backend, ghi nhận tại tài liệu `TEST_CASE_TICH_HOP_FE_BE.md`.<br>- Kết quả: Đạt 41/41 trường hợp PASS 100%, bảo đảm không còn bất kỳ endpoint nào bị lỗi kết nối hay lỗi 500.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [Báo cáo QA](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/docs/evidence/TV4/Tuan03/Report/TEST_CASE_TICH_HOP_FE_BE.md) | **100%** |

---

### Xác nhận hoàn thành

- Toàn bộ nội dung công việc của **Tuần 4 (Lab 04)** của thành viên **Nguyễn Hữu Trung Sơn (TV4 - 2312739)** đã hoàn thành 100%.
- File báo cáo nộp bài theo chuẩn: `Lab4_2312739_NguyenHuuTrungSon.docx` đã được tạo tại thư mục `~/Downloads/` và lưu trữ tại `docs/evidence/TV4/Lab4_2312739_NguyenHuuTrungSon.docx`.
