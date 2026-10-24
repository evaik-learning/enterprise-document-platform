/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_GATEWAY_BASE_URL?: string;
  readonly VITE_APP_NAME?: string;
  readonly VITE_APP_VERSION?: string;
  readonly VITE_ENVIRONMENT?: string;
  readonly VITE_FEATURE_SIGNATURE?: string;
  readonly VITE_FEATURE_REALTIME?: string;
  readonly VITE_FEATURE_ADVANCED_AUDIT?: string;
  readonly VITE_FEATURE_MULTI_ORGANIZATION?: string;
}
