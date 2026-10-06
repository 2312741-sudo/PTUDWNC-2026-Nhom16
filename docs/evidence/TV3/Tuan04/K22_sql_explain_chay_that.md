# K22 — đếm câu SQL mỗi request + EXPLAIN (ANALYZE, BUFFERS) — 2026-10-02 13:53

Dữ liệu trong DB lúc đo: Recipes=285, RecipeIngredients=266, RecipeSteps=496, Users=829

| Request | HTTP | Số lệnh SQL (round-trip) | Số câu trong các lệnh | Tổng ms SQL | Lệnh chậm nhất ms | > 100 ms |
|---|---|---|---|---|---|---|
| GET chi tiết (2 nguyên liệu, 2 bước) | 200 | 2 | 1 + 1 | 3.1 | 2.4 | 0 |
| GET chi tiết (10 nguyên liệu, 6 bước) | 200 | 2 | 1 + 1 | 2.0 | 1.4 | 0 |
| GET dashboard /me/recipes (1 công thức) | 200 | 2 | 1 + 1 | 3.8 | 3.0 | 0 |
| GET dashboard /me/recipes (12 công thức) | 200 | 2 | 1 + 1 | 1.6 | 0.9 | 0 |
| GET dashboard /me/recipes/counts (1 công thức) | 200 | 1 | 1 | 1.5 | 1.5 | 0 |
| GET dashboard /me/recipes/counts (12 công thức) | 200 | 1 | 1 | 0.6 | 0.6 | 0 |
| POST nguyên liệu (đang có 2) | 201 | 2 | 1 + 1 | 2.0 | 1.3 | 0 |
| POST nguyên liệu (đang có 10) | 201 | 2 | 1 + 1 | 1.8 | 1.1 | 0 |
| DELETE nguyên liệu đầu (còn 2 phải đánh lại thứ tự) | 204 | 2 | 1 + 3 | 1.9 | 1.1 | 0 |
| DELETE nguyên liệu đầu (còn 10 phải đánh lại thứ tự) | 204 | 2 | 1 + 11 | 2.3 | 1.3 | 0 |
| POST bước (đang có 2) | 201 | 2 | 1 + 1 | 1.6 | 1.0 | 0 |
| POST bước (đang có 6) | 201 | 2 | 1 + 1 | 1.8 | 0.9 | 0 |
| DELETE bước đầu (còn 2 phải đánh lại số) | 204 | 2 | 1 + 3 | 1.6 | 1.0 | 0 |
| DELETE bước đầu (còn 6 phải đánh lại số) | 204 | 2 | 1 + 7 | 1.8 | 1.1 | 0 |

## Câu SQL và EXPLAIN (ANALYZE, BUFFERS) — bản lớn của mỗi cặp, chỉ các SELECT

### GET chi tiết (10 nguyên liệu, 6 bước)
- 1.4 ms, 1 câu:
```sql
SELECT r3."Id", r3."AuthorId", r3."CategoryId", r3."CookTimeMinutes", r3."CreatedAt", r3."Description", r3."Difficulty", r3."Instructions", r3."IsDeleted", r3."PrepTimeMinutes", r3."PublishedAt", r3."RowVersion", r3."Servings", r3."Slug", r3."Status", r3."Title", r3."UpdatedAt", r3."Nutrition_Calories", r3."Nutrition_Carbohydrates", r3."Nutrition_Fat", r3."Nutrition_Fiber", r3."Nutrition_Protein", r3."Nutrition_Sodium", r4."Id", r4."CreatedAt", r4."IsDeleted", r4."Name", r4."Notes", r4."OrderIndex", r4."Quantity", r4."RecipeId", r4."RowVersion", r4."Unit", r4."UpdatedAt", r5."Id", r5."CreatedAt", r5."Description", r5."ImageUrl", r5."IsDeleted", r5."RecipeId", r5."RowVersion", r5."StepNumber", r5."TimerMinutes", r5."Title", r5."UpdatedAt", r6."Id", r6."AltText", r6."CreatedAt", r6."IsDeleted", r6."IsPrimary", r6."MediumUrl", r6."OrderIndex", r6."OriginalUrl", r6."RecipeId", r6."RowVersion", r6."ThumbnailUrl", r6."UpdatedAt"
FROM (
    SELECT r."Id", r."AuthorId", r."CategoryId", r."CookTimeMinutes", r."CreatedAt", r."Description", r."Difficulty", r."Instructions", r."IsDeleted", r."PrepTimeMinutes", r."PublishedAt", r."RowVersion", r."Servings", r."Slug", r."Status", r."Title", r."UpdatedAt", r."Nutrition_Calories", r."Nutrition_Carbohydrates", r."Nutrition_Fat", r."Nutrition_Fiber", r."Nutrition_Protein", r."Nutrition_Sodium"
    FROM "Recipes" AS r
    WHERE NOT (r."IsDeleted") AND r."Slug" = @slug
    LIMIT 1
) AS r3
LEFT JOIN (
    SELECT r0."Id", r0."CreatedAt", r …
```
```text
Sort  (cost=38.49..38.51 rows=6 width=3112) (actual time=0.231..0.234 rows=60 loops=1)
  Sort Key: r."Id", r0."Id", r1."Id"
  Sort Method: quicksort  Memory: 51kB
  Buffers: shared hit=16
  ->  Hash Right Join  (cost=28.16..38.41 rows=6 width=3112) (actual time=0.094..0.118 rows=60 loops=1)
        Hash Cond: (r0."RecipeId" = r."Id")
        Buffers: shared hit=16
        ->  Seq Scan on "RecipeIngredients" r0  (cost=0.00..8.63 rows=251 width=112) (actual time=0.005..0.033 rows=252 loops=1)
              Filter: (NOT "IsDeleted")
              Rows Removed by Filter: 14
              Buffers: shared hit=6
        ->  Hash  (cost=28.12..28.12 rows=3 width=3000) (actual time=0.047..0.048 rows=6 loops=1)
              Buckets: 1024  Batches: 1  Memory Usage: 11kB
              Buffers: shared hit=10
              ->  Nested Loop Left Join  (cost=4.71..28.12 rows=3 width=3000) (actual time=0.039..0.041 rows=6 loops=1)
                    Buffers: shared hit=10
                    ->  Nested Loop Left Join  (cost=0.41..16.46 rows=1 width=2380) (actual time=0.023..0.024 rows=1 loops=1)
                          Buffers: shared hit=5
                          ->  Limit  (cost=0.27..8.29 rows=1 width=328) (actual time=0.019..0.019 rows=1 loops=1)
                                Buffers: shared hit=3
                                ->  Index Scan using "IDX_Recipe_Slug" on "Recipes" r  (cost=0.27..8.29 rows=1 width=328) (actual time=0.018..0.018 rows=1 loops=1)
                                      Index Cond: (("Slug")::text = 'k22-10nl-0ed2f7255dcd425dbf2c6'::text)
                                      Filter: (NOT "IsDeleted")
                                      Buffers: shared hit=3
                          ->  Index Scan using "IX_RecipeImages_RecipeId_OrderIndex" on "RecipeImages" r2  (cost=0.14..8.15 rows=1 width=2052) (actual time=0.003..0.003 rows=0 loops=1)
                                Index Cond: ("RecipeId" = r."Id")
                                Filter: (NOT "IsDeleted")
                                Buffers: shared hit=2
                    ->  Bitmap Heap Scan on "RecipeSteps" r1  (cost=4.30..11.63 rows=3 width=620) (actual time=0.013..0.013 rows=6 loops=1)
                          Recheck Cond: ((r."Id" = "RecipeId") AND (NOT "IsDeleted"))
                          Heap Blocks: exact=3
                          Buffers: shared hit=5
                          ->  Bitmap Index Scan on "IX_RecipeSteps_RecipeId_StepNumber"  (cost=0.00..4.29 rows=3 width=0) (actual time=0.008..0.008 rows=13 loops=1)
                                Index Cond: ("RecipeId" = r."Id")
                                Buffers: shared hit=2
Planning:
  Buffers: shared hit=9
Planning Time: 0.473 ms
Execution Time: 0.292 ms
```
- 0.5 ms, 1 câu:
```sql
SELECT c."Name", (
    SELECT a."DisplayName"
    FROM "AspNetUsers" AS a
    WHERE a."Id" = @authorId
    LIMIT 1) AS "Author"
FROM "Categories" AS c
WHERE NOT (c."IsDeleted") AND c."Id" = @categoryId
LIMIT 1
```
```text
Limit  (cost=8.43..16.45 rows=1 width=436) (actual time=0.016..0.017 rows=1 loops=1)
  Buffers: shared hit=5
  InitPlan 1 (returns $0)
    ->  Limit  (cost=0.28..8.29 rows=1 width=21) (actual time=0.010..0.010 rows=1 loops=1)
          Buffers: shared hit=3
          ->  Index Scan using "PK_AspNetUsers" on "AspNetUsers" a  (cost=0.28..8.29 rows=1 width=21) (actual time=0.010..0.010 rows=1 loops=1)
                Index Cond: ("Id" = '4259d337-6b90-4f7f-b7be-f58cb78936cf'::text)
                Buffers: shared hit=3
  ->  Index Scan using "PK_Categories" on "Categories" c  (cost=0.14..8.16 rows=1 width=436) (actual time=0.016..0.016 rows=1 loops=1)
        Index Cond: ("Id" = '9857bd19-70e1-4d98-b380-803f957c9001'::uuid)
        Filter: (NOT "IsDeleted")
        Buffers: shared hit=5
Planning Time: 0.078 ms
Execution Time: 0.026 ms
```

### GET dashboard /me/recipes (12 công thức)
- 0.7 ms, 1 câu:
```sql
SELECT count(*)::int
FROM "Recipes" AS r
WHERE NOT (r."IsDeleted") AND r."AuthorId" = @query_AuthorId
```
```text
Aggregate  (cost=8.29..8.30 rows=1 width=4) (actual time=0.041..0.041 rows=1 loops=1)
  Buffers: shared hit=3
  ->  Index Scan using "IDX_Recipe_AuthorId" on "Recipes" r  (cost=0.27..8.29 rows=1 width=0) (actual time=0.034..0.038 rows=12 loops=1)
        Index Cond: (("AuthorId")::text = 'b8cf907a-6848-41f0-abcb-cc9c92256603'::text)
        Filter: (NOT "IsDeleted")
        Buffers: shared hit=3
Planning Time: 0.086 ms
Execution Time: 0.052 ms
```
- 0.9 ms, 1 câu:
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
Limit  (cost=8.30..37.83 rows=1 width=715) (actual time=0.053..0.127 rows=12 loops=1)
  Buffers: shared hit=124
  ->  Result  (cost=8.30..37.83 rows=1 width=715) (actual time=0.053..0.126 rows=12 loops=1)
        Buffers: shared hit=124
        ->  Sort  (cost=8.30..8.31 rows=1 width=159) (actual time=0.024..0.025 rows=12 loops=1)
              Sort Key: (COALESCE(r."UpdatedAt", r."CreatedAt")) DESC, r."Id"
              Sort Method: quicksort  Memory: 28kB
              Buffers: shared hit=3
              ->  Index Scan using "IDX_Recipe_AuthorId" on "Recipes" r  (cost=0.27..8.29 rows=1 width=159) (actual time=0.012..0.017 rows=12 loops=1)
                    Index Cond: (("AuthorId")::text = 'b8cf907a-6848-41f0-abcb-cc9c92256603'::text)
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
                ->  Index Scan using ux_recipe_images_one_primary on "RecipeImages" r0  (cost=0.12..8.14 rows=1 width=516) (actual time=0.000..0.000 rows=0 loops=12)
                      Index Cond: ("RecipeId" = r."Id")
                      Filter: (NOT "IsDeleted")
                      Buffers: shared hit=24
        SubPlan 3
          ->  Aggregate  (cost=8.85..8.87 rows=1 width=4) (actual time=0.004..0.004 rows=1 loops=12)
                Buffers: shared hit=36
                ->  Bitmap Heap Scan on "RecipeIngredients" r1  (cost=4.29..8.85 rows=2 width=0) (actual time=0.002..0.002 rows=1 loops=12)
                      Recheck Cond: (r."Id" = "RecipeId")
                      Filter: (NOT "IsDeleted")
                      Heap Blocks: exact=12
                      Buffers: shared hit=36
                      ->  Bitmap Index Scan on "IX_RecipeIngredients_RecipeId_OrderIndex"  (cost=0.00..4.29 rows=2 width=0) (actual time=0.001..0.001 rows=1 loops=12)
                            Index Cond: ("RecipeId" = r."Id")
                            Buffers: shared hit=24
        SubPlan 4
          ->  Aggregate  (cost=4.33..4.35 rows=1 width=4) (actual time=0.002..0.002 rows=1 loops=12)
                Buffers: shared hit=37
                ->  Index Only Scan using "IX_RecipeSteps_RecipeId_StepNumber" on "RecipeSteps" r2  (cost=0.27..4.33 rows=3 width=0) (actual time=0.002..0.002 rows=1 loops=12)
                      Index Cond: ("RecipeId" = r."Id")
                      Heap Fetches: 12
                      Buffers: shared hit=37
Planning:
  Buffers: shared hit=3
Planning Time: 0.244 ms
Execution Time: 0.191 ms
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
GroupAggregate  (cost=8.30..8.32 rows=1 width=6) (actual time=0.021..0.021 rows=1 loops=1)
  Group Key: "Status"
  Buffers: shared hit=3
  ->  Sort  (cost=8.30..8.31 rows=1 width=2) (actual time=0.018..0.019 rows=12 loops=1)
        Sort Key: "Status"
        Sort Method: quicksort  Memory: 25kB
        Buffers: shared hit=3
        ->  Index Scan using "IDX_Recipe_AuthorId" on "Recipes" r  (cost=0.27..8.29 rows=1 width=2) (actual time=0.013..0.016 rows=12 loops=1)
              Index Cond: (("AuthorId")::text = 'b8cf907a-6848-41f0-abcb-cc9c92256603'::text)
              Filter: (NOT "IsDeleted")
              Buffers: shared hit=3
Planning Time: 0.062 ms
Execution Time: 0.030 ms
```

### POST nguyên liệu (đang có 10)
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
```text
Sort  (cost=30.20..30.22 rows=6 width=1060) (actual time=0.132..0.135 rows=60 loops=1)
  Sort Key: r."Id", r0."Id"
  Sort Method: quicksort  Memory: 51kB
  Buffers: shared hit=13
  ->  Hash Right Join  (cost=19.87..30.12 rows=6 width=1060) (actual time=0.074..0.097 rows=60 loops=1)
        Hash Cond: (r0."RecipeId" = r."Id")
        Buffers: shared hit=13
        ->  Seq Scan on "RecipeIngredients" r0  (cost=0.00..8.63 rows=251 width=112) (actual time=0.004..0.033 rows=252 loops=1)
              Filter: (NOT "IsDeleted")
              Rows Removed by Filter: 14
              Buffers: shared hit=6
        ->  Hash  (cost=19.83..19.83 rows=3 width=948) (actual time=0.028..0.030 rows=6 loops=1)
              Buckets: 1024  Batches: 1  Memory Usage: 11kB
              Buffers: shared hit=7
              ->  Nested Loop Left Join  (cost=4.44..19.83 rows=3 width=948) (actual time=0.021..0.024 rows=6 loops=1)
                    Buffers: shared hit=7
                    ->  Limit  (cost=0.15..8.17 rows=1 width=328) (actual time=0.007..0.007 rows=1 loops=1)
                          Buffers: shared hit=2
                          ->  Index Scan using "PK_Recipes" on "Recipes" r  (cost=0.15..8.17 rows=1 width=328) (actual time=0.006..0.006 rows=1 loops=1)
                                Index Cond: ("Id" = 'cd565a2f-9920-4b83-922a-ce3759247958'::uuid)
                                Filter: (NOT "IsDeleted")
                                Buffers: shared hit=2
                    ->  Bitmap Heap Scan on "RecipeSteps" r1  (cost=4.30..11.63 rows=3 width=620) (actual time=0.012..0.012 rows=6 loops=1)
                          Recheck Cond: ((r."Id" = "RecipeId") AND (NOT "IsDeleted"))
                          Heap Blocks: exact=3
                          Buffers: shared hit=5
                          ->  Bitmap Index Scan on "IX_RecipeSteps_RecipeId_StepNumber"  (cost=0.00..4.29 rows=3 width=0) (actual time=0.009..0.009 rows=13 loops=1)
                                Index Cond: ("RecipeId" = r."Id")
                                Buffers: shared hit=2
Planning:
  Buffers: shared hit=6
Planning Time: 0.244 ms
Execution Time: 0.177 ms
```
- 0.7 ms, 1 câu:
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
UPDATE "RecipeIngredients" SET "CreatedAt" = @p5, "IsDeleted" = @p6, "Name" = @p7, "Notes" = @p8, "OrderIndex" = @p9, "Quantity" = @p10, "RecipeId" = @p11, "RowVersion" = @p12, "Unit" = @p13, "UpdatedAt" = @p14
WHERE "Id" = @p15 AND "RowVersion" = @p16;
UPDATE "RecipeIngredients" SET "OrderIndex" = @p17, "RowVersion" = @p18, "UpdatedAt" = @p19
WHERE "Id" = @p20 AND "RowVersion" = @p21;
UPDATE "RecipeIngredients" SET "OrderIndex" = @p22, "RowVersion" = @p23, "UpdatedAt" = @p24
WHERE "Id" = @p25 AND "RowVersion" = @p26;
UPDATE "RecipeIngredients" SET "OrderIndex" = @p27, "RowVersion" = @p28, "UpdatedAt" = @p29
WHERE "Id" = @p30 AND "RowVersion" = @p31;
UPDATE "RecipeIngredients" SET "OrderIndex" = @p32, "RowVersion" = @p33, "UpdatedAt" = @p34
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
- 0.9 ms, 1 câu:
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
- 0.8 ms, 1 câu:
```sql
INSERT INTO "RecipeSteps" ("Id", "CreatedAt", "Description", "ImageUrl", "IsDeleted", "RecipeId", "RowVersion", "StepNumber", "TimerMinutes", "Title", "UpdatedAt")
VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10);
```

### DELETE bước đầu (còn 6 phải đánh lại số)
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
- 0.7 ms, 7 câu:
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
