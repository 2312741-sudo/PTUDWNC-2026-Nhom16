# Release Checklist Tuần 6 — TV4 (cổng G7)

- **Ngày lập:** 17/10/2026 · **Người lập:** TV4 — Nguyễn Hữu Trung Sơn (2312739)
- **Nhánh:** `2312739_NHTSon_D7` · **PR:** [#37](https://github.com/2312741-sudo/PTUDWNC-2026-Nhom16/pull/37) (chờ Tâm review)
- **Nguồn checklist:** `docs/KE_HOACH_DU_AN.md` §12.5.
- **Quy ước trạng thái:** ✅ đạt (có bằng chứng) · ⚠️ **đạt một phần** (nêu rõ nợ) · ⬜ **chưa đạt/chờ** (nêu lý do, người phụ thuộc).

> Nguyên tắc: **không tự đánh dấu ✅ cho việc cần reviewer xác nhận** (mục 4) và **không tự nhận "đạt" cho nợ ngoài TV4**. Số liệu lấy từ `SO_EVIDENCE_TUAN_6.md`.

## Bảng 9 mục §12.5

| # | Mục release | Trạng thái | Bằng chứng | Ghi chú / nợ |
|---|---|---|---|---|
| 1 | **34/34 FR** có implementation + test + người chịu trách nhiệm | ⚠️ | `../MAPPING_K01_FR_NFR_ADR_EVIDENCE.md` (bảng FR→file/test) | Đối chiếu xong ở TV4, **chờ Tâm xác nhận** (mục 4) |
| 2 | **30/30 NFR** có kết quả đo hoặc ghi "chưa đạt + lý do" | ⚠️ | `SO_EVIDENCE_TUAN_6.md` §1, §3 · `Tuan05/` | Nợ ghi rõ: `NFR-SEO-004` (TV3), Google OAuth K09 (credentials), `npm audit` major (nhóm) |
| 3 | **10/10 CONS** kiểm tra; ADR mâu thuẫn đã chốt | ⚠️ | `docs/adr/` (**6 ADR**), `../MAPPING...` | CONS đối chiếu ở mapping; ADR mới nhất `ADR-TV4-004` (pin ImageSharp 3.1.12) |
| 4 | **4 người × 24 K** có minh chứng + demo + reviewer xác nhận | ⬜ **0/24 Tâm xác nhận** | `TRANG_THAI_THUC_HIEN_TUAN_6.md` §4 | **Điều kiện G7** — không tự đánh dấu |
| 5 | **33 endpoint** baseline + route ADR đối chiếu OpenAPI, có test | ⚠️ | **38 đăng ký `Map*`** (`Program.cs` + extensions); Scalar | Đối chiếu OpenAPI tại `/scalar/v1`; chênh số do route phát sinh theo ADR |
| 6 | **5 critical E2E pass**; Application coverage ≥ 80%; không lỗi chặn luồng/lộ dữ liệu | ⚠️ | `SO_EVIDENCE_TUAN_6.md` §3.2; `logs/regression_e2e_run{1,2}.log` | Coverage **96.17%** ✅; E2E **44/46 ×2** — 2 lỗi **drift spec/product** (`wizard-week4` (d)/(d2), §3.1); luồng `category` **chưa có spec** (TV2) |
| 7 | Compose/Nginx/HTTPS/secrets/volumes/backup/restore chạy thật | ⚠️ | `logs/w62_staging_up.log`; `docs/RUNBOOK.md` | Staging up healthy (api/frontend/nginx/HTTPS 200); backup/restore theo RUNBOOK (A2) |
| 8 | README, Scalar, ADR, **CHANGELOG**, báo cáo, **tài khoản demo** bàn giao đầy đủ | ✅ | `CHANGELOG.md` (**0.5.0**), `README.md` §5.3 (tài khoản demo), `/scalar/v1` | Tài khoản demo: `masterchef@culinary.local` (Admin) + 4 Author, mật khẩu `User@123456` (seed, dev-only) |
| 9 | Ghi rõ production dependencies chưa cấu hình / giới hạn hạ tầng | ✅ | `docs/RUNBOOK.md` §"Giới hạn chung"; `HANDOFF_TV4_TUAN6.md` §6 | OAuth, `npm audit` major, `/metrics` in-memory, k6 proxy… |

## Script bàn giao (buổi bảo vệ)

1. **Dựng từ checkout sạch** (runbook A1, README §5): `docker compose -f docker-compose.dev.yml up -d` → `dotnet restore --locked-mode` → tạo `.env` (sinh `JWT_SIGNING_KEY`) → `dotnet run --project src/backend/CulinaryBlog.API -- --migrate --seed` → `npm ci && npm run dev`.
2. **Backup/restore** (runbook A2): dump Postgres → phục hồi từ backup → xác thực dữ liệu.
3. **Secrets** (không commit): cần đặt `POSTGRES_PASSWORD`, `RUSTFS_ACCESS_KEY`, `RUSTFS_SECRET_KEY`, `JWT_SIGNING_KEY`, `Jwt__Issuer`, `Jwt__Audience`. `.env` **không** nằm trong git.
4. **Tài khoản demo**: đăng nhập `masterchef@culinary.local` / `User@123456` (Admin) — chỉ dùng cho demo cục bộ.

## Kết luận W6-6

- CHANGELOG **0.5.0** + README (tài khoản demo) **đã cập nhật**.
- **Chưa thể đóng G7**: mục **4 (0/24 ô K)** phụ thuộc **Tâm xác nhận**; mục **6** còn **drift E2E** và **thiếu spec `category`**.
- Không có secret nào bị commit (đã quét `deploy/scan-secrets.sh` — `logs/scan_secrets.log`).
