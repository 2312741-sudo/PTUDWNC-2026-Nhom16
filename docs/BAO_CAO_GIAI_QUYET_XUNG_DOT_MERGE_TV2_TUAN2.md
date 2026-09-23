# BÁO CÁO GIẢI QUYẾT XUNG ĐỘT (MERGE CONFLICT) TUẦN 2

> **Dự án**: Culinary Blog — Blog Ẩm thực và Nấu ăn
> **Học phần**: Phát Triển Ứng Dụng Web Nâng Cao (PTUDWNC) — Nhóm 16
> **Người merge**: Nguyễn Hữu Trung Sơn (TV4 — 2312739)
> **Ngày**: 23/09/2026
> **Nhánh**: `2312796-ngo-quoc-truong-vi-feat/TV2-week2-discovery-search-google` → merge `main`
> **Reviewer phê duyệt**: None (chưa phê duyệt)

---

## 1. Bối cảnh & Nguyên nhân xung đột

- Nhánh TV2 được tách từ `main` tại commit `6e64a3b` (đầu tuần) để làm Công việc Tuần 2 (B2–B4, B7): khám phá/tìm kiếm công thức và Google login.
- Trong khoảng giữa tuần, `main` được thêm **nhiều nhánh khác** merge vào cùng lúc:
  - **TV4 (Trung Sơn)**: D1 Tuần 2 — MinIO storage/upload, thêm package `Minio`, docs IMAGE_CONTRACT, HANDOFF.
  - **TV3 (Quốc Trung)**: Recipe aggregate + AuthDbContext hợp nhất (PR #8, #10).
  - **TV1 (Thanh Tâm)**: Các trang `login/register/recipes` trong Next.js, seed dữ liệu Lab 2 và báo cáo tổng hợp.
- Khi TV2 merge `main` về cuối tuần, cả hai phía đã **sửa đổi độc lập cùng các file** nên Git không tự hợp được, dẫn tới **7 file xung đột** (6 kiểu `both modified`, 1 kiểu `both added`).

> **Lưu ý**: Trang frontend `auth/login` và `auth/recipes` nằm ở phía "theirs" thực chất do **TV1 (Nguyễn Thanh Tâm)** tạo ở commit `6033f5f`, chứ không phải TV4. TV4 chỉ chạm vào backend MinIO và `packages.lock.json` là nguyên nhân gây tình trạng TV2 gọi chung là “xung đột TV2 ↔ TV4”.

## 2. Bảng tổng hợp 7 file xung đột & cách giải quyết

| # | File | Kiểu conflict | Phía TV2 (ours) | Phía main (theirs) | **Quyết định & lý do** |
|:--:|---|---|---|---|---|
| 1 | `src/frontend/src/lib/api.ts` | both modified | `getRecipes`, `searchRecipes`, `loginWithGoogle` | `login`, `register` | **Giữ cả 2 bộ hàm.** Đây là conflict “giả” — Git ghép lệch vì cả 2 phía cùng chèn code sau hàm `logout`. Các hàm không trùng tên nên giữ toàn bộ. |
| 2 | `src/frontend/src/app/recipes/page.tsx` | both added | Trang **đầy đủ**: lọc/sắp xếp/phân trang/category, dùng `RecipeCard` (server component) | Trang **stub** ghi: “đang được TV2 & TV3 hoàn thiện theo tiến độ Tuần 2 & 3” | **Giữ trọn bản TV2.** Bản trên main chỉ là chỗ chờ TV2 bàn giao. |
| 3 | `src/frontend/src/app/auth/login/page.tsx` | both added | Có Google OAuth thật (`GoogleSignInButton`), lưu key `token`, redirect `/` | UI đẹp hơn (show/hide password, forgot link), dùng hàm `login()` qua `api.ts`, nút Google chỉ là **placeholder `alert()`**, lưu key `accessToken`/`refreshToken`, redirect `/dashboard/profile` | **Hợp nhất**: lấy UI của main làm gốc, **thay placeholder bằng `GoogleSignInButton` của TV2**, giữ luồng token `accessToken`/`refreshToken` và redirect `/dashboard/profile` của main. |
| 4-7 | `packages.lock.json` × 4 (Backend API, Infrastructure, Tests, ConcurrencySpike) | both modified | Thêm `Google.Apis.Auth` (TV2) → transitive `System.Management` | Thêm `Minio` (TV4) → transitive `System.IO.Hashing`, `System.Reactive` | **Regenerate bằng `dotnet restore`.** File lock là file sinh tự động; csproj đã hợp cả 2 package nên ghép tay sẽ sai `contentHash`. Chạy `dotnet restore -p:RestorePackagesWithLockFile=true --force-evaluate` rồi xác nhận `--locked-mode` pass (đúng lệnh CI). |

## 3. Chuẩn hóa phụ (ngoài phạm vi conflict) — Token key

Trên chính `main`, TV1 mới lưu `accessToken`/`refreshToken` trong khi các trang `dashboard/profile` và `dashboard/categories` cũ vẫn đọc `token` → **đăng nhập xong vào dashboard sẽ không lấy được token**. Nhóm chốt chuẩn hóa về **`accessToken` (+ `refreshToken`)**:

- `login/page.tsx` (đã hợp): lưu `accessToken`/`refreshToken`/`user`.
- `src/frontend/src/components/GoogleSignInButton.tsx`: chuyển từ `token` sang `accessToken`/`refreshToken`.
- `dashboard/profile/page.tsx` và `dashboard/categories/page.tsx` (2 chỗ): đọc `accessToken`, fallback về `token` cho tương thích ngược.

## 4. Kết quả kiểm thử & xác nhận

| Hạng mục | Lệnh | Kết quả |
|---|---|---|
| Khôi phục package | `dotnet restore CulinaryBlog.sln --locked-mode` | ✅ Pass (đồng bộ lệnh CI `backend.yml`) |
| Build backend | `dotnet build CulinaryBlog.sln -c Release` | ✅ 0 warning / 0 error |
| Test backend | `dotnet test CulinaryBlog.sln -c Release` | ✅ **83/83 pass** (CulinaryBlog.Tests 78 + ConcurrencySpike 5) |
| Build frontend | `npm run build` | ✅ Compile + typecheck + lint OK, đủ 11 routes (lỗi `ECONNREFUSED` khi prerender chỉ do backend chưa chạy local) |
| Conflict còn lại | `git ls-files -u` | ✅ 0 file unmerged |

Đối chiếu tài liệu/kế hoạch (`docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md` mục 3.2, `docs/evidence/TV2/TUAN_2.md`):
- Endpoint `GET /api/v1/recipes`, `GET /api/v1/recipes/search`, `POST /api/v1/auth/google` — ✅ đều tồn tại trong `Program.cs`.
- Contract docs `SEARCH_AND_DISCOVERY_CONTRACT.md`, `RECIPE_LIST_CONTRACT.md`, `GOOGLE_AUTH_CONTRACT.md` — ✅ khớp triển khai (schema `data/meta`, filter/sort/page, `q >= 2`, chỉ trả về `Published`).
- Test `DiscoveryAndSearchTests`/`CategoryTests`/`ArchitectureTests` — ✅ đang trong bộ test pass ở trên.

## 5. Bàn giao

- Kết quả merge ở trạng thái **đã resolve, đã verify, chưa commit** — để Nhóm trưởng (TV1) review theo quy trình trước khi tạo PR chính thức.
- Nội dung này cũng được ghi lại trong **message của merge commit** để lịch sử Git rõ ràng người merge, vấn đề gặp phải và cách giải quyết.