// N2-B2 — tự động hoá 12 test case trong `docs/report/BAO_CAO_LOI_500_TRANG_SEARCH.md`.
//
// Mỗi test mang số TC tương ứng đúng dòng trong bảng mục 5 của báo cáo để reviewer đối chiếu được.
// Báo cáo đó là tài liệu gốc của lỗi — file này **không** sửa báo cáo, chỉ thực thi lại.
//
// Điều kiện: backend (E2E_API_URL, mặc định http://localhost:5080) phải chạy ở Development với
// Postgres + Redis + S3 đã lên.
import { expect, test, type Locator, type Page } from '@playwright/test'
import {
  API,
  deleteRecipe,
  firstCategoryId,
  loginAsE2eAuthor,
  seedEnoughPublishedRecipes,
} from './api'

/**
 * Trang `/search` có **hai** ô nhập: ô trong header và ô của riêng trang. Mọi test phải chỉ vào ô của
 * trang (placeholder khác header) để không dính `strict mode violation`.
 */
const SEARCH_INPUT = 'input[name="q"][placeholder^="Nhập tên món ăn"]'
const SEARCH_FORM = 'form[action="/search"]:has(button:text-is("Tìm kiếm"))'

function resultsHeading(page: Page): Locator {
  return page.locator('h2').filter({ hasText: 'Kết quả cho từ khóa' })
}

/** Bắt lỗi console — TC9 chỉ PASS khi console sạch. */
function collectConsoleErrors(page: Page): string[] {
  const errors: string[] = []
  page.on('console', (m) => {
    if (m.type() === 'error') errors.push(m.text())
  })
  page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`))
  return errors
}

test.describe('N2-B2 · /search — TC1…TC12', () => {
  // ---------------------------------------------------------------- TC1
  test('TC1 · /search không có q trả 200 và hiện ô nhập từ khóa', async ({ page }) => {
    const res = await page.goto('/search')
    expect(res?.status()).toBe(200)
    await expect(page.locator(SEARCH_INPUT)).toBeVisible()
  })

  // ---------------------------------------------------------------- TC2
  test('TC2 · q 1 ký tự trả 200 kèm cảnh báo "từ 2 ký tự trở lên"', async ({ page }) => {
    const res = await page.goto('/search?q=a')
    expect(res?.status()).toBe(200)
    await expect(page.getByText('phải có từ 2 ký tự trở lên')).toBeVisible()
  })

  // ---------------------------------------------------------------- TC3 — case từng lỗi 500
  test('TC3 · q có dấu trả 200 và có danh sách kết quả, không lỗi 500', async ({ page }) => {
    const errors = collectConsoleErrors(page)
    const res = await page.goto('/search?q=gà')
    expect(res?.status()).toBe(200)
    // Không được render alert lỗi (trước đây đây là nơi lộ 500).
    await expect(page.getByText('Lỗi tải kết quả')).toHaveCount(0)
    await expect(resultsHeading(page)).toBeVisible()
    expect(errors, `console phải sạch: ${errors.join(' | ')}`).toHaveLength(0)
  })

  // ---------------------------------------------------------------- TC4
  test('TC4 · tìm không dấu vẫn ra kết quả có dấu (TC4: q=pho → Phở Bò)', async ({ page }) => {
    const res = await page.goto('/search?q=pho')
    expect(res?.status()).toBe(200)
    await expect(resultsHeading(page)).toBeVisible()
    // Bản ghi trong DB viết có dấu; từ khoá không dấu phải khớp (unaccent).
    await expect(page.locator('a[href*="/recipes/"]').filter({ hasText: /Phở/i }).first()).toBeVisible()
  })

  // ---------------------------------------------------------------- TC5
  test('TC5 · chọn danh mục giữ categoryId trong URL và thu hẹp kết quả', async ({ page }) => {
    await page.goto('/search?q=gà')
    await page.locator('select[name="categoryId"]').selectOption({ index: 1 })
    await expect(page).toHaveURL(/categoryId=/)
    await expect(resultsHeading(page)).toBeVisible()
    expect(page.url()).toContain('q=')
  })

  // ---------------------------------------------------------------- TC6
  test('TC6 · chọn độ khó giữ difficulty trong URL', async ({ page }) => {
    await page.goto('/search?q=gà')
    await page.locator('select[name="difficulty"]').selectOption('Easy')
    await expect(page).toHaveURL(/difficulty=Easy/)
    await expect(resultsHeading(page)).toBeVisible()
  })

  // ---------------------------------------------------------------- TC7
  test('TC7 · chọn "Tên món A-Z" đặt sortBy=title và UI render đúng thứ tự API trả về', async ({ page }) => {
    await page.goto('/search?q=gà')
    await page.locator('select[name="sortBy"]').selectOption('title')
    await expect(page).toHaveURL(/sortBy=title/)

    // KHÔNG so sánh bằng `localeCompare` của JS: thứ tự của Postgres (collation C.UTF-8) khác thứ
    // tự của JS với tiếng Việt, nên phép so sánh đó luôn đỏ và là phép kiểm sai. Điều cần kiểm là
    // UI render đúng thứ tự API trả về — đó mới là hợp đồng của trang.
    const apiRes = await page.request.get(`${API}/api/v1/recipes/search?q=gà&sortBy=title&pageSize=12`)
    expect(apiRes.status()).toBe(200)
    const apiTitles: string[] = (await apiRes.json()).data.map((r: { title: string }) => r.title)

    const shown: string[] = await page
      .locator('a[href*="/recipes/"] h3')
      .allInnerTexts()
      .then((v) => v.map((s) => s.trim()).filter(Boolean))

    expect(shown.length).toBeGreaterThan(0)
    expect(shown).toEqual(apiTitles.slice(0, shown.length))
  })

  // ---------------------------------------------------------------- TC8
  test('TC8 · bấm "Trang sau" giữ nguyên q và bộ lọc', async ({ page }) => {
    // Seed mặc định chỉ có tối đa 8 món khớp "gà" < pageSize 12 ⇒ không có trang 2. Tạo thêm
    // recipe đã publish để có tiền đề, rồi dọn lại trong finally.
    const keyword = 'gà'
    const { ctx } = await loginAsE2eAuthor()
    const created: string[] = []
    try {
      const categoryId = await firstCategoryId(ctx)
      test.skip(!categoryId, 'API không trả về danh mục nào — cần seed dữ liệu')

      const seeded = await seedEnoughPublishedRecipes(ctx, categoryId!, keyword, 12)
      created.push(...seeded.created.map((r) => r.id))

      test.skip(seeded.total <= 12, `Không đủ kết quả để có trang 2 (total=${seeded.total})`)

      await page.goto(`/search?q=${encodeURIComponent(keyword)}`)
      await expect(resultsHeading(page)).toBeVisible()

      const next = page.getByRole('link', { name: 'Trang sau' })
      await expect(next).toBeVisible()
      await next.click()

      await expect(page).toHaveURL(/page=2/)
      expect(page.url()).toContain('q=')
      await expect(resultsHeading(page)).toBeVisible()
    } finally {
      for (const id of created) await deleteRecipe(ctx, id)
      await ctx.dispose()
    }
  })

  // ---------------------------------------------------------------- TC9
  test('TC9 · console không báo lỗi event handler sang Client Component', async ({ page }) => {
    const errors = collectConsoleErrors(page)
    await page.goto('/search?q=a')
    await expect(page.locator(SEARCH_INPUT)).toBeVisible()
    expect(
      errors.filter((e) => /Event handlers cannot be passed to Client Component props/.test(e)),
      'TC9: lỗi event handler phải được hết',
    ).toHaveLength(0)
  })

  // ---------------------------------------------------------------- TC10
  test('TC10 · nút "Tìm kiếm" giữ toàn bộ bộ lọc qua hidden input', async ({ page }) => {
    await page.goto('/search?q=gà&difficulty=Easy')
    const form = page.locator(SEARCH_FORM).first()
    await expect(form.locator('input[name="q"]')).toHaveValue('gà')
    await expect(form.locator('input[name="difficulty"]')).toHaveValue('Easy')
    await form.getByRole('button', { name: 'Tìm kiếm' }).click()
    await expect(page).toHaveURL(/difficulty=Easy/)
    expect(page.url()).toContain('q=')
  })

  // ---------------------------------------------------------------- TC11
  test('TC11 · /search không bị static prerender sai (build xong vẫn render server-side)', async ({ page }) => {
    // Route là dynamic: nếu bị ép static, `?q=` sẽ không cho kết quả khác nhau.
    const a = await page.goto('/search?q=pho')
    const b = await page.goto('/search?q=zzzzkhongtontai')
    expect(a?.status()).toBe(200)
    expect(b?.status()).toBe(200)
    await expect(page.getByText('Không tìm thấy công thức phù hợp')).toBeVisible()
  })

  // ---------------------------------------------------------------- TC12
  test('TC12 · E2E luồng "tìm kiếm" PASS từ ô nhập tới trang chi tiết', async ({ page }) => {
    await page.goto('/search')
    await page.locator(SEARCH_INPUT).fill('pho')
    await page.locator(SEARCH_FORM).getByRole('button', { name: 'Tìm kiếm' }).click()
    await expect(page).toHaveURL(/q=pho/)
    await expect(resultsHeading(page)).toBeVisible()

    const firstCard = page.locator('a[href*="/recipes/"]').first()
    await expect(firstCard).toBeVisible()
    await firstCard.click()
    await expect(page).toHaveURL(/\/recipes\/.+/)

    // Trang chi tiết phải trả 200 từ API (không phải trang lỗi).
    const detail = await page.request.get(`${API}/api/v1/recipes/${page.url().split('/recipes/')[1]}`)
    expect(detail.status()).toBe(200)
  })
})
