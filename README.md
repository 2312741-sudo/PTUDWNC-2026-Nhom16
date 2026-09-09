# Phát triển Ứng dụng Web Nâng cao (PTUDWNC)

Repository lưu trữ và quản lý mã nguồn bài tập, dự án thực hành môn học **Phát triển Ứng dụng Web Nâng cao**.

---

## 👥 Phân chia nhánh nhóm (Branching Convention)

Mỗi thành viên làm việc trên một nhánh riêng biệt theo định dạng:
`[MSSV]_[HoVaTen]_[VaiTro]`

- **Nhánh chính (`main`)**: Chứa khung dự án nền tảng và tích hợp chung.
- **Trưởng nhóm**: `2312741_NguyenThanhTam_Leader`
- **Các thành viên**: `<MSSV>_<HoTen>_Member`

### 📌 Quy trình làm việc với Git cho các thành viên

1. **Clone repository về máy**:
   ```bash
   git clone https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16.git
   cd PTUDWNC
   ```
2. **Tạo và chuyển sang nhánh riêng từ `main`**:
   ```bash
   git checkout main
   git pull origin main
   git checkout -b <MSSV>_<HoVaTen>_<VaiTro>
   ```
3. **Commit và push lên nhánh cá nhân**:
   ```bash
   git add .
   git commit -m "feat(chuong-01): hoàn thành bài tập ..."
   git push -u origin <MSSV>_<HoVaTen>_<VaiTro>
   ```

---

## 📁 Danh mục bài tập các chương

- **Chương 1**: `Chuong_01/` — Kiến trúc Web hiện đại & Thiết kế RESTful API (Culinary Blog API .NET 10 Clean Architecture)
  - 📄 Báo cáo chi tiết: [`Chuong_01/Bao_cao_Bai_tap_Chuong_01.docx`](Chuong_01/Bao_cao_Bai_tap_Chuong_01.docx)
  - 📖 Hướng dẫn chạy và tài liệu API: [`Chuong_01/README.md`](Chuong_01/README.md)
  - 🧪 Kết quả kiểm thử 27 test cases: [`Chuong_01/docs/Ket_qua_kiem_thu.md`](Chuong_01/docs/Ket_qua_kiem_thu.md)
  - 📋 Bản đặc tả OpenAPI: [`Chuong_01/docs/openapi.v1.json`](Chuong_01/docs/openapi.v1.json)
