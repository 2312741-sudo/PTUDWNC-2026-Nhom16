"use client";

import { useEffect, useReducer, useState } from "react";
import { useRouter } from "next/navigation";
import { Controller, FieldError, useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  ApiError, BasicInfo, Category, DIFFICULTIES, NUTRITION_FIELDS, RecipeDetail, UnauthorizedError,
  createRecipe, getCategories, getRecipeDetail, isConflict, toBasicInfo, updateRecipe,
} from "@/lib/recipe-editor";
import { BasicInfoInput, BasicInfoOutput, basicInfoSchema, emptyToNull } from "@/lib/recipe-schemas";
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
  /** Chỉ dùng làm defaultValues cho form bước 1; sau đó React Hook Form giữ giá trị */
  info: BasicInfo;
  detail: RecipeDetail | null;
  saving: boolean;
  error: string | null;
  conflict: boolean;
}

type ReducerState = Omit<WizardState, "info">;

type Action =
  | { type: "goto"; step: number }
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

function reducer(s: ReducerState, a: Action): ReducerState {
  switch (a.type) {
    case "goto": return { ...s, step: a.step, error: null, conflict: false };
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

/** Thông báo lỗi dưới từng trường; id dùng cho aria-describedby của ô nhập */
function ErrorText({ id, error }: { id: string; error?: FieldError }) {
  return error?.message ? <p id={id} className="mt-1 text-xs text-red-600">{error.message}</p> : null;
}

const ariaOf = (id: string, error?: FieldError) =>
  ({ "aria-invalid": error ? true : undefined, "aria-describedby": error ? id : undefined });

const CONFLICT_MSG = "Công thức vừa được thay đổi ở nơi khác (tab hoặc thiết bị khác). Tải dữ liệu mới nhất rồi sửa lại.";

export default function RecipeWizard({ initial }: { initial?: Partial<WizardState> }) {
  const router = useRouter();
  const { info: initialInfo, ...initialState } = initial ?? {};
  const [s, dispatch] = useReducer(reducer, {
    step: 0, recipeId: null, slug: null, rowVersion: null, detail: null,
    saving: false, error: null, conflict: false, ...initialState,
  });
  const form = useForm<BasicInfoInput, unknown, BasicInfoOutput>({
    resolver: zodResolver(basicInfoSchema),
    defaultValues: initialInfo ?? emptyInfo,
  });
  const { register, control, handleSubmit, formState: { errors } } = form;
  const description = useWatch({ control, name: "description" });
  const nutrition = useWatch({ control, name: "nutrition" });
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

  // Xoá cache ISR phía server + Router Cache phía trình duyệt -> trang công khai thấy thay đổi ngay
  function refreshPublic(...slugs: (string | null | undefined)[]) {
    const list = [...new Set(slugs.filter((x): x is string => !!x))];
    if (list.length === 0) return;
    const token = localStorage.getItem("accessToken");
    fetch("/api/revalidate", {
      method: "POST",
      headers: { "Content-Type": "application/json", ...(token ? { Authorization: `Bearer ${token}` } : {}) },
      body: JSON.stringify({ slugs: list }),
    })
      .catch(() => { /* revalidate lỗi không chặn wizard; ISR vẫn tự làm mới sau 5 phút */ })
      .finally(() => router.refresh());
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
      form.reset(toBasicInfo(d));
    } catch (e) { handleError(e); }
  }

  const run: RunFn = async (fn, optimistic) => {
    const snapshot = s.detail;
    dispatch({ type: "saving" });
    if (optimistic && snapshot) dispatch({ type: "optimistic", detail: optimistic(snapshot) });
    try { await fn(); await reload(); refreshPublic(s.slug); return true; }
    catch (e) {
      if (e instanceof UnauthorizedError) { router.replace("/auth/login"); return false; }
      if (optimistic) {
        dispatch({ type: "rollback", detail: snapshot, message: `${messageOf(e)} — đã hoàn tác thay đổi.` });
        if (isConflict(e)) dispatch({ type: "error", message: CONFLICT_MSG, conflict: true });
      } else handleError(e);
      return false;
    }
  };

  // info là output đã parse của zodResolver (đã bỏ key thừa) -> gửi thẳng cho backend JSON strict
  async function saveBasic(info: BasicInfoOutput) {
    dispatch({ type: "saving" });
    try {
      const saved = s.recipeId
        ? await updateRecipe(s.recipeId, info, s.rowVersion!)
        : await createRecipe(info);
      await reload(saved.slug);
      refreshPublic(saved.slug, s.slug); // đổi tiêu đề có thể đổi slug -> làm mới cả slug cũ
      // Đổi URL sang trang edit: F5 hay bấm lại không tạo thêm bản nháp trùng
      const url = `/dashboard/recipes/${saved.id}/edit?slug=${encodeURIComponent(saved.slug)}`;
      if (window.location.pathname + window.location.search !== url) window.history.replaceState(null, "", url);
      dispatch({ type: "goto", step: 1 });
    } catch (e) { handleError(e); }
  }

  const canGo = (i: number) => i === 0 || !!s.detail;
  const hasNutrition = !!nutrition && Object.values(nutrition).some(v => v !== null);
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
              {...register("title")} {...ariaOf("err-title", errors.title)} />
            <ErrorText id="err-title" error={errors.title} /></label>
          <label className="block text-sm">Mô tả <span className="text-gray-400">({description?.length ?? 0}/2000)</span>
            <textarea className="w-full rounded border p-2" rows={3} maxLength={2000}
              {...register("description")} {...ariaOf("err-description", errors.description)} />
            <ErrorText id="err-description" error={errors.description} /></label>
          <label className="block text-sm">Hướng dẫn chung
            <textarea className="w-full rounded border p-2" rows={4} {...register("instructions")} /></label>
          <div className="grid grid-cols-3 gap-3">
            <label className="text-sm">Sơ chế (phút) *
              <input type="number" min={1} step={1} className="w-full rounded border p-2"
                {...register("prepTimeMinutes", { valueAsNumber: true })} {...ariaOf("err-prep", errors.prepTimeMinutes)} />
              <ErrorText id="err-prep" error={errors.prepTimeMinutes} /></label>
            <label className="text-sm">Nấu (phút) *
              <input type="number" min={0} step={1} className="w-full rounded border p-2"
                {...register("cookTimeMinutes", { valueAsNumber: true })} {...ariaOf("err-cook", errors.cookTimeMinutes)} />
              <ErrorText id="err-cook" error={errors.cookTimeMinutes} /></label>
            <label className="text-sm">Khẩu phần *
              <input type="number" min={1} step={1} className="w-full rounded border p-2"
                {...register("servings", { valueAsNumber: true })} {...ariaOf("err-servings", errors.servings)} />
              <ErrorText id="err-servings" error={errors.servings} /></label>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <label className="text-sm">Độ khó
              <select className="w-full rounded border p-2"
                {...register("difficulty")} {...ariaOf("err-difficulty", errors.difficulty)}>
                {DIFFICULTIES.map(d => <option key={d.value} value={d.value}>{d.label}</option>)}
              </select>
              <ErrorText id="err-difficulty" error={errors.difficulty} /></label>
            <label className="text-sm">Danh mục *
              {/* Controller: option tải bất đồng bộ, select cần value có kiểm soát để hiện đúng danh mục đã chọn */}
              <Controller control={control} name="categoryId" render={({ field }) => (
                <select className="w-full rounded border p-2" {...field} {...ariaOf("err-category", errors.categoryId)}>
                  <option value="">-- Chọn danh mục --</option>
                  {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                </select>
              )} />
              <ErrorText id="err-category" error={errors.categoryId} /></label>
          </div>

          <details className="rounded border p-3" open={hasNutrition || !!errors.nutrition}>
            <summary className="cursor-pointer text-sm font-semibold">Dinh dưỡng (mỗi khẩu phần, không bắt buộc)</summary>
            <div className="mt-3 grid grid-cols-3 gap-3">
              {NUTRITION_FIELDS.map(f => (
                <label key={f.key} className="text-sm">{f.label} ({f.unit})
                  <input type="number" min={0} max={999999.99} step="any" className="w-full rounded border p-2"
                    {...register(`nutrition.${f.key}`, { setValueAs: emptyToNull })}
                    {...ariaOf(`err-nut-${f.key}`, errors.nutrition?.[f.key])} />
                  <ErrorText id={`err-nut-${f.key}`} error={errors.nutrition?.[f.key]} /></label>
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
          <button onClick={handleSubmit(saveBasic)} disabled={s.saving}
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