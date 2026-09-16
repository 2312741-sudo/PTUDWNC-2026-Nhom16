'use client';

import { useEffect, useState } from 'react';
import { Category, CreateCategoryRequest } from '@/types/category';
import { getCategories, createCategory, deleteCategory } from '@/lib/api';
import { Plus, Trash2, Edit3, ShieldAlert, CheckCircle, AlertCircle, RefreshCw } from 'lucide-react';

export default function AdminCategoriesDashboard() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  // Form State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [imageUrl, setImageUrl] = useState('');
  const [orderIndex, setOrderIndex] = useState(0);
  const [submitting, setSubmitting] = useState(false);

  // Load Categories
  const fetchCategories = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getCategories();
      setCategories(data);
    } catch {
      setError('Không thể tải danh sách danh mục từ API.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCategories();
  }, []);

  // Handle Create Category
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim() || name.length < 2 || name.length > 100) {
      setError('Tên danh mục phải có từ 2 đến 100 ký tự.');
      return;
    }

    setSubmitting(true);
    setError(null);
    setSuccessMsg(null);

    // Mock Admin token or empty (backend will validate AdminPolicy in integration)
    const token = typeof window !== 'undefined' ? localStorage.getItem('token') || '' : '';

    const payload: CreateCategoryRequest = {
      name: name.trim(),
      description: description.trim() || undefined,
      imageUrl: imageUrl.trim() || undefined,
      orderIndex: Number(orderIndex) || 0,
    };

    const res = await createCategory(payload, token);
    setSubmitting(false);

    if (!res.success) {
      setError(res.error || 'Có lỗi xảy ra khi tạo danh mục.');
    } else {
      setSuccessMsg(`Tạo danh mục "${res.data?.name}" thành công!`);
      setIsModalOpen(false);
      setName('');
      setDescription('');
      setImageUrl('');
      setOrderIndex(0);
      fetchCategories();
    }
  };

  // Handle Delete Category
  const handleDelete = async (cat: Category) => {
    if (!confirm(`Bạn có chắc chắn muốn xóa danh mục "${cat.name}"?`)) return;

    setError(null);
    setSuccessMsg(null);

    const token = typeof window !== 'undefined' ? localStorage.getItem('token') || '' : '';
    const res = await deleteCategory(cat.id, token);

    if (!res.success) {
      // Handles 409 Conflict when category has recipes (FR-CAT-005)
      setError(res.error || 'Không thể xóa danh mục.');
    } else {
      setSuccessMsg(`Đã xóa danh mục "${cat.name}" thành công.`);
      fetchCategories();
    }
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-gray-100 pb-6">
        <div>
          <div className="flex items-center gap-2 text-xs font-bold text-amber-700 uppercase tracking-wider mb-1">
            <ShieldAlert className="w-4 h-4" />
            <span>Khu vực Quản trị viên (Admin Area)</span>
          </div>
          <h1 className="text-3xl font-extrabold text-gray-900 tracking-tight">
            Quản lý danh mục món ăn
          </h1>
          <p className="text-sm text-gray-500 mt-1">
            Quản trị các danh mục, thứ tự hiển thị navigation và liên kết công thức (FR-CAT-001...005).
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={fetchCategories}
            className="p-2.5 rounded-xl border border-gray-200 text-gray-600 hover:bg-gray-50 transition-colors"
            title="Tải lại danh sách"
          >
            <RefreshCw className="w-4 h-4" />
          </button>

          <button
            onClick={() => {
              setError(null);
              setSuccessMsg(null);
              setIsModalOpen(true);
            }}
            className="px-5 py-2.5 bg-orange-600 hover:bg-orange-700 text-white text-sm font-semibold rounded-xl shadow-md shadow-orange-200 flex items-center gap-2 transition-all"
          >
            <Plus className="w-4 h-4" />
            Thêm danh mục mới
          </button>
        </div>
      </div>

      {/* Notifications */}
      {error && (
        <div className="p-4 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm flex items-center gap-3 animate-fadeIn">
          <AlertCircle className="w-5 h-5 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {successMsg && (
        <div className="p-4 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-700 text-sm flex items-center gap-3 animate-fadeIn">
          <CheckCircle className="w-5 h-5 flex-shrink-0" />
          <span>{successMsg}</span>
        </div>
      )}

      {/* Table of Categories */}
      <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden">
        {loading ? (
          <div className="text-center py-16 text-gray-400 text-sm">
            Đang tải dữ liệu danh mục...
          </div>
        ) : categories.length === 0 ? (
          <div className="text-center py-16 text-gray-400 text-sm">
            Chưa có danh mục nào trong hệ thống. Nhấn &quot;Thêm danh mục mới&quot; để tạo.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-gray-600">
              <thead className="bg-gray-50 text-gray-700 text-xs font-semibold uppercase tracking-wider border-b border-gray-100">
                <tr>
                  <th className="py-4 px-6">Thứ tự</th>
                  <th className="py-4 px-6">Tên danh mục</th>
                  <th className="py-4 px-6">Slug URL</th>
                  <th className="py-4 px-6">Số món ăn</th>
                  <th className="py-4 px-6 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 font-medium">
                {categories.map((cat) => (
                  <tr key={cat.id} className="hover:bg-gray-50/80 transition-colors">
                    <td className="py-4 px-6 font-bold text-gray-900">{cat.orderIndex}</td>
                    <td className="py-4 px-6 text-gray-900 font-semibold">{cat.name}</td>
                    <td className="py-4 px-6 text-gray-500 font-mono text-xs">{cat.slug}</td>
                    <td className="py-4 px-6">
                      <span className="px-2.5 py-1 rounded-full text-xs font-semibold bg-orange-50 text-orange-700">
                        {cat.recipesCount} món
                      </span>
                    </td>
                    <td className="py-4 px-6 text-right space-x-2">
                      <button
                        onClick={() => handleDelete(cat)}
                        className="p-2 rounded-lg text-red-600 hover:bg-red-50 transition-colors"
                        title="Xóa danh mục"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Modal Dialog for Creating Category */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4">
          <div className="bg-white rounded-3xl max-w-lg w-full p-8 shadow-2xl space-y-6 animate-scaleIn">
            <div className="flex items-center justify-between border-b border-gray-100 pb-4">
              <h2 className="text-xl font-bold text-gray-900">Thêm danh mục mới</h2>
              <button
                onClick={() => setIsModalOpen(false)}
                className="text-gray-400 hover:text-gray-600 text-lg font-bold"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">
                  Tên danh mục <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  required
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="Ví dụ: Món nướng BBQ, Món cuốn..."
                  className="w-full px-4 py-2.5 text-sm border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-orange-500"
                />
                <span className="text-[11px] text-gray-400 mt-1 block">
                  Độ dài từ 2 đến 100 ký tự. Slug sẽ được tự động tạo.
                </span>
              </div>

              <div>
                <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">
                  Mô tả danh mục
                </label>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={3}
                  placeholder="Mô tả ngắn gọn về nhóm món ăn..."
                  className="w-full px-4 py-2.5 text-sm border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-orange-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">
                    Thứ tự hiển thị
                  </label>
                  <input
                    type="number"
                    min={0}
                    value={orderIndex}
                    onChange={(e) => setOrderIndex(Number(e.target.value))}
                    className="w-full px-4 py-2.5 text-sm border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-orange-500"
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">
                    Ảnh đại diện URL
                  </label>
                  <input
                    type="url"
                    value={imageUrl}
                    onChange={(e) => setImageUrl(e.target.value)}
                    placeholder="https://..."
                    className="w-full px-4 py-2.5 text-sm border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-orange-500"
                  />
                </div>
              </div>

              <div className="pt-4 flex items-center justify-end gap-3 border-t border-gray-100">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="px-5 py-2.5 text-sm font-medium text-gray-600 hover:bg-gray-100 rounded-xl transition-colors"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-6 py-2.5 text-sm font-semibold text-white bg-orange-600 hover:bg-orange-700 rounded-xl shadow-md shadow-orange-200 transition-all disabled:opacity-50"
                >
                  {submitting ? 'Đang lưu...' : 'Lưu danh mục'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
