import { useState } from 'react';
import { Link, useParams } from 'react-router';
import type { Signer, SigningRequestDetail } from '../../api/contracts';
import { toUserMessage } from '../../api/errors';
import { useSignatureMutations, useSignatureRequest } from '../../api/hooks/usePlatformQueries';
import { Permissions } from '../../auth/permissions';
import { PermissionGate } from '../../auth/PermissionGate';
import { useAuth } from '../../auth/useAuth';
import { ConfirmDialog } from '../../components/dialogs/ConfirmDialog';
import { Alert } from '../../components/feedback/Feedback';
import { QueryBoundary } from '../../components/feedback/QueryBoundary';
import { useToast } from '../../components/feedback/Toast';
import { TextareaField, TextField } from '../../components/forms/Fields';
import { StatusBadge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card, PageHeader } from '../../components/ui/Card';
import { formatDate, formatDateTime } from '../../utils/format';
import { SignatureProgress } from './SignatureProgress';

const OPEN = ['Pending', 'InProgress', 'PartiallySigned'];
const DONE_SIGNER = ['signed', 'declined'];

/** Display-only hint: the Signature service enforces order and eligibility. */
export function isWaitingForEarlierSigner(request: SigningRequestDetail, signer: Signer) {
  return request.signingMode === 'Sequential' && request.signers.some((other) => other.signingOrder < signer.signingOrder && other.status.toLowerCase() !== 'signed');
}

function SignerExperience({ request, signer }: { request: SigningRequestDetail; signer: Signer }) {
  const { sign, decline } = useSignatureMutations();
  const toast = useToast();
  const [name, setName] = useState('');
  const [declining, setDeclining] = useState(false);
  const [reason, setReason] = useState('');
  const [error, setError] = useState<string | null>(null);
  const waiting = isWaitingForEarlierSigner(request, signer);
  const [today] = useState(() => new Date().toLocaleDateString([], { dateStyle: 'long' }));

  const submit = async () => {
    if (!name.trim()) { setError('Type your full name to sign.'); return; }
    setError(null);
    try { await sign.mutateAsync({ id: request.signingRequestId, signerId: signer.signerId, value: name.trim() }); toast.success('You have signed this document.'); }
    catch (cause) { setError(toUserMessage(cause)); }
  };

  return (
    <Card title="Your signature">
      <dl className="mb-4 grid gap-2 text-sm sm:grid-cols-2">
        <div><dt className="text-slate-600">Document</dt><dd className="font-medium"><Link className="text-teal-900 underline" to={`/documents/${request.documentId}?tab=preview`}>Open document</Link></dd></div>
        <div><dt className="text-slate-600">Expires</dt><dd className="font-medium">{request.expiresAt ? formatDate(request.expiresAt) : 'No expiry'}</dd></div>
        <div><dt className="text-slate-600">Required action</dt><dd className="font-medium">Sign as {signer.role}</dd></div>
      </dl>
      {request.message && <p className="mb-4 rounded-lg bg-slate-50 p-3 text-sm">{request.message}</p>}
      {waiting && <div className="mb-4"><Alert tone="info">Waiting for earlier signers to complete before you can sign.</Alert></div>}
      {error && <div className="mb-4"><Alert tone="error">{error}</Alert></div>}
      <div className="grid gap-4 sm:grid-cols-2">
        <TextField label="Name" required value={name} onChange={(event) => setName(event.target.value)} help={`Type “${signer.displayName}” to sign.`} disabled={waiting} />
        <TextField label="Date" value={today} readOnly />
      </div>
      <div className="mt-4 flex gap-2">
        <Button variant="primary" loading={sign.isPending} disabled={waiting} onClick={() => void submit()}>Sign</Button>
        <Button variant="ghost" disabled={sign.isPending} onClick={() => setDeclining(true)}>Decline</Button>
      </div>
      <ConfirmDialog open={declining} title="Decline to sign?" description="The requester will be told you declined. This cannot be undone." confirmLabel="Decline" danger onClose={() => setDeclining(false)}
        onConfirm={async () => { await decline.mutateAsync({ id: request.signingRequestId, signerId: signer.signerId, reason: reason.trim() || undefined }); toast.info('You declined this request.'); }}>
        <TextareaField label="Reason (optional)" value={reason} onChange={(event) => setReason(event.target.value)} maxLength={500} />
      </ConfirmDialog>
    </Card>
  );
}

export function SignatureRequestPage() {
  const { signingRequestId = '' } = useParams();
  const query = useSignatureRequest(signingRequestId);
  const { me } = useAuth();
  const { activate, cancel, resend } = useSignatureMutations();
  const toast = useToast();
  const [cancelling, setCancelling] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const run = async (action: () => Promise<unknown>, success: string) => {
    setError(null);
    try { await action(); toast.success(success); } catch (cause) { setError(toUserMessage(cause)); }
  };

  return (
    <QueryBoundary query={query} loadingLabel="Loading signature request" loadingRows={6}>
      {(request) => {
        const mine = request.signers.find((signer) => !!me?.email && signer.email.toLowerCase() === me.email.toLowerCase());
        const canSign = !!mine && OPEN.includes(request.status) && !DONE_SIGNER.includes(mine.status.toLowerCase());
        const open = OPEN.includes(request.status);
        return (
          <>
            <p className="mb-2"><Link to="/signatures" className="text-sm font-medium text-teal-800 underline">Back to signatures</Link></p>
            <PageHeader eyebrow={`${request.signingMode} signing`} title={request.title} description={request.message ?? undefined}
              actions={<><StatusBadge status={request.status} /><Link to={`/documents/${request.documentId}`} className="text-sm font-medium text-teal-800 underline">Open document</Link></>} />
            {error && <div className="mb-4"><Alert tone="error" onClose={() => setError(null)}>{error}</Alert></div>}

            <div className="grid gap-6 lg:grid-cols-2">
              <div className="space-y-6">
                {canSign && mine && <SignerExperience request={request} signer={mine} />}
                <Card title="Status"><SignatureProgress request={request} /></Card>
              </div>
              <div className="space-y-6">
                <Card title="Signers">
                  <ul className="divide-y divide-slate-100 text-sm">
                    {[...request.signers].sort((a, b) => a.signingOrder - b.signingOrder).map((signer) => (
                      <li key={signer.signerId} className="flex flex-wrap items-center justify-between gap-2 py-2.5">
                        <span><span className="font-medium">{signer.signingOrder}. {signer.displayName}</span><span className="block text-xs text-slate-600">{signer.email} · {signer.role}</span></span>
                        <span className="flex items-center gap-2">
                          <StatusBadge status={signer.status} />
                          <PermissionGate permission={Permissions.SignatureExecute}>
                            {open && !DONE_SIGNER.includes(signer.status.toLowerCase()) && <Button size="sm" variant="ghost" aria-label={`Resend invitation to ${signer.displayName}`} onClick={() => void run(() => resend.mutateAsync({ id: request.signingRequestId, signerId: signer.signerId }), 'Invitation resent.')}>Resend</Button>}
                          </PermissionGate>
                        </span>
                      </li>
                    ))}
                  </ul>
                </Card>
                <PermissionGate permission={Permissions.SignatureExecute}>
                  {(request.status === 'Draft' || open) && (
                    <Card title="Manage request">
                      <div className="flex flex-wrap gap-2">
                        {request.status === 'Draft' && <Button variant="primary" loading={activate.isPending} onClick={() => void run(() => activate.mutateAsync(request.signingRequestId), 'Signature request sent.')}>Send request</Button>}
                        <Button variant="danger" onClick={() => setCancelling(true)}>Cancel request</Button>
                      </div>
                      <p className="mt-3 text-xs text-slate-600">Created {formatDateTime(request.createdAt)}{request.expiresAt ? ` · expires ${formatDate(request.expiresAt)}` : ''}</p>
                    </Card>
                  )}
                </PermissionGate>
              </div>
            </div>
            <ConfirmDialog open={cancelling} title="Cancel signature request?" description="Signers will no longer be able to sign. This cannot be undone." confirmLabel="Cancel request" cancelLabel="Keep request" danger onClose={() => setCancelling(false)}
              onConfirm={async () => { await cancel.mutateAsync(request.signingRequestId); toast.success('Signature request cancelled.'); }} />
          </>
        );
      }}
    </QueryBoundary>
  );
}
