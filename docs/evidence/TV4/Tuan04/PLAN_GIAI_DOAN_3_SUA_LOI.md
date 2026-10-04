# PLAN GIAI ĐOẠN 3 — SỬA LỖI

| Mục | Nội dung |
|---|---|
| Người lập | Nguyễn Hữu Trung Sơn (TV4 · 2312739) |
| Ngày lập | 03/10/2026 |
| Điều kiện bắt đầu | GĐ2 xong **và** `BAO_CAO_GIAI_DOAN_2_D0_XET_LOI.md` đã viết |
| Trạng thái | **Kế hoạch — chưa thực thi** |

---

## 0. Nguyên tắc cốt lõi

1. **Không sửa lỗi không có plan.** Mọi lỗi đưa vào sửa đều phải có dòng trong `PLAN_SUA_LOI.md`.
2. **Mỗi lỗi sửa phải có cách chứng minh đã hết** — test hồi quy, hoặc bằng chứng log trước/sau.
3. **Xếp thứ tự theo rủi ro**, không theo thứ tự phát hiện: `R3` → `R2` → `RX` → `R1`.
4. **Lỗi thuộc quyết định nhóm thì không tự sửa.** Ghi vào handoff, chờ TV1.
5. **Sửa xong phải chạy lại toàn bộ kiểm định** — không chỉ chạy test của lỗi vừa sửa.
6. **Không sửa lỗi của thành viên khác trực tiếp.** Xác nhận rồi gửi log.

---

## 1. Ba nguồn lỗi đưa vào GĐ3

| # | Nguồn | Tài liệu | Trạng thái |
|---|---|---|---|
| 1 | **Lỗi có từ trước** | `BAO_CAO_LOI_TUAN_4_TV4.md` (BUG-W4-01…10) | 01/02/03 đã sửa ở lab; 04/05/06/07/09/10 còn mở |
| 2 | **Lỗi mới từ GĐ2** | `BAO_CAO_GIAI_DOAN_2_D0_XET_LOI.md` | Chưa có — chờ GĐ2 |
| 3 | **Phát sinh từ GĐ1** | Sổ phát sinh trong `BAO_CAO_GIAI_DOAN_1_N2_N4.md` + handoff | Chưa có — chờ GĐ1 |

> GĐ3 **không** tự thêm lỗi mới. Lỗi mới phát hiện khi đang sửa → ghi vào báo cáo lỗi và quyết định
> sửa tiếp hay để GĐ2 kỳ sau, nói rõ trong báo cáo.

---

## 2. Cấu trúc file `PLAN_SUA_LOI.md`

File này là **đầu ra chính** của GĐ3. Bắt buộc có **3 cột** bắt buộc: **sửa gì / quyết gì / ảnh hưởng gì**.

### 2.1. Khung file

```markdown
# PLAN SỬA LỖI

## 0. Thứ tự ưu tiên (theo mức rủi ro)
| Hạng | Mã lỗi | R | Quyết định cần | Ước lượng |

## 1. Bảng tổng hợp — 3 cột bắt buộc
| Mã lỗi | R | **Sửa gì** | **Quyết gì** | **Ảnh hưởng gì** | Chủ sở hữu | Trạng thái |

## 2. Chi tiết từng lỗi
### 2.x — Mã lỗi — <tên>
- **Triệu chứng**: ...
- **Tái hiện**: lệnh + kết quả
- **Nguyên nhân**: file:line
- **Phương án chọn**: ... / phương án bị loại: ... (vì sao)
- **Thay đổi**: file + nội dung
- **Cách chứng minh đã hết**: test hồi quy hoặc log trước/sau
- **Rủi ro hồi quy**: ...
- **Lùi lại được không**: ...

## 3. Lỗi chờ quyết định (không tự sửa)
## 4. Lỗi tạm hoãn + lý do
## 5. Lỗi phát hiện mới trong lúc sửa
```

### 2.2. Bảng 3 cột — giải thích từng cột

| Cột | Bắt buộc trả lời |
|---|---|
| **Sửa gì** | Sửa ở file nào, dòng nào, đổi gì. Nếu là **tài liệu** thì ghi rõ file + mục |
| **Quyết gì** | Có cần quyết định chọn hướng không? Nếu **có** → đưa vào handoff hoặc hỏi TV1. Nếu **không** → ghi "không cần quyết, làm theo NFR/K đã chốt" |
| **Ảnh hưởng gì** | Ảnh hưởng tới: ô K nào · NFR nào · test nào · tài liệu nào · thành viên nào · hạ tầng nào |

> Cột **Quyết gì** là cột dễ bị bỏ sót nhất. Việc "chỉ sửa cho xong" mà không chốt hướng là nguyên nhân
> phổ biến của lần sửa sau phá lần sửa trước.

---

## 3. Thứ tự sửa đề xuất

Theo mức rủi ro đã đánh giá ở GĐ2. Những lỗi **không** còn nghi ngờ thì bỏ qua dòng này.

| Hạng | Nhóm lỗi | Mức | Lý do ưu tiên |
|---|---|---|---|
| 1 | Bảo mật: lộ khoá, sửa nhầm dữ liệu người khác, thiếu TLS, rate limit vô hiệu | `R3` | Rủi ro lớn nhất, ảnh hưởng trực tiếp người dùng |
| 2 | Mất dữ liệu / hỏng luồng chính (`422`, migration, backup không restore được) | `R3` | Khôi phục khó, chậm càng nặng |
| 3 | Sai hành vi ảnh hưởng nghiệp vụ (cache, sitemap, job trùng) | `R2` | Có đường vòng nhưng làm rối dữ liệu đọc |
| 4 | Lỗi UI/ký tự/logic nhẹ | `RX` | Rẻ, sửa nhanh được |
| 5 | Ghi chú/sai thông số | `R1` | Sửa luôn khi đụng vào, không lập task riêng |

> **Lưu ý về BUG-W4-01/02/03:** đã sửa và kiểm chứng ở lab. Ở GĐ3 chỉ **xác nhận fix còn hiệu lực**
> rồi đưa vào nhánh chính, hoặc nếu GĐ1 đã sửa thì bỏ. **Không** sửa lại lần nữa.

---

## 4. Quy trình cho một lỗi

```
1. Đọc lại dòng trong PLAN_SUA_LOI.md  → xác nhận "sửa gì / quyết gì / ảnh hưởng gì"
2. Viết hoặc chạy test tái hiện lỗi  → phải FAIL trước khi sửa
3. Sửa tối thiểu, đúng nguyên nhân gốc (không vá bề mặt)
4. Chạy lại test đó               → phải PASS
5. Chạy TOÀN BỘ kiểm định:
     dotnet build -c Release            (0 warning, 0 error)
     dotnet format --verify-no-changes  (exit 0)
     dotnet test -c Release             (xanh, ghi đúng số test)
     npx tsc --noEmit && npm run build  (0 lỗi)
     deploy/scan-secrets.sh             (exit 0)
     docker exec nginx nginx -t         (hợp lệ, nếu có sửa nginx)
6. Cập nhật trạng thái trong PLAN_SUA_LOI.md
7. Commit 1 lỗi = 1 commit, message nêu mã lỗi
8. Ghi vào báo cáo GĐ3
```

> Bước 2 là bắt buộc. Sửa không có test tái hiện trước thì không biết đã hết lỗi hay chỉ đổi triệu chứng.

---

## 5. Đối chiếu với 3 kế hoạch gốc — bắt buộc làm sau khi xong plan

Sau khi `PLAN_SUA_LOI.md` hoàn tất, **soạn `BAO_CAO_DOI_CHIEU_3_KE_HOACH.md`** trả lời 3 câu hỏi:
làm **đủ** gì, **thiếu** gì, làm **dư** gì.

### 5.1. Ba kế hoạch đối chiếu

| Kế hoạch | Đối chiếu cái gì | Nguồn |
|---|---|---|
| **Kế hoạch tuần 4** | Từng task N0–N4 và từng mục checklist G4/G5 | `KE_HOACH_TUAN_4_TV4.md` mục 3, 6 |
| **Kế hoạch dự án** | Task D1–D7, **K01–K24**, **NFR**, checklist tổng | `docs/KE_HOACH_DU_AN.md` mục 8, 9 |
| **Kế hoạch 6 tuần** | Phần TV4 trong lịch 6 tuần, mốc nghiệm thu | `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md` mục 3.4, 6 |

### 5.2. Khung bảng đối chiếu

**A. ĐÃ ĐỦ**

| Mã | Việc | Bằng chứng | Còn thiếu gì không |
|---|---|---|---|

**B. CÒN THIẾU**

| Mã | Việc còn thiếu | Vì sao thiếu | Chặn ô K / NFR nào | Kế hoạch nào yêu cầu | Có thể làm tiếp không |
|---|---|---|---|---|---|

**C. LÀM DƯ / LỆCH PHẠM VI**

| Mã | Việc đã làm | Kế hoạch nào **không** yêu cầu | Nên giữ hay bỏ | Lý do |
|---|---|---|---|---|

> Mục C là mục hay bị bỏ qua. Làm thêm không phải lỗi, nhưng phải **nói ra** để reviewer biết
> phần nào vượt yêu cầu và phần nào vô nghĩa.

### 5.3. Đối chiếu riêng theo K01–K24

| K | Yêu cầu trong kế hoạch dự án | Kế hoạch tuần 4 có giao không | Trạng thái thật | Đủ / thiếu / dư | Ô nào đang chờ TV1 duyệt |
|---|---|---|---|---|---|

### 5.4. Đối chiếu riêng theo NFR của TV4

| NFR | Ngưỡng yêu cầu | Số đo thật | Đạt / chưa đạt / chưa đo | Hạn chế cần ghi rõ |
|---|---|---|---|---|

> Quy tắc của `NFR-REL-001`: **không tuyên bố đạt uptime/SLA bằng test ngắn.** Nếu chỉ đo được
> 5 phút thì ghi "đo 5 phút, chưa đủ chứng minh 99,5%".

---

## 6. Những lỗi **không** sửa trong GĐ3

Ghi rõ lý do để không ai tưởng là bỏ sót:

| Lỗi | Lý do không sửa |
|---|---|
| Google OAuth thật | ⛔ Thiếu credentials — chờ nhóm |
| Uptime thật 99,5% | ⛔ Cần vận hành dài hạn, không sửa được bằng code |
| Lỗi của thành viên khác | Xác nhận rồi gửi log, không sửa trực tiếp |
| Xoá code soft delete của `Recipe` | ❌ Đã xác nhận **đang dùng trong production** — chỉ sửa tên test + ADR |
| Migration no-op cho snapshot | ❌ **Không có tác dụng lên database**; chỉ cập nhật file snapshot |
| Cảnh báo CI nói số test cũ | Sửa trong N4-C (mục tài liệu), không phải lỗi logic |

---

## 7. Rủi ro khi sửa

| Rủi ro | Dấu hiệu | Cách phòng |
|---|---|---|
| Vá bề mặt, lỗi quay lại ở dạng khác | Test đổi xanh nhưng nguyên nhân gốc còn | Luôn hỏi "vì sao lỗi này xảy ra", sửa nguyên nhân |
| Sửa phá ô K đã đạt | Test mới xanh nhưng test cũ đỏ | Chạy **toàn bộ** bộ test, không chạy lọt |
| Công việc lab lẫn sang nhánh chính | `git status` có file lạ trước khi sửa | Kiểm tra branch + `git status` trước mỗi task |
| Sửa 20 lỗi thành 1 commit khổng lồ | Không review được, revert mất trắng | 1 lỗi = 1 commit |
| Báo "đã xong" nhưng thực tế chỉ đổi config môi trường | Chạy lại từ môi trường sạch | Bằng chứng phải tái lập được từ checkout sạch |
| Xoá nhầm stash của người khác | `stash@{1}` là stash GitHub Desktop | ⛔ Không đụng `stash@{1}` |

---

## 8. Đầu ra của GĐ3

| # | File | Nội dung |
|---|---|---|
| 1 | `PLAN_SUA_LOI.md` | Plan sửa lỗi đầy đủ 3 cột + chi tiết từng lỗi |
| 2 | `BAO_CAO_DOI_CHIEU_3_KE_HOACH.md` | Đủ / thiếu / dư so với tuần 4 + dự án + 6 tuần |
| 3 | `BAO_CAO_GIAI_DOAN_3_SUA_LOI.md` | Báo cáo giai đoạn: đã sửa gì, còn lại gì, vì sao |
| 4 | Cập nhật `BAO_CAO_LOI_TUAN_4_TV4.md` | Trạng thái từng lỗi: đã sửa / chờ quyết / tạm hoãn |
| 5 | Cập nhật `SO_EVIDENCE_TUAN_4.md` | Ô K nào có bằng chứng mới sau khi sửa |
| 6 | Cập nhật handoff | Block nào vẫn đang chờ |

---

## 9. Điều kiện hoàn thành GĐ3

- [ ] `PLAN_SUA_LOI.md` có đủ cột **sửa gì / quyết gì / ảnh hưởng gì** cho mọi lỗi
- [ ] Mỗi lỗi đã có test tái hiện FAIL trước → PASS sau
- [ ] Toàn bộ kiểm định xanh sau lần sửa cuối
- [ ] Lỗi chờ quyết định đã ghi handoff, không tự sửa
- [ ] `BAO_CAO_DOI_CHIEU_3_KE_HOACH.md` trả lời được đủ / thiếu / dư cho cả 3 kế hoạch
- [ ] Có bảng đối chiếu riêng cho K01–K24 và NFR của TV4
- [ ] Báo cáo GĐ3 viết xong
- [ ] **Dừng lại chờ TV1 kiểm tra và quyết định bước tiếp theo**
