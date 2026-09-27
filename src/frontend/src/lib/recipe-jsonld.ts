// Schema.org Recipe JSON-LD cho Google Rich Results (K19).
// Không sinh aggregateRating — hệ thống chưa có đánh giá (SRS v1.1.1).
import { mediaUrl } from "./recipe-editor";

const duration = (m?: number | null) => (m && m > 0 ? `PT${m}M` : undefined);

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function buildRecipeJsonLd(r: any) {
  const images = [...(r.images ?? [])]
    .sort((a, b) => Number(b.isPrimary) - Number(a.isPrimary))
    .map(i => mediaUrl(i.originalUrl ?? i.url))
    .filter(Boolean);
  const n = r.nutrition;
  const has = (v: unknown) => v !== null && v !== undefined;
  const nutrition = n && Object.values(n).some(has) ? {
    "@type": "NutritionInformation",
    calories: has(n.calories) ? `${n.calories} kcal` : undefined,
    proteinContent: has(n.protein) ? `${n.protein} g` : undefined,
    carbohydrateContent: has(n.carbohydrates) ? `${n.carbohydrates} g` : undefined,
    fatContent: has(n.fat) ? `${n.fat} g` : undefined,
    fiberContent: has(n.fiber) ? `${n.fiber} g` : undefined,
    sodiumContent: has(n.sodium) ? `${n.sodium} mg` : undefined,
  } : undefined;

  // JSON.stringify tự bỏ các field undefined
  return {
    "@context": "https://schema.org",
    "@type": "Recipe",
    name: r.title,
    description: r.description || undefined,
    image: images.length ? images : undefined,
    author: r.authorName ? { "@type": "Person", name: r.authorName } : undefined,
    datePublished: r.publishedAt ?? undefined,
    prepTime: duration(r.prepTimeMinutes),
    cookTime: duration(r.cookTimeMinutes),
    totalTime: duration((r.prepTimeMinutes ?? 0) + (r.cookTimeMinutes ?? 0)),
    recipeYield: r.servings ? `${r.servings} khẩu phần` : undefined,
    recipeCategory: r.categoryName ?? undefined,
    recipeIngredient: [...(r.ingredients ?? [])]
      .sort((a, b) => a.orderIndex - b.orderIndex)
      .map(i => [i.quantity, i.unit, i.name].filter(x => has(x) && x !== "").join(" ")),
    recipeInstructions: [...(r.steps ?? [])]
      .sort((a, b) => a.stepNumber - b.stepNumber)
      .map(s => ({ "@type": "HowToStep", position: s.stepNumber, name: s.title, text: s.description })),
    nutrition,
  };
}

/** Chuỗi an toàn để nhúng vào <script>: chặn "</script>" trong dữ liệu người dùng. */
export const jsonLdString = (o: unknown) => JSON.stringify(o).replace(/</g, "\\u003c");