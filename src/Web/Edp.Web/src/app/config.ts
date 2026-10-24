/** Public, build-time configuration. Anything here ships to the browser - never place secrets in it. */
export const appConfig = {
  gatewayBaseUrl: (import.meta.env.VITE_GATEWAY_BASE_URL ?? '').replace(/\/$/, ''),
  appName: import.meta.env.VITE_APP_NAME ?? 'Enterprise Document Platform',
  appVersion: import.meta.env.VITE_APP_VERSION ?? '0.0.0',
  environment: import.meta.env.VITE_ENVIRONMENT ?? 'development'
} as const;
