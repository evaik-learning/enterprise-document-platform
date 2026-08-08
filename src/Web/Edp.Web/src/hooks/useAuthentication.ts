import { useContext } from 'react';
import { AuthenticationContext } from '../features/authentication/AuthenticationProvider';

export function useAuthentication() {
  const context = useContext(AuthenticationContext);

  if (!context) {
    throw new Error('useAuthentication must be used within AuthenticationProvider.');
  }

  return context;
}
