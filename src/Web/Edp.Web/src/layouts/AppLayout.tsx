import { Link, NavLink, Outlet } from 'react-router';
import { useEffect, useState } from 'react';
import { useAuthentication } from '../hooks/useAuthentication';
import { platformApi, type Notification } from '../services/platformApi';

const navigation = [
  { label: 'Dashboard', to: '/' },
  { label: 'Documents', to: '/documents' },
  { label: 'Templates', to: '/templates' },
  { label: 'Workflows', to: '/workflows' },
  { label: 'Approvals', to: '/approvals' },
  { label: 'Signing', to: '/signing' },
  { label: 'Notifications', to: '/notifications' },
  { label: 'Audit', to: '/audit' }
];

export function AppLayout() {
  const { user, logout } = useAuthentication();
  const [unreadCount, setUnreadCount] = useState(0);
  const [latest, setLatest] = useState<Notification[]>([]);
  const [showNotifications, setShowNotifications] = useState(false);

  useEffect(() => {
    const refresh = async () => {
      if (document.hidden) return;
      try {
        const [count, result] = await Promise.all([
          platformApi.notifications.unreadCount(),
          platformApi.notifications.list(false, 1, 5)
        ]);
        setUnreadCount(count.count);
        setLatest(result.items);
      } catch {
      }
    };
    void refresh();
    const interval = window.setInterval(refresh, 60_000);
    document.addEventListener('visibilitychange', refresh);
    return () => { window.clearInterval(interval); document.removeEventListener('visibilitychange', refresh); };
  }, []);

  return (
    <div className="min-h-screen bg-slate-50 text-slate-950">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-teal-700">EDP</p>
            <h1 className="text-lg font-semibold">Enterprise Document Platform</h1>
          </div>
          <div className="flex items-center gap-3">
            <div className="relative">
              <button aria-label={`Notifications${unreadCount ? `, ${unreadCount} unread` : ''}`} aria-expanded={showNotifications} className="relative rounded border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100" type="button" onClick={() => setShowNotifications(value => !value)}>
                Notifications
                {unreadCount > 0 && <span className="ml-2 rounded-full bg-teal-700 px-2 py-0.5 text-xs text-white">{unreadCount > 99 ? '99+' : unreadCount}</span>}
              </button>
              {showNotifications && <div className="absolute right-0 z-10 mt-2 w-80 rounded-lg border border-slate-200 bg-white p-3 shadow-lg"><div className="flex items-center justify-between"><h2 className="font-semibold">Latest notifications</h2><Link className="text-xs font-semibold text-teal-700 underline" to="/notifications" onClick={() => setShowNotifications(false)}>View all</Link></div><div className="mt-2 space-y-2">{latest.length === 0 ? <p className="py-4 text-sm text-slate-500">No notifications yet.</p> : latest.map(notification => <Link className="block rounded p-2 text-sm hover:bg-slate-50" key={notification.id} to="/notifications" onClick={() => setShowNotifications(false)}><p className="font-medium">{notification.subject || notification.notificationType}</p><p className="mt-1 line-clamp-2 text-slate-600">{notification.body}</p></Link>)}</div></div>}
            </div>
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
