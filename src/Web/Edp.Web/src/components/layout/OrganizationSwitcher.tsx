import { useState } from 'react';
import { useNavigate } from 'react-router';
import { useMyOrganizations } from '../../api/hooks/usePlatformQueries';
import { isFeatureEnabled } from '../../app/featureFlags';
import { toUserMessage } from '../../api/errors';
import { useAuth } from '../../auth/useAuth';
import { Popover } from '../navigation/Popover';
import { useToast } from '../feedback/Toast';

/** Tenant switcher. Switching clears every tenant-scoped cache entry and reloads the dashboard. */
export function OrganizationSwitcher() {
  const { me, organizationId, selectOrganization } = useAuth();
  const [open, setOpen] = useState(false);
  const memberships = useMyOrganizations();
  const navigate = useNavigate();
  const toast = useToast();
  const name = me?.organizationName ?? memberships.data?.find((organization) => organization.id === organizationId)?.name ?? 'Organization';

  if (!isFeatureEnabled('multi.organization')) return <span className="hidden text-sm font-medium text-slate-700 md:inline">{name}</span>;

  const switchTo = async (id: string, close: () => void) => {
    close();
    if (id === organizationId) return;
    try {
      await selectOrganization(id);
      void navigate('/');
      toast.success('Organization changed.');
    } catch (cause) {
      toast.error(toUserMessage(cause, 'The organization could not be changed.'));
    }
  };

  return (
    <Popover label={`Organization: ${name}. Change organization`} align="left" onOpenChange={setOpen} trigger={<span className="max-w-40 truncate">{name} <span aria-hidden="true">▾</span></span>}>
      {(close) => (
        <div>
          <h2 className="px-2 pb-1 text-xs font-semibold uppercase tracking-wide text-slate-500">Organizations</h2>
          {open && memberships.isPending && <p className="p-2 text-sm text-slate-600">Loading…</p>}
          {memberships.isError && <p className="p-2 text-sm text-red-800">Organizations could not be loaded.</p>}
          <ul>
            {memberships.data?.filter((organization) => organization.isActive).map((organization) => (
              <li key={organization.id}>
                <button type="button" aria-current={organization.id === organizationId ? 'true' : undefined} className="flex w-full items-center justify-between gap-2 rounded-lg px-2 py-2 text-left text-sm hover:bg-slate-100" onClick={() => void switchTo(organization.id, close)}>
                  <span>{organization.name}<span className="block text-xs text-slate-600">{organization.role}</span></span>
                  {organization.id === organizationId && <span aria-hidden="true">✓</span>}
                  {organization.id === organizationId && <span className="sr-only">Current organization</span>}
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}
    </Popover>
  );
}
