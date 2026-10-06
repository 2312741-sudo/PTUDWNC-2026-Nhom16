import { TextDecoder } from "node:util";
import { render, screen } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import colors from "tailwindcss/colors";
import RecipeDetailPage from "../page";
import OwnerEditButton from "@/components/OwnerEditButton";
import { getRecipeBySlug } from "@/lib/api";
import type { RecipeDetail } from "@/types/recipe";

/**
 * K18 (TV3) — tương phản chữ trắng trên nền cam (Lighthouse a11y 96: bg-orange-600 + chữ trắng ~3,6:1).
 * Tính tỷ lệ tương phản WCAG 2.x từ mã màu Tailwind của đúng class đang render; chữ thường cần >= 4,5:1 (SC 1.4.3).
 * jest-axe chạy trong jsdom không tính được color-contrast (không có layout) nên phải tự tính.
 */
expect.extend(toHaveNoViolations);
jest.mock("@/lib/api", () => ({ getRecipeBySlug: jest.fn() }));
// lucide-react bản ESM không nạp được trong Jest (CJS); biểu tượng không ảnh hưởng màu
jest.mock("lucide-react", () => new Proxy({ __esModule: true }, {
  get: (target: Record<string | symbol, unknown>, key) => (key in target ? target[key] : () => null),
}));

// ---- WCAG 2.x: relative luminance + contrast ratio
const channel = (c: number) => { const s = c / 255; return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4; };
function luminance(hex: string) {
  const h = hex.replace("#", "");
  const full = h.length === 3 ? [...h].map(x => x + x).join("") : h;
  const [r, g, b] = [0, 2, 4].map(i => parseInt(full.slice(i, i + 2), 16));
  return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
}
function contrast(a: string, b: string) {
  const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x);
  return (hi + 0.05) / (lo + 0.05);
}

/** "bg-orange-700" / "hover:bg-orange-800" / "text-white" -> mã hex trong bảng màu Tailwind */
const palette = colors as unknown as Record<string, string | Record<string, string>>;
function hexOf(cls: string): string {
  const m = /^(?:hover:)?(?:bg|text)-([a-z]+)(?:-(\d{2,3}))?$/.exec(cls);
  if (!m) throw new Error(`Không đọc được màu từ class ${cls}`);
  const entry = palette[m[1]];
  const hex = typeof entry === "string" ? entry : entry?.[m[2]];
  if (!hex) throw new Error(`Không có màu Tailwind cho ${cls}`);
  return hex;
}
/** Lấy class màu nền / chữ / nền khi hover của một phần tử */
function colorClasses(el: Element) {
  const list = (el.getAttribute("class") ?? "").split(/\s+/);
  const pick = (re: RegExp) => list.find(c => re.test(c));
  return {
    bg: pick(/^bg-[a-z]+-\d+$/),
    text: pick(/^text-(white|black|[a-z]+-\d+)$/),
    hoverBg: pick(/^hover:bg-[a-z]+-\d+$/),
  };
}

describe("công thức WCAG (kiểm chính hàm tính)", () => {
  it("đen/trắng = 21:1, trắng/trắng = 1:1", () => {
    expect(contrast("#000000", "#ffffff")).toBeCloseTo(21, 5);
    expect(contrast("#ffffff", "#fff")).toBeCloseTo(1, 5);
  });
  it("orange-600 trên trắng < 4,5 (lỗi Lighthouse), orange-700 và orange-800 >= 4,5", () => {
    expect(contrast(hexOf("bg-orange-600"), hexOf("text-white"))).toBeLessThan(4.5);
    expect(contrast(hexOf("bg-orange-700"), hexOf("text-white"))).toBeGreaterThanOrEqual(4.5);
    expect(contrast(hexOf("hover:bg-orange-800"), hexOf("text-white"))).toBeGreaterThanOrEqual(4.5);
  });
});

describe("OwnerEditButton", () => {
  beforeAll(() => { if (!global.TextDecoder) Object.assign(global, { TextDecoder }); });
  beforeEach(() => {
    const payload = Buffer.from(JSON.stringify({ sub: "u1", role: "Author", exp: Math.floor(Date.now() / 1000) + 3600 }))
      .toString("base64url");
    localStorage.setItem("accessToken", `e30.${payload}.sig`);
  });
  afterEach(() => localStorage.clear());

  it("nền và nền khi hover đạt >= 4,5:1 với màu chữ; jest-axe 0 vi phạm", async () => {
    const { container } = render(<OwnerEditButton recipeId="r1" slug="canh-chua" authorId="u1" />);
    const link = await screen.findByRole("link", { name: /Sửa công thức/ });
    const { bg, text, hoverBg } = colorClasses(link);
    expect(bg && text && hoverBg).toBeTruthy();
    expect(contrast(hexOf(bg!), hexOf(text!))).toBeGreaterThanOrEqual(4.5);
    expect(contrast(hexOf(hoverBg!), hexOf(text!))).toBeGreaterThanOrEqual(4.5);
    expect(await axe(container)).toHaveNoViolations();
  });
});

describe("trang chi tiết: số bước tròn", () => {
  const recipe = {
    id: "r1", slug: "canh-chua", title: "Canh chua cá lóc", description: "Canh chua miền Tây.", instructions: "",
    prepTimeMinutes: 15, cookTimeMinutes: 20, totalTimeMinutes: 35, servings: 4, difficulty: "Easy",
    status: "Published", publishedAt: "2026-10-01T00:00:00Z", categoryId: "c1", authorId: "u1", nutrition: null,
    ingredients: [{ id: "i1", recipeId: "r1", name: "Cá lóc", quantity: 500, unit: "g", notes: null, orderIndex: 0 }],
    steps: [
      { id: "s1", recipeId: "r1", stepNumber: 1, title: "Sơ chế cá", description: "Làm sạch cá.", timerMinutes: null },
      { id: "s2", recipeId: "r1", stepNumber: 2, title: "Nấu canh", description: "Nấu nước dùng.", timerMinutes: 10 },
    ],
    images: [], rowVersion: "v1", createdAt: "2026-10-01T00:00:00Z",
  } as unknown as RecipeDetail;

  it("nền số bước đạt >= 4,5:1 với chữ; danh sách bước jest-axe 0 vi phạm", async () => {
    (getRecipeBySlug as jest.Mock).mockResolvedValue(recipe);
    const { container } = render(await RecipeDetailPage({ params: Promise.resolve({ slug: "canh-chua" }) }));
    const list = container.querySelector("ol")!;
    const badges = [...list.querySelectorAll(":scope > li > span:first-child")];
    expect(badges.map(b => b.textContent)).toEqual(["1", "2"]);
    for (const b of badges) {
      const { bg, text } = colorClasses(b);
      expect(bg && text).toBeTruthy();
      expect(contrast(hexOf(bg!), hexOf(text!))).toBeGreaterThanOrEqual(4.5);
    }
    expect(await axe(list)).toHaveNoViolations();
  });
});
