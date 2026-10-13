"use client";

import { useRef, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  IMAGE_TYPES, RecipeDetail, RecipeImage, deleteImage, imageSrc, updateImage, uploadImage,
} from "@/lib/recipe-editor";
import { ImageUploadInput, ImageUploadOutput, imageUploadSchema } from "@/lib/recipe-schemas";
import AuthImage from "./AuthImage";
import { ariaOf, ErrorText } from "./FieldError";
import type { RunFn } from "./RecipeWizard";

interface Props { recipe: RecipeDetail; busy: boolean; run: RunFn; onError: (msg: string) => void }

// onError không còn dùng: lỗi chọn tệp/alt hiện dưới từng ô; lỗi server do run() đưa lên banner
export default function ImagesStep({ recipe, busy, run }: Props) {
  const fileRef = useRef<HTMLInputElement>(null);
  // D27: % tiến trình tải lên (XMLHttpRequest.upload.onprogress) — một tệp một lúc.
  const [uploadPct, setUploadPct] = useState<number | null>(null);
  // Kiểm tra sớm ở client (MIME, 5 MiB, alt); backend vẫn kiểm tra magic bytes (không tin Content-Type)
  const { register, control, handleSubmit, reset, formState: { errors } } = useForm<ImageUploadInput, unknown, ImageUploadOutput>({
    resolver: zodResolver(imageUploadSchema),
    mode: "onChange",
    defaultValues: { altText: "" },
  });
  const images: RecipeImage[] = [...(recipe.images ?? [])].sort((a, b) =>
    Number(b.isPrimary) - Number(a.isPrimary) || a.orderIndex - b.orderIndex);

  // file/altText là output đã parse (alt trim, rỗng -> null)
  async function upload({ file, altText }: ImageUploadOutput) {
    setUploadPct(0);
    const ok = await run(() => uploadImage(recipe.id, file, altText, (p) => setUploadPct(p)));
    setUploadPct(null);
    if (ok) { reset({ altText: "" }); if (fileRef.current) fileRef.current.value = ""; }
  }

  const setPrimary = (img: RecipeImage) =>
    run(() => updateImage(recipe.id, img, { isPrimary: true }),
      d => ({ ...d, images: (d.images ?? []).map(i => ({ ...i, isPrimary: i.id === img.id })) }));

  function remove(img: RecipeImage) {
    if (!confirm("Xoá ảnh này?")) return;
    run(() => deleteImage(recipe.id, img.id), d => ({ ...d, images: (d.images ?? []).filter(i => i.id !== img.id) }));
  }

  return (
    <div className="space-y-4">
      {images.length === 0 ? (
        <p className="text-gray-500">Chưa có ảnh. Ảnh đầu tiên sẽ tự thành ảnh chính.</p>
      ) : (
        <ul className="grid grid-cols-2 gap-3 md:grid-cols-3">
          {images.map((img, idx) => {
            const src = imageSrc(img);
            return (
              <li key={img.id} className={`overflow-hidden rounded border ${img.isPrimary ? "ring-2 ring-emerald-500" : ""}`}>
{/* D27: `AuthImage` tải qua proxy kèm Bearer (ảnh Draft qua /resources/images trả 403 nếu không
                    có Authorization; `<img src>` không gửi được header nên 403). key/`/images/...` dùng img thường. */}
                {src
                  ? <AuthImage src={src} alt={img.altText ?? recipe.title} className="h-32 w-full object-cover" />
                  : <div className="flex flex-col items-center justify-center gap-1 bg-gray-100 p-2 text-center text-xs text-gray-500">
                      Ảnh chưa có đường dẫn
                    </div>}
                <div className="space-y-1 p-2 text-xs">
                  {img.isPrimary && <span className="rounded bg-emerald-100 px-1 text-emerald-700">Ảnh chính</span>}
                  {img.altText && <p className="truncate text-gray-600">{img.altText}</p>}
                  <div className="flex gap-2">
                    {!img.isPrimary && (
                      <button disabled={busy} onClick={() => setPrimary(img)} aria-label={`Đặt làm ảnh chính (ảnh ${idx + 1})`} className="text-blue-600 disabled:opacity-40">Đặt làm ảnh chính</button>
                    )}
                    <button disabled={busy} onClick={() => remove(img)} aria-label={`Xoá ảnh ${idx + 1}${img.altText ? `: ${img.altText}` : ""}`} className="text-red-600 disabled:opacity-40">Xoá</button>
                  </div>
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <div className="space-y-2 rounded border p-3">
        <p className="text-sm font-semibold">Tải ảnh lên</p>
        <div>
          {/* input file không điều khiển được giá trị -> Controller chỉ nhận File qua onChange */}
          <Controller control={control} name="file" render={({ field }) => (
            <input type="file" accept={IMAGE_TYPES.join(",")} aria-label="Chọn ảnh" className="block text-sm"
              name={field.name} onBlur={field.onBlur}
              ref={el => { field.ref(el); fileRef.current = el; }}
              onChange={e => field.onChange(e.target.files?.[0])}
              {...ariaOf("err-image-file", errors.file)} />
          )} />
          <ErrorText id="err-image-file" error={errors.file} />
        </div>
        <div>
          <input aria-label="Mô tả ảnh" className="w-full rounded border p-2 text-sm" placeholder="Mô tả ảnh (alt, không bắt buộc)" maxLength={200}
            {...register("altText")} {...ariaOf("err-image-alt", errors.altText)} />
          <ErrorText id="err-image-alt" error={errors.altText} />
        </div>
        <p className="text-xs text-gray-500">JPEG, PNG, WebP, AVIF — tối đa 5 MiB.</p>
        {uploadPct !== null && (
          <div className="flex items-center gap-2" role="progressbar" aria-label="Tiến trình tải ảnh lên"
            aria-valuenow={uploadPct} aria-valuemin={0} aria-valuemax={100}>
            <div className="h-2 flex-1 overflow-hidden rounded bg-stone-200">
              <div className="h-full bg-emerald-600 transition-[width]" style={{ width: `${uploadPct}%` }} />
            </div>
            <span className="w-11 shrink-0 text-right text-xs tabular-nums text-stone-600">{uploadPct}%</span>
          </div>
        )}
        <button disabled={busy} onClick={handleSubmit(upload)}
          className="rounded bg-emerald-600 px-3 py-1 text-sm text-white disabled:opacity-50">
          {busy ? "Đang tải lên..." : "Tải lên"}
        </button>
      </div>
    </div>
  );
}