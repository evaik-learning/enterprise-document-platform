import { useState } from 'react';
import type { AuditLog } from '../../api/contracts';
import { EmptyState } from '../../components/feedback/Feedback';
import { DataTable, type Column } from '../../components/tables/DataTable';
import { Button } from '../../components/ui/Button';
import { formatDateTime } from '../../utils/format';
import { humanize } from '../../utils/status';

/** Read-only audit records. Sensitive metadata is already stripped by the Audit service; the UI never edits entries. */
export function AuditTable({ logs, isLoading, isRefreshing }: { logs: AuditLog[]; isLoading?: boolean; isRefreshing?: boolean }) {
  const [expanded, setExpanded] = useState<string | null>(null);
  const columns: Column<AuditLog>[] = [
    { key: 'time', header: 'Timestamp', cell: (log) => formatDateTime(log.timestamp) },
    { key: 'action', header: 'Action', cell: (log) => <span className="font-medium">{humanize(log.action.replace(/DomainEvent$|Event$/, ''))}</span> },
    { key: 'resource', header: 'Resource', cell: (log) => <span>{log.entityType ? humanize(log.entityType.replace(/DomainEvent$|Event$/, '')) : '—'}<span className="block text-xs text-slate-600">{log.entityId ?? ''}</span></span> },
    { key: 'actor', header: 'Actor', cell: (log) => log.userId ?? 'System', hideOnCard: true },
    { key: 'correlation', header: 'Correlation ID', cell: (log) => <code className="text-xs">{log.correlationId ?? '—'}</code>, hideOnCard: true },
    {
      key: 'details', header: 'Details', className: 'text-right',
      cell: (log) => (
        <div>
          <Button size="sm" variant="ghost" aria-expanded={expanded === log.id} aria-label={`${expanded === log.id ? 'Hide' : 'Show'} details for ${log.action}`} onClick={() => setExpanded(expanded === log.id ? null : log.id)}>{expanded === log.id ? 'Hide' : 'Details'}</Button>
          {expanded === log.id && <pre className="mt-2 max-h-48 overflow-auto rounded bg-slate-100 p-2 text-left text-xs">{JSON.stringify({ source: 'audit-service', correlationId: log.correlationId, metadata: log.metadata ?? {} }, null, 2)}</pre>}
        </div>
      )
    }
  ];
  return <DataTable caption="Audit records" columns={columns} rows={logs} getRowKey={(log) => log.id} isLoading={isLoading} isRefreshing={isRefreshing} empty={<EmptyState title="No audit records" description="No recorded events match this view." />} />;
}
