import { useEffect, useState } from 'react';
import { Link } from 'react-router';
import type { DocumentSummary } from '../../api/contracts';
import { documentApi } from '../../api/endpoints';
import { toUserMessage } from '../../api/errors';
import { useDocuments } from '../../api/hooks/useDocuments';
import { useTemplates } from '../../api/hooks/useTemplates';
import { Permissions } from '../../auth/permissions';
import { PermissionGate } from '../../auth/PermissionGate';
import { Alert, EmptyState, ErrorState } from '../../components/feedback/Feedback';
import { useToast } from '../../components/feedback/Toast';
import { SelectField, TextField } from '../../components/forms/Fields';
import { DataTable, type Column } from '../../components/tables/DataTable';
import { Pagination } from '../../components/tables/Pagination';
import { StatusBadge } from '../../components/ui/Badge';
import { Button, ButtonLink } from '../../components/ui/Button';
import { PageHeader } from '../../components/ui/Card';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { useUrlListState } from '../../hooks/useUrlListState';
import { saveBlob } from '../../utils/download';
import { formatDateTime } from '../../utils/format';

const STATUSES = ['Requested', 'Generating', 'Generated', 'Failed', 'InReview', 'PendingApproval', 'Approved', 'Rejected', 'PendingSignature', 'Signed', 'Completed', 'Archived', 'Cancelled'];
const DOWNLOADABLE = new Set(['Generated', 'InReview', 'PendingApproval', 'Approved', 'PendingSignature', 'Signed', 'Completed']);

export function DocumentsPage() {
  const url = useUrlListState();
  const toast = useToast();
  const [searchText, setSearchText] = useState(url.get('search'));
  const debounced = useDebouncedValue(searchText);
  const search = url.get('search');
  const status = url.get('status');
  const templateId = url.get('templateId');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { if (debounced !== search) url.update({ search: debounced || null }); }, [debounced]); // eslint-disable-line react-hooks/exhaustive-deps

  const query = useDocuments({ search: search || undefined, status: status || undefined, templateId: templateId || undefined, page: url.page, pageSize: url.pageSize });
  const templates = useTemplates({ page: 1, pageSize: 100 });
  const templateName = (id: string) => templates.data?.items.find((template) => template.id === id)?.name ?? '—';

  const download = async (document: DocumentSummary) => {
    setError(null);
    try { saveBlob(await documentApi.download(document.id), `${document.name}.docx`); toast.success('Download started.'); } catch (cause) { setError(toUserMessage(cause)); }
  };

  const columns: Column<DocumentSummary>[] = [
    { key: 'name', header: 'Document', cell: (d) => <span><Link to={`/documents/${d.id}`} className="font-semibold text-teal-900 hover:underline">{d.name}</Link><span className="block text-xs text-slate-600">{d.documentType}</span></span> },
    { key: 'template', header: 'Template', cell: (d) => templateName(d.templateId) },
    { key: 'version', header: 'Version', cell: (d) => `v${d.currentVersionNumber}`, hideOnCard: true },
    { key: 'status', header: 'Status', cell: (d) => <StatusBadge status={d.status} /> },
    { key: 'created', header: 'Created', cell: (d) => formatDateTime(d.createdAt) },
    {
      key: 'actions', header: 'Actions', srOnlyHeader: true, className: 'text-right',
      cell: (d) => (
        <span className="inline-flex gap-1">
          <ButtonLink size="sm" variant="ghost" to={`/documents/${d.id}`} aria-label={`Open ${d.name}`}>Open</ButtonLink>
          {DOWNLOADABLE.has(d.status) && <Button size="sm" variant="ghost" aria-label={`Download ${d.name}`} onClick={() => void download(d)}>Download</Button>}
        </span>
      )
    }
  ];

  const filtered = !!(search || status || templateId);

  return (
    <>
      <PageHeader eyebrow="Work queue" title="Documents" description="Generate, review, route for approval and sign organization documents."
        actions={<PermissionGate permission={Permissions.DocumentGenerate}><ButtonLink variant="primary" to="/documents/new">Generate document</ButtonLink></PermissionGate>} />

      <div className="mb-4 flex flex-wrap items-end gap-3">
        <TextField label="Search documents" hideLabel type="search" placeholder="Search by name…" value={searchText} onChange={(event) => setSearchText(event.target.value)} className="min-w-56 flex-1" />
        <SelectField label="Status" hideLabel value={status} onChange={(event) => url.update({ status: event.target.value })}>
          <option value="">All statuses</option>
          {STATUSES.map((value) => <option key={value} value={value}>{value}</option>)}
        </SelectField>
        <SelectField label="Template" hideLabel value={templateId} onChange={(event) => url.update({ templateId: event.target.value })}>
          <option value="">All templates</option>
          {templates.data?.items.map((template) => <option key={template.id} value={template.id}>{template.name}</option>)}
        </SelectField>
        {filtered && <Button variant="ghost" onClick={() => { setSearchText(''); url.update({ search: null, status: null, templateId: null }); }}>Clear filters</Button>}
      </div>

      {error && <div className="mb-4"><Alert tone="error" onClose={() => setError(null)}>{error}</Alert></div>}

      {query.isError ? <ErrorState error={query.error} onRetry={() => void query.refetch()} /> : (
        <>
          <DataTable
            caption="Documents"
            columns={columns}
            rows={query.data?.items ?? []}
            getRowKey={(d) => d.id}
            isLoading={query.isPending}
            isRefreshing={query.isFetching}
            empty={<EmptyState title={filtered ? 'No documents match your filters' : 'No documents yet'} description={filtered ? undefined : 'Generate your first document from an active template.'}
              action={!filtered ? <PermissionGate permission={Permissions.DocumentGenerate}><ButtonLink variant="primary" to="/documents/new">Generate document</ButtonLink></PermissionGate> : undefined} />}
          />
          {query.data && query.data.items.length > 0 && <Pagination page={query.data.page} pageSize={query.data.pageSize} totalCount={query.data.totalCount} totalPages={query.data.totalPages} hasNextPage={query.data.hasNextPage} itemCount={query.data.items.length} onPageChange={url.setPage} onPageSizeChange={url.setPageSize} disabled={query.isFetching} />}
        </>
      )}
    </>
  );
}
