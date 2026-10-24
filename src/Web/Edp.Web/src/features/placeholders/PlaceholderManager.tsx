import { useMemo, useState } from 'react';
import { toUserMessage } from '../../api/errors';
import { PLACEHOLDER_DATA_TYPES, type Placeholder, type PlaceholderDiscoveryResult, type ValidationResult } from '../../api/contracts';
import { usePlaceholderMutations, usePlaceholders } from '../../api/hooks/useTemplates';
import { Permissions } from '../../auth/permissions';
import { PermissionGate } from '../../auth/PermissionGate';
import { ConfirmDialog } from '../../components/dialogs/ConfirmDialog';
import { Alert, EmptyState, ErrorState } from '../../components/feedback/Feedback';
import { LoadingSkeleton } from '../../components/feedback/LoadingSkeleton';
import { useToast } from '../../components/feedback/Toast';
import { SelectField, TextField } from '../../components/forms/Fields';
import { DataTable, type Column } from '../../components/tables/DataTable';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { ValidationReport } from '../templates/ValidationReport';
import { PlaceholderFormDialog } from './PlaceholderFormDialog';

interface PlaceholderManagerProps {
  templateId: string;
  versionId: string;
}

type FormState = { placeholder?: Placeholder; initialName?: string } | null;

/** Placeholder CRUD for one template version: list, search/filter, detect, create, edit, delete and validate. */
export function PlaceholderManager({ templateId, versionId }: PlaceholderManagerProps) {
  const query = usePlaceholders(templateId, versionId);
  const mutations = usePlaceholderMutations();
  const toast = useToast();
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [form, setForm] = useState<FormState>(null);
  const [deleting, setDeleting] = useState<Placeholder | null>(null);
  const [discovery, setDiscovery] = useState<PlaceholderDiscoveryResult | null>(null);
  const [validation, setValidation] = useState<ValidationResult | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const rows = useMemo(() => {
    const term = search.trim().toLowerCase();
    return (query.data ?? []).filter((placeholder) =>
      (!term || placeholder.name.toLowerCase().includes(term) || (placeholder.displayName ?? '').toLowerCase().includes(term)) &&
      (!typeFilter || placeholder.dataType === typeFilter));
  }, [query.data, search, typeFilter]);

  const run = async (action: () => Promise<unknown>) => {
    setActionError(null);
    try { await action(); } catch (cause) { setActionError(toUserMessage(cause)); }
  };

  const detect = () => run(async () => {
    const result = await mutations.discover.mutateAsync({ templateId, versionId });
    setDiscovery(result);
    toast.info(`Detected ${result.discovered.length} placeholder${result.discovered.length === 1 ? '' : 's'}.`);
  });

  const validate = () => run(async () => {
    const result = await mutations.validate.mutateAsync({ templateId, versionId });
    setValidation(result);
  });

  const columns: Column<Placeholder>[] = [
    { key: 'name', header: 'Name', cell: (p) => <span><span className="font-medium">{p.name}</span>{p.displayName && <span className="block text-xs text-slate-600">{p.displayName}</span>}</span> },
    { key: 'type', header: 'Type', cell: (p) => p.dataType },
    { key: 'required', header: 'Required', cell: (p) => (p.isRequired ? <Badge tone="info">Required</Badge> : <Badge>Optional</Badge>) },
    { key: 'default', header: 'Default', cell: (p) => p.defaultValue || '—', hideOnCard: true },
    { key: 'uses', header: 'Uses', cell: (p) => p.occurrences, hideOnCard: true },
    {
      key: 'actions', header: 'Actions', srOnlyHeader: true, className: 'text-right',
      cell: (p) => (
        <PermissionGate permission={Permissions.TemplateWrite}>
          <span className="inline-flex gap-1">
            <Button size="sm" variant="ghost" aria-label={`Edit ${p.name}`} onClick={() => setForm({ placeholder: p })}>Edit</Button>
            <Button size="sm" variant="ghost" aria-label={`Delete ${p.name}`} onClick={() => setDeleting(p)}>Delete</Button>
          </span>
        </PermissionGate>
      )
    }
  ];

  if (query.isPending) return <LoadingSkeleton rows={5} label="Loading placeholders" />;
  if (query.isError) return <ErrorState error={query.error} onRetry={() => void query.refetch()} />;

  const existing = new Set(query.data.map((p) => p.name));
  const undefinedNames = discovery?.discovered.filter((name) => !existing.has(name)) ?? [];

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end gap-3">
        <TextField label="Search placeholders" hideLabel type="search" placeholder="Search placeholders…" value={search} onChange={(event) => setSearch(event.target.value)} className="min-w-52 flex-1" />
        <SelectField label="Type" hideLabel value={typeFilter} onChange={(event) => setTypeFilter(event.target.value)}>
          <option value="">All types</option>
          {PLACEHOLDER_DATA_TYPES.map((type) => <option key={type} value={type}>{type}</option>)}
        </SelectField>
        <PermissionGate permission={Permissions.TemplateWrite}>
          <Button onClick={() => void detect()} loading={mutations.discover.isPending}>Detect placeholders</Button>
          <Button variant="primary" onClick={() => setForm({})}>New placeholder</Button>
        </PermissionGate>
        <PermissionGate permission={Permissions.TemplateWrite}>
          <Button onClick={() => void validate()} loading={mutations.validate.isPending}>Validate</Button>
        </PermissionGate>
      </div>

      {actionError && <Alert tone="error" onClose={() => setActionError(null)}>{actionError}</Alert>}

      {discovery && (
        <Alert tone={discovery.missingFromDocument.length > 0 ? 'warning' : 'info'} title="Detection result" onClose={() => setDiscovery(null)}>
          <p>{discovery.discovered.length} found in the file · {discovery.newPlaceholders.length} new · {discovery.existingPlaceholders.length} already defined.</p>
          {discovery.missingFromDocument.length > 0 && <p className="mt-1">Defined but not found in the file: {discovery.missingFromDocument.join(', ')}.</p>}
          {undefinedNames.length > 0 && (
            <ul className="mt-2 flex flex-wrap gap-2">
              {undefinedNames.map((name) => <li key={name}><Button size="sm" onClick={() => setForm({ initialName: name })}>Define “{name}”</Button></li>)}
            </ul>
          )}
        </Alert>
      )}

      {validation && <ValidationReport result={validation} />}

      <DataTable
        caption="Placeholder definitions"
        columns={columns}
        rows={rows}
        getRowKey={(p) => p.id}
        isRefreshing={query.isFetching}
        empty={<EmptyState title={query.data.length === 0 ? 'No placeholders defined' : 'No placeholders match your filters'} description={query.data.length === 0 ? 'Detect placeholders from the uploaded file or create them manually.' : undefined} />}
      />

      {form && <PlaceholderFormDialog key={form.placeholder?.id ?? form.initialName ?? 'new'} open onClose={() => setForm(null)} templateId={templateId} versionId={versionId} placeholder={form.placeholder} initialName={form.initialName} />}

      <ConfirmDialog
        open={deleting !== null}
        title="Delete placeholder?"
        description={deleting ? `“${deleting.name}” will be removed from this template version. Documents generated later will no longer ask for it.` : undefined}
        confirmLabel="Delete"
        danger
        onClose={() => setDeleting(null)}
        onConfirm={async () => {
          if (!deleting) return;
          await mutations.remove.mutateAsync({ templateId, versionId, id: deleting.id });
          toast.success(`Placeholder “${deleting.name}” deleted.`);
        }}
      />
    </div>
  );
}
