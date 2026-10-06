# QUYẾT ĐỊNH THỰC HIỆN GIAI ĐOẠN 1 — TUẦN 4 (N2 → N3 → N4)

| Mục | Nội dung |
|---|---|
| **Người quyết** | Nguyễn Hữu Trung Sơn (2312739 — **TV4**) |
| **Ngày quyết** | 03/10/2026 |
| **Phạm vi** | Bổ sung vào [`PLAN_GIAI_DOAN_1_N2_N4.md`](../plan/PLAN_GIAI_DOAN_1_N2_N4.md) |
| **Nhánh** | `2312739_NHTSon_D5-D6-D7` — ⛔ không push `main`, không commit khi chưa được yêu cầu |
| **Reviewer** | Nguyễn Thanh Tâm (Nhóm trưởng) |
| **Trạng thái** | ✅ **Quyết định có hiệu lực** — kế hoạch GĐ1 cập nhật theo file này |

---

## 0. Nguyên tắc điều hành

### 0.1. Quyết định thuộc thẩm quyền TV4

Ba quyết định dưới đây **do TV4 chốt** và có hiệu lực ngay:

| # | Quyết định |
|---|---|
| **QD1** | **B5 → phương án A** (presigned URL ngắn hạn cho ảnh Draft) |
| **QD2** | **B6 → phương án A** (CLI `--promote-admin`, không seed mật khẩu cứng) |
| **QD3** | 3 task lock còn lại **ngoài phạm vi dự án** → mỗi task 1 đề xuất, **không** tự quyết |

### 0.2. ⭐ Nguyên tắc "ngoài contact" — TV4 tự làm phần phát sinh

> **Quy tắc:** nếu một quyết định của TV4 **sinh ra việc cho thành viên khác**, thì **TV4 tự làm
> phần đó**. Không đẩy việc sang, không dừng chờ phản hồi.

| Hành động | TV4 làm |
|---|---|
| Sửa file của thành viên khác (`ImagesStep.tsx` của TV3, seed của TV1) | ✅ **TV4 tự sửa** |
| **Báo trước** thành viên đó (PR/ghi chú) | ✅ Bắt buộc — để họ biết, **không** để họ phải hỏi |
| **Chờ trả lời** mới bắt đầu | ⛔ **Không.** Ghi mốc thời gian trong PR, tiếp tục làm |
| Có xung đột code sau khi đã sửa | TV4 **giữ bản sửa** (đã theo contract đã chốt), ghi rõ vào handoff để thành viên đó đối chiếu |

**Lý do:** quyết định là của TV4 thì TV4 chịu trách nhiệm cho kết quả. Gửi "xin phép" rồi chờ là
đẩy rủi ro trễ lịch sang người khác.

### 0.3. Ranh giới: cái nào TV4 **không** tự quyết

| Loại | Ví dụ | Lý do |
|---|---|---|
| **Ngoài hạ tầng dự án** | nơi đặt lịch backup, kho lưu 30 ngày, rotate khoá đang chạy | Không kiểm chứng được bằng test trong repo; có chi phí thật |
| **Kiến trúc đã chốt** | đổi D27/PA-2 sang presigned ở mức **contract** | Đã có D27 chốt 27/09 ⇒ phần **mở rộng** cần ghi vào contract (TV4 sửa), phần **đổi hướng** mới cần nhóm |
| **Của thành viên khác, đã chốt contract** | `Recipe` schema, seed data TV1 | Phần phát sinh cụ thể thì TV4 làm; đổi contract gốc thì hỏi |

---

## 1. QD1 — B5 chọn **phương án A**: presigned URL cho ảnh Draft

**Nguồn**: [`DE_XUAT_05_XEM_ANH_DRAFT_TRONG_WIZARD.md`](../../../../proposal/DE_XUAT_05_XEM_ANH_DRAFT_TRONG_WIZARD.md) §3 phương án A
**Vấn đề**: `<img src>` không gửi header `Authorization` ⇒ ảnh recipe **Draft** luôn `403` trong wizard.

### 1.1. Thiết kế được chốt

| Mục | Quyết định |
|---|---|
| Cơ chế | API cấp **URL đã ký** (`GetPresignedObjectUrlAsync`) khi người gọi có quyền |
| Hạn | ⛔ **≤ 10 phút** (chọn 10, không chọn 30) |
| Nơi trả | Thêm trường **nullable** vào `RecipeImageDto` — chỉ điền khi recipe **Draft/Archived** và người gọi là **Owner/Admin** |
| Recipe **Published** | ⛔ **không** cấp presigned — ảnh đã public qua proxy D27 |
| Tương thích ngược | ✅ Trường mới là `null` ⇒ FE cũ vẫn chạy, fallback `imageSrc()` |
| Rò rỉ | `Referrer-Policy: no-referrer` + ⛔ **không** ghi query string của URL ký vào log |
| Lỗi | Không ký được ⇒ trả `null` ⇒ FE fallback proxy ⇒ **không** trả 5xx cho luồng upload |

> ⛔ **Ràng buộc quan trọng:** trường presigned **không được** xuất hiện trong response public
> `GET /recipes/{slug}` cho recipe Published. Đây là điều kiện để không phá D27.

### 1.2. Task phân bổ — TV4 làm **toàn bộ**

| # | Task | File | Chủ sở hữu thật | Người thực hiện |
|---|---|---|---|---|
| B5-1 | Thêm `GetPresignedObjectUrlAsync` vào `IObjectStorageReader` + cài trong `MinioStorageService` | `src/backend/CulinaryBlog.Infrastructure/MinioStorageService.cs` (L17–20, L53) | **TV4** | **TV4** |
| B5-2 | Thêm `Minio:PresignedUrlExpiryMinutes` (mặc định 10, chặn > 15) | `src/backend/CulinaryBlog.Infrastructure/MinioOptions.cs` | **TV4** | **TV4** |
| B5-3 | Thêm trường nullable + điền có điều kiện (Draft + Owner/Admin) | `src/backend/CulinaryBlog.Application/RecipeImages.cs` (`RecipeImageDto` L12–17, mapping L27–29, `RecipeImageAccess` L190) | **TV4** | **TV4** |
| B5-4 | Test: Draft+owner có presigned; Draft+khách **null**; Published **null**; URL hết hạn ⇒ lỗi; không ghi log | `tests/CulinaryBlog.Tests/` | **TV4** | **TV4** |
| **B5-5** | ⭐ **Sửa `IMAGE_CONTRACT.md`** — §3 (DTO) + §5 (PA-3 presigned): hạn ≤ 10 phút, bảng quyền, `Referrer-Policy`, cấm log query | `docs/IMAGE_CONTRACT.md` (L60–70, L112–128) | **TV4** | **TV4** |
| **B5-6** | ⭐ **Sửa `recipe-editor.ts`** — thêm trường vào type `RecipeImage` (L158), `imageSrc` ưu tiên presigned (L175) | `src/frontend/src/lib/recipe-editor.ts` | **TV3** (nhưng là lib dùng chung) | **TV4 tự sửa** |
| **B5-7** | ⭐ **Sửa `ImagesStep.tsx`** — dùng presigned khi có, `referrerPolicy="no-referrer"`, xử lý URL hết hạn (báo tải lại) | `src/frontend/src/app/dashboard/recipes/_wizard/ImagesStep.tsx` (L49–57, L54) | **TV3** | **TV4 tự sửa** — không chờ TV3 |

### 1.3. Thông báo cho TV3 (bắt buộc, không chờ)

Ghi trong PR hoặc handoff, **trước khi merge**:

> TV4 sửa `ImagesStep.tsx` + `recipe-editor.ts` theo quyết định B5-PA-A của TV4 (ảnh Draft trong wizard
> bị 403 vì `<img>` không gửi header Bearer). Thay đổi **chỉ** ở chỗ đọc URL ảnh, **không** đổi luồng
> upload/delete/set-primary. Không có thay đổi hành vi nào khác. Nếu TV3 đang sửa cùng file, báo lại để
> TV4 điều chỉnh — nếu không phản hồi thì bản của TV4 vẫn giữ.

### 1.4. Nghiệm thu

| # | Test case | Kỳ vọng |
|---|---|---|
| 1 | Wizard, recipe Draft có ảnh | ⬜ **Ảnh hiện** (trước đây vỡ) |
| 2 | `GET /recipes/{id}` bằng `curl` **không** token, recipe Draft | ⛔ Không có presigned trong response; ảnh vẫn `403` khi gọi proxy |
| 3 | Recipe **Published**, bất kỳ ai | `200` qua proxy; ⛔ **không** có trường presigned |
| 4 | URL presigned quá 10 phút | `403`/`404` từ storage; UI báo **tải lại**, không ảnh vỡ vĩnh viễn |
| 5 | Tải lại trang nhiều lần | ⛔ Không sinh object rác trong bucket (presigned không ghi file) |
| 6 | Recipe Draft → Published | Ảnh hiện công khai; FE **không** còn dùng presigned |
| 7 | Xoá ảnh trong wizard | URL cũ không còn tải được (`404`) |
| 8 | Log API/Seq | ⛔ **Không** có chuỗi presigned (`X-Amz-Signature`) |
| 9 | `dotnet test` + `tsc --noEmit` + `next build` | Xanh, `Skipped=0` |

---

## 2. QD2 — B6 chọn **phương án A**: CLI `--promote-admin`

**Nguồn**: [`DE_XUAT_06_TAI_TAO_USER_ADMIN_DE_SEED.md`](../../../../proposal/DE_XUAT_06_TAI_TAO_USER_ADMIN_DE_SEED.md) §3 phương án A

### 2.1. Thiết kế được chốt

```powershell
dotnet run --project src/backend/CulinaryBlog.API -- --promote-admin <email>
```

| Mục | Quyết định |
|---|---|
| Hình thức | Lệnh CLI, nhất quán `--migrate` / `--seed` đã có (`Program.cs` L265–276) |
| Mật khẩu | ⛔ **Không** có trong repo; nâng quyền cho **tài khoản đã tồn tại** (email người dùng tự nhập) |
| Môi trường | ✅ Chỉ chạy khi `IsDevelopment()`; ⛔ **từ chối** ở `Testing`/`Production` |
| Seed | ⛔ **Không** thêm user Admin vào `DbSeeder` (tránh mật khẩu cứng trong dữ liệu mẫu) |
| Email không tồn tại | Báo lỗi rõ ràng, **không** crash |
| Idempotent | Chạy lại nhiều lần không nhân bản role |

### 2.2. Task

| # | Task | File | Chủ sở hữu thật | Người thực hiện |
|---|---|---|---|---|
| B6-1 | Thêm nhánh `--promote-admin <email>` cạnh `--migrate`/`--seed` + kiểm tra môi trường | `src/backend/CulinaryBlog.API/Program.cs` (L265–276) | **TV4** | **TV4** |
| B6-2 | Logic gán role `Admin` qua `UserRole` join, chống nhân bản | `Program.cs` hoặc service cạnh `DbSeeder` | **TV4** | **TV4** |
| B6-3 | Test: nâng quyền đúng; email sai ⇒ lỗi rõ; môi trường `Testing` ⇒ từ chối; chạy lại 2 lần không nhân bản | `tests/CulinaryBlog.Tests/` | **TV4** | **TV4** |
| B6-4 | Hướng dẫn dùng trong tài liệu TV4 | `docs/evidence/TV4/HUONG_DAN_CHAY_TV4.md` | **TV4** | **TV4** |

### 2.3. Ảnh hưởng tới GĐ1 — gỡ block N2-E7

| Trước | Sau |
|---|---|
| ⛔ N2-E7 (kiểm tra `/hangfire` chỉ Admin) **không làm được** — không có Admin | ✅ Làm được sau B6-1 |
| ⛔ Không ai tạo được danh mục mới bằng UI | ✅ `POST /categories` ⇒ `201` khi đã nâng quyền |

> 📌 N2-E7 **chuyển từ "chờ B6" sang "làm được"**. Cập nhật đã ghi trong
> [`PLAN_GIAI_DOAN_1_N2_N4.md`](../plan/PLAN_GIAI_DOAN_1_N2_N4.md) khối N2-E.

### 2.4. Cảnh báo bắt buộc

Lệnh cho phép ai có tài khoản local tự nâng quyền ⇒:

1. ⛔ Chỉ `Development` — đã chốt ở B6-1.
2. Hướng dẫn phải nói rõ: **dùng tài khoản thường để test nghiệp vụ**, chỉ dùng Admin cho
   `/hangfire` và `POST /categories` — tránh thói quen test bằng admin.
3. Ghi cảnh báo khi lệnh thành công.

---

## 3. QD3 — 3 task lock còn lại: mỗi task 1 đề xuất, **không tự quyết**

Cả 3 đều liên quan tới **hạ tầng ngoài phạm vi dự án** ⇒ không thể kiểm chứng bằng test trong repo.

| # | Task lock | Đề xuất | TV4 tự làm? |
|---|---|---|---|
| 1 | **Nơi đặt lịch backup 03:00** — GitHub Actions có thể bỏ qua job khi repo lâu không commit | [`DE_XUAT_07_NOI_DAT_LICH_BACKUP.md`](../../../../proposal/DE_XUAT_07_NOI_DAT_LICH_BACKUP.md) | ❌ Chờ Tâm |
| 2 | **Kho lưu backup 30 ngày** — artifact GitHub chỉ giữ **7 ngày** | [`DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md`](../../../../proposal/DE_XUAT_08_KHO_LUU_BACKUP_30_NGAY.md) | ❌ Chờ Tâm + TV2 |
| 3 | **Rotate khoá JWT đã lộ trong git history** | [`DE_XUAT_09_ROTATE_KHOA_JWT_DA_LO.md`](../../../../proposal/DE_XUAT_09_ROTATE_KHOA_JWT_DA_LO.md) | ⚠️ Một phần (xem 3.1) |

### 3.1. Phần TV4 **được** làm ngay cho task lock 3

Không cần ai quyết, nằm trong repo:

| # | Việc |
|---|---|
| 3a | Thêm dòng hướng dẫn sinh khoá vào `.env.example` (`openssl rand -base64 48`) — placeholder `REPLACE_WITH_RANDOM_SECRET_AT_LEAST_64_BYTES` hiện **không** nói cách sinh |
| 3b | `appsettings.json` đặt `Jwt:SigningKey` thành `""` + **guard fail-fast** khi thiếu `Jwt__SigningKey` (tái dùng pattern validate của B2 trong `MinioOptions.cs`) |
| 3c | Cập nhật `docs/HUONG_DAN_TEST_APP.md` + `README.md`: biến này **bắt buộc**, không có giá trị mặc định dùng được |

> ⛔ **Không** tự rotate khoá đang chạy ở môi trường thật, **không** tự xoá git history.

### 3.2. Trong lúc chờ quyết định — **không được ghi là đạt**

| Việc | Ghi đúng là |
|---|---|
| Backup | ✅ Đang chạy 03:00 ICT · ⛔ **chưa** bảo đảm bằng job canh |
| Thời hạn lưu | ⚠️ **7 ngày** (artifact GitHub), ⛔ **không** đạt 30 ngày |
| `restore.sh` | ✅ Đã drill thật, 14 bảng |
| Secret | ✅ Đã bỏ khỏi `render.yaml` + có `scan-secrets.sh` · ⛔ **khoá cũ còn trong history** |

---

## 4. Ảnh hưởng tới kế hoạch GĐ1

### 4.1. Task mới phát sinh — chèn **trước** N2

```
B5 (7 task) ──▶ B6 (4 task) ──▶ QD3-3a/3b/3c
       │
       ▼
   N2-A ──▶ N2-B ──▶ N2-E ──▶ N2-C ──▶ N2-D ──▶ N3 ──▶ N4-C
```

> B5/B6 là **điều kiện tiên quyết**: B6 mở khoá N2-E7, B5 mở khoá xem ảnh Draft trong wizard
> (một phần của N2-D1/D2 về trải nghiệm thật).

### 4.2. Bảng thay đổi trạng thái block

| Mã | Trước | Sau quyết định này |
|---|---|---|
| B1 | Đã triển khai, chờ duyệt `#20` | ⬄ Không đổi |
| B2 | Đã triển khai, chờ duyệt `#21` | ⬄ Không đổi |
| B3 | Xong (PR #19) | ⬄ Không đổi |
| B4 | Đã triển khai, chờ duyệt `#22` | ⬄ Không đổi |
| **B5** | 🔴 Chờ TV3 | ✅ **Đã quyết → PA-A · TV4 tự làm toàn bộ** |
| **B6** | 🔴 Chờ TV1 | ✅ **Đã quyết → PA-A · TV4 tự làm** |
| Lịch backup | 🟠 Chờ chốt nơi đặt | 🔴 **Đề xuất 07 — chờ Tâm** |
| Kho 30 ngày | 🔴 Chờ chốt | 🔴 **Đề xuất 08 — chờ Tâm + TV2** |
| Rotate JWT | 🔴 Chờ Tâm | 🔴 **Đề xuất 09 — chờ Tâm** (TV4 làm 3a/3b/3c) |

### 4.3. Việc **không** làm trong GĐ1

| Việc | Lý do |
|---|---|
| Rotate khoá JWT ở môi trường thật | Ngoài repo, thiếu phối hợp ⇒ dừng dịch vụ |
| Xoá git history | Không hoàn tác được, phá PR đang mở |
| Tạo bucket backup / đổi lịch backup | Ngoài repo, có chi phí |
| Cấu hình TLS, `docker-compose.prod.yml`, profile 2 API | **Thuộc tuần 5** — xem [`PLAN_GIAI_DOAN_1_N2_N4.md`](../plan/PLAN_GIAI_DOAN_1_N2_N4.md) §0.2 |

---

## 5. Điều kiện hoàn thành GĐ1 (cập nhật)

Ngoài checklist đã có trong plan GĐ1, thêm:

- [ ] **B5-1…B5-7** xong; `IMAGE_CONTRACT.md` §3 + §5 đã mô tả PA-3 presigned
- [ ] **B6-1…B6-4** xong; `--promote-admin` chạy được và bị từ chối ở `Testing`
- [ ] N2-E7 chạy được (đã có tài khoản Admin)
- [ ] **TV3 đã được thông báo** về B5-6/B5-7 (ghi trong PR hoặc handoff) — không cần chờ trả lời
- [ ] **QD3-3a/3b/3c** xong
- [ ] Presigned URL **không** xuất hiện trong response public của recipe Published
- [ ] ⛔ Log không chứa `X-Amz-Signature`
- [ ] `dotnet build` 0 warning · `dotnet format --verify-no-changes` exit 0 · `dotnet test` xanh ·
      `npx tsc --noEmit` exit 0 · `npm run build` exit 0 · `deploy/scan-secrets.sh` exit 0

---

## 6. Điều chỉnh các tài liệu liên quan

| File | Cập nhật |
|---|---|
| [`PLAN_GIAI_DOAN_1_N2_N4.md`](../plan/PLAN_GIAI_DOAN_1_N2_N4.md) | Thêm B5/B6 vào thứ tự thực hiện; N2-E7 bỏ trạng thái "chờ B6"; §5.2 cập nhật block |
| [`DE_XUAT_05...`](../../../../proposal/DE_XUAT_05_XEM_ANH_DRAFT_TRONG_WIZARD.md) | Trạng thái → **đã quyết PA-A** |
| [`DE_XUAT_06...`](../../../../proposal/DE_XUAT_06_TAI_TAO_USER_ADMIN_DE_SEED.md) | Trạng thái → **đã quyết PA-A** |
| [`DE_XUAT_07/08/09`](../../../../proposal/) | Tạo mới |
| [`docs/IMAGE_CONTRACT.md`](../../../../IMAGE_CONTRACT.md) | Sẽ cập nhật ở **task B5-5** khi code xong — ⚠️ chưa sửa trong file quyết định này để tránh mô tả contract chưa có hiệu lực |
| [`HANDOFF_TV4_TUAN4_N1.md`](../HANDOFF_TV4_TUAN4_N1.md) §6 | Bổ sung link file quyết định |

> 📌 **Thứ tự đúng:** sửa `IMAGE_CONTRACT.md` **cùng PR** với code B5, **không** sửa trước.
> Contract mô tả trạng thái đã có hiệu lực, không mô tả ý định.
