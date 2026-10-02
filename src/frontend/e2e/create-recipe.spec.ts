import { randomBytes } from "node:crypto";
import { type Locator, expect, test } from "@playwright/test";

/**
 * C7 (TV3) — luồng E2E "Create Recipe" (NFR-MAINT-002): đăng ký user mới qua API -> đăng nhập trên UI
 * -> bước 1 tạo công thức (Draft) -> bước 2 thêm, sửa, xoá nguyên liệu -> bước 3 thêm bước. API + DB thật, không mock.
 * Không dùng tài khoản seed (không có mật khẩu): mỗi lần chạy tự tạo user, mật khẩu sinh ngẫu nhiên trong test
 * và không in ra log/console.
 */
const apiUrl = process.env.E2E_API_URL ?? "http://localhost:5080/api/v1";

// Thoả RegisterValidator: >= 8 ký tự, có hoa, thường, số, ký tự đặc biệt
const newPassword = () => `Aa1!${randomBytes(18).toString("base64url")}`;

// Trace ghi tham số mọi lệnh -> tắt cho spec này để mật khẩu không nằm trong test-results
test.use({ trace: "off" });

/**
 * locator.fill() in giá trị vào tiêu đề bước của báo cáo HTML ("Fill \"...\"") -> mật khẩu bị lộ.
 * Gán qua setter gốc của HTMLInputElement + sự kiện input (React controlled input nhận được), bước chỉ hiện "Evaluate".
 */
async function typeSecret(input: Locator, secret: string) {
  await input.evaluate((el, v) => {
    Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, "value")!.set!.call(el, v);
    el.dispatchEvent(new Event("input", { bubbles: true }));
  }, secret);
}

test.describe("Soạn công thức qua wizard", () => {
  test("đăng ký, đăng nhập, tạo công thức, thêm/sửa/xoá nguyên liệu, thêm bước, xem lại", async ({ page, request }) => {
    const suffix = `${Date.now()}${randomBytes(3).toString("hex")}`;
    const email = `e2e.${suffix}@culinary.local`;
    const password = newPassword();
    const title = `E2E Canh chua cá lóc ${suffix}`;

    // Đăng ký qua API (body đúng record RegisterCommand, backend JSON strict)
    const reg = await request.post(`${apiUrl}/auth/register`, {
      data: { email, password, fullName: "E2E Tác giả", userName: `e2e${suffix}` },
    });
    expect(reg.status(), "đăng ký user E2E").toBe(201);

    // Đăng nhập trên UI
    await page.goto("/auth/login");
    await page.getByPlaceholder("name@example.com").fill(email);
    await typeSecret(page.getByPlaceholder("••••••••"), password);
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

    // Bước 2: thêm 2 nguyên liệu
    const saveRow = page.getByRole("button", { name: "Lưu", exact: true });
    const addIngredient = async (name: string, qty: string, unit: string) => {
      await page.getByLabel("Tên nguyên liệu (dòng 1)").fill(name);
      await page.getByLabel("Số lượng (dòng 1)").fill(qty);
      await page.getByLabel("Đơn vị (dòng 1)").fill(unit);
      await saveRow.click();
      await expect(page.getByRole("cell", { name, exact: true })).toBeVisible(); // hết "(đang lưu…)" = server đã nhận
    };
    await expect(page.getByLabel("Tên nguyên liệu (dòng 1)")).toBeVisible();
    await addIngredient("Cá lóc", "500", "g");
    await addIngredient("Me chua", "50", "g");

    // Sửa: số lượng cá lóc 500 -> 600 (PUT /ingredients/{id})
    const caLoc = page.getByRole("row", { name: /Cá lóc/ });
    await caLoc.getByRole("button", { name: "Sửa" }).click();
    await expect(page.getByText("Đang sửa: Cá lóc")).toBeVisible();
    await page.getByLabel("Số lượng (dòng 2)").fill("600");
    await page.getByRole("button", { name: "Cập nhật" }).click();
    await expect(caLoc.getByRole("cell", { name: "600", exact: true })).toBeVisible();
    await expect(page.getByText("Đang sửa: Cá lóc")).toHaveCount(0);

    // Xoá có confirm(): huỷ thì giữ nguyên, đồng ý thì mất dòng
    const meChua = page.getByRole("row", { name: /Me chua/ });
    page.once("dialog", d => d.dismiss());
    await meChua.getByRole("button", { name: "Xoá" }).click();
    await expect(meChua).toBeVisible();

    let confirmMessage = "";
    page.once("dialog", d => { confirmMessage = d.message(); return d.accept(); });
    await meChua.getByRole("button", { name: "Xoá" }).click();
    await expect(meChua).toHaveCount(0);
    expect(confirmMessage).toBe('Xoá nguyên liệu "Me chua"?');
    await expect(page.getByRole("cell", { name: "Cá lóc", exact: true })).toBeVisible();
    await page.getByRole("button", { name: /^Tiếp/ }).click();

    // Bước 3: các bước làm
    const stepTitle = page.getByLabel("Tiêu đề bước (dòng 1)");
    await expect(stepTitle).toBeVisible();
    await stepTitle.fill("Sơ chế cá");
    await page.getByLabel("Mô tả chi tiết (dòng 1)").fill("Làm sạch cá lóc, cắt khúc, ướp muối tiêu 10 phút.");
    await page.getByRole("button", { name: "Lưu bước", exact: true }).click();
    await expect(page.getByText("Sơ chế cá", { exact: true })).toBeVisible();
    await expect(page.getByText("(đang lưu…)")).toHaveCount(0);
    // Chỉ alert có chữ: Next.js luôn gắn một role=alert rỗng (next-route-announcer) cuối trang
    await expect(page.getByRole("alert").filter({ hasText: /\S/ })).toHaveCount(0);

    // Bước 4 (ảnh, bỏ qua) -> bước 5: xem lại
    await page.getByRole("button", { name: /^Tiếp/ }).click();
    await page.getByRole("button", { name: /^Tiếp/ }).click();
    await expect(page.getByRole("button", { name: /5\. Xem lại/ })).toHaveAttribute("aria-current", "step");
    await expect(page.getByRole("heading", { level: 2, name: title })).toBeVisible();
    await expect(page.getByRole("listitem").filter({ hasText: /^600 g Cá lóc$/ })).toBeVisible();
    await expect(page.getByRole("listitem").filter({ hasText: /^Sơ chế cá$/ })).toBeVisible();
    await expect(page.getByText("✓ Có ít nhất 1 nguyên liệu (1)")).toBeVisible();
    await expect(page.getByText("✓ Có ít nhất 1 bước thực hiện (1)")).toBeVisible();
    await expect(page.getByRole("button", { name: "Xuất bản", exact: true })).toBeEnabled();

    // Dữ liệu thật trên server: còn đúng 1 nguyên liệu (600 g) và 1 bước
    const slug = new URL(page.url()).searchParams.get("slug");
    const token = await page.evaluate(() => localStorage.getItem("accessToken"));
    const res = await request.get(`${apiUrl}/recipes/${slug}`, { headers: { Authorization: `Bearer ${token}` } });
    expect(res.ok()).toBeTruthy();
    const { data } = await res.json();
    expect(data.ingredients.map((i: { name: string; quantity: number }) => [i.name, i.quantity])).toEqual([["Cá lóc", 600]]);
    expect(data.steps).toHaveLength(1);
  });
});
