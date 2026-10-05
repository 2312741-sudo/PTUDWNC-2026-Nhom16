import { generateMetadata } from "../page";
import { getRecipeBySlug } from "@/lib/api";
import type { RecipeDetail } from "@/types/recipe";

/**
 * K19 (TV3) — meta description trang chi tiết. Mô tả rỗng thì Next bỏ luôn thẻ <meta name="description">
 * (Lighthouse SEO 92: "Document does not have a meta description"). Mô tả rỗng/toàn khoảng trắng -> chuỗi dự phòng
 * dựng từ dữ liệu thật (tiêu đề, tổng thời gian, khẩu phần, vài nguyên liệu đầu), tối đa ~155 ký tự.
 */
jest.mock("@/lib/api", () => ({ getRecipeBySlug: jest.fn() }));
// lucide-react bản ESM không nạp được trong Jest (CJS); biểu tượng không ảnh hưởng metadata
jest.mock("lucide-react", () => new Proxy({ __esModule: true }, {
  get: (target: Record<string | symbol, unknown>, key) => (key in target ? target[key] : () => null),
}));
const mockGet = getRecipeBySlug as jest.MockedFunction<typeof getRecipeBySlug>;

const ing = (name: string, orderIndex: number) =>
  ({ id: `i${orderIndex}`, recipeId: "r1", name, quantity: 100, unit: "g", notes: null, orderIndex });

const recipe = (over: Partial<RecipeDetail> = {}): RecipeDetail => ({
  id: "r1", slug: "canh-chua-ca-loc", title: "Canh chua cá lóc", description: "", instructions: "",
  prepTimeMinutes: 15, cookTimeMinutes: 20, totalTimeMinutes: 35, servings: 4,
  difficulty: "Easy", status: "Published", publishedAt: "2026-10-01T00:00:00Z", categoryId: "c1", authorId: "u1",
  nutrition: null, steps: [], images: [], rowVersion: "v1", createdAt: "2026-10-01T00:00:00Z",
  // orderIndex lộn xộn: phải lấy theo thứ tự hiển thị, không theo thứ tự mảng
  ingredients: [ing("Me chua", 1), ing("Cá lóc", 0), ing("Cà chua", 2), ing("Thơm", 3), ing("Giá đỗ", 4), ing("Rau om", 5)],
  ...over,
});

async function meta(r: RecipeDetail) {
  mockGet.mockResolvedValue(r);
  const m = await generateMetadata({ params: Promise.resolve({ slug: r.slug }) });
  const og = m.openGraph as { description?: string } | undefined;
  const tw = m.twitter as { description?: string } | undefined;
  return { description: m.description, og: og?.description, tw: tw?.description };
}

describe.each([["rỗng", ""], ["toàn khoảng trắng", "  \n\t  "]])("mô tả %s", (_name, description) => {
  it("description, openGraph.description, twitter.description là chuỗi dự phòng giống nhau, không rỗng", async () => {
    const { description: d, og, tw } = await meta(recipe({ description }));
    expect(typeof d).toBe("string");
    expect((d as string).trim().length).toBeGreaterThan(20);
    expect(og).toBe(d);
    expect(tw).toBe(d);
  });

  it("dựng từ dữ liệu thật: tiêu đề, tổng thời gian, khẩu phần, nguyên liệu đầu theo orderIndex", async () => {
    const { description: d } = await meta(recipe({ description }));
    expect(d).toMatch(/^Canh chua cá lóc/);
    expect(d).toContain("35 phút");
    expect(d).toContain("4 khẩu phần");
    expect(d).toContain("Cá lóc, Me chua, Cà chua");
    expect((d as string).length).toBeLessThanOrEqual(155);
  });
});

it("không bịa dữ liệu: thiếu nguyên liệu/khẩu phần thì không nhắc tới, không có undefined/null/NaN", async () => {
  const { description: d } = await meta(recipe({ description: "", ingredients: [], servings: 0 }));
  expect(d).toMatch(/^Canh chua cá lóc/);
  expect(d).toContain("35 phút");
  expect(d).not.toMatch(/khẩu phần|nguyên liệu|undefined|null|NaN/i);
});

it("dự phòng quá dài (tiêu đề và nhiều nguyên liệu dài) -> cắt <= 155 ký tự ở ranh giới từ, kết thúc bằng …", async () => {
  const long = Array.from({ length: 12 }, (_, i) => ing(`Nguyên liệu đặc biệt số ${i + 1} loại thượng hạng`, i));
  const { description: d, og, tw } = await meta(recipe({
    description: "", title: "Lẩu mắm miền Tây đầy đủ hương vị cho cả gia đình cuối tuần", ingredients: long,
  }));
  expect((d as string).length).toBeLessThanOrEqual(155);
  expect(d).toMatch(/\S…$/);
  expect(d).not.toMatch(/\s…$/);
  expect(og).toBe(d);
  expect(tw).toBe(d);
});

it("có mô tả thật -> giữ nguyên", async () => {
  const real = "Món canh chua ngọt thanh kiểu miền Tây, nấu với cá lóc đồng và me chín.";
  const { description: d, og, tw } = await meta(recipe({ description: real }));
  expect(d).toBe(real);
  expect(og).toBe(real);
  expect(tw).toBe(real);
});

it("mô tả thật quá dài -> cắt <= 155 ký tự ở ranh giới từ, phần đầu giữ đúng nguyên văn", async () => {
  const real = "Canh chua cá lóc là món ăn quen thuộc của người miền Tây Nam Bộ, vị chua thanh của me, ngọt của thơm, "
    + "thơm mùi rau om và ngò gai, ăn cùng cơm trắng nóng hổi trong những ngày hè oi bức là tuyệt nhất.";
  const { description: d } = await meta(recipe({ description: real }));
  const text = d as string;
  expect(text.length).toBeLessThanOrEqual(155);
  expect(text.endsWith("…")).toBe(true);
  const kept = text.slice(0, -1);
  expect(real.startsWith(kept)).toBe(true);
  expect(real[kept.length]).toMatch(/[\s,.;:]/); // cắt ở khoảng trắng/dấu câu, không giữa từ
});

it("không tìm thấy công thức -> metadata rỗng như cũ", async () => {
  mockGet.mockResolvedValue(null);
  expect(await generateMetadata({ params: Promise.resolve({ slug: "khong-co" }) })).toEqual({});
});
