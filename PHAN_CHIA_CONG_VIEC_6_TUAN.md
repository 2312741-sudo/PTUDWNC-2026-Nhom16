# PHÂN CHIA CÔNG VIỆC CULINARY BLOG — 4 NGƯỜI, 6 TUẦN

Cập nhật: 09/09/2026. Thành viên: TV1–TV4 (thay bằng tên thật khi nhận việc). Tuần 1 tính từ ngày nhóm bắt đầu; chưa có ngày bắt đầu cụ thể nên không tự gán lịch ngày tháng.

Nguồn yêu cầu: SRS Culinary Blog v1.0.0, 71 trang. Tài liệu liên quan: [Kế hoạch dự án tổng thể](KE_HOACH_DU_AN.md), đặc biệt mục 2 về các mâu thuẫn cần chốt và mục 10–11 về 34 FR/30 NFR. File này dùng để giao việc, theo dõi tiến độ và nghiệm thu cá nhân. Mọi công việc dưới đây hiện **Chưa làm**; việc ghi lịch không phải bằng chứng đã triển khai.

## 1. Nguyên tắc và phạm vi mỗi người

Mỗi người làm đầy đủ frontend, backend, cơ sở dữ liệu, validation/bảo mật, cache/tìm kiếm, file/jobs, SEO, quan sát, kiểm thử và triển khai. Phân công theo nghiệp vụ, không chia một người chỉ frontend, một người chỉ backend. Công nghệ giữ nguyên đề: .NET10 Minimal APIs/Clean Architecture/CQRS–MediatR/EF Core; PostgreSQL16; Next.js App Router/TypeScript; Redis7; MinIO; Hangfire; Serilog/OpenTelemetry; Docker/Nginx.

| Người | Phần chính và task trong kế hoạch tổng thể | FR chịu trách nhiệm chính | Reviewer |
|---|---|---|---|
| TV1 | Tài khoản/hồ sơ, nền tảng auth/logging/CI — A1–A7 | FR-AUTH-001/002/006/007; FR-JOB-001; FR-OBS-002 | TV2 |
| TV2 | Danh mục/khám phá/tìm kiếm, Google login — B1–B7 | FR-CAT-001…005; FR-RCP-001; FR-SRCH-001…004; FR-AUTH-003 | TV3 |
| TV3 | Soạn thảo Recipe/nguyên liệu/bước/dinh dưỡng, refresh token — C1–C7 | FR-RCP-002/003/004/009/010; FR-AUTH-004 | TV4 |
| TV4 | Xuất bản/media/SEO/vận hành, logout — D1–D7 | FR-RCP-005/006/007/008; FR-FILE-001/002; FR-JOB-002/003; FR-OBS-001/003; FR-AUTH-005 | TV1 |

Chủ trì phải giao đủ UI → API → dữ liệu → test → docs của phần mình. Công cụ dùng chung chỉ tích hợp một phiên bản vào sản phẩm. Những kỹ năng chưa có cơ hội thực hành trong phần chính phải tự viết LAB trên nhánh cá nhân và demo; chỉ review hoặc chạy code của người khác chưa đủ. Giữ mức 100 điểm công việc dự kiến/người trong kế hoạch tổng thể, không coi đây là điểm chấm của giảng viên.

## 2. Lịch tổng hợp 6 tuần

| Tuần | TV1 | TV2 | TV3 | TV4 | Cổng hoàn thành |
|---|---|---|---|---|---|
| 1 | Auth contracts/Identity/JWT, pipeline/CI; register/login tối thiểu | Category schema/CRUD nền, UI shell, Google contract | ERD/migrations, Draft CRUD nền/Nutrition, concurrency spike | Compose/Nginx/MinIO/health nền, storage contract, logout | G0 giữa tuần: stack chạy; G1 cuối tuần: đăng ký → đăng nhập → category → Draft |
| 2 | Profile/dashboard, lockout/rate limit, welcome email, auth tests | Category UI/detail, Google login, list/filter/sort/page, FTS đầu tiên | Ingredients/steps/wizard/detail, refresh rotation, ownership/version tests | Upload/metadata/primary/delete/resize + UI progress; publish/unpublish | G2: Draft đủ nguyên liệu/bước/ảnh; publish được; không sửa chéo owner |
| 3 | Hoàn thiện auth/error/logging; LAB FTS/cache/media/SEO | Search SSR, Redis/OutputCache/ISR; LAB Identity/concurrency/jobs | Hoàn thiện editor/detail/dashboard; LAB OAuth/media/jobs | Archive/delete/sitemap/robots/SEO/OTEL; LAB Identity/forms/FTS | G3: đủ 34 FR chạy tích hợp + test ban đầu; lab được triển khai từ tuần 1 |
| 4 | Hoàn tất lab cá nhân, security/frontend tests, CI coverage | Hoàn tất lab, k6/EXPLAIN/cache, responsive/a11y | Hoàn tất lab, E2E/concurrency/architecture tests | Hoàn tất lab, health/retry/restore/multi-instance/load | G4: 24/24 kỹ năng mỗi người có minh chứng; G5: coverage đạt, sửa lỗi chặn luồng/bảo mật |
| 5 | Tự deploy/restore, auth review, sửa lỗi và docs | Tự deploy/restore, search/cache/SEO review và số đo | Tự deploy/restore, editor/data/E2E review và số đo | Tự deploy/restore, vận hành/HTTPS/backup review và số đo | G6: staging hoàn chỉnh; performance/SEO/a11y có kết quả và giới hạn; 5 E2E pass |
| 6 | Demo tài khoản/hồ sơ + kỹ năng bổ sung; bàn giao | Demo danh mục/khám phá + kỹ năng bổ sung; bàn giao | Demo soạn thảo/dữ liệu + kỹ năng bổ sung; bàn giao | Demo xuất bản/media/vận hành + kỹ năng bổ sung; bàn giao | G7: regression, release, tài liệu và minh chứng đủ 4 người |

Mỗi tuần có một buổi tích hợp và một buổi demo ngắn. Test và lab được thực hiện song song từ tuần 1; các bài cần recipe/media đầy đủ nối tiếp sau G2. Dành khoảng 25% thời gian tuần 1–4 cho phần kỹ năng cá nhân còn thiếu, không dồn cả 24 nhóm kỹ năng vào tuần 4. Tuần 5 chốt tích hợp và số đo, tuần 6 dành cho sửa lỗi còn lại, regression, demo và bàn giao. Lịch 6 tuần giữ đủ phạm vi FR/NFR/CONS; nếu thiếu năng lực hoặc hạ tầng, ghi rõ trở ngại và điều chỉnh phần optional trước, không đánh dấu hoàn thành khi chưa có bằng chứng.


## 3. Giao việc chi tiết cho từng thành viên

Mỗi ô tuần bao gồm code/cấu hình, test tương ứng và PR được review. Mã task A–D tham chiếu trực tiếp kế hoạch tổng thể; task lớn được chia qua nhiều tuần, không chờ làm xong toàn bộ module mới tích hợp.

### 3.1. TV1 — Tài khoản và hồ sơ

| Tuần / task | Việc phải làm | Đầu ra và điều kiện nghiệm thu |
|---|---|---|
| 1 — A1, A2, A5; A6 nền | Identity/User/roles + migration; JWT/ICurrentUser; register/login cơ bản; DTO/Problem Details/CI/pipeline; lab kiến trúc và API | Giữa tuần bàn giao auth contract; cuối tuần login lấy token, đăng ký Author, mật khẩu được hash; Scalar/CI chạy; TV2–TV4 dùng auth thật |
| 2 — A2, A3, A4, A7 | Lockout/rate limit; profile/dashboard; RHF/Zod forms; SMTP welcome job/retry; kiểm thử auth | Form có inline errors; không đổi email/role qua profile; credential sai thông báo chung; email nhận trong Mailhog, job lỗi có retry |
| 3 — A5, A6, A7 | Log correlation/redaction; tích hợp Google/refresh/logout do các bạn bàn giao; LAB FTS/Redis/OutputCache, upload/resize/SEO | Theo dõi request bằng log; token/password không vào log; có PR lab search không dấu và cache invalidation, MinIO/SEO |
| 4 — A6, A7 | Bổ sung OAuth/rotation/concurrency và các kỹ năng còn thiếu; security/Jest/RTL/E2E register/login, coverage | 24/24 ô kỹ năng có code/test/demo; Application >=80%; lỗi auth/rate limit/secret có negative tests |
| 5 — A7 | Tự build/deploy Nginx/Compose; backup/restore DB/file; test2 API; review TV4, sửa lỗi tích hợp và README/ADR | Khởi động từ checkout sạch; restore được dữ liệu; có số đo trang mình và báo cáo bảo mật |
| 6 — A7 | Regression, chốt evidence/docs, demo tài khoản/hồ sơ và lab ngoài phần chính | Reviewer TV2 xác nhận; demo độc lập; đăng ký/đăng nhập E2E pass; bàn giao không còn lỗi chặn luồng |

### 3.2. TV2 — Danh mục và khám phá

| Tuần / task | Việc phải làm | Đầu ra và điều kiện nghiệm thu |
|---|---|---|
| 1 — B1, B2 nền; B6 nền | Category model/migration/seed, Admin CRUD cơ bản; public UI shell; list DTO/pagination; chốt Google contract | Category có sẵn cho TV3 tạo recipe; Admin tạo danh mục, Author bị từ chối; types thống nhất |
| 2 — B2, B3, B4, B7 | UI quản lý/detail category; Google Code+PKCE/Auth.js/backend verification; list/filter/sort/page; FTS trigger/config/GIN | Google login tích hợp; filter AND/page đúng; query “pho” tìm “phở”; search không lộ Draft |
| 3 — B3, B5, B6 | Search SSR hoàn chỉnh; Redis category/search, OutputCache/ISR isolation; LAB Identity/refresh/concurrency/media/jobs | Đủ các FR tìm kiếm/danh mục; cache invalidation sau mutation; có PR lab ngoài phần chính |
| 4 — B6, B7 | Hoàn tất LAB SMTP/resize/sitemap/ownership; k6/EXPLAIN/cache-hit; responsive/a11y/Jest/RTL/Playwright | 24/24 kỹ năng; query plan và số đo cache; Google invalid-token và Admin permission tests; search E2E |
| 5 — B7 | Tự deploy/restore/test2 API; review TV1; sửa SEO/CWV/JS bundle và docs | Có số đo và giới hạn; public pages metadata/canonical/noindex đúng; startup độc lập |
| 6 — B7 | Regression, demo category/search/Google và kỹ năng bổ sung; chốt evidence | Reviewer TV3 xác nhận; không sai tổng phân trang/lộ private data; bàn giao API/types/query docs |

### 3.3. TV3 — Soạn thảo công thức

| Tuần / task | Việc phải làm | Đầu ra và điều kiện nghiệm thu |
|---|---|---|
| 1 — C1, C2 nền; C6 nền | ERD/Recipe/Nutrition/config/migration, Draft CRUD tối thiểu; UoW/audit/version spike; phối hợp Category/auth | Tạo Draft qua UI/API cuối tuần; category/author FK đúng; thống nhất RowVersion và DTO cho TV4 |
| 2 — C2, C3, C4, C5 | Ingredients/steps/wizard/detail; ownership/renumber/concurrency; refresh rotation/hash/family và client handling | Draft đủ nguyên liệu/bước cho publish; không sửa chéo owner; hai writer/refresh đồng thời không ghi sai |
| 3 — C4, C6, C7 | Hoàn thiện editor/dashboard/detail/Nutrition/JSON-LD; LAB Google/MinIO/jobs và cache/FTS còn thiếu | UI nối bộ ảnh TV4, query TV2; optimistic rollback và conflict reload; lab có code/test |
| 4 — C6, C7 | LAB SMTP/resize/sitemap/ops; xUnit/API/Jest/RTL/Playwright create; architecture/concurrency tests | 24/24 kỹ năng; coverage>=80%; nested transaction rollback đúng, childId khác recipe bị từ chối |
| 5 — C7 | Tự deploy/restore/test2 API; điều phối migration sạch/nâng cấp; review TV2; số đo query/CWV/E2E | Migrations chạy lặp trên môi trường đã quy định; 5 critical E2E tích hợp; docs schema/version đầy đủ |
| 6 — C7 | Regression, demo tạo/sửa/nguyên liệu/bước và lab; chốt evidence | Reviewer TV4 xác nhận; tự giải thích aggregate/UoW/version/owned data; bàn giao test và DB docs |

### 3.4. TV4 — Xuất bản, ảnh và vận hành

| Tuần / task | Việc phải làm | Đầu ra và điều kiện nghiệm thu |
|---|---|---|
| 1 — D1 nền, D3 logout, D5; D6 nền | Compose/PostgreSQL/Redis/MinIO/Nginx/health skeleton; storage contracts; logout; pipeline triển khai nền | FE/API/DB chạy giữa tuần; storage interface cho editor; logout revoke đúng token; không commit secrets |
| 2 — D1, D2, D3, D4 | Upload/metadata/primary/delete, MIME/magic bytes/size; resize; uploader progress; publish/unpublish | 4 định dạng <=5MiB, original/300×300/800×600; đúng1 primary; publish cần dữ liệu theo ADR; không lộ Draft |
| 3 — D3, D4, D5, D6 | Archive/delete theo ADR; sitemap/robots/OG/JSON-LD; OTEL/metrics; LAB Identity/Google/refresh/forms/FTS | Đủ FR media/status/jobs/health; queue persistent; sitemap Published-only; có PR lab ngoài phần chính |
| 4 — D5, D6, D7 | Hoàn tất LAB; retry/race/security/publish E2E; backup/restore, health failure, shared cache/multi-worker | 24/24 kỹ năng; xóa-vs-resize không tái sinh file sai; restore và lỗi dependency có bằng chứng |
| 5 — D5, D7 | Tự deploy/restore/test2 API; review TV3; HTTPS/CORS/volumes, load/SEO/runbook | Staging đầy đủ; trace HTTP→DB; Nginx/health đúng; số đo và giới hạn được ghi rõ |
| 6 — D7 | Regression/release, demo publish/media/ops và lab; chốt evidence | Reviewer TV1 xác nhận; 5 E2E pass; runbook, backup/restore và secrets instructions bàn giao |

## 4. Phụ thuộc, bàn giao và cân bằng khối lượng

1. Giữa tuần 1: TV1 bàn giao auth contract, TV2 Category/list DTO, TV3 Recipe/schema/version, TV4 storage/deploy contracts. Dựng mock UI theo contract trong lúc API chưa sẵn sàng.
2. Cuối tuần 1: chốt auth thật + Category + Draft tối thiểu (G1). Nếu chưa đạt thì xử lý ngay các task nền trước tính năng phụ thuộc, ghi ảnh hưởng lịch.
3. Tuần 2: TV3 bàn giao ingredient/step để TV4 publish; TV4 bàn giao ảnh/URLs/progress để TV3 tích hợp editor/detail. TV2 dùng seed để phát triển search sớm, nghiệm thu lại trên dữ liệu publish thật.
4. Tuần 3: cùng kiểm tra vòng đời đăng ký → category → Draft → ảnh/nguyên liệu/bước → publish → search → unpublish. Thống nhất invalidation API/Redis/ISR và thời gian cập nhật sitemap.
5. Tuần 4–5: kiểm thử và đo xuyên hệ thống; không để một người tự nhận toàn bộ QA/DevOps. TV1 hỗ trợ observability, TV2 public SEO/sitemap, TV3 mutation/cache invalidation cho TV4; chủ trì và reviewer vẫn rõ ràng.
6. Tuần 6: chốt phạm vi, sửa lỗi và regression; không mở thêm bình luận/rating/bookmark hoặc tính năng ngoài SRS.

Mỗi tuần: đầu tuần xác nhận task và phụ thuộc; giữa tuần tích hợp PR; cuối tuần demo sản phẩm + cập nhật evidence cá nhân. Nếu mất một mốc, ghi task thiếu/người xử lý/hạn bù/ảnh hưởng; không chuyển hết lab sang tuần cuối.

## 5. Ma trận đủ 24 nhóm kỹ năng cho từng người

SRS không có một danh sách mang tên “skill” độc lập. Danh mục dưới đây trích từ công nghệ, CONS, FR, NFR, kiến trúc, kiểm thử và triển khai của toàn bộ PDF. Đây là tiêu chí đào tạo bổ sung theo yêu cầu chia nhóm: **mọi ô ở bốn cột phải có minh chứng cá nhân**. Những công cụ được SRS ghi optional là bài nâng cao, không tự nâng thành nghĩa vụ production.

Ký hiệu trong ô: **SP** = đóng góp vào sản phẩm chung; **LAB** = tự viết một phiên bản nhỏ trên nhánh `practice/TVn/...`, có test, được lưu và demo, không merge code trùng vào main. Mỗi LAB phải dùng stack thật của đề khi kỹ năng yêu cầu DB/cache/storage/provider; mock chỉ dùng cho unit/error tests, không thay toàn bộ integration.

| Mã / kỹ năng và nguồn | TV1 phải thực hiện | TV2 phải thực hiện | TV3 phải thực hiện | TV4 phải thực hiện |
|---|---|---|---|---|
| K01 Phân tích SRS, FR/NFR, ADR, API contract (tr. 6–16, 43) | SP ADR auth + test mapping | SP ADR search/cache + mapping | SP ADR schema/version + mapping | SP ADR delete/media + mapping |
| K02 C#/.NET10 Minimal APIs, REST/version, Scalar/RFC7807 (14–15, 47, 61–69) | SP Auth group + lỗi | SP Category/Search group + lỗi | SP Recipe/child group + lỗi | SP Image/status group + lỗi |
| K03 Clean Architecture, entity/value object, DI/interfaces (50–52) | SP Identity adapter/ICurrentUser + LAB domain value object | SP Category/Slug/repository | SP Recipe aggregate/Nutrition/UoW | SP file/job interfaces + status domain methods |
| K04 CQRS/MediatR + logging/validation/caching/invalidation/performance behaviors (51–52) | SP auth handlers; LAB cache query + invalidation | SP category/search handlers; LAB tự viết behavior tối thiểu | SP recipe/refresh handlers; LAB tự viết behavior tối thiểu | SP status/media handlers; LAB tự viết behavior tối thiểu |
| K05 FluentValidation + sanitization + Zod/RHF (15, 41–42, 50) | SP register/profile forms và validators | SP category form/query params | SP wizard/nested form và validators | SP media metadata/status form; LAB recipe form RHF/Zod |
| K06 EF Core/PG16 Code First, migration/config/seed, LINQ/projection/index (15, 40, 54–60) | SP User/Token migration; LAB Recipe query/index | SP Category/FTS migration/query | SP Recipe/Nutrition/Step migration/query | SP Image config/index + LAB seed/query |
| K07 UoW/transaction/audit/soft delete/optimistic concurrency (27–33, 43, 54) | LAB cập nhật Recipe 2 writer + UoW/audit/filter | LAB cập nhật Recipe 2 writer + UoW/audit/filter | SP aggregate/RowVersion + tests | SP primary/status transaction; LAB RowVersion/filter/audit |
| K08 Identity/PBKDF2, JWT, refresh hash/rotation/reuse/logout (17–23, 41) | SP register/login; LAB rotation/reuse/logout | LAB tự viết register/login/refresh/reuse/logout tối thiểu | SP refresh; LAB register/login/hash/logout | SP logout; LAB register/login/refresh/reuse |
| K09 Google OAuth2/PKCE, Auth.js, ID token verification/link (19–20, 47–50, 61) | LAB Google callback/verify/link trên app test | SP Google callback/verify/link | LAB Google callback/verify/link trên app test | LAB Google callback/verify/link trên app test |
| K10 RBAC/resource/policy/rate limit/secrets/HTTPS/CORS (13–15, 41–42) | SP policy + limit; LAB owner/VerifiedAuthor | SP Admin + LAB owner/VerifiedAuthor/limit | SP owner + LAB Admin/VerifiedAuthor/limit | SP owner/dashboard + LAB VerifiedAuthor/limit |
| K11 FTS tsvector/tsquery/unaccent/pg_trgm/GIN/ts_rank, filter/sort/page (36–37) | LAB recipe search + trigger/index/rank/AND/page | SP full search và query analysis | LAB recipe search + trigger/index/rank/AND/page | LAB recipe search + trigger/index/rank/AND/page |
| K12 Redis cache-aside, OutputCache, invalidation, fallback/isolation (28–29, 40, 44, 52) | LAB public recipe cache + output policy + invalidation + Redis down | SP category/search Redis; LAB OutputCache/detail | SP recipe detail invalidation; LAB cache/output/fallback | SP status/image invalidation; LAB cache/output/fallback |
| K13 MinIO/S3 upload/delete, stream/MIME/magic bytes/GUID (33–38, 41) | LAB upload/delete recipe image 4 MIME + boundary | LAB upload/delete recipe image 4 MIME + boundary | LAB upload/delete recipe image 4 MIME + boundary | SP upload/delete đầy đủ + boundary tests |
| K14 Hangfire fire-and-forget/delayed/recurring, retry/persistence/dashboard (38–39) | SP welcome; LAB resize+sitemap+delayed+restart | LAB welcome+resize+sitemap+delayed+restart | LAB welcome+resize+sitemap+delayed+restart | SP resize/sitemap; LAB welcome+delayed+restart |
| K15 SMTP/MailKit, ảnh 300×300/800×600, sitemap XML (38, 48, 58) | SP SMTP; LAB resize/sitemap chạy thật | LAB Mailhog email + resize + XML file | LAB Mailhog email + resize + XML file | SP resize/XML; LAB Mailhog SMTP |
| K16 Next.js App Router/TypeScript/Tailwind, SSR/ISR/CSR (46–50) | SP CSR auth; LAB SSR search + ISR public | SP SSR/ISR + CSR admin | SP CSR editor/ISR detail; LAB SSR search | SP CSR uploader/ISR media; LAB SSR search |
| K17 TanStack Query/server state, optimistic rollback, next/image, split/bundle (41–42, 50) | SP profile mutation; LAB image/list query/rollback | SP category mutation/query; LAB next/image tối ưu | SP ingredient/step mutations và image detail | SP image mutation/progress; LAB query/rollback/bundle |
| K18 Responsive, WCAG2.1 AA/keyboard/screen reader/loading/error (42, 46) | SP login/profile tại 320/768/1200px + VoiceOver/NVDA evidence | SP category/search cùng checklist | SP wizard/editor cùng checklist | SP upload/gallery/status cùng checklist |
| K19 SEO: metadata/OG/Twitter/canonical/301/robots/JSON-LD Recipe (44–45) | LAB recipe page đủ SEO + sitemap/redirect/noindex | SP public metadata; LAB full Recipe JSON-LD/301 | SP detail JSON-LD; LAB sitemap/robots/301 | SP SEO/sitemap/robots; LAB redirect/metadata đủ |
| K20 Serilog/Seq/correlation, OTEL HTTP/DB/custom metrics, health probes (39, 48) | SP logs; LAB OTEL/metrics/health thật | SP query trace; LAB log sink/metrics/health | SP recipe metric; LAB trace/log sink/health | SP OTEL/probes; LAB logging middleware riêng |
| K21 xUnit/unit>=80%, API happy+error, Jest/RTL, Playwright (43) | SP auth + LAB recipe API/UI/E2E | SP category/search API/UI/E2E | SP recipe API/UI/E2E | SP media/publish API/UI/E2E |
| K22 k6/p50/p95/p99, EXPLAIN/N+1/cache hit, CWV/Lighthouse (40–41) | LAB chạy tải/search query + đo trang của mình | SP query/k6; đo trang của mình | SP concurrency/query; k6 + đo trang của mình | SP metrics/load; EXPLAIN + đo trang của mình |
| K23 Docker multi-stage/Compose/Nginx/env/volumes/backup-restore/scaling (43–44, 48, 53) | SP CI build; tự deploy, restore DB/file, test 2 API | SP FE Docker/CI; tự deploy/restore/test 2 API | SP migration startup; tự deploy/restore/test 2 API | SP Compose/Nginx; tự deploy/restore/test 2 API |
| K24 Git/PR/review/CI/static analysis/architecture test/secret scan/docs (14, 42–43) | PR cá nhân + review TV4 + ADR/runbook + CI | PR cá nhân + review TV1 + ADR/runbook + CI | PR cá nhân + review TV2 + ADR/runbook + CI | PR cá nhân + review TV3 + ADR/runbook + CI |

### 5.1. Bài thực hành cá nhân thống nhất, tránh học mỗi người một kiểu

Mỗi người tạo nhánh riêng từ skeleton G1, dùng database/schema/bucket prefix riêng cho lab và không thao tác dữ liệu production. TVn tự thực hiện:

- **L1 — Danh tính và bảo mật:** register/login/PBKDF2; JWT; Google Code+PKCE/Auth.js + backend verify; refresh hash/rotation/reuse; logout; test Guest/Author/owner/non-owner/Admin/VerifiedAuthor. Nếu không có Google credentials, đánh dấu phần integration ngoài còn chờ; không coi mock là hoàn tất Google login.
- **L2 — Recipe nhỏ từ đầu đến cuối:** entity/owned Nutrition, migration/seed, Minimal API/CQRS/validators/UoW, query/mutation, RowVersion 2 writer, soft delete/audit; wizard RHF/Zod và detail. Ít nhất 1 Command và 1 Query do chính người đó viết.
- **L3 — Tìm kiếm và cache:** tự tạo FTS trigger/config/GIN, query rank và filter/sort/page; Redis cache-aside và OutputCache, invalidate sau sửa; không lộ Draft; tắt Redis để test fallback. Kèm EXPLAIN và k6.
- **L4 — Media và jobs:** upload/delete JPEG/PNG/WebP/AVIF đúng nội dung/kích thước; resize 2 size; SMTP gửi Mailhog; Hangfire fire-and-forget, delayed trong lab, recurring sitemap, retry và restart worker. Cùng một bộ 3 loại tác vụ giúp mỗi người học đủ email/image/XML, không phải thêm chức năng mới vào sản phẩm.
- **L5 — UI, SEO và vận hành:** SSR search + ISR published detail + CSR editor; TanStack Query/rollback/next/image; toàn bộ metadata/JSON-LD/robots/redirect; Serilog/Seq/OTEL/metrics/health; Docker/Nginx/HTTPS, backup/restore, 2 API instance và shared cache/jobs.
- **L6 — Chất lượng:** test xUnit/API/Jest/RTL/Playwright; đo coverage/performance/accessibility; architecture test/lint/secret scan; viết README/ADR và review. Mỗi người tự sửa ít nhất một lỗi có test chứng minh trước/sau.

Nếu phần SP đã đáp ứng đầy đủ một nội dung LAB thì dùng PR SP làm minh chứng, không viết lại chỉ để đủ số file. Phần còn thiếu vẫn phải tự bổ sung. Kỹ năng nâng cao optional như Polly circuit breaker, read replica, partition >1 triệu rows, presigned direct upload, domain events: cả 4 đọc/giải thích tradeoff; chỉ triển khai nếu còn thời gian và nhóm thống nhất.

### 5.2. Cách xác nhận đủ skill

Với mỗi Kxx, mỗi TVn phải có bản ghi `TVn-Kxx`: FR/NFR liên quan, đường dẫn code hoặc config, commit/PR, test/lệnh chạy, kết quả thực tế, ảnh/log/video phù hợp, reviewer và ngày xác nhận. Với ô gồm nhiều kỹ thuật, đánh dấu từng kỹ thuật con; thiếu một phần thì ô chưa hoàn thành.

Tổng tối thiểu **24 × 4 = 96 ô minh chứng**, không phải 96 bài triển khai trùng lặp. Một PR được tham chiếu nhiều ô nếu thật sự chứa các kỹ năng đó. Chỉ công nhận cá nhân hoàn thành khi **24/24 ô** có bằng chứng và demo độc lập, không lấy trung bình giữa thành viên để bù thiếu.

## 6. Mốc minh chứng và kiểm tra hoàn thành

| Thời điểm | Yêu cầu chung cho TV1, TV2, TV3, TV4 |
|---|---|
| Cuối tuần 1 | Có môi trường cá nhân, PR kiến trúc/API/schema hoặc cấu hình của mình; mở sổ K01–K24; lab nền bắt đầu sau G1 |
| Cuối tuần 2 | Phần nghiệp vụ chính chạy end-to-end ở mức ban đầu; rà từng kỹ năng thiếu để lập LAB tuần 3–4 |
| Cuối tuần 3 | Đủ34 FR bản tích hợp; có code lab các mảng ngoài chuyên trách; review kỹ năng chưa đạt, không chỉ đếm số PR |
| Cuối tuần 4 | Mỗi người 24/24 ô K có code/config + test + demo + reviewer; các kỹ thuật con trong mỗi ô đều được kiểm tra |
| Cuối tuần 5 | Mỗi người tự deploy/backup/restore/test2 API; có số đo performance/a11y/SEO; 5 critical E2E pass |
| Cuối tuần 6 | Regression pass, evidence hoàn chỉnh, reviewer xác nhận và demo độc lập; release/docs được bàn giao |

Ma trận K23 đã có lab deploy/restore trước cuối tuần 4; tuần 5 là lần xác minh lại trên bản staging tích hợp. Nếu thiếu Google credentials hoặc môi trường đo, ghi đúng phần còn chờ; mock không thay integration và không đánh dấu kỹ năng hoàn thành giả.

| Người | Phần công việc | Kỹ năng xác nhận | Deploy/restore staging | Demo cuối kỳ | Trạng thái hiện tại |
|---|---|---|---|---|---|
| TV1 | A1–A7 | 0/24 | Chưa làm | Chưa làm | Chưa làm |
| TV2 | B1–B7 | 0/24 | Chưa làm | Chưa làm | Chưa làm |
| TV3 | C1–C7 | 0/24 | Chưa làm | Chưa làm | Chưa làm |
| TV4 | D1–D7 | 0/24 | Chưa làm | Chưa làm | Chưa làm |

Mẫu một bản ghi:

```text
Tuần / Người / Task: 2 / TV3 / C3
FR/NFR/Kỹ năng: FR-RCP-010; NFR-SEC-006; K02,K04,K05,K06,K07,K21
Trạng thái: Chưa làm | Đang làm | Chờ tích hợp | Chờ review | Hoàn thành
Đầu ra, đường dẫn code/config, PR/commit:
Test/lệnh chạy, môi trường, kết quả thực tế:
Minh chứng ảnh/log/video/coverage (không có secret):
Reviewer và ngày xác nhận:
Trở ngại, người hỗ trợ, hạn xử lý:
```

Một task chỉ hoàn thành khi đúng FR/ADR, UI/API/DB nối thật, quyền/validation/cache/job phù hợp, test và CI pass, docs cập nhật, có reviewer và evidence cá nhân. Toàn nhóm phải đối chiếu **34 FR, 30 NFR, 10 CONS**, 33 endpoint baseline và các route bổ sung được chốt. Application line coverage>=80%; API có happy/error cases; 5 Playwright flows: register, login, create recipe, publish, search. Các chỉ số cụ thể theo mục11 kế hoạch tổng thể; ghi rõ chưa đạt nếu chưa đo hoặc chỉ có thiết kế.

Mỗi người demo dự kiến15–20 phút: tính năng chính, kỹ năng ngoài phần chính, giải thích code/test và xử lý một lỗi do reviewer chọn. **Sản phẩm đạt yêu cầu và từng người đủ24/24 nhóm kỹ năng là hai điều kiện riêng, đều phải được đáp ứng trong kế hoạch6 tuần.**
