import { useState } from 'react';
import { toUserMessage } from '../../api/errors';
import { useApprovalMutations } from '../../api/hooks/useWorkflows';
import { Permissions } from '../../auth/permissions';
import { PermissionGate } from '../../auth/PermissionGate';
import { Dialog } from '../../components/dialogs/Dialog';
import { Alert } from '../../components/feedback/Feedback';
import { useToast } from '../../components/feedback/Toast';
import { TextareaField, TextField } from '../../components/forms/Fields';
import { Button } from '../../components/ui/Button';

type Decision = 'approve' | 'reject' | 'changes' | 'delegate';

/** Prefix that marks a rejection as a request for changes (the Workflow service has a single rejection outcome). */
export const CHANGES_REQUESTED_PREFIX = 'Changes requested: ';
const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

const COPY: Record<Decision, { title: string; confirm: string; commentLabel: string; required: boolean }> = {
  approve: { title: 'Approve', confirm: 'Approve', commentLabel: 'Comment (optional)', required: false },
  reject: { title: 'Reject', confirm: 'Reject', commentLabel: 'Reason for rejection', required: true },
  changes: { title: 'Request changes', confirm: 'Request changes', commentLabel: 'What needs to change?', required: true },
  delegate: { title: 'Delegate', confirm: 'Delegate', commentLabel: 'Reason for delegating', required: true }
};

/** Approval decisions. Nothing changes in the UI until the backend confirms the transition (no optimistic updates). */
export function ApprovalActions({ taskId, onDone }: { taskId: string; onDone?: () => void }) {
  const { approve, reject, delegate } = useApprovalMutations();
  const toast = useToast();
  const [decision, setDecision] = useState<Decision | null>(null);
  const [comment, setComment] = useState('');
  const [delegateTo, setDelegateTo] = useState('');
  const [error, setError] = useState<string | null>(null);
  const pending = approve.isPending || reject.isPending || delegate.isPending;

  const close = () => { if (pending) return; setDecision(null); setComment(''); setDelegateTo(''); setError(null); };

  const submit = async () => {
    if (!decision) return;
    const copy = COPY[decision];
    if (copy.required && !comment.trim()) { setError(`${copy.commentLabel} is required.`); return; }
    if (decision === 'delegate' && !GUID.test(delegateTo.trim())) { setError('Enter the delegate’s user ID (GUID).'); return; }
    setError(null);
    try {
      if (decision === 'approve') await approve.mutateAsync({ taskId, comment: comment.trim() || undefined });
      else if (decision === 'reject') await reject.mutateAsync({ taskId, comment: comment.trim() });
      else if (decision === 'changes') await reject.mutateAsync({ taskId, comment: `${CHANGES_REQUESTED_PREFIX}${comment.trim()}` });
      else await delegate.mutateAsync({ taskId, userId: delegateTo.trim(), reason: comment.trim() });
      toast.success(decision === 'approve' ? 'Approved.' : decision === 'delegate' ? 'Task delegated.' : decision === 'changes' ? 'Changes requested.' : 'Rejected.');
      setDecision(null);
      setComment('');
      setDelegateTo('');
      onDone?.();
    } catch (cause) {
      setError(toUserMessage(cause));
    }
  };

  const copy = decision ? COPY[decision] : null;

  return (
    <PermissionGate permission={Permissions.WorkflowApprove}>
      <div role="group" aria-label="Approval decision" className="flex flex-wrap gap-2">
        <Button variant="primary" onClick={() => setDecision('approve')}>Approve</Button>
        <Button variant="danger" onClick={() => setDecision('reject')}>Reject</Button>
        <Button onClick={() => setDecision('changes')}>Request changes</Button>
        <Button variant="ghost" onClick={() => setDecision('delegate')}>Delegate</Button>
      </div>
      {copy && decision && (
        <Dialog open onClose={close} size="sm" title={copy.title} dismissible={!pending}
          footer={<><Button onClick={close} disabled={pending}>Cancel</Button><Button variant={decision === 'reject' ? 'danger' : 'primary'} loading={pending} onClick={() => void submit()}>{copy.confirm}</Button></>}>
          <div className="space-y-4">
            {error && <Alert tone="error">{error}</Alert>}
            {decision === 'delegate' && <TextField label="Delegate to (user ID)" required value={delegateTo} onChange={(event) => setDelegateTo(event.target.value)} />}
            <TextareaField label={copy.commentLabel} required={copy.required} value={comment} onChange={(event) => setComment(event.target.value)} maxLength={2000} autoFocus />
            {decision === 'changes' && <p className="text-xs text-slate-600">The task is closed and the comment is recorded in the workflow history for the requester.</p>}
          </div>
        </Dialog>
      )}
    </PermissionGate>
  );
}
