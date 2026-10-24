import { useState } from 'react';
import { Link, Outlet } from 'react-router';
import { appConfig } from '../../app/config';
import { useAuth } from '../../auth/useAuth';
import { Dialog } from '../dialogs/Dialog';
import { Popover } from '../navigation/Popover';
import { SidebarNav } from '../navigation/SidebarNav';
import { GlobalSearch } from './GlobalSearch';
import { NotificationBell } from './NotificationBell';
import { OrganizationSwitcher } from './OrganizationSwitcher';

function UserMenu() {
  const { me, logout } = useAuth();
  const name = me?.name ?? me?.email ?? 'Account';
  return (
    <Popover label={`Account menu for ${name}`} trigger={<span className="flex items-center gap-2"><span aria-hidden="true" className="grid size-7 place-items-center rounded-full bg-slate-900 text-xs font-semibold text-white">{name.slice(0, 1).toUpperCase()}</span><span className="hidden max-w-32 truncate md:inline">{name}</span></span>}>
      {(close) => (
        <div className="text-sm">
          <div className="border-b border-slate-200 px-2 pb-2">
            <p className="font-semibold">{me?.name}</p>
            {me?.email && <p className="text-slate-600">{me.email}</p>}
            {me?.roles && me.roles.length > 0 && <p className="text-xs text-slate-500">{me.roles.join(', ')}</p>}
          </div>
          <Link to="/profile" onClick={close} className="mt-1 block rounded-lg px-2 py-2 hover:bg-slate-100">Profile</Link>
          <button type="button" onClick={logout} className="block w-full rounded-lg px-2 py-2 text-left hover:bg-slate-100">Sign out</button>
        </div>
      )}
    </Popover>
  );
}

/** Application shell: header + permission-aware sidebar (drawer on mobile) + routed content. */
export function AppShell() {
  const [menuOpen, setMenuOpen] = useState(false);
  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <a href="#main-content" className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:z-[70] focus:rounded focus:bg-white focus:px-3 focus:py-2 focus:shadow">Skip to main content</a>
      <header className="sticky top-0 z-30 border-b border-slate-200 bg-white">
        <div className="flex items-center gap-3 px-4 py-2.5 lg:px-6">
          <button type="button" aria-label="Open navigation menu" aria-haspopup="dialog" className="rounded-lg border border-slate-300 px-2.5 py-1.5 text-sm font-medium lg:hidden" onClick={() => setMenuOpen(true)}>Menu</button>
          <Link to="/" className="flex shrink-0 items-center gap-2 font-semibold">
            <span aria-hidden="true" className="grid size-8 place-items-center rounded-lg bg-teal-700 text-sm font-bold text-white">E</span>
            <span className="hidden sm:inline">{appConfig.appName}</span>
          </Link>
          <div className="hidden lg:block"><OrganizationSwitcher /></div>
          <div className="ml-auto hidden flex-1 justify-center md:flex"><GlobalSearch /></div>
          <div className="ml-auto flex items-center gap-1 md:ml-0">
            <NotificationBell />
            <UserMenu />
          </div>
        </div>
        <div className="flex items-center gap-2 border-t border-slate-100 px-4 py-2 md:hidden">
          <div className="min-w-0 flex-1"><GlobalSearch /></div>
          <OrganizationSwitcher />
        </div>
      </header>

      <div className="lg:grid lg:grid-cols-[15rem_1fr]">
        <aside className="hidden border-r border-slate-200 bg-white p-4 lg:sticky lg:top-[57px] lg:block lg:h-[calc(100vh-57px)] lg:overflow-y-auto">
          <SidebarNav />
        </aside>
        <Dialog open={menuOpen} onClose={() => setMenuOpen(false)} title="Navigation" variant="drawer-left">
          <SidebarNav onNavigate={() => setMenuOpen(false)} />
        </Dialog>
        <main id="main-content" tabIndex={-1} className="min-w-0 px-4 py-6 focus:outline-none lg:px-8">
          <div className="mx-auto max-w-7xl"><Outlet /></div>
        </main>
      </div>
    </div>
  );
}
