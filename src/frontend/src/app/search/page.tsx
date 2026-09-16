import Link from 'next/link';
import { searchRecipes } from '@/lib/api';
import RecipeCard from '@/components/RecipeCard';
import { Search, Sparkles, AlertCircle } from 'lucide-react';

interface SearchPageProps {
  searchParams: Promise<{
    q?: string;
    page?: string;
  }>;
}

export default async function SearchPage({ searchParams }: SearchPageProps) {
  const params = await searchParams;
  const q = params.q?.trim() || '';
  const page = params.page ? parseInt(params.page, 10) : 1;

  let result = {
    data: [] as import('@/types/recipe').RecipeSummary[],
    meta: { page: 1, pageSize: 12, total: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false },
  };

  let error: string | null = null;

  if (q.length > 0 && q.length < 2) {
    error = 'Từ khóa tìm kiếm phải có từ 2 ký tự trở lên.';
  } else if (q.length >= 2) {
    result = await searchRecipes(q, { page, pageSize: 12 });
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-10">
      {/* Search Header */}
      <div className="max-w-3xl mx-auto text-center space-y-4">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-orange-100 text-orange-800 text-xs font-semibold">
          <Sparkles className="w-3.5 h-3.5 text-orange-600" />
          <span>Tìm kiếm thông minh không dấu</span>
        </div>

        <h1 className="text-3xl sm:text-4xl font-extrabold text-gray-900 tracking-tight">
          Tìm kiếm công thức nấu ăn
        </h1>

        <form action="/search" method="GET" className="relative flex items-center mt-6">
          <Search className="w-5 h-5 text-gray-400 absolute left-4 pointer-events-none" />
          <input
            type="text"
            name="q"
            defaultValue={q}
            placeholder="Nhập tên món ăn, nguyên liệu (VD: pho, bo, cuon, salad)..."
            className="w-full pl-12 pr-28 py-3.5 text-sm bg-white border border-gray-200 rounded-2xl shadow-sm focus:outline-none focus:ring-2 focus:ring-orange-500 text-gray-900"
          />
          <button
            type="submit"
            className="absolute right-2 px-5 py-2 bg-orange-600 hover:bg-orange-700 text-white text-xs font-semibold rounded-xl shadow-md shadow-orange-200 transition-all"
          >
            Tìm kiếm
          </button>
        </form>
      </div>

      {/* Error alert */}
      {error && (
        <div className="max-w-3xl mx-auto p-4 rounded-2xl bg-red-50 border border-red-200 text-red-700 text-sm flex items-center gap-3">
          <AlertCircle className="w-5 h-5 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {/* Results Section */}
      {q.length >= 2 && (
        <div className="space-y-6">
          <div className="flex items-center justify-between border-b border-gray-100 pb-4">
            <h2 className="text-lg font-bold text-gray-900">
              Kết quả cho từ khóa: <span className="text-orange-600 font-extrabold">&quot;{q}&quot;</span>
            </h2>
            <span className="text-xs font-semibold text-gray-500">
              Tìm thấy {result.meta.total} món ăn
            </span>
          </div>

          {result.data.length === 0 ? (
            <div className="text-center py-20 bg-gray-50 rounded-3xl border border-dashed border-gray-200 p-8 space-y-3">
              <Search className="w-12 h-12 text-gray-300 mx-auto" />
              <h3 className="font-bold text-gray-700 text-lg">Không tìm thấy công thức phù hợp</h3>
              <p className="text-gray-500 text-xs max-w-sm mx-auto">
                Hãy thử tìm kiếm với các từ khóa phổ biến hơn như &quot;bò&quot;, &quot;gà&quot;, &quot;canh&quot;, hoặc &quot;kho&quot;.
              </p>
              <Link
                href="/recipes"
                className="inline-block mt-3 px-5 py-2 rounded-xl bg-orange-600 text-white text-xs font-semibold hover:bg-orange-700 transition-colors"
              >
                Duyệt tất cả công thức
              </Link>
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
              {result.data.map((recipe) => (
                <RecipeCard key={recipe.id} recipe={recipe} />
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
