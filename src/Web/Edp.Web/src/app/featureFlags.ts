/** Feature flags control availability only; the backend still authorizes every operation. */
export interface FeatureFlags {
  'signature.enabled': boolean;
  'realtime.enabled': boolean;
  'advanced.audit.search': boolean;
  'multi.organization': boolean;
}

const flag = (value: string | undefined, fallback: boolean) =>
  value === undefined || value === '' ? fallback : value.toLowerCase() === 'true';

export const featureFlags: FeatureFlags = {
  'signature.enabled': flag(import.meta.env.VITE_FEATURE_SIGNATURE, true),
  'realtime.enabled': flag(import.meta.env.VITE_FEATURE_REALTIME, false),
  'advanced.audit.search': flag(import.meta.env.VITE_FEATURE_ADVANCED_AUDIT, true),
  'multi.organization': flag(import.meta.env.VITE_FEATURE_MULTI_ORGANIZATION, true)
};

export const isFeatureEnabled = (name: keyof FeatureFlags) => featureFlags[name];
