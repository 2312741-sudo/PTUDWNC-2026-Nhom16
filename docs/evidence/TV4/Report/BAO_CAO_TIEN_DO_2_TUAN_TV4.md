# BÁO CÁO TIẾN ĐỘ 2 TUẦN — TV4 · Nguyễn Hữu Trung Sơn (2312739)

> **Kỳ báo cáo**: Tuần 1 – Tuần 2  
> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026)  
> **Nhánh Git**: `2312739_NHTSon_D1-D3-D5-D6` → `2312739_NHTSon_D1-D2-D3-D4`  
> **Reviewer**: Nguyễn Thanh Tâm (Nhóm trưởng)  
> **Cập nhật**: 22/09/2026

---

## Phần 1 — Công việc được phân công

| Mã | Mô tả |
|:---|:---|
| D1 | Storage abstraction/MinIO config, upload/magic bytes/size, metadata/primary/delete API |
| D2 | Resize job original/300×300/800×600, cleanup retry và race handling |
| D3 | Publish/unpublish/archive/delete CQRS + ownership + cache/ISR invalidation; Logout |
| D4 | Image uploader/progress/gallery/primary UI, status action, Recipe JSON-LD/OG, sitemap/robots |
| D5 | Compose/Nginx, health/OTEL/metrics, persistent volumes, backup/restore/multi-instance |
| D6 | Lab cá nhân K01–K24, trọng tâm Identity/Google/refresh, forms, FTS, DB query |
| D7 | Integration/UI/publish E2E, resilience/load/SEO, runbook và release |

---

## Phần 2 — Hiện trạng thực hiện

| Mã | Hoàn thành | Chưa hoàn thành | Tiến độ % (tự đánh giá) |
|:---|:---|:---|:---:|
| **D1** | • `IFileStorageService` contract (UploadAsync/DeleteAsync) + `StoredFile` DTO<br>• `MinioOptions` + đăng ký DI<br>• `MinioStorageService` implement bằng MinIO SDK 7.0.0 + lockfile<br>• Validator upload: 4 MIME (JPEG/PNG/WebP/AVIF) + ≤5MiB + magic bytes<br>• Domain RecipeImage tests: auto primary, đúng 1 primary, remove-promote<br>• Race test 2 writer set primary → không tạo 2 primary<br>• `IMAGE_CONTRACT.md` bàn giao TV3<br>• Image endpoints: POST/PATCH/DELETE (88/88 test pass) | • Test MinIO down → lỗi rõ ràng + log redacted (chờ integration local)<br>• Test E2E trên MinIO thật (cần seed recipe từ TV3) | **80%** |
| **D2** | — | • Resize original/300×300/800×600<br>• Cleanup retry và race handling<br>• Chốt queue (Hangfire/BackgroundService) | **0%** |
| **D3** | • Logout endpoint: Bearer bắt buộc, 204 idempotent<br>• `AUTH_CONTRACT.md` cập nhật theo SRS v1.1.1<br>• Test logout: 401 thiếu token, 204 hợp lệ | • Publish/unpublish CQRS + 422 + ownership<br>• Archive/delete theo ADR (C01 Soft Delete)<br>• Cache/ISR invalidation sau status change<br>• Logout revoke refresh family (chờ TV3 C5) | **30%** |
| **D4** | — | • Uploader UI + progress<br>• Status action buttons<br>• Recipe JSON-LD/OG metadata<br>• Sitemap/robots<br>• Chốt D27 ảnh hiển thị (presigned/proxy) | **0%** |
| **D5** | • `docker-compose.dev.yml`: Postgres 16 + Redis 7 + MinIO + Mailhog + Seq + Nginx<br>• Health endpoints: `/health`, `/health/live`, `/health/ready`<br>• `.env.example` đầy đủ placeholder, không secret<br>• CI workflow backend: build + test + format (pass)<br>• Bucket `culinary-blog` private + `minio-init` | • OTEL tracing/metrics<br>• Persistent volumes backup/restore<br>• Multi-instance test<br>• Nginx production config | **55%** |
| **D6** | • Sổ skill K01–K24 đã mở<br>• K01 (ADR delete/media), K05 (media form), K11 (FTS), K12 (cache), K13 (MinIO), K20 (health/probes), K23 (Docker/Compose), K24 (Git/PR/CI) — 8/24 có minh chứng | • 16 kỹ năng còn thiếu (K02–K04, K06–K10, K14–K19, K21–K22) | **33%** (8/24) |
| **D7** | — | • Integration/UI/publish E2E<br>• Resilience/load/SEO tests<br>• Runbook và release docs | **0%** |

---

## Tổng kết kỳ Tuần 1 – Tuần 2

| Chỉ số | Giá trị |
|:---|:---:|
| Task hoàn thành hoàn toàn | 1/7 (D5 nền) |
| Task hoàn thành một phần | 3/7 (D1, D3, D6) |
| Task chưa bắt đầu | 3/7 (D2, D4, D7) |
| **Tiến độ tổng thể (tự đánh giá)** | **~35%** |

### Cổng hoàn thành

| Cổng | Trạng thái |
|:---|:---:|
| G0 giữa tuần 1 — Stack Compose chạy, `/health` 200 | ✅ Đạt |
| G1 cuối tuần 1 — Contract merged, logout 204/401, CI pass | ✅ Đạt |
| G2 giữa tuần 2 — Upload → StoredFile, primary đúng 1, DELETE không orphan | ✅ Đạt (code + tests) |
| G3 cuối tuần 2 — Publish đầy đủ, unpublish ẩn public, resize 3 kích thước | ❌ Chưa đạt |

### Blockers đang chờ gỡ

| Blocker | Phụ thuộc | Dự kiến |
|:---|:---|:---|
| Test E2E image upload trên MinIO | TV3 — recipe CRUD endpoints (seed recipe) | Tuần 3 |
| Publish/unpublish/archive/delete | TV3 — Recipe entity + ingredient/step fixture | Tuần 3 |
| Logout revoke refresh family | TV3 — C5 refresh token rotation | Tuần 3 |
| Bucket policy D27 (private vs public) | Quyết định nhóm + giảng viên | Cần chốt |
| Resize job queue (Hangfire vs BackgroundService) | Quyết định nhóm theo D23 | Cần chốt |
