"use client";

import { useEffect } from "react";
import { FieldErrors, Resolver, useFieldArray, useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { RecipeDetail, addIngredient, deleteIngredient, updateIngredient } from "@/lib/recipe-editor";
import { IngredientFormInput, emptyToNull, ingredientSchema } from "@/lib/recipe-schemas";
import { ariaOf, ErrorText } from "./FieldError";
import type { RunFn } from "./RecipeWizard";

interface Props {
  recipe: RecipeDetail; busy: boolean; run: RunFn; onError: (msg: string) => void;
  /** Báo wizard số dòng nháp có nội dung chưa lưu -> chặn chuyển bước */
  onPendingChange: (count: number) => void;
}

/** Dòng nháp. useFieldArray chiếm key "id" nên id nguyên liệu trên server để ở serverId (null = dòng thêm mới) */
type DraftRow = IngredientFormInput & { serverId: string | null };
type DraftForm = { drafts: DraftRow[] };

const blankRow: DraftRow = { serverId: null, name: "", quantity: null, unit: "", notes: "" };
const isTemp = (id: string) => id.startsWith("temp-");
/** Dòng thêm mới chưa gõ gì: không kiểm tra, không tính là chưa lưu */
const isBlank = (r: DraftRow) =>
  !r.serverId && !r.name.trim() && r.quantity == null && !r.unit.trim() && !r.notes.trim();

// Lọc dòng nháp trống trước khi qua zodResolver, rồi trả lỗi về đúng chỉ số dòng gốc
const rowsResolver = zodResolver(z.object({ drafts: z.array(ingredientSchema) }));
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
export default function IngredientsStep({ recipe, busy, run, onPendingChange }: Props) {
  const items = [...recipe.ingredients].sort((a, b) => a.orderIndex - b.orderIndex);
  const { register, control, trigger, getValues, setFocus, formState: { errors } } = useForm<DraftForm>({
    resolver: draftsResolver,
    mode: "onTouched",
    defaultValues: { drafts: [blankRow] },
  });
  const { fields, append, insert, remove } = useFieldArray({ control, name: "drafts" });
  const drafts = useWatch({ control, name: "drafts" }) ?? [];
  const pending = drafts.filter(r => !isBlank(r)).length;
  const editingIds = new Set(drafts.map(r => r.serverId).filter((x): x is string => !!x));

  useEffect(() => { onPendingChange(pending); }, [pending, onPendingChange]);
  useEffect(() => () => onPendingChange(0), [onPendingChange]);

  function removeRow(index: number) {
    remove(index);
    if (getValues("drafts").length === 0) append(blankRow, { shouldFocus: false });
  }

  async function saveRow(index: number) {
    const row = getValues(`drafts.${index}`);
    if (isBlank(row)) return;
    if (!(await trigger(`drafts.${index}`))) return;
    // Output đã parse (trim, rỗng -> null, bỏ serverId) -> ingBody gửi đúng 4 field cho backend JSON strict
    const body = ingredientSchema.parse(row);
    const id = row.serverId;
    removeRow(index);
    // Optimistic: hiện ngay trên bảng, lỗi thì wizard hoàn tác
    const ok = await run(
      () => id ? updateIngredient(recipe.id, id, body) : addIngredient(recipe.id, body),
      d => ({
        ...d,
        ingredients: id
          ? d.ingredients.map(i => i.id === id ? { ...i, ...body } : i)
          : [...d.ingredients, { id: `temp-${Date.now()}`, orderIndex: d.ingredients.length, ...body }],
      }));
    if (!ok) insert(Math.min(index, getValues("drafts").length), row); // trả lại dòng nháp để sửa tiếp
  }

  function edit(id: string) {
    const existing = drafts.findIndex(r => r.serverId === id);
    if (existing >= 0) return setFocus(`drafts.${existing}.name`);
    const i = items.find(x => x.id === id)!;
    append({ serverId: id, name: i.name, quantity: i.quantity, unit: i.unit ?? "", notes: i.notes ?? "" });
  }

  function removeServer(id: string, name: string) {
    if (!confirm(`Xoá nguyên liệu "${name}"?`)) return;
    const idx = drafts.findIndex(r => r.serverId === id);
    if (idx >= 0) removeRow(idx);
    run(() => deleteIngredient(recipe.id, id), d => ({ ...d, ingredients: d.ingredients.filter(i => i.id !== id) }));
  }

  return (
    <div className="space-y-4">
      {items.length === 0 ? (
        <p className="text-gray-500">Chưa có nguyên liệu nào.</p>
      ) : (
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b text-left text-gray-500">
              <th className="py-2">Tên</th><th>Số lượng</th><th>Đơn vị</th><th>Ghi chú</th><th><span className="sr-only">Thao tác</span></th>
            </tr>
          </thead>
          <tbody>
            {items.map(i => (
              <tr key={i.id} className={`border-b ${editingIds.has(i.id) ? "bg-orange-50" : ""} ${isTemp(i.id) ? "italic text-gray-400" : ""}`}>
                <td className="py-2 font-medium">{i.name}{isTemp(i.id) && " (đang lưu…)"}</td>
                <td>{i.quantity ?? ""}</td>
                <td>{i.unit ?? ""}</td>
                <td className="text-gray-500">{i.notes ?? ""}</td>
                <td className="whitespace-nowrap text-right">
                  <button disabled={busy || isTemp(i.id)} onClick={() => edit(i.id)} aria-label={`Sửa nguyên liệu ${i.name}`} className="mr-2 text-blue-600 disabled:opacity-40">Sửa</button>
                  <button disabled={busy || isTemp(i.id)} onClick={() => removeServer(i.id, i.name)} aria-label={`Xoá nguyên liệu ${i.name}`} className="text-red-600 disabled:opacity-40">Xoá</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <div className="space-y-3 rounded border p-3">
        <p className="text-sm font-semibold">Thêm / sửa nguyên liệu</p>
        {fields.map((f, index) => {
          const e = errors.drafts?.[index];
          const row = drafts[index] ?? blankRow;
          const n = index + 1;
          const errId = (k: string) => `err-ing-${f.id}-${k}`;
          return (
            <div key={f.id} className={`rounded p-2 ${row.serverId ? "bg-orange-50" : ""}`}>
              <p className="mb-1 text-xs text-gray-500">{row.serverId ? `Đang sửa: ${items.find(x => x.id === row.serverId)?.name ?? ""}` : `Dòng mới ${n}`}</p>
              <div className="grid grid-cols-12 gap-2">
                <div className="col-span-4">
                  <input aria-label={`Tên nguyên liệu (dòng ${n})`} className="w-full rounded border p-2" placeholder="Tên *" maxLength={200}
                    {...register(`drafts.${index}.name`)} {...ariaOf(errId("name"), e?.name)} />
                  <ErrorText id={errId("name")} error={e?.name} />
                </div>
                <div className="col-span-2">
                  <input aria-label={`Số lượng (dòng ${n})`} type="number" min={0} step="any" className="w-full rounded border p-2" placeholder="SL"
                    {...register(`drafts.${index}.quantity`, { setValueAs: emptyToNull })} {...ariaOf(errId("quantity"), e?.quantity)} />
                  <ErrorText id={errId("quantity")} error={e?.quantity} />
                </div>
                <div className="col-span-2">
                  <input aria-label={`Đơn vị (dòng ${n})`} className="w-full rounded border p-2" placeholder="Đơn vị" maxLength={50}
                    {...register(`drafts.${index}.unit`)} {...ariaOf(errId("unit"), e?.unit)} />
                  <ErrorText id={errId("unit")} error={e?.unit} />
                </div>
                <div className="col-span-4">
                  <input aria-label={`Ghi chú (dòng ${n})`} className="w-full rounded border p-2" placeholder="Ghi chú" maxLength={500}
                    {...register(`drafts.${index}.notes`)} {...ariaOf(errId("notes"), e?.notes)}
                    onKeyDown={ev => { if (ev.key === "Enter" && !busy) saveRow(index); }} />
                  <ErrorText id={errId("notes")} error={e?.notes} />
                </div>
              </div>
              <div className="mt-2 flex gap-2">
                <button disabled={busy || isBlank(row)} onClick={() => saveRow(index)}
                  className="rounded bg-emerald-600 px-3 py-1 text-sm text-white disabled:opacity-50">
                  {row.serverId ? "Cập nhật" : "Lưu"}
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
