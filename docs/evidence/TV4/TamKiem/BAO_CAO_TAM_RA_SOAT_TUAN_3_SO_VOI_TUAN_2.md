# BÁO CÁO TẠM — TUẦN 3 SO VỚI TUẦN 2

> **Tài liệu tạm, KHÔNG phải báo cáo chính thức.**
> Người tạo: **TV4 — Nguyễn Hữu Trung Sơn (2312739)**.
> Mục đích: so sánh tiến bộ giữa tuần 2 và tuần 3 của **toàn nhóm**, kiểm tra xem báo cáo có sát với dự án thực tế không.
> **Có thể xóa sau khi TV4 đọc xong.** Không dùng để chấm điểm học phần.

| | |
|---|---|
| Baseline | `origin/main` = `1492b3959fdb019fbe28eb0095207df6dcbcae5c` |
| Tuần 2 | 16/09 – 22/09/2026 · 36 commit |
| Tuần 3 | 23/09 – 29/09/2026 · 79 commit |
| Ngày rà soát | 30/09/2026 |
| Báo cáo đối chiếu | `docs/evidence/TV{1,2,3,4}/**/TUAN_2*.md`, `TUAN_3*.md`, `BAO_CAO_TIEN_DO_TUAN_3*.md` |
| Kế hoạch | `docs/KE_HOACH_DU_AN.md` mục 5 (tuần 2), mục 6 (tuần 3) |

---

## 1. Kết luận nhanh

| Tiêu chí | Tuần 2 | Tuần 3 | Đánh giá |
|---|---|---|---|
| Commit | 36 | **79** | ▲ Tăng mạnh |
| Báo cáo tuần tự sự | Có đủ (TV1, TV2, TV4) | **TV3 không có `TUAN_1`/`TUAN_2`**; TV4 không có file `TUAN_2.md`/`TUAN_3.md` chuẩn, dùng cấu trúc `Tuan02/`, `Tuan03/` | ▼▼ Phân kỳ |
| Test toàn hệ thống | **120/120 + 5** (đo cuối tuần 2) | **178/178** (đo thật 30/09) | ▲ Tăng thật +58 |
| Số route backend | — | 34 | ▲ |
| Mảng bị "báo cáo hoàn thành" nhưng code không có | Category **soft-delete** (đã sửa trong tuần 3) | **Google auth, Redis, LAB, backup, E2E** | ▼▼ Nặng hơn nhiều |
| Kỹ năng kiểm chứng được | — | **~18/96** | ▼ |
| Hạ tầng test | Chỉ xUnit | Vẫn **chỉ xUnit** (0 frontend test) | ═ Không tiến |
| Quyết định nhóm đang treo | **4** (D23, D27, C07, reviewer) | **2** (B1–B6, CR-7) | ▲▲ Đã gỡ 2/4 |

> **Điểm cốt lõi:** tuần 3 có **khối lượng code tăng gấp đôi**, nhưng **độ tin cậy của báo cáo giảm**. Tuần 2 các báo cáo còn gần với thực tế; tuần 3 phát sinh hàng loạt claim cho những thứ **không tồn tại trong repo**.
>
> **Ngoại lệ đáng ghi nhận:** TV4 đã gỡ 3/4 quyết định nhóm bị treo từ tuần 2, kèm ADR đề xuất. Đây là tiến bộ **về quy trình**, không chỉ về code.

---

## 2. Phân bổ commit theo thành viên

| TV | Tuần 2 | Tuần 3 | Δ | Ghi chú |
|---|---|---|---|---|
| **TV1** Tâm | 18 | 15 | −3 | Chủ yếu merge + auth. Không có PR tuần 3 |
| **TV2** Vĩ | 5 | **1** | −4 | Tuần 3 chỉ 1 commit squash nhưng **17 file, +1857/−74** |
| **TV3** Trung | 3 | **25** | ▲▲▲ | Bùng nổ ở recipe authoring |
| **TV4** Sơn | 9 | **37** | ▲▲▲▲ | D1/D2/D4-SEO/D5 hạ tầng |
| **Tổng** | 36 | 79 | ▲ 43 | |

> **Cảnh báo:** số commit **không** phải nỗ lực. TV2 có 1 commit nhưng lớn nhất tuần 3; TV3 có 25 commit nhưng 1 NFR khai mã không tồn tại.

---

## 3. So sánh theo mảng công việc

### 3.0 Bối cảnh tuần 2 — điểm xuất phát bị bỏ qua trong bản đầu

Tài liệu cũ cho thấy cuối tuần 2, `main` **chưa có bất kỳ endpoint recipe nào** — `Program.cs` chỉ có `auth` + `categories`, không `POST/PUT/GET /recipes`, không `/ingredients`, `/steps`, không `POST /auth/refresh` (`JwtService` còn trả `RefreshToken: null`). PR #10 tên nhánh `C2-C3-recipe-api` nhưng thực chất chỉ chứa Phase 0.

> **Ý nghĩa:** tuần 3 không bắt đầu từ "mọi thứ đã xong, giờ polish". Tuần 3 bắt đầu từ một hệ thống **chưa có nghiệp vụ cốt lõi**. Đây là căn cứ để đánh giá công bằng hơn cho toàn bộ thành viên.

### 3.1 Category & Khám phá công thức (TV2)

| | Tuần 2 | Tuần 3 |
|---|---|---|
| Nội dung | Category CRUD, Admin policy, delete 409, slug | FTS `unaccent`+`pg_trgm`+GIN, search SSR, cache abstraction, lọc/sort/phân trang |
| Claim khai | 36/36 test, "hoàn thành" | "B1–B7 100%", "Redis", "FTS trigger+rank", "Google Auth.js" |
| Thực tế tuần 3 | — | B1 92% · B2 86% · B3 60% · B4 50% · B5 44% · **B6 8%** · B7 24% |
| Vấn đề mới của tuần 3 | — | `ConcurrentDictionary` gọi là Redis; `dev_google:` gọi là OAuth; test "pho"→"phở" chạy trên fake repo; **không có branch `practice/TV2`** |
| ✅ **Đã gỡ trong tuần 3** | Soft-delete vi phạm C07 (unique `Name`/`Slug` chặn tạo lại) | `CategoryRepository.DeleteAsync` dùng `db.Categories.Remove()` = **hard delete đúng C07**. Còn nợ kỹ thuật: domain giữ `MarkDeleted`/`IsDeleted` chết, tên test còn `..._and_soft_deletes_when_empty` |

**Kết luận:** tuần 2 báo cáo **gần thực tế hơn**. Tuần 3 mở rộng phạm vi thì tỷ lệ sai tăng mạnh — phần nghiệp vụ lõi vẫn tốt, nhưng phần "hạ tầng" (Redis/Google/LAB) là **khai báo không có code**. Điểm cộng: tuần 3 đã **sửa đúng** lỗi C07 còn tồn đọng từ tuần 2.

### 3.2 Recipe authoring (TV3)

| | Tuần 2 | Tuần 3 |
|---|---|---|
| Nội dung | Recipe aggregate, Ingredients/steps, wizard, **refresh rotation** | Hoàn thiện editor/dashboard/detail, Nutrition, JSON-LD, ownership/version test |
| Commit | 3 | **25** |
| Claim | "Hoàn thành tuần 2" | "C1–C7 100%", khai `NFR-DATA-001` (không tồn tại), `FR-RCP-001` + `NFR-USE-001` (thuộc TV2) |
| Thực tế tuần 3 | — | C1 ~85% · C2 ~85% · C3 ~80% · **C4 ~75%** · C5 ~65% · C6 ~20% · C7 ~35% |
| Điểm tốt bị giấu | — | JSON-LD đủ 14 trường, không bịa `aggregateRating` — **NFR-SEO-001 gần trọn vẹn, không hề khai** |

**Kết luận:** tuần 3 là **tuần thật sự bùng nổ** của TV3. Nhưng báo cáo vừa gán nhầm mã người khác vừa bỏ trống toàn bộ 9 NFR thuộc chính mình.

### 3.3 Hạ tầng & Media (TV4)

| | Tuần 2 | Tuần 3 |
|---|---|---|
| Nội dung | Storage/MinIO, upload, resize job, D1/D2/D3 | Hoàn thiện D1–D4, dựng compose 6 service + nginx, health/OTEL, SEO sitemap/robots, k6, lab L4 |
| Commit | 9 | **37** |
| Claim | "Hoàn thành" | `SO_EVIDENCE_TUAN_3.md` đánh dấu ✅ **15 ô kỹ năng**; bản SRS tự khai **16/24** |
| Thực tế tuần 3 | — | **D1 ✅, D2 ✅**, D3 backend ✅/UI ❌, D4 SEO ✅, **D5 ~40%**, D6 lab 39/39 nhưng **chưa có PR**, **D7 chưa làm** |
| Vấn đề mới | — | "trace thật qua Seq" **sai** (không có sink, không collector); k6 20 VU vs yêu cầu ≥100; không p99 |
| ✅ **Đã gỡ trong tuần 3** | D23 queue chặn D2 cả tuần; D27 bucket chặn D4-UI (lỗi 403 ảnh Draft) | Đã chốt **PA-1 Hangfire** + **PA-2 proxy có auth**, có ADR `ADR-TV4-002` |

**Kết luận:** tuần 3 TV4 tăng mạnh về hạ tầng và có **số đo thật** (k6 3310 request, EXPLAIN 0.339ms, lab 39/39). Nhưng bảng evidence lại đánh dấu ✅ nhiều hơn thực tế.

> **Điểm cần sửa trong bản đầu của báo cáo này:** bản đầu ghi "sổ trung thực của chính TV4 (`Tuan04/...:19` → '≈10%')". Thực tế **`Tuan04` là tài liệu tuần 4 của TV2, không phải sổ của TV4**. Sổ trung thực thật sự của TV4 là bản SRS với các % rất thấp tự khai: `NFR-USE-004` 15%, `NFR-REL-003` 10%, `NFR-SEC-005` 20%. Dựa vào đó, mức hoàn thành thật của TV4 vào khoảng **55–60% theo mã yêu cầu**, không phải mức thấp như bản đầu ngụ ý.

### 3.4 Auth / Identity / CI (TV1)

| | Tuần 2 | Tuần 3 |
|---|---|---|
| Nội dung | JWT/PBKDF2 pipeline, register/login, lockout, CI backend | Refresh token rotation (**vượt phạm vi của TV3**), cache, JSON-LD, `/auth/me` |
| Commit | 18 (gồm merge) | 15 |
| Claim | "Hoàn thành" | Khai PR #11, #12 — **nhưng #11 là của TV2, #12 là của TV4**; PR #1 và #4 **không tồn tại** |
| Thực tế tuần 3 | — | Rotation ✅ nhưng **không có `FamilyId`**; **không có PR nào** cho tuần 3–4; 5 NFR chủ trì bị bỏ trống; secret nằm trong `appsettings.json` + `render.yaml` |

**Kết luận:** tuần 3 TV1 **làm thật** (rotation, `/auth/me`) nhưng **ghi sai số PR** và **không khai 1/6 NFR nào**. Đây là dạng lệch **thiếu khai + sai lịch sử**, nguy hiểm vì reviewer dựa vào PR list sẽ không tìm thấy bằng chứng.

---

## 4. Tuần 3 đã làm được gì THẬT (cần ghi nhận)

Không nên vì các lỗi ở trên mà phủ nhận phần tiến bộ thật:

| # | Hạng mục | Bằng chứng |
|---|---|---|
| 1 | **Test tăng vượt bận** — chạy thật **178/178 PASS**, 0 failed, 0 skipped (173 + 5 spike) | `dotnet test` ngày 30/09 |
| 2 | **D1 Storage/MinIO hoàn thành** — magic bytes 4 định dạng, giới hạn 5 MiB, endpoint upload/delete/set-primary, có test E2E | `ImageUpload.cs:4-26`; `MinioE2ETests.cs` |
| 3 | **D2 Resize job hoàn thành** — Hangfire `WorkerCount=4`, queue PostgreSQL, **4/4 test** | `Program.cs:142-147`; `ImageResizeD2Tests.cs` |
| 4 | **Hạ tầng 6 service** — postgres 14, redis 31, RustFS 41, mailhog 63, seq 71, nginx 86 + 4 volume; 4 health check; OTEL tracing + metrics | `docker-compose.dev.yml`; `Program.cs:149-167`, `:561-563` |
| 5 | **SEO**: sitemap, robots, canonical, OpenGraph, JSON-LD đủ 14 trường | `sitemap.ts`; `robots.ts`; `recipes/[slug]/page.tsx:34,44,45`; `recipe-jsonld.ts:28-49` |
| 6 | **Số đo k6 thật** — 3310 request, 0% fail, p95 225.63ms | `logs/k6_smoke_summary.json` |
| 7 | **Sửa false positive search** | `c0ff83d` (TV2) |
| 8 | **Fix ảnh recipe detail + search bar** | `1492b39` (TV2) |
| 9 | **Fix SSR search** (tách `SearchFilterSelect` ra client component) | `e523579` (TV2) |
| 10 | **Tài liệu cài đặt +360 dòng** | `424013a` (TV2) |
| 11 | **Lab L4 39/39 check PASS** trên branch riêng | `origin/practice/TV4/L4`; `SOK_LAB_L4.md` |
| 12 | **Vá CVE react-server-components** cho cả nhóm (thực ra là tuần 2) | PR #6 `bf1a1ed` |
| 13 | **PR được merge thật**: #16 (27/09), #18 (29/09), #19 (29/09) | `607ee14`, `5504a8a`, `208b7f7` |

### 4.1 8 lỗi hạ tầng đã gỡ — đóng góp cho **cả nhóm**, không chỉ cho TV4

Phần này chỉ có trong tài liệu cũ và là bằng chứng mạnh nhất cho tiến bộ thật của tuần 3:

| Lỗi | Ảnh hưởng ai | Cách sửa | Trạng thái |
|---|---|---|---|
| `JwtService` khai `RoleClaimType` sai → 403 mọi request có role | **Cả 4 người** | Sửa map claim | ✅ |
| MinIO image biến mất khỏi registry sau `prune` | **Toàn bộ D1** | Ghim digest + `restart: unless-stopped` | ✅ |
| `appsettings` thiếu section MinIO → `KeyNotFoundException` lúc boot | **Cả nhóm** | Bổ sung cấu hình + kiểm tra startup | ✅ |
| Lệch mật khẩu PostgreSQL giữa compose và app | **Cả nhóm** | Đồng bộ qua biến môi trường | ✅ |
| MinIO SDK fire-and-forget → ảnh mất | **TV3, TV4** | Chuyển sang `await` thật | ✅ |
| EF đánh dấu `Modified` cho entity con mới → lỗi concurrency | **TV3** | Chỉ đánh dấu entity thực sự thay đổi | ✅ |
| Test `Concurrent_primary_setters_never_produce_two_primaries` **flaky 1/4 lượt** | **Toàn bộ CI** | Sửa race tại `RecipeImagePrimaryConcurrencyTests.cs:56` | ✅ Không tái hiện |
| Nhánh TV4 lệch `main` **23 commit** | **Cả nhóm** | Rebase + báo cáo delta | ✅ |

> **Đánh giá:** 5 lỗi đầu là lỗi **hạ tầng dùng chung** — nếu người khác gặp, cả nhóm đều không test được. Giá trị của việc gỡ chúng **lớn hơn nhiều** so với việc đánh dấu D5/D6/D7 "hoàn thành" trên giấy.

---

## 5. Những gì tuần 3 KHÔNG đạt so với kế hoạch

| Kế hoạch tuần 3 (`KE_HOACH_DU_AN.md` mục 6) | Thực tế |
|---|---|
| LAB đầy đủ cho mỗi người | ❌ TV2 **không có branch lab**; TV3 lab chỉ ~20%; TV4 lab thiếu FTS/Google/refresh/forms; **TV1 không có PR cho tuần 3** |
| 5 Playwright E2E flow | ❌ **0 config, 0 spec, 0 test frontend** |
| Coverage ≥80% Application | ❌ CI có thu thập nhưng **không artifact, không ngưỡng** |
| Backup/restore + restore drill | ❌ **Không tồn tại** trong repo |
| Load test ≥100 concurrent (NFR-PERF-002) | ❌ Mới đo **20 VU** |
| p99 ≤1000ms (NFR-PERF-001) | ❌ k6 **không đo p99** |
| a11y WCAG 2.1 AA có kiểm thử NVDA/VoiceOver | ❌ Không có số đo; `search/page.tsx` 0 thuộc tính `aria-*` |
| HSTS / HTTPS redirect (NFR-SEC-005) | ❌ Không có `UseHsts`/`UseHttpsRedirection` |
| Secret scan CI (NFR-SEC-007) | ❌ Không có; `render.yaml:19-22` lộ key plaintext |
| Rate limit sliding + proxy-safe (NFR-SEC-003) | ❌ Fixed window, không `UseForwardedHeaders` → hỏng sau Nginx |
| Deploy staging / tự deploy (K23) | ❌ Không workflow deploy |
| PR được review (K24) | ❌ TV1 tuần 3–4 không có PR; TV2 lab không có PR; TV4 lab không có PR |

---

## 6. Bảng delta tổng hợp

| Hạng mục | Tuần 2 | Tuần 3 | Δ |
|---|---|---|---|
| Commit | 36 | 79 | **+43** |
| Test thực tế (đo) | **120 + 5** | **173 + 5 = 178** | **+53** |
| Route backend | ~20 (chỉ auth + categories) | 34 | ▲▲ |
| Service compose | postgres + redis | 6 + 4 volume | ▲▲ |
| NFR đạt trọn vẹn (chỉ số của audit) | — | **~7/30** | — |
| Kỹ năng kiểm chứng được | — | **~18/96** | — |
| Quyết định nhóm đã chốt | 0/4 | **3/5** | ▲▲ |
| Lỗi hạ tầng dùng chung đã gỡ | — | **8** | ▲▲ |
| Mảng khai sai trong báo cáo | ~3 | **~12** | ▼▼▼ |
| Mâu thuẫn nội bộ | 0 | **3** (tỷ lệ tuần 4, số ô kỹ năng, tên nhánh) | ▼ |
| Bằng chứng thực thi kèm theo | ít | **nhiều hơn** (k6 log, EXPLAIN log, lab SOK, hangfire log) | ▲ |

### 3 mâu thuẫn thật (không tính khác biệt thời điểm)

| # | Mâu thuẫn |
|---|---|
| 1 | `TUAN_4.md:8` "Hoàn thành 100% (177/177)" vs `Tuan04/TRANG_THAI_THUC_HIEN_TUAN_4.md:19` "29 việc — 3 xong — **≈10%**" — cùng một tuần, hai tỷ lệ không thể cùng đúng |
| 2 | `SO_EVIDENCE_TUAN_3.md:17-35` đánh ✅ **15 ô** (và ✅ K20 trace Seq, ✅ K22 k6) vs bản SRS tự ghi K20 "**còn thiếu trace thật vào Seq**" → mâu thuẫn giữa hai file cùng tác giả |
| 3 | `TUAN_4.md:7` ghi nhánh `2312739_NHTSon_D3-D4-D5-D6` nhưng nhánh tuần 4 thật là `2312739_NHTSon_D5-D6-D7` |

> **Không tính là mâu thuẫn:** chuỗi số test 77 → 120 → 157 → 172 → 178. Đây là **5 thời điểm đo khác nhau**, đều đúng. Bản đầu của báo cáo này đã nhầm chúng thành mâu thuẫn — đã sửa. Số duy nhất sai là **177** (không tồn tại bộ test tương ứng).
>
> **Ghi nhận:** TV4 không có PR review chéo nên các mâu thuẫn 1–3 đều **lọt qua mà không ai bắt**. Một quy tắc bắt buộc: **không tự sửa số liệu trên `main` mà không mở PR**.

---

## 7. Nhận định

1. **Tuần 3 là tuần hiệu quả nhất về khối lượng code** của cả nhóm: 79 commit, +53 test thật, hạ tầng 6 service, 3 PR được merge, D1/D2 hoàn thành, **8 lỗi hạ tầng dùng chung được gỡ**.
2. **Tuần 3 cũng là tuần báo cáo sai lệch nặng nhất**: 3 mảng lớn (Google OAuth, Redis cache, LAB) được khai hoàn thành nhưng không có code; 1 mã NFR không tồn tại; 2 mã thuộc người khác bị gán nhầm; 0/6 NFR của TV1 và 0/9 NFR của TV3 được nhắc đến.
3. **Xu hướng xấu là "claim không kèm bằng chứng chạy được"** — Redis không có client, Seq không có sink, k6 không đúng ngưỡng, backup không tồn tại, E2E không có file. Điều này nguy hiểm hơn việc "chưa làm", vì nó khiến người đánh giá tin rằng đã xong.
4. **Điểm sáng:** TV3 tăng vọng về authoring, TV4 có số đo thật + gỡ 2/4 quyết định nhóm bị treo + sửa 8 lỗi hạ tầng chung, TV1 có 7 merge duy trì `main` + vá CVE, TV2 có nền category nghiệp vụ tốt + sửa đúng lỗi C07 còn tồn từ tuần 2.
5. **Ưu tiên trước khi sang tuần 4/đóng G4:** sửa số liệu sai (tỷ lệ, PR), gỡ secret, sửa rate limit, đánh dấu lại K20/K22, tạo PR cho các branch lab đang treo, và thống nhất **một sổ kỹ năng** cho cả nhóm.

### 7.1 Đính chính so với bản đầu của báo cáo này

Sau khi đọc 5 tài liệu cũ, ba kết luận của bản đầu bị sửa:

| Kết luận cũ | Đính chính |
|---|---|
| "Số test 157 → 167 → 172 → 177 → 178 là mâu thuẫn" | **Sai.** Đây là 5 thời điểm đo, đều đúng. Chỉ `177` sai |
| "TV4 có sổ trung thực ghi ≈10%" | **Sai.** `Tuan04/...` là tài liệu tuần 4 của **TV2**. Sổ thật của TV4 là bản SRS với % rất thấp tự khai → tiến bộ thật **~55–60%** |
| "Tuần 2 không có dữ liệu test" | Có: **120 + 5 = 125** (đo cuối tuần 2). Cho phép tính Δ chính xác là **+53** |

> **Bài học:** cả ba lỗi đều do kết luận khi **thiếu chiều thời gian** và **thiếu nguồn đối chiếu**. Tài liệu cũ là nguồn đối chiếu duy nhất có dữ liệu tuần 1–2.

---

## 8. Ghi chú cuối

- Rà soát này **không sửa file nguồn nào**. Chỉ tạo file báo cáo tạm trong `docs/evidence/TV4/TamKiem/`.
- **Nguồn đối chiếu:** 5 tài liệu cũ trong `docs/evidence/TV4/Temp/` đã được đọc, khai thác và **xóa khỏi project** theo kế hoạch. Nội dung có giá trị đã được hợp nhất vào đây (mục 3.0, 3.3, 4.1, 6, 7.1).
- **Noted:** TV4 luôn giữ backup riêng của các file này ngoài project, nên xóa trong repo không mất dữ liệu gốc.
- Chi tiết đầy đủ theo từng thành viên nằm ở `BAO_CAO_TAM_RA_SOAT_TONG_HOP_3_TUAN.md` cùng thư mục.
- Mọi kết luận kèm `file:line` để tự kiểm chứng. Mục chưa kiểm chứng được ghi rõ `CHƯA XÁC MINH ĐƯỢC`.
- **Có thể xóa sau khi TV4 đọc xong.**
