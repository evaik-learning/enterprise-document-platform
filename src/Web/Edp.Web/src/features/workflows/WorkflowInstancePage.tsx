import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router';
import { ApiError } from '../../services/apiClient';
import { platformApi, type WorkflowHistoryEntry, type WorkflowInstance, type WorkflowTransition } from '../../services/platformApi';

export function WorkflowInstancePage() {
  const { instanceId = '' } = useParams();
  const [instance, setInstance] = useState<WorkflowInstance>();
  const [history, setHistory] = useState<WorkflowHistoryEntry[]>([]);
  const [transitions, setTransitions] = useState<WorkflowTransition[]>([]);
  const [reason, setReason] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const load = async () => {
    setError('');
    try {
      const [nextInstance, nextHistory] = await Promise.all([
        platformApi.workflows.instance(instanceId),
        platformApi.workflows.history(instanceId)
      ]);
      const versions = await platformApi.workflows.versions(nextInstance.workflowId);
      const version = versions.find(item => item.version === nextInstance.workflowVersion);
      const graph = version ? await platformApi.workflows.graph(version.id) : undefined;
      setInstance(nextInstance);
      setHistory(nextHistory);
      setTransitions(graph?.transitions ?? []);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Workflow instance could not be loaded.');
    }
  };

  useEffect(() => { void load(); }, [instanceId]);

  const action = async (kind: 'suspend' | 'cancel' | 'resume') => {
    if (!instance) return;
    if ((kind === 'suspend' || kind === 'cancel') && !reason.trim()) {
      setError('A reason is required for this action.');
      return;
    }
    if (!window.confirm(`${kind[0].toUpperCase()}${kind.slice(1)} this workflow instance?`)) return;
    setBusy(true);
    setError('');
    try {
      if (kind === 'suspend') await platformApi.workflows.suspend(instance.id, reason.trim());
      if (kind === 'cancel') await platformApi.workflows.cancel(instance.id, reason.trim());
      if (kind === 'resume') await platformApi.workflows.resume(instance.id);
      setReason('');
      await load();
    } catch (e) {
      setError(e instanceof ApiError && e.status === 409 ? 'This workflow changed while you were reviewing it. Reloading.' : e instanceof ApiError ? e.message : 'The workflow action could not be completed.');
      await load();
    } finally { setBusy(false); }
  };

  const executeTransition = async (transition: WorkflowTransition) => {
    if (!instance || !window.confirm('Execute this workflow transition?')) return;
    setBusy(true);
    setError('');
    try {
      await platformApi.workflows.transition(instance.id, transition.id);
      await load();
    } catch (e) {
      setError(e instanceof ApiError && e.status === 409 ? 'This workflow changed while you were reviewing it. Reloading.' : e instanceof ApiError ? e.message : 'The workflow transition could not be completed.');
      await load();
    } finally { setBusy(false); }
  };

  if (!instance) return <section className="space-y-4"><Link to="/approvals" className="text-sm text-teal-700 hover:underline">Back to approvals</Link>{error ? <Alert message={error} onRetry={() => void load()} /> : <p className="text-slate-500">Loading workflow instance...</p>}</section>;

  const status = instance.status.toLowerCase();
  const canPause = ['inprogress', 'awaitingapproval'].includes(status);
  const canResume = status === 'paused';
  const canCancel = !['completed', 'rejected', 'cancelled'].includes(status);
  const outgoingTransitions = transitions.filter(transition => transition.fromStateId === instance.currentStateId);

  return <section className="space-y-6"><Link to="/approvals" className="text-sm font-medium text-teal-700 hover:underline">Back to approvals</Link><div className="flex flex-wrap items-start justify-between gap-4"><div><p className="text-sm text-slate-500">Workflow instance</p><h2 className="text-2xl font-semibold">{instance.id}</h2><p className="mt-1 text-sm text-slate-600">Document {instance.documentId} · Version {instance.workflowVersion}</p></div><Status status={instance.status} /></div>{error && <Alert message={error} onRetry={() => void load()} />}<div className="grid gap-6 lg:grid-cols-[1fr_1.4fr]"><section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm"><h3 className="text-lg font-semibold">Allowed actions</h3><p className="mt-1 text-sm text-slate-500">Actions are enabled from the current server status.</p><textarea value={reason} onChange={event => setReason(event.target.value)} placeholder="Reason for suspend or cancel" className="control mt-4 min-h-24" /><div className="mt-3 flex flex-wrap gap-2">{canPause && <button disabled={busy} className="rounded-lg border border-amber-300 px-3 py-2 text-sm font-semibold text-amber-800 disabled:opacity-50" onClick={() => void action('suspend')}>Suspend</button>}{canResume && <button disabled={busy} className="rounded-lg bg-teal-700 px-3 py-2 text-sm font-semibold text-white disabled:opacity-50" onClick={() => void action('resume')}>Resume</button>}{canCancel && <button disabled={busy} className="rounded-lg border border-red-300 px-3 py-2 text-sm font-semibold text-red-700 disabled:opacity-50" onClick={() => void action('cancel')}>Cancel instance</button>}</div>{outgoingTransitions.length > 0 && <div className="mt-5 border-t border-slate-200 pt-4"><h4 className="font-semibold">Available transitions</h4><div className="mt-2 flex flex-wrap gap-2">{outgoingTransitions.map(transition => <button key={transition.id} disabled={busy} className="rounded-lg border border-teal-300 px-3 py-2 text-sm font-semibold text-teal-800 disabled:opacity-50" onClick={() => void executeTransition(transition)}>Transition to {transition.toStateId}</button>)}</div></div>}<dl className="mt-5 space-y-2 text-sm"><div className="flex justify-between gap-4"><dt className="text-slate-500">Current state</dt><dd className="font-medium">{instance.currentStateId ?? 'Not assigned'}</dd></div><div className="flex justify-between gap-4"><dt className="text-slate-500">Workflow</dt><dd className="font-medium">{instance.workflowId}</dd></div></dl></section><section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm"><h3 className="text-lg font-semibold">History</h3>{history.length === 0 ? <p className="mt-3 text-sm text-slate-500">No history entries yet.</p> : <ol className="mt-3 space-y-3">{history.map(entry => <li key={entry.id} className="border-l-2 border-slate-200 pl-4"><p className="font-medium">{entry.description || entry.eventType}</p><p className="text-xs text-slate-500">{formatDate(entry.eventAt)} · {entry.eventType}{entry.userId ? ` · ${entry.userId}` : ''}</p></li>)}</ol>}</section></div></section>;
}

function Alert({ message, onRetry }: { message: string; onRetry?: () => void }) { return <p role="alert" className="rounded-lg bg-red-50 p-4 text-sm text-red-700">{message}{onRetry && <button className="ml-2 font-semibold underline" onClick={onRetry}>Retry</button>}</p>; }
function Status({ status }: { status: string }) { return <span className="rounded-full bg-slate-100 px-3 py-1 text-sm font-semibold">{status}</span>; }
function formatDate(value: string) { return new Date(value).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' }); }
