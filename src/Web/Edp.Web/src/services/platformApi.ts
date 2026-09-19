import { apiDownload, apiGet, apiSend } from './apiClient';

export interface Paged<T> { items: T[]; page: number; pageSize: number; totalCount?: number; hasNextPage?: boolean; }
export interface Template { id: string; name: string; code: string; description?: string; status: string; currentVersionId?: string; createdAt: string; modifiedAt?: string; rowVersion: string; }
export interface Placeholder { id: string; name: string; displayName?: string; dataType: string; isRequired: boolean; defaultValue?: string; format?: string; description?: string; occurrences: number; }
export interface TemplateVersion { id: string; templateId: string; versionNumber: number; fileName: string; contentType: string; fileSize: number; validationStatus: string; status: string; changeDescription?: string; createdAt: string; placeholders: Placeholder[]; }
export interface ValidationIssue { code: string; severity: string; message: string; location?: string; }
export interface ValidationResult { isValid: boolean; status: string; errorCount: number; warningCount: number; errors: ValidationIssue[]; warnings: ValidationIssue[]; validatedAt: string; }
export interface PlaceholderDiscoveryResult { templateVersionId: string; discovered: string[]; newPlaceholders: string[]; existingPlaceholders: string[]; missingFromDocument: string[]; status: string; }
export interface DocumentSummary { id: string; organizationId: string; name: string; documentType: string; status: string; templateId: string; currentVersionNumber: number; createdAt: string; }
export interface DocumentDetail extends DocumentSummary { description?: string; data: Record<string, unknown>; versions: DocumentVersion[]; }
export interface DocumentVersion { id: string; versionNumber: number; status: string; templateId: string; templateVersion: number; generatedAt: string; files: DocumentFile[]; }
export interface DocumentFile { id: string; fileType: string; fileName: string; contentType: string; size: number; storagePath: string; }
export interface DocumentGeneration { documentId: string; jobId: string; status: string; message: string; }
export interface Workflow { id: string; code: string; name: string; status: string; publishedVersion?: number; }
export interface WorkflowVersion { id: string; workflowId: string; version: number; isPublished: boolean; }
export interface WorkflowState { id: string; workflowVersionId: string; name: string; stateType: number; }
export interface WorkflowTransition { id: string; workflowVersionId: string; fromStateId: string; toStateId: string; order: number; }
export interface WorkflowGraph { states: WorkflowState[]; transitions: WorkflowTransition[]; }
export interface WorkflowInstance { id: string; workflowId: string; workflowVersion: number; documentId: string; status: string; currentStateId?: string; }
export interface ApprovalTask { id: string; workflowInstanceId: string; stateId: string; status: string; assignedToUserId: string; deadlineAt?: string; }
export interface WorkflowHistoryEntry { id: string; eventType: string; stateId?: string; approvalTaskId?: string; userId?: string; description: string; eventAt: string; }
export interface Notification { id: string; notificationType: string; subject: string; body: string; status: string; createdAtUtc: string; readAtUtc?: string; correlationId: string; }
export interface AuditLog { id: string; action: string; entityType?: string; entityId?: string; userId?: string; timestamp?: string; correlationId?: string; metadata?: Record<string, unknown>; }
export interface SigningRequest { signingRequestId: string; title: string; status: string; signerCount: number; signedCount: number; createdAt: string; expiresAt?: string; }
export interface Organization { id: string; name: string; description?: string; role: string; isActive: boolean; }
export interface OrganizationMember { userId: string; role: string; isActive: boolean; }
export interface OrganizationCapabilities { organizationId: string; userId: string; role: string; permissions: string[]; }

export const platformApi = {
  templates: {
    list: (query = '') => apiGet<Paged<Template> | Template[]>(`/api/v1/templates${query}`),
    get: (id: string) => apiGet<Template>(`/api/v1/templates/${id}`),
    create: (body: { name: string; code: string; description?: string }) => apiSend<Template>('/api/v1/templates', 'POST', body),
    update: (id: string, body: { name: string; description?: string; rowVersion: string }) => apiSend<Template>(`/api/v1/templates/${id}`, 'PUT', body),
    versions: (id: string) => apiGet<TemplateVersion[]>(`/api/v1/templates/${id}/versions`),
    version: (templateId: string, versionId: string) => apiGet<TemplateVersion>(`/api/v1/templates/${templateId}/versions/${versionId}`),
    upload: (id: string, file: File, changeDescription?: string) => {
      const form = new FormData(); form.append('file', file); if (changeDescription) form.append('changeDescription', changeDescription);
      return apiSend<TemplateVersion>(`/api/v1/templates/${id}/versions`, 'POST', form);
    },
    validate: (templateId: string, versionId: string) => apiSend<unknown>(`/api/v1/templates/${templateId}/versions/${versionId}/validate`, 'POST'),
    validation: (templateId: string, versionId: string) => apiGet<ValidationResult>(`/api/v1/templates/${templateId}/versions/${versionId}/validation`),
    activate: (templateId: string, versionId: string) => apiSend<void>(`/api/v1/templates/${templateId}/versions/${versionId}/activate`, 'POST'),
    deactivate: (id: string) => apiSend<Template>(`/api/v1/templates/${id}/deactivate`, 'POST'),
    archive: (id: string) => apiSend<Template>(`/api/v1/templates/${id}/archive`, 'POST'),
    placeholders: (templateId: string, versionId: string) => apiGet<Placeholder[]>(`/api/v1/templates/${templateId}/versions/${versionId}/placeholders`),
    createPlaceholder: (templateId: string, versionId: string, body: Omit<Placeholder, 'id'>) => apiSend<Placeholder>(`/api/v1/templates/${templateId}/versions/${versionId}/placeholders`, 'POST', body),
    updatePlaceholder: (templateId: string, versionId: string, placeholderId: string, body: Partial<Omit<Placeholder, 'id' | 'name' | 'occurrences'>>) => apiSend<Placeholder>(`/api/v1/templates/${templateId}/versions/${versionId}/placeholders/${placeholderId}`, 'PUT', body),
    deletePlaceholder: (templateId: string, versionId: string, placeholderId: string) => apiSend<void>(`/api/v1/templates/${templateId}/versions/${versionId}/placeholders/${placeholderId}`, 'DELETE'),
    discoverPlaceholders: (templateId: string, versionId: string) => apiSend<PlaceholderDiscoveryResult>(`/api/v1/templates/${templateId}/versions/${versionId}/placeholders/discover`, 'POST'),
    validatePlaceholders: (templateId: string, versionId: string) => apiSend<ValidationResult>(`/api/v1/templates/${templateId}/versions/${versionId}/placeholders/validate`, 'POST'),
    download: (templateId: string, versionId: string) => apiDownload(`/api/v1/templates/${templateId}/versions/${versionId}/download`)
  },
  documents: {
    list: (query = '') => apiGet<Paged<DocumentSummary> | DocumentSummary[]>(`/api/v1/documents${query}`),
    get: (id: string) => apiGet<DocumentDetail>(`/api/v1/documents/${id}`),
    create: (body: { name: string; documentType: string; description?: string; templateId: string; templateVersion: number; data: Record<string, unknown> }) => apiSend<DocumentDetail>('/api/v1/documents', 'POST', body),
    generate: (id: string, body: { name: string; outputFormats: string[]; data: Record<string, unknown> }) => apiSend<DocumentGeneration>(`/api/v1/documents/${id}/generate`, 'POST', body),
    download: (id: string, fileType?: string) => apiDownload(`/api/v1/documents/${id}/download${fileType ? `?fileType=${encodeURIComponent(fileType)}` : ''}`)
  },
  workflows: {
    list: (query = '') => apiGet<Workflow[]>(`/api/v1/workflows${query}`),
    create: (body: { code: string; name: string; description?: string }) => apiSend<Workflow>('/api/v1/workflows', 'POST', body),
    get: (id: string) => apiGet<Workflow>(`/api/v1/workflows/${id}`),
    versions: (workflowId: string) => apiGet<WorkflowVersion[]>(`/api/v1/workflows/${workflowId}/versions?page=1&pageSize=50`),
    graph: (versionId: string) => apiGet<WorkflowGraph>(`/api/v1/workflows/versions/${versionId}/graph`),
    createVersion: (workflowId: string) => apiSend<{ id: string; workflowId: string; version: number; isPublished: boolean }>(`/api/v1/workflows/${workflowId}/versions`, 'POST'),
    archive: (workflowId: string) => apiSend<void>(`/api/v1/workflows/${workflowId}/archive`, 'POST'),
    addState: (versionId: string, body: { name: string; stateType: number; configuration?: Record<string, string> }) => apiSend<WorkflowState>(`/api/v1/workflows/versions/${versionId}/states`, 'POST', body),
    addTransition: (versionId: string, body: { fromStateId: string; toStateId: string; order?: number; triggerType?: string }) => apiSend<WorkflowTransition>(`/api/v1/workflows/versions/${versionId}/transitions`, 'POST', body),
    validate: (versionId: string) => apiSend<{ isValid: boolean; errors: string[] }>(`/api/v1/workflows/versions/${versionId}/validate`, 'POST'),
    publish: (workflowId: string, version: number) => apiSend<void>(`/api/v1/workflows/${workflowId}/versions/${version}/publish`, 'POST'),
    instances: (workflowId: string, documentId: string) => apiSend<WorkflowInstance>(`/api/v1/workflows/${workflowId}/instances`, 'POST', { documentId, correlationId: crypto.randomUUID() }, { headers: { 'Idempotency-Key': crypto.randomUUID() } }),
    instance: (id: string) => apiGet<WorkflowInstance>(`/api/v1/workflows/instances/${id}`),
    history: (id: string) => apiGet<WorkflowHistoryEntry[]>(`/api/v1/workflows/instances/${id}/history?page=1&pageSize=50`),
    cancel: (id: string, reason: string) => apiSend<WorkflowInstance>(`/api/v1/workflows/instances/${id}/cancel`, 'POST', { reason }),
    suspend: (id: string, reason: string) => apiSend<WorkflowInstance>(`/api/v1/workflows/instances/${id}/suspend`, 'POST', { reason }),
    resume: (id: string) => apiSend<WorkflowInstance>(`/api/v1/workflows/instances/${id}/resume`, 'POST'),
    transition: (instanceId: string, transitionId: string) => apiSend<WorkflowInstance>(`/api/v1/workflows/instances/${instanceId}/transitions/${transitionId}`, 'POST'),
    myApprovals: (page = 1, pageSize = 25) => apiGet<ApprovalTask[]>(`/api/v1/workflows/approval-tasks/my?page=${page}&pageSize=${pageSize}`),
    approve: (id: string, comment?: string) => apiSend<ApprovalTask>(`/api/v1/workflows/approval-tasks/${id}/approve`, 'POST', { comment }),
    reject: (id: string, comment: string) => apiSend<ApprovalTask>(`/api/v1/workflows/approval-tasks/${id}/reject`, 'POST', { comment }),
    delegate: (id: string, delegateToUserId: string, reason: string) => apiSend<ApprovalTask>(`/api/v1/workflows/approval-tasks/${id}/delegate`, 'POST', { delegateToUserId, reason })
  },
  notifications: {
    list: (unreadOnly = false, page = 1, pageSize = 25) => apiGet<Paged<Notification>>(`/api/v1/notifications?unreadOnly=${unreadOnly}&page=${page}&pageSize=${pageSize}`),
    unreadCount: () => apiGet<{ count: number }>('/api/v1/notifications/unread-count'),
    read: (id: string) => apiSend<void>(`/api/v1/notifications/${id}/read`, 'POST'),
    readAll: () => apiSend<void>('/api/v1/notifications/read-all', 'POST')
  },
  audit: {
    search: (organizationId: string, query = '') => apiGet<AuditLog[]>(`/api/v1/audit-logs?organizationId=${organizationId}${query}`),
    get: (organizationId: string, id: string) => apiGet<AuditLog>(`/api/v1/audit-logs/${id}?organizationId=${organizationId}`),
  },
  signing: {
    list: () => apiGet<{ items?: SigningRequest[] } | SigningRequest[]>('/api/v1/signing-requests'),
    activate: (id: string) => apiSend<void>(`/api/v1/signing-requests/${id}/activate`, 'POST'),
    cancel: (id: string) => apiSend<void>(`/api/v1/signing-requests/${id}/cancel`, 'POST')
  },
  organizations: {
    mine: () => apiGet<Organization[]>('/bff/organizations'),
    select: (id: string) => apiSend<{ organizationId: string }>(`/bff/organizations/${id}/select`, 'POST'),
    members: (id: string) => apiGet<OrganizationMember[]>(`/api/v1/organizations/${id}/members`),
    addMember: (id: string, userId: string, role: string) => apiSend<OrganizationMember>(`/api/v1/organizations/${id}/members`, 'POST', { userId, role }),
    updateMemberRole: (id: string, userId: string, role: string) => apiSend<OrganizationMember>(`/api/v1/organizations/${id}/members/${userId}/role`, 'PUT', { role }),
    removeMember: (id: string, userId: string) => apiSend<void>(`/api/v1/organizations/${id}/members/${userId}`, 'DELETE'),
    capabilities: (id: string) => apiGet<OrganizationCapabilities>(`/api/v1/organizations/${id}/capabilities`)
  }
};

export function items<T>(result: Paged<T> | T[] | { items?: T[] }) { return Array.isArray(result) ? result : result.items ?? []; }
