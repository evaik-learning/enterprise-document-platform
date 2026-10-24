import { useEffect, useState } from 'react';
import { useAuditSearch } from '../../api/hooks/usePlatformQueries';
import { isFeatureEnabled } from '../../app/featureFlags';
import { useAuth } from '../../auth/useAuth';
import { ErrorState } from '../../components/feedback/Feedback';
import { DateField, TextField } from '../../components/forms/Fields';
import { Pagination } from '../../components/tables/Pagination';
import { Button } from '../../components/ui/Button';
import { PageHeader } from '../../components/ui/Card';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { useUrlListState } from '../../hooks/useUrlListState';
import { AuditTable } from './AuditTable';

/** Immutable audit search. Records are read-only; filters are kept in the URL. */
export function AuditPage() {
  const { organizationId } = useAuth();
  const url = useUrlListState(50);
  const advanced = isFeatureEnabled('advanced.audit.search');
  const [entityId, setEntityId] = useState(url.get('entityId'));
  const [action, setAction] = useState(url.get('action'));
  const [correlationId, setCorrelationId] = useState(url.get('correlationId'));
  const debounced = useDebouncedValue(JSON.stringify({ entityId, action, correlationId }));

  useEffect(() => {
    const parsed = JSON.parse(debounced) as { entityId: string; action: string; correlationId: string };
    if (parsed.entityId === url.get('entityId') && parsed.action === url.get('action') && parsed.correlationId === url.get('correlationId')) return;
    url.update({ entityId: parsed.entityId || null, action: parsed.action || null, correlationId: parsed.correlationId || null });
  }, [debounced]); // eslint-disable-line react-hooks/exhaustive-deps

  const from = url.get('from');
  const to = url.get('to');
  const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  const entityIdFilter = url.get('entityId');
  const query = useAuditSearch(organizationId, {
    entityId: guid.test(entityIdFilter) ? entityIdFilter : undefined,
    action: url.get('action') || undefined,
    correlationId: url.get('correlationId') || undefined,
    from: from ? `${from}T00:00:00Z` : undefined,
    to: to ? `${to}T23:59:59Z` : undefined,
    page: url.page,
    pageSize: url.pageSize
  });
  const invalidEntity = entityId !== '' && !guid.test(entityId);

  return (
    <>
      <PageHeader eyebrow="Compliance" title="Audit log" description="Immutable records of what happened, who did it and when." />
      <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
        <TextField label="Resource ID" value={entityId} onChange={(event) => setEntityId(event.target.value)} error={invalidEntity ? 'Enter a valid ID (GUID).' : undefined} />
        <TextField label="Action" value={action} onChange={(event) => setAction(event.target.value)} />
        {advanced && <TextField label="Correlation ID" value={correlationId} onChange={(event) => setCorrelationId(event.target.value)} />}
        {advanced && <DateField label="From" value={from} onChange={(event) => url.update({ from: event.target.value })} />}
        {advanced && <DateField label="To" value={to} onChange={(event) => url.update({ to: event.target.value })} />}
      </div>
      {(entityId || action || correlationId || from || to) && <div className="mb-4"><Button variant="ghost" onClick={() => { setEntityId(''); setAction(''); setCorrelationId(''); url.update({ entityId: null, action: null, correlationId: null, from: null, to: null }); }}>Clear filters</Button></div>}
      {query.isError ? <ErrorState error={query.error} onRetry={() => void query.refetch()} /> : (
        <>
          <AuditTable logs={query.data?.items ?? []} isLoading={query.isPending} isRefreshing={query.isFetching} />
          {query.data && query.data.items.length > 0 && <Pagination page={query.data.page} pageSize={query.data.pageSize} hasNextPage={query.data.hasNextPage} itemCount={query.data.items.length} onPageChange={url.setPage} onPageSizeChange={url.setPageSize} disabled={query.isFetching} />}
        </>
      )}
    </>
  );
}
