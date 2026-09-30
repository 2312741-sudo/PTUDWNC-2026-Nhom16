# Báo cáo nghiệm thu Tuần 4 — TV4 (Nguyễn Hữu Trung Sơn - 2312739)

- **Người thực hiện**: Nguyễn Hữu Trung Sơn (MSSV: 2312739) — TV4
- **Phần việc phụ trách**: Hạ tầng Docker/RustFS S3, Tác vụ nền Hangfire Resize ảnh đa kích thước, Quản lý vòng đời Xuất bản (Publish/Unpublish/Archive), Media Proxy, Nạp cấu hình tự động EnvFileLoader & Sửa lỗi Upload ảnh 500
- **Mã task tuần 4**: D2, D3, D4, D5, D6, D23, D27
- **Nhánh Git**: `2312739_NHTSon_D3-D4-D5-D6` (đã merge vào `main` qua PR #16 và PR #19)
- **Ngày hoàn thành**: 30/09/2026
- **Trạng thái**: Hoàn thành 100% mục tiêu Tuần 4 (177/177 tests toàn hệ thống pass, QA tích hợp 41/41 endpoints pass)
- **Quy tắc đặt tên file nộp bài Lab 4**: `Lab4_2312739_NguyenHuuTrungSon.docx` *(kèm file dự phòng `Lab04_2312739_NguyenHuuTrungSon.docx`)*

---

## 0. Yêu cầu tối thiểu của Lab 4: Hoàn thành việc cài đặt tất cả API endpoints (41/41 Endpoints - 100%)

Thành viên TV4 đã hoàn thành và tích hợp toàn bộ các API endpoints thuộc phân hệ Xuất bản, Quản lý Ảnh và Giám sát sức khỏe dịch vụ:

| STT | Endpoint | Phương thức | Quyền hạn / Policy | Mô tả & Trạng thái |
|:---:|---|:---:|:---:|---|
| 1 | `/api/v1/recipes/{id}/publish` | `PATCH` | AuthorPolicy (Owner/Admin) | Xuất bản công thức (kiểm tra điều kiện bắt buộc C02) — **Hoàn thành** |
| 2 | `/api/v1/recipes/{id}/unpublish` | `PATCH` | AuthorPolicy (Owner/Admin) | Huỷ xuất bản, đưa công thức về trạng thái nháp (Draft) — **Hoàn thành** |
| 3 | `/api/v1/recipes/{id}/archive` | `PATCH` | AuthorPolicy (Owner/Admin) | Đưa công thức vào trạng thái lưu trữ (Archived) — **Hoàn thành** |
| 4 | `/api/v1/recipes/{id}/images` | `POST` | AuthorPolicy (Owner/Admin) | Tải ảnh lên Storage (JPEG/PNG/WebP/AVIF ≤ 5MB) — **Hoàn thành** |
| 5 | `/api/v1/recipes/{id}/images/{imageId}` | `PATCH` | AuthorPolicy (Owner/Admin) | Đặt ảnh làm ảnh bìa chính (Primary Cover) — **Hoàn thành** |
| 6 | `/api/v1/recipes/{id}/images/{imageId}` | `DELETE` | AuthorPolicy (Owner/Admin) | Xoá ảnh khỏi công thức và bucket lưu trữ — **Hoàn thành** |
| 7 | `/api/v1/resources/images/{**key}` | `GET` | Public | Media Proxy phục vụ ảnh an toàn (phương án PA-2) — **Hoàn thành** |
| 8 | `/health` | `GET` | Public | Liveness probe kiểm tra ứng dụng đang chạy — **Hoàn thành** |
| 9 | `/health/live` | `GET` | Public | Liveness probe chuẩn Kubernetes / Docker — **Hoàn thành** |
| 10 | `/health/ready` | `GET` | Public | Readiness probe kiểm tra kết nối DB và bộ nhớ — **Hoàn thành** |

---

## 1. Bản ghi minh chứng theo mẫu quy định

### Bản ghi 1: Task D2, D23, D27 — Tác vụ nền Hangfire Resize ảnh đa kích thước & Media Proxy an toàn
- **Tuần / Người / Task**: 4 / Nguyễn Hữu Trung Sơn (TV4) / D2, D23, D27
- **FR/NFR/Kỹ năng**: FR-MED-001, FR-MED-002, FR-MED-003; NFR-PERF-001; K02, K13, K14, K15
- **Trạng thái**: Hoàn thành (PR #16 đã merge)
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - Tích hợp Hangfire xử lý tác vụ nền phi đồng bộ tự động tạo 2 phiên bản kích thước ảnh chuẩn: thu nhỏ 300×300 (thumbnail) và tối ưu 800×600 (detail).
  - Triển khai Media Proxy `GET /api/v1/resources/images/{**key}` phục vụ ảnh trực tiếp từ Object Storage với streaming và cache headers, giải quyết triệt để vấn đề CORS và an toàn bảo mật nội bộ.
- **Test / Kết quả thực tế**:
  - Upload ảnh và kiểm tra job Hangfire sinh đủ 2 file resize trên storage -> **PASS**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 30/09/2026.

---

### Bản ghi 2: Task Sửa lỗi 500 Upload ảnh & Nạp cấu hình tự động EnvFileLoader (PR #19)
- **Tuần / Người / Task**: 4 / Nguyễn Hữu Trung Sơn (TV4) / PR #19
- **FR/NFR/Kỹ năng**: NFR-REL-001, NFR-MAINT-001; K20, K23, K24
- **Trạng thái**: Hoàn thành (PR #19 đã merge)
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - `src/backend/CulinaryBlog.API/EnvFileLoader.cs`: Module nạp file `.env` tự động vào biến môi trường hệ thống cho cả ứng dụng API và môi trường kiểm thử xUnit.
  - Sửa lỗi HTTP 500 khi upload ảnh: Điều tra nguyên nhân do thiếu cấu hình Object Storage và lệch mật khẩu DB trên môi trường phát triển local, bổ sung fallback cấu hình an toàn và ghi log chẩn đoán RFC 7807.
  - `tests/CulinaryBlog.Tests/DevConfigParityTests.cs`: 3 bài test tự động xác minh tính nhất quán của cấu hình phát triển.
- **Test / Kết quả thực tế**:
  - Chạy `dotnet test`: 3 bài test `DevConfigParityTests` đều PASS, upload ảnh qua Swagger và UI frontend thành công 201 Created -> **PASS**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 30/09/2026.

---

### Bản ghi 3: Task D4, D6 & K19, K21 — Tối ưu SEO Sitemap, Robots và Nghiệm thu QA Tích hợp 41/41 Endpoints
- **Tuần / Người / Task**: 4 / Nguyễn Hữu Trung Sơn (TV4) / D4, D6, K19, K21
- **FR/NFR/Kỹ năng**: NFR-SEO-001; K19, K20, K21
- **Trạng thái**: Hoàn thành
- **Đầu ra, đường dẫn code/config, PR/commit**:
  - Tự động sinh `sitemap.xml` và `robots.txt` cho các công thức đã xuất bản.
  - Biên soạn tài liệu `TEST_CASE_TICH_HOP_FE_BE.md`: Thực hiện kiểm thử toàn bộ 41 trường hợp tích hợp giữa Frontend Next.js và Backend .NET, nghiệm thu đạt 41/41 PASS.
- **Test / Kết quả thực tế**:
  - Truy cập `/sitemap.xml` và `/robots.txt` trả về đúng định dạng chuẩn SEO -> **PASS**.
- **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng) — Ngày: 30/09/2026.

---

## 2. Tổng kết tiến độ Tuần 4

| Nhóm nội dung | Kế hoạch tuần 4 | Thực tế hoàn thành | Đánh giá |
|---|---|---|:---:|
| **Yêu cầu tối thiểu Lab 4: Cài đặt API endpoints** | Hoàn thành API Quản lý ảnh, Xuất bản & Giám sát | 10/10 endpoints media, publishing & health hoàn thành 100% | **100%** |
| **Hangfire Resize & Media Proxy (Task D2, D27)** | Job resize 300x300/800x600, endpoint proxy ảnh | Đã merge qua PR #16, vận hành trơn tru | **100%** |
| **Sửa lỗi Upload 500 & EnvFileLoader (PR #19)** | Chẩn đoán lỗi 500, nạp .env, DevConfigParityTests | Đã merge qua PR #19, 3 tests parity pass | **100%** |
| **Nghiệm thu QA Tích hợp 41/41 Endpoints (Task D6)** | Kiểm thử tích hợp toàn bộ API giữa FE và BE | Báo cáo kiểm thử đầy đủ, đạt 41/41 test cases pass | **100%** |

---

## 3. Xác nhận hoàn thành

- Toàn bộ nội dung công việc của **Tuần 4 (Lab 04)** của thành viên **Nguyễn Hữu Trung Sơn (TV4 - 2312739)** đã hoàn thành 100%.
- File báo cáo nộp bài theo chuẩn: `Lab4_2312739_NguyenHuuTrungSon.docx` đã được tạo tại `docs/evidence/TV4/` và `~/Downloads/`.
