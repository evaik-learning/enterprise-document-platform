import { gatewayClient } from '../gatewayClient';
import {
  DEFAULT_PAGE_SIZE,
  toPaged,
  type CreateDocumentRequest,
  type DocumentDetail,
  type DocumentFilters,
  type DocumentGeneration,
  type DocumentSummary,
  type DocumentWorkspace,
  type GenerateDocumentRequest,
  type Paged,
  type TimelineEvent
} from '../contracts';
import type { AuditLog } from '../contracts';

const base = '/api/v1/documents';

export const documentApi = {
  async list(filters: DocumentFilters = {}): Promise<Paged<DocumentSummary>> {
    const page = filters.page ?? 1;
    const pageSize = filters.pageSize ?? DEFAULT_PAGE_SIZE;
    const raw = await gatewayClient.get<unknown>(base, { query: { search: filters.search, status: filters.status, templateId: filters.templateId, page, pageSize } });
    return toPaged<DocumentSummary>(raw, page, pageSize);
  },
  get: (id: string) => gatewayClient.get<DocumentDetail>(`${base}/${id}`),
  create: (body: CreateDocumentRequest) => gatewayClient.post<DocumentDetail>(base, body),
  /** Generation may be synchronous (Completed) or asynchronous (Queued/Processing); callers poll the document status. */
  generate: (id: string, body: GenerateDocumentRequest) => gatewayClient.post<DocumentGeneration>(`${base}/${id}/generate`, body),
  download: (id: string, fileType?: string) => gatewayClient.download(`${base}/${id}/download`, { query: { fileType } }),
  workspace: (id: string) => gatewayClient.get<DocumentWorkspace>(`${base}/${id}/workspace`),
  timeline: (id: string) => gatewayClient.get<{ items: TimelineEvent[]; unavailableSections: string[] }>(`${base}/${id}/timeline`),
  audit: (id: string) => gatewayClient.get<{ items: AuditLog[] }>(`${base}/${id}/audit`)
};
