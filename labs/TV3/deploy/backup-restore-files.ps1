# LAB K23 - TV3: backup volume file lab-tv3-files (tar.gz + manifest sha256) -> restore vao volume moi
# lab-tv3-files-restore -> kiem sha256 tung file. Dung image redis:7-alpine da co san lam "hop cong cu" (busybox: tar, sha256sum).
param([switch]$Seed, [switch]$Keep)
$tool = 'redis:7-alpine'
$src = 'lab-tv3-files'
$dst = 'lab-tv3-files-restore'

$freeRam = (Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory / 1MB
if ($freeRam -lt 1.5) { Write-Output ("BLOCKED: RAM trong {0:N2} GB < 1.5 GB" -f $freeRam); exit 2 }

$dir = Join-Path $PSScriptRoot 'backups'
New-Item -ItemType Directory -Force $dir | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
docker volume create $src | Out-Null

# Gieo du lieu mau: 10 anh mon an that cua repo (mount CHI DOC) vao uploads/ cua volume
if ($Seed) {
    $images = Resolve-Path (Join-Path $PSScriptRoot '..\..\..\src\frontend\public\images\recipes')
    docker run --rm --name lab-tv3-fileseed -v "${src}:/data" -v "${images}:/seed:ro" $tool `
        sh -c 'mkdir -p /data/uploads && for f in $(ls /seed | head -n 10); do cp /seed/$f /data/uploads/; done'
}

# 1. Backup: manifest sha256 + tar.gz (volume nguon mount chi doc)
docker run --rm --name lab-tv3-filebackup -v "${src}:/data:ro" -v "${dir}:/backup" $tool `
    sh -c "cd /data && find . -type f | sort | xargs sha256sum > /backup/files-$stamp.sha256 && tar czf /backup/files-$stamp.tgz ."
if ($LASTEXITCODE -ne 0) { Write-Output 'Backup file loi'; exit 1 }
$count = (Get-Content (Join-Path $dir "files-$stamp.sha256")).Count
Write-Output ("Backup: files-{0}.tgz ({1:N0} KB), {2} file" -f $stamp, ((Get-Item (Join-Path $dir "files-$stamp.tgz")).Length / 1KB), $count)

# 2. Restore vao volume moi roi kiem checksum tung file
docker volume rm $dst 2>$null | Out-Null
docker volume create $dst | Out-Null
docker run --rm --name lab-tv3-filerestore -v "${dst}:/data" -v "${dir}:/backup:ro" $tool `
    sh -c "cd /data && tar xzf /backup/files-$stamp.tgz && sha256sum -c /backup/files-$stamp.sha256 > /tmp/r; rc=`$?; echo so_file_OK=`$(grep -c OK /tmp/r); exit `$rc"
$okCode = $LASTEXITCODE
if (-not $Keep) { docker volume rm $dst | Out-Null }
Write-Output $(if ($okCode -eq 0) { "KET QUA: restore $count file, sha256 khop tung file" } else { 'KET QUA: restore LOI/LECH checksum' })
exit $okCode
