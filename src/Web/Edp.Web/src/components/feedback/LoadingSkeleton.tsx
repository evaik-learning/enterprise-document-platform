import { cx } from '../../utils/cx';

export function Skeleton({ className }: { className?: string }) {
  return <div aria-hidden="true" className={cx('animate-pulse rounded bg-slate-200', className ?? 'h-4 w-full')} />;
}

/** Announces busy state to assistive tech while placeholder shapes render. */
export function LoadingSkeleton({ rows = 4, label = 'Loading' }: { rows?: number; label?: string }) {
  return (
    <div role="status" aria-busy="true" aria-live="polite" className="space-y-3 rounded-xl border border-slate-200 bg-white p-5">
      <span className="sr-only">{label}…</span>
      <Skeleton className="h-6 w-1/3" />
      {Array.from({ length: rows }, (_, index) => <Skeleton key={index} className={cx('h-4', index % 2 ? 'w-5/6' : 'w-full')} />)}
    </div>
  );
}

export function FullPageLoading({ label = 'Loading workspace' }: { label?: string }) {
  return (
    <div role="status" aria-busy="true" className="grid min-h-screen place-items-center bg-slate-50 text-slate-700">
      <div className="rounded-lg border border-slate-200 bg-white px-5 py-4 shadow-sm">{label}…</div>
    </div>
  );
}
