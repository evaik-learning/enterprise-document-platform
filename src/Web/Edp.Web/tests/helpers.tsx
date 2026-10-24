import type { ReactElement, PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import { MemoryRouter, type MemoryRouterProps } from 'react-router';
import { vi } from 'vitest';
import type { Me } from '../src/api/contracts';
import { ToastProvider } from '../src/components/feedback/Toast';
import { AuthContext, type AuthContextValue } from '../src/auth/AuthProvider';
import { hasPermission, type PermissionRequirement } from '../src/auth/permissions';

export function createAuth(overrides: Partial<Me> & { status?: AuthContextValue['status'] } = {}): AuthContextValue {
  const me: Me = {
    userId: '11111111-1111-1111-1111-111111111111',
    name: 'John Doe',
    email: 'john@example.com',
    organizationId: '22222222-2222-2222-2222-222222222222',
    organizationName: 'Acme Ltd',
    roles: ['Member'],
    permissions: ['template.read', 'document.read', 'document.generate'],
    requiresOrganizationSelection: false,
    ...overrides
  };
  return {
    status: overrides.status ?? 'authenticated',
    me,
    organizationId: me.organizationId ?? null,
    permissions: me.permissions,
    error: null,
    can: (required: PermissionRequirement, mode) => hasPermission(me.permissions, required, mode),
    refresh: vi.fn(async () => {}),
    selectOrganization: vi.fn(async () => {}),
    logout: vi.fn()
  };
}

export function renderWithProviders(ui: ReactElement, options: { auth?: AuthContextValue; route?: string; routerProps?: MemoryRouterProps } = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: 0 }, mutations: { retry: false } } });
  const auth = options.auth ?? createAuth();
  const Wrapper = ({ children }: PropsWithChildren) => (
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <AuthContext.Provider value={auth}>
          <MemoryRouter initialEntries={[options.route ?? '/']} {...options.routerProps}>{children}</MemoryRouter>
        </AuthContext.Provider>
      </ToastProvider>
    </QueryClientProvider>
  );
  return { queryClient, auth, ...render(ui, { wrapper: Wrapper }) };
}

type Handler = (url: string, init: RequestInit) => Response | Promise<Response>;

/** Replaces global fetch; returns the mock so tests can assert on calls. */
export function mockFetch(handler: Handler) {
  const mock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => handler(String(input), init));
  vi.stubGlobal('fetch', mock);
  return mock;
}

export const json = (body: unknown, init: ResponseInit = {}) =>
  new Response(JSON.stringify(body), { status: 200, headers: { 'Content-Type': 'application/json' }, ...init });

export const headerOf = (init: RequestInit | undefined, name: string) => new Headers(init?.headers).get(name);
