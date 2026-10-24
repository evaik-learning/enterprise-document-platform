import { useState } from 'react';
import { Link, useParams } from 'react-router';
import type { WorkflowHistoryEntry } from '../../api/contracts';
import { toUserMessage } from '../../api/errors';
import { useWorkflowContext, useWorkflowInstanceMutations } from '../../api/hooks/useWorkflows';
import { Permissions } from '../../auth/permissions';
import { PermissionGate } from '../../auth/PermissionGate';
import { useAuth } from '../../auth/useAuth';
import { ConfirmDialog } from '../../components/dialogs/ConfirmDialog';
import { Alert } from '../../components/feedback/Feedback';
import { QueryBoundary } from '../../components/feedback/QueryBoundary';
import { useToast } from '../../components/feedback/Toast';
import { TextareaField } from '../../components/forms/Fields';
import { Timeline } from '../../components/timeline/Timeline';
import { StatusBadge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card, PageHeader } from '../../components/ui/Card';
import { formatDateTime } from '../../utils/format';
import { humanize } from '../../utils/status';
import { ApprovalActions } from '../approvals/ApprovalActions';
import { WorkflowStateVisualizer } from './WorkflowStateVisualizer';

const ACTIVE = ['Pending', 'InProgress', 'AwaitingApproval'];

function HistoryTimeline({ history }: { history: WorkflowHistoryEntry[] }) {
  if (history.length === 0) return <p className="text-sm text-slate-600">No history recorded yet.</p>;
  return <Timeline label="Workflow history" items={[...history].sort((a, b) => a.eventAt.localeCompare(b.eventAt)).map((entry) => ({ id: entry.id, title: humanize(entry.eventType), description: entry.description, timestamp: entry.eventAt, state: /reject|cancel|fail/i.test(entry.eventType) ? 'failed' : 'completed' }))} />;
}

export function WorkflowInstancePage() {
  const { instanceId = '' } = useParams();
  const query = useWorkflowContext(instanceId);
  const { me } = useAuth();
  const mutations = useWorkflowInstanceMutations();
  const toast = useToast();
  const [error, setError] = useState<string | null>(null);
  const [cancelling, setCancelling] = useState(false);
  const [reason, setReason] = useState('');

  const run = async (action: () => Promise<unknown>, success: string) => {
    setError(null);
    try { await action(); toast.success(success); } catch (cause) { setError(toUserMessage(cause)); }
  };

  return (
    <QueryBoundary query={query} loadingLabel="Loading workflow" loadingRows={6}>
      {(context) => {
        const { instance } = context;
        const active = ACTIVE.includes(instance.status);
        const myTask = context.tasks.find((task) => task.status === 'Pending' && !!me?.userId && task.assignedToUserId.toLowerCase() === me.userId.toLowerCase());
        return (
          <>
            <p className="mb-2"><Link to="/workflows?tab=instances" className="text-sm font-medium text-teal-800 underline">Back to workflows</Link></p>
            <PageHeader eyebrow="Workflow instance" title={context.workflow?.name ?? 'Workflow'} description={`Current state: ${context.currentState?.name ?? '—'}`}
              actions={<><StatusBadge status={instance.status} /><Link to={`/documents/${instance.documentId}`} className="text-sm font-medium text-teal-800 underline">Open document</Link></>} />
            {error && <div className="mb-4"><Alert tone="error" onClose={() => setError(null)}>{error}</Alert></div>}

            <div className="grid gap-6 lg:grid-cols-2">
              <Card title="Progress"><WorkflowStateVisualizer states={context.states} transitions={context.transitions} currentStateId={instance.currentStateId} instanceStatus={instance.status} history={context.history} /></Card>
              <div className="space-y-6">
                <Card title="Current task">
                  {context.tasks.filter((task) => task.status === 'Pending').length === 0 ? <p className="text-sm text-slate-600">No task is waiting on anyone right now.</p> : (
                    <ul className="space-y-1 text-sm">{context.tasks.filter((task) => task.status === 'Pending').map((task) => <li key={task.id}>Assigned to {task.assignedToUserId === me?.userId ? 'you' : task.assignedToUserId.slice(0, 8)}{task.deadlineAt ? ` · due ${formatDateTime(task.deadlineAt)}` : ''}</li>)}</ul>
                  )}
                  {myTask && <div className="mt-4"><ApprovalActions taskId={myTask.id} /></div>}
                </Card>

                {active && context.nextTransitions.length > 0 && (
                  <PermissionGate permission={Permissions.WorkflowExecute}>
                    <Card title="Next steps">
                      <p className="mb-3 text-sm text-slate-600">The Workflow service validates every move; unavailable transitions are rejected.</p>
                      <div className="flex flex-wrap gap-2">
                        {context.nextTransitions.map((transition) => {
                          const target = context.states.find((state) => state.id === transition.toStateId);
                          return <Button key={transition.id} variant="primary" loading={mutations.executeTransition.isPending} onClick={() => void run(() => mutations.executeTransition.mutateAsync({ instanceId, transitionId: transition.id }), `Moved to ${target?.name ?? 'next step'}.`)}>Continue to {target?.name ?? 'next step'}</Button>;
                        })}
                      </div>
                    </Card>
                  </PermissionGate>
                )}

                <PermissionGate permission={Permissions.WorkflowExecute}>
                  {(active || instance.status === 'Paused') && (
                    <Card title="Manage">
                      <div className="flex flex-wrap gap-2">
                        {instance.status === 'Paused'
                          ? <Button onClick={() => void run(() => mutations.resume.mutateAsync(instanceId), 'Workflow resumed.')}>Resume</Button>
                          : <Button onClick={() => void run(() => mutations.suspend.mutateAsync({ id: instanceId, reason: 'Paused by user' }), 'Workflow paused.')}>Pause</Button>}
                        <Button variant="danger" onClick={() => setCancelling(true)}>Cancel workflow</Button>
                      </div>
                    </Card>
                  )}
                </PermissionGate>
              </div>
            </div>

            <div className="mt-6"><Card title="History"><HistoryTimeline history={context.history} /></Card></div>

            <ConfirmDialog open={cancelling} title="Cancel workflow?" description="The workflow stops and open tasks are closed. This cannot be undone." confirmLabel="Cancel workflow" cancelLabel="Keep running" danger onClose={() => { setCancelling(false); setReason(''); }}
              confirmDisabled={!reason.trim()}
              onConfirm={async () => { await mutations.cancel.mutateAsync({ id: instanceId, reason: reason.trim() }); toast.success('Workflow cancelled.'); }}>
              <TextareaField label="Reason" required value={reason} onChange={(event) => setReason(event.target.value)} maxLength={1000} />
            </ConfirmDialog>
          </>
        );
      }}
    </QueryBoundary>
  );
}
