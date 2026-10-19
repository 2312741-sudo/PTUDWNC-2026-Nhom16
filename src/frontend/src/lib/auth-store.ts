'use client';

import { useState, useEffect, useCallback } from 'react';
import type { UserDto } from '@/types/auth';

/**
 * Interface cho trạng thái xác thực trong Auth Store (Lab 6)
 */
export interface AuthState {
  user: UserDto | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}

/**
 * Event name dùng để đồng bộ trạng thái Auth giữa các component trên cùng một trang
 */
const AUTH_CHANGE_EVENT = 'culinary_auth_change';

// Cache trạng thái auth trong bộ nhớ client
let currentAuthState: AuthState = {
  user: null,
  accessToken: null,
  refreshToken: null,
  isAuthenticated: false,
  isLoading: true,
};

// Đọc trạng thái ban đầu từ localStorage (chỉ chạy phía client)
function readStorage(): Omit<AuthState, 'isLoading'> {
  if (typeof window === 'undefined') {
    return { user: null, accessToken: null, refreshToken: null, isAuthenticated: false };
  }

  try {
    const accessToken = localStorage.getItem('accessToken');
    const refreshToken = localStorage.getItem('refreshToken');
    const userStr = localStorage.getItem('user');
    const user = userStr ? (JSON.parse(userStr) as UserDto) : null;

    return {
      user,
      accessToken,
      refreshToken,
      isAuthenticated: !!accessToken && !!user,
    };
  } catch (err) {
    console.warn('Lỗi đọc Auth từ localStorage:', err);
    return { user: null, accessToken: null, refreshToken: null, isAuthenticated: false };
  }
}

// Cập nhật và phát sự kiện đồng bộ
function notifyAuthChange(newState: Partial<AuthState>) {
  currentAuthState = { ...currentAuthState, ...newState, isLoading: false };
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new CustomEvent(AUTH_CHANGE_EVENT, { detail: currentAuthState }));
  }
}

/**
 * Auth Store Controller cung cấp các phương thức thao tác trực tiếp không cần hook
 */
export const authStore = {
  getState(): AuthState {
    return { ...currentAuthState };
  },

  setAuth(data: { accessToken: string; refreshToken?: string | null; user: UserDto }) {
    if (typeof window !== 'undefined') {
      localStorage.setItem('accessToken', data.accessToken);
      if (data.refreshToken) {
        localStorage.setItem('refreshToken', data.refreshToken);
      }
      localStorage.setItem('user', JSON.stringify(data.user));
      if (data.user?.email) {
        localStorage.setItem('lastRegisteredEmail', data.user.email);
      }
    }

    notifyAuthChange({
      accessToken: data.accessToken,
      refreshToken: data.refreshToken || null,
      user: data.user,
      isAuthenticated: true,
    });
  },

  setUser(user: UserDto) {
    if (typeof window !== 'undefined') {
      localStorage.setItem('user', JSON.stringify(user));
    }
    notifyAuthChange({ user });
  },

  logout() {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('user');
    }

    notifyAuthChange({
      accessToken: null,
      refreshToken: null,
      user: null,
      isAuthenticated: false,
    });
  },
};

/**
 * React Hook useAuthStore giúp các component (Header, Dashboard, Form)
 * tự động re-render tức thì khi trạng thái đăng nhập thay đổi.
 */
export function useAuthStore() {
  const [state, setState] = useState<AuthState>(() => {
    const initial = readStorage();
    return {
      ...initial,
      isLoading: typeof window === 'undefined',
    };
  });

  useEffect(() => {
    // Đọc trạng thái thực tế sau khi component mounted trên client
    const current = readStorage();
    setState({ ...current, isLoading: false });

    // Lắng nghe thay đổi trạng thái xác thực trong cùng tab
    const handleLocalChange = (e: Event) => {
      const customEvent = e as CustomEvent<AuthState>;
      if (customEvent.detail) {
        setState({ ...customEvent.detail, isLoading: false });
      } else {
        setState({ ...readStorage(), isLoading: false });
      }
    };

    // Lắng nghe thay đổi storage từ các tab trình duyệt khác
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === 'accessToken' || e.key === 'user' || e.key === 'refreshToken') {
        setState({ ...readStorage(), isLoading: false });
      }
    };

    window.addEventListener(AUTH_CHANGE_EVENT, handleLocalChange);
    window.addEventListener('storage', handleStorageChange);

    return () => {
      window.removeEventListener(AUTH_CHANGE_EVENT, handleLocalChange);
      window.removeEventListener('storage', handleStorageChange);
    };
  }, []);

  const login = useCallback((data: { accessToken: string; refreshToken?: string | null; user: UserDto }) => {
    authStore.setAuth(data);
  }, []);

  const logout = useCallback(() => {
    authStore.logout();
  }, []);

  const updateUser = useCallback((user: UserDto) => {
    authStore.setUser(user);
  }, []);

  return {
    ...state,
    login,
    logout,
    updateUser,
  };
}
