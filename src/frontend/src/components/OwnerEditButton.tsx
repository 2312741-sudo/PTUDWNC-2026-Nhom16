'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { Pencil } from 'lucide-react';

interface Claims { sub?: string; role?: string | string[]; exp?: number }

// Đọc claim từ JWT trong localStorage (chỉ để quyết định HIỂN THỊ nút; quyền thật do backend kiểm tra)
function readClaims(): Claims | null {
  const token = localStorage.getItem('accessToken');
  if (!token) return null;
  try {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const json = new TextDecoder().decode(Uint8Array.from(atob(payload), (c) => c.charCodeAt(0)));
    return JSON.parse(json) as Claims;
  } catch {
    return null;
  }
}

interface Props { recipeId?: string; slug: string; authorId?: string }

export default function OwnerEditButton({ recipeId, slug, authorId }: Props) {
  const [canEdit, setCanEdit] = useState(false);

  useEffect(() => {
    const c = readClaims();
    if (!c || !recipeId || (c.exp && c.exp * 1000 < Date.now())) return;
    const roles = Array.isArray(c.role) ? c.role : c.role ? [c.role] : [];
    setCanEdit(roles.includes('Admin') || (!!authorId && c.sub === authorId));
  }, [recipeId, authorId]);

  if (!canEdit) return null;
  return (
    <Link
      href={`/dashboard/recipes/${recipeId}/edit?slug=${encodeURIComponent(slug)}`}
      className="inline-flex items-center gap-1.5 rounded-xl bg-orange-600 px-3 py-1.5 text-sm font-semibold text-white hover:bg-orange-700"
    >
      <Pencil className="w-4 h-4" />
      Sửa công thức
    </Link>
  );
}