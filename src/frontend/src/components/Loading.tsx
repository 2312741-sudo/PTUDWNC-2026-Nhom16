import React from 'react';
import { Loader2 } from 'lucide-react';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';

export interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg' | 'xl';
  text?: string;
  className?: string;
}

export function LoadingSpinner({ size = 'md', text, className }: LoadingSpinnerProps) {
  const sizeMap = {
    sm: 'w-4 h-4',
    md: 'w-7 h-7',
    lg: 'w-10 h-10',
    xl: 'w-14 h-14',
  };

  return (
    <div className={cn('flex flex-col items-center justify-center gap-3 text-emerald-600', className)}>
      <Loader2 className={cn('animate-spin text-emerald-600', sizeMap[size])} />
      {text && <p className="text-sm font-medium text-gray-500 animate-pulse">{text}</p>}
    </div>
  );
}

export function LoadingPage({ text = 'Đang tải dữ liệu...' }: { text?: string }) {
  return (
    <div className="min-h-[50vh] flex items-center justify-center py-20 px-4">
      <div className="bg-white/80 backdrop-blur-md p-8 rounded-3xl border border-gray-100 shadow-xl shadow-emerald-500/5 flex flex-col items-center max-w-sm w-full text-center">
        <div className="w-16 h-16 rounded-2xl bg-emerald-50 flex items-center justify-center mb-4 ring-8 ring-emerald-50/50">
          <LoadingSpinner size="lg" />
        </div>
        <h3 className="text-base font-bold text-gray-900 mb-1">Culinary Blog</h3>
        <p className="text-sm text-gray-500">{text}</p>
      </div>
    </div>
  );
}

export function RecipeCardSkeleton() {
  return (
    <div className="rounded-2xl border border-gray-100 bg-white overflow-hidden shadow-sm flex flex-col h-full">
      <Skeleton className="w-full h-48 rounded-none" />
      <div className="p-5 flex-1 flex flex-col space-y-3">
        <div className="flex items-center justify-between">
          <Skeleton className="h-5 w-20 rounded-full" />
          <Skeleton className="h-4 w-16" />
        </div>
        <Skeleton className="h-6 w-3/4" />
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-4 w-2/3" />
        <div className="mt-auto pt-4 border-t border-gray-50 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Skeleton className="w-7 h-7 rounded-full" />
            <Skeleton className="h-4 w-20" />
          </div>
          <Skeleton className="h-4 w-12" />
        </div>
      </div>
    </div>
  );
}

export function RecipeListSkeleton({ count = 6 }: { count?: number }) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
      {Array.from({ length: count }).map((_, i) => (
        <RecipeCardSkeleton key={`skeleton-${i}`} />
      ))}
    </div>
  );
}

export default LoadingSpinner;
