import Link from 'next/link';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange?: (page: number) => void;
  createPageUrl?: (page: number) => string;
  className?: string;
}

/**
 * Component Phân trang dùng chung (Lab 6)
 * Hỗ trợ cả hai chế độ: SPA Callback (onPageChange) hoặc Next.js URL (createPageUrl)
 */
export default function Pagination({
  currentPage,
  totalPages,
  onPageChange,
  createPageUrl,
  className,
}: PaginationProps) {
  if (totalPages <= 1) return null;

  // Tính toán dải trang hiển thị (tối đa 5 trang kèm dấu ...)
  const getPageNumbers = () => {
    const pages: (number | string)[] = [];
    const delta = 1; // Số trang lân cận hiện tại

    const left = Math.max(2, currentPage - delta);
    const right = Math.min(totalPages - 1, currentPage + delta);

    pages.push(1);

    if (left > 2) {
      pages.push('...');
    }

    for (let i = left; i <= right; i++) {
      pages.push(i);
    }

    if (right < totalPages - 1) {
      pages.push('...');
    }

    if (totalPages > 1) {
      pages.push(totalPages);
    }

    return pages;
  };

  const pages = getPageNumbers();
  const hasPrevious = currentPage > 1;
  const hasNext = currentPage < totalPages;

  const renderPageItem = (page: number | string, index: number) => {
    if (page === '...') {
      return (
        <span
          key={`ellipsis-${index}`}
          className="w-10 h-10 flex items-center justify-center text-gray-400 text-sm select-none"
        >
          ...
        </span>
      );
    }

    const pageNum = Number(page);
    const isActive = pageNum === currentPage;
    const buttonStyles = cn(
      'w-10 h-10 rounded-xl text-sm font-semibold flex items-center justify-center transition-all cursor-pointer',
      isActive
        ? 'bg-emerald-600 text-white shadow-md shadow-emerald-600/20'
        : 'bg-white text-gray-700 border border-gray-200 hover:bg-gray-50 hover:border-gray-300'
    );

    if (createPageUrl) {
      return (
        <Link
          key={pageNum}
          href={createPageUrl(pageNum)}
          aria-label={`Trang ${pageNum}`}
          aria-current={isActive ? 'page' : undefined}
          className={buttonStyles}
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
        className={buttonStyles}
      >
        {pageNum}
      </button>
    );
  };

  return (
    <nav
      aria-label="Phân trang"
      className={cn('flex items-center justify-center gap-1.5 sm:gap-2 my-8', className)}
    >
      {/* Nút Quay lại */}
      {createPageUrl ? (
        hasPrevious ? (
          <Link
            href={createPageUrl(currentPage - 1)}
            aria-label="Trang trước"
            className="w-10 h-10 rounded-xl border border-gray-200 bg-white text-gray-700 flex items-center justify-center hover:bg-gray-50 transition-colors cursor-pointer"
          >
            <ChevronLeft className="w-5 h-5" />
          </Link>
        ) : (
          <span className="w-10 h-10 rounded-xl border border-gray-100 bg-gray-50 text-gray-300 flex items-center justify-center cursor-not-allowed">
            <ChevronLeft className="w-5 h-5" />
          </span>
        )
      ) : (
        <button
          type="button"
          onClick={() => onPageChange?.(currentPage - 1)}
          disabled={!hasPrevious}
          aria-label="Trang trước"
          className="w-10 h-10 rounded-xl border border-gray-200 bg-white text-gray-700 flex items-center justify-center hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors cursor-pointer"
        >
          <ChevronLeft className="w-5 h-5" />
        </button>
      )}

      {/* Dãy số trang */}
      <div className="flex items-center gap-1 sm:gap-1.5">
        {pages.map((p, idx) => renderPageItem(p, idx))}
      </div>

      {/* Nút Trang sau */}
      {createPageUrl ? (
        hasNext ? (
          <Link
            href={createPageUrl(currentPage + 1)}
            aria-label="Trang sau"
            className="w-10 h-10 rounded-xl border border-gray-200 bg-white text-gray-700 flex items-center justify-center hover:bg-gray-50 transition-colors cursor-pointer"
          >
            <ChevronRight className="w-5 h-5" />
          </Link>
        ) : (
          <span className="w-10 h-10 rounded-xl border border-gray-100 bg-gray-50 text-gray-300 flex items-center justify-center cursor-not-allowed">
            <ChevronRight className="w-5 h-5" />
          </span>
        )
      ) : (
        <button
          type="button"
          onClick={() => onPageChange?.(currentPage + 1)}
          disabled={!hasNext}
          aria-label="Trang sau"
          className="w-10 h-10 rounded-xl border border-gray-200 bg-white text-gray-700 flex items-center justify-center hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors cursor-pointer"
        >
          <ChevronRight className="w-5 h-5" />
        </button>
      )}
    </nav>
  );
}
