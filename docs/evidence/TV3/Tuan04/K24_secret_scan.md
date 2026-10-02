# K24 — Secret scan cục bộ (gitleaks) — TV3, 02/10/2026

Công cụ: **gitleaks v8.30.1** (image `zricethezav/gitleaks:latest`), luật mặc định + bỏ qua thư mục build/phụ thuộc
(`node_modules/`, `.next/`, `bin/`, `obj/`, `TestResults/`, `playwright-report/`, `test-results/`, `*.zip`). Chạy với `--redact=100`:
mọi giá trị bị thay bằng `REDACTED`; file này không chép giá trị, không chép email tác giả. Không sửa `.github/`, không thêm bước CI.

## Lệnh
```powershell
# cấu hình: [extend] useDefault = true + [allowlist] paths như trên (file tạm, không commit)
# 1) Toàn bộ lịch sử git, mọi ref (--all: main, các nhánh TV3, nhánh lab, nhánh remote)
docker run --rm -v "D:\CulinaryBlog:/repo:ro" -v "<thư mục tạm>:/out" --entrypoint sh zricethezav/gitleaks:latest -c `
  "git config --global --add safe.directory '*' && gitleaks git /repo --log-opts='--all' --config /out/gitleaks.toml --redact=100 --report-format json --report-path /out/git_all.json --exit-code 0"
# 2) Thư mục làm việc (kể cả file chưa commit / bị .gitignore)
docker run --rm -v "D:\CulinaryBlog:/repo:ro" -v "<thư mục tạm>:/out" zricethezav/gitleaks:latest dir /repo --config /out/gitleaks.toml --redact=100 --report-format json --report-path /out/dir_sp.json --exit-code 0
docker run --rm -v "D:\CulinaryBlog-lab:/repo:ro" ... dir /repo ... --report-path /out/dir_lab.json
```

## Kết quả (đầu ra thật, đã che)
```
[git --all]  205 commits scanned. scanned ~4631543 bytes (4.63 MB) in 5.08s. leaks found: 8
[dir SP]     scanned ~4771519 bytes (4.77 MB) in 1.09s. leaks found: 4
[dir lab]    scanned ~1795905 bytes (1.80 MB) in 802ms. leaks found: 3
```

| # | Nguồn | Vị trí | Luật | Commit / trạng thái git | Đánh giá |
|---|---|---|---|---|---|
| 1–6 | lịch sử git | `docs/evidence/TV4/Tuan04/logs/seq_trace_recipes.log` dòng 29, 31, 33, 35, 37, 39 | generic-api-key | `71f55fd` (TV4), chỉ có trên `origin/2312739_NHTSon_D5-D6-D7` | **Dương tính giả**: chuỗi bị bắt là `ConnectionId=…` trong dòng log Serilog (ID kết nối Kestrel), không phải khoá |
| 7–8 | lịch sử git + thư mục SP/lab | `docs/GOOGLE_AUTH_CONTRACT.md` dòng 21 (`idToken`), 49 (`accessToken`) | generic-api-key | `3a8bcbe` (TV2), có trên `main` và mọi nhánh TV3 | **Dương tính giả**: giá trị chỉ là mẫu JWT bị cắt (31 và 39 ký tự, kết thúc `...`, không đủ 3 phần) |
| 9 | thư mục SP, lab | `.secrets.local.ps1` | generic-api-key | **không commit** — `.git/info/exclude` dòng 14 | Bí mật thật **chỉ ở máy** (đúng thiết kế); không có trong lịch sử git |
| 10 | thư mục SP | `src/frontend/.env.local` | generic-api-key | **không commit** — `.gitignore` dòng 4 `.env.*` | Cấu hình cục bộ; không có trong lịch sử git |

**Kết luận:** không tìm thấy bí mật thật nào đã commit trong lịch sử git (205 commit, mọi nhánh). Không có phát hiện nào trong file của TV3.
Bí mật thật chỉ nằm trong 2 file cục bộ bị git bỏ qua. Không cần handoff (cả 8 phát hiện đã commit đều là dương tính giả).

## Giới hạn
- gitleaks chỉ bắt theo mẫu/độ hỗn loạn; mật khẩu yếu kiểu `postgres`/`admin123` trong chuỗi kết nối mẫu (appsettings, fallback test) **không** bị luật mặc định bắt —
  đã rà tay trong tuần (G: thay bằng `$env:LAB_PG_PASSWORD` trong tài liệu TV3); appsettings/fallback của người khác không sửa.
- Chưa đưa secret scan vào CI (không sửa `.github`); đề xuất nhóm thêm sau khi thống nhất.
