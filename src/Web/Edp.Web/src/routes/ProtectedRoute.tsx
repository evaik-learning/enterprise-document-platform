import type { PropsWithChildren } from 'react';
import { LoadingState } from '../components/LoadingState';
import { LoginPage } from '../features/authentication/LoginPage';
import { useAuthentication } from '../hooks/useAuthentication';

export function ProtectedRoute({ children }: PropsWithChildren) {
  const { user, isLoading } = useAuthentication();

  if (isLoading) {
    return <LoadingState />;
  }

  if (!user?.isAuthenticated) {
    return <LoginPage />;
  }

  return children;
}
