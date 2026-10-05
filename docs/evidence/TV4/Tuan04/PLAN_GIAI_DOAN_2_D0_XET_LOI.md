# PLAN GIAI ĐOẠN 2 — DÒ XÉT LỖI

| Mục | Nội dung |
|---|---|
| Người lập | Nguyễn Hữu Trung Sơn (TV4 · 2312739) |
| Ngày lập | 03/10/2026 |
| Điều kiện bắt đầu | GĐ1 xong **và** `BAO_CAO_GIAI_DOAN_1_N2_N4.md` đã viết |
| Trạng thái | **Kế hoạch — chưa thực thi** *(đúng thực tế 05/10)* |

> [!NOTE]
> **📌 GĐ2 vẫn CHƯA thực thi — vì điều kiện bắt đầu chưa thoả.**
>
> Điều kiện bắt đầu là "GĐ1 xong". Thực tế 05/10: **N2 7/8 xong** (còn `D1/D2/D3`), **N3-A3**
> (Zod/RHF) chưa xong, **N3-A5** (Google OAuth) bị chặn bởi thiếu credentials. GĐ1 mới đạt
> **"đủ điều kiện chặn tiếp"**, chưa phải "xong hoàn toàn" — xem
> [`BAO_CAO_GIAI_DOAN_1_N2_N4.md`](BAO_CAO_GIAI_DOAN_1_N2_N4.md).

---

## 0. Nguyên tắc cốt lõi

1. **Chỉ tìm LỖI MỚI.** 10 lỗi đã có trong `BAO_CAO_LOI_TUAN_4_TV4.md` **không cần test lại** —
   đã có bằng chứng ở `BAO_CAO_LAB_TUAN4_V2.md`. Trừ khi GĐ1 đã sửa code, khi đó chỉ cần **xác nhận
   fix còn hiệu lực**, không dò lại từ đầu.
2. **Mọi lỗi phải tái hiện được.** Không ghi lỗi chỉ vì "đọc code thấy nghi ngờ". Lỗi không tái hiện
   được ghi ở mục 7 (nghi vấn chưa xác nhận), **không** tính vào số lỗi.
3. **Lỗi phải có mức rủi ro + NFR/K liên quan** trước khi vào báo cáo.
4. **Không sửa gì ở GĐ2.** GĐ2 chỉ dò và ghi. Sửa là việc của GĐ3.

---

## 1. Chiến lược branch

| Mục | Nội dung |
|---|---|
| Branch | Tạo **branch lab local** từ `2312739_NHTSon_D5-D6-D7`, tên dạng `lab/<...>` |
| Push | ❌ **Không push.** Không mở PR |
| Mục đích | Thử nghiệm/sửa tự do mà không đụng nhánh chính |
| Vệ sinh | Mỗi nhóm thử nghiệm = 1 commit hoặc 1 stash có nhãn rõ để không lẫn |

> ⚠️ Kinh nghiệm từ đợt lab trước: **tạo branch khi working tree đang bẩn sẽ mang thay đổi đó sang
> branch mới**. Luôn `git stash` hoặc commit dứt điểm trước khi `git checkout -b`, và kiểm tra
> `git status` sau khi tạo.

---

## 2. Môi trường kiểm thử

Bắt buộc bật đủ phụ thuộc, nếu không nhiều test sẽ **thoát sớm (guard)** và tạo cảm giác "xanh giả":

```bash
docker compose -f docker-compose.dev.yml up -d postgres redis s3 mailhog seq otel-collector
```

| Thành phần | Vai trò khi dò lỗi |
|---|---|
| PostgreSQL | Test E2E/EF thật, `pg_dump`/`EXPLAIN` |
| Redis | Test cache/fallback/lock — **tắt Redis là một kịch bản dò lỗi riêng** |
| RustFS (S3) | Test upload/proxy/health credential |
| MailHog | Kiểm tra email thật |
| Seq | Kiểm tra log + TraceId |
| OTEL collector | Kiểm tra trace HTTP→DB |
| Nginx (`:8080`) | **Bắt buộc** — nhiều lỗi chỉ xuất hiện sau proxy (X-Forwarded-For, upstream) |

**Bổ sung:** dựng **2 tiến trình API** (`:5080`, `:5081`) để lộ lỗi multi-instance — đây là nơi
phát sinh lỗi mà chạy 1 instance không thấy.

---

## 3. Thang đánh giá rủi ro

### 3.1. Bốn mức

| Mã | Mức | Định nghĩa | Hậu quả nếu bỏ qua | Ví dụ |
|---|---|---|---|---|
| **R1** | Thấp | Hiển thị/ký tự/nhãn; không mất dữ liệu, không chặn luồng | Người dùng thấy giao diện lệch | Sai dấu tiếng Việt, lệch 1px, thiếu thông báo |
| **R2** | Trung bình | Sai hành vi nhưng có đường vòng; ảnh hưởng một phần nghiệp vụ | Dữ liệu lệch nhẹ, khó truy vết | Cache không invalidate đúng lúc, log sai mức |
| **R3** | Cao / nghiêm trọng | Chặn luồng chính, mất dữ liệu, hoặc rủi ro bảo mật | Mất dữ liệu / lộ thông tin / hệ thống sập | Ghi đè ảnh người khác, rò khoá, mất dữ liệu khi deploy |
| **RX** | **Misc** | Lỗi UI, lỗi ký tự, logic sai nhẹ — **không nằm trên thang tuyến tính** | Ảnh hưởng trải nghiệm, không mất dữ liệu | Focus order sai, animation giật, lỗi font |

### 3.2. Quy tắc tùy biến mức

Được phép nâng/hạ mức khi lỗi không khớp thang, **nhưng bắt buộc ghi lý do**. Ví dụ hợp lệ:

> `R2 → R3+`: lỗi hiển thị thông tin Admin cho mọi khách — mức R1 về hậu quả thị giác, nhưng chạm
> `NFR-SEC-006` nên nâng lên R3.

### 3.3. Bảng ghi

| # | Mã lỗi | Mô tả | Tái hiện | **R** | **NFR** | **K** | Bằng chứng | Lưu ý |
|---|---|---|---|---|---|---|---|---|
| | | | link/log | R1/R2/R3/RX | | | | tùy biến? vì sao |

---

## 4. Ánh xạ rủi ro vào NFR và K01–K24

### 4.1. NFR của TV4 — 9 mục bắt buộc rà

| NFR | Câu hỏi kiểm tra | Lỗi thường gặp |
|---|---|---|
| `NFR-PERF-002` | ≥100 VU thì điểm nghẽn ở đâu? | Hết connection pool p2p16; tăng VU thì p95 vượt ngưỡng |
| `NFR-SEC-004` | Validate **trước khi** buffer? SQL có parameterized? Có CSP chống XSS? | Đọc file vào bộ nhớ trước khi kiểm tra size → DoS |
| `NFR-SEC-005` | TLS ≥1.2? HSTS? CORS explicit? | `listen 80` không chuyển sang HTTPS; `AllowAnyOrigin` |
| `NFR-USE-004` | Mọi async có feedback? Optimistic có rollback? Upload có progress %? | Upload không phần trăm; optimistic không rollback khi lỗi |
| `NFR-REL-001` | Readiness 10s? Có tuyên bố SLA quá mức không? | Ghi "uptime 99,9%" từ test 5 phút |
| `NFR-REL-002` | Timeout 30s? Redis fallback? Hangfire retry? | Redis chết → 500 thay vì fallback |
| `NFR-SCALE-001` | Stateless JWT? Redis shared? Sitemap lock singleton? **2 API thật?** | Bộ đếm in-memory nhân theo số instance; job chạy trùng |
| `NFR-SCALE-003` | Nginx upstream nhiều API? Đã triển khai hay mới thiết kế? | Upstream 1 server; không `proxy_next_upstream` |
| `NFR-SEO-003` | Sitemap đủ 4 trường? cron 02:00 UTC? robots khai báo URL? | Thiếu `changefreq`/`priority`; cron lệch 2 tiếng |

> Ngoài 9 NFR này, **NFR của thành viên khác vẫn có thể bị lỗi do TV4 gây ra** (ví dụ hạ tầng ảnh
> hưởng `NFR-SEC-003` rate limit của TV1, hay `NFR-SEO-002` của TV2). Ghi rõ chủ sở hữu NFR.

### 4.2. K01–K24 — lỗi nào làm **mất ô kỹ năng đã đạt**

| Kỹ năng | Dấu hiệu báo ô này **mất** |
|---|---|
| K02, K10 | Endpoint trả sai mã lỗi, lộ stack trace, thiếu mã lỗi nghiệp vụ |
| K07 | Mất optimistic concurrency, soft delete sai, audit sai |
| K09 | Google OAuth verify sai → tài khoản liên kết nhầm |
| K12, K20 | Cache sai, trace/log mất TraceId, health báo sai |
| K13 | Upload lọt file giả MIME, hoặc xoá nhầm ảnh người khác |
| K14 | Job chạy trùng, không retry, mất job khi restart |
| K15 | Resize sai kích thước, sitemap XML sai schema |
| K16–K18 | Route lỗi 500, thiếu loading/error state, không responsive |
| K19 | Thiếu canonical/OG/JSON-LD, Draft bị index |
| K21 | Test xanh giả do guard; coverage đo sai |
| K22 | Số liệu p95/p99 không tái lập được |
| K23 | Multi-instance lỗi; backup không restore được |
| K24 | CI xanh dù code hỏng; secret lọt vào repo |

### 4.3. Rà riêng các ô đang **chờ TV1 duyệt**

Không đánh dấu ✅ ô nào chưa được duyệt, kể cả lỗi đã hết. Ghi "chờ duyệt" kèm ngày.

---

## 5. Rủi ro thành phần / thư viện — dự đoán trước

Không chờ lỗi xảy ra mới nghĩ. Rà trước các mục này, ghi "có / không / chưa rõ" kèm bằng chứng.

| Thành phần | Cần kiểm | Rủi ro cần dự đoán |
|---|---|---|
| **`ImageSharp` 3.1.x** | Đang kẹt 3.1.x | Nâng lên 4.x là build fail (license thương mại). AVIF không decode → phải fallback ảnh gốc |
| **Npgsql** | Pooling `max100`/instance | Hết pool khi tăng VU; connection không trả về pool khi exception |
| **StackExchange.Redis** | `AbortOnConnectFail`, retry | Redis chết giữa chừng → lỗi lúc runtime dù đã pass lúc khởi động |
| **Hangfire** | Storage PostgreSQL dùng chung | Nhiều worker tranh cùng job; retry lặp vô hạn |
| **Next.js 15 App Router** | Route dynamic, `revalidate` | Route dynamic không prerender → lỗi chỉ lộ lúc `next build`/chạy thật |
| **`dotnet-ef`** | Phiên bản khớp SDK | Lệch phiên bản sinh migration sai |
| **RustFS (S3)** | Bucket phải tồn tại trước khi app báo khoẻ | App báo `Healthy` rồi upload vẫn `503` |
| **Docker Desktop / WSL** | Cổng trùng với bản native | `localhost:5432` trỏ nhầm bản native → `pg_dump` sai nơi |
| **GitHub Actions runner** | Rất ít binary | ⚠️ **ĐÃ VẤP & ĐÃ SỬA** — `redis-cli` không có trên `ubuntu runner` làm job backend đỏ trong khi test xanh; commit `ee5e78b` đã đổi sang `bash /dev/tcp`. **Bài học cần nhớ khi thêm bước chờ dịch vụ mới**: cứ dùng `/dev/tcp` hoặc `docker exec` vào service container |

### 5.1. Phát sinh đã biết từ đợt lab trước — kiểm lại trong GĐ2

| # | Phát sinh | Loại cần rà |
|---|---|---|
| 1 | Rate limit **nhân theo số instance** (in-memory từng tiến trình) | `NFR-SCALE-001` |
| 2 | App **không** đọc `X-Forwarded-For` | `NFR-SEC-003`, `NFR-SEC-005` |
| 3 | `/sitemap.xml` trả **200 rỗng** khi cache rỗng + tranh lock | `NFR-SEO-003` |
| 4 | `RecipeImage.Id` thiếu `ValueGeneratedNever()` → 422 | K07 |
| 5 | Snapshot EF lệch với model | K06 |
| 6 | Scanner bỏ sót secret của dự án | K24, `NFR-SEC-007` |

---

## 6. Rà mâu thuẫn tài liệu

Mục này do TV1 chỉ định rõ. Đây là loại lỗi **không làm hỏng code** nhưng làm sai căn cứ chấm điểm —
nên thường bị bỏ qua.

### 6.1. Ba nhóm mâu thuẫn phải rà

| Nhóm | Ví dụ đã biết | Cách rà |
|---|---|---|
| **Số liệu** | Số test: `54` · `167/167` · `172/172` · `177/177` · `210/210` trong các file khác nhau | `rg "17[0-9] *[/ ] *17[0-9]\|54 Tests\|172\|177\|210\|Total:"` toàn repo; đối chiếu với `dotnet test` thật |
| **Quyết định kiến trúc** | MinIO → đổi sang **RustFS (S3)** vì lỗi version, nhưng tài liệu khác vẫn viết MinIO | `rg -i "minio|rustfs"` và đối chiếu `ADR-TV4-002` |
| **Trạng thái tài liệu** | File bị xoá nhưng vẫn được dẫn chiếu; đường dẫn sai; link tương đối hỏng | Kiểm link tương đối trong từng file; đối chiếu danh sách file với `git ls-files` |

### 6.2. Danh sách tài liệu phải rà

| File | Rà gì |
|---|---|
| `README.md` | Số test, số route, claim "24/24 K", claim SLA |
| `CHANGELOG.md` | Có mục cho tuần 2–4 không; ngày có khớp thực tế không |
| `docs/HUONG_DAN_CAI_DAT_VA_CHAY_CHUONG_TRINH.md` | Số test, tên service, cổng |
| `docs/HUONG_DAN_TEST_APP.md` | Số test ghi "54 Tests" so với thực tế |
| `docs/KE_HOACH_DU_AN.md` | Checklist có mục nào đã đạt nhưng chưa tick không |
| `docs/PHAN_CHIA_CONG_VIEC_6_TUAN.md` | Mốc G4/G5, phân công TV4 |
| `docs/root/SRS_Culinary_Blog_v1.1.1.md` | Đối chiếu với `SRS_Contradictions_Report.md` |
| `docs/evidence/TV4/Tuan04/*.md` | Số test, trạng thái ô K, ngày |
| `docs/evidence/TV4/MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` | Ô K nào có bằng chứng thật |
| `docs/adr/*` | ADR có còn đúng với code không |

### 6.3. Cách ghi

Mỗi mâu thuẫn ghi thành **một dòng** trong mục C của `BAO_CAO_LOI_TUAN_4_TV4.md`:

| # | File:line | Nội dung mâu thuẫn | Nguồn đúng | Mức R | Đề xuất |
|---|---|---|---|---|---|

> Phân biệt: mâu thuẫn **số liệu** thì tự sửa được (đo lại rồi ghi) · mâu thuẫn **quyết định kiến trúc**
> thì thuộc nhóm → handoff · mâu thuẫn **thuộc quyền TV4** → câu hỏi.

---

## 7. Khu vực dò lỗi đề xuất

Xếp theo **giá trị phát hiện / công sức**:

| # | Khu vực | Vì sao đáng dò |
|---|---|---|
| 1 | **Luồng qua Nginx** với 2 API instance | Lab đã chứng minh lỗi chỉ lộ ra ở đây |
| 2 | **Upload/tấn công file** end-to-end | `NFR-SEC-004`; chưa có E2E nào |
| 3 | **Sitemap + lock + cron** | Đã biết 1 nhánh trả XML rỗng |
| 4 | **Rate limit + auth** qua proxy | `NFR-SEC-003`; đã biết không đọc XFF |
| 5 | **EF/transaction/RowVersion** trên PostgreSQL thật | `BUG-W4-01` cho thấy test fake không bắt được |
| 6 | **Cache khi Redis chết** | `NFR-REL-002` fallback |
| 7 | **Backup/restore** | `NFR-REL-003`; drill lần trước 14 bảng |
| 8 | **CI xanh giả** (guard làm test thoát sớm) | `K21` |
| 9 | **Tài liệu** (mục 6) | Rẻ, ảnh hưởng lớn tới chấm điểm |
| 10 | **Rủi ro thư viện** (mục 5) | Rẻ, chủ động |

### 7.1. Khu vực KHÔNG cần dò

| Khu vực | Lý do |
|---|---|
| 10 lỗi đã có trong `BAO_CAO_LOI_TUAN_4_TV4.md` | Đã có bằng chứng; chỉ xác nhận fix còn hiệu lực |
| Google OAuth thật | ⛔ Thiếu credentials; chỉ kiểm được phần không cần network thật |
| Frontend trên Render | ⛔ Chưa có hạ tầng deploy production |
| Test load > 100 VU kéo dài | Chỉ đo smoke/load ngắn đủ để tìm điểm nghẽn, chưa cần stress dài |

---

## 8. Nghi vấn chưa xác nhận

Lỗi **không tái hiện được** không được đưa vào số lỗi. Ghi ở đây kèm lý do.

| # | Nghi vấn | Vì sao chưa xác nhận | Cần gì để xác nhận |
|---|---|---|---|
| | | | |

---

## 9. Đầu ra của GĐ2

1. `BAO_CAO_GIAI_DOAN_2_D0_XET_LOI.md` gồm:
   - Môi trường đã dùng
   - Bảng lỗi mới (mục 3.3) có **R + NFR + K + bằng chứng**
   - Kết quả rủi ro thư viện (mục 5)
   - Kết quả rà mâu thuẫn tài liệu (mục 6)
   - Nghi vấn chưa xác nhận (mục 8)
   - **Xếp hạng thứ tự sửa** đề xuất cho GĐ3
2. Mục C của `BAO_CAO_LOI_TUAN_4_TV4.md` bổ sung các mâu thuẫn tài liệu tìm được
3. Handoff cập nhật nếu có mâu thuẫn kiến trúc cần nhóm quyết
4. Sổ `SO_EVIDENCE_TUAN_4.md` ghi ô K nào **bị rủi ro mất**

---

## 10. Điều kiện hoàn thành GĐ2

- [ ] Branch lab tạo xong, không push
- [ ] Docker đủ phụ thuộc + 2 tiến trình API
- [ ] Mọi lỗi mới đã **tái hiện được** và có bằng chứng
- [ ] Mỗi lỗi có **R1/R2/R3/RX** + **NFR** + **K**
- [ ] Mục 5 rủi ro thư viện đã điền đủ
- [ ] Mục 6 mâu thuẫn tài liệu đã rà, kết quả đã ghi vào báo cáo lỗi mục C
- [ ] **Không** test lại 10 lỗi cũ
- [ ] Báo cáo `BAO_CAO_GIAI_DOAN_2_D0_XET_LOI.md` viết xong
