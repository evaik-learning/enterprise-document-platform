import type { AppNotification, AuditLog } from './notifications';
import type { DocumentSummary } from './documents';
import type { ApprovalTask, WorkflowContext, WorkflowInstance } from './workflows';
import type { DocumentDetail } from './documents';
import type { SigningRequestDetail, SigningRequestSummary } from './signatures';

export interface DashboardData {
  counts: {
    templates: number | null;
    documents: number | null;
    completedDocuments: number | null;
    pendingApprovals: number | null;
    awaitingSignature: number | null;
    unreadNotifications: number | null;
  };
  recentDocuments: DocumentSummary[] | null;
  pendingApprovals: ApprovalTask[] | null;
  pendingSignatures: SigningRequestSummary[] | null;
  recentActivity: AuditLog[] | null;
  recentNotifications: AppNotification[] | null;
  unavailableSections: string[];
}

export type SearchGroupType = 'templates' | 'documents' | 'workflows' | 'signatures';

export interface SearchResultItem {
  id: string;
  title: string;
  subtitle?: string | null;
  status?: string | null;
  url: string;
}

export interface SearchGroup {
  type: SearchGroupType;
  totalCount: number;
  items: SearchResultItem[];
}

export interface SearchResponse {
  query: string;
  groups: SearchGroup[];
}

export interface TimelineEvent {
  id: string;
  timestamp: string;
  type: string;
  title: string;
  description?: string | null;
  source: 'Document' | 'Workflow' | 'Signature' | 'Audit' | string;
  actor?: string | null;
  status: 'completed' | 'failed' | 'info' | string;
  resourceType?: string | null;
  resourceId?: string | null;
}

/** Backend-driven action codes: the UI renders only what the backend says is currently valid. */
export type DocumentAction =
  | 'PREVIEW' | 'DOWNLOAD' | 'GENERATE' | 'START_WORKFLOW' | 'VIEW_WORKFLOW' | 'SEND_FOR_SIGNATURE' | 'VIEW_SIGNATURE' | 'VIEW_AUDIT';

export interface DocumentWorkspace {
  document: DocumentDetail;
  workflow: { instances: WorkflowInstance[]; current?: WorkflowContext | null };
  signature: { requests: SigningRequestSummary[]; current?: SigningRequestDetail | null };
  permissions: string[];
  timeline: TimelineEvent[];
  availableActions: DocumentAction[];
  unavailableSections: string[];
}
