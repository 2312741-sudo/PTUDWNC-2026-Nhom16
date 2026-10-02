import { expect, test } from "@playwright/test";

/**
 * C7 (TV3) — luồng E2E "Create Recipe" (NFR-MAINT-002): đăng nhập -> bước 1 tạo công thức (Draft)
 * -> bước 2 thêm nguyên liệu -> bước 3 thêm bước. Dữ liệu đi qua API + DB thật, không mock.
 * Mật khẩu lấy từ biến môi trường E2E_PASSWORD; thiếu thì test bị skip (không đoán mật khẩu).
 */
const email = process.env.E2E_EMAIL ?? "trung.huynh@culinary.local";
const password = process.env.E2E_PASSWORD;

test.describe("Soạn công thức qua wizard", () => {
  test.skip(!password, "Thiếu E2E_PASSWORD (tài khoản seed chưa có mật khẩu) -> BLOCKED, chưa chạy");

  test("đăng nhập, tạo công thức, thêm nguyên liệu và bước", async ({ page }) => {
    const title = `E2E Canh chua cá lóc ${Date.now()}`;

    // Đăng nhập
    await page.goto("/auth/login");
    await page.getByPlaceholder("name@example.com").fill(email);
    await page.getByPlaceholder("••••••••").fill(password!);
    await page.getByRole("button", { name: /^Đăng nhập/ }).click();
    await expect(page).toHaveURL(/\/dashboard\/profile/);

    // Bước 1: thông tin cơ bản -> POST /recipes (Draft)
    await page.goto("/dashboard/recipes/new");
    await expect(page.getByRole("heading", { level: 1, name: "Tạo công thức mới" })).toBeVisible();
    await page.getByPlaceholder("VD: Canh chua cá lóc").fill(title);
    await page.getByLabel(/Sơ chế \(phút\)/).fill("15");
    await page.getByLabel(/Nấu \(phút\)/).fill("20");
    await page.getByLabel(/Khẩu phần/).fill("4");
    const category = page.getByLabel(/Danh mục/);
    await expect(category.locator("option").nth(1)).toBeAttached(); // danh mục tải bất đồng bộ
    await category.selectOption({ index: 1 });
    await page.getByRole("button", { name: /^Lưu & tiếp/ }).click();

    // Bước 2: nguyên liệu
    const ingredientName = page.getByLabel("Tên nguyên liệu (dòng 1)");
    await expect(ingredientName).toBeVisible();
    await ingredientName.fill("Cá lóc");
    await page.getByLabel("Số lượng (dòng 1)").fill("500");
    await page.getByLabel("Đơn vị (dòng 1)").fill("g");
    await page.getByRole("button", { name: "Lưu", exact: true }).click();
    await expect(page.getByRole("cell", { name: "Cá lóc", exact: true })).toBeVisible(); // hết "(đang lưu…)" = server đã nhận
    await page.getByRole("button", { name: /^Tiếp/ }).click();

    // Bước 3: các bước làm
    const stepTitle = page.getByLabel("Tiêu đề bước (dòng 1)");
    await expect(stepTitle).toBeVisible();
    await stepTitle.fill("Sơ chế cá");
    await page.getByLabel("Mô tả chi tiết (dòng 1)").fill("Làm sạch cá lóc, cắt khúc, ướp muối tiêu 10 phút.");
    await page.getByRole("button", { name: "Lưu", exact: true }).click();
    await expect(page.getByText("Sơ chế cá", { exact: true })).toBeVisible();
    await expect(page.getByText("(đang lưu…)")).toHaveCount(0);
    await expect(page.getByRole("alert")).toHaveCount(0);
  });
});
