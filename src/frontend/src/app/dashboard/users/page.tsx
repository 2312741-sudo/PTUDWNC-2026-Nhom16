'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { User } from '@/types/auth';
import { getUsers } from '@/lib/api';
import {
  Users,
  Shield,
  ChefHat,
  Search,
  Filter,
  Mail,
  Calendar,
  CheckCircle2,
  ExternalLink,
  UserCheck,
  Sparkles,
} from 'lucide-react';

const FALLBACK_USERS: User[] = [
  {
    id: 'u-admin-1',
    email: 'admin@culinaryblog.vn',
    fullName: 'Nguyễn Thanh Tâm',
    displayName: 'Nguyễn Thanh Tâm (Admin)',
    userName: 'admin@culinaryblog.vn',
    roles: ['Admin', 'Author'],
    avatarUrl: '/images/recipes/com-tam-suon-bi-cha.jpg',
    bio: 'Trưởng nhóm phát triển hệ thống Culinary Blog. Quản trị viên & Tác giả ẩm thực.',
    emailConfirmed: true,
    createdAt: '2026-09-01T08:00:00Z',
  },
  {
    id: 'u-author-2',
    email: 'truongvi@culinaryblog.vn',
    fullName: 'Ngô Quốc Trường Vĩ',
    displayName: 'Ngô Quốc Trường Vĩ',
    userName: 'truongvi@culinaryblog.vn',
    roles: ['Author'],
    avatarUrl: '/images/recipes/pho-bo-ha-noi.jpg',
    bio: 'Tác giả chuyên mục món ngon miền Bắc và các món canh thanh mát truyền thống.',
    emailConfirmed: true,
    createdAt: '2026-09-05T09:30:00Z',
  },
  {
    id: 'u-author-3',
    email: 'quoctrung@culinaryblog.vn',
    fullName: 'Huỳnh Quốc Trung',
    displayName: 'Huỳnh Quốc Trung',
    userName: 'quoctrung@culinaryblog.vn',
    roles: ['Author'],
    avatarUrl: '/images/recipes/bun-bo-hue.jpg',
    bio: 'Đầu bếp đam mê khám phá gia vị miền Trung và các món xào, kho đậm đà chuẩn vị.',
    emailConfirmed: true,
    createdAt: '2026-09-10T14:15:00Z',
  },
  {
    id: 'u-author-4',
    email: 'trungson@culinaryblog.vn',
    fullName: 'Nguyễn Hữu Trung Sơn',
    displayName: 'Nguyễn Hữu Trung Sơn',
    userName: 'trungson@culinaryblog.vn',
    roles: ['Author'],
    avatarUrl: '/images/recipes/banh-mi-thit-nuong.jpg',
    bio: 'Nhiếp ảnh gia ẩm thực và biên tập viên công thức món ăn vặt & thức uống tươi mát.',
    emailConfirmed: true,
    createdAt: '2026-09-12T11:00:00Z',
  },
  {
    id: 'u-member-5',
    email: 'huongvi.bepnha@gmail.com',
    fullName: 'Trần Thị Thu Thảo',
    displayName: 'Thu Thảo Bếp Nhà',
    userName: 'huongvi.bepnha@gmail.com',
    roles: ['Author'],
    avatarUrl: '/images/recipes/canh-chua-ca-loc.jpg',
    bio: 'Người nội trợ yêu thích các bữa cơm gia đình đầm ấm, dinh dưỡng và tiết kiệm.',
    emailConfirmed: true,
    createdAt: '2026-09-20T16:45:00Z',
  },
];

export default function UsersDirectoryPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [roleFilter, setRoleFilter] = useState<string>('all');

  useEffect(() => {
    async function loadData() {
      setLoading(true);
      const apiUsers = await getUsers();
      if (apiUsers && apiUsers.length > 0) {
        setUsers(apiUsers);
      } else {
        setUsers(FALLBACK_USERS);
      }
      setLoading(false);
    }
    loadData();
  }, []);

  const filteredUsers = users.filter((u) => {
    const nameMatch = (u.displayName || u.fullName || '')
      .toLowerCase()
      .includes(searchQuery.toLowerCase());
    const emailMatch = (u.email || '').toLowerCase().includes(searchQuery.toLowerCase());
    const queryPass = !searchQuery || nameMatch || emailMatch;

    if (!queryPass) return false;

    if (roleFilter === 'all') return true;
    if (roleFilter === 'Admin') return u.roles?.includes('Admin');
    if (roleFilter === 'Author') return u.roles?.includes('Author');
    if (roleFilter === 'User') return !u.roles?.includes('Admin') && !u.roles?.includes('Author');
    return true;
  });

  const totalAdmins = users.filter((u) => u.roles?.includes('Admin')).length;
  const totalAuthors = users.filter((u) => u.roles?.includes('Author')).length;

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-gray-100 pb-6">
        <div>
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-emerald-50 text-emerald-800 text-xs font-semibold mb-2">
            <Users className="w-3.5 h-3.5 text-emerald-600" />
            <span>Phân hệ Quản trị & Tác giả (Lab 8)</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-gray-900 tracking-tight">
            Danh sách người dùng & tác giả
          </h1>
          <p className="text-sm text-gray-500 mt-1">
            Quản lý tài khoản thành viên, tác giả viết bài và ban quản trị nền tảng Culinary Blog.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Link
            href="/dashboard/profile"
            className="px-4 py-2.5 rounded-xl border border-gray-200 bg-white text-gray-700 text-sm font-semibold hover:bg-gray-50 transition-colors shadow-sm"
          >
            Hồ sơ cá nhân
          </Link>
          <Link
            href="/dashboard/recipes/new"
            className="px-4 py-2.5 rounded-xl bg-emerald-600 text-white text-sm font-semibold hover:bg-emerald-700 transition-colors shadow-md shadow-emerald-200"
          >
            Đăng công thức mới
          </Link>
        </div>
      </div>

      {/* Stats Overview */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-5">
        <div className="bg-white p-5 rounded-2xl border border-gray-100 shadow-sm flex items-center justify-between">
          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">Tổng thành viên</p>
            <p className="text-2xl font-extrabold text-gray-900 mt-1">{users.length}</p>
          </div>
          <div className="w-12 h-12 rounded-xl bg-gray-50 text-gray-600 flex items-center justify-center">
            <Users className="w-6 h-6" />
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-gray-100 shadow-sm flex items-center justify-between">
          <div>
            <p className="text-xs font-medium text-emerald-600 uppercase tracking-wider">Tác giả công thức</p>
            <p className="text-2xl font-extrabold text-emerald-700 mt-1">{totalAuthors}</p>
          </div>
          <div className="w-12 h-12 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center">
            <ChefHat className="w-6 h-6" />
          </div>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-gray-100 shadow-sm flex items-center justify-between">
          <div>
            <p className="text-xs font-medium text-purple-600 uppercase tracking-wider">Quản trị viên (Admin)</p>
            <p className="text-2xl font-extrabold text-purple-700 mt-1">{totalAdmins}</p>
          </div>
          <div className="w-12 h-12 rounded-xl bg-purple-50 text-purple-600 flex items-center justify-center">
            <Shield className="w-6 h-6" />
          </div>
        </div>
      </div>

      {/* Search & Filters */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-4 bg-gray-50/80 p-4 rounded-2xl border border-gray-100">
        <div className="relative w-full sm:w-80">
          <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Tìm theo tên hoặc email..."
            className="w-full pl-10 pr-4 py-2 rounded-xl bg-white border border-gray-200 text-sm text-gray-900 placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-emerald-500/20 focus:border-emerald-600"
          />
        </div>

        <div className="flex items-center gap-1.5 w-full sm:w-auto overflow-x-auto pb-1 sm:pb-0">
          {[
            { id: 'all', label: 'Tất cả' },
            { id: 'Admin', label: 'Quản trị viên' },
            { id: 'Author', label: 'Tác giả' },
            { id: 'User', label: 'Thành viên' },
          ].map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => setRoleFilter(tab.id)}
              className={`px-3.5 py-1.5 rounded-xl text-xs font-semibold whitespace-nowrap transition-colors cursor-pointer ${
                roleFilter === tab.id
                  ? 'bg-emerald-600 text-white shadow-sm'
                  : 'bg-white text-gray-600 border border-gray-200 hover:bg-gray-100'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      {/* Users Grid */}
      {loading ? (
        <div className="text-center py-20 bg-white rounded-3xl border border-gray-100">
          <div className="w-10 h-10 border-4 border-emerald-600 border-t-transparent rounded-full animate-spin mx-auto mb-3" />
          <p className="text-gray-500 text-sm">Đang nạp danh sách người dùng...</p>
        </div>
      ) : filteredUsers.length === 0 ? (
        <div className="text-center py-16 bg-white rounded-3xl border border-dashed border-gray-200 space-y-3">
          <Users className="w-12 h-12 text-gray-300 mx-auto" />
          <h3 className="font-bold text-gray-700 text-base">Không tìm thấy người dùng phù hợp</h3>
          <p className="text-xs text-gray-400">Hãy thử đổi từ khóa tìm kiếm hoặc bỏ chọn bộ lọc vai trò.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredUsers.map((user) => {
            const isAdmin = user.roles?.includes('Admin');
            const isAuthor = user.roles?.includes('Author');
            const displayName = user.displayName || user.fullName || user.email;
            const formattedDate = user.createdAt
              ? new Date(user.createdAt).toLocaleDateString('vi-VN', {
                  day: '2-digit',
                  month: '2-digit',
                  year: 'numeric',
                })
              : 'Tháng 09/2026';

            return (
              <div
                key={user.id}
                className="bg-white rounded-2xl border border-gray-100 shadow-sm hover:shadow-md transition-shadow p-6 flex flex-col justify-between space-y-4"
              >
                <div>
                  {/* User Profile Header */}
                  <div className="flex items-start gap-4">
                    <div className="w-14 h-14 rounded-2xl overflow-hidden bg-gradient-to-tr from-emerald-100 to-amber-100 flex items-center justify-center shrink-0 border border-gray-100 shadow-sm">
                      {user.avatarUrl ? (
                        <img
                          src={user.avatarUrl}
                          alt={displayName}
                          className="w-full h-full object-cover"
                        />
                      ) : (
                        <span className="text-lg font-extrabold text-emerald-800">
                          {displayName.charAt(0).toUpperCase()}
                        </span>
                      )}
                    </div>

                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-1.5">
                        <h3 className="font-bold text-gray-900 text-base truncate" title={displayName}>
                          {displayName}
                        </h3>
                        {user.emailConfirmed && (
                          <span title="Email đã xác thực">
                            <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
                          </span>
                        )}
                      </div>

                      <p className="text-xs text-gray-500 truncate mt-0.5 flex items-center gap-1">
                        <Mail className="w-3.5 h-3.5 text-gray-400" />
                        <span>{user.email}</span>
                      </p>

                      <div className="flex flex-wrap items-center gap-1.5 mt-2">
                        {isAdmin && (
                          <span className="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-purple-100 text-purple-800 border border-purple-200">
                            Admin
                          </span>
                        )}
                        {isAuthor && (
                          <span className="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-emerald-100 text-emerald-800 border border-emerald-200">
                            Tác giả
                          </span>
                        )}
                        {!isAdmin && !isAuthor && (
                          <span className="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-gray-100 text-gray-700">
                            Thành viên
                          </span>
                        )}
                      </div>
                    </div>
                  </div>

                  {/* Bio */}
                  <p className="text-xs text-gray-600 line-clamp-3 mt-4 leading-relaxed bg-gray-50/60 p-3 rounded-xl border border-gray-100/80">
                    {user.bio || 'Chưa cập nhật phần giới thiệu cá nhân.'}
                  </p>
                </div>

                {/* Footer Metadata */}
                <div className="pt-3 border-t border-gray-100 flex items-center justify-between text-xs text-gray-400">
                  <span className="flex items-center gap-1" title="Ngày tham gia">
                    <Calendar className="w-3.5 h-3.5 text-gray-400" />
                    <span>Tham gia: {formattedDate}</span>
                  </span>

                  <Link
                    href={`/search?q=${encodeURIComponent(displayName)}`}
                    className="text-emerald-700 font-semibold hover:text-emerald-800 flex items-center gap-0.5 hover:underline"
                  >
                    Xem bài viết
                    <ExternalLink className="w-3 h-3" />
                  </Link>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
