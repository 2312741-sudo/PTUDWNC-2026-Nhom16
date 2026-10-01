# LAB K23 - TV3: kiem 2 instance sau Nginx (cong 8090): ca hai tra loi, tat 1 instance van phuc vu, bat lai.
param([int]$N = 20)
$base = 'http://localhost:8090'

function Hit([string]$path, [hashtable]$headers = @{}) {
    try {
        $r = Invoke-WebRequest "$base$path" -UseBasicParsing -TimeoutSec 5 -Headers $headers
        [pscustomobject]@{ Code = [int]$r.StatusCode; Instance = $r.Headers['X-Instance'] }
    } catch {
        $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        [pscustomobject]@{ Code = $code; Instance = '-' }
    }
}
function Summary($rows) {
    ($rows | Group-Object Code, Instance | ForEach-Object { "{0} x {1}" -f $_.Count, $_.Name }) -join '; '
}
function PostJson([string]$path, $body, [string]$token) {
    $h = @{}; if ($token) { $h['Authorization'] = "Bearer $token" }
    # Gui byte UTF-8: PS 5.1 mac dinh ma hoa body chuoi bang ISO-8859-1
    $bytes = [Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Compress))
    Invoke-RestMethod "$base$path" -Method Post -Body $bytes -ContentType 'application/json; charset=utf-8' -Headers $h
}

Write-Output "== 1. $N request GET /lab/health qua Nginx (round-robin)"
$rows = 1..$N | ForEach-Object { Hit '/lab/health' }
Write-Output ("   " + (Summary $rows))

Write-Output '== 2. Ghi du lieu qua Nginx: register/login o instance nay, dung token o instance kia'
$email = "k23-$([guid]::NewGuid().ToString('N').Substring(0,8))@lab.test"
$auth = (PostJson '/lab/l1/register' @{ email = $email; password = 'Lab-Pass9x'; displayName = 'K23 Smoke' }).data
$token = $auth.accessToken
foreach ($i in 1..4) { $null = PostJson '/lab/l10/posts' @{ title = "Bai K23 so $i" } $token }
$me = 1..6 | ForEach-Object { Hit '/lab/l1/me' @{ Authorization = "Bearer $token" } }
Write-Output ("   tao 4 bai; GET /lab/l1/me x6: " + (Summary $me))

Write-Output '== 3. Tat lab-tv3-api2, goi tiep qua Nginx'
docker stop lab-tv3-api2 | Out-Null
$down = 1..$N | ForEach-Object { Hit '/lab/health' }
$downMe = 1..5 | ForEach-Object { Hit '/lab/l1/me' @{ Authorization = "Bearer $token" } }
Write-Output ("   health: " + (Summary $down))
Write-Output ("   me:     " + (Summary $downMe))
$failed = @($down + $downMe | Where-Object { $_.Code -ne 200 }).Count

Write-Output '== 4. Bat lai lab-tv3-api2, cho Nginx het fail_timeout (12s)'
docker start lab-tv3-api2 | Out-Null
Start-Sleep -Seconds 12
$back = 1..$N | ForEach-Object { Hit '/lab/health' }
Write-Output ("   " + (Summary $back))

$both = @($rows | Select-Object -ExpandProperty Instance -Unique | Where-Object { $_ -like 'lab-tv3-api*' }).Count
Write-Output ("KET QUA: buoc1 so instance tra loi = {0}; khi tat api2 so request loi = {1}" -f $both, $failed)
exit $(if ($both -eq 2 -and $failed -eq 0) { 0 } else { 1 })
