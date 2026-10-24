import type { PropsWithChildren } from 'react';
import { cx } from '../../utils/cx';
import { humanize, toUxStatus, type UxStatus } from '../../utils/status';

export function Badge({ tone = 'neutral', children }: PropsWithChildren<{ tone?: 'neutral' | 'info' | 'success' | 'warning' | 'danger' }>) {
  return (
    <span
      className={cx(
        'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-semibold',
        tone === 'neutral' && 'bg-slate-100 text-slate-700',
        tone === 'info' && 'bg-sky-100 text-sky-900',
        tone === 'success' && 'bg-emerald-100 text-emerald-900',
        tone === 'warning' && 'bg-amber-100 text-amber-900',
        tone === 'danger' && 'bg-red-100 text-red-900'
      )}
    >
      {children}
    </span>
  );
}

const STATUS_STYLE: Record<UxStatus, { tone: 'neutral' | 'info' | 'success' | 'warning' | 'danger'; icon: string }> = {
  draft: { tone: 'neutral', icon: '◇' },
  active: { tone: 'success', icon: '●' },
  inactive: { tone: 'neutral', icon: '○' },
  processing: { tone: 'info', icon: '↻' },
  pending: { tone: 'warning', icon: '◷' },
  approved: { tone: 'success', icon: '✓' },
  rejected: { tone: 'danger', icon: '✕' },
  signed: { tone: 'success', icon: '✓' },
  completed: { tone: 'success', icon: '✓' },
  failed: { tone: 'danger', icon: '!' },
  cancelled: { tone: 'neutral', icon: '⊘' },
  expired: { tone: 'warning', icon: '◌' }
};

/** Status is always conveyed by an icon and text, never by colour alone. */
export function StatusBadge({ status, label }: { status: string | null | undefined; label?: string }) {
  const ux = toUxStatus(status);
  const style = STATUS_STYLE[ux];
  return (
    <Badge tone={style.tone}>
      <span aria-hidden="true">{style.icon}</span>
      <span>{label ?? (humanize(status) || humanize(ux))}</span>
    </Badge>
  );
}
