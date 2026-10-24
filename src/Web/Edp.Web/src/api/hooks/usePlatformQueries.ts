import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { auditApi, dashboardApi, notificationApi, organizationApi, searchApi, signatureApi } from '../endpoints';
import type { AuditFilters, NotificationPreferences, SigningFilters } from '../contracts';
import { useInvalidation, useQueryKeys } from '../queryKeys';

export function useSignatures(filters: SigningFilters) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.signatures.list(filters), queryFn: () => signatureApi.list(filters), placeholderData: keepPreviousData });
}

export function useSignatureRequest(id: string) {
  const keys = useQueryKeys();
  return useQuery({
    queryKey: keys.signatures.detail(id),
    queryFn: () => signatureApi.get(id),
    enabled: !!id,
    refetchInterval: (query) => (['InProgress', 'PartiallySigned', 'Pending'].includes(query.state.data?.status ?? '') ? 10_000 : false)
  });
}

export function useSignatureMutations() {
  const invalidate = useInvalidation();
  const onSuccess = () => invalidate.signatureChanged();
  return {
    create: useMutation({ mutationFn: signatureApi.create, onSuccess }),
    activate: useMutation({ mutationFn: (id: string) => signatureApi.activate(id), onSuccess }),
    cancel: useMutation({ mutationFn: (id: string) => signatureApi.cancel(id), onSuccess }),
    sign: useMutation({ mutationFn: ({ id, signerId, value }: { id: string; signerId: string; value: string }) => signatureApi.sign(id, signerId, value), onSuccess }),
    decline: useMutation({ mutationFn: ({ id, signerId, reason }: { id: string; signerId: string; reason?: string }) => signatureApi.decline(id, signerId, reason), onSuccess }),
    resend: useMutation({ mutationFn: ({ id, signerId }: { id: string; signerId: string }) => signatureApi.resendInvitation(id, signerId), onSuccess })
  };
}

export function useDashboard() {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.dashboard, queryFn: dashboardApi.get, refetchInterval: 60_000 });
}

export function useGlobalSearch(term: string) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.search(term), queryFn: ({ signal }) => searchApi.search(term, undefined, signal), enabled: term.trim().length >= 2, staleTime: 30_000 });
}

export function useUnreadCount(enabled: boolean) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.notifications.unread, queryFn: notificationApi.unreadCount, enabled, refetchInterval: 30_000, retry: false });
}

export function useLatestNotifications(enabled: boolean) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.notifications.latest, queryFn: () => notificationApi.list(false, 1, 5), enabled, retry: false });
}

export function useNotifications(unreadOnly: boolean, page: number, pageSize: number) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.notifications.list(unreadOnly, page, pageSize), queryFn: () => notificationApi.list(unreadOnly, page, pageSize), placeholderData: keepPreviousData });
}

export function useNotificationMutations() {
  const invalidate = useInvalidation();
  const queryClient = useQueryClient();
  const keys = useQueryKeys();
  const onSuccess = () => invalidate.notifications();
  return {
    markRead: useMutation({ mutationFn: (id: string) => notificationApi.markRead(id), onSuccess }),
    markAllRead: useMutation({ mutationFn: notificationApi.markAllRead, onSuccess }),
    updatePreferences: useMutation({
      mutationFn: (body: NotificationPreferences) => notificationApi.updatePreferences(body),
      onSuccess: (saved) => queryClient.setQueryData(keys.notifications.preferences, saved)
    })
  };
}

export function useNotificationPreferences() {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.notifications.preferences, queryFn: notificationApi.preferences });
}

export function useAuditSearch(organizationId: string | null, filters: AuditFilters) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.audit.search(filters), queryFn: () => auditApi.search(organizationId!, filters), enabled: !!organizationId, placeholderData: keepPreviousData, retry: false });
}

export function useMyOrganizations() {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.organization.mine, queryFn: organizationApi.mine });
}

export function useOrganizationMembers(organizationId: string | null) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.organization.members(organizationId ?? ''), queryFn: () => organizationApi.members(organizationId!), enabled: !!organizationId });
}

export function useOrganizationCapabilities(organizationId: string | null) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.organization.capabilities(organizationId ?? ''), queryFn: () => organizationApi.capabilities(organizationId!), enabled: !!organizationId });
}

export function useMemberMutations() {
  const invalidate = useInvalidation();
  const onSuccess = () => invalidate.organization();
  return {
    add: useMutation({ mutationFn: ({ organizationId, userId, role }: { organizationId: string; userId: string; role: string }) => organizationApi.addMember(organizationId, userId, role), onSuccess }),
    updateRole: useMutation({ mutationFn: ({ organizationId, userId, role }: { organizationId: string; userId: string; role: string }) => organizationApi.updateMemberRole(organizationId, userId, role), onSuccess }),
    remove: useMutation({ mutationFn: ({ organizationId, userId }: { organizationId: string; userId: string }) => organizationApi.removeMember(organizationId, userId), onSuccess })
  };
}
