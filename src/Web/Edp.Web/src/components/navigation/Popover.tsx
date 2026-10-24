import { useEffect, useId, useRef, useState, type ReactNode } from 'react';
import { cx } from '../../utils/cx';

interface PopoverProps {
  /** Accessible name of the trigger button. */
  label: string;
  trigger: ReactNode;
  align?: 'left' | 'right';
  panelClassName?: string;
  children: (close: () => void) => ReactNode;
  onOpenChange?: (open: boolean) => void;
}

/** Disclosure popover (user menu, notifications, organization switcher): Escape/outside click close it and return focus. */
export function Popover({ label, trigger, align = 'right', panelClassName, children, onOpenChange }: PopoverProps) {
  const [open, setOpen] = useState(false);
  const root = useRef<HTMLDivElement>(null);
  const button = useRef<HTMLButtonElement>(null);
  const panelId = useId();

  const set = (next: boolean) => { setOpen(next); onOpenChange?.(next); };

  useEffect(() => {
    if (!open) return;
    const onPointer = (event: MouseEvent) => { if (!root.current?.contains(event.target as Node)) set(false); };
    const onKey = (event: KeyboardEvent) => { if (event.key === 'Escape') { set(false); button.current?.focus(); } };
    document.addEventListener('mousedown', onPointer);
    document.addEventListener('keydown', onKey);
    return () => { document.removeEventListener('mousedown', onPointer); document.removeEventListener('keydown', onKey); };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  return (
    <div ref={root} className="relative">
      <button ref={button} type="button" aria-label={label} aria-expanded={open} aria-controls={open ? panelId : undefined} aria-haspopup="true" onClick={() => set(!open)} className="rounded-lg border border-transparent px-2 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 focus-visible:outline-2 focus-visible:outline-teal-700">
        {trigger}
      </button>
      {open && (
        <div id={panelId} className={cx('absolute z-40 mt-2 max-h-[70vh] min-w-64 overflow-y-auto rounded-xl border border-slate-200 bg-white p-2 shadow-lg', align === 'right' ? 'right-0' : 'left-0', panelClassName)}>
          {children(() => set(false))}
        </div>
      )}
    </div>
  );
}
