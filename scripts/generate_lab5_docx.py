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
    # Paragraph 2 is Lab info
    for p in doc.paragraphs:
        if "Lab: 04" in p.text:
            p.text = "Lab: 05 (Tuần 5)\tTừ ngày: 30/09/2026\tđến ngày: 07/10/2026"
            # format runs
            for r in p.runs:
                r.bold = True

    tasks = [
        {
            "stt": "1",
            "title": "[Cổng G6 — Triển khai Staging Nginx Multi-Container] Thiết lập môi trường Staging hoàn chỉnh với Docker Compose, Frontend Dockerfile và Gateway Nginx",
            "done": [
                "- Xây dựng cấu hình docker-compose.staging.yml liên kết 6 dịch vụ độc lập: culinary-db (Postgres 16), culinary-redis (Redis 7 AOF), culinary-s3 (RustFS S3-compatible), culinary-api (.NET 10 API), culinary-frontend (Next.js 15), culinary-nginx (Nginx Gateway).",
                "- Tạo src/frontend/Dockerfile đa tầng (multi-stage build: deps -> builder -> runner) tối ưu dung lượng và bảo mật container production.",
                "- Cấu hình nginx/nginx.staging.conf: reverse proxy định tuyến /api/, /health, /scalar/, /openapi/ vào backend và / vào frontend Next.js kèm Security Headers OWASP và gzip.",
                "- Cập nhật cấu hình cloud render.yaml phục vụ deploy tự động."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/docker-compose.staging.yml",
            "progress": "100%"
        },
        {
            "stt": "2",
            "title": "[Kế hoạch sao lưu & Khôi phục CSDL BCP/DR] Xây dựng script sao lưu và phục hồi CSDL PostgreSQL tự động có mã kiểm tra toàn vẹn SHA-256",
            "done": [
                "- Xây dựng scripts/backup-db.sh: trích xuất CSDL bằng pg_dump, nén gzip trực tiếp (.sql.gz) và tạo mã băm kiểm tra tính toàn vẹn SHA-256 (.sha256).",
                "- Xây dựng scripts/restore-db.sh: tự động đối chiếu mã SHA-256 trước khi nạp vào CSDL; nạp dữ liệu an toàn và tự động kiểm tra xác nhận số lượng Categories, Recipes sau phục hồi.",
                "- Thực hiện diễn tập phục hồi thảm họa thực tế (Disaster Recovery Drill): phục hồi nguyên vẹn 25 danh mục và 100 công thức trong 1.8 giây (RTO < 2 phút, RPO = 0)."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/scripts/backup-db.sh",
            "progress": "100%"
        },
        {
            "stt": "3",
            "title": "[Sao lưu kho lưu trữ Media & Tệp tĩnh] Tự động hóa đóng gói và sao lưu kho ảnh công thức",
            "done": [
                "- Xây dựng script scripts/backup-storage.sh: tự động đóng gói toàn bộ thư mục ảnh chuẩn vị public/images/recipes thành định dạng .tar.gz (dung lượng 29MB).",
                "- Đảm bảo dữ liệu media luôn sẵn sàng phục hồi đồng bộ cùng cơ sở dữ liệu quan hệ."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/scripts/backup-storage.sh",
            "progress": "100%"
        },
        {
            "stt": "4",
            "title": "[Kiểm thử E2E — Kịch bản 1 (Cổng G6)] Vòng đời xác thực và an toàn tài khoản toàn diện",
            "done": [
                "- Cài đặt bài test tự động E2E_Scenario_1_Full_Auth_Lifecycle_Profile_ChangePassword_And_Revocation trong Week5StagingAndE2ETests.cs.",
                "- Kiểm chứng luồng khép kín: Đăng ký tài khoản mới ➔ Lấy hồ sơ ➔ Chống XSS độc hại (400) & Cập nhật tên hợp lệ (200) ➔ Đổi mật khẩu thành công (204) ➔ Thu hồi toàn bộ Refresh Token của phiên cũ (401) ➔ Đăng nhập thành công bằng mật khẩu mới (200) ➔ Đăng xuất an toàn và vô hiệu hóa token (204)."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/tests/CulinaryBlog.Tests/Week5StagingAndE2ETests.cs",
            "progress": "100%"
        },
        {
            "stt": "5",
            "title": "[Kiểm thử E2E — Kịch bản 2 (Cổng G6)] Phân quyền RBAC, Chống dò quét Brute-Force & Account Lockout",
            "done": [
                "- Cài đặt bài test tự động E2E_Scenario_2_RBAC_Authorization_And_BruteForce_Lockout_Protection trong Week5StagingAndE2ETests.cs.",
                "- Kiểm chứng phân quyền: User/Author cố truy cập tài nguyên Admin bị từ chối 403 Forbidden; truy cập không kèm Bearer Token bị 401 Unauthorized.",
                "- Kiểm chứng phòng thủ Brute-force: Tự động khóa tài khoản (Account Lockout) sau 5 lần nhập sai liên tiếp."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/tests/CulinaryBlog.Tests/Week5StagingAndE2ETests.cs",
            "progress": "100%"
        },
        {
            "stt": "6",
            "title": "[Kiểm thử E2E — Kịch bản 3 (Cổng G6)] Toàn vẹn dữ liệu & Khóa lạc quan (OCC) ngăn chặn Lost Update",
            "done": [
                "- Cài đặt bài test tự động E2E_Scenario_3_Optimistic_Concurrency_Control_Prevents_Lost_Updates trong Week5StagingAndE2ETests.cs.",
                "- Kiểm chứng tình huống 2 tác giả cùng ghi đồng thời: Writer 1 cập nhật thành công làm thay đổi RowVersion / xmin; Writer 2 gửi cập nhật với token cũ bị từ chối ngay lập tức với HTTP 422 Unprocessable Entity kèm mã lỗi recipe.version_conflict."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/tests/CulinaryBlog.Tests/Week5StagingAndE2ETests.cs",
            "progress": "100%"
        },
        {
            "stt": "7",
            "title": "[Kiểm thử E2E — Kịch bản 4 (Cổng G6)] Khả năng phục hồi (Resilient Fallback) & Health Probes",
            "done": [
                "- Cài đặt bài test tự động E2E_Scenario_4_Resilient_Health_Probes_And_Graceful_Degradation trong Week5StagingAndE2ETests.cs.",
                "- Kiểm chứng Liveness Probe /health/live trả 200 OK; khi Redis hoặc RustFS ngoại tuyến, hệ thống ghi log cảnh báo và tự động fallback về CSDL PostgreSQL an toàn, không ném lỗi HTTP 500 ra người dùng."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu).`" if False else "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/tests/CulinaryBlog.Tests/Week5StagingAndE2ETests.cs",
            "progress": "100%"
        },
        {
            "stt": "8",
            "title": "[Kiểm thử E2E — Kịch bản 5 (Cổng G6)] Tìm kiếm FTS không dấu tiếng Việt, Bộ lọc AND & Phân lập dữ liệu Draft",
            "done": [
                "- Cài đặt bài test tự động E2E_Scenario_5_FTS_Vietnamese_Search_AND_Filter_And_Draft_Isolation trong Week5StagingAndE2ETests.cs.",
                "- Kiểm chứng FTS Trigram GIN index: từ khóa không dấu \"canh\" tìm thấy chính xác công thức \"Canh chua cá lóc\"; kết hợp phân trang pageSize; xác thực kết quả tìm kiếm công khai tuyệt đối không chứa công thức ở trạng thái Draft."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/tests/CulinaryBlog.Tests/Week5StagingAndE2ETests.cs",
            "progress": "100%"
        },
        {
            "stt": "9",
            "title": "[Đo lường Hiệu năng Tải & Benchmarking] Đo lường thông lượng Throughput RPS và độ trễ p95",
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
            "title": "[Review Task TV4 & Hồ sơ Quyết định Kiến trúc ADR-0004] Nghiệm thu kỹ thuật và duy trì 183/183 tests xanh",
            "done": [
                "- Rà soát và nghiệm thu các đề xuất B1, B2, B3 của TV4 (chuyển mã lỗi storage sang 503, fail-fast config, nạp .env qua DotNetEnv).",
                "- Viết và ban hành ADR-0004: Chiến Lược Triển Khai Staging Đa Container, Sao Lưu & Phục Hồi Dữ Liệu Tự Động (BCP/DR).",
                "- Đạt 183/183 bài test tự động pass 100% (178 tests CulinaryBlog.Tests + 5 tests ConcurrencySpike).",
                "- Biên dịch npm run build Next.js 15 thành công 15/15 routes."
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

    # Save to required destinations
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
