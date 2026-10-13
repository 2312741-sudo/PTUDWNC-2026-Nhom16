import type { ReactNode } from "react";
import { buildRecipeJsonLd, jsonLdString } from "@/lib/recipe-jsonld";
import { API_BASE_URL } from "@/lib/api";

async function getPublishedRecipe(slug: string) {
  try {
    const res = await fetch(`${API_BASE_URL}/recipes/${encodeURIComponent(slug)}`, { next: { revalidate: 60 } });
    if (!res.ok) return null;
    const r = (await res.json())?.data;
    return r && (r.status === undefined || r.status === "Published") ? r : null; // không lộ Draft
  } catch {
    return null; // API tắt -> trang vẫn render, chỉ thiếu JSON-LD
  }
}

// Layout riêng của trang chi tiết: nhúng JSON-LD mà không phải sửa page.tsx
export default async function RecipeDetailLayout(
  { children, params }: { children: ReactNode; params: Promise<{ slug: string }> },
) {
  const { slug } = await params;
  const recipe = await getPublishedRecipe(slug);
  return (
    <>
      {recipe && (
        <script type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: jsonLdString(buildRecipeJsonLd(recipe, slug)) }} />
      )}
      {children}
    </>
  );
}