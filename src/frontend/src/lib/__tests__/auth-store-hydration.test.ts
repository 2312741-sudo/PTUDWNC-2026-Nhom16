import { renderHook } from '@testing-library/react';
import { useAuthStore } from '../auth-store';

/**
 * Regression cho hydration mismatch trên Header (SSR: chưa đăng nhập / client: đã đăng nhập).
 * Lần render ĐẦU TIÊN phải cố định là "chưa đăng nhập" dù localStorage có session,
 * còn useEffect sau mount mới đọc storage và cập nhật.
 */
describe('useAuthStore — first render must be hydration-safe', () => {
  const user = {
    id: 'u-1',
    email: 'hydrate@culinaryblog.vn',
    fullName: 'Hydrate User',
    userName: 'hydrate_user',
    displayName: 'Hydrate User',
    roles: ['Author'],
  };

  beforeEach(() => {
    localStorage.clear();
  });

  it('does not read localStorage during the initial render', () => {
    localStorage.setItem('accessToken', 'token-hydration');
    localStorage.setItem('refreshToken', 'refresh-hydration');
    localStorage.setItem('user', JSON.stringify(user));

    const firstRenders: boolean[] = [];

    const { result } = renderHook(() => {
      const state = useAuthStore();
      firstRenders.push(state.isAuthenticated);
      return state;
    });

    // Lần render đầu (khớp HTML server) — đây là giá trị từng gây mismatch.
    expect(firstRenders[0]).toBe(false);

    // Sau khi mount, effect đọc storage và chuyển sang trạng thái thật.
    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user?.email).toBe(user.email);
  });

  it('stays unauthenticated when localStorage has no session', () => {
    const { result } = renderHook(() => useAuthStore());
    expect(result.current.isAuthenticated).toBe(false);
  });
});
