import { useAuthStore, getAccessToken, hasRole } from '../auth-store';
import { User } from '@/types/auth';

const mockUser: User = {
  id: 'usr-1',
  email: 'chef@culinary.vn',
  fullName: 'Chef Nguyễn Thanh Tâm',
  userName: 'chef_tam',
  displayName: 'Thanh Tâm',
  roles: ['Author', 'Admin'],
};

describe('Auth Store (Zustand & LocalStorage sync)', () => {
  beforeEach(() => {
    localStorage.clear();
    useAuthStore.getState().logout();
  });

  it('initializes with default empty unauthenticated state', () => {
    const state = useAuthStore.getState();
    expect(state.user).toBeNull();
    expect(state.accessToken).toBeNull();
    expect(state.isAuthenticated).toBe(false);
  });

  it('setAuth updates store and saves to localStorage', () => {
    useAuthStore.getState().setAuth(mockUser, 'test-access-token', 'test-refresh-token');

    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(true);
    expect(state.user?.email).toBe('chef@culinary.vn');
    expect(state.accessToken).toBe('test-access-token');
    expect(state.refreshToken).toBe('test-refresh-token');

    expect(localStorage.getItem('accessToken')).toBe('test-access-token');
    expect(localStorage.getItem('refreshToken')).toBe('test-refresh-token');
    expect(JSON.parse(localStorage.getItem('user')!)).toEqual(mockUser);
    expect(getAccessToken()).toBe('test-access-token');
  });

  it('updateUser partially modifies user state in store and localStorage', () => {
    useAuthStore.getState().setAuth(mockUser, 'test-token');
    useAuthStore.getState().updateUser({ displayName: 'Tâm Nguyễn Pro' });

    const state = useAuthStore.getState();
    expect(state.user?.displayName).toBe('Tâm Nguyễn Pro');
    expect(state.user?.fullName).toBe('Chef Nguyễn Thanh Tâm');

    const stored = JSON.parse(localStorage.getItem('user')!);
    expect(stored.displayName).toBe('Tâm Nguyễn Pro');
  });

  it('logout cleans state and clears localStorage', () => {
    useAuthStore.getState().setAuth(mockUser, 'test-token');
    useAuthStore.getState().logout();

    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(false);
    expect(state.user).toBeNull();
    expect(state.accessToken).toBeNull();
    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(localStorage.getItem('user')).toBeNull();
  });

  it('initialize rehydrates auth state from localStorage', () => {
    localStorage.setItem('accessToken', 'persisted-token');
    localStorage.setItem('refreshToken', 'persisted-refresh');
    localStorage.setItem('user', JSON.stringify(mockUser));

    useAuthStore.getState().initialize();

    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(true);
    expect(state.accessToken).toBe('persisted-token');
    expect(state.user?.email).toBe('chef@culinary.vn');
  });

  it('hasRole checks user permissions correctly', () => {
    expect(hasRole(mockUser, 'Admin')).toBe(true);
    expect(hasRole(mockUser, 'author')).toBe(true);
    expect(hasRole(mockUser, 'Moderator')).toBe(false);
    expect(hasRole(null, 'Admin')).toBe(false);
  });
});
