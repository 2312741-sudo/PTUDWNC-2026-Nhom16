import { notFound } from 'next/navigation';
import Link from 'next/link';
import { getCategoryBySlug, getRecipes } from '@/lib/api';
import RecipeCard from '@/components/RecipeCard';
import { ChefHat, ArrowLeft, Utensils } from 'lucide-react';

export const revalidate = 600; // ISR 10 mins as per SRS

interface CategoryDetailPageProps {
  params: Promise<{ slug: string }>;
}

export default async function CategoryDetailPage({ params }: CategoryDetailPageProps) {
  const { slug } = await params;
  const category = await getCategoryBySlug(slug);

  if (!category) {
    notFound();
  }

  const recipesResult = await getRecipes({ categoryId: category.id, pageSize: 12 });
  const recipes = recipesResult.data;

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-12">
      {/* Breadcrumb / Back button */}
      <div>
        <Link
          href="/categories"
          className="inline-flex items-center gap-1.5 text-sm font-semibold text-gray-500 hover:text-emerald-700 transition-colors"
        >
          <ArrowLeft className="w-4 h-4" />
          Quay lại tất cả danh mục
        </Link>
      </div>

      {/* Category Banner */}
      <div className="bg-gradient-to-r from-emerald-700 via-teal-700 to-amber-700 rounded-3xl p-8 sm:p-12 text-white shadow-xl shadow-emerald-100 flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
        <div className="space-y-4 max-w-2xl">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/20 backdrop-blur text-xs font-semibold">
            <ChefHat className="w-3.5 h-3.5 text-amber-200" />
            <span>Danh mục ẩm thực</span>
          </div>

          <h1 className="text-3xl sm:text-5xl font-extrabold tracking-tight">
            {category.name}
          </h1>

          <p className="text-emerald-50 text-base sm:text-lg leading-relaxed">
            {category.description || 'Tổng hợp những món ăn thơm ngon, bổ dưỡng và dễ làm nhất.'}
          </p>

          <div className="pt-2 text-sm font-medium text-emerald-100">
            {recipes.length > 0 ? `${recipesResult.meta.total} công thức đã xuất bản` : `${category.recipesCount} công thức đã xuất bản`}
          </div>
        </div>
      </div>

      {/* Recipes in Category section */}
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold text-gray-900">
            Các món ăn trong danh mục
          </h2>
          <span className="text-sm text-gray-500">
            {recipes.length > 0 ? `Hiển thị ${recipes.length} món ăn` : 'Hiển thị công thức mới nhất'}
          </span>
        </div>

        {recipes.length > 0 ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            {recipes.map((recipe) => (
              <RecipeCard key={recipe.id} recipe={recipe} />
            ))}
          </div>
        ) : (
          <div className="py-16 text-center bg-gray-50 rounded-2xl border border-dashed border-gray-200 p-8">
            <Utensils className="w-10 h-10 text-emerald-400 mx-auto mb-3" />
            <p className="text-gray-600 font-semibold">
              Chưa có công thức nào được xuất bản trong danh mục này.
            </p>
            <p className="text-xs text-gray-400 mt-1 max-w-md mx-auto">
              Các công thức sẽ được cập nhật tự động khi tác giả (Author) hoặc quản trị viên (Admin) xuất bản món ăn.
            </p>
            <div className="mt-4">
              <Link
                href="/recipes"
                className="inline-block px-4 py-2 rounded-xl bg-emerald-600 text-white text-xs font-semibold hover:bg-emerald-700 transition-colors shadow-sm"
              >
                Khám phá tất cả món ăn
              </Link>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
