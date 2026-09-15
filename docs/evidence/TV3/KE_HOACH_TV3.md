# KẾ HOẠCH THỰC THI & NGHIỆM THU — TV3 Huỳnh Quốc Trung (2312786)

> Phần nghiệp vụ: **Soạn thảo công thức, nguyên liệu, bước làm** (tasks C1–C7).
> Mục tiêu tài liệu: sắp xếp công việc theo phụ thuộc + **chuẩn bị sẵn bằng chứng** để nghiệm thu diễn ra trơn tru.
> Reviewer nghiệm thu toàn bộ PR: Nguyễn Thanh Tâm (Nhóm trưởng).

## 0. Nguyên tắc "nghiệm thu dễ" (đọc trước)

Mỗi task chỉ được coi là xong khi **Definition of Done (mục 12.3 đề)** thỏa mãn. Để nghiệm thu nhanh, TV3 tuân thủ:

1. **Traceability 3 chiều:** mọi commit/PR ghi rõ `FR-…`, `NFR-…`, `Kxx`, `Dxx (ADR)` liên quan. Người review đọc tiêu đề PR là biết map vào ô nào.
2. **Evidence-first:** với mỗi tiêu chí "Nghiệm thu" trong đề, có sẵn 1 artifact tương ứng (test name / EXPLAIN / ảnh / log / coverage). Xem "Sổ evidence" mục 8.
3. **Test đi kèm code trong cùng PR:** không tách "code giờ, test sau". Mỗi route có ≥1 happy + ≥1 error (mục 12.2).
4. **ADR chốt trước khi code phần tranh chấp:** D02/D05/D12/D14/D15/D16/D18/D19/D28 (mục 4).
5. **PR nhỏ theo use case** (`feat/TV3-<task>`), lab riêng (`practice/TV3/Lx`) — không trộn.
6. **Không tự nhận phần người khác:** refresh (C5) là của TV3, nhưng register/login/Google/logout/media/publish là SP của TV1/TV2/TV4; TV3 chỉ LAB.

## 1. Phạm vi TV3 — bản đồ nhanh

| Nhóm | Chi tiết |
|---|---|
| **FR chủ trì** | RCP-002 (GET /recipes/{slug}), RCP-003 (POST /recipes), RCP-004 (PUT /recipes/{id}), RCP-009 (ingredients CRUD), RCP-010 (steps CRUD), AUTH-004 (POST /auth/refresh) |
| **NFR chủ trì** | PERF-004 (N+1/EXPLAIN), SEC-002 (JWT/refresh), SEC-006 (ownership/policy), REL-003 (backup/restore/soft-delete), USE-002 (WCAG AA), MAINT-002 (coverage ≥80%/E2E), MAINT-004 (architecture test), SEO-001 (JSON-LD Recipe), SEO-004 (slug) |
| **ADR sở hữu/đồng sở hữu** | D02, D05, D12, D14, D15, D16, D18, D19, D28 |
| **Điều phối** | Chủ trì rebase/thứ tự **migration** cho cả nhóm (mục 12.1) — nhưng mỗi người tự viết migration của mình |

## 2. Phụ thuộc & bàn giao (để không bị chặn)

**TV3 CẦN nhận:**
- Từ **TV1 (A1)**: auth DTO/interfaces, `ICurrentUser`, cách gắn Authorization header, account fixtures (Author/Admin seed), `ApplicationUser` model + JWT. → cần ở **G1**.
- Từ **TV2 (B1)**: `Category` entity/schema + `CategoryDto`, `RecipeSummaryDto`, `PagedResult<T>`, query params chuẩn. → cần để bật FK Recipe→Category và làm C4 UI.

**TV3 PHẢI bàn giao sớm:**
- Cho **TV2**: entity/DTO Recipe + status enum + RowVersion, endpoint draft. → để TV2 làm list/search (B2/B3).
- Cho **TV4**: fixture Recipe đủ **ingredient + step** để thử publish (D1/D3); interface invalidation cho recipe detail.

## 3. Lộ trình 6 tuần (theo cổng G0–G7)

| Tuần | Task TV3 | Cổng |
|---|---|---|
| 1 | **C1**: ERD/migration, Recipe aggregate + Nutrition, audit/UoW/RowVersion, concurrency spike | G0 stack chạy · G1 Draft tạo được |
| 2 | **C2** (Create/Update/Detail CQRS) + **C3** (Ingredient/Step CRUD) + **C5** (refresh rotation) + ownership/version tests | G2 Draft đủ nguyên liệu/bước; không sửa chéo owner |
| 3 | **C4** (wizard/edit/dashboard/detail UI) + bắt đầu **C6** LAB (OAuth/media/jobs) | G3 đủ 34 FR chạy tích hợp |
| 4 | Hoàn tất **C6** LAB + **C7** (E2E/concurrency/architecture tests, coverage) | G4 24/24 skill có evidence · G5 coverage đạt |
| 5 | Tự deploy/restore, review + số đo (EXPLAIN/k6/a11y), sửa lỗi | G6 staging + 5 E2E pass |
| 6 | Demo soạn thảo/dữ liệu + kỹ năng bổ sung; bàn giao evidence | G7 release + minh chứng đủ |

---

## 4. ADR TV3 phải chốt tuần 1 (điều kiện để code sạch)

| ADR | Nội dung chốt | Trạng thái | Ảnh hưởng task |
|---|---|---|---|
| **D19** | RowVersion bytea opaque + interceptor cập nhật nguyên tử; test 2 writer | Đã đề xuất (ADR-0001) | C1, C2 |
| **D18** | Domain thuần BCL; ApplicationUser ở Infrastructure; SearchVector ở Infra; FluentValidation ở Application | Đã đề xuất (ADR-0001) | C1–C3 |
| **D24/D28** | BaseEntity cho entity nghiệp vụ; Instructions NOT NULL default ""; nutrition 6 cột nullable | Đã đề xuất (ADR-0001) | C1, C2 |
| **D15/D16** | Ingredient 1–200/quantity nullable>0; step StepNumber liên tục, title≤200, desc 1–2000; Difficulty đủ 4 mức (Easy=1..Expert=4) | Đã đề xuất (ADR-0001) | C2, C3 |
| **D02** | validation/publish-thiếu→400; concurrency→**422**; trùng→409 (contract test chung với TV1) | **Cần chốt cùng TV1** | C2, C3, C7 |
| **D14** | slug tự thêm suffix + unique constraint chống race; ổn định sau publish; đổi Draft có 301 | **Cần chốt** | C2 |
| **D05** | refresh 512-bit, lưu SHA-256, rotation trong transaction, revoke family khi reuse | **Cần chốt** | C5 |
| **D12** | dashboard dùng cùng query + scope/status theo quyền; không tin authorId từ client | **Cần chốt cùng TV2** | C2, C4 |

---

## 5. Chi tiết từng task (kèm evidence & DoD)

### C1 — Recipe aggregate + persistence (Tuần 1, 14đ) · FR nền cho RCP-002/003/004
**Skills:** K01, K03, K06, K07 · **ADR:** D18, D19, D24, D28, D15, D16
**Các bước:** entity Recipe + owned Nutrition + child (Ingredient/Step/Image) → EF config + index → AuditInterceptor (audit/RowVersion/soft-delete) → UoW → migration `AddRecipeAggregate` trên DB sạch → concurrency spike.
**Nghiệm thu (đề):** FK đúng · migration chạy DB sạch · rollback transaction · 2 writer không lost update.
**Evidence chuẩn bị:**
- `TV3-K06`: file migration + ảnh chạy `dotnet ef database update` trên DB rỗng thành công.
- `TV3-K07`: test `Two_writers_second_save_is_rejected_no_lost_update` PASS (log xanh) + `Nested_create_failure_rolls_back_whole_aggregate`.
- `TV3-K03`: link Recipe.cs (aggregate + invariant Publish D07).
**Test bắt buộc:** concurrency spike (đã có), rollback test, publish-invariant test.
**DoD:** migration + index đúng schema chương 7; interceptor test được; ADR-0001 merged; PR review bởi Tâm.
**Hiện trạng:** nền đã dựng (Domain + Infra + spike) + ERD chuẩn hóa. Còn: bật FK khi có B1/A1, tạo migration thật, chạy DB sạch, khóa version (D25).

### C2 — Create/Update/Detail CQRS (Tuần 2, 13đ) · FR-RCP-002/003/004
**Skills:** K02, K04, K05, K10, K16 · **ADR:** D02, D12, D14, D19
**Các bước:** Command/Query + Handler (Create→Draft, Update, GetDetail) → FluentValidation → ownership check ở Application (NFR-SEC-006) → sinh slug + suffix chống race (D14) → ETag/RowVersion contract → map DbUpdateConcurrencyException → **422** (D02).
**Nghiệm thu (đề):** tạo luôn Draft · AuthorId từ token (không từ client) · detail nested đúng quyền · conflict có UI reload.
**Evidence:**
- `TV3-K02/K04`: integration test mỗi route (happy + error): POST 201+Location, PUT 200, PUT conflict → 422 (chứa RowVersion mới), GET detail theo quyền.
- `TV3-K10`: test ma trận quyền Guest/Author-owner/non-owner/Admin trên GET /recipes/{slug} và PUT.
- `TV3-K05`: bảng test validator (title biên, cookTime=0, servings<=0, categoryId không tồn tại).
**Test bắt buộc (đề mục 7):** title biên; cookTime=0; servings<=0; CategoryId không tồn tại; non-owner sửa/xem Draft bị từ chối; nested create fail rollback aggregate.
**DoD:** Scalar cập nhật; slug ổn định sau publish; 422 có body chuẩn RFC7807; PR review.

### C3 — Ingredient/Step CRUD + renumber (Tuần 2, 13đ) · FR-RCP-009/010
**Skills:** K02, K03, K04, K05, K06, K07 · **ADR:** D15, D16, D19
**Các bước:** CRUD ingredient/step qua aggregate → StepNumber liên tục 1..N + unique(RecipeId,StepNumber) → renumber trong transaction → validate child thuộc đúng recipe → xử lý thao tác child đồng thời.
**Nghiệm thu (đề):** 1..N liên tục · quantity nullable theo ADR · child thuộc đúng recipe.
**Evidence:**
- `TV3-K07`: test StepNumber liên tục sau xóa; giữ unique khi reorder; lấy childId của recipe khác → thất bại.
- `TV3-K06`: migration child + test transaction reorder.
**Test bắt buộc:** StepNumber liên tục sau xóa; unique khi reorder; ingredient quantity null hợp lệ; child cross-recipe bị từ chối.
**DoD:** transaction đúng; invalidate cache recipe detail (phối hợp interface với TV4); PR review.

### C5 — RefreshToken rotation (Tuần 2, 8đ) · FR-AUTH-004 · NFR-SEC-002
**Skills:** K08 (SP refresh) · **ADR:** D05
**Các bước:** handler refresh → hash SHA-256, rotation trong transaction → phát hiện reuse → revoke cả family → client single-flight (không vòng lặp refresh vô hạn) → không log token.
**Nghiệm thu (đề):** refresh cũ không dùng lại · transaction chống refresh đồng thời · không log token.
**Evidence:**
- `TV3-K08`: test hai refresh cùng token không tạo hai nhánh hợp lệ; refresh của user bị khóa/xóa; reuse → revoke family; grep log không chứa token.
**Test bắt buộc:** concurrent refresh; reuse family; user disabled/deleted; UI không loop.
**DoD:** phối hợp auth contract với TV1; PR review.

### C4 — Wizard/Editor/Dashboard/Detail UI (Tuần 2–3, 12đ) · FR-RCP-002/003/004 (UI)
**Skills:** K05, K16, K17, K18, K19 · **ADR:** D12, D13
**Các bước:** multi-step wizard (`/dashboard/recipes/new`), edit (`/…/[id]/edit`), dashboard list, detail (`/recipes/[slug]` ISR 300s) → RHF/Zod → TanStack Query + optimistic rollback → tổng thời gian/dinh dưỡng đúng → JSON-LD Recipe (cùng TV4) → responsive + a11y (NFR-USE-002).
**Nghiệm thu (đề):** lưu/sửa end-to-end · lỗi inline · optimistic rollback · tổng thời gian/dinh dưỡng đúng.
**Evidence:**
- `TV3-K17`: test Jest/RTL optimistic rollback + error mapping.
- `TV3-K18`: ảnh 320/768/1200px + kết quả NVDA/VoiceOver (contrast ≥4.5:1).
- `TV3-K19`: JSON-LD validate thực tế (không invent rating) — NFR-SEO-001.
**Test bắt buộc:** form validation, state, error mapping, optimistic rollback.
**DoD:** skeleton/empty/toast; noindex cho nội dung riêng tư; ISR chỉ render public (D13); PR review.

### C6 — Lab cá nhân K01–K24 (Tuần 3–4, 25đ)
LAB bắt buộc của TV3 (ngoài phần SP), trên nhánh `practice/TV3/Lx`, dùng stack thật, DB/bucket prefix riêng:
- **L1:** LAB register/login/hash/logout + Google callback/verify/link (K08, K09).
- **L3:** LAB recipe FTS trigger/GIN/rank + Redis cache/output/fallback (K11, K12) + EXPLAIN/k6.
- **L4:** LAB upload/delete 4 MIME + resize + Mailhog + Hangfire delayed/recurring/restart (K13, K14, K15).
- **L5:** LAB SSR search + sitemap/robots/301 + trace/log sink/health (K16, K19, K20).
**Evidence:** mỗi Lx có nhánh/tag/commit + test + demo; nếu SP đã phủ thì trỏ PR SP, không viết lại.

### C7 — Kiểm thử & bàn giao (Tuần 4–5, 15đ) · NFR-MAINT-002/004, PERF-004, REL-003
**Skills:** K21, K22, K23, K24
**Các bước:** unit + integration + UI + E2E create → concurrency suite → architecture test (Domain/Application không ref Infrastructure) → EXPLAIN ANALYZE (không N+1) → tự deploy/restore.
**Evidence:**
- `TV3-K21/MAINT-002`: report coverage Application ≥80%; E2E "Create Recipe" pass.
- `TV3-MAINT-004`: architecture test pass (NetArchTest/ArchUnitNET).
- `TV3-PERF-004`: EXPLAIN ANALYZE các query recipe, chứng minh không N+1.
- `TV3-REL-003`: log restore drill (pg_dump → restore → dữ liệu/refresh token còn).
**DoD:** 5 E2E pass; coverage đạt; runbook; PR review.

---

## 6. Ma trận kỹ năng K01–K24 của TV3 — nơi tạo evidence

| K | Loại | Sẽ chứng minh ở | Evidence key |
|---|---|---|---|
| K01 | SP | ADR-0001 + ma trận mapping | TV3-K01 |
| K02 | SP | C2/C3 Recipe/child endpoint group | TV3-K02 |
| K03 | SP | C1 Recipe aggregate/Nutrition/UoW | TV3-K03 |
| K04 | SP+LAB | C2/C3/C5 handlers + LAB behavior | TV3-K04 |
| K05 | SP | C2/C3/C4 validators + RHF/Zod | TV3-K05 |
| K06 | SP | C1/C3 migration/query/index | TV3-K06 |
| K07 | SP | C1/C3 RowVersion/UoW/soft-delete tests | TV3-K07 |
| K08 | SP+LAB | C5 refresh SP + L1 login/hash/logout | TV3-K08 |
| K09 | LAB | L1 Google callback/verify/link | TV3-K09 |
| K10 | SP+LAB | C2 ownership SP + LAB Admin/VerifiedAuthor/limit | TV3-K10 |
| K11 | LAB | L3 recipe FTS trigger/rank/AND/page | TV3-K11 |
| K12 | SP+LAB | C4 detail invalidation + LAB cache/fallback | TV3-K12 |
| K13 | LAB | L4 upload/delete 4 MIME + boundary | TV3-K13 |
| K14 | LAB | L4 welcome+resize+sitemap+delayed+restart | TV3-K14 |
| K15 | LAB | L4 Mailhog + resize + XML | TV3-K15 |
| K16 | SP+LAB | C4 CSR editor/ISR detail + L5 SSR search | TV3-K16 |
| K17 | SP | C3/C4 ingredient/step mutations + image detail | TV3-K17 |
| K18 | SP | C4 wizard/editor a11y checklist | TV3-K18 |
| K19 | SP+LAB | C4 detail JSON-LD + L5 sitemap/robots/301 | TV3-K19 |
| K20 | SP+LAB | C4 recipe metric + L5 trace/log/health | TV3-K20 |
| K21 | SP | C7 recipe API/UI/E2E | TV3-K21 |
| K22 | SP | C7 concurrency/query + k6 trang của mình | TV3-K22 |
| K23 | SP | C1/C7 migration startup + tự deploy/restore/test 2 API | TV3-K23 |
| K24 | SP | Toàn bộ PR + Tâm review + ADR/runbook + CI | TV3-K24 |

## 7. NFR TV3 chủ trì — cách đo & minh chứng

| NFR | Đo bằng | Evidence |
|---|---|---|
| PERF-004 | EXPLAIN ANALYZE, projection/eager loading, cảnh báo query>100ms | Ảnh plan + PR review trước merge |
| SEC-002 | JWT 15m claims; RT7d SHA-256; rotation/family reuse | Test C5 + cấu hình |
| SEC-006 | AuthorPolicy/AdminPolicy + ownership ở Application; write audit | Test ma trận quyền C2/C3 |
| REL-003 | pg_dump 03:00 giữ 30 ngày; refresh sống qua restart; soft delete D08 | Log restore drill |
| USE-002 | WCAG2.1 AA; contrast≥4.5:1; NVDA/VoiceOver | Ảnh + báo cáo a11y C4 |
| MAINT-002 | coverage≥80%; API happy+error; 5 Playwright flow | Report coverage + E2E |
| MAINT-004 | architecture test Domain/Application không ref Infrastructure | Test pass |
| SEO-001 | JSON-LD Recipe đầy đủ, validate thực tế, không invent rating | Kết quả Rich Results/validator |
| SEO-004 | slug không dấu/chữ thường/gạch nối; đổi Draft có 301 | Test slug + redirect |

## 8. Sổ evidence (mẫu điền sẵn — copy cho mỗi mục)

```
Evidence: TV3-K07 (và FR/NFR liên quan: C1, NFR-REL-003)
Đường dẫn code/config: tests/concurrency-spike/RowVersionConcurrencyTests.cs
Commit/PR: [điền]
Lệnh chạy + môi trường: dotnet test tests/concurrency-spike (Postgres 16, SPIKE_DB=...)
Kết quả thực tế: 4/4 PASS [đính ảnh/log]
Reviewer/ngày: Nguyễn Thanh Tâm / [ngày]
Ghi chú/lỗi còn lại: [ghi rõ]
```

Áp dụng khung này cho 24 ô K01–K24 + 6 FR + 9 NFR chủ trì. Khi review, Tâm chỉ cần mở từng bản ghi → chạy lệnh → đối chiếu kết quả.

## 9. Checklist nghiệm thu cá nhân TV3 (đối chiếu mục 12.5 đề)

- [ ] 6 FR chủ trì (RCP-002/003/004/009/010, AUTH-004) có implementation + test + evidence.
- [ ] 9 NFR chủ trì có số đo/kết quả hoặc ghi rõ giới hạn.
- [ ] 24/24 ô K01–K24 có bản ghi `TV3-Kxx` + demo độc lập.
- [ ] Application coverage ≥80%; E2E "Create Recipe" pass.
- [ ] Concurrency: 2 writer, refresh reuse, step renumber, slug race — không lỗi.
- [ ] Architecture test pass (Domain/Application sạch).
- [ ] Tự dựng app từ checkout sạch theo README + restore 1 backup + tìm log/trace request.
- [ ] Mọi PR được Tâm review; ADR D02/D05/D14/D12 đã chốt.

## 10. Việc làm ngay (theo thứ tự)

1. Gửi nhóm chốt **D02, D05, D14, D12** (đang mơ hồ) — ghi vào sổ ADR.
2. Khóa version **.NET10/EF10/PG16/Npgsql/Redis7** vào `global.json`/lockfile (D25) cùng cả nhóm.
3. Nhận **A1** (auth/ICurrentUser) + **B1** (Category) → bật FK Recipe→Category/Author → tạo migration `AddRecipeAggregate` → chạy DB sạch → lưu `TV3-K06`.
4. Chạy concurrency spike đã có → lưu `TV3-K07`.
5. Bắt đầu **C2** (Create/Update/Detail CQRS) theo tiêu chí trên.
