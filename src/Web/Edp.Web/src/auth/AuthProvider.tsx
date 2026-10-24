import { createContext, useCallback, useEffect, useMemo, useState, type PropsWithChildren } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { setUnauthorizedHandler } from '../api/httpClient';
import { ApiError } from '../api/errors';
import { organizationApi, sessionApi } from '../api/endpoints';
import type { Me } from '../api/contracts';
import { hasPermission, type PermissionRequirement } from './permissions';
import { redirectToLogout } from './authService';

export type AuthStatus = 'loading' | 'anonymous' | 'authenticated' | 'error';

export interface AuthContextValue {
  status: AuthStatus;
  me: Me | null;
  organizationId: string | null;
  permissions: readonly string[];
  error: ApiError | null;
  can: (required: PermissionRequirement, mode?: 'all' | 'any') => boolean;
  refresh: () => Promise<void>;
  /** Switches tenant: updates the session, drops every tenant-scoped cache entry and reloads context. */
  selectOrganization: (organizationId: string) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: PropsWithChildren) {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<AuthStatus>('loading');
  const [me, setMe] = useState<Me | null>(null);
  const [error, setError] = useState<ApiError | null>(null);

  const refresh = useCallback(async () => {
    try {
      const session = await sessionApi.session();
      if (!session.isAuthenticated) {
        setMe(null);
        setStatus('anonymous');
        return;
      }
      setMe(await sessionApi.me());
      setError(null);
      setStatus('authenticated');
    } catch (cause) {
      if (cause instanceof ApiError && cause.isUnauthorized) {
        setMe(null);
        setStatus('anonymous');
        return;
      }
      setError(cause instanceof ApiError ? cause : new ApiError({ status: 0, message: 'The session could not be loaded.' }));
      setStatus('error');
    }
  }, []);

  useEffect(() => {
    // oxlint-disable-next-line react/set-state-in-effect -- initial session load is a sync-with-external-system effect
    void refresh();
  }, [refresh]);

  // A 401 that could not be renewed ends the session: no tenant data may stay in memory.
  useEffect(() => {
    setUnauthorizedHandler(() => {
      queryClient.clear();
      setMe(null);
      setStatus('anonymous');
    });
    return () => setUnauthorizedHandler(undefined);
  }, [queryClient]);

  const selectOrganization = useCallback(
    async (organizationId: string) => {
      await organizationApi.select(organizationId);
      queryClient.clear();
      await refresh();
    },
    [queryClient, refresh]
  );

  const logout = useCallback(() => {
    queryClient.clear();
    redirectToLogout();
  }, [queryClient]);

  const value = useMemo<AuthContextValue>(() => {
    const permissions = me?.permissions ?? [];
    return {
      status,
      me,
      organizationId: me?.organizationId ?? null,
      permissions,
      error,
      can: (required, mode) => hasPermission(permissions, required, mode),
      refresh,
      selectOrganization,
      logout
    };
  }, [error, logout, me, refresh, selectOrganization, status]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
