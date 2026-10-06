# K19 (SP) — JSON-LD Schema.org Recipe trang chi tiết `/recipes/[slug]` (NFR-SEO-001) — TV3

Ngày: 02/10/2026. Nhánh `2312786_HuynhQuocTrung_C7-frontend-tests`. Commit: test đỏ `5930a41` → sửa xanh `f0cc239`.

## Cách nhúng
`src/frontend/src/app/recipes/[slug]/layout.tsx` (server component) gọi `GET /api/v1/recipes/{slug}` (`revalidate: 60`), chỉ khi `status == Published`
mới nhúng `<script type="application/ld+json">` dựng bởi `buildRecipeJsonLd` (`src/frontend/src/lib/recipe-jsonld.ts`); `jsonLdString` thay `<` bằng `\u003c`
để dữ liệu người dùng không đóng được thẻ `</script>`. **Không sinh `aggregateRating`/`review`** — hệ thống không có đánh giá, không bịa.

## Kiểm HTML thật (API 5080 + `next dev` 3000, công thức seed Published `ga-lac-pho-mai-cay`)
Lệnh: `Invoke-WebRequest http://localhost:3000/recipes/ga-lac-pho-mai-cay` rồi trích `<script type="application/ld+json">`.

| Thuộc tính NFR-SEO-001 | Trước sửa | Sau sửa |
|---|---|---|
| name, description, datePublished, prepTime, cookTime, totalTime, recipeYield, recipeIngredient[], recipeInstructions[], nutrition | có | có |
| **image** | **thiếu** (ảnh là đường dẫn tương đối `/images/recipes/...`; `mediaUrl` trả null khi không có `NEXT_PUBLIC_MEDIA_URL`) | có, URL tuyệt đối |
| **author** | **thiếu** (DTO chi tiết chỉ có `authorId`) | có, `Person` |
| recipeCategory (không bắt buộc) | thiếu | có |
| aggregateRating / review | không có | không có |

Lưu ý ISR: layout cache kết quả API 60 s, nên ngay sau khi deploy backend có thể còn thấy bản cũ tới lần làm mới kế tiếp (đã quan sát: bản mới sau ~15 s chờ thêm).

## JSON-LD trích nguyên văn từ HTML sau khi sửa
```json
{
  "@context": "https://schema.org",
  "@type": "Recipe",
  "name": "Gà Lắc Phô Mai Cay",
  "url": "http://localhost:3000/recipes/ga-lac-pho-mai-cay",
  "description": "Miếng gà rút xương chiên vàng giòn rụm lắc đẫm bột phô mai cay kích thích mọi giác quan.",
  "image": [
    "http://localhost:3000/images/recipes/ga-lac-pho-mai-cay.jpg"
  ],
  "author": {
    "@type": "Person",
    "name": "Bếp Trưởng Culinary"
  },
  "datePublished": "2026-09-24T01:54:05.955182Z",
  "prepTime": "PT30M",
  "cookTime": "PT50M",
  "totalTime": "PT80M",
  "recipeYield": "6 khẩu phần",
  "recipeCategory": "Món ăn vặt đường phố",
  "recipeIngredient": [
    "400 g Nguyên liệu chính (Gà)",
    "2 củ Hành tây",
    "50 g Hành lá & Ngò rí",
    "1 củ Tỏi khô",
    "30 g Gừng tươi",
    "2 quả Ớt sừng",
    "3 muỗng canh Nước mắm truyền thống",
    "2 muỗng cà phê Hạt nêm thịt thăn",
    "1 muỗng cà phê Tiêu đen xay",
    "1.5 muỗng canh Đường thốt nốt"
  ],
  "recipeInstructions": [
    {
      "@type": "HowToStep",
      "position": 1,
      "name": "Sơ chế nguyên liệu",
      "text": "Rửa sạch các nguyên liệu tươi cho món Gà Lắc Phô Mai Cay, để ráo nước và cắt thái vừa ăn."
    },
    {
      "@type": "HowToStep",
      "position": 2,
      "name": "Tẩm ướp gia vị",
      "text": "Cho hành, tỏi, nước mắm, tiêu, hạt nêm và đường vào trộn đều cùng nguyên liệu chính trong 20 phút."
    },
    {
      "@type": "HowToStep",
      "position": 3,
      "name": "Phi thơm và tao sơ",
      "text": "Bắc chảo lên bếp, cho dầu ăn vào phi thơm hành tỏi băm, sau đó cho nguyên liệu vào đảo săn trên lửa lớn."
    },
    {
      "@type": "HowToStep",
      "position": 4,
      "name": "Nấu chính và gia nhiệt",
      "text": "Hạ nhỏ lửa, đậy nắp ninh liu riu hoặc đun sôi cho đến khi các nguyên liệu chín mềm và nước sốt sánh lại."
    },
    {
      "@type": "HowToStep",
      "position": 5,
      "name": "Cân chỉnh gia vị",
      "text": "Nếm lại nước sốt và điều chỉnh thêm chút tiêu, ớt và nước mắm cho thật hài hòa, chuẩn vị."
    },
    {
      "@type": "HowToStep",
      "position": 6,
      "name": "Hoàn thiện và thưởng thức",
      "text": "Trình bày món Gà Lắc Phô Mai Cay ra đĩa hoặc tô nóng, rắc hành ngò thái nhỏ và thưởng thức cùng cơm hoặc bún."
    }
  ],
  "nutrition": {
    "@type": "NutritionInformation",
    "calories": "543 kcal",
    "proteinContent": "42 g",
    "carbohydrateContent": "49 g",
    "fatContent": "19 g",
    "fiberContent": "6.5 g",
    "sodiumContent": "841 mg"
  }
}
```

## Test
- Jest `src/frontend/src/lib/__tests__/recipe-jsonld.test.tsx` (8 test): đủ 12 thuộc tính với dữ liệu đúng hình dạng API; ảnh tương đối → tuyệt đối, ảnh chính đứng đầu;
  URL http(s) giữ nguyên, key MinIO thiếu `NEXT_PUBLIC_MEDIA_URL` thì bỏ; author/thời gian ISO 8601/thứ tự nguyên liệu, bước; không có rating dù dữ liệu vào có trường giống rating;
  chặn `</script>`; layout: Published nhúng đúng 1 script, Draft/API lỗi không nhúng. Trước sửa: **2 đỏ / 6 xanh**; sau: 8/8.
- xUnit `tests/CulinaryBlog.Tests/RecipeDetailSeoTests.cs` (HTTP + Postgres thật): công thức Published, khách gọi chi tiết → có `authorName`, `categoryName`, không có `authorEmail`.
  Trước sửa: `[FAIL] thiếu authorName trong DTO chi tiết`; sau: xanh.
- Sau sửa: backend `tests/CulinaryBlog.Tests` **210/210**, Jest **43/43**, `tsc --noEmit` exit 0, `dotnet format --verify-no-changes --severity error` exit 0.

## Thay đổi code
- `src/backend/CulinaryBlog.Application/Recipes.cs`: `RecipeDetailDto` thêm `AuthorName`, `CategoryName` (tham số cuối, mặc định null); `IRecipeDisplayNameReader`;
  `GetRecipeBySlugHandler` nhận reader tuỳ chọn.
- `src/backend/CulinaryBlog.Infrastructure/RecipeDisplayNameReader.cs` (mới): 1 câu SQL (tên danh mục + subquery `DisplayName` tác giả), chỉ cột công khai.
- `src/backend/CulinaryBlog.API/Program.cs`: 1 dòng đăng ký DI cạnh `IMyRecipesRepository`.
- `src/frontend/src/lib/recipe-jsonld.ts`: `absoluteImage`.

## Cần Trung kiểm tay bằng validator ngoài (đã làm ngày 05/10/2026, kết quả ở cuối file)
1. **Schema Markup Validator** (https://validator.schema.org) → tab *Code snippet* → dán khối JSON ở trên (hoặc toàn bộ HTML trang) → ghi số lỗi/cảnh báo: ____
2. **Google Rich Results Test** (https://search.google.com/test/rich-results) → *Code* → dán HTML trang. `localhost` không truy cập được từ Google nên phải dùng chế độ dán mã;
   ảnh `http://localhost:3000/...` có thể bị cảnh báo không tải được — cần URL công khai (staging) mới kiểm được ảnh. Kết quả: ____
3. Dự kiến Google cảnh báo thiếu `aggregateRating`/`video` (tuỳ chọn): **chấp nhận**, vì không có dữ liệu đánh giá và NFR không cho bịa. NFR-SEO-001 ghi "pass 100%" và
   "star rating" — phần "star rating" không đạt được khi chưa có tính năng đánh giá; cần thầy/nhóm xác nhận cách hiểu.

## Kết quả kiểm tay (05/10/2026)

- validator.schema.org (tab Code): Recipe, **0 lỗi, 0 cảnh báo** (ảnh bằng chứng `K19_validator.png`).
- Rich Results Test của Google (tab Code): với địa chỉ `localhost` báo 2 lỗi "URL trong trường image không hợp lệ"; thay `localhost` bằng tên miền công khai (`www.example.com`) thì "Đã phát hiện 1 mục hợp lệ", chỉ còn cảnh báo trường khuyến nghị (`recipeCuisine`, `description`, `aggregateRating`, `keywords`, `video`, `nutrition`). Thiếu `aggregateRating` là cố ý (NFR-SEO-001). Ảnh chụp kết quả không nằm trong repo (TV3 chạy tay).
- JSON-LD thật (Console): `Recipe`, `author` là tên hiển thị, `image` gồm 2 URL tuyệt đối (`K19_jsonld_console.png`), không có `aggregateRating` hay `review`.
