# K22 — đếm câu SQL mỗi request + EXPLAIN (ANALYZE, BUFFERS) — 2026-10-02 18:23

Dữ liệu trong DB lúc đo: Recipes=510, RecipeIngredients=628, RecipeSteps=880, Users=1058

| Request | HTTP | Số lệnh SQL (round-trip) | Số câu trong các lệnh | Tổng ms SQL | Lệnh chậm nhất ms | > 100 ms |
|---|---|---|---|---|---|---|
| GET chi tiết (2 nguyên liệu, 2 bước) | 200 | 5 | 1 + 1 + 1 + 1 + 1 | 4.1 | 0.9 | 0 |
| GET chi tiết (10 nguyên liệu, 6 bước) | 200 | 5 | 1 + 1 + 1 + 1 + 1 | 2.4 | 0.7 | 0 |
| GET dashboard /me/recipes (1 công thức) | 200 | 2 | 1 + 1 | 1.9 | 1.3 | 0 |
| GET dashboard /me/recipes (12 công thức) | 200 | 2 | 1 + 1 | 1.5 | 0.8 | 0 |
| GET dashboard /me/recipes/counts (1 công thức) | 200 | 1 | 1 | 0.7 | 0.7 | 0 |
| GET dashboard /me/recipes/counts (12 công thức) | 200 | 1 | 1 | 0.6 | 0.6 | 0 |
| POST nguyên liệu (đang có 2) | 201 | 2 | 1 + 1 | 1.8 | 1.1 | 0 |
| POST nguyên liệu (đang có 10) | 201 | 2 | 1 + 1 | 1.6 | 1.0 | 0 |
| DELETE nguyên liệu đầu (còn 2 phải đánh lại thứ tự) | 204 | 2 | 1 + 3 | 2.0 | 1.2 | 0 |
| DELETE nguyên liệu đầu (còn 10 phải đánh lại thứ tự) | 204 | 2 | 1 + 11 | 2.4 | 1.3 | 0 |
| POST bước (đang có 2) | 201 | 2 | 1 + 1 | 1.7 | 1.1 | 0 |
| POST bước (đang có 6) | 201 | 2 | 1 + 1 | 1.7 | 1.1 | 0 |
| DELETE bước đầu (còn 2 phải đánh lại số) | 204 | 2 | 1 + 3 | 1.7 | 1.0 | 0 |
| DELETE bước đầu (còn 6 phải đánh lại số) | 204 | 2 | 1 + 7 | 2.3 | 1.5 | 0 |

## Câu SQL và EXPLAIN (ANALYZE, BUFFERS) — bản lớn của mỗi cặp, chỉ các SELECT

### GET chi tiết (10 nguyên liệu, 6 bước)
- 0.7 ms, 1 câu:
```sql
SELECT r."Id", r."AuthorId", r."CategoryId", r."CookTimeMinutes", r."CreatedAt", r."Description", r."Difficulty", r."Instructions", r."IsDeleted", r."PrepTimeMinutes", r."PublishedAt", r."RowVersion", r."Servings", r."Slug", r."Status", r."Title", r."UpdatedAt", r."Nutrition_Calories", r."Nutrition_Carbohydrates", r."Nutrition_Fat", r."Nutrition_Fiber", r."Nutrition_Protein", r."Nutrition_Sodium"
FROM "Recipes" AS r
WHERE NOT (r."IsDeleted") AND r."Slug" = @slug
ORDER BY r."Id"
LIMIT 1
```
```text
Limit  (cost=8.30..8.31 rows=1 width=318) (actual time=0.032..0.032 rows=1 loops=1)
  Buffers: shared hit=3
  ->  Sort  (cost=8.30..8.31 rows=1 width=318) (actual time=0.031..0.031 rows=1 loops=1)
        Sort Key: "Id"
        Sort Method: quicksort  Memory: 25kB
        Buffers: shared hit=3
        ->  Index Scan using "IDX_Recipe_Slug" on "Recipes" r  (cost=0.27..8.29 rows=1 width=318) (actual time=0.025..0.026 rows=1 loops=1)
              Index Cond: (("Slug")::text = 'k22-10nl-6672d4837fe74e15ad395'::text)
              Filter: (NOT "IsDeleted")
              Buffers: shared hit=3
Planning Time: 0.138 ms
Execution Time: 0.050 ms
```
- 0.5 ms, 1 câu:
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
Nested Loop  (cost=12.59..18.19 rows=2 width=128) (actual time=0.025..0.027 rows=10 loops=1)
  Buffers: shared hit=6
  ->  Limit  (cost=8.30..8.31 rows=1 width=16) (actual time=0.017..0.017 rows=1 loops=1)
        Buffers: shared hit=3
        ->  Sort  (cost=8.30..8.31 rows=1 width=16) (actual time=0.016..0.016 rows=1 loops=1)
              Sort Key: r."Id"
              Sort Method: quicksort  Memory: 25kB
              Buffers: shared hit=3
              ->  Index Scan using "IDX_Recipe_Slug" on "Recipes" r  (cost=0.27..8.29 rows=1 width=16) (actual time=0.013..0.013 rows=1 loops=1)
                    Index Cond: (("Slug")::text = 'k22-10nl-6672d4837fe74e15ad395'::text)
                    Filter: (NOT "IsDeleted")
                    Buffers: shared hit=3
  ->  Bitmap Heap Scan on "RecipeIngredients" r0  (cost=4.29..9.87 rows=2 width=112) (actual time=0.006..0.007 rows=10 loops=1)
        Recheck Cond: ("RecipeId" = r."Id")
        Filter: (NOT "IsDeleted")
        Rows Removed by Filter: 1
        Heap Blocks: exact=1
        Buffers: shared hit=3
        ->  Bitmap Index Scan on "IX_RecipeIngredients_RecipeId_OrderIndex"  (cost=0.00..4.29 rows=2 width=0) (actual time=0.002..0.002 rows=21 loops=1)
              Index Cond: ("RecipeId" = r."Id")
              Buffers: shared hit=2
Planning:
  Buffers: shared hit=8
Planning Time: 0.197 ms
Execution Time: 0.048 ms
```
- 0.4 ms, 1 câu:
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
Nested Loop  (cost=12.59..18.64 rows=2 width=634) (actual time=0.023..0.025 rows=6 loops=1)
  Buffers: shared hit=6
  ->  Limit  (cost=8.30..8.31 rows=1 width=16) (actual time=0.014..0.014 rows=1 loops=1)
        Buffers: shared hit=3
        ->  Sort  (cost=8.30..8.31 rows=1 width=16) (actual time=0.013..0.013 rows=1 loops=1)
              Sort Key: r."Id"
              Sort Method: quicksort  Memory: 25kB
              Buffers: shared hit=3
              ->  Index Scan using "IDX_Recipe_Slug" on "Recipes" r  (cost=0.27..8.29 rows=1 width=16) (actual time=0.011..0.012 rows=1 loops=1)
                    Index Cond: (("Slug")::text = 'k22-10nl-6672d4837fe74e15ad395'::text)
                    Filter: (NOT "IsDeleted")
                    Buffers: shared hit=3
  ->  Bitmap Heap Scan on "RecipeSteps" r1  (cost=4.29..10.32 rows=2 width=618) (actual time=0.007..0.007 rows=6 loops=1)
        Recheck Cond: ((r."Id" = "RecipeId") AND (NOT "IsDeleted"))
        Heap Blocks: exact=1
        Buffers: shared hit=3
        ->  Bitmap Index Scan on "IX_RecipeSteps_RecipeId_StepNumber"  (cost=0.00..4.29 rows=2 width=0) (actual time=0.004..0.004 rows=13 loops=1)
              Index Cond: ("RecipeId" = r."Id")
              Buffers: shared hit=2
Planning Time: 0.141 ms
Execution Time: 0.040 ms
```
- 0.4 ms, 1 câu:
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
Nested Loop  (cost=8.44..16.47 rows=1 width=2068) (actual time=0.016..0.017 rows=0 loops=1)
  Buffers: shared hit=5
  ->  Limit  (cost=8.30..8.31 rows=1 width=16) (actual time=0.015..0.015 rows=1 loops=1)
        Buffers: shared hit=3
        ->  Sort  (cost=8.30..8.31 rows=1 width=16) (actual time=0.015..0.015 rows=1 loops=1)
              Sort Key: r."Id"
              Sort Method: quicksort  Memory: 25kB
              Buffers: shared hit=3
              ->  Index Scan using "IDX_Recipe_Slug" on "Recipes" r  (cost=0.27..8.29 rows=1 width=16) (actual time=0.013..0.013 rows=1 loops=1)
                    Index Cond: (("Slug")::text = 'k22-10nl-6672d4837fe74e15ad395'::text)
                    Filter: (NOT "IsDeleted")
                    Buffers: shared hit=3
  ->  Index Scan using "IX_RecipeImages_RecipeId_OrderIndex" on "RecipeImages" r2  (cost=0.14..8.15 rows=1 width=2052) (actual time=0.001..0.001 rows=0 loops=1)
        Index Cond: ("RecipeId" = r."Id")
        Filter: (NOT "IsDeleted")
        Buffers: shared hit=2
Planning:
  Buffers: shared hit=3
Planning Time: 0.117 ms
Execution Time: 0.026 ms
```
- 0.4 ms, 1 câu:
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
Limit  (cost=0.27..24.74 rows=1 width=436) (actual time=0.030..0.030 rows=1 loops=1)
  Buffers: shared hit=8
  ->  Index Scan using "PK_Recipes" on "Recipes" r  (cost=0.27..24.74 rows=1 width=436) (actual time=0.029..0.030 rows=1 loops=1)
        Index Cond: ("Id" = '467265ef-abf0-43d7-a774-e71a7e210b42'::uuid)
        Filter: (NOT "IsDeleted")
        Buffers: shared hit=8
        SubPlan 1
          ->  Limit  (cost=0.28..8.29 rows=1 width=21) (actual time=0.013..0.014 rows=1 loops=1)
                Buffers: shared hit=3
                ->  Index Scan using "PK_AspNetUsers" on "AspNetUsers" a  (cost=0.28..8.29 rows=1 width=21) (actual time=0.013..0.013 rows=1 loops=1)
                      Index Cond: ("Id" = (r."AuthorId")::text)
                      Buffers: shared hit=3
        SubPlan 2
          ->  Limit  (cost=0.14..8.16 rows=1 width=218) (actual time=0.004..0.004 rows=1 loops=1)
                Buffers: shared hit=2
                ->  Index Scan using "PK_Categories" on "Categories" c  (cost=0.14..8.16 rows=1 width=218) (actual time=0.003..0.003 rows=1 loops=1)
                      Index Cond: ("Id" = r."CategoryId")
                      Filter: (NOT "IsDeleted")
                      Buffers: shared hit=2
Planning Time: 0.116 ms
Execution Time: 0.048 ms
```

### GET dashboard /me/recipes (12 công thức)
- 0.7 ms, 1 câu:
```sql
SELECT count(*)::int
FROM "Recipes" AS r
WHERE NOT (r."IsDeleted") AND r."AuthorId" = @query_AuthorId
```
```text
Aggregate  (cost=8.29..8.30 rows=1 width=4) (actual time=0.018..0.018 rows=1 loops=1)
  Buffers: shared hit=3
  ->  Index Scan using "IDX_Recipe_AuthorId" on "Recipes" r  (cost=0.27..8.29 rows=1 width=0) (actual time=0.012..0.016 rows=12 loops=1)
        Index Cond: (("AuthorId")::text = '32fc9c6e-0c34-4a38-91d5-8a18e246caa5'::text)
        Filter: (NOT "IsDeleted")
        Buffers: shared hit=3
Planning Time: 0.068 ms
Execution Time: 0.030 ms
```
- 0.8 ms, 1 câu:
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
Limit  (cost=8.30..44.83 rows=1 width=715) (actual time=0.050..0.143 rows=12 loops=1)
  Buffers: shared hit=123
  ->  Result  (cost=8.30..44.83 rows=1 width=715) (actual time=0.050..0.141 rows=12 loops=1)
        Buffers: shared hit=123
        ->  Sort  (cost=8.30..8.31 rows=1 width=159) (actual time=0.023..0.023 rows=12 loops=1)
              Sort Key: (COALESCE(r."UpdatedAt", r."CreatedAt")) DESC, r."Id"
              Sort Method: quicksort  Memory: 28kB
              Buffers: shared hit=3
              ->  Index Scan using "IDX_Recipe_AuthorId" on "Recipes" r  (cost=0.27..8.29 rows=1 width=159) (actual time=0.012..0.015 rows=12 loops=1)
                    Index Cond: (("AuthorId")::text = '32fc9c6e-0c34-4a38-91d5-8a18e246caa5'::text)
                    Filter: (NOT "IsDeleted")
                    Buffers: shared hit=3
        SubPlan 1
          ->  Limit  (cost=0.14..8.16 rows=1 width=218) (actual time=0.001..0.001 rows=1 loops=12)
                Buffers: shared hit=24
                ->  Index Scan using "PK_Categories" on "Categories" c  (cost=0.14..8.16 rows=1 width=218) (actual time=0.001..0.001 rows=1 loops=12)
                      Index Cond: ("Id" = r."CategoryId")
                      Filter: (NOT "IsDeleted")
                      Buffers: shared hit=24
        SubPlan 2
          ->  Limit  (cost=0.12..8.14 rows=1 width=516) (actual time=0.001..0.001 rows=0 loops=12)
                Buffers: shared hit=24
                ->  Index Scan using ux_recipe_images_one_primary on "RecipeImages" r0  (cost=0.12..8.14 rows=1 width=516) (actual time=0.001..0.001 rows=0 loops=12)
                      Index Cond: ("RecipeId" = r."Id")
                      Filter: (NOT "IsDeleted")
                      Buffers: shared hit=24
        SubPlan 3
          ->  Aggregate  (cost=9.87..9.88 rows=1 width=4) (actual time=0.004..0.004 rows=1 loops=12)
                Buffers: shared hit=36
                ->  Bitmap Heap Scan on "RecipeIngredients" r1  (cost=4.29..9.87 rows=2 width=0) (actual time=0.002..0.002 rows=1 loops=12)
                      Recheck Cond: (r."Id" = "RecipeId")
                      Filter: (NOT "IsDeleted")
                      Heap Blocks: exact=12
                      Buffers: shared hit=36
                      ->  Bitmap Index Scan on "IX_RecipeIngredients_RecipeId_OrderIndex"  (cost=0.00..4.29 rows=2 width=0) (actual time=0.001..0.001 rows=1 loops=12)
                            Index Cond: ("RecipeId" = r."Id")
                            Buffers: shared hit=24
        SubPlan 4
          ->  Aggregate  (cost=10.32..10.33 rows=1 width=4) (actual time=0.003..0.003 rows=1 loops=12)
                Buffers: shared hit=36
                ->  Bitmap Heap Scan on "RecipeSteps" r2  (cost=4.29..10.32 rows=2 width=0) (actual time=0.002..0.002 rows=1 loops=12)
                      Recheck Cond: ((r."Id" = "RecipeId") AND (NOT "IsDeleted"))
                      Heap Blocks: exact=12
                      Buffers: shared hit=36
                      ->  Bitmap Index Scan on "IX_RecipeSteps_RecipeId_StepNumber"  (cost=0.00..4.29 rows=2 width=0) (actual time=0.001..0.001 rows=1 loops=12)
                            Index Cond: ("RecipeId" = r."Id")
                            Buffers: shared hit=24
Planning:
  Buffers: shared hit=3
Planning Time: 0.244 ms
Execution Time: 0.214 ms
```

### GET dashboard /me/recipes/counts (12 công thức)
- 0.6 ms, 1 câu:
```sql
SELECT r."Status", count(*)::int AS "Count"
FROM "Recipes" AS r
WHERE NOT (r."IsDeleted") AND r."AuthorId" = @authorId
GROUP BY r."Status"
```
```text
GroupAggregate  (cost=8.30..8.32 rows=1 width=6) (actual time=0.024..0.024 rows=1 loops=1)
  Group Key: "Status"
  Buffers: shared hit=3
  ->  Sort  (cost=8.30..8.31 rows=1 width=2) (actual time=0.021..0.022 rows=12 loops=1)
        Sort Key: "Status"
        Sort Method: quicksort  Memory: 25kB
        Buffers: shared hit=3
        ->  Index Scan using "IDX_Recipe_AuthorId" on "Recipes" r  (cost=0.27..8.29 rows=1 width=2) (actual time=0.016..0.018 rows=12 loops=1)
              Index Cond: (("AuthorId")::text = '32fc9c6e-0c34-4a38-91d5-8a18e246caa5'::text)
              Filter: (NOT "IsDeleted")
              Buffers: shared hit=3
Planning Time: 0.069 ms
Execution Time: 0.035 ms
```

### POST nguyên liệu (đang có 10)
- 1.0 ms, 1 câu:
```sql
SELECT r2."Id", r2."AuthorId", r2."CategoryId", r2."CookTimeMinutes", r2."CreatedAt", r2."Description", r2."Difficulty", r2."Instructions", r2."IsDeleted", r2."PrepTimeMinutes", r2."PublishedAt", r2."RowVersion", r2."Servings", r2."Slug", r2."Status", r2."Title", r2."UpdatedAt", r2."Nutrition_Calories", r2."Nutrition_Carbohydrates", r2."Nutrition_Fat", r2."Nutrition_Fiber", r2."Nutrition_Protein", r2."Nutrition_Sodium", r3."Id", r3."CreatedAt", r3."IsDeleted", r3."Name", r3."Notes", r3."OrderIndex", r3."Quantity", r3."RecipeId", r3."RowVersion", r3."Unit", r3."UpdatedAt", r4."Id", r4."CreatedAt", r4."Description", r4."ImageUrl", r4."IsDeleted", r4."RecipeId", r4."RowVersion", r4."StepNumber", r4."TimerMinutes", r4."Title", r4."UpdatedAt"
FROM (
    SELECT r."Id", r."AuthorId", r."CategoryId", r."CookTimeMinutes", r."CreatedAt", r."Description", r."Difficulty", r."Instructions", r."IsDeleted", r."PrepTimeMinutes", r."PublishedAt", r."RowVersion", r."Servings", r."Slug", r."Status", r."Title", r."UpdatedAt", r."Nutrition_Calories", r."Nutrition_Carbohydrates", r."Nutrition_Fat", r."Nutrition_Fiber", r."Nutrition_Protein", r."Nutrition_Sodium"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Id" = @id
    LIMIT 1
) AS r2
LEFT JOIN (
    SELECT r0."Id", r0."CreatedAt", r0."IsDeleted", r0."Name", r0."Notes", r0."OrderIndex", r0."Quantity", r0."RecipeId", r0."RowVersion", r0."Unit", r0."UpdatedAt"
    FROM "RecipeIngredients" AS r0
    WHERE NOT (r0."IsDeleted …
```
```text
Sort  (cost=33.55..33.56 rows=5 width=1048) (actual time=0.133..0.135 rows=60 loops=1)
  Sort Key: r."Id", r0."Id"
  Sort Method: quicksort  Memory: 51kB
  Buffers: shared hit=9
  ->  Nested Loop Left Join  (cost=8.86..33.49 rows=5 width=1048) (actual time=0.036..0.058 rows=60 loops=1)
        Buffers: shared hit=9
        ->  Nested Loop Left Join  (cost=4.56..18.63 rows=2 width=936) (actual time=0.025..0.027 rows=6 loops=1)
              Buffers: shared hit=6
              ->  Limit  (cost=0.27..8.29 rows=1 width=318) (actual time=0.011..0.012 rows=1 loops=1)
                    Buffers: shared hit=3
                    ->  Index Scan using "PK_Recipes" on "Recipes" r  (cost=0.27..8.29 rows=1 width=318) (actual time=0.011..0.011 rows=1 loops=1)
                          Index Cond: ("Id" = '467265ef-abf0-43d7-a774-e71a7e210b42'::uuid)
                          Filter: (NOT "IsDeleted")
                          Buffers: shared hit=3
              ->  Bitmap Heap Scan on "RecipeSteps" r1  (cost=4.29..10.32 rows=2 width=618) (actual time=0.010..0.011 rows=6 loops=1)
                    Recheck Cond: ((r."Id" = "RecipeId") AND (NOT "IsDeleted"))
                    Heap Blocks: exact=1
                    Buffers: shared hit=3
                    ->  Bitmap Index Scan on "IX_RecipeSteps_RecipeId_StepNumber"  (cost=0.00..4.29 rows=2 width=0) (actual time=0.008..0.008 rows=13 loops=1)
                          Index Cond: ("RecipeId" = r."Id")
                          Buffers: shared hit=2
        ->  Memoize  (cost=4.30..9.88 rows=2 width=112) (actual time=0.002..0.003 rows=10 loops=6)
              Cache Key: r."Id"
              Cache Mode: logical
              Hits: 5  Misses: 1  Evictions: 0  Overflows: 0  Memory Usage: 2kB
              Buffers: shared hit=3
              ->  Bitmap Heap Scan on "RecipeIngredients" r0  (cost=4.29..9.87 rows=2 width=112) (actual time=0.005..0.007 rows=10 loops=1)
                    Recheck Cond: (r."Id" = "RecipeId")
                    Filter: (NOT "IsDeleted")
                    Rows Removed by Filter: 1
                    Heap Blocks: exact=1
                    Buffers: shared hit=3
                    ->  Bitmap Index Scan on "IX_RecipeIngredients_RecipeId_OrderIndex"  (cost=0.00..4.29 rows=2 width=0) (actual time=0.003..0.003 rows=21 loops=1)
                          Index Cond: ("RecipeId" = r."Id")
                          Buffers: shared hit=2
Planning:
  Buffers: shared hit=8
Planning Time: 0.293 ms
Execution Time: 0.219 ms
```
- 0.6 ms, 1 câu:
```sql
INSERT INTO "RecipeIngredients" ("Id", "CreatedAt", "IsDeleted", "Name", "Notes", "OrderIndex", "Quantity", "RecipeId", "RowVersion", "Unit", "UpdatedAt")
VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10);
```

### DELETE nguyên liệu đầu (còn 10 phải đánh lại thứ tự)
- 1.3 ms, 1 câu:
```sql
SELECT r2."Id", r2."AuthorId", r2."CategoryId", r2."CookTimeMinutes", r2."CreatedAt", r2."Description", r2."Difficulty", r2."Instructions", r2."IsDeleted", r2."PrepTimeMinutes", r2."PublishedAt", r2."RowVersion", r2."Servings", r2."Slug", r2."Status", r2."Title", r2."UpdatedAt", r2."Nutrition_Calories", r2."Nutrition_Carbohydrates", r2."Nutrition_Fat", r2."Nutrition_Fiber", r2."Nutrition_Protein", r2."Nutrition_Sodium", r3."Id", r3."CreatedAt", r3."IsDeleted", r3."Name", r3."Notes", r3."OrderIndex", r3."Quantity", r3."RecipeId", r3."RowVersion", r3."Unit", r3."UpdatedAt", r4."Id", r4."CreatedAt", r4."Description", r4."ImageUrl", r4."IsDeleted", r4."RecipeId", r4."RowVersion", r4."StepNumber", r4."TimerMinutes", r4."Title", r4."UpdatedAt"
FROM (
    SELECT r."Id", r."AuthorId", r."CategoryId", r."CookTimeMinutes", r."CreatedAt", r."Description", r."Difficulty", r."Instructions", r."IsDeleted", r."PrepTimeMinutes", r."PublishedAt", r."RowVersion", r."Servings", r."Slug", r."Status", r."Title", r."UpdatedAt", r."Nutrition_Calories", r."Nutrition_Carbohydrates", r."Nutrition_Fat", r."Nutrition_Fiber", r."Nutrition_Protein", r."Nutrition_Sodium"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Id" = @id
    LIMIT 1
) AS r2
LEFT JOIN (
    SELECT r0."Id", r0."CreatedAt", r0."IsDeleted", r0."Name", r0."Notes", r0."OrderIndex", r0."Quantity", r0."RecipeId", r0."RowVersion", r0."Unit", r0."UpdatedAt"
    FROM "RecipeIngredients" AS r0
    WHERE NOT (r0."IsDeleted …
```
- 1.0 ms, 11 câu:
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
UPDATE "RecipeIngredients" SET "OrderIndex" = @p25, "RowVersion" = @p26, "UpdatedAt" = @p27
WHERE "Id" = @p28 AND "RowVersion" = @p29;
UPDATE "RecipeIngredients" SET "OrderIndex" = @p30, "RowVersion" = @p31, "UpdatedAt" = @p32
WHERE "Id" = @p33 AND "RowVersion" = @p34;
UPDATE "RecipeIngredients" SET "OrderIndex" = @p35, "RowVersion" = @p36, "UpdatedAt" = @p37
WHERE "Id" = @p38 AND "RowVersion" = @p39;
UPDATE "RecipeIngredients" SET "OrderIndex" = @p40, "RowVersion" = @p41, "UpdatedAt" = @p42
WHERE "Id" = @p43 AND "RowVersion" = @p44;
UPDATE "RecipeIngredients" SET "CreatedAt" = @p45, "IsDeleted" = @p46, "Name" = @p47, "Notes" = @p48, "OrderIndex" = @p49, "Quantity" = @p50, "RecipeId" = @p51, "RowVersion" = @p52, "Unit" = @p53, "UpdatedAt" = @p54
WHERE "Id" = @p55 AND "RowVersion" = @p56;
UPDATE "RecipeIn …
```

### POST bước (đang có 6)
- 1.1 ms, 1 câu:
```sql
SELECT r2."Id", r2."AuthorId", r2."CategoryId", r2."CookTimeMinutes", r2."CreatedAt", r2."Description", r2."Difficulty", r2."Instructions", r2."IsDeleted", r2."PrepTimeMinutes", r2."PublishedAt", r2."RowVersion", r2."Servings", r2."Slug", r2."Status", r2."Title", r2."UpdatedAt", r2."Nutrition_Calories", r2."Nutrition_Carbohydrates", r2."Nutrition_Fat", r2."Nutrition_Fiber", r2."Nutrition_Protein", r2."Nutrition_Sodium", r3."Id", r3."CreatedAt", r3."IsDeleted", r3."Name", r3."Notes", r3."OrderIndex", r3."Quantity", r3."RecipeId", r3."RowVersion", r3."Unit", r3."UpdatedAt", r4."Id", r4."CreatedAt", r4."Description", r4."ImageUrl", r4."IsDeleted", r4."RecipeId", r4."RowVersion", r4."StepNumber", r4."TimerMinutes", r4."Title", r4."UpdatedAt"
FROM (
    SELECT r."Id", r."AuthorId", r."CategoryId", r."CookTimeMinutes", r."CreatedAt", r."Description", r."Difficulty", r."Instructions", r."IsDeleted", r."PrepTimeMinutes", r."PublishedAt", r."RowVersion", r."Servings", r."Slug", r."Status", r."Title", r."UpdatedAt", r."Nutrition_Calories", r."Nutrition_Carbohydrates", r."Nutrition_Fat", r."Nutrition_Fiber", r."Nutrition_Protein", r."Nutrition_Sodium"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Id" = @id
    LIMIT 1
) AS r2
LEFT JOIN (
    SELECT r0."Id", r0."CreatedAt", r0."IsDeleted", r0."Name", r0."Notes", r0."OrderIndex", r0."Quantity", r0."RecipeId", r0."RowVersion", r0."Unit", r0."UpdatedAt"
    FROM "RecipeIngredients" AS r0
    WHERE NOT (r0."IsDeleted …
```
- 0.7 ms, 1 câu:
```sql
INSERT INTO "RecipeSteps" ("Id", "CreatedAt", "Description", "ImageUrl", "IsDeleted", "RecipeId", "RowVersion", "StepNumber", "TimerMinutes", "Title", "UpdatedAt")
VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10);
```

### DELETE bước đầu (còn 6 phải đánh lại số)
- 1.5 ms, 1 câu:
```sql
SELECT r2."Id", r2."AuthorId", r2."CategoryId", r2."CookTimeMinutes", r2."CreatedAt", r2."Description", r2."Difficulty", r2."Instructions", r2."IsDeleted", r2."PrepTimeMinutes", r2."PublishedAt", r2."RowVersion", r2."Servings", r2."Slug", r2."Status", r2."Title", r2."UpdatedAt", r2."Nutrition_Calories", r2."Nutrition_Carbohydrates", r2."Nutrition_Fat", r2."Nutrition_Fiber", r2."Nutrition_Protein", r2."Nutrition_Sodium", r3."Id", r3."CreatedAt", r3."IsDeleted", r3."Name", r3."Notes", r3."OrderIndex", r3."Quantity", r3."RecipeId", r3."RowVersion", r3."Unit", r3."UpdatedAt", r4."Id", r4."CreatedAt", r4."Description", r4."ImageUrl", r4."IsDeleted", r4."RecipeId", r4."RowVersion", r4."StepNumber", r4."TimerMinutes", r4."Title", r4."UpdatedAt"
FROM (
    SELECT r."Id", r."AuthorId", r."CategoryId", r."CookTimeMinutes", r."CreatedAt", r."Description", r."Difficulty", r."Instructions", r."IsDeleted", r."PrepTimeMinutes", r."PublishedAt", r."RowVersion", r."Servings", r."Slug", r."Status", r."Title", r."UpdatedAt", r."Nutrition_Calories", r."Nutrition_Carbohydrates", r."Nutrition_Fat", r."Nutrition_Fiber", r."Nutrition_Protein", r."Nutrition_Sodium"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Id" = @id
    LIMIT 1
) AS r2
LEFT JOIN (
    SELECT r0."Id", r0."CreatedAt", r0."IsDeleted", r0."Name", r0."Notes", r0."OrderIndex", r0."Quantity", r0."RecipeId", r0."RowVersion", r0."Unit", r0."UpdatedAt"
    FROM "RecipeIngredients" AS r0
    WHERE NOT (r0."IsDeleted …
```
- 0.8 ms, 7 câu:
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
