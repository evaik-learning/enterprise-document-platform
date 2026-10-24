import { gatewayClient } from '../gatewayClient';
import {
  toPaged,
  type AppNotification,
  type AuditFilters,
  type AuditLog,
  type DashboardData,
  type Me,
  type NotificationPreferences,
  type OrganizationCapabilities,
  type OrganizationMember,
  type OrganizationMembership,
  type Paged,
  type SearchResponse,
  type SessionInfo
} from '../contracts';

export const sessionApi = {
  session: () => gatewayClient.get<SessionInfo>('/bff/auth/user', { skipSessionRenewal: true, retries: 0 }),
  me: () => gatewayClient.get<Me>('/api/v1/me', { skipSessionRenewal: true })
};

export const dashboardApi = {
  get: () => gatewayClient.get<DashboardData>('/api/v1/dashboard')
};

export const searchApi = {
  search: (q: string, type?: string, signal?: AbortSignal) => gatewayClient.get<SearchResponse>('/api/v1/search', { query: { q, type }, signal, retries: 0 })
};

export const notificationApi = {
  async list(unreadOnly = false, page = 1, pageSize = 25): Promise<Paged<AppNotification>> {
    return toPaged<AppNotification>(await gatewayClient.get<unknown>('/api/v1/notifications', { query: { unreadOnly, page, pageSize } }), page, pageSize);
  },
  unreadCount: () => gatewayClient.get<{ count: number }>('/api/v1/notifications/unread-count'),
  markRead: (id: string) => gatewayClient.post<void>(`/api/v1/notifications/${id}/read`),
  markAllRead: () => gatewayClient.post<void>('/api/v1/notifications/read-all'),
  preferences: () => gatewayClient.get<NotificationPreferences>('/api/v1/notifications/preferences'),
  updatePreferences: (body: NotificationPreferences) => gatewayClient.put<NotificationPreferences>('/api/v1/notifications/preferences', body)
};

export const auditApi = {
  async search(organizationId: string, filters: AuditFilters = {}): Promise<Paged<AuditLog>> {
    const page = filters.page ?? 1;
    const pageSize = filters.pageSize ?? 50;
    const raw = await gatewayClient.get<unknown>('/api/v1/audit-logs', {
      query: { organizationId, entityType: filters.entityType, entityId: filters.entityId, action: filters.action, correlationId: filters.correlationId, from: filters.from, to: filters.to, page, pageSize }
    });
    return toPaged<AuditLog>(raw, page, pageSize);
  }
};

export const organizationApi = {
  mine: () => gatewayClient.get<OrganizationMembership[]>('/bff/organizations'),
  select: (id: string) => gatewayClient.post<{ organizationId: string }>(`/bff/organizations/${id}/select`),
  members: (id: string) => gatewayClient.get<OrganizationMember[]>(`/api/v1/organizations/${id}/members`),
  addMember: (id: string, userId: string, role: string) => gatewayClient.post<OrganizationMember>(`/api/v1/organizations/${id}/members`, { userId, role }),
  updateMemberRole: (id: string, userId: string, role: string) => gatewayClient.put<OrganizationMember>(`/api/v1/organizations/${id}/members/${userId}/role`, { role }),
  removeMember: (id: string, userId: string) => gatewayClient.delete(`/api/v1/organizations/${id}/members/${userId}`),
  capabilities: (id: string) => gatewayClient.get<OrganizationCapabilities>(`/api/v1/organizations/${id}/capabilities`)
};
