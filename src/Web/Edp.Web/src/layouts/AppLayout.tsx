import { NavLink, Outlet } from 'react-router';
import { useAuthentication } from '../hooks/useAuthentication';

const navigation = [
  { label: 'Documents', to: '/documents' },
  { label: 'Templates', to: '/templates' },
  { label: 'Workflows', to: '/workflows' },
  { label: 'Approvals', to: '/approvals' },
  { label: 'Audit', to: '/audit' }
];

export function AppLayout() {
  const { user, logout } = useAuthentication();

  return (
    <div className="min-h-screen bg-slate-50 text-slate-950">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-teal-700">EDP</p>
            <h1 className="text-lg font-semibold">Enterprise Document Platform</h1>
          </div>
          <div className="flex items-center gap-3">
            <div className="text-right text-sm">
              <p className="font-medium">{user?.displayName}</p>
              <p className="text-slate-500">{user?.userName}</p>
            </div>
            <button
              className="rounded border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
              type="button"
              onClick={logout}
            >
              Logout
            </button>
          </div>
        </div>
      </header>

      <div className="mx-auto grid max-w-7xl grid-cols-[220px_1fr] gap-6 px-6 py-6">
        <nav className="flex flex-col gap-1">
          {navigation.map((item) => (
            <NavLink
              className={({ isActive }) =>
                [
                  'rounded px-3 py-2 text-sm font-medium',
                  isActive ? 'bg-slate-900 text-white' : 'text-slate-700 hover:bg-white'
                ].join(' ')
              }
              key={item.to}
              to={item.to}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
        <main className="min-w-0">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
