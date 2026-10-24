import { useRef, type KeyboardEvent, type ReactNode } from 'react';
import { cx } from '../../utils/cx';

export interface TabDefinition {
  id: string;
  label: string;
  badge?: ReactNode;
  hidden?: boolean;
  render: () => ReactNode;
}

interface TabsProps {
  label: string;
  tabs: TabDefinition[];
  value: string;
  onChange: (id: string) => void;
}

/** WAI-ARIA tabs: roving tabindex with arrow/Home/End keys. Only the active panel is rendered. */
export function Tabs({ label, tabs, value, onChange }: TabsProps) {
  const visible = tabs.filter((tab) => !tab.hidden);
  const refs = useRef<Record<string, HTMLButtonElement | null>>({});
  const active = visible.find((tab) => tab.id === value) ?? visible[0];

  const onKeyDown = (event: KeyboardEvent<HTMLButtonElement>, index: number) => {
    const move = { ArrowRight: index + 1, ArrowLeft: index - 1, Home: 0, End: visible.length - 1 }[event.key];
    if (move === undefined) return;
    event.preventDefault();
    const next = visible[(move + visible.length) % visible.length];
    if (next) { onChange(next.id); refs.current[next.id]?.focus(); }
  };

  return (
    <div>
      <div role="tablist" aria-label={label} className="flex gap-1 overflow-x-auto border-b border-slate-200">
        {visible.map((tab, index) => {
          const selected = tab.id === active?.id;
          return (
            <button
              key={tab.id}
              ref={(element) => { refs.current[tab.id] = element; }}
              id={`tab-${tab.id}`}
              role="tab"
              type="button"
              aria-selected={selected}
              aria-controls={`panel-${tab.id}`}
              tabIndex={selected ? 0 : -1}
              onClick={() => onChange(tab.id)}
              onKeyDown={(event) => onKeyDown(event, index)}
              className={cx('whitespace-nowrap border-b-2 px-4 py-2.5 text-sm font-medium focus-visible:outline-2 focus-visible:outline-teal-700', selected ? 'border-teal-700 text-teal-900' : 'border-transparent text-slate-600 hover:text-slate-900')}
            >
              {tab.label}{tab.badge && <span className="ml-2">{tab.badge}</span>}
            </button>
          );
        })}
      </div>
      {active && <div id={`panel-${active.id}`} role="tabpanel" aria-labelledby={`tab-${active.id}`} tabIndex={0} className="pt-5 focus-visible:outline-none">{active.render()}</div>}
    </div>
  );
}
