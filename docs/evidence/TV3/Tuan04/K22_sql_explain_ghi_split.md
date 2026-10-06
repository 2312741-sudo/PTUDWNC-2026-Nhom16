# K22 — đếm câu SQL mỗi request + EXPLAIN (ANALYZE, BUFFERS) — 2026-10-02 20:04

Dữ liệu trong DB lúc đo: Recipes=668, RecipeIngredients=882, RecipeSteps=1140, Users=1234

| Request | HTTP | Số lệnh SQL (round-trip) | Số câu trong các lệnh | Tổng ms SQL | Lệnh chậm nhất ms | > 100 ms |
|---|---|---|---|---|---|---|
| GET chi tiết (2 nguyên liệu, 2 bước) | 200 | 5 | 1 + 1 + 1 + 1 + 1 | 8.2 | 2.0 | 0 |
| GET chi tiết (10 nguyên liệu, 6 bước) | 200 | 5 | 1 + 1 + 1 + 1 + 1 | 11.5 | 3.2 | 0 |
| GET dashboard /me/recipes (1 công thức) | 200 | 2 | 1 + 1 | 5.5 | 4.1 | 0 |
| GET dashboard /me/recipes (12 công thức) | 200 | 2 | 1 + 1 | 4.0 | 2.5 | 0 |
| GET dashboard /me/recipes/counts (1 công thức) | 200 | 1 | 1 | 4.1 | 4.1 | 0 |
| GET dashboard /me/recipes/counts (12 công thức) | 200 | 1 | 1 | 1.5 | 1.5 | 0 |
| POST nguyên liệu (đang có 2) | 201 | 4 | 1 + 1 + 1 + 1 | 6.7 | 2.3 | 0 |
| POST nguyên liệu (đang có 10) | 201 | 4 | 1 + 1 + 1 + 1 | 5.6 | 1.8 | 0 |
| DELETE nguyên liệu đầu (còn 2 phải đánh lại thứ tự) | 204 | 4 | 1 + 1 + 1 + 3 | 8.2 | 2.7 | 0 |
| DELETE nguyên liệu đầu (còn 10 phải đánh lại thứ tự) | 204 | 4 | 1 + 1 + 1 + 11 | 8.3 | 2.3 | 0 |
| POST bước (đang có 2) | 201 | 4 | 1 + 1 + 1 + 1 | 8.7 | 3.2 | 0 |
| POST bước (đang có 6) | 201 | 4 | 1 + 1 + 1 + 1 | 5.5 | 2.1 | 0 |
| DELETE bước đầu (còn 2 phải đánh lại số) | 204 | 4 | 1 + 1 + 1 + 3 | 8.0 | 2.5 | 0 |
| DELETE bước đầu (còn 6 phải đánh lại số) | 204 | 4 | 1 + 1 + 1 + 7 | 5.8 | 1.7 | 0 |

## Câu SQL và EXPLAIN (ANALYZE, BUFFERS) — bản lớn của mỗi cặp, chỉ các SELECT

### GET chi tiết (10 nguyên liệu, 6 bước)
- 2.4 ms, 1 câu:
```sql
SELECT r."Id", r."AuthorId", r."CategoryId", r."CookTimeMinutes", r."CreatedAt", r."Description", r."Difficulty", r."Instructions", r."IsDeleted", r."PrepTimeMinutes", r."PublishedAt", r."RowVersion", r."Servings", r."Slug", r."Status", r."Title", r."UpdatedAt", r."Nutrition_Calories", r."Nutrition_Carbohydrates", r."Nutrition_Fat", r."Nutrition_Fiber", r."Nutrition_Protein", r."Nutrition_Sodium"
FROM "Recipes" AS r
WHERE NOT (r."IsDeleted") AND r."Slug" = @slug
ORDER BY r."Id"
LIMIT 1
```
```text
Limit  (cost=8.30..8.31 rows=1 width=315) (actual time=0.069..0.070 rows=1 loops=1)
  Buffers: shared hit=3
  ->  Sort  (cost=8.30..8.31 rows=1 width=315) (actual time=0.068..0.068 rows=1 loops=1)
        Sort Key: "Id"
        Sort Method: quicksort  Memory: 25kB
        Buffers: shared hit=3
        ->  Index Scan using "IDX_Recipe_Slug" on "Recipes" r  (cost=0.28..8.29 rows=1 width=315) (actual time=0.053..0.053 rows=1 loops=1)
              Index Cond: (("Slug")::text = 'k22-10nl-eb4dceab7e7d46dd89aa0'::text)
              Filter: (NOT "IsDeleted")
              Buffers: shared hit=3
Planning Time: 0.705 ms
Execution Time: 0.220 ms
```
- 1.5 ms, 1 câu:
```sql
SELECT r4."Id", r4."CreatedAt", r4."IsDeleted", r4."Name", r4."Notes", r4."OrderIndex", r4."Quantity", r4."RecipeId", r4."RowVersion", r4."Unit", r4."UpdatedAt", r3."Id"
FROM (
    SELECT r."Id"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Slug" = @slug
    ORDER BY r."Id"
    LIMIT 1
) AS r3
INNER JOIN (
    SELECT r0."Id", r0."CreatedAt", r0."IsDeleted", r0."Name", r0."Notes", r0."OrderIndex", r0."Quantity", r0."RecipeId", r0."RowVersion", r0."Unit", r0."UpdatedAt"
    FROM "RecipeIngredients" AS r0
    WHERE NOT (r0."IsDeleted")
) AS r4 ON r3."Id" = r4."RecipeId"
ORDER BY r3."Id"
```
```text
Nested Loop  (cost=12.59..18.64 rows=2 width=128) (actual time=0.131..0.139 rows=10 loops=1)
  Buffers: shared hit=7
  ->  Limit  (cost=8.30..8.31 rows=1 width=16) (actual time=0.086..0.087 rows=1 loops=1)
        Buffers: shared hit=3
        ->  Sort  (cost=8.30..8.31 rows=1 width=16) (actual time=0.085..0.085 rows=1 loops=1)
              Sort Key: r."Id"
              Sort Method: quicksort  Memory: 25kB
              Buffers: shared hit=3
              ->  Index Scan using "IDX_Recipe_Slug" on "Recipes" r  (cost=0.28..8.29 rows=1 width=16) (actual time=0.060..0.067 rows=1 loops=1)
                    Index Cond: (("Slug")::text = 'k22-10nl-eb4dceab7e7d46dd89aa0'::text)
                    Filter: (NOT "IsDeleted")
                    Buffers: shared hit=3
  ->  Bitmap Heap Scan on "RecipeIngredients" r0  (cost=4.29..10.32 rows=2 width=112) (actual time=0.031..0.036 rows=10 loops=1)
        Recheck Cond: ("RecipeId" = r."Id")
        Filter: (NOT "IsDeleted")
        Rows Removed by Filter: 1
        Heap Blocks: exact=2
        Buffers: shared hit=4
        ->  Bitmap Index Scan on "IX_RecipeIngredients_RecipeId_OrderIndex"  (cost=0.00..4.29 rows=2 width=0) (actual time=0.016..0.016 rows=21 loops=1)
              Index Cond: ("RecipeId" = r."Id")
              Buffers: shared hit=2
Planning:
  Buffers: shared hit=8
Planning Time: 0.853 ms
Execution Time: 0.197 ms
```
- 1.9 ms, 1 câu:
```sql
SELECT r6."Id", r6."CreatedAt", r6."Description", r6."ImageUrl", r6."IsDeleted", r6."RecipeId", r6."RowVersion", r6."StepNumber", r6."TimerMinutes", r6."Title", r6."UpdatedAt", r5."Id"
FROM (
    SELECT r."Id"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Slug" = @slug
    ORDER BY r."Id"
    LIMIT 1
) AS r5
INNER JOIN (
    SELECT r1."Id", r1."CreatedAt", r1."Description", r1."ImageUrl", r1."IsDeleted", r1."RecipeId", r1."RowVersion", r1."StepNumber", r1."TimerMinutes", r1."Title", r1."UpdatedAt"
    FROM "RecipeSteps" AS r1
    WHERE NOT (r1."IsDeleted")
) AS r6 ON r5."Id" = r6."RecipeId"
ORDER BY r5."Id"
```
```text
Nested Loop  (cost=12.59..18.83 rows=2 width=634) (actual time=0.044..0.047 rows=6 loops=1)
  Buffers: shared hit=7
  ->  Limit  (cost=8.30..8.31 rows=1 width=16) (actual time=0.018..0.018 rows=1 loops=1)
        Buffers: shared hit=3
        ->  Sort  (cost=8.30..8.31 rows=1 width=16) (actual time=0.017..0.017 rows=1 loops=1)
              Sort Key: r."Id"
              Sort Method: quicksort  Memory: 25kB
              Buffers: shared hit=3
              ->  Index Scan using "IDX_Recipe_Slug" on "Recipes" r  (cost=0.28..8.29 rows=1 width=16) (actual time=0.013..0.014 rows=1 loops=1)
                    Index Cond: (("Slug")::text = 'k22-10nl-eb4dceab7e7d46dd89aa0'::text)
                    Filter: (NOT "IsDeleted")
                    Buffers: shared hit=3
  ->  Bitmap Heap Scan on "RecipeSteps" r1  (cost=4.29..10.51 rows=2 width=618) (actual time=0.016..0.018 rows=6 loops=1)
        Recheck Cond: ((r."Id" = "RecipeId") AND (NOT "IsDeleted"))
        Heap Blocks: exact=2
        Buffers: shared hit=4
        ->  Bitmap Index Scan on "IX_RecipeSteps_RecipeId_StepNumber"  (cost=0.00..4.29 rows=2 width=0) (actual time=0.013..0.013 rows=13 loops=1)
              Index Cond: ("RecipeId" = r."Id")
              Buffers: shared hit=2
Planning Time: 0.250 ms
Execution Time: 0.068 ms
```
- 2.4 ms, 1 câu:
```sql
SELECT r8."Id", r8."AltText", r8."CreatedAt", r8."IsDeleted", r8."IsPrimary", r8."MediumUrl", r8."OrderIndex", r8."OriginalUrl", r8."RecipeId", r8."RowVersion", r8."ThumbnailUrl", r8."UpdatedAt", r7."Id"
FROM (
    SELECT r."Id"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Slug" = @slug
    ORDER BY r."Id"
    LIMIT 1
) AS r7
INNER JOIN (
    SELECT r2."Id", r2."AltText", r2."CreatedAt", r2."IsDeleted", r2."IsPrimary", r2."MediumUrl", r2."OrderIndex", r2."OriginalUrl", r2."RecipeId", r2."RowVersion", r2."ThumbnailUrl", r2."UpdatedAt"
    FROM "RecipeImages" AS r2
    WHERE NOT (r2."IsDeleted")
) AS r8 ON r7."Id" = r8."RecipeId"
ORDER BY r7."Id"
```
```text
Nested Loop  (cost=8.44..16.47 rows=1 width=2068) (actual time=0.027..0.027 rows=0 loops=1)
  Buffers: shared hit=4
  ->  Limit  (cost=8.30..8.31 rows=1 width=16) (actual time=0.016..0.016 rows=1 loops=1)
        Buffers: shared hit=3
        ->  Sort  (cost=8.30..8.31 rows=1 width=16) (actual time=0.016..0.016 rows=1 loops=1)
              Sort Key: r."Id"
              Sort Method: quicksort  Memory: 25kB
              Buffers: shared hit=3
              ->  Index Scan using "IDX_Recipe_Slug" on "Recipes" r  (cost=0.28..8.29 rows=1 width=16) (actual time=0.013..0.013 rows=1 loops=1)
                    Index Cond: (("Slug")::text = 'k22-10nl-eb4dceab7e7d46dd89aa0'::text)
                    Filter: (NOT "IsDeleted")
                    Buffers: shared hit=3
  ->  Index Scan using "IX_RecipeImages_RecipeId_OrderIndex" on "RecipeImages" r2  (cost=0.14..8.15 rows=1 width=2052) (actual time=0.010..0.010 rows=0 loops=1)
        Index Cond: ("RecipeId" = r."Id")
        Filter: (NOT "IsDeleted")
        Buffers: shared hit=1
Planning Time: 0.180 ms
Execution Time: 0.042 ms
```
- 3.2 ms, 1 câu:
```sql
SELECT (
    SELECT a."DisplayName"
    FROM "AspNetUsers" AS a
    WHERE a."Id" = r."AuthorId"
    LIMIT 1) AS "Author", (
    SELECT c."Name"
    FROM "Categories" AS c
    WHERE NOT (c."IsDeleted") AND c."Id" = r."CategoryId"
    LIMIT 1) AS "Category"
FROM "Recipes" AS r
WHERE NOT (r."IsDeleted") AND r."Id" = @recipeId
LIMIT 1
```
```text
Limit  (cost=0.28..24.75 rows=1 width=436) (actual time=0.046..0.046 rows=1 loops=1)
  Buffers: shared hit=8
  ->  Index Scan using "PK_Recipes" on "Recipes" r  (cost=0.28..24.75 rows=1 width=436) (actual time=0.045..0.046 rows=1 loops=1)
        Index Cond: ("Id" = 'e1d3f3e0-6308-4836-b5f4-f5a41a885215'::uuid)
        Filter: (NOT "IsDeleted")
        Buffers: shared hit=8
        SubPlan 1
          ->  Limit  (cost=0.28..8.29 rows=1 width=21) (actual time=0.016..0.017 rows=1 loops=1)
                Buffers: shared hit=3
                ->  Index Scan using "PK_AspNetUsers" on "AspNetUsers" a  (cost=0.28..8.29 rows=1 width=21) (actual time=0.016..0.016 rows=1 loops=1)
                      Index Cond: ("Id" = (r."AuthorId")::text)
                      Buffers: shared hit=3
        SubPlan 2
          ->  Limit  (cost=0.14..8.16 rows=1 width=218) (actual time=0.007..0.007 rows=1 loops=1)
                Buffers: shared hit=2
                ->  Index Scan using "PK_Categories" on "Categories" c  (cost=0.14..8.16 rows=1 width=218) (actual time=0.006..0.006 rows=1 loops=1)
                      Index Cond: ("Id" = r."CategoryId")
                      Filter: (NOT "IsDeleted")
                      Buffers: shared hit=2
Planning Time: 0.177 ms
Execution Time: 0.068 ms
```

### GET dashboard /me/recipes (12 công thức)
- 1.5 ms, 1 câu:
```sql
SELECT count(*)::int
FROM "Recipes" AS r
WHERE NOT (r."IsDeleted") AND r."AuthorId" = @query_AuthorId
```
```text
Aggregate  (cost=8.29..8.31 rows=1 width=4) (actual time=0.035..0.035 rows=1 loops=1)
  Buffers: shared hit=3
  ->  Index Scan using "IDX_Recipe_AuthorId" on "Recipes" r  (cost=0.28..8.29 rows=1 width=0) (actual time=0.027..0.030 rows=12 loops=1)
        Index Cond: (("AuthorId")::text = '77a4da24-e72d-4d13-a2f2-50b6526de7da'::text)
        Filter: (NOT "IsDeleted")
        Buffers: shared hit=3
Planning Time: 0.167 ms
Execution Time: 0.058 ms
```
- 2.5 ms, 1 câu:
```sql
SELECT r."Id", r."Title", r."Slug", r."CategoryId", COALESCE((
    SELECT c."Name"
    FROM "Categories" AS c
    WHERE NOT (c."IsDeleted") AND c."Id" = r."CategoryId"
    LIMIT 1), 'Khác') AS "CategoryName", r."PrepTimeMinutes", r."CookTimeMinutes", r."Servings", r."Difficulty", r."Status", (
    SELECT r0."OriginalUrl"
    FROM "RecipeImages" AS r0
    WHERE NOT (r0."IsDeleted") AND r."Id" = r0."RecipeId" AND r0."IsPrimary"
    LIMIT 1) AS "PrimaryImageUrl", (
    SELECT count(*)::int
    FROM "RecipeIngredients" AS r1
    WHERE NOT (r1."IsDeleted") AND r."Id" = r1."RecipeId") AS "IngredientCount", (
    SELECT count(*)::int
    FROM "RecipeSteps" AS r2
    WHERE NOT (r2."IsDeleted") AND r."Id" = r2."RecipeId") AS "StepCount", r."PublishedAt", r."CreatedAt", r."UpdatedAt", r."RowVersion"
FROM "Recipes" AS r
WHERE NOT (r."IsDeleted") AND r."AuthorId" = @query_AuthorId
ORDER BY COALESCE(r."UpdatedAt", r."CreatedAt") DESC, r."Id"
LIMIT @p2 OFFSET @p
```
```text
Limit  (cost=8.30..39.29 rows=1 width=716) (actual time=0.170..0.293 rows=12 loops=1)
  Buffers: shared hit=115
  ->  Result  (cost=8.30..39.29 rows=1 width=716) (actual time=0.169..0.291 rows=12 loops=1)
        Buffers: shared hit=115
        ->  Sort  (cost=8.30..8.31 rows=1 width=160) (actual time=0.085..0.086 rows=12 loops=1)
              Sort Key: (COALESCE(r."UpdatedAt", r."CreatedAt")) DESC, r."Id"
              Sort Method: quicksort  Memory: 28kB
              Buffers: shared hit=6
              ->  Index Scan using "IDX_Recipe_AuthorId" on "Recipes" r  (cost=0.28..8.29 rows=1 width=160) (actual time=0.032..0.038 rows=12 loops=1)
                    Index Cond: (("AuthorId")::text = '77a4da24-e72d-4d13-a2f2-50b6526de7da'::text)
                    Filter: (NOT "IsDeleted")
                    Buffers: shared hit=3
        SubPlan 1
          ->  Limit  (cost=0.14..8.16 rows=1 width=218) (actual time=0.002..0.002 rows=1 loops=12)
                Buffers: shared hit=24
                ->  Index Scan using "PK_Categories" on "Categories" c  (cost=0.14..8.16 rows=1 width=218) (actual time=0.002..0.002 rows=1 loops=12)
                      Index Cond: ("Id" = r."CategoryId")
                      Filter: (NOT "IsDeleted")
                      Buffers: shared hit=24
        SubPlan 2
          ->  Limit  (cost=0.14..8.15 rows=1 width=516) (actual time=0.002..0.002 rows=0 loops=12)
                Buffers: shared hit=12
                ->  Index Scan using ux_recipe_images_one_primary on "RecipeImages" r0  (cost=0.14..8.15 rows=1 width=516) (actual time=0.001..0.001 rows=0 loops=12)
                      Index Cond: ("RecipeId" = r."Id")
                      Filter: (NOT "IsDeleted")
                      Buffers: shared hit=12
        SubPlan 3
          ->  Aggregate  (cost=10.32..10.33 rows=1 width=4) (actual time=0.007..0.007 rows=1 loops=12)
                Buffers: shared hit=36
                ->  Bitmap Heap Scan on "RecipeIngredients" r1  (cost=4.29..10.32 rows=2 width=0) (actual time=0.004..0.004 rows=1 loops=12)
                      Recheck Cond: (r."Id" = "RecipeId")
                      Filter: (NOT "IsDeleted")
                      Heap Blocks: exact=12
                      Buffers: shared hit=36
                      ->  Bitmap Index Scan on "IX_RecipeIngredients_RecipeId_OrderIndex"  (cost=0.00..4.29 rows=2 width=0) (actual time=0.003..0.003 rows=1 loops=12)
                            Index Cond: ("RecipeId" = r."Id")
                            Buffers: shared hit=24
        SubPlan 4
          ->  Aggregate  (cost=4.31..4.33 rows=1 width=4) (actual time=0.005..0.005 rows=1 loops=12)
                Buffers: shared hit=37
                ->  Index Only Scan using "IX_RecipeSteps_RecipeId_StepNumber" on "RecipeSteps" r2  (cost=0.28..4.31 rows=2 width=0) (actual time=0.004..0.004 rows=1 loops=12)
                      Index Cond: ("RecipeId" = r."Id")
                      Heap Fetches: 12
                      Buffers: shared hit=37
Planning:
  Buffers: shared hit=5
Planning Time: 0.449 ms
Execution Time: 0.424 ms
```

### GET dashboard /me/recipes/counts (12 công thức)
- 1.5 ms, 1 câu:
```sql
SELECT r."Status", count(*)::int AS "Count"
FROM "Recipes" AS r
WHERE NOT (r."IsDeleted") AND r."AuthorId" = @authorId
GROUP BY r."Status"
```
```text
GroupAggregate  (cost=8.30..8.32 rows=1 width=6) (actual time=0.063..0.064 rows=1 loops=1)
  Group Key: "Status"
  Buffers: shared hit=3
  ->  Sort  (cost=8.30..8.31 rows=1 width=2) (actual time=0.057..0.057 rows=12 loops=1)
        Sort Key: "Status"
        Sort Method: quicksort  Memory: 25kB
        Buffers: shared hit=3
        ->  Index Scan using "IDX_Recipe_AuthorId" on "Recipes" r  (cost=0.28..8.29 rows=1 width=2) (actual time=0.036..0.043 rows=12 loops=1)
              Index Cond: (("AuthorId")::text = '77a4da24-e72d-4d13-a2f2-50b6526de7da'::text)
              Filter: (NOT "IsDeleted")
              Buffers: shared hit=3
Planning Time: 0.261 ms
Execution Time: 0.095 ms
```

### POST nguyên liệu (đang có 10)
- 1.5 ms, 1 câu:
```sql
SELECT r."Id", r."AuthorId", r."CategoryId", r."CookTimeMinutes", r."CreatedAt", r."Description", r."Difficulty", r."Instructions", r."IsDeleted", r."PrepTimeMinutes", r."PublishedAt", r."RowVersion", r."Servings", r."Slug", r."Status", r."Title", r."UpdatedAt", r."Nutrition_Calories", r."Nutrition_Carbohydrates", r."Nutrition_Fat", r."Nutrition_Fiber", r."Nutrition_Protein", r."Nutrition_Sodium"
FROM "Recipes" AS r
WHERE NOT (r."IsDeleted") AND r."Id" = @id
ORDER BY r."Id"
LIMIT 1
```
```text
Limit  (cost=0.28..8.29 rows=1 width=315) (actual time=0.013..0.013 rows=1 loops=1)
  Buffers: shared hit=3
  ->  Index Scan using "PK_Recipes" on "Recipes" r  (cost=0.28..8.29 rows=1 width=315) (actual time=0.012..0.012 rows=1 loops=1)
        Index Cond: ("Id" = 'e1d3f3e0-6308-4836-b5f4-f5a41a885215'::uuid)
        Filter: (NOT "IsDeleted")
        Buffers: shared hit=3
Planning Time: 0.153 ms
Execution Time: 0.031 ms
```
- 1.0 ms, 1 câu:
```sql
SELECT r3."Id", r3."CreatedAt", r3."IsDeleted", r3."Name", r3."Notes", r3."OrderIndex", r3."Quantity", r3."RecipeId", r3."RowVersion", r3."Unit", r3."UpdatedAt", r2."Id"
FROM (
    SELECT r."Id"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Id" = @id
    ORDER BY r."Id"
    LIMIT 1
) AS r2
INNER JOIN (
    SELECT r0."Id", r0."CreatedAt", r0."IsDeleted", r0."Name", r0."Notes", r0."OrderIndex", r0."Quantity", r0."RecipeId", r0."RowVersion", r0."Unit", r0."UpdatedAt"
    FROM "RecipeIngredients" AS r0
    WHERE NOT (r0."IsDeleted")
) AS r3 ON r2."Id" = r3."RecipeId"
ORDER BY r2."Id"
```
```text
Sort  (cost=18.64..18.64 rows=2 width=128) (actual time=0.058..0.059 rows=10 loops=1)
  Sort Key: r0."RecipeId"
  Sort Method: quicksort  Memory: 26kB
  Buffers: shared hit=7
  ->  Nested Loop  (cost=4.57..18.63 rows=2 width=128) (actual time=0.026..0.031 rows=10 loops=1)
        Buffers: shared hit=7
        ->  Limit  (cost=0.28..8.29 rows=1 width=16) (actual time=0.009..0.009 rows=1 loops=1)
              Buffers: shared hit=3
              ->  Index Scan using "PK_Recipes" on "Recipes" r  (cost=0.28..8.29 rows=1 width=16) (actual time=0.008..0.008 rows=1 loops=1)
                    Index Cond: ("Id" = 'e1d3f3e0-6308-4836-b5f4-f5a41a885215'::uuid)
                    Filter: (NOT "IsDeleted")
                    Buffers: shared hit=3
        ->  Bitmap Heap Scan on "RecipeIngredients" r0  (cost=4.29..10.32 rows=2 width=112) (actual time=0.012..0.016 rows=10 loops=1)
              Recheck Cond: ("RecipeId" = r."Id")
              Filter: (NOT "IsDeleted")
              Rows Removed by Filter: 1
              Heap Blocks: exact=2
              Buffers: shared hit=4
              ->  Bitmap Index Scan on "IX_RecipeIngredients_RecipeId_OrderIndex"  (cost=0.00..4.29 rows=2 width=0) (actual time=0.006..0.006 rows=21 loops=1)
                    Index Cond: ("RecipeId" = r."Id")
                    Buffers: shared hit=2
Planning:
  Buffers: shared hit=8
Planning Time: 0.299 ms
Execution Time: 0.086 ms
```
- 1.3 ms, 1 câu:
```sql
SELECT r5."Id", r5."CreatedAt", r5."Description", r5."ImageUrl", r5."IsDeleted", r5."RecipeId", r5."RowVersion", r5."StepNumber", r5."TimerMinutes", r5."Title", r5."UpdatedAt", r4."Id"
FROM (
    SELECT r."Id"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Id" = @id
    ORDER BY r."Id"
    LIMIT 1
) AS r4
INNER JOIN (
    SELECT r1."Id", r1."CreatedAt", r1."Description", r1."ImageUrl", r1."IsDeleted", r1."RecipeId", r1."RowVersion", r1."StepNumber", r1."TimerMinutes", r1."Title", r1."UpdatedAt"
    FROM "RecipeSteps" AS r1
    WHERE NOT (r1."IsDeleted")
) AS r5 ON r4."Id" = r5."RecipeId"
ORDER BY r4."Id"
```
```text
Sort  (cost=18.83..18.83 rows=2 width=634) (actual time=0.040..0.041 rows=6 loops=1)
  Sort Key: r1."RecipeId"
  Sort Method: quicksort  Memory: 25kB
  Buffers: shared hit=7
  ->  Nested Loop  (cost=4.57..18.82 rows=2 width=634) (actual time=0.030..0.034 rows=6 loops=1)
        Buffers: shared hit=7
        ->  Limit  (cost=0.28..8.29 rows=1 width=16) (actual time=0.010..0.010 rows=1 loops=1)
              Buffers: shared hit=3
              ->  Index Scan using "PK_Recipes" on "Recipes" r  (cost=0.28..8.29 rows=1 width=16) (actual time=0.009..0.009 rows=1 loops=1)
                    Index Cond: ("Id" = 'e1d3f3e0-6308-4836-b5f4-f5a41a885215'::uuid)
                    Filter: (NOT "IsDeleted")
                    Buffers: shared hit=3
        ->  Bitmap Heap Scan on "RecipeSteps" r1  (cost=4.29..10.51 rows=2 width=618) (actual time=0.015..0.018 rows=6 loops=1)
              Recheck Cond: ((r."Id" = "RecipeId") AND (NOT "IsDeleted"))
              Heap Blocks: exact=2
              Buffers: shared hit=4
              ->  Bitmap Index Scan on "IX_RecipeSteps_RecipeId_StepNumber"  (cost=0.00..4.29 rows=2 width=0) (actual time=0.010..0.010 rows=13 loops=1)
                    Index Cond: ("RecipeId" = r."Id")
                    Buffers: shared hit=2
Planning Time: 0.249 ms
Execution Time: 0.068 ms
```
- 1.8 ms, 1 câu:
```sql
INSERT INTO "RecipeIngredients" ("Id", "CreatedAt", "IsDeleted", "Name", "Notes", "OrderIndex", "Quantity", "RecipeId", "RowVersion", "Unit", "UpdatedAt")
VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10);
```

### DELETE nguyên liệu đầu (còn 10 phải đánh lại thứ tự)
- 2.2 ms, 1 câu:
```sql
SELECT r."Id", r."AuthorId", r."CategoryId", r."CookTimeMinutes", r."CreatedAt", r."Description", r."Difficulty", r."Instructions", r."IsDeleted", r."PrepTimeMinutes", r."PublishedAt", r."RowVersion", r."Servings", r."Slug", r."Status", r."Title", r."UpdatedAt", r."Nutrition_Calories", r."Nutrition_Carbohydrates", r."Nutrition_Fat", r."Nutrition_Fiber", r."Nutrition_Protein", r."Nutrition_Sodium"
FROM "Recipes" AS r
WHERE NOT (r."IsDeleted") AND r."Id" = @id
ORDER BY r."Id"
LIMIT 1
```
- 1.6 ms, 1 câu:
```sql
SELECT r3."Id", r3."CreatedAt", r3."IsDeleted", r3."Name", r3."Notes", r3."OrderIndex", r3."Quantity", r3."RecipeId", r3."RowVersion", r3."Unit", r3."UpdatedAt", r2."Id"
FROM (
    SELECT r."Id"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Id" = @id
    ORDER BY r."Id"
    LIMIT 1
) AS r2
INNER JOIN (
    SELECT r0."Id", r0."CreatedAt", r0."IsDeleted", r0."Name", r0."Notes", r0."OrderIndex", r0."Quantity", r0."RecipeId", r0."RowVersion", r0."Unit", r0."UpdatedAt"
    FROM "RecipeIngredients" AS r0
    WHERE NOT (r0."IsDeleted")
) AS r3 ON r2."Id" = r3."RecipeId"
ORDER BY r2."Id"
```
- 2.3 ms, 1 câu:
```sql
SELECT r5."Id", r5."CreatedAt", r5."Description", r5."ImageUrl", r5."IsDeleted", r5."RecipeId", r5."RowVersion", r5."StepNumber", r5."TimerMinutes", r5."Title", r5."UpdatedAt", r4."Id"
FROM (
    SELECT r."Id"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Id" = @id
    ORDER BY r."Id"
    LIMIT 1
) AS r4
INNER JOIN (
    SELECT r1."Id", r1."CreatedAt", r1."Description", r1."ImageUrl", r1."IsDeleted", r1."RecipeId", r1."RowVersion", r1."StepNumber", r1."TimerMinutes", r1."Title", r1."UpdatedAt"
    FROM "RecipeSteps" AS r1
    WHERE NOT (r1."IsDeleted")
) AS r5 ON r4."Id" = r5."RecipeId"
ORDER BY r4."Id"
```
- 2.3 ms, 11 câu:
```sql
UPDATE "RecipeIngredients" SET "OrderIndex" = @p0, "RowVersion" = @p1, "UpdatedAt" = @p2
WHERE "Id" = @p3 AND "RowVersion" = @p4;
UPDATE "RecipeIngredients" SET "OrderIndex" = @p5, "RowVersion" = @p6, "UpdatedAt" = @p7
WHERE "Id" = @p8 AND "RowVersion" = @p9;
UPDATE "RecipeIngredients" SET "OrderIndex" = @p10, "RowVersion" = @p11, "UpdatedAt" = @p12
WHERE "Id" = @p13 AND "RowVersion" = @p14;
UPDATE "RecipeIngredients" SET "OrderIndex" = @p15, "RowVersion" = @p16, "UpdatedAt" = @p17
WHERE "Id" = @p18 AND "RowVersion" = @p19;
UPDATE "RecipeIngredients" SET "OrderIndex" = @p20, "RowVersion" = @p21, "UpdatedAt" = @p22
WHERE "Id" = @p23 AND "RowVersion" = @p24;
UPDATE "RecipeIngredients" SET "CreatedAt" = @p25, "IsDeleted" = @p26, "Name" = @p27, "Notes" = @p28, "OrderIndex" = @p29, "Quantity" = @p30, "RecipeId" = @p31, "RowVersion" = @p32, "Unit" = @p33, "UpdatedAt" = @p34
WHERE "Id" = @p35 AND "RowVersion" = @p36;
UPDATE "RecipeIngredients" SET "OrderIndex" = @p37, "RowVersion" = @p38, "UpdatedAt" = @p39
WHERE "Id" = @p40 AND "RowVersion" = @p41;
UPDATE "RecipeIngredients" SET "OrderIndex" = @p42, "RowVersion" = @p43, "UpdatedAt" = @p44
WHERE "Id" = @p45 AND "RowVersion" = @p46;
UPDATE "RecipeIngredients" SET "OrderIndex" = @p47, "RowVersion" = @p48, "UpdatedAt" = @p49
WHERE "Id" = @p50 AND "RowVersion" = @p51;
UPDATE "RecipeIngredients" SET "OrderIndex" = @p52, "RowVersion" = @p53, "UpdatedAt" = @p54
WHERE "Id" = @p55 AND "RowVersion" = @p56;
UPDATE "RecipeIn …
```

### POST bước (đang có 6)
- 1.3 ms, 1 câu:
```sql
SELECT r."Id", r."AuthorId", r."CategoryId", r."CookTimeMinutes", r."CreatedAt", r."Description", r."Difficulty", r."Instructions", r."IsDeleted", r."PrepTimeMinutes", r."PublishedAt", r."RowVersion", r."Servings", r."Slug", r."Status", r."Title", r."UpdatedAt", r."Nutrition_Calories", r."Nutrition_Carbohydrates", r."Nutrition_Fat", r."Nutrition_Fiber", r."Nutrition_Protein", r."Nutrition_Sodium"
FROM "Recipes" AS r
WHERE NOT (r."IsDeleted") AND r."Id" = @id
ORDER BY r."Id"
LIMIT 1
```
- 0.9 ms, 1 câu:
```sql
SELECT r3."Id", r3."CreatedAt", r3."IsDeleted", r3."Name", r3."Notes", r3."OrderIndex", r3."Quantity", r3."RecipeId", r3."RowVersion", r3."Unit", r3."UpdatedAt", r2."Id"
FROM (
    SELECT r."Id"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Id" = @id
    ORDER BY r."Id"
    LIMIT 1
) AS r2
INNER JOIN (
    SELECT r0."Id", r0."CreatedAt", r0."IsDeleted", r0."Name", r0."Notes", r0."OrderIndex", r0."Quantity", r0."RecipeId", r0."RowVersion", r0."Unit", r0."UpdatedAt"
    FROM "RecipeIngredients" AS r0
    WHERE NOT (r0."IsDeleted")
) AS r3 ON r2."Id" = r3."RecipeId"
ORDER BY r2."Id"
```
- 1.3 ms, 1 câu:
```sql
SELECT r5."Id", r5."CreatedAt", r5."Description", r5."ImageUrl", r5."IsDeleted", r5."RecipeId", r5."RowVersion", r5."StepNumber", r5."TimerMinutes", r5."Title", r5."UpdatedAt", r4."Id"
FROM (
    SELECT r."Id"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Id" = @id
    ORDER BY r."Id"
    LIMIT 1
) AS r4
INNER JOIN (
    SELECT r1."Id", r1."CreatedAt", r1."Description", r1."ImageUrl", r1."IsDeleted", r1."RecipeId", r1."RowVersion", r1."StepNumber", r1."TimerMinutes", r1."Title", r1."UpdatedAt"
    FROM "RecipeSteps" AS r1
    WHERE NOT (r1."IsDeleted")
) AS r5 ON r4."Id" = r5."RecipeId"
ORDER BY r4."Id"
```
- 2.1 ms, 1 câu:
```sql
INSERT INTO "RecipeSteps" ("Id", "CreatedAt", "Description", "ImageUrl", "IsDeleted", "RecipeId", "RowVersion", "StepNumber", "TimerMinutes", "Title", "UpdatedAt")
VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10);
```

### DELETE bước đầu (còn 6 phải đánh lại số)
- 1.5 ms, 1 câu:
```sql
SELECT r."Id", r."AuthorId", r."CategoryId", r."CookTimeMinutes", r."CreatedAt", r."Description", r."Difficulty", r."Instructions", r."IsDeleted", r."PrepTimeMinutes", r."PublishedAt", r."RowVersion", r."Servings", r."Slug", r."Status", r."Title", r."UpdatedAt", r."Nutrition_Calories", r."Nutrition_Carbohydrates", r."Nutrition_Fat", r."Nutrition_Fiber", r."Nutrition_Protein", r."Nutrition_Sodium"
FROM "Recipes" AS r
WHERE NOT (r."IsDeleted") AND r."Id" = @id
ORDER BY r."Id"
LIMIT 1
```
- 1.3 ms, 1 câu:
```sql
SELECT r3."Id", r3."CreatedAt", r3."IsDeleted", r3."Name", r3."Notes", r3."OrderIndex", r3."Quantity", r3."RecipeId", r3."RowVersion", r3."Unit", r3."UpdatedAt", r2."Id"
FROM (
    SELECT r."Id"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Id" = @id
    ORDER BY r."Id"
    LIMIT 1
) AS r2
INNER JOIN (
    SELECT r0."Id", r0."CreatedAt", r0."IsDeleted", r0."Name", r0."Notes", r0."OrderIndex", r0."Quantity", r0."RecipeId", r0."RowVersion", r0."Unit", r0."UpdatedAt"
    FROM "RecipeIngredients" AS r0
    WHERE NOT (r0."IsDeleted")
) AS r3 ON r2."Id" = r3."RecipeId"
ORDER BY r2."Id"
```
- 1.2 ms, 1 câu:
```sql
SELECT r5."Id", r5."CreatedAt", r5."Description", r5."ImageUrl", r5."IsDeleted", r5."RecipeId", r5."RowVersion", r5."StepNumber", r5."TimerMinutes", r5."Title", r5."UpdatedAt", r4."Id"
FROM (
    SELECT r."Id"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Id" = @id
    ORDER BY r."Id"
    LIMIT 1
) AS r4
INNER JOIN (
    SELECT r1."Id", r1."CreatedAt", r1."Description", r1."ImageUrl", r1."IsDeleted", r1."RecipeId", r1."RowVersion", r1."StepNumber", r1."TimerMinutes", r1."Title", r1."UpdatedAt"
    FROM "RecipeSteps" AS r1
    WHERE NOT (r1."IsDeleted")
) AS r5 ON r4."Id" = r5."RecipeId"
ORDER BY r4."Id"
```
- 1.7 ms, 7 câu:
```sql
UPDATE "RecipeSteps" SET "CreatedAt" = @p0, "Description" = @p1, "ImageUrl" = @p2, "IsDeleted" = @p3, "RecipeId" = @p4, "RowVersion" = @p5, "StepNumber" = @p6, "TimerMinutes" = @p7, "Title" = @p8, "UpdatedAt" = @p9
WHERE "Id" = @p10 AND "RowVersion" = @p11;
UPDATE "RecipeSteps" SET "RowVersion" = @p12, "StepNumber" = @p13, "UpdatedAt" = @p14
WHERE "Id" = @p15 AND "RowVersion" = @p16;
UPDATE "RecipeSteps" SET "RowVersion" = @p17, "StepNumber" = @p18, "UpdatedAt" = @p19
WHERE "Id" = @p20 AND "RowVersion" = @p21;
UPDATE "RecipeSteps" SET "RowVersion" = @p22, "StepNumber" = @p23, "UpdatedAt" = @p24
WHERE "Id" = @p25 AND "RowVersion" = @p26;
UPDATE "RecipeSteps" SET "RowVersion" = @p27, "StepNumber" = @p28, "UpdatedAt" = @p29
WHERE "Id" = @p30 AND "RowVersion" = @p31;
UPDATE "RecipeSteps" SET "RowVersion" = @p32, "StepNumber" = @p33, "UpdatedAt" = @p34
WHERE "Id" = @p35 AND "RowVersion" = @p36;
UPDATE "RecipeSteps" SET "RowVersion" = @p37, "StepNumber" = @p38, "UpdatedAt" = @p39
WHERE "Id" = @p40 AND "RowVersion" = @p41;
```
