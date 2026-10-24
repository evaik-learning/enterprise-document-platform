export type UxStatus =
  | 'draft' | 'active' | 'inactive' | 'processing' | 'pending' | 'approved' | 'rejected'
  | 'signed' | 'completed' | 'failed' | 'cancelled' | 'expired';

const STATUS_MAP: Record<string, UxStatus> = {
  draft: 'draft', requested: 'draft', uploaded: 'draft', notvalidated: 'draft', new: 'draft',
  active: 'active', published: 'active', valid: 'active', enabled: 'active',
  inactive: 'inactive', archived: 'inactive', suspended: 'inactive', paused: 'inactive', deleted: 'inactive',
  queued: 'processing', generating: 'processing', processing: 'processing', inprogress: 'processing', sent: 'processing',
  pending: 'pending', awaitingapproval: 'pending', pendingapproval: 'pending', pendingsignature: 'pending', partiallysigned: 'pending', inreview: 'pending', delegated: 'pending', unread: 'pending',
  approved: 'approved', generated: 'approved',
  rejected: 'rejected', declined: 'rejected', invalid: 'rejected',
  signed: 'signed',
  completed: 'completed', read: 'completed',
  failed: 'failed', error: 'failed',
  cancelled: 'cancelled', canceled: 'cancelled',
  expired: 'expired'
};

/** Maps any backend status string to the shared UX status vocabulary. Unknown values render as `draft`. */
export function toUxStatus(raw: string | null | undefined): UxStatus {
  return STATUS_MAP[(raw ?? '').replace(/[\s_-]/g, '').toLowerCase()] ?? 'draft';
}

/** "PartiallySigned" -> "Partially signed". */
export function humanize(value: string | null | undefined) {
  if (!value) return '';
  const spaced = value.replace(/[_-]+/g, ' ').replace(/([a-z0-9])([A-Z])/g, '$1 $2').trim();
  return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
}

export const isProcessingStatus = (raw: string | null | undefined) => toUxStatus(raw) === 'processing';
