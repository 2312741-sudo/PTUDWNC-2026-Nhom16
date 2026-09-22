# KẾ HOẠCH TUẦN 2 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

- **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026, giải quyết C01–C09)
- **Phần nghiệp vụ**: Xuất bản, hình ảnh, SEO và vận hành — tasks D1–D7 (theo `docs/KE_HOACH_DU_AN.md` mục 8 và `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md`).
- **Mã task tuần 2**: D1 (upload/metadata/primary/delete), D2 (resize 300×300/800×600 + jobs), D3 (publish/unpublish CQRS + logout revoke refresh từ TV3 C5), D4 (nền image uploader/status UI + SEO/sitemap).
- **Nhánh Git đề xuất**: tiếp tục `2312739_NHTSon_D1-D3-D5-D6`; lab: `practice/TV4/L4`.
- **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng).
- **Cổng**: G2 giữa tuần (upload ảnh + set primary chạy) → G3 cuối tuần (Draft có ảnh/nguyên liệu/bước → publish/unpublish chạy end-to-end, không lộ Draft ra public).

> Tuần 2 tính từ khi Tuần 1 đã đạt G1 (CI build/test pass + stack Compose chạy).
> Tài liệu này là **kế hoạch thực thi + khung minh chứng**, mọi việc ban đầu ở trạng thái **Chưa làm**.

---

## 1. Mục tiêu tuần 2

| Tiêu chí | Bàn giao kỳ vọng |
|---|---|
| Nghiệp vụ | Upload ảnh recipe + metadata + primary + delete (FR-FILE-001/002, FR-RCP-008); resize original/300×300/800×600 (D2); publish/unpublish draft với điều kiện ≥1 ingredient + ≥1 step (FR-RCP-005/006, C02); nền image uploader + status UI + SEO/sitemap (D4) |
| Nghiệm thu | 4 định dạng ≤5MiB kèm magic bytes; đúng **1 primary**; path theo FR-RCP-008 `recipes/{recipeId}/{uuid}.{ext}`; publish thiếu thành phần trả 422 `RECIPE_PUBLISH_INCOMPLETE`; Draft/Archived **không lộ** public (kể cả ảnh private bucket theo D27) |
| Skill | Chứng minh SP K01/K02/K03/K04/K06/K07/K10/K12/K13/K20/K23/K24; LAB L4 nền K04/K13/K14/K15; chốt ADR D17/D21/D22/D23/D27 |

---

## 2. Hiện trạng repo tại thời điểm lập kế hoạch

| Mảng | Trạng thái | Ảnh hưởng đến TV4 tuần 2 |
|---|---|---|
| Stack Compose + health | Tuần 1 đã chạy (Postgres/Redis/MinIO/Mailhog/Seq/Nginx), CI pass | Nền để chạy integration test upload/resize/publish |
| `IFileStorageService` + `StoredFile` | Contract có ở Application (`Storage.cs`); `MinioOptions` + DI đã đăng ký | Tuần 2 implement `MinioStorageService` thật + endpoint |
| `POST /auth/logout` | Tuần 1 xong (Bearer, 204/401) | Tuần 2 gắn revoke refresh family khi TV3 C5 bàn giao |
| Recipe/RecipeImage/Ingredient/Step/Nutrition entities | Đã có ở Domain (`Recipe.cs`, `RecipeImage.cs`, ...), migration + config trong Infrastructure | D1/D2/D3 dựng trên entity này; cần xác nhận trạng thái task TV3 (C1/C2) đã merge |
| Cấu trúc ảnh | `RecipeImage` entity có; chưa có endpoint upload/primary/delete | Làm D1 (API) + D2 (resize) + nền D4 (UI) |
| Category + Auth | Tuần 1 xong | Tái sử dụng pattern endpoint/handler/validator |
| `.env.example`, ADR TV4-001 | Tuần 1 xong (D22/D27/D06/D05/D25) | Tuần 2 bổ sung ADR D17/D21/D23 nếu kết luận mới; confirm D27 bucket policy |

---

## 3. Phân rã công việc tuần 2

### N1 — D1: Upload/metadata/primary/delete (+ MinioStorageService thật) (15đ)

**Skills**: K01, K02, K03, K06, K07, K10, K12, K13 · **ADR**: D27 (bucket), D17 (PATCH images), D07 (kiểm tra publish độc lập)

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | Implement `MinioStorageService` trong Infrastructure (MinIO SDK có lockfile; dùng `IFileStorageService`), đăng ký DI | Application/API dùng interface; MinIO qua `MinioOptions` |
| 2 | Validate upload: **4 MIME** (JPEG/PNG/WebP/AVIF theo L4 hoặc danh sách SRS), **≤5MiB**, magic bytes đối chiếu nội dung, GUID path `recipes/{recipeId}/{uuid}.{ext}` (FR-RCP-008) | 400 khi sai MIME/quá size/file giả; đường dẫn chuẩn |
| 3 | Endpoints: `POST /recipes/{id}/images` (201), `PATCH /recipes/{id}/images/{imageId}` (isPrimary/altText/orderIndex — D17), `DELETE /recipes/{id}/images/{imageId}` (204); ảnh đầu tiên tự động thành primary | Đúng 1 primary; ownership đúng (403 non-owner); mọi imageId thuộc recipeId trên URL |
| 4 | Xoá object MinIO tương ứng khi delete; không giữ orphan (bàn luận: soft delete recipe → ảnh giữ hay xoá? tuần này xoá hẳn khi delete image) | Không rò rỉ object |
| 5 | Transaction + RowVersion xử lý race set primary (K07) | Test 2 writer không tạo 2 primary |
| 6 | Contract API hoàn chỉnh cho TV3 tích hợp editor (`docs/IMAGE_CONTRACT.md`) | TV3 review giữa tuần |

### N2 — D2: Resize original/300×300/800×600 + jobs nền (10đ)

**Skills**: K13, K14, K15 (nền), K23 · **ADR**: D23 (queue persistent + retry)

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | Resize ảnh khi upload tạo **original + 300×300 + 800×600**; URLs lưu DB; original fallback khi resize lỗi | Đủ 3 kích thước, dung lượng theo NFR |
| 2 | Dùng job queue persistent (Hangfire/BackgroundService theo quyết định nhóm) để resize bất đồng bộ, retry idempotent | Lỗi enqueue không nuốt im; job restart/retry có test (D23) |
| 3 | Config kích thước qua config, không hard-code | Dễ thay đổi |

### N3 — D3: Publish/unpublish CQRS + ownership + logout revoke refresh (13đ)

**Skills**: K01, K02, K03, K04, K07, K10, K12 · **ADR**: D07 (publish theo Phụ lục B), D06/D05 (logout refresh revoke)

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | `PATCH /recipes/{id}/publish`: điều kiện **≥1 ingredient VÀ ≥1 step** (C02) → thiếu: 422 `RECIPE_PUBLISH_INCOMPLETE` | Mã lỗi chuẩn, test happy + 422 |
| 2 | `PATCH /recipes/{id}/unpublish`: public ẩn ngay (cache invalidation API/Redis/ISR) | Published → Draft ngay; không cache response cũ (D13) |
| 3 | Ownership: chỉ author sở hữu mới publish/unpublish recipe của mình; Admin override theo quyền | 403 non-owner |
| 4 | Logout revoke refresh family khi TV3 C5 bàn giao (tuần 1 đã làm seam) | Kết nối C5, test 204 idempotent |

### N4 — D4 (nền): Image uploader/progress + status UI + SEO/sitemap nền (12đ — nền tuần này, đủ tuần 3)

**Skills**: K05, K16, K17, K18, K19, K22 · **ADR**: D26 (sitemap external), D21 (uptime)

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | Uploader UI (Next.js) có progress/rollback, gallery + thumbnail, chọn primary (TanStack Query optimistic) — nền | Progress thật; ảnh hiển thị bằng URL hợp lệ (private bucket cần presigned/proxy — bàn theo D27) |
| 2 | Status action button (Draft → Published/Unpublished) nền | Gọi publish/unpublish API |
| 3 | Sitemap XML Published-only + robots + canonical; cron 02:00 UTC (nền, đầy đủ tuần 3) | Sitemap không chứa Draft/Archived |

### N5 — D6 (tiếp): Lab L4 + sổ skill

**Skills**: K04, K13, K14, K15, K20, K23 (nền)

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | Tạo nhánh `practice/TV4/L4` từ G1: upload/delete 4 MIME + resize 2 size + Mailhog email + Hangfire delayed/restart | Nhánh lab có commit + test |
| 2 | Cập nhật sổ evidence `TV4-Kxx` theo thực tế tuần 2 | Sổ K trạng thái rõ ràng, không bịa "hoàn thành" |

---

## 4. Phụ thuộc & bàn giao tuần 2

### TV4 cần nhận:
| Từ | Nhận gì | Khi nào |
|---|---|---|
| TV3 (C1/C2) | Recipe entity + ingredient/step bàn giao để publish có fixture | Giữa tuần 2 |
| TV3 (C5) | Refresh token hash/rotation để logout revoke family | Cuối tuần 2 |
| TV1/TV3 | RowVersion/If-Match theo D19 cho race set primary (nếu có) | Giữa tuần 2 |

### TV4 phải bàn giao sớm:
| Bàn giao cho | Gì | Khi nào |
|---|---|---|
| TV3 | `MinioStorageService` hoạt động + `IMAGE_CONTRACT.md` (upload/primary/delete/resize URLs) | Giữa tuần 2 |
| Cả nhóm | Quyết định D27 bucket private/public chốt ADR | Đầu tuần 2 |
| TV1/TV3 | Logout + revoke refresh (sau khi C5 có) | Cuối tuần 2 |

### Thứ tự ưu tiên bàn giao:
1. D27 chốt ADR bucket policy (ảnh hưởng mọi endpoint file).
2. MinioStorageService + upload/primary/delete (D1) → TV3 editor.
3. Resize (D2) → URLs trả trong `StoredFile`.
4. Publish/unpublish (D3) → end-to-end cuối tuần.

---

## 5. Ma trận skill tuần 2 TV4

| K | Loại | Sẽ chứng minh ở | Evidence key |
|---|---|---|---|
| K01 | SP | ADR D17/D21/D22/D23/D27 + mapping FR | TV4-K01 |
| K02 | SP | image/status endpoint group + RFC7807/422 | TV4-K02 |
| K03 | SP | `MinioStorageService` qua interface + status domain methods | TV4-K03 |
| K04 | SP+LAB | publish/unpublish CQRS handlers; LAB behavior | TV4-K04 |
| K05 | SP | validators upload MIME/size + status form nền | TV4-K05 |
| K06 | SP | RecipeImage config/index (đã có từ TV3) + migration nếu cần | TV4-K06 |
| K07 | SP+LAB | primary transaction + RowVersion race test | TV4-K07 |
| K10 | SP | ownership publish/image; LAB VerifiedAuthor | TV4-K10 |
| K12 | SP+LAB | invalidation khi unpublish/xoá ảnh | TV4-K12 |
| K13 | SP+LAB | upload/delete đầy đủ + boundary tests | TV4-K13 |
| K14 | SP nền + LAB | resize job + restart/retry (lab) | TV4-K14 |
| K15 | LAB | Mailhog + resize (lab) | TV4-K15 |
| K20 | SP | health/OTEL nền + log redacted khi MinIO lỗi | TV4-K20 |
| K23 | SP | Compose/Nginx/volumes + CI pass mỗi task | TV4-K23 |
| K24 | SP | PR + review Tâm + ADR/runbook/CI | TV4-K24 |

---

## 6. Checklist cổng tuần 2

- [ ] **G2 giữa tuần**: upload ảnh hợp lệ → `StoredFile` URLs; set primary đúng 1; DELETE xoá object + không orphan.
- [ ] **G3 cuối tuần**: Draft tạo → thêm ingredient/step (TV3) + ảnh (TV4) → `publish` thành công; `unpublish` ẩn public ngay; Draft/Archived không lộ.
- [ ] 4 MIME hợp lệ xác thực magic bytes; ≤5MiB; path `recipes/{recipeId}/{uuid}.{ext}`.
- [ ] Resize original/300×300/800×600 + URLs DB + original fallback.
- [ ] Publish thiếu ingredient/step → 422 `RECIPE_PUBLISH_INCOMPLETE`; đủ → Published.
- [ ] Logout revoke refresh family (khi có C5); 204 idempotent.
- [ ] ADR TV4-001 cập nhật: D17, D21, D23 (nếu kết luận mới); D27 gửi xác nhận nhóm.
- [ ] Lab `practice/TV4/L4` commit + sổ skill cập nhật; CI pass sau mỗi task; không commit secret.

---

## 7. Trở ngại dự kiến & cách xử lý

| Rủi ro | Ảnh hưởng | Giải pháp |
|---|---|---|
| TV3 chưa bàn giao C5 refresh đúng hạn | Logout chưa revoke family | Tuần 1 đã có seam; tuần 2 vẫn giữ Bearer 204, gắn C5 khi có |
| TV3 chưa bàn giao Recipe/ingredient fixture | Publish chưa kiểm thử được trạng thái đủ | Dùng entity có sẵn + self-seed fixture tạm thời; chờ merge chính thức |
| MinIO SDK phiên bản không tương thích | Build hỏng / restore locked-mode fail | Khóa version trong lockfile + kiểm tra thực tế trước merge (D25) |
| Delete recipe soft vs xoá object ảnh | Orphan object trong bucket | Quyết định rõ trong ADR D27; xoá object khi delete image |
| Private bucket ảnh hưởng frontend | `<img src>` không xem được | Presigned URL hoặc proxy có auth; nghiệm thu nhóm + giảng viên (D27) |

---

## 8. Việc làm ngay khi được confirm

1. Chốt ADR D27 bucket policy (private/public) với nhóm.
2. Implement `MinioStorageService` + DI + test integration MinIO down.
3. API upload/primary/delete + validator MIME/size/magic bytes + `IMAGE_CONTRACT.md`.
4. Resize job 300×300/800×600 + config kích thước.
5. Publish/unpublish CQRS + 422 khi thiếu ingredient/step + ownership test.
6. UI uploader/status nền + sitemap/robots nền.
7. Nhánh lab `practice/TV4/L4` + sổ evidence K.