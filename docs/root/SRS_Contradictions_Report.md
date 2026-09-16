# Báo cáo Mâu thuẫn Nội tại — SRS Culinary Blog v1.0.0

> Tài liệu đánh dấu các mâu thuẫn nội tại được phát hiện trong
> `SRS_Culinary_Blog_v1.0.0.md` (bản text convert từ PDF).
> Ngày phân tích: 16/09/2026.
> **Trạng thái: ĐÃ ĐƯỢC GIẢI QUYẾT TOÀN BỘ trong SRS v1.1.1.**

| # | Mô tả tóm tắt | Mã/phần liên quan | Mức độ | Quyết định trong SRS v1.1.1 |
|---|---|---|---|---|
| C01 | Hard Delete vs Soft Delete cho Recipe | FR-RCP-007 / §8.3 / NFR-REL-003 / BaseEntity | Nghiêm trọng | **Soft Delete**: `IsDeleted = true`, query filter `!IsDeleted`. Giữ nguyên file ảnh trên MinIO. |
| C02 | Điều kiện publish: chỉ Steps vs Steps + Ingredients | FR-RCP-005 / RECIPE_PUBLISH_INCOMPLETE | Nghiêm trọng | **≥ 1 ingredient VÀ ≥ 1 step**. Trả về 422 `RECIPE_PUBLISH_INCOMPLETE`. |
| C03 | Category cache TTL: 60 vs 30 phút | FR-CAT-001 / NFR-PERF-003 | Trung bình | **60 phút** (IMemoryCache / Redis). |
| C04 | Default page size: 12 vs 10 | FR-RCP-001 / FR-SRCH-001 | Nhẹ | **Default pageSize = 12**. |
| C05 | Field name: TimerMinutes vs DurationMinutes | §7.3 / FR-RCP-010 / §8.5 | Trung bình | **`TimerMinutes`** cho RecipeStep. |
| C06 | Field name: OrderIndex vs SortOrder | §7.4 / FR-RCP-009 / §8.6 | Trung bình | **`OrderIndex`** cho RecipeIngredient. |
| C07 | Category DELETE: hard delete vs soft delete | FR-CAT-005 / §8.2 | Nghiêm trọng | **Hard Delete**: Xóa vật lý khỏi DB. Trả về 409 nếu còn recipes. |
| C08 | Response format không wrap trong `data` | §5.2 / FR-AUTH-001 / FR-CAT-001 | Trung bình | **Toàn bộ response thành công LUÔN wrap trong `{ "data": ... }`**. |
| C09 | Category uniqueness: logic slug suffix không khớp constraint | §7.6 / FR-CAT-003 | Nhẹ | **Name có UNIQUE constraint**, Slug cũng có UNIQUE (nếu trùng suffix `-2`, `-3`...). |
