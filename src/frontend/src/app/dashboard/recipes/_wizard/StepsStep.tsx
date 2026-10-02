"use client";

import { useEffect } from "react";
import { FieldErrors, Resolver, useFieldArray, useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { RecipeDetail, addStep, deleteStep, reorderSteps, updateStep } from "@/lib/recipe-editor";
import { StepFormInput, emptyToNull, stepSchema } from "@/lib/recipe-schemas";
import { ariaOf, ErrorText } from "./FieldError";
import type { RunFn } from "./RecipeWizard";

interface Props {
  recipe: RecipeDetail; busy: boolean; run: RunFn; onError: (msg: string) => void;
  /** Báo wizard số dòng nháp có nội dung chưa lưu -> chặn chuyển bước */
  onPendingChange: (count: number) => void;
}

/** Dòng nháp. useFieldArray chiếm key "id" nên id bước trên server để ở serverId (null = bước thêm mới) */
type DraftRow = StepFormInput & { serverId: string | null };
type DraftForm = { drafts: DraftRow[] };

const blankRow: DraftRow = { serverId: null, title: "", description: "", timerMinutes: null };
const isTemp = (id: string) => id.startsWith("temp-");
/** Bước thêm mới chưa gõ gì: không kiểm tra, không tính là chưa lưu */
const isBlank = (r: DraftRow) =>
  !r.serverId && !r.title.trim() && !r.description.trim() && r.timerMinutes == null;

// Lọc dòng nháp trống trước khi qua zodResolver, rồi trả lỗi về đúng chỉ số dòng gốc
const rowsResolver = zodResolver(z.object({ drafts: z.array(stepSchema) }));
const draftsResolver: Resolver<DraftForm> = async (values, context, options) => {
  const kept = values.drafts.flatMap((r, i) => (isBlank(r) ? [] : [i]));
  const result = await rowsResolver({ drafts: kept.map(i => values.drafts[i]) }, context, options as never);
  const rowErrors = result.errors.drafts as unknown[] | undefined;
  if (!rowErrors) return { values, errors: {} };
  const drafts: unknown[] = [];
  kept.forEach((orig, j) => { if (rowErrors[j]) drafts[orig] = rowErrors[j]; });
  return { values: {}, errors: { drafts } as FieldErrors<DraftForm> };
};

// onError không còn dùng: lỗi nhập hiện dưới từng ô; lỗi server do run() đưa lên banner
export default function StepsStep({ recipe, busy, run, onPendingChange }: Props) {
  const items = [...recipe.steps].sort((a, b) => a.stepNumber - b.stepNumber);
  const { register, control, trigger, getValues, setFocus, formState: { errors } } = useForm<DraftForm>({
    resolver: draftsResolver,
    mode: "onTouched",
    defaultValues: { drafts: [blankRow] },
  });
  const { fields, append, insert, remove: removeField } = useFieldArray({ control, name: "drafts" });
  const drafts = useWatch({ control, name: "drafts" }) ?? [];
  const pending = drafts.filter(r => !isBlank(r)).length;
  const editingIds = new Set(drafts.map(r => r.serverId).filter((x): x is string => !!x));

  useEffect(() => { onPendingChange(pending); }, [pending, onPendingChange]);
  useEffect(() => () => onPendingChange(0), [onPendingChange]);

  function removeRow(index: number) {
    removeField(index);
    if (getValues("drafts").length === 0) append(blankRow, { shouldFocus: false });
  }

  async function saveRow(index: number) {
    const row = getValues(`drafts.${index}`);
    if (isBlank(row)) return;
    if (!(await trigger(`drafts.${index}`))) return;
    // Output đã parse (trim, bỏ serverId) -> stepBody gửi đúng 4 field (imageUrl: null) cho backend JSON strict
    const body = stepSchema.parse(row);
    const id = row.serverId;
    removeRow(index);
    // Optimistic: hiện ngay trong danh sách, lỗi thì wizard hoàn tác
    const ok = await run(
      () => id ? updateStep(recipe.id, id, body) : addStep(recipe.id, body),
      d => ({
        ...d,
        steps: id
          ? d.steps.map(s => s.id === id ? { ...s, ...body } : s)
          : [...d.steps, { id: `temp-${Date.now()}`, stepNumber: d.steps.length + 1, ...body }],
      }));
    if (!ok) insert(Math.min(index, getValues("drafts").length), row); // trả lại dòng nháp để sửa tiếp
  }

  function edit(id: string) {
    const existing = drafts.findIndex(r => r.serverId === id);
    if (existing >= 0) return setFocus(`drafts.${existing}.title`);
    const s = items.find(x => x.id === id)!;
    append({ serverId: id, title: s.title, description: s.description, timerMinutes: s.timerMinutes });
  }

  function remove(id: string, title: string) {
    if (!confirm(`Xoá bước "${title}"?`)) return;
    const idx = drafts.findIndex(r => r.serverId === id);
    if (idx >= 0) removeRow(idx);
    run(() => deleteStep(recipe.id, id), d => ({
      ...d,
      steps: [...d.steps].sort((a, b) => a.stepNumber - b.stepNumber)
        .filter(s => s.id !== id).map((s, i) => ({ ...s, stepNumber: i + 1 })),
    }));
  }

  function move(index: number, dir: -1 | 1) {
    const ids = items.map(s => s.id);
    const j = index + dir;
    if (j < 0 || j >= ids.length) return;
    [ids[index], ids[j]] = [ids[j], ids[index]];
    run(() => reorderSteps(recipe.id, ids), d => ({
      ...d, steps: ids.map((sid, i) => ({ ...d.steps.find(s => s.id === sid)!, stepNumber: i + 1 })),
    }));
  }

  return (
    <div className="space-y-4">
      {items.length === 0 ? (
        <p className="text-gray-500">Chưa có bước nào.</p>
      ) : (
        <ol className="space-y-2">
          {items.map((s, idx) => (
            <li key={s.id} className={`flex gap-3 rounded border p-3 ${editingIds.has(s.id) ? "bg-orange-50" : ""} ${isTemp(s.id) ? "italic text-gray-400" : ""}`}>
              <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-orange-500 text-sm text-white" aria-hidden>
                {idx + 1}
              </span>
              <div className="flex-1">
                <p className="font-medium">
                  {s.title}{isTemp(s.id) && " (đang lưu…)"}
                  {s.timerMinutes ? <span className="ml-2 text-xs text-gray-500">⏱ {s.timerMinutes} phút</span> : null}
                </p>
                <p className="whitespace-pre-line text-sm text-gray-600">{s.description}</p>
              </div>
              <div className="flex shrink-0 flex-col items-end gap-1 text-sm">
                <div>
                  <button disabled={busy || idx === 0 || isTemp(s.id)} onClick={() => move(idx, -1)} className="px-1 disabled:opacity-30" aria-label={`Đưa bước ${idx + 1} lên`}>▲</button>
                  <button disabled={busy || idx === items.length - 1 || isTemp(s.id)} onClick={() => move(idx, 1)} className="px-1 disabled:opacity-30" aria-label={`Đưa bước ${idx + 1} xuống`}>▼</button>
                </div>
                <button disabled={busy || isTemp(s.id)} onClick={() => edit(s.id)} className="text-blue-600 disabled:opacity-40">Sửa<span className="sr-only"> bước {idx + 1}: {s.title}</span></button>
                <button disabled={busy || isTemp(s.id)} onClick={() => remove(s.id, s.title)} className="text-red-600 disabled:opacity-40">Xoá<span className="sr-only"> bước {idx + 1}: {s.title}</span></button>
              </div>
            </li>
          ))}
        </ol>
      )}

      <div className="space-y-3 rounded border p-3">
        <p className="text-sm font-semibold">Thêm / sửa bước</p>
        {fields.map((f, index) => {
          const e = errors.drafts?.[index];
          const row = drafts[index] ?? blankRow;
          const n = index + 1;
          const errId = (k: string) => `err-step-${f.id}-${k}`;
          return (
            <div key={f.id} className={`space-y-2 rounded p-2 ${row.serverId ? "bg-orange-50" : ""}`}>
              <p className="text-xs text-gray-500">
                {row.serverId ? `Đang sửa bước ${items.findIndex(x => x.id === row.serverId) + 1}` : `Bước mới (dòng ${n})`}
              </p>
              <div className="grid grid-cols-4 gap-2">
                <div className="col-span-3">
                  <input aria-label={`Tiêu đề bước (dòng ${n})`} className="w-full rounded border p-2" placeholder="Tiêu đề bước *" maxLength={200}
                    {...register(`drafts.${index}.title`)} {...ariaOf(errId("title"), e?.title)} />
                  <ErrorText id={errId("title")} error={e?.title} />
                </div>
                <div>
                  <input aria-label={`Hẹn giờ (phút) (dòng ${n})`} type="number" min={0} step={1} className="w-full rounded border p-2" placeholder="Hẹn giờ (phút)"
                    {...register(`drafts.${index}.timerMinutes`, { setValueAs: emptyToNull })} {...ariaOf(errId("timer"), e?.timerMinutes)} />
                  <ErrorText id={errId("timer")} error={e?.timerMinutes} />
                </div>
              </div>
              <div>
                <textarea aria-label={`Mô tả chi tiết (dòng ${n})`} className="w-full rounded border p-2" rows={3} placeholder="Mô tả chi tiết *" maxLength={2000}
                  {...register(`drafts.${index}.description`)} {...ariaOf(errId("description"), e?.description)} />
                <ErrorText id={errId("description")} error={e?.description} />
              </div>
              <div className="flex gap-2">
                <button disabled={busy || isBlank(row)} onClick={() => saveRow(index)}
                  className="rounded bg-emerald-600 px-3 py-1 text-sm text-white disabled:opacity-50">
                  {row.serverId ? "Cập nhật" : "Lưu bước"}
                </button>
                {(row.serverId || !isBlank(row) || fields.length > 1) && (
                  <button disabled={busy} onClick={() => removeRow(index)} className="rounded border px-3 py-1 text-sm disabled:opacity-50">Bỏ</button>
                )}
              </div>
            </div>
          );
        })}
        <button disabled={busy} onClick={() => append(blankRow)} className="text-sm text-emerald-700 disabled:opacity-50">+ Thêm dòng</button>
      </div>
    </div>
  );
}
