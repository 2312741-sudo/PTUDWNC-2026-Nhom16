# BÁO CÁO KIỂM CHỨNG THỰC TẾ (LAB) — TV4 TUẦN 4

| Mục | Nội dung |
|---|---|
| Người thực hiện | Nguyễn Hữu Trung Sơn (TV4) |
| Ngày | 03/10/2026 |
| Nơi chạy | Bản kiểm tra cục bộ của TV4 (local, **không push**, không thuộc dự án) |
| Nhánh gốc | `2312739_NHTSon_D5-D6-D7` |
| Mục đích | Kiểm chứng thực tế các lỗi đã ghi trong `BAO_CAO_LOI_TUAN_4_TV4.md` và các yêu cầu trong `KE_HOACH_TUAN_4_TV4_V2.md`, thay vì chỉ đọc code |
| Kết luận | **3 lỗi được chứng minh + sửa thành công trên bản cục bộ**; 4 lỗi xác nhận/sửa một phần; 3 lỗi còn mở |

> [!WARNING]
> **📌 Ảnh chụp kiểm chứng lúc làm 03/10 — trạng thái tích hợp đã kiểm chứng lại 05/10, rà lại 07/10.**
>
> | Việc | Kết quả |
> |---|---|
> | 3 lỗi "đã sửa thành công" | ⛔ 05/10: **bản vá chưa có trong dự án** (chỉ cục bộ, không push); `RecipeImageConfiguration.cs` vẫn thiếu `ValueGeneratedNever()` ⇒ BUG-W4-01 **còn mở**. → 07/10: `BUG-W4-01` **đã vào `main`** (PR #29 `c624b9f`); `02`/`03` chưa có bản vá trong repo → **sửa trực tiếp tuần 5** (W5-10) |
>
> ⛔ **Không nhầm với 3 lỗi đã đóng ở GĐ1** (DB chết trả `500`, DB chết lúc khởi động giết tiến
> trình, `GET /recipes/{slug}` trả `500` khi thiếu credential storage) — ba lỗi đó **đã merge**
> ở commit `8d9d62b`. Chi tiết: [`BAO_CAO_LOI_TUAN_4_TV4.md`](BAO_CAO_LOI_TUAN_4_TV4.md) và
> [`BAO_CAO_GIAI_DOAN_1_N2_N4.md`](BAO_CAO_GIAI_DOAN_1_N2_N4.md).

---

## 1. Môi trường kiểm thử

| Thành phần | Phiên bản |
|---|---|
| .NET SDK | 10.0.401 |
| Docker / Compose | 29.6.2 / v5.3.1 |
| PostgreSQL, Redis, RustFS (S3), MailHog, Seq, OpenTelemetry | qua `docker-compose.dev.yml` |
| Nginx | container, publish `localhost:8080` |
| API instances | 2 tiến trình host: `localhost:5080` (InstanceId `MSI`), `localhost:5081` (InstanceId `NODE-B`) |

Lưu ý: `docker-compose.dev.yml` **không có API service**, nên phải chạy 2 tiến trình `.NET` thủ công trên host mới tái hiện được mô hình multi-instance mà `A3`/`A5` yêu cầu.

---

## 2. Tổng hợp kết quả

| Mã | Vấn đề | Trạng thái trước | Kết quả lab | Bằng chứng |
|---|---|---|---|---|
| BUG-W4-01 | `RecipeImage` sinh `UPDATE` thay vì `INSERT` | Chỉ đọc code, nghi ngờ | **Tái hiện + sửa** | Test RED→GREEN, Postgres thật |
| BUG-W4-02 | Scanner bỏ sót secret của dự án | Chỉ đọc code | **Tái hiện + sửa** | Secret riêng bị bỏ sót |
| BUG-W4-03 | Rate limiter dùng chung bucket | Chỉ đọc code | **Tái hiện + sửa** | 2 IP khác nhau, 1 bị 429 |
| BUG-W4-04 | README số test cũ | Số liệu mâu thuẫn | **Sửa số liệu** | `dotnet test` đo thực tế |
| BUG-W4-05 | CHANGELOG thiếu Tuần 2–4 | Chỉ có `0.1.0` | **Sửa** | Thêm mục `Unreleased` |
| BUG-W4-06 | Soft delete không nhất quán | Đề xuất xoá nhầm code | **Phải đính chính** | Recipe soft-delete **đang dùng thật** |
| BUG-W4-07 | Tài liệu kỹ thuật cũ | Chỉ đọc code | **Chưa kiểm thử sâu** | — |
| BUG-W4-09 | Sitemap rỗng khi tranh lock | "Luôn trả rỗng" | **Phải đính chính** | Request bình thường trả XML hợp lệ |
| BUG-W4-10 | Snapshot lệch với model | Chỉ đọc code | **Xác nhận + biết cách sửa** | `dotnet ef` sinh migration rỗng |
| A3 | Nginx multi-upstream + failover | 1 upstream, không retry | **Đạt (trừ TLS)** | Round-robin, failover, 502, tự hồi phục |
| A5 | `docker compose --profile` | Không có `profiles:` | **Chưa đạt** | 7 service, `container_name` cứng |
| A6 | Tài liệu cập nhật | README/CHANGELOG cũ | **Đạt** | Số liệu + changelog mới |
| A2 | Code chết | Nghi ngờ | **Một phần** | `Category.MarkDeleted` chỉ dùng trong test |

---

## 3. BẰNG CHỨNG CHI TIẾT

### 3.1 BUG-W4-01 — `RecipeImage` không được sinh `INSERT`

**Nguyên nhân.** `RecipeImageConfiguration` thiếu `ValueGeneratedNever()` trên `Id`, trong khi `RecipeIngredient` và `RecipeStep` đều có. Vì `Id` được sinh ở tầng ứng dụng, EF dựng câu lệnh `UPDATE` → `DbUpdateConcurrencyException` ("1 row affected, 0 expected").

**Tái hiện (RED).** Test mới `tests/CulinaryBlog.Tests/LabRecipeImageValueGeneratedTests.cs`, chạy trên PostgreSQL thật:

```
Metadata: RecipeImage.Id ValueGenerated != Never  -> FAIL
SaveChanges: state = Modified (mong doi = Added) -> FAIL
SaveChanges: DbUpdateConcurrencyException (1 row affected, 0) -> FAIL
```

Bộ test cũ `RecipeImageTests.cs` **không phát hiện được** vì chỉ dùng fake list, không chạy EF/Postgres.

**Sửa trên bản cục bộ (chưa vào repo)** — `src/backend/CulinaryBlog.Infrastructure/Persistence/Configurations/RecipeImageConfiguration.cs`:

```csharp
b.Property(i => i.Id).ValueGeneratedNever();
```

**Xác nhận (GREEN).**

```
Metadata: Never -> PASS
SaveChanges: state = Added -> PASS
Chen 1 ban ghi -> 1 dong trong DB, RowVersion 16 byte -> PASS
```

**Phát hiện bổ sung — `RowVersion` vẫn cần interceptor.** Bản vá cấu hình không tự sinh `RowVersion`: bỏ interceptor thì `RowVersion` là mảng rỗng (0 byte). Chỉ khi đi qua DI stack thật, `AuditableEntityInterceptor` mới gán 16 byte và điền `CreatedAt`. Vì vậy **không được** coi là đã thay thế được interceptor.

**Kết quả.** `214/214` pass (`209` + `5`) trên bản cục bộ — số không dùng làm baseline (xem §3.8), test hồi quy không có trong repo; build sạch `0 warning / 0 error`.

### 3.2 BUG-W4-10 — Snapshot lệch (đi kèm BUG-W4-01)

`src/backend/CulinaryBlog.Infrastructure/Migrations/AuthDbContextModelSnapshot.cs:183-185` còn `.ValueGeneratedOnAdd()`, trong khi `RecipeIngredient` (`:241-242`) và `RecipeStep` (`:289-290`) không có.

- `dotnet ef migrations has-pending-model-changes` → không phát hiện thay đổi.
- `dotnet ef migrations add LabProbe_RecipeImageIdNever` → tạo `Up`/`Down` **rỗng**, chỉ xoá đúng annotation trong snapshot.

**Kết luận.** Có thể đồng bộ snapshot bằng một migration no-op, nhưng migration đó **không có tác dụng lên database** — đây là điểm cần nói rõ khi báo cáo, tránh gây hiểu nhầm rằng đã sửa được lỗi runtime. Probe đã được xoá; lab không giữ migration no-op.

### 3.3 BUG-W4-02 — Scanner bỏ sót secret

**Tái hiện (RED).** Với `deploy/scan-secrets.sh` bản gốc:

| Nội dung thử | Kết quả mong đợi | Kết quả thực tế |
|---|---|---|
| JWT signing key riêng của dự án trong `appsettings.json` | Báo đỏ | **`OK` — bỏ sót** |
| Biến secret literal trong `.github/workflows/backend.yml` | Báo đỏ | **`OK` — bỏ sót** |
| Token mẫu `ghp_...` | Báo đỏ | Báo đỏ |

Nghĩa là scanner chỉ bắt được mẫu nổi tiết, không bắt được bí mật do chính dự án đặt ra.

**Sửa trên bản cục bộ (chưa vào repo).**
- `deploy/scan-secrets.sh`: thêm `check_json_config_secrets` (khoá nhạy cảm trong JSON, chuỗi kết nối có `Password=`), `check_workflow_env_secrets` (bỏ qua giá trị dạng `${{ secrets.* }}`), và cơ chế allowlist.
- `deploy/secret-scan-allowlist.txt`: allowlist cho credential dev/CI, **kèm lý do từng dòng**.
- Ghi rõ cơ chế allowlist dùng Bash associative array, không dùng `grep`: Git Bash `grep` bị `SIGABRT` (rc 134) khi đọc allowlist tiếng Việt.

**Xác nhận (GREEN).**

```
Repo sạch                              -> exit 0, "OK"
Chèn secret riêng vào appsettings.json
và biến secret literal vào backend.yml -> exit 1, bat dung 2 vi tri
```

Ngoài ra gia cố còn phát hiện thêm file cần allowlist: `src/backend/CulinaryBlog.Infrastructure/appsettings.Development.json` (credential kết nối dev) — scanner cũ không nhìn thấy file này.

**Lưu ý an toàn.** Hai bản `appsettings*.json` đã được đặt `Jwt:SigningKey` thành chuỗi rỗng. Không cần thêm validation mới vì `JwtService.Validate()` đã kiểm tra độ dài UTF-8 và được gọi cả lúc dựng singleton lẫn khi resolve. Việc xoá `Jwt__SigningKey` trong `.env` chỉ làm app **fail sớm khi thiếu cấu hình**, đúng như thiết kế.

### 3.4 BUG-W4-03 — Rate limiter dùng chung bucket cho mọi khách

Đây là lỗi nghiêm trọng nhất trong đợt kiểm chứng: **một khách hàng có thể khiến toàn bộ người dùng khác không đăng nhập được.**

**Bằng chứng RED (1 upstream, qua nginx).** Nginx có đặt `X-Forwarded-For`, nhưng app không gọi `UseForwardedHeaders`, nên `RemoteIpAddress` là IP container nginx và mọi khách rơi vào cùng một khoá:

```
Khách A, X-Forwarded-For: 1.1.1.1 -> 401 x10, roi 429 x2
Khách B, X-Forwarded-For: 2.2.2.2 -> 429 x4   (0 lan 401)
Khách C, X-Forwarded-For: 3.3.3.3 -> 429 x4
```

Khách B và C có IP hoàn toàn khác, không gửi một request 401 nào thành công — vì khoá tính theo IP của proxy. Lưu ý: nếu chạy thí nghiệm này khi có 2 upstream, kết quả sẽ gây hiểu nhầm (mỗi node một khoá riêng nên không thấy 429), nên phải cô lập còn 1 node.

**Hạn mức đơn vị.** Gọi thẳng node `5080`: 10 request đầu `401`, từ request 11 trở đi `429` kèm `Retry-After: 60`. Khớp với chính sách 10 lượt/phút.

**Phát hiện mới — hạn mức nhân theo số instance.** Qua nginx với 2 upstream, 12 request từ cùng một IP đều `401`; 6 request tiếp theo vẫn `401`. Nguyên nhân: bộ đếm là in-memory **trên từng tiến trình**, mỗi node một bộ đếm riêng, nên hạn mức thực tế bằng `10 × số instance`. Cấu hình `proxy_next_upstream` mới thêm ở đây cũng làm nhiều request hơn rơi vào node còn sống. **Đây là rủi ro riêng, không nằm trong BUG-W4-03, và cần ghi nhận vào NFR về rate limiting phân tán.**

**Sửa trên bản cục bộ (chưa vào repo)** — `src/backend/CulinaryBlog.API/Program.cs`, đặt **trước** `UseRateLimiter()`:

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownIPNetworks = { new System.Net.IPNetwork(System.Net.IPAddress.Parse("172.16.0.0"), 12) },
    KnownProxies = { System.Net.IPAddress.Loopback }
});
```

Phải khai báo `KnownIPNetworks` rõ ràng; nếu bỏ trống thì header từ proxy trong Docker bị bỏ qua và lỗi quay lại.

**Bằng chứng GREEN (cùng cấu hình, 1 upstream).**

```
Khách A (1.1.1.1) -> 401 x10, roi 429 x2
Khách B (2.2.2.2) -> 401 x4
Khách C (3.3.3.3) -> 401 x4
```

Mỗi IP đã có khoá riêng. Full test sau thay đổi trên bản cục bộ: `214/214` pass — không hồi quy (test hồi quy không có trong repo — xem §3.8/§4).

### 3.5 A3 — Nginx multi-instance, failover

| Tình huống | Kết quả quan sát |
|---|---|
| Round-robin 6 request qua nginx | `MSI, NODE-B, MSI, NODE-B, MSI, NODE-B` — phân bổ đều |
| Giết `NODE-B` | 10/10 request `200` qua `MSI` |
| Giết cả 2 node | 4/4 request `502` (xác nhận nginx là cổng vào thật) |
| Khởi động lại 1 node, **không reload nginx** | Sau `fail_timeout=10s`, 4/4 request `200` |

Nginx tự đánh giá lại sức khoẻ upstream và phục hồi mà không cần khởi động lại. `nginx -t` hợp lệ.

**Chưa đạt:** TLS vẫn chỉ nằm trong khối comment. Chưa sinh chứng thư CA, chưa bật `listen 443 ssl`. Phần này để ngỏ, không tính là hoàn thành.

### 3.6 BUG-W4-09 — Sitemap (đính chính)

`src/backend/CulinaryBlog.API/Program.cs:470-480` có nhánh: khi không đọc được cache **và** không acquire được lock thì trả `string.Empty` với HTTP 200 `application/xml` — client nhận sitemap rỗng mà không có tín hiệu lỗi.

**Đính chính mô tả trong báo cáo gốc.** Báo cáo gốc nói endpoint "trả 200 rỗng". Request bình thường trong lab trả **XML hợp lệ dài 28.086 byte** vì Redis đã có cache. Vì vậy lỗi **không phải trạng thái thường**, mà là **điều kiện cạnh** khi cache rỗng và tranh lock.

**Chưa xác minh:** chưa tái hiện được đúng điều kiện cạnh (cần giành lock trong lúc cache rỗng). Giữ trạng thái **đã xác nhận luồng code, chưa tái hiện thực tế**.

### 3.7 BUG-W4-06 — Soft delete (đính chính đáng chú ý)

Báo cáo gốc đề xuất xoá code soft delete. Kiểm tra thực tế cho thấy đề xuất đó **sẽ phá vỡ tính năng đang chạy**:

- Global query filter còn sống: `src/backend/CulinaryBlog.Infrastructure/IdentityModel.cs:93-103`.
- Recipe soft-delete **đang được production dùng**: `src/backend/CulinaryBlog.Application/Recipes.cs:655` và `src/backend/CulinaryBlog.Infrastructure/RecipeRepository.cs:192`. **Phải giữ nguyên.**
- `CategoryTests.cs:146` đặt tên `soft_deletes_when_empty` nhưng thân test và comment lại xác nhận **hard delete** (`:160`, `:164`). Đây là lỗi tên test gây hiểu nhầm.
- `Category.MarkDeleted` (`src/backend/CulinaryBlog.Domain/Category.cs:42`) chỉ được gọi trong `CategoryTests.cs:76`, không thấy ở production.

**Phạm vi đúng** của lỗi chỉ là: tên test sai, `Category.MarkDeleted` không dùng, và thiếu ADR giải thích quy ước. **Không đụng vào soft delete của Recipe.**

**Chưa làm:** chưa đọc `src/backend/CulinaryBlog.Infrastructure/CategoryRepository.cs` để xác nhận nhánh xoá thực tế.

### 3.8 BUG-W4-04 / BUG-W4-05 / A6 — Tài liệu

**Số liệu thực đo.** `dotnet test CulinaryBlog.sln -c Release` trên `2312739`: `205` + `5` = **`210/210`**, `Skipped=0`. Số `214/214` (209 + 5) là của bản kiểm tra cục bộ; đối chiếu 07/10 **không có 4 test hồi quy đó trong repo** → không dùng.

**Đã sửa trên bản cục bộ (chưa vào repo).**
- `README.md`: `177/177` → `210/210`; `172/172` → `210/210` (cả câu chữ lẫn khối output mẫu).
- Giữ nguyên hai ghi chú lịch sử có ngày (`30/09/2026`) vì chúng là ảnh chụp trạng thái tại thời điểm đó, không phải số liệu hiện hành.
- Thêm ghi chú làm rõ cột `24 / 24 (K01–K24)` là **số tự khai**, chưa có rubric máy kiểm chứng; vòng kiểm chứng chỉ xác nhận được build sạch, `210/210` test, `nginx -t` hợp lệ và scanner exit 0.
- `CHANGELOG.md`: thêm mục `Unreleased` với Tuần 2, Tuần 3, Tuần 4, và mục riêng ghi rõ các bản vá **đã kiểm chứng tại lab nhưng chưa tích hợp**.

**Quyết định không tự ý sửa:** không đổi tỷ lệ `24/24` hay `100%` của các thành viên khác vì đó là đóng góp của họ và cần trưởng nhóm đối chiếu rubric.

### 3.9 A5 — Chưa đạt

`docker-compose.dev.yml` có 7 service; **không service nào khai `profiles:`**, tất cả đều đặt `container_name` cứng, và **không có API service**. Vì vậy `docker compose --profile api-a --profile api-b up` — cách chạy 2 bản API được `A5` mô tả — **không thực hiện được với cấu hình hiện tại**. Đây là khoảng trống thực sự, không phải khác biệt hình thức.

### 3.10 BUG-W4-07

Chưa kiểm thử sâu; giữ nguyên trạng thái mở.

---

## 4. Thay đổi cục bộ (chưa vào repo)

| File | Thay đổi | Trạng thái (rà lại 07/10) |
|---|---|---|
| `deploy/scan-secrets.sh` | +111 dòng: JSON config, connection string, workflow env, allowlist | Chưa vào repo → **W5-10** (BUG-W4-02) |
| `deploy/secret-scan-allowlist.txt` | Mới — allowlist dev/CI kèm lý do | Chưa vào repo → **W5-10** |
| `src/backend/.../RecipeImageConfiguration.cs` | +`ValueGeneratedNever()` | ✅ **Fix BUG-W4-01 — đã vào `main`** (PR #29, `c624b9f`) |
| `src/backend/CulinaryBlog.API/Program.cs` | +`UseForwardedHeaders` | Fix BUG-W4-03 — **chưa vào repo → sửa trực tiếp tuần 5 (W5-10)** |
| `nginx/nginx.dev.conf` | 2 upstream, `max_fails`, `keepalive`, `proxy_next_upstream` | Chưa vào repo — W5-4/W5-10 xem lại; TLS còn nằm trong comment |
| `src/backend/CulinaryBlog.API/appsettings.json` | `Jwt:SigningKey` → rỗng | ✅ Đã vào `main` (QD3-3a) |
| `src/backend/CulinaryBlog.API/appsettings.Development.json` | `Jwt:SigningKey` → rỗng | ✅ Đã vào `main` (QD3-3a) |
| `tests/CulinaryBlog.Tests/LabRecipeImageValueGeneratedTests.cs` | Mới — 4 test hồi quy | ❌ **Không tồn tại trong repo** (đối chiếu 07/10) — viết mới trong W5-10 |
| `README.md`, `CHANGELOG.md` | Số liệu + changelog | Đã cập nhật một phần trong `main` (05/10) |

Không có migration nào được thêm **tại thời điểm viết báo cáo**. Không có thay đổi nào lên `2312739_NHTSon_D5-D6-D7` hay `main` tại thời điểm viết báo cáo này.
*(07/10: riêng `ValueGeneratedNever()` + migration `20261001112029` đã vào `main` qua PR #29 `c624b9f`.)*

---

## 5. Khuyến nghị

1. **Ưu tiên 1 — BUG-W4-03.** Đây là lỗi sẵn sàng bị khai thác để chặn đăng nhập hàng loạt. Bản vá đã kiểm chứng, nhỏ, và cần đi kèm hạn mức phân tán nếu chạy nhiều instance.
2. **Ưu tiên 2 — BUG-W4-01.** Đã tái hiện trên Postgres thật, sửa một dòng, kèm test hồi quy. Nhớ rằng `RowVersion` vẫn phải để interceptor lo.
3. **Ưu tiên 3 — BUG-W4-02.** Scanner hiện tại không bảo vệ được gì cả với bí mật của chính dự án.
4. **Đính chính báo cáo lỗi** cho BUG-W4-06 (giữ soft delete của Recipe), BUG-W4-09 (chỉ là điều kiện cạnh), BUG-W4-01/10 (snapshot lệch là vấn đề metadata, không phải nguyên nhân của 422).
5. **Cần quyết định của nhóm:** A5 (thiết kế `profiles` cho compose) và A3 phần TLS chưa có ai đảm nhận triển khai.
6. **Cột K01–K24** cần đối chiếu rubric chính thức trước khi dùng làm căn cứ chấm điểm.

---

## 6. Tài nguyên cần dọn

- 1 tiến trình API còn chạy ở `localhost:5080`.
- Các container `postgres`, `redis`, `s3`, `mailhog`, `seq`, `otel-collector`, `nginx`.
- Script kiểm thử tạm trong `C:\Users\admin\AppData\Local\Temp\opencode\`.
