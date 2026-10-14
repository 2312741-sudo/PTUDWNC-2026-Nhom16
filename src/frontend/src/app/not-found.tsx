import Link from 'next/link';
import { ChefHat, Home, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';

export default function NotFound() {
  return (
    <div className="min-h-[70vh] flex items-center justify-center py-16 px-4">
      <div className="max-w-md w-full text-center bg-white p-8 sm:p-12 rounded-3xl border border-gray-100 shadow-xl shadow-emerald-500/5">
        <div className="w-20 h-20 mx-auto mb-6 rounded-3xl bg-emerald-50 flex items-center justify-center text-emerald-600 ring-8 ring-emerald-50/50">
          <ChefHat className="w-10 h-10" />
        </div>

        <span className="inline-block px-3 py-1 rounded-full text-xs font-bold bg-emerald-100 text-emerald-800 tracking-wider uppercase mb-3">
          404 — Không tìm thấy trang
        </span>

        <h1 className="text-2xl sm:text-3xl font-extrabold text-gray-900 tracking-tight mb-3">
          Món ăn này chưa được lên thực đơn!
        </h1>

        <p className="text-sm text-gray-500 leading-relaxed mb-8">
          Đường dẫn bạn vừa truy cập không tồn tại, đã bị đổi tên hoặc được đầu bếp dọn sang vị trí khác.
        </p>

        <div className="flex flex-col sm:flex-row items-center justify-center gap-3">
          <Button asChild className="w-full sm:w-auto gap-2">
            <Link href="/">
              <Home className="w-4 h-4" />
              Về trang chủ
            </Link>
          </Button>

          <Button variant="outline" asChild className="w-full sm:w-auto gap-2">
            <Link href="/recipes">
              <Search className="w-4 h-4" />
              Khám phá món khác
            </Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
