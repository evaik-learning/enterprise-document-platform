import { createContext, useCallback, useContext, useMemo, useRef, useState, type PropsWithChildren } from 'react';
import { cx } from '../../utils/cx';

type ToastTone = 'success' | 'error' | 'info';
interface ToastItem { id: number; tone: ToastTone; message: string }

interface ToastApi {
  success: (message: string) => void;
  error: (message: string) => void;
  info: (message: string) => void;
}

const ToastContext = createContext<ToastApi | null>(null);

export function ToastProvider({ children }: PropsWithChildren) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const counter = useRef(0);

  const dismiss = useCallback((id: number) => setToasts((current) => current.filter((toast) => toast.id !== id)), []);
  const push = useCallback((tone: ToastTone, message: string) => {
    const id = ++counter.current;
    setToasts((current) => [...current.slice(-3), { id, tone, message }]);
    setTimeout(() => dismiss(id), tone === 'error' ? 8000 : 5000);
  }, [dismiss]);

  const api = useMemo<ToastApi>(() => ({ success: (m) => push('success', m), error: (m) => push('error', m), info: (m) => push('info', m) }), [push]);

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className="pointer-events-none fixed inset-x-0 bottom-4 z-[60] flex flex-col items-center gap-2 px-4 sm:items-end sm:px-6">
        {toasts.map((toast) => (
          <div
            key={toast.id}
            role={toast.tone === 'error' ? 'alert' : 'status'}
            className={cx(
              'pointer-events-auto flex w-full max-w-sm items-start gap-3 rounded-lg border bg-white p-3 text-sm shadow-lg',
              toast.tone === 'success' && 'border-emerald-300',
              toast.tone === 'error' && 'border-red-300',
              toast.tone === 'info' && 'border-sky-300'
            )}
          >
            <span aria-hidden="true">{toast.tone === 'success' ? '✓' : toast.tone === 'error' ? '✕' : 'i'}</span>
            <p className="flex-1">{toast.message}</p>
            <button type="button" aria-label="Dismiss notification" className="rounded px-1 hover:bg-slate-100" onClick={() => dismiss(toast.id)}>✕</button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast(): ToastApi {
  const context = useContext(ToastContext);
  if (!context) throw new Error('useToast must be used within ToastProvider.');
  return context;
}
