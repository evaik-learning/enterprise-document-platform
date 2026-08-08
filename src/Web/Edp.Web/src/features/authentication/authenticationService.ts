import type { CurrentUser } from '../../models/authentication';
import { apiGet } from '../../services/apiClient';

export function getCurrentUser() {
  return apiGet<CurrentUser>('/bff/auth/user');
}

export function login(returnUrl = window.location.pathname) {
  window.location.assign(`/bff/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`);
}

export function logout(returnUrl = '/') {
  window.location.assign(`/bff/auth/logout?returnUrl=${encodeURIComponent(returnUrl)}`);
}
