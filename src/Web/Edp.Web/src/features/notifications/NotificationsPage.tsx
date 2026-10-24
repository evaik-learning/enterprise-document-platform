import { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router';
import type { AppNotification, NotificationPreferences } from '../../api/contracts';
import { toUserMessage } from '../../api/errors';
import { useNotificationMutations, useNotificationPreferences, useNotifications } from '../../api/hooks/usePlatformQueries';
import { Alert, EmptyState, ErrorState } from '../../components/feedback/Feedback';
import { QueryBoundary } from '../../components/feedback/QueryBoundary';
import { useToast } from '../../components/feedback/Toast';
import { CheckboxField } from '../../components/forms/Fields';
import { Tabs } from '../../components/navigation/Tabs';
import { Pagination } from '../../components/tables/Pagination';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card, PageHeader } from '../../components/ui/Card';
import { LoadingSkeleton } from '../../components/feedback/LoadingSkeleton';
import { useUrlListState } from '../../hooks/useUrlListState';
import { formatDateTime } from '../../utils/format';
import { notificationRoute } from '../../utils/notificationRoutes';

function Inbox() {
  const url = useUrlListState();
  const unreadOnly = url.get('filter') === 'unread';
  const query = useNotifications(unreadOnly, url.page, url.pageSize);
  const { markRead, markAllRead } = useNotificationMutations();
  const navigate = useNavigate();
  const toast = useToast();
  const [error, setError] = useState<string | null>(null);

  const open = async (notification: AppNotification) => {
    setError(null);
    try { if (!notification.readAtUtc) await markRead.mutateAsync(notification.id); } catch (cause) { setError(toUserMessage(cause)); return; }
    void navigate(notificationRoute(notification));
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2">
        <Button aria-pressed={!unreadOnly} variant={unreadOnly ? 'secondary' : 'primary'} onClick={() => url.update({ filter: null })}>All</Button>
        <Button aria-pressed={unreadOnly} variant={unreadOnly ? 'primary' : 'secondary'} onClick={() => url.update({ filter: 'unread' })}>Unread</Button>
        <Button className="ml-auto" loading={markAllRead.isPending} onClick={async () => { setError(null); try { await markAllRead.mutateAsync(); toast.success('All notifications marked as read.'); } catch (cause) { setError(toUserMessage(cause)); } }}>Mark all as read</Button>
      </div>
      {error && <Alert tone="error" onClose={() => setError(null)}>{error}</Alert>}
      {query.isError ? <ErrorState error={query.error} onRetry={() => void query.refetch()} /> : query.isPending ? <LoadingSkeleton rows={5} label="Loading notifications" /> : query.data.items.length === 0 ? (
        <EmptyState title={unreadOnly ? 'No unread notifications' : 'No notifications yet'} description="You are all caught up." />
      ) : (
        <>
          <ul className="divide-y divide-slate-100 rounded-xl border border-slate-200 bg-white">
            {query.data.items.map((notification) => (
              <li key={notification.id}>
                <button type="button" onClick={() => void open(notification)} className="flex w-full items-start justify-between gap-3 px-4 py-3 text-left hover:bg-slate-50 focus-visible:outline-2 focus-visible:outline-teal-700">
                  <span>
                    <span className="flex items-center gap-2 font-medium">{!notification.readAtUtc && <Badge tone="info">Unread</Badge>}{notification.subject}</span>
                    <span className="block text-sm text-slate-600">{notification.body}</span>
                  </span>
                  <span className="shrink-0 text-xs text-slate-500">{formatDateTime(notification.createdAtUtc)}</span>
                </button>
              </li>
            ))}
          </ul>
          <Pagination page={query.data.page} pageSize={query.data.pageSize} hasNextPage={query.data.hasNextPage} itemCount={query.data.items.length} onPageChange={url.setPage} onPageSizeChange={url.setPageSize} disabled={query.isFetching} />
        </>
      )}
    </div>
  );
}

function PreferencesForm({ initial }: { initial: NotificationPreferences }) {
  const { updatePreferences } = useNotificationMutations();
  const toast = useToast();
  const [values, setValues] = useState(initial);
  const [error, setError] = useState<string | null>(null);
  const set = (key: keyof NotificationPreferences) => (event: React.ChangeEvent<HTMLInputElement>) => setValues((previous) => ({ ...previous, [key]: event.target.checked }));
  const dirty = JSON.stringify(values) !== JSON.stringify(initial);

  const save = async () => {
    setError(null);
    try { await updatePreferences.mutateAsync(values); toast.success('Notification preferences saved.'); } catch (cause) { setError(toUserMessage(cause)); }
  };

  return (
    <Card title="Notification preferences">
      <form className="max-w-md space-y-4" onSubmit={(event) => { event.preventDefault(); void save(); }}>
        {error && <Alert tone="error">{error}</Alert>}
        <CheckboxField label="Email notifications" checked={values.emailEnabled} onChange={set('emailEnabled')} />
        <CheckboxField label="In-app notifications" checked={values.inAppEnabled} onChange={set('inAppEnabled')} />
        <CheckboxField label="Workflow notifications" help="Approvals, workflow progress and completion." checked={values.workflowEnabled} onChange={set('workflowEnabled')} />
        <CheckboxField label="Signature notifications" help="Signature requests, reminders and completion." checked={values.signatureEnabled} onChange={set('signatureEnabled')} />
        <CheckboxField label="Audit notifications" checked={values.auditEnabled} onChange={set('auditEnabled')} />
        <Button type="submit" variant="primary" loading={updatePreferences.isPending} disabled={!dirty}>Save preferences</Button>
      </form>
    </Card>
  );
}

function Preferences() {
  const query = useNotificationPreferences();
  return <QueryBoundary query={query} loadingLabel="Loading preferences" loadingRows={4}>{(preferences) => <PreferencesForm key={JSON.stringify(preferences)} initial={preferences} />}</QueryBoundary>;
}

export function NotificationsPage() {
  const [params, setParams] = useSearchParams();
  const tab = params.get('tab') ?? 'inbox';
  return (
    <>
      <PageHeader eyebrow="Notifications" title="Notification center" description="Updates about approvals, signatures and documents." />
      <Tabs label="Notification sections" value={tab} onChange={(id) => setParams({ tab: id })} tabs={[
        { id: 'inbox', label: 'Inbox', render: () => <Inbox /> },
        { id: 'preferences', label: 'Preferences', render: () => <Preferences /> }
      ]} />
    </>
  );
}
