'use client';

import { useState } from 'react';
import { ChefHat } from 'lucide-react';
import { CATEGORY_FALLBACK_IMAGES } from '@/lib/recipeImages';

interface RecipeDetailImageProps {
  src: string;
  alt: string;
  categoryName?: string;
}

export default function RecipeDetailImage({ src, alt, categoryName }: RecipeDetailImageProps) {
  const [imgSrc, setImgSrc] = useState(src);
  const [hasError, setHasError] = useState(false);

  const fallback =
    (categoryName && CATEGORY_FALLBACK_IMAGES[categoryName]) ||
    '/images/recipes/com-tam-suon-bi-cha.jpg';

  const handleError = () => {
    if (imgSrc !== fallback) {
      setImgSrc(fallback);
    } else {
      setHasError(true);
    }
  };

  if (hasError) {
    return (
      <div className="w-full h-72 sm:h-96 rounded-3xl bg-emerald-50 border border-emerald-100 flex flex-col items-center justify-center text-emerald-600 shadow-sm">
        <ChefHat className="w-20 h-20 mb-3 opacity-60" />
        <p className="font-semibold text-base">{alt}</p>
      </div>
    );
  }

  return (
    <div className="relative w-full h-72 sm:h-96 rounded-3xl overflow-hidden shadow-lg shadow-orange-50/50 border border-gray-100 bg-gray-50">
      <img
        src={imgSrc}
        alt={alt}
        onError={handleError}
        className="w-full h-full object-cover transition-opacity duration-300"
      />
    </div>
  );
}
