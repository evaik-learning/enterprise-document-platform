import type { SigningRequestDetail } from '../../api/contracts';
import { Timeline, type TimelineItem } from '../../components/timeline/Timeline';
import { humanize } from '../../utils/status';

/** Signature progress derived from backend-reported signer states; the Signature service is the source of truth. */
export function signatureProgressItems(request: SigningRequestDetail): TimelineItem[] {
  const items: TimelineItem[] = [{ id: 'created', title: 'Request created', timestamp: request.createdAt, state: 'completed' }];
  const signers = [...request.signers].sort((a, b) => a.signingOrder - b.signingOrder);
  const failedRequest = ['Declined', 'Cancelled', 'Expired', 'Failed'].includes(request.status);
  let currentMarked = false;
  for (const signer of signers) {
    const status = signer.status.toLowerCase();
    const signed = status === 'signed' || !!signer.signedAt;
    const declined = status === 'declined' || !!signer.declinedAt;
    if (request.activatedAt || signer.invitedAt) items.push({ id: `sent-${signer.signerId}`, title: `Sent to ${signer.displayName}`, timestamp: signer.invitedAt ?? request.activatedAt, state: 'completed' });
    if (signed) items.push({ id: `signed-${signer.signerId}`, title: `${signer.displayName} signed`, timestamp: signer.signedAt, state: 'completed' });
    else if (declined) items.push({ id: `declined-${signer.signerId}`, title: `${signer.displayName} declined`, description: signer.declineReason, timestamp: signer.declinedAt, state: 'failed' });
    else {
      const isCurrent: boolean = !currentMarked && !failedRequest && request.status !== 'Draft';
      if (isCurrent) currentMarked = true;
      items.push({ id: `awaiting-${signer.signerId}`, title: `Awaiting ${signer.displayName}`, description: humanize(signer.role), state: isCurrent ? 'current' : 'upcoming' });
    }
  }
  items.push({ id: 'completed', title: request.status === 'Completed' ? 'Completed' : failedRequest ? humanize(request.status) : 'Completed', timestamp: request.completedAt, state: request.status === 'Completed' ? 'completed' : failedRequest ? 'failed' : 'upcoming' });
  return items;
}

export function SignatureProgress({ request }: { request: SigningRequestDetail }) {
  return <Timeline label="Signature progress" items={signatureProgressItems(request)} />;
}
