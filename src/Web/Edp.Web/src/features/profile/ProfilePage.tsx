import { Link } from 'react-router';
import { useAuth } from '../../auth/useAuth';
import { Badge } from '../../components/ui/Badge';
import { Card, PageHeader } from '../../components/ui/Card';

export function ProfilePage() {
  const { me, permissions } = useAuth();
  return (
    <>
      <PageHeader eyebrow="Account" title="Profile" description="Your identity comes from your organization’s sign-in provider." />
      <div className="grid gap-6 lg:grid-cols-2">
        <Card title="Identity">
          <dl className="space-y-2 text-sm">
            <div className="flex justify-between gap-4"><dt className="text-slate-600">Name</dt><dd className="font-medium">{me?.name ?? '—'}</dd></div>
            <div className="flex justify-between gap-4"><dt className="text-slate-600">Email</dt><dd className="font-medium">{me?.email ?? '—'}</dd></div>
            <div className="flex justify-between gap-4"><dt className="text-slate-600">Organization</dt><dd className="font-medium">{me?.organizationName ?? '—'}</dd></div>
            <div className="flex justify-between gap-4"><dt className="text-slate-600">Roles</dt><dd className="font-medium">{me?.roles.join(', ') || '—'}</dd></div>
          </dl>
        </Card>
        <Card title="Permissions">
          {permissions.length === 0 ? <p className="text-sm text-slate-600">No permissions.</p> : <ul className="flex flex-wrap gap-2">{permissions.map((permission) => <li key={permission}><Badge>{permission}</Badge></li>)}</ul>}
          <p className="mt-4 text-sm"><Link className="font-medium text-teal-900 underline" to="/notifications?tab=preferences">Notification preferences</Link></p>
        </Card>
      </div>
    </>
  );
}
