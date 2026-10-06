# Đề xuất gỡ block — Rotate khoá ký JWT đã lộ trong git history

> **Block**: [`HANDOFF_TV4_TUAN4_N1.md`](../evidence/TV4/Tuan04/HANDOFF_TV4_TUAN4_N1.md) §4.7 · [`BAO_CAO_LOI_TUAN_4_TV4.md`](../evidence/TV4/Tuan04/report/BAO_CAO_LOI_TUAN_4_TV4.md) `BUG-W4-02`
> **Mức**: 🔴 **Cao — rủi ro bảo mật** · `NFR-SEC-007` (no secrets trong repo) · K24
> **Người lập**: Nguyễn Hữu Trung Sơn (2312739 — TV4) · **Ngày**: 03/10/2026
> **Cần ai quyết**: Nguyễn Thanh Tâm (TV1 — Nhóm trưởng) vì **xoá history là thao tác không thể hoàn tác** và ảnh hưởng mọi thành viên
> **Trạng thái**: 🔴 **CHỜ QUYẾT ĐỊNH — không tự làm.** Việc này **nằm ngoài repo**.

---

## 1. Thực trạng

| Mục | Hiện trạng |
|---|---|
| Secret còn trong file tracked? | ⛔ **Có** — `src/backend/CulinaryBlog.API/appsettings.json:3` (`ConnectionStrings:Database` có `Password=postgres`) và `:9` (`Jwt:SigningKey` 74 ký tự) |
| Đã làm gì? | Bỏ secret khỏi `render.yaml` (`sync: false`) + thêm `deploy/scan-secrets.sh` vào CI (commit `0c692d3`, N1-8) |
| Còn sót ở đâu? | ⛔ **Git history** — khoá đã bị commit trước đó thì **vẫn đọc được** bằng `git show <commit>:<file>` |
| Đã rotate chưa? | ❌ **Chưa** |

> ⚠️ **Đây là điểm quan trọng nhất:** bỏ secret khỏi file **không** làm mất secret. Ai có repo đều có
> thể lấy lại khoá cũ từ history và **tự ký token admin**.

## 2. Vì sao không tự làm

| Lý do | Giải thích |
|---|---|
| Xoá/sửa history **không hoàn tác được** | Đã push ở đâu là mất ở đó |
| Ảnh hưởng **mọi thành viên** | Ai đã clone/từng có commit cũ vẫn giữ bản sao |
| Có thể phá PR đang mở | `PR #19` của TV4, PR TV1/TV2 đang review |
| Cần biết **khoá nào đang chạy** ở môi trường thật | Chỉ Tâm/repository owner biết |

## 3. Phương án — khuyến nghị: **Rotate trước, xoá history sau**

> ⛔ **Không** bắt đầu bằng việc xoá history. Xoá history mà **chưa** rotate khoá là mất khoá cũ
> trong khi **kẻ xấu đã lấy được** ⇒ tệ hơn hiện trạng.

### Bước 1 — Rotate (bắt buộc, làm trước)

Đổi `Jwt:SigningKey` thành giá trị **sinh mới**, đặt vào secret:

| Môi trường | Cách làm | Ghi chú |
|---|---|---|
| Render | Environment variable `Jwt__SigningKey` | ✅ Nên có sẵn |
| GitHub Actions | Repository secret cho `IntegrationTests` | Nếu có test cần token |
| Máy cá nhân | `.env` (không commit) | Đã có `.env.example` từ B3 |

> ⚠️ **Hệ quả:** mọi token đang cầm sẽ hết hiệu lực ⇒ thành viên phải đăng nhập lại. Chấp nhận được.

### Bước 2 — Đổi `appsettings.json` thành placeholder

```jsonc
"Jwt": {
  // KHÔNG đặt khoá thật ở đây. Lấy từ biến môi trường Jwt__SigningKey
  "SigningKey": ""
}
```

Kèm guard khởi động: thiếu `Jwt__SigningKey` ⇒ **fail-fast** (khởi động lỗi rõ ràng, không chạy được
với khoá rỗng). Có thể tái dùng pattern validate của B2 (`MinioOptions.cs`).

### Bước 3 — Xoá secret khỏi history (tùy chọn)

Chỉ làm **sau** khi Bước 1 xong, và **cần Tâm đồng ý**.

| Công cụ | Ghi chú |
|---|---|
| `git filter-repo` | ⚠️ Xoá tất cả commit/tag/branch khác — cần cân nhắc kỹ |
| Tạo repo mới + chỉ copy nội dung hiện tại | Đơn giản hơn, nhưng **mất** toàn bộ lịch sử PR |

⇒ **TV4 khuyến nghị không xoá history** nếu đã rotate: rủi ro thấp hơn, lịch sử được giữ cho
điểm danh bài làm.

## 4. Rủi ro

| Rủi ro | Khả năng | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| ⛔ Rotate xong nhưng **quên** deploy ⇒ app không khởi động | Trung bình | Dừng dịch vụ | Bật guard fail-fast để lỗi rõ ngay; deploy kiểm tra `/health` |
| Rotate làm **hỏng CI** vì secret chưa đặt | Trung bình | Job đỏ | Đặt secret **trước**, test trên nhánh riêng trước khi merge |
| Xoá history phá **PR đang mở** | Cao nếu xoá | Mất review | Không xoá history; chỉ rotate |
| Khoá mới yếu (ngắn, lặp lại ký tự) | Thấp | Đoán được token | Sinh bằng `openssl rand -base64 48`; **không** dùng chuỗi đoán được |
| `scan-secrets.sh` báo động giả trên file mới ⇒ team tắt cổng CI | Trung bình | Mất kiểm soát | Không tắt cổng; thêm allowlist **có lý do ghi rõ** |

## 5. Test case nghiệm thu

| # | Test case | Kỳ vọng |
|---|---|---|
| 1 | `git show <commit-cũ>:src/backend/CulinaryBlog.API/appsettings.json` | ⚠️ **Vẫn thấy** khoá cũ nếu chưa xoá history — **không** dùng làm tiêu chí nghiệm thu |
| 2 | Token ký bằng khoá **cũ** gửi lên API | `401` — khoá cũ **không còn** hiệu lực |
| 3 | Token ký bằng khoá **mới** | `200`/`401` đúng tuỳ endpoint, **không** phải lỗi 500 |
| 4 | Đăng nhập lại sau khi rotate | Thành công, nhận token mới |
| 5 | Khởi động app **không** có `Jwt__SigningKey` | Fail-fast, thông báo chỉ rõ thiếu biến gì |
| 6 | `deploy/scan-secrets.sh` trên HEAD mới | exit 0; phát hiện secret nếu cố tình chèn lại |
| 7 | `git log -p` trên nhánh chính | Không có khoá thật ở **commit mới** |
| 8 | Job CI xanh sau khi đặt secret | Không job nào đỏ vì thiếu secret |

## 6. Quyết định cần chốt

1. Ai thực hiện **rotate** — Tâm (có quyền đặt secret Render) hay TV4?
2. Có **xoá history** không? (TV4 khuyến nghị **không**, chỉ rotate)
3. Có đặt `appsettings.json` thành placeholder + guard fail-fast không?
4. Nhắc nhở thành viên đăng nhập lại ở mức nào (email nhóm hay chỉ ghi handoff)?

> 📌 **TV4 không tự rotate.** Lý do: khoá đang chạy ở môi trường thật nằm ngoài repo, và việc
> rotate mà thiếu phối hợp sẽ làm dừng dịch vụ.
>
> ✅ **Phần TV4 có thể làm ngay, không cần ai quyết** (nằm trong repo):
> 1. `.env.example` **đã có** sẵn `Jwt__SigningKey=REPLACE_WITH_RANDOM_SECRET_AT_LEAST_64_BYTES` → chỉ cần
>    bổ sung **một dòng hướng dẫn sinh giá trị** (`openssl rand -base64 48`) vì hiện placeholder không
>    nói cách sinh.
> 2. Đặt `appsettings.json` thành placeholder `""` + **guard fail-fast** khi thiếu `Jwt__SigningKey`
>    (tái dùng pattern validate của B2 trong `MinioOptions.cs`).
> 3. Cập nhật `docs/HUONG_DAN_TEST_APP.md` + README: biến này **bắt buộc**, không có default dùng được.
