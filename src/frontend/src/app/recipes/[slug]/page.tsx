import { notFound } from 'next/navigation';
import Link from 'next/link';
import { getRecipeBySlug } from '@/lib/api';
import {
  ArrowLeft,
  ChefHat,
  Clock,
  Users,
  Utensils,
  ListOrdered,
  Timer,
  Flame,
} from 'lucide-react';

export const revalidate = 300; // ISR 5 phút cho công thức đã xuất bản (SRS 3.4)

interface RecipeDetailPageProps {
  params: Promise<{ slug: string }>;
}

function DifficultyBadge({ difficulty }: { difficulty: string }) {
  const styles: Record<string, string> = {
    easy: 'bg-green-100 text-green-800',
    medium: 'bg-amber-100 text-amber-800',
    hard: 'bg-orange-100 text-orange-800',
    expert: 'bg-red-100 text-red-800',
  };
  const cls = styles[difficulty?.toLowerCase()] ?? 'bg-gray-100 text-gray-800';
  return (
    <span className={`px-2.5 py-0.5 rounded-full text-xs font-semibold ${cls}`}>
      {difficulty}
    </span>
  );
}

function formatMinutes(minutes: number) {
  if (minutes <= 0) return 'Không cần nấu';
  if (minutes < 60) return `${minutes} phút`;
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return m === 0 ? `${h} giờ` : `${h} giờ ${m} phút`;
}

export default async function RecipeDetailPage({ params }: RecipeDetailPageProps) {
  const { slug } = await params;
  const recipe = await getRecipeBySlug(slug);

  if (!recipe) {
    notFound();
  }

  const totalTime = recipe.totalTimeMinutes ?? recipe.prepTimeMinutes + recipe.cookTimeMinutes;

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-10">
      <div>
        <Link
          href="/recipes"
          className="inline-flex items-center gap-1.5 text-sm font-semibold text-gray-500 hover:text-orange-600 transition-colors"
        >
          <ArrowLeft className="w-4 h-4" />
          Quay lại danh sách công thức
        </Link>
      </div>

      {/* Tiêu đề và thông số */}
      <header className="space-y-5">
        <div className="flex flex-wrap items-center gap-3">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-orange-50 text-orange-700 text-xs font-semibold">
            <ChefHat className="w-3.5 h-3.5" />
            <span>Công thức</span>
          </div>
          <DifficultyBadge difficulty={recipe.difficulty} />
          {recipe.status !== 'Published' && (
            <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-gray-200 text-gray-700">
              {recipe.status === 'Draft' ? 'Bản nháp' : 'Đã lưu trữ'}
            </span>
          )}
        </div>

        <h1 className="text-3xl sm:text-4xl font-extrabold tracking-tight text-gray-900">
          {recipe.title}
        </h1>

        {recipe.description && (
          <p className="text-gray-600 text-base sm:text-lg leading-relaxed">
            {recipe.description}
          </p>
        )}

        <dl className="grid grid-cols-2 sm:grid-cols-4 gap-4 pt-2">
          <div className="bg-gray-50 rounded-2xl p-4">
            <dt className="flex items-center gap-1.5 text-xs font-semibold text-gray-500">
              <Clock className="w-3.5 h-3.5" /> Chuẩn bị
            </dt>
            <dd className="mt-1 font-bold text-gray-900">{formatMinutes(recipe.prepTimeMinutes)}</dd>
          </div>
          <div className="bg-gray-50 rounded-2xl p-4">
            <dt className="flex items-center gap-1.5 text-xs font-semibold text-gray-500">
              <Flame className="w-3.5 h-3.5" /> Nấu
            </dt>
            <dd className="mt-1 font-bold text-gray-900">{formatMinutes(recipe.cookTimeMinutes)}</dd>
          </div>
          <div className="bg-gray-50 rounded-2xl p-4">
            <dt className="flex items-center gap-1.5 text-xs font-semibold text-gray-500">
              <Timer className="w-3.5 h-3.5" /> Tổng
            </dt>
            <dd className="mt-1 font-bold text-gray-900">{formatMinutes(totalTime)}</dd>
          </div>
          <div className="bg-gray-50 rounded-2xl p-4">
            <dt className="flex items-center gap-1.5 text-xs font-semibold text-gray-500">
              <Users className="w-3.5 h-3.5" /> Khẩu phần
            </dt>
            <dd className="mt-1 font-bold text-gray-900">{recipe.servings} người</dd>
          </div>
        </dl>
      </header>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-10">
        {/* Nguyên liệu */}
        <section className="lg:col-span-1 space-y-4">
          <h2 className="flex items-center gap-2 text-xl font-bold text-gray-900">
            <Utensils className="w-5 h-5 text-orange-600" />
            Nguyên liệu
          </h2>

          {recipe.ingredients.length > 0 ? (
            <ul className="space-y-2">
              {recipe.ingredients.map((ing) => (
                <li
                  key={ing.id}
                  className="flex items-start gap-3 bg-white border border-gray-100 rounded-xl p-3 shadow-sm"
                >
                  <span className="mt-1.5 w-1.5 h-1.5 rounded-full bg-orange-500 shrink-0" />
                  <div className="min-w-0">
                    <p className="font-semibold text-gray-900">
                      {ing.quantity !== null && ing.quantity !== undefined && (
                        <span className="text-orange-700">{ing.quantity} </span>
                      )}
                      {ing.unit && <span className="text-orange-700">{ing.unit} </span>}
                      {ing.name}
                    </p>
                    {ing.notes && (
                      <p className="text-xs text-gray-500 mt-0.5">{ing.notes}</p>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-sm text-gray-500 bg-gray-50 rounded-xl p-4 border border-dashed border-gray-200">
              Công thức này chưa có nguyên liệu nào.
            </p>
          )}

          {/* Dinh dưỡng */}
          {recipe.nutrition && (
            <div className="pt-4 space-y-3">
              <h3 className="text-sm font-bold text-gray-900">Dinh dưỡng mỗi khẩu phần</h3>
              <dl className="space-y-1.5 text-sm">
                {recipe.nutrition.calories != null && (
                  <div className="flex justify-between">
                    <dt className="text-gray-500">Năng lượng</dt>
                    <dd className="font-semibold text-gray-900">{recipe.nutrition.calories} kcal</dd>
                  </div>
                )}
                {recipe.nutrition.protein != null && (
                  <div className="flex justify-between">
                    <dt className="text-gray-500">Đạm</dt>
                    <dd className="font-semibold text-gray-900">{recipe.nutrition.protein} g</dd>
                  </div>
                )}
                {recipe.nutrition.carbohydrates != null && (
                  <div className="flex justify-between">
                    <dt className="text-gray-500">Tinh bột</dt>
                    <dd className="font-semibold text-gray-900">{recipe.nutrition.carbohydrates} g</dd>
                  </div>
                )}
                {recipe.nutrition.fat != null && (
                  <div className="flex justify-between">
                    <dt className="text-gray-500">Chất béo</dt>
                    <dd className="font-semibold text-gray-900">{recipe.nutrition.fat} g</dd>
                  </div>
                )}
                {recipe.nutrition.fiber != null && (
                  <div className="flex justify-between">
                    <dt className="text-gray-500">Chất xơ</dt>
                    <dd className="font-semibold text-gray-900">{recipe.nutrition.fiber} g</dd>
                  </div>
                )}
                {recipe.nutrition.sodium != null && (
                  <div className="flex justify-between">
                    <dt className="text-gray-500">Natri</dt>
                    <dd className="font-semibold text-gray-900">{recipe.nutrition.sodium} mg</dd>
                  </div>
                )}
              </dl>
            </div>
          )}
        </section>

        {/* Các bước thực hiện */}
        <section className="lg:col-span-2 space-y-4">
          <h2 className="flex items-center gap-2 text-xl font-bold text-gray-900">
            <ListOrdered className="w-5 h-5 text-orange-600" />
            Cách làm
          </h2>

          {recipe.steps.length > 0 ? (
            <ol className="space-y-4">
              {recipe.steps.map((step) => (
                <li
                  key={step.id}
                  className="flex gap-4 bg-white border border-gray-100 rounded-2xl p-5 shadow-sm"
                >
                  <span className="shrink-0 w-9 h-9 rounded-full bg-orange-600 text-white font-bold flex items-center justify-center">
                    {step.stepNumber}
                  </span>
                  <div className="min-w-0 space-y-1.5">
                    <h3 className="font-bold text-gray-900">{step.title}</h3>
                    <p className="text-gray-600 leading-relaxed whitespace-pre-line">
                      {step.description}
                    </p>
                    {step.timerMinutes != null && step.timerMinutes > 0 && (
                      <p className="inline-flex items-center gap-1.5 text-xs font-semibold text-orange-700 bg-orange-50 rounded-full px-2.5 py-1">
                        <Timer className="w-3.5 h-3.5" />
                        {formatMinutes(step.timerMinutes)}
                      </p>
                    )}
                  </div>
                </li>
              ))}
            </ol>
          ) : (
            <p className="text-sm text-gray-500 bg-gray-50 rounded-xl p-4 border border-dashed border-gray-200">
              Công thức này chưa có bước thực hiện nào.
            </p>
          )}
        </section>
      </div>
    </div>
  );
}