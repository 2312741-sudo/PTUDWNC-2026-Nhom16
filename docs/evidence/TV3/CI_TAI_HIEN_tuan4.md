# CI đỏ — tái hiện và nguyên nhân (TV3, 02/10/2026)

> Không có `gh`; trạng thái CI lấy qua GitHub REST API công khai (không token). Log chi tiết của job cần đăng nhập nên **không tải được**.
> Mật khẩu: role Postgres tạm `culinary_ci_repro` dùng mật khẩu sinh ngẫu nhiên mỗi lần chạy, không lưu; đầu ra đã che `***`.

## 1. Workflow CI (`.github/workflows/backend.yml`)
ubuntu-latest; service `postgres:16` (`POSTGRES_USER/DB=culinary_test`); job env `TEST_DATABASE=Host=localhost;...;Username=culinary_test;Password=<bí mật tạm của CI>`.
Thứ tự: `dotnet restore CulinaryBlog.sln --locked-mode` → `dotnet build --no-restore -c Release` → `dotnet format --verify-no-changes --no-restore`
(không có `--severity error`) → `dotnet test CulinaryBlog.sln --no-build -c Release --collect:"XPlat Code Coverage" --logger trx --results-directory TestResults`
(2 project test: CulinaryBlog.Tests, ConcurrencySpike — chạy song song) → upload `TestResults`. Không có `SPIKE_DB` → spike dùng DB `culinary_spike` suy từ TEST_DATABASE.

## 2. Nhánh nào đỏ (GitHub API, 02/10 ~12:10)
```
nhánh 2312786_HuynhQuocTrung_C7-frontend-tests: 1d64d66 success, 64e8f8b success, e01c5f8 success, af534c1 success, adbf808 success, c2d349a success
nhánh practice/TV3/labs:                         55b9a57 failure, 14ab3f9 failure, 81c967b failure, d34f18b failure, 9e6622a failure
main:  1881e8c … 4515021 failure (23/09)  →  b591e74 (merge PR #15, C4-recipe-ui, chứa 4bf775b) success  →  … 3d0695d success
```
Run lab mới nhất 36967316354: step 8 `Run dotnet test CulinaryBlog.sln --no-build --configuration Release ...` = failure; annotation duy nhất
`Process completed with exit code 1.` (restore/build/format đều success).
**Giả thuyết ban đầu "nhánh C7-frontend-tests đỏ" là sai** — nhánh đó xanh trên CI.

Nhánh lab rẽ từ `main` tại `4515021` (đang đỏ) và `git diff --stat <merge-base>..HEAD -- src tests .github` **rỗng**.

## 3. Tái hiện trên máy (Windows, Postgres 16 local, DB rỗng `culinary_ci_repro`, user riêng, Release, đúng lệnh CI)
| # | Repo/HEAD | Điều kiện | ConcurrencySpike | CulinaryBlog.Tests | Bảng trong DB TEST_DATABASE sau chạy |
|---|---|---|---|---|---|
| 1 | SP `1d64d66` | như CI (song song) | 5/5 | 209/209 | không đo |
| 2 | SP `1d64d66` | `-maxcpucount:1` | 5/5 | 209/209 | không đo |
| 3 | lab `55b9a57` | như CI | 5/5 | 120/120 | **0** → test chạy vào `culinary_blog` (DB dev) |
| 4 | lab `55b9a57` | chuỗi mặc định appsettings hỏng như CI (`ConnectionStrings__Database` user không tồn tại) | 5/5 | **102 pass / 18 fail** | — |
| 5 | SP `1d64d66` | như #4 | 5/5 | **209/209** | 14 |
| 6 | lab `55b9a57` + patch đề xuất (tạm, đã hoàn nguyên) | như #4 | 5/5 | **120/120** | 14 |

Đầu ra #4 (nguyên văn phần cuối):
```
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 2 s - ConcurrencySpike.dll (net10.0)
Failed!  - Failed:    18, Passed:   102, Skipped:     0, Total:   120, Duration: 10 s - CulinaryBlog.Tests.dll (net10.0)
```
Đầu ra #6:
```
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 2 s - ConcurrencySpike.dll (net10.0)
Passed!  - Failed:     0, Passed:   120, Skipped:     0, Total:   120, Duration: 17 s - CulinaryBlog.Tests.dll (net10.0)
```
DB dev `culinary_blog`: 36 user mới trong 30 phút (30 email `tv*@example.test`), muộn nhất 12:16:53 = đúng lần chạy #3.

## 4. 18 test đỏ ở #4 (từ file .trx; tác giả theo `git log`/`git blame`)
Tác giả: `AuthTests.cs` — Nguyễn Thanh Tâm (TV1) 298 dòng, Nguyễn Hữu Trung Sơn (TV4) 15 dòng; `Week3AuthAndPersonalLabTests.cs` — Nguyễn Thanh Tâm 198/198 dòng.

| Lớp | Test | Dòng lỗi đầu |
|---|---|---|
| AuthTests | Client_cannot_assign_admin_role | Assert.Equal() Failure: Values differ |
| AuthTests | Concurrent_registration_creates_exactly_one_account | Assert.Single() Failure: The collection did not contain any matching items |
| AuthTests | Disabled_account_cannot_login | Npgsql.PostgresException : 28P01: password authentication failed for user "ci_khong_co_role_nay" |
| AuthTests | Duplicate_email_is_case_insensitive_and_returns_problem | Assert.Equal() Failure: Values differ |
| AuthTests | Invalid_credentials_return_same_generic_error | Assert.Equal() Failure: Values differ |
| AuthTests | Invalid_registration_has_field_errors_and_no_account | Npgsql.PostgresException : 28P01: password authentication failed for user "ci_khong_co_role_nay" |
| AuthTests | Lockout_after_five_failed_attempts_locks_account_and_returns_423 | Assert.Equal() Failure: Values differ |
| AuthTests | Logout_with_valid_token_returns_no_content | System.NullReferenceException |
| AuthTests | Register_login_me_persist_hash_and_author_without_leaking_secrets | Assert.Equal() Failure: Values differ |
| AuthTests | Update_profile_cannot_modify_email_or_roles | System.NullReferenceException |
| AuthTests | Update_profile_patches_allowed_fields_successfully | System.NullReferenceException |
| AuthTests | Update_profile_rejects_xss_and_invalid_inputs (displayName `<script>`) | System.NullReferenceException |
| AuthTests | Update_profile_rejects_xss_and_invalid_inputs (avatar `javascript:`) | System.NullReferenceException |
| Week3AuthAndPersonalLabTests | Logout_with_refresh_token_revokes_token | System.NullReferenceException |
| Week3AuthAndPersonalLabTests | Refresh_token_invalid_or_nonexistent_returns_unauthorized | Assert.Equal() Failure: Values differ |
| Week3AuthAndPersonalLabTests | Refresh_token_reuse_triggers_family_revocation | System.NullReferenceException |
| Week3AuthAndPersonalLabTests | Refresh_token_rotates_and_issues_new_token_pair | System.NullReferenceException |
| Week3AuthAndPersonalLabTests | Register_and_login_return_valid_refresh_token | Assert.Equal() Failure: Values differ |

Các lớp còn lại trên lab (Recipe*, Category, Discovery, Architecture, ImageUpload, Health) xanh hết: test Recipe* của TV3 trên nhánh lab
dùng repository giả, không chạm DB → **không có test TV3 nào đọc chuỗi mặc định**.

## 5. Kết luận
- **Nguyên nhân (đã xác nhận)**: `Program.cs` (`1881e8c`) chụp chuỗi kết nối lúc dựng builder; `ApiFactory` của nhánh lab chỉ đặt TEST_DATABASE qua
  `ConfigureAppConfiguration` (áp sau) → app dùng chuỗi `postgres@culinary_blog` trong appsettings; CI không có role đó → 28P01.
  SP xanh vì đã có `4bf775b` (`builder.UseSetting("ConnectionStrings:Database", TEST_DATABASE)`).
- **Loại trừ**: (a) song song Spike/Tests — spike dùng DB riêng `culinary_spike`, chạy song song và `-maxcpucount:1` đều xanh; (e) migration trên DB rỗng — xanh trên DB rỗng; (f) Release — tái hiện đều ở Release.
- **Còn là đoán**: CI thật đỏ đúng 18 test này (không đọc được log CI); (b) Linux/collation và (c) múi giờ — **chưa kiểm** vì không chạy Docker
  (RAM trống 0.8–0.9 GB, chưa có image `dotnet/sdk:10.0`, `postgres:16`). Sau khi áp patch, nếu CI vẫn đỏ thì mới cần tới (b)/(c).
- **Sửa**: thuộc hạ tầng test của TV1 → không sửa; handoff + patch ở `handoff/TV1_CI_lab_ApiFactory_khong_dung_TEST_DATABASE.md`
  và `handoff/TV1_patch_lab_ApiFactory_UseSetting.diff` (không commit).
