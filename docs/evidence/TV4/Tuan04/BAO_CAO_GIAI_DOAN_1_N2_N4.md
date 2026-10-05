# BÁO CÁO GIAI ĐOẠN 1 — TUẦN 4 (N2 · N3 · N4-C)

| Mục | Nội dung |
|---|---|
| **Người lập** | Nguyễn Hữu Trung Sơn (TV4 · 2312739) |
| **Ngày chốt** | 04/10/2026 · **cập nhật 05/10/2026** (N3-B/C1/C2 hoàn thành; thêm việc cuối GĐ3) |
| **Nhánh** | `2312739_NHTSon_D5-D6-D7` — **không push `main`** |
| **Commit chốt** | `8d9d62b` (fix storage) · `51719a5` (evidence CI xanh) · `870d6e3` (evidence L5) · `03564c4` (fix khoá JWT, merge `main`) |
| **Phạm vi** | Giai đoạn 1 = tuần 4: **N2** (resilience + E2E publish), **N3** (LAB), **N4-C** (tài liệu + chốt evidence) |
| **Trạng thái** | ⛔ **Cổng G4 — chờ Nguyễn Thanh Tâm (reviewer) xác nhận.** Báo cáo này không tự đánh dấu ô nào là đạt. |

---

## 1. Tóm tắt cho reviewer

Tuần 4 đóng được **N2** và **N4-C**. **N3 gần đóng** — 05/10 đã hoàn thành `N3-B` (Lab L5), `N3-C1` (Sổ K), `N3-C2` (PR lab); còn `N3-A3` và `N3-A5` chưa làm.

**Ba lỗi thật đã tìm ra và sửa, mỗi lỗi kèm test hồi quy:**

| # | Triệu chứng | Nguyên nhân gốc | Cách sửa |
|---|---|---|---|
| 1 | DB chết ⇒ **3 endpoint đọc trả `500`** | `EfUnitOfWork` chạy lệnh qua execution strategy của EF nên `NpgsqlException` bị bọc thành `InvalidOperationException`; `ApiExceptionHandler` chỉ kiểm tra exception **ngoài cùng** | Dò cả chuỗi `InnerException`, map `Npgsql`/socket/timeout → `503 database.unavailable`; đặt nhánh này **trước** nhánh 422 để `DbUpdateException` do mất kết nối không bị báo nhầm |
| 2 | DB chết lúc **khởi động** ⇒ **tiến trình chết** | `AddOrUpdate` của lịch sitemap ném `NpgsqlException` ra khỏi `Main` | Bọc try/catch + log; `/health/ready` vẫn 503 nên orchestrator restart pod khi DB trở lại |
| 3 | `GET /api/v1/recipes/{slug}` trả **`500`** khi thiếu credential storage | `MinioClient.Build()` **ném** `MinioException` khi credential rỗng mà `MinioStorageService` gọi ngay trong **constructor**; B5 khiến mọi lần đọc công thức đều dựng service này | `Lazy<IMinioClient>`: `Build()` chạy lần đầu khi thật sự gọi storage, nên lỗi ra `503 storage.unavailable`. Fail-fast lúc khởi động **giữ nguyên** (`ValidateOnStart`) |

> 🔴 **Lỗi 3 chỉ do CI bắt, không phải do test local.** Máy dev có `Minio__*` trong `.env`; CI không có
> file `.env` nên job không set. Đây là dạng lỗi kinh điển: **môi trường dev che giấu, môi trường mới
> làm lộ**. Bài học đã ghi vào `HUONG_DAN_CHAY_TV4.md`: khi sửa lỗi hạ tầng, kiểm lại bằng cách dựng
> lại đúng điều kiện CI (xoá `Minio__*`, thêm `Redis__Instance=ci`) — **311/311 xanh** tại commit `8d9d62b`.

---

## 2. Số đo đã kiểm chứng

| Hạng mục | Kết quả | Bằng chứng |
|---|---|---|
| Backend `dotnet test CulinaryBlog.sln` | ✅ **321/321** (`CulinaryBlog.Tests` **316/316** + `ConcurrencySpike` **5/5**) sau khi merge `main` 05/10 — trước merge tính **316/316** (311 + 5). `Skipped = 0` | log `dotnet test` mục §6 `SO_EVIDENCE_TUAN_4.md` |
| Coverage `CulinaryBlog.Application` | **84.13%** ≥ ngưỡng cổng **80%** | `deploy/check-coverage.sh 80 TestResults` |
| `dotnet build` | 0 warning / 0 error | CI `Backend week 1` |
| `dotnet format --verify-no-changes` | exit `0` | chạy cục bộ |
| Playwright E2E | **26/26**, 3 lần liên tiếp; `recipe-publish --repeat-each=4` = 16/16 | `src/frontend/e2e/` |
| Frontend `tsc` · `lint` · `build` | đều exit `0` | CI `Frontend CI` |
| k6 tải đọc | `~3606` request, `~120 req/s`, `http_req_failed` **0.00%**, 3 lần xanh | `tests/performance/*.log` |
| Outage drill | Redis 0.2s · S3 0.5s · API 3.5s phục hồi; `/health/live` 200 còn `/health/ready` 503 | `deploy/outage-drill.ps1` |
| **CI** | `Backend week 1` run `37213966752` ✅ · `Frontend CI` run `37213966761` ✅ | GitHub Actions |

> ⚠️ **Số đo trên CI chỉ xanh từ commit `8d9d62b`.** Trước đó run `37211544129` **đỏ** 2 test — đó là
> lỗi 3 ở bảng trên. Đã sửa và push; hai job sau đó đều xanh.

---

## 3. N2 — 7/8 XONG, CÒN 1 CỤM `D1/D2/D3`

> **Phân biệt rõ:** phần **lõi của N2** (resilience C1–C4, tấn công file B2–B4, E2E B1, retry/race E,
> CI, k6, coverage gate) **đã đóng**. Nhưng **toàn bộ khối N2 chưa xong** vì còn cụm
> `N2-D1/D2/D3` treo từ tuần 3. Không được ghi "N2 đã đóng" không điều kiện.
> **N2 = 7/8 việc xong, 1 việc dở dang.**

| Mục | Nội dung | Trạng thái |
|---|---|---|
| N2-C1/C2 | Dừng Redis/S3/DB/worker và đo thời gian phục hồi; mọi lỗi hạ tầng trả `503` chứ không `500` | ✅ |
| N2-C1b | Cache dùng chung giữa nhiều instance qua Redis; sitemap có **distributed lock** | ✅ |
| N2-C1c | Hợp đồng retry khoá bằng test: sitemap **2**, resize **3**, xoá ảnh **3**, welcome 0/1/5/30 phút | ✅ 4/4 |
| N2-C3/C4 | Commit `tests/performance/read-load.js` + 3 log k6, đủ p50/p95/p99 | ✅ |
| N2-B1 | Playwright: đăng ký → đăng nhập → **wizard 5 bước** → publish → tra cứu tìm kiếm | 🟡 **26/26 xanh nhưng mới phủ 2/5 luồng** (publish, search) |
| N2-B2/B3/B4 | File-size attack, MIME spoofing (magic bytes), quyền upload | ✅ |
| N2-E | Retry/race/security: retry idempotent, delete-vs-resize, 2 request đặt primary, xoá file không tồn tại | ✅ |
| **N2-D1/D2/D3** | Progress upload % · UI unpublish/archive · checklist WCAG 320/768/1200 px | ⬜ **Dở dang** — API unpublish/archive đã có tại `Program.cs:620,627` nhưng **UI chưa làm**, nên không E2E được; D3 thuộc **TV2** |

### 3.1 Lỗi sản phẩm đã sửa trong N2-B1

Wizard **mất bước sau lần lưu draft đầu tiên**: lưu xong không chuyển bước, reload thì về bước 1.

- **Không** coi là "flaky test" — đó là lỗi sản phẩm thật, chỉ lộ ra khi chạy lặp.
- Sửa: truyền `?step=` khi lưu (`RecipeWizard.tsx`) và đọc lại `step` từ URL khi mount
  (`EditRecipeClient.tsx`), kèm `settleStep()` trong `recipe-publish.spec.ts`.
- Kiểm chứng: `--repeat-each=4` **16/16**; full suite **3 lần liên tiếp 26/26**.

---

## 4. N3 — GẦN ĐÓNG (còn A3 và A5)

| Mục | Nội dung | Trạng thái |
|---|---|---|
| N3-A1 | PBKDF2 | ✅ Sản phẩm có (ASP.NET Identity); test assert **V3 / SHA512 / ≥100 000 vòng** tại `AuthTests.cs:108-111` |
| N3-A2 | Refresh hash/rotation/reuse detection/logout | ✅ 8/8 test ở `Week3AuthAndPersonalLabTests.cs` (reuse ⇒ thu hồi cả family) |
| N3-A4 | FTS tsvector/trigger/GIN/`ts_rank`/AND/phân trang | ✅ Có trong sản phẩm + test; ⬜ EXPLAIN **đo lại tuần 4 chưa chạy** |
| **N3-A3** | **Zod/RHF phía FE** | ⬜ **Chưa làm** — `zod` có trong `package.json` nhưng `src/` **không import chỗ nào**. FE chỉ dùng `react-hook-form`. Phía BE đã có FluentValidation. |
| **N3-A5** | **Google OAuth2/PKCE** | ⬜ **Chưa làm — còn chờ credentials.** Không tính mock là hoàn thành. |
| **N3-B** | **Lab L5** (7 phase: `search-ssr`, `isr-detail`, `query-rollback`, `image-opt`, `seo`, `observability`, `multi-instance`) | 🟡 **Đã làm 05/10 — nhánh `practice/TV4/L5` đã tạo.** 63 check · **3/7 phase PASS** (`seo` 15/15, `observability` 10/10, `multi-instance` 7/7). 4/7 phase lộ ra vấn đề thật: ISR không hoạt động, ảnh không tối ưu 2 tầng, search trả `no-store`, RowVersion chưa kiểm chứng được. Bằng chứng: `SOK_LAB_L5.md` + `logs/lab_l5_run.log` + `logs/lab_l5_db.txt` |
| **N3-C1** | `SOK_LAB_L5.md` | ✅ **Đã tạo 05/10** — commit `870d6e3`, kèm 2 log. Kết luận lab: *"FAIL có chủ đích — 41/63 check đạt, 3/7 phase PASS, 4/7 phase lộ ra vấn đề thật"* |
| **N3-C2** | Mở PR cho nhánh lab | ✅ **Đã làm 05/10** — PR #28, đã mở rồi **đóng** theo quyết định nhóm: lab ở nhánh riêng, PR chỉ để đánh dấu/review. Commit code lab `6c90ad8`, evidence `870d6e3` |
| **N3-C3** | **Đính chính bảng 24 ô theo quy tắc code + test + log** | ✅ **Đã làm** — xem mục §2 của `SO_EVIDENCE_TUAN_4.md` |

> **Cập nhật 05/10 — N3-B/C1/C2 đã hoàn thành, đã dời khỏi danh sách "chưa làm".**
> Nội dung báo cáo này trước đó ghi "L5 dời sang tuần 5" theo đúng quy tắc ưu tiên trong
> `PLAN_GIAI_DOAN_1_N2_N4.md`. Nay đã thực hiện trong tuần 4 sau khi đóng được GĐ1.
> **N3 vẫn chưa đóng** vì còn `A3` và `A5` chưa làm.
>
> **Vì sao dời N3-B lúc đó:** L5 là lab UI/SEO/vận hành, cần 7 phase với check đếm được. Trong khi đó
> `N2-C` vừa phát hiện và sửa 2 lỗi hạ tầng thật (lỗi 1 và lỗi 2) — ưu tiên đúng là xử lý lỗi sản
> phẩm trước khi làm lab. `N4-C` là điều kiện cổng G4 nên làm trước.

---

## 5. N4-C — ĐÃ ĐÓNG

| Mục | Việc | Trạng thái |
|---|---|---|
| N4-C1 | Cập nhật `docs/evidence/TV4/HUONG_DAN_CHAY_TV4.md` bằng số đo mới | ✅ Số test 154 → **311**, thêm lệnh coverage/E2E/k6/outage, thêm mục "bài học từ lỗi CI" |
| N4-C2 | Cập nhật `README.md` mục test/ops bằng số đo mới | ✅ §4.5: **172/172 → 316/316**, thêm coverage/k6/Playwright/CI; §6 TV4 ghi trạng thái GĐ1 + việc chuyển tuần 5 |
| N4-C3 | Cập nhật `CHANGELOG.md` | ✅ Thêm `0.4.0` (tuần 4) và `0.3.0` (tuần 3) |
| N4-C4 | Chốt `SO_EVIDENCE_TUAN_4.md` + `TRANG_THAI_THUC_HIEN_TUAN_4.md` | ✅ |
| N4-C5 | Ghi rõ ô nào còn thiếu, không làm tròn số | ✅ Chính là báo cáo này + bảng 24 ô |
| N4-A | Runbook `docs/RUNBOOK.md` đầy đủ 7 mục | ⛔ **Thuộc tuần 5** (không thuộc phạm vi GĐ1) |
| N4-B | Deploy lặp lại được, staging, TLS/HSTS | ⛔ **Thuộc tuần 5** |

---

## 6. Giới hạn phải nói rõ (không được làm tròn)

1. **Một node là SPOF** — không có failover. `NFR-REL-001` (uptime ≥ 99,5%) **không được tuyên bố đạt**
   bằng test ngắn; `deploy/outage-drill.ps1` chỉ đo thời gian phục hồi khi tự khởi động lại.
2. **DB outage là mô phỏng, không phải failover thật.** PostgreSQL chạy bằng dịch vụ native
   `postgresql-x64-18` nên **không dừng được** (thiếu quyền Administrator); script trỏ một instance
   sang port đã chết để giả lập mất kết nối.
3. **Cache không có số đo hỗ trợ.** Đã đo p50/p95/p99 nhưng **chưa đo cache-hit ratio**, nên chưa kết
   luận được cache giảm bao nhiêu phần trễ.
4. **`EXPLAIN` không đo lại tuần 4** — số đo chỉ có từ D5 tuần 3; index không đổi nên `N2-C5` đã được
   loại khỏi phạm vi.
5. **UI unpublish/archive chưa có** dù API đã có ⇒ không E2E được luồng gỡ xuất bản.
6. **Chỉ 2/5 luồng E2E.** `register/login` thuộc TV1, `category` thuộc TV2, `create-recipe` thuộc TV3
   — thuộc cổng G6 tuần 5.
7. **Jest/RTL chưa có** (thuộc TV1) ⇒ K21 mới đủ một phần.
8. **Rotate khóa JWT thật chưa làm** — nằm ngoài repo, xem
   [`DE_XUAT_09`](../../../proposal/DE_XUAT_09_ROTATE_KHOA_JWT_DA_LO.md).
9. **Nơi đặt lịch backup 03:00 ICT chưa chốt** — xem
   [`DE_XUAT_07`](../../../proposal/DE_XUAT_07_NOI_DAT_LICH_BACKUP.md).
10. **2 API instance mới đo thủ công qua Nginx**, chưa có compose profile dùng sẵn ⇒ K23 chưa đạt.
11. **Kho backup 30 ngày chưa có** — xem
    [`DE_XUAT_08`](../../../proposal/DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md).
12. `npm audit` còn **10** vulnerability (`9 high`, `1 critical`).
13. **Google OAuth2/PKCE còn chờ credentials** — không có mock nào được tính là hoàn thành.

---

## 7. Phát sinh trong GĐ1

| # | Ngày | Khối | Mô tả | Mức | Xử lý |
|---|---|---|---|---|---|
| 1 | 04/10 | N2-B1 | Wizard mất bước sau lần lưu draft đầu — bị hiểu nhầm là test chập chờn | S2 | Sửa ngay: `?step=` + `settleStep()`; có test hồi quy |
| 2 | 04/10 | N2-C | DB chết ⇒ 3 endpoint đọc trả `500` thay vì `503` | S2 | Sửa ngay: dò `InnerException`; 6 test hồi quy |
| 3 | 04/10 | N2-C | DB chết lúc khởi động giết tiến trình vì lịch sitemap ném ra khỏi `Main` | S2 | Sửa ngay: bọc try/catch + log |
| 4 | 04/10 | N2-C1d | `GET /recipes/{slug}` trả `500` khi thiếu credential storage — **do CI bắt** | S2 | Sửa ngay: `Lazy<IMinioClient>`; 2 test hồi quy |
| 5 | 04/10 | N2-C | PostgreSQL native không dừng được (thiếu quyền Administrator) | S1 | Ghi rõ giới hạn; drill DB chỉ là mô phỏng |
| 6 | 04/10 | N2-D3 | UI unpublish/archive chưa có | S1 | Giao tuần 5; API đã sẵn sàng |
| 7 | 04/10 | N3-A3 | `zod` có trong `package.json` nhưng không dùng trong `src/` | S3 | Ghi nhận, chuyển tuần 5 |
| 8 | 04/10 | N3-A5 | Thiếu credentials Google OAuth | S1 | Ghi "còn chờ", không tính hoàn thành |
| 9 | 04/10 | N3-B | Lab L5 7 phase chưa làm | S1 | Dời tuần 5 theo quy tắc ưu tiên trong plan |
| 10 | 04/10 | Tool | Dùng PowerShell `Get-Content`/`Set-Content` sửa source tiếng Việt đã làm hỏng encoding UTF-8 (`đ` → `?`) | S2 | Khôi phục bằng `git checkout` rồi sửa lại bằng tool edit; đã ghi cảnh báo cho các phiên sau |
| 11 | 05/10 | N3-B/C1/C2 | Lab L5 7 phase đã làm xong; 4/7 phase lộ ra vấn đề thật (ISR không hoạt động, ảnh không tối ưu 2 tầng, search `no-store`, RowVersion chưa kiểm chứng) | S2 | Ghi Sổ K + log; **chuyển sang sửa ở GĐ3/GĐ4**, không sửa trong GĐ1 |
| 12 | 05/10 | Merge | Merge `main` mang vào `docker-compose.staging.yml` **hardcode khoá JWT đã thu hồi** — test `JwtSigningKeyNotCommittedTests` đỏ 1/316 | S2 | Sửa ở **GĐ3** (`03564c4`): `${JWT_SIGNING_KEY:?}`; test 6/6 xanh |
| 13 | 05/10 | TV1 | Báo cáo "Tuần 5" của TV1 có 4 sai lệch đã xác nhận (p95 < mean, nhãn tuần, số test, `render.yaml` không tồn tại) | S2 | Hồ sơ [`KiemChung_Commit_Week5_TV1.md`](KiemChung_Commit_Week5_TV1.md); **không sửa báo cáo của TV1** |

---

## 8. Xin reviewer kiểm tra

1. Ba lỗi thật ở mục 1 — đặc biệt **lỗi 3**: xác nhận cách sửa `Lazy<IMinioClient>` không làm mất
   fail-fast lúc khởi động.
2. Bảng 24 ô kỹ năng mục §2 của `SO_EVIDENCE_TUAN_4.md` — đánh dấu ✅/❌ từng ô.
3. Số đo mục §2: **316/316**, coverage **84.13%**, Playwright **26/26**, k6 `http_req_failed` **0.00%**.
4. Danh sách giới hạn mục 6 — có ô nào reviewer cho là đã đủ bằng chứng dù tôi ghi là chưa thì không?
5. **N3 gần đóng** — `N3-B`/`C1`/`C2` đã xong 05/10. Xác nhận có chấp nhận việc đóng N3 trong
   tuần 4 với `A3`/`A5` chuyển kỳ sau, hay cần giữ.
6. **Lab L5 lộ ra 4 vấn đề thật** (ISR, ảnh, `no-store`, RowVersion) — xác nhận có đưa vào
   sửa ở GĐ3/GĐ4 hay để kỳ sau. Chi tiết `SOK_LAB_L5.md`.
7. **Báo cáo "Tuần 5" của TV1 đánh dấu không đáng tin** — xem
   [`KiemChung_Commit_Week5_TV1.md`](KiemChung_Commit_Week5_TV1.md). Đề nghị TV1 đính chính
   6 điểm §5 của hồ sơ đó.
8. ✅ **CI đã xanh trở lại** ở `8a585ef` (05/10): `Backend week 1` run `37320431750` ✅ + `Frontend CI` run
   `37320431432` ✅. Đã xử lý cả 3 nguyên nhân đỏ: khoá JWT còn sót trong `.md` của TV4 · `ApiFactory` không seed
   khiến `E2E_Scenario_5` của TV1 đỏ trên Postgres sạch · race span trong `TracingObservabilityTests`.

## 9. Chỉ số tài liệu

| Tài liệu | Vai trò |
|---|---|
| [`SO_EVIDENCE_TUAN_4.md`](SO_EVIDENCE_TUAN_4.md) | Evidence chi tiết từng K + log đính kèm |
| [`TRANG_THAI_THUC_HIEN_TUAN_4.md`](TRANG_THAI_THUC_HIEN_TUAN_4.md) | Trạng thái từng mục N2/N3/N4 |
| [`PLAN_GIAI_DOAN_1_N2_N4.md`](PLAN_GIAI_DOAN_1_N2_N4.md) | Kế hoạch + lý do loại bớt phạm vi |
| [`QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md`](QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md) | Quyết định đưa B5/B6 lên trước N2 |
| [`SOK_LAB_L5.md`](SOK_LAB_L5.md) | Sổ K Lab L5 — 63 check, 3/7 phase PASS |
| [`BAO_CAO_GIAI_DOAN_3_SUA_LOI.md`](BAO_CAO_GIAI_DOAN_3_SUA_LOI.md) | Báo cáo GĐ3 — gồm việc cuối: kiểm chứng báo cáo TV1 |
| [`KiemChung_Commit_Week5_TV1.md`](KiemChung_Commit_Week5_TV1.md) | Hồ sơ kiểm chứng báo cáo "Tuần 5" của TV1 |
| [`../HUONG_DAN_CHAY_TV4.md`](../HUONG_DAN_CHAY_TV4.md) | Lệnh cài đặt/chạy + số đo mới |
| [`../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md`](../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md) | Mapping 24 dòng K ↔ FR/NFR ↔ ADR ↔ evidence |