'use client';

import Link from 'next/link';
import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { UtensilsCrossed, Mail, Lock, User, AtSign, Eye, EyeOff, Loader2, AlertCircle, CheckCircle2, ArrowRight } from 'lucide-react';
import { register } from '@/lib/api';

export default function RegisterPage() {
  const router = useRouter();
  const [fullName, setFullName] = useState('');
  const [userName, setUserName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<Record<string, string[]>>({});

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setValidationErrors({});

    if (password !== confirmPassword) {
      setErrorMessage('Mật khẩu nhập lại không khớp.');
      return;
    }

    setIsLoading(true);

    const res = await register({
      fullName,
      userName: userName.toLowerCase().trim(),
      email: email.toLowerCase().trim(),
      password,
    });

    setIsLoading(false);

    if (res.success && res.data) {
      // Auto-login: Store token in localStorage
      if (typeof window !== 'undefined') {
        localStorage.setItem('accessToken', res.data.accessToken);
        if (res.data.refreshToken) {
          localStorage.setItem('refreshToken', res.data.refreshToken);
        }
        localStorage.setItem('user', JSON.stringify(res.data.user));
      }
      router.push('/dashboard/profile');
    } else {
      setErrorMessage(res.error || 'Đăng ký tài khoản không thành công.');
      if (res.validationErrors) {
        setValidationErrors(res.validationErrors);
      }
    }
  };

  return (
    <div className="min-h-[calc(100vh-4rem)] flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8 bg-gradient-to-b from-emerald-50/50 via-white to-gray-50/50">
      <div className="max-w-lg w-full space-y-8 bg-white p-8 sm:p-10 rounded-3xl border border-gray-100 shadow-xl shadow-emerald-500/5">
        {/* Header */}
        <div className="text-center">
          <Link href="/" className="inline-flex items-center gap-2 mb-4 group">
            <div className="relative w-14 h-14 rounded-2xl overflow-hidden ring-2 ring-emerald-600/20 shadow-lg shadow-emerald-900/10 group-hover:scale-105 transition-transform">
              <img src="/logo.jpg" alt="Culinary Blog Logo" className="w-full h-full object-cover" />
            </div>
          </Link>
          <h2 className="text-2xl sm:text-3xl font-bold text-gray-900 tracking-tight">
            Tạo tài khoản Tác giả
          </h2>
          <p className="mt-2 text-sm text-gray-500">
            Đăng ký để chia sẻ các công thức nấu ăn tuyệt vời của bạn
          </p>
        </div>

        {/* Error Alert */}
        {errorMessage && (
          <div className="p-4 rounded-2xl bg-red-50 border border-red-100 flex items-start gap-3">
            <AlertCircle className="w-5 h-5 text-red-500 shrink-0 mt-0.5" />
            <div className="text-sm text-red-700 leading-relaxed font-medium">
              {errorMessage}
            </div>
          </div>
        )}

        {/* Form */}
        <form className="mt-8 space-y-4" onSubmit={handleSubmit}>
          {/* Full Name */}
          <div>
            <label className="block text-xs font-semibold text-gray-700 uppercase tracking-wider mb-2">
              Họ và tên
            </label>
            <div className="relative">
              <User className="w-5 h-5 text-gray-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                required
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                placeholder="Nguyễn Văn A"
                className="w-full pl-11 pr-4 py-3 bg-gray-50 border border-gray-200 rounded-2xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:bg-white transition-all text-gray-900"
              />
            </div>
            {validationErrors.FullName && (
              <p className="mt-1 text-xs text-red-500">{validationErrors.FullName[0]}</p>
            )}
          </div>

          {/* Username */}
          <div>
            <label className="block text-xs font-semibold text-gray-700 uppercase tracking-wider mb-2">
              Tên đăng nhập (Username)
            </label>
            <div className="relative">
              <AtSign className="w-5 h-5 text-gray-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                required
                value={userName}
                onChange={(e) => setUserName(e.target.value)}
                placeholder="nguyenvana"
                className="w-full pl-11 pr-4 py-3 bg-gray-50 border border-gray-200 rounded-2xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:bg-white transition-all text-gray-900"
              />
            </div>
            {validationErrors.UserName && (
              <p className="mt-1 text-xs text-red-500">{validationErrors.UserName[0]}</p>
            )}
          </div>

          {/* Email */}
          <div>
            <label className="block text-xs font-semibold text-gray-700 uppercase tracking-wider mb-2">
              Địa chỉ Email
            </label>
            <div className="relative">
              <Mail className="w-5 h-5 text-gray-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="name@example.com"
                className="w-full pl-11 pr-4 py-3 bg-gray-50 border border-gray-200 rounded-2xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:bg-white transition-all text-gray-900"
              />
            </div>
            {validationErrors.Email && (
              <p className="mt-1 text-xs text-red-500">{validationErrors.Email[0]}</p>
            )}
          </div>

          {/* Password */}
          <div>
            <label className="block text-xs font-semibold text-gray-700 uppercase tracking-wider mb-2">
              Mật khẩu (Tối thiểu 8 ký tự, 1 hoa, 1 số, 1 ký tự đặc biệt)
            </label>
            <div className="relative">
              <Lock className="w-5 h-5 text-gray-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
              <input
                type={showPassword ? 'text' : 'password'}
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full pl-11 pr-11 py-3 bg-gray-50 border border-gray-200 rounded-2xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:bg-white transition-all text-gray-900"
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3.5 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
              >
                {showPassword ? <EyeOff className="w-5 h-5" /> : <Eye className="w-5 h-5" />}
              </button>
            </div>
            {validationErrors.Password && (
              <p className="mt-1 text-xs text-red-500">{validationErrors.Password[0]}</p>
            )}
          </div>

          {/* Confirm Password */}
          <div>
            <label className="block text-xs font-semibold text-gray-700 uppercase tracking-wider mb-2">
              Xác nhận mật khẩu
            </label>
            <div className="relative">
              <Lock className="w-5 h-5 text-gray-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
              <input
                type={showPassword ? 'text' : 'password'}
                required
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full pl-11 pr-4 py-3 bg-gray-50 border border-gray-200 rounded-2xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:bg-white transition-all text-gray-900"
              />
            </div>
          </div>

          <button
            type="submit"
            disabled={isLoading}
            className="w-full mt-2 py-3.5 px-4 rounded-2xl bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-sm shadow-md shadow-emerald-200 hover:shadow-lg transition-all flex items-center justify-center gap-2 disabled:opacity-60 disabled:cursor-not-allowed"
          >
            {isLoading ? (
              <>
                <Loader2 className="w-5 h-5 animate-spin" />
                Đang tạo tài khoản...
              </>
            ) : (
              <>
                Đăng ký ngay
                <ArrowRight className="w-4 h-4" />
              </>
            )}
          </button>
        </form>

        {/* Footer Link */}
        <p className="text-center text-sm text-gray-500">
          Đã có tài khoản?{' '}
          <Link href="/auth/login" className="font-semibold text-emerald-600 hover:text-emerald-700">
            Đăng nhập ngay
          </Link>
        </p>
      </div>
    </div>
  );
}
