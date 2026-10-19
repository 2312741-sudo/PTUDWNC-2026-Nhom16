import { Loader2 } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface LoadingProps {
  message?: string;
  size?: 'sm' | 'md' | 'lg';
  fullPage?: boolean;
  className?: string;
}

/**
 * Component Hiển thị trạng thái đang tải dùng chung (Lab 6)
 */
export default function Loading({
  message = 'Đang tải dữ liệu...',
  size = 'md',
  fullPage = false,
  className,
}: LoadingProps) {
  const sizeMap = {
    sm: 'w-5 h-5',
    md: 'w-8 h-8',
    lg: 'w-12 h-12',
  };

  const content = (
    <div className={cn('flex flex-col items-center justify-center gap-3 p-6 text-center', className)}>
      <Loader2 className={cn('animate-spin text-emerald-600', sizeMap[size])} />
      {message && <p className="text-sm font-medium text-gray-500 animate-pulse">{message}</p>}
    </div>
  );

  if (fullPage) {
    return (
      <div className="min-h-[50vh] flex items-center justify-center w-full">
        {content}
      </div>
    );
  }

  return content;
}
