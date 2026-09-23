import Link from 'next/link';
import { UtensilsCrossed, Heart } from 'lucide-react';

export default function Footer() {
  return (
    <footer className="bg-gray-900 text-gray-300 border-t border-gray-800 mt-20">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
          {/* Brand */}
          <div className="space-y-4 md:col-span-2">
            <div className="flex items-center gap-3">
              <div className="relative w-8 h-8 rounded-lg overflow-hidden shadow-sm ring-1 ring-emerald-500/30 shrink-0 bg-emerald-950">
                <img
                  src="/logo.jpg"
                  alt="Culinary Blog Logo"
                  className="w-full h-full object-cover"
                />
              </div>
              <span className="text-xl font-extrabold text-white tracking-tight">
                Culinary<span className="text-emerald-400">Blog</span>
              </span>
            </div>
            <p className="text-sm text-gray-400 max-w-sm">
              Nền tảng chia sẻ và khám phá tinh hoa ẩm thực trực tuyến. Xây dựng bằng .NET 10 Clean Architecture và Next.js App Router.
            </p>
            <p className="text-xs text-gray-500">
              Đồ án môn Phát triển ứng dụng Web nâng cao — Nhóm 16.
            </p>
          </div>

          {/* Quick Links */}
          <div>
            <h3 className="text-sm font-semibold text-white uppercase tracking-wider mb-4">
              Khám phá
            </h3>
            <ul className="space-y-2 text-sm">
              <li>
                <Link href="/" className="hover:text-white transition-colors">
                  Trang chủ
                </Link>
              </li>
              <li>
                <Link href="/recipes" className="hover:text-white transition-colors">
                  Tất cả công thức
                </Link>
              </li>
              <li>
                <Link href="/categories" className="hover:text-white transition-colors">
                  Danh mục món ăn
                </Link>
              </li>
              <li>
                <Link href="/search" className="hover:text-white transition-colors">
                  Tìm kiếm thông minh
                </Link>
              </li>
            </ul>
          </div>

          {/* Legal / Dev info */}
          <div>
            <h3 className="text-sm font-semibold text-white uppercase tracking-wider mb-4">
              Thành viên nhóm
            </h3>
            <ul className="space-y-2 text-xs text-gray-400">
              <li>TV1: Nguyễn Thanh Tâm (Leader)</li>
              <li>TV2: Ngô Quốc Trường Vĩ (Discovery/Search)</li>
              <li>TV3: Huỳnh Quốc Trung (Recipe aggregate)</li>
              <li>TV4: Nguyễn Hữu Trung Sơn (Publish & Media)</li>
            </ul>
          </div>
        </div>

        <div className="border-t border-gray-800 mt-12 pt-6 flex flex-col sm:flex-row items-center justify-between text-xs text-gray-500 gap-4">
          <p>© 2026 Culinary Blog. Tất cả quyền được bảo lưu.</p>
          <p className="flex items-center gap-1">
            Được phát triển với <Heart className="w-3.5 h-3.5 text-red-500 fill-red-500" /> bởi Nhóm 16.
          </p>
        </div>
      </div>
    </footer>
  );
}
