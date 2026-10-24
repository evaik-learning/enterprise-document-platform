import { useEffect, useState } from 'react';
import { Link } from 'react-router';
import type { Template } from '../../api/contracts';
import { useTemplates } from '../../api/hooks/useTemplates';
import { Permissions } from '../../auth/permissions';
import { PermissionGate } from '../../auth/PermissionGate';
import { EmptyState, ErrorState } from '../../components/feedback/Feedback';
import { SelectField, TextField } from '../../components/forms/Fields';
import { DataTable, type Column } from '../../components/tables/DataTable';
import { Pagination } from '../../components/tables/Pagination';
import { StatusBadge } from '../../components/ui/Badge';
import { ButtonLink } from '../../components/ui/Button';
import { PageHeader } from '../../components/ui/Card';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { useUrlListState } from '../../hooks/useUrlListState';
import { formatDateTime } from '../../utils/format';

const STATUSES = ['Draft', 'Active', 'Inactive', 'Archived'];

export function TemplatesPage() {
  const url = useUrlListState();
  const [searchText, setSearchText] = useState(url.get('search'));
  const debounced = useDebouncedValue(searchText);
  const search = url.get('search');
  const status = url.get('status');

  useEffect(() => { if (debounced !== search) url.update({ search: debounced || null }); }, [debounced]); // eslint-disable-line react-hooks/exhaustive-deps

  const query = useTemplates({ search: search || undefined, status: status || undefined, page: url.page, pageSize: url.pageSize });

  const columns: Column<Template>[] = [
    { key: 'name', header: 'Name', cell: (template) => <span><Link to={`/templates/${template.id}`} className="font-semibold text-teal-900 hover:underline">{template.name}</Link><span className="block text-xs text-slate-600">{template.code}</span></span> },
    { key: 'status', header: 'Status', cell: (template) => <StatusBadge status={template.status} /> },
    { key: 'version', header: 'Current version', cell: (template) => (template.currentVersionId ? 'Active version set' : 'None'), hideOnCard: true },
    { key: 'owner', header: 'Owner', cell: (template) => template.createdBy ?? '—', hideOnCard: true },
    { key: 'updated', header: 'Updated', cell: (template) => formatDateTime(template.modifiedAt ?? template.createdAt) },
    {
      key: 'actions', header: 'Actions', srOnlyHeader: true, className: 'text-right',
      cell: (template) => (
        <span className="inline-flex gap-1">
          <ButtonLink size="sm" variant="ghost" to={`/templates/${template.id}`} aria-label={`View ${template.name}`}>View</ButtonLink>
          <PermissionGate permission={Permissions.DocumentGenerate}>
            {template.status === 'Active' && <ButtonLink size="sm" variant="ghost" to={`/documents/new?templateId=${template.id}`} aria-label={`Generate document from ${template.name}`}>Generate</ButtonLink>}
          </PermissionGate>
        </span>
      )
    }
  ];

  const filtered = !!search || !!status;

  return (
    <>
      <PageHeader eyebrow="Library" title="Templates" description="Manage document templates, their versions and placeholders."
        actions={<PermissionGate permission={Permissions.TemplateWrite}><ButtonLink variant="primary" to="/templates/new">Create template</ButtonLink></PermissionGate>} />

      <div className="mb-4 flex flex-wrap items-end gap-3">
        <TextField label="Search templates" hideLabel type="search" placeholder="Search by name or code…" value={searchText} onChange={(event) => setSearchText(event.target.value)} className="min-w-56 flex-1" />
        <SelectField label="Status" hideLabel value={status} onChange={(event) => url.update({ status: event.target.value })}>
          <option value="">All statuses</option>
          {STATUSES.map((value) => <option key={value} value={value}>{value}</option>)}
        </SelectField>
      </div>

      {query.isError ? <ErrorState error={query.error} onRetry={() => void query.refetch()} /> : (
        <>
          <DataTable
            caption="Templates"
            columns={columns}
            rows={query.data?.items ?? []}
            getRowKey={(template) => template.id}
            isLoading={query.isPending}
            isRefreshing={query.isFetching}
            empty={
              <EmptyState
                title={filtered ? 'No templates match your filters' : 'No templates found'}
                description={filtered ? 'Try a different search or status.' : 'Create your first template.'}
                action={<PermissionGate permission={Permissions.TemplateWrite}>{!filtered && <ButtonLink variant="primary" to="/templates/new">Create template</ButtonLink>}</PermissionGate>}
              />
            }
          />
          {query.data && query.data.items.length > 0 && (
            <Pagination page={query.data.page} pageSize={query.data.pageSize} totalCount={query.data.totalCount} totalPages={query.data.totalPages} hasNextPage={query.data.hasNextPage} itemCount={query.data.items.length} onPageChange={url.setPage} onPageSizeChange={url.setPageSize} disabled={query.isFetching} />
          )}
        </>
      )}
    </>
  );
}
