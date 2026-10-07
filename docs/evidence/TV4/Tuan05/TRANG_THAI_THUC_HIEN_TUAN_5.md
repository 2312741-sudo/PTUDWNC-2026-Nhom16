# TRẠNG THÁI THỰC HIỆN TUẦN 5 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

- **SRS**: v1.1.1 · **Reviewer**: Nguyễn Thanh Tâm (Nhóm trưởng)
- **Nhánh**: `2312739_NHTSon_D5-D7` từ `origin/main` = `262201b` (merge PR #29 — đã đóng tuần 4)
- **Kế hoạch**: [`KE_HOACH_TUAN_5_TV4.md`](KE_HOACH_TUAN_5_TV4.md) · **Mô tả việc**: [`MO_TA_CONG_VIEC_TUAN_5.md`](MO_TA_CONG_VIEC_TUAN_5.md) · **Sổ evidence**: [`SO_EVIDENCE_TUAN_5.md`](SO_EVIDENCE_TUAN_5.md)
- **Cổng**: **G6** — staging hoàn chỉnh; performance/SEO/a11y có kết quả và giới hạn; 5 E2E pass

---

## 📌 Trạng thái ngày 07/10/2026 — ảnh chụp lúc bắt đầu tuần

> Đây là tài liệu **trạng thái**, cập nhật khi có việc đổi. Mọi ô ghi "0%" nghĩa là **chưa có bằng chứng
> chạy thật trong tuần 5** — kể cả việc thấy "có vẻ đã làm" ở tuần 4.

### 0.1. Tổng quan

| Mục | Trạng thái 07/10 |
|---|---|
| Nhánh | ✅ Tạo `2312739_NHTSon_D5-D7` từ `origin/main` = `262201b` |
| Tài liệu tuần 5 | ✅ Kế hoạch · Mô tả việc · Trạng thái · Sổ evidence (4 file trong `Tuan05/`) |
| Việc W5-1…W5-9 | ⬜ **0/9** — chưa bắt đầu |
| PR cho nhánh tuần 5 | ⬜ Chưa mở |
| Baseline trên `262201b` | ⬜ **Chưa chạy lại** — số dưới đây là số kế thừa từ tuần 4 |

### 0.2. Số liệu kế thừa từ tuần 4 (chưa phải số của tuần 5)

| Hạng mục | Số (tuần 4, tại `fd90572`) | Trạng thái dùng cho tuần 5 |
|---|---|---|
| `dotnet test -c Release` | **426/426 pass** (421 + 5 `ConcurrencySpike`), Skipped 0 | ⏳ Phải đo lại trên `262201b` |
| Build / format | 0 warning · 0 error · `dotnet format` exit 0 | ⏳ Đo lại |
| Coverage gate | `CulinaryBlog.Application` **96.31%** ≥ 80% | ⏳ Đo lại |
| Frontend | Jest **85/85** · `tsc`/`lint`/`build` exit 0 | ⏳ Đo lại |
| Playwright | **26/26** (publish 4 + search 12 + upload 10) | ⏳ Chạy lại + mở rộng 5 luồng G6 |
| k6 | 3 lần, 0.00% lỗi, p50 6.23ms / p95 14.14ms / p99 25.06ms — **đo trên dev** | ⏳ Đo lại **trên staging** |
| CI | Xanh mới nhất được xác nhận: `8a585ef` (Backend + Frontend) | ⏳ Xác nhận run cho `main` sau PR #29 |

> ⚠️ Số tuần 4 **không** được dùng làm số chính của tuần 5. Việc W5-1 là đo lại toàn bộ.

---

## 1. Tóm tắt tiến độ

| # | Khối | Việc | Kế hoạch | Xong | Đang làm | Còn lại | Tỷ lệ |
|---|---|---|---|---|---|---|---|
| W5-1 | Mở đầu | Baseline + mở PR | 3 | 0 | 0 | 3 | 0% |
| W5-2 | Staging | Deploy lặp lại được (2 lần) | 4 | 0 | 0 | 4 | 0% |
| W5-3 | Staging | 2 API instance + Nginx upstream | 4 | 0 | 0 | 4 | 0% |
| W5-4 | Bảo mật | HTTPS / HSTS / CORS / volumes | 5 | 0 | 0 | 5 | 0% |
| W5-5 | Vận hành | Runbook 7 mục | 7 | 0 | 0 | 7 | 0% |
| W5-6 | UI | Progress upload + Unpublish/Archive | 4 | 0 | 0 | 4 | 0% |
| W5-7 | Kiểm thử | 5 E2E flows trên staging | 5 | 0 | 0 | 5 | 0% |
| W5-8 | Số đo | k6 + SEO trên staging | 3 | 0 | 0 | 3 | 0% |
| W5-9 | Lab/nợ | Zod/RHF · Google OAuth · 3 phase L5 · npm audit | 4 | 0 | 0 | 4 | 0% |
| **Tổng** | | | **39** | **0** | **0** | **39** | **0%** |

> Tỷ lệ tính theo **số việc có bằng chứng chạy thật trong tuần 5**.

---

## 2. Việc đang làm / kế hoạch theo ngày

| Ngày | Việc dự kiến | Trạng thái |
|---|---|---|
| 07/10 | W5-1 baseline + mở PR · bắt W5-2 | ⬜ Chưa bắt đầu |
| 08/10 | W5-2 (2 lần) + W5-3 profile 2 API | ⬜ |
| 09/10 | W5-4 HTTPS/HSTS/CORS · bắt W5-6 (D4-UI) | ⬜ |
| 10/10 | W5-5 runbook · W5-6 xong · W5-9 | ⬜ |
| 11/10 | W5-7 5 E2E · W5-8 số đo · chốt sổ, nộp review | ⬜ |

*(Ngày tham chiếu — nhóm chưa chốt ngày bắt đầu/kết thúc tuần 5 trong tài liệu chung.)*

---

## 3. Việc kế thừa từ tuần 4 (chưa xong, ghi ở đây để khỏi dò lại)

| Việc | Nguồn | Việc tuần 5 phải làm |
|---|---|---|
| `N2-D1/D2` — progress upload % + nút Unpublish/Archive | `Tuan04/plan/PLAN_GIAI_DOAN_1_N2_N4.md` §7 | **W5-6** |
| `N4-A` runbook 7 mục | `PLAN_GIAI_DOAN_1_N2_N4.md` §7 | **W5-5** |
| `N4-B` deploy staging · 2 API · TLS/HSTS · volumes | `PLAN_GIAI_DOAN_1_N2_N4.md` §7 | **W5-2, W5-3, W5-4** |
| 5 E2E flows (G6) — tuần 4 mới có 2 luồng của TV4 | `PLAN_GIAI_DOAN_1_N2_N4.md` §7 | **W5-7** |
| `N3-A3` Zod/RHF · `N3-A5` Google OAuth (chờ credentials) | `PLAN_GIAI_DOAN_1_N2_N4.md` §7 | **W5-9** |
| Số đo load/SEO trên staging | 6-tuần L88 | **W5-8** |
| 3 phase Lab L5 chưa PASS (`isr-detail`, `image-opt`, `search-ssr`) | `Tuan04/SOK_LAB_L5.md` | **W5-9** (cắt trước) |
| `npm audit` 10 vulnerability (9 high, 1 critical) | `Tuan04/Report/BAO_CAO_TIEN_DO_TUAN_4_TV4_SRS.md` | **W5-9** |
| 3 việc hạ tầng chờ chốt (lịch backup · kho 30 ngày · rotate JWT) | đề xuất 07/08/09 | ⛔ **Không tự quyết** — §5 |
| 24 ô K chờ Tâm xác nhận + mapping K01 | `../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` | Nộp lại + chờ xác nhận |

**Không thuộc TV4 tuần 5**: checklist WCAG/responsive (**TV2**), Jest/RTL frontend unit test (**TV1**).

---

## 4. Ô kỹ năng K01–K24 (giữ nguyên số của tuần 4 — chưa ô nào được Tâm xác nhận)

| Mức | Số ô | Ô | Ghi chú tuần 5 |
|---|---:|---|---|
| 🟢 đủ bằng chứng | 6 | K08, K13, K19, K20, K21, K24 | Chờ Tâm xác nhận và ghi ngày |
| 🟢 gần đạt | 8 | K02, K03, K07, K10, K12, K14, K15, K23 | K10/K23 mở lại ở W5-4/W5-2 |
| 🟡 có nền, thiếu lab/đo lại | 8 | K01, K04, K05, K06, K11, K16, K17, K22 | K05/K17 mở ở W5-6; K22 mở ở W5-8 |
| ⬜ còn thiếu thật | 2 | K09 (Google credentials), K18 (thuộc TV2) | Không tự đánh dấu đạt |

---

## 5. Block / việc chờ nhóm

| Việc | Mức | Cần ai | Từ ngày |
|---|---|---|---|
| Chốt nơi đặt lịch backup (đề xuất 07) | 🔴 | Tâm + TV2 | 30/09 |
| Kho lưu backup 30 ngày (đề xuất 08) | 🔴 | Tâm + TV2 | 30/09 |
| Rotate/xoá khoá JWT ngoài repo (đề xuất 09) | 🟡 | Tâm | 03/10 |
| Google OAuth credentials (K09) | 🟡 | Nhóm | từ tuần 3 |
| Tâm xác nhận 24 ô K + mapping | 🟡 | Tâm | 30/09 |
| G1 rate limit phân tán (TV1) — ảnh hưởng số k6 | 🟡 | TV1 | 03/10 |
| Luồng E2E `category` do ai viết | 🟡 | TV2 | 07/10 |

---

## 6. Rủi ro đang theo dõi

| Rủi ro | Mức | Xử lý |
|---|---|---|
| 39 việc trong 5 ngày | 🔴 | Thứ tự + thứ tự cắt ở `MO_TA_CONG_VIEC_TUAN_5.md` §4; không cắt việc điều kiện G6 |
| Staging chỉ có 1 API, `container_name` cứng, không TLS | 🔴 | W5-2/3/4 — đây chính là phần G6 đang thiếu |
| D4-UI dở dang từ tuần 3 → không E2E được luồng gỡ xuất bản | 🟡 | W5-6 làm cùng ngày với W5-2 |
| Số đo tuần 4 làm trên dev → không đại diện staging | 🟡 | W5-8 đo lại trên staging, ghi giới hạn |
| CI có thể đỏ do thay đổi compose/nginx | 🟡 | Chạy lại CI sau mỗi lần sửa cấu hình |
| Nhánh mới chưa có PR → vi phạm DoD | 🔴 | W5-1 mở PR ngay ngày đầu |

---

## 7. Nhật ký thay đổi

| Ngày | Nội dung |
|---|---|
| 07/10 | Tạo nhánh `2312739_NHTSon_D5-D7` từ `origin/main` = `262201b`; lập 4 tài liệu tuần 5; ghi trạng thái bắt đầu **0/9 khối việc**. Chưa chạy kiểm định trên nhánh mới. |
