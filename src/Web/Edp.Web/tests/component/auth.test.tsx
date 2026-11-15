import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { LoginPage } from '../../src/auth/LoginPage';
import { OrganizationSelectPage } from '../../src/auth/OrganizationSelectPage';
import { createAuth, json, mockFetch, renderWithProviders } from '../helpers';

const devUser = { id: 'aaaaaaaa-0000-0000-0000-000000000001', displayName: 'Alice Admin', email: 'alice@example.com', description: 'Owner of everything' };

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('LoginPage', () => {
  const unauthenticated = () => createAuth({ status: 'anonymous' });

  it('offers demo personas only when the Gateway exposes them', async () => {
    mockFetch(() => json({ devAuthEnabled: true, entraConfigured: false, devUsers: [devUser] }));
    renderWithProviders(<LoginPage />, { auth: unauthenticated(), route: '/login' });

    expect(await screen.findByRole('button', { name: /Alice Admin/ })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Microsoft Entra ID/ })).not.toBeInTheDocument();
  });

  it('shows only Entra sign-in when demo users are disabled', async () => {
    mockFetch(() => json({ devAuthEnabled: false, entraConfigured: true, devUsers: [] }));
    renderWithProviders(<LoginPage />, { auth: unauthenticated(), route: '/login' });

    expect(await screen.findByRole('button', { name: /Microsoft Entra ID/ })).toBeInTheDocument();
    expect(screen.queryByText(/Local demo sign-in/)).not.toBeInTheDocument();
  });

  it('keeps the Entra button when the config call fails', async () => {
    mockFetch(() => json({ title: 'down' }, { status: 500 }));
    renderWithProviders(<LoginPage />, { auth: unauthenticated(), route: '/login' });

    expect(await screen.findByRole('button', { name: /Microsoft Entra ID/ })).toBeInTheDocument();
  });
});

describe('OrganizationSelectPage', () => {
  it('creates an organization and selects it', async () => {
    const auth = createAuth({ organizationId: null, requiresOrganizationSelection: true });
    const fetchMock = mockFetch((url, init) => {
      if (url.includes('/bff/organizations')) return json([]);
      if (url.endsWith('/api/v1/organizations') && init.method === 'POST') return json({ id: 'new-org-id', name: 'Contoso' }, { status: 201 });
      return json({}, { status: 404 });
    });
    renderWithProviders(<OrganizationSelectPage />, { auth, route: '/select-organization' });

    await userEvent.type(await screen.findByLabelText(/Organization name/), 'Contoso');
    await userEvent.click(screen.getByRole('button', { name: 'Create' }));

    await waitFor(() => expect(auth.selectOrganization).toHaveBeenCalledWith('new-org-id'));
    const post = fetchMock.mock.calls.find(([, init]) => init?.method === 'POST');
    expect(JSON.parse(String(post?.[1]?.body))).toEqual({ name: 'Contoso' });
  });
});
