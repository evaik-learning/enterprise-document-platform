import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import type { DocumentDetail } from '../../api/contracts';
import { templateApi } from '../../api/endpoints';
import { toUserMessage } from '../../api/errors';
import { useDocumentMutations } from '../../api/hooks/useDocuments';
import { useQueryKeys } from '../../api/queryKeys';
import { Dialog } from '../../components/dialogs/Dialog';
import { Alert } from '../../components/feedback/Feedback';
import { LoadingSkeleton } from '../../components/feedback/LoadingSkeleton';
import { useToast } from '../../components/feedback/Toast';
import { FormErrorSummary } from '../../components/forms/Fields';
import { Button } from '../../components/ui/Button';
import { isUsableVersion } from './GenerateDocumentPage';
import { buildGenerationData, initialValues, PlaceholderValueFields, type FieldValues } from './PlaceholderValueFields';

/** Re-run generation for an existing document (e.g. after a failure) by supplying the values again. */
export function RegenerateDialog({ document, onClose }: { document: DocumentDetail; onClose: () => void }) {
  const keys = useQueryKeys();
  const { generate } = useDocumentMutations();
  const toast = useToast();
  const versions = useQuery({ queryKey: keys.templates.versions(document.templateId), queryFn: () => templateApi.versions(document.templateId) });
  const version = useMemo(() => (versions.data ?? []).filter(isUsableVersion)[0], [versions.data]);
  const [values, setValues] = useState<{ versionKey: string; values: FieldValues } | null>(null);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const currentValues: FieldValues = version ? (values?.versionKey === version.id ? values.values : initialValues(version.placeholders)) : {};

  const submit = async () => {
    if (!version) return;
    const built = buildGenerationData(version.placeholders, currentValues);
    setErrors(built.errors);
    if (Object.keys(built.errors).length > 0) return;
    setError(null);
    try {
      await generate.mutateAsync({ id: document.id, body: { name: document.name, outputFormats: ['DOCX', 'PDF'], data: built.data } });
      toast.success('Document generation started.');
      onClose();
    } catch (cause) {
      setError(toUserMessage(cause));
    }
  };

  return (
    <Dialog open onClose={onClose} size="lg" title="Generate document" description="Enter the placeholder values again to generate this document." dismissible={!generate.isPending}
      footer={<><Button onClick={onClose} disabled={generate.isPending}>Cancel</Button><Button variant="primary" disabled={!version} loading={generate.isPending} onClick={() => void submit()}>Generate</Button></>}>
      <div className="space-y-4">
        {error && <Alert tone="error">{error}</Alert>}
        <FormErrorSummary errors={Object.values(errors)} />
        {versions.isPending && <LoadingSkeleton rows={3} label="Loading template" />}
        {versions.isSuccess && !version && <Alert tone="warning">The template has no active, validated version.</Alert>}
        {version && <PlaceholderValueFields placeholders={version.placeholders} values={currentValues} errors={errors} disabled={generate.isPending} onChange={(name, value) => setValues({ versionKey: version.id, values: { ...currentValues, [name]: value } })} />}
      </div>
    </Dialog>
  );
}
