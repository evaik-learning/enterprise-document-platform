import { NavLink } from 'react-router';
import { useAuth } from '../../auth/useAuth';
import { cx } from '../../utils/cx';
import { visibleNavigation } from './navigation';

export function SidebarNav({ onNavigate }: { onNavigate?: () => void }) {
  const { can } = useAuth();
  const groups = visibleNavigation(can);
  return (
    <nav aria-label="Main" className="space-y-5">
      {groups.map((group, index) => (
        <div key={group.heading ?? index}>
          {group.heading && <p className="mb-1 px-3 text-xs font-semibold uppercase tracking-wide text-slate-500">{group.heading}</p>}
          <ul className="space-y-0.5">
            {group.items.map((item) => (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end={item.end}
                  onClick={onNavigate}
                  className={({ isActive }) => cx('block rounded-lg px-3 py-2 text-sm font-medium focus-visible:outline-2 focus-visible:outline-teal-700', isActive ? 'bg-slate-900 text-white' : 'text-slate-700 hover:bg-slate-100')}
                >
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </div>
      ))}
    </nav>
  );
}
