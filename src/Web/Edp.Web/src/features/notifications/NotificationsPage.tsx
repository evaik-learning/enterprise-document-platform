import { useEffect, useState } from 'react';
import { ApiError } from '../../services/apiClient';
import { platformApi, type Notification } from '../../services/platformApi';

export function NotificationsPage() {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [error, setError] = useState('');
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [page, setPage] = useState(1);
  const [hasNextPage, setHasNextPage] = useState(false);
  const [loading, setLoading] = useState(true);

  const load = async (requestedPage = page) => {
    setLoading(true);
    setError('');
    try {
      const result = await platformApi.notifications.list(unreadOnly, requestedPage, 25);
      setNotifications(result.items);
      setHasNextPage(Boolean(result.hasNextPage));
      setPage(requestedPage);
    } catch (exception) {
      setError(exception instanceof ApiError ? exception.message : 'Notifications could not be loaded.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(1); }, [unreadOnly]);
  useEffect(() => {
    const refresh = () => { if (!document.hidden) void load(); };
    const interval = window.setInterval(refresh, 60_000);
    document.addEventListener('visibilitychange', refresh);
    return () => { window.clearInterval(interval); document.removeEventListener('visibilitychange', refresh); };
  }, [page, unreadOnly]);

  const read = async (id: string) => {
    try { await platformApi.notifications.read(id); await load(); }
    catch (exception) { setError(exception instanceof ApiError ? exception.message : 'The notification could not be marked as read.'); }
  };

  const readAll = async () => {
    try { await platformApi.notifications.readAll(); await load(); }
    catch (exception) { setError(exception instanceof ApiError ? exception.message : 'Notifications could not be marked as read.'); }
  };

  return <section className="space-y-6">
    <div className="flex flex-wrap justify-between gap-3"><div><p className="text-sm font-medium text-teal-700">Inbox</p><h2 className="text-3xl font-semibold">Notifications</h2><p className="mt-1 text-sm text-slate-600">Workflow, signing and document updates for your account.</p></div><div className="flex gap-2"><button className={`rounded-lg border px-4 py-2 text-sm font-semibold ${unreadOnly ? 'border-teal-300 bg-teal-50 text-teal-800' : 'border-slate-300 bg-white'}`} onClick={() => setUnreadOnly(value => !value)}>{unreadOnly ? 'Showing unread' : 'Unread only'}</button><button className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold" onClick={() => void readAll()}>Mark all read</button></div></div>
    {error && <p role="alert" className="rounded-lg bg-red-50 p-4 text-sm text-red-700">{error}</p>}
    <div aria-live="polite" className="space-y-3">{loading ? <div className="rounded-xl border border-slate-200 bg-white p-10 text-center text-sm text-slate-500">Loading notifications...</div> : notifications.length === 0 ? <div className="rounded-xl border border-dashed border-slate-300 bg-white p-10 text-center text-sm text-slate-500">You are all caught up.</div> : notifications.map(notification => { const isRead = notification.status.toLowerCase() === 'read' || Boolean(notification.readAtUtc); return <article className={`rounded-xl border bg-white p-5 shadow-sm ${isRead ? 'border-slate-200' : 'border-teal-200 bg-teal-50/30'}`} key={notification.id}><div className="flex justify-between gap-3"><div><h3 className="font-semibold">{notification.subject || notification.notificationType}</h3><p className="mt-1 whitespace-pre-wrap text-sm text-slate-600">{notification.body}</p></div>{!isRead && <button className="text-xs font-semibold text-teal-700 underline" onClick={() => void read(notification.id)}>Mark read</button>}</div><p className="mt-3 text-xs text-slate-500">{new Date(notification.createdAtUtc).toLocaleString()} · Correlation {notification.correlationId}</p></article>; })}</div>
    <div className="flex items-center justify-between"><span className="text-sm text-slate-500">Page {page}</span><div className="flex gap-2"><button disabled={page === 1 || loading} className="rounded border border-slate-300 bg-white px-3 py-2 text-sm disabled:cursor-not-allowed disabled:opacity-50" onClick={() => void load(page - 1)}>Previous</button><button disabled={!hasNextPage || loading} className="rounded border border-slate-300 bg-white px-3 py-2 text-sm disabled:cursor-not-allowed disabled:opacity-50" onClick={() => void load(page + 1)}>Next</button></div></div>
  </section>;
}
