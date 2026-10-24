export interface Me {
  userId?: string | null;
  name?: string | null;
  email?: string | null;
  organizationId?: string | null;
  organizationName?: string | null;
  roles: string[];
  /** Permission names such as `template.read` (see auth/permissions.ts). */
  permissions: string[];
  requiresOrganizationSelection: boolean;
}

export interface SessionInfo {
  isAuthenticated: boolean;
  displayName?: string | null;
  userName?: string | null;
}

export interface OrganizationMembership {
  id: string;
  name: string;
  description?: string;
  role: string;
  isActive: boolean;
}

export interface OrganizationMember {
  userId: string;
  role: string;
  isActive: boolean;
}

export interface OrganizationCapabilities {
  organizationId: string;
  userId: string;
  role: string;
  permissions: string[];
}
