import { randomBytes } from "node:crypto";
import { type APIRequestContext, type Page, expect, test } from "@playwright/test";

/**
 * C7 (TV3) — tiện ích cho các spec E2E tuần 4. Mỗi test tự tạo user mới (mật khẩu ngẫu nhiên, chỉ nằm trong bộ nhớ,
 * không trả ra ngoài, không in log) và tự tạo dữ liệu qua API thật -> các test độc lập, chạy riêng lẻ được.
 */
export const apiUrl = process.env.E2E_API_URL ?? "http://localhost:5080/api/v1";

export interface Session { accessToken: string; refreshToken?: string; user?: unknown; email: string }

const uniq = () => `${Date.now()}${randomBytes(3).toString("hex")}`;
const sleep = (ms: number) => new Promise(r => setTimeout(r, ms));

/**
 * Đăng ký user mới qua API, dùng luôn token trả về (1 lượt gọi auth / test).
 * Policy "auth" giới hạn 10 lượt/phút theo IP -> gặp 429 thì chờ Retry-After rồi thử lại.
 */
export async function newUser(request: APIRequestContext): Promise<Session> {
  const suffix = uniq();
  const email = `e2e.${suffix}@culinary.local`;
  for (let attempt = 0; attempt < 3; attempt++) {
    const res = await request.post(`${apiUrl}/auth/register`, {
      data: { email, password: `Aa1!${randomBytes(18).toString("base64url")}`, fullName: "E2E Tác giả", userName: `e2e${suffix}` },
    });
    if (res.status() === 429) {
      const wait = (Number(res.headers()["retry-after"]) || 60) * 1000 + 500;
      test.info().setTimeout(test.info().timeout + wait); // thời gian chờ không tính vào timeout của test
      await sleep(wait);
      continue;
    }
    expect(res.status(), "đăng ký user E2E").toBe(201);
    const { data } = await res.json();
    return { accessToken: data.accessToken, refreshToken: data.refreshToken, user: data.user, email };
  }
  throw new Error("Đăng ký bị giới hạn tần suất (429) quá 3 lần");
}

/** Đưa phiên vào localStorage như trang đăng nhập làm (accessToken/refreshToken/user) */
export async function signIn(page: Page, s: Session) {
  await page.goto("/robots.txt"); // trang nhẹ cùng origin, chỉ để có localStorage của localhost:3000
  await page.evaluate(v => {
    localStorage.setItem("accessToken", v.accessToken);
    if (v.refreshToken) localStorage.setItem("refreshToken", v.refreshToken);
    if (v.user) localStorage.setItem("user", JSON.stringify(v.user));
  }, s);
}

export const auth = (s: Session) => ({ Authorization: `Bearer ${s.accessToken}` });

export interface CreatedRecipe { id: string; slug: string; title: string }

/** Tạo công thức Draft qua API. Body khớp đúng record CreateRecipe (JSON strict, difficulty dạng số) */
export async function createRecipe(request: APIRequestContext, s: Session, opts: {
  title?: string; ingredients?: number; steps?: number;
} = {}): Promise<CreatedRecipe> {
  const cats = await request.get(`${apiUrl}/categories`);
  expect(cats.ok(), "GET /categories").toBeTruthy();
  const categoryId = (await cats.json()).data[0].id as string;
  const title = opts.title ?? `E2E Gỏi cuốn tôm thịt ${uniq()}`;
  const res = await request.post(`${apiUrl}/recipes`, {
    headers: auth(s),
    data: {
      title, description: "Món cuốn tươi mát cho ngày hè.", instructions: "",
      prepTimeMinutes: 20, cookTimeMinutes: 10, servings: 4, difficulty: 1, categoryId, nutrition: null,
    },
  });
  expect(res.status(), "POST /recipes").toBe(201);
  const { data } = await res.json();
  for (let i = 1; i <= (opts.ingredients ?? 0); i++) {
    const r = await request.post(`${apiUrl}/recipes/${data.id}/ingredients`, {
      headers: auth(s), data: { name: `Nguyên liệu ${i}`, quantity: 100 * i, unit: "g", notes: null },
    });
    expect(r.ok(), `thêm nguyên liệu ${i}`).toBeTruthy();
  }
  for (let i = 1; i <= (opts.steps ?? 0); i++) {
    const r = await request.post(`${apiUrl}/recipes/${data.id}/steps`, {
      headers: auth(s), data: { title: `Bước ${i}`, description: `Mô tả bước ${i}.`, timerMinutes: null, imageUrl: null },
    });
    expect(r.ok(), `thêm bước ${i}`).toBeTruthy();
  }
  return { id: data.id, slug: data.slug, title };
}

export async function getDetail(request: APIRequestContext, s: Session, slugOrId: string) {
  const res = await request.get(`${apiUrl}/recipes/${slugOrId}`, { headers: auth(s) });
  expect(res.ok(), `GET /recipes/${slugOrId}`).toBeTruthy();
  return (await res.json()).data;
}

export const editUrl = (r: { id: string; slug: string }) => `/dashboard/recipes/${r.id}/edit?slug=${encodeURIComponent(r.slug)}`;

/** Mở trang sửa và chờ wizard nạp xong (bước 1 hiện tiêu đề công thức) */
export async function openEditor(page: Page, r: { id: string; slug: string }) {
  await page.goto(editUrl(r));
  await expect(page.getByRole("heading", { level: 1, name: "Sửa công thức" })).toBeVisible();
}

export const stepButton = (page: Page, n: number) =>
  page.getByRole("navigation", { name: "Các bước soạn công thức" }).getByRole("button", { name: new RegExp(`^${n}\\. `) });

/** JPEG thật (canvas.toBlob) để qua kiểm tra magic bytes và giải mã khi resize ở backend */
export async function makeJpeg(page: Page, color: string, size = 64): Promise<Buffer> {
  const b64 = await page.evaluate(async ({ color, size }) => {
    const c = document.createElement("canvas");
    c.width = size; c.height = size;
    const ctx = c.getContext("2d")!;
    ctx.fillStyle = color; ctx.fillRect(0, 0, size, size);
    const blob: Blob = await new Promise(res => c.toBlob(b => res(b!), "image/jpeg", 0.9));
    const bytes = new Uint8Array(await blob.arrayBuffer());
    let bin = ""; bytes.forEach(x => { bin += String.fromCharCode(x); });
    return btoa(bin);
  }, { color, size });
  return Buffer.from(b64, "base64");
}

/** Ghi lại mọi response lỗi từ API (để khẳng định "không có 422") */
export function trackApiErrors(page: Page) {
  const errors: string[] = [];
  page.on("response", res => {
    if (res.url().startsWith(apiUrl) && res.status() >= 400) errors.push(`${res.status()} ${res.request().method()} ${res.url()}`);
  });
  return errors;
}
