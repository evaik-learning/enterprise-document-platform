import { useRef, useState } from 'react';
import { toUserMessage } from '../../api/errors';
import { useWorkflowInstanceMutations, useWorkflows } from '../../api/hooks/useWorkflows';
import { Dialog } from '../../components/dialogs/Dialog';
import { Alert, EmptyState } from '../../components/feedback/Feedback';
import { LoadingSkeleton } from '../../components/feedback/LoadingSkeleton';
import { useToast } from '../../components/feedback/Toast';
import { SelectField } from '../../components/forms/Fields';
import { Button } from '../../components/ui/Button';

interface Props {
  documentId: string;
  documentName: string;
  onClose: () => void;
  onStarted: (instanceId: string) => void;
}

/** Start a published workflow for a generated document. The Workflow service creates and owns the instance. */
export function StartWorkflowDialog({ documentId, documentName, onClose, onStarted }: Props) {
  const workflows = useWorkflows();
  const { start } = useWorkflowInstanceMutations();
  const toast = useToast();
  const idempotencyKey = useRef(crypto.randomUUID());
  const [workflowId, setWorkflowId] = useState('');
  const [error, setError] = useState<string | null>(null);
  const published = (workflows.data ?? []).filter((workflow) => workflow.status === 'Published' && workflow.publishedVersion);

  const submit = async () => {
    setError(null);
    try {
      const instance = await start.mutateAsync({ workflowId, documentId, idempotencyKey: idempotencyKey.current });
      toast.success('Workflow started.');
      onStarted(instance.id);
      onClose();
    } catch (cause) {
      setError(toUserMessage(cause));
    }
  };

  return (
    <Dialog open onClose={onClose} title="Start workflow" description={`Route “${documentName}” through a published workflow.`} dismissible={!start.isPending}
      footer={<><Button onClick={onClose} disabled={start.isPending}>Cancel</Button><Button variant="primary" disabled={!workflowId} loading={start.isPending} onClick={() => void submit()}>Start workflow</Button></>}>
      <div className="space-y-4">
        {error && <Alert tone="error">{error}</Alert>}
        {workflows.isPending && <LoadingSkeleton rows={2} label="Loading workflows" />}
        {workflows.isError && <Alert tone="error">{toUserMessage(workflows.error)}</Alert>}
        {workflows.isSuccess && published.length === 0 && <EmptyState title="No published workflows" description="Publish a workflow definition before routing documents." />}
        {published.length > 0 && (
          <SelectField label="Workflow" required value={workflowId} onChange={(event) => setWorkflowId(event.target.value)}>
            <option value="">Select a workflow</option>
            {published.map((workflow) => <option key={workflow.id} value={workflow.id}>{workflow.name} (v{workflow.publishedVersion})</option>)}
          </SelectField>
        )}
      </div>
    </Dialog>
  );
}
