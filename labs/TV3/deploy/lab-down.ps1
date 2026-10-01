# LAB K23 - TV3: tat va xoa moi container/network/volume lab-tv3-* cua compose lab (khong dung culinaryblog-*).
param([switch]$RemoveImage)
$compose = Join-Path $PSScriptRoot 'docker-compose.lab.yml'
# Gia tri gia chi de compose doc duoc file (bien :? bat buoc); down khong dung toi chung
if (-not $env:LAB_PG_PASSWORD) { $env:LAB_PG_PASSWORD = 'unused' }
if (-not $env:LAB_JWT_KEY) { $env:LAB_JWT_KEY = 'unused' }
docker compose -f $compose down -v --remove-orphans
# Container tam cua script backup file (neu con sot)
docker ps -a --filter 'name=lab-tv3-' --format '{{.Names}}' | Where-Object { $_ -like 'lab-tv3-*' } | ForEach-Object { docker rm -f $_ | Out-Null }
docker volume ls --filter 'name=lab-tv3-' --format '{{.Name}}' | Where-Object { $_ -like 'lab-tv3-*' } | ForEach-Object { docker volume rm $_ | Out-Null }
if ($RemoveImage) { docker image rm lab-tv3-api:dev 2>$null | Out-Null }
Write-Output 'Con lai (phai rong):'
docker ps -a --filter 'name=lab-tv3-' --format '{{.Names}}'
docker volume ls --filter 'name=lab-tv3-' --format '{{.Name}}'
