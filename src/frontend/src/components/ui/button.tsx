import * as React from 'react';
import { cn } from '@/lib/utils';

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'default' | 'destructive' | 'outline' | 'secondary' | 'ghost' | 'link' | 'emerald';
  size?: 'default' | 'sm' | 'lg' | 'icon';
  isLoading?: boolean;
  asChild?: boolean;
}

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = 'default', size = 'default', isLoading, asChild = false, children, disabled, ...props }, ref) => {
    const baseStyles =
      'inline-flex items-center justify-center rounded-xl text-sm font-semibold transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-500 disabled:pointer-events-none disabled:opacity-50 select-none cursor-pointer';

    const variants: Record<string, string> = {
      default: 'bg-emerald-600 text-white hover:bg-emerald-700 shadow-md shadow-emerald-600/20 active:scale-[0.98]',
      emerald: 'bg-gradient-to-r from-emerald-600 to-teal-600 text-white hover:from-emerald-700 hover:to-teal-700 shadow-lg shadow-emerald-600/25 active:scale-[0.98]',
      destructive: 'bg-rose-600 text-white hover:bg-rose-700 shadow-md shadow-rose-600/20 active:scale-[0.98]',
      outline: 'border border-gray-200 bg-white hover:bg-gray-50 text-gray-700 hover:text-emerald-700 hover:border-emerald-300',
      secondary: 'bg-emerald-50 text-emerald-800 hover:bg-emerald-100',
      ghost: 'hover:bg-gray-100 text-gray-700 hover:text-gray-900',
      link: 'text-emerald-600 underline-offset-4 hover:underline p-0 h-auto',
    };

    const sizes: Record<string, string> = {
      default: 'h-10 px-4 py-2 gap-2',
      sm: 'h-8 rounded-lg px-3 text-xs gap-1.5',
      lg: 'h-12 rounded-2xl px-6 text-base gap-2.5',
      icon: 'h-10 w-10 p-0',
    };

    const combinedClass = cn(baseStyles, variants[variant], sizes[size], className);

    if (asChild && React.isValidElement(children)) {
      return React.cloneElement(children as React.ReactElement<any>, {
        className: cn(combinedClass, (children.props as any)?.className),
        ...props,
      });
    }

    return (
      <button
        ref={ref}
        disabled={disabled || isLoading}
        className={combinedClass}
        {...props}
      >
        {isLoading && (
          <svg
            className="animate-spin -ml-1 mr-2 h-4 w-4 text-current"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            />
          </svg>
        )}
        {children}
      </button>
    );
  }
);
Button.displayName = 'Button';
