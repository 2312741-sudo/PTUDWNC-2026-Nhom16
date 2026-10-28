'use client';

import { useFavorites } from '@/lib/favorites';
import { Heart } from 'lucide-react';
import { useState } from 'react';

interface FavoriteButtonProps {
  recipe: {
    id?: string;
    slug: string;
    title: string;
    description?: string;
    categoryName?: string;
    cookTimeMinutes?: number;
    servings?: number;
    difficulty?: string;
    imageUrl?: string;
  };
  variant?: 'icon' | 'badge' | 'full';
  className?: string;
}

export default function FavoriteButton({
  recipe,
  variant = 'icon',
  className = '',
}: FavoriteButtonProps) {
  const { has, toggle, isLoaded } = useFavorites();
  const [isBouncing, setIsBouncing] = useState(false);

  const isFav = isLoaded && has(recipe.slug);

  const handleClick = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsBouncing(true);
    toggle(recipe);
    setTimeout(() => setIsBouncing(false), 400);
  };

  if (variant === 'badge' || variant === 'full') {
    return (
      <button
        onClick={handleClick}
        type="button"
        aria-label={isFav ? 'Bỏ lưu khỏi danh sách yêu thích' : 'Lưu vào danh sách yêu thích'}
        aria-pressed={isFav}
        className={`inline-flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-semibold transition-all duration-200 border ${
          isFav
            ? 'bg-rose-50 border-rose-200 text-rose-700 shadow-sm'
            : 'bg-white border-gray-200 text-gray-700 hover:bg-gray-50 hover:border-rose-200 hover:text-rose-600'
        } ${isBouncing ? 'scale-105' : 'scale-100'} ${className}`}
      >
        <Heart
          className={`w-4 h-4 transition-colors ${
            isFav ? 'fill-rose-500 text-rose-500' : 'text-gray-400'
          }`}
        />
        <span>{isFav ? 'Đã lưu yêu thích' : 'Lưu công thức'}</span>
      </button>
    );
  }

  return (
    <button
      onClick={handleClick}
      type="button"
      aria-label={isFav ? 'Bỏ lưu khỏi danh sách yêu thích' : 'Lưu vào danh sách yêu thích'}
      aria-pressed={isFav}
      className={`p-2 rounded-full backdrop-blur-md transition-all duration-200 shadow-sm ${
        isFav
          ? 'bg-rose-500/90 text-white hover:bg-rose-600'
          : 'bg-white/90 text-gray-600 hover:text-rose-600 hover:bg-white'
      } ${isBouncing ? 'scale-125' : 'scale-100'} ${className}`}
      title={isFav ? 'Bỏ lưu yêu thích' : 'Lưu vào yêu thích'}
    >
      <Heart
        className={`w-4 h-4 transition-colors ${
          isFav ? 'fill-white stroke-white' : 'stroke-current'
        }`}
      />
    </button>
  );
}
