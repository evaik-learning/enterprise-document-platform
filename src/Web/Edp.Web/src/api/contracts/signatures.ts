export type SigningMode = 'Sequential' | 'Parallel';
export type SigningRequestStatus = 'Draft' | 'Pending' | 'InProgress' | 'PartiallySigned' | 'Completed' | 'Declined' | 'Expired' | 'Cancelled' | 'Failed';
export type SignerStatus = string;

export interface SigningRequestSummary {
  signingRequestId: string;
  title: string;
  status: SigningRequestStatus | string;
  signingMode?: SigningMode | string;
  signerCount: number;
  signedCount: number;
  createdAt: string;
  expiresAt?: string;
}

export interface Signer {
  signerId: string;
  email: string;
  displayName: string;
  role: string;
  status: SignerStatus;
  signingOrder: number;
  invitedAt?: string;
  signedAt?: string;
  declinedAt?: string;
  declineReason?: string;
}

export interface SignatureField {
  signatureFieldId: string;
  signerId: string;
  documentPage: number;
  fieldType: string;
  required: boolean;
  label?: string;
  completedAt?: string;
}

export interface SignatureAuditEntry {
  signatureActionId: string;
  signerId: string;
  signerEmail: string;
  actionType: string;
  occurredAt: string;
}

export interface SigningRequestDetail {
  signingRequestId: string;
  workflowInstanceId: string;
  documentId: string;
  title: string;
  message?: string;
  status: SigningRequestStatus | string;
  signingMode: SigningMode | string;
  createdAt: string;
  expiresAt?: string;
  activatedAt?: string;
  completedAt?: string;
  signers: Signer[];
  signatureFields: SignatureField[];
  auditTrail: SignatureAuditEntry[];
}

export interface SignerInput {
  email: string;
  displayName: string;
  role: string;
  signingOrder: number;
  isRequired: boolean;
}

export interface CreateSigningRequestInput {
  documentId: string;
  documentVersionId: string;
  workflowInstanceId?: string;
  title: string;
  message?: string;
  signingMode: SigningMode;
  expiresAt?: string;
  signers: SignerInput[];
}

export interface SigningFilters {
  status?: string;
  documentId?: string;
  page?: number;
  pageSize?: number;
}
