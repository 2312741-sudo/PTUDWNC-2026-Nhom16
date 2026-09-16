# KẾ HOẠCH TUẦN 1 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

- **Phần nghiệp vụ**: Xuất bản, hình ảnh, SEO và vận hành — tasks D1–D7 (theo `KE_HOACH_DU_AN.md` mục 8 và `PHAN_CHIA_CONG_VIEC_6_TUAN.md`).
- **Mã task tuần 1**: D1 (nền), D3 (logout), D5, D6 (nền).
- **Nhánh Git đề xuất**: `feat/TV4-week1-deploy-storage`; lab: `practice/TV4/L4`.
- **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng).
- **Cổng**: G0 giữa tuần (stack chạy) → G1 cuối tuần (đăng ký → đăng nhập → category → Draft chạy xuyên suốt).

> Tuần 1 tính từ ngày nhóm bắt đầu; không tự gán lịch ngày tháng cụ thể (theo `PHAN_CHIA_CONG_VIEC_6_TUAN.md`).
> Tài liệu này là **kế hoạch thực thi + khung minh chứng**, mọi việc ban đầu ở trạng thái **Chưa làm**.
> Xem thêm [MO_TA_CONG_VIEC_TV4.md](MO_TA_CONG_VIEC_TV4.md) để nắm tổng quan công việc/flow/hạn chế/chuyển giao.

## 1. Mục tiêu tuần 1 (trích best practice từ 2 hồ sơ)

| Tiêu chí | Bàn giao kỳ vọng |
|---|---|
| Nghiệp vụ | Compose/PostgreSQL/Redis/MinIO/Nginx/health skeleton; storage contracts; logout; pipeline triển khai nền |
| Nghiệm thu | FE/API/DB chạy được giữa tuần (G0); storage interface cho editor; logout revoke đúng token; không commit secrets (G1) |
| Skill | Mở sổ K01–K24, bắt đầu lab nền sau G1 (D6) |

## 2. Hiện trạng repo tại thời điểm lập kế hoạch

| Mảng | Trạng thái | Ảnh hưởng đến TV4 |
|---|---|---|
| `docker-compose.dev.yml` | Chỉ có `postgres:16` + volume `pgdata` | Cần thêm Redis 7, MinIO, Mailhog, Seq, Nginx (dev) |
| TV1 — A1/A2/A5 nền | Auth contract, `register`/`login`/`me`, JWT 15', Problem Details/RFC7807, Serilog/CorrelationId, CI backend | Có sẵn để nối logout; chưa có `refreshToken` |
| `docs/AUTH_CONTRACT.md` (dòng 13) | Ghi rõ: *"chưa có refreshToken … TV4 chưa thể nghiệm thu revoke/logout với phiên bản access-token-only"* | Logout tuần 1 làm contract + endpoint + seam; revoke family chờ TV3 C5 (tuần 2) |
| TV3 — C1 nền | Recipe aggregate/Nutrition/RowVersion + concurrency spike; FK/migration thật chưa bật (chờ B1/A1) | D1 upload/metadata cần Recipe có ID (tuần 1 chỉ dựng contract, chưa cần DB Recipe) |
| TV2 — B1 | Category nền (tuần 1) | Chưa chặn D5/D1 nền; D3 chỉ cần A1 |

## 3. Phân rã công việc tuần 1 (theo G4.2 + mục 3.4 PHAN_CHIA)

### N1 — D5: Compose đầy đủ + Nginx + health skeleton (12đ, nền)
- **Skills**: K20, K22 (nền), K23, K24 · **ADR**: D22, D25
- **Việc làm**:
  1. Mở rộng `docker-compose.dev.yml`: `redis:7` (APIF, volume), MinIO (creds từ env, volume `miniodata`), Mailhog, Seq (dev), Nginx (cấu hình nền dùng cho FE/API). Giữ Postgres như cũ.
  2. Health endpoints ở root (FR-OBS-001 + ch.8.7): `GET /health` (DB + Redis + MinIO), `GET /health/live` (chỉ process), `GET /health/ready` (chỉ DB + Redis — **MinIO không nằm trong ready**) — dựa trên D22 (ready 503 khi Redis down; live chỉ process).
  3. `.env.example` đầy đủ placeholder (Không secret). Kiểm tra secret scan.
  4. CI workflow nền cho build/docker compose config.
- **Nghiệm thu D5**: probe đúng; trace HTTP→DB (khi có OTEL tuần 3); volumes `pgdata`/`miniodata`; restore skeleton tuần 5.
- **DoD**: `docker compose -f docker-compose.dev.yml up -d` chạy được cả stack; API/DB/Redis/MinIO health trả 200; `readiness` trả 503 khi Redis down (theo D22); `.env.example` không có giá trị thật.

### N2 — D1 (nền): Storage abstraction + MinIO integration (15đ — phần nền, tuần 2 làm đủ)
- **Skills**: K02, K03, K13 (nền) · **ADR**: D27, D25
- **Việc làm**:
  1. `IFileStorageService` + DTO signature (`UploadAsync(stream, contentType, size, prefix)`, `DeleteAsync(key)`, metadata) ở Application (FR-FILE-001/002). Path ảnh theo FR-RCP-008: `recipes/{recipeId}/{uuid}.{ext}`, ảnh đầu tiên tải lên tự động là ảnh chính (IsPrimary = true).
  2. `MinioStorageService` trong Infrastructure + `MinioOptions` (endpoint/accessKey/secretKey/bucket/useSSL) đọc từ config; đăng ký DI.
  3. Khởi tạo bucket `culinary-blog` khi khởi động/starter script; **bucket policy theo quyết định D27** (mặc định private). Lưu ý: SRS 2.4.1 + FR-FILE-001 ghi `public-read` → nếu nhóm muốn giữ private phải có CR, không tự ý đổi.
  4. Cung cấp DTO cho TV3 tích hợp editor image (D4/C4) — cần có từ giữa tuần 2.
- **Nghiệm thu D1**: 4 định dạng, ≤5MiB, magic bytes, GUID path — **tuần 2**. Tuần 1 chỉ cần contract + integration sẵn sàng với App.
- **DoD tuần 1**: interface độc lập (Domain/Application không biết MinIO); config mẫu không secret; MinIO chạy được trong Compose; test integration MinIO down → thất bại rõ ràng + log đỏacted; DTO cho TV3 review.

### N3 — D3 logout (phần contract + endpoint nền, revoke family tuần 2) (13đ)
- **Skills**: K02, K08 (SP logout), K10 · **ADR**: D06, D05 (sở hữu TV3)
- **Việc làm**:
  1. `POST /api/v1/auth/logout` — yêu cầu Bearer (D06), trả 204 idempotent; revoke refresh token hiện tại khi có. SRS FR-AUTH-005 yêu cầu body `{ refreshToken }` **khi có refresh token**; tuần 1 chưa có refresh (TV3 C5 tuần 2) → làm Bearer-only + seam, gắn body `refreshToken` khi C5 về.
  2. Cập nhật `docs/AUTH_CONTRACT.md`: ghi rõ logout yêu cầu Bearer, 204, và trạng thái "refresh chưa có → chỉ revoke access scope".
  3. Test: 401 thiếu/sai token; 204 hợp lệ; không tạo side-effect với token khác user.
- **Nghiệm thu D3**: logout 204; quyền sở hữu token; refresh reuse xử lý theo D05 (tuần 2).
- **DoD tuần 1**: endpoint + contract + happy/error test; Scalar cập nhật OpenAPI; không log token (AUTH_CONTRACT).

### N4 — D6 (nền): mở sổ skill + bắt đầu lab L4/L5 nền (25đ — dần từ tuần 1)
- **Skills**: K04, K13, K14, K15, K20, K23 (nền) · **ADR**: D25
- **Việc làm**:
  1. Tạo nhánh `practice/TV4/L4` từ skeleton G1 (sau khi G1 đóng): upload/delete 4 MIME + resize 300×300/800×600.
  2. Mở sổ evidence `TV4-Kxx` cho từng ô K01–K24, ghi trạng thái **Chưa làm / Đang làm nền**; các LAB cần recipe/media đầy đủ nối tiếp sau G2.
- **DoD**: nhánh lab tồn tại + commit nền; sổ K có trạng thái cập nhật; chưa bịa "hoàn thành".

## 4. Phụ thuộc & bàn giao tuần 1

**TV4 cần nhận:**
- Từ **TV1 (A1)**: auth contract/`ICurrentUser`, cách gắn Authorization header, account fixtures → **đã có** (`docs/AUTH_CONTRACT.md`).
- Từ **TV3 (C1)**: Recipe entity/DTO/status/RowVersion + fixture có nguyên liệu + bước để TV4 thử publish (D3) → bàn giao sớm tuần 1, dùng đủ tuần 2.
- Từ **TV2 (B1)**: Category list DTO để G1 chạy (nền).

**TV4 phải bàn giao sớm:**
- `IFileStorageService` + DTO → cho TV3 tích hợp editor image (D4/C4) và TV2 detail.
- Compose/health/.env.example → cho cả nhóm chạy stack 1 lệnh (G0).
- Logout endpoint + cập nhật AUTH_CONTRACT → cho TV1/TV3 khi làm C5 (refresh rotation).

**Thứ tự ưu tiên bàn giao:**
1. Compose + health (G0 giữa tuần) — cả nhóm cần để chạy.
2. Storage interface (giữa tuần 2) — TV3 cần để tích hợp upload.
3. Logout endpoint (cuối tuần 1) — TV1/TV3 cần khi làm refresh rotation.

## 5. Ma trận skill tuần 1 TV4 — nơi tạo evidence

| K | Loại | Sẽ chứng minh ở | Evidence key |
|---|---|---|---|
| K01 | SP | ADR D22/D27/D06 đề xuất + mapping | TV4-K01 |
| K02 | SP | health + storage + logout endpoint group + lỗi RFC7807 | TV4-K02 |
| K03 | SP | `IFileStorageService` + interface storage (FILE-001/002) | TV4-K03 |
| K04 | LAB (nền) | behavior tối thiểu trong lab L4 | TV4-K04 |
| K06 | SP | MinIO config/seed bucket + storage migration nền | TV4-K06 |
| K08 | SP | logout (SP) | TV4-K08 |
| K10 | SP | logout cần Bearer; policy nền | TV4-K10 |
| K13 | SP nền + LAB | MinIO upload/delete nền + L4 | TV4-K13 |
| K14 | LAB (nền) | welcome+resize+sitemap+delayed+restart (lab) | TV4-K14 |
| K15 | LAB (nền) | Mailhog + resize + XML (lab) | TV4-K15 |
| K20 | SP | health probes + Serilog/OTEL nền | TV4-K20 |
| K23 | SP | Compose/Nginx/volumes/health + CI nền | TV4-K23 |
| K24 | SP | PR + review Tâm + ADR/runbook/CI | TV4-K24 |

> Các ô còn lại (K05, K07, K09, K11, K12, K16, K17, K18, K19, K21, K22) sẽ có minh chứng từ D2–D7/description lab. Chưa ghi "Hoàn thành" khi chưa có code + test + review.

## 6. Checklist cổng tuần 1 TV4

- [ ] **G0 giữa tuần**: stack Compose lên được (Postgres + Redis + MinIO + API), `/health` 200, `.env.example` đầy đủ placeholder, Không secret.
- [ ] **G1 cuối tuần**: `IFileStorageService` merged (contract); logout 204/401 theo AUTH_CONTRACT cập nhật; CI backend/build pass; sổ skill mở, lab nền bắt đầu.
- [ ] Khóa phiên bản Redis7/MinIO/Nginx vào lockfile/README (D25, cùng cả nhóm).
- [ ] Chốt ADR **D22, D27, D06** tuần 1 (đầu tuần 2 gửi xác nhận như mục 2 KE_HOACH) — đặc biệt bucket policy và readiness.
- [ ] Cung cấp `IFileStorageService` DTO cho TV3 review (nếu có thể giữa tuần).

## 7. Trở ngại dự kiến & cách xử lý

| Rủi ro | Ảnh hưởng | Giải pháp |
|---|---|---|
| Chưa có `refreshToken` (TV3 C5 tuần 2) | Không thể revoke family ngay | Đúng kế hoạch; tuần 1 làm contract + seam, tuần 2 gắn C5 |
| D27 bucket private/public chưa chốt | Draft hình ảnh có thể bị lộ | Mặc định private + presigned; ghi ADR, không tự đặt public-read |
| Phiên bản Redis/MinIO SDK thay đổi (D25) | Build hỏng | Khóa version + chạy thực tế trước khi merge |
| Máy chưa cài .NET SDK/nuget | Không build/test được | Tài liệu ghi rõ lệnh; chạy trên máy có SDK, review bằng log/evidence |
| TV3 chưa bàn giao Recipe entity đúng hạn | D3 publish thiếu fixture | Tuần 1 chỉ làm contract logout; D3 publish đầy đủ tuần 2 khi có C2/C3 |
| Mâu thuẫn chuẩn publish (D07) | FR-RCP-005 (ch.3) chỉ cần ≥1 bước; Phụ lục B `RECIPE_PUBLISH_INCOMPLETE` 400 đòi ≥1 nguyên liệu + ≥1 bước | Làm D3 theo phụ lục B (nghiêm hơn), chốt D07 khi làm publish đầy đủ tuần 2 |

## 8. Việc làm ngay khi được confirm

1. Mở rộng `docker-compose.dev.yml` (Redis/MinIO/Mailhog/Seq/Nginx) → chạy G0.
2. Đăng ký `MinioStorageService` DI + `IFileStorageService` (contract).
3. Viết health endpoints + test (D22).
4. Thêm `POST /auth/logout` + cập nhật AUTH_CONTRACT + test.
5. Tạo `.github/workflows` nền (build/docker compose config).
6. Tạo nhánh lab `practice/TV4/L4`, mở sổ evidence TV4-Kxx.
7. Push PR nhỏ từng task để TV1 review (nhánh `feat/TV4-week1-deploy-storage`).