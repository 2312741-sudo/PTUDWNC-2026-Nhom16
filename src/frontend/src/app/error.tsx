'use client';

import { useEffect } from 'react';
import ErrorMessage from '@/components/ErrorMessage';

export default function RootError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error('Next.js Root Error Boundary caught:', error);
  }, [error]);

  return (
    <div className="min-h-[calc(100vh-8rem)] flex items-center justify-center p-4">
      <ErrorMessage
        title="Đã xảy ra sự cố không mong muốn"
        error={error}
        onRetry={() => reset()}
      />
    </div>
  );
}
