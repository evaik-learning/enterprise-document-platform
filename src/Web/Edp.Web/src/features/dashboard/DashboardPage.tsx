import { Link } from 'react-router';
import { useDashboard } from '../../api/hooks/usePlatformQueries';
import { useAuth } from '../../auth/useAuth';
import { Alert, EmptyState } from '../../components/feedback/Feedback';
import { QueryBoundary } from '../../components/feedback/QueryBoundary';
import { StatusBadge } from '../../components/ui/Badge';
import { Card, PageHeader } from '../../components/ui/Card';
import { formatCount, formatDateTime, formatTime } from '../../utils/format';
import { humanize } from '../../utils/status';
import { notificationRoute } from '../../utils/notificationRoutes';
import { Permissions } from '../../auth/permissions';
import type { DashboardData } from '../../api/contracts';

function StatCard({ label, value, to, hint }: { label: string; value: number | null | undefined; to: string; hint?: string }) {
  return (
    <Link to={to} className="block rounded-xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-teal-700 hover:shadow-md focus-visible:outline-2 focus-visible:outline-teal-700">
      <p className="text-sm font-medium text-slate-600">{label}</p>
      <p className="mt-3 text-3xl font-semibold tracking-tight text-slate-900">{formatCount(value)}</p>
      {hint && <p className="mt-1 text-xs text-slate-500">{hint}</p>}
    </Link>
  );
}

const SECTION_LABELS: Record<string, string> = {
  templates: 'templates', documents: 'documents', completedDocuments: 'completed documents', approvals: 'approvals', signatures: 'signatures',
  notifications: 'notifications', recentDocuments: 'recent documents', recentNotifications: 'recent notifications', activity: 'recent activity'
};

function DashboardContent({ data }: { data: DashboardData }) {
  const { can } = useAuth();
  const { counts } = data;
  return (
    <div className="space-y-8">
      {data.unavailableSections.length > 0 && (
        <Alert tone="warning" title="Some information is unavailable">
          Could not load: {[...new Set(data.unavailableSections.map((section) => SECTION_LABELS[section] ?? section))].join(', ')}. Other sections are up to date.
        </Alert>
      )}

      <section aria-label="Summary" className="grid grid-cols-2 gap-4 lg:grid-cols-3 xl:grid-cols-6">
        <StatCard label="Templates" value={counts.templates} to="/templates" hint="Active" />
        <StatCard label="Documents" value={counts.documents} to="/documents" />
        <StatCard label="Pending tasks" value={counts.pendingApprovals} to="/approvals" />
        <StatCard label="Awaiting signature" value={counts.awaitingSignature} to="/signatures" />
        <StatCard label="Completed" value={counts.completedDocuments} to="/documents?status=Completed" />
        <StatCard label="Unread notifications" value={counts.unreadNotifications} to="/notifications" />
      </section>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card title="Recent documents" actions={<Link className="text-sm font-medium text-teal-800 underline" to="/documents">View all</Link>}>
          {!data.recentDocuments || data.recentDocuments.length === 0 ? (
            <EmptyState title="No documents yet" description={can(Permissions.DocumentGenerate) ? 'Generate your first document from an active template.' : undefined} action={can(Permissions.DocumentGenerate) ? <Link className="font-medium text-teal-800 underline" to="/documents/new">Generate document</Link> : undefined} />
          ) : (
            <ul className="divide-y divide-slate-100">
              {data.recentDocuments.map((document) => (
                <li key={document.id} className="flex items-center justify-between gap-3 py-2.5">
                  <div className="min-w-0">
                    <Link to={`/documents/${document.id}`} className="block truncate font-medium text-teal-900 hover:underline">{document.name}</Link>
                    <p className="text-xs text-slate-500">{formatDateTime(document.createdAt)}</p>
                  </div>
                  <StatusBadge status={document.status} />
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card title="Pending approvals" actions={<Link className="text-sm font-medium text-teal-800 underline" to="/approvals">Open inbox</Link>}>
          {!data.pendingApprovals || data.pendingApprovals.length === 0 ? (
            <EmptyState title="No pending approvals" description="You are all caught up." />
          ) : (
            <ul className="divide-y divide-slate-100">
              {data.pendingApprovals.map((task) => (
                <li key={task.id} className="flex items-center justify-between gap-3 py-2.5 text-sm">
                  <Link to={`/approvals/${task.id}`} className="font-medium text-teal-900 hover:underline">Approval task {task.id.slice(0, 8)}</Link>
                  <span className="text-xs text-slate-500">{task.deadlineAt ? `Due ${formatDateTime(task.deadlineAt)}` : 'No deadline'}</span>
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card title="Pending signatures" actions={<Link className="text-sm font-medium text-teal-800 underline" to="/signatures">View all</Link>}>
          {!data.pendingSignatures || data.pendingSignatures.length === 0 ? (
            <EmptyState title="Nothing awaiting signature" />
          ) : (
            <ul className="divide-y divide-slate-100">
              {data.pendingSignatures.map((request) => (
                <li key={request.signingRequestId} className="flex items-center justify-between gap-3 py-2.5 text-sm">
                  <div className="min-w-0">
                    <Link to={`/signatures/${request.signingRequestId}`} className="block truncate font-medium text-teal-900 hover:underline">{request.title}</Link>
                    <p className="text-xs text-slate-500">{request.signedCount} of {request.signerCount} signed</p>
                  </div>
                  <StatusBadge status={request.status} />
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card title="Recent activity" actions={can(Permissions.AuditRead) ? <Link className="text-sm font-medium text-teal-800 underline" to="/audit">Audit log</Link> : undefined}>
          {!data.recentActivity || data.recentActivity.length === 0 ? (
            <EmptyState title="No recent activity" />
          ) : (
            <ul className="space-y-2 text-sm">
              {data.recentActivity.map((entry) => (
                <li key={entry.id} className="flex gap-3"><span className="w-12 shrink-0 text-xs text-slate-500">{formatTime(entry.timestamp)}</span><span>{humanize(entry.action.replace(/DomainEvent$|Event$/, ''))}</span></li>
              ))}
            </ul>
          )}
        </Card>

        <Card title="Recent notifications" className="lg:col-span-2" actions={<Link className="text-sm font-medium text-teal-800 underline" to="/notifications">View all</Link>}>
          {!data.recentNotifications || data.recentNotifications.length === 0 ? (
            <EmptyState title="No notifications yet" />
          ) : (
            <ul className="divide-y divide-slate-100">
              {data.recentNotifications.map((notification) => (
                <li key={notification.id} className="py-2.5 text-sm">
                  <Link to={notificationRoute(notification)} className="font-medium text-teal-900 hover:underline">{!notification.readAtUtc && <span className="sr-only">Unread: </span>}{notification.subject}</Link>
                  <p className="text-slate-600">{notification.body}</p>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>
    </div>
  );
}

export function DashboardPage() {
  const { me } = useAuth();
  const query = useDashboard();
  return (
    <>
      <PageHeader eyebrow={me?.organizationName ?? 'Workspace'} title={me?.name ? `Welcome back, ${me.name.split(' ')[0]}` : 'Dashboard'} description="Your documents, approvals, signatures and activity at a glance." />
      <QueryBoundary query={query} loadingRows={6} loadingLabel="Loading dashboard">{(data) => <DashboardContent data={data} />}</QueryBoundary>
    </>
  );
}
