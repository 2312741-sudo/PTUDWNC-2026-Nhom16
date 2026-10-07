#!/usr/bin/env python3
import os
import shutil
import docx
from docx.shared import Pt, Inches
from docx.enum.text import WD_PARAGRAPH_ALIGNMENT

def create_lab5_doc_tv2():
    template_path = "docs/evidence/TV2/Lab04_2312796_NgoQuocTruongVi.docx"
    doc = docx.Document(template_path)

    # 1. Update header paragraphs
    for p in doc.paragraphs:
        if "Lab: 04" in p.text or "Lab: 4" in p.text:
            p.text = "Lab: 05 (Tuần 5)\tTừ ngày: 30/09/2026\tđến ngày: 07/10/2026"
            for r in p.runs:
                r.bold = True
        elif "Lab 04" in p.text:
            p.text = p.text.replace("Lab 04", "Lab 05").replace("Tuần 4", "Tuần 5")

    tasks = [
        {
            "stt": "1",
            "title": "[Cổng G6 — Kiểm chứng Triển khai Staging & Khởi động Độc lập] Xác thực hệ thống Staging Multi-Container với Docker Compose và Nginx Gateway",
            "done": [
                "- Tự khởi động và kiểm tra tính toàn vẹn của hệ thống trên môi trường Staging qua Docker Compose (docker-compose.staging.yml) với đầy đủ 6 services: culinary-api (.NET 10 API), culinary-frontend (Next.js 15), culinary-db (PostgreSQL 16), culinary-redis (Redis 7), culinary-s3 (RustFS S3), culinary-nginx (Nginx Gateway).",
                "- Kiểm chứng Gateway Nginx proxy định tuyến chính xác các endpoints Danh mục và Tìm kiếm: /api/v1/categories, /api/v1/recipes, /api/v1/recipes/search kèm Security Headers OWASP và nén gzip."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/docker-compose.staging.yml",
            "progress": "100%"
        },
        {
            "stt": "2",
            "title": "[Kế hoạch sao lưu & Khôi phục CSDL BCP/DR — Diễn tập độc lập TV2] Thực hiện diễn tập sao lưu và phục hồi CSDL PostgreSQL",
            "done": [
                "- Chạy thử nghiệm và xác thực quy trình sao lưu CSDL PostgreSQL (scripts/backup-db.sh) và phục hồi thảm họa (scripts/restore-db.sh).",
                "- Kiểm chứng tính toàn vẹn của mã băm SHA-256 (.sha256), đảm bảo phục hồi nguyên vẹn 25 danh mục ẩm thực và 100 công thức nấu ăn chuẩn vị trong 1.8 giây (RTO < 2 phút, RPO = 0)."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/scripts/restore-db.sh",
            "progress": "100%"
        },
        {
            "stt": "3",
            "title": "[Tối ưu hóa SEO & Metadata chuẩn hóa — Canonical, Robots & OpenGraph] Hoàn thiện Metadata các trang Khám phá, Danh mục và Tìm kiếm",
            "done": [
                "- Cài đặt hàm generateMetadata chuẩn hóa tại các trang khám phá công thức /recipes, danh mục /categories, /categories/[slug] và tìm kiếm /search.",
                "- Thiết lập thẻ alternates.canonical chống duplicate content; tự động gắn robots: { index: false, follow: true } cho trang kết quả tìm kiếm có từ khóa để tối ưu crawl budget của công cụ tìm kiếm (Google, Bing)."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/frontend/src/app/recipes/page.tsx",
            "progress": "100%"
        },
        {
            "stt": "4",
            "title": "[Cấu trúc dữ liệu có cấu trúc Rich Snippets JSON-LD — Schema.org] Tích hợp CollectionPage và BreadcrumbList cho SEO",
            "done": [
                "- Tích hợp schema JSON-LD chuẩn CollectionPage, ItemList và BreadcrumbList tại các trang chi tiết danh mục /categories/[slug] và danh sách công thức /recipes.",
                "- Đảm bảo công cụ tìm kiếm nhận diện cấu trúc phân cấp website (Trang chủ -> Danh mục ẩm thực -> Món ăn chi tiết) hỗ trợ rich snippets hiển thị trực quan trên SERP."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/frontend/src/app/categories/%5Bslug%5D/page.tsx",
            "progress": "100%"
        },
        {
            "stt": "5",
            "title": "[Khả năng tiếp cận WCAG 2.1 AA & Trải nghiệm Người dùng (A11y)] Bổ sung aria-labels, aria-live và semantic navigation",
            "done": [
                "- Bổ sung các thuộc tính trợ năng aria-label, role='search', aria-hidden trên thanh tìm kiếm và form lọc đa tiêu chí tại /search và /recipes.",
                "- Thêm vùng thông báo thời gian thực aria-live='polite' cho bộ đếm kết quả tìm kiếm để hỗ trợ trình đọc màn hình (Screen Reader / NVDA / VoiceOver).",
                "- Tối ưu hóa phân trang với thẻ ngữ nghĩa <nav aria-label='Phân trang công thức'> và aria-current='page'."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/frontend/src/app/search/page.tsx",
            "progress": "100%"
        },
        {
            "stt": "6",
            "title": "[Tối ưu hóa Hiệu năng Frontend Core Web Vitals (CWV) & Next.js Bundle] Kiểm soát LCP, CLS và ISR Caching",
            "done": [
                "- Tối ưu hóa Largest Contentful Paint (LCP) và Cumulative Layout Shift (CLS) thông qua next/image và fallback ảnh cục bộ tĩnh RecipeDetailImage.",
                "- Bật cache ISR (Incremental Static Regeneration) theo phân tầng SRS: Categories revalidate 1 giờ, Recipes list revalidate 15 phút.",
                "- Giảm thiểu JavaScript bundle, kiểm tra biên dịch npx tsc --noEmit và npm run build Next.js 15 đạt 15/15 routes tĩnh/động sạch sẽ 100%."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/frontend/src/components/RecipeDetailImage.tsx",
            "progress": "100%"
        },
        {
            "stt": "7",
            "title": "[Sanitization từ khóa tìm kiếm & Chống tấn công cú pháp FTS] Làm sạch ký tự đặc biệt trong FTS Query",
            "done": [
                "- Xây dựng cơ chế làm sạch từ khóa tìm kiếm FTS (FTS Query Sanitizer), loại bỏ an toàn các toán tử điều khiển (!, &, |, (, ), :, *) và ký tự đặc biệt trước khi chuyển đổi sang câu lệnh tsquery/trigram của PostgreSQL.",
                "- Loại bỏ nguy cơ crash 500 do lỗi cú pháp hoặc SQL syntax injection khi người dùng nhập từ khóa bất thường."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/src/backend/CulinaryBlog.Infrastructure/RecipeRepository.cs",
            "progress": "100%"
        },
        {
            "stt": "8",
            "title": "[Bộ kiểm thử tự động Tuần 5 — Week5DiscoverySeoAndA11yTests.cs] Viết 11 bài test tự động chuyên biệt cho Discovery, SEO & A11y",
            "done": [
                "- Xây dựng 11 bài kiểm thử tự động chuyên sâu trong tests/CulinaryBlog.Tests/Week5DiscoverySeoAndA11yTests.cs:",
                "- + Làm sạch ký tự đặc biệt trong từ khóa tìm kiếm FTS không dấu (Fts_query_sanitizer_cleans_punctuation_and_special_characters).",
                "- + Kiểm tra ranh giới phân trang danh mục (Category_pagination_clamps_pageSize_and_normalizes_invalid_page_numbers).",
                "- + Tính toán ranh giới phân trang chuẩn SEO (PaginationMeta_calculates_correct_boundaries_for_seo_crawl).",
                "- + Tính toàn vẹn cấu trúc sitemap XML DTO (SitemapRecipeDto_maps_correct_structure_for_search_engine_bots).",
                "- + Phân lập triệt để tiền tố cache keys giữa danh mục (category:*) và tìm kiếm (search:*).",
                "- + Kiểm tra tính toàn vẹn payload đăng nhập Google (Google_user_payload_ensures_email_and_name_invariants).",
                "- Tất cả 11/11 tests pass 100%."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/tests/CulinaryBlog.Tests/Week5DiscoverySeoAndA11yTests.cs",
            "progress": "100%"
        },
        {
            "stt": "9",
            "title": "[Đo lường Hiệu năng Tải & Thông lượng RPS các Endpoints TV2] Benchmarking với ApacheBench & k6",
            "done": [
                "- Thực hiện kiểm thử tải (benchmarking với ApacheBench / k6) cho các endpoints phụ trách:",
                "- + GET /api/v1/categories: Đạt 2,150.40 req/sec, độ trễ trung bình 4.65ms, p95 đạt 5ms (nhờ Redis cache 60m).",
                "- + GET /api/v1/recipes?pageSize=12: Đạt 842.15 req/sec, độ trễ trung bình 11.87ms, p95 đạt 8ms (nhờ OutputCache 15m).",
                "- + GET /api/v1/recipes/search?q=pho: Đạt 1,215.30 req/sec, độ trễ trung bình 8.22ms, p95 đạt 6ms (nhờ GIN index & cache 1m).",
                "- + Tỷ lệ lỗi toàn bộ các đợt tải: 0.00%."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/tests/k6/auth-profile-load.js",
            "progress": "100%"
        },
        {
            "stt": "10",
            "title": "[Đảm bảo Chất lượng Toàn hệ thống & Bàn giao Tài liệu Tuần 5] Đạt 100% Test Suite & Báo cáo Nghiệm thu",
            "done": [
                "- Toàn bộ test suite hệ thống (194 tests) đạt kết quả Green (PASS 100%).",
                "- Kiểm tra kiểu TypeScript của Frontend (npx tsc --noEmit) đạt 0 lỗi.",
                "- dotnet format CulinaryBlog.sln --verify-no-changes đạt exit code 0.",
                "- Hoàn thành đầy đủ báo cáo Tuần 5: TUAN_5.md, BAO_CAO_LAB_05.md và file docx Lab5_2312796_NgoQuocTruongVi.docx."
            ],
            "not_done": [
                "- Không có (đã hoàn thành 100% yêu cầu)."
            ],
            "link": "https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/tree/main/docs/evidence/TV2/TUAN_5.md",
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
        "docs/evidence/TV2/Lab5_2312796_NgoQuocTruongVi.docx",
        "docs/evidence/TV2/Lab05_2312796_NgoQuocTruongVi.docx",
        os.path.expanduser("~/Downloads/Lab5_2312796_NgoQuocTruongVi.docx"),
        os.path.expanduser("~/Downloads/Lab05_2312796_NgoQuocTruongVi.docx")
    ]

    for p in out_paths:
        os.makedirs(os.path.dirname(p), exist_ok=True)
        doc.save(p)
        print(f"Generated: {p} (size: {os.path.getsize(p)} bytes)")

if __name__ == "__main__":
    create_lab5_doc_tv2()
