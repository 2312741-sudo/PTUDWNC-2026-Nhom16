# Đề xuất gỡ block B5 — Xem trước ảnh recipe **Draft** trong wizard (proxy cần Bearer, `<img>` không gửi được)

> **Block**: [`TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md`](../evidence/TV4/Tuan03/Report/TONG_HOP_BLOCK_SUA_BUG_UPLOAD_ANH.md) §2 B5
> **Mức**: 🟡 Trung bình · **Phát hiện**: [`TEST_CASE_TICH_HOP_FE_BE.md`](../evidence/TV4/Tuan03/Report/TEST_CASE_TICH_HOP_FE_BE.md) §4 (D03/D04)
> **Cần ai quyết**: nhóm + **TV3 (Huỳnh Quốc Trung — chủ sở hữu UI wizard `ImagesStep.tsx`)**
> **Đụng quyết định**: D27 / PA-2 trong `docs/IMAGE_CONTRACT.md` §5

---

## 1. Thực trạng

Đo thật (28/09/2026):

| Cách gọi | Kết quả |
|---|---|
| `GET /api/v1/resources/images/{key}` — recipe **Draft**, có header `Authorization` (chủ sở hữu) | `200` |
| `GET /api/v1/resources/images/{key}` — recipe **Draft**, khách | `403 image.forbidden` |
| `GET /api/v1/resources/images/{key}` — recipe **Published**, khách | `200` |

Nhưng wizard render bằng thẻ HTML:

```tsx
// src/frontend/src/app/dashboard/recipes/_wizard/ImagesStep.tsx:52-57
{src ? <img src={src} alt={...} /> : <div>Chưa cấu hình NEXT_PUBLIC_MEDIA_URL<br />{img.originalUrl}</div>}
```

`<img src>` **không thể** gắn header `Authorization` ⇒ mọi request ảnh đều là "khách" ⇒ ảnh của
recipe Draft **luôn 403** ⇒ UI hiện ảnh vỡ (icon broken) trong lúc đang sửa bài.

Hệ quả thực tế khi ghép FE+BE:
- Đặt `NEXT_PUBLIC_MEDIA_URL` trống ⇒ UI hiện ô *"Chưa cấu hình NEXT_PUBLIC_MEDIA_URL"* (ảnh cũng không hiện).
- Đặt `NEXT_PUBLIC_MEDIA_URL` trỏ proxy ⇒ ảnh **Published** hiện đúng, ảnh **Draft** vỡ.

⇒ Không có tổ hợp cấu hình nào cho xem trước ảnh Draft. Người dùng phải publish mới thấy ảnh.

## 2. Nguyên nhân

1. Proxy D27 thiết kế "Published → public, Draft/Archived → owner/Admin" (`Program.cs:530-538`) —
   đúng về bảo mật, nhưng giả định client gọi được bằng **cơ chế header**.
2. Thẻ `<img>` chỉ gửi cookie, không gửi header ⇒ không khớp với hợp đồng auth Bearer của API.
3. Access token của API nằm trong `localStorage` (không phải cookie) ⇒ server-side rendering /
   `<img>` không tự lấy được token.
4. Backend trả URL ảnh ở dạng **key** (không host) ⇒ FE buộc phải ghép `NEXT_PUBLIC_MEDIA_URL`
   (`recipe-editor.ts:157-161`) ⇒ mọi quyết định về nơi lấy ảnh đẩy sang FE.

## 3. Giải pháp đề xuất

### Phương án A (khuyến nghị) — URL có chữ ký ngắn hạn (presigned) cho ảnh Draft

API trả kèm `thumbnailUrl`/`mediumUrl` đã ký (hạn 5–10 phút) cho người có quyền; `<img>` gọi
được vì không cần header.

| Ưu | Nhược |
|---|---|
| Giữ đúng nguyên tắc "Draft không public" | Cần cấp quyền đọc (`GetPresignedObjectUrlAsync`) cho Minio/RustFS — hiện `IObjectStorageReader` mới chỉ có `ReadAsync` |
| `<img>` hoạt động, không sửa kiến trúc auth | URL lộ trong DOM/Network ⇒ phải ngắn hạn thật chặt (và cân nhắc `Referrer-Policy`) |
| Không cần đổi cơ chế auth của FE | Cần thêm trường/DTO hoặc thay URL ⇒ **chạm contract `IMAGE_CONTRACT` §1** |

### Phương án B — `token` truy vấn cho endpoint proxy (rủi ro thấp hơn, kém đẹp hơn)

Cho phép `GET /api/v1/resources/images/{key}?access_token=<jwt>` chỉ khi đúng recipe Draft và token
còn hạn. `<img>` gửi được vì token nằm trong URL.

| Ưu | Nhược |
|---|---|
| Ít code, không cần presigned | Token lọt vào log proxy/CDN ⇒ phải chặn log, giới hạn hạn token, rủi ro lộ token |
| Không đụng storage SDK | Nhóm có thể không muốn token trong URL (chính sách bảo mật) |

### Phương án C — dùng `fetch` + `blob:` trong wizard thay vì `<img src>`

Wizard tải ảnh bằng `fetch` (có header) rồi gán `URL.createObjectURL(blob)` vào `<img>`.

| Ưu | Nhược |
|---|---|
| **Không** đụng backend, **không** đụng contract | Chỉ sửa được wizard (TV3); trang public dùng `<img>` vẫn phải dựa vào Published (được, vì Published public) |
| Bảo mật giữ nguyên | Tốn công JS phía client; cần xử lý revoke/hủy object URL |

### Phương án D — chấp nhận: Draft không xem trước, chỉ publish mới thấy ảnh

Chỉ sửa UI để hiện rõ *"Ảnh sẽ hiển thị sau khi publish"* thay vì ảnh vỡ. Chi phí thấp nhất,
trải nghiệm kém hơn.

## 4. Rủi ro

| Rủi ro | Khả năng | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| Presigned URL lộ ⇒ ai cũng xem được ảnh Draft trong thời hạn | Trung bình | Rò rỉ ảnh nháp | Hạn ≤ 10 phút, không ghi log, `Referrer-Policy: no-referrer` |
| `?access_token=` lọt vào access log / lịch sử trình duyệt | **Cao** nếu chọn B | Rò rỉ phiên đăng nhập | Không log query string; `Cache-Control: no-store`; cân nhắc cấm log |
| Thêm trường mới vào DTO ảnh ⇒ lệch `IMAGE_CONTRACT` §1, ảnh cũ vẫn dùng được | Trung bình | Phải cập nhật tài liệu + FE | Cập nhật `IMAGE_CONTRACT.md` **cùng PR**; giữ tương thích ngược (thiếu trường ⇒ FE fallback) |
| Sửa `ImagesStep.tsx` ⇒ đụng code của TV3 | **Cao** | Tranh chấp phạm vi | Báo TV3 trước; đây là lý do block này tồn tại |
| Ảnh vỡ trong UI bị báo là bug khác | Cao | Nhận nhầm report | Đã ghi vào test case + hướng dẫn TV4 để không report nhầm |

## 5. Test case

| # | Test case | Kỳ vọng theo phương án được chọn |
|---|---|---|
| 1 | Wizard: recipe Draft có ảnh, mở bước "Ảnh" | Ảnh hiện (A/B/C) hoặc thông báo rõ ràng (D) — **không** ảnh vỡ |
| 2 | Xem ảnh recipe Draft bằng tài khoản khác, không phải chủ | Vẫn `403` khi gọi bằng `curl` không token |
| 3 | Trang public xem ảnh recipe **Published** | `200`, hiện ảnh (đã xác nhận hoạt động) |
| 4 | URL ảnh hết hạn (A) | `403`/`404`, UI báo tải lại |
| 5 | Tải lại trang nhiều lần | Không sinh object rác trong bucket |
| 6 | Mobile/slow network: 3 ảnh Draft | Không tải trùng ảnh, không treo UI |
| 7 | Recipe chuyển Draft → Published | Ảnh hiện công khai, không còn phụ thuộc token |
| 8 | Xoá ảnh trong wizard | Ảnh biến mất khỏi danh sách, không còn URL cũ chạy được (`404`) |

## 6. Chuỗi lỗi liên quan (nếu có)

```
Nguoi dung mo wizard buoc "Anh" voi recipe Draft
  └─ ImagesStep.tsx:49  src = imageSrc(img) = MEDIA + originalUrl
      └─ <img src="http://localhost:5080/api/v1/resources/images/recipes/{id}/{uuid}.png">
          └─ KHONG co header Authorization (the <img> khong gui duoc)
              └─ Program.cs:530-537  isPublished = false -> !isOwner -> throw 403 image.forbidden
                  └─ UI: anh vo (broken image) — nguoi dung tuong lai la loi upload
```

## 7. Quyết định cần chốt

1. Chọn phương án A (presigned), B (token trong query), C (fetch + blob, chỉ wizard) hay D (chấp nhận)?
2. `IMAGE_CONTRACT.md` §1 có được cập nhật để thêm trường/URL đã ký không (A) — ai chịu phần tài liệu?
3. Ai phụ trách sửa `ImagesStep.tsx` (TV3) và khi nào review?
