# TRẠNG THÁI THỰC HIỆN TUẦN 4 — TV4 · Nguyễn Hữu Trung Sơn (2312739)

- **SRS**: v1.1.1 (Approved 16/09/2026) · **Reviewer**: Nguyễn Thanh Tâm (Nhóm trưởng)
- **Nhánh**: `2312739_NHTSon_D5-D6-D7` từ `origin/main` = `7fe8fc2`
- **Kế hoạch**: [`KE_HOACH_TUAN_4_TV4.md`](KE_HOACH_TUAN_4_TV4.md) · **Mô tả việc**: [`MO_TA_CONG_VIEC_TUAN_4.md`](MO_TA_CONG_VIEC_TUAN_4.md) · **Sổ evidence**: [`SO_EVIDENCE_TUAN_4.md`](SO_EVIDENCE_TUAN_4.md)
- **Cổng nghiệm thu**: **G4** 24/24 ô kỹ năng K01–K24 · **G5** coverage ≥ 80% + sửa lỗi chặn/bảo mật + CI xanh

---

## 1. Tóm tắt tiến độ

| Nhóm | Task | Kế hoạch | Đã xong | Đang làm | Còn lại | Tỷ lệ |
|---|---|---|---|---|---|---|
| N0 | Mở đầu tuần 4 (baseline, bằng chứng 500, mapping K01) | 5 | 3 | 0 | 2 | 60% |
| N1 | D5 — Health/observability/backup/multi-instance | 8 | 0 | 0 | 8 | 0% |
| N2 | D7 — E2E, tấn công file, CI, số đo | 8 | 0 | 0 | 8 | 0% |
| N3 | D6 — Lab L5 + bù mục 2 L4 | 4 | 0 | 0 | 4 | 0% |
| N4 | Runbook, release, bàn giao | 4 | 0 | 0 | 4 | 0% |
| **Tổng** | | **29** | **3** | **0** | **26** | **≈10%** |

> Tỷ lệ tính theo **số việc đã có bằng chứng**, không tính "đã lên kế hoạch".

---

## 2. N0 — Mở đầu tuần 4

| # | Việc | Trạng thái | Bằng chứng / commit |
|---|---|---|---|
| 1 | Tạo nhánh `2312739_NHTSon_D5-D6-D7` từ `origin/main` (`7fe8fc2`) | ✅ Xong | Nhánh có trên remote, HEAD `7fe8fc2` |
| 2 | Tạo `docs/evidence/TV4/Tuan04/` với 4 tài liệu | ✅ Xong | `KE_HOACH_TUAN_4_TV4.md`, `MO_TA_CONG_VIEC_TUAN_4.md`, `SO_EVIDENCE_TUAN_4.md`, `TRANG_THAI_THUC_HIEN_TUAN_4.md` |
| 3 | Commit `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md` vào nhánh này | ✅ Xong | Commit trong nhánh tuần 4 (nội dung **không sửa**) |
| 4 | Chạy lại baseline trên `7fe8fc2` (build / format / test) và ghi số liệu thật | ⬜ Chưa làm | Số của tuần 3 là 172/172 tại `80b2c0e` — **không dùng lại**, vì `main` đã có thêm 4 commit |
| 5 | Bảng mapping K01: FR/NFR ↔ ADR ↔ đường dẫn evidence cho 24 ô | ⬜ Chưa làm | Tuần 3 ghi "Chưa làm" |

**Ghi chú bổ sung (30/09)** — nằm ở **tài liệu trạng thái này**, không sửa báo cáo gốc: lỗi
`/search` đã được **TV2 sửa trên `main`** bằng commit `e523579` *"fix(frontend): extract
SearchFilterSelect to client component"* (đúng **Phương án B** trong báo cáo). File
`docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md` được **giữ nguyên 100%** như quyết định nhóm, để
giữ nguyên giá trị làm bằng chứng; việc xác nhận lại bằng test (TC1–TC3) thuộc N0 và cần chạy
trước khi đóng báo cáo.

---

## 3. N1 — D5: Health, observability, backup/restore, multi-instance

| # | Việc | Trạng thái | Bằng chứng | Ghi chú |
|---|---|---|---|---|
| 1 | `ObjectStorageHealthCheck` xác thực credential (B4) | ⬜ Chưa làm | — | Có đề xuất sẵn; có thể tự làm |
| 2 | Test `/health/ready` → 503 khi Redis chết (thay dòng "200 hoặc 503" ở `HealthTests.cs:41`) | ⬜ Chưa làm | — | |
| 3 | OTEL collector + Serilog→Seq + **trace HTTP→DB thật** | ⬜ Chưa làm | — | Tuần 3 đã tự ghi nhận thiếu bằng chứng trace |
| 4 | Script backup `pg_dump` 03:00 giữ 30 ngày + backup file + **drill restore** | ⬜ Chưa làm | — | Timezone backup cần nhóm chốt |
| 5 | 2 API instance dùng chung cache/queue (hoặc ADR ghi giới hạn) | ⬜ Chưa làm | — | Cache hiện là in-process |
| 6 | Sitemap cron 02:00 UTC + distributed lock (CR-7) | ⬜ Chưa làm | — | |
| 7 | B1/B2 (500 → 503; fail-fast) | ⛔ Chờ quyết định nhóm | — | Không tự sửa trước khi duyệt |
| 8 | Bỏ secret hardcode khỏi `render.yaml` + secret scan CI | ⬜ Chưa làm | — | |

---

## 4. N2 — D7: E2E, tấn công file, CI, số đo

| # | Việc | Trạng thái | Bằng chứng | Ghi chú |
|---|---|---|---|---|
| 1 | Playwright thật (`playwright.config.ts` + `@playwright/test`) | ⬜ Chưa làm | — | Điều kiện cho 4 ô K |
| 2 | 5 luồng E2E (TV4 viết **publish** + **search**) | ⬜ Chưa làm | — | Search phủ TC1–TC12 của báo cáo 500 |
| 3 | Cổng CI frontend (`npm ci` → `tsc` → `next build` → `lint`) | ⬜ Chưa làm | — | Nguyên nhân gốc lỗi 500 lọt CI |
| 4 | Kịch bản tấn công file (size / MIME giả / hỏng / rate limit / ownership) | ⬜ Chưa làm | — | Hiện chỉ có unit test |
| 5 | Resilience có số liệu (DB/Redis/storage/worker/2 instance) | ⬜ Chưa làm | — | |
| 6 | Commit script k6 + EXPLAIN lại | ⬜ Chưa làm | — | Tuần 3 chạy bằng heredoc ⇒ không tái lập được |
| 7 | Ngưỡng coverage `Application` ≥ 80% trong CI | ⬜ Chưa làm | — | |
| 8 | D4-UI còn treo: progress upload + nút Unpublish/Archive + WCAG | ⬜ Chưa làm | — | Nợ từ tuần 3 |

---

## 5. N3 — D6: Lab

| # | Việc | Trạng thái | Bằng chứng | Ghi chú |
|---|---|---|---|---|
| 1 | Bù mục 2 L4 (refresh/forms/FTS/Google) | ⬜ Chưa làm | — | Nợ tuần 3 |
| 2 | Lab L5 (8 phase) trên `practice/TV4/L5` | ⬜ Chưa làm | — | Chấm điểm kiểu L4 |
| 3 | `SOK_LAB_L5.md` | ⬜ Chưa làm | — | |
| 4 | Mở PR cho nhánh lab | ⬜ Chưa làm | — | `practice/TV4/L4` hiện chỉ có branch |

---

## 6. N4 — Runbook, release, bàn giao

| # | Việc | Trạng thái | Bằng chứng | Ghi chú |
|---|---|---|---|---|
| 1 | `docs/RUNBOOK.md` có số liệu thật | ⬜ Chưa làm | — | |
| 2 | Deploy lặp lại được (compose prod hoặc checklist Render) | ⬜ Chưa làm | — | |
| 3 | Cập nhật `HUONG_DAN_CHAY_TV4.md` + `README.md` + `CHANGELOG.md` | ⬜ Chưa làm | — | |
| 4 | Chốt sổ 24/24 K + nộp review Tâm | ⬜ Chưa làm | — | |

---

## 7. Kỹ năng

| Chỉ số | Tuần 3 vào | Tuần 4 hiện tại | Mục tiêu |
|---|---|---|---|
| Số ô kỹ năng có minh chứng | 9/24 | **9/24** | 24/24 tại G4 |
| Ô đang làm dở | — | 0 | — |
| Ô còn thiếu | 15 | **15** | 0 |

Danh sách 15 ô cần bù: K02, K03, K04, K06, K07, K09, K10, K14, K15, K16, K17, K18, K19, K21, K22.
Chi tiết theo ô: `SO_EVIDENCE_TUAN_4.md` mục 2.

---

## 8. Rủi ro đang theo dõi

| Rủi ro | Mức độ | Xử lý |
|---|---|---|
| Không có hạ tầng test frontend nào (0 jest, 0 playwright, CI không build FE) | 🔴 Cao | Làm N2-1 và N2-3 trước các E2E khác; ưu tiên luồng `search` |
| Chưa có backup/restore | 🔴 Cao | N1-4 tạo script tối giảu chạy được trước khi lịch 03:00 |
| Cache in-process ⇒ số đo 2 instance không đáng tin | 🟡 | Chọn Redis hoặc ADR ghi giới hạn; không đo rồi báo như 2 instance |
| B1/B2/B4/B6 chưa có quyết định nhóm | 🟡 | B4 tự làm; B1/B2/B6 ghi "chờ quyết định" |
| Chưa có tài khoản Admin để test `/hangfire`, DevConfigParityTests | 🟡 | Dùng seed có sẵn, ghi hạn chờ |
| 15 ô kỹ năng trong 1 tuần | 🟡 | N2 và N3 làm song song; ô thiếu thì ghi thiếu, không đánh dấu đủ |
| `main` có 4 commit mới | 🟡 | Commit nhỏ + rebase `origin/main` trước khi mở PR |

---

## 9. Ghi chú nguồn gốc tài liệu `Lab 04` trong `docs/evidence/TV4/`

> [!NOTE]
> 4 file `TUAN_4.md`, `BAO_CAO_LAB_04.md`, `Lab04_2312739_NguyenHuuTrungSon.docx`,
> `Lab4_2312739_NguyenHuuTrungSon.docx` nằm trực tiếp trong `docs/evidence/TV4/` **không do TV4
> tạo**; chúng được tạo trong commit `80b2c0e` của **Nguyễn Thanh Tâm (TV1 — nhóm trưởng)**
> (cùng một commit sinh bộ 3 file tương ứng cho TV2 và TV3).
> Theo quyết định nhóm ngày 30/09: **giữ nguyên, không sửa, không xoá**, chỉ ghi chú nguồn gốc;
> **không dùng làm minh chứng cá nhân của TV4**. Tài liệu do chính TV4 viết: bộ 4 file trong
> `Tuan04/` này và `docs/evidence/TV4/HUONG_DAN_CHAY_TV4.md` (commit `c5361eb`).

---

## 10. Việc làm tiếp theo (thứ tự ưu tiên)

1. Chạy lại baseline trên `7fe8fc2` → điền số liệu thật vào `SO_EVIDENCE_TUAN_4.md` mục 1.
2. Verify `/search` (TC1–TC3) + `next build` → đóng nốt báo cáo lỗi 500.
3. Dựng Playwright + cổng CI frontend (N2-1, N2-3).
4. B4 health check + test 503 khi Redis chết (N1-1, N1-2).
5. Backup/restore script + drill (N1-4).
6. OTEL collector + Seq + chụp trace thật (N1-3).
7. Sitemap cron 02:00 UTC + distributed lock (N1-6).
8. Lab L5 + bù mục 2 L4; mở PR cho nhánh lab (N3).
9. Kịch bản tấn công file + resilience + k6 script (N2-4, N2-5, N2-6).
10. Ngưỡng coverage CI + D4-UI còn treo (N2-7, N2-8).
11. Runbook + release + chốt sổ 24/24 K, nộp review Tâm (N4).
