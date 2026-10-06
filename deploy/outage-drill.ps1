# N2-C1 / N2-C2 — dừng dịch vụ lần lượt, đo phản ứng HTTP và thời gian phục hồi.
# Chạy: powershell -NoProfile -ExecutionPolicy Bypass -File deploy/outage-drill.ps1
# Log : docs/evidence/TV4/Tuan04/logs/c1_outage_drill.log
#
# GIỚI HẠN MÔI TRƯỜNG (đã biết trước, ghi vào evidence để không ai tưởng là chưa làm):
#   * PostgreSQL chạy native dưới tài khoản dịch vụ; máy này không chạy admin nên
#     `Stop-Service` lẫn `pg_ctl stop` đều trả "Operation not permitted".
#     => PHASE DB dùng instance thứ hai trỏ sang port không có gì lắng nghe (5499).
#        Đây vẫn là lỗi Npgsql thật ở tầng app, chỉ khác là không cần quyền admin.
#        Lệnh chuẩn để chạy thật (cần admin): Stop-Service postgresql-x64-18.

$ErrorActionPreference = 'Continue'
$base = 'http://localhost:5080'
$dead = 'http://localhost:5081'   # instance trỏ port DB chết
$out = 'docs/evidence/TV4/Tuan04/logs/c1_outage_drill.log'
$null = New-Item -ItemType Directory -Force -Path (Split-Path $out)
Remove-Item $out -ErrorAction SilentlyContinue

function W($m) { Add-Content -Path $out -Value ((Get-Date).ToString('HH:mm:ss.fff') + '  ' + $m) }

function Probe($name, $url) {
    $code = try { (Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 20).StatusCode }
              catch { $c = [int]$_.Exception.Response.StatusCode; if ($c -eq 0) { 'ERR' } else { $c } }
    W ("  {0,-24} {1}" -f $name, $code)
    return $code
}

# Chờ tới khi endpoint trả về $want; đo thời gian phục hồi thật.
function WaitFor($label, $url, $want, $maxSec) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $maxSec) {
        $c = try { (Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 10).StatusCode }
                catch { $x = [int]$_.Exception.Response.StatusCode; if ($x -eq 0) { 'ERR' } else { $x } }
        if ("$c" -eq "$want") { W ("  RECOVER  {0,-22} {1,6:N1}s -> {2}" -f $label, $sw.Elapsed.TotalSeconds, $c); return $true }
        Start-Sleep -Milliseconds 500
    }
    W ("  STUCK    {0,-22} >{1}s (khong ve {2})" -f $label, $maxSec, $want)
    return $false
}

# Key ảnh đúng contract IMAGE_CONTRACT §1 = recipes/{recipeId}/{uuid}.ext.
# recipeId phải tồn tại thì proxy mới chạy tới StatObject; object thì cố tình KHÔNG tồn tại,
# nên S3 up -> 404 (image.not_found) va S3 down -> 503 (storage.unavailable).
$recipeId = 'ebaf8de7-cc57-4e46-bfe5-d05670da34b5'
$listUrl  = "$base/api/v1/recipes?page=1&pageSize=12"
$uncached = "$base/api/v1/recipes?page=$((Get-Random -Minimum 8000 -Maximum 9000))&pageSize=12"
$detailUrl = "$base/api/v1/recipes/khoai-lang-lac-pho-mai"
$imgUrl   = "$base/api/v1/resources/images/recipes/$recipeId/no-such-object.jpg"
$readyUrl = "$base/health/ready"

function Suite($label) {
    W "== $label =="
    return @{
        list     = Probe 'recipes.list(cached)'    $listUrl
        uncached = Probe 'recipes.list(uncached)'  $uncached
        detail   = Probe 'recipes.detail'          $detailUrl
        image    = Probe 'media.image(S3)'         $imgUrl
        ready    = Probe 'health/ready'            $readyUrl
    }
}

W 'N2-C1/C2 outage drill. 500 = BUG (loi chua duoc phan loai). 503 = DUNG (da bao loi tam thoi).'
W '---'

# PHASE -1: khoi dong API san. Script tu trun thanh nen chay duoc tren may sach.
#
# Chay DLL truc tiep, KHONG dung `dotnet run`: `dotnet run` tao them tien trinh con, nen `$p.Id` tro
# ve tien trinh cha — len `Stop-Process` cha, tien trinh con van chay lai va giu port 5080.
# Chay DLL thi $p.Id chinh la app, dung 1 lenh Stop-Process la don dep.
#
# DLL phai la ABSOLUTE: Start-Process -WorkingDirectory doi goc thu muc truoc khi chay, nen duong dan
# tuong doi ("src\backend\...\CulinaryBlog.API.dll") se khong tim thay file va app khong khoi dong.
$dll = (Resolve-Path 'src\backend\CulinaryBlog.API\bin\Debug\net10.0\CulinaryBlog.API.dll').Path
$wd  = (Resolve-Path 'src\backend\CulinaryBlog.API').Path

# Don sach: app chay DLL co ten tien trinh la "dotnet", nen Get-Process "CulinaryBlog.API" KHONG bat
# duoc. Phai theo PID tuong tu netstat.
foreach ($port in 5080, 5081) {
    $owning = netstat -ano | Select-String ":$port\s+.*LISTENING\s+(\d+)" | Select-Object -First 1
    if ($owning -and $owning.Matches[0].Groups[1].Value -notin $PID) {
        W "  don process cu tren port $port (pid $($owning.Matches[0].Groups[1].Value))"
        Stop-Process -Id $owning.Matches[0].Groups[1].Value -Force -ErrorAction SilentlyContinue
    }
}
Start-Sleep -Seconds 3

function StartApi($outLog, $errLog, $url, $dbConn) {
    if ($dbConn) { $env:ConnectionStrings__Database = $dbConn }
    if ($url)    { $env:ASPNETCORE_URLS = $url }
    $proc = Start-Process dotnet -ArgumentList "$dll --urls $url" -WorkingDirectory $wd `
        -RedirectStandardOutput $outLog -RedirectStandardError $errLog -WindowStyle Hidden -PassThru
    $env:ConnectionStrings__Database = $null
    $env:ASPNETCORE_URLS = $null
    return $proc
}

W '--- PHASE -1 - KHOI DONG API ---'
$apiProc = StartApi 'C:\Users\admin\AppData\Local\Temp\opencode\api_c1.log' 'C:\Users\admin\AppData\Local\Temp\opencode\api_c1.err' 'http://localhost:5080' $null
W ('  pid = ' + $apiProc.Id)
if (-not (WaitFor 'api.ready' "$base/health/ready" 200 150)) {
    W '  ABORT: API khong len duoc. Xem log api_c1.err.'
    Write-Output "FAILED (xem $out)"
    exit 1
}

$b = Suite 'PHASE 0 - BASELINE (Redis + Postgres + S3 deu up)'

W '--- PHASE 1 - DUNG REDIS ---'
docker stop culinaryblog-redis | Out-Null
Start-Sleep -Seconds 3
$p1 = Suite 'PHASE 1 - REDIS DOWN'
W '  Y nghia: doc van 200 nho fallback in-process cache; /health/ready 503 vi mat Redis.'
W '--- PHASE 2 - BAT LAI REDIS ---'
docker start culinaryblog-redis | Out-Null
WaitFor 'redis.list' $listUrl 200 90 | Out-Null
WaitFor 'redis.ready' $readyUrl 200 90 | Out-Null

W '--- PHASE 3 - DUNG S3/MINIO ---'
docker stop culinaryblog-s3 | Out-Null
Start-Sleep -Seconds 3
$p3 = Suite 'PHASE 3 - S3 DOWN'
W '  Y nghia: media.image phai doi tu 404 (object thieu) sang 503 (khong noi duoc S3).'
W '--- PHASE 4 - BAT LAI S3 ---'
docker start culinaryblog-s3 | Out-Null
WaitFor 's3.image' $imgUrl 404 120 | Out-Null
WaitFor 's3.ready' $readyUrl 200 120 | Out-Null

W '--- PHASE 5 - DB KHONG TRUY CAP DUOC (instance 5081 tro port 5499 chet) ---'
$deadOut = 'C:\Users\admin\AppData\Local\Temp\opencode\api_deaddb.log'
$deadErr = 'C:\Users\admin\AppData\Local\Temp\opencode\api_deaddb.err'
$deadProc = StartApi $deadOut $deadErr 'http://localhost:5081' 'Host=localhost;Port=5499;Database=culinary_blog;Username=postgres;Password=postgres;Maximum Pool Size=5'
W ('  pid = ' + $deadProc.Id + ' (Port=5499 khong co gi de loi -> dang chay that voi DB down)')
Start-Sleep -Seconds 10
# Chan doan: app co chet ngay khong? Log rong nghia la chua chay duoc toi muc nao het.
W ("  chan doan: exited=" + $deadProc.HasExited +
   ' out=' + (Get-Item $deadOut -ErrorAction SilentlyContinue).Length +
   ' err=' + (Get-Item $deadErr -ErrorAction SilentlyContinue).Length)
# Cho 180s chu khong 90s: khi DB khong truy cap duoc, Hangfire retry connection nhieu lan
# (da do: app chi bat dau LISTEN sau ~95s). Neu chi cho 90s, drill bao "STUCK" va ket luan
# sai rang app khong khoi duoc — trong khi thuc te no chi khoi dong lau hon.
$pd = @{}
$pd.alive  = WaitFor 'dead.khoi-dong' "$dead/health/live" 200 180
$pd.ready  = Probe 'dead.ready'        "$dead/health/ready"
$pd.list    = Probe 'dead.list'          "$dead/api/v1/recipes?page=1&pageSize=12"
$pd.uncach  = Probe 'dead.list(uncach)'  "$dead/api/v1/recipes?page=8899&pageSize=12"
$pd.detail  = Probe 'dead.detail'        "$dead/api/v1/recipes/khoai-lang-lac-pho-mai"
W '  Y nghia: moi duong co cham DB deu 503, khong duoc phat ra 500.'
Stop-Process -Id $deadProc.Id -Force -ErrorAction SilentlyContinue

W '--- PHASE 6 - DUNG HANGFIRE WORKER/API (kill tien trinh 5080) ---'
Stop-Process -Id $apiProc.Id -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 4
$pw = Probe 'worker.down.ready' $readyUrl
W '  Y nghia: node API/worker la SPOF - client thay connection refused, khong co failover.'

$after = @{}
W '--- PHASE 7 - KHOI DONG LAI API ---'
$apiProc2 = StartApi 'C:\Users\admin\AppData\Local\Temp\opencode\api_c1b.log' 'C:\Users\admin\AppData\Local\Temp\opencode\api_c1b.err' 'http://localhost:5080' $null
W ('  pid moi = ' + $apiProc2.Id)
WaitFor 'api.list'  $listUrl  200 120 | Out-Null
WaitFor 'api.ready' $readyUrl 200 120 | Out-Null
$after.list = Probe 'recipes.list(cached)' $listUrl
$after.image = Probe 'media.image(S3)' $imgUrl

W '--- TONG HOP ---'
W "  Baseline        list=$($b.list)/$($b.uncached)  image=$($b.image)  ready=$($b.ready)"
W "  Redis down      list=$($p1.list)/$($p1.uncached)  ready=$($p1.ready)   [mong doi 200/503]"
W "  S3 down         image=$($p3.image)  ready=$($p3.ready)   [mong doi 503]"
W "  DB unreachable  ready=$($pd.ready)  list=$($pd.list)  uncached=$($pd.uncach)  detail=$($pd.detail)"
W "  Worker/API down ready=$pw   [connection refused]"
W "  Phuc hoi        list=$($after.list)  image=$($after.image)"
Write-Output "OK -> $out"
