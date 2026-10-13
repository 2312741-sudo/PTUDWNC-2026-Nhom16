# docs/ — Chỉ mục tổng hợp tài liệu (skill cho agent)

> **Mục đích file này**: thay cho việc tự quét/đọc toàn bộ `/docs`, đây là bản đồ định tuyến — mỗi mục ghi *nội dung tóm tắt*, *sự thật đáng chú ý* và *khi nào cần mở file đó*. Agent/developer đọc tới đây là đủ để quyết định mở file chi tiết nào (chỉ mở khi cần số liệu đầy đủ).
>
> Cập nhật lần cuối: **09/10/2026**. Khi thêm tài liệu mới, **bắt buộc** cập nhật bảng chỉ mục bên dưới.

---

## 1. Dự án trong 30 giây

**Culinary Blog** — nền tảng chia sẻ & khám phá công thức ẩm thực. Đồ án môn PTUDWNC, Nhóm 16, HK1 2026. Repo: `2312741-sudo/PTUDWNC-2026-Nhom16`.

| Tầng | Công nghệ |
|---|---|
| Backend | .NET 10 Minimal APIs, Clean Architecture, CQRS + MediatR, FluentValidation, ASP.NET Core Identity, JWT HS256 (access 15' / refresh 7 ngày hash SHA-256), RFC 7807, Serilog, OpenTelemetry, Hangfire |
| Frontend | Next.js 15 App Router + TypeScript, Tailwind CSS, Auth.js v5, TanStack Query, React Hook Form + Zod |
| DB / Cache / Storage | PostgreSQL 16 (EF Core 10 Code First, FTS `tsvector`+`unaccent`+GIN), Redis 7, RustFS S3 (tên biến `Minio__*`, code vẫn `MinioStorageService`) |
| Infra | Docker Compose (dev/staging), Nginx (8080), GitHub Actions CI, k6, Lighthouse CI, Seq, MailHog |
| Test | xUnit, Jest+RTL, Playwright, k6, architecture tests, secret scan |

**Cổng**: FE `3000` · API `5080` · Nginx `8080` · PG `5432` · Redis `6379` · S3 `9000/9001` · MailHog `1025/8025` · Seq `5341`.
**Nhóm API**: prefix `/api/v1` → `/auth/*`, `/categories/*`, `/recipes`, `/recipes/search`, `/categories/{slug}/recipes`, `/recipes/{id}/images`, `/recipes/{id}/ingredients|steps`, `/me/recipes*`, `/resources/images/{key}`; health ở gốc (`/health`, `/health/live`, `/health/ready`); `/scalar/v1` (API docs), `/hangfire` (Admin).
**Quy ước chung**: response bọc `{ data, meta? }`; upload ảnh ≤ 5 MiB (JPEG/PNG/WebP/AVIF, xác thực magic bytes); `pageSize` default 12 / max 50.

**Thành viên** (reviewer & nghiệm thu toàn bộ: **TV1 Nguyễn Thanh Tâm**):

| TV | Họ tên | MSSV | Phân hệ | Task |
|---|---|---|---|---|
| TV1 | Nguyễn Thanh Tâm *(Nhóm trưởng)* | 2312741 | Auth, hồ sơ, bảo mật, CI, worker | A1–A7 |
| TV2 | Ngô Quốc Trường Vĩ | 2312796 | Category, tìm kiếm FTS, Google login | B1–B7 |
| TV3 | Huỳnh Quốc Trung | 2312786 | Soạn thảo Recipe (aggregate, concurrency), RefreshTokens | C1–C7 |
| TV4 | Nguyễn Hữu Trung Sơn | 2312739 | Storage/ảnh, publish, deploy/ops, giám sát | D1–D7 |

---

## 2. Cấu trúc thư mục `/docs`

```
docs/
├── README.md                  ← file này (chỉ mục, ĐỌC TRƯỚC MỌI THỨ)
├── KE_HOACH_DU_AN.md          ← NGUỒN GỐC: quy ước, FR/NFR, ma trận K, kiến trúc
├── PHAN_CHIA_CONG_VIEC_6_TUAN.md  ← phân công + tiến độ thực tế theo tuần
├── ERD.md                     ← schema DB + hành vi xóa/chỉ mục
├── *_CONTRACT.md              ← 6 hợp đồng API (source of truth khi gọi/sửa endpoint)
├── HUONG_DAN_CAI_DAT_*.md     ← chạy app lần đầu
├── HUONG_DAN_TEST_APP.md      ← chạy test / cấu trúc test / CI
├── RUNBOOK.md                 ← vận hành staging (backup, failover, scale, trace)
├── RUNBOOK_SOAN_THAO_CONG_THUC.md ← debug module soạn thảo recipe (dev)
├── agent-skill/               ← skill step-by-step cho agent (xem mục 4.6)
├── root/                      ← bộ SRS (yêu cầu phần mềm) — bản chuẩn v1.1.1
├── adr/                       ← Architecture Decision Records (3 series, XEM MỤC 5)
├── proposal/                  ← 10 đề xuất cải thiện (DE_XUAT_*, nhiều cái CHỜ QUYẾT ĐỊNH)
├── report/                    ← 6 báo cáo lỗi/sự cố (đã sửa / chưa sửa)
└── evidence/                  ← bằng chứng nghiệm thu theo TV1–TV4, theo tuần, ma trận K01–K24
```

---

## 3. Chỉ mục file mức gốc `docs/`

| File | Nội dung tóm tắt | Mở khi nào |
|---|---|---|
| **KE_HOACH_DU_AN.md** (69KB) | Hồ sơ dự án + kế hoạch 6 tuần. Chứa: 10 ràng buộc CONS, bảng quyết định **D01–D29**, ma trận **34 FR / 30 NFR / 24 kỹ năng K01–K24**, kiến trúc, quy tắc test/DoD/PR, cấu trúc repo dự kiến, quyết định đã chốt C01–C09. | Cần trả lời bất kỳ câu hỏi *"quy ước/chỉ tiêu/ai chủ trì FR nào/phụ thuộc ra sao"*. **Đây là tài liệu gốc, ưu tiên trên mọi tài liệu khác.** |
| **PHAN_CHIA_CONG_VIEC_6_TUAN.md** (35KB) | Giao việc từng tuần cho 4 TV + **tiến độ thực tế** (số test, PR đang chờ, block còn mở, kỹ năng đã xác nhận). Cập nhật 03/10/2026: **210/210 test pass**, build 0 warning, QA FE–BE 41/41. | Cần biết *ai làm gì, tuần nào, đang block ở đâu, PR nào chờ review*. |
| **ERD.md** | ERD Mermaid đối chiếu SRS v1.1.1: `Categories`, `Recipes` (+ `RecipeNutrition` owned), `RecipeSteps/Ingredients/Images`, `RefreshTokens`, AspNet*; FK RESTRICT/CASCADE; Recipe **soft delete**, Category **hard delete**; `Difficulty` **bắt đầu từ 1**; 7 điểm chênh lệch cần sửa trong code. | Tra schema, khóa ngoại, hành vi xóa, chỉ mục, lý do bất thường về kiểu dữ liệu. |
| **AUTH_CONTRACT.md** | 5 endpoint auth (`register/login/me/logout`), wrap `{data}`, password 8–128 ký tự, lockout 5 lần/15' → **423**, rate limit 10 req/phút/IP → **429 + Retry-After: 60**, JWT 15'. | Sửa/tích hợp luồng auth thuần; cần shape response & mã lỗi. |
| **GOOGLE_AUTH_CONTRACT.md** | Google OAuth: FE Auth.js v5 Code+PKCE, BE nhận `idToken` → verify bằng `GoogleApis.Auth`. `POST /api/v1/auth/google`, lỗi 400/401/403/502. Env: `AUTH_*`, `NEXT_PUBLIC_API_URL=http://localhost:5080/api/v1`. | Sửa luồng Google login hoặc biến môi trường liên quan. |
| **CATEGORY_CONTRACT.md** | CRUD `/api/v1/categories`, hard delete **409** nếu còn recipe, cache TTL **60'** (C03), Name/Slug UNIQUE, DTO + mã lỗi RFC 7807. | Gọi/sửa API danh mục; biết ràng buộc xóa/sinh slug. |
| **RECIPE_LIST_CONTRACT.md** | Chuẩn phân trang **mọi** list recipe: `{data, meta:{page,pageSize,total,totalPages,hasNextPage,hasPreviousPage}}`; allowlist sort; public chỉ `Published` + cache, dashboard `no-store`. | Cần format list/phân trang/quy tắc cache public vs private. |
| **SEARCH_AND_DISCOVERY_CONTRACT.md** | `GET /recipes` (lọc) + `GET /recipes/search` (FTS tiếng Việt không dấu, `q` 2–200 ký tự, `ts_rank`); chuẩn hóa qua `SlugHelper`+`unaccent`. | Sửa tìm kiếm, kiểm tra hành vi FTS không dấu. |
| **IMAGE_CONTRACT.md** (17KB) | Upload `/recipes/{id}/images` (≤5 MiB, magic bytes), proxy `GET /resources/images/{key}` (Published công khai cache 3600s, Draft 403/`no-store`), resize **Hangfire + ImageSharp** (300×300, 800×600), presigned URL 10' (PA-3), chỉ mục `ux_recipe_images_one_primary`. Lỗi: `file.*`, 422 `recipe.version_conflict`. | Sửa upload/hiển thị/resize ảnh, proxy media, presigned, bug 422/403/500 ảnh. |
| **HUONG_DAN_CAI_DAT_VA_CHAY_CHUONG_TRINH.md** | **Entry point "chạy được app"**: prerequisite (.NET SDK 10, Node ≥20, Docker), 6 container, `.env`, lệnh `--migrate`/`--seed`, bảng URL–cổng, tài khoản seed, test, troubleshooting. | Chạy app lần đầu; tra cổng/tài khoản/env. |
| **HUONG_DAN_TEST_APP.md** | Testing pyramid (Unit → Integration → Architecture → E2E Playwright), nhóm test, lệnh `dotnet test` + filter, 4 bước CI backend, kịch bản cURL (423/429/409…). **Lưu ý: số "54/54" trong file đã cũ.** | Chạy/kiểm thử, viết test mới, đối chiếu CI trước khi commit. |
| **RUNBOOK.md** | Vận hành staging (TV4): dựng stack (`docker-compose.staging.yml`), backup/restore (`deploy/backup-db.sh`, giữ 30 ngày, drill 14/14 bảng), failover Redis-down, scale 2 API (5080+5081), k6 ngưỡng p95 200/800/500ms, tra TraceId → Seq. | Câu hỏi vận hành/hậu cần (KHÔNG phải hướng dẫn cài dev). |
| **RUNBOOK_SOAN_THAO_CONG_THUC.md** | Runbook module soạn thảo (TV3): lệnh chạy dev đúng chuẩn (`. .\.secrets.local.ps1`, `--migrate` **không** dùng `dotnet ef database update`), bảng lỗi thường gặp (422 concurrency, 403, DLL khóa, JSON strict 400, SLOW_SQL…). | Debug khi chạy/sửa module recipe editor. |

---

## 4. Các thư mục con

### 4.1. `docs/root/` — Bộ SRS

| File | Vai trò |
|---|---|
| `SRS_Culinary_Blog_v1.1.1 Detail.md` (142KB) | **BẢN CHUẨN (canonical) — toàn văn SRS v1.1.1** (Approved 16/09/2026), chương 1–8 + phụ lục, đã áp C01–C09 và chuẩn §8.1 Auth. Mọi thay đổi sau 1.1.1 phải qua Change Request. |
| `SRS_Culinary_Blog_v1.1.1.md` (4.8KB) | Bìa/tóm tắt v1.1.1: lịch sử phiên bản, bảng 9 mâu thuẫn C01–C09, tóm tắt 7 endpoint Auth §8.1. Đọc nhanh thay vì mở bản 142KB. |
| `SRS_Culinary_Blog_v1.0.0.md` (141KB) + `.pdf` | Bản gốc lịch sử 04/06/2026 (convert từ PDF, có sai trình bày). Chỉ dùng để đối chiếu quá khứ — **không** dùng làm yêu cầu hiện hành. |
| `SRS_Contradictions_Report.md` | Bảng 9 mâu thuẫn nội tại C01–C09 của v1.0.0 + quyết định tương ứng trong v1.1.1. Đã giải quyết toàn bộ. |

> **Quy tắc**: trích dẫn yêu cầu → v1.1.1. SRS là "spec & record lịch sử", **không sửa hồi tố** (ví dụ SRS vẫn ghi MinIO trong khi hạ tầng dev đã là RustFS — xem ADR-TV4-002).

### 4.2. `docs/adr/` — Architecture Decision Records (**3 series, TRÙNG SỐ**)

| Series | File | Phạm vi | Ghi chú trạng thái |
|---|---|---|---|
| `NNNN-topic.md` | `0001-auth-week1` | Auth nền tảng (TV1) | *Đề xuất, chưa xác nhận chính thức* |
| | `0002-category-and-google-auth-week1` | Category + phân trang D11 + Google D04 (TV2) | **Accepted** 14/09 |
| | `0003-category-and-search-caching-week3` | Cache-Aside TTL phân cấp + FTS GIN (TV2) | **Accepted** 28/09 |
| | `0004-staging-deployment-and-disaster-recovery` | Staging 6 service + BCP/DR, RTO<2', RPO=0 | **Accepted** 05/10 (G6) |
| `ADR-000N-topic.md` | `ADR-0001-recipe-schema-va-concurrency` | Schema Recipe aggregate + OCC (TV3) | *TV3 tự chốt cho C1, nhóm chưa họp* |
| | `ADR-0002-recipe-version-schema-va-nap-du-lieu` | RowVersion 2 tầng, 422 `version_conflict`, `AsSplitQuery` | *Đề xuất — chờ TV1 review* |
| `ADR-TV4-NNN-topic.md` | `ADR-TV4-001-van-hanh-storage-logout-tuan-1` | Health live/ready, bucket private, logout Bearer | *Đề xuất, áp dụng dev* |
| | `ADR-TV4-002-doi-minio-sang-rustfs` | **MinIO gỡ image → đổi dev/CI sang RustFS 1.0.0**; code vẫn `Minio*`; CI rule `Skipped=0`; prod KHÔNG dùng RustFS | *Đã áp dụng dev/CI* |
| | `ADR-TV4-003-quy-uoc-soft-delete` | Recipe **soft** delete / Category **hard** delete (C07) — chốt quy ước | Chốt 09/10 |
| | `ADR-TV4-004-pin-imagesharp-3-1-12` | Pin ImageSharp 3.1.12 + `NuGetAuditSuppress` 5 advisory (bản vá 4.1.2 trả phí) | Chốt 09/10 |

> ⚠️ **Cảnh báo**: `0001/0002` và `ADR-0001/ADR-0002` **đụng số hiệu** — khác series, khác tác giả. Luôn ghi rõ tiền tố đầy đủ khi trích dẫn.
> Không phải ADR nào cũng "Accepted" — kiểm tra phần trạng thái trước khi coi là quyết định chính thức.

### 4.3. `docs/proposal/` — 10 đề xuất (`DE_XUAT_*`)

| File | Vấn đề | Trạng thái |
|---|---|---|
| `01_LOI_STORAGE_TRA_503_CO_MA_LOI` | Storage lỗi bị nuốt thành 500 → trả **503 `storage.unavailable`** | Đã code, chờ review #20 |
| `02_FAIL_FAST_KHI_THIEU_CAU_HINH` | Thiếu credential vẫn boot + `/health` Healthy → fail-fast | Đã code, chờ review #21 |
| `03_NAP_FILE_DOT_ENV` | App .NET không nạp `.env` → bug 500 | ✅ Xong (PR #19) |
| `04_HEALTH_CHECK_STORAGE_XAC_THUC` | Health storage chỉ TCP probe → probe xác thực thật | Đã code, chờ #22 |
| `05_XEM_ANH_DRAFT_TRONG_WIZARD` | Ảnh Draft không xem được trong wizard (block **B5**, cần TV3) | Đã chốt PA-3 presigned, **chưa code** |
| `06_TAI_TAO_USER_ADMIN_DE_SEED` | Không có user Admin → block **B6** (cần TV1) | Đã chốt CLI `--promote-admin`, **chưa code** |
| `07_NOI_DAT_LICH_BACKUP` | Chưa chốt nơi đặt lịch backup 03:00 | 🔴 Chờ TV1 quyết định |
| `08_KHO_LUU_BACKUP_30_NGAY` | GitHub artifact chỉ giữ 7 ngày, yêu cầu 30 ngày | 🔴 Chờ TV1+TV2 |
| `09_ROTATE_KHOA_JWT_DA_LO` | `Jwt:SigningKey` + PG password còn trong **git history** → rotate trước, xóa history sau | 🔴 Chờ TV1 (không thể hoàn tác) |
| `DE_XUAT_GIAI_QUYET_D23_D27` | Chốt D23 (queue resize → PA-1 Hangfire) và D27 (ảnh private → PA-2 proxy) | Chờ nhóm/GV xác nhận |

### 4.4. `docs/report/` — Báo cáo lỗi

| File | Sự cố | Trạng thái |
|---|---|---|
| `BAO_CAO_LOI_UPLOAD_ANH_500` | Upload ảnh 500 do `MinioOptions` rỗng | ⚠️ Bản **TRƯỚC khi sửa** (28/09) |
| `BAO_CAO_LOI_UPLOAD_ANH_500_DA_SUA` | Bản sửa + nguyên nhân thứ 2 (mật khẩu PG lệch) + test chống tái diễn | ✅ Đã sửa |
| `BAO_CAO_LOI_500_TRANG_SEARCH` | `/search` 500: RSC truyền arrow function cho `<select>` | File ghi "CHƯA SỬA" nhưng **code đã sửa** (client component `SearchFilterSelect`) |
| `BAO_CAO_KIEM_TRA_LOI_422_TAI_ANH_TV3_GUI_TV4` | 422 `version_conflict` khi upload ảnh: thiếu `ValueGeneratedNever()` cho `RecipeImage.Id` | ✅ Sửa qua PR #29 (`c624b9f`) |
| `BAO_CAO_GIAI_QUYET_XUNG_DOT_MERGE_TV2_TUAN2` | 7 file conflict khi merge TV2, chuẩn hoá key token | ✅ Đã giải quyết (83/83 test lúc đó) |
| `BAO_CAO_GIAI_QUYET_MAU_THUAN_SRS` | 9 mâu thuẫn C01–C09 trong SRS v1.0.0 | ✅ Đã giải quyết trong v1.1.1 |

### 4.5. `docs/evidence/` — Bằng chứng nghiệm thu (161 file)

Cấu trúc: `evidence/<TV>/<tuần>/` — TV1/TV2/TV3 phần lớn file phẳng `TUAN_n.md`; TV4 tổ chức `Tuan01`–`Tuan05` (+ `logs/`, `report/`, `plan/`).

**Thành viên theo TV** (từ tên `.docx` + header file): TV1 Tâm 2312741 · TV2 Vĩ 2312796 · TV3 Trung 2312786 · TV4 Sơn 2312739.

**Quy ước tên file**:

| Tiền tố | Ý nghĩa |
|---|---|
| `KE_HOACH_TUAN_n_*` | Kế hoạch đầu tuần (task, nhánh, reviewer, cổng) |
| `MO_TA_CONG_VIEC_TUAN_n` | Chi tiết việc + Definition of Done + ma trận FR/NFR/K |
| `TRANG_THAI_THUC_HIEN_TUAN_n` | Trạng thái cập nhật theo ngày (% từng khối) |
| `SO_EVIDENCE_TUAN_n` | **Sổ bằng chứng cuối tuần** — bảng 24 ô K + chứng mới + mẫu bản ghi |
| `HANDOFF_TV4_TUAn*` | Bàn giao cuối tuần (build/test/E2E, việc blocked, hướng tiếp tục) |
| `TUAN_n.md` (TV1–TV3) | Báo cáo nghiệm thu tuần, mẫu 3 mục: bản ghi chứng / test tự động / bàn giao |
| `BAO_CAO_LAB_nn` | Báo cáo Lab môn học (`.md` song song `.docx` nộp) |
| `BANG_MINH_CHUNG_K01_K24` | Sổ minh chứng tổng 24 ô K của 1 người (TV3: 22 XONG / 2 CHƯA = K18, K24) |
| `Kxx_*` | Bằng chứng gắn thẳng 1 ô kỹ năng (log NVDA, EXPLAIN, Lighthouse, secret scan…) |
| `MAPPING_K01_FR_NFR_ADR_EVIDENCE` | Bảng đối chiếu **K ↔ FR/NFR ↔ ADR ↔ evidence** cho người review |
| `HUONG_DAN_CHAY_TV4` | Hướng dẫn dựng/mở môi trường riêng máy TV4 (466 dòng, có bảng 10 sự cố L1–L10) |
| `logs/` | Log chạy thật: `baseline_*`, `k6_*`, `staging_*`, `lab_l*`, `scan_secrets*` |

**File index quan trọng cần đọc trước khi tra evidence**:
- `TV4/MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` — 4 bảng A–D: trạng thái từng ô K (🟡12/🔴4/⬜8, **0/24 xác nhận** tại 30/09), gap NFR chưa có bằng chứng.
- `TV3/BANG_MINH_CHUNG_K01_K24.md` — trạng thái ô K của TV3.
- `TV4/Report/README.md` — thư mục `Report/` là **báo cáo phi chính thống**, KHÔNG dùng làm căn cứ nghiệm thu, phải đối chiếu `Tuan04/SO_EVIDENCE_TUAN_4.md`.

> **Lệch chuẩn đã biết**: tên tuần `TUAN_n` vs `Tuan0n` vs `tuan3` không đồng nhất; TV2 thiếu `TUAN_5`, TV3 thiếu `TUAN_1/2`; TV4 không có `TUAN_n.md` (tham chiếu trong `MAPPING_...` dòng 51–52 trỏ file không tồn tại).

### 4.6. `docs/agent-skill/` — Skill step-by-step cho agent

Mỗi file = 1 skill viết theo template (frontmatter `name/description/type/trang-thai/uu-tien` + mục Kích hoạt / Chuẩn bị / Các bước / Definition of done / Bẫy / Tham chiếu). Agent chọn skill theo `description` rồi làm theo từng bước — không cần tự đọc lại `/docs`.

**Giai đoạn 1 (đã tạo 09/10/2026 — 10 skill):**

| Skill | Việc làm được | Khi nào dùng |
|---|---|---|
| `run-project` (chung) | Dựng & chạy dự án lạ từ checkout sạch | "chạy app được không" |
| `run-checks` (chung) | Thang kiểm tra lint → typecheck → build → test, quy tắc "test đỏ không commit" | Trước commit/PR |
| `debug-error` (chung) | Quy trình debug 6 bước có kỷ luật | Bất kỳ lỗi chưa rõ nguyên nhân |
| `security-hygiene` (chung) | Chống rò rỉ secret + dependency audit | Trước commit/push, thêm package |
| `cd-setup-dev` | Dựng môi trường Culinary Blog (env, 6 container, migrate/seed, 5080 + 3000) | Machine mới / reset môi trường |
| `cd-verify-pr` | 4 bước CI backend + `tsc`/`next build` + `Skipped=0` (210 test) | Trước mọi PR |
| `cd-migrate-seed` | `--migrate`/`--seed`, `dotnet ef migrations add`, lệnh cấm `ef database update` | Đổi schema DB |
| `cd-new-api-endpoint` | Thêm endpoint CQRS: record+validator+handler → `MapGroup` → `{data}` → test → contract | Yêu cầu endpoint mới |
| `cd-fix-image-bug` | Bảng triệu chứng → nguyên nhân ảnh (500/400/403/422/resize/presigned) | Lỗi upload/hiển thị ảnh |
| `cd-write-evidence` | Ghi evidence tuần đúng mẫu 5 file + 7 thành phần bản ghi | Cuối tuần / bàn giao |

Còn đề xuất: `DE_XUAT_SKILL_TEMP.md` (file tạm, sẽ xóa). Giai đoạn 2–3 (G3/G5–G7, P5/P7/P10–P12, P8/P13/P14) xem file đó.

---

## 5. Bảng định tuyến — "Tôi cần X → đọc file Y"

| nhu cầu | file |
|---|---|
| Muốn làm việc theo quy trình/lệnh đã chốt sẵn (skill) | `agent-skill/` — chọn theo bảng mục 4.6 |
| Chạy app lần đầu / tra cổng, env, tài khoản seed | `HUONG_DAN_CAI_DAT_VA_CHAY_CHUONG_TRINH.md` |
| Chạy test, cấu trúc test, 4 bước CI | `HUONG_DAN_TEST_APP.md` |
| Quy ước dự án, FR/NFR/CONS, quyết định D01–D29, ma trận K | `KE_HOACH_DU_AN.md` |
| Ai làm gì, tuần nào, tiến độ/PR/block hiện tại | `PHAN_CHIA_CONG_VIEC_6_TUAN.md` |
| Yêu cầu phần mềm (spec) — bản chuẩn | `root/SRS_Culinary_Blog_v1.1.1 Detail.md` (cần chi tiết) / `root/SRS_Culinary_Blog_v1.1.1.md` (cần nhanh) |
| Schema, FK, hành vi xóa, chỉ mục | `ERD.md` |
| Gọi/sửa endpoint (auth/category/list/search/image/google) | `*_CONTRACT.md` tương ứng |
| Debug lỗi khi chạy module soạn thảo | `RUNBOOK_SOAN_THAO_CONG_THUC.md` |
| Vận hành staging: backup, failover, scale, trace, load test | `RUNBOOK.md` |
| "Đã từng quyết định gì về vấn đề này?" | `adr/` (3 series, xem mục 4.2) |
| Lịch sử lỗi đã gặp & cách sửa | `report/` |
| Đề xuất cải thiện / việc đang chờ quyết định | `proposal/` (mục 4.3 — 3 cái 🔴 chặn tiến độ) |
| Bằng chứng nghiệm thu, trạng thái ô K01–K24 | `evidence/TV*/...` (mục 4.5) |
| Mâu thuẫn SRS cũ đã xử lý thế nào | `root/SRS_Contradictions_Report.md` + `report/BAO_CAO_GIAI_QUYET_MAU_THUAN_SRS.md` |

---

## 6. Bẫy dữ liệu cần biết (số liệu/duyên ref đã lỗi thời)

1. **Số test**: `HUONG_DAN_TEST_APP.md` ghi "54/54", `HUONG_DAN_CAI_DAT` ghi "172+", README gốc ghi "177/177" — số **mới nhất là 210/210** (205 `CulinaryBlog.Tests` + 5 `ConcurrencySpike`, 03/10/2026 theo `PHAN_CHIA_CONG_VIEC_6_TUAN.md`). Số `172/172`, `214/214` đã bị đính chính là lỗi thời.
2. **MinIO vs RustFS**: SRS & evidence ghi MinIO (không sửa hồi tố); hạ tầng dev/CI hiện là **RustFS**, biến môi trường vẫn `Minio__*`. Ghi ADR-TV4-002.
3. **`BAO_CAO_LOI_500_TRANG_SEARCH.md`** ghi "CHƯA SỬA" nhưng code đã sửa — đừng tin trạng thái trong file.
4. **ADR trùng số** giữa 3 series (mục 4.2).
5. **Tham chiếu treo**: `MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` trỏ `TV4/TUAN_4.md`, `TV4/BAO_CAO_LAB_04.md` — không tồn tại.
6. **PR đang chờ review** (tại 03/10): #20, #21, #22; block mở: **B5** (ảnh Draft wizard, cần TV3), **B6** (tài khoản Admin, cần TV1).
7. Chưa ô K nào được reviewer Nguyễn Thanh Tâm xác nhận ngày review (theo bảng K của TV3/TV4).

---

## 7. Quy ước cập nhật chỉ mục này

- Thêm/sửa file trong `/docs` → cập nhật bảng ở mục 3/4 tương ứng (nếu là tuần mới của evidence thì chỉ cần ghi chú ở mục 4.5).
- Giữ format: **`đường dẫn`** → 1–3 dòng tóm tắt + "mở khi nào". Không sao chép nội dung chi tiết vào đây.
- File mới phải trả lời được 2 câu: *nó chứa gì* và *agent nên mở nó khi nào*.
