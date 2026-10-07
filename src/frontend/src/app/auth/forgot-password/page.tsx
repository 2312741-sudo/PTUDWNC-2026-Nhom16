'use client';

import Link from 'next/link';
import { useState } from 'react';
import { UtensilsCrossed, Mail, Loader2, AlertCircle, CheckCircle, ArrowLeft, Send, ExternalLink } from 'lucide-react';
import { forgotPassword } from '@/lib/api';

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const cleanEmail = email.trim();
    if (!cleanEmail || !cleanEmail.includes('@')) {
      setErrorMessage('Vui lòng nhập địa chỉ email hợp lệ.');
      return;
    }

    setIsLoading(true);
    setErrorMessage(null);
    setSuccessMessage(null);

    const res = await forgotPassword(cleanEmail);
    setIsLoading(false);

    if (res.success) {
      setSuccessMessage(res.message || 'Mật khẩu mới đã được gửi về email của bạn. Vui lòng kiểm tra hộp thư.');
    } else {
      setErrorMessage(res.error || 'Không thể cấp lại mật khẩu. Vui lòng thử lại sau.');
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-3xl max-w-md w-full p-8 shadow-sm border border-gray-100">
        {/* Brand */}
        <div className="flex flex-col items-center mb-8">
          <Link href="/" className="flex items-center gap-2 group mb-6">
            <div className="w-10 h-10 rounded-xl bg-emerald-600 flex items-center justify-center text-white shadow-md shadow-emerald-200 group-hover:scale-105 transition-transform">
              <UtensilsCrossed className="w-5 h-5" />
            </div>
            <span className="text-xl font-bold tracking-tight text-gray-900">
              Culinary<span className="text-emerald-600">Blog</span>
            </span>
          </Link>
          <h1 className="text-2xl font-bold text-gray-900 tracking-tight">Quên mật khẩu?</h1>
          <p className="text-sm text-gray-500 mt-1 text-center">
            Nhập email tài khoản của bạn. Hệ thống sẽ cấp mật khẩu mới an toàn và gửi trực tiếp về hòm thư.
          </p>
        </div>

        {/* Error Alert */}
        {errorMessage && (
          <div className="mb-6 p-4 rounded-2xl bg-red-50 border border-red-100 flex items-start gap-3 text-red-600 text-sm">
            <AlertCircle className="w-5 h-5 shrink-0 mt-0.5" />
            <div className="font-medium">{errorMessage}</div>
          </div>
        )}

        {/* Success Alert */}
        {successMessage ? (
          <div className="space-y-6">
            <div className="p-5 rounded-2xl bg-emerald-50 border border-emerald-100 text-emerald-800 space-y-3">
              <div className="flex items-center gap-2.5 font-bold text-base text-emerald-900">
                <CheckCircle className="w-5 h-5 text-emerald-600 shrink-0" />
                <span>Gửi mật khẩu mới thành công!</span>
              </div>
              <p className="text-xs leading-relaxed text-emerald-700">
                Mật khẩu mới đã được gửi tới hòm thư <strong>{email}</strong>. Vui lòng mở hộp thư email của bạn để lấy mật khẩu đăng nhập.
              </p>

              {/* Dev hint for MailHog */}
              <div className="pt-2 border-t border-emerald-200/60 flex items-center justify-between text-xs">
                <span className="text-emerald-700">Môi trường Dev (MailHog):</span>
                <a
                  href="http://localhost:8025"
                  target="_blank"
                  rel="noreferrer"
                  className="font-semibold text-emerald-800 underline hover:text-emerald-900 flex items-center gap-1"
                >
                  Xem MailHog <ExternalLink className="w-3 h-3" />
                </a>
              </div>
            </div>

            <div className="space-y-3">
              <Link
                href="/auth/login"
                className="w-full py-3.5 px-4 rounded-2xl bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-sm shadow-md shadow-emerald-200 hover:shadow-lg transition-all flex items-center justify-center gap-2"
              >
                Đăng nhập với mật khẩu mới
              </Link>
              <button
                type="button"
                onClick={() => {
                  setSuccessMessage(null);
                  setEmail('');
                }}
                className="w-full py-2.5 px-4 rounded-2xl border border-gray-200 hover:bg-gray-50 text-gray-600 font-medium text-xs transition-colors"
              >
                Nhập địa chỉ email khác
              </button>
            </div>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-5">
            <div>
              <label className="block text-xs font-semibold text-gray-700 uppercase tracking-wider mb-2">
                Email tài khoản <span className="text-red-500">*</span>
              </label>
              <div className="relative">
                <Mail className="w-5 h-5 text-gray-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
                <input
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="your.email@example.com"
                  className="w-full pl-11 pr-4 py-3 bg-gray-50 border border-gray-200 rounded-2xl text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:bg-white transition-all text-gray-900"
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={isLoading}
              className="w-full py-3.5 px-4 rounded-2xl bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-sm shadow-md shadow-emerald-200 hover:shadow-lg transition-all flex items-center justify-center gap-2 disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {isLoading ? (
                <>
                  <Loader2 className="w-5 h-5 animate-spin" />
                  Đang xử lý & gửi email...
                </>
              ) : (
                <>
                  <Send className="w-4 h-4" />
                  Gửi mật khẩu mới về email
                </>
              )}
            </button>

            {/* Back link */}
            <div className="pt-2 text-center">
              <Link
                href="/auth/login"
                className="inline-flex items-center gap-1.5 text-xs font-semibold text-gray-500 hover:text-emerald-600 transition-colors"
              >
                <ArrowLeft className="w-3.5 h-3.5" />
                Quay lại trang Đăng nhập
              </Link>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
