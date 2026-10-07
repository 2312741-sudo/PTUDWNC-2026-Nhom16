import { type Page, expect, test } from "@playwright/test";
import {
  apiUrl, auth, createRecipe, getDetail, makeJpeg, newUser, openEditor, signIn, stepButton, trackApiErrors,
} from "./support";

/**
 * C7 (TV3) — tự kiểm chức năng tuần 4 trên trình duyệt thật (API + Postgres + MinIO thật, không mock).
 * Mỗi test tự tạo user và dữ liệu riêng -> độc lập, chạy riêng lẻ được (`npx playwright test -g "<tên>"`).
 */

// Trace ghi tham số mọi lệnh (kể cả body đăng ký) -> tắt để mật khẩu không nằm trong test-results
test.use({ trace: "off" });

const CONFLICT_MSG = "Công thức vừa được thay đổi ở nơi khác";
/** Next.js luôn gắn một role=alert rỗng (next-route-announcer) -> chỉ lấy alert có chữ */
const alertWithText = (page: Page) => page.getByRole("alert").filter({ hasText: /\S/ });

test.describe("Wizard soạn công thức — tuần 4", () => {
  test("(a) để trống tiêu đề -> lỗi đúng ô tiêu đề, không gửi POST /recipes", async ({ page, request }) => {
    await signIn(page, await newUser(request));
    const posts: string[] = [];
    page.on("request", r => { if (r.method() === "POST" && /\/recipes\/?$/.test(new URL(r.url()).pathname)) posts.push(r.url()); });

    await page.goto("/dashboard/recipes/new");
    const category = page.getByLabel(/Danh mục/);
    await expect(category.locator("option").nth(1)).toBeAttached();
    await category.selectOption({ index: 1 });
    await page.getByRole("button", { name: /^Lưu & tiếp/ }).click();

    const title = page.getByPlaceholder("VD: Canh chua cá lóc");
    await expect(title).toHaveAttribute("aria-invalid", "true");
    await expect(title).toHaveAttribute("aria-describedby", "err-title");
    await expect(page.locator("#err-title")).toHaveText("Tiêu đề phải từ 5 đến 200 ký tự");
    await expect(title).toBeFocused();
    // Chỉ ô tiêu đề lỗi: các ô bắt buộc khác đã hợp lệ
    await expect(page.locator("[aria-invalid=true]")).toHaveCount(1);
    await page.waitForTimeout(1500); // đủ thời gian để một request (nếu có) kịp đi
    expect(posts, "không được gọi POST /recipes").toEqual([]);
    await expect(stepButton(page, 1)).toHaveAttribute("aria-current", "step");
  });

  test("(b) Lưu & tiếp -> 10 giây sau vẫn ở bước 2, URL /edit", async ({ page, request }) => {
    await signIn(page, await newUser(request));
    await page.goto("/dashboard/recipes/new");
    await page.getByPlaceholder("VD: Canh chua cá lóc").fill(`E2E Bánh xèo ${Date.now()}`);
    const category = page.getByLabel(/Danh mục/);
    await expect(category.locator("option").nth(1)).toBeAttached();
    await category.selectOption({ index: 1 });
    await page.getByRole("button", { name: /^Lưu & tiếp/ }).click();

    const editUrl = /\/dashboard\/recipes\/[0-9a-f-]{36}\/edit\?slug=/;
    await expect(stepButton(page, 2)).toHaveAttribute("aria-current", "step");
    await expect(page).toHaveURL(editUrl);
    await page.waitForTimeout(10_000);
    await expect(stepButton(page, 2)).toHaveAttribute("aria-current", "step");
    await expect(page.getByLabel("Tên nguyên liệu (dòng 1)")).toBeVisible();
    await expect(page).toHaveURL(editUrl);
  });

  test("(c) thêm 3 bước, xoá bước 2, thêm bước -> không 422, đánh số liền", async ({ page, request }) => {
    const s = await newUser(request);
    const r = await createRecipe(request, s);
    await signIn(page, s);
    const errors = trackApiErrors(page);
    await openEditor(page, r);
    await stepButton(page, 3).click();

    const list = page.locator("ol.space-y-2 > li");
    const addStep = async (title: string) => {
      await page.getByLabel("Tiêu đề bước (dòng 1)").fill(title);
      await page.getByLabel("Mô tả chi tiết (dòng 1)").fill(`Làm ${title.toLowerCase()} cẩn thận.`);
      await page.getByRole("button", { name: "Lưu bước", exact: true }).click();
      await expect(page.getByText(title, { exact: true })).toBeVisible();
      await expect(page.getByText("(đang lưu…)")).toHaveCount(0);
    };
    await addStep("Rửa rau");
    await addStep("Luộc tôm");
    await addStep("Cuốn bánh");
    await expect(list).toHaveCount(3);

    page.once("dialog", d => d.accept());
    await page.getByRole("button", { name: "Xoá bước 2: Luộc tôm" }).click();
    await expect(page.getByText("Luộc tôm", { exact: true })).toHaveCount(0);
    await expect(page.getByText("(đang lưu…)")).toHaveCount(0);
    await expect(list).toHaveCount(2);

    await addStep("Pha nước chấm");
    await expect(list).toHaveCount(3);
    await expect(alertWithText(page)).toHaveCount(0);
    expect(errors.filter(e => e.startsWith("422")), "không có 422").toEqual([]);
    expect(errors, "không có lỗi API nào").toEqual([]);

    const d = await getDetail(request, s, r.slug);
    const steps = [...d.steps].sort((a: { stepNumber: number }, b: { stepNumber: number }) => a.stepNumber - b.stepNumber);
    expect(steps.map((x: { stepNumber: number; title: string }) => [x.stepNumber, x.title]))
      .toEqual([[1, "Rửa rau"], [2, "Cuốn bánh"], [3, "Pha nước chấm"]]);
  });

  test("(d) tải 2 ảnh JPEG vào công thức có sẵn -> không 422, ảnh đầu là ảnh chính; .gif và > 5 MiB bị chặn", async ({ page, request }) => {
    const s = await newUser(request);
    const r = await createRecipe(request, s);
    await signIn(page, s);
    const errors = trackApiErrors(page);
    const uploads: number[] = [];
    page.on("response", res => {
      if (res.request().method() === "POST" && /\/images$/.test(res.url())) uploads.push(res.status());
    });
    await openEditor(page, r);
    await stepButton(page, 4).click();

    const file = page.getByLabel("Chọn ảnh");
    const uploadBtn = page.getByRole("button", { name: "Tải lên", exact: true });

    // Bị chặn ở client: không request nào đi
    await file.setInputFiles({ name: "anim.gif", mimeType: "image/gif", buffer: Buffer.from("GIF89a\x01\x00\x01\x00\x00\x00\x00;", "binary") });
    await uploadBtn.click();
    await expect(page.locator("#err-image-file")).toHaveText("Chỉ nhận ảnh JPEG, PNG, WebP hoặc AVIF");
    await expect(file).toHaveAttribute("aria-invalid", "true");

    const big = Buffer.alloc(5 * 1024 * 1024 + 1, 0);
    big.set([0xff, 0xd8, 0xff, 0xe0]);
    await file.setInputFiles({ name: "big.jpg", mimeType: "image/jpeg", buffer: big });
    await uploadBtn.click();
    await expect(page.locator("#err-image-file")).toHaveText("Ảnh tối đa 5 MiB");
    await page.waitForTimeout(500);
    expect(uploads, ".gif và > 5 MiB không được gửi lên server").toEqual([]);

    // Hai ảnh JPEG thật
    const jpegs = [await makeJpeg(page, "#e67e22"), await makeJpeg(page, "#27ae60")];
    for (const [i, buffer] of jpegs.entries()) {
      await file.setInputFiles({ name: `anh-${i + 1}.jpg`, mimeType: "image/jpeg", buffer });
      await expect(page.locator("#err-image-file")).toHaveCount(0);
      await page.getByLabel("Mô tả ảnh").fill(`Ảnh thử ${i + 1}`);
      await uploadBtn.click();
      await expect(page.getByText(`Ảnh thử ${i + 1}`, { exact: true })).toBeVisible();
      await expect(uploadBtn).toBeEnabled();
    }
    expect(uploads, "2 lần POST /images đều 201").toEqual([201, 201]);
    expect(errors.filter(e => e.startsWith("422")), "không có 422").toEqual([]);
    await expect(alertWithText(page)).toHaveCount(0);

    // Ảnh đầu là ảnh chính (đứng đầu lưới, có nhãn), ảnh hai không
    const cards = page.locator("ul.grid > li");
    await expect(cards).toHaveCount(2);
    await expect(cards.nth(0)).toContainText("Ảnh chính");
    await expect(cards.nth(0)).toContainText("Ảnh thử 1");
    await expect(cards.nth(1)).not.toContainText("Ảnh chính");

    // Ảnh nháp hiển thị được trong trình soạn: PA-3 (IMAGE_CONTRACT.md §7b, chốt 28/09) trả presignedUrl
    // ký trực tiếp vào RustFS/MinIO (không còn blob: như PA-2 cũ — PA-2 bị loại vì mất ảnh sau reload),
    // nên <img src> là URL ký thật (AuthImage chỉ đổi sang blob: khi qua proxy /resources/images/,
    // không áp dụng cho presignedUrl). Vẫn kiểm ảnh giải mã được (naturalWidth > 0) đúng ý nghĩa gốc.
    for (const alt of ["Ảnh thử 1", "Ảnh thử 2"]) {
      const img = page.getByRole("img", { name: alt });
      await expect(img).toHaveAttribute("src", /^https?:\/\/.+X-Amz-Signature=/);
      await expect.poll(() => img.evaluate((el: HTMLImageElement) => el.complete && el.naturalWidth > 0)).toBe(true);
    }

    const d = await getDetail(request, s, r.slug);
    const imgs = [...d.images].sort((a: { orderIndex: number }, b: { orderIndex: number }) => a.orderIndex - b.orderIndex);
    expect(imgs.map((i: { altText: string; isPrimary: boolean }) => [i.altText, i.isPrimary]))
      .toEqual([["Ảnh thử 1", true], ["Ảnh thử 2", false]]);
  });

  test("(d2) tải ảnh, xoá, tải lại -> không banner xung đột, ảnh mới là ảnh chính và hiện được", async ({ page, request }) => {
    const s = await newUser(request);
    const r = await createRecipe(request, s);
    await signIn(page, s);
    const errors = trackApiErrors(page);
    await openEditor(page, r);
    await stepButton(page, 4).click();

    const file = page.getByLabel("Chọn ảnh");
    const uploadBtn = page.getByRole("button", { name: "Tải lên", exact: true });
    const upload = async (alt: string, color: string) => {
      await file.setInputFiles({ name: `${color.slice(1)}.jpg`, mimeType: "image/jpeg", buffer: await makeJpeg(page, color) });
      await page.getByLabel("Mô tả ảnh").fill(alt);
      await uploadBtn.click();
      await expect(page.getByText(alt, { exact: true })).toBeVisible();
      await expect(uploadBtn).toBeEnabled();
    };

    await upload("Ảnh cũ", "#8e44ad");
    // Chờ job resize (D23) ghi xong ThumbnailUrl: job đổi RowVersion dòng ảnh, xoá trùng đúng lúc đó nhận 422 —
    // cuộc đua riêng thuộc job resize (đã ghi handoff), không phải lỗi "xoá rồi tải lại" mà ca này kiểm
    await expect.poll(async () => (await getDetail(request, s, r.id)).images[0]?.thumbnailUrl ?? null,
      { timeout: 15_000 }).not.toBeNull();
    page.once("dialog", d => d.accept());
    await page.getByRole("button", { name: "Xoá ảnh 1: Ảnh cũ" }).click();
    await expect(page.getByText("Chưa có ảnh. Ảnh đầu tiên sẽ tự thành ảnh chính.")).toBeVisible();
    await expect(uploadBtn).toBeEnabled();

    await upload("Ảnh mới", "#16a085");
    await expect(alertWithText(page)).toHaveCount(0);
    await expect(page.getByText(CONFLICT_MSG)).toHaveCount(0);
    expect(errors, "không có lỗi API nào (trước đây POST /images -> 422)").toEqual([]);

    const cards = page.locator("ul.grid > li");
    await expect(cards).toHaveCount(1);
    await expect(cards.nth(0)).toContainText("Ảnh chính");
    // PA-3 (IMAGE_CONTRACT.md §7b) — xem chú thích ở ca (d) phía trên.
    const img = page.getByRole("img", { name: "Ảnh mới" });
    await expect(img).toHaveAttribute("src", /^https?:\/\/.+X-Amz-Signature=/);
    await expect.poll(() => img.evaluate((el: HTMLImageElement) => el.complete && el.naturalWidth > 0)).toBe(true);

    const d = await getDetail(request, s, r.id);
    expect(d.images.map((i: { altText: string; isPrimary: boolean }) => [i.altText, i.isPrimary])).toEqual([["Ảnh mới", true]]);
  });

  test("(e) hai trình duyệt cùng sửa tiêu đề -> bên sau thấy xung đột và nút Tải dữ liệu mới nhất", async ({ browser, request }) => {
    const s = await newUser(request);
    const r = await createRecipe(request, s);
    const ctxA = await browser.newContext();
    const ctxB = await browser.newContext();
    try {
      const a = await ctxA.newPage();
      const b = await ctxB.newPage();
      await signIn(a, s);
      await signIn(b, s);
      await openEditor(a, r);
      await openEditor(b, r); // cả hai cùng giữ rowVersion ban đầu

      const titleA = `${r.title} (A sửa)`;
      await a.getByPlaceholder("VD: Canh chua cá lóc").fill(titleA);
      await a.getByRole("button", { name: /^Lưu & tiếp/ }).click();
      await expect(stepButton(a, 2)).toHaveAttribute("aria-current", "step");

      await b.getByPlaceholder("VD: Canh chua cá lóc").fill(`${r.title} (B sửa)`);
      await b.getByRole("button", { name: /^Lưu & tiếp/ }).click();
      await expect(alertWithText(b)).toContainText(CONFLICT_MSG);
      const reload = b.getByRole("button", { name: "Tải dữ liệu mới nhất" });
      await expect(reload).toBeVisible();
      await expect(stepButton(b, 1)).toHaveAttribute("aria-current", "step");

      await reload.click();
      await expect(b.getByPlaceholder("VD: Canh chua cá lóc")).toHaveValue(titleA);
      await expect(alertWithText(b)).toHaveCount(0);
      // Sau khi tải bản mới nhất, B lưu được (rowVersion mới)
      const titleB = `${r.title} (B sửa lại)`;
      await b.getByPlaceholder("VD: Canh chua cá lóc").fill(titleB);
      await b.getByRole("button", { name: /^Lưu & tiếp/ }).click();
      await expect(stepButton(b, 2)).toHaveAttribute("aria-current", "step");
      expect((await getDetail(request, s, r.id)).title).toBe(titleB);
    } finally {
      await ctxA.close();
      await ctxB.close();
    }
  });

  test("(f) mất mạng khi lưu nguyên liệu -> bảng hoàn tác, dòng nháp giữ nguyên chữ; có mạng lại thì lưu được", async ({ page, request }) => {
    const s = await newUser(request);
    const r = await createRecipe(request, s);
    await signIn(page, s);
    await openEditor(page, r);
    await stepButton(page, 2).click();

    const ingPost = /\/recipes\/[0-9a-f-]+\/ingredients$/;
    await page.route(ingPost, route => (route.request().method() === "POST" ? route.abort("internetdisconnected") : route.continue()));

    await page.getByLabel("Tên nguyên liệu (dòng 1)").fill("Nước mắm Phú Quốc");
    await page.getByLabel("Số lượng (dòng 1)").fill("2");
    await page.getByLabel("Đơn vị (dòng 1)").fill("muỗng");
    await page.getByLabel("Ghi chú (dòng 1)").fill("loại 40 độ đạm");
    await page.getByRole("button", { name: "Lưu", exact: true }).first().click();

    await expect(alertWithText(page)).toContainText("đã hoàn tác thay đổi");
    await expect(page.getByText("Chưa có nguyên liệu nào.")).toBeVisible();
    await expect(page.getByRole("cell", { name: /Nước mắm/ })).toHaveCount(0);
    await expect(page.getByLabel("Tên nguyên liệu (dòng 1)")).toHaveValue("Nước mắm Phú Quốc");
    await expect(page.getByLabel("Số lượng (dòng 1)")).toHaveValue("2");
    await expect(page.getByLabel("Đơn vị (dòng 1)")).toHaveValue("muỗng");
    await expect(page.getByLabel("Ghi chú (dòng 1)")).toHaveValue("loại 40 độ đạm");

    await page.unroute(ingPost);
    await page.getByRole("button", { name: "Lưu", exact: true }).first().click();
    await expect(page.getByRole("cell", { name: "Nước mắm Phú Quốc", exact: true })).toBeVisible();
    await expect(page.getByText("(đang lưu…)")).toHaveCount(0);
    await expect(alertWithText(page)).toHaveCount(0);
    const d = await getDetail(request, s, r.slug);
    expect(d.ingredients.map((i: { name: string; quantity: number; unit: string }) => [i.name, i.quantity, i.unit]))
      .toEqual([["Nước mắm Phú Quốc", 2, "muỗng"]]);
  });

  test("(g) xuất bản -> trang công khai có JSON-LD Recipe đúng, ảnh tuyệt đối trả 200", async ({ page, request }) => {
    const s = await newUser(request);
    const r = await createRecipe(request, s, { ingredients: 2, steps: 2 });
    await signIn(page, s);
    const up = await request.post(`${apiUrl}/recipes/${r.id}/images`, {
      headers: auth(s),
      multipart: { file: { name: "goi-cuon.jpg", mimeType: "image/jpeg", buffer: await makeJpeg(page, "#c0392b", 320) }, altText: "Gỏi cuốn" },
    });
    expect(up.status(), "upload ảnh qua API").toBe(201);

    await openEditor(page, r);
    await stepButton(page, 5).click();
    await page.getByRole("button", { name: "Xuất bản", exact: true }).click();
    const publicLink = page.getByRole("link", { name: /Xem trang công khai/ });
    await expect(publicLink).toBeVisible();
    await expect(page.getByText("Published", { exact: true })).toBeVisible();
    await publicLink.click();
    await expect(page).toHaveURL(new RegExp(`/recipes/${r.slug}$`));

    const raw = await page.locator('script[type="application/ld+json"]').allTextContents();
    const docs = raw.map(t => JSON.parse(t));
    const ld = docs.find(d => d["@type"] === "Recipe");
    expect(ld, "có JSON-LD @type Recipe").toBeTruthy();
    expect(ld.name).toBe(r.title);
    expect(ld.author?.["@type"]).toBe("Person");
    expect(typeof ld.author?.name).toBe("string");
    expect(ld.author.name.length).toBeGreaterThan(0);
    expect(ld.author.name, "author.name không lộ email").not.toContain("@");
    expect(ld).not.toHaveProperty("aggregateRating");
    expect(ld).not.toHaveProperty("review");
    expect(ld.recipeIngredient).toHaveLength(2);
    expect(ld.recipeInstructions).toHaveLength(2);

    expect(Array.isArray(ld.image) && ld.image.length > 0, "có image").toBe(true);
    for (const url of ld.image as string[]) {
      expect(url).toMatch(/^https?:\/\//);
      const res = await request.get(url); // không kèm token: trang công khai phải tải được
      expect(res.status(), `GET ${url}`).toBe(200);
      expect(res.headers()["content-type"]).toMatch(/^image\//);
    }
  });

  test("(h1) bấm Tiếp bằng bàn phím -> focus nhảy tới tiêu đề bước mới", async ({ page, request }) => {
    const s = await newUser(request);
    const r = await createRecipe(request, s, { ingredients: 1, steps: 1 });
    await signIn(page, s);
    await openEditor(page, r);

    await page.getByRole("button", { name: /^Lưu & tiếp/ }).focus();
    await page.keyboard.press("Enter");
    await expect(page.getByRole("heading", { level: 2, name: "Bước 2/5: Nguyên liệu" })).toBeFocused();

    for (const [n, name] of [[3, "Các bước"], [4, "Ảnh"], [5, "Xem lại & Xuất bản"]] as const) {
      await page.getByRole("button", { name: /^Tiếp/ }).focus();
      await page.keyboard.press("Enter");
      await expect(page.getByRole("heading", { level: 2, name: `Bước ${n}/5: ${name}` })).toBeFocused();
    }
    // Quay lại bằng Space cũng dời focus
    await page.getByRole("button", { name: /Quay lại/ }).focus();
    await page.keyboard.press("Space");
    await expect(page.getByRole("heading", { level: 2, name: "Bước 4/5: Ảnh" })).toBeFocused();
  });

  test("(h2) viewport 320/768/1200 -> không cuộn ngang ở cả 5 bước", async ({ page, request }) => {
    const s = await newUser(request);
    const r = await createRecipe(request, s, { ingredients: 3, steps: 3 });
    await signIn(page, s);
    const overflow: string[] = [];
    for (const width of [320, 768, 1200]) {
      await page.setViewportSize({ width, height: 900 });
      await openEditor(page, r);
      for (let n = 1; n <= 5; n++) {
        await stepButton(page, n).click();
        await expect(stepButton(page, n)).toHaveAttribute("aria-current", "step");
        const m = await page.evaluate(() => ({ sw: document.documentElement.scrollWidth, iw: window.innerWidth }));
        if (m.sw > m.iw) overflow.push(`${width}px bước ${n}: scrollWidth ${m.sw} > innerWidth ${m.iw}`);
      }
    }
    expect(overflow, "không được cuộn ngang").toEqual([]);
  });
});
