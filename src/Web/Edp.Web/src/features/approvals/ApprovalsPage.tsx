import { Link } from 'react-router';
import type { ApprovalInboxItem } from '../../api/contracts';
import { useApprovalInbox } from '../../api/hooks/useWorkflows';
import { EmptyState, ErrorState } from '../../components/feedback/Feedback';
import { DataTable, type Column } from '../../components/tables/DataTable';
import { Pagination } from '../../components/tables/Pagination';
import { StatusBadge } from '../../components/ui/Badge';
import { ButtonLink } from '../../components/ui/Button';
import { PageHeader } from '../../components/ui/Card';
import { useUrlListState } from '../../hooks/useUrlListState';
import { formatDate } from '../../utils/format';

export function ApprovalsPage() {
  const url = useUrlListState();
  const query = useApprovalInbox(url.page, url.pageSize);

  const columns: Column<ApprovalInboxItem>[] = [
    { key: 'document', header: 'Document', cell: (item) => item.documentId ? <Link className="font-semibold text-teal-900 hover:underline" to={`/approvals/${item.taskId}`}>{item.documentName ?? 'Document'}</Link> : <Link className="font-semibold text-teal-900 hover:underline" to={`/approvals/${item.taskId}`}>Task {item.taskId.slice(0, 8)}</Link> },
    { key: 'workflow', header: 'Workflow', cell: (item) => item.workflowName ?? '—' },
    { key: 'status', header: 'Status', cell: (item) => <StatusBadge status={item.status} /> },
    { key: 'due', header: 'Due', cell: (item) => (item.deadlineAt ? formatDate(item.deadlineAt) : '—') },
    { key: 'actions', header: 'Action', srOnlyHeader: true, className: 'text-right', cell: (item) => <ButtonLink size="sm" variant="primary" to={`/approvals/${item.taskId}`} aria-label={`Review ${item.documentName ?? `task ${item.taskId.slice(0, 8)}`}`}>Review</ButtonLink> }
  ];

  return (
    <>
      <PageHeader eyebrow="Inbox" title="My pending approvals" description="Review documents waiting for your decision." />
      {query.isError ? <ErrorState error={query.error} onRetry={() => void query.refetch()} /> : (
        <>
          <DataTable caption="Pending approvals" columns={columns} rows={query.data?.items ?? []} getRowKey={(item) => item.taskId} isLoading={query.isPending} isRefreshing={query.isFetching}
            empty={<EmptyState title="No pending approvals" description="You are all caught up." />} />
          {query.data && query.data.items.length > 0 && <Pagination page={query.data.page} pageSize={query.data.pageSize} hasNextPage={query.data.hasNextPage} itemCount={query.data.items.length} onPageChange={url.setPage} onPageSizeChange={url.setPageSize} disabled={query.isFetching} />}
        </>
      )}
    </>
  );
}
