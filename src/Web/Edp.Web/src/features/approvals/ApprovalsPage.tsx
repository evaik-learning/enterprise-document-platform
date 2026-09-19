import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { ApiError } from '../../services/apiClient';
import { platformApi, type ApprovalTask } from '../../services/platformApi';

export function ApprovalsPage() {
  const [tasks, setTasks] = useState<ApprovalTask[]>([]);
  const [page, setPage] = useState(1);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState('');
  const [comments, setComments] = useState<Record<string, string>>({});
  const [delegateOpen, setDelegateOpen] = useState<string>();
  const [delegateUser, setDelegateUser] = useState('');
  const [delegateReason, setDelegateReason] = useState('');

  const load = async (nextPage = page) => {
    setError('');
    try { setTasks(await platformApi.workflows.myApprovals(nextPage)); }
    catch (e) { setError(e instanceof ApiError ? e.message : 'Approvals could not be loaded.'); }
  };
  useEffect(() => { void load(); }, [page]);

  const act = async (task: ApprovalTask, action: 'approve' | 'reject') => {
    const comment = comments[task.id]?.trim() ?? '';
    if (action === 'reject' && !comment) { setError('A rejection reason is required.'); return; }
    if (!window.confirm(`${action === 'approve' ? 'Approve' : 'Reject'} this approval task?`)) return;
    setBusy(task.id); setError('');
    try {
      if (action === 'approve') await platformApi.workflows.approve(task.id, comment || undefined);
      else await platformApi.workflows.reject(task.id, comment);
      setTasks(current => current.filter(item => item.id !== task.id));
    } catch (e) {
      setError(e instanceof ApiError && e.status === 409 ? 'This task changed while you were reviewing it. Reloading the approval queue.' : e instanceof ApiError ? e.message : 'The task could not be updated.');
      await load();
    } finally { setBusy(''); }
  };

  const delegate = async (task: ApprovalTask) => {
    if (!delegateUser.trim() || !delegateReason.trim()) { setError('A delegate user ID and reason are required.'); return; }
    setBusy(task.id); setError('');
    try {
      await platformApi.workflows.delegate(task.id, delegateUser.trim(), delegateReason.trim());
      setDelegateOpen(undefined); setDelegateUser(''); setDelegateReason(''); await load();
    } catch (e) {
      setError(e instanceof ApiError && e.status === 409 ? 'This task changed while you were reviewing it. Reloading the approval queue.' : e instanceof ApiError ? e.message : 'The task could not be delegated.');
    } finally { setBusy(''); }
  };

  return <section className="space-y-6"><div><p className="text-sm font-medium text-teal-700">My work</p><h2 className="text-3xl font-semibold">Approvals</h2><p className="mt-1 text-sm text-slate-600">Review approval tasks assigned to your current organization identity.</p></div>{error && <Alert message={error} onRetry={() => void load()} />}<div className="space-y-3">{tasks.length === 0 ? <div className="rounded-xl border border-dashed border-slate-300 bg-white p-10 text-center text-sm text-slate-500">You have no outstanding approval tasks on this page.</div> : tasks.map(task => <article key={task.id} className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm"><div className="flex flex-wrap justify-between gap-3"><div><h3 className="font-semibold">Approval task</h3><p className="mt-1 text-xs text-slate-500">Task {task.id}</p><Link to={`/workflow-instances/${task.workflowInstanceId}`} className="mt-1 inline-block text-sm text-teal-700 hover:underline">Open workflow instance</Link></div><Status status={task.status} /></div><dl className="mt-4 grid gap-2 text-sm sm:grid-cols-2"><div><dt className="text-slate-500">Workflow instance</dt><dd className="font-medium">{task.workflowInstanceId}</dd></div><div><dt className="text-slate-500">Assigned user</dt><dd className="font-medium">{task.assignedToUserId}</dd></div><div><dt className="text-slate-500">Deadline</dt><dd className="font-medium">{task.deadlineAt ? formatDate(task.deadlineAt) : 'No deadline'}</dd></div></dl><textarea aria-label={`Comment for approval ${task.id}`} placeholder="Optional approval comment, required for rejection" value={comments[task.id] ?? ''} onChange={event => setComments(current => ({ ...current, [task.id]: event.target.value }))} className="control mt-4 min-h-24 w-full" /><div className="mt-3 flex flex-wrap gap-2"><button disabled={busy === task.id} className="rounded-lg bg-emerald-700 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50" onClick={() => void act(task, 'approve')}>Approve</button><button disabled={busy === task.id} className="rounded-lg border border-red-300 px-4 py-2 text-sm font-semibold text-red-700 disabled:opacity-50" onClick={() => void act(task, 'reject')}>Reject</button><button disabled={busy === task.id} className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-semibold disabled:opacity-50" onClick={() => { setDelegateOpen(task.id); setError(''); }}>Delegate</button></div>{delegateOpen === task.id && <form className="mt-4 space-y-3 rounded-lg bg-slate-50 p-4" onSubmit={event => { event.preventDefault(); void delegate(task); }}><p className="text-sm font-semibold">Delegate approval</p><p className="text-xs text-slate-500">The member directory lookup is not available in the current API, so delegation requires a verified organization member ID.</p><input required value={delegateUser} onChange={event => setDelegateUser(event.target.value)} placeholder="Organization member user ID" className="control" /><textarea required value={delegateReason} onChange={event => setDelegateReason(event.target.value)} placeholder="Reason for delegation" className="control min-h-20" /><div className="flex gap-2"><button disabled={busy === task.id} className="rounded-lg bg-slate-900 px-3 py-2 text-sm font-semibold text-white disabled:opacity-50">Confirm delegation</button><button type="button" className="rounded-lg border border-slate-300 px-3 py-2 text-sm" onClick={() => setDelegateOpen(undefined)}>Cancel</button></div></form>}</article>)}</div><div className="flex items-center justify-between"><button disabled={page === 1} className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-semibold disabled:opacity-50" onClick={() => setPage(current => current - 1)}>Previous</button><span className="text-sm text-slate-500">Page {page}</span><button disabled={tasks.length < 25} className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-semibold disabled:opacity-50" onClick={() => setPage(current => current + 1)}>Next</button></div></section>;
}

function Alert({ message, onRetry }: { message: string; onRetry?: () => void }) { return <p role="alert" className="rounded-lg bg-red-50 p-4 text-sm text-red-700">{message}{onRetry && <button className="ml-2 font-semibold underline" onClick={onRetry}>Retry</button>}</p>; }
function Status({ status }: { status: string }) { return <span className="rounded-full bg-amber-50 px-2.5 py-1 text-xs font-semibold text-amber-800">{status}</span>; }
function formatDate(value: string) { return new Date(value).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' }); }
