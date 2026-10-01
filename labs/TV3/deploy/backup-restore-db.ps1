# LAB K23 - TV3: backup DB lab_tv3 (pg_dump -Fc) -> restore vao lab_tv3_restore -> so so dong cac bang chinh.
# Mat khau lay tu $env:PGPASSWORD (hoac $env:LAB_PG_PASSWORD), khong ghi vao file.
param([string]$Source = 'lab_tv3', [string]$Target = 'lab_tv3_restore', [switch]$Keep)

# Chot an toan: chi DB lab_tv3*, khong bao gio dung culinary_*
if ($Source -notmatch '^lab_tv3[a-z0-9_]*$' -or $Target -notmatch '^lab_tv3_[a-z0-9_]+$' -or $Source -eq $Target) {
    Write-Output 'Tu choi: chi duoc dung DB lab_tv3*, va Target phai khac Source'; exit 1
}
if (-not $env:PGPASSWORD) {
    if ($env:LAB_PG_PASSWORD) { $env:PGPASSWORD = $env:LAB_PG_PASSWORD } else { Write-Output 'Thieu PGPASSWORD'; exit 1 }
}
$bin = 'C:\Program Files\PostgreSQL\16\bin'
$conn = @('-h', 'localhost', '-p', '5432', '-U', 'postgres')
$dir = Join-Path $PSScriptRoot 'backups'
New-Item -ItemType Directory -Force $dir | Out-Null
$dump = Join-Path $dir ("{0}-{1}.dump" -f $Source, (Get-Date -Format 'yyyyMMdd-HHmmss'))

# 1. Backup dang custom (-Fc): nen, restore chon loc duoc, gom ca schema lab_tv3_hangfire
& "$bin\pg_dump.exe" @conn -Fc -f $dump $Source
if ($LASTEXITCODE -ne 0) { Write-Output 'pg_dump loi'; exit 1 }
Write-Output ("Backup: {0} ({1:N0} KB)" -f (Split-Path $dump -Leaf), ((Get-Item $dump).Length / 1KB))

# 2. Restore vao DB moi (xoa DB tam cu neu co)
& "$bin\dropdb.exe" @conn --if-exists $Target
& "$bin\createdb.exe" @conn $Target
& "$bin\pg_restore.exe" @conn --no-owner --exit-on-error -d $Target $dump
if ($LASTEXITCODE -ne 0) { Write-Output 'pg_restore loi'; exit 1 }

# 3. So so dong tung bang chinh giua nguon va ban restore (SQL qua file, tranh PS 5.1 lam hong dau ")
$wanted = 'lab_users', 'lab_refresh_tokens', 'lab_recipes', 'lab_images', 'lab_sitemaps', 'lab_posts', 'lab_comments', 'lab_tv3_hangfire.job'
$sql = Join-Path $dir 'count.sql'
# Chi so bang co that o DB nguon (bang cua LAB sau co the chua duoc tao neu API chua chay lai)
"SELECT t FROM unnest(ARRAY['" + ($wanted -join "','") + "']) t WHERE to_regclass(t) IS NOT NULL;" | Out-File $sql -Encoding ascii
$tables = @(& "$bin\psql.exe" @conn -d $Source -At -f $sql)
$missing = @($wanted | Where-Object { $tables -notcontains $_ })
if ($missing.Count) { Write-Output ("Bang chua co o {0} (bo qua): {1}" -f $Source, ($missing -join ', ')) }
if ($tables.Count -eq 0) { Write-Output 'Khong co bang nao de so'; exit 1 }
(($tables | ForEach-Object { "SELECT '$_', count(*) FROM $_" }) -join "`nUNION ALL ") + ';' | Out-File $sql -Encoding ascii
function Counts($db) {
    $map = @{}
    & "$bin\psql.exe" @conn -d $db -At -F '|' -f $sql | ForEach-Object { $p = $_ -split '\|'; $map[$p[0]] = [long]$p[1] }
    if ($LASTEXITCODE -ne 0) { Write-Output "psql loi khi dem $db"; exit 1 }
    $map
}
$src = Counts $Source
$dst = Counts $Target
Remove-Item $sql

$mismatch = 0
Write-Output ('{0,-22} {1,8} {2,8}  {3}' -f 'Bang', $Source, $Target, 'Khop')
foreach ($t in $tables) {
    # Thieu so (null) o mot ben cung tinh la LECH, khong duoc coi null = null la khop
    $same = ($null -ne $src[$t]) -and ($null -ne $dst[$t]) -and ($src[$t] -eq $dst[$t])
    if (-not $same) { $mismatch++ }
    Write-Output ('{0,-22} {1,8} {2,8}  {3}' -f $t, $src[$t], $dst[$t], $(if ($same) { 'OK' } else { 'LECH' }))
}
if (-not $Keep) { & "$bin\dropdb.exe" @conn --if-exists $Target; Write-Output "Da xoa DB tam $Target" }
Write-Output $(if ($mismatch -eq 0) { 'KET QUA: restore KHOP so dong tat ca bang' } else { "KET QUA: LECH $mismatch bang" })
exit $mismatch
