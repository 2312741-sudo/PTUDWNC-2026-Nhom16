# BÁO CÁO LAB - MÔN PHÁT TRIỂN ỨNG DỤNG WEB NÂNG CAO

- **Lab**: **06 (Tuần 6 — Kiểm Thử Hồi Quy Toàn Diện, Nghiệm Thu 24/24 Kỹ Năng K01–K24 Cho 4 Thành Viên, Kịch Bản Demo Phân Hệ & Bàn Giao Release v1.0.0)**
- **Từ ngày**: **08/10/2026** **đến ngày**: **14/10/2026**
- **MSSV**: **2312741**
- **Họ và tên**: **Nguyễn Thanh Tâm**
- **Nhóm**: **16** (Nhóm trưởng — TV1)
- **Tên file nộp theo quy định**: `Lab6_2312741_NguyenThanhTam.docx` *(kèm file dự phòng `Lab06_2312741_NguyenThanhTam.docx`)*

---

### Bảng công việc thực hiện (Lab 06 / Tuần 6)

| STT | Công việc được giao | Liên kết đến github branch / PR / file | Tiến độ % |
|:---:|---|---|:---:|
| **1** | **[Kiểm thử hồi quy Backend] Chạy toàn diện 446 bài test tự động backend bảo đảm 100% Green**<br><br>_**Đã hoàn thành:**_<br>- Kiểm thử bộ test chính `tests/CulinaryBlog.Tests/CulinaryBlog.Tests.csproj`: 441/441 bài test tự động vượt qua 100% trong thời gian 2m55s.<br>- Kiểm thử bộ test tương tranh `tests/concurrency-spike/ConcurrencySpike.csproj`: 5/5 bài test tương tranh vượt qua 100%.<br>- Tổng số bài test backend đạt **446/446 tests PASS 100%** (0 Failed, 0 Skipped).<br>- Duy trì Code Coverage tầng `CulinaryBlog.Application` đạt **96.17%** (vượt xa chỉ tiêu 80%).<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [CulinaryBlog.Tests](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/tests/CulinaryBlog.Tests) | **100%** |
| **2** | **[Kiểm thử hồi quy Frontend] Chạy 86 bài test Jest và biên dịch Next.js 15 App Router production sạch sẽ**<br><br>_**Đã hoàn thành:**_<br>- Chạy Jest test suite trong `src/frontend`: Đạt **15/15 test suites passed**, **86/86 tests passed 100%**.<br>- Biên dịch `npm run build` cho Next.js 15.1.11 thành công toàn bộ **16/16 routes tĩnh và động** mà không gặp lỗi typecheck hay compilation nào.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [src/frontend](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/frontend) | **100%** |
| **3** | **[Chuẩn hóa mã nguồn & Rà soát bảo mật] Xác thực định dạng code và kiểm tra rò rỉ secret**<br><br>_**Đã hoàn thành:**_<br>- Chạy `dotnet format CulinaryBlog.sln --verify-no-changes --no-restore`: Exit code 0, không có vi phạm formatting nào trong toàn bộ solution.<br>- Chạy `bash deploy/scan-secrets.sh`: Kết quả `OK: không phát hiện secret bị gửi vào repo.`<br>- Chạy `bash deploy/scan-secrets.test.sh`: Đạt 9/9 pass, 0 fail.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [deploy/scan-secrets.sh](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/deploy/scan-secrets.sh) | **100%** |
| **4** | **[Nghiệm thu 24/24 Kỹ năng] Rà soát và đánh giá ma trận K01–K24 cho toàn bộ 4 thành viên**<br><br>_**Đã hoàn thành:**_<br>- Rà soát mã nguồn, cấu hình và bài test của TV1, TV2, TV3, TV4 đối chiếu 24 nhóm kỹ năng.<br>- Xác nhận cả 4 thành viên đạt **24/24 nhóm kỹ năng** có code/config và test kiểm chứng thực tế.<br>- Review và merge các PR chốt Tuần 6: PR #34, PR #35 (TV3), PR #36 (TV4), PR #31 (TV2) vào nhánh chính `main`.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [PHAN_CHIA_CONG_VIEC_6_TUAN.md](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md) | **100%** |
| **5** | **[Kịch bản Demo phân hệ Tài khoản & Hồ sơ] Xây dựng và diễn tập kịch bản demo 15–20 phút**<br><br>_**Đã hoàn thành:**_<br>- Thiết lập kịch bản 5 phần hoàn chỉnh: (1) Kiến trúc & cấu hình PBKDF2 HMAC-SHA512 + JWT HS512; (2) Luồng đăng ký, đăng nhập & chống user enumeration; (3) Dashboard hồ sơ, đổi mật khẩu & thu hồi token đa phiên; (4) Chống brute-force lockout (5 lần) + Rate limit + XSS; (5) Diễn tập khôi phục sự cố dữ liệu BCP/DR SHA-256 (1.8s).<br>- Diễn tập chạy thử nghiệm thành công 100% trên môi trường local và staging.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [docs/evidence/TV1/TUAN_6.md](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/docs/evidence/TV1/TUAN_6.md) | **100%** |
| **6** | **[Bàn giao đồ án Release v1.0.0] Đóng gói hồ sơ tài liệu và cập nhật README.md**<br><br>_**Đã hoàn thành:**_<br>- Cập nhật toàn diện `README.md` với tiến độ nghiệm thu Cổng G7, bảng số liệu kiểm thử chính xác, hướng dẫn chạy app và hướng dẫn chạy demo cho Giảng viên.<br>- Cập nhật tiến độ 4 thành viên trong `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md`.<br>- Hoàn tất bộ tài liệu dự án gồm SRS v1.1.1, ERD, 6 Hợp đồng kỹ thuật, 8 ADRs, 2 Runbooks vận hành.<br><br>_**Chưa hoàn thành:**_<br>- Không có (đã hoàn thành 100% yêu cầu). | [README.md](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/README.md) | **100%** |

---

### Xác nhận hoàn thành

- Toàn bộ **6/6 nội dung công việc của Tuần 6 (Lab 06)** của thành viên **Nguyễn Thanh Tâm (Nhóm trưởng — TV1 / 2312741)** đã hoàn thành 100%.
- Hệ thống đã đáp ứng đầy đủ và vượt mức các chỉ tiêu nghiệm thu của **Cổng G7**: 446/446 bài test backend pass, 86/86 bài test frontend Jest pass, biên dịch Next.js 16 routes thành công, định dạng mã nguồn chuẩn mực, bảo mật không rò rỉ secret, 4 thành viên đủ 24/24 K-skills và bàn giao Release v1.0.0 hoàn chỉnh.
- File báo cáo nộp bài theo chuẩn: `Lab6_2312741_NguyenThanhTam.docx` (kèm file dự phòng `Lab06_2312741_NguyenThanhTam.docx`).
