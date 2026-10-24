export interface AppNotification {
  id: string;
  notificationType: string;
  subject: string;
  body: string;
  status: string;
  createdAtUtc: string;
  readAtUtc?: string | null;
  correlationId: string;
  resourceType?: string | null;
  resourceId?: string | null;
}

export interface NotificationPreferences {
  emailEnabled: boolean;
  inAppEnabled: boolean;
  workflowEnabled: boolean;
  signatureEnabled: boolean;
  auditEnabled: boolean;
}

export interface AuditLog {
  id: string;
  action: string;
  entityType?: string;
  entityId?: string;
  userId?: string;
  timestamp?: string;
  correlationId?: string;
  metadata?: Record<string, unknown>;
}

export interface AuditFilters {
  entityType?: string;
  entityId?: string;
  action?: string;
  correlationId?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}
