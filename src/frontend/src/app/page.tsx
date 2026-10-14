import Link from 'next/link';
import { getCategories, getRecipes } from '@/lib/api';
import CategoryCard from '@/components/CategoryCard';
import RecipeCard from '@/components/RecipeCard';
import { Button } from '@/components/ui/button';
import {
  Sparkles,
  Search,
  BookOpen,
  ArrowRight,
  ShieldCheck,
  Flame,
  ChefHat,
  UtensilsCrossed,
  Clock,
  Heart,
  Mail,
  Award,
} from 'lucide-react';

export default async function HomePage() {
  const [categories, recipesResult] = await Promise.all([
    getCategories(),
    getRecipes({ pageSize: 6, sortBy: 'createdAt', sortOrder: 'desc' }),
  ]);

  const recentRecipes = recipesResult.data || [];

  return (
    <div className="space-y-20 pb-16">
      {/* 1. Hero Section */}
      <section className="relative overflow-hidden bg-gradient-to-b from-emerald-50/80 via-white to-gray-50/40 py-20 lg:py-28 border-b border-gray-100">
        <div className="absolute inset-0 bg-[radial-gradient(#10b981_1px,transparent_1px)] [background-size:24px_24px] opacity-[0.12] pointer-events-none" />

        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center relative z-10">
          <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-emerald-100/90 text-emerald-900 text-xs font-bold mb-6 shadow-sm border border-emerald-300/40 animate-in fade-in slide-in-from-top-3">
            <Sparkles className="w-4 h-4 text-emerald-600 animate-pulse" />
            <span>Nền tảng Ẩm thực & Blog Công thức Chuẩn vị Việt Nam</span>
          </div>

          <h1 className="text-4xl sm:text-5xl lg:text-6xl font-black text-gray-950 tracking-tight max-w-4xl mx-auto leading-tight sm:leading-none">
            Khơi nguồn đam mê ẩm thực cùng{' '}
            <span className="text-transparent bg-clip-text bg-gradient-to-r from-emerald-600 via-teal-600 to-amber-600">
              hàng trăm công thức
            </span>{' '}
            đỉnh cao
          </h1>

          <p className="mt-6 text-base sm:text-lg text-gray-600 max-w-2xl mx-auto leading-relaxed">
            Khám phá hương vị truyền thống & hiện đại, nguyên liệu định lượng chuẩn xác, quy trình chi tiết từng bước và bảng giá trị dinh dưỡng minh bạch.
          </p>

          {/* Search bar banner */}
          <div className="mt-8 max-w-xl mx-auto">
            <form action="/search" method="GET" className="relative flex items-center shadow-xl shadow-emerald-600/10 rounded-2xl">
              <Search className="w-5 h-5 text-gray-400 absolute left-4 pointer-events-none" />
              <input
                type="text"
                name="q"
                placeholder="Tìm món ăn, nguyên liệu (VD: phở bò, cá kho, bánh mì)..."
                className="w-full pl-12 pr-32 py-4 text-sm bg-white border border-gray-200 rounded-2xl focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-transparent transition-all text-gray-800"
              />
              <button
                type="submit"
                className="absolute right-2 px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white text-sm font-semibold rounded-xl transition-all shadow-md shadow-emerald-600/20 active:scale-95 cursor-pointer"
              >
                Tìm kiếm
              </button>
            </form>
            <div className="flex flex-wrap items-center justify-center gap-2 mt-3 text-xs text-gray-500">
              <span className="font-semibold text-gray-700">Gợi ý tìm nhanh:</span>
              <Link href="/search?q=phở" className="px-2.5 py-1 rounded-full bg-white border border-gray-200 hover:text-emerald-700 hover:border-emerald-300">
                Phở bò
              </Link>
              <Link href="/search?q=kho" className="px-2.5 py-1 rounded-full bg-white border border-gray-200 hover:text-emerald-700 hover:border-emerald-300">
                Cá kho tộ
              </Link>
              <Link href="/search?q=bún" className="px-2.5 py-1 rounded-full bg-white border border-gray-200 hover:text-emerald-700 hover:border-emerald-300">
                Bún chả
              </Link>
              <Link href="/search?q=bánh" className="px-2.5 py-1 rounded-full bg-white border border-gray-200 hover:text-emerald-700 hover:border-emerald-300">
                Bánh ngọt
              </Link>
            </div>
          </div>

          {/* Quick CTA Actions */}
          <div className="mt-10 flex flex-wrap items-center justify-center gap-4">
            <Button size="lg" asChild className="gap-2 shadow-lg shadow-emerald-600/20">
              <Link href="/recipes">
                <BookOpen className="w-5 h-5" />
                Khám phá công thức
              </Link>
            </Button>

            <Button size="lg" variant="outline" asChild className="gap-2">
              <Link href="/categories">
                Xem danh mục
                <ArrowRight className="w-4 h-4 text-emerald-600" />
              </Link>
            </Button>
          </div>
        </div>
      </section>

      {/* 2. Featured Categories Grid */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col sm:flex-row sm:items-end justify-between mb-8 gap-4">
          <div>
            <div className="inline-flex items-center gap-1.5 text-xs font-bold text-emerald-700 tracking-wider uppercase mb-1">
              <UtensilsCrossed className="w-3.5 h-3.5" />
              Bộ sưu tập phong phú
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

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {categories.slice(0, 6).map((cat) => (
            <CategoryCard key={cat.id} category={cat} />
          ))}
        </div>
      </section>

      {/* 3. Trending & Latest Recipes Showcase */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col sm:flex-row sm:items-end justify-between mb-8 gap-4">
          <div>
            <div className="inline-flex items-center gap-1.5 text-xs font-bold text-amber-600 tracking-wider uppercase mb-1">
              <Flame className="w-3.5 h-3.5" />
              Mới cập nhật hôm nay
            </div>
            <h2 className="text-2xl sm:text-3xl font-extrabold text-gray-900 tracking-tight">
              Công thức nấu ăn & Bài viết blog mới nhất
            </h2>
          </div>

          <Link
            href="/recipes"
            className="text-sm font-semibold text-emerald-700 hover:text-emerald-800 flex items-center gap-1 group"
          >
            Xem toàn bộ món ngon{' '}
            <ArrowRight className="w-4 h-4 group-hover:translate-x-1 transition-transform" />
          </Link>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {recentRecipes.map((recipe) => (
            <RecipeCard key={recipe.id} recipe={recipe} />
          ))}
        </div>
      </section>

      {/* 4. Statistics Banner */}
      <section className="bg-gradient-to-r from-emerald-800 via-teal-800 to-emerald-900 text-white py-14 shadow-inner">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-8 text-center">
            <div className="space-y-1">
              <div className="text-3xl sm:text-4xl font-extrabold text-emerald-300">500+</div>
              <p className="text-xs sm:text-sm text-emerald-100 font-medium">Công thức chuẩn vị</p>
            </div>
            <div className="space-y-1">
              <div className="text-3xl sm:text-4xl font-extrabold text-amber-300">25+</div>
              <p className="text-xs sm:text-sm text-emerald-100 font-medium">Danh mục đa dạng</p>
            </div>
            <div className="space-y-1">
              <div className="text-3xl sm:text-4xl font-extrabold text-teal-300">10,000+</div>
              <p className="text-xs sm:text-sm text-emerald-100 font-medium">Đầu bếp & Độc giả</p>
            </div>
            <div className="space-y-1">
              <div className="text-3xl sm:text-4xl font-extrabold text-rose-300">4.9 ★</div>
              <p className="text-xs sm:text-sm text-emerald-100 font-medium">Đánh giá yêu thích</p>
            </div>
          </div>
        </div>
      </section>

      {/* 5. Features Grid */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="text-center max-w-2xl mx-auto mb-12">
          <h2 className="text-2xl sm:text-3xl font-extrabold text-gray-900 tracking-tight">
            Vì sao chọn Culinary Blog?
          </h2>
          <p className="mt-2 text-sm text-gray-600">
            Nền tảng công nghệ hiện đại mang lại trải nghiệm nấu nướng mượt mà, tiện lợi và an tâm nhất.
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          <div className="bg-white p-8 rounded-3xl border border-gray-100 shadow-sm hover:shadow-md transition-shadow space-y-4">
            <div className="w-12 h-12 rounded-2xl bg-emerald-50 text-emerald-700 flex items-center justify-center font-bold ring-8 ring-emerald-50/50">
              <Search className="w-6 h-6" />
            </div>
            <h3 className="text-lg font-bold text-gray-900">Tìm kiếm toàn văn FTS</h3>
            <p className="text-sm text-gray-500 leading-relaxed">
              Tích hợp PostgreSQL Full-Text Search qua unaccent và pg_trgm, gõ tiếng Việt không dấu vẫn tìm ra món ăn chỉ trong tích tắc.
            </p>
          </div>

          <div className="bg-white p-8 rounded-3xl border border-gray-100 shadow-sm hover:shadow-md transition-shadow space-y-4">
            <div className="w-12 h-12 rounded-2xl bg-amber-50 text-amber-700 flex items-center justify-center font-bold ring-8 ring-amber-50/50">
              <Clock className="w-6 h-6" />
            </div>
            <h3 className="text-lg font-bold text-gray-900">Chi tiết từng công đoạn</h3>
            <p className="text-sm text-gray-500 leading-relaxed">
              Hướng dẫn nấu nướng trực quan với ảnh minh họa, định lượng nguyên liệu rõ ràng và bộ hẹn giờ đếm ngược thông minh.
            </p>
          </div>

          <div className="bg-white p-8 rounded-3xl border border-gray-100 shadow-sm hover:shadow-md transition-shadow space-y-4">
            <div className="w-12 h-12 rounded-2xl bg-teal-50 text-teal-700 flex items-center justify-center font-bold ring-8 ring-teal-50/50">
              <ShieldCheck className="w-6 h-6" />
            </div>
            <h3 className="text-lg font-bold text-gray-900">Bảo mật & Tốc độ cao</h3>
            <p className="text-sm text-gray-500 leading-relaxed">
              Hệ thống xác thực JWT an toàn, bảo vệ mật khẩu với PBKDF2 100.000 iterations và tăng tốc tải trang nhờ Redis cache đa tầng.
            </p>
          </div>
        </div>
      </section>

      {/* 6. Newsletter Subscription */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="bg-gradient-to-br from-emerald-500 to-teal-700 rounded-3xl p-8 sm:p-14 text-white text-center shadow-xl shadow-emerald-500/10 relative overflow-hidden">
          <div className="relative z-10 max-w-2xl mx-auto space-y-4">
            <div className="w-14 h-14 mx-auto rounded-2xl bg-white/10 backdrop-blur-md flex items-center justify-center text-white ring-8 ring-white/10">
              <Mail className="w-7 h-7" />
            </div>
            <h2 className="text-2xl sm:text-3xl font-extrabold tracking-tight">
              Đăng ký nhận bí quyết nấu ngon mỗi tuần
            </h2>
            <p className="text-sm sm:text-base text-emerald-50 leading-relaxed">
              Cập nhật công thức theo mùa, mẹo bảo quản thực phẩm và thực đơn gia đình lành mạnh trực tiếp vào hộp thư của bạn.
            </p>
            <form onSubmit={(e) => e.preventDefault()} className="flex flex-col sm:flex-row gap-2.5 max-w-md mx-auto pt-2">
              <input
                type="email"
                placeholder="Nhập địa chỉ email của bạn..."
                className="flex-1 px-4 py-3 rounded-xl bg-white text-gray-900 placeholder:text-gray-400 text-sm focus:outline-none focus:ring-2 focus:ring-amber-400"
              />
              <button
                type="submit"
                className="px-6 py-3 rounded-xl bg-amber-500 hover:bg-amber-600 text-gray-950 font-bold text-sm transition-all shadow-md active:scale-95 cursor-pointer"
              >
                Đăng ký ngay
              </button>
            </form>
            <p className="text-[11px] text-emerald-100 opacity-80">
              Cam kết không gửi thư rác. Bạn có thể hủy đăng ký bất kỳ lúc nào.
            </p>
          </div>
        </div>
      </section>
    </div>
  );
}
