import { useMemo } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../auth/useAuth';

/**
 * Every key starts with the organization id, so switching tenant can never serve another tenant's cached data
 * (the cache is also cleared on switch as a second line of defence).
 */
export function createQueryKeys(org: string | null) {
  const scope = org ?? 'no-org';
  return {
    scope,
    me: [scope, 'me'] as const,
    dashboard: [scope, 'dashboard'] as const,
    search: (q: string, type?: string) => [scope, 'search', q, type ?? 'all'] as const,
    templates: {
      all: [scope, 'templates'] as const,
      list: (filters: unknown) => [scope, 'templates', 'list', filters] as const,
      detail: (id: string) => [scope, 'templates', 'detail', id] as const,
      versions: (id: string) => [scope, 'templates', 'versions', id] as const,
      validation: (id: string, versionId: string) => [scope, 'templates', 'validation', id, versionId] as const,
      placeholders: (id: string, versionId: string) => [scope, 'templates', 'placeholders', id, versionId] as const
    },
    documents: {
      all: [scope, 'documents'] as const,
      list: (filters: unknown) => [scope, 'documents', 'list', filters] as const,
      detail: (id: string) => [scope, 'documents', 'detail', id] as const,
      workspace: (id: string) => [scope, 'documents', 'workspace', id] as const,
      timeline: (id: string) => [scope, 'documents', 'timeline', id] as const,
      audit: (id: string) => [scope, 'documents', 'audit', id] as const
    },
    workflows: {
      all: [scope, 'workflows'] as const,
      list: [scope, 'workflows', 'list'] as const,
      versions: (id: string) => [scope, 'workflows', 'versions', id] as const,
      graph: (versionId: string) => [scope, 'workflows', 'graph', versionId] as const,
      context: (instanceId: string) => [scope, 'workflows', 'context', instanceId] as const
    },
    approvals: {
      all: [scope, 'approvals'] as const,
      inbox: (page: number, pageSize: number) => [scope, 'approvals', 'inbox', page, pageSize] as const,
      detail: (taskId: string) => [scope, 'approvals', 'detail', taskId] as const
    },
    signatures: {
      all: [scope, 'signatures'] as const,
      list: (filters: unknown) => [scope, 'signatures', 'list', filters] as const,
      detail: (id: string) => [scope, 'signatures', 'detail', id] as const
    },
    notifications: {
      all: [scope, 'notifications'] as const,
      list: (unreadOnly: boolean, page: number, pageSize: number) => [scope, 'notifications', 'list', unreadOnly, page, pageSize] as const,
      unread: [scope, 'notifications', 'unread'] as const,
      preferences: [scope, 'notifications', 'preferences'] as const,
      latest: [scope, 'notifications', 'latest'] as const
    },
    audit: {
      all: [scope, 'audit'] as const,
      search: (filters: unknown) => [scope, 'audit', 'search', filters] as const
    },
    organization: {
      all: [scope, 'organization'] as const,
      mine: [scope, 'organization', 'mine'] as const,
      members: (id: string) => [scope, 'organization', 'members', id] as const,
      capabilities: (id: string) => [scope, 'organization', 'capabilities', id] as const
    }
  };
}

export type QueryKeys = ReturnType<typeof createQueryKeys>;

export function useQueryKeys(): QueryKeys {
  const { organizationId } = useAuth();
  return useMemo(() => createQueryKeys(organizationId), [organizationId]);
}

/** Invalidation groups matching the cross-feature effects described in Phase 10 section 40. */
export function useInvalidation() {
  const queryClient = useQueryClient();
  const keys = useQueryKeys();
  return useMemo(() => {
    const invalidate = (...queryKeys: Array<readonly unknown[]>) => Promise.all(queryKeys.map((queryKey) => queryClient.invalidateQueries({ queryKey })));
    return {
      templates: () => invalidate(keys.templates.all),
      documents: () => invalidate(keys.documents.all, keys.dashboard),
      /** Workflow transition/approval: workflow, inbox, document timeline/workspace, notifications and dashboard counts. */
      workflowChanged: () => invalidate(keys.workflows.all, keys.approvals.all, keys.documents.all, keys.notifications.all, keys.dashboard),
      signatureChanged: () => invalidate(keys.signatures.all, keys.documents.all, keys.notifications.all, keys.dashboard),
      notifications: () => invalidate(keys.notifications.all, keys.dashboard),
      organization: () => invalidate(keys.organization.all)
    };
  }, [keys, queryClient]);
}
