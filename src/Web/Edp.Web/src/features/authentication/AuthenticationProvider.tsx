import { createContext, useCallback, useEffect, useMemo, useState, type PropsWithChildren } from 'react';
import type { CurrentUser } from '../../models/authentication';
import { getCurrentUser, login, logout } from './authenticationService';

interface AuthenticationContextValue {
  user: CurrentUser | null;
  isLoading: boolean;
  refresh: () => Promise<void>;
  login: () => void;
  logout: () => void;
}

export const AuthenticationContext = createContext<AuthenticationContextValue | null>(null);

export function AuthenticationProvider({ children }: PropsWithChildren) {
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const refresh = useCallback(async () => {
    setIsLoading(true);
    try {
      setUser(await getCurrentUser());
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const value = useMemo(
    () => ({
      user,
      isLoading,
      refresh,
      login: () => login(),
      logout: () => logout()
    }),
    [isLoading, refresh, user]
  );

  return <AuthenticationContext.Provider value={value}>{children}</AuthenticationContext.Provider>;
}
