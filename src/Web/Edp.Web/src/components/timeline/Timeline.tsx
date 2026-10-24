import type { ReactNode } from 'react';
import { cx } from '../../utils/cx';
import { formatDateTime } from '../../utils/format';

export interface TimelineItem {
  id: string;
  title: string;
  description?: string | null;
  timestamp?: string | null;
  /** Rendered as an icon plus screen-reader text so state is never colour-only. */
  state?: 'completed' | 'current' | 'upcoming' | 'failed';
  meta?: ReactNode;
}

const MARKERS = { completed: '✓', current: '●', upcoming: '○', failed: '✕' } as const;
const LABELS = { completed: 'Completed', current: 'In progress', upcoming: 'Upcoming', failed: 'Failed' } as const;

export function Timeline({ items, label }: { items: TimelineItem[]; label: string }) {
  return (
    <ol aria-label={label} className="relative space-y-4 border-l-2 border-slate-200 pl-6">
      {items.map((item) => {
        const state = item.state ?? 'completed';
        return (
          <li key={item.id} className="relative">
            <span
              aria-hidden="true"
              className={cx(
                'absolute -left-[2.15rem] grid size-6 place-items-center rounded-full border-2 bg-white text-xs font-bold',
                state === 'completed' && 'border-emerald-600 text-emerald-700',
                state === 'current' && 'border-teal-700 text-teal-800',
                state === 'upcoming' && 'border-slate-300 text-slate-400',
                state === 'failed' && 'border-red-600 text-red-700'
              )}
            >
              {MARKERS[state]}
            </span>
            <p className="text-sm font-semibold text-slate-900">
              {item.title}
              <span className="sr-only"> — {LABELS[state]}</span>
            </p>
            {item.description && <p className="text-sm text-slate-600">{item.description}</p>}
            <p className="text-xs text-slate-500">{item.timestamp ? formatDateTime(item.timestamp) : null}{item.meta}</p>
          </li>
        );
      })}
    </ol>
  );
}
