import Link from 'next/link';
import { RecipeSummary } from '@/types/recipe';
import { Clock, Users, ChefHat } from 'lucide-react';

interface RecipeCardProps {
  recipe: RecipeSummary;
}

export default function RecipeCard({ recipe }: RecipeCardProps) {
  const getDifficultyBadge = (difficulty: string) => {
    switch (difficulty?.toLowerCase()) {
      case 'easy':
        return <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800">Dễ</span>;
      case 'medium':
        return <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-100 text-amber-800">Trung bình</span>;
      case 'hard':
        return <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-200 text-amber-900">Khó</span>;
      case 'expert':
        return <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-rose-100 text-rose-800">Chuyên gia</span>;
      default:
        return <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-gray-100 text-gray-800">{difficulty}</span>;
    }
  };

  return (
    <div className="group bg-white rounded-2xl border border-gray-100 shadow-sm hover:shadow-xl hover:border-emerald-200 transition-all duration-300 flex flex-col overflow-hidden hover:-translate-y-1">
      {/* Image Container */}
      <div className="relative w-full h-48 bg-gradient-to-tr from-emerald-50 to-amber-50 flex items-center justify-center overflow-hidden">
        {recipe.primaryImageUrl ? (
          <img
            src={recipe.primaryImageUrl}
            alt={recipe.title}
            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
          />
        ) : (
          <div className="text-emerald-300 group-hover:scale-110 transition-transform">
            <ChefHat className="w-16 h-16" />
          </div>
        )}

        <div className="absolute top-3 left-3">
          <span className="px-3 py-1 rounded-full text-xs font-bold bg-white/95 backdrop-blur text-gray-800 shadow-sm border border-gray-100">
            {recipe.categoryName}
          </span>
        </div>

        <div className="absolute top-3 right-3">
          {getDifficultyBadge(recipe.difficulty)}
        </div>
      </div>

      {/* Content */}
      <div className="p-5 flex-1 flex flex-col justify-between space-y-4">
        <div>
          <h3 className="font-bold text-gray-900 text-lg group-hover:text-emerald-700 transition-colors line-clamp-1 mb-1">
            {recipe.title}
          </h3>
          <p className="text-xs text-gray-500 line-clamp-2 leading-relaxed">
            {recipe.description}
          </p>
        </div>

        {/* Meta info */}
        <div className="pt-3 border-t border-gray-100 flex items-center justify-between text-xs text-gray-500">
          <div className="flex items-center gap-3">
            <span className="flex items-center gap-1 font-medium text-emerald-800" title="Thời gian nấu">
              <Clock className="w-3.5 h-3.5 text-emerald-600" />
              {recipe.cookTimeMinutes} phút
            </span>
            <span className="flex items-center gap-1" title="Khẩu phần">
              <Users className="w-3.5 h-3.5 text-gray-400" />
              {recipe.servings} người
            </span>
          </div>

          <span className="text-[11px] font-medium text-gray-400 truncate max-w-[100px]">
            {recipe.authorDisplayName}
          </span>
        </div>
      </div>
    </div>
  );
}
