/** Permission names issued by the Gateway `/api/v1/me`. They gate UI only; the backend authorizes every request. */
export const Permissions = {
  TemplateRead: 'template.read',
  TemplateWrite: 'template.write',
  DocumentRead: 'document.read',
  DocumentGenerate: 'document.generate',
  WorkflowRead: 'workflow.read',
  WorkflowExecute: 'workflow.execute',
  WorkflowApprove: 'workflow.approve',
  SignatureRead: 'signature.read',
  SignatureExecute: 'signature.execute',
  NotificationRead: 'notification.read',
  AuditRead: 'audit.read',
  OrganizationAdmin: 'organization.admin'
} as const;

export type Permission = (typeof Permissions)[keyof typeof Permissions];

export type PermissionRequirement = Permission | readonly Permission[];

/** `all` (default) requires every listed permission; `any` requires at least one. */
export function hasPermission(granted: readonly string[], required: PermissionRequirement | undefined, mode: 'all' | 'any' = 'all') {
  if (required === undefined) return true;
  const list: readonly Permission[] = typeof required === 'string' ? [required] : required;
  if (list.length === 0) return true;
  return mode === 'all' ? list.every((permission) => granted.includes(permission)) : list.some((permission) => granted.includes(permission));
}
