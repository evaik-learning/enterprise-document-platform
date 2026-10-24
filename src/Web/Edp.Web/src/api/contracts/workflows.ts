export type WorkflowStatus = 'Draft' | 'Published' | 'Archived' | 'Suspended';
export type InstanceStatus = 'Pending' | 'InProgress' | 'AwaitingApproval' | 'Completed' | 'Rejected' | 'Cancelled' | 'Failed' | 'Paused';
export type StateType = 'Start' | 'Task' | 'Approval' | 'ParallelApprovalAll' | 'ParallelApprovalAny' | 'End';
export const STATE_TYPES: StateType[] = ['Start', 'Task', 'Approval', 'ParallelApprovalAll', 'ParallelApprovalAny', 'End'];

export interface Workflow {
  id: string;
  code: string;
  name: string;
  status: WorkflowStatus | string;
  publishedVersion?: number;
}

export interface WorkflowVersion {
  id: string;
  workflowId: string;
  version: number;
  isPublished: boolean;
}

export interface WorkflowState {
  id: string;
  workflowVersionId: string;
  name: string;
  stateType: StateType | string;
}

export interface WorkflowTransition {
  id: string;
  workflowVersionId: string;
  fromStateId: string;
  toStateId: string;
  order: number;
}

export interface WorkflowGraph {
  states: WorkflowState[];
  transitions: WorkflowTransition[];
}

export interface WorkflowInstance {
  id: string;
  workflowId: string;
  workflowVersion: number;
  documentId: string;
  status: InstanceStatus | string;
  currentStateId?: string;
}

export interface ApprovalTask {
  id: string;
  workflowInstanceId: string;
  stateId: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Delegated' | 'Cancelled' | 'Expired' | string;
  assignedToUserId: string;
  deadlineAt?: string;
}

export interface WorkflowHistoryEntry {
  id: string;
  eventType: string;
  stateId?: string;
  approvalTaskId?: string;
  userId?: string;
  description: string;
  eventAt: string;
}

/** Composed by the Gateway: instance + definition graph + tasks + history. */
export interface WorkflowContext {
  instance: WorkflowInstance;
  workflow?: Workflow;
  states: WorkflowState[];
  transitions: WorkflowTransition[];
  currentState?: WorkflowState;
  nextTransitions: WorkflowTransition[];
  tasks: ApprovalTask[];
  history: WorkflowHistoryEntry[];
}

export interface ApprovalInboxItem {
  taskId: string;
  workflowInstanceId?: string;
  workflowId?: string;
  workflowName?: string;
  documentId?: string;
  documentName?: string;
  status: string;
  instanceStatus?: string;
  assignedToUserId?: string;
  deadlineAt?: string;
}

export interface ApprovalDetail {
  task: ApprovalTask;
  instance?: WorkflowInstance;
  document?: import('./documents').DocumentDetail;
  history: WorkflowHistoryEntry[];
}

export const ACTIVE_INSTANCE_STATUSES: string[] = ['Pending', 'InProgress', 'AwaitingApproval', 'Paused'];
