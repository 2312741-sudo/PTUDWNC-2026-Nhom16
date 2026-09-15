import Link from 'next/link';
import { Category } from '@/types/category';
import { ChefHat, ArrowRight } from 'lucide-react';

interface CategoryCardProps {
  category: Category;
}

export default function CategoryCard({ category }: CategoryCardProps) {
  return (
    <Link
      href={`/categories/${category.slug}`}
      className="group relative flex flex-col justify-between bg-white rounded-2xl p-6 border border-gray-100 shadow-sm hover:shadow-xl hover:border-orange-200 transition-all duration-300 hover:-translate-y-1 overflow-hidden"
    >
      <div className="absolute top-0 right-0 w-32 h-32 bg-orange-50 rounded-full blur-2xl -mr-10 -mt-10 group-hover:bg-orange-100 transition-colors pointer-events-none" />

      <div>
        <div className="w-12 h-12 rounded-xl bg-orange-100 text-orange-600 flex items-center justify-center mb-4 group-hover:scale-110 group-hover:bg-orange-600 group-hover:text-white transition-all shadow-sm">
          <ChefHat className="w-6 h-6" />
        </div>

        <h3 className="text-lg font-bold text-gray-900 group-hover:text-orange-600 transition-colors mb-2">
          {category.name}
        </h3>

        <p className="text-sm text-gray-500 line-clamp-2 mb-4 leading-relaxed">
          {category.description || 'Khám phá các công thức nấu ăn đặc sắc trong danh mục này.'}
        </p>
      </div>

      <div className="flex items-center justify-between pt-4 border-t border-gray-50 mt-2">
        <span className="text-xs font-semibold px-2.5 py-1 rounded-full bg-orange-50 text-orange-700">
          {category.recipesCount} công thức
        </span>

        <span className="text-xs font-medium text-orange-600 flex items-center gap-1 group-hover:translate-x-1 transition-transform">
          Xem ngay <ArrowRight className="w-3.5 h-3.5" />
        </span>
      </div>
    </Link>
  );
}
