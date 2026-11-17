'use client';

import { useState, useTransition } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Search, X } from 'lucide-react';

interface RecipeSearchBarProps {
  initialQuery?: string;
  placeholder?: string;
}

export default function RecipeSearchBar({
  initialQuery = '',
  placeholder = 'Tìm kiếm theo tên món ăn, nguyên liệu... (Ví dụ: gà, tôm, phở, bún, pancake...)',
}: RecipeSearchBarProps) {
  const [query, setQuery] = useState(initialQuery);
  const router = useRouter();
  const searchParams = useSearchParams();
  // Trang /recipes được render lại ở server sau mỗi lần đổi ?q=, có thể mất vài giây.
  // Không có trạng thái chờ thì người dùng tưởng bấm "Tìm kiếm" không có tác dụng.
  const [isPending, startTransition] = useTransition();

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    const params = new URLSearchParams(searchParams.toString());
    const trimmed = query.trim();
    if (trimmed) {
      params.set('q', trimmed);
    } else {
      params.delete('q');
    }
    params.set('page', '1'); // Reset về trang 1 khi tìm kiếm mới
    startTransition(() => router.push(`/recipes?${params.toString()}`));
  };

  const handleClear = () => {
    setQuery('');
    const params = new URLSearchParams(searchParams.toString());
    params.delete('q');
    params.set('page', '1');
    startTransition(() => router.push(`/recipes?${params.toString()}`));
  };

  return (
    <form onSubmit={handleSearch} role="search" className="w-full" aria-busy={isPending}>
      <div className="relative flex items-center">
        <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
          <Search className="h-5 w-5 text-emerald-600" />
        </div>
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder={placeholder}
          aria-label="Từ khóa tìm kiếm công thức"
          className="block w-full pl-11 pr-28 py-3.5 bg-white border border-gray-200 rounded-2xl text-sm placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 shadow-sm transition-all"
        />
        <div className="absolute inset-y-0 right-0 pr-2 flex items-center gap-1.5">
          {query && (
            <button
              type="button"
              onClick={handleClear}
              className="p-1.5 text-gray-400 hover:text-gray-600 rounded-full hover:bg-gray-100 transition-colors"
              title="Xóa từ khóa"
            >
              <X className="w-4 h-4" />
            </button>
          )}
          <button
            type="submit"
            disabled={isPending}
            className="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 disabled:opacity-70 disabled:cursor-wait text-white text-xs font-bold rounded-xl shadow-sm hover:shadow transition-all whitespace-nowrap"
          >
            {isPending ? 'Đang tìm…' : 'Tìm kiếm'}
          </button>
        </div>
      </div>
    </form>
  );
}
