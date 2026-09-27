# Evidence LAB C6 — Tuần 3 — TV3 Huỳnh Quốc Trung (2312786)

Nhánh: `practice/TV3/labs` · Tag: `TV3-LAB-L1`, `TV3-LAB-L3`, `TV3-LAB-L4` · Code: `labs/TV3/` · Reviewer: Nguyễn Thanh Tâm

Quy định áp dụng (mục 5.1 PHAN_CHIA_CONG_VIEC_6_TUAN): nhánh riêng, DB `lab_tv3*`/bucket `lab-tv3` riêng, stack thật;
mock chỉ cho unit/error test.

| K | Nội dung LAB | Code | Test / lệnh | Trạng thái |
|---|---|---|---|---|
| K08 | Register/login PBKDF2 (Identity V3 ≥100k vòng), JWT 15', refresh 512-bit lưu SHA-256, rotation, logout thu hồi, lỗi đăng nhập chung | `L1/AuthEndpoints.cs`, `L1/TokenService.cs` | `L1AuthTests` (7 test) | ✅ Code + test pass |
| K09 | Google callback → verify ID token (chữ ký/aud/exp) → tạo mới / đăng nhập / liên kết theo email đã xác minh; chặn chiếm tài khoản | `L1/Google.cs`, `/lab/l1/google`, `/lab/l1/google-demo` | `L1AuthTests` (5 test Google, verifier thật: thiếu ClientId + token hỏng) | ⏳ Logic + test lỗi xong; **integration Google thật chờ Client ID** |
| K11 | tsvector trigger (A/B/C), GIN, unaccent, `plainto_tsquery` AND, `ts_rank`, lọc + phân trang, không lộ Draft, EXPLAIN | `LabDb.cs`, `L3/SearchEndpoints.cs` | `L3SearchTests` (7 test) · `k6/search.js` | ✅ Code + test pass · ⏳ ảnh k6 |
| K12 | Redis cache-aside (version key), OutputCache tag, invalidation khi sửa, Redis chết → fallback DB | `L3/RecipeCache.cs` | `L3RedisFallbackTests` ✅ · `Search_is_cached_then_invalidated_after_update` [docker] | ✅ Fallback · ⏳ HIT/MISS chờ Docker |
| K13 | Upload 4 MIME theo magic bytes, biên 5 MiB, key GUID + đuôi theo nội dung, delete không mồ côi | `L4/ImageValidator.cs`, `L4/MediaEndpoints.cs`, `L4/ObjectStorage.cs` | `L4MediaJobsTests` (unit + API) · upload MinIO [docker] | ✅ Validation · ⏳ MinIO chờ Docker |
| K14 | Hangfire fire-and-forget (welcome), delayed (reminder), recurring (sitemap), retry, persistence PostgreSQL | `L4/Jobs.cs`, `Program.cs` | `L4MediaJobsTests` (enqueue/scheduled/recurring) | ✅ Code + test · ⏳ video restart |
| K15 | SMTP Mailhog (MailKit), resize 300×300/800×600, sitemap XML chỉ Published | `L4/Jobs.cs` | `Sitemap_xml_lists_only_published_recipes` ✅ · resize [docker] | ✅ XML · ⏳ Mailhog/resize chờ Docker |

## Kết quả chạy

```
dotnet test labs/TV3/Lab.TV3.Tests --filter "Infra!=docker"
<dán kết quả: Passed/Failed/Total>
```

## Trở ngại

- Docker Desktop trên máy chưa khởi động được (WSL2) → test `[Trait("Infra","docker")]` (Redis HIT/MISS, MinIO, resize) chưa chạy.
  Hướng xử lý: sửa WSL2 hoặc chạy bản Windows của MinIO/Mailhog/Redis (README).
- Chưa có Google OAuth Client ID → K09 integration thật đang chờ.