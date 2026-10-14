'use client';

import React from 'react';
import Link from 'next/link';
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange?: (page: number) => void;
  createPageUrl?: (page: number) => string;
  className?: string;
  siblingCount?: number;
}

export function Pagination({
  currentPage,
  totalPages,
  onPageChange,
  createPageUrl,
  className,
  siblingCount = 1,
}: PaginationProps) {
  if (totalPages <= 1) return null;

  // Generate page numbers with ellipsis
  const getPageNumbers = () => {
    const totalNumbers = siblingCount * 2 + 3;
    const totalBlocks = totalNumbers + 2;

    if (totalPages <= totalBlocks) {
      return Array.from({ length: totalPages }, (_, i) => i + 1);
    }

    const leftSiblingIndex = Math.max(currentPage - siblingCount, 1);
    const rightSiblingIndex = Math.min(currentPage + siblingCount, totalPages);

    const shouldShowLeftDots = leftSiblingIndex > 2;
    const shouldShowRightDots = rightSiblingIndex < totalPages - 2;

    if (!shouldShowLeftDots && shouldShowRightDots) {
      const leftItemCount = 3 + 2 * siblingCount;
      const leftRange = Array.from({ length: leftItemCount }, (_, i) => i + 1);
      return [...leftRange, '...', totalPages];
    }

    if (shouldShowLeftDots && !shouldShowRightDots) {
      const rightItemCount = 3 + 2 * siblingCount;
      const rightRange = Array.from({ length: rightItemCount }, (_, i) => totalPages - rightItemCount + i + 1);
      return [1, '...', ...rightRange];
    }

    if (shouldShowLeftDots && shouldShowRightDots) {
      const middleRange = Array.from(
        { length: rightSiblingIndex - leftSiblingIndex + 1 },
        (_, i) => leftSiblingIndex + i
      );
      return [1, '...', ...middleRange, '...', totalPages];
    }

    return [];
  };

  const pages = getPageNumbers();

  const renderPageItem = (page: number | string, index: number) => {
    if (page === '...') {
      return (
        <span
          key={`dots-${index}`}
          className="inline-flex items-center justify-center w-9 h-9 text-gray-400 select-none text-sm font-medium"
        >
          •••
        </span>
      );
    }

    const pageNum = Number(page);
    const isActive = pageNum === currentPage;

    const buttonClass = cn(
      'inline-flex items-center justify-center min-w-9 h-9 px-3 rounded-xl text-sm font-semibold transition-all duration-200 select-none cursor-pointer',
      isActive
        ? 'bg-emerald-600 text-white shadow-md shadow-emerald-600/25 ring-2 ring-emerald-600/30'
        : 'text-gray-700 bg-white border border-gray-200 hover:bg-emerald-50 hover:text-emerald-700 hover:border-emerald-300'
    );

    if (createPageUrl) {
      return (
        <Link
          key={pageNum}
          href={createPageUrl(pageNum)}
          aria-label={`Trang ${pageNum}`}
          aria-current={isActive ? 'page' : undefined}
          className={buttonClass}
        >
          {pageNum}
        </Link>
      );
    }

    return (
      <button
        key={pageNum}
        type="button"
        onClick={() => onPageChange?.(pageNum)}
        aria-label={`Trang ${pageNum}`}
        aria-current={isActive ? 'page' : undefined}
        className={buttonClass}
      >
        {pageNum}
      </button>
    );
  };

  const renderNavButton = (
    targetPage: number,
    disabled: boolean,
    icon: React.ReactNode,
    label: string
  ) => {
    const buttonClass = cn(
      'inline-flex items-center justify-center w-9 h-9 rounded-xl border border-gray-200 bg-white text-gray-700 transition-all duration-200 select-none',
      disabled
        ? 'opacity-40 cursor-not-allowed pointer-events-none'
        : 'hover:bg-emerald-50 hover:text-emerald-700 hover:border-emerald-300 cursor-pointer shadow-sm'
    );

    if (createPageUrl && !disabled) {
      return (
        <Link href={createPageUrl(targetPage)} aria-label={label} className={buttonClass}>
          {icon}
        </Link>
      );
    }

    return (
      <button
        type="button"
        disabled={disabled}
        onClick={() => !disabled && onPageChange?.(targetPage)}
        aria-label={label}
        className={buttonClass}
      >
        {icon}
      </button>
    );
  };

  return (
    <nav
      role="navigation"
      aria-label="Phân trang công thức và bài viết"
      className={cn('flex items-center justify-center gap-1.5 sm:gap-2 my-8 select-none', className)}
    >
      {/* First Page */}
      {renderNavButton(1, currentPage <= 1, <ChevronsLeft className="w-4 h-4" />, 'Đến trang đầu')}

      {/* Prev Page */}
      {renderNavButton(
        currentPage - 1,
        currentPage <= 1,
        <ChevronLeft className="w-4 h-4" />,
        'Chuyển đến trang trước'
      )}

      {/* Page Numbers */}
      <div className="flex items-center gap-1 sm:gap-1.5">
        {pages.map((p, idx) => renderPageItem(p, idx))}
      </div>

      {/* Next Page */}
      {renderNavButton(
        currentPage + 1,
        currentPage >= totalPages,
        <ChevronRight className="w-4 h-4" />,
        'Chuyển đến trang sau'
      )}

      {/* Last Page */}
      {renderNavButton(
        totalPages,
        currentPage >= totalPages,
        <ChevronsRight className="w-4 h-4" />,
        'Đến trang cuối'
      )}
    </nav>
  );
}

export default Pagination;
