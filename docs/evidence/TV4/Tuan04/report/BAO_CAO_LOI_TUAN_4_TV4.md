# BÁO CÁO LỖI / BUG TUẦN 4 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **Tại sao có file này:** theo yêu cầu ngày 03/10 — **lỗi không nhất thiết phải ghi vào kế hoạch**.
> Kế hoạch chỉ ghi *việc cần làm*; lỗi ghi ở đây, tách riêng, để kế hoạch khỏi bị phình và để
> không ai đọc kế hoạch tưởng là danh sách sửa lỗi.
>
> - **Ngày lập**: 03/10/2026 · **Baseline**: nhánh `2312739_NHTSon_D5-D6-D7` = `55b4c2b`
> - **Phạm vi:** lỗi của **TV4** (phần lớn) + lỗi ảnh hưởng tới TV4 nhưng thuộc thành viên khác (ghi rõ chủ sở hữu)
> - **Không liên quan:** [`KE_HOACH_TUAN_4_TV4_V2.md`](../KE_HOACH_TUAN_4_TV4_V2.md) — kế hoạch, không phải lỗi

**Ký hiệu mức độ**: 🔴 chặn luồng / rủi ro bảo mật · 🟠 sai hành vi nhưng chưa chặn · 🟡 nợ kỹ thuật

---

## 🧪 TRẠNG THÁI KIỂM CHỨNG THỰC TẾ (kiểm chứng 03/10/2026, rà lại 07/10)

Báo cáo này ban đầu được lập **chỉ bằng đọc code**. Sau đó TV4 đã **chạy thật trên một bản kiểm tra
cục bộ** (local, không push, không thuộc dự án) để kiểm chứng.
Bằng chứng đầy đủ: [`BAO_CAO_LAB_TUAN4_V2.md`](BAO_CAO_LAB_TUAN4_V2.md).

| Mã | Trạng thái sau kiểm chứng | Kết quả |
|---|---|---|
| BUG-W4-01 | 🟠 Đã tái hiện 03/10 — **07/10: đã vào `main`** (PR #29 `c624b9f`) | Test RED→GREEN trên PostgreSQL thật; test hồi quy + chốt C1/C2 còn lại ở tuần 5 (W5-10) |
| BUG-W4-02 | 🔴 **Đã tái hiện — chưa có bản vá trong dự án** | Scanner cũ bỏ sót JWT key và secret literal; **sửa trực tiếp tuần 5** (W5-10) |
| BUG-W4-03 | 🔴 **Đã tái hiện — chưa có bản vá trong dự án** | 2 IP khác nhau dùng chung 1 bucket; **sửa trực tiếp tuần 5** (W5-10) |
| BUG-W4-04 | 🟠 Số liệu đã đo thực tế | `210/210`; 4 con số mâu thuẫn trong README đã xác định |
| BUG-W4-05 | 🟡 Xác nhận | `CHANGELOG.md` chỉ có `0.1.0` |
| BUG-W4-06 | 🟡 **Phải đính chính** | Xem mục — **không được xoá soft delete của Recipe** |
| BUG-W4-07 | 🟠 Xác nhận + đã sửa một phần | Round-robin/failover OK; **TLS chưa bật** |
| BUG-W4-08 | 🟡 Chưa kiểm thử sâu | — |
| BUG-W4-09 | 🟡 **Phải đính chính** | Xem mục — chỉ là **điều kiện cạnh**, không phải trạng thái thường |
| BUG-W4-10 | 🟡 Xác nhận | Có thể đồng bộ snapshot, nhưng **migration no-op không sửa lỗi runtime** |

**Hai rủi ro mới phát hiện khi kiểm chứng, chưa có trong danh sách ban đầu:**

1. 🔴 **Hạn mức rate limit nhân theo số instance.** Bộ đếm là in-memory **trên từng tiến trình**,
   nên hạn mức thực tế = `10 × số instance`. Qua Nginx với 2 node, 12 request cùng một IP đều `401`
   (6/node, chưa vượt 10). `proxy_next_upstream` làm nhiều request rơi vào node sống hơn nữa.
   ⇒ Cần ghi nhận vào NFR về rate limiting phân tán; **không nằm trong BUG-W4-03**.
2. 🟡 **Ghi công "15 ô ✅" là số tự khai.** `KE_HOACH_TUAN_4_TV4_V2.md` phải nói rõ đây là kết quả
   báo cáo ba tuần, không phải số đếm trực tiếp của TV4.

> **Lưu ý về phạm vi (đính chính 07/10):** các bản vá BUG-W4-02/03 **không tồn tại trong repo** — TV4 chỉ
> kiểm tra trên bản vá cục bộ, không push. Baseline đo được trên nhánh gốc là `210/210` (`205` + `5`);
> con số `214` lấy từ bản vá cục bộ (các test hồi quy kèm theo **không có trong repo**) → **không** dùng
> để cập nhật README. Riêng `BUG-W4-01` đã vào `main` qua PR #29 (`c624b9f`) — phần test hồi quy + chốt
> C1/C2 còn lại ở tuần 5 (W5-10).

---

## A. Lỗi đang mở

> **Nghĩa của mục A:** lỗi **chưa có bản vá trên nhánh chính** `2312739_NHTSon_D5-D6-D7` / `main`.
> Lỗi 01/02/03 khi lập báo cáo **chưa có bản vá nào trong dự án** (TV4 chỉ kiểm chứng trên bản vá cục bộ,
> không push) — nên tính là đang mở. *Cập nhật 07/10: `BUG-W4-01` đã vào `main` (PR #29); `02`/`03` sửa
> trực tiếp trong tuần 5 (W5-10).* Xem bảng trạng thái ở mục trên.

> [!WARNING]
> **Kiểm chứng lại 05/10/2026 — dễ nhầm với lỗi GĐ1** *(rà lại 07/10)*.
>
> | Kiểm tra | Kết quả 05/10 |
> |---|---|
> | `RecipeImageConfiguration.cs` có `ValueGeneratedNever()`? | ❌ **Không** — vẫn lệch với `RecipeIngredientConfiguration.cs:20` và `RecipeStepConfiguration.cs:20` ⇒ BUG-W4-01 **chưa merge** |
>
> *(07/10: `main` **đã có** `ValueGeneratedNever()` + migration `20261001112029` — vào qua PR #29 `c624b9f`.)*
>
> ⛔ **Ba lỗi đã đóng ở GĐ1 là lỗi KHÁC**, không phải 01/02/03: DB chết trả `500`, DB chết lúc
> khởi động giết tiến trình, `GET /recipes/{slug}` trả `500` khi thiếu credential storage. Cả ba
> **đã merge** ở commit `8d9d62b` và khoá bằng test hồi quy. Xem
> [`BAO_CAO_GIAI_DOAN_1_N2_N4.md`](BAO_CAO_GIAI_DOAN_1_N2_N4.md) mục 1.
>
> BUG-W4-01 vẫn **cần Tâm chốt C1 hay C2** (đụng schema `RecipeImages` dùng chung với TV3).

### BUG-W4-01 🔴 — `422 recipe.version_conflict` khi thêm ảnh vào recipe **đã có ảnh**

| | |
|---|---|
| **Chủ sở hữu** | **TV4** (`RecipeImage` là phần D1 của TV4) · phần schema là TV3 |
| **Trạng thái** | 🟠 Đã tái hiện 03/10 — **07/10: đã vào `main`** (PR #29 `c624b9f`) — còn test hồi quy + chốt C1/C2 (W5-10) |
| **Nguồn** | Báo cáo của TV3 gửi TV4 → [`docs/report/BAO_CAO_KIEM_TRA_LOI_422_TAI_ANH_TV3_GUI_TV4.md`](../../../../report/BAO_CAO_KIEM_TRA_LOI_422_TAI_ANH_TV3_GUI_TV4.md) |

**Nguyên nhân gốc:** `RecipeImageConfiguration.cs` **không** có `b.Property(i => i.Id).ValueGeneratedNever();`,
khác `RecipeIngredientConfiguration.cs:20` và `RecipeStepConfiguration.cs:20`. Vì `Id` để `ValueGeneratedOnAdd`
mà domain lại gán `Guid.NewGuid()` (non-empty) ngay khi `recipe.AddImage()`, EF đánh dấu entity là
`Modified` ⇒ `UPDATE ... WHERE RowVersion = <rỗng>` ⇒ **0 dòng** ⇒ `DbUpdateConcurrencyException` ⇒ API trả `422`.

**Workaround đang chạy trên `main`:** `AuditableEntityInterceptor.cs:41-48` ép
`RecipeIngredient or RecipeStep or RecipeImage` + `RowVersion.OriginalValue.Length == 0` về `EntityState.Added`.
Đã xác minh 01/10 bằng test chạy trên PostgreSQL thật: entity `RecipeImage` mới có state `Detached`,
`SaveChanges` ra `INSERT`, `RecipeImages` có 1 dòng, `RowVersion` gán 16 byte.

**Vì sao vẫn ghi là lỗi chứ không phải "đã xử lý":**
1. Workaround là **lá chắn tại interceptor**, không phải sửa gốc. `RecipeImageConfiguration.cs` vẫn lệch
   với hai entity con kia, và `Migrations/AuthDbContextModelSnapshot.cs:183-185` vẫn ghi `.ValueGeneratedOnAdd()`.
2. Workaround chỉ bắt đúng **3** class đã liệt kê. Thêm entity con thứ 4 vào aggregate mà quên cập nhật
   danh sách ⇒ lỗi 422 quay lại, không có test nào bắt được (vì bản thân `Testing` không đi qua luồng này khi guard tắt).
3. Rủi ro: interceptor đang **che một cấu hình sai**, khiến người sau đọc code tin là `RecipeImage` đã cấu hình đúng.

**Việc cần làm — chọn 1 trong 2, cần Tâm chốt (không tự quyết vì đụng schema của TV3):**

| | Phương án | Đánh đổi |
|---|---|---|
| **C1** | Thêm `ValueGeneratedNever()` vào `RecipeImageConfiguration.cs` + **migration snapshot**; giữ interceptor làm lưới an toàn | Đúng gốc; cần migration ⇒ phải bàn với TV3 vì `RecipeImages` là bảng dùng chung |
| **C2** | Giữ nguyên, **thêm test hồi quy** chứng minh thêm ảnh vào recipe đã có ảnh trả `201` (không `422`) | Nhanh, ít rủi ro; nhưng cấu hình vẫn lệch và nợ kỹ thuật vẫn treo |

> **Không chọn C3** (bỏ workaround trong interceptor trước khi sửa cấu hình) — sẽ làm lỗi 422 quay lại ngay.

---

### BUG-W4-02 🔴 — Secret **vẫn còn** trong file tracked, và `scan-secrets.sh` **không bắt được**

| | |
|---|---|
| **Chủ sở hữu** | **TV4** — `KE_HOACH_DU_AN.md` giao "bỏ secret hardcode + secret scan CI"; N1-8 đã làm một phần |
| **Trạng thái** | 🟠 Đã tái hiện 03/10 (scanner mới bắt đúng, allowlist có lý do) — **chưa có bản vá trong dự án** → sửa trực tiếp tuần 5 (W5-10). N1-8 mới chỉ xử lý `render.yaml` |

N1-8 (`0c692d3`, 30/09) bỏ JWT key khỏi `render.yaml` (`sync: false`) và thêm `deploy/scan-secrets.sh`.
Nhưng còn sót:

| File:line | Nội dung |
|---|---|
| `src/backend/CulinaryBlog.API/appsettings.json:3` | `Password=postgres` trong `ConnectionStrings:Database` |
| `src/backend/CulinaryBlog.API/appsettings.json:9` | `Jwt:SigningKey` — 74 ký tự, ai đọc repo cũng ký được token admin |
| `src/backend/CulinaryBlog.API/appsettings.Development.json:3` | `Password=postgres` |
| `src/backend/CulinaryBlog.API/appsettings.Development.json:6` | `Jwt:SigningKey` |
| `src/backend/CulinaryBlog.API/appsettings.Development.json:13-14` | `AccessKey: minioadmin` / `SecretKey: minioadmin` |
| `.github/workflows/backend.yml:47-48` | `RUSTFS_ACCESS_KEY: minioadmin` / `RUSTFS_SECRET_KEY: minioadmin` |
| `.github/workflows/backend.yml:67-68` | `MINIO_ACCESS: minioadmin` / `MINIO_SECRET: minioadmin` |

**Lỗi của chính công cụ chống secret — phần nguy hiểm hơn:** `deploy/scan-secrets.sh` chạy đúng 3 hàm:
- `check_yaml_secret_keys render.yaml` · `check_yaml_secret_keys docker-compose.dev.yml` — chỉ nhận mẫu `key:` / `value:` kiểu Render
- `scan_known_token_patterns` — 7 pattern token nổi tiếng (`ghp_`, `AKIA`, `sk-`, `xox*`, PEM…), và loại trừ `:!docs`

⇒ **không** đọc `appsettings*.json`, **không** đọc khối `env:` của workflow. Script báo `OK: kh�ng phát hiện secret`
trong khi repo vẫn còn 6 chỗ. Đây là loại lỗi "công cụ xanh nhưng việc chưa xong" — nguy hiểm vì ai đó
cũng sẽ tin là đã xong.

**Việc cần làm (A1 trong kế hoạch V2):**
1. Dọn `appsettings.Development.json` → đọc từ biến môi trường / user-secrets (`DevConfigParityTests` đã có sẵn để kiểm tra).
2. `appsettings.json` — đặt `Jwt:SigningKey` về chuỗi trống + `ValidateOnStart()` bắt buộc có biến môi trường (không để key mặc định nào tồn tại).
3. Mở rộng `scan-secrets.sh`: thêm quét `**/appsettings*.json` (mọi khóa chứa `key`/`password`/`secret`/`connectionstring` có giá trị literal) và `env:` của `.github/workflows/*.yml`.
4. Thêm test cho chính script: chèn JWT hardcode vào JSON → phải exit 1 (bản gốc mới chỉ thử nghiệm ở `render.yaml`).
5. **Rotate JWT signing key thật** — khoá cũ còn trong git history, việc này **nằm ngoài repo**, cần người quản trị Render làm (xem câu hỏi Q4 của kế hoạch V2).

---

### BUG-W4-03 🔴 — Rate limit dùng chung **một** bucket cho toàn bộ người dùng sau Nginx

| | |
|---|---|
| **Chủ sở hữu** | **TV1** (`Program.cs`) — TV4 giữ phần nginx |
| **Trạng thái** | 🟠 Đã tái hiện 03/10 (`UseForwardedHeaders` + mỗi IP một bucket, xác minh trên bản cục bộ) — **chưa có bản vá trong dự án** → sửa trực tiếp tuần 5 (W5-10); `Program.cs` là phần TV1 → PR riêng, báo trước |

`Program.cs:120-125`: `AddRateLimiter` chỉ có **một** policy `auth`, dùng `RateLimitPartition.GetFixedWindowLimiter`
(**fixed window**, không phải sliding), khoá theo `http.Connection.RemoteIpAddress`. Toàn repo **không** có `UseForwardedHeaders`.

Trong khi `nginx/nginx.dev.conf:18` **đã** set `X-Forwarded-For` — nhưng app không đọc header đó.
⇒ `RemoteIpAddress` là **IP container Nginx** ⇒ **mọi người dùng chia chung 1 bucket 10 request/phút**.
`NFR-SEC-003` yêu cầu 5 tiêu chí, đạt **1/5**.

**Hệ quả trực tiếp lên việc của TV4:** mọi số đo k6 của N2-6 sẽ bị giới hạn bởi rate limit của chính endpoint
đang đo ⇒ **không đo được p95/p99 thật**. Cần chốt G1 **trước** khi chạy k6, hoặc đo ngoài proxy.

> TV4 **không tự sửa** vì sửa `Program.cs` là ngoài phân công. Nếu Tâm giao thì TV4 sửa được trong 1 giờ.

---

### BUG-W4-04 🟠 — `README.md` ghi **4 con số test khác nhau**, không khớp số thật

| File:line | Ghi | Thực tế tại `55b4c2b` |
|---|---|---|
| `README.md:62` | `177 / 177` (172 + 5) | **210 / 210** (205 + 5) |
| `README.md:224` | `172 / 172` | ↑ |
| `README.md:233` | `172 / 172` | ↑ |
| `README.md:352` | `172 / 172` | ↑ |

`README.md:72` cũng ghi TV1 *"24 / 24 (K01–K24)"* — không liên quan tới ô của TV4, nhưng cho thấy `README`
đang dùng những con số kỹ năng **chưa qua nghiệm thu** (báo cáo 3 tuần mục 9: bảng 24 ô trong
`SO_EVIDENCE_TUAN_3.md:17-35` đánh ✅ nhiều hơn số `PHAN_CHIA:180` chấp nhận).

**Việc cần làm (A6):** để **một** con số test duy nhất ở một chỗ, các chỗ khác trỏ tới;
cập nhật 4 dòng trên + dòng 72.

---

### BUG-W4-05 🟠 — `CHANGELOG.md` đứng `0.1.0` từ 09/09, không có mục tuần 2 / 3 / 4

`CHANGELOG.md:3` = `## 0.1.0 — 2026-09-09`. Từ đó tới `55b4c2b` đã có **143+** commit, 5 bản release thực tế
(RustFS, Hangfire, media proxy, OTEL+Seq, Redis shared cache), nhưng changelog không có dòng nào.

`NFR-MAINT-003` **thuộc TV4** theo danh sách 21 mã đã đính chính (danh sách gốc từ `KE_HOACH_DU_AN.md` bỏ sót mã này).
Báo cáo 3 tuần mục 3.2 ghi đây là vi phạm của TV1, nhưng mã thì thuộc TV4 — cần chốt lại khi sửa.

**Việc cần làm (A6):** thêm mục tuần 2/3/4 theo `Unreleased`, không sửa lịch sử `0.1.0`.

---

### BUG-W4-06 🟡 — Domain soft-delete còn sống nhưng repository đã **hard delete**

`Recipe.cs:287` `MarkDeleted()` · `Recipe.cs:298` `SoftDelete()` (alias) · `BaseEntity.cs:18-19` `IsDeleted`
vẫn tồn tại và `AuditableEntityInterceptor.cs:64-67` vẫn còn nhánh `case EntityState.Deleted → Modified(IsDeleted = true)`.
Nhưng `CategoryRepository.DeleteAsync` dùng `db.Categories.Remove()` = **hard delete** (đúng C07).

⇒ Query filter `!IsDeleted` không còn tác dụng với `Category`; tên test còn để dấu vết
(`CategoryTests.cs:146`: `DeleteCategory_blocks_when_recipes_exist_and_soft_deletes_when_empty`).

**Việc cần làm (A2):** ghi ADR nói rõ `Recipe` dùng soft delete còn `Category` dùng hard delete, và
đổi tên test cho khỏi sai. **KHÔNG xoá code soft delete** — xem phần đính chính bên dưới.

> #### 🧪 ĐÍNH CHÍNH SAU KIỂM CHỨNG THỰC TẾ (03/10/2026, lab)
>
> Phần "Việc cần làm" ở bản đầu của mục này **đề xuất xoá `MarkDeleted`/`SoftDelete`/`IsDeleted`**.
> Kiểm chứng thực tế cho thấy đề xuất đó **sẽ phá vỡ tính năng đang chạy**:
>
> | Kiểm tra | Kết quả |
> |---|---|
> | Global query filter | **Còn sống** — `src/backend/CulinaryBlog.Infrastructure/IdentityModel.cs:93-103` |
> | `Recipe` soft delete trong production | **Có dùng thật** — `src/backend/CulinaryBlog.Application/Recipes.cs:655`, `src/backend/CulinaryBlog.Infrastructure/RecipeRepository.cs:192` |
> | `CategoryTests.cs:146` | Tên test nói `soft_deletes_when_empty`, nhưng thân test và comment (`:160`, `:164`) xác nhận **hard delete** → lỗi tên gây hiểu nhầm |
> | `Category.MarkDeleted` (`src/backend/CulinaryBlog.Domain/Category.cs:42`) | Chỉ được gọi tại `CategoryTests.cs:76`, **không thấy ở production** |
>
> ⇒ **Phạm vi đúng của lỗi chỉ là:** (1) đổi tên test, (2) `Category.MarkDeleted` là code chết,
> (3) thiếu ADR giải thích quy ước. Tuyệt đối **không** xoá soft delete của `Recipe`/`BaseEntity`.

---

### BUG-W4-07 🟠 — Nginx: 1 server trong `upstream`, không `proxy_next_upstream`, không TLS

`nginx/nginx.dev.conf:6-8` — `upstream culinary_api` chỉ có **một** `server` ⇒ khi API-1 chết thì **502**,
không chuyển sang API-2. `nginx.dev.conf:11` — `listen 80`, không có `ssl_certificate`.
Không có `proxy_next_upstream` ở bất kỳ file nginx nào.

Cùng nhóm với BUG-W4-03: đây là nguyên nhân làm hỏng `NFR-SEC-003` **và** `NFR-SEC-005` (20%),
`NFR-SCALE-003` (65%).

> `nginx/nginx.multiinstance.conf` **đã** có `upstream` 2 server + round-robin — nhưng đó là file **kiểm chứng
> bài toán**, chưa phải cấu hình dùng hàng ngày, và chưa có profile `docker-compose` cho nó.

**Việc cần làm (A3, A5):** `upstream` nhiều server + `proxy_next_upstream error timeout http_502 http_503 http_504;`
trong `nginx.dev.conf`; thêm profile `docker-compose` 2 API dùng `nginx.multiinstance.conf`.

---

### BUG-W4-08 🟡 — 3 tài liệu tuần 4 mô tả trạng thái **không còn tồn tại**

Quyết định nhóm 30/09 ghi "giữ nguyên 4 file Lab 04, không sửa, không xoá", và 3 tài liệu của TV4 ghi lại
điều đó. Nhưng commit `f85e1e6` ("docs(tv4): chuyển mapping K01 ra gốc TV4/ + xoá 4 file Lab04") đã xoá
`TUAN_4.md`, `BAO_CAO_LAB_04.md`, `Lab04_*.docx`, `Lab4_*.docx`.

Kiểm chứng 03/10: `docs/evidence/TV4/` hiện chỉ còn `HUONG_DAN_CHAY_TV4.md` và `MAPPING_K01_FR_NFR_ADR_EVIDENCE.md`.

Các dòng đang mô tả trạng thái cũ: `KE_HOACH_TUAN_4_TV4.md:15-25` · `SO_EVIDENCE_TUAN_4.md:10-15` ·
`TRANG_THAI_THUC_HIEN_TUAN_4.md:132-139`.

**Việc cần làm:** cập nhật 3 file trên thành "4 file Lab 04 **đã bị xoá bởi TV1** ở `f85e1e6`; nội dung
ghi trong đó không dùng làm minh chứng của TV4". **Không** khôi phục lại file đã xoá.

---

### BUG-W4-09 🟡 — `/sitemap.xml` trả `200` với XML **rỗng** khi generator khác đang giữ khoá

Không phải lỗi — là hành vi có chủ đích để không phát sinh tải. Nhưng dễ bị người sau báo nhầm là lỗi
(`SO_EVIDENCE_TUAN_4.md` mục `TV4-K14` đã ghi rõ, giữ nguyên).

**Việc cần làm:** ghi một dòng vào `docs/RUNBOOK.md` (N4-1) + trả header `X-Sitemap-Generated: false`
để phân biệt được bằng mắt thường.

> #### 🧪 ĐÍNH CHÍNH SAU KIỂM CHỨNG THỰC TẾ (03/10/2026, lab)
>
> Tiêu đề và phần mô tả ở bản đầu nói endpoint "trả `200` với XML rỗng" — **diễn đạt này quá tuyệt đối**.
> Request thực tế qua Nginx tới 2 API instance trả **XML hợp lệ dài 28.086 byte** với HTTP 200, vì Redis
> đã có cache.
>
> ⇒ Lỗi **không phải trạng thái thường**, mà là **điều kiện cạnh**: cache rỗng **và** không giành được lock
> cùng lúc. Mô tả đúng phải là: *"có thể trả `200` với XML rỗng khi cache rỗng và tranh lock thất bại"*.
>
> **Trạng thái kiểm chứng:** luồng code tại `Program.cs:470-480` đã xác nhận; **chưa tái hiện được** đúng
> điều kiện cạnh (cần giành lock trong lúc cache rỗng). Không nên coi là đã tái hiện hoàn toàn.

---

### BUG-W4-10 🟡 — Snapshot EF vẫn ghi `ValueGeneratedOnAdd()` cho `RecipeImage`

`src/backend/CulinaryBlog.Infrastructure/Migrations/AuthDbContextModelSnapshot.cs:183-185` ghi `ValueGeneratedOnAdd()`
cho `RecipeImage`, lệch với `RecipeIngredient`/`RecipeStep` (`ValueGeneratedNever`).
Là hệ quả của BUG-W4-01: nếu ai đó chạy `dotnet ef migrations add` từ snapshot này, migration sinh ra
lại cấu hình sai và lỗi 422 quay lại dù interceptor có che.

**Việc cần làm:** sửa cùng phương án C1 của BUG-W4-01 — không tách riêng.

> #### 🧪 KẾT QUẢ KIỂM CHỨNG (03/10/2026, lab)
>
> Xác nhận snapshot **thật sự lệch**, và tìm ra cách đồng bộ — kèm một cảnh báo quan trọng:
>
> | Kiểm tra | Kết quả |
> |---|---|
> | `AuthDbContextModelSnapshot.cs:183-185` vs `:241-242` / `:289-290` | Lệch đúng như mô tả |
> | `dotnet ef migrations has-pending-model-changes` | **Không** phát hiện thay đổi |
> | `dotnet ef migrations add LabProbe_RecipeImageIdNever` | Sinh migration với `Up`/`Down` **rỗng**; chỉ xoá đúng annotation `.ValueGeneratedOnAdd()` trong snapshot |
>
> ⚠️ **Cảnh báo:** migration đó **không có tác dụng gì lên database** — nó chỉ cập nhật file snapshot.
> Khi báo cáo phải nói rõ điều này để không ai hiểu nhầm rằng đã "sửa được" lỗi runtime bằng migration.
> Lỗi runtime nằm ở cấu hình EF, không nằm ở migration. Probe đã được xoá; lab **không** giữ migration no-op.
>
> **Lưu ý thêm:** sửa cấu hình không làm mất tác dụng của interceptor — bỏ `AuditableEntityInterceptor` thì
> `RowVersion` là mảng rỗng (0 byte); chỉ qua DI stack thậm chuỗi mới được gán 16 byte cùng `CreatedAt`.

---

## B. Lỗi đã phát hiện và sửa trong tuần 4 (N1) — để người sau không lặp lại

| Mã | Lỗi | Vì sao test không bắt được | Cách sửa |
|---|---|---|---|
| BUG-W4-FIX-01 | `RecurringJob.AddOrUpdate` (static API) gọi lúc đăng ký DI ⇒ `InvalidOperationException: Current JobStorage instance has not been initialized yet` ⇒ **app không khởi động ở mọi môi trường thật** | Môi trường `Testing` không bật Hangfire | Đăng lịch **sau** `builder.Build()` qua `IRecurringJobManager` lấy từ DI |
| BUG-W4-FIX-02 | Sink Serilog gán vào `Log.Logger` *sau* `builder.Host.UseSerilog(...)` ⇒ `UseSerilog` thay thế logger đó ⇒ **Seq nhận 0 event của app** | Chỉ kiểm được khi bật thật container Seq | Gộp `.WriteTo.Seq(url)` vào đúng `LoggerConfiguration` của `UseSerilog`, bọc try/catch để Seq chết không làm app không khởi động |
| BUG-W4-FIX-03 | Bước "chờ Redis sẵn sàng" trong CI dùng `redis-cli` ⇒ **job đỏ** trong khi toàn bộ test đều xanh | `redis-cli` chỉ nằm bên trong service container, không có trên `ubuntu` runner | Đổi sang `bash /dev/tcp` của bash (commit `ee5e78b`) |

> **Bài học chung:** cả 3 lỗi trên đều **không** bị bộ test tự động phát hiện, vì bộ test chạy ở môi trường
> `Testing` bằng guard, không giống môi trường thật. Mọi thay đổi cấu hình hạ tầng ở tuần 4 phải kèm
> **bằng chứng chạy thật**, không được chỉ dựa vào test xanh.

---

## C. Bẫy môi trường trên máy TV4 (không phải lỗi code, nhưng gây lỗi giả)

| Mã | Bẫy | Hậu quả nếu quên |
|---|---|---|
| M1 | Máy có **native PostgreSQL 18** và container PostgreSQL cùng nhận cổng `5432`; `localhost:5432` trỏ vào native | `pg_dump`/`pg_restore` phải dùng client tại `E:\PostgreSQL\bin`; dữ liệu thật nằm ở native, **không** nằm trong container |
| M2 | Test E2E storage/Redis/DB **thoát sớm (guard)** nếu thiếu `docker compose up -d postgres redis s3 mailhog seq otel-collector` | `Skipped=0` trên máy có đủ container **≠** `Skipped=0` trên máy khác — không được dùng "xanh" làm bằng chứng nếu chưa nói rõ môi trường |
| M3 | `*.dump` đã nằm trong `.gitignore` vì dump chứa dữ liệu người dùng | Không được commit backup vào repo |
| M4 | Runner GitHub có rất ít binary; `redis-cli`, `psql`… không có sẵn | Khi thêm bước chờ dịch vụ mới, dùng `bash /dev/tcp` hoặc `docker exec` vào service container |
| M5 | `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md` phải giữ nguyên blob `93661aa13e6fa2e081b9a89b2517eca5d0df083c` | Bản sửa đã ở trên `main` (`e523579` của TV2); sửa blob thì mất giá trị làm bằng chứng |

---

## D. Quy tắc ghi file này

1. **Lỗi không ghi vào kế hoạch.** Kế hoạch (`KE_HOACH_TUAN_4_TV4_V2.md`) chỉ ghi *việc cần làm*;
   mỗi việc có mã `BUG-W4-xx` trỏ tới mục ở đây.
2. Mỗi lỗi phải có **`file:line`** hoặc **log** làm bằng chứng. Không có bằng chứng thì ghi "chưa xác minh".
3. Ghi rõ **chủ sở hữu**. Lỗi của người khác: ghi tên họ, TV4 **không** tự sửa — chỉ đề xuất (mục 7 của kế hoạch V2).
4. Lỗi của người khác **không** được dùng làm lý do nâng/giảm tỷ lệ hoàn thành của TV4.
5. Lỗi đã sửa chuyển từ mục A xuống mục B **kèm** lý do vì sao test không bắt được.
6. Bẫy môi trường (mục C) không tính vào tỷ lệ, nhưng **phải** ghi lại để người sau không mất ngày.
