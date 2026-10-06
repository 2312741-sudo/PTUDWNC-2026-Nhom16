# Báo cáo Giai đoạn 2 – Kiểm tra tích hợp tổng hợp (End-to-End Integration) – TV4

**Người thực hiện:** Nguyễn Hữu Trung Sơn (TV4 – 2312739)
**Ngày lập:** 05/10/2026
**Cập nhật:** 05/10/2026 (sau nhận định lại mục tiêu GĐ2)
**Nhánh:** 2312739_NHTSon_D5-D6-D7
**Commit mới nhất:** 6ea4aa7
**PR:** https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/pull/29

## 1. Mục tiêu Giai đoạn 2
Giai đoạn 2 tập trung vào kiểm tra tổng hợp toàn hệ thống (integration end-to-end): đánh giá chức năng từ đầu đến cuối, ưu tiên sự phối hợp giữa Frontend và Backend thay vì kiểm tra đơn lẻ.

Quy trình theo yêu cầu:
1. Xác nhận chức năng đã được triển khai đầy đủ.
2. Kiểm tra chức năng hoạt động ổn định trong hệ thống.
3. Kiểm tra giao tiếp FE–BE đảm bảo chính xác và ổn định khi hai thành phần tích hợp.

## 2. Phạm vi kiểm tra tích hợp (FE–BE)
| STT | Luồng | Mức độ | Ghi chú |
|---|---|---|---|
| 1 | Đăng ký → Đăng nhập → JWT | Đã có | Playwright 26/26; FE–API auth. Dẫn chiếu SO_EVIDENCE_TUAN_4.md |
| 2 | Wizard tạo/cập nhật + Upload ảnh | Đã có (cần rà soát cross-integration) | Gọi API + upload; IMAGE_CONTRACT.md |
| 3 | Tìm kiếm/Khám phá | Đã có | DiscoveryAndSearchTests + E2E |
| 4 | Chi tiết công thức + SEO/JSON-LD | Đã có | FE render theo API |
| 5 | Ảnh (presigned/proxy) Draft vs Published | Đã có phần lớn; rà soát | §7b IMAGE_CONTRACT.md; DbSeeder fix |

## 3. Kết quả kiểm chứng tích hợp
| Mục | Kết quả | Bằng chứng |
|---|---|---|
| Playwright toàn bộ | 26/26 PASS (3 lần liên tiếp) | src/frontend/e2e/*; SO_EVIDENCE_TUAN_4.md |
| Recipe-publish repeat | 16/16 PASS (--repeat-each=4) | SO_EVIDENCE_TUAN_4.md |
| FE–BE contract ảnh | Khớp IMAGE_CONTRACT.md | docs/IMAGE_CONTRACT.md (gồm §7c 05/10) |
| Auth/JWT tích hợp | Ổn định | Week4AuthAndSecurityLabTests + E2E |
| Tìm kiếm tích hợp | Ổn định | DiscoveryAndSearchTests + E2E |
| Seed không ghi đè URL ảnh | 2/2 PASS | DbSeederUserImageUrlTests |

## 4. Tóm tắt triển khai đầy đủ (dẫn chiếu GĐ1)
| Nhóm | Tóm tắt | Dẫn chiếu |
|---|---|---|
| N2 | 7/8 xong; còn N2-D1/D2/D3 | BAO_CAO_GIAI_DOAN_1_N2_N4.md §3 |
| N4-C | 426/426; 85/85; build/format 0 lỗi; App 96.31% | BAO_CAO_GIAI_DOAN_1_N2_N4.md §2; README.md |
| N3 | Gần đóng; Lab L5 3/7 PASS | BAO_CAO_GIAI_DOAN_1_N2_N4.md; BAO_CAO_GIAI_DOAN_2_N3.md |
| Media | Proxy auth + resize + presigned Draft | IMAGE_CONTRACT.md; ADR-TV4-001 |

## 5. Quan sát giao tiếp FE–BE (thực tế)
- (a) Seed ghi đè URL ảnh → ảnh sai; **đã fix + test hồi quy**.
- (b) --migrate báo thành công giả → **đã fix** (lỗi thật + exit 1).
- (c) ux_recipe_images_one_primary + soft-delete → **ghi rõ** §7c IMAGE_CONTRACT.md.

## 6. Kết luận GĐ2
- Chức năng triển khai đầy đủ – xác nhận qua E2E 26/26 + evidence GĐ1.
- Hoạt động ổn định – 3 lần liên tiếp xanh; 426/426 + 85/85 xanh.
- Giao tiếp FE–BE chính xác/ổn định – xác minh qua các luồng cốt lõi; fix quan trọng được khoá bằng test.
- Báo cáo độc lập: tóm tắt + dẫn chiếu GĐ1, đảm bảo có khái quát nếu GĐ1 không truy cập được.
