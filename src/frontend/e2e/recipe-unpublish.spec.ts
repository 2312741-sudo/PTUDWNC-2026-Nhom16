import { expect, test, type Page } from '@playwright/test'
import {
  browserAccessToken,
  createPublishedRecipe,
  deleteRecipe,
  firstCategoryId,
  loginAsE2eAuthor,
  type CreatedRecipe,
} from './api'

/**
 * W5-6 (N2-D2) — luồng quản lý trạng thái công thức ngay trên dashboard:
 *
 *   1. Gỡ đăng  : Published → Draft   (PATCH /recipes/{id}/unpublish)
 *   2. Lưu trữ  : Draft   → Archived  (PATCH /recipes/{id}/archive)
 *   3. Xoá      : soft delete → item biến mất khỏi danh sách (record vẫn còn trong DB)
 *
 * Before: nút Unpublish/Archive chưa từng có ở FE, chỉ mới có API ở backend
 * (`Program.cs` — trước đây chỉ có nút Publish trong wizard). Đây là nợ `N2-D1/D2` tuần 3
 * đã lead sang tuần 5 (xem `MO_TA_CONG_VIEC_TUAN_5.md` W5-6).
 *
 * Dữ liệu: tạo 1 recipe Published qua API (helper dùng chung), rồi đổi trạng thái bằng
 * **bấm nút thật** trên giao diện dashboard. Dọn ở afterAll bằng soft delete.
 */

const RUN = String(Date.now() % 1_000_000)
const TITLE = `Cà ri rút ngọn ${RUN}`

/** Tiêm phiên đăng nhập vào localStorage trước khi app hydrate (xem B1-1 recipe-publish). */
async function signIn(page: Page): Promise<void> {
  const token = await browserAccessToken()
  await page.addInitScript((accessToken: string) => {
    window.localStorage.setItem('accessToken', accessToken)
  }, token)
}

/** Hàng trong bảng "Công thức của tôi" khớp tiêu đề — slug unique nên chỉ có đúng một hàng. */
const rowOf = (page: Page) => page.locator('tbody tr', { hasText: TITLE })

test.describe('W5-6 · Quản lý trạng thái từ dashboard', () => {
  let categoryId = ''
  let created: CreatedRecipe | null = null
  let auth: Awaited<ReturnType<typeof loginAsE2eAuthor>>

  test.beforeAll(async () => {
    auth = await loginAsE2eAuthor()
    categoryId = (await firstCategoryId(auth.ctx)) ?? ''
    if (!categoryId) {
      throw new Error('Không có danh mục nào còn sống để tạo công thức kiểm thử.')
    }
    created = await createPublishedRecipe(auth.ctx, categoryId, TITLE)
    if (!created) {
      throw new Error('Không tạo được recipe Published để kiểm thử Gỡ đăng.')
    }
  })

  test.afterAll(async () => {
    if (created) await deleteRecipe(auth.ctx, created.id)
  })

  test('Gỡ đăng: Published → Bản nháp', async ({ page }) => {
    await signIn(page)
    await page.goto('/dashboard/recipes')
    await expect(rowOf(page)).toContainText('Đã đăng')

    page.once('dialog', (d) => d.accept())
    await rowOf(page).getByRole('button', { name: /^Gỡ đăng/ }).click()

    await expect(rowOf(page)).toContainText('Bản nháp')
    await expect(rowOf(page)).not.toContainText('Đã đăng')
    // Mục Đã đăng cũng phải giảm đi: recipe không còn nằm ở tab Published.
    await expect(rowOf(page).getByRole('button', { name: /^Gỡ đăng/ })).toHaveCount(0)
  })

  test('Lưu trữ: Bản nháp → Lưu trữ (mở lại từ tab Lưu trữ)', async ({ page }) => {
    await signIn(page)
    await page.goto('/dashboard/recipes')
    await expect(rowOf(page)).toContainText('Bản nháp')

    page.once('dialog', (d) => d.accept())
    await rowOf(page).getByRole('button', { name: /^Lưu trữ/ }).click()

    await expect(rowOf(page)).toContainText('Lưu trữ')
    // Recipe đã lưu trữ thì không còn nút Lưu trữ để bấm lại (tránh tác động lặp).
    await expect(rowOf(page).getByRole('button', { name: /^Lưu trữ/ })).toHaveCount(0)
  })

  test('Xoá: đồng ý hai bước → item biến mất khỏi danh sách', async ({ page }) => {
    await signIn(page)
    await page.goto('/dashboard/recipes')
    await expect(rowOf(page)).toContainText('Lưu trữ')

    page.once('dialog', (d) => d.accept())
    await rowOf(page).getByRole('button', { name: /^Xoá/ }).click()

    // Soft delete: danh sách không còn hàng này (API delete đã sẵn, W5-6 chỉ thêm nút).
    await expect(rowOf(page)).toHaveCount(0)
  })
})