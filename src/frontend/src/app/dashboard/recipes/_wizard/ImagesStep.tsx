"use client";

import { useRef, useState } from "react";
import {
  IMAGE_MAX_BYTES, IMAGE_TYPES, RecipeDetail, RecipeImage, deleteImage, imageSrc, isPresignedStale, updateImage,
  uploadImage,
} from "@/lib/recipe-editor";
import type { RunFn } from "./RecipeWizard";

interface Props { recipe: RecipeDetail; busy: boolean; run: RunFn; onError: (msg: string) => void }

export default function ImagesStep({ recipe, busy, run, onError }: Props) {
  const fileRef = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [alt, setAlt] = useState("");
  const images: RecipeImage[] = [...(recipe.images ?? [])].sort((a, b) =>
    Number(b.isPrimary) - Number(a.isPrimary) || a.orderIndex - b.orderIndex);

  function pick(f: File | null) {
    if (!f) return setFile(null);
    // Kiểm tra sớm ở client; backend vẫn kiểm tra magic bytes (không tin Content-Type)
    if (!IMAGE_TYPES.includes(f.type)) { onError("Chỉ nhận ảnh JPEG, PNG, WebP hoặc AVIF"); return; }
    if (f.size > IMAGE_MAX_BYTES) { onError("Ảnh tối đa 5 MiB"); return; }
    setFile(f);
  }

  async function upload() {
    if (!file) return onError("Chọn một ảnh trước");
    if (alt.length > 200) return onError("Mô tả ảnh tối đa 200 ký tự");
    const ok = await run(() => uploadImage(recipe.id, file, alt.trim() || null));
    if (ok) { setFile(null); setAlt(""); if (fileRef.current) fileRef.current.value = ""; }
  }

  const setPrimary = (img: RecipeImage) =>
    run(() => updateImage(recipe.id, img, { isPrimary: true }),
      d => ({ ...d, images: (d.images ?? []).map(i => ({ ...i, isPrimary: i.id === img.id })) }));

  function remove(img: RecipeImage) {
    if (!confirm("Xoá ảnh này?")) return;
    run(() => deleteImage(recipe.id, img.id), d => ({ ...d, images: (d.images ?? []).filter(i => i.id !== img.id) }));
  }

  /**
   * B5: URL ký hết hạn sau ~10 phút nên thẻ <img> sẽ 403. PATCH lại chính ảnh đó (không đổi field nào)
   * để nhận presignedUrl mới; `run` tự reload detail nên state được đồng bộ.
   */
  const refreshUrl = (img: RecipeImage) =>
    run(() => updateImage(recipe.id, img, {}));

  return (
    <div className="space-y-4">
      {images.length === 0 ? (
        <p className="text-gray-500">Chưa có ảnh. Ảnh đầu tiên sẽ tự thành ảnh chính.</p>
      ) : (
        <ul className="grid grid-cols-2 gap-3 md:grid-cols-3">
          {images.map(img => {
            const src = imageSrc(img);
            // B5: URL ký là bearer token trong query string -> không gửi Referer, và phải báo
            // khi hết hạn vì <img> không tự retry (bị 403 image.forbidden).
            const stale = isPresignedStale(img.presignedUrl);
            return (
              <li key={img.id} className={`overflow-hidden rounded border ${img.isPrimary ? "ring-2 ring-emerald-500" : ""}`}>
                {src && !stale
                  // eslint-disable-next-line @next/next/no-img-element
                  ? <img src={src} alt={img.altText ?? recipe.title} referrerPolicy="no-referrer"
                      className="h-32 w-full object-cover" />
                  : <div className="flex h-32 flex-col items-center justify-center gap-1 bg-gray-100 p-2 text-center text-xs text-gray-500">
                      {stale ? <>Ảnh đã hết hạn liên kết tải</> : <>Chưa cấu hình NEXT_PUBLIC_MEDIA_URL<br />{img.originalUrl}</>}
                    </div>}
                {stale && (
                  <button disabled={busy} onClick={() => refreshUrl(img)}
                    className="w-full border-t bg-amber-50 py-1 text-xs text-amber-800 disabled:opacity-40">
                    {busy ? "Đang tải lại..." : "Tải lại liên kết ảnh"}
                  </button>
                )}
                <div className="space-y-1 p-2 text-xs">
                  {img.isPrimary && <span className="rounded bg-emerald-100 px-1 text-emerald-700">Ảnh chính</span>}
                  {img.altText && <p className="truncate text-gray-600">{img.altText}</p>}
                  <div className="flex gap-2">
                    {!img.isPrimary && (
                      <button disabled={busy} onClick={() => setPrimary(img)} className="text-blue-600 disabled:opacity-40">Đặt làm ảnh chính</button>
                    )}
                    <button disabled={busy} onClick={() => remove(img)} className="text-red-600 disabled:opacity-40">Xoá</button>
                  </div>
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <div className="space-y-2 rounded border p-3">
        <p className="text-sm font-semibold">Tải ảnh lên</p>
        <input ref={fileRef} type="file" accept={IMAGE_TYPES.join(",")} aria-label="Chọn ảnh"
          onChange={e => pick(e.target.files?.[0] ?? null)} className="block text-sm" />
        <input aria-label="Mô tả ảnh" className="w-full rounded border p-2 text-sm" placeholder="Mô tả ảnh (alt, không bắt buộc)" maxLength={200}
          value={alt} onChange={e => setAlt(e.target.value)} />
        <p className="text-xs text-gray-500">JPEG, PNG, WebP, AVIF — tối đa 5 MiB.</p>
        <button disabled={busy || !file} onClick={upload}
          className="rounded bg-emerald-600 px-3 py-1 text-sm text-white disabled:opacity-50">
          {busy ? "Đang tải lên..." : "Tải lên"}
        </button>
      </div>
    </div>
  );
}