import type { ReactNode } from 'react';
import type { UseQueryResult } from '@tanstack/react-query';
import { ErrorState } from './Feedback';
import { LoadingSkeleton } from './LoadingSkeleton';

interface QueryBoundaryProps<T> {
  query: UseQueryResult<T>;
  /** Rendered with data once loaded. */
  children: (data: T) => ReactNode;
  loadingRows?: number;
  loadingLabel?: string;
}

/** Initial loading -> skeleton, failure -> ErrorState with retry, success -> content (background refresh keeps content). */
export function QueryBoundary<T>({ query, children, loadingRows, loadingLabel }: QueryBoundaryProps<T>) {
  if (query.isPending) return <LoadingSkeleton rows={loadingRows} label={loadingLabel} />;
  if (query.isError) return <ErrorState error={query.error} onRetry={() => void query.refetch()} />;
  return <>{children(query.data)}</>;
}
