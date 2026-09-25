import Link from 'next/link';
import { getRecipes, getCategories } from '@/lib/api';
import RecipeCard from '@/components/RecipeCard';
import { RecipeFilters } from '@/types/recipe';
import { Filter, SlidersHorizontal, BookOpen, ChevronLeft, ChevronRight } from 'lucide-react';

interface RecipesPageProps {
  searchParams: Promise<{
    page?: string;
    pageSize?: string;
    sortBy?: string;
    sortOrder?: string;
    categoryId?: string;
    difficulty?: string;
    maxCookTime?: string;
    minServings?: string;
  }>;
}

export default async function RecipesPage({ searchParams }: RecipesPageProps) {
  const params = await searchParams;

  const filters: RecipeFilters = {
    page: params.page ? parseInt(params.page, 10) : 1,
    pageSize: params.pageSize ? parseInt(params.pageSize, 10) : 12,
    sortBy: params.sortBy || 'createdAt',
    sortOrder: params.sortOrder || 'desc',
    categoryId: params.categoryId,
    difficulty: params.difficulty,
    maxCookTime: params.maxCookTime ? parseInt(params.maxCookTime, 10) : undefined,
    minServings: params.minServings ? parseInt(params.minServings, 10) : undefined,
  };

  const [recipesResult, categories] = await Promise.all([
    getRecipes(filters),
    getCategories(),
  ]);

  const { data: recipes, meta } = recipesResult;

  const buildUrl = (newParams: Partial<Record<string, string | number | undefined>>) => {
    const search = new URLSearchParams();
    const merged = {
      page: filters.page,
      sortBy: filters.sortBy,
      sortOrder: filters.sortOrder,
      categoryId: filters.categoryId,
      difficulty: filters.difficulty,
      maxCookTime: filters.maxCookTime,
      minServings: filters.minServings,
      ...newParams,
    };

    Object.entries(merged).forEach(([key, val]) => {
      if (val !== undefined && val !== null && val !== '') {
        search.set(key, val.toString());
      }
    });

    return `/recipes?${search.toString()}`;
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-10">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 border-b border-gray-100 pb-8">
        <div>
          <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-emerald-100/80 text-emerald-800 text-xs font-semibold mb-3 border border-emerald-200/50">
            <BookOpen className="w-3.5 h-3.5 text-emerald-700" />
            <span>Kho tàng ẩm thực</span>
          </div>
          <h1 className="text-3xl sm:text-4xl font-extrabold text-gray-900 tracking-tight">
            Khám phá công thức nấu ăn
          </h1>
          <p className="text-gray-500 text-sm mt-2">
            Tìm thấy {meta.total} công thức đã được xuất bản và kiểm duyệt chất lượng.
          </p>
        </div>

        {/* Sorting controls */}
        <div className="flex items-center gap-3">
          <label className="text-xs font-bold text-gray-500 uppercase tracking-wider">
            Sắp xếp:
          </label>
          <div className="flex rounded-xl bg-gray-100 p-1 text-xs font-semibold">
            <Link
              href={buildUrl({ sortBy: 'createdAt', sortOrder: 'desc', page: 1 })}
              className={`px-3 py-1.5 rounded-lg transition-colors ${
                filters.sortBy === 'createdAt' ? 'bg-white text-emerald-700 shadow-sm' : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              Mới nhất
            </Link>
            <Link
              href={buildUrl({ sortBy: 'cookTimeMinutes', sortOrder: 'asc', page: 1 })}
              className={`px-3 py-1.5 rounded-lg transition-colors ${
                filters.sortBy === 'cookTimeMinutes' ? 'bg-white text-emerald-700 shadow-sm' : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              Nhanh nhất
            </Link>
            <Link
              href={buildUrl({ sortBy: 'title', sortOrder: 'asc', page: 1 })}
              className={`px-3 py-1.5 rounded-lg transition-colors ${
                filters.sortBy === 'title' ? 'bg-white text-emerald-700 shadow-sm' : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              Tên A-Z
            </Link>
          </div>
        </div>
      </div>

      {/* Main Layout: Sidebar Filters + Recipes Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 items-start">
        {/* Sidebar Filters */}
        <aside className="bg-white p-6 rounded-2xl border border-gray-100 shadow-sm space-y-6">
          <div className="flex items-center justify-between border-b border-gray-100 pb-3">
            <span className="font-bold text-sm text-gray-900 flex items-center gap-2">
              <Filter className="w-4 h-4 text-emerald-700" />
              Bộ lọc nâng cao
            </span>
            {(filters.categoryId || filters.difficulty || filters.maxCookTime || filters.minServings) && (
              <Link href="/recipes" className="text-xs text-emerald-700 hover:underline">
                Đặt lại
              </Link>
            )}
          </div>

          {/* Filter: Categories */}
          <div>
            <h4 className="text-xs font-bold text-gray-500 uppercase tracking-wider mb-3">
              Danh mục
            </h4>
            <div className="space-y-1.5 max-h-[360px] overflow-y-auto pr-1">
              <Link
                href={buildUrl({ categoryId: undefined, page: 1 })}
                className={`flex items-center justify-between px-3 py-2 rounded-xl text-xs font-semibold transition-colors ${
                  !filters.categoryId ? 'bg-emerald-50 text-emerald-800' : 'text-gray-600 hover:bg-gray-50'
                }`}
              >
                <span>Tất cả danh mục</span>
                <span className={`text-[10px] px-1.5 py-0.5 rounded-full ${
                  !filters.categoryId ? 'bg-emerald-200 text-emerald-900 font-bold' : 'bg-gray-100 text-gray-500'
                }`}>
                  {categories.reduce((acc, c) => acc + (c.recipesCount || 0), 0)}
                </span>
              </Link>
              {categories.map((cat) => (
                <Link
                  key={cat.id}
                  href={buildUrl({ categoryId: cat.id, page: 1 })}
                  className={`flex items-center justify-between px-3 py-2 rounded-xl text-xs font-semibold transition-colors ${
                    filters.categoryId === cat.id ? 'bg-emerald-50 text-emerald-800' : 'text-gray-600 hover:bg-gray-50'
                  }`}
                >
                  <span className="truncate pr-2">{cat.name}</span>
                  <span className={`text-[10px] px-1.5 py-0.5 rounded-full shrink-0 ${
                    filters.categoryId === cat.id ? 'bg-emerald-200 text-emerald-900 font-bold' : 'bg-gray-100 text-gray-500'
                  }`}>
                    {cat.recipesCount}
                  </span>
                </Link>
              ))}
            </div>
          </div>

          {/* Filter: Difficulty */}
          <div className="border-t border-gray-100 pt-4">
            <h4 className="text-xs font-bold text-gray-500 uppercase tracking-wider mb-3">
              Độ khó
            </h4>
            <div className="flex flex-wrap gap-2">
              {['Easy', 'Medium', 'Hard', 'Expert'].map((d) => (
                <Link
                  key={d}
                  href={buildUrl({ difficulty: filters.difficulty === d ? undefined : d, page: 1 })}
                  className={`px-3 py-1.5 rounded-lg text-xs font-semibold border transition-all ${
                    filters.difficulty === d
                      ? 'bg-emerald-600 border-emerald-600 text-white shadow-sm'
                      : 'border-gray-200 text-gray-600 hover:bg-gray-50'
                  }`}
                >
                  {d === 'Easy' ? 'Dễ' : d === 'Medium' ? 'Trung bình' : d === 'Hard' ? 'Khó' : 'Chuyên gia'}
                </Link>
              ))}
            </div>
          </div>

          {/* Filter: Max Cook Time */}
          <div className="border-t border-gray-100 pt-4">
            <h4 className="text-xs font-bold text-gray-500 uppercase tracking-wider mb-3">
              Thời gian nấu tối đa
            </h4>
            <div className="grid grid-cols-2 gap-2">
              {[15, 30, 60, 120].map((mins) => (
                <Link
                  key={mins}
                  href={buildUrl({ maxCookTime: filters.maxCookTime === mins ? undefined : mins, page: 1 })}
                  className={`px-3 py-2 rounded-xl text-center text-xs font-semibold border transition-all ${
                    filters.maxCookTime === mins
                      ? 'bg-emerald-600 border-emerald-600 text-white shadow-sm'
                      : 'border-gray-200 text-gray-600 hover:bg-gray-50'
                  }`}
                >
                  $\le$ {mins} phút
                </Link>
              ))}
            </div>
          </div>
        </aside>

        {/* Recipes Grid & Pagination */}
        <main className="lg:col-span-3 space-y-8">
          {recipes.length === 0 ? (
            <div className="text-center py-20 bg-white rounded-3xl border border-dashed border-gray-200 p-8 space-y-3">
              <BookOpen className="w-12 h-12 text-emerald-300 mx-auto" />
              <h3 className="font-bold text-gray-800 text-lg">Không tìm thấy công thức nào</h3>
              <p className="text-gray-500 text-xs max-w-sm mx-auto leading-relaxed">
                Hãy thử nới lỏng các bộ lọc hoặc chọn danh mục khác để khám phá thêm món ăn.
              </p>
              <Link
                href="/recipes"
                className="inline-block mt-3 px-5 py-2 rounded-xl bg-emerald-600 text-white text-xs font-semibold hover:bg-emerald-700 transition-colors shadow-sm"
              >
                Xóa tất cả bộ lọc
              </Link>
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-6">
              {recipes.map((recipe) => (
                <RecipeCard key={recipe.id} recipe={recipe} />
              ))}
            </div>
          )}

          {/* Pagination Controls */}
          {meta.totalPages > 1 && (
            <div className="flex items-center justify-between border-t border-gray-100 pt-6">
              <span className="text-xs text-gray-500 font-medium">
                Trang {meta.page} trên {meta.totalPages} ({meta.total} công thức)
              </span>

              <div className="flex items-center gap-2">
                {meta.hasPreviousPage ? (
                  <Link
                    href={buildUrl({ page: meta.page - 1 })}
                    className="p-2 rounded-xl border border-gray-200 text-gray-700 hover:bg-gray-50 transition-colors"
                    title="Trang trước"
                  >
                    <ChevronLeft className="w-4 h-4" />
                  </Link>
                ) : (
                  <button disabled className="p-2 rounded-xl border border-gray-100 text-gray-300 cursor-not-allowed">
                    <ChevronLeft className="w-4 h-4" />
                  </button>
                )}

                <span className="px-3 py-1.5 bg-emerald-50 text-emerald-700 text-xs font-bold rounded-lg">
                  {meta.page}
                </span>

                {meta.hasNextPage ? (
                  <Link
                    href={buildUrl({ page: meta.page + 1 })}
                    className="p-2 rounded-xl border border-gray-200 text-gray-700 hover:bg-gray-50 transition-colors"
                    title="Trang sau"
                  >
                    <ChevronRight className="w-4 h-4" />
                  </Link>
                ) : (
                  <button disabled className="p-2 rounded-xl border border-gray-100 text-gray-300 cursor-not-allowed">
                    <ChevronRight className="w-4 h-4" />
                  </button>
                )}
              </div>
            </div>
          )}
        </main>
      </div>
    </div>
  );
}
