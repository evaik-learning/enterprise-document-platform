import type { PropsWithChildren } from 'react';
import { AuthenticationProvider } from '../features/authentication/AuthenticationProvider';

export function AppProviders({ children }: PropsWithChildren) {
  return <AuthenticationProvider>{children}</AuthenticationProvider>;
}
