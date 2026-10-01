# LAB K23 - TV3: build + chay 2 API lab sau Nginx. Can: $env:LAB_PG_PASSWORD. Khong ghi bi mat ra file.
param([switch]$NoBuild)
$compose = Join-Path $PSScriptRoot 'docker-compose.lab.yml'

# Rang buoc may 7.7 GB RAM / o C: it: khong du thi dung, khong build
$freeRam = (Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory / 1MB
$freeC = (Get-PSDrive C).Free / 1GB
Write-Output ("RAM trong {0:N2} GB, C: trong {1:N2} GB" -f $freeRam, $freeC)
if ($freeRam -lt 1.5 -or $freeC -lt 3) { Write-Output 'BLOCKED: can RAM trong >= 1.5 GB va C: >= 3 GB'; exit 2 }

if (-not $env:LAB_PG_PASSWORD) { Write-Output 'Thieu $env:LAB_PG_PASSWORD'; exit 1 }
if (-not $env:LAB_JWT_KEY) {
    # Khoa ky JWT ngau nhien chi song trong phien PowerShell nay; 2 instance nhan CUNG khoa qua compose
    $bytes = New-Object byte[] 48
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $env:LAB_JWT_KEY = [Convert]::ToBase64String($bytes)
}

if ($NoBuild) { docker compose -f $compose up -d } else { docker compose -f $compose up -d --build }
if ($LASTEXITCODE -ne 0) { Write-Output "docker compose up loi (exit $LASTEXITCODE)"; exit $LASTEXITCODE }

# Cho ca 2 instance san sang (goi thang cong 5090/5091)
foreach ($port in 5090, 5091) {
    $ok = $false
    for ($i = 0; $i -lt 60 -and -not $ok; $i++) {
        try { $ok = (Invoke-WebRequest "http://localhost:$port/lab/health" -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200 }
        catch { Start-Sleep -Seconds 2 }
    }
    Write-Output ("API cong {0}: {1}" -f $port, $(if ($ok) { 'san sang' } else { 'KHONG len' }))
}
docker ps --filter 'name=lab-tv3-' --format '{{.Names}} {{.Status}} {{.Ports}}'
