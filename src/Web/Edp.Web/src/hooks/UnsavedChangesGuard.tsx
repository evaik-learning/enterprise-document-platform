import { useEffect } from 'react';
import { useBlocker } from 'react-router';
import { ConfirmDialog } from '../components/dialogs/ConfirmDialog';

/** Warns before leaving (tab close/refresh or in-app navigation) while a form has unsaved edits. */
export function UnsavedChangesGuard({ dirty }: { dirty: boolean }) {
  const blocker = useBlocker(({ currentLocation, nextLocation }) => dirty && currentLocation.pathname !== nextLocation.pathname);

  useEffect(() => {
    if (!dirty) return;
    const handler = (event: BeforeUnloadEvent) => { event.preventDefault(); };
    window.addEventListener('beforeunload', handler);
    return () => window.removeEventListener('beforeunload', handler);
  }, [dirty]);

  return (
    <ConfirmDialog
      open={blocker.state === 'blocked'}
      title="Discard unsaved changes?"
      description="You have changes that have not been saved. If you leave now they will be lost."
      confirmLabel="Discard and leave"
      cancelLabel="Keep editing"
      danger
      onConfirm={() => blocker.proceed?.()}
      onClose={() => blocker.reset?.()}
    />
  );
}
