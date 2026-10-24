import { useState, type FormEvent } from 'react';
import { Link, useParams, useSearchParams } from 'react-router';
import { ApiError, toUserMessage } from '../../api/errors';
import type { Template, TemplateVersion, ValidationResult } from '../../api/contracts';
import { templateApi } from '../../api/endpoints';
import { useTemplate, useTemplateMutations, useTemplateVersions, useValidationResult } from '../../api/hooks/useTemplates';
import { useInvalidation } from '../../api/queryKeys';
import { Permissions } from '../../auth/permissions';
import { PermissionGate } from '../../auth/PermissionGate';
import { ConfirmDialog } from '../../components/dialogs/ConfirmDialog';
import { Dialog } from '../../components/dialogs/Dialog';
import { Alert, EmptyState } from '../../components/feedback/Feedback';
import { QueryBoundary } from '../../components/feedback/QueryBoundary';
import { useToast } from '../../components/feedback/Toast';
import { FileUploader } from '../../components/files/FileUploader';
import { FormErrorSummary, TextareaField, TextField } from '../../components/forms/Fields';
import { Tabs } from '../../components/navigation/Tabs';
import { DataTable, type Column } from '../../components/tables/DataTable';
import { StatusBadge } from '../../components/ui/Badge';
import { Button, ButtonLink } from '../../components/ui/Button';
import { Card, PageHeader } from '../../components/ui/Card';
import { saveBlob } from '../../utils/download';
import { formatBytes, formatDateTime } from '../../utils/format';
import { PlaceholderManager } from '../placeholders/PlaceholderManager';
import { validateTemplateDetails } from './CreateTemplatePage';
import { ValidationReport } from './ValidationReport';

function EditTemplateDialog({ template, onClose }: { template: Template; onClose: () => void }) {
  const { update } = useTemplateMutations();
  const toast = useToast();
  const invalidate = useInvalidation();
  const [name, setName] = useState(template.name);
  const [description, setDescription] = useState(template.description ?? '');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [serverError, setServerError] = useState<string | null>(null);
  const [conflict, setConflict] = useState(false);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateTemplateDetails({ name, code: template.code, description });
    delete found.code;
    setErrors(found);
    if (Object.keys(found).length > 0) return;
    try {
      await update.mutateAsync({ id: template.id, name: name.trim(), description: description.trim() || undefined, rowVersion: template.rowVersion });
      toast.success('Template updated.');
      onClose();
    } catch (cause) {
      if (cause instanceof ApiError && cause.isConflict) setConflict(true);
      setServerError(toUserMessage(cause));
    }
  };

  return (
    <Dialog open onClose={onClose} title="Edit template" dismissible={!update.isPending}
      footer={<><Button onClick={onClose} disabled={update.isPending}>Cancel</Button><Button variant="primary" type="submit" form="edit-template" loading={update.isPending} disabled={conflict}>Save changes</Button></>}>
      <form id="edit-template" onSubmit={(event) => void submit(event)} noValidate className="space-y-4">
        <FormErrorSummary errors={Object.values(errors)} />
        {conflict ? (
          <Alert tone="warning" title="This record was changed by another user.">
            Your changes were not saved. <button type="button" className="font-semibold underline" onClick={() => { void invalidate.templates(); onClose(); }}>Reload latest</button>
          </Alert>
        ) : serverError && <Alert tone="error">{serverError}</Alert>}
        <TextField label="Name" required value={name} onChange={(event) => setName(event.target.value)} error={errors.name} maxLength={250} />
        <TextField label="Code" value={template.code} disabled help="The code cannot be changed." readOnly />
        <TextareaField label="Description" value={description} onChange={(event) => setDescription(event.target.value)} error={errors.description} maxLength={1000} />
      </form>
    </Dialog>
  );
}

function UploadVersionDialog({ templateId, onClose }: { templateId: string; onClose: () => void }) {
  const invalidate = useInvalidation();
  const [note, setNote] = useState('');
  return (
    <Dialog open onClose={onClose} title="Upload new version" description="Upload a .docx file. The new version must be validated and activated before use." footer={<Button onClick={onClose}>Done</Button>}>
      <div className="space-y-4">
        <TextField label="Change description" value={note} onChange={(event) => setNote(event.target.value)} maxLength={1000} />
        <FileUploader accept=".docx" maxSizeBytes={10 * 1024 * 1024} label="New version file" onUpload={(file, onProgress, signal) => templateApi.uploadVersion(templateId, file, note || undefined, onProgress, signal)} onUploaded={() => void invalidate.templates()} />
      </div>
    </Dialog>
  );
}

function VersionsTab({ template, onShowPlaceholders }: { template: Template; onShowPlaceholders: (versionId: string) => void }) {
  const versions = useTemplateVersions(template.id);
  const { validate, activateVersion } = useTemplateMutations();
  const toast = useToast();
  const [report, setReport] = useState<{ versionNumber: number; result: ValidationResult } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [activating, setActivating] = useState<TemplateVersion | null>(null);

  const runValidate = async (version: TemplateVersion) => {
    setError(null);
    try {
      await validate.mutateAsync({ templateId: template.id, versionId: version.id });
      setReport({ versionNumber: version.versionNumber, result: await templateApi.validation(template.id, version.id) });
    } catch (cause) { setError(toUserMessage(cause)); }
  };

  const download = async (version: TemplateVersion) => {
    setError(null);
    try { saveBlob(await templateApi.downloadVersion(template.id, version.id), version.fileName); } catch (cause) { setError(toUserMessage(cause)); }
  };

  const columns: Column<TemplateVersion>[] = [
    { key: 'v', header: 'Version', cell: (v) => <span className="font-semibold">v{v.versionNumber}</span> },
    { key: 'file', header: 'File', cell: (v) => <span>{v.fileName}<span className="block text-xs text-slate-600">{formatBytes(v.fileSize)}</span></span> },
    { key: 'validation', header: 'Validation', cell: (v) => <StatusBadge status={v.validationStatus} /> },
    { key: 'status', header: 'Status', cell: (v) => <StatusBadge status={v.status} /> },
    { key: 'created', header: 'Uploaded', cell: (v) => formatDateTime(v.createdAt), hideOnCard: true },
    {
      key: 'actions', header: 'Actions', srOnlyHeader: true, className: 'text-right',
      cell: (v) => (
        <span className="inline-flex flex-wrap justify-end gap-1">
          <Button size="sm" variant="ghost" aria-label={`Placeholders for version ${v.versionNumber}`} onClick={() => onShowPlaceholders(v.id)}>Placeholders</Button>
          <Button size="sm" variant="ghost" aria-label={`Download version ${v.versionNumber}`} onClick={() => void download(v)}>Download</Button>
          <PermissionGate permission={Permissions.TemplateWrite}>
            <Button size="sm" variant="ghost" aria-label={`Validate version ${v.versionNumber}`} onClick={() => void runValidate(v)}>Validate</Button>
            {v.status !== 'Active' && <Button size="sm" variant="ghost" aria-label={`Activate version ${v.versionNumber}`} disabled={v.validationStatus !== 'Valid'} onClick={() => setActivating(v)}>Activate</Button>}
          </PermissionGate>
        </span>
      )
    }
  ];

  return (
    <QueryBoundary query={versions} loadingLabel="Loading versions">
      {(rows) => (
        <div className="space-y-4">
          {error && <Alert tone="error" onClose={() => setError(null)}>{error}</Alert>}
          {report && <div><p className="mb-1 text-sm font-semibold">Version {report.versionNumber} validation</p><ValidationReport result={report.result} /></div>}
          <DataTable caption="Template versions" columns={columns} rows={rows} getRowKey={(v) => v.id} empty={<EmptyState title="No versions uploaded" description="Upload a .docx file to create the first version." />} />
          <ConfirmDialog open={activating !== null} title="Activate version?" description={activating ? `Version ${activating.versionNumber} will become the version used for new documents.` : undefined} confirmLabel="Activate"
            onClose={() => setActivating(null)}
            onConfirm={async () => { if (!activating) return; await activateVersion.mutateAsync({ templateId: template.id, versionId: activating.id }); toast.success(`Version ${activating.versionNumber} activated.`); }} />
        </div>
      )}
    </QueryBoundary>
  );
}

function PlaceholdersTab({ template, versionId, onSelectVersion }: { template: Template; versionId: string; onSelectVersion: (versionId: string) => void }) {
  const versions = useTemplateVersions(template.id);
  const validation = useValidationResult(template.id, versionId, !!versionId);
  const selected = versionId || versions.data?.find((v) => v.id === template.currentVersionId)?.id || versions.data?.at(-1)?.id || '';
  return (
    <QueryBoundary query={versions} loadingLabel="Loading versions">
      {(rows) => rows.length === 0 ? <EmptyState title="Upload a version first" description="Placeholders belong to a template version." /> : (
        <div className="space-y-4">
          <label className="block text-sm font-medium">Version
            <select value={selected} onChange={(event) => onSelectVersion(event.target.value)} className="ml-2 rounded-lg border border-slate-300 bg-white px-2 py-1.5 text-sm">
              {rows.map((v) => <option key={v.id} value={v.id}>v{v.versionNumber} — {v.fileName}</option>)}
            </select>
          </label>
          {validation.data && <ValidationReport result={validation.data} />}
          <PlaceholderManager key={selected} templateId={template.id} versionId={selected} />
        </div>
      )}
    </QueryBoundary>
  );
}

export function TemplateDetailPage() {
  const { templateId = '' } = useParams();
  const [params, setParams] = useSearchParams();
  const query = useTemplate(templateId);
  const { deactivate, archive } = useTemplateMutations();
  const toast = useToast();
  const [dialog, setDialog] = useState<'edit' | 'upload' | 'deactivate' | 'archive' | null>(null);
  const tab = params.get('tab') ?? 'overview';
  const versionId = params.get('version') ?? '';
  const setTab = (next: string, version?: string) => setParams((previous) => { const value = new URLSearchParams(previous); value.set('tab', next); if (version) value.set('version', version); else if (next !== 'placeholders') value.delete('version'); return value; });

  return (
    <QueryBoundary query={query} loadingLabel="Loading template">
      {(template) => (
        <>
          <p className="mb-2"><Link to="/templates" className="text-sm font-medium text-teal-800 underline">Back to templates</Link></p>
          <PageHeader title={template.name} description={template.description ?? undefined} eyebrow={template.code}
            actions={
              <>
                <StatusBadge status={template.status} />
                {template.status === 'Active' && <PermissionGate permission={Permissions.DocumentGenerate}><ButtonLink variant="primary" to={`/documents/new?templateId=${template.id}`}>Generate document</ButtonLink></PermissionGate>}
                <PermissionGate permission={Permissions.TemplateWrite}>
                  <Button onClick={() => setDialog('upload')}>Upload version</Button>
                  <Button onClick={() => setDialog('edit')}>Edit</Button>
                  {template.status === 'Active' && <Button onClick={() => setDialog('deactivate')}>Deactivate</Button>}
                  {template.status !== 'Archived' && <Button variant="danger" onClick={() => setDialog('archive')}>Archive</Button>}
                </PermissionGate>
              </>
            } />
          <Tabs label="Template sections" value={tab} onChange={(id) => setTab(id)}
            tabs={[
              { id: 'overview', label: 'Overview', render: () => (
                <Card><dl className="grid gap-4 text-sm sm:grid-cols-2">
                  <div><dt className="text-slate-600">Status</dt><dd><StatusBadge status={template.status} /></dd></div>
                  <div><dt className="text-slate-600">Code</dt><dd className="font-medium">{template.code}</dd></div>
                  <div><dt className="text-slate-600">Created</dt><dd>{formatDateTime(template.createdAt)}{template.createdBy ? ` by ${template.createdBy}` : ''}</dd></div>
                  <div><dt className="text-slate-600">Last updated</dt><dd>{formatDateTime(template.modifiedAt ?? template.createdAt)}</dd></div>
                </dl></Card>
              ) },
              { id: 'versions', label: 'Versions', render: () => <VersionsTab template={template} onShowPlaceholders={(id) => setTab('placeholders', id)} /> },
              { id: 'placeholders', label: 'Placeholders', render: () => <PlaceholdersTab template={template} versionId={versionId} onSelectVersion={(id) => setTab('placeholders', id)} /> }
            ]} />

          {dialog === 'edit' && <EditTemplateDialog template={template} onClose={() => setDialog(null)} />}
          {dialog === 'upload' && <UploadVersionDialog templateId={template.id} onClose={() => setDialog(null)} />}
          <ConfirmDialog open={dialog === 'deactivate'} title="Deactivate template?" description="New documents can no longer be generated from this template until it is activated again." confirmLabel="Deactivate" onClose={() => setDialog(null)}
            onConfirm={async () => { await deactivate.mutateAsync(template.id); toast.success('Template deactivated.'); }} />
          <ConfirmDialog open={dialog === 'archive'} title="Archive template?" description="Archived templates are hidden from generation. Existing documents are not affected." confirmLabel="Archive" danger onClose={() => setDialog(null)}
            onConfirm={async () => { await archive.mutateAsync(template.id); toast.success('Template archived.'); }} />
        </>
      )}
    </QueryBoundary>
  );
}
