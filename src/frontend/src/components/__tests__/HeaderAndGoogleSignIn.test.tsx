import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import Header from '../Header';
import GoogleSignInButton from '../GoogleSignInButton';
import { authStore } from '@/lib/auth-store';
import type { User } from '@/types/auth';

jest.mock('next/navigation', () => ({
  usePathname: () => '/',
  useRouter: () => ({ push: jest.fn(), replace: jest.fn(), prefetch: jest.fn() }),
}));

describe('Header Admin Link Visibility & GoogleSignInButton Prod Guard', () => {
  const authorUser: User = {
    id: 'u-author',
    email: 'author@culinaryblog.vn',
    fullName: 'Tác giả Thường',
    userName: 'author_normal',
    displayName: 'Tác giả Thường',
    roles: ['Author'],
  };

  const adminUser: User = {
    id: 'u-admin',
    email: 'admin@culinaryblog.vn',
    fullName: 'Quản trị viên',
    userName: 'admin_user',
    displayName: 'Quản trị viên',
    roles: ['Admin', 'Author'],
  };

  beforeEach(() => {
    localStorage.clear();
    authStore.logout();
  });

  it('hides admin navigation links for unauthenticated guests', () => {
    render(<Header />);
    expect(screen.queryByRole('link', { name: 'Quản trị DM' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Người dùng' })).not.toBeInTheDocument();
  });

  it('hides admin navigation links for non-admin Author users', () => {
    authStore.setAuth({
      accessToken: 'token-author',
      user: authorUser,
    });

    render(<Header />);
    expect(screen.getByRole('link', { name: 'Công thức của tôi' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Quản trị DM' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Người dùng' })).not.toBeInTheDocument();
  });

  it('shows admin navigation links for Admin users', () => {
    authStore.setAuth({
      accessToken: 'token-admin',
      user: adminUser,
    });

    render(<Header />);
    expect(screen.getByRole('link', { name: 'Quản trị DM' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Người dùng' })).toBeInTheDocument();
  });

  it('does not expose Dev Mode mock login modal when NODE_ENV is production and Client ID is missing', () => {
    const originalEnv = process.env.NODE_ENV;
    const originalClientId = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID;
    Object.defineProperty(process.env, 'NODE_ENV', { value: 'production', configurable: true });
    delete process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID;

    try {
      render(<GoogleSignInButton />);
      expect(screen.queryByText(/Cấu hình Google Client ID \/ Chế độ thử nghiệm/i)).not.toBeInTheDocument();
      fireEvent.click(screen.getByRole('button', { name: 'Tiếp tục với Google' }));
      expect(screen.queryByText(/Đăng nhập Nhanh \(1-Click Dev\)/i)).not.toBeInTheDocument();
      expect(screen.getByText(/Đăng nhập Google chưa được kích hoạt/i)).toBeInTheDocument();
    } finally {
      Object.defineProperty(process.env, 'NODE_ENV', { value: originalEnv, configurable: true });
      if (originalClientId !== undefined) {
        process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID = originalClientId;
      }
    }
  });
});
