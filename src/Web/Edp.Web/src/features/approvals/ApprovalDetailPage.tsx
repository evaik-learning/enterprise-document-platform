import { Link, useNavigate, useParams } from 'react-router';
import { useApprovalDetail } from '../../api/hooks/useWorkflows';
import { Alert } from '../../components/feedback/Feedback';
import { QueryBoundary } from '../../components/feedback/QueryBoundary';
import { Timeline } from '../../components/timeline/Timeline';
import { StatusBadge } from '../../components/ui/Badge';
import { ButtonLink } from '../../components/ui/Button';
import { Card, PageHeader } from '../../components/ui/Card';
import { formatDateTime } from '../../utils/format';
import { humanize } from '../../utils/status';
import { DocumentPreview } from '../documents/DocumentPreview';
import { ApprovalActions, CHANGES_REQUESTED_PREFIX } from './ApprovalActions';

/** Approval detail: document preview, workflow context, history and the decision actions. */
export function ApprovalDetailPage() {
  const { taskId = '' } = useParams();
  const query = useApprovalDetail(taskId);
  const navigate = useNavigate();

  return (
    <QueryBoundary query={query} loadingLabel="Loading approval" loadingRows={6}>
      {({ task, instance, document, history }) => {
        const pending = task.status === 'Pending';
        return (
          <>
            <p className="mb-2"><Link to="/approvals" className="text-sm font-medium text-teal-800 underline">Back to approvals</Link></p>
            <PageHeader eyebrow="Approval" title={document?.name ?? `Task ${task.id.slice(0, 8)}`} description={task.deadlineAt ? `Due ${formatDateTime(task.deadlineAt)}` : undefined}
              actions={<><StatusBadge status={task.status} />{instance && <ButtonLink variant="ghost" to={`/workflows/instances/${instance.id}`}>Workflow</ButtonLink>}</>} />

            {!pending && <div className="mb-4"><Alert tone="info">This task is already {humanize(task.status).toLowerCase()}; no further action is needed.</Alert></div>}

            <div className="grid gap-6 lg:grid-cols-[1.4fr_1fr]">
              <div className="space-y-6">
                <Card title="Document">{document ? <DocumentPreview document={document} /> : <p className="text-sm text-slate-600">The document could not be loaded.</p>}</Card>
              </div>
              <div className="space-y-6">
                {pending && <Card title="Your decision"><ApprovalActions taskId={task.id} onDone={() => void navigate('/approvals')} /></Card>}
                <Card title="History">
                  {history.length === 0 ? <p className="text-sm text-slate-600">No history recorded yet.</p> : (
                    <Timeline label="Approval history" items={[...history].sort((a, b) => a.eventAt.localeCompare(b.eventAt)).map((entry) => ({
                      id: entry.id,
                      title: humanize(entry.eventType),
                      description: entry.description.startsWith(CHANGES_REQUESTED_PREFIX) ? entry.description.slice(CHANGES_REQUESTED_PREFIX.length) : entry.description,
                      timestamp: entry.eventAt,
                      state: /reject|cancel|fail/i.test(entry.eventType) ? 'failed' : 'completed'
                    }))} />
                  )}
                </Card>
              </div>
            </div>
          </>
        );
      }}
    </QueryBoundary>
  );
}
