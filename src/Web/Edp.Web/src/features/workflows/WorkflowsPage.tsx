import { useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import type { Workflow, WorkflowInstance } from '../../api/contracts';
import { toUserMessage } from '../../api/errors';
import { useWorkflowDefinitionMutations, useWorkflowInstances, useWorkflows } from '../../api/hooks/useWorkflows';
import { Permissions } from '../../auth/permissions';
import { PermissionGate } from '../../auth/PermissionGate';
import { ConfirmDialog } from '../../components/dialogs/ConfirmDialog';
import { Dialog } from '../../components/dialogs/Dialog';
import { Alert, EmptyState, ErrorState } from '../../components/feedback/Feedback';
import { useToast } from '../../components/feedback/Toast';
import { FormErrorSummary, SelectField, TextareaField, TextField } from '../../components/forms/Fields';
import { Tabs } from '../../components/navigation/Tabs';
import { DataTable, type Column } from '../../components/tables/DataTable';
import { Pagination } from '../../components/tables/Pagination';
import { StatusBadge } from '../../components/ui/Badge';
import { Button, ButtonLink } from '../../components/ui/Button';
import { PageHeader } from '../../components/ui/Card';
import { useUrlListState } from '../../hooks/useUrlListState';
import { shortId } from '../../utils/format';

const CAN_DESIGN = [Permissions.WorkflowExecute, Permissions.OrganizationAdmin] as const;

function CreateWorkflowDialog({ onClose }: { onClose: () => void }) {
  const { create } = useWorkflowDefinitionMutations();
  const toast = useToast();
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [errors, setErrors] = useState<string[]>([]);
  const [serverError, setServerError] = useState<string | null>(null);

  const submit = async () => {
    const found: string[] = [];
    if (!code.trim()) found.push('Code is required.');
    else if (!/^[A-Za-z0-9][A-Za-z0-9_-]*$/.test(code.trim())) found.push('Code may contain letters, numbers, dashes and underscores only.');
    if (!name.trim()) found.push('Name is required.');
    setErrors(found);
    if (found.length > 0) return;
    try {
      await create.mutateAsync({ code: code.trim(), name: name.trim(), description: description.trim() || undefined });
      toast.success(`Workflow “${name.trim()}” created.`);
      onClose();
    } catch (cause) { setServerError(toUserMessage(cause)); }
  };

  return (
    <Dialog open onClose={onClose} title="New workflow" description="Create the definition, then add states and transitions and publish a version." dismissible={!create.isPending}
      footer={<><Button onClick={onClose} disabled={create.isPending}>Cancel</Button><Button variant="primary" loading={create.isPending} onClick={() => void submit()}>Create workflow</Button></>}>
      <form className="space-y-4" noValidate onSubmit={(event) => { event.preventDefault(); void submit(); }}>
        <FormErrorSummary errors={errors} />
        {serverError && <Alert tone="error">{serverError}</Alert>}
        <TextField label="Code" required value={code} onChange={(event) => setCode(event.target.value)} maxLength={100} help="Unique identifier, e.g. CONTRACT-APPROVAL." />
        <TextField label="Name" required value={name} onChange={(event) => setName(event.target.value)} maxLength={250} />
        <TextareaField label="Description" value={description} onChange={(event) => setDescription(event.target.value)} maxLength={1000} />
      </form>
    </Dialog>
  );
}

function DefinitionsTab() {
  const query = useWorkflows();
  const { archive } = useWorkflowDefinitionMutations();
  const toast = useToast();
  const [creating, setCreating] = useState(false);
  const [archiving, setArchiving] = useState<Workflow | null>(null);

  const columns: Column<Workflow>[] = [
    { key: 'name', header: 'Workflow', cell: (w) => <span><Link to={`/workflows/definitions/${w.id}`} className="font-semibold text-teal-900 hover:underline">{w.name}</Link><span className="block text-xs text-slate-600">{w.code}</span></span> },
    { key: 'status', header: 'Status', cell: (w) => <StatusBadge status={w.status} /> },
    { key: 'published', header: 'Published version', cell: (w) => (w.publishedVersion ? `v${w.publishedVersion}` : 'None') },
    {
      key: 'actions', header: 'Actions', srOnlyHeader: true, className: 'text-right',
      cell: (w) => (
        <span className="inline-flex gap-1">
          <ButtonLink size="sm" variant="ghost" to={`/workflows/definitions/${w.id}`} aria-label={`Open ${w.name}`}>Open</ButtonLink>
          <PermissionGate permission={CAN_DESIGN} mode="any">{w.status !== 'Archived' && <Button size="sm" variant="ghost" aria-label={`Archive ${w.name}`} onClick={() => setArchiving(w)}>Archive</Button>}</PermissionGate>
        </span>
      )
    }
  ];

  return (
    <div className="space-y-4">
      <div className="flex justify-end"><PermissionGate permission={CAN_DESIGN} mode="any"><Button variant="primary" onClick={() => setCreating(true)}>New workflow</Button></PermissionGate></div>
      {query.isError ? <ErrorState error={query.error} onRetry={() => void query.refetch()} /> : (
        <DataTable caption="Workflow definitions" columns={columns} rows={query.data ?? []} getRowKey={(w) => w.id} isLoading={query.isPending} isRefreshing={query.isFetching}
          empty={<EmptyState title="No workflows yet" description="Create a workflow definition to route documents through review and approval." />} />
      )}
      {creating && <CreateWorkflowDialog onClose={() => setCreating(false)} />}
      <ConfirmDialog open={archiving !== null} title="Archive workflow?" description="Archived workflows can no longer be started. Running instances are not affected." confirmLabel="Archive" danger onClose={() => setArchiving(null)}
        onConfirm={async () => { if (!archiving) return; await archive.mutateAsync(archiving.id); toast.success('Workflow archived.'); }} />
    </div>
  );
}

const INSTANCE_STATUSES = ['Pending', 'InProgress', 'AwaitingApproval', 'Completed', 'Rejected', 'Cancelled', 'Failed', 'Paused'];

function InstancesTab() {
  const url = useUrlListState();
  const status = url.get('status');
  const query = useWorkflowInstances({ status: status || undefined, page: url.page, pageSize: url.pageSize });

  const columns: Column<WorkflowInstance>[] = [
    { key: 'id', header: 'Instance', cell: (i) => <Link to={`/workflows/instances/${i.id}`} className="font-semibold text-teal-900 hover:underline">Instance {shortId(i.id)}</Link> },
    { key: 'status', header: 'Status', cell: (i) => <StatusBadge status={i.status} /> },
    { key: 'document', header: 'Document', cell: (i) => <Link to={`/documents/${i.documentId}`} className="text-teal-900 underline">Open document</Link> },
    { key: 'version', header: 'Definition version', cell: (i) => `v${i.workflowVersion}`, hideOnCard: true }
  ];

  return (
    <div className="space-y-4">
      <SelectField label="Status" hideLabel value={status} onChange={(event) => url.update({ status: event.target.value })}>
        <option value="">All statuses</option>
        {INSTANCE_STATUSES.map((value) => <option key={value} value={value}>{value}</option>)}
      </SelectField>
      {query.isError ? <ErrorState error={query.error} onRetry={() => void query.refetch()} /> : (
        <>
          <DataTable caption="Workflow instances" columns={columns} rows={query.data?.items ?? []} getRowKey={(i) => i.id} isLoading={query.isPending} isRefreshing={query.isFetching}
            empty={<EmptyState title="No workflow instances" description="Start a workflow from a document to see it here." />} />
          {query.data && query.data.items.length > 0 && <Pagination page={query.data.page} pageSize={query.data.pageSize} hasNextPage={query.data.hasNextPage} itemCount={query.data.items.length} onPageChange={url.setPage} onPageSizeChange={url.setPageSize} disabled={query.isFetching} />}
        </>
      )}
    </div>
  );
}

export function WorkflowsPage() {
  const [params, setParams] = useSearchParams();
  const tab = params.get('tab') ?? 'definitions';
  return (
    <>
      <PageHeader eyebrow="Automation" title="Workflows" description="Define review and approval flows and follow running instances." />
      <Tabs label="Workflow sections" value={tab} onChange={(id) => setParams({ tab: id })} tabs={[
        { id: 'definitions', label: 'Definitions', render: () => <DefinitionsTab /> },
        { id: 'instances', label: 'Instances', render: () => <InstancesTab /> }
      ]} />
    </>
  );
}
