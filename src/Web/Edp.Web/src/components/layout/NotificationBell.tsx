import { Link, useNavigate } from 'react-router';
import { useLatestNotifications, useNotificationMutations, useUnreadCount } from '../../api/hooks/usePlatformQueries';
import { Permissions } from '../../auth/permissions';
import { useAuth } from '../../auth/useAuth';
import { formatDateTime } from '../../utils/format';
import { notificationRoute } from '../../utils/notificationRoutes';
import { Popover } from '../navigation/Popover';

/** Header bell with unread badge and the latest notifications; opening one marks it read and navigates to its resource. */
function BellIcon() {
  return (
    <svg aria-hidden="true" className="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9" />
      <path d="M10.3 21a1.94 1.94 0 0 0 3.4 0" />
    </svg>
  );
}

export function NotificationBell() {
  const { can } = useAuth();
  const enabled = can(Permissions.NotificationRead);
  const unread = useUnreadCount(enabled);
  const latest = useLatestNotifications(enabled);
  const { markRead } = useNotificationMutations();
  const navigate = useNavigate();
  if (!enabled) return null;

  const count = unread.data?.count ?? 0;
  const label = count > 0 ? `Notifications, ${count} unread` : 'Notifications';

  return (
    <Popover label={label} trigger={<span className="flex items-center gap-1.5"><BellIcon />{count > 0 && <span className="rounded-full bg-teal-700 px-1.5 text-xs font-bold text-white">{count > 99 ? '99+' : count}</span>}</span>} panelClassName="w-80">
      {(close) => (
        <div>
          <div className="flex items-center justify-between px-2 pb-2">
            <h2 className="text-sm font-semibold">Notifications</h2>
            <Link to="/notifications" onClick={close} className="text-xs font-semibold text-teal-800 underline">View all</Link>
          </div>
          {latest.isError && <p className="p-3 text-sm text-red-800">Notifications could not be loaded.</p>}
          {latest.data && latest.data.items.length === 0 && <p className="p-3 text-sm text-slate-600">You are all caught up.</p>}
          <ul>
            {latest.data?.items.map((notification) => (
              <li key={notification.id}>
                <button
                  type="button"
                  className="block w-full rounded-lg px-2 py-2 text-left text-sm hover:bg-slate-100"
                  onClick={() => {
                    close();
                    if (!notification.readAtUtc) markRead.mutate(notification.id);
                    void navigate(notificationRoute(notification));
                  }}
                >
                  <span className="flex items-center gap-2 font-medium">{!notification.readAtUtc && <span aria-hidden="true" className="size-2 rounded-full bg-teal-700" />}{!notification.readAtUtc && <span className="sr-only">Unread: </span>}{notification.subject}</span>
                  <span className="block text-slate-600">{notification.body}</span>
                  <span className="block text-xs text-slate-500">{formatDateTime(notification.createdAtUtc)}</span>
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}
    </Popover>
  );
}
