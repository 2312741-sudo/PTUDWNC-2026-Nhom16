'use client';

import { create } from 'zustand';
import { User, AuthResponse } from '@/types/auth';

export interface AuthState {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  isInitialized: boolean;

  // Actions
  setAuth: (user: User, accessToken: string, refreshToken?: string | null) => void;
  setAuthFromResponse: (response: AuthResponse) => void;
  updateUser: (partialUser: Partial<User>) => void;
  logout: () => void;
  initialize: () => void;
  setLoading: (isLoading: boolean) => void;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  accessToken: null,
  refreshToken: null,
  isAuthenticated: false,
  isLoading: false,
  isInitialized: false,

  setAuth: (user: User, accessToken: string, refreshToken?: string | null) => {
    if (typeof window !== 'undefined') {
      try {
        localStorage.setItem('accessToken', accessToken);
        if (refreshToken) {
          localStorage.setItem('refreshToken', refreshToken);
        } else {
          localStorage.removeItem('refreshToken');
        }
        localStorage.setItem('user', JSON.stringify(user));
        // Dispatch storage event so cross-tab or non-zustand listeners catch it
        window.dispatchEvent(new Event('auth-state-change'));
      } catch (err) {
        console.error('Failed to save auth to localStorage:', err);
      }
    }

    set({
      user,
      accessToken,
      refreshToken: refreshToken ?? null,
      isAuthenticated: true,
      isLoading: false,
      isInitialized: true,
    });
  },

  setAuthFromResponse: (response: AuthResponse) => {
    get().setAuth(response.user, response.accessToken, response.refreshToken);
  },

  updateUser: (partialUser: Partial<User>) => {
    const currentUser = get().user;
    if (!currentUser) return;

    const updatedUser = { ...currentUser, ...partialUser };
    if (typeof window !== 'undefined') {
      try {
        localStorage.setItem('user', JSON.stringify(updatedUser));
        window.dispatchEvent(new Event('auth-state-change'));
      } catch (err) {
        console.error('Failed to update user in localStorage:', err);
      }
    }

    set({ user: updatedUser });
  },

  logout: () => {
    if (typeof window !== 'undefined') {
      try {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        window.dispatchEvent(new Event('auth-state-change'));
      } catch (err) {
        console.error('Failed to clear auth from localStorage:', err);
      }
    }

    set({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
      isLoading: false,
      isInitialized: true,
    });
  },

  initialize: () => {
    if (typeof window === 'undefined') return;

    try {
      const token = localStorage.getItem('accessToken');
      const refreshToken = localStorage.getItem('refreshToken');
      const storedUser = localStorage.getItem('user');

      if (token && storedUser) {
        const user = JSON.parse(storedUser) as User;
        set({
          user,
          accessToken: token,
          refreshToken,
          isAuthenticated: true,
          isInitialized: true,
          isLoading: false,
        });
      } else {
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          isAuthenticated: false,
          isInitialized: true,
          isLoading: false,
        });
      }
    } catch (err) {
      console.error('Failed to load auth state from localStorage:', err);
      set({ isInitialized: true, isLoading: false });
    }
  },

  setLoading: (isLoading: boolean) => {
    set({ isLoading });
  },
}));

/**
 * Non-reactive helper to get token anywhere
 */
export function getAccessToken(): string | null {
  if (typeof window !== 'undefined') {
    return localStorage.getItem('accessToken');
  }
  return null;
}

/**
 * Helper to check role
 */
export function hasRole(user: User | null, role: string): boolean {
  if (!user || !user.roles) return false;
  return user.roles.some((r) => r.toLowerCase() === role.toLowerCase());
}
