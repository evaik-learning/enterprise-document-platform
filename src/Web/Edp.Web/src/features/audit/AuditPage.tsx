import { useEffect, useState } from 'react';
import { useAuthentication } from '../../hooks/useAuthentication';
import { ApiError } from '../../services/apiClient';
import { platformApi, type AuditLog } from '../../services/platformApi';

const pageSize = 50;

function formatTime(value?: string) {
  if (!value) return 'Unknown time';
  const date = new Date(value);
  return `${date.toLocaleString()} (${date.toISOString()})`;
}

function safeMetadata(metadata?: Record<string, unknown>) {
  if (!metadata) return [];
  return Object.entries(metadata).filter(([key]) => !/secret|token|password|credential|authorization|content|ipaddress/i.test(key));
}

export function AuditPage() {
  const { user } = useAuthentication();
  const organizationId = user?.claims.find(claim => claim.type === 'organization_id')?.value;
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [selected, setSelected] = useState<AuditLog | null>(null);
  const [entityType, setEntityType] = useState('');
  const [entityId, setEntityId] = useState('');
  const [action, setAction] = useState('');
  const [correlationId, setCorrelationId] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [page, setPage] = useState(1);
  const [hasNextPage, setHasNextPage] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const load = async (requestedPage = page) => {
    if (!organizationId) return;
    setLoading(true);
    setError('');
    const params = new URLSearchParams({ page: String(requestedPage), pageSize: String(pageSize) });
    if (entityType) params.set('entityType', entityType);
    if (entityId) params.set('entityId', entityId);
    if (action) params.set('action', action);
    if (correlationId) params.set('correlationId', correlationId);
    if (from) params.set('from', new Date(`${from}T00:00:00`).toISOString());
    if (to) params.set('to', new Date(`${to}T23:59:59.999`).toISOString());
    try {
      const result = await platformApi.audit.search(organizationId, `&${params.toString()}`);
      setLogs(result);
      setPage(requestedPage);
      setHasNextPage(result.length === pageSize);
      setSelected(null);
    } catch (exception) {
      setError(exception instanceof ApiError ? exception.message : 'Audit activity could not be loaded.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(1); }, [organizationId]);

  if (!organizationId) return <section className="space-y-6"><div><p className="text-sm font-medium text-teal-700">Traceability</p><h2 className="text-3xl font-semibold">Audit activity</h2></div><p className="rounded-lg bg-amber-50 p-4 text-sm text-amber-800">Select an organization before viewing audit activity.</p></section>;

  return <section className="space-y-6">
    <div><p className="text-sm font-medium text-teal-700">Traceability</p><h2 className="text-3xl font-semibold">Audit activity</h2><p className="mt-1 text-sm text-slate-600">Immutable, organization-scoped events. Times show your locale with UTC available in each detail.</p></div>
    {error && <p role="alert" className="rounded-lg bg-red-50 p-4 text-sm text-red-700">{error}</p>}
    <form className="grid gap-3 rounded-xl border border-slate-200 bg-white p-4 md:grid-cols-3" onSubmit={event => { event.preventDefault(); void load(1); }}>
      <input aria-label="Audit entity type" placeholder="Entity type" value={entityType} onChange={event => setEntityType(event.target.value)} className="rounded border border-slate-300 px-3 py-2 text-sm" />
      <input aria-label="Audit entity ID" placeholder="Entity ID" value={entityId} onChange={event => setEntityId(event.target.value)} className="rounded border border-slate-300 px-3 py-2 text-sm" />
      <input aria-label="Audit action" placeholder="Action" value={action} onChange={event => setAction(event.target.value)} className="rounded border border-slate-300 px-3 py-2 text-sm" />
      <input aria-label="Audit correlation ID" placeholder="Correlation ID" value={correlationId} onChange={event => setCorrelationId(event.target.value)} className="rounded border border-slate-300 px-3 py-2 text-sm" />
      <label className="text-xs text-slate-500">From<input type="date" value={from} onChange={event => setFrom(event.target.value)} className="mt-1 block w-full rounded border border-slate-300 px-3 py-2 text-sm text-slate-900" /></label>
      <label className="text-xs text-slate-500">To<input type="date" value={to} onChange={event => setTo(event.target.value)} className="mt-1 block w-full rounded border border-slate-300 px-3 py-2 text-sm text-slate-900" /></label>
      <button className="rounded bg-slate-900 px-4 py-2 text-sm font-semibold text-white md:col-span-3 md:justify-self-start" type="submit">Apply filters</button>
    </form>
    <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm"><table className="min-w-full text-left text-sm"><thead className="border-b border-slate-200 bg-slate-50 text-xs uppercase tracking-wide text-slate-500"><tr><th className="px-5 py-3">Time</th><th className="px-5 py-3">Action</th><th className="px-5 py-3">Entity</th><th className="px-5 py-3">Correlation</th><th className="px-5 py-3"><span className="sr-only">Details</span></th></tr></thead><tbody className="divide-y divide-slate-100">{loading ? <tr><td colSpan={5} className="px-5 py-12 text-center text-slate-500">Loading audit events...</td></tr> : logs.length === 0 ? <tr><td colSpan={5} className="px-5 py-12 text-center text-slate-500">No audit events match these filters.</td></tr> : logs.map(log => <tr key={log.id}><td className="px-5 py-4 text-slate-500">{log.timestamp ? new Date(log.timestamp).toLocaleString() : 'Unknown'}</td><td className="px-5 py-4 font-medium">{log.action}</td><td className="px-5 py-4">{log.entityType ?? 'Unknown'} {log.entityId ? `· ${log.entityId}` : ''}</td><td className="px-5 py-4 font-mono text-xs text-slate-500">{log.correlationId ?? 'Unknown'}</td><td className="px-5 py-4 text-right"><button className="font-semibold text-teal-700 underline" onClick={() => setSelected(log)}>View details</button></td></tr>)}</tbody></table></div>
    <div className="flex items-center justify-between"><span className="text-sm text-slate-500">Page {page}</span><div className="flex gap-2"><button disabled={page === 1 || loading} className="rounded border border-slate-300 bg-white px-3 py-2 text-sm disabled:opacity-50" onClick={() => void load(page - 1)}>Previous</button><button disabled={!hasNextPage || loading} className="rounded border border-slate-300 bg-white px-3 py-2 text-sm disabled:opacity-50" onClick={() => void load(page + 1)}>Next</button></div></div>
    {selected && <aside aria-label="Audit event details" className="rounded-xl border border-teal-200 bg-teal-50/40 p-5"><div className="flex items-start justify-between gap-3"><div><h3 className="font-semibold">Audit event details</h3><p className="mt-1 text-sm text-slate-600">{formatTime(selected.timestamp)}</p></div><button className="text-sm font-semibold text-slate-700 underline" onClick={() => setSelected(null)}>Close</button></div><dl className="mt-4 grid gap-3 text-sm md:grid-cols-2"><div><dt className="text-slate-500">Action</dt><dd className="font-medium">{selected.action}</dd></div><div><dt className="text-slate-500">Actor</dt><dd>{selected.userId ?? 'System'}</dd></div><div><dt className="text-slate-500">Entity</dt><dd>{selected.entityType} · {selected.entityId}</dd></div><div><dt className="text-slate-500">Correlation ID</dt><dd className="font-mono text-xs">{selected.correlationId ?? 'Unknown'}</dd></div></dl><div className="mt-4"><h4 className="text-sm font-semibold">Safe summary</h4>{safeMetadata(selected.metadata).length === 0 ? <p className="mt-1 text-sm text-slate-500">No safe metadata is available.</p> : <dl className="mt-2 space-y-2 text-sm">{safeMetadata(selected.metadata).map(([key, value]) => <div className="flex gap-3" key={key}><dt className="min-w-32 text-slate-500">{key}</dt><dd className="wrap-break-word">{typeof value === 'object' ? JSON.stringify(value) : String(value)}</dd></div>)}</dl>}</div></aside>}
  </section>;
}
