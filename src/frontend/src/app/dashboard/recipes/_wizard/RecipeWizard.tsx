"use client";

import { useEffect, useReducer, useState } from "react";
import { useRouter } from "next/navigation";
import {
  ApiError, BasicInfo, Category, DIFFICULTIES, UnauthorizedError,
  createRecipe, getCategories, updateRecipe,
} from "@/lib/recipe-editor";

export const STEPS = ["Thông tin cơ bản", "Nguyên liệu", "Các bước", "Xem lại & Xuất bản"] as const;

export interface WizardState {
  step: number;
  recipeId: string | null;
  slug: string | null;
  rowVersion: string | null;
  info: BasicInfo;
  saving: boolean;
  error: string | null;
}

type Action =
  | { type: "goto"; step: number }
  | { type: "setInfo"; patch: Partial<BasicInfo> }
  | { type: "saving" }
  | { type: "saved"; id: string; slug: string; rowVersion: string }
  | { type: "error"; message: string };

export const emptyInfo: BasicInfo = {
  title: "", description: "", instructions: "",
  prepTimeMinutes: 0, cookTimeMinutes: 0, servings: 1,
  difficulty: "Easy", categoryId: "", nutrition: null,
};

function reducer(s: WizardState, a: Action): WizardState {
  switch (a.type) {
    case "goto": return { ...s, step: a.step, error: null };
    case "setInfo": return { ...s, info: { ...s.info, ...a.patch } };
    case "saving": return { ...s, saving: true, error: null };
    case "saved": return { ...s, saving: false, recipeId: a.id, slug: a.slug, rowVersion: a.rowVersion };
    case "error": return { ...s, saving: false, error: a.message };
  }
}

function validate(i: BasicInfo): string | null {
  if (!i.title.trim()) return "Vui lòng nhập tiêu đề";
  if (!i.categoryId) return "Vui lòng chọn danh mục";
  if (i.servings < 1) return "Khẩu phần phải ≥ 1";
  if (i.prepTimeMinutes < 0 || i.cookTimeMinutes < 0) return "Thời gian không hợp lệ";
  return null;
}

export default function RecipeWizard({ initial }: { initial?: Partial<WizardState> }) {
  const router = useRouter();
  const [s, dispatch] = useReducer(reducer, {
    step: 0, recipeId: null, slug: null, rowVersion: null,
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
    if (e instanceof ApiError && e.status === 422 && e.code?.includes("CONCURRENCY"))
      return dispatch({ type: "error", message: "Dữ liệu đã bị thay đổi ở nơi khác, hãy tải lại trang." });
    dispatch({ type: "error", message: e instanceof Error ? e.message : "Có lỗi xảy ra" });
  }

  async function saveBasic() {
    const err = validate(s.info);
    if (err) return dispatch({ type: "error", message: err });
    dispatch({ type: "saving" });
    try {
      const saved = s.recipeId
        ? await updateRecipe(s.recipeId, s.info, s.rowVersion!)
        : await createRecipe(s.info);
      dispatch({ type: "saved", id: saved.id, slug: saved.slug, rowVersion: saved.rowVersion });
      dispatch({ type: "goto", step: 1 });
    } catch (e) { handleError(e); }
  }

  const set = (patch: Partial<BasicInfo>) => dispatch({ type: "setInfo", patch });
  const canGo = (i: number) => i === 0 || !!s.recipeId; // phải tạo recipe trước

  return (
    <div className="mx-auto max-w-3xl p-6">
      <h1 className="mb-4 text-2xl font-bold">{s.recipeId ? "Sửa công thức" : "Tạo công thức mới"}</h1>

      <ol className="mb-6 flex gap-2">
        {STEPS.map((label, i) => (
          <li key={label}>
            <button
              disabled={!canGo(i)}
              onClick={() => dispatch({ type: "goto", step: i })}
              className={`rounded px-3 py-1 text-sm ${i === s.step ? "bg-orange-500 text-white" : "bg-gray-100"} disabled:opacity-40`}
            >
              {i + 1}. {label}
            </button>
          </li>
        ))}
      </ol>

      {s.error && <p className="mb-4 rounded bg-red-50 p-3 text-red-700">{s.error}</p>}

      {s.step === 0 && (
        <div className="space-y-3">
          <input className="w-full rounded border p-2" placeholder="Tiêu đề"
            value={s.info.title} onChange={e => set({ title: e.target.value })} />
          <textarea className="w-full rounded border p-2" placeholder="Mô tả" rows={3}
            value={s.info.description} onChange={e => set({ description: e.target.value })} />
          <textarea className="w-full rounded border p-2" placeholder="Hướng dẫn chung" rows={4}
            value={s.info.instructions} onChange={e => set({ instructions: e.target.value })} />
          <div className="grid grid-cols-3 gap-3">
            <label className="text-sm">Sơ chế (phút)
              <input type="number" min={0} className="w-full rounded border p-2"
                value={s.info.prepTimeMinutes} onChange={e => set({ prepTimeMinutes: +e.target.value })} /></label>
            <label className="text-sm">Nấu (phút)
              <input type="number" min={0} className="w-full rounded border p-2"
                value={s.info.cookTimeMinutes} onChange={e => set({ cookTimeMinutes: +e.target.value })} /></label>
            <label className="text-sm">Khẩu phần
              <input type="number" min={1} className="w-full rounded border p-2"
                value={s.info.servings} onChange={e => set({ servings: +e.target.value })} /></label>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <select className="rounded border p-2" value={s.info.difficulty}
              onChange={e => set({ difficulty: e.target.value as BasicInfo["difficulty"] })}>
              {DIFFICULTIES.map(d => <option key={d.value} value={d.value}>{d.label}</option>)}
            </select>
            <select className="rounded border p-2" value={s.info.categoryId}
              onChange={e => set({ categoryId: e.target.value })}>
              <option value="">-- Danh mục --</option>
              {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </div>
        </div>
      )}

      {s.step === 1 && <p className="text-gray-500">TODO: quản lý nguyên liệu (recipe #{s.recipeId})</p>}
      {s.step === 2 && <p className="text-gray-500">TODO: quản lý các bước + kéo thả reorder</p>}
      {s.step === 3 && <p className="text-gray-500">TODO: xem lại & xuất bản (PATCH /publish)</p>}

      <div className="mt-6 flex justify-between">
        <button disabled={s.step === 0} onClick={() => dispatch({ type: "goto", step: s.step - 1 })}
          className="rounded border px-4 py-2 disabled:opacity-40">← Quay lại</button>
        {s.step === 0 ? (
          <button onClick={saveBasic} disabled={s.saving}
            className="rounded bg-orange-500 px-4 py-2 text-white disabled:opacity-50">
            {s.saving ? "Đang lưu..." : "Lưu & tiếp →"}
          </button>
        ) : s.step < STEPS.length - 1 ? (
          <button onClick={() => dispatch({ type: "goto", step: s.step + 1 })}
            className="rounded bg-orange-500 px-4 py-2 text-white">Tiếp →</button>
        ) : (
          <button onClick={() => router.push("/dashboard/recipes")}
            className="rounded border px-4 py-2">Về danh sách</button>
        )}
      </div>
    </div>
  );
}