import { useState, type ReactNode } from 'react';
import { toUserMessage } from '../../api/errors';
import { Alert } from '../feedback/Feedback';
import { Button } from '../ui/Button';
import { Dialog } from './Dialog';

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  description?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
  confirmDisabled?: boolean;
  onConfirm: () => Promise<unknown> | void;
  onClose: () => void;
  children?: ReactNode;
}

export function ConfirmDialog({ open, title, description, confirmLabel = 'Confirm', cancelLabel = 'Cancel', danger, confirmDisabled, onConfirm, onClose, children }: ConfirmDialogProps) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const close = () => { setError(null); onClose(); };
  const confirm = async () => {
    setBusy(true);
    setError(null);
    try {
      await onConfirm();
      onClose();
    } catch (cause) {
      setError(toUserMessage(cause));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog
      open={open}
      onClose={close}
      title={title}
      description={description}
      size="sm"
      dismissible={!busy}
      footer={
        <>
          <Button disabled={busy} onClick={close}>{cancelLabel}</Button>
          <Button variant={danger ? 'danger' : 'primary'} loading={busy} disabled={confirmDisabled} onClick={() => void confirm()}>{confirmLabel}</Button>
        </>
      }
    >
      {children}
      {error && <div className="mt-3"><Alert tone="error">{error}</Alert></div>}
    </Dialog>
  );
}
