'use client';

import { useEffect, useState } from 'react';
import { User, UpdateProfileRequest } from '@/types/auth';
import { getMe, updateProfile } from '@/lib/api';
import { UserCheck, Shield, AlertCircle, CheckCircle, Save, RefreshCw, Mail, Image as ImageIcon, FileText } from 'lucide-react';

export default function ProfileDashboardPage() {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Form fields
  const [displayName, setDisplayName] = useState('');
  const [avatarUrl, setAvatarUrl] = useState('');
  const [bio, setBio] = useState('');

  // Field validation errors
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  const token = typeof window !== 'undefined' ? localStorage.getItem('token') || '' : '';

  const loadProfile = async () => {
    setLoading(true);
    setError(null);

    // If no token in localStorage, provide a fallback demo user for UI demonstration
    if (!token) {
      const demoUser: User = {
        id: 'tv1-demo-uuid',
        email: '2312741@dlu.edu.vn',
        fullName: 'Nguyễn Thanh Tâm',
        userName: '2312741',
        displayName: 'Nguyễn Thanh Tâm',
        roles: ['Author'],
        avatarUrl: 'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150',
        bio: 'Nhóm trưởng nhóm 16 — Phụ trách xác thực, hồ sơ và bảo mật hệ thống.',
        emailConfirmed: true,
        createdAt: '2026-09-16T10:00:00Z',
      };
      setUser(demoUser);
      setDisplayName(demoUser.fullName || demoUser.displayName);
      setAvatarUrl(demoUser.avatarUrl || '');
      setBio(demoUser.bio || '');
      setLoading(false);
      return;
    }

    const res = await getMe(token);
    if (res.success && res.data) {
      setUser(res.data);
      setDisplayName(res.data.displayName || '');
      setAvatarUrl(res.data.avatarUrl || '');
      setBio(res.data.bio || '');
    } else {
      setError(res.error || 'Không thể tải thông tin hồ sơ.');
    }
    setLoading(false);
  };

  useEffect(() => {
    loadProfile();
  }, []);

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    const trimmedName = displayName.trim();
    if (!trimmedName) {
      errors.displayName = 'Tên hiển thị không được để trống.';
    } else if (trimmedName.length > 100) {
      errors.displayName = 'Tên hiển thị tối đa 100 ký tự.';
    } else if (trimmedName.includes('<') || trimmedName.includes('>')) {
      errors.displayName = 'Tên hiển thị không được chứa ký tự HTML/script.';
    }

    const trimmedAvatar = avatarUrl.trim();
    if (trimmedAvatar) {
      if (trimmedAvatar.length > 500) {
        errors.avatarUrl = 'URL ảnh đại diện tối đa 500 ký tự.';
      } else if (!/^https?:\/\/.+/i.test(trimmedAvatar)) {
        errors.avatarUrl = 'URL ảnh đại diện phải là liên kết hợp lệ (http:// hoặc https://).';
      }
    }

    if (bio.length > 2000) {
      errors.bio = 'Tiểu sử tối đa 2000 ký tự.';
    }

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    setSaving(true);
    setError(null);
    setSuccess(null);

    const payload: UpdateProfileRequest = {
      displayName: displayName.trim(),
      avatarUrl: avatarUrl.trim() || null,
      bio: bio.trim() || null,
    };

    if (!token) {
      // Local demo mode save
      setTimeout(() => {
        setUser((prev) => (prev ? { ...prev, ...payload } : null));
        setSuccess('Đã cập nhật hồ sơ thành công (chế độ demo).');
        setSaving(false);
      }, 500);
      return;
    }

    const res = await updateProfile(payload, token);
    if (res.success && res.data) {
      setUser(res.data);
      setSuccess('Cập nhật hồ sơ thành công!');
      setFieldErrors({});
    } else {
      setError(res.error || 'Không thể cập nhật hồ sơ.');
      if (res.validationErrors) {
        const mapped: Record<string, string> = {};
        for (const [key, msgs] of Object.entries(res.validationErrors)) {
          mapped[key.toLowerCase()] = msgs[0];
        }
        setFieldErrors(mapped);
      }
    }
    setSaving(false);
  };

  return (
    <div className="min-h-screen bg-neutral-50 py-10">
      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-neutral-900 tracking-tight">Hồ Sơ & Tài Khoản</h1>
          <p className="mt-1 text-sm text-neutral-600">
            Quản lý thông tin cá nhân và xem quyền hạn tài khoản trên Culinary Blog.
          </p>
        </div>

        {/* Alerts */}
        {error && (
          <div className="mb-6 p-4 rounded-xl bg-red-50 border border-red-200 flex items-start gap-3 text-red-700">
            <AlertCircle className="w-5 h-5 flex-shrink-0 mt-0.5" />
            <div className="text-sm font-medium">{error}</div>
          </div>
        )}

        {success && (
          <div className="mb-6 p-4 rounded-xl bg-emerald-50 border border-emerald-200 flex items-start gap-3 text-emerald-700">
            <CheckCircle className="w-5 h-5 flex-shrink-0 mt-0.5" />
            <div className="text-sm font-medium">{success}</div>
          </div>
        )}

        {loading ? (
          <div className="bg-white rounded-2xl p-8 border border-neutral-200 shadow-sm flex items-center justify-center py-20 text-neutral-500">
            <RefreshCw className="w-6 h-6 animate-spin mr-2" /> Đang tải hồ sơ...
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {/* Left Column: Avatar & Summary Card */}
            <div className="bg-white rounded-2xl p-6 border border-neutral-200 shadow-sm flex flex-col items-center text-center">
              <div className="relative w-28 h-28 rounded-full overflow-hidden bg-neutral-100 border-2 border-amber-500 mb-4 flex items-center justify-center">
                {avatarUrl && /^https?:\/\//i.test(avatarUrl) ? (
                  <img
                    src={avatarUrl}
                    alt={displayName || 'Avatar'}
                    className="w-full h-full object-cover"
                    onError={(e) => {
                      (e.target as HTMLImageElement).style.display = 'none';
                    }}
                  />
                ) : (
                  <div className="text-4xl font-bold text-neutral-400">
                    {(displayName || 'U').charAt(0).toUpperCase()}
                  </div>
                )}
              </div>

              <h2 className="text-xl font-bold text-neutral-900">{displayName || 'Chưa đặt tên'}</h2>
              <p className="text-xs text-neutral-500 mt-1 flex items-center gap-1">
                <Mail className="w-3.5 h-3.5" /> {user?.email}
              </p>

              <div className="mt-4 flex flex-wrap gap-1.5 justify-center">
                {user?.roles?.map((role) => (
                  <span
                    key={role}
                    className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-100 text-amber-800 border border-amber-200 flex items-center gap-1"
                  >
                    <Shield className="w-3 h-3" /> {role}
                  </span>
                ))}
              </div>

              <div className="mt-6 pt-6 border-t border-neutral-100 w-full text-left">
                <div className="text-xs font-semibold uppercase tracking-wider text-neutral-400 mb-2">Quy định bảo mật (A3)</div>
                <ul className="text-xs text-neutral-600 space-y-1.5">
                  <li className="flex items-center gap-1.5">
                    <CheckCircle className="w-3.5 h-3.5 text-emerald-500" /> Không đổi email qua profile
                  </li>
                  <li className="flex items-center gap-1.5">
                    <CheckCircle className="w-3.5 h-3.5 text-emerald-500" /> Không đổi role từ phía client
                  </li>
                  <li className="flex items-center gap-1.5">
                    <CheckCircle className="w-3.5 h-3.5 text-emerald-500" /> Chặn mã độc XSS / HTML
                  </li>
                </ul>
              </div>
            </div>

            {/* Right Column: Update Form */}
            <div className="md:col-span-2 bg-white rounded-2xl p-6 sm:p-8 border border-neutral-200 shadow-sm">
              <form onSubmit={handleSave} className="space-y-6">
                {/* Display Name */}
                <div>
                  <label htmlFor="displayName" className="block text-sm font-semibold text-neutral-900 mb-1">
                    Tên hiển thị <span className="text-red-500">*</span>
                  </label>
                  <div className="relative">
                    <input
                      id="displayName"
                      type="text"
                      value={displayName}
                      onChange={(e) => setDisplayName(e.target.value)}
                      placeholder="VD: Nguyễn Thanh Tâm"
                      className={`w-full px-4 py-2.5 rounded-xl border text-sm transition focus:outline-none focus:ring-2 ${
                        fieldErrors.displayName
                          ? 'border-red-300 focus:ring-red-200 bg-red-50/20'
                          : 'border-neutral-300 focus:ring-amber-200 focus:border-amber-500'
                      }`}
                    />
                  </div>
                  {fieldErrors.displayName && (
                    <p className="mt-1.5 text-xs text-red-600 flex items-center gap-1 font-medium">
                      <AlertCircle className="w-3.5 h-3.5" /> {fieldErrors.displayName}
                    </p>
                  )}
                  <p className="mt-1 text-xs text-neutral-500">Tối đa 100 ký tự, không chứa thẻ HTML.</p>
                </div>

                {/* Email (Read-only) */}
                <div>
                  <label htmlFor="email" className="block text-sm font-semibold text-neutral-900 mb-1">
                    Email tài khoản
                  </label>
                  <div className="relative">
                    <input
                      id="email"
                      type="email"
                      value={user?.email || ''}
                      disabled
                      className="w-full px-4 py-2.5 rounded-xl border border-neutral-200 bg-neutral-100 text-neutral-500 text-sm cursor-not-allowed"
                    />
                  </div>
                  <p className="mt-1 text-xs text-neutral-500">Email dùng làm định danh đăng nhập và không được thay đổi.</p>
                </div>

                {/* Avatar URL */}
                <div>
                  <label htmlFor="avatarUrl" className="block text-sm font-semibold text-neutral-900 mb-1">
                    URL Ảnh đại diện
                  </label>
                  <div className="relative">
                    <input
                      id="avatarUrl"
                      type="text"
                      value={avatarUrl}
                      onChange={(e) => setAvatarUrl(e.target.value)}
                      placeholder="https://example.com/avatar.jpg"
                      className={`w-full px-4 py-2.5 rounded-xl border text-sm transition focus:outline-none focus:ring-2 ${
                        fieldErrors.avatarUrl
                          ? 'border-red-300 focus:ring-red-200 bg-red-50/20'
                          : 'border-neutral-300 focus:ring-amber-200 focus:border-amber-500'
                      }`}
                    />
                  </div>
                  {fieldErrors.avatarUrl && (
                    <p className="mt-1.5 text-xs text-red-600 flex items-center gap-1 font-medium">
                      <AlertCircle className="w-3.5 h-3.5" /> {fieldErrors.avatarUrl}
                    </p>
                  )}
                  <p className="mt-1 text-xs text-neutral-500">Bắt đầu bằng http:// hoặc https:// (tối đa 500 ký tự).</p>
                </div>

                {/* Bio */}
                <div>
                  <label htmlFor="bio" className="block text-sm font-semibold text-neutral-900 mb-1">
                    Tiểu sử / Giới thiệu
                  </label>
                  <textarea
                    id="bio"
                    rows={4}
                    value={bio}
                    onChange={(e) => setBio(e.target.value)}
                    placeholder="Chia sẻ đôi nét về niềm đam mê ẩm thực của bạn..."
                    className={`w-full px-4 py-2.5 rounded-xl border text-sm transition focus:outline-none focus:ring-2 ${
                      fieldErrors.bio
                        ? 'border-red-300 focus:ring-red-200 bg-red-50/20'
                        : 'border-neutral-300 focus:ring-amber-200 focus:border-amber-500'
                    }`}
                  />
                  {fieldErrors.bio && (
                    <p className="mt-1.5 text-xs text-red-600 flex items-center gap-1 font-medium">
                      <AlertCircle className="w-3.5 h-3.5" /> {fieldErrors.bio}
                    </p>
                  )}
                  <div className="flex justify-between mt-1 text-xs text-neutral-500">
                    <span>Không dùng ký tự điều khiển.</span>
                    <span>{bio.length}/2000 ký tự</span>
                  </div>
                </div>

                {/* Actions */}
                <div className="pt-4 flex items-center justify-end gap-3 border-t border-neutral-100">
                  <button
                    type="button"
                    onClick={loadProfile}
                    className="px-4 py-2.5 rounded-xl border border-neutral-300 text-neutral-700 text-sm font-medium hover:bg-neutral-50 transition"
                  >
                    Khôi phục
                  </button>
                  <button
                    type="submit"
                    disabled={saving}
                    className="px-5 py-2.5 rounded-xl bg-amber-500 hover:bg-amber-600 active:bg-amber-700 text-white text-sm font-semibold shadow-sm transition flex items-center gap-2 disabled:opacity-50"
                  >
                    {saving ? (
                      <>
                        <RefreshCw className="w-4 h-4 animate-spin" /> Đang lưu...
                      </>
                    ) : (
                      <>
                        <Save className="w-4 h-4" /> Lưu thay đổi
                      </>
                    )}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
