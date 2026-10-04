# Đề xuất gỡ block B6 — Không có user **Admin** nên không test được `/hangfire` và CRUD category

> **Block**: [`TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md`](../evidence/TV4/Tuan03/Report/TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md) §2 B6
> **Mức**: 🟢 Thấp · **Phát hiện**: [`TEST_CASE_TICH_HOP_FE_BE.md`](../evidence/TV4/Tuan03/Report/TEST_CASE_TICH_HOP_FE_BE.md) B03, §8
> **Cần ai quyết**: ✅ **ĐÃ CHỐT** — TV4 (Nguyễn Hữu Trung Sơn), ngày 03/10/2026 → chọn **phương án A**
>
> ✅ **TRẠNG THÁI 03/10/2026: ĐÃ QUYẾT — phương án A (CLI `--promote-admin`).** Xem
> [`QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md`](../evidence/TV4/Tuan04/QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md) §2.
> ⏳ **Chưa có code** — chờ triển khai B6-1…B6-4.
>
> ```powershell
> dotnet run --project src/backend/CulinaryBlog.API -- --promote-admin <email>
> ```
> ⛔ Không seed mật khẩu cứng · chỉ chạy ở `Development` · idempotent.
>
> **Quyết định của TV4 về phần việc của TV1:** `DbSeeder` là phần của **TV1**, nhưng do **TV4** đã
> chốt phương án nên **TV4 tự làm** (theo nguyên tắc "ngoài contact"). ⛔ **Không sửa seed data** của
> TV1 — chỉ thêm nhánh CLI trong `Program.cs` (file của TV4). TV1 **được thông báo trước** để biết.

---

## 1. Thực trạng

| Mắt xích | Hiện trạng |
|---|---|
| Role `Admin` | Đã seed (`DbSeeder.cs:46-50`) |
| **User** có role `Admin` | **Không có** — seeder chỉ tạo 5 tác giả `...@culinary.local`, không gán role |
| Đăng ký mới | Tự được gán `Author` (đã kiểm: `GET /auth/me` → `Author`) |
| `POST /api/v1/categories` | `403` với mọi tài khoản thường (đúng) ⇒ **không ai tạo được danh mục mới bằng UI** |
| `GET /hangfire` (dashboard job resize D23) | `403` với tài khoản thường (đã kiểm) ⇒ **không ai xem được job** |
| Cách lên Admin | Sửa DB thủ công: `UPDATE "AspNetUsers" SET ...` qua role join — mỗi máy một kiểu |

Hệ quả trong lúc QA: muốn kiểm tra job resize 300×300/800×600, dashboard Hangfire, hoặc tạo danh mục
mới, phải tự ý đụng database — dễ sót lại quyền Admin trong DB dùng chung.

## 2. Nguyên nhân

1. Seeder tạo **role** nhưng không tạo **user** mang role `Admin` — có thể là cố ý tránh đặt mật khẩu
   admin cố định trong dữ liệu mẫu, nhưng thiếu bước "đường đi chính thức" để lên Admin.
2. Không có tài liệu nào mô tả cách lên Admin an toàn cho môi trường dev.
3. `AdminDashboardAuthorizationFilter` + `AdminPolicy` chỉ kiểm tra role, không có cơ chế quản lý
   vai trò qua API (không có endpoint phân quyền) ⇒ chỉ có thể bằng DB.

## 3. Giải pháp đề xuất

### Phương án A (khuyến nghị) — CLI/command `promote-admin` thay vì seed mật khẩu cứng

Thêm lệnh chạy tay, **không** đặt mật khẩu admin trong repo:

```powershell
# Tạo/nâng quyền cho chính tài khoản đang đăng kập, không cần biết mật khẩu ai
dotnet run --project src/backend/CulinaryBlog.API -- --promote-admin <email>
```

- Không có secret trong git; ai có tài khoản local tự nâng quyền.
- Dùng chung cho cả CI (nếu sau này cần test quyền Admin).
- Nhất quán với pattern `--migrate` / `--seed` đã có (`Program.cs:202-216`).

### Phương án B — seed sẵn admin với mật khẩu dev

```csharp
// DbSeeder: tạo admin@culinary.local với mật khẩu dev, chỉ khi môi trường Development
```

| Ưu | Nhược |
|---|---|
| Đơn giản, ai cũng đăng nhập được | Mật khẩu admin nằm trong git ⇒ nếu nhầm lên production là lỗ hổng |
| Test tự động dễ dùng | Dễ thành thói quen dùng tài khoản admin cho mọi thứ |

Nếu chọn B **bắt buộc**: đặt trong `appsettings.Development.json` (dev-only), in cảnh báo khi
seed, và ghi rõ trong `README` rằng tài khoản này không tồn tại ở production.

### Phương án C — tài liệu hoá cách nâng quyền bằng SQL

Thêm vào `docs/HUONG_DAN_CHAY_TV4.md` một mục "cần quyền Admin thì làm thế này" kèm câu SQL.
Chi phí ~10 phút, không đụng code, nhưng mỗi máy vẫn tự làm và dễ quên dọn.

## 4. Rủi ro

| Rủi ro | Khả năng | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| Chọn B: mật khẩu admin trong repo lọt lên môi trường thật | Trung bình | Chiếm tài khoản quản trị | Chỉ seed ở `Development`; tài liệu cảnh báo; kiểm tra seed không chạy ở Production |
| Chọn A: ai cũng tự nâng quyền trên máy dev ⇒ dễ quen tài khoản admin | Cao (dev) | Xử lý sự cố sai | Chỉ cho phép khi `IsDevelopment()`; hướng dẫn dùng tài khoản thường khi test |
| Đụng `DbSeeder`/`Program.cs` ⇒ rủi ro hồi quy seed | Thấp | Seed lỗi, app chạy lệch dữ liệu | Chạy full test suite; kiểm tra nhánh `--seed` vẫn hoạt động |
| Không làm gì ⇒ mỗi người tự sửa DB, dễ sót quyền | **Cao** | Rủi ro bảo mật dữ liệu dev, khó truy vết | Tối thiểu chọn C để có 1 cách làm thống nhất |

## 5. Test case

| # | Test case | Kỳ vọng |
|---|---|---|
| 1 | `dotnet run -- --promote-admin <email>` (phương án A) | Lệnh thành công, in email vừa nâng quyền |
| 2 | Sau khi nâng: `GET /auth/me` | `roles` chứa `Admin` |
| 3 | Sau khi nâng: `POST /categories` | `201` |
| 4 | Sau khi nâng: `GET /hangfire` | `200` (dashboard hiển thị) |
| 5 | Tài khoản thường: `POST /categories`, `GET /hangfire` | `403` (đã kiểm, phải giữ nguyên) |
| 6 | Lệnh với email không tồn tại | Báo lỗi rõ ràng, không crash |
| 7 | Lệnh ở môi trường `Testing`/CI | Bị từ chối (nếu chọn giới hạn theo môi trường) |
| 8 | `dotnet test CulinaryBlog.sln -c Release` sau thay đổi | Xanh, `Skipped=0` |
| 9 | Seeding chạy lại nhiều lần (`--seed`) | Không nhân bản user/role, không mất dữ liệu |

## 6. Chuỗi lỗi liên quan (nếu có)

```
Nguoi dung can xem job resize / tao danh muc
  └─ GET /hangfire, POST /categories -> 403 (require role Admin)
      └─ khong co user nao co role Admin (DbSeeder chi tao role, khong tao user)
          └─ phai sua DB thu cong: UPDATE "AspNetUsers" ... qua role join
              └─ moi may lam theo mot cach -> de sot quyen Admin trong DB dung chung
                  └─ QA khong phai quay ve tim loi upload -> ton thoi gian
```

## 7. Quyết định cần chốt

✅ **ĐÃ CHỐT 03/10/2026 — TV4 chọn phương án A.** Mục này giữ lại để đối chiếu, không cần hỏi lại.

| # | Câu hỏi | Quyết định |
|---|---|---|
| 1 | Chọn phương án nào? | ✅ **A** — CLI `--promote-admin`. B bị loại vì đặt mật khẩu trong git; C không gỡ được block |
| 2 | Nếu B: tài khoản ở đâu, ai cảnh báo? | ⛔ Không áp dụng — không chọn B |
| 3 | Cần endpoint quản lý vai trò trong dự án không? | ⛔ **Không** — ngoài phạm vi tuần 4. Chỉ CLI local, chỉ `Development` |

### Phần còn lại để theo dõi

| Việc | Trạng thái |
|---|---|
| B6-1…B6-4 triển khai | ⏳ Chưa làm |
| Lệnh **bị từ chối** ở `Testing`/`Production` | 🔲 Test khi làm B6-1 |
| Chạy lại 2 lần không nhân bản role | 🔲 Test idempotent |
| ⛔ **Không** thêm user Admin vào `DbSeeder` | 🔲 Kiểm tra khi review |
| **N2-E7** chạy được sau khi có Admin | ⏳ Đang chờ B6 |
| Hướng dẫn dùng trong `docs/evidence/TV4/HUONG_DAN_CHAY_TV4.md` | 🔲 Chưa viết |

Xem kế hoạch chi tiết và nguyên tắc "ngoài contact":
[`QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md`](../evidence/TV4/Tuan04/QUYET_DINH_THUC_HIEN_GIAI_DOAN_1.md) §2.
