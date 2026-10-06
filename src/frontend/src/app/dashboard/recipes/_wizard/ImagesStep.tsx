"use client";

import { useRef } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  IMAGE_TYPES, RecipeDetail, RecipeImage, deleteImage, imageSrc, isPresignedStale, updateImage, uploadImage,
} from "@/lib/recipe-editor";
import { ImageUploadInput, ImageUploadOutput, imageUploadSchema } from "@/lib/recipe-schemas";
import AuthImage from "./AuthImage";
import { ariaOf, ErrorText } from "./FieldError";
import type { RunFn } from "./RecipeWizard";

interface Props { recipe: RecipeDetail; busy: boolean; run: RunFn; onError: (msg: string) => void }

// onError không còn dùng: lỗi chọn tệp/alt hiện dưới từng ô; lỗi server do run() đưa lên banner
export default function ImagesStep({ recipe, busy, run }: Props) {
  const fileRef = useRef<HTMLInputElement>(null);
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
    const ok = await run(() => uploadImage(recipe.id, file, altText));
    if (ok) { reset({ altText: "" }); if (fileRef.current) fileRef.current.value = ""; }
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
          {images.map((img, idx) => {
            const src = imageSrc(img);
            // B5: URL ký là bearer token trong query string -> không gửi Referer, và phải báo
            // khi hết hạn vì <img> không tự retry (bị 403 image.forbidden).
            const stale = isPresignedStale(img.presignedUrl);
            return (
              <li key={img.id} className={`overflow-hidden rounded border ${img.isPrimary ? "ring-2 ring-emerald-500" : ""}`}>
{/* B5 + D27: `AuthImage` tải qua proxy kèm Bearer (URL ký là bearer token trong query string,
                    `<img src>` không gửi được Authorization nên sẽ 403). `stale` bắt trường hợp
                    URL ký đã hết hạn: <img>/fetch không tự retry, nên cần nút ký lại. */}
                {src
                  ? <AuthImage src={src} alt={img.altText ?? recipe.title} className="h-32 w-full object-cover" />
                  : <div className="flex flex-col items-center justify-center gap-1 bg-gray-100 p-2 text-center text-xs text-gray-500">
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
        <button disabled={busy} onClick={handleSubmit(upload)}
          className="rounded bg-emerald-600 px-3 py-1 text-sm text-white disabled:opacity-50">
          {busy ? "Đang tải lên..." : "Tải lên"}
        </button>
      </div>
    </div>
  );
}