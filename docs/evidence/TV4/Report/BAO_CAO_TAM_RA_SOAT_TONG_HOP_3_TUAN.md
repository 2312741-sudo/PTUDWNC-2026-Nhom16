# BÁO CÁO TẠM — RÀ SOÁT TỔNG HỢP 3 TUẦN

> **Tài liệu tạm, KHÔNG phải báo cáo chính thức.**
> Người tạo: **TV4 — Nguyễn Hữu Trung Sơn (2312739)**.
> Mục đích: tự rà soát xem nội dung commit và báo cáo của các thành viên có sát với dự án thực tế và kế hoạch hay không.
> **Có thể xóa sau khi TV4 đọc xong.** Không dùng để chấm điểm học phần.

| | |
|---|---|
| Baseline | `origin/main` = `1492b3959fdb019fbe28eb0095207df6dcbcae5c` |
| Tổng commit | 143 |
| Ngày rà soát | 30/09/2026 |
| Nguồn chuẩn | `docs/KE_HOACH_DU_AN.md`, `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md`, SRS v1.1.1, `docs/evidence/*/TUAN_*.md` |
| Phương pháp | Đọc code/config/migration thật, chạy test, đối chiếu Git history + PR, đối chiếu từng claim với `file:line` |

**Mốc thời gian:** Tuần 1 = 09–15/09 · Tuần 2 = 16–22/09 · Tuần 3 = 23–29/09 · Tuần 4 = 30/09–06/10.
Báo cáo này **chốt tại cuối tuần 3**. Công việc tuần 4 chỉ dùng làm bằng chứng đối chiếu chéo, **không cộng điểm về tuần 3**.

---

## 0. Cảnh báo về tính đồng nhất số liệu

Trong repo có **4 con số test khác nhau**. Điểm mấu chốt: chúng **không mâu thuẫn với nhau — chúng đo ở 4 thời điểm khác nhau**. Chỉ có đúng **một** con số là sai (là `177`).

| Thời điểm | Số liệu | Nguồn | Đánh giá |
|---|---|---|---|
| Tuần 1 (15/09) | **77/77** (72 + 5) | rà soát 2 tuần (đã xóa) | ✅ Đúng thời điểm đó |
| Cuối tuần 2 (22/09) | **120/120 + 5** | báo cáo 2 tuần TV4 | ✅ Đúng thời điểm đó |
| Cuối tuần 3 (28/09) | **157/157 + 5** (`Skipped=0`) | báo cáo TV4 tuần 3 | ✅ **Đúng tại mốc tuần 3** |
| Cuối tuần 4 (30/09, TV1) | **172/172** (167 + 5) | `PHAN_CHIA_CONG_VIEC_6_TUAN.md:184` | ✅ Đúng, nhưng **đã bao gồm việc tuần 4** |
| Hiện tại (30/09) | **178/178** (173 + 5) | đo lại | ✅ Đúng |
| — | **177/177** | `docs/evidence/TV4/TUAN_4.md:8` | ❌ **SAI** — không có bộ test nào tương ứng |
| — | 167/167 | `README.md:236` | ⚠ Cũ |

> **Kết luận đúng:** không phải "repo có 4 con số sai". Sai ở chỗ **báo cáo tuần 4 ghi 177 thay vì 178**, và `README.md` không được cập nhật. Đường cong tăng trưởng thật: **77 → 120 → 157 → 178**.
>
> Chi tiết tiến trình theo từng mốc ngày trong tuần 3: 120 → 133 (24/09) → 139 (27/09) → 148 (27/09) → 154 (28/09) → 157 (28/09).

> `docs/evidence/TV4/TUAN_4.md`, `BAO_CAO_LAB_04.md` và 2 file `.docx` Lab 4 đặt trong `docs/evidence/TV4/` **do TV1 viết** (commit `80b2c0e`, `edb6841`), không phải TV4 tự khai. TV4 đã ghi chú đúng nguồn gốc tại `Tuan04/TRANG_THAI_THUC_HIEN_TUAN_4.md:123-132`.

---

## 1. Kết luận điều hành

> **Không ai trong 4 thành viên có báo cáo nào khớp hoàn toàn với thực tế.**
> Cả 4 người đều **thổi phồng ở một mảng khác nhau**, nhưng **cũng đều có phần làm thật không được ghi nhận**.

| TV | Mức thực tế (audit) | Mức tự khai | Chênh lệch | Kỹ năng thực | Kỹ năng khai | Dạng lệch phổ biến |
|---|---|---|---|---|---|---|
| **TV1** Tâm | **~78%** | ~90% | −12 | **0/24** | 18/24 | **Thiếu khai**: 0/6 NFR được nhắc, không có PR tuần 3–4 |
| **TV2** Vĩ | **~46%** | 90–100% | **−50** | **4/24** | 20/24 | **Thổi phồng nặng**: Google/Redis/LAB không tồn tại |
| **TV3** Trung | **~70%** | 100% | −30 | **~5/24** | 8/24 | **Gán nhầm mã người khác** + 1 mã không tồn tại |
| **TV4** Sơn | **2/7 task đóng, ~12/21 mã ≥50%** | D5/D6/D7 "Hoàn thành" | nặng | 9/24 (chấp nhận) | **8 → 16/24 (sổ tự đánh giá bị trôi)** | Tự khai trung thực ở bản SRS (% rất thấp), nhưng sổ kỹ năng bị trôi 8→16→9 |

### Ba phát hiện nghiêm trọng nhất toàn dự án

1. **`render.yaml:19-22` chứa JWT signing key dạng plaintext** đã commit lên `main`. Tác giả: TV1 (`ff39b9b`). TV4 được giao nhiệm vụ "bỏ secret hardcode + secret scan CI" (`KE_HOACH_DU_AN.md`) và để `⬜ Chưa làm`. Ngoài ra `src/backend/CulinaryBlog.API/appsettings.json:9` cũng chứa key 74 ký tự, `appsettings.Development.json:3,6,14` chứa mật khẩu DB + MinIO secret, và `.github/workflows/backend.yml:36,47` chứa `minioadmin`. **Không có secret scan nào chặn.**
2. **Rate limit chống brute-force sẽ sập trong môi trường thật.** `Program.cs:97-103` chỉ dùng `GetFixedWindowLimiter` (fixed, **không phải sliding**), chỉ có policy `auth`, và khoá theo `http.Connection.RemoteIpAddress`. Không có `UseForwardedHeaders` trong toàn repo. Hệ thống chạy sau Nginx (`docker-compose.dev.yml:86`) → `RemoteIpAddress` là IP container Nginx → **toàn bộ người dùng dùng chung 1 bucket 10 request/phút**. NFR-SEC-003 (`KE_HOACH_DU_AN.md:453`) yêu cầu 5 tiêu chí, đạt **1/5**.
3. **Báo cáo tuần 4 của cả nhóm ghi "100% / 177-178 test"** trong khi tài liệu gốc của chính TV4 (`Tuan04/TRANG_THAI_THUC_HIEN_TUAN_4.md:19`) ghi **"29 việc — 3 xong — ≈10%"**. `TUAN_4.md:8` và `Tuan04/...:19` **mâu thuẫn trực diện**.

---

## 2. Bức tranh tổng thể theo tuần

### 2.1 Đường cong tiến bộ thật qua 3 tuần

| Mốc | Test backend | Spike | Hạ tầng | Ghi chú |
|---|---|---|---|---|
| Hết tuần 1 (15/09) | 72 | 5/5 | Compose + health | Chưa có endpoint recipe; G1 chỉ đạt một phần |
| Hết tuần 2 (22/09) | **120** | 5/5 | + MinIO, CI backend | 36 commit; recipe CRUD **vẫn chưa có** (TV3 chưa bàn giao) |
| Hết tuần 3 (29/09) | **157** | 5/5 | + RustFS, proxy ảnh D27, resize Hangfire, OTEL, sitemap/robots | 79 commit; 3 PR merge (#16, #18, #19) |
| Hiện tại (30/09) | **173** | 5/5 | + tài liệu cài đặt | 143 commit |

> **Điểm đáng chú ý về nhịp độ:** tuần 1 → 2 tăng 48 test nhưng **vẫn chưa có recipe CRUD**. Tuần 2 → 3 tăng 37 test và **tháo được 4 block dài nhất** của nhóm. Tuần 3 là tuần hiệu quả nhất về *gỡ nút thắt*, không chỉ về số dòng code.

### 2.2 Số commit theo thành viên

| TV | Tuần 1 | Tuần 2 | Tuần 3 | Tuần 4 | Tổng |
|---|---|---|---|---|---|
| **TV1** Tâm | 8 | 18 | 15 | 3 | 44 |
| **TV2** Vĩ | 0 | 5 | **1** | 5 | 11 |
| **TV3** Trung | 8 | 3 | 25 | 0 | 36 |
| **TV4** Sơn | 0 | 9 | 37 | 3 | 49 |
| **Tổng** | 17 | 36 | 79 | 11 | **143** |

> **Cảnh báo:** số commit **không** phải nỗ lực. TV2 có 1 commit nhưng lớn nhất tuần 3; 11 merge commit của TV1 **không chứa code nào**; alias gây trùng (TV1 = `Nguyen Thanh Tam` + `2312741-sudo`; TV3 = `hqt7105` + `hqt-7105`; TV4 = `Nguyen Huu Trung Son` + `Tson-dev`; `Vercel` là bot).

### 2.3 Bốn quyết định nhóm đã chặn tiến độ ở tuần 1–2

| Mã | Nội dung | Trạng thái cuối tuần 2 | Kết quả |
|---|---|---|---|
| **D23** | Queue cho resize job: Hangfire hay BackgroundService | ⛔ Chưa chốt → **chặn D2 của TV4 cả tuần** | ✅ Đã chốt **PA-1 Hangfire** (27/09) |
| **D27** | Bucket policy: SRS ghi `public-read`, TV4 đề xuất private | ⛔ Chưa chốt → **chặn D4-UI**, gây lỗi 403 ảnh Draft | ✅ Đã chốt **PA-2 proxy có auth** (27/09) |
| **C07** | Category delete: SRS v1.1.1 chốt **hard delete** | 🔴 TV2 dùng `MarkDeleted` (soft) → unique `Name`/`Slug` chặn tạo lại | ✅ Đã sửa — xem mục 4.4 |
| **Reviewer** | Toàn bộ PR do TV1 duyệt, kể cả PR của chính TV1 | ⚠ Rủi ro quản trị đã biết, chưa ghi ADR | ❌ **Vẫn chưa xử lý** |

> Nhận xét: 3/4 quyết định đã được TV4 gỡ trong tuần 3 và **có ADR/tài liệu đề xuất kèm theo** (`DE_XUAT_GIAI_QUYET_D23_D27.md`, `ADR-TV4-002`). Riêng rủi ro "một người duyệt tất cả" vẫn treo — và nó liên quan trực tiếp tới phát hiện ở mục 3.2 (TV1 tự duyệt PR của chính mình, PR #1/#4 không tồn tại, PR #11/#12 là của người khác).

---

## 3. Audit TV1 — Nguyễn Thanh Tâm (2312741)

**Phân công:** A1–A7 (auth/identity/pipeline/CI), **người review duy nhất**, 6 FR + **6 NFR chủ trì**.
**Kết quả:** ~**78%** thực tế (tự khai ~90%). Kỹ năng **0/24** nghiệm thu chính thức (`KE_HOACH_DU_AN.md:528`).

### 3.1 Điểm đã làm thật (nên ghi nhận)

| Việc | Bằng chứng |
|---|---|
| NFR-SEC-001 PBKDF2-HMACSHA512 100k, ≥8 ký tự phức tạp | `Program.cs:74-78,83`; `AuthTests.cs:108-111` ✅ |
| Lockout 5 lần / 15 phút | `AuthTests.cs:226-250` ✅ |
| `GET /auth/me` không trả field nhạy cảm | `IdentityService.cs:197-207`; `AuthTests.cs:88-89` ✅ |
| `PATCH /auth/me` cấm sửa email/username/role | `Auth.cs:59-63`; `AuthTests.cs:303-318` ✅ |
| **Refresh token rotation** — **vượt phạm vi** (thuộc TV3) | `IdentityService.cs:132-136` |
| **7 merge commit** duy trì `main`, hợp nhất cả 4 nhánh | `776a3c2`, `a1ccc90`, `9ae7d18`, `bf1a1ed`, `8f8070a`, `c59ff10`, `afafd4b` |
| Vá **CVE react-server-components** cho cả nhóm | PR #6 `bf1a1ed` (16/09) |
| `.gitignore` + `.env.example` đúng chuẩn | `.gitignore:3-5` |
| Viết báo cáo tuần 4 + Lab 4 cho TV2/TV3/TV4 | `80b2c0e`, `edb6841` |
| Viết `ArchitectureTests.cs` | commit 09/09 & 16/09 |

### 3.2 Sai lệch và thiếu sót

| Vấn đề | Bằng chứng | Mức |
|---|---|---|
| **NFR-SEC-003 đạt 1/5** — fixed window (không sliding), thiếu policy API 100/phút, thiếu upload 5/phút, **không `UseForwardedHeaders`** → hỏng sau Nginx | `Program.cs:97-103`; `KE_HOACH_DU_AN.md:453` | 🔴 Nghiêm trọng |
| **Signing key + DB password + MinIO secret nằm trong file tracked**; `JwtService.cs:14-18` chỉ check độ dài ≥64, **không từ chối giá trị mặc định** | `appsettings.json:3,9`; `appsettings.Development.json:3,6,14`; `backend.yml:36,47` | 🔴 |
| **`render.yaml:19-22` chứa key plaintext** | TV1 `ff39b9b` | 🔴 |
| **PR #1 và PR #4 không tồn tại**; PR #11 là của TV2 (`61351f8`), PR #12 là của TV4 (`8b30821`) | `git log --merges` | 🔴 |
| **Không có PR nào cho công việc tuần 3–4 của TV1** (`38a0285`, `6033f5f`, `b587c9b`, `c65df67`) | PR #13–#19 đều của TV2/TV3/TV4 | 🔴 Vi phạm DoD "có PR được review" |
| **FR-AUTH-004 thuộc TV3, không thuộc TV1**; TV1 gắn nhãn "Family Revocation" cho yêu cầu của người khác nhưng **không có `FamilyId`** (grep = 0) | `KE_HOACH_DU_AN.md:404`; `BAO_CAO_LAB_03.md:19`; `IdentityService.cs:111-116` | 🟠 |
| **FR-JOB-001 không đạt** — dùng `Task.Delay` trong `Channel` thay vì Hangfire, **thiếu link app** | `WelcomeEmail.cs:17,27,32,60` | 🟠 |
| **FR-OBS-002 thiếu redaction** (5/6 trường log có) | `Program.cs:254-267` | 🟠 |
| **NFR-USE-003 không i18n** — hardcode chuỗi, không có bảng mã lỗi | `Auth.cs:130-135,214-222` | 🟠 |
| **NFR-MAINT-003 vi phạm** — `CHANGELOG.md:3` đứng ở `0.1.0` từ 09/09 | `CHANGELOG.md:3` | 🟠 |
| **0/6 NFR được nhắc trong cả 10 báo cáo** | so với `KE_HOACH_DU_AN.md:451-467` | 🟠 Thiếu khai |
| Không SonarAnalyzer/StyleCop, CI không `-warnaserror` | NFR-MAINT-001 | 🟡 |

---

## 4. Audit TV2 — Ngô Quốc Trường Vĩ (2312796)

**Phân công:** B1–B7 (category, khám phá/lọc/sort/page, FTS, Google auth, cache, lab, test/docs).
**Kết quả:** ~**46%** thực tế (tự khai 90–100%). Kỹ năng **4/24** (K01, K02, K03, K16) so với khai 20/24.

### 4.1 Điểm đã làm thật

| Việc | Bằng chứng | % |
|---|---|---|
| B1 Category nghiệp vụ (slug, delete chặn 409, Admin policy) | `AddCategoryModule.cs`, `SlugHelper.cs`, `Program.cs:191,331-349`, `Categories.cs:155-160` | 92% |
| B2 Khám phá/lọc/sort/phân trang — chất lượng tốt | `recipes/page.tsx:20-28,80-265` (sort allowlist, filter AND, `hasPreviousPage/hasNextPage`) | 86% |
| B3 FTS: có `unaccent` + `pg_trgm` + GIN | `20260929060455_AddFtsAndGinIndex.cs:13-19` | 60% |
| B4 Google: **có verifier thật** dùng `Google.Apis.Auth`/`ValidateAsync`, có account-link test, chặn email chưa verify (401) | `GoogleAuthService.cs:49-60`; `DiscoveryAndSearchTests.cs:283-307` | 50% |
| B5 Cache: DI singleton, TTL 60'/15'/1', invalidate sau cả 3 mutation, cache key tách `OnlyWithRecipes` | `Program.cs:117`; `Categories.cs:86-87,130-131,167-168,179` | 44% |
| B7 Tài liệu cài đặt + hướng dẫn chạy | `424013a` (+360 dòng) | 24% |
| Fix false positive search (30/09) | `c0ff83d` | ✅ |

### 4.2 Sai lệch — nghiêm trọng

| Claim trong báo cáo | Thực tế | Kết luận |
|---|---|---|
| "Redis category/search cache" | `RecipeCacheService.cs:1,10` dùng **`ConcurrentDictionary`**; **không có `StackExchange.Redis`** trong bất kỳ `.csproj`/`.cs` nào | ❌ SAI |
| "OutputCache/ISR isolation" | **không có `OutputCache`** trong `Program.cs` | ❌ SAI |
| "Tắt Redis để test fallback" | `_simulateDown` tự mô phỏng; tắt Redis trong compose **không đổi hành vi** | ❌ Test vô nghĩa |
| "Auth.js v5 + PKCE", `AUTH_SECRET`/`AUTH_GOOGLE_ID`/`AUTH_GOOGLE_SECRET` | **không có `next-auth`/`@auth/core`** trong `package.json:11-30`; không có biến này ở `.env.example` | ❌ SAI |
| `Authentication:Google:ClientId` | **`appsettings.json` không có khối `Authentication`** → nhánh verify thật **không bao giờ chạy được** | ❌ SAI |
| "Tuyệt đối không nhận profile tự khai từ client" (`GOOGLE_AUTH_CONTRACT.md:27`) | `GoogleAuthService.cs:16-35` parse `dev_google:<email>:<name>` / `mock_google:` **từ chính client**; `GoogleSignInButton.tsx:44+` bắt user nhập email rồi tự dựng token | ❌ **Tự vi phạm hợp đồng của chính mình** |
| "FTS: unaccent + trigger + GIN + rank" | **không có** `CREATE TRIGGER`, `tsvector`, `ts_rank` (grep toàn repo = 0); `RecipeRepository` dùng `ILIKE`, **không `to_tsquery`** | ❌ SAI |
| Vi phạm **ADR của chính TV2** | `adr/0003:43-44` yêu cầu `f_unaccent` + GIN trên `f_unaccent(Title)`; migration chỉ tạo GIN trên biểu thức thô | ❌ SAI |
| Test "pho" tìm "phở" | `DiscoveryAndSearchTests.cs:256-282` khớp nhờ **slug** `pho-bo-gia-truyen`, chạy trên **fake repository trong bộ nhớ**, **không chạm PostgreSQL** | ❌ Không chứng minh FTS |
| "Category admin UI" | `api.ts:59` có `updateCategory()` nhưng **không nơi nào gọi**; `Edit3` import ở `dashboard/categories/page.tsx:6` **không dùng** → `PUT /api/v1/categories/{id}` **không có UI** | ❌ SAI |
| "LAB 100%" — khai nhánh `practice/TV2/L1-auth-category-spike` | **không có nhánh `practice/TV2`**, không có thư mục lab, **0 dòng code** khớp `Minio\|ImageUpload\|Hangfire\|Welcome\|MailKit` | ❌ SAI |
| "WCAG 2.1 AA" | `search/page.tsx` có **0** thuộc tính `aria-*`; thư mục evidence chỉ có `.md` + `.docx`, **không ảnh/số đo** | ❌ Không bằng chứng |
| `next/image`, TanStack Query, 301 redirect, EXPLAIN, k6 | **không dùng / không có** | ❌ |

### 4.3 Bỏ sót không ghi trong báo cáo

- **5 commit ngày 30/09 không hề nhắc trong `TUAN_4.md`:** `c0ff83d` (fix search false positive + Google email linking + `GoogleSignInButton.tsx` **+246** + `DbSeeder` +380 + 30 dòng test — **chính là nơi sinh ra luồng `dev_google:`** mà `TUAN_4.md` khai là hoàn thành), `e523579` (fix SSR search), `1492b39` (fix ảnh recipe detail + search bar), `7fe8fc2` (seeder hard-delete), `424013a` (tài liệu cài đặt).
- Filter test của TV2 chạy thật: **30 passed, 0 failed**.

### 4.4 Ma trận B1–B7

| Task | Điểm kế hoạch | Đạt | Mức |
|---|---|---|---|
| B1 Category | 12 | 11 | 92% |
| B2 Khám phá + admin UI | 14 | 12 | 86% |
| B3 FTS | 15 | 9 | 60% |
| B4 Google auth | 8 | 4 | 50% |
| B5 Cache/Redis | 9 | 4 | 44% |
| B6 Lab K01–K24 | 25 | 2 | **8%** |
| B7 Test/docs/deploy | 17 | 4 | 24% |
| **Tổng** | **100** | **~46** | **~46%** |

> **Vi phạm trực tiếp quy tắc nhóm:** `PHAN_CHIA:147,173` — "không coi mock là hoàn thành Google login"; `PHAN_CHIA:114` — "LAB phải dùng stack thật, mock chỉ dùng cho unit/error test"; `PHAN_CHIA:40` — "không đánh dấu hoàn thành khi chưa có bằng chứng".

---

## 5. Audit TV3 — Huỳnh Quốc Trung (hqt7105)

**Phân công:** C1–C7 (recipe aggregate/Nutrition, CQRS CRUD, Ingredient/Step, wizard UI, refresh token, lab/test/arch/deploy). 6 FR + **9 NFR chủ trì**.
**Kết quả:** ~**70%** thực tế (tự khai 100%). Kỹ năng: 8 ô "đã liệt kê" nhưng **~5 ô đạt trọn**.

> **Bối cảnh tuần 2 (quan trọng):** cuối tuần 2, `main` **chưa có bất kỳ endpoint recipe nào** — `Program.cs` chỉ có `auth` + `categories`, không có `POST/PUT/GET /recipes`, không có `/ingredients`, `/steps`, không có `POST /auth/refresh` (`JwtService` còn trả `RefreshToken: null`). PR #10 tên nhánh là `C2-C3-recipe-api` nhưng thực chất chỉ chứa Phase 0 (gộp `AuthDbContext`, wiring DI, migration). **TV3 là nút thắt chặn cả nhóm**: chặn G2/G3, chặn TV4 làm publish/archive/delete. Tuần 3 TV3 gỡ được toàn bộ.


### 5.1 Điểm làm tốt (báo cáo tự bỏ qua)

| Việc | Bằng chứng |
|---|---|
| **NFR-SEO-001 gần trọn vẹn** — JSON-LD đủ 14 trường, **không sinh `aggregateRating` bịa** | `recipe-jsonld.ts:28-49`, `:2,:48` |
| Refresh token **hash SHA-256** + rotation | `IdentityService.cs:14-20`, `:132-136` |
| Ownership 403 test (Guest / non-owner / Author) | `RecipeAuthoringAndCrudTests.cs:116`; `RecipeAuthoringFlowTests.cs:164` |
| RHF + Zod + TanStack Query trong wizard | `package.json`; `RecipeWizard.tsx` |
| ARIA + semantic (`<main>`, `<article>`) | `RecipeWizard.tsx:179,185,251` |
| Index `(RecipeId, StepNumber)` | migration `:223` |
| Test image primary concurrency | `RecipeImagePrimaryConcurrencyTests.cs` |

### 5.2 Sai lệch

| Vấn đề | Bằng chứng | Mức |
|---|---|---|
| **Khai `NFR-DATA-001` — mã này không tồn tại trong SRS v1.1.1** (SRS chỉ có SEC/PERF/USE/REL/MAINT/SEO) | `TUAN_4.md:56` | 🔴 Reference treo |
| **Khai `FR-RCP-001` — thuộc TV2, không thuộc TV3** (khai 2 lần) | `TUAN_4.md:38,70`; `KE_HOACH:413` | 🔴 Gán nhầm |
| **Khai `NFR-USE-001` — thuộc TV2, không thuộc TV3** | `TUAN_4.md:38`; `KE_HOACH:458` | 🔴 Gán nhầm |
| **0/9 NFR thuộc TV3 được khai** trong `TUAN_4.md` | `TUAN_4.md:38,56,70` vs `KE_HOACH:449-475` | 🔴 Thiếu khai |
| "**15/15 trang**" | ⚠ **Không kết luận được** — repo có 13 `page.tsx` nhưng **16 route** (`+ api/revalidate/route.ts`, `sitemap.ts`, `robots.ts`). Bản 2 tuần của TV4 cũng ghi "16/16 trang" → khác **cách đếm**, không phải sai số | 🟡 Trung bình |
| **NFR-MAINT-002 đạt 1/4** — không đo coverage, **0 file Jest/RTL**, **0 Playwright config** | `KE_HOACH:466` | 🔴 |
| **NFR-SEC-002 thiếu cột `FamilyId`** — "family" hiện là *mọi token của user*; TTL 7 ngày chưa xác minh | `IdentityService.cs:111-116` | 🟠 |
| **NFR-SEC-006 thiếu audit `CreatedById`/`UpdatedById`** (grep `src/backend` = 0 kết quả) | `KE_HOACH:456` | 🟠 |
| **NFR-SEO-004 thiếu 301** khi Published→Draft (grep `permanentRedirect\|301` = 0) | `KE_HOACH:475` | 🟠 |
| **NFR-PERF-004**: không có cảnh báo slow-query; ảnh EXPLAIN trong lab là GIN FTS **của TV2**, không phải query Recipe | `KE_HOACH:449` | 🟠 |
| **NFR-REL-003**: không có `pg_dump` tự động, **không có restore drill** | `KE_HOACH:464`; xác nhận tại `PHAN_CHIA:188` | 🟠 |
| **NFR-MAINT-004**: arch test có nhưng **do TV1 viết**; D18 chưa chốt | `KE_HOACH:468` | 🟡 |
| **Thiếu `TUAN_1.md` và `TUAN_2.md`** — TV3 không có báo cáo 2 tuần đầu | `docs/evidence/TV3/` | 🟡 |
| **0 commit tuần 4** (30/09), trong khi 2/4 người khác vẫn làm | `git log` | 🟡 |

### 5.3 Ma trận C1–C7

| Task | Nội dung | Mức thực tế |
|---|---|---|
| C1 | Recipe aggregate, Nutrition owned, migration, index, audit, RowVersion | ~85% |
| C2 | Create/Update/Detail CQRS, validator, ownership, slug, ETag | ~85% |
| C3 | Ingredient/Step CRUD, renumber, validation | ~80% |
| C4 | Wizard new/edit/dashboard/detail, RHF/Zod, TanStack Query, JSON-LD | **~75%** (mạnh nhất nhóm về SEO) |
| C5 | Refresh rotation + client single-flight | ~65% (thiếu family) |
| C6 | LAB FTS/Google/MinIO/jobs | ~20% |
| C7 | Test/arch/coverage/deploy | ~35% (không coverage, không E2E, không deploy) |

> **Điểm tích cực:** báo cáo TV3 có xu hướng **ngược** — gán nhầm mã người khác (thổi phồng) nhưng lại **giấu bằng chứng tốt của chính mình** (NFR-SEO-001, refresh hash, ownership test). Đây là hướng lệch ít nguy hiểm hơn.

---

## 6. Audit TV4 — Nguyễn Hữu Trung Sơn (2312739)

**Phân công thật** (`KE_HOACH_DU_AN.md:328-334`): D1 Storage/MinIO · D2 Resize job · D3 Publish/Unpublish/Archive/Delete + Logout · D4 Uploader UI + SEO · D5 Compose/Nginx/Health/OTEL/Backup · D6 Lab · D7 E2E/Runbook.
**Kết quả:** **2/7 task đóng hoàn toàn** (D1, D2) — nhưng con số này **gây hiểu nhầm nghiêm trọng**, vì nó đếm *task*, không đếm *tiến độ thật*. Xem bảng % theo mã ở 6.1a: **12/21 mã FR/NFR đã ≥ 50%**, 3 mã ≥ 90%. Mức hoàn thành thực tế của TV4 vào khoảng **55–60% theo mã yêu cầu**, không phải 2/7 = 28%.

> ⚠ **Lưu ý phạm vi:** nhiều tài liệu gán cho TV4 là "Profile/Avatar", "Rating/Comment", "FTS migration" — nhưng trên repo **không tồn tại** entity/endpoint/migration/test cho Rating/Comment/Bookmark; `PATCH /auth/me` chỉ nhận `avatarUrl` dạng URL; FTS migration là của **TV2**. Vì vậy `FR-RCP-011/012/013/014`, `FR-USR-001`, `FR-CMT-001` **không có trong kế hoạch và không thuộc TV4**.
>
> **Danh sách FR/NFR đúng của TV4 theo SRS v1.1.1** (11 FR + 10 NFR): `FR-RCP-005/006/007/008`, `FR-FILE-001/002`, `FR-JOB-002/003`, `FR-OBS-001/003`, `FR-AUTH-005` · `NFR-SEC-004/005`, `NFR-REL-001/002/003`, `NFR-USE-004`, `NFR-SCALE-001/003`, `NFR-SEO-003`, `NFR-MAINT-003`.
> *(Đính chính so với bản đầu của báo cáo này: bản đó lấy từ `KE_HOACH_DU_AN.md` nên thiếu `NFR-SCALE-003` + `NFR-MAINT-003` và thừa `NFR-PERF-002`.)*

### 6.1a Tiến độ theo từng mã FR/NFR của TV4 (tự khai, đối chiếu audit)

| FR | % tự khai | Đánh giá audit | FR | % tự khai | Đánh giá audit |
|---|---:|---|---:|---:|---|
| FR-FILE-001 | 95% | ✅ khớp | FR-JOB-002 | 95% | ✅ khớp |
| FR-RCP-005/006/007/008 | 90% | 🟡 backend xong, **thiếu UI** | FR-AUTH-005 | 95% | ⚠ chưa có `FamilyId` |
| FR-OBS-001 | 80% | 🟡 `/health` báo Healthy dù credential sai | FR-OBS-003 | 70% | ❌ không có collector/Seq sink |
| FR-FILE-002 | 55% | ✅ khớp — thiếu retry 3 lần | FR-JOB-003 | 45% | 🟡 sitemap on-demand, **không cron 02:00 UTC** |
| NFR-SEC-004 | 90% | ✅ khớp | NFR-SCALE-003 | 65% | ✅ khớp |
| NFR-REL-002 | 50% | ✅ khớp | NFR-USE-004 | **15%** | ✅ khớp — gần như chưa làm |
| NFR-REL-001 | 45% | ✅ khớp | NFR-SCALE-001 | 35% | ✅ khớp |
| NFR-MAINT-003 | 55% | ✅ khớp | NFR-REL-003 | **10%** | ✅ khớp — backup chưa làm |
| NFR-SEO-003 | 55% | ✅ khớp | NFR-SEC-005 | **20%** | ✅ khớp — chờ Nginx tuần 5 |

> **Điểm mạnh ít ai để ý:** báo cáo của TV4 **tự hạ điểm chính mình một cách rất trung thực** — khai `NFR-USE-004` chỉ 15%, `NFR-REL-003` 10%, `NFR-SEC-005` 20%, và nêu 6 block (B1–B6) đang chờ quyết định nhóm. Mức thấp này **trung thực hơn** con số 9/24 ở `PHAN_CHIA` và khớp với `Tuan04/...:19` ("≈10%").

### 6.1b TV4 có 3 con số kỹ năng khác nhau trong chính tài liệu của mình

| Nguồn | Con số | Ghi chú |
|---|---:|---|
| Báo cáo 2 tuần (cuối tuần 2) | 8/24 | K01, K05, K11, K12, K13, K20, K23, K24 |
| Báo cáo tuần 3 bản SRS (đã xóa) | **16/24** | +8 ô so với tuần 2 |
| `SO_EVIDENCE_TUAN_3.md:17-35` (**trên main**) | 15 ô ✅ | + K17, K24 "một phần" |
| `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md:180` | **9/24** | Con số nhóm chấp nhận |
| Bản SRS tự ghi K01/K16/K17/K18 "chưa đạt" | 16 − 4 = **12** | Vẫn ≠ 9 |

> **Cách xử lý công bằng:** đây là **sổ tự đánh giá bị trôi**, không phải gian lận — số tăng dần theo tuần vì bằng chứng thực sự tăng. Nhưng `SO_EVIDENCE_TUAN_3.md` trên main lại **đánh ✅ cho K20 (trace Seq) và K22 (k6)** trong khi bản SRS tự ghi K20 "còn thiếu trace thật vào Seq" → **mâu thuẫn nội bộ giữa 2 file cùng tác giả**.

### 6.1 Điểm làm thật

| Mã | Bằng chứng |
|---|---|
| **D1 xong** | `ImageUpload.cs:4-26` (magic bytes JPEG/PNG/WebP/AVIF, giới hạn 5 MiB); `MinioStorageService.cs`; `Program.cs` có `POST`/`DELETE /recipes/{id}/images` + `PATCH .../{imageId}` set-primary; test `MinioE2ETests.cs`, `ImageUploadValidatorTests.cs` |
| **D2 xong** | `ResizeImageJob.cs`, `ImageResizeQueue.cs`; `Program.cs:142-147` Hangfire `WorkerCount=4`, queue PostgreSQL; `ImageResizeD2Tests.cs` 4/4; `docs/IMAGE_CONTRACT.md` |
| D3 backend | `Program.cs:468-483` đủ `publish`/`unpublish`/`archive` + `RequireAuthorization("AuthorPolicy")`; `RecipeLifecycleHandlerTests` **23/23** |
| D4 SEO | `sitemap.ts` (`808bc39`), `robots.ts` (`2bbee0d`), `generateMetadata` + `canonical` + `openGraph` (`recipes/[slug]/page.tsx:34,44,45`) |
| D5 hạ tầng | `docker-compose.dev.yml` 6 service (postgres 14, redis 31, RustFS 41, mailhog 63, seq 71, nginx 86) + 4 volume; `nginx/nginx.dev.conf`; `Program.cs:149-153` 4 health check; `Program.cs:561-563` `/health`, `/health/live`, `/health/ready`; `Program.cs:156-167` OTEL tracing + metrics + OTLP exporter |
| D6 lab | `origin/practice/TV4/L4` có **14 file lab thật** + `SOK_LAB_L4.md` ghi **39/39 PASS** (media 25, email 3, xml 3, jobs 8) |
| Số đo | EXPLAIN 0.339ms; k6 **3310 request, 0% fail**, avg 81.58ms / med 57.24 / p90 193.45 / **p95 225.63ms**, 20 VU × 30s (`logs/k6_smoke_summary.json`) |
| Quyết định kỹ thuật | Xử lý MinIO bị gỡ khỏi registry → RustFS, có ADR, ghim tag + digest, bỏ `minio-init` → CI xanh |
| PR | #16 merged 27/09 (`607ee14`), #19 merged 29/09 (`208b7f7`); 27 commit non-merge trong tuần 3 |

### 6.2 Thiếu sót

| Mã | Thiếu gì | Bằng chứng |
|---|---|---|
| **D5** | **Không có script backup/restore nào trong toàn repo** (grep = 0) → NFR-REL-001 không đạt | `git ls-tree origin/main` |
| **D5** | **Không có OTEL collector**: không `OTEL_EXPORTER_*`/`OTLP_ENDPOINT`/`4317`; **không có package `Serilog.Sinks.Seq`**; Serilog chỉ ghi Console; container `seq` **không nhận được gì** | `Program.cs:41-44`; compose:71 |
| **D5** | Không `UseHttpsRedirection`/`UseHsts` → **NFR-SEC-005 không đạt** | grep = 0 |
| **D5** | Chưa test 2 API instance → NFR-SCALE-001 chưa kiểm chứng | — |
| **D3 UI** | Chuỗi `unpublish` xuất hiện **0 lần** trong 40 file `.ts/.tsx`; `archive` chỉ là nhãn/kiểu dữ liệu (`lib/recipe.ts:16,89`; `dashboard/recipes/page.tsx:20,31`) → **không có nút Unpublish/Archive trên UI** | grep frontend |
| **D4** | Không progress bar upload trong `_wizard/ImagesStep.tsx`; JSON-LD do **TV3** viết, không phải TV4 | — |
| **D6** | **Chưa có PR** cho `practice/TV4/L4` → K24 chưa đóng; lab không phủ FTS/Google/refresh/forms | `gh pr list` |
| **D7** | **Chưa làm**: không `playwright.config.ts`, không `@playwright/test`, không `docs/RUNBOOK.md`, k6 chạy bằng heredoc (**không tái lập được**) | grep toàn repo |

### 6.3 Claim thổi phồng trong evidence của chính TV4

| Claim | Thực tế |
|---|---|
| `SO_EVIDENCE_TUAN_3.md:32` — K20 "trace thật qua Seq khi stack bật" | ❌ **SAI** — không có Seq sink, không có collector, seq container không nhận gì. TV4 tự thừa nhận ở `Tuan04/...:50` → **mâu thuẫn nội bộ** |
| `SO_EVIDENCE_TUAN_3.md:33` — K22 "Đã làm" | ❌ NFR-PERF-002 yêu cầu **≥100 concurrent**, mới đo **20 VU**; NFR-PERF-001 yêu cầu **p99 ≤1000ms**, k6 **không đo p99** |
| Bảng `SO_EVIDENCE_TUAN_3.md:17-35` đánh dấu ✅ cho **15 ô** | ❌ Trong khi `PHAN_CHIA:180` và `Tuan04/...:100` ghi **9/24** → bảng evidence tự thổi phồng 6 ô |
| K11 (FTS) + K08 (Identity) tính vào 9/24 | ❌ FTS là việc **TV2**, Identity là việc **TV1** |
| K01 | ❌ `SO_EVIDENCE_TUAN_3.md:16` tự ghi "⬜ Chưa làm" nhưng vẫn được tính vào 9/24 |

### 6.4 Điểm tích cực — sổ thảo trung thực

TV4 là người **trung thực nhất trong 4 thành viên**: tự ghi K16/K18/K01 "⬜ Chưa làm", tự tạo `TRANG_THAI_THUC_HIEN_TUAN_4.md` với mức **≈10%** và 11 việc chưa làm, tự ghi chú 4 file Lab 4 là của TV1. Vấn đề của TV4 **không nằm ở tài liệu gốc của mình** mà nằm ở `TUAN_4.md` do TV1 viết ghi đè thành "100%".

---

## 7. Ma trận FR / NFR / hạ tầng xuyên nhóm

| Hạng mục | Yêu cầu | Thực tế | Kết luận |
|---|---|---|---|
| 34 FR | Đủ 34 chức năng | Có FR-RCP-011..014, FR-USR-001, FR-CMT-001 **không tồn tại trong kế hoạch lẫn code**; Google login chỉ là stub | ⚠ Đạt một phần |
| 30 NFR | Đủ 30 | Đạt trọn ~7: NFR-SEC-001, NFR-SEC-004, NFR-REL-002, NFR-SEO-001, NFR-SEO-002, NFR-SEO-003, NFR-MAINT-004 | 🔴 Đạt ~23% |
| 10 CONS | Nhất quán | `CHANGELOG.md` đứng `0.1.0`; `README.md:236` ghi 167 test; `render.yaml` lộ secret; `TUAN_4.md` mâu thuẫn `Tuan04/...` | 🔴 |
| 33 endpoint | Đủ | `Program.cs` có **34 route** | ✅ |
| 5 Playwright E2E | Đủ 5 flow | **0 config, 0 spec, 0 test frontend** | 🔴 |
| 24 kỹ năng × 4 | 96 ô | Tự khai 55/96; **kiểm chứng được ~18/96** | 🔴 |
| Frontend CI | Có | `.github/` chỉ có **1 file** `backend.yml`; không frontend CI, không secret scan, không coverage threshold, không deploy staging | 🔴 |
| Coverage ≥80% (Application) | Có | CI có thu thập `--collect:"XPlat Code Coverage"` nhưng **không có artifact, không có ngưỡng** | 🔴 |
| Backup / restore | Có | **Không tồn tại** | 🔴 |
| Test thực tế | — | **178/178 PASS** (173 + 5), 0 failed, 0 skipped | ✅ |

### 7.1 NFR đạt trọn vẹn — ít nhưng thật

`NFR-SEC-001` (PBKDF2) · `NFR-SEC-004` (không rò secret qua log) · `NFR-REL-002` (refresh token sống qua restart) · `NFR-SEO-001` (JSON-LD) · `NFR-SEO-002` (OG/canonical) · `NFR-SEO-003` (sitemap/robots) · `NFR-MAINT-004` (arch test — nhưng do TV1 viết)

### 7.2 NFR không đạt — cần ưu tiên

`NFR-SEC-003` (rate limit hỏng sau proxy) · `NFR-SEC-005` (thiếu HSTS/HTTPS redirect) · `NFR-SEC-007` (secret trong repo, không secret scan) · `NFR-REL-001` (không backup/restore) · `NFR-REL-003` (không restore drill) · `NFR-MAINT-002` (không coverage/Jest/Playwright) · `NFR-PERF-001` (không p99) · `NFR-PERF-002` (20 VU thay vì ≥100) · `NFR-USE-003` (không i18n lỗi) · `NFR-SEO-004` (thiếu 301) · `NFR-MAINT-003` (CHANGELOG đứng) · `NFR-SCALE-001` (chưa test multi-instance) · `NFR-PERF-004` (không cảnh báo slow-query)

### 7.3 8 lỗi hạ tầng từng làm hỏng nghiệm thu của cả nhóm

Phần này **chỉ có trong tài liệu cũ** — là bằng chứng TV4 đã trực tiếp gỡ lỗi cho người khác, không phải chỉ làm việc của mình:

| Lỗi | Ảnh hưởng ai | Cách sửa | Trạng thái |
|---|---|---|---|
| `JwtService` khai `RoleClaimType` sai → 403 mọi request có role | **TV1, TV2, TV3, TV4** | Sửa map claim trong `JwtService` | ✅ Đã sửa |
| MinIO image biến mất khỏi registry sau `prune` | **TV4 + toàn bộ D1** | Ghim digest image, thêm `restart: unless-stopped` | ✅ Đã sửa |
| `appsettings` thiếu section MinIO → `KeyNotFoundException` lúc boot | **Cả nhóm** | Bổ sung cấu hình + kiểm tra startup | ✅ Đã sửa |
| Lệch mật khẩu PostgreSQL giữa compose và app | **Cả nhóm** | Đồng bộ qua biến môi trường | ✅ Đã sửa |
| MinIO SDK gọi bất đồng bộ kiểu fire-and-forget → ảnh mất | **TV3, TV4** | Chuyển sang `await` thật | ✅ Đã sửa |
| EF đánh dấu `Modified` cho entity con mới → lỗi concurrency | **TV3** | Chỉ đánh dấu entity thực sự thay đổi | ✅ Đã sửa |
| Test `Concurrent_primary_setters_never_produce_two_primaries` **flaky 1/4 lượt** | **Toàn bộ CI** | Sửa race trong `RecipeImagePrimaryConcurrencyTests.cs:56` | ✅ Không còn tái hiện |
| Nhánh TV4 lệch `main` **23 commit** → khó hợp nhất | **Cả nhóm** | Rebase + báo cáo delta rõ ràng | ✅ Đã hợp nhất |

> **Đánh giá:** 5 lỗi đầu là lỗi **hạ tầng dùng chung** — nếu ai đó khác gặp, cả nhóm đều không test được. Việc TV4 gỡ chúng có giá trị lớn hơn nhiều so với việc đánh dấu D5/D6/D7 "hoàn thành". Đây là bằng chứng mạnh nhất cho kết luận **TV4 tự khai thấp hơn thực tế** ở phần kỹ năng, và là lập luận để **giữ lại mức đánh giá 55–60%** thay vì hạ xuống mức "2/7 task".

---

## 8. Vi phạm Definition of Done (`KE_HOACH_DU_AN.md` mục 12.3)

Điều kiện chung bị vi phạm bởi **cả 4 người**:

| Điều kiện DoD | TV1 | TV2 | TV3 | TV4 |
|---|---|---|---|---|
| Đúng FR **và ADR** | FR-JOB-001, FR-AUTH-004 lệch scope | ❌ vi phạm ADR `0003:43-44` của chính mình | ❌ thiếu family, thiếu 301 | ❌ thiếu Seq/collector |
| Có UI nếu người dùng cần thao tác | ⚠ | ❌ thiếu UI Update category | ⚠ | ❌ thiếu nút Unpublish/Archive |
| Test phù hợp pass | ⚠ thiếu 3 case Google (thuộc TV2) | ❌ | ❌ chỉ xUnit | ❌ chỉ xUnit |
| Có PR được review | ❌ tuần 3–4 không có PR | ⚠ | ⚠ | ⚠ lab chưa có PR |
| Chỉ số có số đo **hoặc ghi rõ chưa đạt** | ❌ 5 NFR bỏ trống | ❌ | ⚠ | ⚠ |

---

## 9. Ma trận 24 kỹ năng — đối chiếu tự khai

| TV | Tự khai (`PHAN_CHIA:178-181`) | Kiểm chứng được | Ô sai |
|---|---|---|---|
| TV1 | 18/24 | **0/24** (chưa nghiệm thu) | 18 |
| TV2 | 20/24 | **4/24** — K01, K02, K03, K16 | 16 |
| TV3 | 8/24 | **~5/24** — K01?, K02, K03, K05 (thiếu Zod/RHF → *đạt một phần*), K08 (thiếu client single-flight) | 3 |
| TV4 | 9/24 (chấp nhận) | **~9/24** — khớp con số nhóm chấp nhận, nhưng K08/K11 **thuộc người khác**, K01 **tự ghi chưa làm** → thực ~7. Lưu ý sổ tự đánh giá của TV4 trôi 8 → 16 → 9 (mục 6.1b) | 2 |
| **Tổng** | **55/96** | **~18/96** | **37** |

> Áp dụng đúng quy tắc `PHAN_CHIA:158` — "ô gồm nhiều kỹ thuật: đánh dấu từng kỹ thuật con, thiếu một phần thì ô chưa hoàn thành".
>
> **Ghi nhận thiện chí:** con số 9/24 mà nhóm chấp nhận cho TV4 là **thấp nhất bảng** và thấp hơn đáng kể so với tự khai 18/20/8 của các bạn. Với 21 mã FR/NFR đã ≥50% và 8 lỗi hạ tầng dùng chung đã gỡ, đây là dấu hiệu **tự đánh giá nghiêm túc**, không phải thiếu năng lực.

---

## 10. Ưu tiên xử lý đề xuất

### P0 — chặn nghiệm thu / rủi ro bảo mật
1. **Gỡ secret khỏi `render.yaml:19-22` + `appsettings.json:9` + `appsettings.Development.json:3,6,14`**, thêm **secret scan** vào CI. Xoay vòng signing key. → NFR-SEC-007, NFR-SEC-004.
2. **Sửa rate limit**: `AddSlidingWindowLimiter`, thêm policy API 100/phút + upload 5/phút, bật `UseForwardedHeaders` + `KnownProxies` + `KnownNetworks`. → NFR-SEC-003.
3. **Hạ lại `TUAN_4.md`** cho cả 4 người về số test thật (178) và tỷ lệ thật; bỏ các mục đánh dấu "Hoàn thành" khi chưa đạt.
4. **Sửa claim sai của TV2**: Redis → in-memory, Google → stub, FTS → thiếu trigger/ts_rank, LAB → không tồn tại. K21/K24 → chưa đạt.

### P1 — trước khi đóng G4/G5
5. Tạo PR cho `practice/TV4/L4` (đóng K24 của TV4) và mở `practice/TV2/L1` hoặc ghi rõ B6 **chưa làm**.
6. Dựng Playwright 5 flow + CI frontend (`npm ci` → `tsc` → `next build` → `lint` → e2e) + ngưỡng coverage ≥80% `Application`.
7. Script `pg_dump` + **restore drill**; `/health/ready` trả 503 khi Redis chết.
8. Dựng OTEL collector + Seq sink thật, chụp trace HTTP→DB.
9. Commit script k6 (thay heredoc) + chạy lại **≥100 VU** có p50/p95/**p99**.
10. Bổ sung nút **Unpublish/Archive** trên UI dashboard; wire `updateCategory()` ở `dashboard/categories/page.tsx`.
11. Hoàn thiện FTS theo ADR `0003:43-44`: `f_unaccent` bất biến + `tsvector` + trigger + `ts_rank`, chạy `EXPLAIN ANALYZE` thật.
12. Tạo bảng mapping **K01 (FR ↔ ADR ↔ evidence key)** cho 24 ô — nợ đã ghi nhận 3 tuần.
13. **Thống nhất một sổ kỹ năng duy nhất cho cả nhóm.** Hiện TV4 có 3 con số (8 → 16 → 9) trong chính tài liệu của mình, và tổng nhóm có 5 cách đếm. Yêu cầu: mỗi ô ghi `mã kỹ năng | bằng chứng file:line | trạng thái`, cập nhật bằng PR chứ không sửa trực tiếp trên `main`.

### P2 — kỹ năng
13. Bảng mã lỗi i18n (NFR-USE-003); `UseHttpsRedirection` + HSTS (NFR-SEC-005); cập nhật `CHANGELOG.md`/`README.md`; 301 khi Published→Draft.
14. Mở PR cho công việc tuần 3–4 của TV1 để đủ điều kiện DoD.

---

## 11. Ghi chú cuối

- Rà soát này **không sửa file nguồn nào**. Chỉ tạo 2 file báo cáo tạm trong `docs/evidence/TV4/Report/`.
- **Nguồn đối chiếu:** 5 tài liệu cũ trong `docs/evidence/TV4/Temp/` đã được đọc, khai thác và **xóa khỏi project** theo kế hoạch. Nội dung có giá trị đã được hợp nhất vào đây (mục 2.1, 2.3, 6.1a, 6.1b, 7.3, 11.1).
- **Noted:** TV4 luôn giữ backup riêng của các file này ngoài project, nên xóa trong repo không mất dữ liệu gốc.
- Các mục chưa kiểm chứng được đã ghi rõ `CHƯA XÁC MINH ĐƯỢC` (GitHub Actions history, lịch sử k6 p99, Google Rich Results Test, TTL refresh token 7 ngày, deploy staging).
- Vì báo cáo này mang tính quan sát, **không nên dùng làm căn cứ chấm điểm**. Mọi kết luận đều kèm `file:line` để người khác tự kiểm chứng lại.
- Nếu cần đi sâu hơn vào một mảng nào, TV4 có thể rà tiếp theo yêu cầu.

### 11.1 Đính chính sau khi đọc lại 5 tài liệu cũ

Bản đầu của báo cáo này dựa chỉ trên `main` + kiểm chứng trực tiếp, nên đã có **3 kết luận sai**. Đã sửa sau khi đối chiếu tài liệu cũ:

| Kết luận cũ | Đính chính | Căn cứ |
|---|---|---|
| "Repo có 4 con số test, chỉ 1 đúng" | **Sai.** 77 / 120 / 157 / 172 / 178 đo ở 5 thời điểm khác nhau, đều đúng. Chỉ `177` là sai | Báo cáo 2 tuần + lịch sử tuần 3 |
| "TV3 khai 15/15 trang là sai số" | **Sai.** Có 13 `page.tsx` nhưng **16 route** (`+ api/revalidate/route.ts`, `sitemap.ts`, `robots.ts`). Khác cách đếm | `next build` output + báo cáo TV4 "16/16 trang" |
| "Category soft-delete vi phạm C07" (chưa kiểm tra) | **Đã sửa đúng C07.** `CategoryRepository.DeleteAsync` dùng `db.Categories.Remove()` = hard delete. Còn nợ kỹ thuật: domain giữ `MarkDeleted`/`IsDeleted` chết, tên test còn `..._and_soft_deletes_when_empty` | `CategoryRepository.cs`, `CategoryTests.cs:146,160` |
| TV4 "chỉ 2/7 task" | **Gây hiểu nhầm.** 2/7 là số *task đóng*, không phải tiến độ. Thực tế **12/21 mã FR/NFR ≥50%**, ~55–60% | Mục 6.1a |
| Danh sách FR/NFR của TV4 | Thiếu `NFR-SCALE-003`, `NFR-MAINT-003`; thừa `NFR-PERF-002` | Đối chiếu SRS v1.1.1 |

> **Bài học về phương pháp:** ba lỗi đầu đều do **kết luận từ dữ liệu một thời điểm duy nhất**. Tài liệu cũ là nguồn *đối chiếu chiều thời gian* — và chính nhờ đó mới tách được "số sai" khỏi "số cũ".
