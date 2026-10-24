import { QueryClient } from '@tanstack/react-query';

/**
 * Server state lives here (templates, documents, workflows, approvals, signatures, notifications, audit).
 * Transient failures are retried inside the HTTP client for safe requests, so queries do not retry again.
 */
export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { staleTime: 30_000, gcTime: 5 * 60_000, retry: false, refetchOnWindowFocus: true },
      mutations: { retry: false }
    }
  });
}
