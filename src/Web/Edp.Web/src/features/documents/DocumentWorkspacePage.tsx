import { useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router';
import type { DocumentAction, DocumentDetail, DocumentWorkspace } from '../../api/contracts';
import { documentApi } from '../../api/endpoints';
import { toUserMessage } from '../../api/errors';
import { useDocumentAudit, useDocumentWorkspace } from '../../api/hooks/useDocuments';
import { isFeatureEnabled } from '../../app/featureFlags';
import { Alert, EmptyState, ErrorState } from '../../components/feedback/Feedback';
import { QueryBoundary } from '../../components/feedback/QueryBoundary';
import { useToast } from '../../components/feedback/Toast';
import { Tabs } from '../../components/navigation/Tabs';
import { StatusBadge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card, PageHeader } from '../../components/ui/Card';
import { Spinner } from '../../components/ui/Spinner';
import { saveBlob } from '../../utils/download';
import { formatBytes, formatDateTime } from '../../utils/format';
import { isProcessingStatus } from '../../utils/status';
import { AuditTable } from '../audit/AuditTable';
import { SendForSignatureDialog } from '../signatures/SendForSignatureDialog';
import { SignatureProgress } from '../signatures/SignatureProgress';
import { StartWorkflowDialog } from '../workflows/StartWorkflowDialog';
import { WorkflowStateVisualizer } from '../workflows/WorkflowStateVisualizer';
import { ActivityTimeline } from './ActivityTimeline';
import { DocumentPreview } from './DocumentPreview';
import { RegenerateDialog } from './RegenerateDialog';

function latestVersion(document: DocumentDetail) {
  return [...document.versions].sort((a, b) => b.versionNumber - a.versionNumber)[0];
}

function OverviewTab({ document, onDownload }: { document: DocumentDetail; onDownload: (fileType: string) => void }) {
  const latest = latestVersion(document);
  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <Card title="Details">
        <dl className="grid gap-3 text-sm sm:grid-cols-2">
          <div><dt className="text-slate-600">Type</dt><dd className="font-medium">{document.documentType}</dd></div>
          <div><dt className="text-slate-600">Status</dt><dd><StatusBadge status={document.status} /></dd></div>
          <div><dt className="text-slate-600">Current version</dt><dd className="font-medium">v{document.currentVersionNumber}</dd></div>
          <div><dt className="text-slate-600">Created</dt><dd>{formatDateTime(document.createdAt)}</dd></div>
          <div><dt className="text-slate-600">Template</dt><dd><Link className="font-medium text-teal-900 underline" to={`/templates/${document.templateId}`}>View template</Link></dd></div>
          {document.description && <div className="sm:col-span-2"><dt className="text-slate-600">Description</dt><dd>{document.description}</dd></div>}
        </dl>
      </Card>
      <Card title="Files">
        {!latest || latest.files.length === 0 ? <p className="text-sm text-slate-600">No generated files yet.</p> : (
          <ul className="space-y-2">
            {latest.files.map((file) => (
              <li key={file.id} className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-slate-200 p-3 text-sm">
                <span><span className="font-medium">{file.fileName}</span><span className="block text-xs text-slate-600">{file.fileType} · {formatBytes(file.size)} · version {latest.versionNumber}</span></span>
                <Button size="sm" aria-label={`Download ${file.fileName}`} onClick={() => onDownload(file.fileType)}>Download</Button>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}

function WorkflowTab({ workspace }: { workspace: DocumentWorkspace }) {
  const current = workspace.workflow.current;
  if (!current) return <EmptyState title="No workflow started" description="Start a workflow to route this document for review and approval." />;
  const pendingTasks = current.tasks.filter((task) => task.status === 'Pending');
  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <Card title={current.workflow?.name ?? 'Workflow'} actions={<StatusBadge status={current.instance.status} />}>
        <p className="mb-4 text-sm text-slate-600">Current state: <strong>{current.currentState?.name ?? '—'}</strong></p>
        <WorkflowStateVisualizer states={current.states} transitions={current.transitions} currentStateId={current.instance.currentStateId} instanceStatus={current.instance.status} history={current.history} />
        <p className="mt-4 text-sm"><Link className="font-medium text-teal-900 underline" to={`/workflows/instances/${current.instance.id}`}>Open workflow details</Link></p>
      </Card>
      <Card title="Open tasks">
        {pendingTasks.length === 0 ? <p className="text-sm text-slate-600">No tasks are waiting right now.</p> : (
          <ul className="divide-y divide-slate-100 text-sm">{pendingTasks.map((task) => <li key={task.id} className="flex items-center justify-between py-2"><span>Task {task.id.slice(0, 8)}{task.deadlineAt ? ` · due ${formatDateTime(task.deadlineAt)}` : ''}</span><StatusBadge status={task.status} /></li>)}</ul>
        )}
      </Card>
    </div>
  );
}

function SignatureTab({ workspace }: { workspace: DocumentWorkspace }) {
  const current = workspace.signature.current;
  if (!current) return <EmptyState title="No signature request" description="Send the document for signature once it is approved." />;
  return (
    <Card title={current.title} actions={<StatusBadge status={current.status} />}>
      <SignatureProgress request={current} />
      <p className="mt-4 text-sm"><Link className="font-medium text-teal-900 underline" to={`/signatures/${current.signingRequestId}`}>Open signature request</Link></p>
    </Card>
  );
}

function AuditTab({ documentId }: { documentId: string }) {
  const query = useDocumentAudit(documentId, true);
  if (query.isError) return <ErrorState error={query.error} onRetry={() => void query.refetch()} />;
  return <AuditTable logs={query.data?.items ?? []} isLoading={query.isPending} />;
}

export function DocumentWorkspacePage() {
  const { documentId = '' } = useParams();
  const [params, setParams] = useSearchParams();
  const navigate = useNavigate();
  const toast = useToast();
  const query = useDocumentWorkspace(documentId);
  const [dialog, setDialog] = useState<'workflow' | 'signature' | 'generate' | null>(null);
  const [error, setError] = useState<string | null>(null);
  const tab = params.get('tab') ?? 'overview';
  const setTab = (id: string) => setParams((previous) => { const next = new URLSearchParams(previous); next.set('tab', id); return next; });

  const download = async (document: DocumentDetail, fileType?: string) => {
    setError(null);
    try {
      saveBlob(await documentApi.download(document.id, fileType), `${document.name}.${(fileType ?? 'docx').toLowerCase()}`);
      toast.success('Download started.');
    } catch (cause) { setError(toUserMessage(cause)); }
  };

  return (
    <QueryBoundary query={query} loadingLabel="Loading document workspace" loadingRows={6}>
      {(workspace) => {
        const { document } = workspace;
        const actions = new Set<DocumentAction>(workspace.availableActions);
        const processing = isProcessingStatus(document.status);
        const latest = latestVersion(document);
        const signaturesEnabled = isFeatureEnabled('signature.enabled');
        return (
          <>
            <p className="mb-2"><Link to="/documents" className="text-sm font-medium text-teal-800 underline">Back to documents</Link></p>
            <PageHeader eyebrow={document.documentType} title={document.name} description={document.description ?? undefined} actions={<StatusBadge status={document.status} />} />

            {processing && <div className="mb-4"><Alert tone="info" title="Generating document"><span className="inline-flex items-center gap-2"><Spinner size="sm" /> This page refreshes automatically. You can leave and come back.</span></Alert></div>}
            {document.status === 'Failed' && <div className="mb-4"><Alert tone="error" title="Generation failed">The document could not be generated. {actions.has('GENERATE') ? 'Check the values and try again.' : ''}</Alert></div>}
            {workspace.unavailableSections.length > 0 && <div className="mb-4"><Alert tone="warning">Some information is temporarily unavailable ({workspace.unavailableSections.join(', ')}). Available actions may be incomplete.</Alert></div>}
            {error && <div className="mb-4"><Alert tone="error" onClose={() => setError(null)}>{error}</Alert></div>}

            <div className="mb-6 flex flex-wrap gap-2" role="group" aria-label="Document actions">
              {actions.has('DOWNLOAD') && latest?.files.map((file) => <Button key={file.id} onClick={() => void download(document, file.fileType)}>Download {file.fileType}</Button>)}
              {actions.has('GENERATE') && <Button variant="primary" onClick={() => setDialog('generate')}>{document.status === 'Failed' ? 'Retry generation' : 'Generate'}</Button>}
              {actions.has('START_WORKFLOW') && <Button variant="primary" onClick={() => setDialog('workflow')}>Start workflow</Button>}
              {signaturesEnabled && actions.has('SEND_FOR_SIGNATURE') && <Button onClick={() => setDialog('signature')}>Send for signature</Button>}
            </div>

            <Tabs label="Document sections" value={tab} onChange={setTab} tabs={[
              { id: 'overview', label: 'Overview', render: () => <OverviewTab document={document} onDownload={(fileType) => void download(document, fileType)} /> },
              { id: 'preview', label: 'Preview', hidden: !actions.has('PREVIEW'), render: () => <DocumentPreview document={document} /> },
              { id: 'workflow', label: 'Workflow', hidden: !(actions.has('VIEW_WORKFLOW') || actions.has('START_WORKFLOW')), render: () => <WorkflowTab workspace={workspace} /> },
              { id: 'signatures', label: 'Signatures', hidden: !signaturesEnabled || !(actions.has('VIEW_SIGNATURE') || actions.has('SEND_FOR_SIGNATURE')), render: () => <SignatureTab workspace={workspace} /> },
              { id: 'activity', label: 'Activity', render: () => <ActivityTimeline events={workspace.timeline} /> },
              { id: 'audit', label: 'Audit', hidden: !actions.has('VIEW_AUDIT'), render: () => <AuditTab documentId={document.id} /> }
            ]} />

            {dialog === 'workflow' && <StartWorkflowDialog documentId={document.id} documentName={document.name} onClose={() => setDialog(null)} onStarted={() => setTab('workflow')} />}
            {dialog === 'signature' && <SendForSignatureDialog document={document} onClose={() => setDialog(null)} onSent={(id) => void navigate(`/signatures/${id}`)} />}
            {dialog === 'generate' && <RegenerateDialog document={document} onClose={() => setDialog(null)} />}
          </>
        );
      }}
    </QueryBoundary>
  );
}
