export interface UserClaim {
  type: string;
  value: string;
}

export interface AuthenticatedUser {
  isAuthenticated: true;
  displayName: string;
  userName: string;
  claims: UserClaim[];
}

export interface AnonymousUser {
  isAuthenticated: false;
  displayName: null;
  userName: null;
  claims: [];
}

export type CurrentUser = AuthenticatedUser | AnonymousUser;
