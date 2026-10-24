import type { AppNotification } from '../api/contracts';

/** Resolves a notification's resource reference to an in-app route; the backend still authorizes the target. */
export function notificationRoute(notification: Pick<AppNotification, 'resourceType' | 'resourceId' | 'notificationType'>): string {
  const id = notification.resourceId;
  if (!id) return '/notifications';
  switch ((notification.resourceType ?? '').toLowerCase()) {
    case 'workflow': return notification.notificationType.toLowerCase().startsWith('approval') ? '/approvals' : `/workflows/instances/${id}`;
    case 'document': return `/documents/${id}`;
    case 'signature': return `/signatures/${id}`;
    case 'template': return `/templates/${id}`;
    default: return '/notifications';
  }
}
