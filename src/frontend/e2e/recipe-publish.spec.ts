import { expect, test, type Page } from '@playwright/test'
import {
  browserAccessToken,
  createPublishedRecipe,
  deleteRecipe,
  firstCategoryId,
  loginAsE2eAuthor,
} from './api'

/**
 * N2-B1 — luồng "Xuất bản công thức" chạy qua **giao diện thật** (wizard 5 bước).
 *
 * Vì sao cần spec riêng: `search.spec.ts` và `upload-security.spec.ts` đều tạo dữ liệu **qua API**
 * rồi mới kiểm tra trang. Như vậy luồng publish thật (wizard → validate → lưu nháp → xuất bản →
 * trang công khai) chưa từng được chạy bởi ai. Đây là luồng do người dùng thật bấm tay, nên nếu
 * wizard hỏng thì mọi spec khác vẫn xanh — đúng kiểu "báo cáo xanh nhưng sản phẩm hỏng".
 *
 * Mọi bài kiểm tra ở đây đều đi qua UI (click, nhập liệu, điều hướng) chứ không gọi `fetch`.
 */

const WIZARD_NEW = '/dashboard/recipes/new'

/** Hậu tố để mỗi lần chạy ra một recipe khác nhau — tránh đụng unique index slug khi chạy lại. */
const TITLE_RUN_TAG = String(Date.now() % 1_000_000)

/**
 * Tiêm phiên đăng nhập vào `localStorage` **trước** khi app hydrate.
 *
 * Vì sao `addInitScript` chứ không `goto` rồi `evaluate`: `RecipeWizard.tsx:88` kiểm tra token ngay
 * lần render đầu và nếu thiếu thì `router.replace('/auth/login')` — tức là lúc đã gán token sau
 * cũng muộn. `addInitScript` chạy trước mọi script của trang nên token luôn kịp.
 */
async function signIn(page: Page): Promise<void> {
  const token = await browserAccessToken()
  await page.addInitScript((accessToken: string) => {
    window.localStorage.setItem('accessToken', accessToken)
  }, token)
}

/** Chờ wizard dựng xong bước đầu — dấu hiệu là tiêu đề form đã hiện. */
async function gotoWizard(page: Page): Promise<void> {
  await page.goto(WIZARD_NEW)
  await expect(page.getByRole('heading', { name: 'Tạo công thức mới' })).toBeVisible()
}

/** Thanh 5 bước nằm trong nav — scope hẹp để không đụng nút khác cùng tên bên ngoài. */
const stepTab = (page: Page, stepNumber: number) =>
  page.getByRole('navigation', { name: 'Các bước soạn công thức' }).getByRole('button', { name: new RegExp(`^${stepNumber}\\.`) })

/**
 * Bấm một ô trên thanh điều hướng 5 bước, ví dụ `gotoStep(page, 2)`.
 *
 * Vì sao phải xác nhận `aria-current="step"`: nếu chỉ `click()` rồi đi tiếp thì khi wizard **không**
 * chuyển bước, lỗi đầu tiên lộ ra là một cái `timeout` 60s ở ngay ô nhập của bước kế tiếp — dấu hiệu
 * hoàn toàn không liên quan tới nguyên nhân thật. Kiểm tra `aria-current` biến "wizard kẹt ở bước 1"
 * thành một lỗi nói đúng sự thật ngay tại chỗ.
 */
async function gotoStep(page: Page, stepNumber: number): Promise<void> {
  const tab = stepTab(page, stepNumber)
  await tab.click()
  await expect(tab).toHaveAttribute('aria-current', 'step')
}

/**
 * Badge trạng thái (Draft/Published) nằm cạnh tiêu đề ở bước 5.
 *
 * Vì sao phải đi qua `locator('..')`: `<h2>` chỉ chứa tên món, badge là `<span>` anh em ngay bên cạnh
 * (`ReviewStep.tsx:29`). Không có `data-testid` hay `aria-label` nào trên badge, nên bám vào cấu trúc
 * cha–con là lựa chọn ít giòn nhất.
 */
const statusBadge = (page: Page, title: string) =>
  page.getByRole('heading', { name: title, level: 2 }).locator('..')

/**
 * Nút "Xuất bản" thật ở bước 5.
 *
 * Vì sao bắt buộc `exact`: `getByRole` mặc định khớp **substring** không phân biệt hoa thường, mà trên
 * thanh bước đã có nút `5. Xem lại & Xuất bản` — cũng chứa chữ "Xuất bản". Thiếu `exact` thì Playwright
 * báo `strict mode violation` vì khớp 2 phần tử.
 */
const publishButton = (page: Page) =>
  page.getByRole('button', { name: 'Xuất bản', exact: true })

/**
 * Chờ ô nhập đầu tiên của bước 2/3 (dòng nháp — placeholder cố định, không đổi số dòng) đã sẵn sàng
 * **và không còn request nào bay nữa**.
 *
 * Vì sao dùng placeholder thay vì label: ô nhập của dòng nháp có `aria-label` động ("Tên nguyên liệu
 * (dòng 1)", "dòng 2", …) nên `getByLabel('Tên nguyên liệu')` khớp nhiều phần tử → strict violation,
 * trong khi placeholder (`Tên *`, `SL`, `Tiêu đề bước *`, …) chỉ tồn tại trên dòng nháp đang nhập.
 * `.last()` lấy đúng dòng nháp mới nhất (dòng cũ đã lưu thành dòng trong bảng, không còn input).
 *
 * Vì sao phải chờ `networkidle`: lần lưu nháp đầu tiên làm URL đổi từ `/dashboard/recipes/new` sang
 * `/dashboard/recipes/{id}/edit`, nên Next render lại và wizard được dựng lại một lần nữa. Nếu test
 * điền ô giữa lúc remount thì React thay thế cây component và **xoá sạch những gì vừa gõ** — biểu
 * hiện là `Tên nguyên liệu` nhận được rồi `Số lượng` thì không thấy. Chờ mạng rảnh là cách chắc chắn
 * remount đã xong trước khi bắt đầu nhập.
 */
async function settleDraftRow(page: Page, inputSelector: string): Promise<void> {
  const input = page.locator(inputSelector).last()
  await expect(input).toBeVisible()
  await page.waitForLoadState('networkidle')
  await expect(input).toBeVisible()
}

/**
 * Điền bước 1 — thông tin cơ bản.
 *
 * `categoryId` truyền vào thay vì chọn "mục đầu tiên" trong `<select>`: danh mục đầu tiên có thể
 * là danh mục mới thêm, và mục đó đã được `firstCategoryId()` dò sống trước đó, nên chọn theo `id`
 * giữ bài kiểm tra ổn định giữa các lần chạy.
 */
async function fillBasicInfo(
  page: Page,
  categoryId: string,
  title: string,
): Promise<void> {
  await page.getByLabel('Tiêu đề *').fill(title)
  // Nhãn này là động: "Mô tả (0/2000)" → "Mô tả (42/2000)". Regex neo đầu để không đụng các nhãn khác.
  await page.getByLabel(/^Mô tả/).fill('Món dễ làm cho người bận rộn, dùng nguyên liệu dễ kiếm.')
  await page.getByLabel('Sơ chế (phút) *').fill('15')
  await page.getByLabel('Nấu (phút) *').fill('25')
  await page.getByLabel('Khẩu phần *').fill('4')
  await page.getByLabel('Danh mục *').selectOption(categoryId)
}

/** Bấm "Lưu & tiếp" ở bước 1 — đây là lần gọi POST đầu tiên, tạo recipe ở trạng thái Draft. */
async function saveDraftAndAdvance(page: Page): Promise<void> {
  await page.getByRole('button', { name: 'Lưu & tiếp →' }).click()
}

/**
 * Lưu nháp rồi lấy id recipe từ URL.
 *
 * Vì sao phải lấy id: wizard đổi URL sang `/dashboard/recipes/{id}/edit` sau lần lưu đầu. Bài kiểm tra
 * nào cũng tạo ra một recipe thật trong DB, nên phải gom id lại để `afterAll` dọn — nếu không thì mỗi
 * lần chạy lại để lại một bản nháp mồ côi, và DB test phình dần theo số lần chạy.
 */
async function saveDraftAndGetId(page: Page): Promise<string> {
  await saveDraftAndAdvance(page)
  await page.waitForURL(/\/dashboard\/recipes\/[0-9a-f-]+\/edit/)
  const recipeId = page.url().match(/\/dashboard\/recipes\/([0-9a-f-]+)\/edit/)?.[1]
  expect(recipeId, 'URL sau khi lưu nháp phải chứa id recipe').toBeTruthy()
  return recipeId!
}

/** Thêm một nguyên liệu ở bước 2 — đổ vào dòng nháp (placeholder cố định) rồi bấm "Lưu". */
async function addIngredient(
  page: Page,
  name: string,
  quantity: string,
  unit: string,
): Promise<void> {
  await settleDraftRow(page, 'input[placeholder="Tên *"]')
  await page.locator('input[placeholder="Tên *"]').last().fill(name)
  await page.locator('input[placeholder="SL"]').last().fill(quantity)
  await page.locator('input[placeholder="Đơn vị"]').last().fill(unit)
  await page.getByRole('button', { name: 'Lưu', exact: true }).click()
}

/** Thêm một bước thực hiện ở bước 3 — dòng nháp có placeholder cố định, nút "Lưu bước". */
async function addStep(
  page: Page,
  title: string,
  description: string,
): Promise<void> {
  await settleDraftRow(page, 'input[placeholder="Tiêu đề bước *"]')
  await page.locator('input[placeholder="Tiêu đề bước *"]').last().fill(title)
  await page.locator('textarea[placeholder="Mô tả chi tiết *"]').last().fill(description)
  await page.getByRole('button', { name: 'Lưu bước', exact: true }).click()
}

test.describe('N2-B1 · Xuất bản công thức qua UI', () => {
  let categoryId = ''
  const createdRecipeIds: string[] = []

  test.beforeAll(async () => {
    const auth = await loginAsE2eAuthor()
    const found = await firstCategoryId(auth.ctx)
    if (!found) {
      throw new Error(
        'Không có danh mục nào còn sống để tạo công thức. ' +
          'Cần seed dữ liệu danh mục trước khi chạy spec publish.',
      )
    }
    categoryId = found
  })

  test.afterAll(async () => {
    if (createdRecipeIds.length === 0) return
    const auth = await loginAsE2eAuthor()
    for (const id of createdRecipeIds) await deleteRecipe(auth.ctx, id)
  })

  /**
   * B1-1 · Bước 1 chặn nhập liệu thiếu trước khi gọi API.
   *
   * Đây là lý do có cả lớp validate client: nếu chỉ kiểm ở server thì mỗi lần bấm "Lưu & tiếp"
   * với dữ liệu rỗng là một round-trip + một bản ghi Draft rác trong DB.
   */
  test('B1-1 · validate client chặn tiêu đề quá ngắn và thiếu danh mục', async ({ page }) => {
    await signIn(page)
    await gotoWizard(page)

    await page.getByLabel('Tiêu đề *').fill('abc')
    await page.getByLabel('Sơ chế (phút) *').fill('10')
    await page.getByLabel('Nấu (phút) *').fill('10')
    await page.getByLabel('Khẩu phần *').fill('2')

    await page.getByRole('button', { name: 'Lưu & tiếp →' }).click()

    // Lỗi zod hiện dưới chính ô nhập (`#err-title`) chứ không phải banner; banner chỉ nhận lỗi server.
    await expect(page.locator('#err-title')).toHaveText('Tiêu đề phải từ 5 đến 200 ký tự')
    await expect(page.getByLabel('Tiêu đề *')).toHaveAttribute('aria-invalid', 'true')
    // Còn nguyên ở bước 1 — không được nhảy bước.
    await expect(page.getByRole('heading', { name: 'Tạo công thức mới' })).toBeVisible()

    // Sửa tiêu đề cho hợp lệ rồi bỏ trống danh mục → phải bắt lỗi mới, không báo lỗi cũ.
    await page.getByLabel('Tiêu đề *').fill('Canh rau củ tìm khoảng 400')
    await page.getByRole('button', { name: 'Lưu & tiếp →' }).click()

    await expect(page.locator('#err-category')).toHaveText('Vui lòng chọn danh mục')
  })

  /**
   * B1-2 · Nút "Xuất bản" bị khoá khi recipe chưa đủ nguyên liệu + bước.
   *
   * Lưu ý: bài này **cố tình không** bấm nút — nếu nút bị khoá thì click chỉ là no-op và test xanh
   * giả. Ta kiểm đúng trạng thái `disabled` để chắc chắn ràng buộc này có thật trong UI.
   */
  test('B1-2 · nút Xuất bản bị khoá khi thiếu nguyên liệu và bước', async ({ page }) => {
    await signIn(page)
    await gotoWizard(page)

    const title = `Trà tắc lộc giới hạn ${TITLE_RUN_TAG}`
    await fillBasicInfo(page, categoryId, title)
    createdRecipeIds.push(await saveDraftAndGetId(page))

    // Bước 2: nguyên liệu
    await gotoStep(page, 2)
    await addIngredient(page, 'Lá lôi', '200', 'g')

    // Bước 3: chưa thêm bước nào
    await gotoStep(page, 3)
    await expect(page.getByText('Chưa có bước nào.')).toBeVisible()

    // Bước 5: xem lại — chưa đủ điều kiện.
    await gotoStep(page, 5)
    await expect(page.getByText('Có ít nhất 1 bước thực hiện')).toBeVisible()
    await expect(publishButton(page)).toBeDisabled()

    // Dọn: recipe nháp này không cần giữ (id đã nộp vào createdRecipeIds để afterAll xoá).
    await page.getByRole('button', { name: 'Về danh sách' }).click()
    await page.waitForURL(/\/dashboard\/recipes$/)
  })

  /**
   * B1-3 · Kịch bản trọn vẹn: lưu nháp → thêm nguyên liệu → thêm bước → **xuất bản** → trang công khai.
   *
   * Đây là bài quyết định N2-B1 đạt hay không.
   */
  test('B1-3 · publish trọn vẹn: nháp → nguyên liệu → bước → xuất bản → trang công khai', async ({
    page,
  }) => {
    await signIn(page)
    await gotoWizard(page)

    const title = `Phở bò gia đình ${TITLE_RUN_TAG}`

    // --- Bước 1: thông tin cơ bản → POST tạo recipe ---
    await fillBasicInfo(page, categoryId, title)
    createdRecipeIds.push(await saveDraftAndGetId(page))

    // 🟢 Chốt chặn hồi quy cho lỗi đã tìm ra: sau lần lưu đầu, wizard phải **tự** sang bước 2.
    // Trước khi sửa, việc đổi URL sang route `edit` khiến wizard bị dựng lại và quay về bước 1, nên
    // người dùng phải bấm "Nguyên liệu" lần nữa dù đã bấm "Lưu & tiếp". Chờ form xuất hiện trước rồi
    // mới kiểm bước: nếu lỗi quay lại bước 1 thì form này không bao giờ hiện và test đỏ đúng chỗ.
    await settleDraftRow(page, 'input[placeholder="Tên *"]')
    await expect(stepTab(page, 2)).toHaveAttribute('aria-current', 'step')

    // --- Bước 2: nguyên liệu (wizard đã tự sang bước này sau khi lưu) ---
    await expect(page.getByText('Chưa có nguyên liệu nào.')).toBeVisible()
    await addIngredient(page, 'Bắp hành', '3', 'cọng')
    await addIngredient(page, 'Bắp gừng', '50', 'g')
    // Hai nguyên liệu phải nằm trong bảng, không chỉ "đã gửi đi".
    // exact: ô "Sửa nguyên liệu Bắp hành / Xoá" cũng chứa tên món — substring sẽ strict violation.
    await expect(page.getByRole('cell', { name: 'Bắp hành', exact: true })).toBeVisible()
    await expect(page.getByRole('cell', { name: 'Bắp gừng', exact: true })).toBeVisible()

    // --- Bước 3: các bước ---
    await gotoStep(page, 3)
    await expect(page.getByText('Chưa có bước nào.')).toBeVisible()
    await addStep(page, 'Ninh xương', 'Hầm xương với 2 lít nước trong 2 giờ, hớt bọt.')
    await addStep(page, 'Pha nước lượng', 'Nêm nếm vừa ăn, thêm hành lá.')
    await expect(page.getByText('Ninh xương')).toBeVisible()

    // --- Bước 4: ảnh — bài này không upload ảnh, chỉ đi qua để xác nhận bước không chặn ---
    await gotoStep(page, 4)
    await expect(page.getByText('Chưa có ảnh.')).toBeVisible()

    // --- Bước 5: xem lại và xuất bản ---
    await gotoStep(page, 5)
    await expect(page.getByRole('heading', { name: title })).toBeVisible()

    // Trạng thái trước khi publish phải là Draft.
    await expect(statusBadge(page, title)).toContainText('Draft')

    const publishBtn = publishButton(page)
    await expect(publishBtn).toBeEnabled()
    await publishBtn.click()

    // Có dữ liệu đủ thì phải publish được; nếu server từ chối thì alert sẽ hiện và bài này đỏ.
    await expect(page.getByRole('link', { name: 'Xem trang công khai →' })).toBeVisible()
    await expect(statusBadge(page, title)).toContainText('Published')

    // --- Kiểm chứng bằng chứng độc lập: trang công khai phải thấy món này ---
    await page.getByRole('link', { name: 'Xem trang công khai →' }).click()
    await page.waitForURL(/\/recipes\/[^/]+$/)
    await expect(page.getByRole('heading', { name: title })).toBeVisible()

    // Trang công khai không được còn nhãn "bản nháp".
    await expect(page.getByText('Bản nháp')).toHaveCount(0)

    // --- Trang "Công thức của tôi" phải phản ánh trạng thái Published ---
    await page.goto('/dashboard/recipes')
    await expect(page.getByRole('link', { name: 'Viết công thức mới' })).toBeVisible()
    const row = page.getByRole('row').filter({ hasText: title })
    await expect(row).toBeVisible()
    await expect(row).toContainText('Đã đăng')
    await expect(row.getByRole('link', { name: 'Xem' })).toBeVisible()
  })

  /**
   * B1-4 · Chỉnh sửa sau khi xuất bản rồi gỡ xuất bản — trạng thái phải quay về Draft.
   *
   * Vì sao đáng test: gỡ xuất bản là hành vi nguy hiểm nếu làm sai — nếu `PATCH /unpublish` trả
   * thành công nhưng UI vẫn hiện "Đã đăng" thì người dùng tưởng còn hiện trên site.
   */
  test('B1-4 · sau khi publish, trang chi tiết hiện đúng trạng thái Published', async ({
    page,
  }) => {
    await signIn(page)

    // Tạo + publish bằng API để bài này tập trung vào *hiển thị trạng thái*, không lặp lại wizard.
    const auth = await loginAsE2eAuthor()
    const created = await createPublishedRecipe(
      auth.ctx,
      categoryId,
      `Bánh mì sạch ${TITLE_RUN_TAG}`,
    )
    if (!created) {
      throw new Error('Không tạo được recipe đã xuất bản để kiểm tra trạng thái.')
    }
    createdRecipeIds.push(created.id)

    // Trang edit mở ở bước 1, mà badge trạng thái chỉ nằm ở bước 5 → phải bấm chuyển bước.
    await page.goto(`/dashboard/recipes/${created.id}/edit?slug=${created.slug}`)
    await page.waitForURL(/\/dashboard\/recipes\/[0-9a-f-]+\/edit/)
    await expect(page.getByRole('heading', { name: 'Sửa công thức' })).toBeVisible()
    await gotoStep(page, 5)

    await expect(page.getByRole('heading', { name: `Bánh mì sạch ${TITLE_RUN_TAG}` })).toBeVisible()
    await expect(statusBadge(page, `Bánh mì sạch ${TITLE_RUN_TAG}`)).toContainText('Published')
    // Đã publish thì không còn nút "Xuất bản" để bấm lần nữa.
    await expect(publishButton(page)).toHaveCount(0)

    // Trang danh sách của tôi: có nút "Xem" (chỉ xuất hiện khi Published).
    await page.goto('/dashboard/recipes')
    const row = page.getByRole('row').filter({ hasText: `Bánh mì sạch ${TITLE_RUN_TAG}` })
    await expect(row).toContainText('Đã đăng')
    await expect(row.getByRole('link', { name: 'Xem' })).toBeVisible()
  })
})

