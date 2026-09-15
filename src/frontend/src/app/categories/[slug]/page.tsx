import { notFound } from 'next/navigation';
import Link from 'next/link';
import { getCategoryBySlug } from '@/lib/api';
import { ChefHat, ArrowLeft, Utensils, Clock, Flame } from 'lucide-react';

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

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-12">
      {/* Breadcrumb / Back button */}
      <div>
        <Link
          href="/categories"
          className="inline-flex items-center gap-1.5 text-sm font-semibold text-gray-500 hover:text-orange-600 transition-colors"
        >
          <ArrowLeft className="w-4 h-4" />
          Quay lại tất cả danh mục
        </Link>
      </div>

      {/* Category Banner */}
      <div className="bg-gradient-to-r from-orange-600 to-amber-600 rounded-3xl p-8 sm:p-12 text-white shadow-xl shadow-orange-100 flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
        <div className="space-y-4 max-w-2xl">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/20 backdrop-blur text-xs font-semibold">
            <ChefHat className="w-3.5 h-3.5" />
            <span>Danh mục ẩm thực</span>
          </div>

          <h1 className="text-3xl sm:text-5xl font-extrabold tracking-tight">
            {category.name}
          </h1>

          <p className="text-orange-50 text-base sm:text-lg leading-relaxed">
            {category.description || 'Tổng hợp những món ăn thơm ngon, bổ dưỡng và dễ làm nhất.'}
          </p>

          <div className="pt-2 text-sm font-medium text-orange-100">
            {category.recipesCount} công thức đã xuất bản
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
            Hiển thị công thức mới nhất
          </span>
        </div>

        {/* Recipes Grid Placeholder (will be connected with TV3 recipes in Week 2) */}
        <div className="py-16 text-center bg-gray-50 rounded-2xl border border-dashed border-gray-200 p-8">
          <Utensils className="w-10 h-10 text-orange-400 mx-auto mb-3" />
          <p className="text-gray-600 font-semibold">
            Chưa có công thức nào được xuất bản trong danh mục này.
          </p>
          <p className="text-xs text-gray-400 mt-1 max-w-md mx-auto">
            Các công thức sẽ được cập nhật tự động khi tác giả (Author) hoặc quản trị viên (Admin) xuất bản món ăn.
          </p>
        </div>
      </div>
    </div>
  );
}
