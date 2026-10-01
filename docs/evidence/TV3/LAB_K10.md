# LAB K10 - TV3: Policy Admin/VerifiedAuthor, chu so huu, rate limit (nhanh practice/TV3/labs, KHONG merge)
- Ky nang: RBAC theo role, policy theo claim, resource-based authorization (owner), rate limit 429 + Retry-After.
- File tao: labs/TV3/Lab.TV3.Api/L10/AuthorizationEndpoints.cs, labs/TV3/Lab.TV3.Tests/L10AuthorizationTests.cs
- File sua: Lab.TV3.Api/LabDb.cs (cot verified_author, bang lab_posts/lab_comments), L1/AuthEndpoints.cs (LabUser.VerifiedAuthor),
  L1/TokenService.cs (claim verified_author), Program.cs (AddL10Authorization, UseRateLimiter, MapL10Authorization)
- Commit do: 4837af2 (6/6 FAIL, POST /lab/l10/posts -> 404) - LAB_K10_do.txt
- Commit xanh: f7da99f (6/6 PASS) - LAB_K10_xanh.txt
- Lenh: $env:LAB_PG="Host=localhost;Port=5432;Username=postgres;Password=..."; dotnet test labs/TV3/Lab.TV3.Tests --filter "FullyQualifiedName~L10" -m:1
- Ket qua that: L10 6/6 Passed; ca bo lab (Infra!=docker) 59/59 sau K10, 61/61 sau K23; format verify (--severity error) exit 0.
- Test phu: Guest 401 (4 endpoint); Author/VerifiedAuthor vao /admin/stats 403, Admin 200; owner sua 200, non-owner 403 (DB khong doi),
  Admin sua bai nguoi khac 200, bai khong ton tai 404; Author thuong publish 403, VerifiedAuthor non-owner 403, VerifiedAuthor owner 200;
  Admin qua policy VerifiedAuthor; binh luan thu 6/phut -> 429 + Retry-After + code RATE_LIMITED, user khac van 201, DB chi co 6 dong.
- JWT that (app lab phat), DB that lab_tv3_test; vai tro doi bang UPDATE lab_users roi dang nhap lai (khong mock).
- Chua do / gioi han: rate limiter in-memory -> moi instance dem rieng (2 API sau Nginx thi gioi han thuc te x2);
  chua kiem HTTPS/CORS/secret (ngoai pham vi LAB nay); 404 tra truoc 403 nen lo su ton tai cua bai (chap nhan trong lab).
