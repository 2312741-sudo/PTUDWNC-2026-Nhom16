"use client";

import Link from "next/link";
import { Category, DIFFICULTIES, RecipeDetail, publishRecipe } from "@/lib/recipe-editor";

interface Props {
  recipe: RecipeDetail;
  categories: Category[];
  busy: boolean;
  run: (fn: () => Promise<unknown>) => Promise<boolean>;
}

export default function ReviewStep({ recipe: r, categories, busy, run }: Props) {
  const category = categories.find(c => c.id === r.categoryId)?.name ?? "—";
  const difficulty = DIFFICULTIES.find(d => d.value === r.difficulty)?.label ?? r.difficulty;
  const published = r.status === "Published";
  const checks = [
    { ok: r.ingredients.length > 0, label: `Có ít nhất 1 nguyên liệu (${r.ingredients.length})` },
    { ok: r.steps.length > 0, label: `Có ít nhất 1 bước thực hiện (${r.steps.length})` },
  ];
  const ready = checks.every(c => c.ok);

  return (
    <div className="space-y-4">
      <div className="rounded border p-4">
        <div className="flex items-start justify-between gap-2">
          <h2 className="text-xl font-semibold">{r.title}</h2>
          <span className={`rounded px-2 py-0.5 text-xs ${published ? "bg-emerald-100 text-emerald-700" : "bg-gray-100 text-gray-600"}`}>
            {r.status ?? "Draft"}
          </span>
        </div>
        {r.description && <p className="mt-1 text-gray-600">{r.description}</p>}
        <p className="mt-2 text-sm text-gray-500">
          {category} • {difficulty} • Sơ chế {r.prepTimeMinutes}′ • Nấu {r.cookTimeMinutes}′ • {r.servings} khẩu phần
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <div className="rounded border p-4">
          <h3 className="mb-2 font-semibold">Nguyên liệu</h3>
          <ul className="list-disc pl-5 text-sm">
            {[...r.ingredients].sort((a, b) => a.orderIndex - b.orderIndex).map(i => (
              <li key={i.id}>{[i.quantity, i.unit, i.name].filter(v => v !== null && v !== "").join(" ")}</li>
            ))}
          </ul>
        </div>
        <div className="rounded border p-4">
          <h3 className="mb-2 font-semibold">Các bước</h3>
          <ol className="list-decimal pl-5 text-sm">
            {[...r.steps].sort((a, b) => a.stepNumber - b.stepNumber).map(s => <li key={s.id}>{s.title}</li>)}
          </ol>
        </div>
      </div>

      <ul className="space-y-1 text-sm">
        {checks.map(c => (
          <li key={c.label} className={c.ok ? "text-emerald-700" : "text-red-600"}>{c.ok ? "✓" : "✗"} {c.label}</li>
        ))}
      </ul>

      <div className="flex flex-wrap gap-2">
        {!published && (
          <button disabled={busy || !ready} onClick={() => run(() => publishRecipe(r.id))}
            className="rounded bg-emerald-600 px-4 py-2 text-white disabled:opacity-50">
            {busy ? "Đang xử lý..." : "Xuất bản"}
          </button>
        )}
        {published && (
          <Link href={`/recipes/${r.slug}`} className="rounded border px-4 py-2">Xem trang công khai →</Link>
        )}
      </div>
    </div>
  );
}