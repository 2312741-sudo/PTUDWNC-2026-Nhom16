import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import Link from 'next/link';
import { getRecipeBySlug } from '@/lib/api';
import { getRecipeImage } from '@/lib/recipeImages';
import { Clock, Users, ChefHat, ArrowLeft, ListChecks, Flame } from 'lucide-react';

export const revalidate = 600; // ISR 10 mins

interface RecipeDetailPageProps {
  params: Promise<{ slug: string }>;
}

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000';

const DIFFICULTY_LABEL: Record<string, string> = {
  Easy: 'Dễ',
  Medium: 'Trung bình',
  Hard: 'Khó',
  Expert: 'Chuyên gia',
};

// eslint-disable-next-line @typescript-eslint/no-unused-vars
function buildRecipeJsonLd(recipe: any) {
  return {
    '@context': 'https://schema.org',
    '@type': 'Recipe',
    name: recipe.title,
    description: recipe.description,
    image: recipe.images?.length ? recipe.images.map((i: any) => i.originalUrl) : undefined,
    author: { '@type': 'Person', name: 'Đầu bếp' },
    datePublished: recipe.publishedAt ? new Date(recipe.publishedAt).toISOString() : undefined,
    prepTime: recipe.prepTimeMinutes ? `PT${recipe.prepTimeMinutes}M` : undefined,
    cookTime: recipe.cookTimeMinutes ? `PT${recipe.cookTimeMinutes}M` : undefined,
    totalTime: recipe.prepTimeMinutes + recipe.cookTimeMinutes
      ? `PT${recipe.prepTimeMinutes + recipe.cookTimeMinutes}M`
      : undefined,
    recipeYield: recipe.servings ? `${recipe.servings} người` : undefined,
    recipeIngredient: recipe.ingredients?.map((i: any) => i.name) ?? [],
    recipeInstructions: (recipe.steps ?? []).map((s: any, idx: number) => ({
      '@type': 'HowToStep',
      position: idx + 1,
      name: s.title,
      text: s.description,
    })),
  };
}

export async function generateMetadata({ params }: RecipeDetailPageProps): Promise<Metadata> {
  const { slug } = await params;
  const { data: recipe } = await getRecipeBySlug(slug);
  if (!recipe) return {};

  const url = `${SITE_URL}/recipes/${encodeURIComponent(slug)}`;
  return {
    title: recipe.title,
    description: recipe.description,
    alternates: { canonical: url },
    openGraph: {
      title: recipe.title,
      description: recipe.description,
      type: 'article',
      url,
      images: recipe.images?.length ? [{ url: recipe.images[0].originalUrl }] : [],
      publishedTime: recipe.publishedAt ? new Date(recipe.publishedAt).toISOString() : undefined,
    },
  };
}

export default async function RecipeDetailPage({ params }: RecipeDetailPageProps) {
  const { slug } = await params;
  const { success, data: recipe, error } = await getRecipeBySlug(slug);

  if (!success || !recipe) {
    notFound();
  }

  const imageUrl = recipe.images?.find((i: any) => i.isPrimary)?.originalUrl || (recipe.images?.[0]?.originalUrl ?? null);
  const jsonLd = buildRecipeJsonLd(recipe);

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />

      <div>
        <Link
          href="/recipes"
          className="inline-flex items-center gap-1.5 text-sm font-semibold text-gray-500 hover:text-emerald-700 transition-colors"
        >
          <ArrowLeft className="w-4 h-4" />
          Quay lại tất cả công thức
        </Link>
      </div>

      <article>
        <div className="space-y-4">
          <div className="flex flex-wrap items-center gap-3">
            <span className="px-3 py-1 rounded-full text-xs font-bold bg-white shadow-sm border border-gray-100 text-gray-800">
              {recipe.categoryName ?? 'Ẩm thực'}
            </span>
            <span className={`px-2.5 py-0.5 rounded-full text-xs font-semibold ${
              recipe.difficulty?.toLowerCase() === 'easy'
                ? 'bg-emerald-100 text-emerald-800'
                : recipe.difficulty?.toLowerCase() === 'medium'
                  ? 'bg-amber-100 text-amber-800'
                  : recipe.difficulty?.toLowerCase() === 'hard'
                    ? 'bg-amber-200 text-amber-900'
                    : 'bg-rose-100 text-rose-800'
            }`}>
              {DIFFICULTY_LABEL[recipe.difficulty] ?? recipe.difficulty}
            </span>
          </div>

          <h1 className="text-3xl sm:text-5xl font-extrabold tracking-tight text-gray-900">
            {recipe.title}
          </h1>

          <p className="text-gray-600 text-base sm:text-lg leading-relaxed max-w-3xl">
            {recipe.description}
          </p>

          <div className="flex flex-wrap items-center gap-4 text-sm text-gray-500 pt-1">
            <span className="flex items-center gap-1.5 font-medium text-emerald-800">
              <Clock className="w-4 h-4 text-emerald-600" />
              {recipe.totalTimeMinutes ?? recipe.prepTimeMinutes + recipe.cookTimeMinutes} phút
            </span>
            <span className="flex items-center gap-1.5">
              <Users className="w-4 h-4 text-gray-400" />
              {recipe.servings} người
            </span>
            {recipe.publishedAt && (
              <span className="text-xs text-gray-400">
                Xuất bản: {new Date(recipe.publishedAt).toLocaleDateString('vi-VN')}
              </span>
            )}
          </div>
        </div>

        {imageUrl && (
          <div className="mt-8">
            <img
              src={imageUrl}
              alt={recipe.title}
              className="w-full h-72 sm:h-96 object-cover rounded-3xl shadow-lg shadow-emerald-100 border border-gray-100"
            />
          </div>
        )}

        {recipe.nutrition && (
          <div className="mt-8 bg-emerald-50/70 rounded-2xl p-6 border border-emerald-100">
            <h2 className="text-lg font-bold text-gray-900 mb-4 flex items-center gap-2">
              <Flame className="w-5 h-5 text-emerald-700" />
              Dinh dưỡng mỗi khẩu phần
            </h2>
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-4 text-sm">
              {recipe.nutrition.calories != null && (
                <div><span className="font-semibold text-gray-900">{recipe.nutrition.calories}</span> kcal</div>
              )}
              {recipe.nutrition.protein != null && (
                <div><span className="font-semibold text-gray-900">{recipe.nutrition.protein}g</span> protein</div>
              )}
              {recipe.nutrition.carbohydrates != null && (
                <div><span className="font-semibold text-gray-900">{recipe.nutrition.carbohydrates}g</span> carbs</div>
              )}
              {recipe.nutrition.fat != null && (
                <div><span className="font-semibold text-gray-900">{recipe.nutrition.fat}g</span> chất béo</div>
              )}
              {recipe.nutrition.fiber != null && (
                <div><span className="font-semibold text-gray-900">{recipe.nutrition.fiber}g</span> chất xơ</div>
              )}
              {recipe.nutrition.sodium != null && (
                <div><span className="font-semibold text-gray-900">{recipe.nutrition.sodium}mg</span> natri</div>
              )}
            </div>
          </div>
        )}

        <div className="mt-10 grid grid-cols-1 md:grid-cols-3 gap-8">
          <div className="md:col-span-1 bg-white rounded-2xl border border-gray-100 shadow-sm p-6 h-fit">
            <h2 className="text-lg font-bold text-gray-900 mb-4 flex items-center gap-2">
              <ListChecks className="w-5 h-5 text-emerald-700" />
              Nguyên liệu
            </h2>
            <ul className="space-y-2.5">
              {(recipe.ingredients ?? []).map((ing: any) => (
                <li key={ing.id} className="flex items-start justify-between gap-3 text-sm">
                  <span className="text-gray-800">{ing.name}</span>
                  <span className="text-gray-500 whitespace-nowrap">
                    {ing.quantity != null ? `${ing.quantity} ${ing.unit ?? ''}`.trim() : ing.unit ?? ''}
                  </span>
                </li>
              ))}
            </ul>
          </div>

          <div className="md:col-span-2 space-y-6">
            <h2 className="text-xl font-bold text-gray-900 flex items-center gap-2">
              <ChefHat className="w-6 h-6 text-emerald-700" />
              Các bước thực hiện
            </h2>
            {(recipe.steps ?? []).map((step: any, idx: number) => (
              <div key={step.id} className="flex gap-4 bg-white rounded-2xl border border-gray-100 shadow-sm p-5">
                <div className="flex-shrink-0 w-9 h-9 rounded-full bg-emerald-600 text-white font-bold flex items-center justify-center">
                  {idx + 1}
                </div>
                <div className="space-y-1">
                  <h3 className="font-semibold text-gray-900">{step.title}</h3>
                  <p className="text-sm text-gray-600 leading-relaxed">{step.description}</p>
                  {step.timerMinutes ? (
                    <p className="text-xs font-medium text-amber-700 flex items-center gap-1">
                      <Clock className="w-3.5 h-3.5" /> {step.timerMinutes} phút
                    </p>
                  ) : null}
                </div>
              </div>
            ))}
          </div>
        </div>
      </article>
    </div>
  );
}