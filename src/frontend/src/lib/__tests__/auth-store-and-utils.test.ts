import { cn } from '../utils';
import { ApiException, parseApiError, isUnauthorizedError, isForbiddenError } from '../error';
import { authStore } from '../auth-store';
import type { User } from '@/types/auth';

describe('Lab 6 - Utils & Error Handling & Auth Store', () => {
  describe('cn() utility', () => {
    it('merges tailwind classes cleanly', () => {
      const result = cn('px-4 py-2', 'bg-emerald-600', { 'text-white': true, 'text-black': false });
      expect(result).toContain('px-4');
      expect(result).toContain('bg-emerald-600');
      expect(result).toContain('text-white');
      expect(result).not.toContain('text-black');
    });

    it('resolves conflicting tailwind classes with last-one-wins', () => {
      const result = cn('p-2', 'p-4');
      expect(result).toBe('p-4');
    });
  });

  describe('Error handling utilities', () => {
    it('creates ApiException with proper status and code', () => {
      const err = new ApiException('Token hết hạn', 401, 'auth.token_expired');
      expect(err.message).toBe('Token hết hạn');
      expect(err.status).toBe(401);
      expect(err.code).toBe('auth.token_expired');
      expect(isUnauthorizedError(err)).toBe(true);
      expect(isForbiddenError(err)).toBe(false);
    });

    it('identifies 403 Forbidden correctly', () => {
      const err = new ApiException('Tài khoản bị khóa', 403, 'auth.account_disabled');
      expect(isForbiddenError(err)).toBe(true);
      expect(isUnauthorizedError(err)).toBe(false);
    });

    it('parses error objects and strings into readable messages', () => {
      expect(parseApiError('Lỗi máy chủ')).toBe('Lỗi máy chủ');
      expect(parseApiError(new Error('Failed to fetch'))).toContain('Không thể kết nối đến máy chủ');
      expect(parseApiError({ detail: 'Email đã tồn tại' })).toBe('Email đã tồn tại');
    });
  });

  describe('authStore state management', () => {
    const mockUser: User = {
      id: 'user-001',
      email: 'chef@culinaryblog.vn',
      fullName: 'Bếp Trưởng Test',
      userName: 'chef_test',
      displayName: 'Bếp Trưởng Test',
      roles: ['Author'],
    };

    beforeEach(() => {
      localStorage.clear();
      authStore.logout();
    });

    it('stores and retrieves authentication data', () => {
      authStore.setAuth({
        accessToken: 'access-jwt-token-123',
        refreshToken: 'refresh-token-456',
        user: mockUser,
      });

      const state = authStore.getState();
      expect(state.isAuthenticated).toBe(true);
      expect(state.accessToken).toBe('access-jwt-token-123');
      expect(state.user?.email).toBe('chef@culinaryblog.vn');
      expect(localStorage.getItem('accessToken')).toBe('access-jwt-token-123');
    });

    it('clears state on logout', () => {
      authStore.setAuth({
        accessToken: 'access-jwt-token-123',
        user: mockUser,
      });

      authStore.logout();

      const state = authStore.getState();
      expect(state.isAuthenticated).toBe(false);
      expect(state.accessToken).toBeNull();
      expect(state.user).toBeNull();
      expect(localStorage.getItem('accessToken')).toBeNull();
    });
  });
});
