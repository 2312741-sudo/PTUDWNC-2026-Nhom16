import * as React from 'react';
import { cn } from '@/lib/utils';

export interface BadgeProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'secondary' | 'destructive' | 'outline' | 'success';
}

const badgeVariants: Record<NonNullable<BadgeProps['variant']>, string> = {
  default: 'border-transparent bg-emerald-600 text-white shadow hover:bg-emerald-700',
  secondary: 'border-transparent bg-gray-100 text-gray-800 hover:bg-gray-200',
  destructive: 'border-transparent bg-red-600 text-white shadow hover:bg-red-700',
  outline: 'border-gray-200 text-gray-700',
  success: 'border-transparent bg-emerald-100 text-emerald-800',
};

export function Badge({ className, variant = 'default', ...props }: BadgeProps) {
  return (
    <div
      className={cn(
        'inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:ring-offset-2',
        badgeVariants[variant],
        className
      )}
      {...props}
    />
  );
}
