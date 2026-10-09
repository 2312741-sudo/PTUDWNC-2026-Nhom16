# T3 — Rà soát tính nhất quán dữ liệu (SELECT-only)

Ngày chạy: 07/10/2026. Toàn bộ câu lệnh là `SELECT` (file `/tmp/consistency_checks.sql`, không commit vì là file tạm ngoài repo), **không sửa dữ liệu**. Chạy trên hai DB:
- `cb_w5_seed`: DB tạm sạch, 8/8 migration mới nhất, vừa `--seed` (25 categories theo log / **24 thật**, 100 recipes).
- `culinary_blog`: DB dev, **chỉ đọc**, không chạm.

**Sửa một lỗi quan trọng khi viết câu lệnh**: `RecipeStatus` enum thật là `Draft=0, Published=1, Archived=2` (`src/backend/CulinaryBlog.Domain/Enums/RecipeStatus.cs`) — bản nháp đầu dùng nhầm `Status=2` cho "Published" (thực ra là Archived), làm 3 chỉ số ra toàn 0 sai. Đã sửa lại `Status=1` và chạy lại; số liệu dưới đây là **sau khi sửa**.

## Kết quả

| Chỉ số | `cb_w5_seed` (sạch) | `culinary_blog` (dev) |
|---|---|---|
| Ảnh sống `IsPrimary` trùng trong cùng công thức | 0 | 0 |
| Ảnh xoá mềm còn `IsPrimary=true` | 0 | **4** |
| `StepNumber` trùng giữa bước sống | 0 | 0 |
| `StepNumber` hổng giữa bước sống (không liên tục 1..N) | 0 | 0 |
| `OrderIndex` nguyên liệu không liên tục | 0 | 0 |
| Recipe Published thiếu nguyên liệu sống | 0 | **6** |
| Recipe Published thiếu bước sống | 0 | **6** |
| Recipe Published không có ảnh chính sống | 0 | **9** |
| Slug trùng giữa công thức sống | 0 | 0 |
| Slug công thức sống trùng slug công thức đã xoá mềm | 0 | 0 |
| `RowVersion` null (Recipes / RecipeImages) | 0 / 0 | 0 / 0 |
| Ảnh có `OriginalUrl` dạng `/images/recipes/...` (dấu vết DbSeeder) | 100 | 128 |
| Tổng recipes sống / categories sống | 110* / 24 | 207 / 25 |

\* `cb_w5_seed` có 110 (không phải 100) vì 10 recipe được tạo thêm bởi 8 ca Playwright E2E chạy trước đó trong cùng phiên (test tự `POST /auth/register` rồi tạo recipe mới) — không phải seed, không ảnh hưởng kết luận "DbSeeder tạo dữ liệu sạch".

## Chi tiết 9 công thức Published có vấn đề trên `culinary_blog` (chỉ mã, không email/tên)

| RecipeId (rút gọn) | Nguyên liệu sống | Bước sống | Ảnh chính sống |
|---|---|---|---|
| `9b8ee329…` | 3 | 5 | 0 |
| `5e439606…` | 1 | 1 | 0 |
| `f2154ea6…` | 1 | 1 | 0 |
| `1dbc8690…` | 0 | 0 | 0 |
| `f4c6e0a0…` | 0 | 0 | 0 |
| `713ea384…` | 0 | 0 | 0 |
| `279cb7a5…` | 0 | 0 | 0 |
| `5c1b89e0…` | 0 | 0 | 0 |
| `a0827acd…` | 0 | 0 | 0 |

**Hai nhóm khác nhau, không nên gộp chung**:
1. **3 công thức đầu** (`9b8ee329`, `5e439606`, `f2154ea6`): có nguyên liệu/bước đầy đủ, **có** nằm trong nhóm "ảnh xoá mềm còn `IsPrimary=true`" ở trên — tức là công thức này **từng có** ảnh chính, người dùng xoá ảnh đó, không tải ảnh thay thế, cờ `IsPrimary` trên dòng đã xoá mềm không được dọn. Đây là **dữ liệu thiếu bước dọn dẹp sau xoá ảnh**, không phải lỗi migration (đã kiểm chỉ mục thật trên `culinary_blog`: vẫn là bản CŨ `WHERE "IsPrimary" = true` không lọc `IsDeleted` — khớp đúng việc `culinary_blog` chưa áp migration `20261004155213`, nhất quán với lịch sử migration lệch đã biết).
2. **6 công thức sau**: **rỗng hoàn toàn** — Published nhưng 0 nguyên liệu, 0 bước, 0 ảnh. Đây là **vi phạm C02 thật trong dữ liệu dev** (SRS: công thức Published phải có nguyên liệu + bước). Nhiều khả năng là dữ liệu cũ tạo trước khi ràng buộc C02 được áp dụng ở tầng Application, hoặc tạo trực tiếp bằng script/seed cũ. **Không sửa** (chỉ đọc, DB dev); đưa vào bàn giao cho nhóm — có thể cần một job dọn dữ liệu hoặc chuyển các recipe này về Draft.

## Kết luận
- `cb_w5_seed` (DbSeeder trên nhánh TV3 hiện tại) sinh dữ liệu **nhất quán 100%** theo mọi tiêu chí trên — không phát hiện lỗi ở tầng ghi dữ liệu mới.
- Toàn bộ 3 vấn đề thật tìm thấy chỉ nằm ở `culinary_blog` (dữ liệu dev tích lũy qua nhiều tuần), không phải lỗi code hiện tại của TV3. Đã ghi lại để nhóm biết, không sửa theo đúng luật "DB dev chỉ đọc".
- Phát hiện (4.2 trong `KIEM_TRA_TUAN4.md`) về chỉ mục `ux_recipe_images_one_primary` được xác nhận thêm một lần nữa tại đây bằng dữ liệu thật: `culinary_blog` đang chạy chỉ mục bản CŨ (không lọc `IsDeleted`), phù hợp với việc 4 dòng ảnh xoá mềm vẫn giữ `IsPrimary=true` tồn tại được (không có ảnh sống nào cạnh tranh `IsPrimary=true` cùng lúc nên không vi phạm ngay chỉ mục cũ).
