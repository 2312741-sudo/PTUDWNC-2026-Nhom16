"use client";

import { useState } from "react";
import {
  RecipeDetail, StepInput, addStep, deleteStep, reorderSteps, updateStep,
} from "@/lib/recipe-editor";

interface Props {
  recipe: RecipeDetail;
  busy: boolean;
  run: (fn: () => Promise<unknown>) => Promise<boolean>;
  onError: (msg: string) => void;
}

const empty: StepInput = { title: "", description: "", timerMinutes: null };

export default function StepsStep({ recipe, busy, run, onError }: Props) {
  const [form, setForm] = useState<StepInput>(empty);
  const [editingId, setEditingId] = useState<string | null>(null);
  const items = [...recipe.steps].sort((a, b) => a.stepNumber - b.stepNumber);

  async function submit() {
    if (!form.title.trim()) return onError("Vui lòng nhập tiêu đề bước");
    if (!form.description.trim()) return onError("Vui lòng nhập mô tả bước");
    if (form.timerMinutes !== null && form.timerMinutes < 0) return onError("Hẹn giờ không được âm");
    const body: StepInput = {
      title: form.title.trim(), description: form.description.trim(), timerMinutes: form.timerMinutes,
    };
    const ok = await run(() => editingId ? updateStep(recipe.id, editingId, body) : addStep(recipe.id, body));
    if (ok) { setForm(empty); setEditingId(null); }
  }

  function edit(id: string) {
    const s = items.find(x => x.id === id)!;
    setEditingId(id);
    setForm({ title: s.title, description: s.description, timerMinutes: s.timerMinutes });
  }

  function remove(id: string, title: string) {
    if (!confirm(`Xoá bước "${title}"?`)) return;
    run(() => deleteStep(recipe.id, id));
    if (editingId === id) { setEditingId(null); setForm(empty); }
  }

  function move(index: number, dir: -1 | 1) {
    const ids = items.map(s => s.id);
    const j = index + dir;
    if (j < 0 || j >= ids.length) return;
    [ids[index], ids[j]] = [ids[j], ids[index]];
    run(() => reorderSteps(recipe.id, ids));
  }

  return (
    <div className="space-y-4">
      {items.length === 0 ? (
        <p className="text-gray-500">Chưa có bước nào.</p>
      ) : (
        <ol className="space-y-2">
          {items.map((s, idx) => (
            <li key={s.id} className={`flex gap-3 rounded border p-3 ${editingId === s.id ? "bg-orange-50" : ""}`}>
              <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-orange-500 text-sm text-white">
                {idx + 1}
              </span>
              <div className="flex-1">
                <p className="font-medium">
                  {s.title}
                  {s.timerMinutes ? <span className="ml-2 text-xs text-gray-500">⏱ {s.timerMinutes} phút</span> : null}
                </p>
                <p className="whitespace-pre-line text-sm text-gray-600">{s.description}</p>
              </div>
              <div className="flex shrink-0 flex-col items-end gap-1 text-sm">
                <div>
                  <button disabled={busy || idx === 0} onClick={() => move(idx, -1)} className="px-1 disabled:opacity-30" title="Lên">▲</button>
                  <button disabled={busy || idx === items.length - 1} onClick={() => move(idx, 1)} className="px-1 disabled:opacity-30" title="Xuống">▼</button>
                </div>
                <button disabled={busy} onClick={() => edit(s.id)} className="text-blue-600 disabled:opacity-40">Sửa</button>
                <button disabled={busy} onClick={() => remove(s.id, s.title)} className="text-red-600 disabled:opacity-40">Xoá</button>
              </div>
            </li>
          ))}
        </ol>
      )}

      <div className="space-y-2 rounded border p-3">
        <p className="text-sm font-semibold">{editingId ? "Sửa bước" : "Thêm bước"}</p>
        <div className="grid grid-cols-4 gap-2">
          <input className="col-span-3 rounded border p-2" placeholder="Tiêu đề bước *"
            value={form.title} onChange={e => setForm({ ...form, title: e.target.value })} />
          <input type="number" min={0} className="rounded border p-2" placeholder="Hẹn giờ (phút)"
            value={form.timerMinutes ?? ""}
            onChange={e => setForm({ ...form, timerMinutes: e.target.value === "" ? null : +e.target.value })} />
        </div>
        <textarea className="w-full rounded border p-2" rows={3} placeholder="Mô tả chi tiết *"
          value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} />
        <div className="flex gap-2">
          <button disabled={busy} onClick={submit}
            className="rounded bg-emerald-600 px-3 py-1 text-sm text-white disabled:opacity-50">
            {editingId ? "Cập nhật" : "+ Thêm bước"}
          </button>
          {editingId && (
            <button onClick={() => { setEditingId(null); setForm(empty); }} className="rounded border px-3 py-1 text-sm">Huỷ</button>
          )}
        </div>
      </div>
    </div>
  );
}