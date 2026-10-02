import { render } from "@testing-library/react";
import { buildRecipeJsonLd, jsonLdString } from "../recipe-jsonld";
import RecipeDetailLayout from "@/app/recipes/[slug]/layout";

/**
 * K19 / NFR-SEO-001 — JSON-LD Schema.org Recipe của trang chi tiết.
 * Fixture lấy theo đúng hình dạng GET /api/v1/recipes/{slug} (công thức seed "ga-lac-pho-mai-cay", rút gọn).
 */
const NFR_KEYS = ["name", "description", "image", "author", "datePublished", "prepTime", "cookTime", "totalTime",
  "recipeYield", "recipeIngredient", "recipeInstructions", "nutrition"];

const detail = {
  id: "r1", title: "Gà Lắc Phô Mai Cay", slug: "ga-lac-pho-mai-cay", description: "Gà chiên giòn lắc phô mai cay.",
  prepTimeMinutes: 20, cookTimeMinutes: 25, servings: 2, status: "Published", publishedAt: "2026-09-30T11:39:15Z",
  authorId: "u1", authorName: "Bếp Nhà Trung", categoryName: "Món chính",
  nutrition: { calories: 543, protein: 42, carbohydrates: 49, fat: 19, fiber: 6.5, sodium: 841 },
  images: [
    { id: "i2", originalUrl: "/images/recipes/phu.jpg", isPrimary: false, orderIndex: 1 },
    { id: "i1", originalUrl: "/images/recipes/ga-lac-pho-mai-cay.jpg", isPrimary: true, orderIndex: 0 },
  ],
  ingredients: [
    { id: "g2", name: "Phô mai bột", quantity: 30, unit: "g", notes: null, orderIndex: 1 },
    { id: "g1", name: "Đùi gà", quantity: 500, unit: "g", notes: null, orderIndex: 0 },
  ],
  steps: [
    { id: "s2", stepNumber: 2, title: "Chiên", description: "Chiên vàng giòn." },
    { id: "s1", stepNumber: 1, title: "Ướp", description: "Ướp gà 30 phút." },
  ],
};

describe("buildRecipeJsonLd", () => {
  it("co du 12 thuoc tinh NFR-SEO-001 voi du lieu that cua API", () => {
    const ld = buildRecipeJsonLd(detail) as Record<string, unknown>;
    expect(ld["@context"]).toBe("https://schema.org");
    expect(ld["@type"]).toBe("Recipe");
    expect(NFR_KEYS.filter(k => ld[k] === undefined)).toEqual([]);
  });

  it("anh duong dan tuong doi thanh URL tuyet doi, anh chinh dung truoc", () => {
    expect(buildRecipeJsonLd(detail).image).toEqual([
      "http://localhost:3000/images/recipes/ga-lac-pho-mai-cay.jpg",
      "http://localhost:3000/images/recipes/phu.jpg",
    ]);
  });

  it("URL http(s) giu nguyen; key MinIO khong co NEXT_PUBLIC_MEDIA_URL thi bo, khong xuat duong dan tuong doi", () => {
    const ld = buildRecipeJsonLd({ ...detail, images: [
      { originalUrl: "https://cdn.example.com/a.webp", isPrimary: true },
      { originalUrl: "recipes/r1/abc.webp", isPrimary: false },
    ] });
    expect(ld.image).toEqual(["https://cdn.example.com/a.webp"]);
  });

  it("author la Person, thoi gian ISO 8601, nguyen lieu va buoc dung thu tu", () => {
    const ld = buildRecipeJsonLd(detail);
    expect(ld.author).toEqual({ "@type": "Person", name: "Bếp Nhà Trung" });
    expect([ld.prepTime, ld.cookTime, ld.totalTime]).toEqual(["PT20M", "PT25M", "PT45M"]);
    expect(ld.recipeIngredient).toEqual(["500 g Đùi gà", "30 g Phô mai bột"]);
    expect(ld.recipeInstructions).toEqual([
      { "@type": "HowToStep", position: 1, name: "Ướp", text: "Ướp gà 30 phút." },
      { "@type": "HowToStep", position: 2, name: "Chiên", text: "Chiên vàng giòn." },
    ]);
    expect(ld.nutrition).toMatchObject({ "@type": "NutritionInformation", calories: "543 kcal", sodiumContent: "841 mg" });
  });

  it("khong bia danh gia: khong co aggregateRating/review du du lieu vao co truong giong rating", () => {
    const json = JSON.stringify(buildRecipeJsonLd({ ...detail, rating: 5, ratingCount: 10, reviews: [{ text: "ngon" }] }));
    expect(json).not.toMatch(/aggregateRating|"review|ratingValue|ratingCount/);
  });

  it("jsonLdString chan </script> trong du lieu nguoi dung", () => {
    const s = jsonLdString(buildRecipeJsonLd({ ...detail, title: "</script><script>alert(1)</script>" }));
    expect(s).not.toContain("</script>");
    expect(JSON.parse(s).name).toBe("</script><script>alert(1)</script>");
  });
});

describe("RecipeDetailLayout", () => {
  const renderLayout = async (body: unknown, ok = true) => {
    (global as unknown as { fetch: jest.Mock }).fetch = jest.fn().mockResolvedValue({ ok, json: async () => body });
    const el = await RecipeDetailLayout({ children: <p>noi dung</p>, params: Promise.resolve({ slug: "ga-lac-pho-mai-cay" }) });
    return render(el).container;
  };
  const scripts = (c: HTMLElement) => c.querySelectorAll('script[type="application/ld+json"]');

  it("Published: nhung dung 1 script ld+json bang buildRecipeJsonLd", async () => {
    const c = await renderLayout({ data: detail });
    expect(scripts(c)).toHaveLength(1);
    expect(JSON.parse(scripts(c)[0].innerHTML)).toEqual(JSON.parse(JSON.stringify(buildRecipeJsonLd(detail, "ga-lac-pho-mai-cay"))));
  });

  it("Draft hoac API loi: khong nhung JSON-LD (khong lo ban nhap)", async () => {
    expect(scripts(await renderLayout({ data: { ...detail, status: "Draft" } }))).toHaveLength(0);
    expect(scripts(await renderLayout(null, false))).toHaveLength(0);
  });
});
