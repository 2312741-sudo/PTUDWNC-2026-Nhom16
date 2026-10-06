# BÁO CÁO GIAI ĐOẠN 3 — SỬA LỖI (TUẦN 4, TV4)

| Mục | Nội dung |
|---|---|
| **Người lập** | Nguyễn Hữu Trung Sơn (TV4 · 2312739) |
| **Ngày lập** | 05/10/2026 |
| **Nhánh** | `2312739_NHTSon_D5-D6-D7` — **không push `main`** |
| **Phạm vi** | Giai đoạn 3 = sửa lỗi phát hiện được trong GĐ1, GĐ2 và các việc phát sinh |
| **Việc cuối của GĐ3** | Kiểm chứng lại báo cáo "Tuần 5" của TV1 (`21aa722`) → kèm lỗi khoá JWT tìm ra và đã sửa |
| **Trạng thái** | 🟡 Hoàn thành phần đã thực thi. Còn mở `BUG-W4-04/05/06/07/09/10` — xem mục 3 |

---

## 1. Tóm tắt

GĐ3 gồm **02 việc sửa lỗi**, cả hai đều có test hồi quy chứng minh lỗi còn trước khi sửa:

| # | Lỗi | Mức | Nguyên nhân gốc | Cách chứng minh | Trạng thái |
|---|---|---|---|---|---|
| 1 | `docker-compose.staging.yml` hardcode **khoá JWT đã thu hồi** | `R3` | Commit `21aa722` của TV1 chép nguyên khoá dev mà `JwtSettings.Validate()` đã cấm, vào file compose | `JwtSigningKeyNotCommittedTests.Revoked_key_appears_only_in_the_blocklist` **FAIL 1/316** trước → **PASS 6/6** sau | ✅ Đã sửa |
| 2 | Xung đột `.gitignore` khi merge `main` | `R1` | Nhánh tuần và `21aa722` cùng sửa `.gitignore` ở hai vùng khác nhau | `git merge` báo `CONFLICT (content)`. Sau khi ghép cả hai: dump `.sql.gz` bị ignore, `.gitkeep` vẫn được theo dõi | ✅ Đã giải quyết |

**Việc cuối của GĐ3 — kiểm chứng báo cáo "Tuần 5" của TV1** — phát hiện **4 sai lệch đã xác nhận được** trong báo cáo và đó là lý do tìm ra lỗi ở bảng trên. Hồ sơ: [`KiemChung_Commit_Week5_TV1.md`](../misc/KiemChung_Commit_Week5_TV1.md).

---

## 2. Việc cuối của GĐ3 — kiểm chứng báo cáo "Tuần 5" của TV1

### 2.1. Vì sao đưa vào GĐ3

Khi merge `main` vào nhánh tuần, TV4 phải đọc nội dung mới để chắc không phá việc đã làm. Việc kiểm chứng phát hiện **một lỗi bảo mật thật** (`R3`) trong file cấu hình mà commit đó mang vào ⇒ **đúng phạm vi GĐ3 (sửa lỗi)**, không phải một task review thường.

Việc này được đặt làm **việc cuối của GĐ3** vì nó là việc phát sinh sau cùng, ngay trước lúc chốt bàn giao.

### 2.2. Bốn sai lệch đã xác nhận được

| # | Sai lệch | Cách xác nhận |
|---|---|---|
| 1 | **Số liệu p95 vi phạm bất biến toán học** — cả 3 endpoint đều ghi p95 < mean (7ms < 20.95ms; 6ms < 9.61ms; 2ms < 2.12ms). p95 không bao giờ nhỏ hơn mean | Đọc bảng số đo trong `TUAN_5.md` |
| 2 | **Nhãn tuần sai** — báo cáo ghi "Tuần 5", nhưng repo đã có `TUAN_4.md` của chính TV1 ghi task **A6, A7** với **177/177 test**. Commit `21aa722` ghi lại đúng task **A7** nhưng đổi thành "Tuần 5" và **183/183** | Đối chiếu `TUAN_4.md` với `TUAN_5.md` |
| 3 | **Số test không khớp thực tế** — báo cáo nói 183 (178 + 5). Thực tế sau khi merge vào nhánh tuần là **316 + 5 = 321** | Chạy `dotnet test CulinaryBlog.sln` |
| 4 | **`render.yaml` được ghi là đầu ra nhưng không tồn tại trong commit** | `git show --name-only 21aa722` |

### 2.3. Vấn đề bản chất bằng chứng

| Mục | Báo cáo ghi | Minh chứng trong repo |
|---|---|---|
| BCP/DR drill | RTO < 2 phút, RPO = 0, "25 Categories và 100 Recipes" | ⛔ Không có `.sql.gz`, không có log diễn tập. `backups/` chỉ có `.gitkeep`. Lưu ý CSDL thật có **101** recipe, không phải 100 |
| RTO / RPO | Số đo thời gian | ⛔ Không có mốc thời gian, không có quy trình nào xác định RPO |
| Hiệu năng | Bảng p50/p95 | ⛔ Có script `tests/k6/auth-profile-load.js` nhưng **không có output chạy**. Số đo lại bằng `ab` — công cụ không xuất p95 đáng tin |
| 5 kịch bản E2E | 5 test | 🟢 File `Week5StagingAndE2ETests.cs` **có thật**, 5 `[Fact]`, `IClassFixture<ApiFactory>` + `HttpClient` thật. Còn lại: cần TV1 nói rõ `ApiFactory` dựng PostgreSQL/Redis thật hay in-memory |

Phần script BCP/DR tự thân có vẻ hợp lý về logic: `set -euo pipefail`, `pg_dump | gzip`, `sha256sum` tạo **sau khi** ghi file, `restore-db.sh` đối chiếu checksum trước khi nạp. **Vấn đề không nằm ở script mà ở chỗ không có bản ghi chạy.**

### 2.4. Hai điểm về vai trò và công nhận

- **Người xác nhận là chính người thực hiện**: cả 5 bản ghi ghi *"Reviewer xác nhận: Nguyễn Thanh Tâm (Nhóm trưởng)"* — không phải nghiệm thu độc lập.
- **Nhận công đề xuất của TV4**: mục "Bản ghi 5" liệt kê đề xuất B1 *"chuyển mã lỗi storage từ 500 sang 503 `storage.unavailable`"*. Việc này **đã do TV4 triển khai trước** (`Lazy<IMinioClient>`, `StorageFailureContractTests` 9/9), không phải thành tự thân xác nhận của TV1.

### 2.5. Lỗi kỹ thuật phát hiện trong lúc kiểm chứng

Commit `21aa722` đưa vào `docker-compose.staging.yml:81` đúng khoá mà `JwtSettings.Validate()` đã thu hồi:

```yaml
Jwt__SigningKey: "<khoá dev đã bị thu hồi — xem danh sách chặn trong JwtService.cs>"
```

> ⛔ **Không chép lại giá trị khoá thật vào tài liệu.** Tài liệu này là file text mà test
> `JwtSigningKeyNotCommittedTests.Revoked_key_appears_only_in_the_blocklist` quét; danh sách cho
> phép chỉ gồm `JwtService.cs` và chính file test. Chép khoá vào đây sẽ làm CI đỏ y hệt lỗi gốc.

Hệ quả: nếu chạy staging bằng file này, toàn bộ JWT được ký bằng khoá đã bị cấm trong codebase — **và khoá đó nằm công khai trong git**. Đây đúng là bất biến bảo mật mà nhóm đã viết test để chặn.

**Đã sửa** (mục 3, lỗi 1).

### 2.6. Phạm vi

TV4 **không sửa** `TUAN_5.md`, `BAO_CAO_LAB_05.md` hay các file `.docx` của TV1 — giữ nguyên bản ghi minh chứng gốc để TV1 tự đính chính theo §5 của hồ sơ kiểm chứng.

---

## 3. Chi tiết hai lỗi đã sửa

### 3.1. Lỗi 1 — khoá JWT đã thu hồi trong compose staging

| Mục | Nội dung |
|---|---|
| **Sửa gì** | `docker-compose.staging.yml:81` — thay giá trị hardcode bằng `${JWT_SIGNING_KEY:?...}`, **không có default** |
| **Quyết gì** | Không cần quyết — làm theo bất biến bảo mật đã có test. Thiếu biến thì compose **fail rõ ràng**, chấp nhận hơn là chạy bằng khoá đã biết |
| **Ảnh hưởng gì** | K24 (bảo mật), NFR-SEC-001, test `JwtSigningKeyNotCommittedTests`. Sửa file của TV1 nhưng là lỗi cấu hình nên sửa luôn để nhánh tuần xanh |
| **Nguyên nhân gốc** | `21aa722` chép khoá dev đã bị `JwtSettings.Validate()` thu hồi vào file compose |
| **Cách chứng minh** | Trước: **1 fail / 316**. Sau: **6/6 pass**. Ngoài ra `docker compose config` — thiếu biến → exit 1 kèm thông báo; có biến → exit 0 |
| **Rủi ro hồi quy** | Người chạy staging phải tự sinh key. Đã bù bằng thông báo lỗi ngay trong compose và placeholder trong `.env.example` |
| **Lùi lại được** | Có — `git revert 03564c4` |

### 3.2. Lỗi 2 — xung đột `.gitignore`

| Mục | Nội dung |
|---|---|
| **Sửa gì** | `.gitignore` — ghép cả hai vùng: nhánh tuần (artifact Playwright, `run.log`) + `21aa722` (`backups/**/*.sql*`, `*.tar*`, `!backups/**/.gitkeep`) |
| **Quyết gì** | Không cần quyết — hai bên không giao nhau nên giữ cả hai |
| **Ảnh hưởng gì** | Không ảnh hưởng mã nguồn; quyết định `backups/**` có bị commit nhầm hay không |
| **Cách chứng minh** | `backups/database/culinary_test.sql.gz` → bị ignore ✔ · `backups/database/.gitkeep` → vẫn được theo dõi ✔ |

---

## 4. Kiểm định toàn bộ sau lần sửa cuối

| Hạng mục | Kết quả |
|---|---|
| `dotnet build CulinaryBlog.sln -c Release` | **Build succeeded — 0 Warning, 0 Error** |
| `dotnet test CulinaryBlog.sln` | **316/316** `CulinaryBlog.Tests` + **5/5** `ConcurrencySpike`, Skipped 0 |
| Trạng thái trước khi sửa | 315 pass / **1 fail** |
| `docker compose -f docker-compose.staging.yml config` | exit 0 khi có `JWT_SIGNING_KEY`; exit 1 kèm thông báo khi thiếu |
| Evidence L5 (`SOK_LAB_L5.md` + 2 log) | nguyên vẹn sau merge |
| Evidence L4 (`SOK_LAB_L4.md`) | nguyên vẹn sau merge |
| File bảo vệ `BAO_CAO_LOI_500_TRANG_SEARCH.md` | nguyên vẹn — hash `93661aa13e6fa2e081b9a89b2517eca5d0df083c` không đổi |

> ⛔ **CI backend đang ĐỎ — đã xem log thật 05/10, không phải "chưa chạy".** `03564c4`, `d4edfa2` và
> `0dfac4e` đều đỏ ở `JwtSigningKeyNotCommittedTests.Revoked_key_appears_only_in_the_blocklist`
> (run `37300109133`, `37305355389`, `37315121347`) vì **chính hồ sơ này và
> `KiemChung_Commit_Week5_TV1.md` chép lại khoá JWT đã thu hồi vào file `.md`** — đúng bất biến mà
> nhóm đã viết test để chặn. Đã gỡ, test local `6/6` xanh. Ngoài ra `870d6e3` đỏ ở
> `TracingObservabilityTests` (flaky, chưa xử lý). CI xanh gần nhất: `8d9d62b` (run `37213966752`).

---

## 5. Lỗi còn mở — không sửa trong GĐ3

| Mã lỗi | Lỗi | Vì sao không sửa |
|---|---|---|
| BUG-W4-01 | `422 recipe.version_conflict` khi thêm ảnh vào recipe đã có ảnh | ✅ **Đã sửa trên lab** `lab/TV4-audit-tuan4` nhưng ⛔ **chưa merge** nhánh tuần — đã kiểm chứng lại 05/10: `RecipeImageConfiguration.cs` **vẫn thiếu** `ValueGeneratedNever()`. ⏳ Cần **Tâm chốt C1 hay C2** (đụng schema của TV3, không tự quyết) |
| BUG-W4-02 | Secret còn trong file tracked, `scan-secrets.sh` không bắt được | ✅ Đã sửa trên lab nhưng ⛔ **chưa merge** |
| BUG-W4-03 | 2 IP khác nhau dùng chung 1 bucket | ✅ Đã sửa trên lab nhưng ⛔ **chưa merge** |
| BUG-W4-04 | Số liệu mâu thuẫn trong README | ✅ Đã xác định (README đã cập nhật 05/10) — cần Tâm đối chiếu |
| BUG-W4-05 | `CHANGELOG.md` thiếu mục | ✅ Đã sửa — nay có `0.3.0`, `0.4.0`, `0.4.1` |
| BUG-W4-06 | Phải đính chính | ⛔ **Không được xoá soft delete của `Recipe`** — đã xác nhận đang dùng, chỉ sửa tên test + ADR |
| BUG-W4-07 | Round-robin/failover OK nhưng **TLS chưa bắt** | ⏳ TLS thuộc tuần 5 (`N4-B`) |
| BUG-W4-08 | **Chưa kiểm tra sâu** | ⏳ Chưa xác minh — không tự kết luận |
| BUG-W4-09 | Phải đính chính | ⏳ Điều kiện cạnh tranh, không phải trạng thái thường |
| BUG-W4-10 | Migration no-op không sửa lỗi runtime | ❌ Không có tác dụng lên database; chỉ cập nhật snapshot |
| — | Google OAuth thật | ⛔ Thiếu credentials |
| — | Uptime thật 99,5% | ⛔ Cần vận hành dài hạn, không sửa được bằng code |
| — | RowVersion/optimistic concurrency | ⛔ Tài khoản E2E không sở hữu công thức, `GetRecipesQuery` không có filter chủ sở hữu |

> **Ba lỗi GĐ1 đã đóng KHÁC với BUG-W4-01/02/03.** Ba lỗi trong báo cáo GĐ1 là: DB chết trả
> `500`, DB chết lúc khởi động giết tiến trình, và `GET /recipes/{slug}` trả `500` khi thiếu
> credential storage — cả ba **đã merge** vào nhánh tuần ở commit `8d9d62b` và khoá bằng test hồi
> quy. `BUG-W4-01/02/03` của [`BAO_CAO_LOI_TUAN_4_TV4.md`](BAO_CAO_LOI_TUAN_4_TV4.md) là lỗi khác,
> bản vá vẫn nằm trên nhánh lab `lab/TV4-audit-tuan4` và **chưa merge**. Không được tính chồng hai
> danh sách này.

---

## 6. Phát sinh mới trong GĐ3

| # | Ngày | Mô tả | Mức | Xử lý |
|---|---|---|---|---|
| 1 | 05/10 | `21aa722` hardcode khoá JWT đã thu hồi vào `docker-compose.staging.yml` | `R3` | Sửa ngay: `${JWT_SIGNING_KEY:?}`; test hồi quy 1 fail → 6 pass |
| 2 | 05/10 | Báo cáo "Tuần 5" của TV1 có p95 < mean ở cả 3 endpoint | `R2` | Ghi vào hồ sơ kiểm chứng; yêu cầu TV1 đo lại bằng k6 |
| 3 | 05/10 | `TUAN_5.md` ghi lại task A7 đã nộp ở `TUAN_4.md`, số test 177 → 183 | `R2` | Ghi vào hồ sơ kiểm chứng; yêu cầu TV1 làm rõ |
| 4 | 05/10 | `render.yaml` ghi trong báo cáo nhưng không có trong commit | `R1` | Ghi vào hồ sơ kiểm chứng |
| 5 | 05/10 | Báo cáo BCP/DR tuyên bố RTO/RPO nhưng không lưu bản ghi chạy | `R2` | Ghi vào hồ sơ kiểm chứng |
| 6 | 05/10 | `dotnet build` fail với `MSB3027` do 2 tiến trình API lab đang giữ DLL | `R1` | Dừng tiến trình rồi build lại — **không phải** lỗi mã nguồn |

---

## 7. Việc còn lại của GĐ3

- [x] `KiemChung_Commit_Week5_TV1.md` — hồ sơ kiểm chứng TV1
- [x] Lỗi khoá JWT — sửa + test hồi quy
- [x] Xung đột `.gitignore` — giải quyết
- [x] Build + test toàn bộ xanh
- [ ] **CI chạy lại cho commit `03564c4`**
- [ ] TV1 đính chính 6 điểm §2.2–2.4
- [ ] **Dừng lại chờ TV1 kiểm tra và quyết định bước tiếp theo**

---

## 8. Chỉ số tài liệu

| Tài liệu | Vai trò |
|---|---|
| [`KiemChung_Commit_Week5_TV1.md`](../misc/KiemChung_Commit_Week5_TV1.md) | Hồ sơ kiểm chứng commit "Tuần 5" của TV1 — **việc cuối của GĐ3** |
| [`PLAN_GIAI_DOAN_3_SUA_LOI.md`](../plan/PLAN_GIAI_DOAN_3_SUA_LOI.md) | Kế hoạch GĐ3 |
| [`BAO_CAO_GIAI_DOAN_1_N2_N4.md`](BAO_CAO_GIAI_DOAN_1_N2_N4.md) | Báo cáo GĐ1 — **đã cập nhật 05/10** |
| [`TRANG_THAI_THUC_HIEN_TUAN_4.md`](../TRANG_THAI_THUC_HIEN_TUAN_4.md) | Trạng thái từng mục — **đã cập nhật 05/10** |
| [`SO_EVIDENCE_TUAN_4.md`](../SO_EVIDENCE_TUAN_4.md) | Sổ evidence — **đã cập nhật 05/10** |
### 5. Fix DbSeeder – không ghi đè URL ảnh người dùng (TV3 §C.1) — 05/10/2026

**Vấn đề:** DbSeeder.SeedAsync dùng điều kiện
img.OriginalUrl.Contains("photo-1546069901-ba9599a7e63c") || !img.OriginalUrl.StartsWith("/images/recipes/")
→ vế thứ hai khớp **mọi** URL không bắt đầu /images/recipes/ (kể cả khoá MinIO/ảnh upload người dùng ecipes/{id}/{uuid}.ext và URL tuyệt đối qua proxy D27) → mỗi lần khởi động lại ghi đè URL ảnh thật thành /images/recipes/{slug}.jpg.

**Fix:** Chỉ coi là ảnh mẫu khi OriginalUrl bắt đầu bằng /images/ **và KHÔNG** bắt đầu bằng /images/recipes/ (các ảnh placeholder dạng /images/categories/..., /images/authors/... nếu có), hoặc rõ ràng là ảnh placeholder Unsplash. Điều kiện mới:
Contains("photo-1546069901-ba9599a7e63c") || (StartsWith("/images/") && !StartsWith("/images/recipes/"))

**Bằng chứng:** 	ests/CulinaryBlog.Tests/DbSeederUserImageUrlTests.cs (mới, 2 test)
- Test 1: Upload ảnh PNG qua API → OriginalUrl có dạng ecipes/{recipeId}/{uuid}.png → chạy DbSeeder.SeedAsync 2 lần → URL **không đổi**.
- Test 2: Giả lập URL tuyệt đối qua proxy http://localhost:5080/api/v1/resources/images/{uuid} → seed lại → URL **không đổi**.
→ Chứng minh đỏ/xanh: tạm thời khôi phục điều kiện cũ → 2/2 FAIL, khôi phục fix → 2/2 PASS.

**File sửa:** src/backend/CulinaryBlog.Infrastructure/Persistence/DbSeeder.cs.

---

### 6. Fix Program.cs --migrate – không nuốt lỗi (TV3 §C.2) — 05/10/2026

**Vấn đề:** if (args.Contains("--migrate")) có 	ry { ...MigrateAsync(); } catch { } rồi vẫn Console.WriteLine("Database migrations applied successfully.") → báo thành công giả khi migration thất bại.

**Fix:** Bỏ catch { }, bắt Exception ex, in Console.Error.WriteLine($"Database migration FAILED: {ex.GetBaseException().Message}"), set Environment.ExitCode = 1, **không** in thông báo thành công khi có lỗi.

**File sửa:** src/backend/CulinaryBlog.API/Program.cs.

---

### 7. Rà soát IMAGE_CONTRACT.md – bổ sung §7c + sửa tham chiếu (TV3 §E.3) — 05/10/2026

**Vấn đề:** TV3 ghi *"chỉ mục thuộc IMAGE_CONTRACT §4"* nhưng §4 là "Quy ước Lỗi & Problem Details (RFC 7807)". Hợp đồng trước đây **không mô tả** ux_recipe_images_one_primary.

**Thay đổi:** Bổ sung §7c. Ràng buộc ở tầng DB: chỉ mục một-ảnh-chính (N2-E4):
- Mô tả rõ tên, UNIQUE, bộ lọc "IsPrimary" = true AND "IsDeleted" = false", nguồn (Configuration + migrations).
- **Giải thích kỹ thuật đúng:** AuditableEntityInterceptor (D08) chuyển Deleted → Modified + IsDeleted = true (soft-delete ở tầng DB), nên MarkImageDeleted vẫn để dòng trong bảng giữ IsPrimary=true → **bộ lọc IsDeleted=false là BẮT BUỘC**, không phải phòng xa.
- Giải thích lý do phải lưu nhiều lần trong transaction (Postgres unique partial không deferrable; EF không đảm bảo thứ tự UPDATE).

**Sửa tham chiếu sai:** ApiExceptionHandler.cs comment cập nhật §4 → §7c.

**File sửa:** docs/IMAGE_CONTRACT.md, src/backend/CulinaryBlog.API/ApiExceptionHandler.cs.

### 8. GĐ2 – Kiểm tra tích hợp tổng hợp FE–BE (05/10/2026)

Thực hiện theo định nghĩa GĐ2: xác nhận chức năng đầy đủ → kiểm tra ổn định → kiểm tra giao tiếp FE–BE.
- Playwright: 26/26 PASS (3 lần liên tiếp), recipe-publish repeat 16/16 PASS.
- Tích hợp: wizard+auth+search+media khớp IMAGE_CONTRACT.md (gồm §7c).
- Fix tích hợp: (a) DbSeeder không ghi đè URL ảnh người dùng – khoá bằng DbSeederUserImageUrlTests 2/2; (b) --migrate không nuốt lỗi (exit 1); (c) làm rõ ux_recipe_images_one_primary + soft-delete qua interceptor.
- Độc lập báo cáo: BAO_CAO_GIAI_DOAN_2_N3.md tóm tắt + dẫn chiếu GĐ1.

---

## 9. GĐ3 – Rà soát và đóng GitHub Issues (05/10/2026)

### 9.1 Tổng quan 5 issues đã sửa và đóng

| # | Issue | Trạng thái sau sửa | Test hồi quy | File chính |
|---|---|---|---|---|
| 20 | [B1] Storage lỗi trả 500 server.error thay vì 503 storage.unavailable | ✅ Đã sửa + đóng | StorageFailureContractTests | MinioStorageService.cs (GuardAsync → AppException 503) |
| 21 | [B2] API khởi động thành công khi thiếu cấu hình storage | ✅ Đã sửa + đóng | StorageFailureContractTests (B2 fail-fast) | Program.cs:156 (validate AccessKey/SecretKey) |
| 22 | [B4] /health trả Healthy dù credential sai (chỉ TCP probe) | ✅ Đã sửa + đóng | HealthTests | ObjectStorageCredentialProbe.cs + Health.cs (StatObject probe) |
| 23 | [B5] Wizard không xem được ảnh Draft (img không gửi Bearer) | ✅ Đã sửa + đóng | RecipeImagePresignedB5Tests + RecipeDetailPresignedB5Tests | FE ImagesStep.tsx (presignedUrl + isPresignedStale) + IMAGE_CONTRACT.md §7b |
| 24 | [B6] Không có Admin nên không test được /hangfire | ✅ Đã sửa + đóng | PromoteAdminCommandTests + ImageConcurrencyE7Tests (E7 hangfire) | DbSeeder.cs (masterchef@culinary.local) + PromoteAdminCommand CLI |

### 9.2 Chi tiết sửa

**#20 B1 – Storage 503:**
- Fix: MinioStorageService.GuardAsync bọc MinioException / HttpRequestException / SocketException / IOException / TimeoutException → AppException(503, "storage.unavailable", ...). Không bọc ObjectNotFoundException (→ 404) và OperationCanceledException khi ct.IsCancellationRequested (tránh báo 503 nhầm).
- Test: StorageFailureContractTests (7 test case: upload → 503, proxy → 503, validator → 400 file.too_large, recipe → 404, ...).

**#21 B2 – Fail-fast:**
- Fix: Program.cs:156 validate AccessKey/SecretKey lúc khởi động; rỗng → throw fail-fast (trước đó chỉ log + khởi động tiếp).
- Test: StorageFailureContractTests — B2 "fail-fast lúc khởi động".

**#22 B4 – Health check credential:**
- Fix: Thêm ObjectStorageCredentialProbe (Infrastructure) — StatObject trên key ProbeKey; phân biệt AccessDeniedException (credential sai → Unhealthy), BucketNotFoundException → Unhealthy, ObjectNotFoundException (404 sau xác thực) → Healthy, lỗi mạng → Unhealthy. Đăng ký trong Program.cs:218.
- Test: HealthTests — /health/ready có check object-storage; port mở + AccessKey sai → 503 Unhealthy; credential đúng → Healthy.

**#23 B5 – Draft images presigned:**
- Fix: Draft images hiển thị qua presigned URL (PA-3) thay vì proxy (trả 403 vì thẻ <img> không gửi Bearer). FE imageSrc() ưu tiên presignedUrl; isPresignedStale() phát hiện URL hết hạn → hiện nút "Tải lại liên kết ảnh" (PATCH lấy URL mới).
- Hợp đồng: docs/IMAGE_CONTRACT.md §7b — trường presignedUrl (nullable).
- Test: RecipeImagePresignedB5Tests + RecipeDetailPresignedB5Tests — Draft owner/Admin có URL, Published → null, signer fail-soft, URL hết hạn 15 phút, không lọt log.

**#24 B6 – Admin user:**
- Fix: DbSeeder seed user masterchef@culinary.local role Admin, password User@123456. Thêm CLI --promote-admin nâng tài khoản hiện có lên Admin.
- Test: PromoteAdminCommandTests (ExtractEmail parse, promote role) + ImageConcurrencyE7Tests (E7 Hangfire dashboard không public).

### 9.3 Kết luận GĐ3
- **5/5 issues đã sửa, có test hồi quy, đã đóng trên GitHub.**
- Các fix đều được khoá bằng test tích hợp/unit; không phát hiện issue còn sót.
- **GĐ3 hoàn tất**: kết hợp kết quả GĐ1 (N2/N4 evidence, 426/426, 96.31%) + GĐ2 (integration FE–BE 26/26, contract khớp) + đóng 5 issues.

---

**Báo cáo GĐ1:** BAO_CAO_GIAI_DOAN_1_N2_N4.md
**Báo cáo GĐ2:** BAO_CAO_GIAI_DOAN_2_N3.md
**Báo cáo GĐ3:** BAO_CAO_GIAI_DOAN_3_SUA_LOI.md (file này)
**PR:** https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/pull/29
