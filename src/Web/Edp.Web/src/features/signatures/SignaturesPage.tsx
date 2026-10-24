import { Link } from 'react-router';
import type { SigningRequestSummary } from '../../api/contracts';
import { useSignatures } from '../../api/hooks/usePlatformQueries';
import { EmptyState, ErrorState } from '../../components/feedback/Feedback';
import { SelectField } from '../../components/forms/Fields';
import { DataTable, type Column } from '../../components/tables/DataTable';
import { Pagination } from '../../components/tables/Pagination';
import { StatusBadge } from '../../components/ui/Badge';
import { PageHeader } from '../../components/ui/Card';
import { useUrlListState } from '../../hooks/useUrlListState';
import { formatDate, formatDateTime } from '../../utils/format';

const STATUSES = ['Draft', 'Pending', 'InProgress', 'PartiallySigned', 'Completed', 'Declined', 'Expired', 'Cancelled', 'Failed'];

export function SignaturesPage() {
  const url = useUrlListState();
  const status = url.get('status');
  const query = useSignatures({ status: status || undefined, page: url.page, pageSize: url.pageSize });

  const columns: Column<SigningRequestSummary>[] = [
    { key: 'title', header: 'Request', cell: (r) => <Link to={`/signatures/${r.signingRequestId}`} className="font-semibold text-teal-900 hover:underline">{r.title}</Link> },
    { key: 'status', header: 'Status', cell: (r) => <StatusBadge status={r.status} /> },
    { key: 'progress', header: 'Signed', cell: (r) => `${r.signedCount} of ${r.signerCount}` },
    { key: 'expires', header: 'Expires', cell: (r) => formatDate(r.expiresAt), hideOnCard: true },
    { key: 'created', header: 'Created', cell: (r) => formatDateTime(r.createdAt) }
  ];

  return (
    <>
      <PageHeader eyebrow="Signatures" title="Signature requests" description="Track documents sent for signature and sign the requests addressed to you." />
      <div className="mb-4">
        <SelectField label="Status" hideLabel value={status} onChange={(event) => url.update({ status: event.target.value })}>
          <option value="">All statuses</option>
          {STATUSES.map((value) => <option key={value} value={value}>{value}</option>)}
        </SelectField>
      </div>
      {query.isError ? <ErrorState error={query.error} onRetry={() => void query.refetch()} /> : (
        <>
          <DataTable caption="Signature requests" columns={columns} rows={query.data?.items ?? []} getRowKey={(r) => r.signingRequestId} isLoading={query.isPending} isRefreshing={query.isFetching}
            empty={<EmptyState title="No signature requests" description="Send a document for signature from its workspace." />} />
          {query.data && query.data.items.length > 0 && <Pagination page={query.data.page} pageSize={query.data.pageSize} totalCount={query.data.totalCount} totalPages={query.data.totalPages} hasNextPage={query.data.hasNextPage} itemCount={query.data.items.length} onPageChange={url.setPage} onPageSizeChange={url.setPageSize} disabled={query.isFetching} />}
        </>
      )}
    </>
  );
}
