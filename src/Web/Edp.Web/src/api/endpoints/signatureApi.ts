import { gatewayClient } from '../gatewayClient';
import {
  DEFAULT_PAGE_SIZE,
  toPaged,
  type CreateSigningRequestInput,
  type Paged,
  type SigningFilters,
  type SigningRequestDetail,
  type SigningRequestSummary
} from '../contracts';

const base = '/api/v1/signing-requests';
// The signing API consumes JSON on every route, so body-less commands send an empty object.
const empty = {};

export const signatureApi = {
  async list(filters: SigningFilters = {}): Promise<Paged<SigningRequestSummary>> {
    const page = filters.page ?? 1;
    const pageSize = filters.pageSize ?? DEFAULT_PAGE_SIZE;
    const raw = await gatewayClient.get<unknown>(base, { query: { status: filters.status, documentId: filters.documentId, page, pageSize } });
    return toPaged<SigningRequestSummary>(raw, page, pageSize);
  },
  get: (id: string) => gatewayClient.get<SigningRequestDetail>(`${base}/${id}`),
  create: (input: CreateSigningRequestInput) =>
    gatewayClient.post<SigningRequestSummary>(base, {
      workflowInstanceId: input.workflowInstanceId ?? '00000000-0000-0000-0000-000000000000',
      documentId: input.documentId,
      documentVersionId: input.documentVersionId,
      signingMode: input.signingMode,
      title: input.title,
      message: input.message,
      expiresAt: input.expiresAt,
      provider: 'LocalDemo',
      signers: input.signers,
      signatureFields: []
    }),
  activate: (id: string) => gatewayClient.post<void>(`${base}/${id}/activate`, empty),
  cancel: (id: string) => gatewayClient.post<void>(`${base}/${id}/cancel`, empty),
  /** `signatureValue` is the signer's typed name; no signing credentials are ever held in the browser. */
  sign: (id: string, signerId: string, signatureValue: string) => gatewayClient.post<void>(`${base}/${id}/sign/${signerId}`, { signatureValue }),
  decline: (id: string, signerId: string, reason?: string) => gatewayClient.post<void>(`${base}/${id}/decline/${signerId}`, empty, { query: { reason } }),
  resendInvitation: (id: string, signerId: string) => gatewayClient.post<void>(`${base}/${id}/resend-invitation/${signerId}`, empty)
};
