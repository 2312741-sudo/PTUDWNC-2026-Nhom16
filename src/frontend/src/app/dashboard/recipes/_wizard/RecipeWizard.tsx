"use client";

import { useEffect, useReducer, useState } from "react";
import { useRouter } from "next/navigation";
import {
  ApiError, BasicInfo, Category, DIFFICULTIES, NUTRITION_FIELDS, Nutrition, RecipeDetail, UnauthorizedError,
  createRecipe, getCategories, getRecipeDetail, isConflict, toBasicInfo, updateRecipe,
} from "@/lib/recipe-editor";
import IngredientsStep from "./IngredientsStep";
import StepsStep from "./StepsStep";
import ImagesStep from "./ImagesStep";
import ReviewStep from "./ReviewStep";

export const STEPS = ["Thông tin cơ bản", "Nguyên liệu", "Các bước", "Ảnh", "Xem lại & Xuất bản"] as const;

/** fn gọi API; optimistic (tuỳ chọn) cập nhật UI ngay, lỗi thì hoàn tác về snapshot. */
export type RunFn = (fn: () => Promise<unknown>, optimistic?: (d: RecipeDetail) => RecipeDetail) => Promise<boolean>;

export interface WizardState {
  step: number;
  recipeId: string | null;
  slug: string | null;
  rowVersion: string | null;
  info: BasicInfo;
  detail: RecipeDetail | null;
  saving: boolean;
  error: string | null;
  conflict: boolean;
}

type Action =
  | { type: "goto"; step: number }
  | { type: "setInfo"; patch: Partial<BasicInfo> }
  | { type: "resetInfo"; info: BasicInfo }
  | { type: "saving" }
  | { type: "optimistic"; detail: RecipeDetail }
  | { type: "detail"; detail: RecipeDetail }
  | { type: "rollback"; detail: RecipeDetail | null; message: string }
  | { type: "error"; message: string; conflict?: boolean };

export const emptyInfo: BasicInfo = {
  title: "", description: "", instructions: "",
  prepTimeMinutes: 10, cookTimeMinutes: 10, servings: 1,
  difficulty: "Easy", categoryId: "", nutrition: null,
};

const emptyNutrition: Nutrition = { calories: null, protein: null, carbohydrates: null, fat: null, fiber: null, sodium: null };

function reducer(s: WizardState, a: Action): WizardState {
  switch (a.type) {
    case "goto": return { ...s, step: a.step, error: null, conflict: false };
    case "setInfo": return { ...s, info: { ...s.info, ...a.patch } };
    case "resetInfo": return { ...s, info: a.info };
    case "saving": return { ...s, saving: true, error: null, conflict: false };
    case "optimistic": return { ...s, detail: a.detail };
    case "detail": return {
      ...s, saving: false, detail: a.detail,
      recipeId: a.detail.id, slug: a.detail.slug, rowVersion: a.detail.rowVersion,
    };
    case "rollback": return { ...s, saving: false, detail: a.detail, error: a.message };
    case "error": return { ...s, saving: false, error: a.message, conflict: !!a.conflict };
  }
}

// Khớp CreateRecipeValidator / NutritionValidator bên backend
function validate(i: BasicInfo): string | null {
  const t = i.title.trim().length;
  if (t < 5 || t > 200) return "Tiêu đề phải từ 5 đến 200 ký tự";
  if (i.description.length > 2000) return "Mô tả tối đa 2000 ký tự";
  if (!i.categoryId) return "Vui lòng chọn danh mục";
  if (!Number.isInteger(i.servings) || i.servings < 1) return "Khẩu phần phải là số nguyên ≥ 1";
  if (i.prepTimeMinutes <= 0 || i.cookTimeMinutes <= 0) return "Thời gian sơ chế và nấu phải lớn hơn 0";
  if (i.nutrition && Object.values(i.nutrition).some(v => v !== null && v < 0)) return "Giá trị dinh dưỡng không được âm";
  return null;
}

const CONFLICT_MSG = "Công thức vừa được thay đổi ở nơi khác (tab hoặc thiết bị khác). Tải dữ liệu mới nhất rồi sửa lại.";

export default function RecipeWizard({ initial }: { initial?: Partial<WizardState> }) {
  const router = useRouter();
  const [s, dispatch] = useReducer(reducer, {
    step: 0, recipeId: null, slug: null, rowVersion: null, detail: null,
    info: emptyInfo, saving: false, error: null, conflict: false, ...initial,
  });
  const [categories, setCategories] = useState<Category[]>([]);

  useEffect(() => {
    if (!localStorage.getItem("accessToken")) { router.replace("/auth/login"); return; }
    getCategories().then(setCategories).catch(e => dispatch({ type: "error", message: messageOf(e) }));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function messageOf(e: unknown): string {
    if (e instanceof ApiError && e.code === "RECIPE_PUBLISH_INCOMPLETE")
      return "Cần ít nhất 1 nguyên liệu và 1 bước thực hiện trước khi xuất bản.";
    return e instanceof Error ? e.message : "Có lỗi xảy ra";
  }

  function handleError(e: unknown) {
    if (e instanceof UnauthorizedError) { router.replace("/auth/login"); return; }
    if (isConflict(e)) return dispatch({ type: "error", message: CONFLICT_MSG, conflict: true });
    dispatch({ type: "error", message: messageOf(e) });
  }

  async function reload(slug = s.slug) {
    if (!slug) return;
    dispatch({ type: "detail", detail: await getRecipeDetail(slug) });
  }

  // Conflict reload: lấy bản mới nhất từ server, bỏ phần sửa dở ở bước 1
  async function reloadLatest() {
    if (!s.slug) return;
    dispatch({ type: "saving" });
    try {
      const d = await getRecipeDetail(s.slug);
      dispatch({ type: "detail", detail: d });
      dispatch({ type: "resetInfo", info: toBasicInfo(d) });
    } catch (e) { handleError(e); }
  }

  const run: RunFn = async (fn, optimistic) => {
    const snapshot = s.detail;
    dispatch({ type: "saving" });
    if (optimistic && snapshot) dispatch({ type: "optimistic", detail: optimistic(snapshot) });
    try { await fn(); await reload(); return true; }
    catch (e) {
      if (e instanceof UnauthorizedError) { router.replace("/auth/login"); return false; }
      if (optimistic) {
        dispatch({ type: "rollback", detail: snapshot, message: `${messageOf(e)} — đã hoàn tác thay đổi.` });
        if (isConflict(e)) dispatch({ type: "error", message: CONFLICT_MSG, conflict: true });
      } else handleError(e);
      return false;
    }
  };

  async function saveBasic() {
    const err = validate(s.info);
    if (err) return dispatch({ type: "error", message: err });
    dispatch({ type: "saving" });
    try {
      const saved = s.recipeId
        ? await updateRecipe(s.recipeId, s.info, s.rowVersion!)
        : await createRecipe(s.info);
      await reload(saved.slug);
      // Đổi URL sang trang edit: F5 hay bấm lại không tạo thêm bản nháp trùng
      const url = `/dashboard/recipes/${saved.id}/edit?slug=${encodeURIComponent(saved.slug)}`;
      if (window.location.pathname + window.location.search !== url) window.history.replaceState(null, "", url);
      dispatch({ type: "goto", step: 1 });
    } catch (e) { handleError(e); }
  }

  const set = (patch: Partial<BasicInfo>) => dispatch({ type: "setInfo", patch });
  const setNut = (key: keyof Nutrition, raw: string) =>
    set({ nutrition: { ...(s.info.nutrition ?? emptyNutrition), [key]: raw === "" ? null : +raw } });
  const canGo = (i: number) => i === 0 || !!s.detail;
  const hasNutrition = !!s.info.nutrition && Object.values(s.info.nutrition).some(v => v !== null);
  const onError = (m: string) => dispatch({ type: "error", message: m });
  const last = STEPS.length - 1;

  return (
    <div className="mx-auto max-w-3xl p-6">
      <h1 className="mb-4 text-2xl font-bold">{s.recipeId ? "Sửa công thức" : "Tạo công thức mới"}</h1>

      <nav aria-label="Các bước soạn công thức">
        <ol className="mb-6 flex flex-wrap gap-2">
          {STEPS.map((label, i) => (
            <li key={label}>
              <button
                disabled={!canGo(i) || s.saving}
                aria-current={i === s.step ? "step" : undefined}
                onClick={() => dispatch({ type: "goto", step: i })}
                className={`rounded px-3 py-1 text-sm ${i === s.step ? "bg-orange-500 text-white" : "bg-gray-100"} disabled:opacity-40`}
              >
                {i + 1}. {label}
              </button>
            </li>
          ))}
        </ol>
      </nav>

      {s.step === 0 && (
        <div className="space-y-3">
          <label className="block text-sm">Tiêu đề *
            <input className="w-full rounded border p-2" placeholder="VD: Canh chua cá lóc" maxLength={200}
              value={s.info.title} onChange={e => set({ title: e.target.value })} /></label>
          <label className="block text-sm">Mô tả <span className="text-gray-400">({s.info.description.length}/2000)</span>
            <textarea className="w-full rounded border p-2" rows={3} maxLength={2000}
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
              <input type="number" min={1} step={1} className="w-full rounded border p-2"
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

          <details className="rounded border p-3" open={hasNutrition}>
            <summary className="cursor-pointer text-sm font-semibold">Dinh dưỡng (mỗi khẩu phần, không bắt buộc)</summary>
            <div className="mt-3 grid grid-cols-3 gap-3">
              {NUTRITION_FIELDS.map(f => (
                <label key={f.key} className="text-sm">{f.label} ({f.unit})
                  <input type="number" min={0} step="any" className="w-full rounded border p-2"
                    value={s.info.nutrition?.[f.key] ?? ""} onChange={e => setNut(f.key, e.target.value)} /></label>
              ))}
            </div>
          </details>
        </div>
      )}

      {s.step === 1 && s.detail && <IngredientsStep recipe={s.detail} busy={s.saving} run={run} onError={onError} />}
      {s.step === 2 && s.detail && <StepsStep recipe={s.detail} busy={s.saving} run={run} onError={onError} />}
      {s.step === 3 && s.detail && <ImagesStep recipe={s.detail} busy={s.saving} run={run} onError={onError} />}
      {s.step === 4 && s.detail && <ReviewStep recipe={s.detail} categories={categories} busy={s.saving} run={run} />}

      {s.error && (
        <div role="alert" className="mt-4 rounded bg-red-50 p-3 text-red-700">
          <p>{s.error}</p>
          {s.conflict && (
            <button onClick={reloadLatest} disabled={s.saving}
              className="mt-2 rounded bg-red-600 px-3 py-1 text-sm text-white disabled:opacity-50">
              Tải dữ liệu mới nhất
            </button>
          )}
        </div>
      )}

      <div className="mt-6 flex justify-between">
        <button disabled={s.step === 0 || s.saving} onClick={() => dispatch({ type: "goto", step: s.step - 1 })}
          className="rounded border px-4 py-2 disabled:opacity-40">← Quay lại</button>
        {s.step === 0 ? (
          <button onClick={saveBasic} disabled={s.saving}
            className="rounded bg-orange-500 px-4 py-2 text-white disabled:opacity-50">
            {s.saving ? "Đang lưu..." : "Lưu & tiếp →"}
          </button>
        ) : s.step < last ? (
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