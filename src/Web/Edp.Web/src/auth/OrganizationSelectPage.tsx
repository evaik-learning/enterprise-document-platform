import { useState, type FormEvent } from 'react';
import { Navigate, useNavigate, useSearchParams } from 'react-router';
import { toUserMessage } from '../api/errors';
import { organizationApi } from '../api/endpoints';
import { useMyOrganizations } from '../api/hooks/usePlatformQueries';
import { TextField } from '../components/forms/Fields';
import { Alert, EmptyState, ErrorState } from '../components/feedback/Feedback';
import { LoadingSkeleton } from '../components/feedback/LoadingSkeleton';
import { Button } from '../components/ui/Button';
import { safeReturnUrl } from './authService';
import { useAuth } from './useAuth';

/** Shown after sign-in when the session has no active organization (tenant) yet. */
export function OrganizationSelectPage() {
  const { me, selectOrganization, logout } = useAuth();
  const memberships = useMyOrganizations();
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const [busyId, setBusyId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [newName, setNewName] = useState('');
  const [creating, setCreating] = useState(false);

  if (me && !me.requiresOrganizationSelection) return <Navigate to={safeReturnUrl(params.get('returnUrl'))} replace />;

  const choose = async (id: string) => {
    setBusyId(id);
    setError(null);
    try {
      await selectOrganization(id);
      void navigate(safeReturnUrl(params.get('returnUrl')), { replace: true });
    } catch (cause) {
      setError(toUserMessage(cause, 'The organization could not be selected.'));
    } finally {
      setBusyId(null);
    }
  };

  const create = async (event: FormEvent) => {
    event.preventDefault();
    const name = newName.trim();
    if (!name) return;
    setCreating(true);
    setError(null);
    try {
      const created = await organizationApi.create({ name });
      await selectOrganization(created.id);
      void navigate(safeReturnUrl(params.get('returnUrl')), { replace: true });
    } catch (cause) {
      setError(toUserMessage(cause, 'The organization could not be created.'));
    } finally {
      setCreating(false);
    }
  };

  const active = memberships.data?.filter((organization) => organization.isActive) ?? [];

  return (
    <main className="mx-auto max-w-xl px-4 py-16">
      <h1 className="text-2xl font-semibold">Choose an organization</h1>
      <p className="mt-1 text-sm text-slate-600">Your work, permissions and data are scoped to one organization at a time.</p>
      <div className="mt-6 space-y-3">
        {error && <Alert tone="error">{error}</Alert>}
        {memberships.isPending && <LoadingSkeleton rows={3} label="Loading organizations" />}
        {memberships.isError && <ErrorState error={memberships.error} onRetry={() => void memberships.refetch()} />}
        {memberships.data && active.length === 0 && <EmptyState title="No organizations available" description="You are not an active member of any organization. Ask an administrator to add you." />}
        <ul className="space-y-2">
          {active.map((organization) => (
            <li key={organization.id} className="flex items-center justify-between gap-3 rounded-xl border border-slate-200 bg-white p-4">
              <div>
                <p className="font-semibold">{organization.name}</p>
                <p className="text-sm text-slate-600">{organization.role}{organization.description ? ` · ${organization.description}` : ''}</p>
              </div>
              <Button variant="primary" size="sm" loading={busyId === organization.id} disabled={busyId !== null} onClick={() => void choose(organization.id)}>Continue</Button>
            </li>
          ))}
        </ul>
        <form onSubmit={(event) => void create(event)} className="rounded-xl border border-dashed border-slate-300 bg-white p-4" aria-label="Create organization">
          <h2 className="text-sm font-semibold">Create a new organization</h2>
          <p className="mt-1 text-xs text-slate-600">You become its Owner and can then invite members and assign roles.</p>
          <div className="mt-3 flex items-end gap-3">
            <TextField className="flex-1" label="Organization name" value={newName} onChange={(event) => setNewName(event.target.value)} maxLength={200} required />
            <Button type="submit" variant="secondary" loading={creating} disabled={creating || busyId !== null || !newName.trim()}>Create</Button>
          </div>
        </form>
        <Button variant="ghost" onClick={logout}>Sign out</Button>
      </div>
    </main>
  );
}
