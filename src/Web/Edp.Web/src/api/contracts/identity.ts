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

export interface DevUserSummary {
  id: string;
  displayName: string;
  email: string;
  description: string;
}

/** Sign-in options exposed by the Gateway; demo users are only ever listed in local development. */
export interface AuthConfig {
  devAuthEnabled: boolean;
  entraConfigured: boolean;
  devUsers: DevUserSummary[];
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
