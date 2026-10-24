import { useRef, useState } from 'react';
import { toUserMessage } from '../../api/errors';
import type { DocumentDetail, SignerInput, SigningMode } from '../../api/contracts';
import { useSignatureMutations } from '../../api/hooks/usePlatformQueries';
import { Dialog } from '../../components/dialogs/Dialog';
import { Alert } from '../../components/feedback/Feedback';
import { useToast } from '../../components/feedback/Toast';
import { CheckboxField, DateField, FormErrorSummary, RadioGroup, TextareaField, TextField } from '../../components/forms/Fields';
import { Button } from '../../components/ui/Button';
import { formatDate } from '../../utils/format';

interface SignerRow { key: number; displayName: string; email: string; role: string; isRequired: boolean }

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function validateSigningForm(input: { title: string; signers: Array<Pick<SignerRow, 'displayName' | 'email'>>; expiresOn: string }, today = new Date()) {
  const errors: string[] = [];
  if (!input.title.trim()) errors.push('A title is required.');
  if (input.signers.length === 0) errors.push('Add at least one signer.');
  const seen = new Set<string>();
  input.signers.forEach((signer, index) => {
    const label = `Signer ${index + 1}`;
    if (!signer.displayName.trim()) errors.push(`${label}: name is required.`);
    if (!EMAIL.test(signer.email.trim())) errors.push(`${label}: a valid email is required.`);
    const email = signer.email.trim().toLowerCase();
    if (email && seen.has(email)) errors.push(`${label}: duplicate email address.`);
    seen.add(email);
  });
  if (input.expiresOn && new Date(`${input.expiresOn}T23:59:59Z`) < today) errors.push('The expiry date must be in the future.');
  return errors;
}

/** Signature preparation: configure signers and order, review, then create and send the request. */
export function SendForSignatureDialog({ document, onClose, onSent }: { document: DocumentDetail; onClose: () => void; onSent: (signingRequestId: string) => void }) {
  const { create, activate } = useSignatureMutations();
  const toast = useToast();
  const nextKey = useRef(1);
  const [step, setStep] = useState<'configure' | 'review'>('configure');
  const [title, setTitle] = useState(`Signature: ${document.name}`);
  const [message, setMessage] = useState('');
  const [mode, setMode] = useState<SigningMode>('Sequential');
  const [expiresOn, setExpiresOn] = useState('');
  const [signers, setSigners] = useState<SignerRow[]>([{ key: 0, displayName: '', email: '', role: 'Signer', isRequired: true }]);
  const [problems, setProblems] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [createdId, setCreatedId] = useState<string | null>(null);

  const latestVersion = [...document.versions].sort((a, b) => b.versionNumber - a.versionNumber)[0];
  const busy = create.isPending || activate.isPending;

  const update = (key: number, patch: Partial<SignerRow>) => setSigners((rows) => rows.map((row) => (row.key === key ? { ...row, ...patch } : row)));
  const move = (index: number, delta: number) => setSigners((rows) => { const next = [...rows]; const target = index + delta; if (target < 0 || target >= next.length) return rows; [next[index], next[target]] = [next[target]!, next[index]!]; return next; });

  const review = () => {
    const found = validateSigningForm({ title, signers, expiresOn });
    setProblems(found);
    if (found.length === 0) setStep('review');
  };

  const send = async () => {
    if (!latestVersion) { setError('The document has no generated version to sign.'); return; }
    setError(null);
    try {
      let id = createdId;
      if (!id) {
        const input: SignerInput[] = signers.map((signer, index) => ({ displayName: signer.displayName.trim(), email: signer.email.trim(), role: signer.role.trim() || 'Signer', signingOrder: index + 1, isRequired: signer.isRequired }));
        const created = await create.mutateAsync({ documentId: document.id, documentVersionId: latestVersion.id, title: title.trim(), message: message.trim() || undefined, signingMode: mode, expiresAt: expiresOn ? `${expiresOn}T23:59:59Z` : undefined, signers: input });
        id = created.signingRequestId;
        setCreatedId(id);
      }
      await activate.mutateAsync(id);
      toast.success('Signature request sent.');
      onSent(id);
      onClose();
    } catch (cause) {
      setError(toUserMessage(cause));
    }
  };

  return (
    <Dialog open onClose={onClose} size="lg" title="Send for signature" description={step === 'configure' ? 'Choose who signs, in which order, and whether each signature is required.' : 'Review the request before it is sent.'} dismissible={!busy}
      footer={step === 'configure'
        ? <><Button onClick={onClose}>Cancel</Button><Button variant="primary" onClick={review}>Review</Button></>
        : <><Button onClick={() => setStep('configure')} disabled={busy || !!createdId}>Back</Button><Button variant="primary" loading={busy} onClick={() => void send()}>{createdId ? 'Retry sending' : 'Send for signature'}</Button></>}>
      <div className="space-y-4">
        {error && <Alert tone="error">{error}{createdId && ' The request was created; use “Retry sending” to send it.'}</Alert>}
        {step === 'configure' ? (
          <>
            <FormErrorSummary errors={problems} />
            <TextField label="Title" required value={title} onChange={(event) => setTitle(event.target.value)} maxLength={250} />
            <TextareaField label="Message to signers" value={message} onChange={(event) => setMessage(event.target.value)} maxLength={1000} />
            <div className="grid gap-4 sm:grid-cols-2">
              <RadioGroup legend="Signing order" name="signing-mode" value={mode} onChange={setMode} options={[{ value: 'Sequential', label: 'Sequential', help: 'Signers sign one after another, in the order below.' }, { value: 'Parallel', label: 'Parallel', help: 'All signers can sign at the same time.' }]} />
              <DateField label="Expires on" value={expiresOn} onChange={(event) => setExpiresOn(event.target.value)} help="Optional." />
            </div>
            <fieldset className="space-y-3">
              <legend className="text-sm font-semibold">Signers</legend>
              {signers.map((signer, index) => (
                <div key={signer.key} className="grid gap-3 rounded-lg border border-slate-200 p-3 sm:grid-cols-[2rem_1fr_1fr_8rem]">
                  <span className="pt-7 text-sm font-semibold" aria-hidden="true">{index + 1}</span>
                  <TextField label={`Signer ${index + 1} name`} value={signer.displayName} onChange={(event) => update(signer.key, { displayName: event.target.value })} />
                  <TextField label={`Signer ${index + 1} email`} type="email" value={signer.email} onChange={(event) => update(signer.key, { email: event.target.value })} />
                  <TextField label={`Signer ${index + 1} role`} value={signer.role} onChange={(event) => update(signer.key, { role: event.target.value })} />
                  <div className="flex flex-wrap items-center gap-2 sm:col-span-4 sm:pl-8">
                    <CheckboxField label="Required" checked={signer.isRequired} onChange={(event) => update(signer.key, { isRequired: event.target.checked })} />
                    {mode === 'Sequential' && <><Button size="sm" variant="ghost" disabled={index === 0} aria-label={`Move signer ${index + 1} up`} onClick={() => move(index, -1)}>Move up</Button><Button size="sm" variant="ghost" disabled={index === signers.length - 1} aria-label={`Move signer ${index + 1} down`} onClick={() => move(index, 1)}>Move down</Button></>}
                    <Button size="sm" variant="ghost" disabled={signers.length === 1} aria-label={`Remove signer ${index + 1}`} onClick={() => setSigners((rows) => rows.filter((row) => row.key !== signer.key))}>Remove</Button>
                  </div>
                </div>
              ))}
              <Button size="sm" onClick={() => setSigners((rows) => [...rows, { key: nextKey.current++, displayName: '', email: '', role: 'Signer', isRequired: true }])}>Add signer</Button>
            </fieldset>
          </>
        ) : (
          <dl className="space-y-3 text-sm">
            <div><dt className="text-slate-600">Document</dt><dd className="font-medium">{document.name} (v{latestVersion?.versionNumber ?? '—'})</dd></div>
            <div><dt className="text-slate-600">Title</dt><dd className="font-medium">{title}</dd></div>
            <div><dt className="text-slate-600">Signing order</dt><dd className="font-medium">{mode}{expiresOn ? ` · expires ${formatDate(`${expiresOn}T00:00:00Z`)}` : ''}</dd></div>
            <div><dt className="text-slate-600">Signers</dt><dd><ol className="list-decimal pl-5">{signers.map((signer) => <li key={signer.key}>{signer.displayName} &lt;{signer.email}&gt; — {signer.role}{signer.isRequired ? '' : ' (optional)'}</li>)}</ol></dd></div>
          </dl>
        )}
      </div>
    </Dialog>
  );
}
