"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { RecipeDetail, UnauthorizedError, getRecipeDetail, toBasicInfo } from "@/lib/recipe-editor";
import RecipeWizard, { WizardState } from "../../_wizard/RecipeWizard";

export default function EditRecipeClient() {
  const { id } = useParams<{ id: string }>();
  const slug = useSearchParams().get("slug");
  const router = useRouter();
  const [initial, setInitial] = useState<Partial<WizardState> | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!localStorage.getItem("accessToken")) { router.replace("/auth/login"); return; }
    (async () => {
      try {
        // API chi tiết tra theo slug; không có slug thì thử bằng id
        const d: RecipeDetail = await getRecipeDetail(slug ?? id);
        if (d.id !== id) throw new Error("Không khớp công thức cần sửa");
        setInitial({
          step: 0, recipeId: d.id, slug: d.slug, rowVersion: d.rowVersion,
          detail: d, info: toBasicInfo(d),
        });
      } catch (e) {
        if (e instanceof UnauthorizedError) { router.replace("/auth/login"); return; }
        setError(e instanceof Error ? e.message : "Không tải được công thức");
      }
    })();
  }, [id, slug, router]);

  if (error) return (
    <div className="mx-auto max-w-3xl p-6">
      <p className="mb-4 rounded bg-red-50 p-3 text-red-700">{error}</p>
      <Link href="/dashboard/recipes" className="text-blue-600 underline">← Về danh sách</Link>
    </div>
  );
  if (!initial) return <p className="mx-auto max-w-3xl p-6 text-gray-500">Đang tải...</p>;
  return <RecipeWizard initial={initial} />;
}