'use client';

import { useState, useMemo } from 'react';
import Link from 'next/link';
import { useFavorites, SavedRecipeItem } from '@/lib/favorites';
import {
  Heart,
  Search,
  Trash2,
  Clock,
  Users,
  ChefHat,
  ArrowRight,
  BookOpen,
  Filter,
  Printer,
  Sparkles,
} from 'lucide-react';
import { CATEGORY_FALLBACK_IMAGES } from '@/lib/recipeImages';

export default function SavedRecipesPage() {
  const { favorites, count, isLoaded, remove } = useFavorites();
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [confirmClear, setConfirmClear] = useState(false);

  // Extract categories from saved recipes
  const categories = useMemo(() => {
    const set = new Set<string>();
    favorites.forEach((f) => {
      if (f.categoryName) set.add(f.categoryName);
    });
    return Array.from(set);
  }, [favorites]);

  // Filter recipes based on search and category
  const filteredRecipes = useMemo(() => {
    return favorites.filter((item) => {
      const matchSearch =
        !searchTerm ||
        item.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (item.description &&
          item.description.toLowerCase().includes(searchTerm.toLowerCase())) ||
        (item.categoryName &&
          item.categoryName.toLowerCase().includes(searchTerm.toLowerCase()));

      const matchCategory =
        selectedCategory === 'all' || item.categoryName === selectedCategory;

      return matchSearch && matchCategory;
    });
  }, [favorites, searchTerm, selectedCategory]);

  const handleClearAll = () => {
    favorites.forEach((f) => remove(f.slug));
    setConfirmClear(false);
  };

  const handlePrint = () => {
    if (typeof window !== 'undefined') {
      window.print();
    }
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* Header bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-gray-100 pb-6 print:border-none">
        <div className="space-y-1">
          <div className="flex items-center gap-2.5">
            <div className="p-2.5 rounded-2xl bg-rose-50 text-rose-600 border border-rose-100 shadow-sm">
              <Heart className="w-6 h-6 fill-rose-500" />
            </div>
            <div>
              <h1 className="text-2xl sm:text-3xl font-extrabold text-gray-900 tracking-tight">
                Công thức đã lưu
              </h1>
              <p className="text-xs sm:text-sm text-gray-500">
                Bộ sưu tập các món ăn yêu thích bạn đã lưu để nấu bất cứ lúc nào
              </p>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-2.5 self-start sm:self-auto print:hidden">
          {count > 0 && (
            <>
              <button
                onClick={handlePrint}
                className="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl text-xs font-semibold bg-white border border-gray-200 text-gray-700 hover:bg-gray-50 transition-colors shadow-sm"
              >
                <Printer className="w-4 h-4 text-gray-500" />
                <span>In danh sách</span>
              </button>

              <button
                onClick={() => setConfirmClear(true)}
                className="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl text-xs font-semibold bg-rose-50 border border-rose-100 text-rose-700 hover:bg-rose-100 transition-colors"
              >
                <Trash2 className="w-4 h-4 text-rose-500" />
                <span>Xoá tất cả</span>
              </button>
            </>
          )}

          <Link
            href="/recipes"
            className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-xs font-bold bg-emerald-600 text-white hover:bg-emerald-700 shadow-sm transition-all"
          >
            <BookOpen className="w-4 h-4" />
            <span>Khám phá thêm</span>
          </Link>
        </div>
      </div>

      {/* Confirmation Modal for Clearing */}
      {confirmClear && (
        <div
          role="dialog"
          aria-modal="true"
          className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4 animate-in fade-in duration-200"
        >
          <div className="bg-white rounded-2xl max-w-sm w-full p-6 space-y-4 shadow-xl border border-gray-100">
            <h3 className="text-lg font-bold text-gray-900">Xoá toàn bộ công thức đã lưu?</h3>
            <p className="text-sm text-gray-600">
              Thao tác này sẽ xoá toàn bộ {count} công thức khỏi danh sách yêu thích của bạn. Bạn có chắc chắn không?
            </p>
            <div className="flex items-center justify-end gap-3 pt-2">
              <button
                onClick={() => setConfirmClear(false)}
                className="px-4 py-2 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-100 transition-colors"
              >
                Huỷ
              </button>
              <button
                onClick={handleClearAll}
                className="px-4 py-2 rounded-xl text-xs font-bold text-white bg-rose-600 hover:bg-rose-700 transition-colors shadow-sm"
              >
                Xác nhận xoá
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Filter and Search Bar */}
      {count > 0 && (
        <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 bg-white p-3 rounded-2xl border border-gray-100 shadow-sm print:hidden">
          <div className="relative flex-1">
            <Search className="w-4 h-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              placeholder="Tìm kiếm trong danh sách đã lưu..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-9 pr-4 py-2 text-sm bg-gray-50 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-emerald-500/20 focus:border-emerald-500 transition-all"
            />
          </div>

          {categories.length > 0 && (
            <div className="flex items-center gap-1.5 overflow-x-auto pb-1 sm:pb-0">
              <button
                onClick={() => setSelectedCategory('all')}
                className={`px-3 py-1.5 rounded-xl text-xs font-semibold shrink-0 transition-colors ${
                  selectedCategory === 'all'
                    ? 'bg-emerald-600 text-white'
                    : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                }`}
              >
                Tất cả ({count})
              </button>
              {categories.map((cat) => (
                <button
                  key={cat}
                  onClick={() => setSelectedCategory(cat)}
                  className={`px-3 py-1.5 rounded-xl text-xs font-semibold shrink-0 transition-colors ${
                    selectedCategory === cat
                      ? 'bg-emerald-600 text-white'
                      : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                  }`}
                >
                  {cat}
                </button>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Content Grid */}
      {!isLoaded ? (
        <div className="py-20 text-center space-y-3">
          <div className="w-8 h-8 border-2 border-emerald-600 border-t-transparent rounded-full animate-spin mx-auto" />
          <p className="text-sm text-gray-500">Đang tải danh sách công thức...</p>
        </div>
      ) : count === 0 ? (
        /* Empty State */
        <div className="bg-white rounded-3xl border border-dashed border-gray-200 p-12 text-center space-y-6 max-w-lg mx-auto my-8">
          <div className="w-20 h-20 bg-rose-50 border border-rose-100 rounded-full flex items-center justify-center mx-auto text-rose-500 shadow-sm">
            <Heart className="w-10 h-10" />
          </div>
          <div className="space-y-2">
            <h3 className="text-xl font-bold text-gray-900">Chưa có công thức nào được lưu</h3>
            <p className="text-sm text-gray-500 leading-relaxed">
              Khi tìm thấy công thức yêu thích, hãy nhấn vào biểu tượng trái tim để lưu lại và truy cập nhanh tại đây bất kỳ lúc nào!
            </p>
          </div>
          <Link
            href="/recipes"
            className="inline-flex items-center gap-2 px-6 py-3 rounded-xl bg-emerald-600 text-white font-bold text-sm hover:bg-emerald-700 shadow-md shadow-emerald-600/20 transition-all"
          >
            <span>Khám phá công thức ngay</span>
            <ArrowRight className="w-4 h-4" />
          </Link>
        </div>
      ) : filteredRecipes.length === 0 ? (
        /* No Search Results */
        <div className="bg-white rounded-2xl border border-gray-100 p-10 text-center space-y-3">
          <p className="font-semibold text-gray-800">Không tìm thấy công thức nào phù hợp</p>
          <p className="text-xs text-gray-500">Thử tìm kiếm với từ khoá khác hoặc đổi danh mục</p>
          <button
            onClick={() => {
              setSearchTerm('');
              setSelectedCategory('all');
            }}
            className="text-xs font-semibold text-emerald-600 hover:underline pt-2"
          >
            Xoá bộ lọc tìm kiếm
          </button>
        </div>
      ) : (
        /* Recipe Cards Grid */
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredRecipes.map((recipe) => {
            const fallbackImg =
              (recipe.categoryName && CATEGORY_FALLBACK_IMAGES[recipe.categoryName]) ||
              'https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=800&auto=format&fit=crop';
            const displayImg = recipe.imageUrl || fallbackImg;

            return (
              <div
                key={recipe.slug}
                className="group bg-white rounded-2xl border border-gray-100 shadow-sm hover:shadow-lg transition-all duration-300 flex flex-col overflow-hidden relative"
              >
                {/* Image */}
                <div className="relative w-full h-48 bg-gray-100 overflow-hidden">
                  <img
                    src={displayImg}
                    alt={recipe.title}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                    onError={(e) => {
                      (e.target as HTMLImageElement).src = fallbackImg;
                    }}
                  />

                  {recipe.categoryName && (
                    <div className="absolute top-3 left-3">
                      <span className="px-2.5 py-1 rounded-full text-[11px] font-bold bg-white/95 backdrop-blur text-gray-800 shadow-sm">
                        {recipe.categoryName}
                      </span>
                    </div>
                  )}

                  {/* Remove button */}
                  <button
                    onClick={() => remove(recipe.slug)}
                    className="absolute top-3 right-3 p-2 rounded-full bg-rose-500 text-white shadow-md hover:bg-rose-600 transition-colors"
                    title="Xoá khỏi danh sách yêu thích"
                    aria-label={`Xoá ${recipe.title} khỏi yêu thích`}
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                </div>

                {/* Content */}
                <div className="p-5 flex-1 flex flex-col justify-between space-y-4">
                  <div className="space-y-1.5">
                    <Link
                      href={`/recipes/${encodeURIComponent(recipe.slug)}`}
                      className="font-bold text-gray-900 text-base sm:text-lg group-hover:text-emerald-700 transition-colors line-clamp-1 block"
                    >
                      {recipe.title}
                    </Link>
                    {recipe.description && (
                      <p className="text-xs text-gray-500 line-clamp-2 leading-relaxed">
                        {recipe.description}
                      </p>
                    )}
                  </div>

                  <div className="pt-3 border-t border-gray-100 flex items-center justify-between text-xs text-gray-500">
                    <div className="flex items-center gap-3">
                      {recipe.cookTimeMinutes !== undefined && (
                        <span className="flex items-center gap-1 font-medium text-emerald-800">
                          <Clock className="w-3.5 h-3.5 text-emerald-600" />
                          {recipe.cookTimeMinutes} phút
                        </span>
                      )}
                      {recipe.servings !== undefined && (
                        <span className="flex items-center gap-1">
                          <Users className="w-3.5 h-3.5 text-gray-400" />
                          {recipe.servings} người
                        </span>
                      )}
                    </div>

                    <Link
                      href={`/recipes/${encodeURIComponent(recipe.slug)}`}
                      className="inline-flex items-center gap-1 text-xs font-bold text-emerald-600 hover:text-emerald-700"
                    >
                      Xem chi tiết <ArrowRight className="w-3.5 h-3.5" />
                    </Link>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
