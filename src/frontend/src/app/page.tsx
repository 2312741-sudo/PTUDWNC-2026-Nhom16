import Link from 'next/link';
import { getCategories, getRecipes } from '@/lib/api';
import CategoryCard from '@/components/CategoryCard';
import RecipeCard from '@/components/RecipeCard';
import {
  Sparkles,
  Search,
  BookOpen,
  ArrowRight,
  ShieldCheck,
  ChefHat,
  Flame,
  Layers,
  ChevronRight,
} from 'lucide-react';

export default async function HomePage() {
  const [categoriesRaw, recipesResult] = await Promise.all([
    getCategories(),
    getRecipes({ page: 1, pageSize: 6, sortBy: 'createdAt', sortOrder: 'desc' }),
  ]);

  const categories = Array.isArray(categoriesRaw) ? categoriesRaw : [];
  const latestRecipes = Array.isArray(recipesResult?.data) ? recipesResult.data : [];

  return (
    <div className="space-y-16 pb-12">
      {/* Hero Section */}
      <section className="relative overflow-hidden bg-gradient-to-b from-emerald-50/70 via-amber-50/20 to-white py-20 lg:py-28">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center relative z-10">
          <div className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-emerald-100/80 text-emerald-900 text-xs font-semibold mb-6 shadow-sm border border-emerald-200/50">
            <Sparkles className="w-4 h-4 text-emerald-600" />
            <span>Nền tảng ẩm thực cao cấp — .NET 10 & Next.js App Router</span>
          </div>

          <h1 className="text-4xl sm:text-5xl lg:text-6xl font-extrabold text-gray-900 tracking-tight max-w-4xl mx-auto leading-tight sm:leading-none">
            Khơi dậy đam mê ẩm thực cùng{' '}
            <span className="text-transparent bg-clip-text bg-gradient-to-r from-emerald-600 via-teal-600 to-amber-600">
              hàng trăm công thức
            </span>{' '}
            chuẩn vị
          </h1>

          <p className="mt-6 text-lg sm:text-xl text-gray-600 max-w-2xl mx-auto leading-relaxed">
            Khám phá tinh hoa ẩm thực phong phú, công thức chi tiết từng bước, định lượng nguyên liệu chuẩn xác và giá trị dinh dưỡng minh bạch.
          </p>

          {/* Quick CTA Actions */}
          <div className="mt-10 flex flex-wrap items-center justify-center gap-4">
            <Link
              href="/recipes"
              className="px-8 py-3.5 rounded-xl bg-emerald-600 text-white font-semibold shadow-lg shadow-emerald-200/80 hover:bg-emerald-700 hover:scale-105 transition-all flex items-center gap-2"
            >
              <BookOpen className="w-5 h-5" />
              Khám phá công thức
            </Link>

            <Link
              href="/categories"
              className="px-8 py-3.5 rounded-xl bg-white text-gray-800 font-semibold border border-gray-200 hover:bg-gray-50 hover:border-gray-300 transition-all flex items-center gap-2 shadow-sm"
            >
              Xem danh mục
              <ArrowRight className="w-4 h-4 text-emerald-600" />
            </Link>
          </div>
        </div>
      </section>

      {/* Main Exploration Section: Sidebar Categories + Main Content New Recipes (Lab 7) */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
          {/* Sidebar: Danh sách Category */}
          <aside className="lg:col-span-1 space-y-4">
            <div className="bg-white p-5 rounded-2xl border border-gray-100 shadow-sm sticky top-24">
              <div className="flex items-center gap-2 pb-3 mb-3 border-b border-gray-100">
                <Layers className="w-4 h-4 text-emerald-600" />
                <h3 className="font-extrabold text-gray-900 text-sm uppercase tracking-wider">
                  Danh mục món ăn
                </h3>
              </div>

              {categories.length === 0 ? (
                <p className="text-xs text-gray-400 py-2">Đang tải danh mục...</p>
              ) : (
                <div className="space-y-1">
                  {categories.slice(0, 10).map((cat) => (
                    <Link
                      key={cat.id}
                      href={`/categories/${encodeURIComponent(cat.slug)}`}
                      className="flex items-center justify-between px-3 py-2.5 rounded-xl text-xs font-medium text-gray-700 hover:bg-emerald-50 hover:text-emerald-800 transition-colors group"
                    >
                      <span className="truncate group-hover:translate-x-0.5 transition-transform">
                        {cat.name}
                      </span>
                      <ChevronRight className="w-3.5 h-3.5 text-gray-300 group-hover:text-emerald-600 shrink-0" />
                    </Link>
                  ))}
                </div>
              )}

              <div className="pt-3 mt-2 border-t border-gray-100">
                <Link
                  href="/categories"
                  className="w-full py-2 px-3 rounded-xl bg-gray-50 hover:bg-emerald-50 text-emerald-700 text-xs font-semibold flex items-center justify-center gap-1 transition-colors"
                >
                  Xem tất cả danh mục
                  <ArrowRight className="w-3.5 h-3.5" />
                </Link>
              </div>
            </div>
          </aside>

          {/* Main Content: Danh sách công thức mới nhất */}
          <div className="lg:col-span-3 space-y-6">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-gray-100 pb-4">
              <div className="flex items-center gap-2.5">
                <div className="w-8 h-8 rounded-lg bg-emerald-50 text-emerald-700 flex items-center justify-center font-bold">
                  <Flame className="w-4 h-4 text-amber-500" />
                </div>
                <div>
                  <h2 className="text-xl sm:text-2xl font-extrabold text-gray-900 tracking-tight">
                    Công thức mới nhất
                  </h2>
                  <p className="text-xs text-gray-500">
                    Cập nhật liên tục những món ngon và công thức mới từ cộng đồng tác giả
                  </p>
                </div>
              </div>

              <Link
                href="/recipes"
                className="text-xs font-semibold text-emerald-700 hover:text-emerald-800 flex items-center gap-1 group whitespace-nowrap self-start sm:self-auto"
              >
                Xem tất cả công thức{' '}
                <ArrowRight className="w-3.5 h-3.5 group-hover:translate-x-1 transition-transform" />
              </Link>
            </div>

            {latestRecipes.length === 0 ? (
              <div className="text-center py-16 bg-gray-50 rounded-2xl border border-dashed border-gray-200 p-8 space-y-2">
                <ChefHat className="w-10 h-10 text-gray-300 mx-auto" />
                <h4 className="font-bold text-gray-700 text-sm">Chưa có công thức xuất bản</h4>
                <p className="text-xs text-gray-400">
                  Hãy là người đầu tiên chia sẻ công thức tại mục Đăng công thức!
                </p>
                <div className="pt-2">
                  <Link
                    href="/dashboard/recipes/new"
                    className="inline-block px-4 py-2 rounded-xl bg-emerald-600 text-white text-xs font-semibold hover:bg-emerald-700 transition-colors shadow-sm"
                  >
                    Viết công thức mới
                  </Link>
                </div>
              </div>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-6">
                {latestRecipes.map((recipe) => (
                  <RecipeCard key={recipe.id} recipe={recipe} />
                ))}
              </div>
            )}
          </div>
        </div>
      </section>

      {/* Featured Categories Grid */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col sm:flex-row sm:items-end justify-between mb-8 gap-4">
          <div>
            <div className="text-xs font-bold text-emerald-700 tracking-wider uppercase mb-1">
              Phân loại đa dạng
            </div>
            <h2 className="text-2xl sm:text-3xl font-extrabold text-gray-900 tracking-tight">
              Danh mục món ăn nổi bật
            </h2>
          </div>

          <Link
            href="/categories"
            className="text-sm font-semibold text-emerald-700 hover:text-emerald-800 flex items-center gap-1 group"
          >
            Xem tất cả danh mục{' '}
            <ArrowRight className="w-4 h-4 group-hover:translate-x-1 transition-transform" />
          </Link>
        </div>

        {categories.length === 0 ? (
          <div className="text-center py-12 bg-gray-50 rounded-2xl border border-gray-100 p-8">
            <p className="text-gray-500 text-sm">
              Đang kết nối backend hoặc cơ sở dữ liệu chưa có danh mục mẫu.
            </p>
            <p className="text-xs text-gray-400 mt-2">
              (Truy cập <Link href="/dashboard/categories" className="text-emerald-700 underline">Quản trị danh mục</Link> để thêm danh mục mới).
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
            {categories.slice(0, 6).map((cat) => (
              <CategoryCard key={cat.id} category={cat} />
            ))}
          </div>
        )}
      </section>

      {/* Highlights / Features */}
      <section className="bg-gray-50 py-16 border-y border-gray-100">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            <div className="bg-white p-8 rounded-2xl border border-gray-100 shadow-sm space-y-3">
              <div className="w-10 h-10 rounded-lg bg-emerald-50 text-emerald-700 flex items-center justify-center font-bold">
                <Search className="w-5 h-5" />
              </div>
              <h3 className="text-lg font-bold text-gray-900">Tìm kiếm không dấu</h3>
              <p className="text-sm text-gray-500 leading-relaxed">
                Tích hợp PostgreSQL Full-Text Search qua unaccent và pg_trgm, dễ dàng tìm thấy món ăn bạn cần chỉ với vài ký tự.
              </p>
            </div>

            <div className="bg-white p-8 rounded-2xl border border-gray-100 shadow-sm space-y-3">
              <div className="w-10 h-10 rounded-lg bg-amber-100 text-amber-600 flex items-center justify-center font-bold">
                <BookOpen className="w-5 h-5" />
              </div>
              <h3 className="text-lg font-bold text-gray-900">Chi tiết từng bước</h3>
              <p className="text-sm text-gray-500 leading-relaxed">
                Hướng dẫn nấu ăn trực quan với ảnh minh họa cho từng công đoạn, kèm bộ hẹn giờ đếm ngược tiện lợi.
              </p>
            </div>

            <div className="bg-white p-8 rounded-2xl border border-gray-100 shadow-sm space-y-3">
              <div className="w-10 h-10 rounded-lg bg-emerald-100 text-emerald-600 flex items-center justify-center font-bold">
                <ShieldCheck className="w-5 h-5" />
              </div>
              <h3 className="text-lg font-bold text-gray-900">Kiến trúc tin cậy</h3>
              <p className="text-sm text-gray-500 leading-relaxed">
                Xây dựng theo chuẩn Clean Architecture, bảo mật JWT + PBKDF2 100.000 iterations và cache Redis tốc độ cao.
              </p>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
