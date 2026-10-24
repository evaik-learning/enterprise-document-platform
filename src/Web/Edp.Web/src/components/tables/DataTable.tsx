import type { ReactNode } from 'react';
import { cx } from '../../utils/cx';
import { Skeleton } from '../feedback/LoadingSkeleton';

export interface Column<T> {
  key: string;
  /** Plain text so it can double as the label on mobile cards. */
  header: string;
  cell: (row: T) => ReactNode;
  className?: string;
  /** Hide on mobile cards (e.g. low-value columns). */
  hideOnCard?: boolean;
  /** Visually hide the header (actions column). */
  srOnlyHeader?: boolean;
}

interface DataTableProps<T> {
  caption: string;
  columns: Column<T>[];
  rows: T[];
  getRowKey: (row: T) => string;
  isLoading?: boolean;
  /** True while a background refetch/page change is running; rows stay visible but dimmed. */
  isRefreshing?: boolean;
  empty?: ReactNode;
}

/** Table on md+ screens, card list below, so tables stay usable on mobile. */
export function DataTable<T>({ caption, columns, rows, getRowKey, isLoading, isRefreshing, empty }: DataTableProps<T>) {
  if (isLoading) {
    return (
      <div role="status" aria-busy="true" className="space-y-2 rounded-xl border border-slate-200 bg-white p-4">
        <span className="sr-only">Loading {caption}…</span>
        {Array.from({ length: 5 }, (_, index) => <Skeleton key={index} className="h-8 w-full" />)}
      </div>
    );
  }
  if (rows.length === 0 && empty) return <>{empty}</>;

  return (
    <div aria-busy={isRefreshing || undefined} className={cx('transition-opacity', isRefreshing && 'opacity-60')}>
      <div className="hidden overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm md:block">
        <table className="min-w-full text-left text-sm">
          <caption className="sr-only">{caption}</caption>
          <thead className="border-b border-slate-200 bg-slate-50 text-xs uppercase tracking-wide text-slate-600">
            <tr>
              {columns.map((column) => (
                <th key={column.key} scope="col" className={cx('px-4 py-3 font-semibold', column.className)}>
                  <span className={cx(column.srOnlyHeader && 'sr-only')}>{column.header}</span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {rows.map((row) => (
              <tr key={getRowKey(row)} className="hover:bg-slate-50">
                {columns.map((column) => <td key={column.key} className={cx('px-4 py-3 align-middle', column.className)}>{column.cell(row)}</td>)}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ul aria-label={caption} className="space-y-3 md:hidden">
        {rows.map((row) => (
          <li key={getRowKey(row)} className="space-y-2 rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            {columns.filter((column) => !column.hideOnCard).map((column, index) => (
              <div key={column.key} className={cx(index === 0 ? 'text-base font-semibold' : 'flex items-baseline justify-between gap-3 text-sm')}>
                {index !== 0 && !column.srOnlyHeader && <span className="text-slate-600">{column.header}</span>}
                <div className={cx(index !== 0 && 'text-right')}>{column.cell(row)}</div>
              </div>
            ))}
          </li>
        ))}
      </ul>
    </div>
  );
}
