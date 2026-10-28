'use client';

import { useState } from 'react';
import {
  Utensils,
  Play,
  Printer,
  Minus,
  Plus,
  Check,
  RotateCcw,
  Sparkles,
} from 'lucide-react';
import { RecipeDetail, RecipeIngredient } from '@/types/recipe';
import FavoriteButton from '@/components/FavoriteButton';
import CookingModeModal from '@/components/CookingModeModal';

interface RecipeInteractiveSectionProps {
  recipe: RecipeDetail;
  categoryName?: string;
}

export default function RecipeInteractiveSection({
  recipe,
  categoryName,
}: RecipeInteractiveSectionProps) {
  const baseServings = recipe.servings || 4;
  const [servings, setServings] = useState<number>(baseServings);
  const [checkedIngredients, setCheckedIngredients] = useState<string[]>([]);
  const [isCookingModeOpen, setIsCookingModeOpen] = useState(false);

  const ratio = servings / (baseServings || 1);

  const scaleQuantity = (qty?: number | null): string => {
    if (qty === null || qty === undefined) return '';
    const val = qty * ratio;
    // Format to max 2 decimals, strip trailing zeroes
    return parseFloat(val.toFixed(2)).toString();
  };

  const toggleIngredientChecked = (id: string) => {
    setCheckedIngredients((prev) =>
      prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id]
    );
  };

  const handlePrint = () => {
    if (typeof window !== 'undefined') {
      window.print();
    }
  };

  const hasNutrition =
    !!recipe.nutrition &&
    typeof recipe.nutrition === 'object' &&
    Object.values(recipe.nutrition).some((v) => v !== null && v !== undefined);

  return (
    <div className="space-y-6">
      {/* Top Action Bar */}
      <div className="bg-gradient-to-r from-amber-50 to-orange-50 border border-amber-200/70 rounded-2xl p-4 sm:p-5 flex flex-wrap items-center justify-between gap-4 shadow-sm print:hidden">
        <div className="space-y-1">
          <span className="inline-flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-amber-800">
            <Sparkles className="w-3.5 h-3.5 text-amber-600" />
            Trải nghiệm nấu nướng
          </span>
          <p className="text-sm font-semibold text-gray-800">
            Sẵn sàng nấu? Bật Chế độ Nấu ăn để theo dõi từng bước cùng đồng hồ bấm giờ!
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2.5">
          <button
            onClick={() => setIsCookingModeOpen(true)}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl font-bold text-sm bg-gradient-to-r from-amber-600 to-orange-600 hover:from-amber-700 hover:to-orange-700 text-white shadow-md shadow-orange-500/20 transition-all hover:scale-[1.02] active:scale-[0.98]"
          >
            <Play className="w-4 h-4 fill-white" />
            Bắt đầu nấu
          </button>

          <FavoriteButton
            variant="badge"
            recipe={{
              id: recipe.id,
              slug: recipe.slug,
              title: recipe.title,
              description: recipe.description,
              categoryName,
              cookTimeMinutes: recipe.cookTimeMinutes,
              servings: recipe.servings,
              difficulty: recipe.difficulty,
            }}
          />

          <button
            onClick={handlePrint}
            className="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-semibold bg-white border border-gray-200 text-gray-700 hover:bg-gray-50 transition-colors shadow-sm"
            title="In công thức này"
          >
            <Printer className="w-4 h-4 text-gray-500" />
            <span>In</span>
          </button>
        </div>
      </div>

      {/* Ingredients & Servings Scaler */}
      <div className="space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-3 pb-2 border-b border-gray-100">
          <h2 className="flex items-center gap-2 text-xl font-bold text-gray-900">
            <Utensils className="w-5 h-5 text-orange-600" />
            Nguyên liệu
          </h2>

          {/* Interactive Servings Controller */}
          <div className="flex items-center gap-2 bg-white border border-gray-200 rounded-xl px-3 py-1.5 shadow-sm print:hidden">
            <span className="text-xs text-gray-500 font-medium">Khẩu phần:</span>
            <button
              onClick={() => setServings((prev) => Math.max(1, prev - 1))}
              disabled={servings <= 1}
              className="p-1 rounded-lg hover:bg-gray-100 text-gray-600 disabled:opacity-30 disabled:pointer-events-none transition-colors"
              aria-label="Giảm khẩu phần"
            >
              <Minus className="w-3.5 h-3.5" />
            </button>
            <span className="font-bold text-sm text-gray-900 min-w-[2rem] text-center">
              {servings}
            </span>
            <button
              onClick={() => setServings((prev) => Math.min(20, prev + 1))}
              disabled={servings >= 20}
              className="p-1 rounded-lg hover:bg-gray-100 text-gray-600 disabled:opacity-30 disabled:pointer-events-none transition-colors"
              aria-label="Tăng khẩu phần"
            >
              <Plus className="w-3.5 h-3.5" />
            </button>
            {servings !== baseServings && (
              <button
                onClick={() => setServings(baseServings)}
                className="p-1 rounded-lg hover:bg-gray-100 text-orange-600 transition-colors ml-1"
                title="Khôi phục khẩu phần ban đầu"
              >
                <RotateCcw className="w-3.5 h-3.5" />
              </button>
            )}
          </div>
        </div>

        {/* Ingredients list with check-off functionality */}
        {(recipe.ingredients?.length ?? 0) > 0 ? (
          <ul className="space-y-2">
            {recipe.ingredients.map((ing) => {
              const isChecked = checkedIngredients.includes(ing.id);
              const scaledQty = scaleQuantity(ing.quantity);

              return (
                <li
                  key={ing.id}
                  onClick={() => toggleIngredientChecked(ing.id)}
                  className={`flex items-start gap-3 p-3 rounded-xl border transition-all cursor-pointer select-none ${
                    isChecked
                      ? 'bg-gray-50 border-gray-200 text-gray-400 opacity-75'
                      : 'bg-white border-gray-100 hover:border-amber-200 shadow-sm text-gray-900'
                  }`}
                >
                  <div
                    className={`mt-0.5 w-5 h-5 rounded-lg border flex items-center justify-center shrink-0 transition-colors ${
                      isChecked
                        ? 'bg-emerald-500 border-emerald-500 text-white'
                        : 'border-gray-300 bg-white hover:border-amber-500'
                    }`}
                  >
                    {isChecked && <Check className="w-3.5 h-3.5 stroke-[3]" />}
                  </div>

                  <div className="min-w-0 flex-1">
                    <p
                      className={`font-semibold text-sm sm:text-base ${
                        isChecked ? 'line-through text-gray-400' : 'text-gray-900'
                      }`}
                    >
                      {scaledQty && (
                        <span className={isChecked ? 'text-gray-400' : 'text-orange-700 font-bold'}>
                          {scaledQty}{' '}
                        </span>
                      )}
                      {ing.unit && (
                        <span className={isChecked ? 'text-gray-400' : 'text-orange-700 font-medium'}>
                          {ing.unit}{' '}
                        </span>
                      )}
                      <span>{ing.name}</span>
                    </p>
                    {ing.notes && (
                      <p className="text-xs text-gray-500 mt-0.5">{ing.notes}</p>
                    )}
                  </div>
                </li>
              );
            })}
          </ul>
        ) : (
          <p className="text-sm text-gray-500 bg-gray-50 rounded-xl p-4 border border-dashed border-gray-200">
            Công thức này chưa có nguyên liệu nào.
          </p>
        )}

        {/* Scaled Nutrition facts */}
        {hasNutrition && recipe.nutrition && (
          <div className="pt-4 space-y-3">
            <div className="flex items-center justify-between">
              <h3 className="text-sm font-bold text-gray-900">Dinh dưỡng ({servings} người)</h3>
              {servings !== baseServings && (
                <span className="text-[11px] font-medium text-amber-700 bg-amber-50 px-2 py-0.5 rounded-full">
                  Đã tự động tính theo {servings} phần
                </span>
              )}
            </div>
            <dl className="grid grid-cols-2 gap-2 text-xs sm:text-sm bg-gray-50/70 p-3.5 rounded-xl border border-gray-100">
              {recipe.nutrition.calories != null && (
                <div className="flex justify-between py-1 border-b border-gray-100">
                  <dt className="text-gray-500">Năng lượng</dt>
                  <dd className="font-semibold text-gray-900">
                    {Math.round(recipe.nutrition.calories * ratio)} kcal
                  </dd>
                </div>
              )}
              {recipe.nutrition.protein != null && (
                <div className="flex justify-between py-1 border-b border-gray-100">
                  <dt className="text-gray-500">Đạm</dt>
                  <dd className="font-semibold text-gray-900">
                    {Math.round(recipe.nutrition.protein * ratio * 10) / 10} g
                  </dd>
                </div>
              )}
              {recipe.nutrition.carbohydrates != null && (
                <div className="flex justify-between py-1 border-b border-gray-100">
                  <dt className="text-gray-500">Tinh bột</dt>
                  <dd className="font-semibold text-gray-900">
                    {Math.round(recipe.nutrition.carbohydrates * ratio * 10) / 10} g
                  </dd>
                </div>
              )}
              {recipe.nutrition.fat != null && (
                <div className="flex justify-between py-1 border-b border-gray-100">
                  <dt className="text-gray-500">Chất béo</dt>
                  <dd className="font-semibold text-gray-900">
                    {Math.round(recipe.nutrition.fat * ratio * 10) / 10} g
                  </dd>
                </div>
              )}
            </dl>
          </div>
        )}
      </div>

      {/* Cooking Mode Modal */}
      <CookingModeModal
        isOpen={isCookingModeOpen}
        onClose={() => setIsCookingModeOpen(false)}
        title={recipe.title}
        steps={recipe.steps ?? []}
        ingredients={recipe.ingredients ?? []}
        servings={servings}
      />
    </div>
  );
}
