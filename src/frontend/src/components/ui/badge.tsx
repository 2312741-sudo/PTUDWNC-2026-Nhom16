import * as React from 'react';
import { cn } from '@/lib/utils';

export interface BadgeProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'secondary' | 'destructive' | 'outline' | 'success' | 'warning' | 'info';
}

export function Badge({ className, variant = 'default', ...props }: BadgeProps) {
  const variants: Record<string, string> = {
    default: 'border-transparent bg-emerald-600 text-white shadow-sm hover:bg-emerald-700',
    secondary: 'border-transparent bg-emerald-100 text-emerald-800 hover:bg-emerald-200',
    destructive: 'border-transparent bg-rose-100 text-rose-800 hover:bg-rose-200',
    outline: 'text-gray-700 border-gray-200 bg-white',
    success: 'border-transparent bg-emerald-50 text-emerald-700 ring-1 ring-inset ring-emerald-600/20',
    warning: 'border-transparent bg-amber-50 text-amber-700 ring-1 ring-inset ring-amber-600/20',
    info: 'border-transparent bg-sky-50 text-sky-700 ring-1 ring-inset ring-sky-600/20',
  };

  return (
    <div
      className={cn(
        'inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:ring-offset-2',
        variants[variant],
        className
      )}
      {...props}
    />
  );
}
