# LAB C6 — TV3 Huỳnh Quốc Trung (2312786)

App thử nghiệm **độc lập** theo mục 5.1 `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md`: nhánh `practice/TV3/labs`,
database `lab_tv3` / `lab_tv3_test`, bucket `lab-tv3`, schema Hangfire `lab_tv3_hangfire`. **Không merge vào main.**

| Lab | Kỹ năng | Code | Test |
|---|---|---|---|
| L1 | K08 register/login/PBKDF2/JWT/refresh/logout · K09 Google verify/callback/link | `Lab.TV3.Api/L1` | `L1AuthTests` |
| L3 | K11 FTS trigger/GIN/unaccent/rank/AND/page + EXPLAIN · K12 Redis cache-aside/OutputCache/invalidation/fallback | `Lab.TV3.Api/L3`, `k6/search.js` | `L3SearchTests`, `L3RedisFallbackTests` |
| L4 | K13 upload/delete 4 MIME + magic bytes + biên 5 MiB · K14 Hangfire fire-and-forget/delayed/recurring/retry · K15 Mailhog/resize/sitemap XML | `Lab.TV3.Api/L4` | `L4MediaJobsTests` |
| L5/K16 | SSR search: HTML render phía server, encode XSS, noindex, phân trang (`/lab/l16/search`) | `Lab.TV3.Api/L16` | `L16SsrSearchTests` |
| L5/K19 | `sitemap.xml`, `robots.txt`, trang chi tiết canonical/OG, 301 slug cũ + URL cũ | `Lab.TV3.Api/L19` | `L19SeoTests` |
| L5/K20 | Serilog (Console/File/Seq theo cấu hình), correlation id, OTel trace/metric, `/health/live` `/health/ready` | `Lab.TV3.Api/L20` | `L20ObservabilityTests` |
| K22 | Đếm câu SQL/request (`X-Sql-Count`), N+1 vs 1 câu, EXPLAIN, cảnh báo `SLOW_SQL`, k6 p95<500 ms | `Lab.TV3.Api/L22`, `k6/search.js` | `L22PerformanceTests` |

## Chạy

```powershell
# Test không cần Docker/Redis (PostgreSQL local; mật khẩu nạp từ .secrets.local.ps1, không commit):
. .\.secrets.local.ps1; $env:LAB_PG = "Host=localhost;Port=5432;Username=postgres;Password=$env:LAB_PG_PASSWORD"
dotnet test labs/TV3/Lab.TV3.Tests --filter "FullyQualifiedName!~L3SearchTests" -m:1   # 82 test (02/10/2026)

# Khi có Docker: bật Redis + MinIO + Mailhog rồi chạy đủ test
docker compose -f labs/TV3/docker-compose.lab.yml up -d
$env:LAB_REDIS="localhost:6379"; dotnet test labs/TV3/Lab.TV3.Tests

# Chạy app lab: http://localhost:5090  (Hangfire: /lab/hangfire, Mailhog: http://localhost:8025, MinIO: http://localhost:9001)
dotnet run --project labs/TV3/Lab.TV3.Api
```

Không có Docker: tải bản Windows chạy trực tiếp — `minio.exe server D:\minio-data` (dl.min.io), `MailHog_windows_amd64.exe`
(github.com/mailhog/MailHog/releases), Redis 7 tương thích cho Windows (Memurai Developer).

## Demo cần quay/chụp làm evidence

1. **Hangfire restart (K14):** `POST /lab/l4/reminder {"email":"a@b.c","delaySeconds":60}` → tắt app → bật lại sau 30s → job vẫn chạy đúng giờ (lưu trên PostgreSQL). Chụp `/lab/hangfire`.
2. **Retry (K14):** tắt Mailhog → `POST /lab/l4/welcome` → job Failed/Retry trên dashboard → bật Mailhog → retry thành công, email hiện ở `:8025`.
3. **Redis down (K12):** `docker stop culinary-lab-tv3-redis-1` → `GET /lab/l3/search?q=pho` vẫn 200, header `X-Cache: BYPASS`.
4. **EXPLAIN (K11):** `GET /lab/l3/explain?q=pho&forceIndex=true` → `Bitmap Index Scan on ix_lab_recipes_search`.
5. **k6 (K22):** `k6 run labs/TV3/k6/search.js` → chụp p95.
6. **Google thật (K09):** tạo OAuth Client ID (Web) trên Google Cloud Console, thêm Authorized JavaScript origin `http://localhost:5090`,
   đặt `Google:ClientId` (user-secrets hoặc biến môi trường `Google__ClientId`) → mở `/lab/l1/google-demo` → đăng nhập → thấy `created`/`linked`.
   Chưa có Client ID ⇒ ghi "integration Google còn chờ" (mock chỉ dùng cho test lỗi, không tính hoàn tất).