import { getCategories } from '@/lib/api';
import CategoryCard from '@/components/CategoryCard';
import { ChefHat } from 'lucide-react';

export const revalidate = 3600; // ISR 1 hour as required in SRS

export default async function CategoriesPage() {
  const categories = await getCategories();

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
      <div className="text-center max-w-2xl mx-auto mb-16">
        <div className="inline-flex items-center justify-center w-12 h-12 rounded-2xl bg-orange-100 text-orange-600 mb-4 shadow-sm">
          <ChefHat className="w-6 h-6" />
        </div>
        <h1 className="text-3xl sm:text-4xl font-extrabold text-gray-900 tracking-tight">
          Danh mục ẩm thực
        </h1>
        <p className="mt-4 text-base text-gray-500">
          Khám phá công thức nấu ăn được phân loại khoa học theo bữa ăn, phong cách ẩm thực và khẩu vị yêu thích.
        </p>
      </div>

      {categories.length === 0 ? (
        <div className="text-center py-20 bg-gray-50 rounded-3xl border border-dashed border-gray-200">
          <p className="text-gray-500 font-medium">Chưa có danh mục nào được khởi tạo.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-8">
          {categories.map((cat) => (
            <CategoryCard key={cat.id} category={cat} />
          ))}
        </div>
      )}
    </div>
  );
}
