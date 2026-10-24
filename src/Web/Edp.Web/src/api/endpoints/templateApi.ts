import { gatewayClient } from '../gatewayClient';
import {
  DEFAULT_PAGE_SIZE,
  toPaged,
  type Paged,
  type Placeholder,
  type PlaceholderDiscoveryResult,
  type PlaceholderInput,
  type PlaceholderUpdate,
  type Template,
  type TemplateFilters,
  type TemplateVersion,
  type ValidationResult
} from '../contracts';
import { uploadWithProgress } from '../httpClient';

const base = '/api/v1/templates';

export const templateApi = {
  async list(filters: TemplateFilters = {}): Promise<Paged<Template>> {
    const page = filters.page ?? 1;
    const pageSize = filters.pageSize ?? DEFAULT_PAGE_SIZE;
    const raw = await gatewayClient.get<unknown>(base, { query: { search: filters.search, status: filters.status, page, pageSize } });
    return toPaged<Template>(raw, page, pageSize);
  },
  get: (id: string) => gatewayClient.get<Template>(`${base}/${id}`),
  create: (body: { name: string; code: string; description?: string }) => gatewayClient.post<Template>(base, body),
  update: (id: string, body: { name: string; description?: string; rowVersion: string }) => gatewayClient.put<Template>(`${base}/${id}`, body),
  deactivate: (id: string) => gatewayClient.post<Template>(`${base}/${id}/deactivate`),
  archive: (id: string) => gatewayClient.post<Template>(`${base}/${id}/archive`),

  versions: (id: string) => gatewayClient.get<TemplateVersion[]>(`${base}/${id}/versions`),
  version: (templateId: string, versionId: string) => gatewayClient.get<TemplateVersion>(`${base}/${templateId}/versions/${versionId}`),
  uploadVersion: (templateId: string, file: File, changeDescription?: string, onProgress?: (percent: number) => void, signal?: AbortSignal) => {
    const form = new FormData();
    form.append('file', file);
    if (changeDescription) form.append('changeDescription', changeDescription);
    return uploadWithProgress<TemplateVersion>(`${base}/${templateId}/versions`, form, onProgress, signal);
  },
  validate: (templateId: string, versionId: string) => gatewayClient.post<unknown>(`${base}/${templateId}/versions/${versionId}/validate`),
  validation: (templateId: string, versionId: string) => gatewayClient.get<ValidationResult>(`${base}/${templateId}/versions/${versionId}/validation`),
  activateVersion: (templateId: string, versionId: string) => gatewayClient.post<void>(`${base}/${templateId}/versions/${versionId}/activate`),
  downloadVersion: (templateId: string, versionId: string) => gatewayClient.download(`${base}/${templateId}/versions/${versionId}/download`)
};

const placeholders = (templateId: string, versionId: string) => `${base}/${templateId}/versions/${versionId}/placeholders`;

export const placeholderApi = {
  list: (templateId: string, versionId: string) => gatewayClient.get<Placeholder[]>(placeholders(templateId, versionId)),
  create: (templateId: string, versionId: string, body: PlaceholderInput) => gatewayClient.post<Placeholder>(placeholders(templateId, versionId), body),
  update: (templateId: string, versionId: string, id: string, body: PlaceholderUpdate) => gatewayClient.put<Placeholder>(`${placeholders(templateId, versionId)}/${id}`, body),
  remove: (templateId: string, versionId: string, id: string) => gatewayClient.delete(`${placeholders(templateId, versionId)}/${id}`),
  discover: (templateId: string, versionId: string) => gatewayClient.post<PlaceholderDiscoveryResult>(`${placeholders(templateId, versionId)}/discover`),
  validate: (templateId: string, versionId: string) => gatewayClient.post<ValidationResult>(`${placeholders(templateId, versionId)}/validate`)
};
