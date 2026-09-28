# ADR-TV4-002 — Image MinIO bị gỡ khỏi registry: đổi dev/CI sang RustFS

Ngày: 28/09/2026 · Người thực hiện: Nguyễn Hữu Trung Sơn (2312739) — TV4.
Phạm vi: hạ tầng lưu trữ object **môi trường dev + CI**. **Không** thay đổi code sản phẩm, hợp đồng API, hay quyết định của production.
Trạng thái: **Đã áp dụng** cho dev/CI. Cần nhóm trưởng (TV1) xác nhận khi lên phương án production.
Liên quan: `ADR-TV4-001` §D27 (bucket private + proxy ảnh), `docs/IMAGE_CONTRACT.md` §5, `docs/DE_XUAT_GIAI_QUYET_D23_D27.md`.

---

## 1. Tóm tắt (TL;DR)

Toàn bộ repo (dev compose, GitHub Actions, hướng dẫn test) đang dùng image `quay.io/minio/minio:latest`.
**MinIO đã gỡ toàn bộ image public của họ khỏi registry.** Runner CI và máy dev **không còn pull được image** →
toàn bộ pipeline đỏ, mọi test bị skip, và dev stack không dựng được trên máy mới.

**Quyết định**: dùng **RustFS** (`rustfs/rustfs`, Apache-2.0, chuẩn S3) làm object storage cho **dev + CI**,
giữ nguyên code sản phẩm (`MinioStorageService` **không đổi một dòng nào**).

---

## 2. Hiện trạng

| Hạng mục | Trước khi đổi | Sau khi đổi |
|---|---|---|
| Image dev compose | `quay.io/minio/minio:latest` | `rustfs/rustfs:1.0.0` (ghim tag + digest) |
| Service compose | `minio` (container `culinaryblog-minio`) | `s3` (container `culinaryblog-s3`) |
| Bucket bootstrap | service `minio-init` dùng `mc alias set` + `mb` | **bỏ hẳn** — app tự `BucketExists` → `MakeBucket` khi khởi động |
| Volume | `miniodata` | `s3data` (volume cũ **giữ lại**, không xoá) |
| Cổng | `9000` API / `9001` console | **giữ nguyên** `9000` / `9001` |
| CI service | `minio`, health `minio/health/live` | `objectstorage`, health-cmd `curl /health` |
| Test env | `MINIO_ENDPOINT` / `MINIO_ACCESSKEY` / `MINIO_SECRETKEY` | **giữ nguyên tên biến** (xem §5) |
| Code sản phẩm | `MinioStorageService` (6 thao tác S3) | **không đổi dòng nào** |
| SRS v1.1.1 / evidence Tuan01 | ghi "MinIO" | **giữ nguyên** (là spec/record lịch sử) |

Các dịch vụ khác **không đổi**: `postgres:16-alpine`, `redis:7-alpine`, `mailhog/mailhog:v1.0.1`, `datalust/seq`, `nginx:alpine`.

---

## 3. Nguyên nhân gốc (Root cause)

**Không phải lỗi code.** Chuỗi sự kiện:

1. Repo cấu hình container storage bằng image MinIO chính thức:
   `image: quay.io/minio/minio:latest` trong `docker-compose.dev.yml` và trong service của `.github/workflows/backend.yml`.
2. MinIO (công ty thương mại) **ngừng phân phối image container công khai**:
   - `quay.io/minio/minio` → **HTTP 401 Unauthorized**
   - `minio/minio` trên Docker Hub → **HTTP 404** (repository không tồn tại)
3. Docker trên runner GitHub Actions và trên máy dev **không pull được image** → container không khởi động.
4. Hệ quả trên CI — đây là điểm nguy hiểm nhất:
   - Job chết ngay ở bước **"Initialize containers"**.
   - Mọi bước sau (`restore`, `build`, `dotnet format`, `test`) bị **`skipped`**.
   - **5 run đỏ liên tiếp** mà **không dòng log nào chứa lỗi code**. Test E2E storage vốn thiết kế để **skip an toàn** khi storage không reachable → CI có thể "xanh giả" nếu chỉ nhìn trạng thái tổng.

> **Bài học cốt lõi**: hạ tầng CI là một phụ thuộc bên ngoài có thể biến mất bất cứ lúc nào. Một dấu hiệu đỏ *không* mặc định là lỗi logic — phải đọc tới bước bị skip đầu tiên trước khi sửa code.

### Biện minh cho lựa chọn RustFS

| Tiêu chí | RustFS | `coollabsio/minio` (mirror do TV3 chọn ở `84dddd4`) | `quay.io/minio/minio` (MinIO chính thức) |
|---|---|---|---|
| License | **Apache-2.0**, mã nguồn mở | Theo MinIO (AGPL-3.0) — nhưng là **mirror bên thứ ba, không phải nguồn chính thức** | AGPL-3.0 |
| Tình trạng registry | Đang phát hành, tag công khai | Mirror cộng đồng, phụ thuộc người duy trì bên thứ ba | **Không còn** |
| Ghim theo digest | ✅ đã ghim `sha256:8cc9801…` | Chưa ghim | Không áp dụng |
| API | S3 chuẩn, tương thích MinIO SDK | S3 chuẩn | S3 chuẩn |
| Health endpoint | `/health` | `/minio/health/live` | `/minio/health/live` |
| Phụ thuộc vào bên thứ ba | Thấp (Apache, image chính thức) | **Cao** (mirror có thể biến mất giống MinIO) | Không còn |

---

## 4. Giải pháp đã áp dụng

### 4.1. Thay image ở mọi nơi
`docker-compose.dev.yml` + `.github/workflows/backend.yml` → `rustfs/rustfs:1.0.0` (ghim **tag + digest**
`sha256:8cc9801755448b71a786705ce76692c77e14936cccd87cf2fc31842e58f4d1ff` ở **cả hai** nơi).

Trong `docker-compose.dev.yml`, digest nằm sau tag nên có thể override bằng biến môi trường:

```yaml
image: rustfs/rustfs:${S3_TAG:-1.0.0}@${S3_DIGEST:-sha256:8cc9801...}
```

Đổi `S3_TAG` mà quên đổi `S3_DIGEST` sẽ không khởi động được container — cần đổi **cả hai** cùng lúc.

### 4.2. Bỏ `minio-init`
Ứng dụng đã tự tạo bucket khi khởi động (`BucketExists` → `MakeBucket`), nên container init thừa chỉ gây lỗi khi image đổi.
Xác minh: bucket `culinary-blog` và prefix `recipes/` **thực sự tồn tại** sau khi chạy app.

### 4.3. Đổi health check
RustFS **không** phục vụ `/minio/health/live` → dùng `--health-cmd "curl /health"`. Sai endpoint này sẽ khiến service bị coi là unhealthy vĩnh viễn.

### 4.4. Gia cố pipeline (để lỗi tương tự lộ ra sớm, rõ hơn)
- Ghim image theo **tag + digest** → tag bị đổi/xoá vẫn không làm hỏng build.
- Ghim SDK `10.0.401` khớp `global.json` → không đổi kết quả `dotnet format`/warning theo máy.
- `timeout-minutes: 30` → job treo bị cắt thay vì treo tới hết giờ runner.
- `concurrency` + `cancel-in-progress` → push liên tiếp không tốn runner.
- `--blame-hang-timeout 10m` → test treo bị bắt dump stack thay vì chờ mù.
- **Quy tắc vận hành mới**: CI xanh phải kèm `Skipped=0`, nếu không thì coi như **chưa kiểm thử gì**.

### 4.5. Đồng bộ tài liệu
`.env.example`, `README.md` (bảng dịch vụ), `docs/HUONG_DAN_TEST_APP.md`, comment trong `tests/CulinaryBlog.Tests/MinioE2ETests.cs`.

---

## 5. Điều KHÔNG đổi (có chủ đích)

| Không đổi | Lý do |
|---|---|
| Tên biến môi trường `Minio__*` / `MINIO_*` | `MinioStorageService` và test đọc biến này; đổi tên sẽ kéo theo sửa code sản phẩm — **không đáng rủi ro vì một sự cố hạ tầng**. RustFS tương thích S3 nên giá trị truyền vào không đổi. |
| Tên class `MinioStorageService`, `MinIOHealthCheck` | Cùng lý do; tên class là chi tiết hiện thực, đổi sẽ lan sang `Program.cs`, DI, test, contract. |
| Cổng `9000` / `9001` | Không phá script của thành viên khác. |
| `SRS v1.1.1` và evidence Tuan01 | Là **spec và record lịch sử** đã được giảng viên chấp nhận. Sửa lại sẽ là sai lệch hồi tố. Thay vào đó, ADR này giải thích sự khác biệt ở tầng triển khai. |
| Volume `miniodata` cũ | Giữ lại an toàn; dữ liệu dev cũ không quan trọng nhưng **không nên xoá âm thầm**. |

---

## 6. Rủi ro

| # | Rủi ro | Mức | Xử lý / Giảm thiểu |
|---|---|---|---|
| R1 | **Image RustFS bi gỡ hoặc tag bị đổi** → CI đỏ lặp lại | Trung bình | Đã ghim **digest** `sha256:8cc9801…`; digest không đổi kể cả khi tag đổi. Nếu digest biến mất thì phải chọn backend khác và **cập nhật ADR này**. |
| R2 | **RustFS không tương thích 100% với MinIO** ở tính năng chưa dùng (presigned URL, bucket policy, `mc`) | Trung bình | Hiện chỉ dùng 6 thao tác cơ bản (`MakeBucket/Put/Get/Stat/Remove/BucketExists`) — đã kiểm chứng chạy thật. Ảnh public **không** đi qua presigned mà qua **proxy có auth** (PA-2, `IMAGE_CONTRACT.md §5`) nên không phụ thuộc tính năng MinIO riêng. **Nếu sau này dùng presigned phải test lại trên RustFS.** |
| R3 | **Nhầm RustFS với MinIO khi đọc tài liệu / khi deploy thật** | Cao | Đã ghi rõ ở README, handoff và ADR này. **Production KHÔNG được dùng RustFS** — phải dùng object storage có license rõ ràng (xem R5). |
| R4 | **Hai image khác nhau trên các nhánh** (TV4: RustFS · TV3: `coollabsio/minio`) | Cao | ✅ **Đã gỡ trong PR #16**: RustFS là lựa chọn thống nhất cho dev + CI, `docker-compose.dev.yml` của main (từng dùng `coollabsio/minio` + `minio-init`) đã chuyển sang RustFS khi merge. |
| R5 | **SRS v1.1.1 vẫn ghi MinIO** → giảng viên/người đọc có thể hiểu sai là dự án còn chạy MinIO | Trung bình | ADR này + mục "Quyết định kiến trúc" ở README giải thích rõ: **SRS mô tả ý định (S3-compatible, bucket riêng, ảnh private)**; RustFS chỉ là *hiện thực thay thế* ở tầng triển khai. Nếu cần, xin giảng viên xác nhận bằng văn bản. |
| R6 | **E2E storage "skip âm thầm"** khiến CI xanh mà không kiểm thử gì | Cao (đã xảy ra) | Đã thêm quy tắc `Skipped=0`; bằng chứng gỡ: `148/148 + 5/5` với `Skipped=0` trên **cả** endpoint CI và service thật từ compose. |
| R7 | Console RustFS khác giao diện MinIO (ảnh không đổi) | Thấp | Chỉ ảnh hưởng thao tác tay; API/SDK giống hệt. Đã cập nhật `docs/HUONG_DAN_TEST_APP.md`. |
| R8 | Đổi image làm **mất dữ liệu dev cũ** trong volume `miniodata` | Thấp | Volume cũ **giữ nguyên**; bucket dev tạo lại tự động. Dữ liệu dev có thể tạo lại bằng `--seed`. |

---

## 7. Kế hoạch hành động

| # | Việc | Ai | Khi nào |
|---|---|---|---|
| 1 | Duyệt ADR này (RustFS cho dev/CI — CR-6) | TV1 (nhóm trưởng) | ✅ **Nhóm đã chốt RustFS** khi gỡ conflict PR #16; cần TV1 xác nhận chính thức |
| 2 | Đồng bộ compose/CI trên các nhánh khác về RustFS | TV3, TV1 | Khi merge |
| 3 | Chọn object storage **có license** cho staging/production | Cả nhóm | Tuần 5 (mục 5.5 của đề) |
| 4 | Nếu quyết định dùng presigned URL → test lại trên backend mới trước | TV4 | Khi có yêu cầu |
| 5 | Giữ quy tắc `Skipped=0` khi đọc kết quả CI | Cả nhóm | Mãi |

---

## 8. Tài liệu liên quan

- [`ADR-TV4-001` — Vận hành, storage và logout tuần 1](ADR-TV4-001-van-hanh-storage-logout-tuan-1.md) (D27)
- [`IMAGE_CONTRACT.md` §5](../IMAGE_CONTRACT.md) — proxy ảnh có auth (PA-2), §7 — ảnh phái sinh
- [`DE_XUAT_GIAI_QUYET_D23_D27.md`](../DE_XUAT_GIAI_QUYET_D23_D27.md) — Hangfire (PA-1) + proxy (PA-2)
- [`HUONG_DAN_TEST_APP.md`](../HUONG_DAN_TEST_APP.md) — chạy test với storage thật, kiểm tra `Skipped=0`
- [`SO_EVIDENCE_TUAN_3.md` §TV4-K23](../evidence/TV4/Tuan03/SO_EVIDENCE_TUAN_3.md) — bằng chứng gỡ sự cố
- [`HANDOFF_TV4_TUAN3.md`](../evidence/TV4/Tuan03/HANDOFF_TV4_TUAN3.md) — quy tắc cho người tiếp nhận
