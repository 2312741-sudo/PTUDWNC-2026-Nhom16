'use client';

import { useEffect } from 'react';
import { ErrorMessage } from '@/components/ErrorMessage';

export default function RootError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error('Unhandled Root Error:', error);
  }, [error]);

  return (
    <div className="min-h-[60vh] flex items-center justify-center p-4">
      <ErrorMessage
        title="Trang web gặp sự cố tạm thời"
        message={error.message || 'Hệ thống đang được bảo trì hoặc xử lý yêu cầu. Vui lòng thử lại sau.'}
        onRetry={reset}
        showHomeButton={true}
      />
    </div>
  );
}
