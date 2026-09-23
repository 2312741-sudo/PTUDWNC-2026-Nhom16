# Blooms & Handoff — TV4 Tuần 2 (Blocked tasks / Khi người phụ trách vắng mặt)

> **Tác giả**: Nguyễn Hữu Trung Sơn (2312739 — TV4)
> **Mục đích**: danh sách các task của TV4 **đang bị block**, điều kiện để **gỡ block**, và **hướng dẫn để người khác tiếp tục** nếu TV4 không có mặt để trao đổi. Tài liệu tự túc: đọc xong là biết *ai cần bàn giao gì, code hiện đang ra sao, và công việc kế tiếp chính xác là gì*.
> **SRS tham chiếu**: v1.1.1 (Approved 16/09/2026) · **Nhánh**: `2312739_NHTSon_D1-D2-D3-D4` · **Reviewer**: Nguyễn Thanh Tâm
> **Cập nhật lần cuối**: 23/09/2026

---

## 0a. Bản sửa đổi 23/09/2026 (merge C2/C3 của TV3 vào nhánh TV4 — gỡ triệt để block 2.1) ⭐

> Merge local commit `2086ee8` (TV4) = `32cf816` (D1.3 + fix format) + `8d2497e` (TV3 C2/C3).
> **ĐIỀU KIỆN 3 (recipe CRUD endpoints) đã có**: `POST/PUT /api/v1/recipes`, `GET /{slug}`,
> `POST/PUT/DELETE /{id}/ingredients`, `POST/PUT/DELETE /{id}/steps`, `PATCH /{id}/steps/reorder`
> (là từ `src/backend/CulinaryBlog.Application/Recipes.cs` của TV3). Build Release 0 warning, `dotnet format` sạch, **88/88 test pass**, CI run #78 = success.
> → **Block 2.1 đã gỡ HOÀN TOÀN** (4/4 điều kiện). Seed recipe giờ làm được qua API (`POST /recipes` với Bearer author),
> test E2E D1.3 (upload/PATCH/DELETE image trên MinIO) chạy được mà không cần fixture thủ công.

| Block | Trạng thái sau kiểm chứng (23/09) |
|---|---|
| **2.1** Recipe cluster | ✅ **Gỡ hoàn toàn** — 4/4 điều kiện: DI tách `IRecipeRepository` + `IRecipeImageRepository`; migration `20260919061954_AddRecipeAggregate`; **recipe CRUD endpoints (TV3 C2/C3)**; envelope + RFC7807 (422). D1.3 image endpoints test E2E được qua API. |
| **2.2** C5 refresh token | ❌ Chưa gỡ — nhánh TV3 C2/C3 không mang theo C5 (`/auth/refresh` vẫn không có; `JwtService` trả `RefreshToken: null`). Chờ TV3 bàn giao tiếp hoặc TV4 tự triển khai sau khi chốt ADR D05. |
| **2.3** D27 bucket policy | ❌ Chưa gỡ — chờ quyết định nhóm + CR (không liên quan merge này). |
| **2.4** D23 queue resize | ❌ Chưa gỡ — chưa có `Hangfire`/`ImageSharp` trong csproj (không liên quan merge này). |

---

## 0. Bản sửa đổi 22/09/2026 (kiểm chứng sau khi TV3 merge PR #10)

> TV3 merge `5b36251` (PR #10, "Go block 2.1") nhưng **chỉ gỡ ĐIỀU KIỆN 1, 2, 4** của block 2.1:
> wiring DI (`IApplicationDbContext`/`IUnitOfWork` từ `AuthDbContext`), migration `20260919061954_AddRecipeAggregate`
> (Recipes/RecipeImages/RecipeIngredients/RecipeSteps/RefreshTokens), envelope + RFC7807.
> **ĐIỀU KIỆN 3 (endpoints CRUD recipe: tạo recipe / thêm ingredient / thêm step) vẫn CHƯA có** trong `Program.cs`
> (chỉ có auth + categories). Các bản cũ 17/09/2026 vẫn còn trong git history (commit `8e14c68`).

| Block | Trạng thái sau kiểm chứng |
|---|---|
| **2.1** Recipe cluster | 🔶 **Gỡ một phần**: D1.3 image endpoints làm được ngay (đã đủ wiring + migration + envelope); cần seed recipe để test end-to-end (thiếu condition 3). |
| **2.2** C5 refresh token | ❌ Chưa gỡ — chỉ có schema/entity `RefreshToken` (bản tối thiểu), `JwtService` vẫn trả `RefreshToken: null`. |
| **2.3** D27 bucket policy | ❌ Chưa gỡ — chờ quyết định nhóm + CR. |
| **2.4** D23 queue resize | ❌ Chưa gỡ — chưa có `Hangfire`/`ImageSharp` trong csproj. |

---

## Cách dùng tài liệu này

| Bạn là | Đọc phần |
|---|---|
| TV3/TV1 muốn **bàn giao** để mở khoá TV4 | Mục "[Bàn giao chờ nhận](#2-bàn-giao-chờ-nhận-để-gỡ-block)" — mỗi dòng có *điều kiện gỡ cụ thể* |
| Reviewer muốn **kiểm tra/đánh giá** | Mục "[Checklist nghiệm thu sau khi gỡ](#4-checklist-nghiệm-thu-sau-khi-gỡ)" |
| Ai đó thay TV4 **làm tiếp** | Mục "[Hướng dẫn làm tiếp chi tiết](#5-hướng-dẫn-làm-tiếp-chi-tiết-theo-api-template)" + "[Code hiện trạng](#6-code-hiện-trạng)" |
| Mọi người | "[Nguyên tắc không phá vỡ](#7-nguyên-tắc-không-phá-vỡ)" + "[Kênh trao đổi](#8-kênh-trao-đổi-và-escalation)" |

---

## 1. Tóm tắt trạng thái tuần 2 TV4

| Task | Nội dung | Trạng thái | Block bởi |
|---|---|---|---|
| **D1.1** | `MinioStorageService` + DI + lockfile | ✅ Xong (MinIO SDK 7.0.0) | — |
| **D1.2** | Validator 4 MIME + ≤5MiB + magic bytes | ✅ Xong + 16 unit test | — |
| **D1.5** | Domain primary invariant + race spike test | ✅ Xong (9 domain + 1 spike) | — |
| **D1.6** | `docs/IMAGE_CONTRACT.md` (bàn giao TV3) | ✅ Xong | — |
| **D1.3** | API upload/PATCH primary/DELETE image | 🟢 **Đã triển khai + E2E làm được** (23/09: block 2.1 gỡ hoàn toàn, có seed qua API) | Không còn block — còn lại: chạy E2E trên MinIO + chứng minh MinIO down/log redacted (D1.1c) |
| **D1.4/D27** | Bucket policy private + xử lý orphan | 🔴 Block | Quyết định nhóm D27 |
| **D2** | Resize 300×300/800×600 + job nền | 🔴 Block | Quyết định nhóm D23 (queue) + package ảnh |
| **D3.1/D3.2** | Publish/unpublish CQRS + 422 + ownership | 🔶 **Fixtures đã đủ** (C2/C3 tạo recipe + ingredient/step qua API); chưa có publish endpoint | TV3 C2 thêm `Publish/Unpublish` command (hoặc TV4 tự làm — 2 command vẫn chưa có trong `Recipes.cs`) |
| **D3.3** | Logout revoke refresh family | 🔴 Block | TV3 C5 (refresh token) |
| **D4** | UI uploader/status + sitemap/robots nền | 🔴 Block | TV3 C1/C2 + D27 + D23 (UI cần API ảnh chạy) |
| **D6** | Lab `practice/TV4/L4` | 🟡 Chưa bắt đầu | Không ai block — độc lập, chạy song song |

> **Luật bàn giao**: chỉ đóng 1 block khi **bàn giao + hướng dẫn mục 4–6 thoả**. Đừng đóng block chỉ vì "code compile".

---

## 2. Bàn giao chờ nhận (để gỡ block)

> Đây là những gì TV4 cần **từ người khác**. Khi bạn bàn giao đủ các điều kiện dưới đây, TV4 (hoặc người thay thế) có thể làm tiếp ngay không cần hỏi.

### 2.1. 🅰️ Từ TV3 — Recipe cluster khả dụng trong API (`D1.3`, `D3.1`, `D3.2`, `D4`)

> **Bản sửa đổi 23/09/2026**: TV3 bàn giao **C2/C3** qua merge local `2086ee8` vào nhánh `2312739_NHTSon_D1-D2-D3-D4` (và đã push — CI #78 success).
> **ĐIỀU KIỆN 3 ĐÃ ĐẠT** — recipe CRUD endpoints có trong `Program.cs` (xem "Hệ quả thực tế" bên dưới). Block 2.1 giờ gỡ **hoàn toàn 4/4**. Trạng thái từng điều kiện DoR:

| Điều kiện gỡ | Trạng thái (23/09) |
|---|---|
| 1. DI recipe trong host (`IApplicationDbContext`/`IUnitOfWork`) | ✅ Đạt — `AuthDbContext` implement `IApplicationDbContext`; `Program.cs` giờ register cả `IRecipeRepository` + `IRecipeImageRepository`. |
| 2. Migration áp dụng lên DB dev (bảng recipes/recipe_images/...) | ✅ Đạt (code) — migration `20260919061954_AddRecipeAggregate`; cần `dotnet run --migrate`. |
| 3. Endpoints CRUD recipe (tạo recipe / ingredient / step) | ✅ **Đạt** — TV3 C2/C3: `POST/PUT /api/v1/recipes`, `GET /recipes/{slug}`, `POST/PUT/DELETE /{id}/ingredients`, `POST/PUT/DELETE /{id}/steps`, `PATCH /{id}/steps/reorder` (từ `Application/Recipes.cs`). |
| 4. Envelope `{data}` + RFC7807 + `ApiExceptionHandler` | ✅ Đạt — 422 `recipe.version_conflict` (RowVersion/unique-primary) + `DomainException` 400/422 (C02). |

**Hệ quả thực tế cho TV4 (sau 23/09):**
- **D1.3 image endpoints test E2E được ngay**: tạo recipe qua `POST /recipes` (Bearer author) → `POST /recipes/{id}/images` (multipart) → PATCH primary → DELETE.
- Không còn cần `--seed` thủ công — seed qua API là chính thức.

**Còn lại mà TV3 chưa mang đến (ảnh hưởng D3):**
- **Publish/unpublish không nằm trong C2/C3** — TV3 cần thêm 2 command `PublishRecipeCommand`/`UnpublishRecipeCommand` trong `Recipes.cs`
  (hoặc TV4 tự triển khai sau khi hết block, theo điều kiện FR-RCP-005/006 + C02).
- **C5 refresh token KHÔNG nằm trong C2/C3** — block 2.2 vẫn đứng (xem 2.2).

**Sau khi gỡ đủ, TV4 làm tiếp (không cần TV3):**
- `POST /recipes/{id}/images` (multipart) → 201 `RecipeImageDto`.
- `PATCH /recipes/{id}/images/{imageId}` → 200 (isPrimary/altText/orderIndex, D17).
- `DELETE /recipes/{id}/images/{imageId}` → 204 + xoá object MinIO.
- `PATCH /recipes/{id}/publish` & `/unpublish` → 200/422 + ownership 403 (khi TV3 thêm Publish/Unpublish).

---

### 2.2. 🅱️ Từ TV3 — C5 refresh token (`D3.3` logout revoke)

**Điều kiện gỡ:**
- TV3 merge khả năng **refresh token family** (hash/rotation) và expose một cách để **vô hiệu family** khi người dùng logout (interface/DbSet/endpoint internal).

**Sau khi gỡ, TV4 làm tiếp:**
- Trong `POST /auth/logout` (hiện là seam Bearer-only, 204, xem `Auth.cs`): sau khi xác định được refresh family của phiên → revoke family, giữ trả 204 idempotent.
- KHÔNG tự build refresh rotation song song — tránh hai luồng trùng nhau (ADR D05).

---

### 2.3. 🅲 từ cả nhóm — Quyết định D27 bucket policy (`D1.4`, `D2`, `D4`)

Đề xuất hiện tại (ADR-TV4-001): bucket **private mặc định**; ảnh phục vụ qua **presigned URL hoặc proxy có auth**. SRS 2.4.1 ghi `public-read` → nếu đổi phải **CR + xác nhận giảng viên/nhóm**.

| Phương án | Ảnh hưởng đến code TV4 |
|---|---|
| Private + presigned URL | `StoredFile.Url` hiện trả **key** (đường dẫn tương đối). Cần thêm method sinh presigned (thêm vào `IFileStorageService`) hoặc endpoint proxy `GET /resources/images/{key}` |
| Public bucket cho ảnh đã publish | Cần bỏ proxy, URL tuyệt đối; **rủi ro lộ Draft/Archived** → phân biệt strict |
| Hybrid (AI đang dùng) | Tin cũ nhất hiện chưa làm — chờ chốt |

**Cần chốt từ nhóm, TV4 ghi vào ADR sau khi có. Không tự ý đổi code trước khi có quyết định.**

---

### 2.4. 🅳 từ cả nhóm — Queue cho resize theo D23 (`D2`)

| Phương án | Trạng thái | Điều kiện gỡ |
|---|---|---|
| Hangfire (SRS mô tả) | Chưa chốt | Đồng ý thêm package `Hangfire` (+ lockfile D25 + CI services) |
| `BackgroundService` thủ công | Chưa chốt | Đồng ý không dùng Hangfire (tránh phụ thuộc mới) |

**Sau khi chốt, TV4 làm tiếp:**
- Resize tạo `{uuid}_original`, `{uuid}_300x300`, `{uuid}_800x600` (config kích thước, không hard-code).
- Job retry idempotent; original fallback khi resize lỗi.
- Boundary test: delete vs resize không tái sinh ảnh đã xoá.

---

## 3. Bàn giao TV4 → người khác (đã xong, đang dùng được ngay)

| Bàn giao cho | Gì | Ở đâu |
|---|---|---|
| TV3 | `IMAGE_CONTRACT.md` (DTO, endpoints, lỗi, tích hợp) | `docs/IMAGE_CONTRACT.md` |
| Mọi người | `MinioStorageService` (upload/delete qua interface) | `src/backend/CulinaryBlog.Infrastructure/MinioStorageService.cs` |
| Mọi người | Validator ảnh (magic bytes/MIME/size) | `src/backend/CulinaryBlog.Application/ImageUpload.cs` |
| Reviewer | Evidence tests | `tests/CulinaryBlog.Tests/ImageUploadValidatorTests.cs`, `RecipeImageDomainTests.cs`, `tests/concurrency-spike/RecipeImagePrimaryConcurrencyTests.cs` |

---

## 4. Checklist nghiệm thu sau khi gỡ

> Dùng để verify khi TV4 vắng mặt. Mỗi dòng có lệnh chạy cụ thể.

| # | Kiểm tra | Lệnh / Cách | Kỳ vọng |
|---|---|---|---|
| 1 | Build sạch | `dotnet build CulinaryBlog.sln --no-restore --configuration Release` | 0 warning / 0 error |
| 2 | Format LF | `dotnet format CulinaryBlog.sln --verify-no-changes --no-restore` | im lặng (pass) |
| 3 | Toàn bộ test | `dotnet test CulinaryBlog.sln --no-build --configuration Release` | **77/77 pass** (72 unit + 5 spike) |
| 4 | Lockfile khớp CI | `dotnet restore CulinaryBlog.sln --locked-mode` | thành công |
| 5 | Upload ảnh hợp lệ | `curl -F "file=@x.jpg" POST /api/v1/recipes/{id}/images` (với Bearer owner) | 201 + `{ "data": RecipeImageDto }`, `isPrimary=true` (ảnh đầu) |
| 6 | Upload sai format | file GIF / PNG nhưng Content-Type JPEG | 400 `file.invalid_type` |
| 7 | Upload >5MiB | file 6MiB | 400 `file.too_large` |
| 8 | Set primary | PATCH ảnh thứ 2 `{"isPrimary":true}` | 200; đúng 1 primary (query DB) |
| 9 | Race 2 writer | chạy spike test | 1 primary cuối cùng (test mẫu đã có) |
| 10 | Delete → xoá object | DELETE ảnh + kiểm tra bucket MinIO | 204; object không còn (no orphan) |
| 11 | Publish thiếu thành phần | PATCH publish recipe 0 ingredient | 422 `RECIPE_PUBLISH_INCOMPLETE` |
| 12 | Publish đủ | recipe ≥1 ingredient + ≥1 step | 200 Published |
| 13 | Non-owner | PATCH publish bằng user khác | 403 |
| 14 | Draft không lộ public | GET public endpoint khi recipe Draft | 404/không trả dữ liệu |
| 15 | CI | GitHub Actions `Backend week 1` | success |
| 16 | Không secret | `git grep -i "password\|secret\|token" --diff` | không lộ credential |

---

## 5. Hướng dẫn làm tiếp chi tiết (theo API template)

> Pattern làm theo nhóm đã thống nhất (xem `Categories.cs` + `Program.cs` Category endpoints), không tự sáng tạo kiến trúc mới.

### 5.1. Template một handler (Application layer)

```csharp
// CulinaryBlog.Application/RecipeImages.cs (nơi đặt các command recipe image)
public sealed record UploadRecipeImageCommand(Guid RecipeId, string FileName, string ContentType, Stream Content, long SizeBytes, string? AltText, ClaimsPrincipal User)
    : IRequest<RecipeImageDto>;

public sealed class UploadRecipeImageCommandValidator : AbstractValidator<UploadRecipeImageCommand>
{
    public UploadRecipeImageCommandValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        // magic bytes + size qua ImageUploadValidator.Validate(...)
    }
}

public sealed class UploadRecipeImageHandler : IRequestHandler<UploadRecipeImageCommand, RecipeImageDto>
{
    private readonly IApplicationDbContext _db;   // sau khi gỡ block 2.1
    private readonly IFileStorageService _storage; // ĐÃ CÓ (MinioStorageService)
    private readonly ICurrentUser _currentUser;    // ĐÃ CÓ

    public async Task<RecipeImageDto> Handle(UploadRecipeImageCommand cmd, CancellationToken ct)
    {
        // 1) Load recipe từ _db.Recipes -> 404 nếu không có; kiểm tra ownership -> 403
        // 2) ImageUploadValidator.Validate(content, size, contentType) -> 400 nếu fail
        // 3) StoredFile sf = await _storage.UploadAsync(content, fileName, contentType, $"recipes/{recipeId}", ct);
        // 4) recipe.AddImage(sf.Key, altText)  // ảnh đầu tự primary (domain đã có)
        // 5) Save -> trả RecipeImageDto
        throw new NotImplementedException(); // điểm TV4 sẽ xử lý khi gỡ block
    }
}
```

### 5.2. Đăng ký endpoint (Program.cs, sau AddInfrastructure)

```csharp
app.MapPost("/api/v1/recipes/{id:guid}/images", async (Guid id, IFormFile? file, string? altText, ISender sender, ClaimsPrincipal user, CancellationToken ct) =>
{
    // dựa pattern endpoint category: sender.Send(new UploadRecipeImageCommand(...))
    // -> Results.Created($"/api/v1/recipes/{id}/images/{imageId}", Envelope.Ok(dto));
});
```

### 5.3. `RecipeImageDto` (định nghĩa trong IMAGE_CONTRACT.md)

```csharp
public sealed record RecipeImageDto(
    Guid Id, Guid RecipeId, string OriginalUrl, string? MediumUrl, string? ThumbnailUrl,
    string? AltText, bool IsPrimary, int OrderIndex, DateTime CreatedAt, DateTime UpdatedAt);
```

### 5.4. Điểm lưu ý khi làm tiếp

- **Cross-recipe**: mọi lệnh phải load image qua `recipeId` + `imageId` (filter `i.RecipeId == cmd.RecipeId`) — không cho set primary ảnh của recipe khác. Trả `image.not_found`.
- **Race primary**: dùng cơ chế chống xung đột hiện có (RowVersion `BaseEntity` + partial unique index `ux_recipe_images_one_primary` đã tồn tại trong `RecipeImageConfiguration`). Mẫu xử lý xem spike test.
- **Không nuốt lỗi MinIO**: exception lan ra; chuyển thành problem (5xx) — `MinioStorageService` đã làm vậy.
- **Log redacted**: không log accessKey/secretKey của MinIO khi lỗi — `MinioOptions` đã tách config.

---

## 6. Code hiện trạng

| File | Vai trò | Ghi chú gỡ block |
|---|---|---|
| `src/backend/CulinaryBlog.Infrastructure/MinioStorageService.cs` | impl `IFileStorageService` | ✅ ĐÃ XONG |
| `src/backend/CulinaryBlog.Application/ImageUpload.cs` | `ImageFormats` + `ImageUploadValidator` | ✅ ĐÃ XONG |
| `src/backend/CulinaryBlog.Application/Storage.cs` | `IFileStorageService` + `StoredFile` | contract cố định |
| `src/backend/CulinaryBlog.API/Program.cs` | host | ✅ 23/09: group recipe có đủ — C2/C3 (create/update/detail + ingredient/step CRUD + reorder) **và** D1.3 images (upload/PATCH/DELETE). DI đăng ký cả `IRecipeRepository` + `IRecipeImageRepository`. |
| `src/backend/CulinaryBlog.Infrastructure/DependencyInjection.cs` | `AddInfrastructure` | ➖ Đã bị xoá ở `5b36251` (TV3 gộp vào `AuthDbContext`) |
| `src/backend/CulinaryBlog.Application/Recipes.cs` | C2/C3 recipe CQRS (TV3) | ✅ 23/09 có trong nhánh TV4 — auth/đăng ký qua `IRecipeRepository`. |
| `src/backend/CulinaryBlog.Infrastructure/RecipeRepository.cs` | repo recipe (TV3) | ✅ 23/09 có — `.Include` child (ingredients/steps/images). |
| `src/backend/CulinaryBlog.Domain/Entities/Recipe.cs` | `AddImage/SetPrimaryImage/RemoveImage` (aggregate) | bất biến primary trong domain |
| `src/backend/CulinaryBlog.Domain/Entities/RecipeImage.cs` | entity ảnh | ctor internal, qua aggregate |
| `src/backend/CulinaryBlog.Infrastructure/Persistence/Configurations/RecipeImageConfiguration.cs` | partial unique index + RowVersion | đã bảo vệ tầng DB |
| `src/backend/CulinaryBlog.API/ApiExceptionHandler.cs` | map `AppException` → RFC7807 | đã có |
| `tests/concurrency-spike/RecipeImagePrimaryConcurrencyTests.cs` | spike race primary | mẫu để làm test tương tự |
| `docs/IMAGE_CONTRACT.md` | contract cho TV3 | bàn giao sẵn |

### 6.1. Kiểm tra nhanh hiện trạng wiring (sau 22/09)

```powershell
# Sau TV3 merge, kỳ vọng tìm thấy:
Select-String -Path src/backend/CulinaryBlog.API/Program.cs -Pattern "IApplicationDbContext|IUnitOfWork|AuthDbContext|ApplicationDbContext"
# Kết quả thực tế 22/09: AddScoped<IApplicationDbContext>(sp => GetRequiredService<AuthDbContext>())
#                          AddScoped<IUnitOfWork, EfUnitOfWork>()  — condition 1 đạt; condition 3 (recipe CRUD) chưa.
```

---

## 7. Nguyên tắc không phá vỡ

1. **Đừng sửa contract** `IFileStorageService`/`StoredFile` mà chưa hỏi — nó là điểm bàn giao TV3.
2. **Đừng tự xoá/đổi** `ux_recipe_images_one_primary` hay RowVersion — chúng là phòng thủ D19 đã nghiệm thu.
3. **Đừng nuôi 2 luồng refresh token** (rotation) — chỉ gắn vào revoke khi C5 chính thức.
4. **Đừng commit `Minio` accessKey/secretKey**: chỉ dùng `.env` + `MinioOptions`. CI không có MinIO service — nếu thêm integration test cần MinIO, phải thêm service MinIO vào `.github/workflows/backend.yml`.
5. **Giữ chuẩn LF** (`.gitattributes` đã set) — chạy `dotnet format` trước commit.
6. **Lockfile**: bất kỳ package nào thêm (Hangfire/ImageSharp) phải regenerate `packages.lock.json` bằng `dotnet restore` (không dùng `--locked-mode` khi add mới), CI dùng `--locked-mode` sẽ báo lỗi nếu không khớp.

---

## 8. Kênh trao đổi và escalation

| Vấn đề | Kênh | Người |
|---|---|---|
| Chưa rõ recipe API shape | Issue/PR review | TV3 (sở hữu recipe) + Nguyễn Thanh Tâm (reviewer) |
| Xung đột wiring Program.cs | PR review | TV1 (sở hữu host) + TV3 |
| D27/D23 chưa chốt | Họp nhóm / comment kế hoạch | Cả nhóm — cần quyết định chính thức |
| Nghi vấn bảo mật bucket | Link ADR-TV4-001 | TV4 + TV1 + giảng viên (nếu CR) |

**Escalation nếu TV4 vắng mặt lâu**: người tiếp nhận nên review `docs/evidence/TV4/Tuan02/` (kế hoạch + mô tả + sổ evidence + trạng thái) rồi liên hệ TV3 trước, vì đa số block nằm ở Recipe cluster, không phải ở phần TV4 đã hoàn thành.

---

## Phụ lục: bản ghi thay đổi bàn giao

| Ngày | Ai | Nội dung bàn giao/thay đổi |
|---|---|---|
| 17/09/2026 | TV4 | Sơn soạn tài liệu này; D1.1/D1.2/D1.5/D1.6 xong (commit `a763cbf`) |
| 22/09/2026 | TV3 (merge PR #10) | Gỡ **một phần** block 2.1: wiring `IApplicationDbContext`/`IUnitOfWork` + migration `20260919061954_AddRecipeAggregate` + envelope/RFC7807 (commit `5b36251`). Condition 3 (recipe CRUD endpoints) **vẫn chưa** — cập nhật mục 0/2.1/6. |
| 22/09/2026 | TV4 | Kiểm chứng lại trạng thái block; cập nhật tài liệu này. |
| 23/09/2026 | TV3 (C2/C3) + TV4 | **Gỡ hoàn toàn block 2.1**: merge local `2086ee8` (TV4) = `32cf816` + `8d2497e` đưa recipe CRUD endpoints (TV3 C2/C3) vào nhánh TV4; 88/88 test pass, CI #78 success. Seed recipe qua API được; D1.3 test E2E được. Mục 0a/2.1/6/Phụ lục cập nhật. |