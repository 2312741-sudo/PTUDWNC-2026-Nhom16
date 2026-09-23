# KẾ HOẠCH TUẦN 3 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

- **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026, giải quyết C01–C09)
- **Phần nghiệp vụ**: Xuất bản, hình ảnh, SEO và vận hành — tasks D3–D7 (theo `docs/KE_HOACH_DU_AN.md` mục 8 và `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md`).
- **Mã task tuần 3** (theo 6-tuần, dòng 3 TV4): D3 (archive/delete theo ADR D08), D4 (hoàn thiện uploader UI + status + SEO sitemap/robots/OG/JSON-LD), D5 (OTEL/metrics/health), D6 (lab L4 Identity/Google/refresh/forms/FTS). Cộng phần bàn giao thiếu tuần 2: D2 resize, D1.1c test MinIO down, E2E D1.3, D3.3 logout revoke chờ TV3 C5.
- **Nhánh Git đề xuất**: tuần 3 khởi động từ main đã cập nhật (PR #14 đã vào main) → `2312739_NHTSon_D3-D4-D5-D6`; lab: `practice/TV4/L4`.
- **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng).
- **Cổng**: G4 giữa tuần (publish/unpublish/archive end-to-end + ảnh hiển thị được qua presigned theo D27; sitemap Published-only) → G5 cuối tuần (đủ FR media/status/jobs/health; queue persistent; sitemap/OG/JSON-LD; OTEL trace HTTP→DB; CI xanh).

> Tuần 3 tính từ ngày 23/09/2026 (kết thúc tuần 2: block 2.1 đã gỡ hoàn toàn, D3.1/D3.2 + D1.3 đã trong main).
> Tài liệu này là **kế hoạch thực thi + khung minh chứng**, mọi việc ban đầu ở trạng thái **Chưa làm** trừ phần đã được đánh dấu hoàn thành.

---

## 0. Bối cảnh bước vào tuần 3 (ghi nhận thực tế 23/09)

- **PR #14 (D1.3 image API + D3.1/D3.2 publish/unpublish)** đã được merge nhầm vào main (commit `3eb6de3` + `c512943`) trước khi hoàn tất nghiệm thu trên nhánh TV4. **Quyết định nhóm**: giữ nguyên trên main, đưa việc rà soát/khắc phục vào kế hoạch tuần 3. Việc này đồng nghĩa D1.3/D3.1/D3.2 đã "chính thức" mặt bằng main và TV4 cần tập trung phía sau.
- **CI main đang đỏ (pre-existing từ tuần 2, không do D3 gây ra)**: 16 test Auth/Week3 fail vì `relation "RefreshTokens" does not exist`. Nguyên nhân kỹ thuật: **2 migration tạo trùng bảng** — `20260919061954_AddRecipeAggregate` đã `CreateTable("RefreshTokens")`, sau đó `20260923104044_AddRefreshTokens` `CreateTable("RefreshTokens")` lần nữa → `Migrate()` fail "already exists" → Npgsql rollback → DB thiếu `RefreshTokens`. Main `29b171c` cũng fail y hệt (run #failure trước khi nhánh TV4 merge). → **ưu tiên #1 tuần 3: sửa duplicate migration + CI xanh**, vì nó chặn mọi thành viên.
- Block còn lại từ tuần 2: C5 refresh của TV3 (ảnh hưởng D3.3 logout revoke), D27 bucket policy (ảnh hiển thị frontend), D23 queue resize (chưa chốt Hangfire/BackgroundService).

---

## 1. Mục tiêu tuần 3

| Tiêu chí | Bàn giao kỳ vọng |
|---|---|
| Nghiệp vụ | Archive/delete recipe theo ADR D08 (FR-RCP-006/007); hoàn thiện uploader UI + status/action buttons (FR-RCP-008, C4/TV3 editor); sitemap XML Published-only + robots + canonical + JSON-LD (FR-SEO, D26); OTEL/metrics/health thành phần (FR-OBS-001/003, D20/D21/D22); logout revoke refresh family nếu TV3 C5 bàn giao (FR-AUTH-005) |
| Nghiệm thu | Archive ẩn public ngay, giữ dữ liệu, owner/Admin; DELETE recipe soft theo D08, không lộ search/cache, không mất ảnh cần restore; uploader hiển thị ảnh bằng URL hợp lệ (D27); sitemap chỉ chứa Published, không chứa Draft/Archived; trace HTTP→DB có correlation; CI xanh toàn bộ |
| Skill | Chứng minh SP K01/K02/K03/K04/K05/K06/K07/K10/K12/K13/K20/K22/K23/K24; LAB L4 K04/K13/K14/K15/K12/K17/K19; chốt ADR D08/D17/D21/D22/D23/D26/D27 |

---

## 2. Hiện trạng repo tại thời điểm lập kế hoạch

| Mảng | Trạng thái | Ảnh hưởng đến TV4 tuần 3 |
|---|---|---|
| Block 2.1 (recipe cluster) | ✅ **Gỡ hoàn toàn** 23/09 (4/4 điều kiện) | Recipe CRUD + ingredient/step + D1.3 images có trong API; seed qua `POST /recipes` |
| D1.3 + D3.1/D3.2 | ✅ Trong main (PR #14, merge nhầm) | Base phát triển D3 archive/D4 UI; cần CI xanh và E2E thật trên MinIO |
| CI | 🔴 main đang đỏ (16 test Auth/Week3 fail — duplicate migration `RefreshTokens`) | **Chặn mọi người** → fix đầu tuần |
| D2 resize | ❌ Chưa bắt đầu (chờ chốt D23 queue) | Nếu chốt sớm, làm trong tuần 3 kèm D5 jobs |
| C5 refresh (TV3) | ❌ Chưa bàn giao | D3.3 logout revoke vẫn seam Bearer 204, gắn khi có C5 |
| D27 bucket policy | ❌ Chưa chốt nhóm | Uploader UI cần presigned/proxy để hiển thị ảnh |
| Frontend Next.js | Có skeleton tuần 1 (TV3/TV1 sở hữu chung) | D4 phối hợp TV3 (C4 editor/dashboard) + TV1 (host/deploy) |

---

## 3. Phân rã công việc tuần 3

### N0 — Mở đầu: fix CI main + rà soát PR #14 đã merge (ưu tiên cao)

**Skills**: K01, K23, K24 · **ADR**: D25 (lockfile) · **Block**: không — làm ngay

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | Sửa duplicate migration `RefreshTokens`: bỏ `CreateTable` trùng trong `AddRefreshTokens` (giữ thêm index `IDX_RefreshToken_Hash`/`IX_RefreshTokens_UserId` vào migration `AddRecipeAggregate` đã tạo bảng), hoặc tạo migration merge sạch | `Migrate()` chạy được trên DB mới; `dotnet test CulinaryBlog.sln` xanh (120 + 5 spike), CI main xanh |
| 2 | Rà soát diff PR #14 so với main trước merge (tính năng + test + docs) | Không xung đột logic; không lộ secret; test vẫn pass |
| 3 | Xóa/Cập nhật note CI fail cũ trong tài liệu tuần 2 sau khi xanh | Docs không chứa trạng thái hết hạn gây nhầm lẫn |

### N1 — D3: Archive/delete + invalidation + D3.3 logout revoke (theo FR-RCP-006/007, D08)

**Skills**: K01, K02, K03, K04, K07, K10, K12 · **ADR**: D08 (soft delete), D13 (unpublish ẩn ngay), D06/D05 (logout refresh revoke)

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | `PATCH /recipes/{id}/archive`: Published/Draft → Archived, ẩn public ngay, giữ dữ liệu, owner/Admin (403 non-owner); idempotent | Mã lỗi chuẩn + test happy/403/404 |
| 2 | `DELETE /recipes/{id}` (tuỳ ADR D08): soft delete theo ADR, không xoá vật lý ảnh cần restore; không lộ trong search/cache | Test delete + không còn trong public list/search |
| 3 | Invalidation khi archive/unpublish/delete: clear cache (Redis/OutputCache/ISR — phối hợp TV2/TV3) | Draft/Archived không được phục vụ bởi cache cũ |
| 4 | D3.3 logout revoke refresh family khi TV3 C5 bàn giao; giữ 204 idempotent | 204; refresh cũ không dùng được (test nếu có C5) |
| 5 | Hoàn tất E2E D1.3 + D1.1c: chạy với MinIO local: upload/PATCH primary/DELETE, MinIO down → lỗi rõ ràng + log redacted | Evidence log thật, không commit secret |

### N2 — D4: Uploader/status UI hoàn thiện + SEO (FR-RCP-008, FR-SEO, D26)

**Skills**: K16, K17, K18, K19, K22 · **ADR**: D17 (PATCH images), D26 (sitemap), D27 (presigned/proxy)

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | Uploader UI (Next.js + TanStack Query optimistic): progress, rollback khi lỗi, gallery/thumbnail, chọn primary | Progress thật; lỗi hiển thị + rollback; đúng 1 primary |
| 2 | Status/action buttons: Publish/Unpublish/Archive từ dashboard edit (phối hợp TV3 C4) | Đổi trạng thái end-to-end, reload UI theo trạng thái |
| 3 | Ảnh hiển thị qua presigned URL hoặc proxy có auth theo D27 | `<img>` hiển thị được; Draft/Archived ảnh không lộ public |
| 4 | Sitemap XML **Published-only** + robots.txt + canonical + JSON-LD (dữ liệu có cấu trúc); cron 02:00 UTC (D26) | Sitemap không chứa Draft/Archived; Google-tested file hợp lệ |

### N3 — D5: OTEL/metrics/health (FR-OBS-001/003, D20/D21/D22)

**Skills**: K20, K22, K23 · **ADR**: D20 (OTEL trace HTTP→DB), D21 (uptime 99,5%), D22 (Redis down fallback vs readiness)

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | Serilog/Seq/OTEL: trace HTTP→DB có correlation id qua Nginx (phối hợp TV1) | Trace thấy request đi qua Nginx→API→DB |
| 2 | Metrics: request duration/count, DB query time; health endpoint thành phần (db/redis/minio) đúng D22 | `/health` phản ánh đúng; Redis down → API fallback nhưng readiness 503 |
| 3 | EXPLAIN + k6/load cho publish/list query (K22) | Số liệu thật ghi trong sổ evidence |

### N4 — D2 (bàn giao thiếu tuần 2): Resize original/300×300/800×600 + job (FR-JOB-002/003, D23)

**Skills**: K13, K14, K15, K23 · **ADR**: D23 (queue persistent + retry) · **Block**: quyết định D23 của nhóm

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | Chốt queue với nhóm (Hangfire/BackgroundService) | Quyết định ghi ADR D23 |
| 2 | Resize tạo `{uuid}_original/300x300/800x600`, URLs DB, config kích thước không hard-code | Đủ 3 kích thước; original fallback khi lỗi |
| 3 | Job persistent: retry idempotent, restart worker không mất job; boundary delete vs resize | Test restart/retry; không tái sinh ảnh đã xoá |

### N5 — D6: Lab L4 hoàn thiện

**Skills**: K04, K12, K13, K14, K15, K17, K19

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | Nhánh `practice/TV4/L4`: upload/delete 4 MIME + resize 300×300/800×600 + Mailhog email + Hangfire delayed/restart | Commit lab + test |
| 2 | Lab theo PHAN_CHIA tuần 3 TV4: Identity/Google/refresh/forms/FTS (phần TV4 cần học) | PR lab ngoài phần chính, có sổ K |

---

## 4. Phụ thuộc & bàn giao tuần 3

### TV4 cần nhận:
| Từ | Nhận gì | Khi nào |
|---|---|---|
| TV3 (C5) | Refresh token family để logout revoke (D3.3) | Sớm tuần 3 |
| Cả nhóm | Quyết định D27 bucket policy (presigned/proxy vs public-read) | Đầu tuần 3 |
| Cả nhóm | Quyết định D23 queue (Hangfire vs BackgroundService) | Đầu tuần 3 |
| Nhóm trưởng | Xác nhận giữ nguyên PR #14 đã merge hay cần điều chỉnh | Đầu tuần 3 |

### TV4 phải bàn giao sớm:
| Bàn giao cho | Gì | Khi nào |
|---|---|---|
| Cả nhóm | Fix duplicate migration `RefreshTokens` + CI xanh | Ngày 1–2 |
| TV3 (C4) | Contract/status API archive + image URL hiển thị (presigned/proxy) | Giữa tuần 3 |
| Cả nhóm | E2E D1.3/D3 + D2 resize + D5 metrics chạy thật | Cuối tuần 3 |

### Thứ tự ưu tiên:
1. Fix CI main (N0) — chặn tất cả.
2. Chốt D27 + D23 (ảnh hiển thị + resize).
3. Archive/delete + invalidation (D3) → uploader UI ghép TV3 C4.
4. Sitemap/JSON-LD/OTEL (D4/D5) → cuối tuần chốt G5.

---

## 5. Ma trận skill tuần 3 TV4

| K | Loại | Sẽ chứng minh ở | Evidence key |
|---|---|---|---|
| K01 | SP | ADR D08/D17/D21/D22/D23/D26/D27 + mapping FR | TV4-K01 |
| K02 | SP | archive/delete status endpoints + RFC7807 | TV4-K02 |
| K03 | SP | presigned/proxy qua `IFileStorageService` + domain methods | TV4-K03 |
| K04 | SP+LAB | archive/delete CQRS handlers; LAB behavior | TV4-K04 |
| K05 | SP | validator status transition + sanitize UI input | TV4-K05 |
| K06 | SP | migration fix `RefreshTokens` + index mới | TV4-K06 |
| K07 | SP | concurrency khi đổi trạng thái/xoá (RowVersion) | TV4-K07 |
| K10 | SP | ownership archive/delete; 403 non-owner | TV4-K10 |
| K12 | SP+LAB | invalidation archive/unpublish/delete + LAB cache | TV4-K12 |
| K13 | SP+LAB | upload/delete + E2E MinIO + 4 MIME (lab) | TV4-K13 |
| K14 | SP+LAB | resize job + restart/retry (D23) | TV4-K14 |
| K15 | LAB | Mailhog + resize + sitemap (lab) | TV4-K15 |
| K16 | SP | uploader UI Next.js hoàn thiện | TV4-K16 |
| K17 | SP | TanStack Query optimistic + rollback | TV4-K17 |
| K18 | SP | WCAG2.1 AA + keyboard/loading/error checklist | TV4-K18 |
| K19 | SP | sitemap/robots/OG/JSON-LD Published-only | TV4-K19 |
| K20 | SP | OTEL trace + metric + health thành phần | TV4-K20 |
| K22 | SP | EXPLAIN + k6 + cache hit | TV4-K22 |
| K23 | SP | CI xanh + Compose/volumes + queue service | TV4-K23 |
| K24 | SP | PR nhỏ từng task + review Tâm | TV4-K24 |

---

## 6. Checklist cổng tuần 3

- [ ] CI main xanh: `dotnet test CulinaryBlog.sln` pass (120 + 5 spike); fix duplicate migration `RefreshTokens`.
- [ ] Archive: Archived ẩn public ngay, giữ dữ liệu, owner/Admin; DELETE soft theo D08; không lộ search/cache.
- [ ] Uploader UI: progress + rollback + gallery + primary; ảnh hiển thị qua presigned/proxy (D27).
- [ ] Status buttons Publish/Unpublish/Archive end-to-end với TV3 C4.
- [ ] Sitemap XML Published-only + robots + canonical + JSON-LD; cron 02:00 UTC (D26).
- [ ] Resize original/300×300/800×600 + queue persistent + retry + original fallback (nếu D23 chốt).
- [ ] Logout revoke refresh family (khi TV3 C5 có); 204 idempotent.
- [ ] OTEL trace HTTP→DB; health phản ánh đúng D22; EXPLAIN/k6 có số liệu.
- [ ] Lab `practice/TV4/L4` commit + sổ skill cập nhật; CI pass sau mỗi task; không commit secret.

---

## 7. Trở ngại dự kiến & cách xử lý

| Rủi ro | Ảnh hưởng | Giải pháp |
|---|---|---|
| Main CI đỏ kéo dài (duplicate migration) | Chặn mọi thành viên | Fix N0 ngay đầu tuần; phối hợp TV3 (migration thuộc cluster recipe) nếu cần |
| C5 TV3 chưa bàn giao | D3.3 logout chưa revoke family | Giữ seam Bearer 204; gắn khi có |
| D27 chưa chốt | Uploader ảnh không hiển thị | Tạm presigned tự động trong code; chốt nhóm trước G4 |
| D23 chưa chốt queue | Resize (D2) trễ | Dùng `BackgroundService` tối giản tạm nếu cần minh chứng, đổi Hangfire khi thống nhất |
| PR #14 đã merge nhầm gây xung đột docs/số liệu | Doc nhầm trạng thái | Rà soát diff, note rõ trong sổ evidence; đưa vào báo cáo nhóm |

---

## 8. Việc làm ngay khi được confirm

1. Fix duplicate migration `RefreshTokens` + push + CI xanh → báo nhóm.
2. Xác nhận với nhóm: giữ PR #14 trên main? D27 bucket? D23 queue?
3. Archive/delete CQRS + invalidation + test.
4. E2E D1.3 trên MinIO + D1.1c MinIO down/log redacted.
5. Presigned/proxy hiển thị ảnh → uploader UI + status buttons (ghép TV3 C4).
6. Sitemap/robots/JSON-LD + OTEL/metrics/health.
7. Resize job (tuỳ D23) + lab L4 + sổ evidence K.