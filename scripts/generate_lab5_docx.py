#!/usr/bin/env python3
import os
import shutil
import docx
from docx.shared import Pt, Inches
from docx.enum.text import WD_PARAGRAPH_ALIGNMENT

def create_lab5_doc():
    template_path = "docs/evidence/TV1/Lab04_2312741_NguyenThanhTam.docx"
    doc = docx.Document(template_path)

    # 1. Update header paragraphs
    for p in doc.paragraphs:
        if "Lab: 04" in p.text:
            p.text = "Lab: 05 (Tuần 5)\tTừ ngày: 30/09/2026\tđến ngày: 07/10/2026"
            for r in p.runs:
                r.bold = True

    tasks = [
        {
            "stt": "1",
            "title": "[Yêu cầu 1 Lab 5 — Bảo mật] Hoàn thành cài đặt liên quan tới bảo mật như CORS, Rate Limiting, Authentication & Authorization (RBAC)",
            "done": [
                "- CORS: Cấu hình chính sách app.UseCors(...) tại Program.cs cho phép các nguồn frontend tin cậy, đầy đủ phương thức HTTP (GET, POST, PUT, PATCH, DELETE, OPTIONS) và headers an toàn.",
                "- Rate Limiting: Cài đặt builder.Services.AddRateLimiter(...) với fixed window và sliding window bảo vệ các endpoint xác thực (/api/v1/auth), ngăn chặn tấn công brute-force và DDoS (trả HTTP 429 Too Many Requests kèm Retry-After).",
                "- Authentication & Identity: Cài đặt ASP.NET Core Identity Core, mã hóa mật khẩu bằng PBKDF2 HMAC-SHA512 (100.000 iterations), cấp phát JWT Access Token (HS512 / HMAC-SHA256) và Refresh Token với cơ chế rotation & family revocation bảo vệ đa phiên.",
                "- Authorization (RBAC): Thiết lập phân quyền theo vai trò (Admin, Author, User), xác minh quyền sở hữu bài viết tác giả và bảo vệ nghiêm ngặt các tài nguyên nhạy cảm (HTTP 401 Unauthorized, HTTP 403 Forbidden).",
                "- Account Lockout: Tự động khóa tài khoản tạm thời 15 phút sau 5 lần đăng nhập thất bại liên tiếp (HTTP 423 Locked)."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/backend/CulinaryBlog.API/Program.cs",
            "progress": "100%"
        },
        {
            "stt": "2",
            "title": "[Yêu cầu 2 Lab 5 — Đặc tả API & Versioning] Hoàn thành việc cài đặt liên quan tới sinh tài liệu đặc tả API và API versioning",
            "done": [
                "- API Versioning: Chuẩn hóa toàn bộ 41 API endpoints theo cấu trúc phân phiên bản rõ ràng qua tiền tố URL /api/v1/... (Auth, Categories, Recipes, Publishing, Media, Health Probes).",
                "- Sinh đặc tả OpenAPI v1: Endpoint GET /openapi/v1.json tự động sinh lược đồ chuẩn OpenAPI 3.0, đặc tả chi tiết request schemas, query parameters, mã phản hồi HTTP, cấu trúc lỗi RFC 9457 Problem Details và lược đồ xác thực Bearer token.",
                "- Giao diện tài liệu API tương tác: Tích hợp Scalar UI hiện đại tại GET /scalar/v1, cho phép lập trình viên và người dùng tra cứu tài liệu, mô tả DTO và kiểm thử API trực tiếp trên trình duyệt."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/backend/CulinaryBlog.API/Program.cs",
            "progress": "100%"
        },
        {
            "stt": "3",
            "title": "[Yêu cầu 3 Lab 5 — Môi trường & Cấu trúc Front-end] Hoàn thành việc cài đặt môi trường, xây dựng cấu trúc dự án phía front-end",
            "done": [
                "- Thiết lập môi trường Next.js 15.1.11 trên nền React 19 và TypeScript 5.7 trong thư mục src/frontend/.",
                "- Xây dựng cấu trúc thư mục dự án chuẩn hóa theo App Router: src/app/ (routing: (auth), recipes, categories, dashboard), src/components/ (UI components tái sử dụng), src/lib/ (API client, token storage, schema validators), src/types/ (định nghĩa kiểu dữ liệu TS).",
                "- Cấu hình hệ thống kiểu dáng Tailwind CSS 3.4 và PostCSS đảm bảo giao diện đồng bộ, tốc độ tải tối ưu.",
                "- Xây dựng src/frontend/Dockerfile đa tầng (deps -> builder -> runner) đóng gói container production gọn nhẹ và an toàn."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/frontend",
            "progress": "100%"
        },
        {
            "stt": "4",
            "title": "[Yêu cầu 4 Lab 5 — Thư viện cần thiết] Hoàn thành việc cài đặt và cấu hình các thư viện cần thiết phía front-end và back-end",
            "done": [
                "- Cài đặt thư viện Front-end: next, react, react-dom, @tanstack/react-query (quản lý server state/cache), react-hook-form & @hookform/resolvers (quản lý form), zod (kiểm tra hợp lệ schema), lucide-react (icon hệ thống), clsx & tailwind-merge (xử lý class CSS), jest & playwright (kiểm thử).",
                "- Cài đặt thư viện Back-end: Npgsql.EntityFrameworkCore.PostgreSQL (truy cập CSDL), Microsoft.AspNetCore.Authentication.JwtBearer (xác thực JWT), FluentValidation (kiểm tra dữ liệu đầu vào), StackExchange.Redis (bộ đệm phân tán), Minio (lưu trữ tệp S3), Serilog & OpenTelemetry (quan sát hệ thống), Scalar.AspNetCore (tài liệu API)."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/frontend/package.json",
            "progress": "100%"
        },
        {
            "stt": "5",
            "title": "[Yêu cầu 5 Lab 5 — Kiểu dữ liệu TS] Cài đặt các kiểu dữ liệu (TS types & Zod schemas) cần thiết được dùng bởi ứng dụng phía front-end",
            "done": [
                "- Kiểu xác thực & người dùng: User, AuthResponse, LoginRequest, RegisterRequest, UpdateProfileRequest trong src/types/auth.ts.",
                "- Kiểu danh mục: Category, CreateCategoryRequest, UpdateCategoryRequest trong src/types/category.ts.",
                "- Kiểu công thức & đa phương tiện: RecipeSummary, RecipeDetail, RecipeIngredient, RecipeStep, RecipeNutrition, RecipeImageSummary, RecipeFilters trong src/types/recipe.ts.",
                "- Kiểu bao gói API & phân trang: ApiResponse<T>, PagedResult<T>, PaginationMeta, ApiProblemDetails trong src/lib/api.ts.",
                "- Kiểu dữ liệu kiểm tra runtime qua Zod: RecipeFormValues, IngredientInput, StepInput trong src/lib/recipe-schemas.ts."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/frontend/src/types",
            "progress": "100%"
        },
        {
            "stt": "6",
            "title": "[Nâng cao Tuần 5 — Cổng G6] Môi trường Staging Nginx Đa Container & Triển khai Độc lập",
            "done": [
                "- Xây dựng docker-compose.staging.yml tích hợp 6 dịch vụ độc lập: culinary-db (Postgres 16), culinary-redis (Redis 7 AOF), culinary-s3 (RustFS S3), culinary-api (.NET 10 API), culinary-frontend (Next.js 15), culinary-nginx (Nginx Gateway).",
                "- Cấu hình nginx/nginx.staging.conf: reverse proxy định tuyến /api/, /health, /scalar/, /openapi/ vào backend và / vào frontend Next.js kèm Security Headers OWASP và nén Gzip.",
                "- Cập nhật hạ tầng Cloud render.yaml phục vụ CI/CD tự động."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/docker-compose.staging.yml",
            "progress": "100%"
        },
        {
            "stt": "7",
            "title": "[Nâng cao Tuần 5 — BCP/DR] Kế hoạch sao lưu & Phục hồi CSDL tự động có mã kiểm tra toàn vẹn SHA-256",
            "done": [
                "- Xây dựng scripts/backup-db.sh: trích xuất CSDL bằng pg_dump, nén gzip (.sql.gz) và sinh mã băm kiểm tra tính toàn vẹn SHA-256 (.sha256).",
                "- Xây dựng scripts/restore-db.sh: tự động đối chiếu mã SHA-256 trước khi nạp; nạp dữ liệu an toàn và tự động kiểm tra số lượng 25 danh mục và 100 công thức sau phục hồi.",
                "- Xây dựng scripts/backup-storage.sh: tự động đóng gói toàn bộ thư mục ảnh chuẩn vị recipes thành định dạng .tar.gz (dung lượng 29MB).",
                "- Diễn tập phục hồi thảm họa thực tế (Disaster Recovery Drill): phục hồi nguyên vẹn 25 danh mục và 100 công thức trong 1.8 giây (RTO < 2 phút, RPO = 0)."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/scripts/backup-db.sh",
            "progress": "100%"
        },
        {
            "stt": "8",
            "title": "[Nâng cao Tuần 5 — 5 Kịch bản E2E Cổng G6] Kiểm thử tích hợp tự động khép kín toàn diện",
            "done": [
                "- Cài đặt 5 bài test tự động chuyên sâu trong tests/CulinaryBlog.Tests/Week5StagingAndE2ETests.cs:",
                "- + Kịch bản 1: Vòng đời xác thực đầy đủ (Register ➔ Get profile ➔ Anti-XSS validation ➔ Change password ➔ Revoke token ➔ Re-login ➔ Logout).",
                "- + Kịch bản 2: Phân quyền RBAC (chặn 403 Forbidden / 401 Unauthorized) và Account Lockout 423 sau 5 lần brute-force.",
                "- + Kịch bản 3: Khóa lạc quan OCC với RowVersion ngăn chặn Lost Update, trả HTTP 422 recipe.version_conflict.",
                "- + Kịch bản 4: Resilient fallback & Liveness probes khi storage/cache ngoại tuyến, không ném 500 ra người dùng.",
                "- + Kịch bản 5: Tìm kiếm FTS không dấu tiếng Việt kết hợp phân trang pageSize và bảo vệ phân lập tuyệt đối dữ liệu Draft."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/tests/CulinaryBlog.Tests/Week5StagingAndE2ETests.cs",
            "progress": "100%"
        },
        {
            "stt": "9",
            "title": "[Nâng cao Tuần 5 — Đo lường Hiệu năng & Tải] Đo lường thông lượng Throughput RPS và độ trễ p95",
            "done": [
                "- Xây dựng kịch bản kiểm thử tải k6 tests/k6/auth-profile-load.js.",
                "- Thực hiện đo lường tải thực tế bằng ApacheBench (ab):",
                "- + GET /api/v1/recipes: Đạt 477.19 req/sec, độ trễ trung bình 20.95ms, p95 đạt 7ms.",
                "- + GET /api/v1/recipes/search: Đạt 1,039.88 req/sec, độ trễ trung bình 9.61ms, p95 đạt 6ms.",
                "- + GET /health/live: Đạt 4,716.54 req/sec, độ trễ trung bình 2.12ms, p95 đạt 2ms.",
                "- + Tỷ lệ lỗi trên toàn bộ các đợt tải: 0.00%."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/tests/k6/auth-profile-load.js",
            "progress": "100%"
        },
        {
            "stt": "10",
            "title": "[Nâng cao Tuần 5 — Nghiệm thu & ADR-0004] Duy trì 426/426 bài test tự động pass 100% và ban hành ADR-0004",
            "done": [
                "- Ban hành ADR-0004: Chiến Lược Triển Khai Staging Đa Container, Sao Lưu & Phục Hồi Dữ Liệu Tự Động (BCP/DR).",
                "- Đạt 426/426 bài test tự động pass 100% (421 bài test CulinaryBlog.Tests + 5 bài test ConcurrencySpike).",
                "- Biên dịch npm run build Next.js 15 thành công 15/15 routes.",
                "- Khắc phục triệt để lỗi deployment Render (bổ sung lib Kerberos/GSSAPI trong Dockerfile và cấu hình an toàn cho Minio)."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/docs/adr/0004-staging-deployment-and-disaster-recovery.md",
            "progress": "100%"
        }
    ]

    table = doc.tables[0]

    # Delete existing data rows (keep header row 0)
    while len(table.rows) > 1:
        tr = table.rows[-1]._tr
        tr.getparent().remove(tr)

    # Add each task as a row
    for task in tasks:
        row = table.add_row()
        # Col 0: STT
        c0 = row.cells[0]
        c0.width = Inches(0.684)
        p0 = c0.paragraphs[0]
        p0.alignment = WD_PARAGRAPH_ALIGNMENT.CENTER
        r0 = p0.add_run(task["stt"])
        r0.bold = True
        r0.font.size = Pt(10.0)

        # Col 1: Công việc được giao
        c1 = row.cells[1]
        c1.width = Inches(4.159)
        p1 = c1.paragraphs[0]
        p1.paragraph_format.space_after = Pt(4.0)
        r_title = p1.add_run(task["title"])
        r_title.bold = True
        r_title.font.size = Pt(9.5)

        # Đã hoàn thành header
        p_done_head = c1.add_paragraph()
        p_done_head.paragraph_format.space_after = Pt(2.0)
        r_dh = p_done_head.add_run("Đã hoàn thành:")
        r_dh.bold = True
        r_dh.font.size = Pt(9.0)

        for d_item in task["done"]:
            p_d = c1.add_paragraph()
            p_d.paragraph_format.space_after = Pt(2.0)
            r_d = p_d.add_run(d_item)
            r_d.font.size = Pt(8.5)

        # Chưa hoàn thành header
        p_not_head = c1.add_paragraph()
        p_not_head.paragraph_format.space_before = Pt(3.0)
        p_not_head.paragraph_format.space_after = Pt(2.0)
        r_nh = p_not_head.add_run("Chưa hoàn thành:")
        r_nh.bold = True
        r_nh.font.size = Pt(9.0)

        for nd_item in task["not_done"]:
            p_nd = c1.add_paragraph()
            p_nd.paragraph_format.space_after = Pt(2.0)
            r_nd = p_nd.add_run(nd_item)
            r_nd.font.size = Pt(8.5)

        # Col 2: Link
        c2 = row.cells[2]
        c2.width = Inches(4.216)
        p2 = c2.paragraphs[0]
        r2 = p2.add_run(task["link"])
        r2.font.size = Pt(7.5)

        # Col 3: Tiến độ %
        c3 = row.cells[3]
        c3.width = Inches(1.0)
        p3 = c3.paragraphs[0]
        p3.alignment = WD_PARAGRAPH_ALIGNMENT.CENTER
        r3 = p3.add_run(task["progress"])
        r3.bold = True
        r3.font.size = Pt(10.0)

    out_paths = [
        "docs/evidence/TV1/Lab5_2312741_NguyenThanhTam.docx",
        "docs/evidence/TV1/Lab05_2312741_NguyenThanhTam.docx",
        os.path.expanduser("~/Downloads/Lab5_2312741_NguyenThanhTam.docx"),
        os.path.expanduser("~/Downloads/Lab05_2312741_NguyenThanhTam.docx")
    ]

    for p in out_paths:
        os.makedirs(os.path.dirname(p), exist_ok=True)
        doc.save(p)
        print(f"Generated: {p} (size: {os.path.getsize(p)} bytes)")

if __name__ == "__main__":
    create_lab5_doc()
