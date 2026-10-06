# PLAN TRIỂN KHAI — QUY TRÌNH 3 GIAI ĐOẠN CỦA TV4 (TUẦN 4)

| Mục | Nội dung |
|---|---|
| Người lập | Nguyễn Hữu Trung Sơn (TV4 · 2312739) |
| Ngày lập | 03/10/2026 |
| Nhánh làm việc | `2312739_NHTSon_D5-D6-D7` (không push `main`) |
| Phạm vi | Tổ chức công việc còn lại của tuần 4 theo 3 giai đoạn do TV1 giao |
| Trạng thái file | 🟡 **Kế hoạch — đã thực thi GĐ1, GĐ3; GĐ2 (D0 xét lỗi) còn mở** |

> [!NOTE]
> **📌 Trạng thái 3 giai đoạn, cập nhật 05/10.**
>
> | Giai đoạn | Trạng thái | Báo cáo |
> |---|---|---|
> | **GĐ1** — N2/N3/N4 | 🟡 Xong phần đủ điều kiện: **N2 7/8** (dở dang `D1/D2/D3`), **N3-B/C1/C2 xong**, N4-C xong. Còn `N3-A3` (Zod/RHF) và `N3-A5` (Google OAuth — chờ credentials) | [`BAO_CAO_GIAI_DOAN_1_N2_N4.md`](../report/BAO_CAO_GIAI_DOAN_1_N2_N4.md) |
> | **GĐ2** — D0 xét lỗi | ⬜ **Chưa thực thi** | [`PLAN_GIAI_DOAN_2_D0_XET_LOI.md`](PLAN_GIAI_DOAN_2_D0_XET_LOI.md) (chỉ là plan) |
> | **GĐ3** — sửa lỗi | 🟡 Đã thực thi **2 lỗi** (`BUG-W4-M1` khoá JWT `R3`, `BUG-W4-M2` conflict `.gitignore`) + **việc cuối: kiểm chứng báo cáo TV1**. Còn `BUG-W4-01/02/03` (đã sửa trên lab, **chưa merge**) và các lỗi chờ quyết định | [`BAO_CAO_GIAI_DOAN_3_SUA_LOI.md`](../report/BAO_CAO_GIAI_DOAN_3_SUA_LOI.md) |

---

## 0. Vì sao cần file này

Công việc tuần 4 còn lại được chia thành **3 giai đoạn nối tiếp nhau**, mỗi giai đoạn có đầu vào /
đầu ra riêng và **sinh ra một báo cáo riêng**. Ba giai đoạn không được gộp làm một lúc, vì:

- Giai đoạn 1 tạo ra **phát sinh**; không ghi lại phát sinh thì giai đoạn 2 sẽ phải đoán.
- Giai đoạn 2 tìm ra **lỗi mới**; nếu không tách bạch lỗi cũ với lỗi mới thì báo cáo lỗi sẽ loãng.
- Giai đoạn 3 cần một **plan sửa lỗi riêng** để đối chiếu với 3 kế hoạch gốc; làm sửa lỗi xen kẽ
  với làm tuần 4 sẽ không biết cái nào đủ, cái nào thiếu, cái nào làm dư.

> **Nguyên tắc bất di bất dịch:** Giai đoạn sau **chỉ bắt đầu** khi báo cáo giai đoạn trước đã xong
> và TV1 đã kiểm tra. Không nhảy cóc.

---

## 1. Bản đồ 3 giai đoạn

| Giai đoạn | Tên | Nội dung | Plan chi tiết | Báo cáo sau khi xong |
|---|---|---|---|---|
| **1** | Làm công việc tuần 4 | Làm **N2 → N3 → N4**, ghi phát sinh theo quy tắc | [`PLAN_GIAI_DOAN_1_N2_N4.md`](PLAN_GIAI_DOAN_1_N2_N4.md) | `BAO_CAO_GIAI_DOAN_1_N2_N4.md` |
| **2** | Dò xét lỗi | Branch lab, tìm **lỗi mới**, đánh giá rủi ro theo NFR + K01–K24, rà mâu thuẫn tài liệu | [`PLAN_GIAI_DOAN_2_D0_XET_LOI.md`](PLAN_GIAI_DOAN_2_D0_XET_LOI.md) | `BAO_CAO_GIAI_DOAN_2_D0_XET_LOI.md` |
| **3** | Sửa lỗi | Plan sửa lỗi (sửa gì / quyết gì / ảnh hưởng gì), rồi đối chiếu 3 kế hoạch gốc | [`PLAN_GIAI_DOAN_3_SUA_LOI.md`](PLAN_GIAI_DOAN_3_SUA_LOI.md) | `BAO_CAO_GIAI_DOAN_3_SUA_LOI.md` |

### Mũi tên phụ thuộc

```
GĐ1 (N2→N3→N4) ──phát sinh──> BÁO CÁO LỖI (mục A) + HANDOFF + câu hỏi
     │                              │
     │                              ▼
     └──sinh dữ liệu đo────────> GĐ2 (lab, lỗi mới, đánh giá rủi ro)
                                            │
                                            ▼
                                   GĐ3 (plan sửa lỗi) ──> đối chiếu 3 kế hoạch gốc
                                            │
                                            ▼
                                    TV1 kiểm tra & quyết định bước tiếp
```

---

## 2. Quy tắc xử lý **phát sinh** trong Giai đoạn 1

Đây là phần cốt lõi của yêu cầu. Một phát sinh được xử lý qua **hai bước đánh giá, theo đúng thứ tự**.

### Bước 1 — Xét theo MỨC ĐỘ

| Mã | Mức độ | Đặc điểm | Cách xử lý |
|---|---|---|---|
| **S1** | Lỗi kỹ thuật nhỏ, sửa được ngay | Sai thông số, thiếu/thừa code, lỗi hình thức | **Sửa tại chỗ.** Không ghi báo cáo, không hỏi. Ghi 1 dòng vào sổ tiến độ |
| **S2** | Ảnh hưởng tới task trong khối N đang làm | Chặn 1 task, nhưng khối vẫn tiến được | **Dừng, xác định loại phát sinh (Bước 2)**, xử lý theo loại rồi tiếp tục |
| **S3** | Nghiêm trọng hơn | Chặn cả khối, ảnh hưởng thành viên khác, đổi cấu trúc/thư viện | **Dừng, xác định loại**, xử lý theo loại. Nếu chưa xử lý xong thì **báo TV1 ngay** |

> S2 và S3 đều **không** tự ý quyết khi vượt quyền; phải đi qua Bước 2.

### Bước 2 — Xét theo LOẠI

| Mã | Loại | Định nghĩa | Nơi ghi | Điều kiện |
|---|---|---|---|---|
| **T1** | **Bug** | Lỗi/error dẫn tới failure hoặc kết quả sai | [`BAO_CAO_LOI_TUAN_4_TV4.md`](../report/BAO_CAO_LOI_TUAN_4_TV4.md) mục A | **Ghi báo cáo lỗi** nếu nó **thật sự làm block task**: ảnh hưởng thành viên khác, đổi kế cấu project, đổi công cụ/thư viện |
| | | | *Sửa ngay tại chỗ* | Nếu **ít nghiêm trọng**: sai thông số, lỗi thiếu/thừa code → sửa luôn, chỉ ghi 1 dòng vào sổ |
| **T2** | **Block** | Cần quyết định, có nhiều hướng giải quyết, hoặc liên quan giữa các thành viên | [`HANDOFF_TV4_TUAN4_N1.md`](../HANDOFF_TV4_TUAN4_N1.md) | Là **quyết định của nhóm** hoặc **phối hợp giữa các thành viên** → ghi vào **handoff** |
| | | | **Câu hỏi cho TV1** | Là **lựa chọn thuộc quyền TV4** → tạo câu hỏi **tại thời điểm đã xác định được mức độ** (không để cuối tuần) |
| **T3** | **Khác** | Không thuộc T1/T2, quá đa dạng để liệt kê hết | Thử giải quyết trước | **Giải quyết được** → làm ngay, ghi sổ. **Không** giải quyết được → **tách thành file riêng**, không nhét vào báo cáo lỗi |

### Ma trận tra nhanh

| | **S1** | **S2** | **S3** |
|---|---|---|---|
| **T1 bug** | Sửa ngay | Ghi báo cáo lỗi nếu block task; sửa ngay nếu nhẹ | Ghi báo cáo lỗi + báo TV1 |
| **T2 block** | Ghi 1 dòng sổ | Handoff (nếu liên quan nhóm) hoặc câu hỏi (nếu thuộc TV4) | Handoff + báo TV1, **dừng task** |
| **T3 khác** | Sửa ngay | Sửa ngay nếu được; không được → file riêng | File riêng + báo TV1 |

### Ba nguyên tắc kèm theo

1. **Không sửa code của thành viên khác trực tiếp.** Xác nhận bằng test rồi gửi log cho TV2/TV3.
2. **Không đánh dấu xong khi chưa có bằng chứng.** Ô kỹ năng chỉ chuyển ✅ khi có
   code/config + test + kết quả thật + TV1 xác nhận và ghi ngày.
3. **Không làm tròn số.** Thiếu ô nào ghi rõ ô đó thiếu.

---

## 3. Thang đánh giá rủi ro dùng ở Giai đoạn 2

Giai đoạn 2 dùng thang này. Giai đoạn 3 **dùng lại nguyên thang** để quyết định thứ tự sửa.

| Mã | Mức | Định nghĩa | Ví dụ |
|---|---|---|---|
| **R1** | Thấp / không nghiêm trọng | Lỗi hiển thị, không mất dữ liệu, không chặn luồng | Sai ký tự tiếng Việt, lệch 1px, message thiếu dấu |
| **R2** | Trung bình | Sai hành vi nhưng có đường vòng, ảnh hưởng một phần nghiệp vụ | Cache không invalidate đúng lúc, log ghi sai mức |
| **R3** | Cao / nghiêm trọng | Chặn luồng chính, mất dữ liệu, hoặc rủi ro bảo mật | Sửa nhầm ảnh của người khác, rò khoá, mất dữ liệu khi deploy |
| **RX** | **Misc** | Lỗi UI, lỗi ký tự, logic sai nhẹ — **không nằm trên thang tuyến tính** | Lỗi font tiếng Việt, focus order, animation giật |

> Cho phép **tùy biến mức độ** khi lỗi không khớp thang, nhưng **bắt buộc** ghi kèm lý do điều chỉnh
> (ví dụ: "R3+ vì chạm NFR-SEC-005 dù chỉ ảnh hưởng 1 trang").

---

## 4. Đánh giá độ phức tạp & quyết định tách nhỏ

Tự đánh giá: khối **N2** phức tạp quá cao nếu làm một lượt.

| Khối | Số việc gốc | Phạm vi | Đánh giá | Quyết định |
|---|---|---|---|---|
| **N2** | 8 | Playwright, cổng CI frontend, E2E tấn công file, đo resilience, k6 + EXPLAIN, ngưỡng coverage, D4-UI | 🔴 **Quá cao** — trộn 3 miền khác nhau (hạ tầng test, đo đạc, UI), thất bại 1 phần chặn cả phần còn lại | **Tách 4** → N2-A, N2-B, N2-C, N2-D |
| **N3** | 4 | Bù L4 mục 2, lab L5, sổ K, mở PR | 🟠 **Cao** — L5 có nhiều phase | **Tách 3** → N3-A, N3-B, N3-C |
| **N4** | 4 | Runbook, deploy lặp lại được, cập nhật tài liệu, chốt evidence | 🟡 **Vừa** | **Tách 3** → N4-A, N4-B, N4-C để có checkpoint |

Tổng: **10 khối nhỏ** thay cho 3 khối lớn. Chi tiết trong plan GĐ1.

---

## 5. Tài liệu đầu vào và đầu ra của cả 3 giai đoạn

### Đầu vào (đã có sẵn trong repo)

| Tài liệu | Vai trò |
|---|---|
| `docs/KE_HOACH_DU_AN.md` | Task D1–D7, **K01–K24**, **NFR-001..004**, checklist |
| `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md` | Lịch 6 tuần, mốc nghiệm thu G4/G5, phân công TV4 |
| `docs/root/SRS_Culinary_Blog_v1.1.1.md` | SRS chuẩn (FR/NFR/CONS) |
| `docs/evidence/TV4/Tuan04/misc/KE_HOACH_TUAN_4_TV4.md` | Kế hoạch tuần 4 gốc (N0–N4) — **giữ nguyên, không sửa** |
| `docs/evidence/TV4/Tuan04/KE_HOACH_TUAN_4_TV4_V2.md` | Kế hoạch tuần 4 V2 (đã đối chiếu báo cáo 3 tuần + kết quả lab) |
| `docs/evidence/TV4/Tuan04/HANDOFF_TV4_TUAN4_N1.md` | Bàn giao N0/N1 — nơi ghi **block** cần nhóm quyết |
| `docs/evidence/TV4/Tuan04/report/BAO_CAO_LOI_TUAN_4_TV4.md` | **10 lỗi đã biết** — GĐ2 không cần test lại |
| `docs/evidence/TV4/Tuan04/report/BAO_CAO_LAB_TUAN4_V2.md` | Kết quả kiểm chứng thực tế đã có (BUG-01/02/03 tái hiện + sửa) |
| `docs/evidence/TV4/Tuan04/SO_EVIDENCE_TUAN_4.md` | Sổ K hiện tại — nền cho đối chiếu |
| `docs/evidence/TV4/Tuan04/TRANG_THAI_THUC_HIEN_TUAN_4.md` | Trạng thái thực hiện tuần 4 |
| `docs/evidence/TV4/MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` | Bảng mapping K01 — 24 dòng, chờ TV1 duyệt |

### Đầu ra (tạo mới)

| Giai đoạn | File bắt buộc tạo ra |
|---|---|
| GĐ1 | `BAO_CAO_GIAI_DOAN_1_N2_N4.md` · cập nhật `SO_EVIDENCE_TUAN_4.md`, `TRANG_THAI_THUC_HIEN_TUAN_4.md` · **handoff** (nếu có T2) · **báo cáo lỗi** (nếu có T1 block) · **file riêng** (nếu có T3 không giải quyết được) |
| GĐ2 | `BAO_CAO_GIAI_DOAN_2_D0_XET_LOI.md` · bổ sung mục C (mâu thuẫn tài liệu) vào **báo cáo lỗi** nếu phát hiện |
| GĐ3 | `PLAN_SUA_LOI.md` · `BAO_CAO_GIAI_DOAN_3_SUA_LOI.md` · `BAO_CAO_DOI_CHIEU_3_KE_HOACH.md` |

---

## 6. Việc cần TV1 quyết trước khi bắt đầu GĐ1

Các mục dưới đây **đang treo** và ảnh hưởng trực tiếp tới GĐ1. Không có câu trả lời thì GĐ1 vẫn chạy
được phần không liên quan, nhưng các mục bị chặn sẽ ghi "chờ quyết định" thay vì làm vội.

| # | Cần quyết | Ảnh hưởng tới | Nguồn |
|---|---|---|---|
| 1 | Duyệt **B1** (`500` → `503 storage.unavailable`) | N2-B (mã lỗi từ chối file), N4-A runbook | `HANDOFF` mục 3 #2, `docs/proposal/DE_XUAT_01` |
| 2 | Duyệt **B2** (fail-fast khi thiếu cấu hình) | N2-C, N4-A | `HANDOFF` mục 3 #3, `DE_XUAT_02` |
| 3 | Duyệt **B4** (health check xác thực credential) | Đã làm ở N1, cần xác nhận | `HANDOFF` mục 3 #4, `DE_XUAT_04` |
| 4 | **Múi giờ lịch backup 03:00** | N4-A runbook | `HANDOFF` mục 3 #5 |
| 5 | **Kho lưu backup 30 ngày** (GitHub artifact chỉ 7 ngày) | N4-A, NFR-REL-003 | `HANDOFF` mục 4 #3 |
| 6 | Tài khoản **Admin** (B6) | N2-B (test `/hangfire`), N2-C | `HANDOFF` mục 4, `DE_XUAT_06` |
| 7 | **Google OAuth credentials** (K09) | N3-A | `HANDOFF` mục 4 #4 |
| 8 | Có **TLS thật** trong tuần 4 không (NFR-SEC-005)? | N4-B | Lab đã xác nhận TLS chỉ nằm trong comment |
| 9 | Có làm **`profiles` docker-compose cho 2 API** không (NFR-SCALE-003)? | N4-B, K23 | Lab đã xác nhận compose chưa có `profiles:` |

### 6.1. Một việc cần dọn trước (kỹ thuật, thuộc TV4)

Công việc lab đợt trước **chưa được commit**: đang nằm trong `stash@{0}` trên `lab/TV4-audit-tuan4`
(9 file code/config + allowlist + 4 test hồi quy + README/CHANGELOG). Cần TV1 chọn một trong ba:

1. Commit lên `lab/TV4-audit-tuan4` để giữ làm căn cứ cho GĐ3 → **khuyến nghị**
2. Áp dụng thẳng vào `2312739_NHTSon_D5-D6-D7` trong GĐ3
3. Bỏ, vì GĐ2 sẽ dò lại từ đầu

> ⚠️ **Không** xoá `stash@{1}` — đó là stash có sẵn của GitHub Desktop, không thuộc công việc này.

---

## 7. Nguyên tắc chung cho cả 3 giai đoạn

1. **Mỗi khối nhỏ = 1 commit.** Không dồn cuối ngày. Giữ build xanh sau mỗi commit.
2. **Rebase `origin/main` trước khi mở PR**, không để conflict dồn.
3. **Mọi "đã làm" phải kèm `file:line` hoặc log.** Không có "nên làm", không có "chắc là".
4. **Không dùng số liệu của tuần trước.** Đo lại rồi ghi số thật (bài học từ mâu thuẫn 172/177/210).
5. **Không claim đạt khi chưa đo.** Đặc biệt với uptime, SLA, coverage — viết rõ giới hạn.
6. **Ghi log đã redact**, không commit secret, không commit `*.dump`.
7. **Phân biệt rõ "đã kiểm chứng ở lab" và "đã tích hợp vào nhánh chính".**

---

## 8. Điều kiện chuyển giai đoạn (exit criteria)

### GĐ1 → GĐ2

- [ ] N2-A…N2-D, N3-A…N3-C, N4-A…N4-C có kết quả ghi rõ (xong / chưa xong / chờ quyết định)
- [ ] `SO_EVIDENCE_TUAN_4.md` và `TRANG_THAI_THUC_HIEN_TUAN_4.md` đã cập nhật
- [ ] Mọi phát sinh đã phân loại T1/T2/T3 và đi đúng nơi
- [ ] Handoff đã ghi các block cần nhóm quyết
- [ ] Build sạch · `dotnet format` sạch · `dotnet test` xanh · `npm run build` xanh
- [ ] Báo cáo GĐ1 viết xong

### GĐ2 → GĐ3

- [ ] Branch lab đã tạo, không push
- [ ] Mọi lỗi **mới** đã tái hiện được và có bằng chứng RED
- [ ] Mỗi lỗi mới đã gắn mức R1/R2/R3/RX **và** NFR/K liên quan
- [ ] Rủi ro thành phần/thư viện đã dự đoán
- [ ] Mâu thuẫn tài liệu đã rà và ghi lại
- [ ] **Không** test lại 10 lỗi đã có trong báo cáo lỗi
- [ ] Báo cáo GĐ2 viết xong

### GĐ3 → Chờ TV1

- [ ] `PLAN_SUA_LOI.md` có đủ 3 cột: **sửa gì / quyết gì / ảnh hưởng gì**
- [ ] Từng lỗi có test hồi quy hoặc cách chứng minh đã hết
- [ ] `BAO_CAO_DOI_CHIEU_3_KE_HOACH.md` trả lời được: làm **đủ** gì, **thiếu** gì, **dư** gì
- [ ] Báo cáo GĐ3 viết xong
