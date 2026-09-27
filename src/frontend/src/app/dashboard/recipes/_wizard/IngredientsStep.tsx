"use client";

import { useState } from "react";
import {
  IngredientInput, RecipeDetail, addIngredient, deleteIngredient, updateIngredient,
} from "@/lib/recipe-editor";
import type { RunFn } from "./RecipeWizard";

interface Props { recipe: RecipeDetail; busy: boolean; run: RunFn; onError: (msg: string) => void }

const empty: IngredientInput = { name: "", quantity: null, unit: "", notes: "" };
const isTemp = (id: string) => id.startsWith("temp-");

export default function IngredientsStep({ recipe, busy, run, onError }: Props) {
  const [form, setForm] = useState<IngredientInput>(empty);
  const [editingId, setEditingId] = useState<string | null>(null);
  const items = [...recipe.ingredients].sort((a, b) => a.orderIndex - b.orderIndex);

  async function submit() {
    if (!form.name.trim()) return onError("Vui lòng nhập tên nguyên liệu");
    if (form.quantity !== null && form.quantity < 0) return onError("Số lượng không được âm");
    const body: IngredientInput = {
      name: form.name.trim(), quantity: form.quantity,
      unit: form.unit?.trim() || null, notes: form.notes?.trim() || null,
    };
    const id = editingId;
    setForm(empty); setEditingId(null);
    // Optimistic: hiện ngay trên bảng, lỗi thì wizard hoàn tác
    const ok = await run(
      () => id ? updateIngredient(recipe.id, id, body) : addIngredient(recipe.id, body),
      d => ({
        ...d,
        ingredients: id
          ? d.ingredients.map(i => i.id === id ? { ...i, ...body, unit: body.unit, notes: body.notes } : i)
          : [...d.ingredients, { id: `temp-${Date.now()}`, orderIndex: d.ingredients.length, name: body.name, quantity: body.quantity, unit: body.unit, notes: body.notes }],
      }));
    if (!ok) { setForm(body); setEditingId(id); } // trả lại nội dung form để sửa tiếp
  }

  function edit(id: string) {
    const i = items.find(x => x.id === id)!;
    setEditingId(id);
    setForm({ name: i.name, quantity: i.quantity, unit: i.unit ?? "", notes: i.notes ?? "" });
  }

  function remove(id: string, name: string) {
    if (!confirm(`Xoá nguyên liệu "${name}"?`)) return;
    if (editingId === id) { setEditingId(null); setForm(empty); }
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
              <tr key={i.id} className={`border-b ${editingId === i.id ? "bg-orange-50" : ""} ${isTemp(i.id) ? "italic text-gray-400" : ""}`}>
                <td className="py-2 font-medium">{i.name}{isTemp(i.id) && " (đang lưu…)"}</td>
                <td>{i.quantity ?? ""}</td>
                <td>{i.unit ?? ""}</td>
                <td className="text-gray-500">{i.notes ?? ""}</td>
                <td className="whitespace-nowrap text-right">
                  <button disabled={busy || isTemp(i.id)} onClick={() => edit(i.id)} className="mr-2 text-blue-600 disabled:opacity-40">Sửa</button>
                  <button disabled={busy || isTemp(i.id)} onClick={() => remove(i.id, i.name)} className="text-red-600 disabled:opacity-40">Xoá</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <div className="rounded border p-3">
        <p className="mb-2 text-sm font-semibold">{editingId ? "Sửa nguyên liệu" : "Thêm nguyên liệu"}</p>
        <div className="grid grid-cols-12 gap-2">
          <input aria-label="Tên nguyên liệu" className="col-span-4 rounded border p-2" placeholder="Tên *"
            value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} />
          <input aria-label="Số lượng" type="number" min={0} step="any" className="col-span-2 rounded border p-2" placeholder="SL"
            value={form.quantity ?? ""}
            onChange={e => setForm({ ...form, quantity: e.target.value === "" ? null : +e.target.value })} />
          <input aria-label="Đơn vị" className="col-span-2 rounded border p-2" placeholder="Đơn vị"
            value={form.unit ?? ""} onChange={e => setForm({ ...form, unit: e.target.value })} />
          <input aria-label="Ghi chú" className="col-span-4 rounded border p-2" placeholder="Ghi chú"
            value={form.notes ?? ""} onChange={e => setForm({ ...form, notes: e.target.value })}
            onKeyDown={e => e.key === "Enter" && submit()} />
        </div>
        <div className="mt-2 flex gap-2">
          <button disabled={busy} onClick={submit}
            className="rounded bg-emerald-600 px-3 py-1 text-sm text-white disabled:opacity-50">
            {editingId ? "Cập nhật" : "+ Thêm"}
          </button>
          {editingId && (
            <button onClick={() => { setEditingId(null); setForm(empty); }} className="rounded border px-3 py-1 text-sm">Huỷ</button>
          )}
        </div>
      </div>
    </div>
  );
}