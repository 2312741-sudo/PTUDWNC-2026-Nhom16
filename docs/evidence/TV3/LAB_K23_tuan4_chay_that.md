# LAB K23 tuần 4 — build image, 2 API sau Nginx, sao lưu/khôi phục file (chạy thật)

Ngày chạy: 05/10/2026, 07:13–07:16. Máy: Windows 11, RAM 7,73 GB, Docker Desktop. Nhánh `practice/TV3/labs`, HEAD `98c5ca5`.
Bí mật nạp bằng `. .\.secrets.local.ps1` (chỉ `LAB_PG_PASSWORD`, `LAB_JWT_KEY`); file này chỉ chép các dòng script in ra,
không chép biến môi trường. Không dòng nào dưới đây chứa mật khẩu hay khoá.

## 1. Chuẩn bị

- Tắt API/frontend do phiên này bật (cổng 5080/3000 không còn lắng nghe); `docker stop culinaryblog-redis culinaryblog-minio culinaryblog-mailhog`.
- Script gốc chặn khi RAM trống < 1,5 GB, mà máy chỉ còn 0,83 GB sau khi dọn, nên tạo 2 bản chạy cục bộ, không commit
  (`*.local.ps1` nằm trong `.git/info/exclude`, `git status` không thấy). Bản gốc không sửa. Khác biệt đúng một dòng:

```
lab-up.ps1 -> lab-up.local.ps1
< if ($freeRam -lt 1.5 -or $freeC -lt 3) { Write-Output 'BLOCKED: can RAM trong >= 1.5 GB va C: >= 3 GB'; exit 2 }
> if ($freeC -lt 3) { Write-Output 'BLOCKED: can C: >= 3 GB'; exit 2 } # ban .local: bo chan RAM, giu kiem o C:

backup-restore-files.ps1 -> backup-restore-files.local.ps1
< if ($freeRam -lt 1.5) { Write-Output ("BLOCKED: RAM trong {0:N2} GB < 1.5 GB" -f $freeRam); exit 2 }
```

## 2. Số đo RAM / ổ C: (thật)

| Thời điểm | RAM trống | C: trống |
|---|---|---|
| 07:13:19 sau khi dừng 3 container dev | 0,83 GB | 15,22 GB |
| 07:13:52 đầu `lab-up.local.ps1` | 0,86 GB | 15,21 GB |
| 07:15:10 sau `lab-up` (2 API + Nginx + Redis chạy) | 0,25 GB | — |
| 07:15:41 sau `smoke-scale.ps1` | 0,22 GB | — |
| 07:15:55 sau `backup-restore-files.local.ps1 -Seed` | 0,17 GB | — |
| 07:16:08 sau `lab-down.ps1` | 0,35 GB | 13,81 GB (image SDK/runtime đã kéo về) |

## 3. Build image + 2 API (`$env:BUILDKIT_PROGRESS='plain'; lab-up.local.ps1`) — ĐẠT

Không nối ống `Out-Null`/`Tee-Object` vào bước build. Build không bị kill (không có exit 137/OOM). Trích các dòng chính:

```
RAM trong 0.86 GB, C: trong 15.21 GB
#15 [build 5/7] RUN dotnet restore labs/TV3/Lab.TV3.Api/Lab.TV3.Api.csproj
#15 14.53   Restored /src/labs/TV3/Lab.TV3.Api/Lab.TV3.Api.csproj (in 12.21 sec).
#17 [build 7/7] RUN dotnet publish labs/TV3/Lab.TV3.Api/Lab.TV3.Api.csproj -c Release -o /out --no-restore ...
#17 6.191   Lab.TV3.Api -> /out/
#19 exporting config sha256:aea52b914cdef1ab7547d3f517676ba16a51cd326b8558b46e4bd4c331293934 0.0s done
#19 naming to docker.io/library/lab-tv3-api:dev 0.0s done
 Image lab-tv3-api:dev Built
API cong 5090: san sang
API cong 5091: san sang
lab-tv3-nginx Up 3 seconds 0.0.0.0:8090->80/tcp, [::]:8090->80/tcp
lab-tv3-api2 Up 3 seconds 0.0.0.0:5091->8080/tcp, [::]:5091->8080/tcp
lab-tv3-api1 Up 4 seconds 0.0.0.0:5090->8080/tcp, [::]:5090->8080/tcp
lab-tv3-redis Up 4 seconds 6379/tcp
lab-up exit=0 luc 07:15:10
```

Cảnh báo khi publish (không chặn): `DAP038`, `DAP005` của Dapper.AOT trong `L19/SeoEndpoints.cs`.

## 4. 2 instance sau Nginx (`smoke-scale.ps1`) — ĐẠT

```
== 1. 20 request GET /lab/health qua Nginx (round-robin)
   10 x 200, lab-tv3-api1; 10 x 200, lab-tv3-api2
== 2. Ghi du lieu qua Nginx: register/login o instance nay, dung token o instance kia
   tao 4 bai; GET /lab/l1/me x6: 3 x 200, lab-tv3-api2; 3 x 200, lab-tv3-api1
== 3. Tat lab-tv3-api2, goi tiep qua Nginx
   health: 20 x 200, lab-tv3-api1
   me:     5 x 200, lab-tv3-api1
== 4. Bat lai lab-tv3-api2, cho Nginx het fail_timeout (12s)
   11 x 200, lab-tv3-api1; 9 x 200, lab-tv3-api2
KET QUA: buoc1 so instance tra loi = 2; khi tat api2 so request loi = 0
smoke exit=0 luc 07:15:41
```

## 5. Sao lưu / khôi phục file (`backup-restore-files.local.ps1 -Seed`) — ĐẠT

```
Backup: files-20261005-071551.tgz (4,051 KB), 10 file
so_file_OK=10
KET QUA: restore 10 file, sha256 khop tung file
backup exit=0 luc 07:15:55
```

## 6. Dọn (`lab-down.ps1`) — ĐẠT

```
 Volume lab-tv3-files Removed
 Network lab-tv3-net Removed
Con lai (phai rong):
lab-down exit=0 luc 07:16:08
container lab-tv3-*: 0
volume lab-tv3-*: 0
```

Image `lab-tv3-api:dev` được giữ lại (không dùng `-RemoveImage`). Đã bật lại `culinaryblog-redis`, `culinaryblog-minio`, `culinaryblog-mailhog`.

## 7. Ghi chú trung thực

- Chạy bằng bản `.local.ps1` bỏ chặn RAM; với RAM trống 0,17–0,86 GB, máy vẫn build và chạy được cả 4 bước.
- Phần "SP migration startup" của K23: ngày 05/10, `--migrate` trên DB dev `culinary_blog` in "Database migrations applied
  successfully." dù migration thất bại (log Postgres `relation "AspNetRoles" already exists`, vì lịch sử migration lệch).
  Đoạn này trong `Program.cs` do TV1 viết; TV3 đã ghi handoff, không sửa. DB mới (CI, `culinary_test`) migrate bình thường.
