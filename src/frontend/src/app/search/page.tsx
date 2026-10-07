import type { Metadata } from 'next';
import Link from 'next/link';
import { searchRecipes, getCategories } from '@/lib/api';
import RecipeCard from '@/components/RecipeCard';
import { SearchFilterSelect } from '@/components/SearchFilterSelect';
import { Search, Sparkles, AlertCircle, Filter, ArrowUpDown } from 'lucide-react';

interface SearchPageProps {
  searchParams: Promise<{
    q?: string;
    page?: string;
    categoryId?: string;
    difficulty?: string;
    maxCookTime?: string;
    minServings?: string;
    sortBy?: string;
    sortOrder?: string;
  }>;
}

export async function generateMetadata({ searchParams }: SearchPageProps): Promise<Metadata> {
  const params = await searchParams;
  const q = params.q?.trim() || '';

  return {
    title: q ? `Tìm kiếm: "${q}" | Culinary Blog` : 'Tìm kiếm công thức nấu ăn | Culinary Blog',
    description: 'Tìm kiếm công thức nấu ăn thơm ngon, hấp dẫn và dễ làm với công nghệ tìm kiếm thông minh không dấu.',
    alternates: {
      canonical: '/search',
    },
    robots: {
      index: !q, // Noindex query pages to prevent thin/duplicate content in search engines
      follow: true,
    },
  };
}

export default async function SearchPage({ searchParams }: SearchPageProps) {
  const params = await searchParams;
  const q = params.q?.trim() || '';
  const page = params.page ? Math.max(1, parseInt(params.page, 10)) : 1;
  const categoryId = params.categoryId || '';
  const difficulty = params.difficulty || '';
  const maxCookTime = params.maxCookTime ? parseInt(params.maxCookTime, 10) : undefined;
  const minServings = params.minServings ? parseInt(params.minServings, 10) : undefined;
  const sortBy = params.sortBy || 'createdAt';
  const sortOrder = params.sortOrder || 'desc';

  const categories = await getCategories();

  let result = {
    data: [] as import('@/types/recipe').RecipeSummary[],
    meta: { page: 1, pageSize: 12, total: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false },
  };

  let error: string | null = null;

  if (q.length > 0 && q.length < 2) {
    error = 'Từ khóa tìm kiếm phải có từ 2 ký tự trở lên.';
  } else if (q.length >= 2) {
    result = await searchRecipes(q, {
      page,
      pageSize: 12,
      categoryId: categoryId || undefined,
      difficulty: difficulty || undefined,
      maxCookTime,
      minServings,
      sortBy,
      sortOrder,
    });
  }

  const buildQueryUrl = (newParams: Record<string, string | number | undefined>) => {
    const searchObj = new URLSearchParams();
    if (q) searchObj.set('q', q);
    if (categoryId) searchObj.set('categoryId', categoryId);
    if (difficulty) searchObj.set('difficulty', difficulty);
    if (maxCookTime !== undefined) searchObj.set('maxCookTime', maxCookTime.toString());
    if (minServings !== undefined) searchObj.set('minServings', minServings.toString());
    if (sortBy && sortBy !== 'createdAt') searchObj.set('sortBy', sortBy);
    if (sortOrder && sortOrder !== 'desc') searchObj.set('sortOrder', sortOrder);

    for (const [key, value] of Object.entries(newParams)) {
      if (value === undefined || value === '') {
        searchObj.delete(key);
      } else {
        searchObj.set(key, value.toString());
      }
    }

    const queryStr = searchObj.toString();
    return queryStr ? `/search?${queryStr}` : '/search';
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-10">
      {/* Search Header */}
      <div className="max-w-3xl mx-auto text-center space-y-4">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-emerald-100/80 text-emerald-800 text-xs font-semibold border border-emerald-200/50">
          <Sparkles className="w-3.5 h-3.5 text-emerald-600" />
          <span>Tìm kiếm thông minh không dấu</span>
        </div>

        <h1 className="text-3xl sm:text-4xl font-extrabold text-gray-900 tracking-tight">
          Tìm kiếm công thức nấu ăn
        </h1>

        <form action="/search" method="GET" className="relative flex items-center mt-6" role="search">
          <Search className="w-5 h-5 text-gray-400 absolute left-4 pointer-events-none" aria-hidden="true" />
          <input
            type="text"
            name="q"
            defaultValue={q}
            aria-label="Nhập từ khóa tìm kiếm món ăn hoặc nguyên liệu"
            placeholder="Nhập tên món ăn, nguyên liệu (VD: pho, bo, cuon, salad)..."
            className="w-full pl-12 pr-28 py-3.5 text-sm bg-white border border-gray-200 rounded-2xl shadow-sm focus:outline-none focus:ring-2 focus:ring-emerald-500 text-gray-900"
          />
          {categoryId && <input type="hidden" name="categoryId" value={categoryId} />}
          {difficulty && <input type="hidden" name="difficulty" value={difficulty} />}
          {sortBy !== 'createdAt' && <input type="hidden" name="sortBy" value={sortBy} />}
          {sortOrder !== 'desc' && <input type="hidden" name="sortOrder" value={sortOrder} />}
          <button
            type="submit"
            aria-label="Thực hiện tìm kiếm công thức"
            className="absolute right-2 px-5 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-semibold rounded-xl shadow-md shadow-emerald-200/60 transition-all"
          >
            Tìm kiếm
          </button>
        </form>
      </div>

      {/* Error alert */}
      {error && (
        <div role="alert" className="max-w-3xl mx-auto p-4 rounded-2xl bg-red-50 border border-red-200 text-red-700 text-sm flex items-center gap-3">
          <AlertCircle className="w-5 h-5 flex-shrink-0" aria-hidden="true" />
          <span>{error}</span>
        </div>
      )}

      {/* Results Section */}
      {q.length >= 2 && (
        <div className="space-y-6">
          {/* Filter Bar & Controls */}
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 bg-gray-50/80 p-4 rounded-2xl border border-gray-200/60">
            <div>
              <h2 className="text-lg font-bold text-gray-900">
                Kết quả cho từ khóa: <span className="text-emerald-700 font-extrabold">&quot;{q}&quot;</span>
              </h2>
              <span className="text-xs font-semibold text-gray-500" aria-live="polite">
                Tìm thấy {result.meta.total} món ăn (Trang {result.meta.page}/{Math.max(1, result.meta.totalPages)})
              </span>
            </div>

            <div className="flex flex-wrap items-center gap-3">
              {/* Category Filter */}
              <div className="flex items-center gap-1.5 text-xs text-gray-600">
                <Filter className="w-3.5 h-3.5 text-gray-400" />
                <form action="/search" method="GET" className="inline">
                  <input type="hidden" name="q" value={q} />
                  {difficulty && <input type="hidden" name="difficulty" value={difficulty} />}
                  <SearchFilterSelect
                    name="categoryId"
                    defaultValue={categoryId}
                    options={[
                      { value: '', label: 'Tất cả danh mục' },
                      ...categories.map((c) => ({ value: c.id, label: c.name })),
                    ]}
                  />
                </form>
              </div>

              {/* Difficulty Filter */}
              <form action="/search" method="GET" className="inline">
                <input type="hidden" name="q" value={q} />
                {categoryId && <input type="hidden" name="categoryId" value={categoryId} />}
                <SearchFilterSelect
                  name="difficulty"
                  defaultValue={difficulty}
                  options={[
                    { value: '', label: 'Độ khó' },
                    { value: 'Easy', label: 'Dễ' },
                    { value: 'Medium', label: 'Trung bình' },
                    { value: 'Hard', label: 'Khó' },
                    { value: 'Expert', label: 'Chuyên gia' },
                  ]}
                />
              </form>

              {/* Sort Order */}
              <div className="flex items-center gap-1.5 text-xs text-gray-600">
                <ArrowUpDown className="w-3.5 h-3.5 text-gray-400" />
                <form action="/search" method="GET" className="inline">
                  <input type="hidden" name="q" value={q} />
                  {categoryId && <input type="hidden" name="categoryId" value={categoryId} />}
                  {difficulty && <input type="hidden" name="difficulty" value={difficulty} />}
                  <SearchFilterSelect
                    name="sortBy"
                    defaultValue={sortBy}
                    options={[
                      { value: 'createdAt', label: 'Mới nhất' },
                      { value: 'cookTimeMinutes', label: 'Nấu nhanh nhất' },
                      { value: 'title', label: 'Tên món A-Z' },
                    ]}
                  />
                </form>
              </div>
            </div>
          </div>

          {result.data.length === 0 ? (
            <div className="text-center py-20 bg-gray-50 rounded-3xl border border-dashed border-gray-200 p-8 space-y-3">
              <Search className="w-12 h-12 text-gray-300 mx-auto" />
              <h3 className="font-bold text-gray-700 text-lg">Không tìm thấy công thức phù hợp</h3>
              <p className="text-gray-500 text-xs max-w-sm mx-auto">
                Hãy thử tìm kiếm với các từ khóa phổ biến hơn như &quot;bò&quot;, &quot;gà&quot;, &quot;canh&quot;, hoặc &quot;kho&quot;.
              </p>
              <div className="flex justify-center gap-3 pt-2">
                {(categoryId || difficulty) && (
                  <Link
                    href={`/search?q=${encodeURIComponent(q)}`}
                    className="inline-block px-4 py-2 rounded-xl bg-gray-200 text-gray-700 text-xs font-semibold hover:bg-gray-300 transition-colors shadow-sm"
                  >
                    Xóa bộ lọc
                  </Link>
                )}
                <Link
                  href="/recipes"
                  className="inline-block px-4 py-2 rounded-xl bg-emerald-600 text-white text-xs font-semibold hover:bg-emerald-700 transition-colors shadow-sm"
                >
                  Duyệt tất cả công thức
                </Link>
              </div>
            </div>
          ) : (
            <>
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                {result.data.map((recipe) => (
                  <RecipeCard key={recipe.id} recipe={recipe} />
                ))}
              </div>

              {/* Pagination */}
              {result.meta.totalPages > 1 && (
                <div className="flex items-center justify-center gap-2 pt-6">
                  {result.meta.hasPreviousPage && (
                    <Link
                      href={buildQueryUrl({ page: page - 1 })}
                      className="px-4 py-2 bg-white border border-gray-200 rounded-xl text-xs font-semibold text-gray-700 hover:bg-gray-50 transition-colors shadow-sm"
                    >
                      Trang trước
                    </Link>
                  )}
                  <span className="text-xs font-medium text-gray-500 px-3">
                    {page} / {result.meta.totalPages}
                  </span>
                  {result.meta.hasNextPage && (
                    <Link
                      href={buildQueryUrl({ page: page + 1 })}
                      className="px-4 py-2 bg-white border border-gray-200 rounded-xl text-xs font-semibold text-gray-700 hover:bg-gray-50 transition-colors shadow-sm"
                    >
                      Trang sau
                    </Link>
                  )}
                </div>
              )}
            </>
          )}
        </div>
      )}
    </div>
  );
}
