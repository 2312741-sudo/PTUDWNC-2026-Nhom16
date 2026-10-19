'use client';

import { useState, useEffect, useRef, useCallback } from 'react';
import { loginWithGoogle } from '@/lib/api';
import { authStore } from '@/lib/auth-store';
import { Settings, X, AlertCircle, Sparkles, UserCheck, ShieldCheck, Check, ExternalLink } from 'lucide-react';

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
  const isRegister = buttonText.toLowerCase().includes('đăng ký');
  const [loading, setLoading] = useState(false);
  const [gsiLoaded, setGsiLoaded] = useState(false);
  const [scriptError, setScriptError] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  // Client ID: priority from env, fallback to localStorage (for local testing)
  const envClientId = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID || '';
  const [clientId, setClientId] = useState(envClientId);
  const [customClientId, setCustomClientId] = useState('');
  const [showDevModal, setShowDevModal] = useState(false);
  const [activeTab, setActiveTab] = useState<'real_oauth' | 'dev_mock'>('real_oauth');
  const [testEmail, setTestEmail] = useState(initialEmail || 'chef.demo@culinaryblog.vn');
  const [testName, setTestName] = useState('Đầu Bếp Demo');
  const [savedSuccess, setSavedSuccess] = useState(false);

  const isDev = process.env.NODE_ENV !== 'production';
  const googleButtonContainerRef = useRef<HTMLDivElement>(null);

  // Initialize client ID from localStorage if not configured in env
  useEffect(() => {
    if (typeof window !== 'undefined') {
      const savedClientId = localStorage.getItem('google_client_id');
      if (savedClientId && !envClientId) {
        setClientId(savedClientId);
        setCustomClientId(savedClientId);
      }
      const savedEmail = localStorage.getItem('lastRegisteredEmail');
      if (savedEmail && !initialEmail) {
        setTestEmail(savedEmail);
      }
    }
  }, [envClientId, initialEmail]);

  // Handle Google Credential Response from GIS SDK or Dev Mock
  const handleAuthPayload = useCallback(async (token: string) => {
    setLoading(true);
    setErrorMsg(null);

    try {
      const res = await loginWithGoogle(token);
      if (res.success && res.data) {
        authStore.setAuth(res.data);
        setShowDevModal(false);
        onSuccess?.(res.data);
      } else {
        const msg = res.error || 'Đăng nhập Google không thành công.';
        setErrorMsg(msg);
        onError?.(msg);
      }
    } catch (err: any) {
      const msg = err.message || 'Lỗi kết nối tới hệ thống xác thực Google.';
      setErrorMsg(msg);
      onError?.(msg);
    } finally {
      setLoading(false);
    }
  }, [onSuccess, onError]);

  // Google GIS callback
  const handleGoogleCredentialResponse = useCallback((response: any) => {
    const idToken = response?.credential;
    if (!idToken) {
      const msg = 'Không nhận được Google ID Token hợp lệ từ Google Identity Services.';
      setErrorMsg(msg);
      onError?.(msg);
      return;
    }
    handleAuthPayload(idToken);
  }, [handleAuthPayload, onError]);

  // Load Google Identity Services (GIS) Web SDK
  useEffect(() => {
    if (typeof window === 'undefined') return;

    if (window.google?.accounts?.id) {
      setGsiLoaded(true);
      return;
    }

    const existingScript = document.getElementById('google-gsi-client') as HTMLScriptElement | null;
    if (existingScript) {
      existingScript.addEventListener('load', () => setGsiLoaded(true));
      existingScript.addEventListener('error', () => setScriptError(true));
      return;
    }

    const script = document.createElement('script');
    script.id = 'google-gsi-client';
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = () => setGsiLoaded(true);
    script.onerror = () => setScriptError(true);
    document.head.appendChild(script);
  }, []);

  // Initialize and Render Official Google Button via GIS SDK
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
        context: isRegister ? 'signup' : 'signin',
      });

      if (googleButtonContainerRef.current) {
        googleButtonContainerRef.current.innerHTML = '';
      }

      window.google.accounts.id.renderButton(googleButtonContainerRef.current, {
        type: 'standard',
        shape: 'rectangular',
        theme: 'outline',
        text: isRegister ? 'signup_with' : 'continue_with',
        size: 'large',
        locale: 'vi',
        width: 380,
      });

      // Prompt One Tap if supported
      window.google.accounts.id.prompt();
    } catch (err) {
      console.warn('Google Identity Services render error:', err);
    }
  }, [gsiLoaded, clientId, isRegister, handleGoogleCredentialResponse]);

  // Click handler for fallback button
  const handleFallbackClick = () => {
    setErrorMsg(null);

    // If Client ID is configured and GIS is available, trigger One Tap or prompt
    if (clientId && window.google?.accounts?.id) {
      window.google.accounts.id.prompt((notification: any) => {
        if (notification?.isNotDisplayed?.() || notification?.isSkippedMoment?.()) {
          const renderedBtn = googleButtonContainerRef.current?.querySelector('div[role=button]') as HTMLElement | null;
          if (renderedBtn) {
            renderedBtn.click();
          } else if (isDev) {
            setShowDevModal(true);
          }
        }
      });
      return;
    }

    // When Client ID is not configured, open Assistant Modal to configure or test
    setShowDevModal(true);
  };

  // Dev mode: Save custom Google Client ID
  const handleSaveClientId = (e: React.FormEvent) => {
    e.preventDefault();
    const cleaned = customClientId.trim();
    if (cleaned) {
      localStorage.setItem('google_client_id', cleaned);
      setClientId(cleaned);
      setSavedSuccess(true);
      setTimeout(() => {
        setSavedSuccess(false);
        setShowDevModal(false);
      }, 1000);
    }
  };

  // Quick mock login with test account
  const handleDevMockLogin = (email: string, name: string) => {
    const devToken = `dev_google:${encodeURIComponent(email)}:${encodeURIComponent(name)}`;
    handleAuthPayload(devToken);
  };

  return (
    <div className="w-full space-y-2">
      {/* If Google Client ID is configured, render official Google Identity Services button */}
      {clientId ? (
        <div className="w-full flex flex-col items-center">
          <div
            ref={googleButtonContainerRef}
            className="w-full flex justify-center min-h-[44px] transition-opacity duration-200"
          />

          {/* Quick config button */}
          <button
            type="button"
            onClick={() => setShowDevModal(true)}
            className="text-[11px] text-gray-400 hover:text-emerald-600 mt-1.5 flex items-center gap-1 transition-colors cursor-pointer"
          >
            <Settings className="w-3 h-3" /> Cấu hình Google Client ID / Chế độ thử nghiệm
          </button>
        </div>
      ) : (
        /* Standard Google Sign-In Button compliant with Google Brand Guidelines */
        <div className="w-full flex flex-col items-center">
          <button
            type="button"
            onClick={handleFallbackClick}
            disabled={loading}
            aria-label={buttonText}
            className="w-full flex items-center justify-center gap-3 px-4 py-3 bg-white border border-gray-300 rounded-xl text-sm font-medium text-gray-700 hover:bg-gray-50 active:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-emerald-500 shadow-sm transition-all disabled:opacity-50 group cursor-pointer"
          >
            {/* Official Google 4-Color SVG Icon */}
            <svg className="w-5 h-5 shrink-0" viewBox="0 0 24 24" aria-hidden="true">
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

          <p className="text-[11px] text-gray-400 mt-1 flex items-center gap-1">
            <span className="inline-block w-1.5 h-1.5 rounded-full bg-amber-400" />
            Bấm nút để cấu hình Google Client ID hoặc thử nghiệm 1-click
          </p>
        </div>
      )}

      {/* Script load error alert */}
      {scriptError && (
        <div className="p-2.5 rounded-xl bg-amber-50 border border-amber-200 text-xs text-amber-800 flex items-center gap-2">
          <AlertCircle className="w-4 h-4 shrink-0 text-amber-600" />
          <span>Không thể tải thư viện Google Identity Services (có thể do trình duyệt chặn quảng cáo).</span>
        </div>
      )}

      {/* General error message */}
      {errorMsg && (
        <div className="p-3 rounded-xl bg-red-50 border border-red-200 text-xs text-red-700 flex items-start gap-2">
          <AlertCircle className="w-4 h-4 shrink-0 text-red-500 mt-0.5" />
          <span>{errorMsg}</span>
        </div>
      )}

      {/* Developer & Setup Assistant Modal (Never Asks for Google Password) */}
      {showDevModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-fade-in font-sans">
          <div className="w-full max-w-lg bg-white rounded-3xl border border-gray-200 shadow-2xl relative overflow-hidden flex flex-col max-h-[90vh]">
            {/* Header */}
            <div className="p-6 border-b border-gray-100 flex items-center justify-between bg-gradient-to-r from-emerald-50/50 to-white">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-2xl bg-white border border-gray-200 shadow-sm flex items-center justify-center">
                  <svg className="w-5 h-5" viewBox="0 0 24 24">
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
                </div>
                <div>
                  <h3 className="text-base font-bold text-gray-900 flex items-center gap-2">
                    Google OAuth (Môi trường Dev)
                    <span className="px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800 text-[10px] font-semibold tracking-wide">
                      DEV MODE
                    </span>
                  </h3>
                  <p className="text-xs text-gray-500">
                    Cấu hình Client ID hoặc đăng nhập nhanh với tài khoản thử nghiệm
                  </p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setShowDevModal(false)}
                className="p-2 rounded-xl text-gray-400 hover:text-gray-700 hover:bg-gray-100 transition-colors"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Navigation Tabs */}
            <div className="flex border-b border-gray-100 px-6 pt-3 gap-6 bg-gray-50/50">
              <button
                type="button"
                onClick={() => setActiveTab('real_oauth')}
                className={`pb-3 text-xs font-semibold flex items-center gap-2 border-b-2 transition-all cursor-pointer ${
                  activeTab === 'real_oauth'
                    ? 'border-emerald-600 text-emerald-700'
                    : 'border-transparent text-gray-500 hover:text-gray-900'
                }`}
              >
                <ShieldCheck className="w-4 h-4" />
                Cấu hình Google Client ID Thật
              </button>
              <button
                type="button"
                onClick={() => setActiveTab('dev_mock')}
                className={`pb-3 text-xs font-semibold flex items-center gap-2 border-b-2 transition-all cursor-pointer ${
                  activeTab === 'dev_mock'
                    ? 'border-emerald-600 text-emerald-700'
                    : 'border-transparent text-gray-500 hover:text-gray-900'
                }`}
              >
                <Sparkles className="w-4 h-4" />
                Đăng nhập Nhanh (1-Click Dev)
              </button>
            </div>

            {/* Content Area */}
            <div className="p-6 overflow-y-auto space-y-5 text-sm">
              {activeTab === 'real_oauth' ? (
                <div className="space-y-4">
                  <div className="p-3.5 rounded-2xl bg-emerald-50/70 border border-emerald-100 text-xs text-emerald-800 space-y-1">
                    <p className="font-semibold flex items-center gap-1.5">
                      <ShieldCheck className="w-4 h-4 text-emerald-600" />
                      Google Identity Services chuẩn quốc tế:
                    </p>
                    <p className="text-emerald-700 leading-relaxed">
                      Dán OAuth 2.0 Web Client ID từ{' '}
                      <span className="font-semibold">Google Cloud Console</span> để bật nút Google Sign-In chính thức và xác thực tài khoản Google thật.
                    </p>
                  </div>

                  <form onSubmit={handleSaveClientId} className="space-y-3">
                    <label htmlFor="google-client-id-input" className="block text-xs font-semibold text-gray-700">
                      Google OAuth Web Client ID
                    </label>
                    <div className="space-y-1">
                      <input
                        id="google-client-id-input"
                        type="text"
                        value={customClientId}
                        onChange={(e) => setCustomClientId(e.target.value)}
                        placeholder="xxxx.apps.googleusercontent.com"
                        className="w-full px-3.5 py-2.5 rounded-xl border border-gray-300 text-gray-900 text-xs font-mono focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500"
                      />
                      <p className="text-[11px] text-gray-500">
                        Giá trị sẽ được lưu vào <code className="font-mono text-emerald-700">localStorage</code> và tự động khởi tạo Google GIS SDK.
                      </p>
                    </div>

                    <div className="flex items-center justify-between pt-2">
                      {savedSuccess ? (
                        <span className="text-xs text-emerald-600 font-semibold flex items-center gap-1">
                          <Check className="w-4 h-4" /> Đã lưu thành công! Đang kích hoạt...
                        </span>
                      ) : (
                        <span className="text-[11px] text-gray-400">
                          {clientId ? '✓ Đang kích hoạt Client ID' : 'Chưa có Client ID'}
                        </span>
                      )}

                      <div className="flex gap-2">
                        {clientId && (
                          <button
                            type="button"
                            onClick={() => {
                              localStorage.removeItem('google_client_id');
                              setClientId(envClientId);
                              setCustomClientId('');
                            }}
                            className="px-3 py-2 text-xs font-medium text-gray-600 hover:text-red-600 rounded-xl hover:bg-gray-100 transition-colors"
                          >
                            Xóa cấu hình
                          </button>
                        )}
                        <button
                          type="submit"
                          disabled={!customClientId.trim()}
                          className="px-4 py-2 bg-emerald-600 text-white rounded-xl text-xs font-semibold hover:bg-emerald-700 transition-all shadow-sm disabled:opacity-50"
                        >
                          Lưu & Kích hoạt
                        </button>
                      </div>
                    </div>
                  </form>

                  <div className="pt-2 border-t border-gray-100 text-[11px] text-gray-500 space-y-1">
                    <p className="font-semibold text-gray-700">Hướng dẫn nhanh Google Cloud Console:</p>
                    <ol className="list-decimal list-inside space-y-0.5 text-gray-600">
                      <li>Tạo dự án mới tại Google Cloud Console.</li>
                      <li>Vào Credentials → Create Credentials → OAuth client ID.</li>
                      <li>Chọn Application type: <span className="font-medium">Web application</span>.</li>
                      <li>Thêm Authorized JavaScript origins: <code className="bg-gray-100 px-1 py-0.5 rounded">http://localhost:3000</code></li>
                    </ol>
                  </div>
                </div>
              ) : (
                <div className="space-y-4">
                  <div className="p-3.5 rounded-2xl bg-blue-50/70 border border-blue-100 text-xs text-blue-800 space-y-1">
                    <p className="font-semibold flex items-center gap-1.5">
                      <Sparkles className="w-4 h-4 text-blue-600" />
                      Mô phỏng đăng nhập tức thì (Không cần GCP):
                    </p>
                    <p className="text-blue-700 leading-relaxed">
                      Hệ thống tự động cấp token phiên làm việc có vai trò <span className="font-semibold">Author</span> qua API backend <code className="font-mono text-blue-900">POST /api/v1/auth/google</code> mà không yêu cầu nhập mật khẩu.
                    </p>
                  </div>

                  {/* Preset Accounts */}
                  <div className="space-y-2">
                    <p className="text-xs font-semibold text-gray-700">Tài khoản mẫu đề xuất:</p>
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
                      <button
                        type="button"
                        onClick={() => handleDevMockLogin('chef.vietnam@gmail.com', 'Bếp Trưởng Việt')}
                        disabled={loading}
                        className="p-3 rounded-2xl border border-gray-200 hover:border-emerald-500 hover:bg-emerald-50/40 text-left transition-all flex items-start gap-3 group cursor-pointer"
                      >
                        <div className="w-8 h-8 rounded-xl bg-emerald-100 text-emerald-800 flex items-center justify-center text-xs font-bold shrink-0">
                          🍳
                        </div>
                        <div className="min-w-0 flex-1">
                          <p className="text-xs font-bold text-gray-900 truncate group-hover:text-emerald-700">
                            Bếp Trưởng Việt
                          </p>
                          <p className="text-[11px] text-gray-500 truncate">chef.vietnam@gmail.com</p>
                          <span className="inline-block mt-1 px-1.5 py-0.5 bg-gray-100 text-gray-600 rounded text-[9px] font-medium">
                            Role: Author
                          </span>
                        </div>
                      </button>

                      <button
                        type="button"
                        onClick={() => handleDevMockLogin('demo.author@culinaryblog.vn', 'Google Author Demo')}
                        disabled={loading}
                        className="p-3 rounded-2xl border border-gray-200 hover:border-emerald-500 hover:bg-emerald-50/40 text-left transition-all flex items-start gap-3 group cursor-pointer"
                      >
                        <div className="w-8 h-8 rounded-xl bg-blue-100 text-blue-800 flex items-center justify-center text-xs font-bold shrink-0">
                          👨‍🍳
                        </div>
                        <div className="min-w-0 flex-1">
                          <p className="text-xs font-bold text-gray-900 truncate group-hover:text-blue-700">
                            Google Author Demo
                          </p>
                          <p className="text-[11px] text-gray-500 truncate">demo.author@culinaryblog.vn</p>
                          <span className="inline-block mt-1 px-1.5 py-0.5 bg-gray-100 text-gray-600 rounded text-[9px] font-medium">
                            Role: Author
                          </span>
                        </div>
                      </button>
                    </div>
                  </div>

                  {/* Custom Test Account Form */}
                  <div className="pt-2 border-t border-gray-100 space-y-3">
                    <p className="text-xs font-semibold text-gray-700">Hoặc tùy chọn email test bất kỳ:</p>
                    <div className="space-y-2">
                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                        <input
                          type="text"
                          value={testName}
                          onChange={(e) => setTestName(e.target.value)}
                          placeholder="Tên hiển thị"
                          className="px-3 py-2 rounded-xl border border-gray-300 text-xs text-gray-900 focus:outline-none focus:ring-1 focus:ring-emerald-500"
                        />
                        <input
                          type="email"
                          value={testEmail}
                          onChange={(e) => setTestEmail(e.target.value)}
                          placeholder="test.user@gmail.com"
                          className="px-3 py-2 rounded-xl border border-gray-300 text-xs text-gray-900 focus:outline-none focus:ring-1 focus:ring-emerald-500"
                        />
                      </div>
                      <button
                        type="button"
                        onClick={() => handleDevMockLogin(testEmail, testName)}
                        disabled={loading || !testEmail.includes('@')}
                        className="w-full py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold rounded-xl text-xs transition-all flex items-center justify-center gap-2 disabled:opacity-50 shadow-sm cursor-pointer"
                      >
                        <UserCheck className="w-4 h-4" />
                        {loading ? 'Đang xác thực...' : 'Đăng nhập với email này'}
                      </button>
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Footer */}
            <div className="p-4 px-6 bg-gray-50 border-t border-gray-100 flex items-center justify-between text-xs text-gray-500">
              <span className="text-[11px]">Culinary Blog • Identity & Google OAuth 2.0</span>
              <button
                type="button"
                onClick={() => setShowDevModal(false)}
                className="text-gray-600 hover:text-gray-900 font-medium"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
