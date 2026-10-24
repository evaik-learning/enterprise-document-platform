export type DocumentStatus =
  | 'Requested' | 'Queued' | 'Generating' | 'Generated' | 'Failed' | 'Draft' | 'InReview' | 'PendingApproval' | 'Approved'
  | 'Rejected' | 'PendingSignature' | 'Signed' | 'Completed' | 'Archived' | 'Cancelled' | 'Deleted';

export interface DocumentSummary {
  id: string;
  organizationId: string;
  name: string;
  documentType: string;
  status: DocumentStatus | string;
  templateId: string;
  currentVersionNumber: number;
  createdAt: string;
}

export interface DocumentFile {
  id: string;
  fileType: string;
  fileName: string;
  contentType: string;
  size: number;
}

export interface DocumentVersion {
  id: string;
  versionNumber: number;
  status: string;
  templateId: string;
  templateVersion: number;
  generatedAt: string;
  files: DocumentFile[];
}

export interface DocumentDetail extends DocumentSummary {
  description?: string;
  versions: DocumentVersion[];
}

export interface DocumentGeneration {
  documentId: string;
  jobId: string;
  status: string;
  message: string;
}

export interface CreateDocumentRequest {
  name: string;
  documentType: string;
  description?: string;
  templateId: string;
  templateVersion: number;
}

export interface GenerateDocumentRequest {
  name: string;
  outputFormats: string[];
  data: Record<string, unknown>;
}

export interface DocumentFilters {
  search?: string;
  status?: string;
  templateId?: string;
  page?: number;
  pageSize?: number;
}

/** Statuses during which generation is still running and the UI should keep refreshing. */
export const PROCESSING_DOCUMENT_STATUSES: string[] = ['Queued', 'Generating'];
