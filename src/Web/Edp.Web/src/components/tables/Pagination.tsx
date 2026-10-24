import { Button } from '../ui/Button';

interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount?: number;
  totalPages?: number;
  hasNextPage: boolean;
  itemCount: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (pageSize: number) => void;
  disabled?: boolean;
}

const PAGE_SIZES = [10, 25, 50, 100];

/** Window of page numbers around the current page, e.g. 1 … 4 5 [6] 7 8 … 52. */
export function pageWindow(current: number, total: number): Array<number | 'gap'> {
  if (total <= 7) return Array.from({ length: total }, (_, index) => index + 1);
  const pages = new Set([1, total, current - 1, current, current + 1].filter((page) => page >= 1 && page <= total));
  const sorted = [...pages].sort((a, b) => a - b);
  const result: Array<number | 'gap'> = [];
  sorted.forEach((page, index) => {
    if (index > 0 && page - (sorted[index - 1] ?? 0) > 1) result.push('gap');
    result.push(page);
  });
  return result;
}

/** Server-side pagination controls; works with or without a known total. */
export function Pagination({ page, pageSize, totalCount, totalPages, hasNextPage, itemCount, onPageChange, onPageSizeChange, disabled }: PaginationProps) {
  const first = itemCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const last = (page - 1) * pageSize + itemCount;
  return (
    <nav aria-label="Pagination" className="flex flex-wrap items-center justify-between gap-3 pt-4 text-sm">
      <p className="text-slate-600" aria-live="polite">
        {totalCount !== undefined ? `Showing ${first}–${last} of ${totalCount.toLocaleString()}` : `Showing ${first}–${last}`}
      </p>
      <div className="flex flex-wrap items-center gap-2">
        {onPageSizeChange && (
          <label className="flex items-center gap-2 text-slate-600">
            Rows
            <select value={pageSize} disabled={disabled} onChange={(event) => onPageSizeChange(Number(event.target.value))} className="rounded-lg border border-slate-300 bg-white px-2 py-1">
              {PAGE_SIZES.map((size) => <option key={size} value={size}>{size}</option>)}
            </select>
          </label>
        )}
        <Button size="sm" disabled={disabled || page <= 1} onClick={() => onPageChange(page - 1)}>Previous</Button>
        {totalPages !== undefined && totalPages > 1 && (
          <ul className="hidden items-center gap-1 sm:flex">
            {pageWindow(page, totalPages).map((entry, index) =>
              entry === 'gap'
                ? <li key={`gap-${index}`} aria-hidden="true" className="px-1 text-slate-500">…</li>
                : <li key={entry}><button type="button" disabled={disabled} aria-label={`Page ${entry}`} aria-current={entry === page ? 'page' : undefined} onClick={() => onPageChange(entry)} className={entry === page ? 'rounded-lg bg-slate-900 px-3 py-1 font-semibold text-white' : 'rounded-lg px-3 py-1 hover:bg-slate-100'}>{entry}</button></li>
            )}
          </ul>
        )}
        <Button size="sm" disabled={disabled || !hasNextPage} onClick={() => onPageChange(page + 1)}>Next</Button>
      </div>
    </nav>
  );
}
