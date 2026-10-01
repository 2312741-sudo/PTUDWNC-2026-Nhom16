# LAB K23 - TV3: Docker multi-stage, Compose 2 API + Nginx, backup/restore (nhanh practice/TV3/labs, KHONG merge)
- Ky nang: Dockerfile multi-stage, Compose (env/volume/network), Nginx load balancing + failover, scale 2 instance stateless, backup/restore DB + file.
- File tao: labs/TV3/deploy/{Dockerfile, Dockerfile.dockerignore, docker-compose.lab.yml, nginx.conf, .gitignore,
  lab-up.ps1, lab-down.ps1, smoke-scale.ps1, backup-restore-db.ps1, backup-restore-files.ps1}, Lab.TV3.Tests/L23ScalingTests.cs
- File sua: Lab.TV3.Api/Program.cs (middleware X-Instance)
- Commit do: f36ddf3 (2/2 FAIL, thieu header X-Instance) - LAB_K23_do.txt; commit xanh: 57cab5f - LAB_K23_xanh.txt
- Ket qua that: L23 2/2 Passed (2 app that cung DB lab_tv3_test: token + refresh phat o api1 dung duoc o api2); ca bo lab 61/61;
  docker compose config --quiet exit 0 (4 service lab-tv3-redis/api1/api2/nginx).
- Backup/restore DB (LAB_K23_restore_db.txt): lab_tv3 -> lab_tv3_restore khop 6 bang; lab_tv3_test -> lab_tv3_test_restore
  khop 8 bang (users 273, refresh 371, recipes 109, posts 36, comments 36, hangfire.job 14...). Lan 1 lo loi script (null=null), da sua.
- BLOCKED (rang buoc 8): RAM Available 508 / 660 / 429 MB < 1,5 GB qua 3 lan do -> KHONG chay docker build/run.
  Chua do: build image, goi qua Nginx thay 2 instance, tat api2 van phuc vu, backup/restore file volume. Script da viet, parse OK, chua chay.
- Lam tay khi RAM >= 1,5 GB (dong bot Chrome/VS Code), tu thu muc labs/TV3/deploy:
  $env:LAB_PG_PASSWORD="<mat khau postgres>"; $env:PGPASSWORD=$env:LAB_PG_PASSWORD
  .\lab-up.ps1                          # build multi-stage + chay 4 container
  .\smoke-scale.ps1 | Tee-Object ..\..\..\docs\evidence\TV3\LAB_K23_smoke.txt
  .\backup-restore-db.ps1               # sau smoke -> co lab_posts trong lab_tv3
  .\backup-restore-files.ps1 -Seed | Tee-Object ..\..\..\docs\evidence\TV3\LAB_K23_files.txt
  .\lab-down.ps1 -RemoveImage           # xoa sach lab-tv3-*; kiem: docker ps -a | Select-String lab-tv3
- Luu y: Postgres host phai nhan ket noi tu container (host.docker.internal); neu bi tu choi -> xem pg_hba.conf (chua kiem).
