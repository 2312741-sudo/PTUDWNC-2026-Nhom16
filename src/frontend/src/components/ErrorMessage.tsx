'use client';

import React from 'react';
import Link from 'next/link';
import { AlertCircle, RefreshCw, Home, ArrowLeft } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { getErrorMessage } from '@/lib/errors';

export interface ErrorMessageProps {
  title?: string;
  message?: string | unknown;
  onRetry?: () => void;
  showHomeButton?: boolean;
  className?: string;
}

export function ErrorMessage({
  title = 'Đã xảy ra sự cố',
  message,
  onRetry,
  showHomeButton = true,
  className,
}: ErrorMessageProps) {
  const displayMessage = getErrorMessage(message, 'Không thể tải dữ liệu vào lúc này. Vui lòng thử lại sau.');

  return (
    <div
      role="alert"
      className={cn(
        'w-full max-w-xl mx-auto my-8 p-6 sm:p-8 rounded-3xl bg-white border border-rose-100 shadow-xl shadow-rose-500/5 text-center',
        className
      )}
    >
      <div className="w-14 h-14 mx-auto mb-4 rounded-2xl bg-rose-50 flex items-center justify-center text-rose-600 ring-8 ring-rose-50/50">
        <AlertCircle className="w-7 h-7" />
      </div>

      <h3 className="text-lg sm:text-xl font-bold text-gray-900 mb-2">{title}</h3>
      <p className="text-sm text-gray-600 leading-relaxed mb-6 max-w-md mx-auto">{displayMessage}</p>

      <div className="flex flex-wrap items-center justify-center gap-3">
        {onRetry && (
          <Button onClick={onRetry} variant="default" className="gap-2">
            <RefreshCw className="w-4 h-4" />
            Thử lại
          </Button>
        )}

        {showHomeButton && (
          <Button variant="outline" asChild className="gap-2">
            <Link href="/">
              <Home className="w-4 h-4" />
              Về trang chủ
            </Link>
          </Button>
        )}
      </div>
    </div>
  );
}

export function InlineErrorAlert({
  message,
  className,
  onDismiss,
}: {
  message?: string | unknown;
  className?: string;
  onDismiss?: () => void;
}) {
  if (!message) return null;
  const text = getErrorMessage(message);

  return (
    <div
      role="alert"
      className={cn(
        'flex items-center justify-between gap-3 p-3.5 rounded-xl bg-rose-50 border border-rose-200 text-rose-800 text-sm animate-in fade-in',
        className
      )}
    >
      <div className="flex items-center gap-2.5 min-w-0">
        <AlertCircle className="w-4 h-4 shrink-0 text-rose-600" />
        <span className="truncate">{text}</span>
      </div>
      {onDismiss && (
        <button
          type="button"
          onClick={onDismiss}
          className="text-xs font-semibold text-rose-700 hover:text-rose-900 ml-2 shrink-0 underline"
        >
          Đóng
        </button>
      )}
    </div>
  );
}

export default ErrorMessage;
