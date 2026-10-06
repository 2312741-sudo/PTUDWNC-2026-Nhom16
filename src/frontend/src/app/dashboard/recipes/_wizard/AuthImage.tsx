"use client";

import { useEffect, useState } from "react";

const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080/api/v1";
const MEDIA = process.env.NEXT_PUBLIC_MEDIA_URL;

/** Ảnh đi qua proxy D27 (/resources/images) — recipe Draft trả 403 nếu không có Bearer. */
const needsToken = (src: string) =>
  src.startsWith(`${API}/resources/images/`) || (!!MEDIA && src.startsWith(`${MEDIA.replace(/\/$/, "")}/`));

interface Props { src: string; alt: string; className?: string }

/**
 * <img src> không gửi được header Authorization -> ảnh qua proxy được tải bằng fetch kèm Bearer
 * rồi hiển thị qua blob URL (DE_XUAT_05 phương án C). URL khác (/images/..., host khác) dùng <img src> thường.
 */
export default function AuthImage({ src, alt, className }: Props) {
  const secured = needsToken(src);
  // Gắn kết quả với src đã tải để đổi src thì không hiện blob cũ
  const [loaded, setLoaded] = useState<{ src: string; blobUrl: string | null } | null>(null);

  useEffect(() => {
    if (!secured) return;
    let cancelled = false;
    let blobUrl: string | null = null;
    const ctrl = new AbortController();
    const token = localStorage.getItem("accessToken");
    fetch(src, { headers: token ? { Authorization: `Bearer ${token}` } : {}, signal: ctrl.signal })
      .then(res => { if (!res.ok) throw new Error(`HTTP ${res.status}`); return res.blob(); })
      .then(blob => {
        if (cancelled) return;
        blobUrl = URL.createObjectURL(blob);
        setLoaded({ src, blobUrl });
      })
      .catch(() => { if (!cancelled) setLoaded({ src, blobUrl: null }); });
    return () => {
      cancelled = true;
      ctrl.abort();
      if (blobUrl) URL.revokeObjectURL(blobUrl);
    };
  }, [src, secured]);

  // eslint-disable-next-line @next/next/no-img-element
  if (!secured) return <img src={src} alt={alt} className={className} />;

  const current = loaded?.src === src ? loaded : null;
  if (current?.blobUrl) {
    // eslint-disable-next-line @next/next/no-img-element
    return <img src={current.blobUrl} alt={alt} className={className} />;
  }
  return (
    <div className="flex h-32 items-center justify-center bg-gray-100 p-2 text-center text-xs text-gray-500">
      {current
        ? <span>Không tải được ảnh<span className="block truncate">{alt}</span></span>
        : <span aria-live="polite">Đang tải ảnh...</span>}
    </div>
  );
}
