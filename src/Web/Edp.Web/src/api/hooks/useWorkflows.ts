import { keepPreviousData, useMutation, useQuery } from '@tanstack/react-query';
import { approvalApi, workflowApi } from '../endpoints';
import { ACTIVE_INSTANCE_STATUSES } from '../contracts';
import { useInvalidation, useQueryKeys } from '../queryKeys';

export function useWorkflows() {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.workflows.list, queryFn: () => workflowApi.list() });
}

export function useWorkflowInstances(filters: { status?: string; page: number; pageSize: number }) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: [...keys.workflows.all, 'instances', filters], queryFn: () => workflowApi.instances(filters), placeholderData: keepPreviousData });
}

export function useWorkflowVersions(workflowId: string) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.workflows.versions(workflowId), queryFn: () => workflowApi.versions(workflowId), enabled: !!workflowId });
}

export function useWorkflowGraph(versionId: string | undefined) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.workflows.graph(versionId ?? ''), queryFn: () => workflowApi.graph(versionId!), enabled: !!versionId });
}

/** Instance state is owned by the Workflow service; poll while it is active so the UI follows backend transitions. */
export function useWorkflowContext(instanceId: string) {
  const keys = useQueryKeys();
  return useQuery({
    queryKey: keys.workflows.context(instanceId),
    queryFn: () => workflowApi.context(instanceId),
    enabled: !!instanceId,
    refetchInterval: (query) => (ACTIVE_INSTANCE_STATUSES.includes(query.state.data?.instance.status ?? '') ? 10_000 : false)
  });
}

export function useWorkflowDefinitionMutations() {
  const invalidate = useInvalidation();
  const onSuccess = () => invalidate.workflowChanged();
  return {
    create: useMutation({ mutationFn: workflowApi.create, onSuccess }),
    archive: useMutation({ mutationFn: (id: string) => workflowApi.archive(id), onSuccess }),
    createVersion: useMutation({ mutationFn: (id: string) => workflowApi.createVersion(id), onSuccess }),
    addState: useMutation({ mutationFn: ({ versionId, ...body }: { versionId: string; name: string; stateType: string; approverUserIds?: string[] }) => workflowApi.addState(versionId, body), onSuccess }),
    addTransition: useMutation({ mutationFn: ({ versionId, ...body }: { versionId: string; fromStateId: string; toStateId: string; order?: number }) => workflowApi.addTransition(versionId, body), onSuccess }),
    validate: useMutation({ mutationFn: (versionId: string) => workflowApi.validateVersion(versionId) }),
    publish: useMutation({ mutationFn: ({ id, version }: { id: string; version: number }) => workflowApi.publish(id, version), onSuccess })
  };
}

export function useWorkflowInstanceMutations() {
  const invalidate = useInvalidation();
  const onSuccess = () => invalidate.workflowChanged();
  return {
    start: useMutation({ mutationFn: ({ workflowId, documentId, idempotencyKey }: { workflowId: string; documentId: string; idempotencyKey?: string }) => workflowApi.startInstance(workflowId, documentId, idempotencyKey), onSuccess }),
    executeTransition: useMutation({ mutationFn: ({ instanceId, transitionId }: { instanceId: string; transitionId: string }) => workflowApi.executeTransition(instanceId, transitionId), onSuccess }),
    cancel: useMutation({ mutationFn: ({ id, reason }: { id: string; reason: string }) => workflowApi.cancel(id, reason), onSuccess }),
    suspend: useMutation({ mutationFn: ({ id, reason }: { id: string; reason: string }) => workflowApi.suspend(id, reason), onSuccess }),
    resume: useMutation({ mutationFn: (id: string) => workflowApi.resume(id), onSuccess })
  };
}

export function useApprovalInbox(page: number, pageSize: number) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.approvals.inbox(page, pageSize), queryFn: () => approvalApi.inbox(page, pageSize), placeholderData: keepPreviousData });
}

export function useApprovalDetail(taskId: string) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.approvals.detail(taskId), queryFn: () => approvalApi.get(taskId), enabled: !!taskId });
}

/** Approval decisions are never applied optimistically: the UI refreshes only after the backend confirms. */
export function useApprovalMutations() {
  const invalidate = useInvalidation();
  const onSuccess = () => invalidate.workflowChanged();
  return {
    approve: useMutation({ mutationFn: ({ taskId, comment }: { taskId: string; comment?: string }) => approvalApi.approve(taskId, comment), onSuccess }),
    reject: useMutation({ mutationFn: ({ taskId, comment }: { taskId: string; comment: string }) => approvalApi.reject(taskId, comment), onSuccess }),
    delegate: useMutation({ mutationFn: ({ taskId, userId, reason }: { taskId: string; userId: string; reason: string }) => approvalApi.delegate(taskId, userId, reason), onSuccess })
  };
}
