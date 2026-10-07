# T1 — Tự deploy/restore & T6 — E2E luồng TV3

## T1. Tự deploy & restore

### Đã làm thật (trên `cb_w5_seed`, không phải checkout mới hoàn toàn — xem hạn chế)

| Bước | Lệnh | Kết quả thật |
|---|---|---|
| Migrate DB rỗng | `dotnet ef database update` (DB `cb_w5_seed` rỗng) | 8/8 migration, xem `KIEM_TRA_TUAN4.md`/`SCHEMA_RECIPE.md` |
| Seed | `dotnet run -- --seed` | 100 recipes, 24 categories, 1200 ingredients, 600 steps |
| Khởi động API | `dotnet run -- --no-auto-migrate` | API lên cổng 5080, `GET /categories` 200 |
| Backup | `pg_dump -Fc -f cb_w5_seed_backup.dump cb_w5_seed` | File dump 292.335 byte |
| Xoá DB | `DROP DATABASE cb_w5_seed; CREATE DATABASE cb_w5_seed;` | DB rỗng lại |
| Restore | `pg_restore -d cb_w5_seed cb_w5_seed_backup.dump` | Thành công, không lỗi |
| **Đối chiếu số dòng trước/sau** | `SELECT count(*)` 5 bảng | **Recipes 110/110, RecipeIngredients 1209/1209, RecipeSteps 611/611, RecipeImages 100/100, Categories 24/24 — khớp tuyệt đối trước và sau.** |

### Smoke test API thật sau khi bật lại Docker (MinIO/Redis/Mailhog)

- `POST /auth/register` → **201**
- `POST /recipes` (JSON đúng record, `difficulty` dạng số theo quy ước STRICT JSON) → **201**
- `POST /recipes/{id}/images` (multipart, ảnh JPEG thật) → **201** — xác nhận storage (RustFS/MinIO cổng 9000) hoạt động, khác với lúc Docker tắt ở Phần 0 (lúc đó `500`, lỗi `ECONNREFUSED localhost:9000`).

### Hạn chế thật — BLOCKED RAM cho phần "checkout hoàn toàn mới" (`D:\tmp\cb-clean`)

Máy chỉ còn **0,24–0,45 GB RAM trống** xuyên suốt phiên làm việc (đo nhiều lần, xem `BAO_CAO_TUAN5.md` mục nhật ký RAM). `git clone` + `dotnet restore`/`build` lại từ đầu + `npm ci` lại từ đầu cho một checkout **hoàn toàn tách biệt** (`D:\tmp\cb-clean`) cần chạy đồng thời với DB Postgres + 3 container Docker đã bật — vượt quá RAM an toàn đã quy định (0,3–0,8 GB) nếu cộng thêm một bộ build/dependency thứ hai. **Không ép chạy** theo đúng luật đã đặt ra ("Không đủ RAM thì BLOCKED RAM").

**Đã làm tương đương về bản chất** (không phải cùng một việc, ghi rõ khác biệt): toàn bộ `restore --locked-mode`, `dotnet test`, `npm ci`, `npm run build`, `--migrate`/`--seed`, khởi động API+frontend bản build, đều đã chạy **thật** trên checkout hiện có (`D:\CulinaryBlog`, nhánh `2312786_HuynhQuocTrung_C7-frontend-tests`) với DB tạm hoàn toàn mới mỗi lần (`cb_w5_seed`, không phải `culinary_blog`) — tức đã chứng minh "deploy từ mã nguồn + DB rỗng hoạt động", chỉ chưa chứng minh thêm bước "clone lại từ Git" (vốn chỉ thêm rủi ro về đường dẫn/quyền, không thêm rủi ro về code).

**Việc cần làm lại khi có RAM** (ghi vào "VIỆC CỦA TRUNG" hoặc làm ở Tuần 6): `git clone` nhánh đã push vào `D:\tmp\cb-clean`, lặp lại đúng trình tự trên.

## T6. E2E luồng quan trọng phần TV3

### Đã chạy thật (Phần 0, trước khi Docker bật)

`npx playwright test` trên bản build (`npm start`) + API `--no-auto-migrate`: **8/11 pass**. 3 fail đều ở bước **upload ảnh** (`wizard-week4.spec.ts` dòng 99, 162, 280) — nguyên nhân xác nhận trong log API: `HttpRequestException: ... (localhost:9000)` (RustFS/MinIO chưa chạy vì Docker Desktop tắt lúc đó).

### Xác nhận gốc rễ đã hết sau khi bật Docker (nhẹ hơn chạy lại toàn bộ Playwright)

Sau khi bật Docker (3 container `healthy`), **không chạy lại toàn bộ Playwright** (cần đồng thời Chromium + frontend + API — vượt RAM 0,37 GB hiện có), mà xác nhận bằng cách nhẹ hơn: chạy API một mình (không frontend/Chromium) và gọi trực tiếp đúng 3 bước mà 3 test đó làm qua `curl` — đăng ký, tạo recipe, **upload ảnh JPEG thật qua `POST /recipes/{id}/images`** → **201** (trước đó cùng bước này qua Playwright nhận `500`). Cùng nguyên nhân (storage down) cùng được xác nhận đã hết khi storage lên — tỷ lệ pass thực tế của 3 ca kia khi storage sẵn sàng **suy ra là cao** nhưng **chưa chạy lại đúng bằng Playwright** nên không tự nhận "11/11 PASS".

### 5 luồng E2E quan trọng (yêu cầu G6: "5 critical E2E tích hợp")

| # | Luồng | Trạng thái |
|---|---|---|
| 1 | Đăng ký | ✅ Có trong `create-recipe.spec.ts`/`wizard-week4.spec.ts` (mỗi test tự đăng ký), đã PASS |
| 2 | Đăng nhập | ✅ Cùng trên (dùng token vừa đăng ký) |
| 3 | Tạo công thức (wizard) | ✅ PASS (8/8 ca không liên quan ảnh) |
| 4 | Thêm nguyên liệu/bước/ảnh | ⚠️ Nguyên liệu/bước PASS; **ảnh** — xác nhận gốc rễ hết (curl 201) nhưng chưa chạy lại bằng Playwright thật (xem trên) |
| 5 | Xuất bản → xem trang công khai | ⚠️ Cùng tình trạng — ca `(g)` trong `wizard-week4.spec.ts` cần ảnh trước khi publish, nên phụ thuộc #4 |

**Kết luận trung thực**: 3/5 luồng xác nhận PASS đầy đủ bằng Playwright thật; 2/5 luồng còn lại (ảnh, publish-có-ảnh) đã xác nhận **nguyên nhân chặn đã hết** bằng bằng chứng khác (curl thật), nhưng **chưa tự nhận XONG bằng đúng công cụ Playwright** — cần chạy lại khi máy rảnh RAM hơn (ghi vào việc cần làm lại).

### Quan sát tìm kiếm TV2 (chỉ đọc, không sửa)

Chưa kiểm được công thức mới tạo có xuất hiện ở tìm kiếm TV2 hay không trong phiên này — phần này cần frontend + trang `/search` chạy cùng lúc, cùng hạn chế RAM như trên. Để lại cho lần chạy lại Playwright đầy đủ (vì `wizard-week4.spec.ts` ca `(g)` đã có bước tương tự: xuất bản rồi xem trang công khai, chỉ chưa mở rộng sang trang `/search`).
