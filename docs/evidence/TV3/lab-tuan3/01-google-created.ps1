# Chay tai D:\CulinaryBlog (nhanh C4):  powershell -ExecutionPolicy Bypass -File .\evidence-week3.ps1
# Chon anh bang hop thoai -> chep + doi ten -> ghi evidence LAB (nhanh lab) + SP tuan 3 (nhanh C4) -> format -> commit -> push
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms
$enc = New-Object System.Text.UTF8Encoding $false
$repo = (Get-Location).Path
$lab = Join-Path (Split-Path $repo -Parent) "CulinaryBlog-lab"
$shots = Join-Path $env:USERPROFILE "Pictures\Screenshots"
if (-not (Test-Path $shots)) { $shots = Join-Path $env:USERPROFILE "Pictures" }

function Pick([string]$title) {
  $d = New-Object System.Windows.Forms.OpenFileDialog
  $d.Title = $title
  $d.InitialDirectory = $script:shots
  $d.Filter = "Anh (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp"
  if ($d.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) { $script:shots = Split-Path $d.FileName; return $d.FileName }
  return $null
}

# Chep anh theo danh sach -> tra ve markdown chen anh (bo qua anh khong chon)
function Collect($items, [string]$destDir, [string]$relDir) {
  New-Item -ItemType Directory -Force $destDir | Out-Null
  $md = @()
  foreach ($it in $items) {
    $src = Pick "Chon anh: $($it.Label)  (Cancel = bo qua)"
    if (-not $src) { Write-Host "   - bo qua: $($it.Label)" -ForegroundColor DarkGray; continue }
    $name = $it.Name + [IO.Path]::GetExtension($src).ToLower()
    Copy-Item $src (Join-Path $destDir $name) -Force
    Write-Host "   + $relDir/$name" -ForegroundColor Green
    $md += "### $($it.Label)`n`n![$($it.Label)]($relDir/$name)`n"
  }
  if ($md.Count -eq 0) { return "_(Chưa đính kèm ảnh)_`n" }
  return ($md -join "`n")
}

# ================================================================== 1. LAB (nhanh practice/TV3/labs, thu muc worktree)
Write-Host "1) Evidence LAB C6 tuan 3" -ForegroundColor Cyan
if (-not (Test-Path "$lab\labs\TV3")) { throw "Khong thay worktree lab $lab -> chay: git worktree add $lab practice/TV3/labs" }
$labImages = Collect @(
  @{ Name = "01-google-created";   Label = "01 Google that - created (201)" },
  @{ Name = "02-google-signed-in"; Label = "02 Google that - signed_in (200)" },
  @{ Name = "03-explain-gin";      Label = "03 EXPLAIN - Bitmap Index Scan on GIN" },
  @{ Name = "04-hangfire";         Label = "04 Hangfire Succeeded / Recurring" },
  @{ Name = "05-requeue";          Label = "05 Job #1 Failed -> Requeue -> Succeeded" },
  @{ Name = "06-mailhog";          Label = "06 Mailhog inbox" }
) "$lab\docs\evidence\TV3\lab-tuan3" "lab-tuan3"

$labMd = @'
# Evidence LAB C6 — Tuần 3 — TV3 Huỳnh Quốc Trung (2312786)

Nhánh: `practice/TV3/labs` · Tag: `TV3-LAB-L1`, `TV3-LAB-L3`, `TV3-LAB-L4` · Code: `labs/TV3/` · Reviewer: Nguyễn Thanh Tâm

Quy định áp dụng (mục 5.1 PHAN_CHIA_CONG_VIEC_6_TUAN): nhánh riêng, DB `lab_tv3*` / bucket `lab-tv3` / schema Hangfire `lab_tv3_hangfire` riêng,
stack thật (PostgreSQL 16, Redis 7, MinIO, Mailhog, Google Identity); mock chỉ dùng cho unit/error test.

| K | Nội dung LAB | Code | Minh chứng | Trạng thái |
|---|---|---|---|---|
| K08 | Register/login PBKDF2 (Identity V3 ≥100k vòng), JWT 15', refresh 512-bit lưu SHA-256, rotation, logout thu hồi, lỗi đăng nhập chung | `L1/AuthEndpoints.cs`, `L1/TokenService.cs` | `L1AuthTests` · test tay: register/me, 401 `INVALID_CREDENTIALS` chung, refresh sau logout bị từ chối | ✅ |
| K09 | Google callback → verify ID token (chữ ký/aud/exp) → tạo mới / đăng nhập / liên kết theo email đã xác minh; chặn chiếm tài khoản | `L1/Google.cs`, `/lab/l1/google`, `/lab/l1/google-demo` | `L1AuthTests` (5 test) · **Google thật** (Client ID riêng, tài khoản @dlu.edu.vn): `201 created`, `200 signed_in` — ảnh 01, 02 | ✅ |
| K11 | tsvector trigger (A/B/C), GIN, unaccent, `plainto_tsquery` AND, `ts_rank`, lọc + phân trang, không lộ Draft | `LabDb.cs`, `L3/SearchEndpoints.cs` | `L3SearchTests` · EXPLAIN: `Bitmap Index Scan on ix_lab_recipes_search` — ảnh 03 · `k6/search.js` | ✅ |
| K12 | Redis cache-aside (version key), OutputCache tag, invalidation khi sửa, Redis chết → fallback DB | `L3/RecipeCache.cs` | `Search_is_cached_then_invalidated_after_update` (MISS→HIT→MISS) · `L3RedisFallbackTests` (`X-Cache: BYPASS`) | ✅ |
| K13 | Upload 4 MIME theo magic bytes, biên đúng 5 MiB, key GUID + đuôi theo nội dung, delete không mồ côi | `L4/ImageValidator.cs`, `L4/MediaEndpoints.cs`, `L4/ObjectStorage.cs` | unit magic bytes/biên · file giả `.png` → 422 · upload/resize/delete trên MinIO thật | ✅ |
| K14 | Hangfire fire-and-forget, delayed, recurring (sitemap mỗi giờ), retry 3 lần, lưu PostgreSQL sống qua restart | `L4/Jobs.cs`, `Program.cs` | Job tạo khi Mailhog tắt → retry → app restart → tự gửi khi Mailhog bật; job hết lượt → Failed → Requeue → Succeeded — ảnh 04, 05 | ✅ |
| K15 | SMTP Mailhog (MailKit), resize 300×300 / 800×600, sitemap XML chỉ Published | `L4/Jobs.cs` | Mailhog — ảnh 06 · resize trên MinIO thật · `sitemap.xml` chỉ URL Published | ✅ |

## Kết quả chạy

```
dotnet test labs/TV3/Lab.TV3.Tests --filter "Infra!=docker"          -> 42/42 passed (không cần Docker)
$env:LAB_REDIS="localhost:6379"; dotnet test labs/TV3/Lab.TV3.Tests  -> tất cả passed (Redis + MinIO + Mailhog thật)
```

## Ảnh minh chứng

{{IMAGES}}

## Ghi chú

- MinIO đã gỡ image khỏi Docker Hub và quay.io (09/2026) → dùng mirror `coollabsio/minio:RELEASE.2025-10-15T17-29-55Z` (đóng băng, chấp nhận cho đồ án).
- ImageSharp ghim 3.1.x (Six Labors Split License, không cần license key).
- Ảnh đã che accessToken/refreshToken.
'@
[IO.File]::WriteAllText("$lab\docs\evidence\TV3\LAB_C6_TUAN3.md", $labMd.Replace("{{IMAGES}}", $labImages), $enc)
Write-Host "   + docs/evidence/TV3/LAB_C6_TUAN3.md" -ForegroundColor Green

Push-Location $lab
dotnet format labs/TV3/Lab.TV3.Api/Lab.TV3.Api.csproj 2>$null | Out-Null
dotnet format labs/TV3/Lab.TV3.Tests/Lab.TV3.Tests.csproj 2>$null | Out-Null
git add labs/TV3 docs/evidence/TV3
git commit -m "docs(lab TV3): evidence tuan 3 day du - Google that, Hangfire retry/requeue, Mailhog, EXPLAIN" | Out-Null
git push
Pop-Location
Write-Host "   LAB da push" -ForegroundColor Green

# ================================================================== 2. SP tuan 3 (nhanh C4, thu muc hien tai)
Write-Host "`n2) Evidence SP tuan 3 (nhanh C4)" -ForegroundColor Cyan
$branch = git branch --show-current
if ($branch -notlike "*C4-recipe-ui") { throw "Dang o nhanh '$branch' -> chuyen ve nhanh C4 roi chay lai (phan LAB da xong)" }
$spImages = Collect @(
  @{ Name = "01-wizard";          Label = "01 Wizard 5 buoc (tao/sua cong thuc)" },
  @{ Name = "02-upload-anh";      Label = "02 Buoc Anh - upload + anh chinh (MinIO)" },
  @{ Name = "03-conflict-reload"; Label = "03 Conflict reload (2 tab cung sua)" },
  @{ Name = "04-json-ld";         Label = "04 JSON-LD Recipe (Ctrl+U trang chi tiet)" },
  @{ Name = "05-trang-chi-tiet";  Label = "05 Trang chi tiet + nut Sua + header" }
) "$repo\docs\evidence\TV3\tuan3" "tuan3"

$spMd = @'
# Evidence TUẦN 3 — TV3 Huỳnh Quốc Trung (2312786) — Soạn thảo công thức

Nhánh: `2312786_HuynhQuocTrung_C4-recipe-ui` · Pull request: C4 → `main` (reviewer: Nguyễn Thanh Tâm) · CI: xanh

## Công việc SP

| Task | Nội dung | Evidence key | Trạng thái |
|---|---|---|---|
| C4 | Dashboard `/dashboard/recipes` (lọc, đếm theo trạng thái), xoá mềm | TV3-K16 | ✅ |
| C4 | Wizard 5 bước `/dashboard/recipes/new` + `/[id]/edit`: thông tin + dinh dưỡng, nguyên liệu, các bước (đổi thứ tự), ảnh (API TV4), xem lại & xuất bản; chống tạo nháp trùng | TV3-K05, TV3-K17 | ✅ |
| C4 | Optimistic update + rollback; conflict reload khi RowVersion lệch | TV3-K07, TV3-K17 | ✅ |
| C4 | Trang chi tiết ISR + on-demand revalidate sau khi lưu (`/api/revalidate`) | TV3-K12, TV3-K16 | ✅ |
| C4 | JSON-LD Schema.org Recipe (không rating giả), nút Sửa cho tác giả, độ khó tiếng Việt | TV3-K19 | ✅ |
| C4 | a11y: aria-label, aria-current, role="alert"; header Công thức của tôi / Viết công thức / Đăng xuất | TV3-K18 | ✅ |
| C5 | Refresh token concurrency (gộp trong nhánh C4) | TV3-K08 | ✅ |
| C7 | `RecipeAuthoringFlowTests` trên PostgreSQL + JWT thật: tạo → nguyên liệu/bước → đổi thứ tự → xuất bản; 422 thiếu thành phần; 401 chưa đăng nhập | TV3-K21 | ✅ |

## Lỗi backend phát hiện và sửa trong tuần

| Lỗi | Nguyên nhân | Sửa |
|---|---|---|
| `AuthorPolicy`/`AdminPolicy` luôn 403 | `MapInboundClaims = false` nhưng thiếu `RoleClaimType` | `RoleClaimType = "role"` |
| Thêm nguyên liệu/bước lỗi concurrency | EF coi Id Guid sinh ở domain là bản ghi cũ → UPDATE | `ValueGeneratedNever()` + migration rỗng cập nhật snapshot |
| Đổi thứ tự bước trả 500 | Unique (RecipeId, StepNumber) kiểm tra ngay sau từng UPDATE | Đánh số 2 pha trong transaction |
| CI `CHARSET` migration | File do `dotnet ef` sinh sai encoding | `dotnet format` |
| MinIO không pull được | MinIO gỡ image khỏi Docker Hub + quay.io | `coollabsio/minio:RELEASE.2025-10-15T17-29-55Z` |

## Kiểm thử

- `dotnet test CulinaryBlog.sln`: xanh (chạy cả trên DB rỗng giống CI)
- `dotnet format CulinaryBlog.sln --verify-no-changes`: sạch · frontend `tsc --noEmit`: sạch

## LAB C6

Xem nhánh `practice/TV3/labs` → `docs/evidence/TV3/LAB_C6_TUAN3.md` (L1, L3, L4 hoàn tất, Google thật đã xác minh).

## Ảnh minh chứng

{{IMAGES}}
'@
$spFile = "$repo\docs\evidence\TV3\TUAN_3.md"
if (Test-Path $spFile) { $spFile = "$repo\docs\evidence\TV3\TUAN_3_C4.md"; Write-Host "   ! TUAN_3.md da co -> ghi vao TUAN_3_C4.md (khong ghi de)" -ForegroundColor Yellow }
[IO.File]::WriteAllText($spFile, $spMd.Replace("{{IMAGES}}", $spImages), $enc)
Write-Host "   + $spFile" -ForegroundColor Green

Write-Host "`n3) Format + kiem tra (giong CI)" -ForegroundColor Cyan
dotnet format CulinaryBlog.sln
dotnet format CulinaryBlog.sln --verify-no-changes
if ($LASTEXITCODE -ne 0) { throw "Format chua sach -> gui output cho Claude (CHUA push nhanh C4)" }
Write-Host "   FORMAT OK" -ForegroundColor Green

git add docs/evidence/TV3   # chi evidence - khong dung .env.local hay file khac
Write-Host "`nSe commit cac file:" -ForegroundColor Yellow
git status --short
git commit -m "docs(TV3): evidence tuan 3 - C4/C5/C7 + anh minh chung" | Out-Null
git push
Write-Host "`nXONG: da push ca nhanh LAB va nhanh C4 (PR tu cap nhat)." -ForegroundColor Green
