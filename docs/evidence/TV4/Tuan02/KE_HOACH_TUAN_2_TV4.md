# KẾ HOẠCH TUẦN 2 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

- **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026, giải quyết C01–C09)
- **Mã task tuần 2**: D1 (upload/metadata/primary/delete), D4 (resize 300×300/800×600), D3 (publish/unpublish), D2/D4 nền image uploader cho editor.
- **Nhánh Git đề xuất**: tiếp tục nhánh `2312739_NHTSon_D1-D3-D5-D6` (hoặc tách sub-branch theo task).
- **Reviewer & nghiệm thu**: Nguyễn Thanh Tâm (Nhóm trưởng).
- **Trạng thái**: Chưa bắt đầu — ghi theo kế hoạch Tuần 2 sau khi Tuần 1 đã pass CI + stack chạy.

> Tài liệu này là **kế hoạch thực thi Tuần 2**, mọi việc ban đầu ở trạng thái **Chưa làm**.
> Cập nhật trạng thái theo thực tế ở `TRANG_THAI_THUC_HIEN_TUAN_2.md`.

---

## 1. Mục tiêu tuần 2

| Tiêu chí | Bàn giao kỳ vọng |
|---|---|
| Nghiệp vụ | Upload ảnh recipe + metadata (FR-FILE-001/002); primary; delete; resize 300×300 / 800×600; publish/unpublish draft (FR-RCP-005/006) có điều kiện ≥1 ingredient + ≥1 step (C02) |
| Nghiệm thu | Editor có thể upload ảnh; ảnh path theo FR-RCP-008 `recipes/{recipeId}/{uuid}.{ext}`; 422 khi publish thiếu thành phần; private bucket xử lý đúng cách (D27) |
| Skill | Chứng minh K04/K13/K14/K15 lab; K01/K02/K03/K06/K08/K10/K20/K23/K24 SP; mở sổ TV4-Kxx hoàn thành mục đã đăng ký Tuần 1 |

---

## 2. Phân rã công việc tuần 2

### N2-tu2 — D1: MinioStorageService + API upload/metadata/primary/delete (đầy đủ)

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | Triển khai `MinioStorageService` trong Infrastructure (MinIO SDK hoặc HTTP-based), implement `IFileStorageService` | Application/API không phụ thuộc MinIO; phục vụ upload/delete qua interface |
| 2 | Validator ảnh: 4 MIME hợp lệ (jpeg/png/webp/gif hoặc theo SRS), giới hạn kích thước, magic bytes | 400 khi sai MIME/quá size/file giả |
| 3 | Endpoints: upload (201), set primary (200), delete (204); path `recipes/{recipeId}/{uuid}.{ext}` theo FR-RCP-008 | CRUD ảnh đúng chuẩn, dữ liệu recipe liên kết đúng |
| 4 | Xóa ảnh cũ khi set primary thay thế / delete recipe (soft delete → ảnh ?) | Không rò rỉ object trong bucket |
| 5 | Contract API cho TV3 tích hợp editor | `docs/` contract mới (IMAGE_CONTRACT.md) |

### N4-tu2 — D4: Resize 300×300 / 800×600

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | Resize khi upload (tạo 2 bản thumb + medium) theo SRS | `{uuid}_300x300.{ext}`, `{uuid}_800x600.{ext}` |
| 2 | Cấu hình kích thước qua config, không hard-code | Dễ thay đổi/khóa |

### N3-tu2 — D3: Publish/unpublish draft

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | `PATCH /recipes/{id}/publish` với điều kiện ≥1 ingredient + ≥1 step (C02) | 422 `RECIPE_PUBLISH_INCOMPLETE` khi thiếu; 200 khi đủ |
| 2 | `PATCH /recipes/{id}/unpublish`; public ẩn ngay (cache invalidation) | Draft/Published chuyển trạng thái đúng |
| 3 | Test ownership (author mới được publish recipe của mình) | 403 cho non-owner |

### D6 (tiếp) — Lab + sổ skill

| # | Việc làm | Kết quả mong đợi |
|---|---|---|
| 1 | Tạo nhánh `practice/TV4/L4` từ skeleton Tuần 1; lab resize/sitemap/delayed/restart | Nhánh lab có commit |
| 2 | Cập nhật sổ evidence `TV4-K04/K13/K14/K15` theo mục đăng ký Tuần 1 | Sổ K có trạng thái rõ ràng |

---

## 3. Phụ thuộc & bàn giao tuần 2

### TV4 cần nhận:
| Từ | Nhận gì | Khi nào |
|---|---|---|
| TV3 (C1/C2) | Recipe entity/DTO/status/RowVersion + fixture có nguyên liệu + bước | Tuần 2 |
| TV1/TV3 | C5 refresh token (để logout revoke family) | Cuối tuần 2 |

### TV4 phải bàn giao:
| Bàn giao cho | Gì | Khi nào |
|---|---|---|
| TV3 | `IFileStorageService` + DTO (Tuần 1 đã có) + API upload contract | Giữa tuần 2 |
| Cả nhóm | Bucket policy D27 private/public xác nhận (CR-1 đã mở Tuần 1) | Đầu tuần 2 |

---

## 4. Checklist cổng tuần 2

- [ ] `MinioStorageService` implement + integration test MinIO down → rõ ràng (DoD Tuần 1 đã hứa).
- [ ] Upload/primary/delete/resize hoàn tất + contract API cho editor.
- [ ] Publish 422 khi thiếu thành phần; unpublish ẩn public.
- [ ] Lab `practice/TV4/L4` có commit; sổ K04/K13/K14/K15 đã cập nhật.
- [ ] CI pass sau mỗi task.