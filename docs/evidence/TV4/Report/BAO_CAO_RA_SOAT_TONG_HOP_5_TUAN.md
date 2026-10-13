# BÁO CÁO TẠM — RÀ SOÁT TỔNG HỢP 5 TUẦN (CẢ NHÓM 4 THÀNH VIÊN)

> **Tài liệu tạm, KHÔNG phải báo cáo chính thức.**
> Người tạo: **TV4 — Nguyễn Hữu Trung Sơn (2312739)** (đóng vai người rà soát độc lập cho cả nhóm).
> Mục đích: kiểm chứng **cả 4 thành viên** "đã thực sự làm gì" trong 5 tuần, dựa trên **chạy thật project + Git/PR + code**, chứ không chỉ đọc báo cáo tự khai.
> **Có thể xóa sau khi cả nhóm đọc xong.** Không dùng để chấm điểm học phần.

| | |
|---|---|
| Baseline chuẩn | `origin/main` = `5a33164` (merge PR #35, 11/10/2026) · **260 commit non-merge** |
| Nhánh rà soát | `2312739_NHTSon_D5-D7` — HEAD `764f21b` (**đã chứa** `origin/main`), ahead 37 chưa push |
| Ngày rà soát | 11/10/2026 (cuối tuần 5) |
| Nguồn chuẩn | `docs/KE_HOACH_DU_AN.md`, `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md`, SRS v1.1.1, `docs/evidence/*`, GitHub PR, commit thật |
| Phương pháp | **Chạy thật**: `dotnet test` · `tsc --noEmit` · `jest` · `playwright --list`; đối chiếu `git log`/`gh pr`; đọc code/config/migration và đối chiếu từng claim với `file:line`. Báo cáo tự khai của thành viên chỉ dùng **tham khảo**. |

**Mốc thời gian:** Tuần 1 = 09–15/09 · Tuần 2 = 16–22/09 · Tuần 3 = 23–29/09 · Tuần 4 = 30/09–06/10 · Tuần 5 = 07–11/10.
Báo cáo **chốt tại cuối tuần 5 (11/10)**. Công việc sáng 11/10 (PR #31/#34/#35 vừa merge) được ghi như mốc đóng tuần, không cộng vào tuần 6.

Bản 3 tuần trước (`BAO_CAO_TAM_RA_SOAT_TONG_HOP_3_TUAN.md`) đã audit tuần 1–3 **cả 4 người**. Bản 5 tuần này **đối chiếu chiều thời gian**: giữ kết luận tuần 1–3 khi chúng vẫn đúng, chỉnh khi có bằng chứng mới ở tuần 4–5, và **chấm lại số liệu đo được**.

---

## 0. Cảnh báo về tính đồng nhất số liệu

### 0.0. Phương pháp & môi trường kiểm chứng (chạy thật ngày 11/10)

| Việc chạy | Kết quả | Ghi chú |
|---|---|---|
| `dotnet test CulinaryBlog.sln` | **441/441 PASS** (`CulinaryBlog.Tests`) + **5/5 PASS** (`ConcurrencySpike`) = **446/446** · 0 fail · 0 skip · ~14 phút | .NET 10.0.401, PostgreSQL 16 local |
| `npx tsc --noEmit` (frontend) | **exit 0**, 0 lỗi | |
| `npx jest` (frontend) | **86/86 PASS**, **15/15 suite** (~14 s) | |
| `npx playwright test --list` | **46 test / 8 file spec** (liệt kê được, **KHÔNG chạy** được) | Docker daemon DOWN; Redis 6379 & MinIO/RustFS 9000 DOWN |
| `gh pr list` | 36 PR · xem §2.3 | |

> ⚠ **Phát hiện về khả năng tái lập (reproducibility):** lần chạy `dotnet test` **đầu tiên** báo **361 pass / 80 fail** vì tôi dùng mật khẩu Postgres của `.env.example` (`postgres`). Volume Postgres trên máy này được khởi tạo bằng `.env` (mật khẩu dev riêng trên máy — xem `.env:25`, không ghi giá trị vào báo cáo). Đổi đúng mật khẩu → **446/446 xanh**.
> **Hệ quả:** 80 fail là **lỗi môi trường, KHÔNG phải hồi quy code**; nhưng nó lộ ra rằng **một checkout sạch bằng `.env.example` sẽ đỏ 80 test** trừ khi volume được tạo đúng mật khẩu. Cần ghi rõ trong README/RUNBOOK.

### 0.1. Số test backend theo mốc — nhiều con số, đo ở nhiều thời điểm khác nhau

| Mốc / nguồn | Số liệu | Ghi chú |
|---|---|---|
| W1 (15/09) | **77/77** (72 + 5) | báo cáo 2 tuần (đã xóa) |
| W2 (22/09) | **125/125** (120 + 5) | |
| W3 (28/09) | **162/162** (157 + 5) | |
| W4 — tự khai trong `TUAN_4.md` | TV1 **177/177** · TV2 **178/178** · TV3 **177/177** | ⚠ chênh nhau **cùng 1 tuần** — do đo trên nhánh riêng, khác mốc commit |
| W4 — cuối kỳ (05/10) | **426/426** (421 + 5) | `TRANG_THAI_THUC_HIEN_TUAN_4.md` tại `fd90572` |
| W5 — tự khai | TV1 **183/183** · TV2 **194/194** · TV3 **281/281 + 5/5** → `Tuan06` **421/421 + 5/5** · TV4 **430/430** | ⚠ **4 con số khác nhau** cùng tuần 5 |
| **Đo lại 11/10 (bản này)** | **446/446** (441 + 5) trên nhánh rà soát | ✅ nguồn chuẩn duy nhất |

> **Kết luận đúng:** các con số **177/178/183/194/281/421/426/430** đều là số **thật tại snapshot của từng người/nhánh**, không phải bịa. Chúng **không so sánh trực tiếp được** vì (a) khác mốc commit, (b) số test tăng khi test của người khác merge vào `main`. Con số **duy nhất có nghĩa nghiệm thu** là số xanh trên **baseline tích hợp**: **446/446 ngày 11/10**.
> **Lưu ý:** độ vênh **178 → 426** của tuần 4 không phải một người viết thêm 248 test — phần lớn test của TV1/TV3 trở về qua `origin/main` (xác nhận bởi báo cáo tuần 4 của TV4).

### 0.2. Số commit theo thành viên/tuần — tính lại trên `origin/main` (author-date, non-merge)

| TV | W1 | W2 | W3 | W4 | W5 | Tổng |
|---|---:|---:|---:|---:|---:|---:|
| **TV1** Tâm (2312741) | 7 | 10 | 12 | 7 | 4 | **40** |
| **TV2** Vĩ (2312796) | 1 | 4 | 1 | 6 | 5 | **17** |
| **TV3** Trung (hqt7105) | 8 | 2 | 24 | 74 | 23 | **131** |
| **TV4** Sơn (2312739) | 0 | 9 | 27 | 34 | **1** | **71** |
| Vercel bot | 0 | 1 | 0 | 0 | 0 | 1 |
| **Tổng** | 16 | 26 | 64 | 121 | 33 | **260** |

> ⚠ **Số commit KHÔNG phải nỗ lực.** TV3 chiếm 131 (nhiều do migration/dọn nhánh); TV2 chỉ 17 nhưng nhiều commit lớn; TV1 có 40 (nhiều merge + tài liệu).
> **TV4 lệch nền đếm:** báo cáo của TV4 ghi **W5 = 28 commit** — đúng, nhưng tính **trên nhánh riêng** (`2312739_NHTSon_D5-D7`). **Trên `origin/main` TV4 W5 = 1** (commit `b7fb34a` suppress advisory). Toàn bộ 28 commit W5 của TV4 **chưa merge về main** (PR #32/#33 bị CLOSED — xem §2.3).

### 0.3. Số ô kỹ năng K — sổ tự đánh giá bị trôi (không phải gian lận)

| Nguồn | Con số | Ghi chú |
|---|---|---|
| TV1 (tự khai) | **24/24** | `LAB_04`; nhưng `LAB_KIEN_TRUC_API.md:3,12,17` ghi K01–K24 "**chưa xác nhận**" |
| TV2 (tự khai) | **24/24** (plan) / **20/24** (`TUAN_3.md:127`) | TV3 audit chỉ ra nhiều ô thiếu test |
| TV3 (tự khai) | **23/24 XONG, 1 CHƯA (K24)** | `BANG_MINH_CHUNG_K01_K24.md:14`; kế hoạch ghi 8/24 |
| TV4 (tự khai) | 6 🟢 + 8 🟢-gần + 8 🟡 + 2 ⬜ = **0/24 được Tâm nghiệm thu** | `TRANG_THAI_THUC_HIEN_TUAN_5.md §4` |
| `PHAN_CHIA_CONG_VIEC_6_TUAN.md` (nhóm) | TV1 18 · TV2 24 · TV3 8 · TV4 9 (+8 chờ) | 0/24 "đã xác nhận" |

> **Kết luận đúng:** ô K chỉ **được nghiệm thu** khi có `code/config + test chạy thật + Tâm xác nhận + ngày` (quy tắc `KE_HOACH_DU_AN.md:391`). Hiện **0/24 × 4 người được Tâm xác nhận** — nợ cũ nhất của nhóm (từ tuần 2). Mọi con số "24/24" đang là **tự khai**, chưa phải nghiệm thu.

---

## 1. Kết luận điều hành

> **Trạng thái sản phẩm (đo thật 11/10): xanh và lớn.** **446/446 test backend**, **tsc 0 lỗi**, **jest 86/86**, **46 E2E test Playwright tồn tại** (8 spec). Đây là mức đáng ghi nhận và **khớp** với kỳ vọng cuối tuần 5.
> **Trạng thái quy trình: đỏ.** **PR #27/#31/#34/#35 đều do TV4 (Tson-dev) merge, 0 review của Tâm** — vi phạm trực tiếp quy tắc "Tâm là người review duy nhất mọi PR" (`KE_HOACH_DU_AN.md:484`). **0/96 ô kỹ năng được nghiệm thu.**

### 1.1. Bảng audit cả 4 người (tuần 1–5)

| TV | Việc thật đáng ghi nhận (kiểm chứng) | Chỗ lệch / thiếu | Trạng thái thật |
|---|---|---|---|
| **TV1** Tâm | Auth/Identity/JWT/PBKDF2/lockout; CI backend; backup/restore `scripts/backup-db.sh`+`restore-db.sh`; `docker-compose.staging.yml`; `nginx/nginx.staging.conf`; `render.yaml`; `Week5StagingAndE2ETests.cs`; ADR-0004; review merge main | **Tự review chính mình** (mọi báo cáo "reviewer = Tâm"); **không merge/review PR nào trong tuần 5** dù được giao; 24/24 tự khai nhưng sổ thật ghi "chưa xác nhận" | ~78–85% · K **0/24 nghiệm thu** |
| **TV2** Vĩ | Category nghiệp vụ (409/unique/Admin policy); list/filter/sort/page; `Week5DiscoverySeoAndA11yTests.cs`; Jest; PR #18/#25/#31 (email password recovery — merged 11/10) | **FTS/GIN** bị TV3+TV4 độc lập chứng minh **không dùng index**; Google login từng là stub; E2E **category flow thiếu (G6 còn 1/5)** | ~46–60% · K **~4/24 kiểm chứng** |
| **TV3** Trung | Recipe aggregate/Nutrition/RowVersion; CQRS CRUD + Ingredient/Step; wizard RHF/Zod; refresh hash+rotation; JSON-LD đủ trường; `Tuan05/`+`Tuan06/` EXPLAIN/k6/schema/test-review; PR #27/#34/#35 | Có **mâu thuẫn nội bộ**: `TUAN_4.md` ghi "Reviewer: Tâm, 30/09" còn `BANG_MINH_CHUNG_K01_K24.md:41` ghi "chưa có xác nhận" | ~70–80% · K **~5/24**, **K24 chưa** |
| **TV4** Sơn | Storage/MinIO→RustFS + ADR; upload/primary/delete + magic bytes; resize Hangfire; publish/unpublish/archive/delete + UI 3 nút; sitemap/robots/SEO; staging 2 lần + 2 API + TLS + runbook; `Week5StagingAndE2ETests`; tự tìm & sửa nhiều bug (async-void, JPEG signature, 503, sitemap retry) | PR #32/#33 **bị CLOSED chưa merge**; W5 thật trên main chỉ **1 commit**; K09 Google còn chờ credentials | ~55–60% · K **0/24 nghiệm thu**, **G6 4/5** |

### 1.2. Ba phát hiện nghiêm trọng nhất (cập nhật tuần 5)

1. **Quy trình review PR sụp hoàn toàn.** `gh pr view` cho thấy **#27, #31, #34, #35 đều `mergedBy = Tson-dev` (TV4)** với `reviews: []`. Người **được giao review duy nhất là TV1 Tâm** (`KE_HOACH_DU_AN.md:484`) **không review/merge PR nào trong tuần 5**. Trớ trêu: PR #32/#33 **của chính TV4** lại bị CLOSED không merge. → DoD "có PR được review" **không đạt cho mọi thành viên**.
2. **0/96 ô kỹ năng K được nghiệm thu.** Cả kế hoạch (`PHAN_CHIA:528-531` = 0/24 mỗi người), sổ TV3 (`BANG_MINH_CHUNG:41`), và TV4 tự chấm (`MAPPING...:48`) **đều hội tụ về 0**. Con số 24/24 tự khai của TV1/TV2 **mâu thuẫn** với chính tài liệu khác của họ.
3. **FTS/GIN: 1 người báo thành công, 2 người độc lập chứng minh ngược.** TV2 báo "GIN trigram hoạt động"; TV3 (`HIEU_NANG_TUAN5.md:15–17`) và TV4 (`W5-8`) đều đo `EXPLAIN` thấy **Seq Scan/không dùng index** ("trigram GIN chưa dùng", `Recipes` chỉ ~167–170 dòng). Bằng chứng **nghiêng về phía TV3/TV4**.

### 1.3. Ghi nhận công bằng

- **Sản phẩm tích hợp thật sự chạy** (446/446 + tsc 0 + jest 86/86) — thành tựu của **cả nhóm**, lớn hơn đáng kể so với mốc 3 tuần (178/178).
- **TV4 trung thực nhất về hạ tầng**: tự chấm 52/56 = 93%, ghi rõ giới hạn self-signed TLS, tự tìm/sửa bug hạ tầng dùng chung (async-void làm mất ảnh, JPEG 3-byte bypass, DB-down trả 500 thay vì 503).
- **TV3 mạnh nhất về tính liêm chính số liệu**: tự tạo `Tuan05/`/`Tuan06/` với EXPLAIN, k6, data-integrity, schema, review test TV2 — trong đó có cả việc **bác claim của TV2 và tự nhận K24 chưa xong**.

---

## 2. Bức tranh tổng thể theo tuần

### 2.1. Đường cong tiến bộ (đo thật + ghi nguồn)

| Mốc | Test backend (+spike) | Hạ tầng / cột mốc |
|---|---|---|
| Hết W1 (15/09) | 77 | Identity/JWT/CI backend, Category nền, Compose/Nginx health |
| Hết W2 (22/09) | 125 | MinIO/storage, recipe aggregate (TV3 bàn giao), search đầu |
| Hết W3 (29/09) | 162 | Gỡ 4 block (D23/D27/C07 + recipe API), RustFS, resize Hangfire, OTEL, sitemap |
| Hết W4 (06/10) | 426 | Health/trace/backup-restore, đóng 5 issue, lab L5, E2E 26/26 |
| **Hết W5 (11/10, đo lại)** | **446** | **Staging 2 lần + 2 API + TLS + runbook + 40/40 E2E staging + k6 102 VU** |
| **11/10 (đo thật bản này)** | **446/446** · tsc **0** · jest **86/86** | 46 test Playwright (8 spec) tồn tại |

### 2.2. Mốc PR đóng tuần 5 (nguồn: `gh pr list`)

| PR | Người mở | Nội dung | Trạng thái | mergedBy |
|---|---|---|---|---|
| **#31** | TV2 | auth: email password recovery flow | **MERGED 11/10 06:50** | **Tson-dev (TV4)** |
| **#34** | TV3 | tuần 5 — kiểm thử, Schema, số đo, review test TV2 | **MERGED 11/10 06:53** | **Tson-dev (TV4)** |
| **#35** | TV3 | tuần 5-6 — Schema, regression, a11y | **MERGED 11/10 07:21** | **Tson-dev (TV4)** |
| **#32** | TV4 | Tuần 5 (D5/D7) — baseline 426/426, dọn nhánh lab | **CLOSED (không merge)** 07/10 | — |
| **#33** | TV4 | practice/TV4/L4 — lab L4 media/email/xml/jobs | **CLOSED (không merge)** 07/10 | — |
| #27 | TV3 | wizard + JSON-LD/SEO + a11y | MERGED 05/10 | **Tson-dev (TV4)**, `reviews: []` |

> **Điểm nóng:** 4/4 PR được merge trong tuần 5 **không qua Tâm**; 2 PR của TV4 **bị đóng không merge**. Cả hai chiều đều vi phạm quy tắc nhóm.

### 2.3. Quyết định nhóm từng chặn tiến độ — trạng thái cuối tuần 5

| Mã | Nội dung | Kết quả |
|---|---|---|
| D23 | Queue resize: Hangfire vs BackgroundService | ✅ Chốt **Hangfire** (W3) |
| D27 | Bucket policy: public-read vs private | ✅ Chốt **proxy có auth** + presigned tồn đọng đã xử lý (W5) |
| C07 | Category delete: hard vs soft | ✅ Sửa **hard delete** + ADR-TV4-003 |
| **Reviewer** | Toàn bộ PR do **Tâm** duyệt | ❌ **Vỡ ở tuần 5**: 4 PR merge không qua Tâm (§2.2) |
| Backup lịch/kho | Nơi đặt lịch + giữ 30 ngày | 🔴 **Chưa chốt** (nợ từ 30/09) |
| Google OAuth creds | K09 | 🔴 **Chưa có** (từ tuần 3) |

---

## 3. Audit TV1 — Nguyễn Thanh Tâm (2312741)

**Phân công:** A1–A7 (auth/identity/pipeline/CI), **người review duy nhất**, 6 FR + 6 NFR chủ trì.
**Commit trên `origin/main`:** W1=7 · W2=10 · W3=12 · W4=7 · W5=4 = **40** (kể cả merge).

### 3.1. Đã làm thật (kiểm chứng)

| Việc | Bằng chứng |
|---|---|
| NFR-SEC-001 PBKDF2-HMACSHA512 100k, password phức tạp | `Program.cs`, `AuthTests.cs` (đã xác minh ở bản 3 tuần) |
| Lockout 5 lần/15 phút; `GET/PATCH /auth/me` không lộ field nhạy cảm | bản 3 tuần §3.1 |
| **Backup/restore thật** | `scripts/backup-db.sh`, `scripts/restore-db.sh`, `scripts/backup-storage.sh` **tồn tại** ✓ |
| **Staging hạ tầng** | `docker-compose.staging.yml`, `nginx/nginx.staging.conf`, `render.yaml` **tồn tại** ✓ |
| **Test tuần 5** | `tests/CulinaryBlog.Tests/Week5StagingAndE2ETests.cs` **tồn tại** ✓ |
| **ADR staging/DR** | `docs/adr/0004-staging-deployment-and-disaster-recovery.md` ✓ |
| Duy trì `main` | nhiều merge commit; vá CVE react-server-components (PR #6) |

### 3.2. Sai lệch / thiếu sót (cập nhật tuần 5)

| Vấn đề | Bằng chứng | Mức |
|---|---|---|
| **Không review/merge PR nào trong tuần 5** dù là reviewer duy nhất | `gh pr view`: #27/#31/#34/#35 `mergedBy=Tson-dev`, `reviews: []` | 🔴 Quy trình |
| **Tự review chính mình** — mọi báo cáo TUAN_4/5 ghi "reviewer = Tâm" | đối chiếu `PHAN_CHIA:58` (không ai review Tâm) | 🔴 |
| **Tự khai 24/24 nhưng tài liệu khác ghi "chưa xác nhận"** | `LAB_KIEN_TRUC_API.md:3,12,17` | 🟠 |
| **Tâm viết báo cáo `TUAN_4.md` cho TV2/TV3/TV4** (batch `80b2c0e`) | `Tuan04/TRANG_THAI...:180-189` (TV4 tự ghi nguồn) | 🟠 Tính liêm chính minh chứng |
| NFR-SEC-003 rate limit (bản 3 tuần: 1/5, fixed window, thiếu XFF) — **đã có BUG-W4-03 sửa `UseForwardedHeaders`** (do TV4) | `Program.cs` (`afdb4a7`) | 🟡 cải thiện |
| FR-JOB-001 dùng Channel/Task.Delay thay Hangfire, thiếu link app | bản 3 tuần §3.2 | 🟠 |
| CHANGELOG đứng `0.1.0` | bản 3 tuần §3.2 | 🟡 |

> **Nhận xét:** TV1 làm nền tảng auth + hạ tầng **thật và tốt**, nhưng **vai trò reviewer — trách nhiệm chính của nhóm trưởng — không được thực thi trong tuần 5**. Đây là nguyên nhân gốc khiến cả nhóm vi phạm DoD "có PR được review" và 0/96 ô K được nghiệm thu.

---

## 4. Audit TV2 — Ngô Quốc Trường Vĩ (2312796)

**Phân công:** B1–B7 (category, khám phá/lọc/sort/page, FTS, Google auth, cache, lab, test/docs).
**Commit trên `origin/main`:** W1=1 · W2=4 · W3=1 · W4=6 · W5=5 = **17**.

### 4.1. Đã làm thật (kiểm chứng)

| Việc | Bằng chứng |
|---|---|
| Category nghiệp vụ: slug, 409 khi còn recipe, Admin policy | bản 3 tuần §4.1 (92%) |
| List/filter/sort/page chất lượng tốt (sort allowlist, filter AND, phân trang đủ) | bản 3 tuần §4.1 (86%) |
| **Test tuần 5** | `tests/CulinaryBlog.Tests/Week5DiscoverySeoAndA11yTests.cs` **tồn tại** ✓ |
| **PR mới** | #31 auth email password recovery **MERGED 11/10** ✓ |
| Jest frontend (phần search/discovery) | jest tổng **86/86** ✓ |

### 4.2. Sai lệch — điểm nóng

| Claim | Thực tế / Kiểm chứng | Kết luận |
|---|---|---|
| **FTS "GIN trigram hoạt động"** | TV3 `HIEU_NANG_TUAN5.md:15–17` + TV4 W5-8 `EXPLAIN` đều thấy **không dùng index** (Seq Scan, ~167 dòng) | ❌ **Bị 2 người độc lập bác** |
| **Google login** | từng là `dev_google:`/`mock_google:` (stub); verify thật **chưa chạy** vì thiếu credentials | ❌ K09 chưa đạt |
| **E2E category (G6 luồng 5)** | Playwright `--list` chỉ có **8 spec**, **KHÔNG có `category.spec.ts`** | ❌ **G6 còn 1/5** |
| Category count 25 (seed) vs 24 (đo) | TV3 `KIEM_TRA_TUAN4.md:104-106` | ⚠ sai lệch dữ liệu |
| K21/K24 (E2E/lab) | PR #27 là **của TV3**; lab TV2 không tồn tại | ❌ |

> **Nhận xét:** TV2 làm phần khám phá/category **dùng được**, và vừa bổ sung email recovery (PR #31, merged 11/10). Nhưng **hai hạng mục quan trọng nhất của phần mình — FTS và Google — chưa đạt**, và **luồng E2E category** (do G6 yêu cầu) **vẫn thiếu**, là một trong các block chặn G6.

---

## 5. Audit TV3 — Huỳnh Quốc Trung (hqt7105)

**Phân công:** C1–C7 (recipe aggregate/Nutrition, CQRS CRUD, Ingredient/Step, wizard UI, refresh token, lab/test/arch/deploy). 6 FR + 9 NFR chủ trì.
**Commit trên `origin/main`:** W1=8 · W2=2 · W3=24 · W4=74 · W5=23 = **131**.

### 5.1. Đã làm thật (kiểm chứng)

| Việc | Bằng chứng |
|---|---|
| Recipe aggregate, Nutrition owned, RowVersion, migration/index | bản 3 tuần §5.1 (~85%) |
| CQRS Create/Update/Detail + ownership 403 test | bản 3 tuần §5.1 |
| Ingredient/Step CRUD, renumber 1..N, unique `(RecipeId, StepNumber)` | migration + tests |
| Wizard RHF + Zod + TanStack Query; ARIA/semantic | `RecipeWizard.tsx`, package.json |
| Refresh token hash SHA-256 + rotation | `IdentityService.cs` |
| **NFR-SEO-001 JSON-LD đủ 14 trường, không bịa aggregateRating** | `recipe-jsonld.ts:28-49` |
| **Số đo & review độc lập W5/W6** | `Tuan05/` (`HIEU_NANG_TUAN5.md`, `NHAT_QUAN_DU_LIEU.md`, `SCHEMA_RECIPE.md`, `REVIEW_TEST_TV2.md`); `Tuan06/` |
| PR #27, #34, #35 (Schema/a11y/regression) | `gh pr list` |

### 5.2. Sai lệch / điểm cần làm rõ

| Vấn đề | Bằng chứng | Mức |
|---|---|---|
| **Mâu thuẫn nội bộ về reviewer** | `TUAN_4.md:50,64,77` ghi "Reviewer: Tâm — 30/09/2026" **vs** `BANG_MINH_CHUNG_K01_K24.md:41` ghi "**chưa có xác nhận ô nào**" | 🔴 |
| **Nghi vấn nguồn gốc `TUAN_4.md`** | TV4 ghi các file `TUAN_4.md` gốc do **TV1** tạo (batch `80b2c0e`), không phải cá nhân tự viết | 🟠 |
| **K24 chưa xong** | `BANG_MINH_CHUNG_K01_K24.md:14` tự nhận; PR #27 merged **0 review** | 🟠 |
| Khai `NFR-DATA-001` (không tồn tại trong SRS) + gán nhầm `FR-RCP-001`/`NFR-USE-001` (của TV2) | bản 3 tuần §5.2 | 🟠 |
| NFR-SEC-002 thiếu `FamilyId`; NFR-SEO-004 thiếu 301 | bản 3 tuần §5.2 | 🟠 |

> **Nhận xét:** TV3 là **trụ cột khối lượng** (131 commit) và **người audit số liệu nghiêm túc nhất**. Điểm trừ nằm ở **tính nhất quán tài liệu** (reviewer) và **2 mã FR/NFR gán nhầm/tự bịa** ở bản tuần 4.

---

## 6. Audit TV4 — Nguyễn Hữu Trung Sơn (2312739)

**Phân công:** D1 Storage/MinIO · D2 Resize · D3 Publish/Unpublish/Archive/Delete + Logout · D4 Uploader UI + SEO · D5 Compose/Nginx/Health/OTEL/Backup · D6 Lab · D7 E2E/Runbook.
**Commit trên `origin/main`:** W1=0 · W2=9 · W3=27 · W4=34 · **W5=1** = **71** (toàn bộ 28 commit W5 nằm ở nhánh riêng, **chưa merge**).

### 6.1. Đã làm thật (kiểm chứng)

| Mã | Bằng chứng |
|---|---|
| D1 Storage | `MinioStorageService`, magic bytes JPEG/PNG/WebP/AVIF, 5 MiB; `ImageUploadValidatorTests` |
| D2 Resize | `ResizeImageJob`, Hangfire WorkerCount=4, queue PostgreSQL, `ImageResizeD2Tests` 4/4 |
| D3 backend | publish/unpublish/archive/delete + AuthorPolicy; `RecipeLifecycleHandlerTests` 23/23 |
| D3 UI | nút **Gỡ đăng/Lưu trữ/Xóa** + xác nhận 2 bước; `recipe-unpublish.spec.ts` 3/3 |
| D4 SEO | `sitemap.ts` 96 URL, `robots.ts`, `generateMetadata`+canonical+OG |
| D5 hạ tầng | `docker-compose.staging.yml`, `nginx/nginx.staging.conf`, 2 API instance, TLS self-signed, HSTS, volumes, runbook |
| D6/D7 | `practice/TV4/L4` 14 file lab 39/39; `Week5StagingAndE2ETests.cs`; 40/40 E2E staging ×2 |
| Bug tự tìm & sửa | async-void stream copy mất ảnh; JPEG 3-byte signature bypass; DB-down trả 500→503; sitemap `[AutomaticRetry]`; `UseForwardedHeaders` (BUG-W4-03) |

### 6.2. Sai lệch / thiếu sót

| Vấn đề | Bằng chứng | Mức |
|---|---|---|
| **PR #32/#33 bị CLOSED, không merge** | `gh pr`: state `CLOSED`, `mergedAt: null` | 🔴 |
| **W5 thực trên `main` = 1 commit**, không phải 28 | `git log origin/main` | 🟠 Truyền thông số liệu |
| **G6 còn 1/5 luồng** (category thiếu) | Playwright `--list` không có `category.spec.ts` | 🔴 |
| K09 Google chờ credentials; 2 phase L5 + `cqrs-behavior` cắt tuần 6 | W5-9 ghi ⬜ | 🟠 |
| NFR-SEO-004 (301 đổi slug) chưa có | W5-8 ghi nợ TV3 | 🟠 |
| Không tự tuyên bố SLA 99.5% (đúng — self-signed) | W5-4/runbook giới hạn rõ | ✅ trung thực |

> **Nhận xét:** TV4 làm khối hạ tầng/vận hành **dày và trung thực**, tự chấm 93%, gỡ nhiều bug dùng chung. Điểm trừ: **đóng PR của chính mình**, và G6 vẫn thiếu luồng category (không thuộc phạm vi TV4 tự quyết).

---

## 7. Ma trận FR / NFR / hạ tầng xuyên nhóm (cuối tuần 5)

| Hạng mục | Yêu cầu | Thực tế đo được | Kết luận |
|---|---|---|---|
| Test thật | — | **446/446** backend · **86/86** jest · **tsc 0** | ✅ |
| 34 FR | Đủ | Mọi FR chính có code; **Google/FTS/301 slug/E2E category** còn nợ | ⚠ |
| 30 NFR | Đủ | Đạt trọn nhiều: SEC-001/004, SEO-001/002/003, REL-002, MAINT-004 | ⚠ đa số đạt |
| 10 CONS | Nhất quán | CHANGELOG đứng `0.1.0`; một số secret từng lộ (đã dọn) | ⚠ |
| 5 E2E G6 | 5/5 | **4/5** — thiếu **category** | 🔴 |
| Playwright | Cấu hình + spec | **46 test / 8 spec tồn tại** (runtime chặn vì Docker down) | ✅ artifact |
| 24 K × 4 | 96 ô | **0/96 được Tâm nghiệm thu** | 🔴 |
| Review PR | 1 reviewer | **0/4 PR tuần 5 qua Tâm** | 🔴 |
| Backup/restore | Có | script + drill staging (14 bảng) | ✅ |
| Multi-instance | Có | 2 API + Nginx upstream, failover 0/8 | ✅ |

---

## 8. Vi phạm Definition of Done (`KE_HOACH_DU_AN.md` mục 12.3)

| Điều kiện DoD | TV1 | TV2 | TV3 | TV4 |
|---|---|---|---|---|
| Có **PR được review** | ❌ không review ai + tự review mình | ❌ #31 merged không qua Tâm | ❌ #27/#34/#35 merged không qua Tâm | ❌ #32/#33 closed |
| Đúng **FR và ADR** | FR-JOB-001 lệch | ❌ FTS/GIN sai + vi phạm ADR 0003 | ❌ gán nhầm mã | ❌ thiếu 301 slug |
| **UI nếu cần** | ⚠ | ❌ thiếu UI update category | ⚠ | ✅ W5 bổ sung 3 nút |
| Test phù hợp **pass** | ✅ (446 chung) | ⚠ FTS không chứng minh | ⚠ K24 chưa | ✅ |
| Số đo **có hoặc ghi chưa đạt** | ⚠ | ⚠ | ✅ (EXPLAIN/k6) | ✅ (kèm giới hạn) |
| Ô K được **nghiệm thu** | ❌ | ❌ | ❌ (K24) | ❌ |

---

## 9. Ma trận 24 kỹ năng — đối chiếu tự khai vs kiểm chứng

| TV | Tự khai | Nghiệm thu được | Ghi chú |
|---|---|---|---|
| TV1 | 24/24 (LAB_04) | **0/24** | chính sổ TV1 ghi "chưa xác nhận" |
| TV2 | 24/24 (plan) / 20/24 | **~4/24** (K01–K03, K16) | TV3 audit chỉ ra thiếu test/Google/FTS |
| TV3 | 23/24 (1 chưa = K24) | **~5/24** | đạt một phần K05/K08 |
| TV4 | 0/24 nghiệm thu (tự chấm 6+8+8+2) | **~9/24 chấp nhận** (còn K08/K11 của người khác) | trung thực nhất |

> Áp dụng quy tắc `KE_HOACH_DU_AN.md:391`: ô nhiều kỹ thuật con, thiếu một phần thì **chưa hoàn thành**. Vì không ai được Tâm ký, cột "nghiệm thu" **= 0 cho cả 4**; các số "kiểm chứng được" chỉ là quan sát nội bộ, **không thay nghiệm thu**.

---

## 10. Ưu tiên xử lý đề xuất (tuần 6 / đóng G6, nghiệm thu cuối)

### P0 — chặn nghiệm thu
1. **Tâm thực hiện vai trò reviewer**: review ngược #27/#31/#34/#35 và **mở lại/duyệt #32/#33** (hoặc mở PR mới cho công việc W5 của TV4). → DoD "PR được review".
2. **Luồng E2E `category`** (G6 5/5) — TV2/Tâm xác nhận ai viết → chạy trên staging.
3. **Google OAuth credentials** (K09) — chờ từ tuần 3; mock không tính là hoàn thành.
4. **Chốt 24 ô K + mapping K01** cho từng người, có ngày — nợ từ tuần 2.

### P1 — trước khi tổng kết
5. **FTS đúng ADR-0003**: `f_unaccent` bất biến + `tsvector` + trigger + `ts_rank`; đo lại `EXPLAIN` thấy dùng GIN. (TV2)
6. `NFR-SEO-004` — 301 khi đổi slug (TV3).
7. `query-rollback` (RowVersion) + `cqrs-behavior` (K04) + `image-opt` — chạy nốt lab L5 (TV4).
8. Chốt 3 việc hạ tầng: nơi đặt lịch backup · kho 30 ngày · rotate khoá JWT ngoài repo (Tâm/TV2).

### P2 — tài liệu / học phần
9. **Một sổ K duy nhất** cho nhóm (mỗi ô: `mã | bằng chứng file:line | trạng thái | reviewer/ngày`), cập nhật qua PR.
10. Ghi rõ trong README/RUNBOOK: mật khẩu Postgres local khác `.env.example` → checkout sạch sẽ đỏ 80 test nếu volume sai mật khẩu.
11. Cập nhật `CHANGELOG.md`/`README.md` (số test, mốc).

---

## 11. Ghi chú cuối

- Rà soát này **không sửa file nguồn nào** của tuần/kế hoạch/sổ; chỉ ghi lại **1 báo cáo** trong `Report/`.
- **Đã chạy thật:** `dotnet test` (446/446), `tsc --noEmit` (0), `jest` (86/86), `playwright --list` (46/8), `gh pr list`, `git log`. **Chưa chạy được:** Playwright runtime, staging/Docker (daemon DOWN), k6, các drill — nên các số E2E/load vẫn dẫn nguồn từ tài liệu TV3/TV4, **chưa tái lập tại máy này**.
- **CHƯA XÁC MINH ĐƯỢC:** nội dung review trên GitHub (không tồn tại), lịch sử CI Actions đầy đủ, số `npm audit` theo từng ngày, nội dung từng file log trong `Tuan05/logs/`.
- Quy tắc giữ nguyên: **không copy số tuần trước làm số tuần này**; ô thiếu ghi `⬜`; không coi mock là hoàn thành; không ghi secret vào báo cáo; mọi trích dẫn kèm `file:line`/commit/PR.
- Báo cáo mang tính **quan sát độc lập**, **không dùng làm căn cứ chấm điểm**. Mọi kết luận đều kèm nguồn để tự kiểm chứng.

### 11.1. Đính chính so với bản 3 tuần

| Kết luận cũ (3 tuần) | Cập nhật cuối tuần 5 | Căn cứ |
|---|---|---|
| "Không có script backup/restore nào trong repo" | **Sai ở mốc 11/10** — `scripts/backup-db.sh` + `restore-db.sh` + `backup-storage.sh` đã tồn tại | kiểm tra `Get-ChildItem` ✓ |
| "0 Playwright config/spec/test" | **Đã có** — `playwright.config.ts` + **8 file spec / 46 test** | `npx playwright test --list` ✓ |
| "Jest 0 file" (kỳ vọng TV3) | **86/86 pass, 15 suite** | `npx jest` ✓ |
| TV4 "không có collector Seq/OTEL" | Cần kiểm chứng lại — không nằm trong phạm vi bản này | `CHƯA XÁC MINH` |
| "Repo có 5 con số test, chỉ 1 đúng" | **Không phải** — tất cả là số thật theo snapshot; chỉ số **trên baseline tích hợp** mới dùng để nghiệm thu (**446/446**) | §0.1 |
| TV2 "thổi phồng nặng" | Vẫn đúng phần FTS/Google; **bổ sung** PR #31 merged 11/10 | §4 |

> **Bài học phương pháp:** đừng kết luận từ một thời điểm. Cùng một hạng mục, đo ở mốc commit khác nhau cho con số khác nhau — **và cả hai đều có thể đúng**. Chỉ số trên **baseline tích hợp (`origin/main`)** mới dùng để nghiệm thu; muốn đánh giá "làm được gì" phải **chạy thật** chứ không đọc tự khai.
