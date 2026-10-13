# ADR-TV4-004 — Pin `SixLabors.ImageSharp` 3.1.12: không có bản vá miễn phí cho 5 advisory hiện hành

Ngày: 09/10/2026 · Người thực hiện: Nguyễn Hữu Trung Sơn (2312739) — TV4 (phát hiện trong lúc dựng stack W5-4).
Phạm vi: phụ thuộc thư viện xử lý ảnh `SixLabors.ImageSharp` (dùng ở `ResizeImageJob` — resize thumbnail/medium).
Trạng thái: **Chốt giữ 3.1.12 + suppress NuGetAudit cho 5 advisory** (rủi ro chấp nhận, ghi rõ lý do).
Liên quan: `docs/evidence/TV4/Tuan05/MO_TA_CONG_VIEC_TUAN_5.md` (W5-4), `Directory.Build.props`.

---

## 1. Tóm tắt (TL;DR)

Ngày 09/10/2026 `dotnet restore` (Docker) bắt đầu **fail** với `NU1902/NU1903` (TreatWarningsAsErrors)
cho `SixLabors.ImageSharp 3.1.11` — NuGetAudit nhận dữ liệu advisory mới (công bố 15/09/2026). Điều tra:

- **5 advisory** ảnh hưởng: `GHSA-gwg2-r3hj-4w44` (ICC CLUT, Moderate) · `GHSA-j3p4-wp97-rph4`
  (HistogramEqualization, High) · `GHSA-j9gm-c75j-xc9q` (TIFF CCITT T4, High) ·
  `GHSA-jjfr-hcj7-qf5w` (TIFF CCITT T6, High) · `GHSA-wmxv-xphr-5c9g` (BigTIFF, Moderate).
- Phạm vi ảnh hưởng của cả 5: **`>= 2.0.0, <= 4.1.1`** → không tồn tại bản 3.x/4.x cũ nào an toàn.
- `first_patched_version` của nhóm này = **4.1.2** (riêng GHSA-jjfr-hcj7-qf5w trả về tường minh `4.1.2`).
- **ImageSharp 4.x chuyển sang comercial**: build với 4.1.2 fail vì thiếu license trả phí
  (`SixLabors.ImageSharp.targets`: "No Six Labors license found ... sixlabors.com/pricing").

⇒ 3.1.12 = **bản Apache-2.0 mới nhất và duy nhất dùng được**; 4.1.2 là bản vá nhưng **không hợp pháp để dùng miễn phí**.
Vì vậy không thể "chỉ cần nâng version" — đây là bài toán **không có bản vá dùng được**, nên ghi rủi ro chấp nhận.

## 2. Quyết định

1. Nâng `SixLabors.ImageSharp` **3.1.11 → 3.1.12** (patch release cuối của nhánh Apache-2.0 — vẫn dính 5 advisory,
   nhưng là bản sạch nhất dùng được; bản 4.1.1 về trước hư hơn không có gì hơn).
2. `Directory.Build.props`: thêm `NuGetAuditSuppress` cho đúng **5 URL advisory** (audit VẪN BẬT cho mọi package
   khác; chỉ bỏ qua gói này). Lý do chính thức recorded trong file, không che giấu.
3. **KHÔNG** nâng lên 4.x — yêu cầu license trả phí, ngoài khả năng dự án.
4. Khi SixLabors có bản vá miễn phí (2.x/3.x) hoặc dự án mua license, bỏ suppress + nâng version.

## 3. Hệ quả

- Build/CI không còn fail vì audit (restore + publish trong Docker OK).
- Thư viện xử lý ảnh vùng nhớ không bị exploit trong phạm vi dự án: `ResizeImageJob` chỉ chạy ảnh do **chính
  dự án upload** (validator đã chặn file quá nhỏ và phải có magic bytes phù hợp MIME); không có upload ảnh công khai
  tùy ý kiểu vô hạn. Rủi ro còn lại ghi nhận trong báo cáo W5-4 (giới hạn đã biết).

## 4. Xác minh

- `dotnet restore CulinaryBlog.sln` → tất cả 6 dự án restore thành công (0 lỗi audit).
- `dotnet list ... package --vulnerable` → giữ báo cáo nhưng không còn làm lỗi build.
- `dotnet build CulinaryBlog.sln -c Release` → 0W/0E; 427/427 test pass; `dotnet format --verify-no-changes` OK.