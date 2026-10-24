import { isFeatureEnabled, type FeatureFlags } from '../../app/featureFlags';
import { Permissions, type PermissionRequirement } from '../../auth/permissions';

export interface NavItem {
  label: string;
  to: string;
  permission?: PermissionRequirement;
  mode?: 'all' | 'any';
  flag?: keyof FeatureFlags;
  end?: boolean;
}

export interface NavGroup {
  heading?: string;
  items: NavItem[];
}

export const navigation: NavGroup[] = [
  {
    items: [
      { label: 'Dashboard', to: '/', end: true },
      { label: 'Templates', to: '/templates', permission: Permissions.TemplateRead },
      { label: 'Documents', to: '/documents', permission: Permissions.DocumentRead },
      { label: 'Workflows', to: '/workflows', permission: Permissions.WorkflowRead },
      { label: 'Approvals', to: '/approvals', permission: Permissions.WorkflowApprove },
      { label: 'Signatures', to: '/signatures', permission: Permissions.SignatureRead, flag: 'signature.enabled' },
      { label: 'Notifications', to: '/notifications', permission: Permissions.NotificationRead },
      { label: 'Audit', to: '/audit', permission: Permissions.AuditRead }
    ]
  },
  {
    heading: 'Administration',
    items: [
      { label: 'Organization', to: '/admin/organization', permission: Permissions.OrganizationAdmin },
      { label: 'Users', to: '/admin/users', permission: Permissions.OrganizationAdmin },
      { label: 'Roles', to: '/admin/roles', permission: Permissions.OrganizationAdmin },
      { label: 'Settings', to: '/admin/settings', permission: Permissions.OrganizationAdmin }
    ]
  }
];

/** Items the user may see. Hiding is a usability aid; every route and API call is re-authorized by the backend. */
export function visibleNavigation(can: (required: PermissionRequirement, mode?: 'all' | 'any') => boolean): NavGroup[] {
  return navigation
    .map((group) => ({
      ...group,
      items: group.items.filter((item) => (!item.flag || isFeatureEnabled(item.flag)) && (item.permission === undefined || can(item.permission, item.mode)))
    }))
    .filter((group) => group.items.length > 0);
}
