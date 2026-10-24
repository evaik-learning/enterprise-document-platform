import { Navigate, Outlet, useLocation } from 'react-router';
import { FullPageLoading } from '../components/feedback/LoadingSkeleton';
import { ErrorState } from '../components/feedback/Feedback';
import { Button } from '../components/ui/Button';
import type { PermissionRequirement } from './permissions';
import { useAuth } from './useAuth';

interface ProtectedRouteProps {
  permission?: PermissionRequirement;
  mode?: 'all' | 'any';
  /** The organization selector itself must be reachable before a tenant is chosen. */
  requireOrganization?: boolean;
}

export function ForbiddenPage() {
  return (
    <div role="alert" className="mx-auto max-w-lg rounded-xl border border-slate-200 bg-white p-8 text-center">
      <h1 className="text-xl font-semibold">Access denied</h1>
      <p className="mt-2 text-sm text-slate-600">You do not have permission to view this page. Contact your organization administrator if you need access.</p>
    </div>
  );
}

/** Route guard: session -> tenant -> permission. Redirects keep the intended route as `returnUrl`. */
export function ProtectedRoute({ permission, mode, requireOrganization = true }: ProtectedRouteProps) {
  const { status, me, can, error, refresh } = useAuth();
  const location = useLocation();
  const target = encodeURIComponent(`${location.pathname}${location.search}`);

  if (status === 'loading') return <FullPageLoading />;
  if (status === 'error') {
    return (
      <div className="mx-auto mt-24 max-w-lg space-y-4 px-4">
        <ErrorState error={error} title="The session could not be loaded" />
        <Button variant="primary" onClick={() => void refresh()}>Retry</Button>
      </div>
    );
  }
  if (status === 'anonymous') return <Navigate to={`/login?returnUrl=${target}`} replace />;
  if (requireOrganization && me?.requiresOrganizationSelection) return <Navigate to={`/select-organization?returnUrl=${target}`} replace />;
  if (permission && !can(permission, mode)) return <ForbiddenPage />;
  return <Outlet />;
}
