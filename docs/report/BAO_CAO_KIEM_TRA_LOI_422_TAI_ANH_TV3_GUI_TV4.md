# Báo cáo kiểm tra: lỗi 422 khi tải ảnh lên recipe đã có sẵn (báo cáo TV3 gửi TV4)

> **Người kiểm tra**: Nguyễn Hữu Trung Sơn (2312739 — TV4)
> **Ngày kiểm tra**: 01/10/2026
> **Báo cáo nguồn**: `D:\WNC\BAO_LOI_UPLOAD_ANH_gui_Son.md` (TV3 — Huỳnh Quốc Trung, TV3)
> **Mức độ**: cần xác nhận nguyên nhân trước khi sửa
> **Phạm vi**: FR-FILE-001/002 (Upload/PATCH/DELETE ảnh recipe) — TV4

> **Ghi chú**: Báo cáo này chỉ **kiểm chứng nhận định** của TV3, **chưa sửa code**.

---

## 🔴 ĐÍNH CHÍNH 03/10/2026 — KẾT LUẬN DƯỚI ĐÂY **ĐÃ BỊ BÁC BỎ**

> **Đọc mục này trước khi dùng báo cáo.** Bản gốc 01/10 kết luận giả thuyết (1) của TV3 là **SAI**.
> Kết luận đó **không đúng** — nguyên nhân thật đúng như TV3 nghi ngờ.

| Nội dung | Bản gốc 01/10 | **Đã kiểm chứng lại 03/10** |
|---|---|---|
| Giả thuyết (1) — thiếu `ValueGeneratedNever()` | ❌ **SAI** | ✅ **ĐÚNG** |
| Lý do bác bỏ | Cho rằng `AuditableEntityInterceptor` đã "workaround được" nên 422 không xảy ra | Workaround **không đáng tin** — nó chỉ che lỗi ở một đường gọi, và **vỡ khi** gặp đúng điều kiện thật |
| Kết quả thực tế | Không tái hiện được trên `main` | **Đã tái hiện được** trên PostgreSQL thật: `DbUpdateConcurrencyException` |
| Nguyên nhân gốc | — | `RecipeImageConfiguration.cs` thiếu `b.Property(i => i.Id).ValueGeneratedNever();` |
| Đã sửa chưa | — | ✅ **Đã sửa trên máy TV4** (bản vá cục bộ, không push — không thuộc dự án; "4 test hồi quy" ghi kèm **không tồn tại trong repo**, đối chiếu 07/10). ⛔ **Chưa merge** vào `2312739_NHTSon_D5-D6-D7` / `main` **tại thời điểm 03/10** |
| Cập nhật 07/10 | — | ✅ **`BUG-W4-01` đã vào `main`** (PR #29, commit `c624b9f`, migration `20261001112029`); còn lại test hồi quy + chốt C1/C2 → tuần 5, `KE_HOACH_TUAN_5_TV4.md` W5-10 |

**Vì sao bản gốc bác bỏ sai:** trạng thái `Added` mà `AuditableEntityInterceptor` tạo ra chỉ là **triệu chứng
của việc EF đánh dấu entity sai** — không phải cơ chế che lỗi. Chỉ cần một luồng gọi khác (không đi qua
interceptor, hoặc entity không được tracked như kỳ vọng) là 422 hiện ra ngay.

**Nguồn đầy đủ:** [`../evidence/TV4/Tuan04/report/BAO_CAO_LOI_TUAN_4_TV4.md`](../evidence/TV4/Tuan04/report/BAO_CAO_LOI_TUAN_4_TV4.md)
mục `BUG-W4-01` · [`../evidence/TV4/Tuan04/report/BAO_CAO_LAB_TUAN4_V2.md`](../evidence/TV4/Tuan04/report/BAO_CAO_LAB_TUAN4_V2.md)

> ⚠️ **Bài học**: báo cáo kết luận "không phải lỗi" là loại kết luận **nguy hiểm nhất** — vì nó khiến
> người đọc tin là đã xong và đóng mục. Chỉ nên kết luận "không phải lỗi" sau khi đã **tái hiện thất bại
> đúng điều kiện thật** (DB thật, đúng luồng API), không chỉ bằng đọc code.

---

## 1. Tóm tắt kết quả kiểm tra

| Giả thuyết | Kết luận | Giải thích |
|---|---|---|
| (1) 422 `recipe.version_conflict` khi tải ảnh vào recipe **đã có sẵn** do thiếu `ValueGeneratedNever()` cho `RecipeImage` | **Cơ sở ĐÚNG. Hiện tại CHƯA tái hiện trên `main`.** | Snapshot/Configuration của `RecipeImage` vẫn để `Id` là `ValueGeneratedOnAdd` (khác Ingredient/Step). Tuy nhiên, `AuditableEntityInterceptor` (TV4, commit `e3e8315`, sau đó TV1 bổ sung `RecipeImage` ở `f13e732`) có nhánh `Modified + RowVersion.OriginalValue.Length==0 → chuyển về Added`, nên workaround này đang chặn 422 ở luồng API hiện hành. |
| (2) 500 trước đó do TV3 **chưa tạo `.env` riêng** (thiếu MinIO creds) | **ĐÚNG.** | Khớp timeline TV3: sau khi khắc phục cấu hình (user-secrets/env), 500 biến mất và 422 mới lộ ra. Nguyên nhân 500 là cấu hình môi trường local (MinIO AccessKey/SecretKey rỗng), không phải lỗi code repo. |

---

## 2. Kiểm chứng thực tế (test chẩn đoán với Postgres thật)

Đã chạy 2 test chẩn đoán tạm thời trên `main` (DB Postgres local đang chạy):

### 2.1 Model metadata (EF Core)

| Entity | `Id` — `ValueGenerated` | `RowVersion` — `IsConcurrencyToken` |
|---|---|---|
| `RecipeImage` | **`OnAdd`** | **`True`** |
| `RecipeIngredient` | **`Never`** | **`True`** |

⇒ Xác nhận: **RecipeImageConfiguration.cs chưa có `b.Property(i => i.Id).ValueGeneratedNever();`** (chỉ có Ingredient/Step). Snapshot (`AuthDbContextModelSnapshot.cs:184`) vẫn là `.ValueGeneratedOnAdd()`.

### 2.2 Thêm ảnh vào recipe ĐÃ TỒN TẠI (load qua `Include(r => r.Images)`)

Thử nghiệm trực tiếp qua DbContext (tương tự luồng `RecipeImageRepository.GetRecipeWithImagesAsync` + `recipe.AddImage`):

| Thông số đo | Kết quả |
|---|---|
| **EF state** của `RecipeImage` mới **trước SaveChanges** | **`Detached`** |
| **SaveChanges outcome** | **`SaveChanges OK`** |
| **Số dòng `RecipeImages` sau SaveChanges** | **`1`** |
| **`RowVersion` được gán** | **`16 bytes`** (token opaque) |
| **Insert/Update thực tế** | **INSERT** (thành công) — **không thấy UPDATE 0 dòng** |

**Giải thích kỹ thuật:** Mặc dù `Id` là `Guid.NewGuid()` (non-empty) lúc add vào collection, `AuditableEntityInterceptor` (dòng 43–48) phát hiện `entry.State == Modified`, `entity is RecipeImage`, và `OriginalValue.RowVersion.Length == 0` (entity mới trong bộ nhớ, chưa từng load từ DB) → **tự động ép `entry.State = EntityState.Added` trước khi SaveChanges**. Nhờ đó EF sinh **INSERT INTO "RecipeImages"** thay vì UPDATE. Đây là **workaround** đã có trên `main` (TV4 `e3e8315` + TV1 `f13e732` bổ sung `RecipeImage`).

---

## 3. Phân tích báo cáo TV3

### 3.1 Về nhận định 422

TV3 đọc code rất chính xác:

| Điểm TV3 nêu | Kiểm chứng |
|---|---|
| `RecipeImageConfiguration.cs:12` **không có `ValueGeneratedNever()`**; snapshot `ValueGeneratedOnAdd` | **ĐÚNG.** (xem §2.1) |
| `recipe.AddImage` chỉ `_images.Add(image)` (domain), EF có thể coi là Modified do Id non-empty | **ĐÚNG về nguyên lý EF.** |
| Khi EF coi Modified → interceptor `AuditableEntityInterceptor` đổi RowVersion (Modified) → có thể dẫn đến UPDATE 0 dòng → `DbUpdateConcurrencyException` → gộp thành 422 `recipe.version_conflict` (ApiExceptionHandler) | **ĐÚNG về kịch bản lý thuyết.** Trên `main` hiện tại **đã bị chặn** bởi nhánh chuyển Modified→Added của interceptor. |

**Kết luận:** TV3 nêu **root cause chính xác** (thiếu config). Tuy nhiên, do đã có workaround trong interceptor (bao gồm `RecipeImage` từ `f13e732`), 422 **chưa tái hiện** với luồng chuẩn API hiện tại. 422 có thể vẫn xuất hiện ở một số path khác (vd. entity detached, unit test không dùng interceptor đúng cách, hoặc context khác) — đúng như TV3 nghi ngờ.

### 3.2 Về 500 trước đó

TV3 viết: *“Trước khi gặp 422, máy mình còn gặp một lỗi cấu hình khác (500 do thiếu khóa MinIO…) Đã khắc phục ở máy mình bằng user-secrets, và 422 mới lộ ra sau đó.”*

**Kiểm chứng:** Phù hợp hoàn toàn với `BAO_CAO_LOI_UPLOAD_ANH_500.md` (TV4). MinIO creds rỗng → `MinioException` (401 Unauthorized) → `ApiExceptionHandler` nhánh default trả **500 `server.error`**. Sau khi bổ sung cấu hình môi trường (user-secrets/.env), 500 biến mất, request tới storage thành công hơn, các lỗi khác (nếu có) mới lộ ra.

**Kết luận:** **ĐÚNG.**

---

## 4. Khuyến nghị sửa (theo đúng nhận định TV3)

TV3 đề xuất **đúng và chuẩn**:

1. **Thêm `b.Property(i => i.Id).ValueGeneratedNever();`** vào `RecipeImageConfiguration.cs` (giống hệt `RecipeIngredientConfiguration.cs:20`, `RecipeStepConfiguration.cs:20`).
2. **Kiểm tra migration:** `dotnet ef migrations add Fix_RecipeImage_Id_ValueGeneratedNever` — snapshot sẽ thay đổi (`ValueGeneratedOnAdd` → `Never`). Up/Down có thể rỗng (vì DB không dùng identity), nhưng vẫn nên có migration để snapshot khớp model (tham khảo `20260926145137_RecipeChildIdsValueGeneratedNever`).
3. **Viết test tích hợp** thêm ảnh vào recipe **đã có sẵn** (qua API hoặc trên DbContext thật) để tái hiện trước khi sửa (red) và xác minh sau khi sửa (green). Cách này tránh phụ thuộc hoàn toàn vào interceptor làm lá chắn.

**Lý do khuyến nghị:** sửa **root cause** ở tầng Configuration/Model, không nên dựa vào interceptor để “gỡ lỗi ngầm” cho entity con. Việc interceptor còn là **lá chắn phòng vệ hữu ích**, nhưng config đúng sẽ khiến EF hành xử nhất quán và rõ ràng hơn (đúng pattern Ingredient/Step đã làm từ commit `d67300d`).

---

## 5. Kết luận tổng thể

- **Nhận định TV3 về 422 là CÓ CƠ SỞ ĐÚNG.** Thiếu `ValueGeneratedNever()` cho `RecipeImage` — đây là **root cause**.
- **Trên `main` hiện hành chưa tái hiện 422** vì `AuditableEntityInterceptor` đã bao gồm `RecipeImage` (workaround). Tuy nhiên, đó là **giải pháp vá tạm thời**, không phải fix cấu hình chuẩn.
- **Nhận định TV3 về 500 là ĐÚNG.** Do thiếu cấu hình MinIO local (.env/user-secrets).
- **Đề xuất sửa của TV3 là hợp lý, cần thiết và nên được thực hiện.** (ưu tiên fix config trước, có thể giữ interceptor như lá chắn).

**Kết luận cuối cùng:** Báo cáo TV3 **chính xác về mặt phân tích code**. Cần sửa `RecipeImageConfiguration.cs` thêm `ValueGeneratedNever()` và cân nhắc thêm migration + test tích hợp để tránh tái phát.