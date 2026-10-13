# Changelog

## 0.4.1 — 2026-10-05 (Giai đoạn 3 sửa lỗi + đóng N3 — TV4)

Nhánh `2312739_NHTSon_D5-D6-D7`.

### Sửa lỗi thật

- **`docker-compose.staging.yml` hardcode khoá JWT đã thu hồi.** File do TV1 thêm trong commit
  `21aa722` đặt cứng đúng khoá mà `JwtSettings.Validate()` đã cấm trong codebase — nghĩa là JWT của
  staging được ký bằng khoá nằm công khai trong git. Test
  `JwtSigningKeyNotCommittedTests.Revoked_key_appears_only_in_the_blocklist` đỏ **1/316**.
  Sửa: thay bằng `${JWT_SIGNING_KEY:?...}` **không có giá trị mặc định**, để thiếu biến thì
  `docker compose config` exit 1 kèm thông báo. Kiểm chứng cả hai chiều; test **6/6** xanh.

### Kiểm chứng báo cáo của thành viên khác

- **Báo cáo "Tuần 5" của TV1 (`21aa722`) đánh dấu KHÔNG ĐÁNG TIN.** 4 sai lệch đã xác nhận:
  1. **Số liệu p95 vi phạm bất biến toán học** — cả 3 endpoint đều ghi p95 < mean
     (7ms < 20.95ms; 6ms < 9.61ms; 2ms < 2.12ms); p95 không bao giờ nhỏ hơn mean.
  2. **Nhãn tuần sai** — repo đã có `TUAN_4.md` của chính TV1 ghi task A6/A7 với 177/177 test;
     commit này ghi lại đúng task A7 nhưng đổi thành "Tuần 5" và 183/183.
  3. **Số test không khớp** — báo cáo nói 183; thực tế sau khi merge là 321 (316 + 5).
  4. **`render.yaml` ghi là đầu ra nhưng không tồn tại trong commit.**
- **Thiếu minh chứng chạy**: tuyên bố RTO < 2 phút / RPO = 0 cho BCP/DR nhưng không lưu log
  diễn tập, không có dump; tuyên bố hiệu năng có script k6 nhưng không có output (đo lại bằng `ab`,
  công cụ không xuất p95 đáng tin).
- **Vai trò**: cả 5 bản ghi đều tự xác nhận — người xác nhận là chính người thực hiện.
- **Công nhận**: mục "Bản ghi 5" liệt kê đề xuất B1 (`500` → `503 storage.unavailable`) thuộc TV4,
  đã được TV4 triển khai trước.
- Hồ sơ: [`KiemChung_Commit_Week5_TV1.md`](docs/evidence/TV4/Tuan04/misc/KiemChung_Commit_Week5_TV1.md).
  **Không sửa báo cáo của TV1** — giữ nguyên bản ghi gốc để TV1 tự đính chính.

### Đóng N3-B / N3-C1 / N3-C2

- **Lab L5** (`practice/TV4/L5`, commit `6c90ad8`) — 7 phase chạy thật: **63 check, 41 đạt,
  3/7 phase PASS** (`seo` 15/15, `observability` 10/10, `multi-instance` 7/7).
- **4 phase lộ ra vấn đề thật**: `isr-detail` — code khai `revalidate = 300` nhưng runtime không có
  `x-nextjs-cache`, ISR không hoạt động; `image-opt` — `next/image` 0 file, có 9 file `<img>` thô,
  không có biến thể medium/large; `search-ssr` — SSR thật nhưng trả `no-store`; `query-rollback` —
  optimistic rollback có thật nhưng RowVersion chưa kiểm chứng được.
- Sổ K + 2 log (`SOK_LAB_L5.md`, `logs/lab_l5_run.log`, `logs/lab_l5_db.txt`) — commit `870d6e3`.
- PR #28 đã mở rồi **đóng** theo quyết định nhóm: lab ở nhánh riêng, PR chỉ để đánh dấu/review.

### Sửa

- `.gitignore` — ghép sau xung đột khi merge `main`: giữ cả artifact Playwright + `run.log`
  (nhánh tuần) lẫn `backups/**/*.sql*` (từ `21aa722`).

### Đồng bộ tài liệu tuần 4 (05/10, sau `d4edfa2`)

Rà toàn bộ tài liệu trong `docs/evidence/TV4/Tuan04/` để loại mâu thuẫn trạng thái:

- **N2 không còn được ghi không điều kiện là "đã đóng".** Số thực tế: **7/8 việc xong**, dở dang
  `D1/D2/D3` (progress upload, UI unpublish/archive, checklist WCAG). Sửa ở
  `TRANG_THAI_THUC_HIEN_TUAN_4.md`, `SO_EVIDENCE_TUAN_4.md` và báo cáo GĐ1.
- **Sửa lỗi báo cáo GĐ3 về danh sách lỗi.** Trước đó ghi "BUG-W4-01/02/03 đã sửa và kiểm chứng ở
  GĐ1 — không sửa lần nữa". Đã kiểm chứng lại 05/10: `RecipeImageConfiguration.cs` **vẫn thiếu**
  `ValueGeneratedNever()` và 3 bản vá **chưa có trong dự án** (chỉ TV4 kiểm tra trên bản vá cục bộ,
  không push) ⇒ 3 lỗi này **còn mở**. Ba lỗi đã đóng ở GĐ1 là lỗi **khác** (DB chết trả `500`, DB chết lúc
  khởi động, storage `500`) và đã merge ở `8d9d62b`. Bổ sung bảng phân biệt ở GĐ3, GĐ1,
  `BAO_CAO_LOI_TUAN_4_TV4.md` và `BAO_CAO_LAB_TUAN4_V2.md`.
  → *Cập nhật 07/10: `BUG-W4-01` đã vào `main` (PR #29, `c624b9f`); `BUG-W4-02/03` chưa có bản vá nào
  trong repo và được đưa vào **sửa trực tiếp trong tuần 5** (`KE_HOACH_TUAN_5_TV4.md` W5-10).*
- **Bổ sung 2 lỗi bị thiếu** trong danh sách lỗi còn mở của GĐ3: `BUG-W4-08` (chưa kiểm tra sâu)
  và TLS chưa bắt của `BUG-W4-07`.
- **Thêm banner "ảnh chụp lịch sử"** cho các tài liệu kế hoạch để dòng "chưa làm" trong đó không bị
  hiểu là trạng thái hiện tại: `KE_HOACH_TUAN_4_TV4.md`, `KE_HOACH_TUAN_4_TV4_V2.md`,
  `MO_TA_CONG_VIEC_TUAN_4.md`, `HANDOFF_TV4_TUAN4_N1.md`, `PLAN_GIAI_DOAN_1_N2_N4.md`.
- `PLAN_TRIEN_KHAI_TV4_TUAN4.md`: đổi trạng thái từ "chưa thực thi" sang **GĐ1/GĐ3 đã thực thi,
  GĐ2 còn mở**.
- `README.md`: số test sau khi merge `main` là **321/321 (316 + 5)**, không phải 316 — 5 test tuần 5
  của TV1 được merge vào. Giữ lại số đo 04/10 (`8d9d62b`) như bản ghi lịch sử.

### Số đo sau thay đổi

- `dotnet build -c Release`: **0 warning, 0 error**.
- `dotnet test`: **316/316** `CulinaryBlog.Tests` + **5/5** `ConcurrencySpike` (trước khi sửa: 1 đỏ).
- `docker compose -f docker-compose.staging.yml config`: exit 0 khi có `JWT_SIGNING_KEY`, exit 1 khi thiếu.
- Ô kỹ năng còn thiếu thật: **3 → 2** (K09 chờ credentials, K18 thuộc TV2).
- ✅ **CI xanh trở lại ở `8a585ef` (05/10)**: `Backend week 1` run `37320431750` ✅ + `Frontend CI` run `37320431432` ✅.
  Đã xử lý trọn vẹn 3 nguyên nhân làm backend đỏ:
  - `03564c4`/`d4edfa2`/`0dfac4e` đỏ ở `JwtSigningKeyNotCommittedTests.Revoked_key_appears_only_in_the_blocklist`:
    bản sửa ở `docker-compose.staging.yml` đúng, nhưng **hai hồ sơ `.md` của TV4 lại chép nguyên khoá đã thu hồi**,
    mà test quét cả `.md` ⇒ chính tài liệu phá bất biến mà test bảo vệ. Đã gỡ khỏi
    `BAO_CAO_GIAI_DOAN_3_SUA_LOI.md` và `KiemChung_Commit_Week5_TV1.md`; test local `6/6` xanh.
  - `5593c23` đỏ ở `Week5StagingAndE2ETests.E2E_Scenario_5_FTS_Vietnamese_Search_AND_Filter_And_Draft_Isolation`:
    `ApiFactory.EnsureMigrated()` chỉ gọi `Database.Migrate()`, **không seed**, và `Program.cs:343` bỏ qua
    `DbSeeder` khi `Environment=Testing`. Test của TV1 kỳ vọng có sẵn "Canh chua cá lóc" nên đỏ trên Postgres
    sạch của CI. Đã thêm `DbSeeder.SeedAsync` (idempotent) vào `EnsureMigrated()`.
  - `870d6e3` đỏ ở `TracingObservabilityTests...child_db_span` (`Assert.NotNull()`): race thật — test đọc
    `activities` ngay sau `GetAsync`, nhưng `ActivityStopped` của span HTTP chạy trên thread Kestrel **sau** khi
    response đã trả. Đã thay bằng vòng chờ 10s theo đúng mẫu polling mà test Redis đang dùng.

---

## 0.4.0 — 2026-10-04 (Giai đoạn 1 — TV4, tuần 4)

Nhánh `2312739_NHTSon_D5-D6-D7`. Số đo đều lấy từ lần chạy thật, có lệnh và log trong
`docs/evidence/TV4/Tuan04/`.

### Sửa lỗi thật (kèm test hồi quy)

- **`GET /api/v1/recipes/{slug}` trả `500` khi thiếu credential object storage.** `MinioClient.Build()`
  ném `MinioException` khi credential rỗng mà `MinioStorageService` gọi nó ngay trong constructor;
  B5 khiến mọi lần đọc công thức đều dựng service này → hỏng cả endpoint không liên quan tới ảnh.
  Sửa bằng `Lazy<IMinioClient>`: `Build()` chạy lần đầu khi thật sự gọi storage, nên lỗi ra
  `503 storage.unavailable`. **Lỗi do CI bắt, không phải do test local** (máy dev có `Minio__*` trong
  `.env`, CI không có). Khoá bằng 2 test hồi quy; kiểm chứng bằng cách ép `Build()` ở constructor thì
  2 test đỏ.
- **DB không truy cập được làm 3 endpoint đọc trả `500`.** `EfUnitOfWork` chạy lệnh qua execution
  strategy của EF nên `NpgsqlException` gốc bị bọc thành `InvalidOperationException`, còn
  `ApiExceptionHandler` chỉ kiểm tra exception ngoài cùng. Sửa: dò cả chuỗi `InnerException`, map
  `Npgsql`/socket/timeout → `503 database.unavailable`, và đặt nhánh này **trước** nhánh 422 để
  `DbUpdateException` do mất kết nối không bị báo nhầm "dữ liệu đã thay đổi".
- **DB chết lúc khởi động giết cả tiến trình** vì `AddOrUpdate` của lịch sitemap ném `NpgsqlException`
  ra khỏi `Main`. Sửa: bọc try/catch + log; `/health/ready` vẫn 503 nên orchestrator restart pod khi
  DB trở lại và lịch được đăng ký lại.
- **Wizard mất bước sau lần lưu draft đầu tiên.** Lưu xong không chuyển bước, reload thì về bước 1.
  Sửa: truyền `?step=` khi lưu và đọc lại `step` từ URL khi mount (`RecipeWizard.tsx`,
  `EditRecipeClient.tsx`), kèm `settleStep()` trong E2E.

### Thêm

- Resilience N2-C1/C1b/C1c: sitemap retry **2**, resize retry **3**, xoá ảnh retry **3**, retry
  idempotent khi xoá file không tồn tại; cache dùng chung giữa nhiều instance qua Redis; sitemap theo
  lịch **02:00 UTC** với distributed lock.
- `tests/performance/read-load.js` (k6) + `README.md` — 3 log chuẩn, `http_req_failed` 0.00%.
- `deploy/outage-drill.ps1` — kịch bản dừng Redis/S3/DB/worker, đo thời gian phục hồi.
- Playwright E2E luồng publish: đăng ký → đăng nhập → wizard 5 bước → publish → tra cứu tìm kiếm.
- `ImageMagicBytesE6Tests`, `ImageConcurrencyE7Tests`, `BackgroundJobRetryContractTests`,
  `TracingObservabilityTests`.

### Số đo

- Backend **316/316** (311 + 5); coverage `CulinaryBlog.Application` **84.13%** ≥ 80%;
  `dotnet format --verify-no-changes` exit 0; build 0 warning / 0 error.
- Playwright **26/26**, 3 lần liên tiếp; `tsc`/`lint`/`build` exit 0.
- Cả hai job CI xanh trên commit `8d9d62b` (`Backend week 1` run `37213966752`, `Frontend CI` run
  `37213966761`).

### Chưa làm (chuyển tuần 5)

- N2-D3 checklist WCAG/responsive (thuộc TV2); 3 luồng E2E `register/login` (TV1), `category` (TV2),
  `create-recipe` (TV3).
- N4-A runbook đầy đủ và N4-B deploy staging + TLS/HSTS (cần chứng thư).
- Lab L5 và Google OAuth2/PKCE (thiếu credentials — không coi mock là hoàn thành).

## 0.3.0 — 2026-09-30 (TV4, tuần 3 — đã merge qua PR #16)

- D23 Hangfire queue persistent + dashboard Admin; D27 media proxy công khai / Published công khai;
  D2 resize 300×300/800×600 idempotent; D4 sitemap/robots/OG/JSON-LD; D5 EXPLAIN + k6.
- D6 Lab L4 (`practice/TV4/L4`): 4 phase, **39/39 check PASS**.
- PR #19: `503 storage.unavailable` + validate lúc khởi động; `DevConfigParityTests`; gỡ secret khỏi
  `render.yaml`.

## 0.1.0 — 2026-09-09

- TV1 tuần 1: .NET 10 Clean Architecture, Identity/PostgreSQL, Author register/login, JWT và ICurrentUser.
- FluentValidation/MediatR, Problem Details, correlation logging và Scalar.
- Migration auth, PostgreSQL integration tests, architecture lab, CI và auth handoff docs.
- Chưa tích hợp sản phẩm nhóm đầy đủ; xem evidence về kết quả thực tế.
