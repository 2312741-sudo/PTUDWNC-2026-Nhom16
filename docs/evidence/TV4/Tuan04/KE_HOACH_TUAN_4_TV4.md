# KẾ HOẠCH TUẦN 4 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

- **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026, giải quyết dứt điểm C01–C09)
- **Phần nghiệp vụ**: Xuất bản, hình ảnh, SEO và **vận hành** — theo `docs/KE_HOACH_DU_AN.md` mục 8 và `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md` mục 3.4
- **Mã task tuần 4**: **D5** (Compose/Nginx, health/OTEL/metrics, persistent volumes, **backup/restore/multi-instance**), **D6** (lab cá nhân K01–K24), **D7** (Integration/UI/publish E2E, resilience/load/SEO, **runbook và release**). Cộng phần bàn giao còn treo từ tuần 3: D4-UI (progress upload + nút Unpublish/Archive), CR-7 (sitemap job lịch 02:00 UTC + distributed lock), và các đề xuất **B1/B2** đang chờ quyết định nhóm.
- **Nhánh Git**: `2312739_NHTSon_D5-D6-D7` (tv4/week4), khởi tạo từ `origin/main` = `7fe8fc2`
- **Nhánh lab**: `practice/TV4/L4` (đã có, chưa merge vào main theo đúng quy ước LAB) · lab mới dự kiến: `practice/TV4/L5`
- **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng)
- **Cổng nghiệm thu**: **G4** giữa tuần → **24/24 ô kỹ năng K01–K24 có code/config + test + demo + reviewer xác nhận**; **G5** cuối tuần → **Application line coverage ≥ 80% có số đo thật, sửa xong lỗi chặn luồng / lỗi bảo mật, CI xanh**.

> Tuần 4 tính từ **30/09/2026** (ngày PR #19 của TV4 được merge vào `main` qua `208b7f7`).
> Tài liệu này là **kế hoạch thực thi + khung minh chứng**. Mọi việc ở tuần 4 khởi đầu ở trạng thái
> **Chưa làm**, trừ phần đã được kiểm chứng thật ở mục 2 (có đường dẫn/commit/số liệu).

> [!IMPORTANT]
> **Ghi chú về nguồn gốc file `Lab 04` trong `docs/evidence/TV4/`** — không phải do TV4 tạo
> 4 file `TUAN_4.md`, `BAO_CAO_LAB_04.md`, `Lab04_2312739_NguyenHuuTrungSon.docx`,
> `Lab4_2312739_NguyenHuuTrungSon.docx` nằm trực tiếp trong `docs/evidence/TV4/` được tạo bởi
> commit **`80b2c0e` của Nguyễn Thanh Tâm (TV1 — nhóm trưởng)**, subject
> *"docs: complete week 4 reports for all members (TV2, TV3, TV4) and update README"*
> (cùng một commit sinh ra bộ 3 file tương ứng cho TV2 và TV3).
> → Theo quyết định nhóm 30/09: **giữ nguyên file, không sửa, không xoá** (không tự ý đụng tài liệu
> của thành viên khác), nhưng **không dùng làm minh chứng cá nhân của TV4**. Toàn bộ minh chứng
> tuần 4 của TV4 nằm trong `docs/evidence/TV4/Tuan04/`. Tài liệu do chính TV4 viết tuần 4:
> `docs/evidence/TV4/HUONG_DAN_CHAY_TV4.md` (commit `c5361eb`).

---

## 0. Bối cảnh bước vào tuần 4 (đã kiểm chứng trên `origin/main` = `7fe8fc2`, 30/09/2026)

| # | Sự kiện | Trạng thái |
|---|---|---|
| 1 | **PR #19 của TV4 đã merge** vào `main` (`208b7f7`): fix lỗi upload ảnh `500` do thiếu cấu hình `Minio`/S3, `DotNetEnv` + `EnvFileLoader`, `DevConfigParityTests` (3 test) | ✅ Đã vào `main` |
| 2 | **PR #16 (tuần 3) đã merge** (`607ee14`): D23 Hangfire persistent + dashboard Admin, D27 media proxy có auth, D2 resize 300×300/800×600, D4 sitemap/robots/OG/JSON-LD, D5 EXPLAIN + k6 | ✅ Đã vào `main` |
| 3 | **Lỗi `500` trang `/search` (do TV4 phát hiện)** đã được **TV2 sửa** trên `main`: `e523579` *"fix(frontend): extract SearchFilterSelect to client component"* — đúng **Phương án B** trong báo cáo của TV4 | ✅ Đã sửa (xem mục N0-3 để verify lại bằng test) |
| 4 | `main` đã có thêm 4 commit sau PR #19: `e523579` (fix search SSR), `424013a` (hướng dẫn cài đặt), `c0ff83d` (search/Google/seeder), `7fe8fc2` (seeder hard-delete recipe cũ); ngày 30/09 có thêm `1492b39` (TV2: sửa hiển thị ảnh + ô tìm kiếm ở `/recipes`) | ✅ Nhánh tuần 4 lấy từ đây, đã merge `1492b39` (merge `4770602`) |
| 5 | **Bộ kiểm thử cuối đã xác nhận**: `172/172` pass (`167` `CulinaryBlog.Tests` + `5` `ConcurrencySpike`, `Skipped=0`) tại `80b2c0e`; QA tích hợp FE–BE `41/41` | ✅ Đã **đo lại 30/09** trên nhánh tuần 4: **178/178** (`173` + `5`), `Skipped=0`, build 0 warning, coverage `Application` **83.37%** — xem `SO_EVIDENCE_TUAN_4.md` §1 |
| 6 | **Kỹ năng TV4 hiện tại: 9/24 ô** (K01, K05, K08, K11, K12, K13, K20, K23, K24) theo `PHAN_CHIA_CONG_VIEC_6_TUAN.md` | ❌ Còn thiếu **15 ô** → đây là mục tiêu lớn nhất của G4 |
| 7 | Chưa bắt đầu ở tuần 4: retry/race/publish E2E, backup & restore multi-container, shared cache/multi-worker, file-size attack + MIME spoofing | ❌ Xem N1/N2/N4 |
| 8 | **Block chờ quyết định nhóm**: B1 (`500` → `503 storage.unavailable`), B2 (fail-fast khi thiếu cấu hình), B4 (health check storage xác thực credential), B5 (xem ảnh Draft trong wizard), B6 (tạo/distribute tài khoản Admin) | ⛔ Xem mục 7 |

---

## 1. Mục tiêu tuần 4

| Tiêu chí | Bàn giao kỳ vọng |
|---|---|
| **Nghiệp vụ** | D5 hoàn thiện phần **còn thiếu thật sự**: health check xác thực credential storage (B4), OTEL có **collector thật** + Serilog→Seq + **bằng chứng trace HTTP→DB**, **backup/restore có script + drill thật** (`pg_dump` 03:00, giữ 30 ngày), **2 API instance dùng chung cache + chung queue**; sitemap theo **cron 02:00 UTC + distributed lock** (CR-7) |
| **Nghiệm thu** | `/health/ready` **503** khi Redis chết (có test, không phải "chấp nhận 200 hoặc 503"); restore được cả **dữ liệu và file**; 2 instance không sinh job trùng; **kịch bản tấn công file** (size/MIME spoofing) bị chặn đúng mã lỗi; **deploy lặp lại được** bằng 1 lệnh từ checkout sạch; runbook có số liệu failover/retry thật |
| **Skill** | Đóng nốt **15 ô K còn thiếu** (K02, K03, K04, K06, K07, K09, K10, K14, K15, K16, K17, K18, K19, K21, K22) bằng SP + LAB L5; bổ sung `.gitignore`/secret-scan gate, CI có **cổng frontend**; đo **coverage có ngưỡng** |
| **Cổng** | G4: 24/24 ô K có minh chứng + reviewer Tâm xác nhận · G5: coverage ≥ 80% có số đo, sửa xong lỗi chặn luồng/bảo mật, CI xanh |

---

## 2. Hiện trạng repo (đã rà soát từng file — không phải ước lượng)

### 2.1 D5 — Hạ tầng / health / observability / backup / scale

| Hạng mục | Trạng thái thật | Đường dẫn / bằng chứng |
|---|---|---|
| Dockerfile multi-stage | 🟡 Chỉ **backend**, `EXPOSE 8080`, **không có `HEALTHCHECK`**, không có Dockerfile cho frontend | `Dockerfile` |
| Compose dev | ✅ 6 service + volume `pgdata`/`redisdata`/`s3data`/`seqdata`; ⚠️ **chỉ postgres + s3 có healthcheck** (redis/mailhog/seq/nginx không có) | `docker-compose.dev.yml` |
| Nginx | 🟡 `listen 80` **không có `ssl_certificate`**, không có `proxy_next_upstream`, chỉ proxy `/api/` + `/health` (không `/health/live`, `/health/ready`) | `nginx/nginx.dev.conf` |
| Compose production | ❌ **Không có** `docker-compose.prod.yml` | `Test-Path` = false |
| Health endpoints | ✅ `/health`, `/health/live`, `/health/ready`; ⚠️ `ready` chỉ gồm `database` + `redis` | `src/backend/CulinaryBlog.API/Health.cs`, `Program.cs:149-153, 561-563` |
| **Health check storage** | ⛔ **Chỉ TCP probe** (`TcpHealthCheckHelper.CheckTcpAsync`, `Health.cs:36-43`) → cổng mở nhưng credential sai vẫn báo `Healthy` (đúng nguyên nhân gốc lỗi 500 ở tuần 3) | Block **B4** — `docs/proposal/DE_XUAT_04_HEALTH_CHECK_STORAGE_XAC_THUC.md` (**chưa implement**) |
| **Test health khi dependency chết** | ⛔ `HealthTests.cs` chỉ có 2 test và **chấp nhận cả 200 lẫn 503** → không có kịch bản failure | `tests/CulinaryBlog.Tests/HealthTests.cs:41` |
| OTEL trace + metrics | 🟡 Đã cấu hình (ASP.NET + HttpClient + EF) nhưng `AddOtlpExporter()` **không có endpoint** → không nơi nhận; **không có service otel-collector** | `Program.cs:156-167` |
| Serilog → Seq | ⛔ Chỉ `WriteTo.Console`; **không có `Serilog.Sinks.Seq`** dù container `datalust/seq:2026.1` đã có trong compose và **chưa service nào dùng** | `Program.cs:41-44`; `docker-compose.dev.yml:71-84` |
| **Bằng chứng trace thật** | ⛔ **Không có** file trace nào trong `Tuan03/logs/` (chỉ có d2/explain/k6) | `TRANG_THAI_THUC_HIEN_TUAN_3.md` đã tự ghi nhận |
| **Backup / restore** | ⛔ **Không tồn tại**: `pg_dump` xuất hiện **0 lần** ngoài văn bản SRS; không có `deploy/`, không có `scripts/`, không có cron | `Test-Path deploy/ scripts/ = false` |
| **Multi-instance / shared cache** | ⛔ `RecipeCacheService` là `ConcurrentDictionary` **trong tiến trình**; **0 dòng** dùng `StackExchange.Redis`; `render.yaml` để `plan: free` | `src/backend/CulinaryBlog.Infrastructure/RecipeCacheService.cs:7-56` |
| Distributed lock | ⛔ `0` match `DistributedLock` / `DisableConcurrentExecution` | Yêu cầu: `KE_HOACH_DU_AN.md:339` |
| Sitemap job lịch 02:00 UTC | ⛔ Không có `RecurringJob`/`Cron`; chỉ có endpoint on-demand `GET /recipes/sitemap` | `Program.cs:366-369` |
| No-secrets | ⛔ `render.yaml:18-19` **hardcode JWT signing key** dạng rõ | Vi phạm `KE_HOACH_DU_AN.md:332` |
| Resilience (Polly) | ⛔ Không có `AddStandardResilienceHandler`; retry hiện có: `WelcomeEmailWorker` 0/1/5/30 phút, Hangfire `[AutomaticRetry(3)]` | grep `WaitAndRetry` = 0 |

### 2.2 D6 — Lab cá nhân

| Hạng mục | Trạng thái thật |
|---|---|
| `practice/TV4/L4` | ✅ **Có source trên nhánh riêng `origin/practice/TV4/L4`** (15 file: `Lab.L4.csproj`, `Program.cs`, 4 phase `Media/Email/Sitemap/Jobs` + `DbEvidencePhase`, helper, `README.md`) — **không có trong `main`** (đúng quy ước LAB: không merge code trùng) |
| Kết quả L4 | 28/09: **4 phase · 39/39 check PASS** (`media` 25/25, `email` 3/3, `xml` 3/3, `jobs` 8/8) — sổ `Tuan03/SOK_LAB_L4.md` + log `Tuan03/logs/lab_l4_{run.log,db.txt}` ⚠️ **nằm trên nhánh `origin/practice/TV4/L4`, chưa có trong `main`** ⇒ tuần 4 cần mở PR để reviewer xem được (N3-4) |
| **Lab mục 2 (Identity/Google/refresh/forms/FTS)** | ⛔ **Chưa làm** — đây là phần `PHAN_CHIA` mục 3.4 và `KE_HOACH_DU_AN` mục 4.2 đã xếp vào **tuần 3** nhưng sổ tuần 3 tự ghi "để tuần 4" → **tuần 4 phải bù** |
| Lab L5 (UI/SEO/vận hành) | ⛔ Chưa có; `KE_HOACH_DU_AN.md` mục 9.1 cấp cho TV4 các ô LAB L1–L6 |
| Lab L6 (chất lượng) | ⛔ Chưa có; liên quan trực tiếp 2 gap: **không có ngưỡng coverage**, **không có hạ tầng test frontend** |

### 2.3 D7 — E2E / resilience / load / SEO / runbook / release

| Hạng mục | Trạng thái thật |
|---|---|
| **Playwright (5 luồng bắt buộc)** | ⛔ **Không có gì**: không `playwright.config.*`, không `*.spec.ts`, `@playwright/test` chỉ nằm trong `package-lock.json` dưới dạng optional peer |
| **Test frontend (Jest/RTL)** | ⛔ `src/frontend/package.json` **không có script `test`**, không có jest/vitest/@testing-library; **0 test file** trong toàn repo |
| **CI có build frontend?** | ⛔ **Không.** `.github/workflows/backend.yml` chỉ có 4 bước .NET (`restore --locked-mode`, `build`, `format --verify-no-changes`, `test`); **không có `setup-node`, `npm ci`, `next build`, `next lint`** |
| ⇒ Hệ quả đã chứng minh | ⛔ Lỗi `500` toàn bộ trang `/search` lọt vào `main` vì **không có cổng CI nào kiểm tra frontend** và route dynamic không bị `next build` prerender → chi tiết `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md` §4.2, §5 |
| k6 | 🟡 **Đã đo** (20 VU × 30s, 3310 req, 0% fail, p95 225.63 ms) nhưng **script chạy bằng heredoc inline, không commit** → không tái lập được | `Tuan03/logs/k6_smoke_recipes.log` (`script: /k6.js`) |
| File-size attack / MIME spoofing | 🟡 Chỉ có **unit test** validator (`ImageUploadValidatorTests` 6 facts + 2 theories) — **chưa có kịch bản E2E tấn công** | Yêu cầu `KE_HOACH_DU_AN.md` mục 8 (D7) |
| Runbook | ⛔ **Không có** file `*RUNBOOK*`; `docs/HUONG_DAN_TEST_APP.md` và `docs/evidence/TV4/HUONG_DAN_CHAY_TV4.md` mới dừng ở build + troubleshoot | Checklist `KE_HOACH_DU_AN.md:545` còn nguyên `- [ ]` |
| Coverage ≥ 80% | 🟡 **Có collect** (`--collect:"XPlat Code Coverage"`) nhưng **không có ngưỡng, không đọc số liệu, không báo cáo**; grep `Threshold` trong `*.csproj` = 0 | `CulinaryBlog.Tests.csproj:8` |
| SEO | ✅ sitemap/robots/OG/JSON-LD Published-only đã có; ⛔ thiếu **cron 02:00 UTC + distributed lock** (CR-7) | `Program.cs:366-369` |

---

## 3. Phân rã công việc tuần 4

> Thứ tự ưu tiên: **N0 → N1 → N2 → N3 → N4**. N2 (E2E) đặt trước N3 (lab) vì E2E dùng
> chính những kịch bản tấn công/resilience mà runbook tuần 5 sẽ trích dẫn lại; nhưng **N3 có
> deadline cứng** vì G4 là cổng nghiệm thu 24/24.

### N0 — Mở đầu tuần 4: chốt baseline + đưa bằng chứng lỗi 500 vào nhánh ⭐

**Skills**: K01, K24 · **Không phụ thuộc ai**

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | Tạo nhánh `2312739_NHTSon_D5-D6-D7` từ `origin/main` (`7fe8fc2`); tạo `docs/evidence/TV4/Tuan04/` | ✅ Xong 30/09 — nhánh có, folder có |
| 2 | **Đưa `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md`** (đang untracked) vào nhánh này | ✅ Xong 30/09 — commit trong nhánh tuần 4 |
| 3 | **Verify lại lỗi 500 đã được sửa** bằng **TC1–TC3** của chính báo cáo (`/search`, `/search?q=a`, `/search?q=gà`) và bằng **`next build`**; đối chiếu `e523579` (TV2 đã dùng **Phương án B**: tách `src/frontend/src/components/SearchFilterSelect.tsx`) | Log verify lưu `Tuan04/logs/search_500_verify.log`; nếu còn lỗi → báo TV2, **không sửa trực tiếp trên nhánh TV4** |
| 4 | Chạy lại baseline trên `7fe8fc2`: `dotnet build -c Release` (0 warning) → `dotnet format --verify-no-changes` → `dotnet test` (ghi đúng số test mới) | Số liệu tuần 4 ghi vào `SO_EVIDENCE_TUAN_4.md`, **không** dùng lại 172/172 của tuần 3 một cách mơ hồ |
| 5 | Ghi bảng mapping **K01**: FR/NFR ↔ ADR ↔ đường dẫn evidence cho 24 ô (tuần 3 ghi "Chưa làm") | Bảng mapping là thứ reviewer Tâm cần để nghiệm thu G4 |

### N1 — D5: Health thật, observability có bằng chứng, backup/restore, multi-instance

**Skills**: K07, K14, K20, K22, K23 · **ADR**: D22 (fallback vs readiness), D21 (uptime), D23 (queue)

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | **B4 — `ObjectStorageHealthCheck` xác thực credential** (bucket/`StatObject`), không chỉ TCP; giữ TCP probe làm check phụ | `GET /health` phân biệt được "cổng mở nhưng credential sai" (đúng bài học từ lỗi 500 tuần 3) |
| 2 | **Health failure test thật**: thêm test/kịch bản `/health/ready` → **503** khi Redis down; **thay** dòng "chấp nhận 200 hoặc 503" ở `HealthTests.cs:41` bằng kỳ vọng dứt khoát theo D22 | Test fail được khi hành vi sai ⇒ **không còn "xanh giả"** |
| 3 | **OTEL + Seq có nơi nhận**: thêm `otel-collector` vào compose, trỏ `AddOtlpExporter()` theo env, thêm `Serilog.Sinks.Seq` (ghi log + correlation id) | Chụp được **trace HTTP→DB thật** (1 trace request `/recipes` → SQL), lưu `Tuan04/logs/seq_trace_*.log` + ảnh chụp dashboard |
| 4 | **Backup/restore thật**: script `pg_dump` theo lịch **03:00** giữ **30 ngày** + backup volume file; **một drill restore** lên DB/file mới và đối chiếu số bảng/số object | `deploy/backup.sh` (+ `restore.sh`), `Tuan04/logs/backup_restore_drill.log`; ghi rõ **timezone backup là quyết định của nhóm**, không suy ra UTC |
| 5 | **2 API instance + shared state**: chuyển `RecipeCacheService` sang Redis dùng chung (stack đã có Redis 7 + AOF) hoặc **ghi ADR rõ giới hạn**; nhiều worker Hangfire dùng chung PostgreSQL queue | Dựng được 2 instance; **sitemap không sinh trùng** (xem N1-6); refresh token sống qua restart |
| 6 | **CR-7 — sitemap cron 02:00 UTC + distributed lock** (dùng `[DisableConcurrentExecution]` hoặc lock Redis) | Job chạy đúng lịch, 2 worker chạy song song vẫn **1 lần**; Published-only; retry 2 |
| 7 | **B1/B2** (`500` → `503 storage.unavailable`; fail-fast khi thiếu cấu hình) — **chỉ làm sau khi nhóm duyệt** | Nếu chưa duyệt: giữ nguyên, ghi rõ trong sổ là "chờ quyết định" |
| 8 | **No-secrets**: `render.yaml` bỏ JWT key hardcode → biến môi trường/secret store; thêm **secret scan** vào CI | `grep` không còn key trong repo; CI fail nếu phát hiện |

### N2 — D7: E2E, resilience, tấn công file, cổng CI frontend

**Skills**: K02, K03, K04, K06, K10, K16, K17, K18, K21, K24 · **Phụ thuộc**: N1-1 (health), N1-3 (observability)

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | **Dựng Playwright thật**: `playwright.config.ts` + **`npm i -D @playwright/test`** + script `test` trong `package.json` | Hạ tầng E2E có thật, không chỉ nằm trong tài liệu |
| 2 | **5 luồng bắt buộc** (register, login, create recipe, publish, search) — TV4 viết luồng **publish** và **search** (phần của mình), các luồng còn lại do TV1/TV3/TV2 viết; dùng **tài khoản seed**, không tạo dữ liệu thật | `search` E2E chính là bản tự động hoá **TC1–TC12** của báo cáo lỗi 500 → chặn tái phát |
| 3 | **Cổng CI frontend** (workflow mới hoặc bước mới): `npm ci` → `tsc --noEmit` → `next build` → `next lint` | Đây là **nguyên nhân gốc** lỗi 500 lọt; không có cổng này thì E2E cũng không được bảo vệ |
| 4 | **Kịch bản tấn công file** (E2E, không chỉ unit): file > 5 MiB, extension/MIME giả (đổi tên `.exe`→`.jpg`), file rỗng/hỏng, nhiều request upload liên tiêp | Từ chối đúng mã lỗi `FILE_SIZE_EXCEEDED` / `FILE_MIME_INVALID`; **không** để lọt 500 |
| 5 | **Resilience có số liệu**: dừng lại từng dependency (DB / Redis / S3 / worker) rồi đo: request lỗi trả gì, có retry không, phục hồi sau bao lâu; **tắt Redis** để chứng minh fallback (D22) | Bảng số liệu failover/retry trong `SO_EVIDENCE_TUAN_4.md` + log |
| 6 | **k6 script commit được** (thay heredoc inline) + chạy lại có số đo p50/p95/p99; **EXPLAIN** lại sau khi có thay đổi index | Script trong `tests/performance/`, lệnh tái lập được ghi trong runbook |
| 7 | **Coverage có ngưỡng**: đọc `coverage.cobertura.xml` trong CI, đặt ngưỡng **line ≥ 80% cho `CulinaryBlog.Application`**, báo cáo số liệu thật | Không còn trạng thái "collect rồi bỏ"; G5 có số để chấm |
| 8 | **D4-UI còn treo từ tuần 3**: thanh **progress % upload** + nút **Unpublish/Archive** (API đã có sẵn) | Đóng nợ D4; bằng chứng 320/768/1200 px + checklist WCAG (K18) |

### N3 — D6: Lab L5 + bù mục 2 của L4

**Skills**: K04, K05, K09, K15, K19 · **Điều kiện**: nhánh `practice/TV4/L5`, DB/schema/bucket prefix riêng

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | **Bù mục 2 L4** (Identity/Google/refresh/forms/FTS) — phần `PHAN_CHIA` xếp tuần 3, nay bù tuần 4 | Nếu thiếu Google credentials → ghi rõ "integration ngoài còn chờ", **không coi mock là hoàn thành** |
| 2 | **Lab L5** (`practice/TV4/L5`): SSR search + ISR published detail + CSR editor; TanStack Query optimistic/rollback; `next/image`; metadata/JSON-LD/robots/redirect; Serilog/OTEL/metrics/health; **2 API instance + shared cache/jobs** | 1 phase có check thật cho từng ý, **giống cách L4 chấm 39/39** để reviewer đối chiếu được |
| 3 | Sổ K cho lab: `Tuan04/SOK_LAB_L5.md` — kỹ thuật con nào có/không, giới hạn, lỗi gặp | Ô K chỉ được tính khi có code + test + log, **không tính khi chỉ đọc** |
| 4 | Mở **PR cho nhánh lab** (yêu cầu `PHAN_CHIA` mục 3.4 — hiện `practice/TV4/L4` mới chỉ có branch, chưa có PR) | Reviewer có đường vào để xem |

### N4 — D7: Runbook, release, bàn giao

**Skills**: K01, K23, K24

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | **Runbook** `docs/RUNBOOK.md`: dựng stack từ checkout sạch, backup/restore, failover từng dependency, 2 instance, lệnh k6, cách tìm log/trace của một request vừa thao tác | Có **số liệu thật** trong từng mục, không viết "nên làm" |
| 2 | **Deploy lặp lại được**: `docker-compose.prod.yml` (nếu chốt) hoặc checklist Render; chạy 2 lần từ sạch để chứng minh idempotent | Ghi rõ giới hạn hạ tầng thay vì ngụ ý đã đạt SLA (D21) |
| 3 | Cập nhật `docs/evidence/TV4/HUONG_DAN_CHAY_TV4.md` + `README.md` mục test/ops; `CHANGELOG.md` | Tài liệu khớp với trạng thái thật |
| 4 | Chốt `SO_EVIDENCE_TUAN_4.md` + `TRANG_THAI_THUC_HIEN_TUAN_4.md`, nộp review Tâm | 24/24 ô K có minh chứng; **ghi rõ ô nào còn thiếu** thay vì làm tròn số |

---

## 4. Phụ thuộc & bàn giao tuần 4

### TV4 cần nhận

| Từ | Nhận gì | Khi nào | Nếu chậm |
|---|---|---|---|
| **Nhóm trưởng (TV1)** | Duyệt **B1, B2, B4, B6** (B4 thì TV4 có thể tự làm vì đã có đề xuất) | Ngày 1 | Tự làm B4 (rủi ro thấp, có đề xuất sẵn); B1/B2 ghi "chờ quyết định" |
| **Nhóm trưởng (TV1)** | Tài khoản **Admin** để test `/hangfire`, `DevConfigParityTests`, health theo vai trò (**B6**) | Ngày 1–2 | Dùng seed có sẵn; ghi rõ hạn chờ |
| **TV2** | Xác nhận `/search` đã hết lỗi 500 (TC1–TC12) | Ngày 1 | TV4 tự verify bằng E2E rồi gửi log; **không sửa code TV2** |
| **TV3 (C4/C5)** | Fixture recipe Published + refresh token để E2E publish | Ngày 1–2 | Tự seed qua API trong `WebApplicationFactory` |
| **Cả nhóm** | Chốt **timezone lịch backup** (03:00 theo múi giờ nào) | Ngày 1 | Mặc định giờ máy chủ, **ghi rõ giới hạn** thay vì tự suy ra UTC |

### TV4 phải bàn giao

| Cho | Gì | Khi nào |
|---|---|---|
| Cả nhóm | **Health/observability đúng thực tế**: health check xác thực storage, `/health/ready` 503 khi Redis chết, trace HTTP→DB vào Seq/OTLP | Giữa tuần 4 |
| Cả nhóm | **Backup/restore + multi-instance** đã chạy thật, có log drill | Cuối tuần 4 |
| Cả nhóm | **Cổng CI frontend + E2E search/publish** (bịt lỗ hổng đã lộ ra bởi lỗi 500) | Giữa tuần 4 |
| TV1/TV2/TV3 | **Runbook** + k6 script commit + số đo p95/p99 | Cuối tuần 4 |
| Cả nhóm | **Sổ K 24/24** kèm đường dẫn/commit/test cho reviewer | Cuối tuần 4 |

---

## 5. Ma trận kỹ năng tuần 4 TV4 — 15 ô còn thiếu

> Cột "Ô đã có" = 9 ô đã được `PHAN_CHIA` ghi nhận (K01, K05, K08, K11, K12, K13, K20, K23, K24).
> Mỗi ô chỉ được chuyểi sang "có minh chứng" khi có **đường dẫn code/config + test + kết quả thật + reviewer Tâm xác nhận**.

| K | Kỹ thuật con cần chứng minh | Việc tuần 4 | Nơi sẽ chứng minh |
|---|---|---|---|
| K02 | Minimal APIs, REST/version, Scalar/RFC7807 | N1-1 (health check mới), N1-7 (B1 mã lỗi `storage.unavailable`), N2-4 (mã lỗi từ chối file) | `Health.cs`, `Program.cs`, test |
| K03 | Clean Architecture, interface, DI, value object | `ObjectStorageHealthCheck` cài đúng tầng; `IObjectStorageReader` dùng lại thay vì nhân bản | `Health.cs` + `ImageProxyD27Tests` |
| K04 | CQRS/MediatR + behavior tự viết (LAB) | N3-2: lab L5 tự viết behavior tối thiểu (logging/caching/invalidation) | `practice/TV4/L5` + `SOK_LAB_L5.md` |
| K05 | FluentValidation + sanitization + Zod/RHF | N2-8 (form status), N3-1 (form RHF/Zod trong lab) | form wizard + test |
| K06 | EF/PG16, migration, LINQ/projection/index | N1-6 (index phục vụ sitemap), N2-6 (EXPLAIN lại) | migration + `logs/explain_*` |
| K07 | UoW/transaction/audit/soft delete/RowVersion | N1-5 (2 instance không mất dữ liệu), race sitemap; LAB RowVersion/audit | spike/2-writer test |
| K09 | Google OAuth2/PKCE + verify | N3-1 bù mục 2 L4; nếu thiếu credentials → ghi rõ chờ, **không tính hoàn thành** | `practice/TV4/L4` / L5 |
| K10 | RBAC/ownership/policy/rate limit/secrets | N1-8 (bỏ secret khỏi `render.yaml` + secret scan), N2-4 (quyền upload) | CI + test |
| K14 | Hangfire: recurring, retry, persistence, dashboard | N1-6 (sitemap cron + lock), N1-5 (nhiều worker chung queue) | job log + `SOK_LAB_L5.md` |
| K15 | SMTP/MailKit, resize, sitemap XML | N1-4 (backup file), N1-6 (sitemap XML sinh đúng lịch) | log + XML mẫu |
| K16 | Next.js App Router, SSR/ISR/CSR | N2-1/2/3 (cổng CI build FE), N3-2 | `next build` log |
| K17 | TanStack Query, optimistic, next/image | N2-8 (progress upload), N3-2 | UI + test |
| K18 | Responsive, WCAG2.1 AA, keyboard/loading/error | N2-8 (checklist 320/768/1200 px, focus, aria) | ảnh + checklist |
| K19 | SEO metadata/OG/canonical/robots/JSON-LD/301 | N1-6 (sitemap lịch), N3-2 (lab SEO đủ) | `sitemap.xml` + log |
| K21 | xUnit/API, Jest/RTL, **Playwright** | N2-1/2 (Playwright thật), N2-7 (coverage ngưỡng) | `.github/workflows/`, test report |
| K22 | k6 p50/p95/p99, EXPLAIN, CWV | N2-6 (script commit + đo lại), N2-5 (số đo resilience) | `tests/performance/`, logs |

**Điểm nhấn**: K16/K17/K18/K21 là 4 ô đòi hỏi **hạ tầng test frontend** mà hiện **không tồn tại**
(N2-1, N2-3) và K22 đòi **script k6 commit được** (N2-6). Đây là khối lượng lớn nhất và phải làm
song song với N1, không dồn cuối tuần.

---

## 6. Checklist cổng tuần 4

### G4 — 24/24 ô kỹ năng (giữa tuần)

- [ ] Bảng mapping K01 (FR/NFR ↔ ADR ↔ evidence) hoàn chỉnh 24 dòng
- [ ] 15 ô K còn thiếu có tối thiểu code/config + test + kết quả thật
- [ ] N2-1: `playwright.config.ts` + `@playwright/test` đã cài, chạy được ≥ 1 luồng
- [ ] N3: `practice/TV4/L5` có ít nhất 1 phase chấm điểm kiểu L4 (check count rõ ràng)

### G5 — chất lượng & an toàn (cuối tuần)

- [ ] `/health/ready` trả **503** khi Redis chết, có test chứng minh (thay dòng "200 hoặc 503")
- [ ] Health check storage **xác thực credential**, không chỉ TCP
- [ ] **Trace HTTP→DB thật** trong Seq/OTLP + file bằng chứng
- [ ] **Backup 03:00 giữ 30 ngày** chạy thật + **drill restore** dữ liệu + file, có log
- [ ] **2 API instance** dùng chung cache/queue; sitemap **không sinh trùng**
- [ ] Sitemap chạy theo **cron 02:00 UTC** + distributed lock
- [ ] **5 luồng E2E Playwright** pass (search + publish của TV4)
- [ ] **Cổng CI frontend** (`npm ci` → `tsc` → `next build` → `lint`) chạy xanh
- [ ] **Kịch bản tấn công file** (size/MIME spoofing/hỏng) bị chặn đúng mã lỗi
- [ ] **Coverage `CulinaryBlog.Application` ≥ 80%** có số đo + ngưỡng trong CI
- [ ] **k6 script commit** được, tái lập được; có p50/p95/p99
- [ ] **Runbook** có số liệu thật, không mục "nên làm"
- [ ] `render.yaml` không còn secret hardcode; CI có secret scan
- [ ] Baseline `7fe8fc2`: build 0 warning · `dotnet format` sạch · `dotnet test` xanh (ghi đúng số)
- [ ] Không commit secret; log bằng chứng đã redact

---

## 7. Trở ngại dự kiến & cách xử lý

| Rủi ro | Ảnh hưởng | Cách xử lý |
|---|---|---|
| **Không có hạ tầng test frontend nào** (0 jest, 0 playwright, CI không build FE) | K16/K17/K18/K21 không thể đạt; lỗi 500 kiểu cũ tái phát | N2-1 + N2-3 làm **trước** phần E2E khác; ưu tiên luồng `search` (tái hiện đúng lỗi đã gặp) |
| **Không có `pg_dump`/backup** | Mất dữ liệu khi demo; không đạt NFR-REL-003 | N1-4: script tối giảu chạy được trên máy Windows lẫn container trước khi lịch 03:00 |
| **Multi-instance** mới dừng ở thiết kế | NFR-SCALE-001 chưa có bằng chứng | Chọn phạm vi nhỏ nhất thật được: 2 API instance + chung PostgreSQL/Redis; ghi rõ hạn chế (không claim MinIO multi-node) |
| **Cache hiện là in-process** nên 2 instance sẽ lệch dữ liệu | Số đo p95/p99 không đáng tin khi scale | Hoặc chuyển Redis, hoặc **ADR ghi rõ giới hạn** và chỉ đo 1 instance. Không đo rồi báo như 2 instance |
| **`render.yaml` đang hardcode JWT key** | Vi phạm "no secrets", rủi ro rò khoá | N1-8 sửa + secret scan; **không** tự rotate key đang dùng |
| **Thiếu tài khoản Admin** (B6) | Không test được `/hangfire`, dashboard, health theo vai trò | Dùng seed; ghi "chờ TV1" trong sổ thay vì tự tạo Admin ngoài seed |
| **Mất 4 commit mới trên `main`** khi đang làm nhánh dài | Conflict `Program.cs`, search, seeder, compose | Mỗi task nhỏ 1 commit + rebase `origin/main` **trước khi mở PR** |
| **Kỹ năng 15 ô trong 1 tuần** | Dồn cuối tuần không kịp | N2 (E2E/CI) và N3 (lab) làm **song song**; mỗi ô K chỉ tính khi có bằng chứng — thiếu thì ghi thiếu, **không** đánh dấu cho đủ |
| **`ImageSharp` phải giữ 3.1.x** | Nâng version là build fail (license thương mại 4.x) | Giữ `3.1.11`; AVIF không decode → fallback original (đã xử lý ở tuần 3) |
| **Bốn file `Lab 04` trong `docs/evidence/TV4/` không phải của TV4** | Dễ bị hiểu nhầm là minh chứng cá nhân | Ghi chú ở đầu tài liệu này + `TRANG_THAI_THUC_HIEN_TUAN_4.md`; **không** sửa/xoá file của người khác |

---

## 8. Việc làm ngay

1. ✅ Tạo nhánh `2312739_NHTSon_D5-D6-D7` từ `origin/main` (`7fe8fc2`).
2. ✅ Tạo `docs/evidence/TV4/Tuan04/` và bộ 4 tài liệu (kế hoạch, mô tả, sổ evidence, trạng thái).
3. ✅ Commit `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md` vào nhánh này.
4. Chạy lại baseline (`build` → `format` → `test`) trên `7fe8fc2` và ghi số liệu thật.
5. Verify lỗi 500 `/search` đã hết (TC1–TC3) — nếu hết, đóng báo cáo ở mục N0-3; nếu còn, báo TV2.
6. Dựng **Playwright + cổng CI frontend** (N2-1, N2-3) — việc có giá trị cao nhất và đang chặn 4 ô K.
7. B4 `ObjectStorageHealthCheck` + test `/health/ready` 503 khi Redis chết (N1-1, N1-2).
8. Backup/restore script + drill (N1-4).
9. OTEL collector + Serilog→Seq + chụp trace thật (N1-3).
10. Sitemap cron 02:00 UTC + distributed lock (N1-6).
11. Lab `practice/TV4/L5` + bù mục 2 L4 (N3) · mở PR cho nhánh lab.
12. Chốt sổ 24/24 K + nộp review Tâm.

---

## 9. Tài liệu liên quan

| Tài liệu | Vai trò |
|---|---|
| `docs/KE_HOACH_DU_AN.md` mục 8 (task D1–D7), mục 9 (K01–K24), mục 4.2 (lịch 6 tuần) | Nguồn chuẩn cho task và nghiệm thu |
| `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md` mục 3.4 (TV4 tuần 4), mục 6 (mốc G4/G5) | Giao việc và cổng nghiệm thu tuần 4 |
| `docs/evidence/TV4/Tuan03/TRANG_THAI_THUC_HIEN_TUAN_3.md` | Bối cảnh tuần 3, gap kế thừa, sự cố CI và upload 500 |
| `docs/evidence/TV4/Tuan03/KE_HOACH_TUAN_3_TV4.md` | Kế hoạch tuần 3 (D3–D7) để đối chiếu phần nào đã làm |
| `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md` | Báo cáo lỗi 500 trang `/search` do TV4 phát hiện (đã commit ở nhánh này) |
| `docs/report/BAO_CAO_LOI_UPLOAD_ANH_500.md` + `..._DA_SUA.md` | Bài học cấu hình → cơ sở cho B1/B2/B4 |
| `docs/proposal/DE_XUAT_01..06` | 6 đề xuất B1–B6 đang chờ quyết định nhóm |
| `docs/IMAGE_CONTRACT.md`, `docs/adr/ADR-TV4-001`, `ADR-TV4-002` | Contract media + ADR vận hành đã chốt |
| `docs/evidence/TV4/HUONG_DAN_CHAY_TV4.md` | Hướng dẫn chạy/troubleshoot do chính TV4 viết |
| `docs/root/SRS_Culinary_Blog_v1.1.1.md` | SRS chuẩn (FR/NFR/CONS) |
