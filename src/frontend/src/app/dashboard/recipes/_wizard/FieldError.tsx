import type { FieldError } from "react-hook-form";

/** Thông báo lỗi dưới từng trường; id dùng cho aria-describedby của ô nhập */
export function ErrorText({ id, error }: { id: string; error?: FieldError }) {
  return error?.message ? <p id={id} className="mt-1 text-xs text-red-600">{error.message}</p> : null;
}

export const ariaOf = (id: string, error?: FieldError) =>
  ({ "aria-invalid": error ? true : undefined, "aria-describedby": error ? id : undefined });
