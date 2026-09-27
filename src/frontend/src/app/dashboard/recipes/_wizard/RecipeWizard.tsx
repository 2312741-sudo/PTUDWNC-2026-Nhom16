"use client";

import { useEffect, useReducer, useState } from "react";
import { useRouter } from "next/navigation";
import {
  ApiError, BasicInfo, Category, DIFFICULTIES, RecipeDetail, UnauthorizedError,
  createRecipe, getCategories, getRecipeDetail, updateRecipe,
} from "@/lib/recipe-editor";
import IngredientsStep from "./IngredientsStep";
import StepsStep from "./StepsStep";
import ReviewStep from "./ReviewStep";

export const STEPS = ["Thông tin cơ bản", "Nguyên liệu", "Các bước", "Xem lại & Xuất bản"] as const;

export interface WizardState {
  step: number;
  recipeId: string | null;
  slug: string | null;
  rowVersion: string | null;
  info: BasicInfo;
  detail: RecipeDetail | null;
  saving: boolean;
  error: string | null;
}

type Action =
  | { type: "goto"; step: number }
  | { type: "setInfo"; patch: Partial<BasicInfo> }
  | { type: "saving" }
  | { type: "done" }
  | { type: "detail"; detail: RecipeDetail }
  | { type: "error"; message: string };

export const emptyInfo: BasicInfo = {
  title: "", description: "", instructions: "",
  prepTimeMinutes: 10, cookTimeMinutes: 10, servings: 1,
  difficulty: "Easy", categoryId: "", nutrition: null,
};

function reducer(s: WizardState, a: Action): WizardState {
  switch (a.type) {
    case "goto": return { ...s, step: a.step, error: null };
    case "setInfo": return { ...s, info: { ...s.info, ...a.patch } };
    case "saving": return { ...s, saving: true, error: null };
    case "done": return { ...s, saving: false };
    case "detail": return {
      ...s, saving: false, detail: a.detail,
      recipeId: a.detail.id, slug: a.detail.slug, rowVersion: a.detail.rowVersion,
    };
    case "error": return { ...s, saving: false, error: a.message };
  }
}

function validate(i: BasicInfo): string | null {
  const t = i.title.trim().length; if (t < 5 || t > 200) return "Tiêu đề phải từ 5 đến 200 ký tự";
  if (i.description.length > 2000) return "Mô tả tối đa 2000 ký tự";
  if (!i.categoryId) return "Vui lòng chọn danh mục";
  if (i.servings < 1) return "Khẩu phần phải ≥ 1";
  if (i.prepTimeMinutes <= 0 || i.cookTimeMinutes <= 0) return "Thời gian sơ chế và nấu phải lớn hơn 0";
  return null;
}

export default function RecipeWizard({ initial }: { initial?: Partial<WizardState> }) {
  const router = useRouter();
  const [s, dispatch] = useReducer(reducer, {
    step: 0, recipeId: null, slug: null, rowVersion: null, detail: null,
    info: emptyInfo, saving: false, error: null, ...initial,
  });
  const [categories, setCategories] = useState<Category[]>([]);

  useEffect(() => {
    if (!localStorage.getItem("accessToken")) { router.replace("/auth/login"); return; }
    getCategories().then(setCategories).catch(handleError);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function handleError(e: unknown) {
    if (e instanceof UnauthorizedError) { router.replace("/auth/login"); return; }
    if (e instanceof ApiError && e.code === "RECIPE_PUBLISH_INCOMPLETE")
      return dispatch({ type: "error", message: "Cần ít nhất 1 nguyên liệu và 1 bước thực hiện trước khi xuất bản." });
    if (e instanceof ApiError && e.status === 422 && /CONCURRENCY|ROW_?VERSION/i.test(e.code ?? ""))
      return dispatch({ type: "error", message: "Dữ liệu đã bị thay đổi ở nơi khác, hãy tải lại trang." });
    dispatch({ type: "error", message: e instanceof Error ? e.message : "Có lỗi xảy ra" });
  }

  async function reload(slug = s.slug) {
    if (!slug) return;
    dispatch({ type: "detail", detail: await getRecipeDetail(slug) });
  }

  // Chạy một thao tác API rồi tải lại chi tiết (để có RowVersion + danh sách mới)
  async function run(fn: () => Promise<unknown>) {
    dispatch({ type: "saving" });
    try { await fn(); await reload(); return true; }
    catch (e) { handleError(e); return false; }
  }

  async function saveBasic() {
    const err = validate(s.info);
    if (err) return dispatch({ type: "error", message: err });
    dispatch({ type: "saving" });
    try {
      const saved = s.recipeId
        ? await updateRecipe(s.recipeId, s.info, s.rowVersion!)
        : await createRecipe(s.info);
      await reload(saved.slug);
      dispatch({ type: "goto", step: 1 });
    } catch (e) { handleError(e); }
  }

  const set = (patch: Partial<BasicInfo>) => dispatch({ type: "setInfo", patch });
  const canGo = (i: number) => i === 0 || !!s.detail;

  return (
    <div className="mx-auto max-w-3xl p-6">
      <h1 className="mb-4 text-2xl font-bold">{s.recipeId ? "Sửa công thức" : "Tạo công thức mới"}</h1>

      <ol className="mb-6 flex flex-wrap gap-2">
        {STEPS.map((label, i) => (
          <li key={label}>
            <button
              disabled={!canGo(i) || s.saving}
              onClick={() => dispatch({ type: "goto", step: i })}
              className={`rounded px-3 py-1 text-sm ${i === s.step ? "bg-orange-500 text-white" : "bg-gray-100"} disabled:opacity-40`}
            >
              {i + 1}. {label}
            </button>
          </li>
        ))}
      </ol>

      {s.step === 0 && (
        <div className="space-y-3">
          <label className="block text-sm">Tiêu đề *
            <input className="w-full rounded border p-2" placeholder="VD: Canh chua cá lóc"
              value={s.info.title} onChange={e => set({ title: e.target.value })} /></label>
          <label className="block text-sm">Mô tả
            <textarea className="w-full rounded border p-2" rows={3}
              value={s.info.description} onChange={e => set({ description: e.target.value })} /></label>
          <label className="block text-sm">Hướng dẫn chung
            <textarea className="w-full rounded border p-2" rows={4}
              value={s.info.instructions} onChange={e => set({ instructions: e.target.value })} /></label>
          <div className="grid grid-cols-3 gap-3">
            <label className="text-sm">Sơ chế (phút) *
              <input type="number" min={1} className="w-full rounded border p-2"
                value={s.info.prepTimeMinutes} onChange={e => set({ prepTimeMinutes: +e.target.value })} /></label>
            <label className="text-sm">Nấu (phút) *
              <input type="number" min={1} className="w-full rounded border p-2"
                value={s.info.cookTimeMinutes} onChange={e => set({ cookTimeMinutes: +e.target.value })} /></label>
            <label className="text-sm">Khẩu phần *
              <input type="number" min={1} className="w-full rounded border p-2"
                value={s.info.servings} onChange={e => set({ servings: +e.target.value })} /></label>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <label className="text-sm">Độ khó
              <select className="w-full rounded border p-2" value={s.info.difficulty}
                onChange={e => set({ difficulty: e.target.value as BasicInfo["difficulty"] })}>
                {DIFFICULTIES.map(d => <option key={d.value} value={d.value}>{d.label}</option>)}
              </select></label>
            <label className="text-sm">Danh mục *
              <select className="w-full rounded border p-2" value={s.info.categoryId}
                onChange={e => set({ categoryId: e.target.value })}>
                <option value="">-- Chọn danh mục --</option>
                {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select></label>
          </div>
        </div>
      )}

      {s.step === 1 && s.detail && (
        <IngredientsStep recipe={s.detail} busy={s.saving} run={run} onError={m => dispatch({ type: "error", message: m })} />
      )}
      {s.step === 2 && s.detail && (
        <StepsStep recipe={s.detail} busy={s.saving} run={run} onError={m => dispatch({ type: "error", message: m })} />
      )}
      {s.step === 3 && s.detail && (
        <ReviewStep recipe={s.detail} categories={categories} busy={s.saving} run={run} />
      )}

      {s.error && <p className="mt-4 rounded bg-red-50 p-3 text-red-700">{s.error}</p>}

      <div className="mt-6 flex justify-between">
        <button disabled={s.step === 0 || s.saving} onClick={() => dispatch({ type: "goto", step: s.step - 1 })}
          className="rounded border px-4 py-2 disabled:opacity-40">← Quay lại</button>
        {s.step === 0 ? (
          <button onClick={saveBasic} disabled={s.saving}
            className="rounded bg-orange-500 px-4 py-2 text-white disabled:opacity-50">
            {s.saving ? "Đang lưu..." : "Lưu & tiếp →"}
          </button>
        ) : s.step < STEPS.length - 1 ? (
          <button onClick={() => dispatch({ type: "goto", step: s.step + 1 })} disabled={s.saving}
            className="rounded bg-orange-500 px-4 py-2 text-white disabled:opacity-50">Tiếp →</button>
        ) : (
          <button onClick={() => router.push("/dashboard/recipes")}
            className="rounded border px-4 py-2">Về danh sách</button>
        )}
      </div>
    </div>
  );
}