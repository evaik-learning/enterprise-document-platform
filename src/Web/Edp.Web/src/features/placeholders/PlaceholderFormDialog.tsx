import { useState, type FormEvent } from 'react';
import { ApiError, toUserMessage } from '../../api/errors';
import { PLACEHOLDER_DATA_TYPES, type Placeholder } from '../../api/contracts';
import { usePlaceholderMutations } from '../../api/hooks/useTemplates';
import { Dialog } from '../../components/dialogs/Dialog';
import { CheckboxField, FormErrorSummary, SelectField, TextareaField, TextField } from '../../components/forms/Fields';
import { Alert } from '../../components/feedback/Feedback';
import { Button } from '../../components/ui/Button';
import { useToast } from '../../components/feedback/Toast';

interface PlaceholderFormDialogProps {
  open: boolean;
  onClose: () => void;
  templateId: string;
  versionId: string;
  /** Existing placeholder when editing; omitted when creating. */
  placeholder?: Placeholder;
  initialName?: string;
}

const NAME_PATTERN = /^[A-Za-z][A-Za-z0-9_]*$/;

export function validatePlaceholderForm(values: { name: string; displayName: string; defaultValue: string; format: string; description: string }, creating: boolean) {
  const errors: Record<string, string> = {};
  if (creating) {
    if (!values.name.trim()) errors.name = 'Name is required.';
    else if (!NAME_PATTERN.test(values.name.trim())) errors.name = 'Use letters, numbers and underscores, starting with a letter.';
    else if (values.name.length > 200) errors.name = 'Name must be 200 characters or fewer.';
  }
  if (values.displayName.length > 200) errors.displayName = 'Display name must be 200 characters or fewer.';
  if (values.defaultValue.length > 1000) errors.defaultValue = 'Default value must be 1000 characters or fewer.';
  if (values.format.length > 200) errors.format = 'Format must be 200 characters or fewer.';
  if (values.description.length > 1000) errors.description = 'Description must be 1000 characters or fewer.';
  return errors;
}

/** Create/edit form. Client checks give fast feedback; the Template service remains the authority. */
export function PlaceholderFormDialog({ open, onClose, templateId, versionId, placeholder, initialName }: PlaceholderFormDialogProps) {
  const creating = !placeholder;
  const mutations = usePlaceholderMutations();
  const toast = useToast();
  const [name, setName] = useState(placeholder?.name ?? initialName ?? '');
  const [displayName, setDisplayName] = useState(placeholder?.displayName ?? '');
  const [dataType, setDataType] = useState<string>(placeholder?.dataType ?? 'String');
  const [isRequired, setIsRequired] = useState(placeholder?.isRequired ?? true);
  const [defaultValue, setDefaultValue] = useState(placeholder?.defaultValue ?? '');
  const [format, setFormat] = useState(placeholder?.format ?? '');
  const [description, setDescription] = useState(placeholder?.description ?? '');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [serverError, setServerError] = useState<string | null>(null);

  const pending = mutations.create.isPending || mutations.update.isPending;

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validatePlaceholderForm({ name, displayName, defaultValue, format, description }, creating);
    setErrors(found);
    setServerError(null);
    if (Object.keys(found).length > 0) return;
    const body = { displayName: displayName.trim() || undefined, dataType, isRequired, defaultValue: defaultValue || undefined, format: format.trim() || undefined, description: description.trim() || undefined };
    try {
      if (placeholder) await mutations.update.mutateAsync({ templateId, versionId, id: placeholder.id, body });
      else await mutations.create.mutateAsync({ templateId, versionId, body: { ...body, name: name.trim(), occurrences: 0 } });
      toast.success(creating ? `Placeholder “${name.trim()}” created.` : `Placeholder “${placeholder?.name}” updated.`);
      onClose();
    } catch (cause) {
      if (cause instanceof ApiError && Object.keys(cause.fieldErrors()).length > 0) {
        const fields = cause.fieldErrors();
        setErrors({ name: fields.name ?? '', displayName: fields.displayname ?? '', dataType: fields.datatype ?? '', defaultValue: fields.defaultvalue ?? '', format: fields.format ?? '', description: fields.description ?? '' });
      }
      setServerError(toUserMessage(cause));
    }
  };

  const summary = Object.values(errors).filter(Boolean);

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={creating ? 'New placeholder' : `Edit ${placeholder?.name}`}
      description="Placeholders are filled in when a document is generated from this template version."
      dismissible={!pending}
      footer={
        <>
          <Button onClick={onClose} disabled={pending}>Cancel</Button>
          <Button variant="primary" type="submit" form="placeholder-form" loading={pending}>{creating ? 'Create placeholder' : 'Save changes'}</Button>
        </>
      }
    >
      <form id="placeholder-form" onSubmit={(event) => void submit(event)} noValidate className="space-y-4">
        <FormErrorSummary errors={summary} />
        {serverError && <Alert tone="error">{serverError}</Alert>}
        <TextField label="Name" required disabled={!creating} value={name} onChange={(event) => setName(event.target.value)} error={errors.name} help={creating ? 'Must match the placeholder text in the template, e.g. CustomerName.' : 'The name cannot be changed.'} maxLength={200} />
        <TextField label="Display name" value={displayName} onChange={(event) => setDisplayName(event.target.value)} error={errors.displayName} maxLength={200} />
        <SelectField label="Data type" required value={dataType} onChange={(event) => setDataType(event.target.value)} error={errors.dataType}>
          {PLACEHOLDER_DATA_TYPES.map((type) => <option key={type} value={type}>{type}</option>)}
        </SelectField>
        <CheckboxField label="Required" help="Document generation will require a value." checked={isRequired} onChange={(event) => setIsRequired(event.target.checked)} />
        <TextField label="Default value" value={defaultValue} onChange={(event) => setDefaultValue(event.target.value)} error={errors.defaultValue} />
        <TextField label="Format" value={format} onChange={(event) => setFormat(event.target.value)} error={errors.format} help="Optional display/validation format, e.g. dd/MM/yyyy." />
        <TextareaField label="Description" value={description} onChange={(event) => setDescription(event.target.value)} error={errors.description} />
      </form>
    </Dialog>
  );
}
