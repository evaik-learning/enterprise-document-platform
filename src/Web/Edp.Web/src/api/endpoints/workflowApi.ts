import { gatewayClient } from '../gatewayClient';
import {
  toPaged,
  type ApprovalDetail,
  type ApprovalInboxItem,
  type ApprovalTask,
  type Paged,
  type Workflow,
  type WorkflowContext,
  type WorkflowGraph,
  type WorkflowInstance,
  type WorkflowState,
  type WorkflowTransition,
  type WorkflowVersion
} from '../contracts';

const base = '/api/v1/workflows';

export const workflowApi = {
  list: (page = 1, pageSize = 100) => gatewayClient.get<Workflow[]>(base, { query: { page, pageSize } }),
  get: (id: string) => gatewayClient.get<Workflow>(`${base}/${id}`),
  create: (body: { code: string; name: string; description?: string }) => gatewayClient.post<Workflow>(base, body),
  archive: (id: string) => gatewayClient.post<void>(`${base}/${id}/archive`, {}),
  versions: (id: string) => gatewayClient.get<WorkflowVersion[]>(`${base}/${id}/versions`, { query: { page: 1, pageSize: 50 } }),
  createVersion: (id: string) => gatewayClient.post<WorkflowVersion>(`${base}/${id}/versions`, {}),
  graph: (versionId: string) => gatewayClient.get<WorkflowGraph>(`${base}/versions/${versionId}/graph`),
  addState: (versionId: string, body: { name: string; stateType: string; approverUserIds?: string[] }) => gatewayClient.post<WorkflowState>(`${base}/versions/${versionId}/states`, body),
  addTransition: (versionId: string, body: { fromStateId: string; toStateId: string; order?: number; triggerType?: string }) => gatewayClient.post<WorkflowTransition>(`${base}/versions/${versionId}/transitions`, body),
  validateVersion: (versionId: string) => gatewayClient.post<{ isValid: boolean; errors: string[] }>(`${base}/versions/${versionId}/validate`, {}),
  publish: (id: string, version: number) => gatewayClient.post<void>(`${base}/${id}/versions/${version}/publish`, {}),

  /** Instance state is owned by the Workflow service; this lists them (optionally by document or status). */
  async instances(filters: { documentId?: string; status?: string; page?: number; pageSize?: number } = {}): Promise<Paged<WorkflowInstance>> {
    const page = filters.page ?? 1;
    const pageSize = filters.pageSize ?? 25;
    return toPaged<WorkflowInstance>(await gatewayClient.get<unknown>(`${base}/instances`, { query: { documentId: filters.documentId, status: filters.status, page, pageSize } }), page, pageSize);
  },
  startInstance: (workflowId: string, documentId: string, idempotencyKey?: string) =>
    gatewayClient.post<WorkflowInstance>(`${base}/${workflowId}/instances`, { documentId, correlationId: crypto.randomUUID() }, { idempotencyKey }),
  /** Instance + definition graph + tasks + history, composed by the Gateway. */
  context: (instanceId: string) => gatewayClient.get<WorkflowContext>(`/api/v1/workflow-instances/${instanceId}`),
  cancel: (id: string, reason: string) => gatewayClient.post<WorkflowInstance>(`${base}/instances/${id}/cancel`, { reason }),
  suspend: (id: string, reason: string) => gatewayClient.post<WorkflowInstance>(`${base}/instances/${id}/suspend`, { reason }),
  resume: (id: string) => gatewayClient.post<WorkflowInstance>(`${base}/instances/${id}/resume`, {}),
  executeTransition: (instanceId: string, transitionId: string) => gatewayClient.post<WorkflowInstance>(`${base}/instances/${instanceId}/transitions/${transitionId}`, {})
};

export const approvalApi = {
  async inbox(page = 1, pageSize = 25): Promise<Paged<ApprovalInboxItem>> {
    return toPaged<ApprovalInboxItem>(await gatewayClient.get<unknown>('/api/v1/approvals', { query: { page, pageSize } }), page, pageSize);
  },
  get: (taskId: string) => gatewayClient.get<ApprovalDetail>(`/api/v1/approvals/${taskId}`),
  approve: (taskId: string, comment?: string) => gatewayClient.post<ApprovalTask>(`${base}/approval-tasks/${taskId}/approve`, { comment }),
  reject: (taskId: string, comment: string) => gatewayClient.post<ApprovalTask>(`${base}/approval-tasks/${taskId}/reject`, { comment }),
  delegate: (taskId: string, delegateToUserId: string, reason: string) => gatewayClient.post<ApprovalTask>(`${base}/approval-tasks/${taskId}/delegate`, { delegateToUserId, reason })
};
