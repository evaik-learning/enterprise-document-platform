import { useEffect, useId, useRef, useState } from 'react';
import { Link } from 'react-router';
import { useGlobalSearch } from '../../api/hooks/usePlatformQueries';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { humanize } from '../../utils/status';
import { StatusBadge } from '../ui/Badge';

const GROUP_LABELS: Record<string, string> = { templates: 'Templates', documents: 'Documents', workflows: 'Workflows', signatures: 'Signatures' };

/** Debounced, tenant- and permission-scoped search across templates, documents, workflows and signatures. */
export function GlobalSearch() {
  const [term, setTerm] = useState('');
  const [open, setOpen] = useState(false);
  const debounced = useDebouncedValue(term.trim(), 300);
  const query = useGlobalSearch(debounced);
  const root = useRef<HTMLDivElement>(null);
  const inputId = useId();

  useEffect(() => {
    const onPointer = (event: MouseEvent) => { if (!root.current?.contains(event.target as Node)) setOpen(false); };
    document.addEventListener('mousedown', onPointer);
    return () => document.removeEventListener('mousedown', onPointer);
  }, []);

  const showPanel = open && term.trim().length >= 2;

  return (
    <div ref={root} role="search" className="relative w-full max-w-md">
      <label htmlFor={inputId} className="sr-only">Search documents, templates, workflows</label>
      <input
        id={inputId}
        type="search"
        value={term}
        autoComplete="off"
        placeholder="Search documents, templates, workflows..."
        onChange={(event) => { setTerm(event.target.value); setOpen(true); }}
        onFocus={() => setOpen(true)}
        onKeyDown={(event) => { if (event.key === 'Escape') { setOpen(false); setTerm(''); } }}
        className="block w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm placeholder:text-slate-500 focus-visible:outline-2 focus-visible:outline-teal-700"
      />
      {showPanel && (
        <div aria-live="polite" className="absolute left-0 right-0 z-40 mt-2 max-h-[70vh] overflow-y-auto rounded-xl border border-slate-200 bg-white p-2 shadow-lg">
          {(query.isFetching || debounced !== term.trim()) && !query.data && <p className="p-3 text-sm text-slate-600">Searching…</p>}
          {query.isError && <p className="p-3 text-sm text-red-800">Search is unavailable right now.</p>}
          {query.data && query.data.groups.every((group) => group.items.length === 0) && <p className="p-3 text-sm text-slate-600">No results for “{query.data.query}”.</p>}
          {query.data?.groups.filter((group) => group.items.length > 0).map((group) => (
            <section key={group.type} aria-label={GROUP_LABELS[group.type] ?? humanize(group.type)} className="mb-1">
              <h3 className="px-3 pt-2 text-xs font-semibold uppercase tracking-wide text-slate-500">{GROUP_LABELS[group.type] ?? humanize(group.type)} <span className="font-normal normal-case">({group.totalCount} result{group.totalCount === 1 ? '' : 's'})</span></h3>
              <ul>
                {group.items.map((item) => (
                  <li key={`${group.type}-${item.id}`}>
                    <Link to={item.url} onClick={() => setOpen(false)} className="flex items-center justify-between gap-2 rounded-lg px-3 py-2 text-sm hover:bg-slate-100 focus-visible:outline-2 focus-visible:outline-teal-700">
                      <span className="min-w-0"><span className="block truncate font-medium">{item.title}</span>{item.subtitle && <span className="block truncate text-xs text-slate-600">{item.subtitle}</span>}</span>
                      {item.status && <StatusBadge status={item.status} />}
                    </Link>
                  </li>
                ))}
              </ul>
            </section>
          ))}
        </div>
      )}
    </div>
  );
}
