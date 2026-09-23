'use client';

import Link from 'next/link';
import { useState } from 'react';
import { UtensilsCrossed, Search, Menu, X, Shield } from 'lucide-react';

export default function Header() {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');

  return (
    <header className="sticky top-0 z-50 bg-white/95 backdrop-blur border-b border-gray-100 shadow-sm">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Logo & Brand */}
          <Link href="/" className="flex items-center gap-3 group">
            <div className="relative w-10 h-10 rounded-xl overflow-hidden shadow-md shadow-emerald-200/50 ring-2 ring-emerald-600/30 group-hover:ring-emerald-600 transition-all duration-300 group-hover:scale-105 shrink-0 bg-emerald-50">
              <img
                src="/logo.jpg"
                alt="Culinary Blog Logo"
                className="w-full h-full object-cover"
              />
            </div>
            <div className="flex flex-col">
              <span className="text-xl font-extrabold tracking-tight text-gray-900 group-hover:text-emerald-700 transition-colors">
                Culinary<span className="text-emerald-600">Blog</span>
              </span>
              <span className="text-[10px] text-emerald-800/70 font-semibold tracking-wider uppercase">
                Khám phá & Chia sẻ ẩm thực
              </span>
            </div>
          </Link>

          {/* Desktop Search Bar */}
          <div className="hidden md:flex flex-1 max-w-md mx-8">
            <form
              action="/search"
              method="GET"
              className="w-full relative flex items-center"
            >
              <Search className="w-4 h-4 text-gray-400 absolute left-3 pointer-events-none" />
              <input
                type="text"
                name="q"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Tìm công thức, nguyên liệu (VD: phở bò, bún chả)..."
                className="w-full pl-9 pr-4 py-2 text-sm bg-gray-50 border border-gray-200 rounded-full focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:bg-white transition-all text-gray-800"
              />
            </form>
          </div>

          {/* Desktop Navigation Links */}
          <nav className="hidden lg:flex items-center gap-6 text-sm font-medium text-gray-700">
            <Link href="/" className="hover:text-emerald-600 transition-colors">
              Trang chủ
            </Link>
            <Link href="/recipes" className="hover:text-emerald-600 transition-colors">
              Công thức
            </Link>
            <Link href="/categories" className="hover:text-emerald-600 transition-colors">
              Danh mục
            </Link>
            <Link
              href="/dashboard/categories"
              className="flex items-center gap-1.5 px-3 py-1.5 bg-amber-50 text-amber-800 rounded-lg hover:bg-amber-100 transition-colors text-xs font-semibold"
            >
              <Shield className="w-3.5 h-3.5 text-amber-600" />
              Quản trị DM
            </Link>
            <Link
              href="/dashboard/profile"
              className="flex items-center gap-1.5 px-3 py-1.5 bg-neutral-100 text-neutral-800 rounded-lg hover:bg-neutral-200 transition-colors text-xs font-semibold"
            >
              Hồ sơ (A3)
            </Link>
          </nav>

          {/* Auth Buttons */}
          <div className="hidden sm:flex items-center gap-3">
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
          </div>

          {/* Mobile Menu Button */}
          <div className="flex md:hidden items-center">
            <button
              onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
              className="p-2 text-gray-600 hover:text-gray-900 focus:outline-none"
              aria-label="Toggle Navigation Menu"
            >
              {isMobileMenuOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
            </button>
          </div>
        </div>
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
            <Link
              href="/dashboard/categories"
              onClick={() => setIsMobileMenuOpen(false)}
              className="px-3 py-2 rounded-lg bg-amber-50 text-amber-800"
            >
              Quản trị Danh mục
            </Link>
          </div>
          <div className="pt-3 border-t border-gray-100 flex gap-2">
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
          </div>
        </div>
      )}
    </header>
  );
}
