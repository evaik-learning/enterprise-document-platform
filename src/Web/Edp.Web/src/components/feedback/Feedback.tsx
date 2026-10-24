import type { PropsWithChildren, ReactNode } from 'react';
import { correlationReference, toUserMessage, ApiError } from '../../api/errors';
import { cx } from '../../utils/cx';
import { Button } from '../ui/Button';

type AlertTone = 'info' | 'success' | 'warning' | 'error';

export function Alert({ tone = 'info', title, onClose, children }: PropsWithChildren<{ tone?: AlertTone; title?: string; onClose?: () => void }>) {
  const icon = { info: 'i', success: '✓', warning: '!', error: '✕' }[tone];
  return (
    <div
      role={tone === 'error' ? 'alert' : 'status'}
      className={cx(
        'flex items-start gap-3 rounded-lg border p-4 text-sm',
        tone === 'info' && 'border-sky-200 bg-sky-50 text-sky-950',
        tone === 'success' && 'border-emerald-200 bg-emerald-50 text-emerald-950',
        tone === 'warning' && 'border-amber-200 bg-amber-50 text-amber-950',
        tone === 'error' && 'border-red-200 bg-red-50 text-red-950'
      )}
    >
      <span aria-hidden="true" className="mt-0.5 grid size-5 shrink-0 place-items-center rounded-full border border-current text-xs font-bold">{icon}</span>
      <div className="min-w-0 flex-1">
        {title && <p className="font-semibold">{title}</p>}
        <div>{children}</div>
      </div>
      {onClose && <button type="button" aria-label="Dismiss" className="rounded p-1 hover:bg-black/5" onClick={onClose}>✕</button>}
    </div>
  );
}

/** Failure panel with a support reference (correlation id) for troubleshooting. */
export function ErrorState({ error, title = 'Something went wrong', onRetry }: { error: unknown; title?: string; onRetry?: () => void }) {
  const reference = correlationReference(error);
  const forbidden = error instanceof ApiError && error.isForbidden;
  const notFound = error instanceof ApiError && error.isNotFound;
  return (
    <div role="alert" className="rounded-xl border border-red-200 bg-red-50 p-6 text-red-950">
      <h2 className="text-base font-semibold">{forbidden ? 'Access denied' : notFound ? 'Not found' : title}</h2>
      <p className="mt-1 text-sm">{toUserMessage(error)}</p>
      {reference && <p className="mt-2 text-xs text-red-900">Reference: <code>{reference}</code></p>}
      {onRetry && !forbidden && !notFound && <Button className="mt-4" size="sm" onClick={onRetry}>Try again</Button>}
    </div>
  );
}

export function EmptyState({ title, description, action }: { title: string; description?: string; action?: ReactNode }) {
  return (
    <div className="rounded-xl border border-dashed border-slate-300 bg-white px-6 py-12 text-center">
      <p className="text-base font-semibold text-slate-900">{title}</p>
      {description && <p className="mx-auto mt-1 max-w-md text-sm text-slate-600">{description}</p>}
      {action && <div className="mt-4 flex justify-center">{action}</div>}
    </div>
  );
}
