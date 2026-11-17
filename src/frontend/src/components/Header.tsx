'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { Search, Menu, X, Shield, BookOpen, Plus, LogOut, Users, Heart, User } from 'lucide-react';
import { useAuthStore } from '@/lib/auth-store';
import { useFavorites } from '@/lib/favorites';

export default function Header() {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const { isAuthenticated: loggedIn, logout: storeLogout, user } = useAuthStore();
  const isAdmin = loggedIn && !!user?.roles?.some((r) => r.toLowerCase() === 'admin');
  const { count: favCount, isLoaded: favLoaded } = useFavorites();
  const pathname = usePathname();
  const router = useRouter();

  function logout() {
    storeLogout();
    setIsMobileMenuOpen(false);
    router.push('/auth/login');
  }

  // Các nút điều hướng dạng "pill" — dùng chung cho hàng điều hướng thứ hai (từ md trở lên).
  const pillBase =
    'flex items-center gap-1.5 px-3 py-1.5 rounded-lg transition-colors text-xs font-semibold whitespace-nowrap';

  return (
    <header className="sticky top-0 z-50 bg-white/95 backdrop-blur border-b border-gray-100 shadow-sm">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Hàng 1: logo + ô tìm kiếm + tài khoản. Không đặt nav ở đây nữa vì khi đăng nhập
            có tới 8 mục điều hướng, hàng bị tràn và ô tìm kiếm bị bóp đè lên chữ "Trang chủ". */}
        <div className="flex items-center gap-3 h-16">
          {/* Logo & Brand */}
          <Link href="/" className="flex items-center gap-3 group shrink-0">
            <div className="relative w-10 h-10 rounded-xl overflow-hidden shadow-md shadow-emerald-200/50 ring-2 ring-emerald-600/30 group-hover:ring-emerald-600 transition-all duration-300 group-hover:scale-105 shrink-0 bg-emerald-50">
              <img
                src="/logo.jpg"
                alt="Culinary Blog Logo"
                className="w-full h-full object-cover"
              />
            </div>
            <div className="flex flex-col whitespace-nowrap">
              <span className="text-xl font-extrabold tracking-tight text-gray-900 group-hover:text-emerald-700 transition-colors">
                Culinary<span className="text-emerald-600">Blog</span>
              </span>
              <span className="hidden sm:block text-[10px] text-emerald-800/70 font-semibold tracking-wider uppercase">
                Khám phá & Chia sẻ ẩm thực
              </span>
            </div>
          </Link>

          {/* Desktop Search Bar — có nút bấm riêng, không chỉ dựa vào phím Enter */}
          <div className="hidden md:flex flex-1 min-w-[12rem] max-w-xl mx-2 lg:mx-6">
            <form
              action="/search"
              method="GET"
              role="search"
              className="w-full relative flex items-center"
            >
              <Search className="w-4 h-4 text-gray-400 absolute left-3 pointer-events-none" />
              <input
                type="search"
                name="q"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Tìm công thức, nguyên liệu (VD: phở bò)..."
                aria-label="Tìm công thức"
                className="w-full pl-9 pr-24 py-2 text-sm bg-gray-50 border border-gray-200 rounded-full focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:bg-white transition-all text-gray-800"
              />
              <button
                type="submit"
                className="absolute right-1 top-1 bottom-1 px-3 rounded-full bg-emerald-600 text-white text-xs font-semibold hover:bg-emerald-700 transition-colors"
              >
                Tìm kiếm
              </button>
            </form>
          </div>

          {/* Auth Buttons — dưới lg chỉ hiện biểu tượng (vẫn giữ tên truy cập qua sr-only) */}
          <div className="hidden sm:flex items-center gap-2 whitespace-nowrap ml-auto shrink-0">
            {loggedIn ? (
              <>
                <Link
                  href="/dashboard/recipes/new"
                  className="flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-white bg-emerald-600 rounded-xl hover:bg-emerald-700 shadow-sm shadow-emerald-200 transition-all"
                >
                  <Plus className="w-4 h-4" />
                  <span className="sr-only lg:not-sr-only">Viết công thức</span>
                </Link>
                <Link
                  href="/dashboard/profile"
                  aria-current={pathname?.startsWith('/dashboard/profile') ? 'page' : undefined}
                  className="flex items-center gap-2 px-3 py-2 text-sm font-medium text-gray-700 hover:text-emerald-700 bg-gray-50 hover:bg-emerald-50/70 border border-gray-200 hover:border-emerald-200 rounded-xl transition-all"
                  title="Hồ sơ cá nhân"
                >
                  {user?.avatarUrl ? (
                    <img
                      src={user.avatarUrl}
                      alt={user?.displayName || 'Avatar'}
                      className="w-5 h-5 rounded-full object-cover border border-gray-300"
                    />
                  ) : (
                    <User className="w-4 h-4 text-emerald-600" />
                  )}
                  <span className="max-w-[8rem] xl:max-w-[11rem] truncate">
                    {user?.displayName || user?.fullName || 'Hồ sơ'}
                  </span>
                </Link>
                <button
                  onClick={logout}
                  title="Đăng xuất"
                  className="flex items-center gap-1.5 px-2.5 py-2 text-sm font-medium text-gray-600 hover:text-red-600 transition-colors"
                >
                  <LogOut className="w-4 h-4" />
                  <span className="sr-only lg:not-sr-only">Đăng xuất</span>
                </button>
              </>
            ) : (
              <>
                <Link
                  href="/auth/login"
                  className="px-4 py-2 text-sm font-medium text-gray-700 hover:text-emerald-600 transition-colors"
                >
                  Đăng nhập
                </Link>
                <Link
                  href="/auth/register"
                  className="px-4 py-2 text-sm font-medium text-white bg-emerald-600 rounded-xl hover:bg-emerald-700 shadow-sm shadow-emerald-200 transition-all"
                >
                  Đăng ký
                </Link>
              </>
            )}
          </div>

          {/* Mobile Menu Button */}
          <div className="flex md:hidden items-center ml-auto sm:ml-0">
            <button
              onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
              className="p-2 text-gray-600 hover:text-gray-900 focus:outline-none"
              aria-label="Toggle Navigation Menu"
              aria-expanded={isMobileMenuOpen}
            >
              {isMobileMenuOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
            </button>
          </div>
        </div>

        {/* Hàng 2 (từ md trở lên): điều hướng. Trước đây nav chỉ hiện từ lg nên khoảng
            768–1023px không có đường nào tới các trang (menu di động cũng ẩn từ md). */}
        <nav
          aria-label="Điều hướng chính"
          className="hidden md:flex flex-wrap items-center gap-x-5 gap-y-2 py-2 border-t border-gray-100 text-sm font-medium text-gray-700"
        >
          <Link href="/" aria-current={pathname === '/' ? 'page' : undefined} className="whitespace-nowrap hover:text-emerald-600 transition-colors">
            Trang chủ
          </Link>
          <Link href="/recipes" aria-current={pathname?.startsWith('/recipes') ? 'page' : undefined} className="whitespace-nowrap hover:text-emerald-600 transition-colors">
            Công thức
          </Link>
          <Link href="/categories" aria-current={pathname?.startsWith('/categories') ? 'page' : undefined} className="whitespace-nowrap hover:text-emerald-600 transition-colors">
            Danh mục
          </Link>
          <div className="flex flex-wrap items-center gap-2 ml-auto">
            {loggedIn && (
              <Link
                href="/dashboard/recipes"
                aria-current={pathname?.startsWith('/dashboard/recipes') ? 'page' : undefined}
                className={`${pillBase} bg-emerald-50 text-emerald-800 hover:bg-emerald-100`}
              >
                <BookOpen className="w-3.5 h-3.5 text-emerald-600" />
                Công thức của tôi
              </Link>
            )}
            {isAdmin && (
              <>
                <Link
                  href="/dashboard/categories"
                  className={`${pillBase} bg-amber-50 text-amber-800 hover:bg-amber-100`}
                >
                  <Shield className="w-3.5 h-3.5 text-amber-600" />
                  Quản trị DM
                </Link>
                <Link
                  href="/dashboard/users"
                  className={`${pillBase} bg-blue-50 text-blue-800 hover:bg-blue-100`}
                >
                  <Users className="w-3.5 h-3.5 text-blue-600" />
                  Người dùng
                </Link>
              </>
            )}
            <Link
              href="/dashboard/favorites"
              aria-current={pathname?.startsWith('/dashboard/favorites') ? 'page' : undefined}
              className={`${pillBase} bg-rose-50 text-rose-800 hover:bg-rose-100`}
            >
              <Heart className="w-3.5 h-3.5 text-rose-600 fill-rose-500" />
              <span>Yêu thích</span>
              {favLoaded && favCount > 0 && (
                <span className="px-1.5 py-0.2 rounded-full text-[10px] bg-rose-500 text-white font-bold leading-tight">
                  {favCount}
                </span>
              )}
            </Link>
          </div>
        </nav>
      </div>

      {/* Mobile Drawer */}
      {isMobileMenuOpen && (
        <div className="md:hidden border-t border-gray-100 bg-white px-4 pt-3 pb-6 space-y-3">
          <form action="/search" method="GET" className="relative flex items-center mb-3">
            <Search className="w-4 h-4 text-gray-400 absolute left-3" />
            <input
              type="text"
              name="q"
              placeholder="Tìm món ăn..."
              className="w-full pl-9 pr-4 py-2 text-sm bg-gray-50 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </form>
          <div className="flex flex-col gap-2 font-medium text-gray-700">
            <Link
              href="/"
              onClick={() => setIsMobileMenuOpen(false)}
              className="px-3 py-2 rounded-lg hover:bg-emerald-50 hover:text-emerald-700"
            >
              Trang chủ
            </Link>
            <Link
              href="/recipes"
              onClick={() => setIsMobileMenuOpen(false)}
              className="px-3 py-2 rounded-lg hover:bg-emerald-50 hover:text-emerald-700"
            >
              Công thức
            </Link>
            <Link
              href="/categories"
              onClick={() => setIsMobileMenuOpen(false)}
              className="px-3 py-2 rounded-lg hover:bg-emerald-50 hover:text-emerald-700"
            >
              Danh mục
            </Link>
            {loggedIn && (
              <>
                <Link
                  href="/dashboard/profile"
                  onClick={() => setIsMobileMenuOpen(false)}
                  className="flex items-center gap-2 px-3 py-2 rounded-lg bg-gray-100 text-gray-800 font-medium hover:bg-gray-200"
                >
                  {user?.avatarUrl ? (
                    <img
                      src={user.avatarUrl}
                      alt={user?.displayName || 'Avatar'}
                      className="w-5 h-5 rounded-full object-cover"
                    />
                  ) : (
                    <User className="w-4 h-4 text-emerald-600" />
                  )}
                  <span>Hồ sơ cá nhân ({user?.displayName || user?.fullName || 'Tài khoản'})</span>
                </Link>
                <Link
                  href="/dashboard/recipes"
                  onClick={() => setIsMobileMenuOpen(false)}
                  className="px-3 py-2 rounded-lg bg-emerald-50 text-emerald-800"
                >
                  Công thức của tôi
                </Link>
                <Link
                  href="/dashboard/recipes/new"
                  onClick={() => setIsMobileMenuOpen(false)}
                  className="px-3 py-2 rounded-lg hover:bg-emerald-50 hover:text-emerald-700"
                >
                  + Viết công thức
                </Link>
              </>
            )}
            {isAdmin && (
              <>
                <Link
                  href="/dashboard/categories"
                  onClick={() => setIsMobileMenuOpen(false)}
                  className="px-3 py-2 rounded-lg bg-amber-50 text-amber-800"
                >
                  Quản trị Danh mục
                </Link>
                <Link
                  href="/dashboard/users"
                  onClick={() => setIsMobileMenuOpen(false)}
                  className="px-3 py-2 rounded-lg bg-blue-50 text-blue-800"
                >
                  Người dùng & Tác giả
                </Link>
              </>
            )}
            <Link
              href="/dashboard/favorites"
              onClick={() => setIsMobileMenuOpen(false)}
              className="flex items-center justify-between px-3 py-2 rounded-lg bg-rose-50 text-rose-800"
            >
              <span className="flex items-center gap-2">
                <Heart className="w-4 h-4 text-rose-600 fill-rose-500" />
                Công thức đã lưu
              </span>
              {favLoaded && favCount > 0 && (
                <span className="px-2 py-0.5 rounded-full text-xs bg-rose-500 text-white font-bold">
                  {favCount}
                </span>
              )}
            </Link>
          </div>
          <div className="pt-3 border-t border-gray-100 flex gap-2">
            {loggedIn ? (
              <button
                onClick={logout}
                className="flex-1 text-center py-2 border border-gray-200 rounded-lg text-sm font-medium text-red-600 hover:bg-red-50"
              >
                Đăng xuất
              </button>
            ) : (
              <>
                <Link
                  href="/auth/login"
                  className="flex-1 text-center py-2 border border-gray-200 rounded-lg text-sm font-medium hover:bg-gray-50"
                >
                  Đăng nhập
                </Link>
                <Link
                  href="/auth/register"
                  className="flex-1 text-center py-2 bg-emerald-600 text-white rounded-lg text-sm font-medium hover:bg-emerald-700 shadow-sm shadow-emerald-200"
                >
                  Đăng ký
                </Link>
              </>
            )}
          </div>
        </div>
      )}
    </header>
  );
}