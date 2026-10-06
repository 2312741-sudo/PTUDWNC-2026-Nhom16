# Hướng dẫn cài đặt & chạy ứng dụng — riêng TV4 (Nguyễn Hữu Trung Sơn)

> **Vì sao cần file này?** `README.md` mục 5 viết cho **bash** (`export ...`) và giả định
> PostgreSQL của container **vừa được tạo** (với mật khẩu mặc định `postgres`).
> Trên máy TV4 (Windows + **PowerShell 5.1**) và với volume `pgdata` đã tạo từ trước
> (khi `docker-compose.dev.yml` còn dùng image MinIO / password `admin123`), các lệnh đó **không chạy được**.
> File này ghi đúng thứ tự thao tác trên máy TV4, mọi lệnh dưới đây **đã chạy thật và kiểm chứng** ngày 28/09/2026.
>
> Liên quan: `README.md` §5, `docs/HUONG_DAN_TEST_APP.md`,
> `docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md` (vì sao dev/CI dùng RustFS thay MinIO).

---

## 0. Mô hình cấu hình (đọc trước khi làm gì cả)

| Tầng | Ở đâu | Có commit? | Vai trò |
|---|---|:---:|---|
| **Default** | `src/backend/CulinaryBlog.API/appsettings*.json`, `docker-compose.dev.yml` | ✅ | Giá trị **chuẩn** để máy mới clone chạy được ngay, không cần làm gì thêm |
| **Giá trị thật của máy** | `.env` ở thư mục gốc | ❌ (`.gitignore`) | **Override** default. Tạo bằng `cp .env.example .env` |
| **Mẫu** | `.env.example` | ✅ | Chỉ là template, **không** chứa giá trị thật |

Cả hai tầng **đều được đọc**:

- `docker compose` tự nạp `.env` cho biến `${VAR:-default}`.
- API + test đọc `.env` qua `EnvFileLoader` (`src/backend/CulinaryBlog.API/EnvFileLoader.cs`).
  Bỏ qua khi `ASPNETCORE_ENVIRONMENT=Production`; biến đã export sẵn trong shell/CI **luôn thắng** `.env`.

> Default PostgreSQL trong repo là **`postgres`** (chuẩn PostgreSQL). Máy TV4 dùng volume cũ
> nên `.env` của TV4 để `admin123` — đây là lý do phải có `.env`, không sửa file đã commit.

---

## 0.1. TL;DR — copy-paste toàn bộ (PowerShell)

```powershell
# 0) Chuyển tới repo
Set-Location "D:\WNC\PTUDWNC-2026-Nhom16"

# 1) Tạo .env cho máy này (copy từ mẫu, KHÔNG commit)
Copy-Item .env.example .env
#    -> sửa .env: POSTGRES_PASSWORD + ConnectionStrings__Database + TEST_DATABASE
#       cùng dùng MỘT mật khẩu. Máy TV4 giữ admin123 (volume cũ).

# 2) Bật hạ tầng Docker (Postgres, Redis, RustFS, Mailhog, Seq, Nginx)
docker compose -f docker-compose.dev.yml up -d

# 3) Đặt lại password Postgres cho khớp .env (chỉ cần khi gặp 28P01 - xem mục 3)
docker exec culinaryblog-pg psql -U postgres -c "ALTER USER postgres PASSWORD 'admin123';"

# 4) Backend: restore -> migrate -> chạy API ở cổng 5080 (.env được nạp tự động)
dotnet restore CulinaryBlog.sln --locked-mode
dotnet run --project src/backend/CulinaryBlog.API -- --migrate
dotnet run --project src/backend/CulinaryBlog.API -- --urls http://localhost:5080

# 5) Mở cửa sổ PowerShell THỨ HAI cho frontend (giữ nguyên cửa sổ API đang chạy)
Set-Location "D:\WNC\PTUDWNC-2026-Nhom16\src\frontend"
Copy-Item ..\..\.env.example .env.local   # rồi sửa .env.local phần Frontend ở cuối file
npm install
npm run dev
```

Sau khi chạy xong: **http://localhost:3000** · API docs **http://localhost:5080/scalar/v1**

> **Không còn phải `export` gì cho API.** Nếu vẫn muốn override trong 1 phiên shell thì dùng
> `$env:TEN = "gia tri"` (PowerShell) — biến đó thắng `.env`.

---

## 1. Yêu cầu môi trường

| Công cụ | Phiên bản | Kiểm tra |
|---|---|---|
| .NET SDK | 10.x | `dotnet --version` |
| Node.js | 20+ | `node -v` |
| Docker Desktop | đang **chạy** | `docker version` (phải có dòng `Server:`) |
| Git | bất kỳ | `git --version` |

Nếu `docker version` báo `error during connect ... the daemon is not running` → mở Docker Desktop
và đợi tới khi có dòng `Server:` (xem mục 9, lỗi L1).

---

## 2. Vì sao lệnh trong `README.md` không chạy được trên máy TV4?

Ba nguyên nhân độc lập, cần loại trừ theo thứ tự:

| # | Triệu chứng | Nguyên nhân | Lệnh kiểm tra |
|---|---|---|---|
| A | `$env:ConnectionStrings__Database = ...` chạy xong nhưng app vẫn lỗi kết nối | `README.md` (bản cũ) dùng cú pháp **bash `export`**; PowerShell hiểu `export` là lệnh *khác* nên biến không được set | `echo $env:ConnectionStrings__Database` |
| B | `28P01 password authentication failed for user "postgres"` dù `.env`/compose đã để `admin123` | `POSTGRES_PASSWORD` **chỉ được dùng lúc khởi tạo volume lần đầu**. Volume `pgdata` của TV4 đã tạo từ trước (khi còn dùng image MinIO) với `admin123`, còn default mới trong repo là `postgres` ⇒ hai bên lệch nhau | `docker exec culinaryblog-pg psql -U postgres -tAc "select rolpassword from pg_authid where rolname='postgres'"` |
| C | `42P03 database "culinary_blog" does not exist` | Database chưa được tạo (EF tự tạo ở lần `--migrate` đầu tiên, nếu user có quyền `CREATEDB`) | `docker exec culinaryblog-pg psql -U postgres -tAc "select datname from pg_database where datistemplate=false"` |

> **Bĩ quán:** chọn **một** mật khẩu duy nhất rồi dùng ở **cả 3 nơi**:
> (1) PostgreSQL thật, (2) `ConnectionStrings__Database` trong `.env` cho API, (3) `TEST_DATABASE` trong `.env` cho test.
> Lệch 1 chỗ là `28P01`. Cả 3 biến nằm trong `.env` ở gốc repo — sửa một lần là xong.

---

## 3. Kiểm tra mật khẩu PostgreSQL đang dùng (3 lệnh, đã kiểm chứng)

```powershell
# 3.1 Xem Postgres đang lưu mật khẩu gì (SCRAM-SHA-256$... = có đặt mật khẩu)
docker exec culinaryblog-pg psql -U postgres -tAc "select rolpassword from pg_authid where rolname='postgres'"

# 3.2 Thử đúng đường đi mà app/test đi: từ container KHÁC trong cùng network
docker run --rm -e PGPASSWORD=admin123 --network ptudwnc-2026-nhom16_default postgres:16-alpine psql -h culinaryblog-pg -U postgres -tAc "select 1"
# Trả về 1  -> mật khẩu ĐÚNG
# Trả về lỗi 28P01 -> mật khẩu SAI
```

> ⚠️ **Cảnh báo dễ bị sai:** `docker exec culinaryblog-pg psql -U postgres ...` **không có `-h`**
> luôn trả về `1` kể cả khi sai mật khẩu, vì `pg_hba.conf` của image `postgres:16-alpine` đang là
> `local all all trust` và `host ... 127.0.0.1/32 trust`. Chỉ kết nối từ container khác
> (`-h culinaryblog-pg`) mới đi đúng rule `host all all all scram-sha-256` như app thật.
> Tên network lấy bằng: `docker network ls | Select-String "_default"`
> (thường là `<tên-thư-mục>_default`, ví dụ `ptudwnc-2026-nhom16_default`).

---

## 4. Bước 1 — Khởi động hạ tầng Docker

```powershell
docker compose -f docker-compose.dev.yml up -d
docker compose -f docker-compose.dev.yml ps
```

Cần **6 container** `Up`, trong đó `culinaryblog-pg` và `culinaryblog-s3` phải là `healthy`:

| Container | Vai trò | Cổng máy host |
|---|---|---|
| `culinaryblog-pg` | PostgreSQL 16 | `5432` |
| `culinaryblog-redis` | Redis 7 | `6379` |
| `culinaryblog-s3` | **RustFS** (S3-compatible, thay image MinIO đã bị gỡ khỏi registry) | `9000` (S3 API) / `9001` (console) |
| `culinaryblog-mailhog` | Mailhog (xem mail WelcomeEmail) | `1025` (SMTP) / `8025` (web UI) |
| `culinaryblog-seq` | Seq (nhận log OTEL) | `5341` || `culinaryblog-nginx` | Nginx dev | `8080` |

Nếu container `Up` nhưng `unhealthy`, xem mục 9 (L4).

---

## 5. Bước 2 — Chọn một trong hai cách xử lý mật khẩu

> Mọi cách dưới đây đều ghi vào **`.env`** (không sửa file đã commit). Sửa `.env` xong thì
> `docker compose` và API đều tự đọc, **không** cần `export`.

### Cách A (khuyến nghị cho máy có volume cũ) — giữ `admin123`, khai trong `.env`

Volume `pgdata` của TV4 đã tạo từ trước với mật khẩu `admin123`, trong khi default mới trong repo là
`postgres`. Cách ít rủi ro nhất là **giữ nguyên volume** và khai `admin123` trong `.env`:

```powershell
# .env — 3 dòng này phải cùng một mật khẩu
POSTGRES_PASSWORD=admin123
ConnectionStrings__Database=Host=localhost;Port=5432;Database=culinary_blog;Username=postgres;Password=admin123
TEST_DATABASE=Host=localhost;Port=5432;Database=culinary_test;Username=postgres;Password=admin123
```

Xác minh bằng lệnh 3.2 ở mục 3 (phải ra `1`).

Nếu volume của bạn **rỗng/mới**, dùng default `postgres` thì bỏ mục này, copy `.env.example` là xong.

> 💡 Còn một cách nữa là xoá volume cho sạch:
> `docker compose -f docker-compose.dev.yml down -v` rồi `up -d`.
> **Không dùng nếu bạn còn dữ liệu dev** — lệnh này xoá **cả** `pgdata`, `s3data` (ảnh recipe), `redisdata`.

### Cách B — đặt mật khẩu PostgreSQL của riêng bạn về mặc định `postgres`

Sửa `.env` thành `postgres` cho cả 3 biến, rồi đặt lại mật khẩu trong volume:

```powershell
# (1) Nếu bạn dùng PostgreSQL native cài trên Windows, BỎ QUA container culinaryblog-pg
#     để khỏi tranh cổng 5432 — xem mục 9, lỗi L3
#
# (2) Sửa .env: POSTGRES_PASSWORD / ConnectionStrings__Database / TEST_DATABASE = postgres

# (3) Đặt mật khẩu trong volume cho khớp (chạy qua socket nội bộ trust nên không cần mật khẩu cũ)
docker exec culinaryblog-pg psql -U postgres -c "ALTER USER postgres PASSWORD 'postgres';"
docker compose -f docker-compose.dev.yml up -d --force-recreate postgres
```

> ⚠️ Biến trong shell (đã `export`/`$env:`) **thắng** `.env`. Nếu từng set rồi quên xoá thì app vẫn
> dùng giá trị cũ. Kiểm tra bằng `echo $env:ConnectionStrings__Database`, xoá bằng `Remove-Item Env:ConnectionStrings__Database`.

---

## 6. Bước 3 — Chạy Backend API (.NET 10)

```powershell
Set-Location "D:\WNC\PTUDWNC-2026-Nhom16"

# 1) Khôi phục package đúng phiên bản đã khoá (CI cũng chạy lệnh này)
dotnet restore CulinaryBlog.sln --locked-mode

# 2) Cấu hình: .env đã tự được nạp bởi EnvFileLoader (mục 0).
#    Muốn override trong 1 phiên shell thì dùng $env: (PowerShell), KHÔNG dùng export.
#    $env:ConnectionStrings__Database = "Host=localhost;Port=5432;Database=culinary_blog;Username=postgres;Password=..."
#    $env:Jwt__SigningKey            = "chuoi_ky_ban >= 64 bytes"

# 3) Áp dụng migration (tự tạo DB `culinary_blog` nếu chưa có)
dotnet run --project src/backend/CulinaryBlog.API -- --migrate

# 4) (Tùy chọn) Nạp dữ liệu mẫu: 25 danh mục + 100 công thức
dotnet run --project src/backend/CulinaryBlog.API -- --seed

# 5) Chạy API
dotnet run --project src/backend/CulinaryBlog.API -- --urls http://localhost:5080
```

| Kiểm tra | Kết quả mong đợi |
|---|---|
| http://localhost:5080/health/live | `200` |
| http://localhost:5080/health/ready | `200` (Redis + Postgres sẵn sàng) |
| http://localhost:5080/scalar/v1 | Trang tài liệu API |

> 💡 API **tự migrate + seed lúc khởi động** (trừ khi chạy `--no-auto-migrate` hoặc môi trường `Testing`),
> nên bước 3/4 ở trên chỉ cần khi muốn chủ động chạy trước.

### 6.1. Cần tài khoản Admin? Dùng `--promote-admin` (B6, ✅ chốt 28/09: PA-A)

Tài khoản đăng ký qua UI/API mặc định là **Author**. Lệnh này nâng một tài khoản **đã tồn tại** lên role `Admin` mà **không cần mật khẩu**:

```powershell
# Nâng lên Admin (đăng nhập lại để nhận claim mới)
dotnet run --project src/backend/CulinaryBlog.API -- --promote-admin <email-cua-ban>
# Cũng chấp nhận dạng --promote-admin=<email> và không phân biệt hoa/thường
```

| Trường hợp | Kết quả | Exit code |
|---|---|---|
| Thiếu email | `Thieu email. Cach dung: ...` | `2` |
| Email sai định dạng | `Email khong hop le: '...'` | `2` |
| Chạy ở `Testing` / `Production` / bất kỳ môi trường nào khác `Development` | `Tu choi chay o moi truong '...'. Chi chay o Development.` | `2` |
| Email không tồn tại | `Khong tim thay tai khoan '...'` | `1` |
| Lần đầu | `Da them role Admin cho '<user>' (<id>).` | `0` |
| Chạy lại (đã là Admin) | `'<user>' da co role Admin. Khong thay doi gi.` | `0` |

Lưu ý an toàn:
- **Chỉ chạy ở `Development`.** Muốn kiểm tra ở môi trường khác phải bỏ launch profile để không bị ép `Development`:
  `dotnet run --project src/backend/CulinaryBlog.API --no-launch-profile -- --promote-admin <email>`.
- **Idempotent** — chạy bao nhiêu lần cũng chỉ có đúng **một** bản ghi role trong `UserRoles`.
- Không sửa `DbSeeder`, không sinh mật khẩu mới, không nhân bản account admin mặc định.
- Chỉ cần role `Admin` **đã có sẵn trong DB** (do `HasData`/seed tạo) — lệnh không tự tạo role.
- Sau khi nâng, **đăng xuất và đăng nhập lại** để JWT mới chứa claim role.

---

## 7. Bước 4 — Chạy Frontend (Next.js 15)

```powershell
Set-Location "D:\WNC\PTUDWNC-2026-Nhom16\src\frontend"

# Next.js KHÔNG đọc .env ở thư mục gốc — cần .env.local riêng cho frontend
Copy-Item ..\..\.env.example .env.local
# Trong .env.local, sửa khối "Frontend" cuối file:
#   NEXT_PUBLIC_API_URL=http://localhost:5080/api/v1
#   NEXT_PUBLIC_MEDIA_URL=http://localhost:5080/api/v1/resources/images
#   NEXT_PUBLIC_SITE_URL=http://localhost:3000

npm install
npm run dev
```

Mở **http://localhost:3000**. `NEXT_PUBLIC_API_URL` có sẵn default trong code
(`http://localhost:5080/api/v1`) nên API vẫn chạy được ngay cả khi thiếu `.env.local`. Các biến tuỳ chọn:

| Biến | Mặc định | Công dụng |
|---|---|---|
| `NEXT_PUBLIC_API_URL` | `http://localhost:5080/api/v1` | Địa chỉ API |
| `NEXT_PUBLIC_MEDIA_URL` | *(không có default trong code)* | Tiền tố URL ảnh (API trả `originalUrl`/`mediumUrl`/`thumbnailUrl` ở dạng **key**, ví dụ `recipes/{id}/{uuid}_300x300.png` — không có host). **Để trống thì UI hiện ô "Chưa cấu hình NEXT_PUBLIC_MEDIA_URL" chứ không hiện ảnh** ⇒ phải khai trong `src/frontend/.env.local`. Lưu ý: `<img>` không gửi header Bearer, nên xem trước ảnh của recipe **Draft** qua proxy sẽ 403 — xem `docs/evidence/TV4/Tuan03/Report/DE_XUAT_05_XEM_ANH_DRAFT_TRONG_WIZARD.md` |
| `NEXT_PUBLIC_SITE_URL` | `http://localhost:3000` | Site URL cho canonical/OG/sitemap (D4/D26) |

---

## 8. Bước 5 — Chạy bộ kiểm thử (bắt buộc `Skipped=0`)

```powershell
Set-Location "D:\WNC\PTUDWNC-2026-Nhom16"

# TEST_DATABASE lấy từ .env (EnvFileLoader nạp sẵn). Không có .env thì test tự dùng
# default khớp docker-compose.dev.yml (Username=postgres;Password=postgres).
# $env:TEST_DATABASE = "Host=localhost;Port=5432;Database=culinary_test;Username=postgres;Password=..."

# Kiểm tra mã + định dạng (giống hệt CI)
dotnet build CulinaryBlog.sln --configuration Release
dotnet format CulinaryBlog.sln --verify-no-changes --no-restore

# Toàn bộ test
dotnet test CulinaryBlog.sln

# Coverage + ngưỡng cổng G5 (80%) — chính xác lệnh CI chạy
# BẮT BUỘC có --results-directory, nếu không file coverage nằm trong tests/**/TestResults
# và deploy/check-coverage.sh sẽ không đọc được.
dotnet test CulinaryBlog.sln --no-build --configuration Release --collect:"XPlat Code Coverage" --results-directory TestResults
bash deploy/check-coverage.sh 80 TestResults
```

Kết quả chuẩn trên máy TV4 (**05/10/2026**, sau merge `origin/main` `2961a22` → commit `fd90572`):

```
Passed!  - Failed: 0, Passed: 421, Skipped: 0, Total: 421 - CulinaryBlog.Tests.dll
Passed!  - Failed: 0, Passed:   5, Skipped: 0, Total:   5 - ConcurrencySpike.dll
```

| Hạng mục | Số đo |
|---|---|
| Backend | **421 + 5 = 426/426**, `Skipped = 0` *(lần merge trước 05/10: 316 + 5 = 321/321 tại `72e4044`/`d4edfa2`)* |
| Coverage `CulinaryBlog.Application` | **96.31%** ≥ ngưỡng **80%** *(cũ: 84.13%)* — `Domain` 85.19% |
| `dotnet format CulinaryBlog.slnx --verify-no-changes` | exit `0` |
| `dotnet build` | 0 warning / 0 error |
| Playwright | **26/26**, 3 lần liên tiếp đều xanh |
| Jest | **85/85** |
| `npx tsc --noEmit` · `npm run lint` · `npm run build` | đều exit `0` |

### 6.1 Kiểm thử luồng publish (Playwright) và tải k6 — lệnh tuần 4

```powershell
Set-Location "D:\WNC\PTUDWNC-2026-Nhom16\src\frontend"
npm ci
npx playwright install chromium

# API + frontend đã chạy sẵn; E2E_START_BACKEND=0 để không tự bật lại backend
$env:E2E_START_BACKEND = "0"
npx playwright test --retries=0                 # 26/26

# Chỉ luồng publish, lặp 4 lần để bắt trường hợp chập chờn
npx playwright test recipe-publish --repeat-each=4 --retries=0
```

> 🔁 **Cách bắt lỗi chập chờn thật:** `recipe-publish` từng đỏ 2/3 lần vì wizard chuyển bước
> chưa kịp render. Không phải "flaky test" mà là **lỗi sản phẩm** — sửa bằng `?step=` ở
> `RecipeWizard.tsx` và `settleStep()` trong `recipe-publish.spec.ts`. Sau khi sửa: 3 lần full
> suite liên tiếp đều 26/26, `--repeat-each=4` là 16/16.

```powershell
Set-Location "D:\WNC\PTUDWNC-2026-Nhom16"
k6 run tests/performance/read-load.js      # ~3606 request, ~120 req/s, http_req_failed = 0.00%
pwsh -File deploy/outage-drill.ps1         # dừng Redis/S3/DB/worker, đo thời gian phục hồi
```

> ⚠️ **Outage drill chỉ là mô phỏng**: PostgreSQL chạy bằng dịch vụ native `postgresql-x64-18` trên
> máy TV4 nên **không dừng được** (thiếu quyền Administrator). Phần DB trong script trỏ một
> instance sang port đã chết để giả lập mất kết nối — **không phải failover thật**. Xem phần
> "Giới hạn" trong `SO_EVIDENCE_TUAN_4.md`.

> 🚨 **Quy tắc của nhóm (không được phá):** CI xanh mà `Skipped > 0` là **xanh giả**.
> Test E2E storage cố tình *skip an toàn* khi không kết nối được object storage —
> đó chính là lý do 5 run CI trước đó "xanh/skip" mà không ai thấy lỗi
> (xem `docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md`).
> Muốn E2E storage chạy thật thì container `culinaryblog-s3` phải `healthy`:
> ```powershell
> docker compose -f docker-compose.dev.yml ps s3
> ```

> 🔴 **Bài học từ lỗi CI `8d9d62b` (đọc trước khi chạy test):** test local xanh **không** bảo chứng
> CI xanh. Lỗi `GET /recipes/{slug}` trả `500` khi thiếu credential object storage chỉ lộ ra trên CI
> vì máy dev có `Minio__*` trong `.env` còn CI không có file `.env`. Khi sửa lỗi hạ tầng, kiểm lại
> bằng cách **dựng lại đúng điều kiện CI** (xoá `Minio__*`, thêm `Redis__Instance=ci`).

---

## 9. Bước 6 (tuỳ chọn) — Chạy lab L4 của TV4

Lab nằm ở **nhánh riêng** `practice/TV4/L4` (không có trong nhánh PR #16):

```powershell
git switch practice/TV4/L4

# Bật Mailhog (lab cần SMTP + API để đọc mail)
docker run -d --name lab-mailhog -p 1025:1025 -p 8025:8025 mailhog/mailhog:v1.0.1

# Biến môi trường cho lab
# LƯU Ý: lab nằm ở nhánh practice/TV4/L4 — nhánh đó CHƯA có EnvFileLoader,
# nên vẫn phải set thủ công (API ở nhánh chính thì đọc .env rồi, mục 0).
$env:TEST_DATABASE      = "Host=localhost;Port=5432;Database=culinary_test;Username=postgres;Password=admin123"
$env:Minio__Endpoint    = "127.0.0.1:9000"
$env:Minio__AccessKey   = "minioadmin"
$env:Minio__SecretKey   = "minioadmin"
$env:Minio__Bucket      = "culinary-blog"

# 4 phase yêu cầu (exit code 0 = PASS, 1 = FAIL)
dotnet run --project practice/TV4/L4 -- all

# phase chẩn đoán (không tính vào 4 phase)
dotnet run --project practice/TV4/L4 -- db
dotnet run --project practice/TV4/L4 -- purge

git switch 2312739_NHTSon_D3-D4-D5-D6   # quay lại nhánh chính
```

Log mỗi lần chạy nằm ở `out/lab-<RunId>.log`; sổ bằng chứng: `docs/evidence/TV4/Tuan03/SOK_LAB_L4.md`.

---

## 10. Checklist "đã chạy được"

```powershell
docker compose -f docker-compose.dev.yml ps                                   # 6 container, pg + s3 = healthy
docker exec culinaryblog-pg psql -U postgres -tAc "select 1"                  # Postgres OK
Invoke-WebRequest http://localhost:5080/health/ready -UseBasicParsing | Select-Object StatusCode   # 200
Invoke-WebRequest http://localhost:9000/health -UseBasicParsing | Select-Object StatusCode         # 200 (RustFS)
Invoke-WebRequest http://localhost:3000 -UseBasicParsing | Select-Object StatusCode                # 200
dotnet test CulinaryBlog.sln                                                  # 316 + 5, Skipped=0
```

---

## 11. Xử lý sự cố thường gặp

| # | Lỗi / triệu chứng | Nguyên nhân | Cách xử lý |
|---|---|---|---|
| L1 | `error during connect ... the daemon is not running` | Docker Desktop chưa chạy | Mở `Docker Desktop`, đợi có dòng `Server:` trong `docker version` |
| L2 | `Npgsql.PostgresException 28P01 password authentication failed for user "postgres"` | Mật khẩu trong volume ≠ mật khẩu bạn truyền vào | Mục 3 + **Cách A** mục 5 |
| L3 | `Failed to bind to address http://127.0.0.1:5432: address already in use` | Đã có PostgreSQL native trên Windows chiếm cổng | `Get-NetTCPConnection -LocalPort 5432 -State Listen` để xem; hoặc đổi cổng publish trong compose và sửa connection string cho khớp |
| L4 | `culinaryblog-pg` hoặc `culinaryblog-s3` `unhealthy` | Healthcheck sai/chưa sẵn sàng | `docker compose -f docker-compose.dev.yml logs --tail 50 s3`; RustFS health là `/health` (**không** phải `/minio/health/live`) |
| L5 | `42P03 database "culinary_blog" does not exist` | Chưa migrate | `dotnet run --project src/backend/CulinaryBlog.API -- --migrate` |
| L6 | Test đỏ hàng loạt với `Npgsql.PostgresException` | `TEST_DATABASE` sai hoặc lệch với mật khẩu DB thật | Sửa `TEST_DATABASE` trong `.env` (mục 8). Nhớ `.env` chỉ nạp 1 lần mỗi tiến trình — sửa xong phải chạy lại `dotnet test` |
| L6b | Sửa `.env` xong mà app vẫn dùng giá trị cũ | Biến đã `export`/`$env:` trong shell **thắng** `.env` | `echo $env:ConnectionStrings__Database`; xoá bằng `Remove-Item Env:ConnectionStrings__Database` (mục 5 Cách B) |
| L7 | `next build` báo `Dynamic server usage: Route /sitemap.xml` | Cố tĩnh pre-render sitemap khi backend chưa chạy | Đã xử lý bằng `export const dynamic = 'force-dynamic'` trong `src/frontend/src/app/sitemap.ts` — build vẫn exit 0 |
| L8 | `next lint` mở prompt hỏi cấu hình ESLint | Repo **chưa** có cấu hình ESLint | Không phải lỗi; kiểm tra FE bằng `npx tsc --noEmit` + `npm run build` |
| L9 | Ảnh recipe không hiện | `NEXT_PUBLIC_MEDIA_URL` sai hoặc để trống; hoặc dùng nhầm URL gốc của object storage | Đặt `NEXT_PUBLIC_MEDIA_URL=http://localhost:5080/api/v1/resources/images` (xem `docs/IMAGE_CONTRACT.md` §5). Ảnh recipe **Draft** vẫn 403 vì `<img>` không gửi Bearer — xem `docs/evidence/TV4/Tuan03/Report/DE_XUAT_05_XEM_ANH_DRAFT_TRONG_WIZARD.md` |
| L10 | `culinaryblog-seq` cứ `Restarting (1)`; log ghi `No default admin password was supplied` | Từ Seq 2026.1, lần chạy đầu **bắt buộc** có `SEQ_FIRSTRUN_ADMINPASSWORD` hoặc `SEQ_FIRSTRUN_NOAUTHENTICATION`; volume `seqdata` cũ chưa có cấu hình này | Compose đã đặt `SEQ_FIRSTRUN_NOAUTHENTICATION=true` và ghim tag `datalust/seq:2026.1`. Nếu container vẫn lỗi: xoá riêng volume seq rồi `up -d seq` → `docker volume rm <tên-thư-mục>_seqdata` |

---

## 12. Phụ lục A — Tương đương cho bash / Git Bash / WSL / CI

| PowerShell | bash |
|---|---|
| `$env:FOO = "bar"` | `export FOO="bar"` |
| `echo $env:FOO` | `echo $FOO` |
| `Remove-Item Env:FOO` | `unset FOO` |
| `Get-NetTCPConnection -LocalPort 5432` | `ss -ltnp \| grep 5432` |

Biến dùng trong toàn bộ hướng dẫn — **khai trong `.env` ở thư mục gốc** (trừ nhóm `NEXT_PUBLIC_*` khai trong `src/frontend/.env.local`):
`ConnectionStrings__Database`, `Jwt__SigningKey`, `ASPNETCORE_ENVIRONMENT`, `TEST_DATABASE`,
`POSTGRES_PASSWORD`, `Minio__Endpoint`, `Minio__AccessKey`, `Minio__SecretKey`, `Minio__Bucket`.

> 🔒 **Không commit secret.** `.env` đã nằm trong `.gitignore` **và** `.dockerignore` (nên không lọt vào
> Docker image). Mật khẩu `admin123`/`minioadmin` ở trên là giá trị **dev-only** — trong repo chỉ còn
> default `postgres`/`minioadmin` trong `appsettings.Development.json` và `docker-compose.dev.yml`.
> Production phải dùng object storage **có license** — không dùng RustFS
> (xem `docs/adr/ADR-TV4-002-doi-minio-sang-rustfs.md` §6).

---

## 13. Phụ lục B — Những gì đã kiểm chứng trên máy TV4 (28/09/2026)

| Hạng mục | Kết quả |
|---|---|
| `docker compose -f docker-compose.dev.yml up -d` | 6 container `Up`; `culinaryblog-pg`, `culinaryblog-s3` = `healthy` |
| API chạy đúng với biến trong hướng dẫn | `/health/live` = 200, `/health/ready` = 200, `/openapi/v1.json` = 200, `/scalar/v1` = 200; `--migrate` in *"Database migrations applied successfully."* |
| `http://localhost:9000/health` (RustFS) | 200 |
| `http://localhost:5341` (Seq) | 200 sau khi ghim `datalust/seq:2026.1` + `SEQ_FIRSTRUN_NOAUTHENTICATION=true` (trước đó container loop `Restarting (1)`) |
| `ALTER USER postgres PASSWORD '...'` qua `docker exec` | Thành công (socket `trust` không cần mật khẩu cũ); sau đó test sai/đúng mật khẩu qua container khác cho kết quả `28P01` / `1` như mong đợi |
| `pg_hba.conf` của `culinaryblog-pg` | `local ... trust`, `host ... 127.0.0.1/32 trust`, `host all all all scram-sha-256` → giải thích vì sao `psql` không có `-h` luôn "thành công" |
| `dotnet build CulinaryBlog.sln --configuration Release` | 0 warning, 0 error |
| `dotnet format CulinaryBlog.sln --verify-no-changes` | Sạch |
| `dotnet test CulinaryBlog.sln` | `311/311 + 5/5` tại `8d9d62b` (04/10) · `316/316 + 5/5` = `321/321` sau khi merge `main` (05/10) · **`421/421 + 5/5` = `426/426` sau merge `origin/main` `2961a22` → `fd90572` (05/10), `Skipped=0` |
| `npx tsc --noEmit` (frontend) | exit 0 |
| `npm run build` (frontend) | exit 0, 16/16 trang |
| CI sau khi push PR #16 | run `36391382819` — `172/172`, `Skipped=0` |
| CI sau khi push commit `8d9d62b` (04/10) | `Backend week 1` run `37213966752` — `311/311 + 5/5`, `Skipped=0`, coverage gate 80% pass · `Frontend CI` run `37213966761` — thành công |
| Playwright `npx playwright test --retries=0` | `26/26`, chạy **3 lần liên tiếp** đều xanh |
