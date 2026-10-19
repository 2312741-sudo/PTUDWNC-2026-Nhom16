'use client';

import { AlertCircle, RotateCcw } from 'lucide-react';
import { cn } from '@/lib/utils';
import { parseApiError } from '@/lib/error';

export interface ErrorMessageProps {
  title?: string;
  error?: unknown;
  message?: string;
  onRetry?: () => void;
  fullPage?: boolean;
  className?: string;
}

/**
 * Component Hiển thị lỗi dùng chung (Lab 6)
 * Hỗ trợ phân giải lỗi tự động từ parseApiError và nút Thử lại (onRetry)
 */
export default function ErrorMessage({
  title = 'Đã xảy ra lỗi',
  error,
  message,
  onRetry,
  fullPage = false,
  className,
}: ErrorMessageProps) {
  const displayMessage = message || (error ? parseApiError(error) : 'Không thể hoàn thành tác vụ. Vui lòng thử lại sau.');

  const content = (
    <div
      className={cn(
        'max-w-md w-full p-6 rounded-2xl bg-red-50 border border-red-200 text-center space-y-3 shadow-sm',
        className
      )}
    >
      <div className="w-12 h-12 rounded-full bg-red-100 text-red-600 mx-auto flex items-center justify-center">
        <AlertCircle className="w-6 h-6" />
      </div>

      <div className="space-y-1">
        <h4 className="text-base font-bold text-red-900">{title}</h4>
        <p className="text-sm text-red-700 leading-relaxed">{displayMessage}</p>
      </div>

      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="inline-flex items-center gap-2 px-4 py-2 bg-red-600 text-white rounded-xl text-xs font-semibold hover:bg-red-700 transition-colors shadow-sm cursor-pointer"
        >
          <RotateCcw className="w-3.5 h-3.5" />
          <span>Thử lại</span>
        </button>
      )}
    </div>
  );

  if (fullPage) {
    return (
      <div className="min-h-[50vh] flex items-center justify-center p-4 w-full">
        {content}
      </div>
    );
  }

  return content;
}
