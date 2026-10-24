import { useEffect, useId, useRef, type PropsWithChildren, type ReactNode } from 'react';
import { cx } from '../../utils/cx';

interface DialogProps extends PropsWithChildren {
  open: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  footer?: ReactNode;
  /** `modal` is centred; `drawer-left`/`drawer-right` slide in from the edge (used for mobile navigation and detail panels). */
  variant?: 'modal' | 'drawer-left' | 'drawer-right';
  size?: 'sm' | 'md' | 'lg';
  /** Prevents closing via Escape/backdrop while an operation is in flight. */
  dismissible?: boolean;
}

/**
 * Built on the native <dialog> element so focus is trapped, the page behind is inert and focus
 * returns to the opener on close. Content is mounted only while open so forms start fresh.
 */
export function Dialog({ open, onClose, title, description, footer, children, variant = 'modal', size = 'md', dismissible = true }: DialogProps) {
  const ref = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  const descriptionId = useId();

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) {
      if (typeof dialog.showModal === 'function') dialog.showModal();
      else dialog.setAttribute('open', '');
    } else if (!open && dialog.open) {
      if (typeof dialog.close === 'function') dialog.close();
      else dialog.removeAttribute('open');
    }
  }, [open]);

  return (
    // Backdrop click is a pointer convenience; Escape (onCancel) is the keyboard path.
    // oxlint-disable-next-line jsx-a11y/click-events-have-key-events, jsx-a11y/no-noninteractive-element-interactions
    <dialog
      ref={ref}
      aria-labelledby={titleId}
      aria-describedby={description ? descriptionId : undefined}
      onCancel={(event) => { event.preventDefault(); if (dismissible) onClose(); }}
      onClick={(event) => { if (dismissible && event.target === ref.current) onClose(); }}
      className={cx(
        'bg-white p-0 text-slate-900 shadow-2xl backdrop:bg-slate-900/50',
        variant === 'modal' && cx('m-auto w-[calc(100%-2rem)] rounded-xl', size === 'sm' && 'max-w-md', size === 'md' && 'max-w-xl', size === 'lg' && 'max-w-3xl'),
        variant === 'drawer-left' && 'm-0 mr-auto h-full max-h-none w-80 max-w-[85vw]',
        variant === 'drawer-right' && 'm-0 ml-auto h-full max-h-none w-full max-w-xl'
      )}
    >
      {open && (
        <div className="flex max-h-[90vh] flex-col" style={variant === 'modal' ? undefined : { maxHeight: '100vh', height: '100%' }}>
          <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-5 py-4">
            <div>
              <h2 id={titleId} className="text-lg font-semibold">{title}</h2>
              {description && <p id={descriptionId} className="mt-1 text-sm text-slate-600">{description}</p>}
            </div>
            <button type="button" aria-label={`Close ${title}`} disabled={!dismissible} className="rounded p-1 text-slate-500 hover:bg-slate-100 disabled:opacity-50" onClick={onClose}>✕</button>
          </header>
          <div className="flex-1 overflow-y-auto px-5 py-4">{children}</div>
          {footer && <footer className="flex flex-wrap justify-end gap-2 border-t border-slate-200 px-5 py-3">{footer}</footer>}
        </div>
      )}
    </dialog>
  );
}
