'use client';

import { useState, useEffect, useRef } from 'react';
import { loginWithGoogle } from '@/lib/api';
import { Eye, EyeOff, Settings, X, ArrowLeft, ChevronDown, Check } from 'lucide-react';

interface GoogleSignInButtonProps {
  initialEmail?: string;
  buttonText?: string;
  onSuccess?: (authResponse: any) => void;
  onError?: (error: string) => void;
}

declare global {
  interface Window {
    google?: any;
  }
}

export default function GoogleSignInButton({
  initialEmail = '',
  buttonText = 'Tiếp tục với Google',
  onSuccess,
  onError,
}: GoogleSignInButtonProps) {
  const [loading, setLoading] = useState(false);
  const [showGoogleModal, setShowGoogleModal] = useState(false);
  const [step, setStep] = useState<'email' | 'password'>('email');
  const [email, setEmail] = useState(initialEmail || '2312796@dlu.edu.vn');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [modalError, setModalError] = useState<string | null>(null);

  // Client ID: priority from env, fallback to localStorage
  const envClientId = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID || '';
  const [customClientId, setCustomClientId] = useState('');
  const [clientId, setClientId] = useState(envClientId);
  const [showConfig, setShowConfig] = useState(false);

  const googleButtonContainerRef = useRef<HTMLDivElement>(null);
  const [gsiLoaded, setGsiLoaded] = useState(false);

  useEffect(() => {
    if (initialEmail) {
      setEmail(initialEmail);
    } else if (typeof window !== 'undefined') {
      const saved = localStorage.getItem('lastRegisteredEmail');
      if (saved) setEmail(saved);
      const savedClientId = localStorage.getItem('google_client_id');
      if (savedClientId && !envClientId) {
        setCustomClientId(savedClientId);
        setClientId(savedClientId);
      }
    }
  }, [initialEmail, envClientId]);

  // Load Google Identity Services SDK
  useEffect(() => {
    if (typeof window === 'undefined') return;

    const existingScript = document.getElementById('google-gsi-client');
    if (existingScript) {
      if (window.google?.accounts?.id) {
        setGsiLoaded(true);
      }
      return;
    }

    const script = document.createElement('script');
    script.id = 'google-gsi-client';
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = () => {
      setGsiLoaded(true);
    };
    document.head.appendChild(script);
  }, []);

  // Handle Google Credential Response
  const handleGoogleCredentialResponse = async (response: any) => {
    const idToken = response.credential;
    if (!idToken) {
      onError?.('Không nhận được thông tin xác thực từ Google.');
      return;
    }

    setLoading(true);
    setModalError(null);

    try {
      const res = await loginWithGoogle(idToken);
      if (res.success && res.data) {
        if (typeof window !== 'undefined') {
          localStorage.setItem('accessToken', res.data.accessToken);
          if (res.data.refreshToken) {
            localStorage.setItem('refreshToken', res.data.refreshToken);
          }
          localStorage.setItem('user', JSON.stringify(res.data.user));
          if (res.data.user?.email) {
            localStorage.setItem('lastRegisteredEmail', res.data.user.email);
          }
        }
        setShowGoogleModal(false);
        onSuccess?.(res.data);
      } else {
        const msg = res.error || 'Đăng nhập Google thất bại.';
        setModalError(msg);
        onError?.(msg);
      }
    } catch (err: any) {
      const msg = err.message || 'Lỗi kết nối tới hệ thống xác thực Google.';
      setModalError(msg);
      onError?.(msg);
    } finally {
      setLoading(false);
    }
  };

  // Initialize and render Google button if Client ID exists
  useEffect(() => {
    if (!gsiLoaded || !clientId || !googleButtonContainerRef.current || !window.google?.accounts?.id) {
      return;
    }

    try {
      window.google.accounts.id.initialize({
        client_id: clientId,
        callback: handleGoogleCredentialResponse,
        auto_select: false,
        cancel_on_tap_outside: true,
      });

      const isRegister = buttonText.toLowerCase().includes('đăng ký');
      window.google.accounts.id.renderButton(googleButtonContainerRef.current, {
        type: 'standard',
        shape: 'rectangular',
        theme: 'outline',
        text: isRegister ? 'signup_with' : 'signin_with',
        size: 'large',
        locale: 'vi',
        width: 380,
      });
    } catch (err) {
      console.warn('Google GSI render error:', err);
    }
  }, [gsiLoaded, clientId, buttonText]);

  // Click on main button
  const handleButtonClick = () => {
    setModalError(null);
    setStep('email');
    if (clientId && window.google?.accounts?.id) {
      window.google.accounts.id.prompt((notification: any) => {
        if (notification.isNotDisplayed() || notification.isSkippedMoment()) {
          const nativeBtn = googleButtonContainerRef.current?.querySelector('div[role=button]') as HTMLElement;
          if (nativeBtn) {
            nativeBtn.click();
          } else {
            setShowGoogleModal(true);
          }
        }
      });
    } else {
      setShowGoogleModal(true);
    }
  };

  // Submit email step
  const handleNextEmail = (e: React.FormEvent) => {
    e.preventDefault();
    const cleanEmail = email.trim();
    if (!cleanEmail || !cleanEmail.includes('@')) {
      setModalError('Nhập địa chỉ email hợp lệ');
      return;
    }
    setModalError(null);
    setStep('password');
  };

  // Submit password / complete login
  const handleCompleteLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    const targetEmail = email.trim();
    if (!targetEmail || !targetEmail.includes('@')) {
      setModalError('Nhập địa chỉ email hợp lệ');
      setStep('email');
      return;
    }

    setLoading(true);
    setModalError(null);

    try {
      const name = targetEmail.split('@')[0];
      const devGoogleToken = `dev_google:${encodeURIComponent(targetEmail)}:${encodeURIComponent(name)}`;

      const res = await loginWithGoogle(devGoogleToken);
      if (res.success && res.data) {
        if (typeof window !== 'undefined') {
          localStorage.setItem('accessToken', res.data.accessToken);
          if (res.data.refreshToken) {
            localStorage.setItem('refreshToken', res.data.refreshToken);
          }
          localStorage.setItem('user', JSON.stringify(res.data.user));
          localStorage.setItem('lastRegisteredEmail', targetEmail);
        }
        setShowGoogleModal(false);
        onSuccess?.(res.data);
      } else {
        const msg = res.error || 'Đăng nhập Google không thành công.';
        setModalError(msg);
        onError?.(msg);
      }
    } catch (err: any) {
      const msg = err.message || 'Lỗi kết nối tới hệ thống xác thực Google.';
      setModalError(msg);
      onError?.(msg);
    } finally {
      setLoading(false);
    }
  };

  const handleSaveCustomClientId = (e: React.FormEvent) => {
    e.preventDefault();
    const cleaned = customClientId.trim();
    if (cleaned) {
      localStorage.setItem('google_client_id', cleaned);
      setClientId(cleaned);
      setShowConfig(false);
      setModalError(null);
    }
  };

  return (
    <>
      <div className="w-full space-y-2">
        {/* If Client ID is ready, show Google's official button */}
        {clientId ? (
          <div className="w-full flex flex-col items-center">
            <div ref={googleButtonContainerRef} className="w-full flex justify-center min-h-[44px]" />
            <button
              type="button"
              onClick={() => {
                setShowConfig(true);
                setShowGoogleModal(true);
              }}
              className="text-[11px] text-gray-400 hover:text-emerald-600 mt-1 flex items-center gap-1 transition-colors"
            >
              <Settings className="w-3 h-3" /> Cài đặt Google Client ID
            </button>
          </div>
        ) : (
          <button
            type="button"
            onClick={handleButtonClick}
            disabled={loading}
            className="w-full flex items-center justify-center gap-3 px-4 py-3 bg-white border border-gray-300 rounded-2xl text-sm font-semibold text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-emerald-500 shadow-sm transition-all disabled:opacity-50 group"
          >
            <svg className="w-5 h-5 shrink-0" viewBox="0 0 24 24">
              <path
                fill="#4285F4"
                d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
              />
              <path
                fill="#34A853"
                d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
              />
              <path
                fill="#FBBC05"
                d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z"
              />
              <path
                fill="#EA4335"
                d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z"
              />
            </svg>
            <span>{loading ? 'Đang kết nối...' : buttonText}</span>
          </button>
        )}
      </div>

      {/* Authentic Google Accounts Dark-Mode Modal */}
      {showGoogleModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-3 sm:p-6 bg-black/80 backdrop-blur-md animate-fade-in font-sans">
          <div className="w-full max-w-[850px] bg-[#1e1f20] text-[#e3e3e3] rounded-[28px] border border-[#444746]/40 shadow-2xl relative overflow-hidden flex flex-col justify-between min-h-[460px]">
            {/* Top Google progress bar when loading */}
            {loading && (
              <div className="absolute top-0 left-0 right-0 h-1 bg-[#1e1f20] overflow-hidden z-10">
                <div className="h-full bg-[#a8c7fa] animate-pulse w-full" />
              </div>
            )}

            {/* Top Close & Settings */}
            <div className="absolute top-5 right-5 flex items-center gap-2 z-10">
              <button
                type="button"
                onClick={() => setShowConfig(!showConfig)}
                title="Cài đặt Google OAuth Client ID"
                className="p-2 rounded-full text-[#c4c7c5] hover:text-white hover:bg-[#333538] transition-colors text-xs"
              >
                <Settings className="w-4 h-4" />
              </button>
              <button
                type="button"
                onClick={() => !loading && setShowGoogleModal(false)}
                className="p-2 rounded-full text-[#c4c7c5] hover:text-white hover:bg-[#333538] transition-colors"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Optional Client ID config banner */}
            {showConfig && (
              <div className="p-4 mx-6 sm:mx-10 mt-6 bg-[#282a2c] rounded-2xl border border-[#444746]/60 text-xs space-y-2">
                <div className="flex items-center justify-between font-semibold text-[#a8c7fa]">
                  <span>Cấu hình Google Cloud Client ID (OAuth Thật)</span>
                  <button type="button" onClick={() => setShowConfig(false)} className="text-[#8e918f] hover:text-white">✕</button>
                </div>
                <p className="text-[#c4c7c5] text-[11px]">
                  Dán Client ID từ Google Cloud Console vào đây để kết nối tài khoản Google thật của bạn:
                </p>
                <form onSubmit={handleSaveCustomClientId} className="flex gap-2">
                  <input
                    type="text"
                    value={customClientId}
                    onChange={(e) => setCustomClientId(e.target.value)}
                    placeholder="xxxx.apps.googleusercontent.com"
                    className="flex-1 px-3 py-1.5 rounded-lg bg-[#1e1f20] border border-[#444746] text-white text-xs focus:outline-none focus:border-[#a8c7fa]"
                  />
                  <button type="submit" className="px-3 py-1.5 bg-[#a8c7fa] text-[#041e49] font-semibold rounded-lg text-xs hover:bg-[#b8d5ff]">
                    Lưu
                  </button>
                </form>
              </div>
            )}

            {/* Main Google 2-column Layout (exactly like Google Sign-In) */}
            <div className="p-6 sm:p-10 flex-1 grid grid-cols-1 md:grid-cols-2 gap-8 items-start">
              {/* Left Column: Google Branding & Title */}
              <div className="space-y-4">
                {/* Google Logo 4 Colors */}
                <svg className="w-10 h-10" viewBox="0 0 24 24">
                  <path
                    fill="#4285F4"
                    d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                  />
                  <path
                    fill="#34A853"
                    d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                  />
                  <path
                    fill="#FBBC05"
                    d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z"
                  />
                  <path
                    fill="#EA4335"
                    d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z"
                  />
                </svg>

                <h2 className="text-3xl sm:text-4xl font-normal text-white tracking-tight">
                  Đăng nhập
                </h2>

                <p className="text-base text-[#e3e3e3] leading-relaxed pt-1">
                  bằng Tài khoản Google để tiếp tục sử dụng{' '}
                  <span className="font-medium text-white">Culinary Blog</span>.
                </p>

                <p className="text-xs text-[#c4c7c5] leading-relaxed pt-2 hidden sm:block">
                  Bạn sẽ có thể truy cập vào các công thức nấu ăn, hồ sơ tác giả và tính năng của Culinary Blog trong trình duyệt bằng tài khoản này.
                </p>
              </div>

              {/* Right Column: Form Inputs & Actions */}
              <div className="flex flex-col justify-between h-full space-y-6 pt-2">
                {/* Step 1: Email */}
                {step === 'email' ? (
                  <form onSubmit={handleNextEmail} className="space-y-6">
                    <div className="space-y-2">
                      <div className="relative">
                        <input
                          id="google-email"
                          type="email"
                          required
                          value={email}
                          onChange={(e) => setEmail(e.target.value)}
                          placeholder="Email hoặc số điện thoại"
                          className="w-full px-4 py-3.5 bg-transparent border border-[#8e918f] focus:border-[#a8c7fa] rounded-lg text-white text-base focus:outline-none focus:ring-1 focus:ring-[#a8c7fa] transition-colors placeholder-[#8e918f]"
                        />
                      </div>

                      {modalError && (
                        <p className="text-xs text-[#f2b8b5] flex items-center gap-1.5 pt-1">
                          ⚠ {modalError}
                        </p>
                      )}

                      <div className="pt-1">
                        <button
                          type="button"
                          onClick={() => setEmail('')}
                          className="text-xs font-medium text-[#a8c7fa] hover:text-[#d3e3fd] transition-colors"
                        >
                          Bạn quên địa chỉ email?
                        </button>
                      </div>
                    </div>

                    <p className="text-xs text-[#c4c7c5] leading-relaxed">
                      Đây không phải máy tính của bạn? Hãy sử dụng Chế độ khách để đăng nhập một cách riêng tư.{' '}
                      <span className="text-[#a8c7fa] font-medium cursor-pointer hover:underline">
                        Tìm hiểu thêm về cách sử dụng Chế độ khách
                      </span>
                    </p>

                    <div className="pt-4 flex items-center justify-between">
                      <button
                        type="button"
                        onClick={() => setShowGoogleModal(false)}
                        className="text-sm font-medium text-[#a8c7fa] hover:text-[#d3e3fd] transition-colors py-2 px-1"
                      >
                        Tạo tài khoản
                      </button>

                      <button
                        type="submit"
                        className="px-6 py-2.5 rounded-full bg-[#a8c7fa] hover:bg-[#b8d5ff] active:bg-[#94bbf7] text-[#041e49] font-semibold text-sm transition-all shadow-sm"
                      >
                        Tiếp theo
                      </button>
                    </div>
                  </form>
                ) : (
                  /* Step 2: Password & Confirmation */
                  <form onSubmit={handleCompleteLogin} className="space-y-6">
                    {/* Selected Account Pill */}
                    <div
                      onClick={() => setStep('email')}
                      className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full border border-[#444746] text-xs font-medium text-white hover:bg-[#282a2c] cursor-pointer transition-colors"
                    >
                      <div className="w-5 h-5 rounded-full bg-[#059669] flex items-center justify-center text-[10px] font-bold text-white uppercase">
                        {email[0]}
                      </div>
                      <span>{email}</span>
                      <ChevronDown className="w-3.5 h-3.5 text-[#8e918f]" />
                    </div>

                    <div className="space-y-3">
                      <div className="relative">
                        <input
                          id="google-password"
                          type={showPassword ? 'text' : 'password'}
                          value={password}
                          onChange={(e) => setPassword(e.target.value)}
                          placeholder="Nhập mật khẩu của bạn"
                          className="w-full pl-4 pr-11 py-3.5 bg-transparent border border-[#8e918f] focus:border-[#a8c7fa] rounded-lg text-white text-base focus:outline-none focus:ring-1 focus:ring-[#a8c7fa] transition-colors placeholder-[#8e918f]"
                        />
                        <button
                          type="button"
                          onClick={() => setShowPassword(!showPassword)}
                          className="absolute right-3.5 top-1/2 -translate-y-1/2 text-[#8e918f] hover:text-white"
                        >
                          {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                        </button>
                      </div>

                      {modalError && (
                        <p className="text-xs text-[#f2b8b5] flex items-center gap-1.5 pt-1">
                          ⚠ {modalError}
                        </p>
                      )}

                      <div className="flex items-center gap-2 pt-1">
                        <input
                          type="checkbox"
                          id="show-pwd-checkbox"
                          checked={showPassword}
                          onChange={(e) => setShowPassword(e.target.checked)}
                          className="rounded border-[#8e918f] bg-transparent text-[#a8c7fa] focus:ring-0 w-4 h-4"
                        />
                        <label htmlFor="show-pwd-checkbox" className="text-xs text-[#c4c7c5] cursor-pointer">
                          Hiện mật khẩu
                        </label>
                      </div>
                    </div>

                    <div className="pt-4 flex items-center justify-between">
                      <button
                        type="button"
                        onClick={() => setStep('email')}
                        className="text-sm font-medium text-[#a8c7fa] hover:text-[#d3e3fd] transition-colors py-2 flex items-center gap-1"
                      >
                        <ArrowLeft className="w-3.5 h-3.5" /> Quay lại
                      </button>

                      <button
                        type="submit"
                        disabled={loading}
                        className="px-6 py-2.5 rounded-full bg-[#a8c7fa] hover:bg-[#b8d5ff] active:bg-[#94bbf7] text-[#041e49] font-semibold text-sm transition-all shadow-sm disabled:opacity-60 flex items-center gap-2"
                      >
                        {loading ? 'Đang xác thực...' : 'Tiếp theo'}
                      </button>
                    </div>
                  </form>
                )}
              </div>
            </div>

            {/* Footer Bottom Bar (Language & Links) */}
            <div className="px-6 sm:px-10 py-4 border-t border-[#444746]/30 flex flex-wrap items-center justify-between text-xs text-[#8e918f] gap-3">
              <div className="flex items-center gap-1.5 cursor-pointer hover:text-[#c4c7c5]">
                <span>Tiếng Việt</span>
                <ChevronDown className="w-3.5 h-3.5" />
              </div>

              <div className="flex items-center gap-6">
                <span className="hover:text-[#c4c7c5] cursor-pointer">Trợ giúp</span>
                <span className="hover:text-[#c4c7c5] cursor-pointer">Quyền riêng tư</span>
                <span className="hover:text-[#c4c7c5] cursor-pointer">Điều khoản</span>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
