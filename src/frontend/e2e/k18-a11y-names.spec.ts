import { expect, test } from "@playwright/test";
import { createRecipe, newUser, openEditor, signIn, stepButton } from "./support";

/**
 * K18 (TV3) — kiểm TÊN TRUY CẬP (accessible name) thật của các phần tử NVDA từng đọc sai (log
 * `docs/evidence/TV3/Tuan04/K18_nvda_speech_log.txt`), bằng Playwright thật (không đoán, không đọc code suông).
 * Không ghi K18 XONG chỉ vì spec này xanh — còn phải nghe lại NVDA thật (N4/N8/N13/N14/N16/N17, xem LAB_K18_checklist.md).
 */
test.describe("K18 — tên truy cập (accessible name)", () => {
  test("Ô Mô tả: tên KHÔNG chứa bộ đếm '/2000', bộ đếm nằm ở phần tử phụ (aria-describedby)", async ({ page, request }) => {
    const s = await newUser(request);
    const r = await createRecipe(request, s);
    await signIn(page, s);
    await openEditor(page, r);

    const desc = page.getByLabel("Mô tả", { exact: true });
    await expect(desc).toBeVisible();
    // Tên truy cập thật qua ARIA snapshot — không lẫn số đếm.
    const snap = await desc.ariaSnapshot();
    console.log("[K18] Mô tả — ariaSnapshot:", snap);
    expect(snap, "tên ô Mô tả không chứa '/2000'").not.toContain("/2000");

    await desc.fill("Một đoạn mô tả thử để xem bộ đếm có đổi tên ô không.");
    const snapAfter = await desc.ariaSnapshot();
    console.log("[K18] Mô tả sau khi gõ — ariaSnapshot:", snapAfter);
    expect(snapAfter, "gõ xong tên ô vẫn không đổi, không chứa số đếm").not.toContain("/2000");

    // K18 N16 (NVDA 09/10/2026, build C9-tuan6): nối describedby thẳng tới bộ đếm sống (N/2000) khiến NVDA đọc
    // lại sau MỖI phím dù bộ đếm không có aria-live — nội dung node bị describedby đổi theo từng phím là đủ để
    // trình duyệt báo "mô tả đã đổi" cho ô đang focus. Sửa: describedby trỏ tới câu TĨNH không đổi khi gõ; bộ
    // đếm số vẫn hiển thị cho người nhìn thấy nhưng ẩn khỏi cây accessibility (aria-hidden).
    const describedBy = await desc.getAttribute("aria-describedby");
    expect(describedBy, "ô Mô tả phải có aria-describedby").toBeTruthy();
    const hint = page.locator(`#${describedBy!.split(" ")[0]}`);
    await expect(hint).toHaveText("Tối đa 2000 ký tự.");
    console.log("[K18] Nội dung mô tả tĩnh (describedby):", await hint.textContent());

    const counter = page.locator("#description-count");
    await expect(counter).toContainText("/2000");
    await expect(counter).toHaveAttribute("aria-hidden", "true");
    console.log("[K18] Bộ đếm số (chỉ hiển thị thị giác, aria-hidden):", await counter.textContent());
  });

  test("Ô Tiêu đề: tên không chứa số đếm (đối chiếu)", async ({ page, request }) => {
    const s = await newUser(request);
    const r = await createRecipe(request, s);
    await signIn(page, s);
    await openEditor(page, r);
    const title = page.getByLabel("Tiêu đề", { exact: false });
    const snap = await title.first().ariaSnapshot();
    console.log("[K18] Tiêu đề — ariaSnapshot:", snap);
    expect(snap).not.toMatch(/\/\d+/);
  });

  test("Bước Nguyên liệu: nút Sửa/Xoá có tên đầy đủ, đúng dấu cách, kèm tên nguyên liệu", async ({ page, request }) => {
    const s = await newUser(request);
    const r = await createRecipe(request, s, { ingredients: 1 });
    await signIn(page, s);
    await openEditor(page, r);
    await stepButton(page, 2).click();

    const editBtn = page.getByRole("button", { name: /^Sửa nguyên liệu /i }).first();
    await expect(editBtn).toBeVisible();
    const name = await editBtn.evaluate(el => el.getAttribute("aria-label") ?? el.textContent);
    console.log("[K18] Nút Sửa nguyên liệu — tên truy cập thật:", name);
    expect(name, "phải có dấu cách giữa 'Sửa' và 'nguyên liệu' (không dính 'Sửanguyên liệu')")
      .toMatch(/^Sửa nguyên liệu /);
    expect(name).not.toMatch(/Sửanguyên/);

    const delBtn = page.getByRole("button", { name: /^Xoá nguyên liệu /i }).first();
    const delName = await delBtn.evaluate(el => el.getAttribute("aria-label") ?? el.textContent);
    console.log("[K18] Nút Xoá nguyên liệu — tên truy cập thật:", delName);
    expect(delName).toMatch(/^Xoá nguyên liệu /);
  });

  test("Bước Các bước: nút Sửa/Xoá có tên đầy đủ kèm số thứ tự + tiêu đề bước", async ({ page, request }) => {
    const s = await newUser(request);
    const r = await createRecipe(request, s, { steps: 1 });
    await signIn(page, s);
    await openEditor(page, r);
    await stepButton(page, 3).click();

    const editBtn = page.getByRole("button", { name: /^Sửa bước \d+: /i }).first();
    await expect(editBtn).toBeVisible();
    const name = await editBtn.evaluate(el => el.getAttribute("aria-label") ?? el.textContent);
    console.log("[K18] Nút Sửa bước — tên truy cập thật:", name);
    expect(name).toMatch(/^Sửa bước \d+: .+/);
  });

  test("Dashboard: liên kết Sửa/Xoá kèm tên công thức", async ({ page, request }) => {
    const s = await newUser(request);
    const r = await createRecipe(request, s);
    await signIn(page, s);
    await page.goto("/dashboard/recipes");
    await expect(page.getByText(r.title, { exact: false }).first()).toBeVisible();

    const editLink = page.getByRole("link", { name: new RegExp(`^Sửa công thức .*${escapeRe(r.title)}`) });
    await expect(editLink).toBeVisible();
    const name = await editLink.evaluate(el => el.getAttribute("aria-label") ?? el.textContent);
    console.log("[K18] Dashboard — liên kết Sửa — tên truy cập thật:", name);
    expect(name, "liên kết Sửa phải kèm tên công thức, không chỉ 'Sửa'").toMatch(new RegExp(escapeRe(r.title)));

    // Liên kết "Xem" chỉ hiện cho công thức Published (page.tsx: r.status === "Published") — công thức
    // vừa tạo còn Draft, nên ĐÚNG là không có liên kết Xem. Chỉ kiểm vắng mặt, không coi là lỗi.
    const viewLink = page.getByRole("link", { name: new RegExp(`^Xem công thức .*${escapeRe(r.title)}`) });
    await expect(viewLink).toHaveCount(0);
    console.log("[K18] Dashboard — Draft không có liên kết Xem (đúng thiết kế, chỉ Published mới có)");
  });
});

function escapeRe(s: string) { return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&"); }
