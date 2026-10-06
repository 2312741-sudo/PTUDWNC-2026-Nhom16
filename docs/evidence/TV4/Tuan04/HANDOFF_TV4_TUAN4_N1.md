# HANDOFF TV4 — sau N1 (tuần 4)

- **TV4**: Nguyễn Hữu Trung Sơn (2312739) · **Reviewer**: Nguyễn Thanh Tâm
- **Nhánh**: `2312739_NHTSon_D5-D6-D7` · **SRS**: v1.1.1
- **Ngày bàn giao**: 30/09/2026
- **Phạm vi bàn giao**: N0 (xong) + N1 (xong 8/8 việc kỹ thuật). N2/N3/N4 chưa làm.

> [!NOTE]
> **📌 Ảnh chụp lúc bàn giao 30/09 — phần "N2/N3/N4 chưa làm" đã cũ.**
>
> Cập nhật 05/10: **N2 7/8 xong** (dở dang `D1/D2/D3` — progress upload, UI unpublish/archive,
> WCAG), **N3-B/C1/C2 xong** (Lab L5 63 check, 3/7 phase PASS), **N4-C xong**; còn `N3-A3`
> (Zod/RHF) và `N3-A5` (Google OAuth — chờ credentials).
> Xem [`TRANG_THAI_THUC_HIEN_TUAN_4.md`](TRANG_THAI_THUC_HIEN_TUAN_4.md) và
> [`BAO_CAO_GIAI_DOAN_1_N2_N4.md`](report/BAO_CAO_GIAI_DOAN_1_N2_N4.md).

---

## 1. Trạng thái kiểm định lúc bàn giao

| Hạng mục | Kết quả |
|---|---|
| `dotnet build -c Release` | ✅ 0 warning / 0 error |
| `dotnet test -c Release` (toàn bộ) | ✅ **210/210 pass** (205 `CulinaryBlog.Tests` + 5 `ConcurrencySpike`), Failed 0, Skipped 0 |
| `dotnet format --verify-no-changes` | ✅ exit 0 (CI cũng chạy bước này) |
| `npx tsc --noEmit` (frontend) | ✅ exit 0 |
| `npm run build` (frontend) | ✅ exit 0, 17 route build xong |
| Secret scan (`deploy/scan-secrets.sh`) | ✅ pass; đã thử cố ý chèn JWT hardcode → bị bắt, exit 1 |
| CI GitHub Actions | ✅ cả `Backend week 1` và `Frontend CI` xanh trên commit `ee5e78b` |

Đã merge `origin/main` (`3d0695d`, TV2 discovery lab) trước khi push, không có conflict. Số test
tăng từ 205 lên 210 vì TV2 thêm `Week4DiscoveryAndPerformanceLabTests`.

Lưu ý: các test E2E storage/Redis/DB chạy thật với service local. Nếu máy khác không bật
`docker compose -f docker-compose.dev.yml up -d postgres redis s3 mailhog seq otel-collector` thì các
test phụ thuộc dịch vụ sẽ thoát sớm (guard) chứ không fail — đây là hành vi có chủ đích của bộ test hiện tại.

## 2. Những gì đã giao (theo mã việc)

| Mã | Nội dung | File chính | Bằng chứng |
|---|---|---|---|
| N1-1 | Health probe kiểm tra credential object storage thật (không chỉ TCP) | `Health.cs`, `ObjectStorageCredentialProbe.cs` | `HealthTests` 18/18 |
| N1-2 | Bằng chứng quyết định: Redis chết → `/health/ready` 503; credential S3 sai → 503; phụ thuộc khoẻ → 200 | `HealthTests.cs` | 18/18 pass |
| N1-3 | OTEL collector + Seq: trace HTTP→DB thật, log Serilog cùng TraceId | `Program.cs`, `docker-compose.dev.yml`, `deploy/otel-collector-config.yaml` | `logs/seq_trace_recipes.log`, `TracingObservabilityTests` 2/2 |
| N1-4 | `pg_dump`/`pg_restore` + drill thật, chống ghi đè DB có dữ liệu | `deploy/backup.sh`, `deploy/restore.sh` | drill 14 bảng, guard exit 1 |
| N1-5 | Cache dùng chung giữa nhiều instance qua Redis thật | `RecipeCacheService.cs`, `RedisOptions.cs` | `RedisSharedCacheTests` 6/6 |
| N1-6 | Sitemap theo lịch + khoá phân tán chống sinh trùng | `SitemapGenerator.cs`, `Program.cs` | `SitemapLockTests` 3/3, `/sitemap.xml` 200 |
| N1-7 | B1 `503 storage.unavailable` + thông điệp tiếng Việt, không retry; B2 validate MinIO lúc khởi động | `MinioStorageService.cs`, `MinioOptions.cs`, `recipe-editor.ts` | `StorageFailureContractTests` |
| N1-8 | Bỏ secret khỏi `render.yaml` + secret scan trong CI | `render.yaml`, `deploy/scan-secrets.sh`, `backend.yml` | pass + bắt lỗi khi thử |

## 3. Việc reviewer cần xác nhận

1. **Mapping K01–K24** (`../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md`) — 8 ô có bằng chứng mới trong N1, đang
   chờ duyệt. Chưa ô nào được tự đánh dấu "đạt".
2. **B1** (`#20`): chọn `503 storage.unavailable` + không retry tự động — có đúng ý không?
3. **B2** (`#21`): validate credential MinIO lúc khởi động, bỏ qua môi trường `Testing` — có đúng ý không?
4. **B4** (`#22`): `/health/ready` gồm cả probe credential storage, còn TCP probe chỉ ở `/health`.
5. **Lịch backup 03:00 Asia/Ho_Chi_Minh** (`= 20:00 UTC hôm trước`): đồng ý đặt ở cron host/CI thay vì
   trong repo không?

## 4. Hạn chế đã biết — chưa được tính là đạt

> ⭐ **Cập nhật 03/10 (sau N1):** các hạn chế **2, 3, 7** dưới đây giờ đã có **đề xuất riêng** và **một
> quyết định đã chốt** — xem [`QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md`](misc/QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md)
> và `docs/proposal/DE_XUAT_07/08/09`. **TV4 không tự quyết** vì cả ba nằm ngoài hạ tầng dự án.

1. **Hai tiến trình API thật đã kiểm chứng** (nginx round-robin, 5/5 mỗi instance, cache dùng chung
   qua Redis, fallback khi Redis chết) — nhưng mới chạy thủ công trên máy, **chưa có profile
   docker-compose sẵn dùng** cho cả nhóm, và chưa đo trên Render. Xem `logs/multi_instance_two_api.log`.
2. **Lịch backup 03:00 Asia/Ho_Chi_Minh đã có trong repo** (`.github/workflows/backup.yml`,
   cron `0 20 * * *` UTC) nhưng **cần secret `DATABASE_URL`** trong repository settings, nếu thiếu
   workflow sẽ đỏ với thông báo rõ ràng. Lưu ý vận hành: GitHub **có thể trễ hoặc bỏ qua** job theo
   lịch khi repo lâu không có commit, nên cron trên Render/host là lựa chọn đáng tin hơn cho mục tiêu
   99% có backup. Cần reviewer chốt nơi đặt lịch chính.
   → **[`DE_XUAT_07_NOI_DAT_LICH_BACKUP.md`](../../../proposal/DE_XUAT_07_NOI_DAT_LICH_BACKUP.md)** — 3 phương
   án, khuyến nghị **A: giữ GitHub Actions + thêm job canh** (chi phí 0, phát hiện hỏng trong ≤ 24h).
3. **Bản sao lưu trên GitHub chỉ giữ 7 ngày (artifact)**. Yêu cầu giữ **30 ngày** chỉ thoả khi
   chạy script trên host có ổ đĩa riêng. Muốn đúng nghị quyết thì phải đẩy sang kho riêng
   (RustFS/S3/NAS).
   → **[`DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md`](../../../proposal/DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md)** —
   khuyến nghị **A: bucket RustFS/S3 riêng + lifecycle 30 ngày**. ⚠️ `BACKUP_KEEP_DAYS=30` trong
   `backup.yml` **chỉ dọn trên runner**, ⛔ **không** phải giữ 30 ngày thật.
4. **Google OAuth (K09) còn chờ credentials** — không tính hoàn thành.
5. **Chưa có k6 / Playwright / Lighthouse** (K21, K22) — thuộc N2.
6. **Bẫy CI đã vấp: `redis-cli` không có trên ubuntu runner.** Bước "chờ Redis sẵn sàng" dùng
   `redis-cli` làm job backend đỏ trong khi toàn bộ test đều xanh — `redis-cli` chỉ nằm bên trong
   service container. Đã đổi sang `/dev/tcp` của bash. Khi thêm bước chờ dịch vụ mới, nhớ runner
   GitHub có rất ít binary; cứ dùng `bash /dev/tcp` hoặc `docker exec` vào service container.
7. **Secret cũ còn trong git history.** Đã bỏ khỏi `render.yaml` nhưng **phải rotate khoá JWT thật**;
   việc này cần làm ngoài repo.
   → **[`DE_XUAT_09_ROTATE_KHOA_JWT_DA_LO.md`](../../../proposal/DE_XUAT_09_ROTATE_KHOA_JWT_DA_LO.md)** —
   khuyến nghị **rotate trước, xoá history sau** (hoặc **không** xoá history). ⛔ TV4 **không** tự
   rotate khoá đang chạy và **không** xoá history; chỉ làm được phần trong repo (3a/3b/3c).
8. **Bucket `culinary-blog` phải tồn tại trước khi app báo khoẻ.** `HealthTests` tự bootstrap bucket qua
   `IObjectStorageWriter`, nhưng ứng dụng thật thì không — production cần bucket có sẵn hoặc bước khởi tạo
   khi deploy (ADR D27 nói bucket private).

## 5. Bẫy đã vấp — để người sau không lặp lại

1. `RecurringJob.AddOrUpdate` (static API) gọi lúc đăng ký DI sẽ làm **app không khởi động** ở môi trường
   thật (`Current JobStorage instance has not been initialized yet`). Bộ test không bắt được vì
   `Testing` không bật Hangfire. Phải đăng lịch sau `builder.Build()` qua `IRecurringJobManager`.
2. Sink Serilog phải nằm trong `LoggerConfiguration` của `UseSerilog`. Gán riêng vào `Log.Logger` ở dòng
   sau sẽ bị `UseSerilog` thay thế và Seq **không nhận event nào**.
3. Máy này có **native PostgreSQL 18 và container PostgreSQL cùng nhận cổng 5432**. `localhost:5432` trỏ
   tới native; database có dữ liệu thật nằm ở native, không nằm trong container. `pg_dump`/`pg_restore`
   phải dùng client tại `E:\PostgreSQL\bin`.
4. `docs/evidence/TV4/Report/` là **do chính TV4 tạo** (tự rà soát nội dung commit + báo cáo của
   cả 4 thành viên) và chỉ dành cho TV4 đọc đối chiếu — **không** stage, không commit, không sửa,
   không dùng làm minh chứng hay căn cứ chấm điểm cho bất kỳ ai.
   *(Đính chính 03/10: dòng này trước đây ghi nhầm "là việc của TV1". Tác giả đúng là TV4.)*
5. `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md` phải giữ nguyên blob
   `93661aa13e6fa2e081b9a89b2517eca5d0df083c`.
6. File backup `*.dump` đã được thêm vào `.gitignore` vì dump chứa dữ liệu người dùng — không được commit.

## 6. Bước tiếp theo đề xuất

> **Cập nhật 03/10:** các bước dưới đây đã được tách thành kế hoạch 3 giai đoạn —
> [`PLAN_TRIEN_KHAI_TV4_TUAN4.md`](plan/PLAN_TRIEN_KHAI_TV4_TUAN4.md) (tổng),
> [`PLAN_GIAI_DOAN_1_N2_N4.md`](plan/PLAN_GIAI_DOAN_1_N2_N4.md),
> [`PLAN_GIAI_DOAN_2_D0_XET_LOI.md`](plan/PLAN_GIAI_DOAN_2_D0_XET_LOI.md),
> [`PLAN_GIAI_DOAN_3_SUA_LOI.md`](plan/PLAN_GIAI_DOAN_3_SUA_LOI.md). **Chưa thực thi gì.**
>
> ⭐ **Bổ sung 03/10:** [`QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md`](misc/QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md) —
> TV4 đã **chốt** B5 (→ PA-A) và B6 (→ PA-A), và **tự thực hiện** luôn phần việc sinh ra cho TV3/TV1
> theo nguyên tắc "ngoài contact" (báo trước, ⛔ không chờ trả lời). B5/B6 là **điều kiện tiên quyết**
> của N2: B6 mở khoá N2-E7. 3 việc hạ tầng còn lại **không tự quyết** — đã tách thành đề xuất 07/08/09.

1. Reviewer duyệt mapping + B1/B2/B4, rồi cập nhật issue `#20`, `#21`, `#22`.
2. ⭐ **Trước tiên: triển khai B5 → B6 → QD3-3a/3b/3c** (điều kiện tiên quyết của N2) — xem
   [`QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md`](misc/QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md) §4.1.
3. Hoàn thiện nốt K23 ở mức triển khai: đóng gói 2 API vào profile docker-compose dùng chung *(tuần 5)*.
4. Chốt 3 việc hạ tầng với Tâm + TV2 — dựa trên đề xuất 07/08/09, **không** tự quyết:
   [`DE_XUAT_07`](../../../proposal/DE_XUAT_07_NOI_DAT_LICH_BACKUP.md) (nơi đặt lịch),
   [`DE_XUAT_08`](../../../proposal/DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md) (kho 30 ngày),
   [`DE_XUAT_09`](../../../proposal/DE_XUAT_09_ROTATE_KHOA_JWT_DA_LO.md) (rotate khoá JWT).
5. Sang N2: Playwright, publish E2E, k6 đo p95/p99 và tỉ lệ cache hit, CI frontend.
   ⛔ **Đính chính phạm vi**: bản gốc ghi "Playwright **5 luồng**" — cổng *5 E2E flows* thuộc
   **tuần 5** (6-tuần L170). Tuần 4 của TV4 chỉ làm **publish + search**; `register/login` (TV1),
   `category` (TV2), `create-recipe` (TV3) do thành viên khác viết. EXPLAIN chỉ chạy lại **nếu có
   đổi index**. Chi tiết: `PLAN_GIAI_DOAN_1_N2_N4.md` §0.2.
