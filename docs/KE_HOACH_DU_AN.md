# CULINARY BLOG — HỒ SƠ DỰ ÁN VÀ KẾ HOẠCH PHÂN CÔNG 4 THÀNH VIÊN

Ngày biên soạn: 09/09/2026 · Dự án: CULINARY-BLOG-V1 · Môn: Phát triển ứng dụng Web nâng cao.

Nguồn: `SRS_Culinary_Blog_v1.0.0.pdf`, phiên bản 1.0.0 ngày 04/06/2026, 71 trang, tại `/Users/nthtam/Downloads/SRS_Culinary_Blog_v1.0.0.pdf`. Số trang dẫn dưới đây là số trang PDF. Đã đọc toàn bộ 8 chương, lịch sử tài liệu và 3 phụ lục. Một số bảng trong PDF bị tràn khỏi mép phải, đặc biệt trang 28–29, 47–48 và 63–65; phần thiếu được đối chiếu giữa các chương, không được xem là dữ liệu đã xác nhận nếu vẫn còn mâu thuẫn.

Đây là **một tài liệu kế hoạch tổng hợp**, không phải mã nguồn ứng dụng đã hoàn thành. Mọi trạng thái công việc ban đầu là **Chưa làm**. Tài liệu giữ phạm vi đề, tổ chức lại theo phụ thuộc triển khai và thêm cơ chế đánh giá từng cá nhân. Các quy ước đề xuất để xử lý mâu thuẫn không phải thay đổi SRS đã được giảng viên phê duyệt.

## Mục lục

1. Phạm vi, sản phẩm bàn giao và nguyên tắc phân việc
2. Điểm cần thống nhất trước khi viết code
3. Kiến trúc, dữ liệu, API và giao diện chung
4. Thứ tự thực hiện và tiến độ dự kiến
5. Phần 1 — Thành viên 1: tài khoản và hồ sơ
6. Phần 2 — Thành viên 2: danh mục và khám phá
7. Phần 3 — Thành viên 3: soạn thảo công thức
8. Phần 4 — Thành viên 4: xuất bản, hình ảnh và vận hành
9. Ma trận kỹ năng bắt buộc cho cả 4 người
10. Ma trận bao phủ 34 yêu cầu chức năng
11. Tiêu chí nghiệm thu 30 yêu cầu phi chức năng
12. Kiểm thử, tích hợp, minh chứng cá nhân và bàn giao

## 1. Phạm vi, sản phẩm bàn giao và nguyên tắc phân việc

### 1.1. Mục tiêu và phạm vi

Xây dựng website chia sẻ, khám phá và quản lý công thức nấu ăn. Một công thức có thông tin cơ bản, danh mục, tác giả, nguyên liệu, bước thực hiện, bộ ảnh và dinh dưỡng. Hệ thống hỗ trợ tìm kiếm tiếng Việt không dấu, phân quyền, lưu ảnh MinIO, cache, tác vụ nền, SEO và giám sát. Nguồn: tr. 6–16.

| Tác nhân | Quyền cần triển khai |
|---|---|
| Guest | Xem nội dung Published, danh mục, tìm kiếm/lọc/sắp xếp/phân trang; không được ghi dữ liệu |
| Author | Có quyền Guest; tạo và quản lý công thức của mình; quản lý ảnh/nguyên liệu/bước; xuất bản/hủy xuất bản/lưu trữ; sửa hồ sơ của mình |
| Admin | Có quyền Author; quản lý danh mục; quản lý công thức của mọi tác giả; xem Hangfire Dashboard và hệ thống log; tài khoản được seed |

Ngoài phạm vi v1: bình luận, đánh giá sao, yêu thích/bookmark, thông báo thời gian thực, ứng dụng mobile native, thanh toán, thương mại điện tử, nhắn tin, GraphQL. Không thêm các tính năng này chỉ để chia đều việc. Không tạo dữ liệu rating giả trong JSON-LD.

Đích dữ liệu ban đầu: tối đa khoảng 10.000 công thức, 5.000 người dùng, 50 danh mục. Seed bằng Bogus tối thiểu 50 công thức và 5 tác giả, thêm Admin, nhiều trạng thái và dữ liệu biên. Dữ liệu hiệu năng 10.000 công thức dùng riêng, không thay thế bộ seed học tập.

### 1.2. Cách chia việc bảo đảm mỗi người đủ kỹ năng

Chia thành **4 phần nghiệp vụ xuyên suốt**, mỗi phần có UI → API → Application/Domain → DB → kiểm thử → triển khai. Chủ trì một phần không đồng nghĩa độc quyền công nghệ của phần đó.

Mỗi thành viên phải hoàn thành đồng thời:

1. Phần nghiệp vụ chính được giao ở mục 5–8.
2. Tất cả 24 nhóm kỹ năng K01–K24 ở mục 9 bằng đóng góp code/cấu hình/kiểm thử thực tế có định danh cá nhân.
3. Bài thực hành bổ sung ở nhánh cá nhân cho kỹ năng chưa có đủ cơ hội trong phần chính.
4. Một lần tự dựng toàn bộ hệ thống từ checkout sạch, một lượt review chéo và một buổi demo cá nhân.

Không tính “đã đọc”, “đã họp”, “đã review code của bạn” hoặc “chạy lại demo nhóm” là hoàn thành kỹ năng lập trình. Pair programming phải ghi rõ phần mỗi người viết và mỗi người phải giải thích, sửa lỗi, chạy lại độc lập. Bài thực hành trên nhánh cá nhân được giữ làm minh chứng; chỉ một phiên bản dùng chung được hợp nhất vào sản phẩm để tránh bốn hệ thống xác thực/cache/upload trùng nhau.

Danh sách thành viên chính thức:
- **TV1 (Nhóm trưởng)**: 2312741 — Nguyễn Thanh Tâm
- **TV2**: 2312796 — Ngô Quốc Trường Vĩ
- **TV3**: 2312786 — Huỳnh Quốc Trung
- **TV4**: 2312739 — Nguyễn Hữu Trung Sơn
Người review & nghiệm thu toàn bộ: **Nguyễn Thanh Tâm (Nhóm trưởng)**.
Lịch 6 tuần và 100 điểm công việc/người là **ước lượng lập kế hoạch**, không phải thời hạn hay thang điểm của giảng viên.

### 1.3. Sản phẩm bàn giao khi dự án được thực hiện xong

- Backend .NET 10 Minimal APIs theo Clean Architecture và CQRS; frontend Next.js App Router/TypeScript.
- EF Core migrations, schema/index/seed PostgreSQL; MinIO, Redis, Hangfire hoạt động thật.
- Dockerfiles multi-stage, Compose development/production, Nginx, cấu hình mẫu không có secret.
- Bộ kiểm thử và báo cáo coverage, tải, accessibility, SEO, bảo mật, backup/restore.
- Hướng dẫn khởi động dưới 5 phút sau khi đã cài công cụ và tải dependency/images; Scalar tại `/scalar`, ADR, CHANGELOG, sổ minh chứng cá nhân.
- Demo 5 luồng E2E bắt buộc: đăng ký, đăng nhập, tạo công thức, xuất bản, tìm kiếm.

## 2. Điểm cần thống nhất trước khi viết code

Nguồn SRS chứa các cách mô tả không đồng nhất. Bảng này giúp nhóm làm việc được ngay với **phương án dự kiến**; đầu tuần 1 ghi ADR/Change Request và gửi giảng viên xác nhận những thay đổi tác động yêu cầu. Có thể dựng khung, môi trường và mẫu UI trong lúc chờ; không tự coi điểm mâu thuẫn đã được phê duyệt.

| Mã | Điểm khác nhau trong SRS | Phương án dự kiến / việc cần chốt | Phụ trách |
|---|---|---|---|
| D01 | Tổng quan ghi 27 FR; bảng tr. 12 có 7+5+10+4+2+3+3 = 34. Bảng NFR ghi SEC có 6 nhưng chi tiết có 7 | Theo 34 mã FR và 30 mã NFR thực tế; dùng ma trận mục 10–11 làm checklist | TV1 |
| D02 | Validation 422 trong chương 3; 400 trong chương 8/phụ lục. Concurrency 409 tr. 31 nhưng 422 tr. 68 | Dự kiến theo phụ lục: validation/file/publish thiếu dữ liệu 400, concurrency 422, trùng dữ liệu 409. Lập contract test chung | TV1 + TV3 |
| D03 | Register dùng fullName/userName và auto-login ở tr. 17–18; chương 8 dùng displayName, trả thông tin user | DTO dùng displayName; định nghĩa ánh xạ Identity UserName từ email; giữ auto-login và trả user + token + expiresIn. Chốt trong OpenAPI trước tích hợp form | TV1 |
| D04 | Google Code Flow + PKCE/Auth.js ở tr. 19–20, callback backend tr. 47; chương 8 nhận idToken | Dự kiến Auth.js xử lý callback frontend bằng Code+PKCE; backend nhận ID token có thể xác minh. Không nhận profile tự khai từ client làm bằng chứng đăng nhập; chốt callback/scopes và DTO | TV2 + TV1 |
| D05 | Refresh token 512-bit tr. 18, 128-bit tr. 41; reuse revoke tùy chọn tr. 21 nhưng bắt buộc tr. 41 | Dự kiến sinh 512-bit ngẫu nhiên mật mã; chỉ lưu SHA-256; rotation trong transaction, bắt buộc revoke family khi reuse; ghi nhận khác biệt độ dài | TV3 |
| D06 | Logout cần Bearer nhưng luồng phụ cho phép token hết hạn tr. 22 | Dự kiến endpoint yêu cầu Bearer như chương 8; nếu hỗ trợ logout bằng refresh token phải đặc tả riêng cơ chế chứng minh quyền sở hữu | TV4 |
| D07 | Publish chỉ cần step tr. 31; phụ lục tr. 68 cần cả ingredient và step | Dự kiến ít nhất 1 nguyên liệu và 1 bước; không bắt buộc ảnh vì SRS không nêu; lặp trạng thái trả 200 | TV4 + TV3 |
| D08 | Recipe hard delete/cascade tr. 32–33 đối lập soft delete tr. 43, 54, 64 | Dự kiến soft delete để phù hợp mô hình dữ liệu/độ bền. Không xóa vật lý ảnh của recipe còn cần khôi phục. Xóa ảnh riêng vẫn chạy job; purge recipe/file chỉ sau chính sách lưu giữ được chốt | TV4 |
| D09 | Category xóa entity tr. 27 nhưng soft delete tr. 63; chưa rõ đếm archived/deleted | Dự kiến soft delete; chặn nếu còn recipe chưa xóa ở bất kỳ trạng thái nào, gồm Archived; xác định riêng liên kết recipe đã soft-delete | TV2 |
| D10 | IMemoryCache category 60 phút; OutputCache list/detail 15/60 phút; NFR dùng Redis 30/5/1 phút | Dự kiến Redis shared cache category/detail/search = 30/5/1 phút; OutputCache public list = 15 phút, public detail tối đa 5 phút. Invalidation cả API lẫn ISR; không cache chung response có Draft/Archived | TV2 + TV4 |
| D11 | items/totalCount và sort=-createdAt/pageSize=12 đối lập data/meta, sortBy/sortOrder/pageSize=10 | Dự kiến data/meta, sortBy/sortOrder; mặc định page=1, pageSize=12, max=50; sortBy=createdAt, sortOrder=desc. Adapter nếu cần tương thích; một schema duy nhất | TV2 |
| D12 | List tr. 28 có Draft/Archived của owner nhưng bước lọc bỏ Archived; chapter 8 chỉ Published | Public UI chỉ Published; dashboard gọi cùng query với phạm vi quyền rõ ràng: Author của mình, Admin theo quyền. Bổ sung tham số scope/status cho dashboard bằng ADR, không tin authorId từ client để cấp quyền | TV3 + TV2 |
| D13 | Category page ISR nhưng FR-CAT-002 trả thêm Draft của current user | ISR chỉ render dữ liệu public; phần cá nhân tải động, private/no-store; không tái sử dụng cache HTML public cho nội dung cá nhân | TV2 |
| D14 | Slug trùng recipe báo 409 tr. 30; phụ lục nói tự thêm suffix | Dự kiến tự thêm suffix có unique constraint chống race; hết lượt thử mới 409. Slug ổn định sau publish; nếu đổi slug Draft phải có lịch sử redirect 301 | TV3 |
| D15 | Name category 2–50 vs DB 100; ingredient 1–100 vs DB 200, quantity/unit bắt buộc vs nullable; cookTime >0 vs >=0; Expert thiếu trong filter | Dự kiến Category 2–100; ingredient 1–200, quantity nullable hoặc >0, unit nullable tối đa 50; cookTime >=0; hỗ trợ Easy/Medium/Hard/Expert. Ghi rõ các nới rộng khác chương 3 | TV2 + TV3 |
| D16 | sortOrder/orderIndex; durationMinutes/timerMinutes; Step Title chỉ xuất hiện chương 7–8 | Dự kiến orderIndex, timerMinutes, title bắt buộc <=200, description 1–2000; stepNumber server quản lý liên tục; cập nhật thứ tự trong transaction | TV3 |
| D17 | PATCH ảnh /primary tr. 34 khác PATCH metadata chương 8 | Dự kiến PATCH `/images/{imageId}` gồm isPrimary/altText/orderIndex; route /primary chỉ giữ nếu contract yêu cầu; mọi imageId phải thuộc recipeId trên URL | TV4 |
| D18 | Domain không dependency ngoài, nhưng ApplicationUser kế thừa IdentityUser và SearchVector là kiểu riêng DB; NFR còn cho phép FluentValidation trong Domain | Dự kiến Domain thuần BCL, model Identity đặt Infrastructure, dùng UserId/interface ở lớp trong; SearchVector ánh xạ Infrastructure; FluentValidation ở Application. Ghi ADR về vị trí ApplicationUser khác mô tả chương 6 | TV1 + TV3 |
| D19 | RowVersion bytea/[Timestamp] được mô tả nhưng chưa chỉ cách cập nhật; CORS chưa có If-Match | Giữ token opaque bytea và triển khai cập nhật nguyên tử khi ghi, kiểm tra bằng test 2 writer; thêm If-Match vào allowed headers và ETag vào exposed headers nếu dùng. Không coi annotation tự giải quyết concurrency | TV3 |
| D20 | VerifiedAuthor yêu cầu emailConfirmed tr. 13 nhưng không có API xác nhận email | Đặc tả luồng xác nhận bổ sung hoặc xin bỏ policy khỏi v1; không chặn Author mới vô thời hạn. Bài lab policy dùng user seed confirmed/unconfirmed; không tự đánh dấu email thật đã xác nhận | TV1 |
| D21 | Uptime 99,5% nhưng ghi khoảng 3,65 giờ/năm tr. 43 | 99,5% tương ứng khoảng 43,8 giờ/365 ngày. Giữ mục tiêu phần trăm; sửa số quy đổi trong ADR, không chứng nhận SLA bằng demo ngắn | TV4 |
| D22 | Redis lỗi fallback DB nhưng readiness fail khi Redis down | Giữ hai test riêng: API trực tiếp vẫn fallback; readiness 503 và proxy ngừng nhận traffic theo thiết kế. Quyết định vận hành khác cần ADR | TV4 |
| D23 | Hangfire có persistent PostgreSQL tr. 38 nhưng tr. 16 nói fire-and-forget bị mất khi không khả dụng | Dùng persistent queue; test restart/retry và lỗi enqueue. Không trả thành công rồi âm thầm bỏ job; định nghĩa cơ chế retry/outbox nếu cần để giữ ý định job sau commit | TV4 + TV1 |
| D24 | Tài liệu nói mọi entity dùng UUID/BaseEntity nhưng Identity dùng string và RefreshToken có cấu trúc riêng | Áp dụng BaseEntity cho entity nghiệp vụ; Identity/RefreshToken theo bảng riêng chương 7; thống nhất FK string UserId | TV1 |
| D25 | Phiên bản Next.js 14+/15, môi trường/browser khác nhau; SDK storage MinIO/AWS khác nhau | Khóa một bộ phiên bản tương thích trong lockfiles/global.json ở tuần 1, ưu tiên baseline .NET10/EF10/PG16/Redis7; chọn AWSSDK.S3 qua interface theo chương 6; kiểm tra công cụ thực tế trước cài | Cả nhóm |
| D26 | Google sitemap ping, công cụ/phiên bản và mô tả Rich Results là phụ thuộc bên ngoài | Cần xác minh tài liệu chính thức khi triển khai. Giữ sitemap/robots/structured data; nếu tích hợp không còn hỗ trợ thì lập CR, không giả báo ping thành công hoặc bảo đảm có rich snippet | TV4 |
| D27 | Bucket public-read nhưng recipe Draft/Archived chỉ owner/Admin xem | URL ảnh công khai không được xem là cơ chế bảo mật Draft. Chốt yêu cầu riêng tư ảnh; nếu cần bảo vệ phải có private bucket/presigned access và CR thay bucket policy | TV4 + TV1 |
| D28 | Recipe Instructions không bắt buộc trong request nhưng NOT NULL ở DB; nutrition có trường chỉ xuất hiện chương 7 | Dự kiến Instructions mặc định chuỗi rỗng; lưu 6 chỉ số nutrition nullable, không âm, theo owned columns; bổ sung DTO đầy đủ | TV3 |
| D29 | Category list sắp theo Name tr. 24 nhưng có OrderIndex dành navigation chương 7 | API list mặc định Name tăng; navigation có thể sắp OrderIndex rồi Name; contract/UI ghi rõ, không thay ngầm thứ tự API | TV2 |

Các mục trên là đối chiếu nội dung tài liệu, chưa phải kết quả kiểm chứng phiên bản SDK/dịch vụ trên Internet. Sổ ADR ghi: vấn đề → trang nguồn → phương án → ảnh hưởng API/DB/test → người chốt → ngày → trạng thái. Đầu tuần 2 không để các điểm về auth, schema, delete, cache và response format ở trạng thái mơ hồ.

## 3. Kiến trúc, dữ liệu, API và giao diện chung

### 3.1. Công nghệ và 10 ràng buộc bắt buộc

| Mã SRS | Nội dung áp dụng cho mọi thành viên |
|---|---|
| CONS-001 | Clean Architecture: Domain, Application, Infrastructure, Presentation; dependency hướng vào trong |
| CONS-002 | CQRS + MediatR; mỗi use case có Command/Query và Handler riêng |
| CONS-003 | .NET 10 Minimal APIs/C#; Next.js App Router/TypeScript, không Pages Router/MVC Controllers |
| CONS-004 | ASP.NET Core Identity/PBKDF2; JWT access 15 phút, refresh 7 ngày |
| CONS-005 | REST `/api/v1`, lỗi RFC 7807 `application/problem+json` |
| CONS-006 | PostgreSQL 16, EF Core 10 Code First/migrations; LINQ hoặc SQL được parameterize |
| CONS-007 | Upload tối đa 5 × 1024 × 1024 bytes; JPEG/PNG/WebP/AVIF; kiểm tra nội dung thật |
| CONS-008 | FluentValidation trong MediatR ValidationBehavior, không nhét validation nghiệp vụ vào endpoint |
| CONS-009 | Docker đóng gói ứng dụng, Dockerfile multi-stage, Compose cho development |
| CONS-010 | Serilog structured logs có CorrelationId, RequestPath, UserId khi xác thực |

Frontend: Tailwind CSS, Auth.js v5, TanStack Query, React Hook Form + Zod, next/image, SSR/ISR/CSR. Hạ tầng: Redis 7 + AOF, MinIO, Hangfire + PostgreSQL queue, SMTP/MailKit và Mailhog dev, Serilog/Seq dev, OpenTelemetry/OTLP/collector và backend quan sát, Nginx HTTPS. Kiểm thử: xUnit, Jest + Testing Library, Playwright, k6, Lighthouse CI, kiểm tra kiến trúc và secret scanning.

Luồng request: Nginx → Minimal API → xác thực/phân quyền → MediatR Logging/Validation/Caching → Handler → repository/UnitOfWork → PostgreSQL hoặc service abstraction → commit → invalidation/job → response. Kiểm tra quyền ở Application trước đọc cache cá nhân hoặc ghi dữ liệu. CacheInvalidation chỉ thực hiện sau thao tác ghi thành công; không cache token/password hoặc payload nhạy cảm vào log.

Cấu trúc dự kiến khi bắt đầu lập trình:

```text
PTUDWNC/
  KE_HOACH_DU_AN.md
  PHAN_CHIA_CONG_VIEC_6_TUAN.md
  src/backend/CulinaryBlog.Domain/
  src/backend/CulinaryBlog.Application/
  src/backend/CulinaryBlog.Infrastructure/
  src/backend/CulinaryBlog.API/
  src/frontend/
  tests/{unit,integration,e2e,performance}/
  deploy/{nginx,observability}/
  docs/{adr,evidence/TV1,evidence/TV2,evidence/TV3,evidence/TV4}/
  docker-compose.yml
  docker-compose.prod.yml
  .env.example
  README.md
  CHANGELOG.md
```

Đây là cấu trúc **sẽ tạo khi viết ứng dụng**; hồ sơ hiện gồm kế hoạch tổng thể và file phân công 6 tuần riêng; chưa tạo mã nguồn ứng dụng.

### 3.2. Dữ liệu cốt lõi

Nguồn tr. 52–60; áp dụng các quyết định D15–D19, D24, D28 khi được chốt.

| Thực thể | Trường/ràng buộc cần có | Quan hệ và lưu ý |
|---|---|---|
| BaseEntity nghiệp vụ | Id UUID; CreatedAt, UpdatedAt UTC; IsDeleted=false; RowVersion opaque | AuditInterceptor; global filter; concurrency được cập nhật thật |
| ApplicationUser | Identity string Id; Email/UserName/password hash/roles; DisplayName <=100; AvatarUrl <=500 nullable; Bio nullable; IsActive; CreatedAt | 1:N Recipes và RefreshTokens; không trả PasswordHash/SecurityStamp |
| RefreshToken | UUID Id; UserId string; TokenHash SHA-256 64 ký tự unique; ExpiresAt, RevokedAt?, ReplacedByTokenHash?, CreatedAt, CreatedByIp <=45 | Không lưu raw token; theo dõi family, revoke/rotation nguyên tử |
| Category | Name <=100 unique; Slug <=120 unique; Description?, ImageUrl <=500?, OrderIndex | 1:N Recipe; FK restrict; chặn xóa danh mục còn công thức |
| Recipe | Title 5–200; Slug <=220 unique; Description <=2000; Instructions; PrepTime >0; CookTime >=0; Servings >0; Difficulty; Status; PublishedAt? | FK Category UUID, Author string; Draft mặc định; filter soft delete |
| RecipeNutrition | Calories, Protein, Carbohydrates, Fat, Fiber, Sodium: decimal(8,2) nullable | Owned entity, 6 cột Nutrition_* trong Recipes; kcal/g/mg theo tr. 56 |
| RecipeIngredient | Name <=200; Quantity decimal(10,3)?; Unit <=50?; Notes <=500?; OrderIndex | FK Recipe; thứ tự ổn định; quantity nếu có phải >0 |
| RecipeStep | StepNumber >0; Title <=200; Description <=2000; TimerMinutes >=0 nullable; ImageUrl <=500 nullable | Unique (RecipeId, StepNumber), liên tục 1..N; transaction khi đổi thứ tự |
| RecipeImage | OriginalUrl <=500; MediumUrl?, ThumbnailUrl?; AltText <=200?; IsPrimary; OrderIndex | FK Recipe; có ảnh thì đúng 1 primary; không có ảnh thì 0 primary |
| SearchVector | PostgreSQL tsvector; trigger khi Title/Description thay đổi; GIN index | unaccent, pg_trgm; cấu hình tìm kiếm tiếng Việt được migration tạo rõ ràng |

Index tối thiểu: slug unique, FK CategoryId/AuthorId, Status/Difficulty/PublishedAt và các tổ hợp thực tế phục vụ filter/sort; GIN SearchVector; hash refresh token; unique thứ tự step và ràng buộc primary image. Review EXPLAIN ANALYZE thay vì thêm index thiếu căn cứ vào mọi trường. Kiểm tra race trên slug, primary image, step numbering, refresh rotation và RowVersion.

### 3.3. Hợp đồng API chung

Base `/api/v1`, ID nghiệp vụ UUID, user ID string; datetime ISO 8601 UTC; auth Bearer; file multipart. Health endpoints ở root. Các schema sau là phương án thống nhất, cần ADR theo mục 2:

```json
{
  "data": [],
  "meta": {
    "page": 1, "pageSize": 12, "total": 0, "totalPages": 0,
    "hasNextPage": false, "hasPreviousPage": false
  }
}
```

Response đơn: `{ "data": { ... } }`; 204 không body. Auth payload trong data: accessToken, refreshToken, expiresIn và user. Quy ước duy nhất được đưa vào OpenAPI và TypeScript types trước khi bốn người nối UI.

```json
{
  "type": "VALIDATION_ERROR", "title": "Dữ liệu không hợp lệ",
  "status": 400, "detail": "Kiểm tra các trường được đánh dấu",
  "errors": { "title": ["Tiêu đề phải có từ 5 đến 200 ký tự"] }
}
```

Trả `X-Correlation-ID` để tra log. Dùng các mã ứng dụng từ phụ lục: AUTH_EMAIL_EXISTS, AUTH_INVALID_CREDENTIALS, AUTH_TOKEN_EXPIRED, AUTH_TOKEN_INVALID, AUTH_REFRESH_TOKEN_EXPIRED, AUTH_REFRESH_TOKEN_REVOKED, AUTH_GOOGLE_TOKEN_INVALID, AUTH_ACCOUNT_DISABLED, RECIPE_NOT_FOUND, RECIPE_SLUG_EXISTS, RECIPE_PUBLISH_INCOMPLETE, RECIPE_FORBIDDEN, RECIPE_CONCURRENCY_CONFLICT, CATEGORY_NOT_FOUND, CATEGORY_NAME_EXISTS, CATEGORY_DELETE_HAS_RECIPES, FILE_SIZE_EXCEEDED, FILE_MIME_INVALID, VALIDATION_ERROR, RATE_LIMIT_EXCEEDED.

Mã HTTP: 200 đọc/sửa; 201 tạo + Location khi có resource; 204 xóa/logout; 400 validation/file; 401 chưa xác thực/token sai; 403 thiếu quyền hoặc tài khoản disabled; 404 không tồn tại/đã xóa; 409 unique hoặc category còn recipe; 422 concurrency theo phương án D02; 429 có Retry-After; 500 lỗi không lộ stack; 503 dependency/health. Luồng login còn có 423 locked ở tr. 19, Google lỗi upstream 502 ở tr. 20: bổ sung OpenAPI sau chốt, không bỏ vì phụ lục A thiếu.

### 3.4. Toàn bộ màn hình và người thực hiện

| Route | Rendering theo SRS / điều kiện | Chủ trì |
|---|---|---|
| `/` | ISR 3600s, recipe nổi bật và categories public | TV2 |
| `/recipes` | SSR dynamic, lọc/sắp xếp/phân trang | TV2 |
| `/recipes/[slug]` | ISR 300s cho Published; Draft/Archived tải động riêng có quyền | TV3 nội dung; TV4 ảnh/SEO |
| `/categories` | ISR 3600s | TV2 |
| `/categories/[slug]` | ISR 600s public; không trộn Draft cá nhân | TV2 |
| `/auth/login` | CSR, email/password và Google; redirect nếu đã login | TV1 + TV2 Google |
| `/auth/register` | CSR, form và inline validation | TV1 |
| `/dashboard` | CSR, Author/Admin; chỉ số từ dữ liệu được phép đọc | TV1 |
| `/dashboard/recipes` | CSR danh sách của user hoặc quyền Admin, trạng thái | TV3 + TV4 action |
| `/dashboard/recipes/new` | CSR multi-step wizard | TV3 |
| `/dashboard/recipes/[id]/edit` | CSR owner/Admin | TV3 + TV4 ảnh |
| `/dashboard/categories` | CSR Admin | TV2 |
| `/profile` | CSR có auth | TV1 |
| `/search` | SSR kết quả FTS | TV2 |

Mọi màn hình có responsive, keyboard, loading/skeleton, empty state, inline error, toast. Mutation thích hợp có optimistic update và rollback; upload có tiến độ %. Meta/Open Graph/canonical đầy đủ cho trang public, noindex cho nội dung riêng tư.

## 4. Thứ tự thực hiện và tiến độ dự kiến

### 4.1. Thứ tự theo phụ thuộc

1. Đọc đề, chốt ADR/API/schema/quyền/cache, khóa phiên bản công cụ.
2. Dựng Git, CI, 4 layer backend, frontend, Compose và dịch vụ ngoài.
3. Migrations/seed, DTO, Problem Details, pipeline, logging, auth tối thiểu và bộ tài khoản kiểm thử.
4. Category trước Recipe để có FK; đồng thời hoàn thành tài khoản và form dùng chung.
5. Draft CRUD + Ingredients + Steps + Nutrition trước Publish.
6. Upload + storage + resize + primary image; tích hợp recipe detail.
7. Publish/Unpublish/Archive/Delete; bảo đảm quyền và invalidation đúng.
8. Search FTS + filter/sort/page trên Published; hoàn thiện UI public và dashboard.
9. Redis/OutputCache/ISR, SEO/sitemap, email, observability đầy đủ; đo và sửa hiệu năng.
10. Security, concurrency, resilience, backup/restore, load, E2E; deploy staging và demo.

Search/query có thể dựng từ seed ngay tuần 2, nhưng nghiệm thu tìm kiếm sau khi tích hợp publish. Auth/health/logging/test được làm từ đầu, không chờ cuối dự án. Luồng găng: schema + auth → category → draft → ingredient/step → publish → search/SEO → E2E/release.

### 4.2. Kế hoạch 6 tuần

| Tuần | TV1 — Thanh Tâm (Leader) | TV2 — Trường Vĩ | TV3 — Quốc Trung | TV4 — Trung Sơn | Cổng hoàn thành |
|---|---|---|---|---|---|
| 1 | Auth contracts/Identity/JWT, pipeline/CI; register/login tối thiểu | Category schema/CRUD nền, UI shell, Google contract | ERD/migrations, Draft CRUD nền/Nutrition, concurrency spike | Compose/Nginx/MinIO/health nền, storage contract, logout | G0 giữa tuần: stack chạy; G1 cuối tuần: đăng ký → đăng nhập → category → Draft |
| 2 | Profile/dashboard, lockout/rate limit, welcome email, auth tests | Category UI/detail, Google login, list/filter/sort/page, FTS đầu tiên | Ingredients/steps/wizard/detail, refresh rotation, ownership/version tests | Upload/metadata/primary/delete/resize + UI progress; publish/unpublish | G2: Draft đủ nguyên liệu/bước/ảnh; publish được; không sửa chéo owner |
| 3 | Hoàn thiện auth/error/logging; LAB FTS/cache/media/SEO | Search SSR, Redis/OutputCache/ISR; LAB Identity/concurrency/jobs | Hoàn thiện editor/detail/dashboard; LAB OAuth/media/jobs | Archive/delete/sitemap/robots/SEO/OTEL; LAB Identity/forms/FTS | G3: đủ 34 FR chạy tích hợp + test ban đầu; lab được triển khai từ tuần 1 |
| 4 | Hoàn tất lab cá nhân, security/frontend tests, CI coverage | Hoàn tất lab, k6/EXPLAIN/cache, responsive/a11y | Hoàn tất lab, E2E/concurrency/architecture tests | Hoàn tất lab, health/retry/restore/multi-instance/load | G4: 24/24 kỹ năng mỗi người có minh chứng; G5: coverage đạt, sửa lỗi chặn luồng/bảo mật |
| 5 | Tự deploy/restore, auth review, sửa lỗi và docs | Tự deploy/restore, search/cache/SEO review và số đo | Tự deploy/restore, editor/data/E2E review và số đo | Tự deploy/restore, vận hành/HTTPS/backup review và số đo | G6: staging hoàn chỉnh; performance/SEO/a11y có kết quả và giới hạn; 5 E2E pass |
| 6 | Demo tài khoản/hồ sơ + kỹ năng bổ sung; bàn giao | Demo danh mục/khám phá + kỹ năng bổ sung; bàn giao | Demo soạn thảo/dữ liệu + kỹ năng bổ sung; bàn giao | Demo xuất bản/media/vận hành + kỹ năng bổ sung; bàn giao | G7: regression, release, tài liệu và minh chứng đủ 4 người |

Mỗi tuần có một buổi tích hợp và một buổi demo ngắn. Test và lab được thực hiện song song từ tuần 1; các bài cần recipe/media đầy đủ nối tiếp sau G2. Dành khoảng 25% thời gian tuần 1–4 cho phần kỹ năng cá nhân còn thiếu, không dồn cả 24 nhóm kỹ năng vào tuần 4. Tuần 5 chốt tích hợp và số đo, tuần 6 dành cho sửa lỗi còn lại, regression, demo và bàn giao. Lịch 6 tuần giữ đủ phạm vi FR/NFR/CONS; nếu thiếu năng lực hoặc hạ tầng, ghi rõ trở ngại và điều chỉnh phần optional trước, không đánh dấu hoàn thành khi chưa có bằng chứng.

File giao việc theo từng người và từng tuần: [PHAN_CHIA_CONG_VIEC_6_TUAN.md](PHAN_CHIA_CONG_VIEC_6_TUAN.md). Các mốc G0–G7 giữ nguyên ý nghĩa để tương thích task A1–D7; nhiều mốc có thể cùng nằm trong một tuần.

## 5. PHẦN 1 — TV1 — Nguyễn Thanh Tâm (2312741 - Nhóm trưởng): Tài khoản, hồ sơ và nền tảng dùng chung

**Đầu ra:** người dùng đăng ký, đăng nhập email, xem/sửa hồ sơ, nhận welcome email; nền tảng auth và xử lý lỗi có thể được các phần khác dùng ngay. FR chính: AUTH-001/002/006/007, JOB-001, OBS-002. Review: Nguyễn Thanh Tâm (Tự rà soát & nghiệm thu).

| Task | Việc thực hiện theo thứ tự | Phụ thuộc | Điểm dự kiến | Nghiệm thu |
|---|---|---|---:|---|
| A1 | Chốt auth DTO, Identity model/roles, migration User, PBKDF2/JWT service; abstractions và DI | G0/ADR | 12 | User/Author/Admin seed; JWT đúng claims/TTL; không lộ hash |
| A2 | Register/Login commands + validators; unique email; lockout; middleware auth/rate limit | A1 | 15 | Auto-login theo ADR; 5 lần sai khóa 15 phút; generic credential error |
| A3 | Get/UpdateProfile query/command, ICurrentUser, form register/login/profile, dashboard shell | A2 | 13 | Chỉ sửa displayName/avatarUrl/bio; field error, skeleton/toast, responsive |
| A4 | WelcomeEmailJob + MailKit/SMTP/Mailhog; template và retry | Register commit | 8 | Email có tên và link app; thất bại retry 1/5/30 phút, failed log |
| A5 | Problem Details, LoggingBehavior/CorrelationId, redaction, CI backend và docs auth | A1 | 10 | Request traceable; log không chứa token/password; Scalar có auth schema |
| A6 | Lab cá nhân K01–K24, trọng tâm FTS, Redis, MinIO/resize, SEO, concurrency | G1 cho lab nền; G2 cho lab tích hợp | 25 | Code và test của TV1, demo độc lập theo mục 9 |
| A7 | Unit/integration/frontend/E2E auth; security review; dựng staging và bàn giao | A1–A6 | 17 | Coverage phần Application >=80%; login/register E2E; evidence đủ |
| | **Tổng** | | **100** | |

Test đặc thù: email trùng; password thiếu từng tiêu chí; role không lấy từ request; disabled vs lockout; User bị xóa sau cấp token; PATCH không đổi email/userName; log không lộ secret; lỗi SMTP không làm rollback đăng ký đã commit; lỗi enqueue được theo dõi/retry. TV1 không tự nhận hoàn thành refresh/Google/logout do TV2/TV3/TV4 thực hiện; phải tích hợp và học đủ qua lab.

Bàn giao sớm: auth DTO/interfaces, lấy currentUser, account fixtures và cách gắn Authorization header ở G1. Cuối tuần 1 bàn giao auth tối thiểu; từ tuần 2 các thành viên khác dùng auth thật, mock chỉ để phát triển UI trước hợp đồng.

## 6. PHẦN 2 — TV2 — Ngô Quốc Trường Vĩ (2312796): Danh mục, khám phá và tìm kiếm

**Đầu ra:** Admin CRUD danh mục; Guest khám phá công thức bằng trang chủ, danh mục, tìm kiếm tiếng Việt, lọc/sắp xếp/phân trang; Google login tích hợp thật. FR chính: CAT-001…005, RCP-001, SRCH-001…004, AUTH-003. Review: Nguyễn Thanh Tâm (Nhóm trưởng).

| Task | Việc thực hiện theo thứ tự | Phụ thuộc | Điểm dự kiến | Nghiệm thu |
|---|---|---|---:|---|
| B1 | Category entity/config/migration/seed, CRUD CQRS/validation/Admin policy | Schema + A1 | 12 | Name/slug unique; đổi tên giữ slug; xóa category còn recipe trả 409 |
| B2 | Category admin/list/detail UI, trang chủ, list recipes có scope/filter/sort/page | B1 + DTO Recipe | 14 | Trạng thái empty/loading/error; phân trang tổng đúng; không lộ Draft |
| B3 | FTS migration unaccent/pg_trgm/config/trigger/GIN; search handler/SSR UI | Recipe schema | 15 | “pho” tìm “phở”; q>=2; rank; chỉ Published; query parameterized |
| B4 | Google Auth.js callback, backend verify ID token/account link và UI button | A1 + D04 | 8 | User mới/cũ, token sai/hết hạn, email không verified và upstream lỗi có test |
| B5 | Redis category/search cache + invalidate, ISR/public metadata/canonical, query tuning | B1–B3 | 9 | TTL đúng ADR; EXPLAIN; cache không trộn user; AND filters |
| B6 | Lab cá nhân K01–K24, trọng tâm PBKDF2/refresh, concurrency, upload/resize, jobs | G1 cho lab nền; G2 cho lab tích hợp | 25 | Có code/test cho những kỹ năng ngoài module khám phá |
| B7 | Unit/integration/UI/E2E search, k6, responsive/a11y, deploy/docs | B1–B6 | 17 | Search E2E; evidence query/cache/SEO; cài và chạy độc lập |
| | **Tổng** | | **100** | |

Test đặc thù: category rỗng 200; slug không tồn tại 404; Author không được mutate category; category có Draft/Archived không xóa được; nhiều filter AND; sort allowlist và tie-breaker ổn định; page/pageSize sai; categoryId không tồn tại khi lọc trả rỗng; dấu/ký tự đặc biệt không gây lỗi tsquery; search luôn loại Draft/Archived/IsDeleted; `/recipes/search` không bị route slug bắt nhầm; cache bị làm mới sau publish/unpublish.

Bàn giao sớm: CategoryDto, RecipeSummaryDto, PagedResult, query params, dữ liệu danh mục seed. Tránh tạo endpoint FTS mới cho profile/category chỉ để đủ kỹ năng; TV1/TV3/TV4 thực hành FTS trên dataset recipe dùng chung ở nhánh cá nhân.

## 7. PHẦN 3 — TV3 — Huỳnh Quốc Trung (2312786): Soạn thảo công thức, nguyên liệu và bước làm

**Đầu ra:** Author/Admin tạo Draft, chỉnh sửa đầy đủ công thức/dinh dưỡng, quản lý nguyên liệu và các bước; chi tiết công thức và dashboard; token refresh rotation. FR chính: RCP-002/003/004/009/010, AUTH-004. Review: Nguyễn Thanh Tâm (Nhóm trưởng).

| Task | Việc thực hiện theo thứ tự | Phụ thuộc | Điểm dự kiến | Nghiệm thu |
|---|---|---|---:|---|
| C1 | Recipe aggregate, Nutrition owned, entity/config/migrations/index/audit/UoW/RowVersion | B1 schema + A1 | 14 | FK đúng, migration chạy trên DB sạch, rollback transaction, 2 writer không lost update |
| C2 | Create/Update/Detail CQRS, validators, ownership, slug, ETag/version contract | C1 | 13 | Tạo luôn Draft; AuthorId từ token; detail nested đúng quyền; conflict có UI reload |
| C3 | Ingredient/Step CRUD, thứ tự/renumber, validation, đồng thời thao tác child | C1–C2 | 13 | 1..N liên tục; ingredient quantity nullable theo ADR; child thuộc đúng recipe |
| C4 | Wizard/new/edit/dashboard list/detail UI, RHF/Zod, TanStack Query, metadata/JSON-LD cùng TV4 | C2–C3 + B2 | 12 | Lưu/sửa end-to-end; lỗi inline; optimistic rollback; tổng thời gian/dinh dưỡng đúng |
| C5 | RefreshToken handler/hash/rotation/family reuse, client refresh single-flight | A1 | 8 | Refresh cũ không dùng lại; transaction chống refresh đồng thời; không log token |
| C6 | Lab cá nhân K01–K24, trọng tâm Google, MinIO, resize/email/sitemap, vận hành | G1 cho lab nền; G2 cho lab tích hợp | 25 | Code/test cá nhân và demo đủ kỹ năng |
| C7 | Unit/integration/UI/create E2E, concurrency, docs và deploy | C1–C6 | 15 | Application coverage >=80%; edit cạnh tranh không ghi đè im lặng |
| | **Tổng** | | **100** | |

Test đặc thù: title biên; cookTime=0; servings<=0; CategoryId không tồn tại; nutrition null và 6 chỉ số; non-owner sửa/xem Draft bị từ chối; nested create thất bại rollback cả aggregate; StepNumber liên tục sau xóa; giữ unique khi reorder; lấy childId của recipe khác phải thất bại; hai refresh cùng token không tạo hai nhánh hợp lệ; refresh của user bị khóa/xóa; UI không chạy vòng lặp refresh vô hạn.

Bàn giao sớm: entity/DTO/status/RowVersion, endpoint draft và fixture đủ ingredient+step cho TV4 thử publish. TV3 chủ trì tích hợp migration trên main, nhưng mỗi người phải tự viết migration của mình; chủ trì không làm thay schema cho cả nhóm.

## 8. PHẦN 4 — TV4 — Nguyễn Hữu Trung Sơn (2312739): Xuất bản, ảnh, SEO và vận hành

**Đầu ra:** quản lý trạng thái công thức, upload/primary/delete ảnh và resize; sitemap, health/tracing; đóng gói/triển khai và độ bền; logout. FR chính: RCP-005/006/007/008, FILE-001/002, JOB-002/003, OBS-001/003, AUTH-005. Review: Nguyễn Thanh Tâm (Nhóm trưởng).

| Task | Việc thực hiện theo thứ tự | Phụ thuộc | Điểm dự kiến | Nghiệm thu |
|---|---|---|---:|---|
| D1 | Storage abstraction/MinIO config, upload/magic bytes/size, metadata/primary/delete API | A1 + C1 | 15 | 4 định dạng, <=5MiB, GUID path; primary đúng; unauthorized không ghi object |
| D2 | Resize job original/300×300/800×600, cleanup retry và race handling | D1 | 10 | URLs lưu DB; original fallback; retry idempotent; delete vs resize không tái sinh ảnh đã xóa |
| D3 | Publish/unpublish/archive/delete CQRS + ownership + cache/ISR invalidation; Logout | C2–C3 + A1 | 13 | Publish đủ step/ingredient theo ADR; public ẩn ngay khi unpublish/archive/delete; logout 204 |
| D4 | Image uploader/progress/gallery/primary UI, status action, Recipe JSON-LD/OG, sitemap/robots | D1–D3 | 12 | Progress/rollback; Published-only sitemap, cron 02:00 UTC; metadata đầy đủ |
| D5 | Compose/Nginx, health/OTEL/metrics, persistent volumes, backup/restore/multi-instance | G0 tăng dần | 12 | Probe đúng; trace nối HTTP→DB; restore được dữ liệu/file; HTTPS và no secrets |
| D6 | Lab cá nhân K01–K24, trọng tâm Identity/Google/refresh, forms, FTS, DB query | G1 cho lab nền; G2 cho lab tích hợp | 25 | Code/test cá nhân, đủ skills ngoài media/ops |
| D7 | Integration/UI/publish E2E, resilience/load/SEO, runbook và release | D1–D6 | 13 | Failover/retry có số liệu; file-size attack test; deploy lặp lại được |
| | **Tổng** | | **100** | |

Test đặc thù: giả extension/MIME, file trên giới hạn và đúng giới hạn, file hỏng/empty, magic bytes WebP/AVIF được xác minh đầy đủ thay vì chỉ nhìn 4 byte; primary lần đầu và khi xóa primary; hai request đặt primary đồng thời; xóa file không tồn tại idempotent; MinIO down; resize xong cập nhật cache; cleanup cả original/medium/thumbnail; retry không xóa object ngoài bucket/prefix được quản lý.

Job sitemap retry 2 lần theo mô tả riêng; resize/delete retry 3; welcome 3 theo lịch 1/5/30 phút. Dashboard `/hangfire` chỉ Admin. Khi nhiều worker cùng chạy sitemap phải có distributed lock. Backup pg_dump hàng ngày 03:00, giữ 30 ngày; **timezone backup cần nhóm chốt**, không suy ra UTC chỉ vì sitemap dùng UTC.

TV4 phụ trách khung triển khai nhưng cả 4 phải có thay đổi cấu hình, tự build/deploy và giải thích request đi qua Nginx. Nếu khối lượng thực tế tăng, TV1 hỗ trợ observability, TV2 SEO/sitemap và TV3 invalidation theo interfaces; không chuyển toàn bộ vận hành cho riêng TV4.

## 9. Ma trận kỹ năng bắt buộc cho cả 4 người

SRS không có một danh sách mang tên “skill” độc lập. Danh mục dưới đây trích từ công nghệ, CONS, FR, NFR, kiến trúc, kiểm thử và triển khai của toàn bộ PDF. Đây là tiêu chí đào tạo bổ sung theo yêu cầu chia nhóm: **mọi ô ở bốn cột phải có minh chứng cá nhân**. Những công cụ được SRS ghi optional là bài nâng cao, không tự nâng thành nghĩa vụ production.

Ký hiệu trong ô: **SP** = đóng góp vào sản phẩm chung; **LAB** = tự viết một phiên bản nhỏ trên nhánh `practice/TVn/...`, có test, được lưu và demo, không merge code trùng vào main. Mỗi LAB phải dùng stack thật của đề khi kỹ năng yêu cầu DB/cache/storage/provider; mock chỉ dùng cho unit/error tests, không thay toàn bộ integration.

| Mã / kỹ năng và nguồn | TV1 (Thanh Tâm) phải thực hiện | TV2 (Trường Vĩ) phải thực hiện | TV3 (Quốc Trung) phải thực hiện | TV4 (Trung Sơn) phải thực hiện |
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
| K24 Git/PR/review/CI/static analysis/architecture test/secret scan/docs (14, 42–43) | PR cá nhân + Tâm review + ADR/runbook + CI | PR cá nhân + Tâm review + ADR/runbook + CI | PR cá nhân + Tâm review + ADR/runbook + CI | PR cá nhân + Tâm review + ADR/runbook + CI |

### 9.1. Bài thực hành cá nhân thống nhất, tránh học mỗi người một kiểu

Mỗi người tạo nhánh riêng từ skeleton G1, dùng database/schema/bucket prefix riêng cho lab và không thao tác dữ liệu production. TVn tự thực hiện:

- **L1 — Danh tính và bảo mật:** register/login/PBKDF2; JWT; Google Code+PKCE/Auth.js + backend verify; refresh hash/rotation/reuse; logout; test Guest/Author/owner/non-owner/Admin/VerifiedAuthor. Nếu không có Google credentials, đánh dấu phần integration ngoài còn chờ; không coi mock là hoàn tất Google login.
- **L2 — Recipe nhỏ từ đầu đến cuối:** entity/owned Nutrition, migration/seed, Minimal API/CQRS/validators/UoW, query/mutation, RowVersion 2 writer, soft delete/audit; wizard RHF/Zod và detail. Ít nhất 1 Command và 1 Query do chính người đó viết.
- **L3 — Tìm kiếm và cache:** tự tạo FTS trigger/config/GIN, query rank và filter/sort/page; Redis cache-aside và OutputCache, invalidate sau sửa; không lộ Draft; tắt Redis để test fallback. Kèm EXPLAIN và k6.
- **L4 — Media và jobs:** upload/delete JPEG/PNG/WebP/AVIF đúng nội dung/kích thước; resize 2 size; SMTP gửi Mailhog; Hangfire fire-and-forget, delayed trong lab, recurring sitemap, retry và restart worker. Cùng một bộ 3 loại tác vụ giúp mỗi người học đủ email/image/XML, không phải thêm chức năng mới vào sản phẩm.
- **L5 — UI, SEO và vận hành:** SSR search + ISR published detail + CSR editor; TanStack Query/rollback/next/image; toàn bộ metadata/JSON-LD/robots/redirect; Serilog/Seq/OTEL/metrics/health; Docker/Nginx/HTTPS, backup/restore, 2 API instance và shared cache/jobs.
- **L6 — Chất lượng:** test xUnit/API/Jest/RTL/Playwright; đo coverage/performance/accessibility; architecture test/lint/secret scan; viết README/ADR và review. Mỗi người tự sửa ít nhất một lỗi có test chứng minh trước/sau.

Nếu phần SP đã đáp ứng đầy đủ một nội dung LAB thì dùng PR SP làm minh chứng, không viết lại chỉ để đủ số file. Phần còn thiếu vẫn phải tự bổ sung. Kỹ năng nâng cao optional như Polly circuit breaker, read replica, partition >1 triệu rows, presigned direct upload, domain events: cả 4 đọc/giải thích tradeoff; chỉ triển khai nếu còn thời gian và nhóm thống nhất.

### 9.2. Cách xác nhận đủ skill

Với mỗi Kxx, mỗi TVn phải có bản ghi `TVn-Kxx`: FR/NFR liên quan, đường dẫn code hoặc config, commit/PR, test/lệnh chạy, kết quả thực tế, ảnh/log/video phù hợp, reviewer và ngày xác nhận. Với ô gồm nhiều kỹ thuật, đánh dấu từng kỹ thuật con; thiếu một phần thì ô chưa hoàn thành.

Tổng tối thiểu **24 × 4 = 96 ô minh chứng**, không phải 96 bài triển khai trùng lặp. Một PR được tham chiếu nhiều ô nếu thật sự chứa các kỹ năng đó. Chỉ công nhận cá nhân hoàn thành khi **24/24 ô** có bằng chứng và demo độc lập, không lấy trung bình giữa thành viên để bù thiếu.

## 10. Ma trận bao phủ toàn bộ 34 yêu cầu chức năng

Các endpoint dưới đây là route chương 8, kết hợp luồng chương 3; khác biệt phải theo ADR ở mục 2. Prefix `/api/v1` được lược bỏ; health giữ ở root. Chủ trì chịu trách nhiệm giao đủ cả UI/BE/test, người khác bổ sung theo ma trận kỹ năng.

| FR | Trang | API / tác vụ | Chủ trì | Kiểm tra chấp nhận tối thiểu |
|---|---:|---|---|---|
| FR-AUTH-001 | 17–18 | POST /auth/register | TV1 | User Author + token theo ADR, email job; duplicate/password lỗi |
| FR-AUTH-002 | 18–19 | POST /auth/login | TV1 | Credential hợp lệ/sai, lockout 5 lần/15 phút, rate limit |
| FR-AUTH-003 | 19–20 | POST /auth/google | TV2 | Verify provider, user mới/link cũ, token invalid/upstream fail |
| FR-AUTH-004 | 20–21 | POST /auth/refresh | TV3 | Rotate, expiration, reuse/family, concurrent refresh |
| FR-AUTH-005 | 21–22 | POST /auth/logout | TV4 | Revoke token của user; idempotent 204 theo auth contract |
| FR-AUTH-006 | 22–23 | GET /auth/me | TV1 | Đúng user, không sensitive fields, 401/404 |
| FR-AUTH-007 | 23 | PATCH /auth/me | TV1 | Partial update hợp lệ, cấm sửa email/userName/role |
| FR-CAT-001 | 23–24 | GET /categories | TV2 | Count Published, sort, cache, empty 200 |
| FR-CAT-002 | 24–25 | GET /categories/{slug} | TV2 | Category + paged recipes; cache public/private tách biệt |
| FR-CAT-003 | 25–26 | POST /categories | TV2 | Admin, slug unique, 201/Location, invalidate |
| FR-CAT-004 | 26 | PUT /categories/{id} | TV2 | Admin, slug giữ nguyên, invalidate, 404 |
| FR-CAT-005 | 26–27 | DELETE /categories/{id} | TV2 | Admin; 409 nếu còn recipe; 204 khi được xóa |
| FR-RCP-001 | 27–28 | GET /recipes | TV2 | Scope đúng quyền; filters/page/sort; response tổng đúng |
| FR-RCP-002 | 28–29 | GET /recipes/{slug} | TV3 | Nested data đúng thứ tự, private status đúng quyền |
| FR-RCP-003 | 29–30 | POST /recipes | TV3 | Draft, current author, nested transaction, category/slug lỗi |
| FR-RCP-004 | 30–31 | PUT /recipes/{id} | TV3 | Owner/Admin, version conflict, invalidate |
| FR-RCP-005 | 31–32 | PATCH /recipes/{id}/publish và /unpublish | TV4 | Đủ dữ liệu, lặp trạng thái 200, visibility + invalidation |
| FR-RCP-006 | 32 | PATCH /recipes/{id}/archive | TV4 | Ẩn public, giữ dữ liệu, owner/Admin; unarchive chưa có route chuẩn |
| FR-RCP-007 | 32–33 | DELETE /recipes/{id} | TV4 | Xóa theo D08, không lộ trong search/cache; không mất ảnh cần restore |
| FR-RCP-008 | 33–34 | POST /recipes/{id}/images; PATCH/DELETE /recipes/{id}/images/{imageId} | TV4 | Upload/metadata/primary/delete, ownership, race; /primary theo D17 |
| FR-RCP-009 | 34–35 | POST /recipes/{id}/ingredients; PUT/DELETE /recipes/{id}/ingredients/{ingId} | TV3 | CRUD, validation, child ownership, invalidate |
| FR-RCP-010 | 35–36 | POST /recipes/{id}/steps; PUT/DELETE /recipes/{id}/steps/{stepId} | TV3 | CRUD, liên tục 1..N, unique/transaction, ownership |
| FR-SRCH-001 | 36–37 | GET /recipes/search | TV2 | FTS không dấu, rank, q>=2, Published-only, injection test |
| FR-SRCH-002 | 37 | Tham số lọc trên list/search | TV2 | categoryId/difficulty/maxCookTime/minServings kết hợp AND |
| FR-SRCH-003 | 37 | Tham số sort trên list/search | TV2 | Allowlist field; mặc định mới nhất; hướng sort theo ADR |
| FR-SRCH-004 | 37 | page/pageSize trên list/search | TV2 | page>=1, default12/max50, total/pages/next/previous đúng |
| FR-FILE-001 | 37–38 | IFileStorageService.UploadAsync | TV4 | S3 bucket culinary-blog; GUID path; 5MiB/4 MIME/magic bytes |
| FR-FILE-002 | 38 | IFileStorageService.DeleteAsync | TV4 | Idempotent; đúng object quản lý; retry 3; log failure |
| FR-JOB-001 | 38 | WelcomeEmailJob | TV1 | HTML tên/link; async sau register; retry 1/5/30 phút |
| FR-JOB-002 | 38 | ImageResizeJob | TV4 | 300×300 + 800×600, update URLs, original fallback/retry |
| FR-JOB-003 | 38–39 | SitemapGenerationJob cron 0 2 * * * UTC | TV4 | Published/categories/static pages, XML, retry2, distributed lock |
| FR-OBS-001 | 39 | GET /health, /health/live, /health/ready | TV4 | Tổng DB/Redis/MinIO; live process; ready DB+Redis 200/503 |
| FR-OBS-002 | 39 | Serilog/request + pipeline logs | TV1 | CorrelationId/path/user/status/duration/TraceId; redaction |
| FR-OBS-003 | 39 | OTEL tracing và metrics | TV4 | HTTP→EF trace, request count/duration/error + recipe created/published |

Chương 8 liệt kê **30 cặp method-route API nghiệp vụ và 3 health endpoints = 33** (không tính `/scalar`, `/hangfire`, Google callback, sitemap/robots và route alias /primary). Khi chốt ADR, xuất danh sách endpoint thực tế từ OpenAPI rồi đối chiếu; mỗi endpoint thực tế phải có ít nhất 1 happy path và 1 error case. Health/live dùng kịch bản process dừng/unreachable làm negative operational case, không giả rằng liveness phải trả lỗi do DB down.

Mức ưu tiên: SRS ghi Google/profile/category delete/archive là Should Have; kế hoạch vẫn giao đầy đủ theo yêu cầu người dùng. FILE/JOB/OBS được mô tả dạng bảng, không tự gán MoSCoW gốc chưa có. Không bỏ chúng khỏi nghiệm thu kỹ năng.

## 11. Nghiệm thu toàn bộ 30 yêu cầu phi chức năng

Số liệu dưới đây là **mục tiêu phải đo khi xây ứng dụng**, chưa phải kết quả hiện tại. Chủ trì tổ chức đo; cả 4 đóng góp minh chứng trên phần mình. Nguồn tr. 40–45.

| NFR | Tiêu chí / minh chứng phải có | Chủ trì |
|---|---|---|
| NFR-PERF-001 | Cache warm: GET cached p50<=150ms; mọi API p95<=500ms, p99<=1000ms. k6 + OTEL, ghi workload/hardware/cache state | TV2 |
| NFR-PERF-002 | >=100 concurrent users trên baseline 2vCPU/4GB; smoke→load→stress; tách bottleneck và đo lại khi thêm instance | TV4 |
| NFR-PERF-003 | Redis hit rate>=80% steady-state; TTL theo D10; create/update/delete/publish làm mới cache; có hit/miss counters | TV2 |
| NFR-PERF-004 | Không N+1, projection/eager loading; query>100ms có cảnh báo; EXPLAIN ANALYZE và review trước merge | TV3 |
| NFR-PERF-005 | LCP<=2,5s; CLS<=0,1; INP<=200ms; First Load JS<=200KB gzip; báo cáo Lighthouse + đo tương tác phù hợp, ghi rõ lab/field | TV2 |
| NFR-SEC-001 | Identity PBKDF2-HMACSHA512 >=100.000 iterations cấu hình/verify; password>=8 có hoa/thường/số/đặc biệt; không plaintext | TV1 |
| NFR-SEC-002 | HS256/JWT15m claims userId/email/roles/jti; RT7d hash SHA-256; rotation/family reuse; độ dài theo D05 | TV3 |
| NFR-SEC-003 | Auth10/phút/IP sliding; API100/phút/IP; upload5/phút/IP; 429 + Retry-After; proxy IP không bypass | TV1 |
| NFR-SEC-004 | FluentValidation, SQL parameterized, XSS sanitization+CSP, validate file size trước buffer, nội dung thật/GUID path | TV4 |
| NFR-SEC-005 | TLS>=1.2; HTTP redirect HTTPS; HSTS max-age31536000; CORS explicit origins; cookie nếu có theo phương án auth đã chốt | TV4 |
| NFR-SEC-006 | AuthorPolicy/AdminPolicy + ownership ở Application, write audit user/time, test Guest/non-owner/Admin | TV3 |
| NFR-SEC-007 | User Secrets dev/env prod; .env.example placeholders; secret scan; kế hoạch rotation signing key (SRS khuyến nghị 90 ngày) | TV1 |
| NFR-USE-001 | 320–767 một cột; 768–1199 hai cột; >=1200 layout desktop; Tailwind; test mobile/iOS/Android | TV2 |
| NFR-USE-002 | WCAG2.1 AA: semantic/ARIA/keyboard; contrast text>=4,5:1/UI>=3:1; NVDA/VoiceOver có kiểm thử | TV3 |
| NFR-USE-003 | RFC7807/error codes i18n-ready, inline field errors, 5xx thân thiện không stack trace | TV1 |
| NFR-USE-004 | Skeleton/empty/toast, optimistic update có rollback, upload progress %; mọi async có feedback | TV4 |
| NFR-REL-001 | Target uptime>=99,5%; maintenance báo trước48h; readiness10s; alert down>1phút; quy đổi giờ theo D21; không tuyên bố đạt SLA cả năm bằng test ngắn | TV4 |
| NFR-REL-002 | Global exception->500, DB reconnect/timeout30s, Redis fallback, Hangfire retry theo job; circuit breaker optional | TV4 |
| NFR-REL-003 | WAL/ACID; pg_dump03:00 giữ30ngày; volume MinIO/DB; refresh token sống qua restart; soft delete theo D08; restore drill | TV3 |
| NFR-MAINT-001 | SonarAnalyzer/StyleCop/EditorConfig; ESLint ruleset theo đề/Prettier; CI không compiler warnings; >=1 reviewer/PR | TV1 |
| NFR-MAINT-002 | Application unit line coverage>=80%; mỗi API happy+error; Jest/RTL; 5 critical Playwright flows | TV3 |
| NFR-MAINT-003 | README setup<5phút với prerequisites sẵn; XML/OpenAPI/Scalar; ADR; CHANGELOG/SemVer | TV1 |
| NFR-MAINT-004 | Architecture tests chống Domain/Application reference Infrastructure; CQRS đọc/ghi tách; giải quyết Identity ở D18 | TV3 |
| NFR-SCALE-001 | Stateless JWT, Redis shared state; lock singleton sitemap; nhiều Hangfire worker/shared PostgreSQL queue; test2 API | TV4 |
| NFR-SCALE-002 | Npgsql pooling max100/instance; B-tree/GIN theo workload; read replica/partition là optional/nâng cao | TV2 |
| NFR-SCALE-003 | Container tách service; Nginx upstream nhiều API; hồ sơ scale MinIO4+ nodes/CDN static; ghi rõ đã triển khai hay mới thiết kế | TV4 |
| NFR-SEO-001 | JSON-LD Recipe đầy đủ name/description/image/author/datePublished/times/yield/ingredients/instructions/nutrition; validate thực tế, không invent rating | TV3 |
| NFR-SEO-002 | Title<=60, description150–160; OG image1200×630 đủ OG fields; Twitter large image; canonical; Draft/Archived noindex | TV2 |
| NFR-SEO-003 | Sitemap XML loc/lastmod/changefreq/priority, cron02UTC; robots khai báo URL; external submission theo D26 | TV4 |
| NFR-SEO-004 | Slug không dấu/chữ thường/gạch nối; Published ổn định; đổi Draft có301; params chỉ filter/sort/page | TV3 |

Không dùng Lighthouse score thay thế mọi chỉ số CWV hay chứng minh INP thực địa. Báo cáo phải nói rõ thiết bị, mạng, dữ liệu, số mẫu, công cụ, thời điểm, cache cold/warm. NFR-SCALE-003 về MinIO distributed/CDN nếu chỉ có thiết kế vì hạ tầng hạn chế thì ghi **chưa nghiệm thu triển khai**, không đánh dấu đạt toàn bộ.

## 12. Kiểm thử, tích hợp, minh chứng và bàn giao

### 12.1. Quy tắc phối hợp

- Nhánh tính năng `feat/TVn-ma-task`; nhánh lab `practice/TVn/Lx`. PR nhỏ theo use case, ghi FR/NFR/Kxx và bước kiểm thử.
- Quy trình review PR: Nguyễn Thanh Tâm (Nhóm trưởng) trực tiếp review và duyệt toàn bộ PR của các thành viên (Trường Vĩ, Quốc Trung, Trung Sơn) cũng như tự kiểm tra code của mình trước khi merge vào nhánh chính.
- API/shared DTO/schema thay đổi phải cập nhật contract trước và báo người tích hợp; không tự sửa tên field rồi để frontend khác lỗi.
- Mỗi người tự viết migration. TV3 (Quốc Trung) điều phối thứ tự/rebase migrations; test migrate DB sạch và nâng cấp từ bản trước trước merge.
- Nguyễn Thanh Tâm điều phối pipeline/auth/review, Ngô Quốc Trường Vĩ phụ trách public UI/query/cache, Huỳnh Quốc Trung phụ trách aggregate/schema, Nguyễn Hữu Trung Sơn phụ trách deploy/storage/jobs. Các file dùng chung chỉ một người sửa tại một thời điểm theo task đã nhận.
- Shared service tích hợp một bản; bài lab giữ nhánh/tag/commit riêng. Không copy folder bốn bản backend/frontend vào sản phẩm để chứng minh chia việc.
- Không đưa secret hoặc dữ liệu tài khoản thật vào evidence. Log cần redaction; account demo dùng dữ liệu seed.

### 12.2. Bộ kiểm thử thực sự bắt buộc

| Tầng | Phạm vi | Điều kiện qua |
|---|---|---|
| Unit xUnit | Domain rule, handlers, validators, token rotation/cache invalidation logic | Application line coverage>=80%; thêm test biên/negative, không chỉ happy path |
| Integration API | Mọi method-route, PostgreSQL/Redis/MinIO/Hangfire thực trên môi trường test | >=1 happy +1 error mỗi route, thêm đầy đủ ma trận quyền cho route nhạy cảm |
| Frontend Jest/RTL | Form validation, state, error mapping, optimistic rollback, gallery/progress | Người dùng thao tác đúng; lỗi API hiển thị được; không chỉ snapshot |
| E2E Playwright | Register, Login, Create Recipe, Publish, Search | 5 luồng chạy trên stack tích hợp; Google thêm flow riêng nếu credentials có |
| Security | Auth/ownership/rate limit, token reuse, XSS/SQL/tsquery injection, MIME/size/path, secrets | Không lộ Draft/cross-user/secret; bị từ chối đúng contract |
| Concurrency | 2 writer recipe, refresh reuse, slug race, primary image, step renumber | Không lost update, không nhiều primary, không token family sai |
| Resilience | Dừng/restart DB/Redis/MinIO/worker; retry/queue; backup/restore | Theo NFR/ADR; dữ liệu/file phục hồi; lỗi có trace/log và UI phù hợp |
| Performance/SEO/a11y | k6/query plan/CWV/bundle/cache; JSON-LD/robots/meta; keyboard/screen reader | Số đo mục11, ghi limitation; lỗi chưa đạt có ticket |

Quy trình test tích hợp quan trọng: đăng ký Author A/B → Admin tạo danh mục → A tạo Draft + nguyên liệu + bước + ảnh → B không sửa được → A publish → Guest tìm “pho” thấy “phở” → A unpublish → Guest không thấy ở list/search/ISR/cache/sitemap sau cơ chế cập nhật đã chốt. Thử recipe đã xóa và private response không được cache lẫn với public. Sitemap chạy hàng ngày nên định nghĩa rõ thời gian cập nhật hay thêm trigger refresh sau publish/unpublish bằng ADR; không hứa “ngay lập tức” cho sitemap nếu chỉ có cron.

### 12.3. Definition of Done cho một task

Task hoàn thành khi: đúng FR và ADR; có UI nếu người dùng cần thao tác; có CQRS/validator/quyền; DB migration/index/transaction đúng; cache/job/log cần thiết đã tích hợp; các test phù hợp pass; CI/lint/architecture/secret scan pass; Scalar/types/docs cập nhật; có PR được review; ghi evidence cá nhân. “API trả 200 trong Postman” chưa đủ để nghiệm thu một tính năng xuyên suốt.

### 12.4. Mẫu sổ công việc và minh chứng

```text
Task: C3 / FR-RCP-010
Người làm: TV3
Trạng thái: Chưa làm | Đang làm | Chờ tích hợp | Chờ review | Hoàn thành
Skill: K02,K03,K04,K05,K06,K07,K17,K21
Phụ thuộc: C1,C2; ADR D16,D19
Đầu ra: endpoint + handler + entity/config + UI + tests
PR/commit/code/config: [điền khi có]
Lệnh/test và môi trường: [điền thực tế]
Kết quả/ảnh/log/coverage: [điền thực tế]
Reviewer/ngày: [điền khi được xác nhận]
Lỗi còn lại/ảnh hưởng: [ghi rõ]
```

| Thành viên | MSSV | Phần nghiệp vụ | Skill SP+LAB | Tự deploy/restore | Review (Tâm duyệt) + demo | Trạng thái ban đầu |
|---|---|---|---|---|---|---|
| TV1 — Nguyễn Thanh Tâm (Nhóm trưởng) | 2312741 | A1–A7 | 0/24 đã xác nhận | Chưa làm | Chưa làm | Chưa làm |
| TV2 — Ngô Quốc Trường Vĩ | 2312796 | B1–B7 | 0/24 đã xác nhận | Chưa làm | Chưa làm | Chưa làm |
| TV3 — Huỳnh Quốc Trung | 2312786 | C1–C7 | 0/24 đã xác nhận | Chưa làm | Chưa làm | Chưa làm |
| TV4 — Nguyễn Hữu Trung Sơn | 2312739 | D1–D7 | 0/24 đã xác nhận | Chưa làm | Chưa làm | Chưa làm |

### 12.5. Demo và release cuối kỳ

Mỗi người dự kiến 15–20 phút: 5–7 phút tính năng chính; 5–7 phút kỹ năng ngoài phần chính (auth/FTS/cache/file/job/SEO luân phiên); phần còn lại giải thích architecture, schema, test và một tình huống lỗi do reviewer chọn. Mỗi người tự dựng app từ checkout sạch theo README, phục hồi một backup và tìm log/trace của request vừa thao tác.

Checklist release:

- [ ] 34/34 mã FR có liên kết implementation + test + người chịu trách nhiệm.
- [ ] 30/30 mã NFR có kết quả đo/kiểm tra hoặc ghi rõ chưa đạt và lý do; không tự coi thiết kế là đã triển khai.
- [ ] 10/10 CONS được kiểm tra; các ADR mâu thuẫn đã chốt với người có thẩm quyền.
- [ ] 4 người × 24 nhóm kỹ năng có minh chứng, demo và reviewer xác nhận.
- [ ] 33 endpoint baseline và mọi route phát sinh theo ADR được đối chiếu OpenAPI, có test phù hợp.
- [ ] 5 critical E2E pass; Application coverage>=80%; không còn lỗi chặn luồng hoặc lộ dữ liệu.
- [ ] Docker/Compose/Nginx/HTTPS/secrets/volumes/backup/restore được chạy thật.
- [ ] README, Scalar, ADR, CHANGELOG, báo cáo và tài khoản demo bàn giao đầy đủ.
- [ ] Ghi rõ production dependencies chưa cấu hình hoặc giới hạn hạ tầng/đo lường còn tồn tại.

**Nguyên tắc nghiệm thu cuối:** sản phẩm hoàn thành là điều kiện của cả nhóm; đủ kỹ năng là điều kiện riêng của từng người. Người làm nhiều ở một kỹ năng không bù cho người khác chưa thực hành kỹ năng đó.
