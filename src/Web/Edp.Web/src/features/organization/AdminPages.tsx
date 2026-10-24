import { useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import type { OrganizationMember } from '../../api/contracts';
import { toUserMessage } from '../../api/errors';
import { useMemberMutations, useMyOrganizations, useOrganizationCapabilities, useOrganizationMembers } from '../../api/hooks/usePlatformQueries';
import { appConfig } from '../../app/config';
import { featureFlags } from '../../app/featureFlags';
import { useAuth } from '../../auth/useAuth';
import { ConfirmDialog } from '../../components/dialogs/ConfirmDialog';
import { Alert, EmptyState, ErrorState } from '../../components/feedback/Feedback';
import { QueryBoundary } from '../../components/feedback/QueryBoundary';
import { useToast } from '../../components/feedback/Toast';
import { SelectField, TextField } from '../../components/forms/Fields';
import { DataTable, type Column } from '../../components/tables/DataTable';
import { Badge, StatusBadge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card, PageHeader } from '../../components/ui/Card';

const ROLES = ['Owner', 'Administrator', 'Member', 'Auditor'];
const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

const ROLE_DESCRIPTIONS: Record<string, string> = {
  Owner: 'Full control, including organization settings and membership. An organization always keeps at least one owner.',
  Administrator: 'Manages members, templates, documents, workflows, signatures and audit.',
  Member: 'Reads templates and workflows, generates and reads documents, receives notifications.',
  Auditor: 'Read-only access to templates, documents, workflows, signatures and the audit log.'
};

export function OrganizationAdminPage() {
  const { me, organizationId, selectOrganization } = useAuth();
  const memberships = useMyOrganizations();
  const toast = useToast();
  const [busy, setBusy] = useState<string | null>(null);

  return (
    <>
      <PageHeader eyebrow="Administration" title="Organization" description="Your organizations and the one currently in use." />
      <Card title={me?.organizationName ?? 'Current organization'}>
        <dl className="grid gap-3 text-sm sm:grid-cols-2">
          <div><dt className="text-slate-600">Organization ID</dt><dd><code>{organizationId}</code></dd></div>
          <div><dt className="text-slate-600">Your role</dt><dd className="font-medium">{me?.roles.join(', ') || '—'}</dd></div>
        </dl>
      </Card>
      <div className="mt-6">
        <QueryBoundary query={memberships} loadingLabel="Loading organizations">
          {(list) => (
            <Card title="Your organizations">
              <ul className="divide-y divide-slate-100">
                {list.map((organization) => (
                  <li key={organization.id} className="flex flex-wrap items-center justify-between gap-3 py-3 text-sm">
                    <span><span className="font-medium">{organization.name}</span><span className="block text-xs text-slate-600">{organization.role}{organization.description ? ` · ${organization.description}` : ''}</span></span>
                    <span className="flex items-center gap-2">
                      {!organization.isActive && <StatusBadge status="inactive" />}
                      {organization.id === organizationId ? <Badge tone="success">Current</Badge> : organization.isActive && (
                        <Button size="sm" loading={busy === organization.id} disabled={busy !== null} onClick={async () => { setBusy(organization.id); try { await selectOrganization(organization.id); toast.success('Organization changed.'); } catch (cause) { toast.error(toUserMessage(cause)); } finally { setBusy(null); } }}>Switch</Button>
                      )}
                    </span>
                  </li>
                ))}
              </ul>
            </Card>
          )}
        </QueryBoundary>
      </div>
    </>
  );
}

export function UsersAdminPage() {
  const { organizationId, me } = useAuth();
  const members = useOrganizationMembers(organizationId);
  const mutations = useMemberMutations();
  const toast = useToast();
  const [userId, setUserId] = useState('');
  const [role, setRole] = useState('Member');
  const [formError, setFormError] = useState<string | null>(null);
  const [removing, setRemoving] = useState<OrganizationMember | null>(null);

  const add = async (event: FormEvent) => {
    event.preventDefault();
    if (!organizationId) return;
    if (!GUID.test(userId.trim())) { setFormError('Enter the user’s ID as a GUID.'); return; }
    setFormError(null);
    try { await mutations.add.mutateAsync({ organizationId, userId: userId.trim(), role }); setUserId(''); toast.success('Member added.'); } catch (cause) { setFormError(toUserMessage(cause)); }
  };

  const columns: Column<OrganizationMember>[] = [
    { key: 'user', header: 'User', cell: (m) => <span><code className="text-xs">{m.userId}</code>{m.userId === me?.userId && <Badge>You</Badge>}</span> },
    {
      key: 'role', header: 'Role',
      cell: (m) => (
        <SelectField label={`Role for ${m.userId}`} hideLabel value={m.role} disabled={mutations.updateRole.isPending || !m.isActive}
          onChange={async (event) => { if (!organizationId) return; try { await mutations.updateRole.mutateAsync({ organizationId, userId: m.userId, role: event.target.value }); toast.success('Role updated.'); } catch (cause) { toast.error(toUserMessage(cause)); } }}>
          {ROLES.map((value) => <option key={value} value={value}>{value}</option>)}
        </SelectField>
      )
    },
    { key: 'status', header: 'Status', cell: (m) => <StatusBadge status={m.isActive ? 'active' : 'inactive'} /> },
    { key: 'actions', header: 'Actions', srOnlyHeader: true, className: 'text-right', cell: (m) => m.isActive && m.userId !== me?.userId ? <Button size="sm" variant="ghost" aria-label={`Remove ${m.userId}`} onClick={() => setRemoving(m)}>Remove</Button> : null }
  ];

  return (
    <>
      <PageHeader eyebrow="Administration" title="Users" description="Members of the current organization and their roles." />
      <div className="space-y-6">
        <Card title="Add member">
          <form className="grid max-w-2xl gap-3 sm:grid-cols-[1fr_12rem_auto] sm:items-end" onSubmit={(event) => void add(event)} noValidate>
            <TextField label="User ID" value={userId} onChange={(event) => setUserId(event.target.value)} help="The user’s object ID (GUID)." />
            <SelectField label="Role" value={role} onChange={(event) => setRole(event.target.value)}>{ROLES.map((value) => <option key={value} value={value}>{value}</option>)}</SelectField>
            <Button type="submit" variant="primary" loading={mutations.add.isPending}>Add</Button>
          </form>
          {formError && <div className="mt-3"><Alert tone="error">{formError}</Alert></div>}
        </Card>
        {members.isError ? <ErrorState error={members.error} onRetry={() => void members.refetch()} /> : (
          <DataTable caption="Organization members" columns={columns} rows={members.data ?? []} getRowKey={(m) => m.userId} isLoading={members.isPending} empty={<EmptyState title="No members" />} />
        )}
      </div>
      <ConfirmDialog open={removing !== null} title="Remove member?" description="They will immediately lose access to this organization." confirmLabel="Remove" danger onClose={() => setRemoving(null)}
        onConfirm={async () => { if (!removing || !organizationId) return; await mutations.remove.mutateAsync({ organizationId, userId: removing.userId }); toast.success('Member removed.'); }} />
    </>
  );
}

export function RolesAdminPage() {
  const { organizationId, permissions } = useAuth();
  const capabilities = useOrganizationCapabilities(organizationId);
  return (
    <>
      <PageHeader eyebrow="Administration" title="Roles" description="What each role is for, and your effective permissions." />
      <div className="grid gap-6 lg:grid-cols-2">
        <Card title="Roles">
          <dl className="space-y-3 text-sm">{ROLES.map((role) => <div key={role}><dt className="font-semibold">{role}</dt><dd className="text-slate-600">{ROLE_DESCRIPTIONS[role]}</dd></div>)}</dl>
        </Card>
        <Card title="Your effective permissions">
          {capabilities.data && <p className="mb-3 text-sm text-slate-600">Role: <strong>{capabilities.data.role}</strong></p>}
          {permissions.length === 0 ? <p className="text-sm text-slate-600">No permissions.</p> : <ul className="flex flex-wrap gap-2">{permissions.map((permission) => <li key={permission}><Badge>{permission}</Badge></li>)}</ul>}
        </Card>
      </div>
    </>
  );
}

export function SettingsAdminPage() {
  return (
    <>
      <PageHeader eyebrow="Administration" title="Settings" description="Application information and feature availability." />
      <div className="grid gap-6 lg:grid-cols-2">
        <Card title="Application">
          <dl className="grid gap-2 text-sm"><div className="flex justify-between"><dt className="text-slate-600">Name</dt><dd>{appConfig.appName}</dd></div><div className="flex justify-between"><dt className="text-slate-600">Version</dt><dd>{appConfig.appVersion}</dd></div><div className="flex justify-between"><dt className="text-slate-600">Environment</dt><dd>{appConfig.environment}</dd></div></dl>
        </Card>
        <Card title="Features">
          <ul className="space-y-2 text-sm">{Object.entries(featureFlags).map(([flag, on]) => <li key={flag} className="flex items-center justify-between"><code>{flag}</code><StatusBadge status={on ? 'active' : 'inactive'} label={on ? 'Enabled' : 'Disabled'} /></li>)}</ul>
          <p className="mt-3 text-xs text-slate-600">Flags control availability only; permissions are always enforced by the backend.</p>
        </Card>
        <Card title="Notifications"><p className="text-sm text-slate-600">Personal delivery preferences are on the <Link className="font-medium text-teal-900 underline" to="/notifications?tab=preferences">notification preferences</Link> page.</p></Card>
      </div>
    </>
  );
}
