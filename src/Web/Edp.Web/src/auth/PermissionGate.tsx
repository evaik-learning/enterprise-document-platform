import type { PropsWithChildren, ReactNode } from 'react';
import type { PermissionRequirement } from './permissions';
import { useAuth } from './useAuth';

interface PermissionGateProps extends PropsWithChildren {
  permission: PermissionRequirement;
  /** `any` shows the content when at least one listed permission is granted. */
  mode?: 'all' | 'any';
  fallback?: ReactNode;
}

/** Hides UI the user cannot use. This is a usability aid, not a security boundary. */
export function PermissionGate({ permission, mode = 'all', fallback = null, children }: PermissionGateProps) {
  const { can } = useAuth();
  return <>{can(permission, mode) ? children : fallback}</>;
}
