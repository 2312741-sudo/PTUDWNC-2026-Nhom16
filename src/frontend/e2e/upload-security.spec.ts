// N2-B3 + N2-B4 — kịch bản tấn công file và kiểm tra quyền upload ảnh.
//
// B3: file quá 5 MiB · `.exe` đổi tên `.jpg` (MIME giả) · file rỗng · JPEG cắt cụt ·
//     MIME khai báo không khớp nội dung · nhiều request upload liên tiếp.
//     Yêu cầu: từ chối đúng mã lỗi và **không lọt 500**.
// B4: khách (không token) và Author khác không được upload vào ảnh của công thức người khác.
//
// Điều kiện: backend (`E2E_API_URL`, mặc định http://localhost:5080) chạy ở Development với
// Postgres + Redis + S3 đã lên (upload thật, không mock).
import { expect, test } from '@playwright/test'
import {
  createPublishedRecipe,
  deleteRecipe,
  firstCategoryId,
  loginAsAuthor,
  loginAsE2eAuthor,
  OTHER_USER,
  uploadImage,
  type AuthedApi,
} from './api'

/** PNG 1×1 hợp lệ — dùng làm "file hợp lệ" để chứng minh các test tấn công không làm hỏng luồng. */
const VALID_PNG = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==',
  'base64',
)

/** Header Windows PE (`MZ`) — nội dung thật của một file `.exe`. */
const PE_EXECUTABLE = Buffer.concat([Buffer.from('MZ', 'ascii'), Buffer.alloc(512, 0x90)])

/**
 * JPEG bị cắt cụt: đúng 3 byte đầu (FFD8FF) nhưng thiếu phần còn lại.
 *
 * Đây là bằng chứng cho lỗ hổng đã vá: chữ ký JPEG chỉ dài **3 byte** nên file này khớp chữ ký
 * hoàn toàn và từng được nhận với `201`. Nay bị chặn bằng `ImageFormats.MinBytes` (xem `ImageUpload.cs`).
 */
const TRUNCATED_JPEG = Buffer.from([0xff, 0xd8, 0xff])

/** 5 MiB + 1 byte — vượt ngưỡng `ImageFormats.MaxBytes`. */
const OVERSIZED = Buffer.alloc(5 * 1024 * 1024 + 1, 0x41)

/**
 * Recipe dùng chung cho một describe: cần cả `id` (để gọi endpoint upload) lẫn `slug`.
 *
 * Phải dùng `slug` khi đọc chi tiết vì API **không có** route `GET /recipes/{id}` — chỉ có
 * `GET /recipes/{slug}`. Gọi bằng `id` trả 404 và làm test fail vì lý do sai.
 */
async function createRecipeForUpload(
  auth: AuthedApi,
): Promise<{ id: string; slug: string } | null> {
  const categoryId = await firstCategoryId(auth.ctx)
  if (!categoryId) return null
  const recipe = await createPublishedRecipe(
    auth.ctx,
    categoryId,
    `E2E upload ${Date.now().toString(36)}`,
  )
  return recipe ? { id: recipe.id, slug: recipe.slug } : null
}

/** Số ảnh đang gắn với recipe, đọc qua route chi tiết hợp lệ (`/{slug}`). */
async function countImages(ctx: AuthedApi['ctx'], slug: string): Promise<number> {
  const res = await ctx.get(`/api/v1/recipes/${slug}`)
  expect(res.status(), `GET /recipes/${slug} phải trả 200`).toBe(200)
  const body = await res.json()
  return (body.data.images ?? []).length
}

test.describe('N2-B3 · tấn công file upload ảnh', () => {
  let auth: AuthedApi
  let target: { id: string; slug: string } | null = null

  // `loginAsE2eAuthor` tự chờ và retry khi gặp 429 (rate-limit theo IP), nhưng bị chặn trong
  // ngân sách 20s nên không bao giờ kéo hook vượt timeout 30s mặc định.
  test.beforeAll(async () => {
    auth = await loginAsE2eAuthor()
  })

  test.afterAll(async () => {
    if (target) await deleteRecipe(auth.ctx, target.id)
    await auth?.ctx.dispose()
  })

  /** Tạo recipe một lần cho cả describe; `test.skip` nếu hết danh mục sống. */
  async function ensureTarget(): Promise<{ id: string; slug: string }> {
    target ??= await createRecipeForUpload(auth)
    test.skip(!target, 'Cần ít nhất một danh mục còn sống để tạo recipe')
    return target!
  }

  test('B3-1 · file vượt 5 MiB bị chặn bằng file.too_large, không 500', async () => {
    const { id } = await ensureTarget()

    const res = await uploadImage(auth.ctx, id, {
      name: 'huge.png',
      mimeType: 'image/png',
      buffer: OVERSIZED,
    })
    expect(res.status).toBe(400)
    expect(res.code).toBe('file.too_large')
    expect(res.status).not.toBe(500)
  })

  test('B3-2 · .exe đổi tên .jpg + khai image/jpeg vẫn bị chặn', async () => {
    const { id } = await ensureTarget()

    const res = await uploadImage(auth.ctx, id, {
      name: 'virus.jpg',
      mimeType: 'image/jpeg',
      buffer: PE_EXECUTABLE,
    })
    expect(res.status).toBe(400)
    expect(res.code).toBe('file.invalid_type')
  })

  test('B3-3 · file rỗng bị chặn bằng file.empty', async () => {
    const { id } = await ensureTarget()

    const res = await uploadImage(auth.ctx, id, {
      name: 'empty.png',
      mimeType: 'image/png',
      buffer: Buffer.alloc(0),
    })
    expect(res.status).toBe(400)
    expect(res.code).toBe('file.empty')
  })

  test('B3-4 · JPEG chỉ còn 3 byte bị chặn bằng file.too_small', async () => {
    const { id } = await ensureTarget()

    // Trước khi vá: trả 201. Magic bytes chỉ kiểm **tiền tố**, mà JPEG chỉ có 3 byte tiền tố,
    // nên file rỗng bị cắt cụt lọt qua biên API rồi mới chết ở job resize (lỗi ngoài request).
    const res = await uploadImage(auth.ctx, id, {
      name: 'cut.jpg',
      mimeType: 'image/jpeg',
      buffer: TRUNCATED_JPEG,
    })
    expect(res.status).toBe(400)
    expect(res.code).toBe('file.too_small')
    expect(res.status).not.toBe(500)
  })

  test('B3-5 · MIME khai báo không khớp nội dung bị chặn', async () => {
    const { id } = await ensureTarget()

    // Nội dung là PNG hợp lệ nhưng khai là JPEG → phải bị chặn, không được "tin Content-Type".
    const res = await uploadImage(auth.ctx, id, {
      name: 'lie.jpg',
      mimeType: 'image/jpeg',
      buffer: VALID_PNG,
    })
    expect(res.status).toBe(400)
    expect(res.code).toBe('file.invalid_type')
  })

  test('B3-6 · nhiều upload liên tiếp: 3 ảnh hợp lệ đều 201, không lọt 500', async () => {
    const { id, slug } = await ensureTarget()

    const statuses: number[] = []
    for (let i = 0; i < 3; i++) {
      const res = await uploadImage(auth.ctx, id, {
        name: `ok-${i}.png`,
        mimeType: 'image/png',
        buffer: VALID_PNG,
      })
      expect(res.status).toBe(201)
      statuses.push(res.status)
    }
    expect(statuses).toEqual([201, 201, 201])

    // Ảnh đầu phải tự động thành primary, còn lại không phải.
    const detail = await auth.ctx.get(`/api/v1/recipes/${slug}`)
    expect(detail.status()).toBe(200)
    const body = await detail.json()
    const images: { isPrimary: boolean }[] = body.data.images ?? []
    expect(images.filter((i) => i.isPrimary)).toHaveLength(1)
  })

  test('B3-7 · sau các đợt tấn công, API vẫn phục vụ bình thường (không 500)', async () => {
    const { id } = await ensureTarget()

    const res = await uploadImage(auth.ctx, id, {
      name: 'still-ok.png',
      mimeType: 'image/png',
      buffer: VALID_PNG,
    })
    expect(res.status).toBe(201)

    const search = await auth.ctx.get('/api/v1/recipes/search?q=khongtontai')
    expect(search.status()).toBe(200)
  })
})

test.describe('N2-B4 · quyền upload ảnh', () => {
  let owner: AuthedApi
  let other: AuthedApi
  let target: { id: string; slug: string } | null = null

  test.beforeAll(async () => {
    owner = await loginAsE2eAuthor()
    other = await loginAsAuthor(OTHER_USER.email, OTHER_USER.password, 'E2E Other Author')
    target = await createRecipeForUpload(owner)
  })

  test.afterAll(async () => {
    if (target) await deleteRecipe(owner.ctx, target.id)
    await owner?.ctx.dispose()
    await other?.ctx.dispose()
  })

  test('B4-1 · khách không có token bị 401', async ({ request }) => {
    test.skip(!target, 'Cần ít nhất một danh mục còn sống để tạo recipe')

    const anon = await request.post(
      `${process.env.E2E_API_URL ?? 'http://localhost:5080'}/api/v1/recipes/${target!.id}/images`,
      {
        multipart: {
          file: { name: 'anon.png', mimeType: 'image/png', buffer: VALID_PNG },
        },
      },
    )
    expect(anon.status()).toBe(401)
    expect(anon.status()).not.toBe(500)
  })

  test('B4-2 · Author khác bị 403 recipe.forbidden', async () => {
    test.skip(!target, 'Cần ít nhất một danh mục còn sống để tạo recipe')

    const res = await uploadImage(other.ctx, target!.id, {
      name: 'not-mine.png',
      mimeType: 'image/png',
      buffer: VALID_PNG,
    })
    expect(res.status).toBe(403)
    expect(res.code).toBe('recipe.forbidden')
    expect(res.status).not.toBe(500)
  })

  test('B4-3 · payload hợp lệ nhưng không đúng chủ vẫn bị chặn trước khi ghi ảnh', async () => {
    test.skip(!target, 'Cần ít nhất một danh mục còn sống để tạo recipe')

    const countBefore = await countImages(owner.ctx, target!.slug)

    const res = await uploadImage(other.ctx, target!.id, {
      name: 'sneaky.png',
      mimeType: 'image/png',
      buffer: VALID_PNG,
    })
    expect(res.status).toBe(403)

    // Bằng chứng mạnh hơn status 403: số ảnh không đổi, tức handler chặn *trước* khi ghi.
    expect(await countImages(owner.ctx, target!.slug)).toBe(countBefore)
  })
})
