import Link from 'next/link';
import { Utensils, Sparkles, Plus, Clock, ChefHat } from 'lucide-react';

export default function RecipesPage() {
  return (
    <div className="min-h-screen bg-gray-50/50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="text-center max-w-2xl mx-auto mb-12">
          <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-orange-50 border border-orange-100 text-orange-700 text-xs font-semibold mb-4">
            <Sparkles className="w-3.5 h-3.5" />
            Khám phá Ẩm thực
          </div>
          <h1 className="text-3xl sm:text-4xl font-extrabold text-gray-900 tracking-tight">
            Kho Công Thức Nấu Ăn
          </h1>
          <p className="mt-3 text-gray-600 text-sm sm:text-base">
            Hàng ngàn công thức nấu ăn ngon miệng, hướng dẫn từng bước chi tiết và thông tin dinh dưỡng chuẩn xác từ cộng đồng.
          </p>
        </div>

        {/* Empty state / Feature in progress note */}
        <div className="bg-white rounded-3xl p-10 border border-gray-100 shadow-sm text-center max-w-xl mx-auto">
          <div className="w-16 h-16 rounded-2xl bg-orange-50 flex items-center justify-center text-orange-600 mx-auto mb-5">
            <Utensils className="w-8 h-8" />
          </div>
          <h2 className="text-xl font-bold text-gray-900 mb-2">
            Tính năng đang được hoàn thiện
          </h2>
          <p className="text-sm text-gray-500 mb-6 leading-relaxed">
            Phân hệ Danh sách công thức, Tìm kiếm Full-Text Search (FTS) và Bộ lọc nâng cao đang được các thành viên (TV2 & TV3) hoàn thiện theo tiến độ Tuần 2 & 3.
          </p>
          <div className="flex flex-col sm:flex-row items-center justify-center gap-3">
            <Link
              href="/categories"
              className="w-full sm:w-auto px-5 py-2.5 rounded-xl bg-orange-600 hover:bg-orange-700 text-white font-medium text-sm transition-colors shadow-sm"
            >
              Xem danh mục món ăn
            </Link>
            <Link
              href="/dashboard/profile"
              className="w-full sm:w-auto px-5 py-2.5 rounded-xl border border-gray-200 hover:bg-gray-50 text-gray-700 font-medium text-sm transition-colors"
            >
              Hồ sơ của tôi
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
