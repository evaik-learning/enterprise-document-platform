import { apiDownload, apiGet, apiSend } from './apiClient';

export interface Paged<T> { items: T[]; page: number; pageSize: number; totalCount?: number; hasNextPage?: boolean; }
export interface Template { id: string; name: string; code: string; description?: string; status: string; currentVersionId?: string; createdAt: string; modifiedAt?: string; rowVersion: string; }
export interface Placeholder { id: string; name: string; displayName?: string; dataType: string; isRequired: boolean; defaultValue?: string; format?: string; description?: string; occurrences: number; }
export interface TemplateVersion { id: string; templateId: string; versionNumber: number; fileName: string; contentType: string; fileSize: number; validationStatus: string; status: string; changeDescription?: string; createdAt: string; placeholders: Placeholder[]; }
export interface DocumentSummary { id: string; organizationId: string; name: string; documentType: string; status: string; templateId: string; currentVersionNumber: number; createdAt: string; }
export interface DocumentDetail extends DocumentSummary { description?: string; data: Record<string, unknown>; versions: DocumentVersion[]; }
export interface DocumentVersion { id: string; versionNumber: number; status: string; templateId: string; templateVersion: number; generatedAt: string; files: DocumentFile[]; }
export interface DocumentFile { id: string; fileType: string; fileName: string; contentType: string; size: number; storagePath: string; }
export interface DocumentGeneration { documentId: string; jobId: string; status: string; message: string; }

export const platformApi = {
  templates: {
    list: (query = '') => apiGet<Paged<Template> | Template[]>(`/api/v1/templates${query}`),
    create: (body: { name: string; code: string; description?: string }) => apiSend<Template>('/api/v1/templates', 'POST', body),
    versions: (id: string) => apiGet<TemplateVersion[]>(`/api/v1/templates/${id}/versions`),
    upload: (id: string, file: File, changeDescription?: string) => {
      const form = new FormData(); form.append('file', file); if (changeDescription) form.append('changeDescription', changeDescription);
      return apiSend<TemplateVersion>(`/api/v1/templates/${id}/versions`, 'POST', form);
    },
    validate: (templateId: string, versionId: string) => apiSend<unknown>(`/api/v1/templates/${templateId}/versions/${versionId}/validate`, 'POST'),
    activate: (templateId: string, versionId: string) => apiSend<void>(`/api/v1/templates/${templateId}/versions/${versionId}/activate`, 'POST'),
    download: (templateId: string, versionId: string) => apiDownload(`/api/v1/templates/${templateId}/versions/${versionId}/download`)
  },
  documents: {
    list: (query = '') => apiGet<Paged<DocumentSummary> | DocumentSummary[]>(`/api/v1/documents${query}`),
    get: (id: string) => apiGet<DocumentDetail>(`/api/v1/documents/${id}`),
    create: (body: { name: string; documentType: string; description?: string; templateId: string; templateVersion: number; data: Record<string, unknown> }) => apiSend<DocumentDetail>('/api/v1/documents', 'POST', body),
    generate: (id: string, body: { name: string; outputFormats: string[]; data: Record<string, unknown> }) => apiSend<DocumentGeneration>(`/api/v1/documents/${id}/generate`, 'POST', body),
    download: (id: string, fileType?: string) => apiDownload(`/api/v1/documents/${id}/download${fileType ? `?fileType=${encodeURIComponent(fileType)}` : ''}`)
  }
};

export function items<T>(result: Paged<T> | T[]) { return Array.isArray(result) ? result : result.items; }
