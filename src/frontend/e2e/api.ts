// N2-B1/B2 — helper gọi API thật từ Playwright.
//
// Vì sao không chỉ bấm qua UI: TC8 cần hơn 12 món đã publish mới có "Trang sau", còn seed mặc
// định chỉ có tối đa 8 món khớp từ khoá. Tạo dữ liệu tiền đề qua API nhanh và xác định hơn là
// click 13 lần trên UI, rồi dọn lại để không lưu rác trong DB.
//
// Mọi tài khoản dùng ở đây là **tài khoản seed** (`DbSeeder`), mật khẩu chung `User@123456`.
import { APIRequestContext, request } from '@playwright/test'
import * as fs from 'node:fs'
import * as path from 'node:path'

export const API = process.env.E2E_API_URL ?? 'http://localhost:5080'

/**
 * Tài khoản E2E riêng, **không** dùng tài khoản seed.
 *
 * Vì sao: `DbSeeder` chỉ đặt mật khẩu khi *tạo mới* user (`if (user == null)`), nên trên một DB
 * đã có sẵn dữ liệu từ tuần trước, mật khẩu của tài khoản seed có thể **không** còn là
 * `User@123456`. Đã kiểm chứng: cả 5 tài khoản seed đều trả `401` với mật khẩu đó. Nếu E2E phụ
 * thuộc vào chúng thì test chỉ chạy được trên DB *hoàn toàn mới* — tức là không chạy được trên
 * môi trường của người khác.
 *
 * `/register` trả role `Author` ngay, đủ để tạo và publish recipe, nên helper tự đăng ký rồi đăng
 * nhập. Tài khoản được tạo 1 lần và tái dùng ở các lần chạy sau.
 */
export const E2E_USER = {
  email: process.env.E2E_USER_EMAIL ?? 'e2e.playwright@culinary.local',
  password: process.env.E2E_USER_PASSWORD ?? 'E2e@Test123456',
}

/**
 * Tài khoản **thứ hai**, dùng cho N2-B4 (quyền upload): cần một Author khác để chứng minh
 * không phải ai cũng sửa được ảnh của công thức người khác. Tách riêng khỏi `E2E_USER` để khi
 * một tài khoản hỏng thì phần còn lại vẫn chạy.
 */
export const OTHER_USER = {
  email: process.env.E2E_OTHER_EMAIL ?? 'e2e.other@culinary.local',
  password: process.env.E2E_OTHER_PASSWORD ?? 'E2e@Test123456',
}

export interface AuthedApi {
  ctx: APIRequestContext
  userId: string
  roles: string[]
}

/**
 * Token đã đăng nhập, cache theo email.
 *
 * Vì sao: API rate-limit **đăng nhập** theo IP (cửa sổ 60s, `Retry-After` ≈ 61). Mỗi lần `login` là
 * một "viên đạn", và 429 là lỗi *hạ tầng* chứ không phải lỗi sản phẩm — nhưng lại làm báo cáo E2E đỏ.
 *
 * Vì sao cache **trên đĩa** chứ không chỉ trong RAM: Playwright dựng worker process mới cho mỗi lần
 * chạy, nên `Map` ở phạm vi module biến mất mỗi lần `npx playwright test`. Đo thực tế: cache RAM
 * chỉ giữ được token trong một lần chạy, và chạy liên tiếp hai lần là lần thứ hai gặt 429 ngay ở
 * `beforeAll`. File cache sống qua các lần chạy nên chỉ phải đăng nhập lại khi token thật sự hết hạn.
 *
 * Đây là cache **cục bộ, chỉ chứa JWT của tài khoản test**, nằm trong `.playwright/` đã gitignore.
 */
interface CachedToken {
  token: string
  userId: string
  roles: string[]
  /** epoch ms; luôn sớm hơn hạn token thật một khoảng để không dùng token sắp hết hạn. */
  usableUntil: number
}

/** Hạ an toàn: coi token hết hạn sớm hơn 60s để không chạy vào lúc nó vừa chết. */
const TOKEN_EARLY_EXPIRY_MS = 60_000

const tokenCache = new Map<string, CachedToken>()

/** Cache trên đĩa dùng để sống qua các lần chạy Playwright khác nhau. */
const TOKEN_CACHE_FILE = path.resolve(__dirname, '..', '.playwright', 'token-cache.json')

function readTokenCache(): Record<string, CachedToken> {
  try {
    return JSON.parse(fs.readFileSync(TOKEN_CACHE_FILE, 'utf8')) as Record<string, CachedToken>
  } catch {
    // Không có file / file hỏng → coi như chưa cache gì.
    return {}
  }
}

function writeTokenCache(entry: Record<string, CachedToken>): void {
  try {
    fs.mkdirSync(path.dirname(TOKEN_CACHE_FILE), { recursive: true })
    fs.writeFileSync(TOKEN_CACHE_FILE, JSON.stringify(entry, null, 2), 'utf8')
  } catch {
    // Cache chỉ là tối ưu hóa: ghi hỏng không được làm test đỏ.
  }
}

/** Lấy `exp` (epoch giây) từ JWT mà không cần verify chữ ký — chỉ để biết token còn hạn không. */
function jwtExpiryMs(token: string): number {
  try {
    const payload = token.split('.')[1]
    if (!payload) return 0
    const json = JSON.parse(Buffer.from(payload, 'base64url').toString('utf8')) as {
      exp?: number
    }
    return typeof json.exp === 'number' ? json.exp * 1000 : 0
  } catch {
    return 0
  }
}

function rememberToken(email: string, token: string, userId: string, roles: string[]): void {
  const usableUntil = jwtExpiryMs(token) - TOKEN_EARLY_EXPIRY_MS
  tokenCache.set(email, { token, userId, roles, usableUntil })
  writeTokenCache({ ...readTokenCache(), [email]: { token, userId, roles, usableUntil } })
}

function recallToken(email: string): CachedToken | null {
  const inMemory = tokenCache.get(email)
  if (inMemory && inMemory.usableUntil > Date.now()) return inMemory

  const fromDisk = readTokenCache()[email]
  if (!fromDisk) return null
  // Token không giải mã được `exp` (0) thì không dùng — an toàn hơn là gọi API rồi mới 401.
  if (fromDisk.usableUntil <= Date.now()) return null

  tokenCache.set(email, fromDisk)
  return fromDisk
}

/**
 * Ngân sách thời gian tối đa cho một lần đăng nhập (kể cả các lần chờ do 429).
 *
 * Vì sao có trần: `beforeAll` của Playwright mặc định timeout 30s. Nếu cứ chờ hết `Retry-After`,
 * hook sẽ chết theo timeout và mọi biến (`auth`, `owner`) gán trong đó **không bao giờ được gán** —
 * lỗi sau đó là `Cannot read properties of undefined`, tức thông báo sai hoàn toàn so với nguyên nhân
 * thật (đang bị rate limit). Giữ hành vi này ở đây, thà lỗi nói thẳng "còn 429" trong 20 giây.
 */
const LOGIN_BUDGET_MS = 20_000

/** Đăng ký nếu chưa có, rồi đăng nhập. Trả về context đã gắn Bearer token. */
export async function loginAsE2eAuthor(): Promise<AuthedApi> {
  return loginAsAuthor(E2E_USER.email, E2E_USER.password, 'E2E Playwright')
}

/**
 * Đăng nhập rồi trả về **JWT thô** để nhét vào `localStorage` của trình duyệt.
 *
 * Vì sao cần: các trang wizard đọc phiên đăng nhập bằng `localStorage.getItem('accessToken')`
 * (`RecipeWizard.tsx:88`), còn {@link AuthedApi} chỉ giữ `APIRequestContext` — không lộ JWT ra ngoài.
 *
 * Vì sao **không** gõ form `/auth/login`: đăng nhập bị rate-limit theo IP, và mỗi test gõ form là
 * một lần gọi thật. Với nhiều test trong cùng một lần chạy thì sẽ dính 429 — đúng thứ mà
 * {@link loginAsAuthor} đã dành cả cache trên đĩa và ngân sách 20s để tránh. Ở đây ta lấy token
 * từ đúng cache đó nên không phát sinh thêm lần đăng nhập nào.
 */
export async function browserAccessToken(): Promise<string> {
  await loginAsE2eAuthor()
  const cached = recallToken(E2E_USER.email)
  if (!cached) {
    throw new Error(
      `Đã đăng nhập "${E2E_USER.email}" nhưng không đọc lại được token từ cache — ` +
        'cache trên đĩa ở .playwright/token-cache.json có thể đã bị xoá giữa chừng.',
    )
  }
  return cached.token
}

/** Bản tổng quát của {@link loginAsE2eAuthor} — dùng cho tài khoản thứ hai của N2-B4. */
export async function loginAsAuthor(
  email: string,
  password: string,
  displayName: string,
): Promise<AuthedApi> {
  const cached = recallToken(email)
  if (cached) {
    return buildContext(API, cached.token, cached.userId, cached.roles)
  }

  const anon = await request.newContext({ baseURL: API })

  // 409 = đã tồn tại từ lần chạy trước → bỏ qua, đăng nhập là được.
  await anon.post('/api/v1/auth/register', {
    data: { email, password, displayName },
  })

  // 429 = hết quota rate limit. Chờ theo `Retry-After` rồi thử lại: đây là hàng đợi tạm thời,
  // báo lỗi 429 ra báo cáo sẽ quy về nhầm cho sản phẩm bị lỗi.
  const maxAttempts = 6
  const startedAt = Date.now()
  for (let attempt = 1; attempt <= maxAttempts; attempt++) {
    const res = await anon.post('/api/v1/auth/login', {
      data: { email, password },
    })

    if (res.status() === 429) {
      const retryAfter = Number(res.headers()['retry-after'] ?? '0')
      const waitMs =
        Number.isFinite(retryAfter) && retryAfter > 0
          ? retryAfter * 1000 + 250
          : 500 * 2 ** (attempt - 1)

      // Không chờ vượt ngân sách: thà báo lỗi rõ ràng còn hơn để hook chết theo timeout.
      if (Date.now() - startedAt + waitMs > LOGIN_BUDGET_MS) {
        await anon.dispose()
        throw new Error(
          `Tài khoản "${email}" vẫn bị rate limit (429) sau ${attempt} lần thử; ` +
            `cần chờ thêm ~${Math.ceil(waitMs / 1000)}s nữa nhưng đã hết ngân sách ` +
            `${LOGIN_BUDGET_MS / 1000}s. Hãy chạy lại spec sau khi hết cửa sổ rate limit.`,
        )
      }

      await new Promise((resolve) => setTimeout(resolve, waitMs))
      continue
    }

    if (!res.ok()) {
      const body = await res.text()
      await anon.dispose()
      throw new Error(
        `Không đăng nhập được tài khoản E2E "${email}": HTTP ${res.status()} ${body}\n` +
          'Cần backend chạy ở Development và truy cập được DB.',
      )
    }

    const payload = await res.json()
    const token: string = payload.data.accessToken
    const userId = payload.data.user.id as string
    const roles = (payload.data.user.roles ?? []) as string[]
    rememberToken(email, token, userId, roles)
    await anon.dispose()
    return buildContext(API, token, userId, roles)
  }

  await anon.dispose()
  throw new Error(
    `Tài khoản "${email}" không đăng nhập được sau ${maxAttempts} lần thử vì rate limit (429).`,
  )
}

async function buildContext(
  baseURL: string,
  token: string,
  userId: string,
  roles: string[],
): Promise<AuthedApi> {
  const ctx = await request.newContext({
    baseURL,
    extraHTTPHeaders: { Authorization: `Bearer ${token}` },
  })
  return { ctx, userId, roles }
}

export interface CreatedRecipe {
  id: string
  slug: string
}

/**
 * Tạo rồi publish một recipe. Trả về `null` nếu bất kừ bước nào thất bại để test có thể `skip` với
 * lý do rõ ràng thay vì fail với lỗi khó hiểu.
 *
 * Lưu ý quan trọng: `POST /{id}/publish` trả **422 `RECIPE_PUBLISH_INCOMPLETE`** nếu recipe chưa có
 * cả nguyên liệu lẫn bước (điều kiện D3.1/D3.2). Phải thêm 2 thứ đó, nếu không recipe sẽ kẹt ở
 * `Draft` và mọi test tìm kiếm theo trạng thái Published sẽ không thấy nó.
 */
export async function createPublishedRecipe(
  ctx: APIRequestContext,
  categoryId: string,
  title: string,
): Promise<CreatedRecipe | null> {
  const created = await ctx.post('/api/v1/recipes', {
    data: {
      title,
      description: `Mô tả cho ${title} — tạo tự động bởi N2-B1/B2 E2E.`,
      instructions: 'Trộn đều, nấu chín, nếm vị.',
      prepTimeMinutes: 10,
      cookTimeMinutes: 20,
      servings: 4,
      difficulty: 1,
      categoryId,
      nutrition: null,
    },
  })
  if (!created.ok()) return null

  const body = await created.json()
  const id: string = body.data.id
  const slug: string = body.data.slug

  const ingredient = await ctx.post(`/api/v1/recipes/${id}/ingredients`, {
    data: { name: 'Nguyên liệu kiểm thử', quantity: 1, unit: 'phần', notes: null },
  })
  if (!ingredient.ok()) return null

  const step = await ctx.post(`/api/v1/recipes/${id}/steps`, {
    data: { title: 'Bước 1', description: 'Trộn và nấu', timerMinutes: 5, imageUrl: null },
  })
  if (!step.ok()) return null

  const published = await ctx.patch(`/api/v1/recipes/${id}/publish`)
  if (!published.ok()) return null

  return { id, slug }
}

/**
 * Lấy id danh mục **thật sự dùng được** (dùng làm `categoryId` khi tạo recipe).
 *
 * Vì sao không lấy thẳng `list[0]`: `GET /api/v1/categories` được cache 60 phút, còn
 * `POST /api/v1/recipes` kiểm tra danh mục bằng truy vấn thẳng DB (`category.not_found`). Nếu
 * danh mục bị xoá ngoài API (ví dụ dọn dữ liệu lab bằng SQL) thì cache còn giữ id cũ trong khi
 * DB đã không còn → mọi lần `POST /recipes` trả 404 và test chỉ `skip` với lý do mơ hồ.
 *
 * Vì vậy: duyệt danh sách và **xác minh** từng mục bằng `GET /api/v1/categories/{slug}` (đường này
 * đọc DB thật, không qua cache) cho tới khi tìm được danh mục còn sống. Trả `null` khi hết danh sách.
 *
 * Số lần dò bị chặn ở {@link MAX_CATEGORY_PROBES}: nếu cache danh mục bị nhiễm bởi dữ liệu lab cũ,
 * danh sách có thể chứa hàng trăm mục đã bị xoá. Dò hết sẽ tạo hàng trăm request → chạm rate limit
 * (429) → mọi request sau đó trả 429 và bị hiểu nhầm là "danh mục hỏng", khiến hàm trả `null` và
 * cả spec bị `skip` trong im lặng. Vì vậy 429 được ném ra như lỗi hạ tầng thật, còn số lần dó thì có trần.
 */
const MAX_CATEGORY_PROBES = 12

export async function firstCategoryId(ctx: APIRequestContext): Promise<string | null> {
  const res = await ctx.get('/api/v1/categories')
  if (!res.ok()) return null
  const body = await res.json()
  const list: unknown[] = body.data ?? body
  if (!Array.isArray(list) || list.length === 0) return null

  const rejected: string[] = []
  for (const candidate of (list as { id: string; slug: string }[]).slice(0, MAX_CATEGORY_PROBES)) {
    const one = await ctx.get(`/api/v1/categories/${encodeURIComponent(candidate.slug)}`)
    if (one.ok()) return candidate.id

    if (one.status() === 429) {
      throw new Error(
        `Bị rate limit (429) khi xác minh danh mục "${candidate.slug}" sau ` +
          `${rejected.length} mục đã bị loại. Hãy chạy lại spec sau khi hết cửa sổ rate limit.`,
      )
    }
    rejected.push(candidate.slug)
  }

  throw new Error(
    `Không tìm được danh mục còn sống trong ${Math.min(list.length, MAX_CATEGORY_PROBES)} ` +
      `mục đầu của ${list.length} mục. Các mục bị loại: ${rejected.join(', ')}. ` +
      'Dữ liệu danh mục có thể đang bị nhiễm hoặc danh mục thật đã bị xoá.',
  )
}

/** Xoá recipe đã tạo (best-effort — dọn dẹp, lỗi không được làm fail test). */
export async function deleteRecipe(ctx: APIRequestContext, id: string): Promise<void> {
  try {
    await ctx.delete(`/api/v1/recipes/${id}`)
  } catch {
    // Bỏ qua: dữ liệu test còn lại không được làm đỏ báo cáo.
  }
}

/**
 * Tạo đủ số recipe đã publish để từ khoá `keyword` có Nhiều hơn `pageSize` kết quả.
 * Tiền tố `E2E` giúp nhận ra dữ liệu test trong DB khi review.
 */
export async function seedEnoughPublishedRecipes(
  ctx: APIRequestContext,
  categoryId: string,
  keyword: string,
  pageSize = 12,
): Promise<{ created: CreatedRecipe[]; total: number }> {
  const keywordRe = new RegExp(keyword, 'iu')
  const probe = await ctx.get(`/api/v1/recipes/search?q=${encodeURIComponent(keyword)}&pageSize=${pageSize}`)
  let total = 0
  if (probe.ok()) {
    total = (await probe.json()).meta.total as number
  }

  const created: CreatedRecipe[] = []
  const stamp = Date.now().toString(36)

  for (let i = total; i <= pageSize; i++) {
    const title = `E2E ${keyword} món số ${stamp}-${i} để kiểm thử phân trang`
    const r = await createPublishedRecipe(ctx, categoryId, title)
    if (r) {
      created.push(r)
      if (keywordRe.test(title)) total++
    }
  }

  // Xác nhận lại từ API thay vì tin vào đếm của mình.
  const after = await ctx.get(`/api/v1/recipes/search?q=${encodeURIComponent(keyword)}&pageSize=${pageSize}`)
  const realTotal = after.ok() ? ((await after.json()).meta.total as number) : total

  return { created, total: realTotal }
}

export interface UploadPayload {
  /** Tên file gửi lên — nằm trong `Content-Disposition`, dùng để "đổi tên" file tấn công. */
  name: string
  /** `Content-Type` khai báo — có thể cố tình khai sai để kiểm tra magic bytes. */
  mimeType: string
  buffer: Buffer
}

/**
 * N2-B3: upload ảnh vào công thức (multipart, field tên `file`).
 *
 * Cố tình **không** dùng `FilePayload` sẵn có của Playwright vì cần kiểm soát chính xác `buffer`
 * và `mimeType` để dựng payload tấn công (`.exe` đổi tên `.jpg`, file rỗng, JPEG cắt cụt).
 */
export async function uploadImage(
  ctx: APIRequestContext,
  recipeId: string,
  file: UploadPayload,
): Promise<{ status: number; code?: string; message?: string }> {
  const res = await ctx.post(`/api/v1/recipes/${recipeId}/images`, {
    multipart: { file: { name: file.name, mimeType: file.mimeType, buffer: file.buffer } },
  })
  const text = await res.text()
  let code: string | undefined
  let message: string | undefined
  try {
    const body = JSON.parse(text)
    code = body?.code
    message = body?.detail ?? body?.message ?? body?.title
  } catch {
    // Không phải JSON: để test tự kết luận từ status.
  }
  return { status: res.status(), code, message }
}
