# Kiểm chứng commit "Tuần 5" của TV1 (21aa722) — ĐÁNH DẤU KHÔNG ĐÁNG TIN

- **Đối tượng kiểm chứng**: commit `21aa722` — `feat(week5): complete Week 5 deliverables for TV1 (Nguyen Thanh Tam)`
- **Người kiểm chứng**: TV4 (Nguyễn Hữu Trung Sơn)
- **Ngày kiểm chứng**: 05/10/2026
- **Cách kiểm chứng**: đọc diff của commit trên nhánh `main`, đối chiếu với nội dung báo cáo, đối chiếu với trạng thái thực tế của nhánh tuần, chạy lại build + test.
- **Kết luận**: **KHÔNG ĐÁNG TIN — cần kiểm tra lại toàn bộ trước khi dùng làm căn cứ nghiệm thu.**

---

## 0. Kết luận ngắn

Commit được merge vào nhánh tuần để không mất code của TV1, nhưng **nội dung báo cáo đi kèm không đủ cơ sở để tin**. Ba nhóm vấn đề:

1. **Báo cáo tự mâu thuẫn với chính nó** — số liệu đo hiệu năng vi phạm bất biến toán học.
2. **Báo cáo ghi "Hoàn thành" nhưng không có minh chứng chạy được trong repo** — không có log, không có dump, không có output k6/ab.
3. **Báo cáo nhận công đã làm của người khác và ghi nhãn sai tuần.**

Chi tiết ở mục 1–3.

---

## 1. Sai lệch đã xác nhận được

### 1.1 Số liệu hiệu năng vi phạm bất biến toán học (nghiêm trọng)

Báo cáo ghi:

| Endpoint | Throughput | mean latency | p95 latency |
|---|---|---|---|
| `GET /api/v1/recipes?pageSize=10` | 477.19 req/s | 20.95 ms | **7 ms** |
| `GET /api/v1/recipes/search?q=pho` | 1,039.88 req/s | 9.61 ms | **6 ms** |
| `GET /health/live` | 4,716.54 req/s | 2.12 ms | **2 ms** |

Vấn đề: **p95 không bao giờ nhỏ hơn mean**. Theo định nghĩa, p95 là giá trị mà 95% số mẫu ≤ nó, nên p95 luôn ≥ mean. Cả ba dòng đều có p95 < mean ⇒ số liệu không thể đo được như ghi, hoặc cột p95 bị điền nhầm (ví dụ lấy p50/p2).

Hệ quả: toàn bộ kết luận "độ trễ p95 vượt xa chỉ tiêu NFR (p95 < 200ms)" **không có giá trị kiểm chứng** cho tới khi đo lại và kèm bản ghi lệnh.

### 1.2 `render.yaml` được nhận công nhưng không tồn tại trong commit

Báo cáo mục "Bản ghi 1" liệt kê đầu ra:
- `render.yaml`: *"Cập nhật hạng tầng Cloud Staging tự động cho Render.com"*

Kiểm tra `git show --name-only 21aa722`: **không có `render.yaml` trong commit.** Đầu ra bị ghi nhận nhưng không tồn tại.

### 1.3 Nhãn tuần sai: thực tế là Tuần 4

- Báo cáo của commit này tự ghi "Tuần 5", ngày 05/10/2026.
- Nhánh tuần đã có sẵn `docs/evidence/TV1/TUAN_4.md`, cũng của TV1, cùng tác giả, ghi rõ **"Tuần / Người / Task: 4"**, task **A6, A7**, kết quả **177/177 tests pass**.
- Commit `21aa722` lại ghi task **A7** ở "Tuần 5".

⇒ **A7 đã được nộp ở Tuần 4 với 177/177 test. Commit này ghi lại đúng task A7 nhưng đổi nhãn thành Tuần 5 và đổi số test thành 183/183.** Hai báo cáo cùng tác giả, cùng task, cùng kỳ, nhưng số liệu khác nhau. Cần TV1 làm rõ cái nào đúng.

### 1.4 Tổng số test không khớp thực tế nhánh tuần

| Nguồn | Số test |
|---|---|
| `TUAN_4.md` (TV1, tuần 4) | 177 |
| `21aa722` (`TUAN_5.md`) | 183 (178 + 5 `ConcurrencySpike`) |
| **Thực tế sau khi merge vào nhánh tuần** | **316 (`CulinaryBlog.Tests`) + 5 (`ConcurrencySpike`) = 321** |

Con số 183 được đưa ra **sau khi nhánh tuần đã có 316 test**. Báo cáo không nêu việc chạy toàn bộ solution, và không giải thích vì sao số test giảm. Kết luận "183/183 xanh" không đại diện cho trạng thái repo khi merge.

### 1.5 Báo cáo nhận công đề xuất của TV4

Mục "Bản ghi 5" ghi: *"Đề xuất B1: Thống nhất chuyển mã lỗi storage không khả dụng từ 500 sang HTTP 503 `storage.unavailable`"* và ghi đây là nội dung *"Rà soát các đề xuất kỹ thuật B1, B2, B3 từ TV4"*.

Việc đổi `500 → 503 storage.unavailable` **đã được TV4 triển khai và commit trước đó** (`MinioStorageService` dùng `Lazy<IMinioClient>`, `StorageFailureContractTests` 9/9). Đề xuất B1 đã thành **hiện thực hoá của TV4**, không phải thành tự thân xác nhận của TV1 trong tuần này.

### 1.6 Người xác nhận là chính người thực hiện

Cả 5 bản ghi đều ghi:
> **Reviewer xác nhận**: Nguyễn Thanh Tâm (Nhóm trưởng)

Người thực hiện và người xác nhận là **cùng một người**. Đây không phải nghiệm thu độc lập, không đạt yêu cầu tách vai trong bản ghi minh chứng.

### 1.7 ⛔ SAI LỆCH MỚI (phát hiện 05/10 khi kiểm tra CI): test E2E Scenario 5 phụ thuộc dữ liệu seed mà CI không có

`CommitWeek5VerificationTests` của commit `21aa722` báo "321 test, xanh". Nhưng khi merge vào
nhánh tuần và chạy trên CI thật, test này **đỏ** — và đây là nguyên nhân khiến job
`Backend week 1` đỏ ở commit `08052fc`:

| Mục | Thực tế |
|---|---|
| Test đỏ | `Week5StagingAndE2ETests.E2E_Scenario_5_FTS_Vietnamese_Search_AND_Filter_And_Draft_Isolation` |
| Run | `37316696268` (`08052fc`) — lần thứ 4 liên tiếp `Backend week 1` đỏ |
| Mã bị đỏ | `Assert.NotEmpty(searchData.Data)` và `Assert.Contains(..., r => r.Title.Contains("Canh"))` |
| Test **không** tự tạo dữ liệu | `Week5StagingAndE2ETests.cs:245-265` chỉ `GET /api/v1/recipes/search?q=canh` rồi kỳ vọng có `"Canh chua cá lóc"` |
| Dữ liệu đó **có** trong seed | `RecipeSeedData.cs:253-256` — `Title: "Canh Chua Cá Lóc Dương"`, slug `canh-chua-ca-loc-dong` |
| Nhưng seed **không chạy** khi test | `Program.cs:343` — `if (!args.Contains("--no-auto-migrate") && !builder.Environment.IsEnvironment("Testing"))` ⇒ môi trường `Testing` **bỏ qua** cả `Migrate` lẫn `DbSeeder.SeedAsync` |
| Test harness chỉ migrate | `AuthTests.cs:44-64` — `EnsureMigrated()` gọi `Database.Migrate()`, **không** seed |

⇒ Test chỉ xanh khi database **đã có sẵn dữ liệu seed từ trước** (máy dev). Trên CI với Postgres
container sạch, test **luôn đỏ**. Đây là test **không tự chứa**, phụ thuộc trạng thái ngoài —
vi phạm nguyên tắc test phải tạo dữ liệu của riêng nó.

⚠️ **Hệ quả nghiêm trọng hơn:** "321 test xanh" trong báo cáo của TV1 là **kết quả chạy trên máy
dev có sẵn seed**, không phải trên môi trường sạch. Con số xanh đó **không tái lập được**.

🛑 **Đề nghị TV1 tự sửa:** hoặc cho `Scenario_5` tự tạo công thức riêng, hoặc `ApiFactory` seed
trong `Testing`. **TV4 không tự sửa test của thành viên khác** — cùng nguyên tắc với việc không
đính chính báo cáo của TV1.

---

## 2. Vấn đề "ghi Hoàn thành nhưng không có minh chứng"

Các mục dưới đây **không khẳng định là sai**, mà là **không thể kiểm chứng** vì repo không lưu bằng chứng chạy.

| Mục | Báo cáo ghi | Minh chứng trong repo |
|---|---|---|
| BCP/DR drill | dump `culinary_blog_20261005_115153.sql.gz` (272K), phục hồi 1.8s, "25 Categories và 100 Recipes", RTO < 2 phút, RPO = 0 | **Không có** file `.sql.gz`, không có log diễn tập. `backups/database/` chỉ có `.gitkeep` (và giờ đã được `.gitignore` loại trừ). |
| RTO/RPO | "RTO < 2 phút, RPO = 0" | Không có đo lường, không có mốc thời gian ghi lại, không có quy trình nào xác định RPO. |
| NFR hiệu năng | p95 theo bảng ở 1.1 | Không có output k6 hay ab trong repo. File `tests/k6/auth-profile-load.js` có **script nhưng không có kết quả chạy**. |
| Số liệu DB sau phục hồi | "25 Categories và 100 Recipes" | Script `restore-db.sh` có thực hiện phép đếm, nhưng **không có bản ghi kết quả thật**. Lưu ý: CSDL thực tế của nhánh tuần có **101** recipe (xem `Tuan04/logs/lab_l5_db.txt`), không phải 100. |
| "183/183 test" | xem 1.4 | Chạy lại sau merge cho kết quả **321 test, xanh** — khác số báo cáo. |
| 5 kịch bản E2E | 5 test trong `Week5StagingAndE2ETests.cs` | File test **có thật**, 5 `[Fact]`, dùng `IClassFixture<ApiFactory>` + `HttpClient` thật. Phần này **có cơ sở**. Điểm chưa rõ: `ApiFactory` có dựng PostgreSQL/Redis thật hay in-memory — cần TV1 nói rõ môi trường chạy. |

Phần script BCP/DR tự thân có vẻ hợp lý về mặt cấu trúc: `backup-db.sh` dùng `set -euo pipefail`, `pg_dump | gzip`, `sha256sum` **tạo sau khi** ghi file backup (đúng thứ tự), `restore-db.sh` đối chiếu checksum trước khi nạp và đếm bản ghi sau khi nạp. **Vấn đề không nằm ở logic script, mà ở chỗ không có bản ghi chạy để chứng minh chúng đã chạy và đạt.**

---

## 3. Lỗi kỹ thuật đã phát hiện và đã sửa trong lúc kiểm chứng

### 3.1 Khoá ký JWT đã thu hồi bị hardcode trong compose staging (đã sửa)

Chạy test trên nhánh tuần sau khi merge cho kết quả **1 fail** — nguyên nhân **không nằm ở merge conflict mà là lỗi bảo mật thật do commit `21aa722` mang vào**:

```
Failed CulinaryBlog.Tests.JwtSigningKeyNotCommittedTests.Revoked_key_appears_only_in_the_blocklist
Khoá dev đã thu hồi chỉ được xuất hiện trong JwtService.cs (danh sách chặn).
Xuất hiện ngoài danh sách ở: docker-compose.staging.yml
```

`docker-compose.staging.yml:81` (do `21aa722` thêm) đặt cứng đúng khoá mà `JwtSettings.Validate()` đã thu hồi:

```yaml
Jwt__SigningKey: "<khoá dev đã bị thu hồi — xem danh sách chặn trong JwtService.cs>"
```

> ⛔ **Không chép lại giá trị khoá thật vào tài liệu.** Hồ sơ này là file text mà test
> `JwtSigningKeyNotCommittedTests.Revoked_key_appears_only_in_the_blocklist` quét; danh sách cho
> phép chỉ gồm `JwtService.cs` và chính file test. Chép khoá vào đây làm CI đỏ y hệt lỗi gốc —
> **đã xảy ra và đã được sửa 05/10**, xem [`BAO_CAO_GIAI_DOAN_3_SUA_LOI.md`](BAO_CAO_GIAI_DOAN_3_SUA_LOI.md).

Ý nghĩa: nếu ai đó chạy staging bằng file này, toàn bộ JWT của staging được ký bằng khoá đã bị cấm trong codebase — và khoá đó đang nằm công khai trong git. Đây đúng là vi phạm bất biến bảo mật mà test của nhóm đã viết để chặn.

**Đã sửa**: thay bằng bắt buộc đọc từ biến môi trường, không có giá trị mặc định, để thiếu thì compose fail rõ ràng:

```yaml
Jwt__SigningKey: ${JWT_SIGNING_KEY:?chua dat JWT_SIGNING_KEY - sinh bang openssl rand -base64 48}
```

Đã kiểm chứng cả hai chiều: thiếu biến → `docker compose config` exit 1 kèm thông báo; có biến → exit 0. Vẫn còn giá trị trong `.env.example` là placeholder ở dạng chữ, không phải khoá dùng được.

### 3.2 Xung đột `.gitignore` khi merge (đã giải quyết)

Chỉ có **một** xung đột, tại `.gitignore`: nhánh tuần thêm quy tắc loại artifact Playwright + `run.log`, còn `21aa722` thêm quy tắc loại `backups/**/*.sql*` và `*.tar*`. Hai bên không giao nhau nên **giữ cả hai**. Đã kiểm chứng: file `.sql.gz` trong `backups/` bị ignore, còn `backups/database/.gitkeep` vẫn được theo dõi.

---

## 4. Kết quả kiểm tra sau khi merge

| Hạng mục | Kết quả |
|---|---|
| `dotnet build CulinaryBlog.sln -c Release` | Build succeeded, **0 Warning, 0 Error** |
| `dotnet test CulinaryBlog.sln` | **316/316** `CulinaryBlog.Tests` + **5/5** `ConcurrencySpike`, Skipped 0 |
| Trước khi sửa khoá JWT | 315 pass / **1 fail** |
| `docker compose -f docker-compose.staging.yml config` | exit 0 khi có `JWT_SIGNING_KEY`, exit 1 kèm thông báo khi thiếu |
| `docs/evidence/TV4/Tuan04/SOK_LAB_L5.md` + 2 log L5 | nguyên vẹn sau merge |
| `docs/evidence/TV4/Tuan03/SOK_LAB_L4.md` | nguyên vẹn sau merge |
| File bảo vệ `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md` | nguyên vẹn, hash `93661aa13e6fa2e081b9a89b2517eca5d0df083c` không đổi |

---

## 5. Việc cần làm lại với TV1

1. **Đo lại hiệu năng** và kèm bản ghi lệnh + output thô. Dùng `k6` thay vì `ab` vì `ab` không xuất p95 đáng tin. Kiểm tra p95 ≥ mean trước khi nộp.
2. **Chạy lại BCP/DR drill** và lưu log diễn tập vào `docs/evidence/TV1/logs/`, gồm checksum, thời gian nạp, số bản ghi đếm được.
3. **Giải thích mâu thuẫn A7 tuần 4 vs tuần 5** và số test 177 → 183 → 321. Làm rõ tuần nào là tuần nào thật.
4. **Bổ sung hoặc xoá mục `render.yaml`** trong báo cáo cho khớp thực tế.
5. **Nêu rõ môi trường chạy** của `Week5StagingAndE2ETests`: PostgreSQL/Redis thật hay in-memory.
6. **Tách vai trò reviewer**: người xác nhận phải khác người thực hiện, hoặc ghi rõ đây là tự nghiệm thu nội bộ.
7. **Đính chính phần công nhận B1/B2/B3**: nêu rõ đã hiện thực hoá bởi TV4 từ tuần trước.
8. **Chốt số liệu ảnh**: repo có 100 file ảnh / 29 MB — khớp 29MB, nhưng CSDL có 101 recipe nên cần giải thích ảnh recipe nào không có file.

---

## 6. Ghi chú về phạm vi

Tài liệu này **không xoá hay sửa nội dung báo cáo của TV1**. `TUAN_5.md`, `BAO_CAO_LAB_05.md` và các file `.docx` giữ nguyên như commit gốc, để TV1 tự đính chính theo mục 5. Hồ sơ này chỉ ghi nhận kết quả kiểm chứng của TV4.

Lỗi khoá JWT tại mục 3.1 là lỗi kỹ thuật trong file cấu hình nên đã sửa luôn để nhánh tuần xanh test. Các sai lệch còn lại nằm trong báo cáo, không tự sửa được vì phải giữ nguyên bản ghi minh chứng gốc của TV1.