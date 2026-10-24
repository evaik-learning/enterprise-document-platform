import { useMemo, useState, type FormEvent } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { toUserMessage } from '../../api/errors';
import type { TemplateVersion } from '../../api/contracts';
import { templateApi } from '../../api/endpoints';
import { useDocumentMutations } from '../../api/hooks/useDocuments';
import { useTemplates } from '../../api/hooks/useTemplates';
import { useQuery } from '@tanstack/react-query';
import { useQueryKeys } from '../../api/queryKeys';
import { Alert, EmptyState } from '../../components/feedback/Feedback';
import { useToast } from '../../components/feedback/Toast';
import { CheckboxField, FormErrorSummary, SelectField, TextareaField, TextField } from '../../components/forms/Fields';
import { Button } from '../../components/ui/Button';
import { Card, PageHeader } from '../../components/ui/Card';
import { UnsavedChangesGuard } from '../../hooks/UnsavedChangesGuard';
import { buildGenerationData, initialValues, PlaceholderValueFields, type FieldValues } from './PlaceholderValueFields';

/** A version can be used for generation when it is active and not known to be invalid. */
export const isUsableVersion = (version: TemplateVersion) => version.status === 'Active' && version.validationStatus !== 'Invalid' && version.validationStatus !== 'NotValidated';

/** Generate a document: select template -> select version -> enter values -> validate -> generate. */
export function GenerateDocumentPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const toast = useToast();
  const keys = useQueryKeys();
  const { create, generate } = useDocumentMutations();
  const templates = useTemplates({ status: 'Active', page: 1, pageSize: 100 });

  const [templateId, setTemplateId] = useState(params.get('templateId') ?? '');
  const [versionId, setVersionId] = useState('');
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [values, setValues] = useState<{ versionKey: string; values: FieldValues } | null>(null);
  const [formats, setFormats] = useState({ DOCX: true, PDF: true });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [serverError, setServerError] = useState<string | null>(null);
  const [createdId, setCreatedId] = useState<string | null>(null);
  const [submitted, setSubmitted] = useState(false);

  const versions = useQuery({ queryKey: keys.templates.versions(templateId), queryFn: () => templateApi.versions(templateId), enabled: !!templateId });
  const usable = useMemo(() => (versions.data ?? []).filter(isUsableVersion), [versions.data]);
  const version = usable.find((candidate) => candidate.id === versionId) ?? usable[0];
  const template = templates.data?.items.find((candidate) => candidate.id === templateId);

  // Values are tied to the effective version; switching version starts from that version's defaults.
  const currentValues: FieldValues = version ? (values?.versionKey === version.id ? values.values : initialValues(version.placeholders)) : {};

  const dirty = !submitted && (name !== '' || Object.values(currentValues).some((value) => value !== '' && value !== false));

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setServerError(null);
    const problems: Record<string, string> = {};
    if (!template) problems.template = 'Select a template.';
    if (!version) problems.version = 'The template has no active, validated version.';
    if (!name.trim()) problems.name = 'Document name is required.';
    const outputFormats = Object.entries(formats).filter(([, on]) => on).map(([format]) => format);
    if (outputFormats.length === 0) problems.formats = 'Select at least one output format.';
    const built = version ? buildGenerationData(version.placeholders, currentValues) : { data: {}, errors: {} };
    const all = { ...problems, ...built.errors };
    setErrors(all);
    if (Object.keys(all).length > 0 || !template || !version) return;

    try {
      let documentId = createdId;
      if (!documentId) {
        const document = await create.mutateAsync({ name: name.trim(), documentType: 'Contract', description: description.trim() || undefined, templateId: template.id, templateVersion: version.versionNumber });
        documentId = document.id;
        setCreatedId(documentId);
      }
      await generate.mutateAsync({ id: documentId, body: { name: name.trim(), outputFormats, data: built.data } });
      setSubmitted(true);
      toast.success('Document generation started.');
      void navigate(`/documents/${documentId}`);
    } catch (cause) {
      setServerError(toUserMessage(cause));
    }
  };

  const busy = create.isPending || generate.isPending;

  return (
    <>
      <UnsavedChangesGuard dirty={dirty} />
      <p className="mb-2"><Link to="/documents" className="text-sm font-medium text-teal-800 underline">Back to documents</Link></p>
      <PageHeader eyebrow="New document" title="Generate document" description="Choose an active template, enter the values and generate. Long-running generation continues in the background." />

      {templates.data && templates.data.items.length === 0 ? (
        <EmptyState title="No active templates" description="Activate a template before generating documents." />
      ) : (
        <form onSubmit={(event) => void submit(event)} noValidate className="space-y-6">
          <FormErrorSummary errors={Object.values(errors)} />
          {serverError && <Alert tone="error" title="The document could not be generated">{serverError}{createdId && ' The draft was saved; submit again to retry generation.'}</Alert>}

          <Card title="1. Template">
            <div className="grid gap-4 sm:grid-cols-2">
              <SelectField label="Template" required value={templateId} disabled={busy || !!createdId} error={errors.template}
                onChange={(event) => { setTemplateId(event.target.value); setVersionId(''); setErrors({}); }}>
                <option value="">Select an active template</option>
                {templates.data?.items.map((candidate) => <option key={candidate.id} value={candidate.id}>{candidate.name} ({candidate.code})</option>)}
              </SelectField>
              <SelectField label="Version" required value={version?.id ?? ''} disabled={busy || !!createdId || usable.length === 0} error={errors.version} onChange={(event) => setVersionId(event.target.value)}>
                {usable.length === 0 && <option value="">{templateId ? 'No usable version' : 'Select a template first'}</option>}
                {usable.map((candidate) => <option key={candidate.id} value={candidate.id}>v{candidate.versionNumber} — {candidate.fileName}</option>)}
              </SelectField>
            </div>
            {templateId && versions.isSuccess && usable.length === 0 && <div className="mt-3"><Alert tone="warning">This template has no active, validated version. Validate and activate a version first.</Alert></div>}
          </Card>

          <Card title="2. Document details">
            <div className="grid gap-4 sm:grid-cols-2">
              <TextField label="Document name" required value={name} disabled={busy} onChange={(event) => setName(event.target.value)} error={errors.name} maxLength={250} />
              <div className="sm:col-span-2"><TextareaField label="Description" value={description} disabled={busy} onChange={(event) => setDescription(event.target.value)} maxLength={1000} /></div>
            </div>
          </Card>

          {version && (
            <Card title="3. Placeholder values">
              <PlaceholderValueFields placeholders={version.placeholders} values={currentValues} errors={errors} disabled={busy} onChange={(field, value) => setValues({ versionKey: version.id, values: { ...currentValues, [field]: value } })} />
            </Card>
          )}

          <Card title="4. Output">
            <div className="flex flex-wrap gap-6">
              <CheckboxField label="DOCX" checked={formats.DOCX} onChange={(event) => setFormats((previous) => ({ ...previous, DOCX: event.target.checked }))} />
              <CheckboxField label="PDF" checked={formats.PDF} onChange={(event) => setFormats((previous) => ({ ...previous, PDF: event.target.checked }))} />
            </div>
            {errors.formats && <p className="mt-2 text-xs font-medium text-red-800">{errors.formats}</p>}
          </Card>

          <div className="flex gap-2">
            <Button type="submit" variant="primary" loading={busy} disabled={!version}>{createdId ? 'Retry generation' : 'Validate and generate'}</Button>
            <Button onClick={() => void navigate('/documents')} disabled={busy}>Cancel</Button>
          </div>
        </form>
      )}
    </>
  );
}
