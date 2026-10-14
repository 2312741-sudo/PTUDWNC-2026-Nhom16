import React from 'react';
import Link from 'next/link';
import { render, screen, fireEvent } from '@testing-library/react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Card, CardHeader, CardTitle, CardContent } from '../ui/card';
import { Badge } from '../ui/badge';
import { Alert, AlertTitle, AlertDescription } from '../ui/alert';
import { Pagination } from '../Pagination';
import { LoadingSpinner, LoadingPage } from '../Loading';
import { ErrorMessage, InlineErrorAlert } from '../ErrorMessage';

describe('Common UI & Reusable Components', () => {
  describe('Button Component', () => {
    it('renders with text and handles click event', () => {
      const handleClick = jest.fn();
      render(<Button onClick={handleClick}>Bắt đầu nấu</Button>);

      const btn = screen.getByRole('button', { name: /Bắt đầu nấu/i });
      expect(btn).toBeInTheDocument();
      fireEvent.click(btn);
      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('shows loading spinner when isLoading is true and disables button', () => {
      render(<Button isLoading>Lưu công thức</Button>);
      const btn = screen.getByRole('button', { name: /Lưu công thức/i });
      expect(btn).toBeDisabled();
    });

    it('renders as child component when asChild is true', () => {
      render(
        <Button asChild>
          <Link href="/recipes">Khám phá</Link>
        </Button>
      );
      const link = screen.getByRole('link', { name: /Khám phá/i });
      expect(link).toBeInTheDocument();
      expect(link).toHaveAttribute('href', '/recipes');
    });
  });

  describe('Input Component', () => {
    it('renders text input and displays error message when provided', () => {
      render(<Input placeholder="Nhập email" error="Email không hợp lệ" />);
      expect(screen.getByPlaceholderText('Nhập email')).toBeInTheDocument();
      expect(screen.getByText('Email không hợp lệ')).toBeInTheDocument();
    });
  });

  describe('Card Component', () => {
    it('renders Card with header and content', () => {
      render(
        <Card>
          <CardHeader>
            <CardTitle>Phở Hà Nội</CardTitle>
          </CardHeader>
          <CardContent>Món ngon truyền thống</CardContent>
        </Card>
      );
      expect(screen.getByText('Phở Hà Nội')).toBeInTheDocument();
      expect(screen.getByText('Món ngon truyền thống')).toBeInTheDocument();
    });
  });

  describe('Badge Component', () => {
    it('renders badge with correct variant styling', () => {
      render(<Badge variant="success">Hoàn thành</Badge>);
      expect(screen.getByText('Hoàn thành')).toBeInTheDocument();
    });
  });

  describe('Alert Component', () => {
    it('renders alert with title and description', () => {
      render(
        <Alert variant="warning">
          <AlertTitle>Lưu ý nhiệt độ</AlertTitle>
          <AlertDescription>Để lửa nhỏ khi ninh xương</AlertDescription>
        </Alert>
      );
      expect(screen.getByRole('alert')).toBeInTheDocument();
      expect(screen.getByText('Lưu ý nhiệt độ')).toBeInTheDocument();
      expect(screen.getByText('Để lửa nhỏ khi ninh xương')).toBeInTheDocument();
    });
  });

  describe('Pagination Component', () => {
    it('renders page numbers and handles page change click', () => {
      const onPageChange = jest.fn();
      render(<Pagination currentPage={2} totalPages={5} onPageChange={onPageChange} />);

      const page3Btn = screen.getByRole('button', { name: 'Trang 3' });
      expect(page3Btn).toBeInTheDocument();
      fireEvent.click(page3Btn);
      expect(onPageChange).toHaveBeenCalledWith(3);
    });

    it('returns null if totalPages <= 1', () => {
      const { container } = render(<Pagination currentPage={1} totalPages={1} />);
      expect(container.firstChild).toBeNull();
    });
  });

  describe('Loading Components', () => {
    it('renders LoadingSpinner with custom text', () => {
      render(<LoadingSpinner text="Đang tải dữ liệu..." />);
      expect(screen.getByText('Đang tải dữ liệu...')).toBeInTheDocument();
    });

    it('renders LoadingPage with heading', () => {
      render(<LoadingPage text="Đang chuẩn bị công thức..." />);
      expect(screen.getByText('Culinary Blog')).toBeInTheDocument();
      expect(screen.getByText('Đang chuẩn bị công thức...')).toBeInTheDocument();
    });
  });

  describe('ErrorMessage Components', () => {
    it('renders ErrorMessage with retry button and triggers callback', () => {
      const handleRetry = jest.fn();
      render(
        <ErrorMessage
          title="Lỗi tải bài viết"
          message="Không thể kết nối đến máy chủ"
          onRetry={handleRetry}
        />
      );

      expect(screen.getByText('Lỗi tải bài viết')).toBeInTheDocument();
      expect(screen.getByText('Không thể kết nối đến máy chủ')).toBeInTheDocument();

      const retryBtn = screen.getByRole('button', { name: /Thử lại/i });
      fireEvent.click(retryBtn);
      expect(handleRetry).toHaveBeenCalledTimes(1);
    });

    it('renders InlineErrorAlert with dismiss button', () => {
      const handleDismiss = jest.fn();
      render(<InlineErrorAlert message="Lỗi nhẹ" onDismiss={handleDismiss} />);
      expect(screen.getByText('Lỗi nhẹ')).toBeInTheDocument();

      const dismissBtn = screen.getByRole('button', { name: /Đóng/i });
      fireEvent.click(dismissBtn);
      expect(handleDismiss).toHaveBeenCalledTimes(1);
    });
  });
});
