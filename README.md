# Phát triển Ứng dụng Web Nâng cao (PTUDWNC)

Repository lưu trữ và quản lý mã nguồn bài tập, dự án thực hành môn học **Phát triển Ứng dụng Web Nâng cao**.

---

## 👥 Thành viên nhóm & Phân chia nhánh (Branching Convention)

Mỗi thành viên làm việc trên một nhánh riêng biệt theo định dạng:
`[MSSV]_[HoVaTen]_[VaiTro]`

| STT | Họ và Tên | MSSV | Vai trò | Tên nhánh Git |
|:---:|:---|:---:|:---:|:---|
| 1 | **Nguyễn Thành Tâm** | `2312741` | Leader | `2312741_NguyenThanhTam_Leader` |
| 2 | *(Thành viên 2)* | ... | Member | `<MSSV>_<HoTen>_Member` |
| 3 | *(Thành viên 3)* | ... | Member | `<MSSV>_<HoTen>_Member` |

### 📌 Quy trình làm việc với Git cho các thành viên

1. **Clone repository về máy**:
   ```bash
   git clone https://github.com/2312741-sudo/PTUDWNC.git
   cd PTUDWNC
   ```

2. **Tạo và chuyển sang nhánh riêng của mình từ `main`**:
   ```bash
   git checkout main
   git pull origin main
   git checkout -b <MSSV>_<HoVaTen>_<VaiTro>
   ```

3. **Làm việc và commit code**:
   ```bash
   git add .
   git commit -m "feat(chuong-01): hoàn thành bài tập ..."
   ```

4. **Push lên nhánh cá nhân trên GitHub**:
   ```bash
   git push -u origin <MSSV>_<HoVaTen>_<VaiTro>
   ```

---

## 📁 Cấu trúc thư mục dự án

```text
PTUDWNC/
├── .gitignore
├── README.md
├── Chuong_01/                  # Chương 1: Kiến trúc Web hiện đại & Thiết kế RESTful API
│   ├── CulinaryBlog.slnx       # Solution file .NET 10
│   ├── CulinaryBlog.http       # HTTP Request tests
│   ├── docs/                   # Tài liệu thiết kế API & thảo luận
│   ├── src/
│   │   ├── CulinaryBlog.Domain/         # Entities, Enums, Exceptions
│   │   ├── CulinaryBlog.Application/    # CQRS (MediatR), DTOs, Mappings (Mapster)
│   │   ├── CulinaryBlog.Infrastructure/ # EF Core, PostgreSQL DbContext, Migrations
│   │   └── CulinaryBlog.API/            # Minimal APIs, Scalar API Reference
│   └── ...
├── Chuong_02/                  # (Dự kiến các chương tiếp theo)
└── ...
```

---

## 🚀 Hướng dẫn khởi chạy Chương 1 (Culinary Blog API)

### 1. Yêu cầu môi trường
- **.NET 10 SDK** trở lên
- **PostgreSQL** đang chạy trên port `5432`
- Tạo database tên `culinary_blog`

### 2. Cấu hình chuỗi kết nối
Kiểm tra chuỗi kết nối trong `Chuong_01/src/CulinaryBlog.API/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=culinary_blog;Username=postgres;Password=yourpassword"
  }
}
```

### 3. Chạy ứng dụng
```bash
cd Chuong_01/src/CulinaryBlog.API
dotnet run
```

### 4. Truy cập tài liệu tương tác Scalar API
Mở trình duyệt truy cập:
```
https://localhost:5001/scalar/v1
# hoặc cổng HTTP tương ứng nếu chạy không SSL (vd: http://localhost:5000/scalar/v1)
```
