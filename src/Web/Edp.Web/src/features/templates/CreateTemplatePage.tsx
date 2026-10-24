import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router';
import { ApiError, toUserMessage } from '../../api/errors';
import type { Template, TemplateVersion, ValidationResult } from '../../api/contracts';
import { templateApi } from '../../api/endpoints';
import { useTemplateMutations } from '../../api/hooks/useTemplates';
import { Alert } from '../../components/feedback/Feedback';
import { useToast } from '../../components/feedback/Toast';
import { FileUploader } from '../../components/files/FileUploader';
import { FormErrorSummary, TextareaField, TextField } from '../../components/forms/Fields';
import { Button } from '../../components/ui/Button';
import { Card, PageHeader } from '../../components/ui/Card';
import { UnsavedChangesGuard } from '../../hooks/UnsavedChangesGuard';
import { cx } from '../../utils/cx';
import { PlaceholderManager } from '../placeholders/PlaceholderManager';
import { ValidationReport } from './ValidationReport';

const STEPS = ['Details', 'Upload file', 'Placeholders', 'Validate', 'Activate'] as const;
const MAX_FILE_BYTES = 10 * 1024 * 1024;
const CODE_PATTERN = /^[A-Za-z0-9][A-Za-z0-9_-]*$/;

export function validateTemplateDetails(values: { name: string; code: string; description: string }) {
  const errors: Record<string, string> = {};
  if (!values.name.trim()) errors.name = 'Name is required.';
  else if (values.name.length > 250) errors.name = 'Name must be 250 characters or fewer.';
  if (!values.code.trim()) errors.code = 'Code is required.';
  else if (!CODE_PATTERN.test(values.code.trim())) errors.code = 'Use letters, numbers, dashes and underscores only.';
  else if (values.code.length > 100) errors.code = 'Code must be 100 characters or fewer.';
  if (values.description.length > 1000) errors.description = 'Description must be 1000 characters or fewer.';
  return errors;
}

function Stepper({ current }: { current: number }) {
  return (
    <ol aria-label="Template creation steps" className="mb-6 flex flex-wrap gap-2 text-sm">
      {STEPS.map((label, index) => (
        <li key={label} aria-current={index === current ? 'step' : undefined} className={cx('flex items-center gap-2 rounded-full border px-3 py-1', index === current ? 'border-teal-700 bg-teal-50 font-semibold text-teal-900' : index < current ? 'border-emerald-300 text-emerald-900' : 'border-slate-300 text-slate-600')}>
          <span aria-hidden="true">{index < current ? '✓' : index + 1}</span>
          <span>{label}</span>
          {index < current && <span className="sr-only">(completed)</span>}
        </li>
      ))}
    </ol>
  );
}

/** Template creation flow: details -> upload -> detect/review placeholders -> validate -> activate. */
export function CreateTemplatePage() {
  const navigate = useNavigate();
  const toast = useToast();
  const { create, validate, activateVersion } = useTemplateMutations();
  const [step, setStep] = useState(0);
  const [name, setName] = useState('');
  const [code, setCode] = useState('');
  const [description, setDescription] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [serverError, setServerError] = useState<string | null>(null);
  const [template, setTemplate] = useState<Template | null>(null);
  const [version, setVersion] = useState<TemplateVersion | null>(null);
  const [validation, setValidation] = useState<ValidationResult | null>(null);

  const dirty = step === 0 && (name !== '' || code !== '' || description !== '');
  const [skipGuard, setSkipGuard] = useState(false);

  const submitDetails = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateTemplateDetails({ name, code, description });
    setErrors(found);
    setServerError(null);
    if (Object.keys(found).length > 0) return;
    try {
      setTemplate(await create.mutateAsync({ name: name.trim(), code: code.trim(), description: description.trim() || undefined }));
      setStep(1);
    } catch (cause) {
      if (cause instanceof ApiError && cause.isConflict) setErrors({ code: 'A template with this code already exists.' });
      setServerError(toUserMessage(cause));
    }
  };

  const runValidation = async () => {
    if (!template || !version) return;
    setServerError(null);
    try {
      await validate.mutateAsync({ templateId: template.id, versionId: version.id });
      setValidation(await templateApi.validation(template.id, version.id));
    } catch (cause) {
      setServerError(toUserMessage(cause));
    }
  };

  const activate = async () => {
    if (!template || !version) return;
    setServerError(null);
    try {
      await activateVersion.mutateAsync({ templateId: template.id, versionId: version.id });
      toast.success(`Template “${template.name}” is active.`);
      setSkipGuard(true);
      void navigate(`/templates/${template.id}`);
    } catch (cause) {
      setServerError(toUserMessage(cause));
    }
  };

  return (
    <>
      <UnsavedChangesGuard dirty={dirty && !skipGuard} />
      <PageHeader eyebrow="Templates" title="Create template" description="Define the template, upload the Word file, review its placeholders, validate and activate." actions={<Link to="/templates" className="text-sm font-medium text-teal-800 underline">Cancel</Link>} />
      <Stepper current={step} />
      {serverError && <div className="mb-4"><Alert tone="error">{serverError}</Alert></div>}

      {step === 0 && (
        <Card>
          <form onSubmit={(event) => void submitDetails(event)} noValidate className="max-w-xl space-y-4">
            <FormErrorSummary errors={Object.values(errors)} />
            <TextField label="Name" required value={name} onChange={(event) => setName(event.target.value)} error={errors.name} maxLength={250} autoFocus />
            <TextField label="Code" required value={code} onChange={(event) => setCode(event.target.value)} error={errors.code} help="Unique identifier, e.g. NDA-STANDARD." maxLength={100} />
            <TextareaField label="Description" value={description} onChange={(event) => setDescription(event.target.value)} error={errors.description} maxLength={1000} />
            <Button type="submit" variant="primary" loading={create.isPending}>Save and continue</Button>
          </form>
        </Card>
      )}

      {step === 1 && template && (
        <Card title="Upload the template file">
          <p className="mb-4 text-sm text-slate-600">Upload a .docx file containing placeholders such as {'{{CustomerName}}'}. The file is stored by the platform; it is never sent to storage directly from the browser.</p>
          <FileUploader accept=".docx" maxSizeBytes={MAX_FILE_BYTES} label="Template file" onUpload={async (file, onProgress, signal) => {
            const uploaded = await templateApi.uploadVersion(template.id, file, 'Initial version', onProgress, signal);
            setVersion(uploaded);
          }} />
          <div className="mt-4 flex gap-2">
            <Button variant="primary" disabled={!version} onClick={() => setStep(2)}>Continue</Button>
            <Button onClick={() => { setSkipGuard(true); void navigate(`/templates/${template.id}`); }}>Save draft and exit</Button>
          </div>
        </Card>
      )}

      {step === 2 && template && version && (
        <Card title="Review placeholders" actions={<Button variant="primary" onClick={() => setStep(3)}>Continue</Button>}>
          <p className="mb-4 text-sm text-slate-600">Detect the placeholders found in the file, then define their data types and defaults.</p>
          <PlaceholderManager templateId={template.id} versionId={version.id} />
        </Card>
      )}

      {step === 3 && template && version && (
        <Card title="Validate the template">
          <p className="mb-4 text-sm text-slate-600">Validation checks the file structure and placeholders. A version must be valid before it can be activated.</p>
          <Button variant="primary" loading={validate.isPending} onClick={() => void runValidation()}>{validation ? 'Validate again' : 'Validate'}</Button>
          {validation && <div className="mt-4"><ValidationReport result={validation} /></div>}
          <div className="mt-4 flex gap-2">
            <Button variant="primary" disabled={!validation?.isValid} onClick={() => setStep(4)}>Continue</Button>
            <Button onClick={() => setStep(2)}>Back to placeholders</Button>
          </div>
        </Card>
      )}

      {step === 4 && template && version && (
        <Card title="Activate">
          <p className="mb-4 text-sm text-slate-600">Activating makes “{template.name}” (version {version.versionNumber}) available for document generation. You can save it as a draft instead and activate later.</p>
          <div className="flex gap-2">
            <Button variant="primary" loading={activateVersion.isPending} onClick={() => void activate()}>Activate template</Button>
            <Button onClick={() => { setSkipGuard(true); void navigate(`/templates/${template.id}`); }}>Save as draft</Button>
          </div>
        </Card>
      )}
    </>
  );
}
