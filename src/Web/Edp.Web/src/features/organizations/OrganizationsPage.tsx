import { useEffect, useState } from 'react';
import { useAuthentication } from '../../hooks/useAuthentication';
import { ApiError } from '../../services/apiClient';
import { platformApi, type Organization, type OrganizationCapabilities, type OrganizationMember } from '../../services/platformApi';

const roles = ['Administrator', 'Member', 'Auditor'];

export function OrganizationsPage() {
  const { user, refresh } = useAuthentication();
  const activeOrganizationId = user?.claims.find(claim => claim.type === 'organization_id')?.value;
  const [organizations, setOrganizations] = useState<Organization[]>([]);
  const [members, setMembers] = useState<OrganizationMember[]>([]);
  const [capabilities, setCapabilities] = useState<OrganizationCapabilities | null>(null);
  const [newUserId, setNewUserId] = useState('');
  const [newRole, setNewRole] = useState('Member');
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState('');

  const loadAccess = async (organizationId: string) => {
    try {
      const [memberResult, capabilityResult] = await Promise.all([
        platformApi.organizations.members(organizationId),
        platformApi.organizations.capabilities(organizationId)
      ]);
      setMembers(memberResult);
      setCapabilities(capabilityResult);
    } catch (exception) {
      setError(exception instanceof ApiError ? exception.message : 'Organization access could not be loaded.');
    }
  };

  useEffect(() => {
    platformApi.organizations.mine()
      .then(result => {
        setOrganizations(result);
        if (activeOrganizationId) void loadAccess(activeOrganizationId);
      })
      .catch(exception => setError(exception instanceof ApiError ? exception.message : 'Organizations could not be loaded.'));
  }, [activeOrganizationId]);

  const select = async (id: string) => {
    setBusy(id); setError(''); setMessage('');
    try { await platformApi.organizations.select(id); await refresh(); window.location.reload(); }
    catch (exception) { setError(exception instanceof ApiError ? exception.message : 'Organization could not be selected.'); }
    finally { setBusy(''); }
  };

  const addMember = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!activeOrganizationId || !newUserId.trim()) return;
    setBusy('add'); setError(''); setMessage('');
    try {
      await platformApi.organizations.addMember(activeOrganizationId, newUserId.trim(), newRole);
      setNewUserId(''); setMessage('Member added.'); await loadAccess(activeOrganizationId);
    } catch (exception) { setError(exception instanceof ApiError ? exception.message : 'Member could not be added.'); }
    finally { setBusy(''); }
  };

  const updateRole = async (member: OrganizationMember, role: string) => {
    if (!activeOrganizationId || member.role === role) return;
    if (!window.confirm(`Change this member's role to ${role}?`)) return;
    setBusy(member.userId); setError(''); setMessage('');
    try { await platformApi.organizations.updateMemberRole(activeOrganizationId, member.userId, role); setMessage('Role updated.'); await loadAccess(activeOrganizationId); }
    catch (exception) { setError(exception instanceof ApiError ? exception.message : 'Role could not be updated.'); }
    finally { setBusy(''); }
  };

  const removeMember = async (member: OrganizationMember) => {
    if (!activeOrganizationId || !window.confirm('Deactivate this organization membership? The user will lose access to this organization.')) return;
    setBusy(member.userId); setError(''); setMessage('');
    try { await platformApi.organizations.removeMember(activeOrganizationId, member.userId); setMessage('Membership deactivated.'); await loadAccess(activeOrganizationId); }
    catch (exception) { setError(exception instanceof ApiError ? exception.message : 'Membership could not be deactivated.'); }
    finally { setBusy(''); }
  };

  const canManageMembers = capabilities?.permissions.includes('Members.Manage') ?? false;
  return <section className="space-y-6">
    <div><p className="text-sm font-medium text-teal-700">Organization settings</p><h2 className="text-3xl font-semibold">Organizations</h2><p className="mt-1 text-sm text-slate-600">Manage organization membership and review effective capabilities. Server authorization remains authoritative.</p></div>
    {error && <p role="alert" className="rounded-lg bg-red-50 p-4 text-sm text-red-700">{error}</p>}
    {message && <p role="status" className="rounded-lg bg-teal-50 p-4 text-sm text-teal-800">{message}</p>}
    <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)]">
      <div className="space-y-6">
        <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm"><h3 className="font-semibold">Organization context</h3><div className="mt-3 space-y-2">{organizations.length === 0 ? <p className="text-sm text-slate-500">No memberships returned.</p> : organizations.map(item => <div key={item.id} className={`rounded-lg border p-3 text-sm ${item.id === activeOrganizationId ? 'border-teal-300 bg-teal-50' : 'border-slate-200'}`}><div className="flex items-start justify-between gap-3"><div><p className="font-medium">{item.name}</p><p className="mt-1 text-xs text-slate-500">Role: {item.role} · {item.isActive ? 'Active' : 'Inactive'}</p></div>{item.id !== activeOrganizationId && item.isActive && <button disabled={busy === item.id} className="rounded border border-slate-300 px-3 py-1.5 text-xs font-semibold" onClick={() => void select(item.id)}>{busy === item.id ? 'Selecting...' : 'Select'}</button>}</div></div>)}</div></div>
        <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm"><h3 className="font-semibold">Effective permissions</h3>{capabilities ? <><p className="mt-2 text-sm text-slate-600">Role: <strong>{capabilities.role}</strong></p><ul className="mt-3 grid gap-2 text-sm sm:grid-cols-2">{capabilities.permissions.map(permission => <li className="rounded bg-slate-50 px-3 py-2" key={permission}>{permission}</li>)}</ul></> : <p className="mt-2 text-sm text-slate-500">Select an active organization to view capabilities.</p>}</div>
      </div>
      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm"><div className="flex flex-wrap items-start justify-between gap-3"><div><h3 className="font-semibold">Members</h3><p className="mt-1 text-sm text-slate-600">Membership changes are server-validated and recorded against the active organization.</p></div>{!canManageMembers && <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">Administrator access required</span>}</div>{canManageMembers && <form className="mt-5 grid gap-3 rounded-lg border border-slate-200 bg-slate-50 p-4 sm:grid-cols-[1fr_160px_auto]" onSubmit={addMember}><label className="text-xs font-medium text-slate-600">User ID<input required value={newUserId} onChange={event => setNewUserId(event.target.value)} placeholder="Entra user object ID" className="mt-1 w-full rounded border border-slate-300 bg-white px-3 py-2 text-sm" /></label><label className="text-xs font-medium text-slate-600">Role<select value={newRole} onChange={event => setNewRole(event.target.value)} className="mt-1 w-full rounded border border-slate-300 bg-white px-3 py-2 text-sm">{roles.map(role => <option key={role}>{role}</option>)}</select></label><button disabled={busy === 'add'} className="self-end rounded bg-slate-900 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50">{busy === 'add' ? 'Adding...' : 'Add member'}</button></form>}<div className="mt-5 space-y-2">{members.length === 0 ? <p className="text-sm text-slate-500">No manageable members are available.</p> : members.map(member => <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-slate-200 p-3 text-sm" key={member.userId}><div><p className="font-mono text-xs text-slate-700">{member.userId}</p><p className="mt-1 text-xs text-slate-500">{member.isActive ? 'Active membership' : 'Inactive membership'}</p></div><div className="flex items-center gap-2"><select aria-label={`Role for ${member.userId}`} disabled={busy === member.userId || !member.isActive} value={member.role} onChange={event => void updateRole(member, event.target.value)} className="rounded border border-slate-300 px-2 py-1.5 text-sm">{['Owner', ...roles].map(role => <option key={role}>{role}</option>)}</select><button disabled={busy === member.userId || !member.isActive} className="rounded border border-red-300 px-3 py-1.5 text-xs font-semibold text-red-700 disabled:opacity-50" onClick={() => void removeMember(member)}>Deactivate</button></div></div>)}</div></div>
    </div>
  </section>;
}
