# ADR-TV4-001 — Vận hành, storage và logout tuần 1

Ngày: 16/09/2026 · Người thực hiện: Nguyễn Hữu Trung Sơn (2312739) — TV4.
Phạm vi: các lựa chọn thuộc phần Xuất bản/ảnh/vận hành (D5, D1 nền, D3) làm trong tuần 1.
Trạng thái: **Đề xuất / áp dụng giai đoạn dev, chưa có xác nhận chính thức của giảng viên/nhóm**. Không xem lựa chọn trong code là phê duyệt mâu thuẫn SRS.

## D22 — Redis lỗi fallback DB nhưng readiness fail khi Redis down

- **Nguồn SRS**: tr. 40, 44 (NFR REL/SCALE), mâu thuẫn mô tả KE_HOACH D22.
- **Quyết định**: giữ hai lần kiểm tra riêng —
  - `/health/live`: chỉ process còn sống (luôn 200 khi app chạy).
  - `/health/ready`: cần DB + Redis, trả 503 khi một trong hai down → proxy/Nginx ngừng nhận traffic.
  - API đọc trực tiếp vẫn fallback DB khi Redis down (cache-aside không chặn luồng).
  - Thực hiện tuần 1 dạng skeleton health, kiểm thử kịch bản down ở tuần 4 theo NFR-REL-002.
- **Ảnh hưởng**: Nginx upstream phải bám theo `/health/ready`; test integration tách biệt "API vẫn trả dữ liệu khi Redis down" và "readiness 503".
- **Người chốt**: TV4 (đề xuất) + confirm nhóm/tuần 2.

## D27 — Bucket MinIO public-read nhưng recipe Draft/Archived chỉ owner/Admin

- **Nguồn SRS**: tr. 33–38, 41; mâu thuẫn KE_HOACH D27.
- **Quyết định (tạm)**: bucket `culinary-blog` **mặc định private**; URL qua presigned hoặc qua API có quyền. Không đặt public-read ở môi trường dev để tránh lộ ảnh Draft/Archived.
  - Nếu nhóm muốn public-read (ảnh public, SEO), phải có CR đổi bucket policy và ghi rõ rủi ro + cơ chế bảo vệ nội dung Draft.
- **Ảnh hưởng**: `IFileStorageService` phải trả URL/đường dẫn có thể chuyển presigned; FE nhận URL có hạn hoặc proxy qua API; test quyền Draft không lộ file.
- **Người chốt**: TV4 + TV1 (bảo mật); confirm tuần 2.

## D06 — Logout cần Bearer, và luồng khi token hết hạn

- **Nguồn SRS**: tr. 21–22 + chương 8; KE_HOACH D06.
- **Quyết định**: `POST /api/v1/auth/logout` **yêu cầu Bearer** (như chương 8). Trả 204.
  - Nếu muốn hỗ trợ logout bằng refresh token (token hết hạn) phải đặc tả riêng cơ chế chứng minh quyền sở hữu — **chưa làm tuần 1** vì chưa có refresh (TV3 C5, tuần 2).
- **Ảnh hưởng**: OpenAPI/Scalar; trạng thái `logout` trong AUTH_CONTRACT được TV1 cập nhật song song; không log token.
- **Người chốt**: TV4 + TV1 + TV3 (khi C5 merge).

## D05 (handoff tham chiếu) — Refresh sẽ thuộc TV3 C5

- **Nguồn SRS**: tr. 18, 41; KE_HOACH D05. TV4 **không** tự làm rotation tuần 1 để tránh hai luồng refresh trùng nhau. Logout tuần 1 revoke scope hiện tại; tuần 2 gắn revoke refresh family do TV3 bàn giao.
- **Người chốt**: TV3 (C5) + TV4 (logout) + TV1 (contract chung).

## D25 (phối hợp) — Khóa phiên bản

- **Nguồn SRS**: tr. môi trường/công cụ; KE_HOACH D25.
- **Quyết định**: cùng nhóm khóa baseline — .NET 10 / EF 10 / Npgsql / PostgreSQL 16 / Redis 7 / MinIO (bản release bất biến) / Next.js (theo khóa chung) — ghi vào `global.json`, `packages.lock.json`, Compose image tag cố định, trước khi phát triển thêm.
- **Ảnh hưởng**: docker-compose dùng image tag cố định, không dùng `latest` cho Redis/MinIO.
- **Người chốt**: cả nhóm.

## Lịch sử và trạng thái

| Ngày | Nội dung | Trạng thái |
|---|---|---|
| 16/09/2026 | Soạn ADR-TV4-001 (D22/D27/D06/D05/D25) | Đề xuất, áp dụng dev |

Ghi chú: mọi quyết định ảnh hưởng yêu cầu (đặc biệt D27 đổi bucket policy) phải gửi giảng viên xác nhận theo quy trình mục 12.1 `KE_HOACH_DU_AN.md`; không tự coi là đã phê duyệt.