# CI đỏ ở nhánh lab — tái hiện và nguyên nhân (TV3, 02/10/2026)

> Trạng thái CI GitHub sau khi sửa: **chưa xác nhận CI GitHub** (chờ Trung báo kết quả run của `2159787`).
> Không có `gh`; trạng thái CI trước khi sửa lấy qua GitHub REST API công khai (không token). Log chi tiết của job cần đăng nhập nên không tải được.
> Mật khẩu: role Postgres tạm `culinary_ci_repro` dùng mật khẩu sinh ngẫu nhiên mỗi lần chạy, không lưu; đầu ra đã che.

## 1. Workflow CI (`.github/workflows/backend.yml`)
ubuntu-latest; service `postgres:16` (`POSTGRES_USER/DB=culinary_test`); job env `TEST_DATABASE=Host=localhost;...;Username=culinary_test;Password=<bí mật tạm của CI>`.
Thứ tự: `dotnet restore CulinaryBlog.sln --locked-mode` → `dotnet build --no-restore -c Release` → `dotnet format --verify-no-changes --no-restore`
→ `dotnet test CulinaryBlog.sln --no-build -c Release --collect:"XPlat Code Coverage" --logger trx --results-directory TestResults`
(2 project test: CulinaryBlog.Tests, ConcurrencySpike) → upload `TestResults`. Không có `SPIKE_DB` → spike dùng DB riêng `culinary_spike` suy từ TEST_DATABASE.

## 2. Nhánh nào đỏ (GitHub API, 02/10 ~12:10, trước khi sửa)
```
2312786_HuynhQuocTrung_C7-frontend-tests: 1d64d66 success, 64e8f8b success, e01c5f8 success, af534c1 success, adbf808 success, c2d349a success
practice/TV3/labs:                         55b9a57 failure, 14ab3f9 failure, 81c967b failure, d34f18b failure, 9e6622a failure
```
Run lab 36967316354: step 8 `dotnet test ...` = failure (restore/build/format success). Nhánh C7-frontend-tests **không** đỏ.

## 3. Nguyên nhân
**Nhánh lab thiếu commit `4bf775b` của chính TV3 (hqt7105, 26/09) đã có trên `main`**: trong `ApiFactory`
(`tests/CulinaryBlog.Tests/AuthTests.cs`) dòng `builder.UseSetting("ConnectionStrings:Database", TEST_DATABASE)`.
Nhánh lab rẽ khỏi `main` tại `4515021`, trước `4bf775b`, và chưa đồng bộ lại. Thiếu dòng này, API trong test dùng chuỗi kết nối trong
`appsettings.json` (chuỗi kết nối được đọc lúc dựng builder, trước khi cấu hình trong bộ nhớ của test được áp) thay vì TEST_DATABASE:
- máy dev có sẵn user/DB đó → test vẫn xanh nhưng ghi vào DB dev `culinary_blog`;
- CI không có user đó → lỗi `28P01 password authentication failed` ở các test đăng ký/đăng nhập.

**Đã sửa:** `2159787 fix(ci): lab dong bo ApiFactory UseSetting ConnectionStrings:Database tu TEST_DATABASE (theo 4bf775b tren main)` — 3 dòng, chỉ `AuthTests.cs`.

## 4. Tái hiện trên máy (Windows, Postgres 16 local, DB rỗng `culinary_ci_repro` + user riêng, Release, đúng lệnh CI)
"Chuỗi mặc định hỏng như CI" = đặt `ConnectionStrings__Database` tới một user không tồn tại, mô phỏng việc CI không có user trong appsettings.

| # | Repo / HEAD | Điều kiện | ConcurrencySpike | CulinaryBlog.Tests | Bảng trong DB TEST_DATABASE |
|---|---|---|---|---|---|
| 1 | SP `1d64d66` | như CI / `-maxcpucount:1` | 5/5 / 5/5 | 209/209 / 209/209 | không đo |
| 2 | lab `55b9a57` | như CI (máy dev) | 5/5 | 120/120 | **0** (test chạy vào `culinary_blog`) |
| 3 | lab `55b9a57` | chuỗi mặc định hỏng như CI | 5/5 | **102 pass / 18 fail** | — |
| 4 | SP `1d64d66` | chuỗi mặc định hỏng như CI | 5/5 | 209/209 | 14 |
| 5 | **lab `2159787`** (sau khi đồng bộ) | chuỗi mặc định hỏng như CI | **5/5** | **120/120** | **14** |

Đầu ra #3 và #5 (nguyên văn):
```
#3  Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 2 s - ConcurrencySpike.dll (net10.0)
    Failed!  - Failed:    18, Passed:   102, Skipped:     0, Total:   120, Duration: 10 s - CulinaryBlog.Tests.dll (net10.0)
#5  === HEAD: 2159787 ... restore exit 0, build Release exit 0, format --verify-no-changes exit 0
    Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 3 s - ConcurrencySpike.dll (net10.0)
    Passed!  - Failed:     0, Passed:   120, Skipped:     0, Total:   120, Duration: 19 s - CulinaryBlog.Tests.dll (net10.0)
```
18 test đỏ ở #3: 13 trong `AuthTests`, 5 trong `Week3AuthAndPersonalLabTests` — đều là test đăng ký/đăng nhập/refresh đi qua DB; lỗi gốc
`Npgsql.PostgresException : 28P01: password authentication failed`, các lỗi `Assert.Equal ... Values differ` / `NullReferenceException` là hệ quả
(đăng ký thất bại → không có token). Test của TV3 trên nhánh lab (Recipe*) dùng repository giả, không chạm DB nên không bị ảnh hưởng.

## 5. Đã loại trừ / còn lại
- Loại trừ có bằng chứng: chạy song song Spike/Tests (spike dùng DB riêng; song song và `-maxcpucount:1` đều xanh), migration trên DB rỗng,
  cấu hình Release.
- Chưa kiểm: khác biệt Linux (collation `en_US.utf8`, TZ `UTC`) — không cần nữa nếu CI xanh sau `2159787`; không chạy Docker vì RAM trống 0.8–0.9 GB.
- Còn là đoán: CI thật đỏ đúng 18 test này (chưa đọc được log job).
- Hệ quả phụ: các lần chạy test lab trước `2159787` đã ghi user test vào DB dev `culinary_blog` (đếm được 36 user mới trong ngày); chưa xoá.
