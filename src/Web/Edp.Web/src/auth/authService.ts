import { appConfig } from '../app/config';

/** Only same-origin relative paths are accepted as post-login destinations (prevents open redirects). */
export function isLocalReturnUrl(value: string | null | undefined): value is string {
  return !!value && value.startsWith('/') && !value.startsWith('//') && !value.startsWith('/\\');
}

export function safeReturnUrl(value: string | null | undefined, fallback = '/') {
  return isLocalReturnUrl(value) && !value.startsWith('/login') ? value : fallback;
}

/** Authentication is delegated to the Gateway BFF (Entra ID); the SPA never handles credentials or tokens. */
export function redirectToLogin(returnUrl = '/') {
  window.location.assign(`${appConfig.gatewayBaseUrl}/bff/auth/login?returnUrl=${encodeURIComponent(safeReturnUrl(returnUrl))}`);
}

export function redirectToLogout() {
  window.location.assign(`${appConfig.gatewayBaseUrl}/bff/auth/logout?returnUrl=${encodeURIComponent('/login')}`);
}
