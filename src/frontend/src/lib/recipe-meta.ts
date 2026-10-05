// Meta description cho trang chi tiết công thức (K19). Mô tả rỗng thì Next bỏ thẻ <meta name="description">
// -> dựng câu dự phòng từ dữ liệu thật của công thức, không thêm thông tin nào không có trong dữ liệu.
import type { RecipeDetail } from "@/types/recipe";

export const META_DESCRIPTION_MAX = 155;
const MAX_INGREDIENTS = 4;

const squash = (s: string) => s.replace(/\s+/g, " ").trim();

/** Cắt <= max ký tự ở khoảng trắng gần nhất, thêm "…" (đã tính vào max) */
export function truncateAtWord(text: string, max = META_DESCRIPTION_MAX): string {
  if (text.length <= max) return text;
  const head = text.slice(0, max); // max - 1 ký tự chữ + "…"
  const cut = head.lastIndexOf(" ", max - 1);
  return `${(cut > 0 ? head.slice(0, cut) : head.slice(0, max - 1)).replace(/[\s,.;:—-]+$/, "")}…`;
}

type MetaSource = Pick<RecipeDetail, "title" | "description" | "prepTimeMinutes" | "cookTimeMinutes" | "servings">
  & Partial<Pick<RecipeDetail, "totalTimeMinutes" | "ingredients">>;

export function recipeMetaDescription(r: MetaSource): string {
  const real = squash(r.description ?? "");
  if (real) return truncateAtWord(real);

  const total = (r.prepTimeMinutes ?? 0) + (r.cookTimeMinutes ?? 0) || (r.totalTimeMinutes ?? 0);
  const facts = [
    total > 0 ? `tổng ${total} phút` : null,
    r.servings > 0 ? `${r.servings} khẩu phần` : null,
  ].filter(Boolean);
  const names = [...(r.ingredients ?? [])]
    .sort((a, b) => a.orderIndex - b.orderIndex)
    .map(i => squash(i.name ?? ""))
    .filter(Boolean)
    .slice(0, MAX_INGREDIENTS);

  let text = squash(r.title);
  if (facts.length) text += ` — ${facts.join(", ")}`;
  text += ".";
  if (names.length) text += ` Nguyên liệu: ${names.join(", ")}.`;
  return truncateAtWord(text);
}
