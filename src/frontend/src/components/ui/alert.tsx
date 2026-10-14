import * as React from 'react';
import { cn } from '@/lib/utils';
import { AlertCircle, CheckCircle2, Info, AlertTriangle } from 'lucide-react';

export interface AlertProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'destructive' | 'success' | 'warning' | 'info';
}

export const Alert = React.forwardRef<HTMLDivElement, AlertProps>(
  ({ className, variant = 'default', children, ...props }, ref) => {
    const variants: Record<string, { container: string; icon: React.ReactNode }> = {
      default: {
        container: 'bg-gray-50 text-gray-900 border-gray-200',
        icon: <Info className="h-5 w-5 text-gray-500" />,
      },
      destructive: {
        container: 'bg-rose-50 text-rose-900 border-rose-200',
        icon: <AlertCircle className="h-5 w-5 text-rose-600" />,
      },
      success: {
        container: 'bg-emerald-50 text-emerald-900 border-emerald-200',
        icon: <CheckCircle2 className="h-5 w-5 text-emerald-600" />,
      },
      warning: {
        container: 'bg-amber-50 text-amber-900 border-amber-200',
        icon: <AlertTriangle className="h-5 w-5 text-amber-600" />,
      },
      info: {
        container: 'bg-sky-50 text-sky-900 border-sky-200',
        icon: <Info className="h-5 w-5 text-sky-600" />,
      },
    };

    const cfg = variants[variant] || variants.default;

    return (
      <div
        ref={ref}
        role="alert"
        className={cn(
          'relative w-full rounded-2xl border p-4 flex items-start gap-3 text-sm shadow-sm',
          cfg.container,
          className
        )}
        {...props}
      >
        <div className="shrink-0 mt-0.5">{cfg.icon}</div>
        <div className="flex-1 min-w-0">{children}</div>
      </div>
    );
  }
);
Alert.displayName = 'Alert';

export const AlertTitle = React.forwardRef<HTMLParagraphElement, React.HTMLAttributes<HTMLHeadingElement>>(
  ({ className, ...props }, ref) => (
    <h5 ref={ref} className={cn('mb-1 font-semibold leading-none tracking-tight', className)} {...props} />
  )
);
AlertTitle.displayName = 'AlertTitle';

export const AlertDescription = React.forwardRef<HTMLParagraphElement, React.HTMLAttributes<HTMLParagraphElement>>(
  ({ className, ...props }, ref) => (
    <div ref={ref} className={cn('text-xs opacity-90 leading-relaxed', className)} {...props} />
  )
);
AlertDescription.displayName = 'AlertDescription';
