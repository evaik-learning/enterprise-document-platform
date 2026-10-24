import { useRef } from 'react';
import { keepPreviousData, useMutation, useQuery } from '@tanstack/react-query';
import { documentApi } from '../endpoints';
import { PROCESSING_DOCUMENT_STATUSES, type DocumentFilters, type GenerateDocumentRequest } from '../contracts';
import { useInvalidation, useQueryKeys } from '../queryKeys';

const POLL_INTERVAL_MS = 3000;
const POLL_CAP_MS = 2 * 60 * 1000;

export function useDocuments(filters: DocumentFilters) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.documents.list(filters), queryFn: () => documentApi.list(filters), placeholderData: keepPreviousData });
}

/** Polls while the backend reports the document as processing; stops after a cap so a stuck job never polls forever. */
function usePollWhileProcessing() {
  const startedAt = useRef<number | null>(null);
  return (status: string | undefined) => {
    const processing = !!status && PROCESSING_DOCUMENT_STATUSES.includes(status);
    if (!processing) { startedAt.current = null; return false as const; }
    startedAt.current ??= Date.now();
    return Date.now() - startedAt.current < POLL_CAP_MS ? POLL_INTERVAL_MS : (false as const);
  };
}

export function useDocument(id: string) {
  const keys = useQueryKeys();
  const poll = usePollWhileProcessing();
  return useQuery({ queryKey: keys.documents.detail(id), queryFn: () => documentApi.get(id), enabled: !!id, refetchInterval: (query) => poll(query.state.data?.status) });
}

export function useDocumentWorkspace(id: string) {
  const keys = useQueryKeys();
  const poll = usePollWhileProcessing();
  return useQuery({ queryKey: keys.documents.workspace(id), queryFn: () => documentApi.workspace(id), enabled: !!id, refetchInterval: (query) => poll(query.state.data?.document.status) });
}

export function useDocumentAudit(id: string, enabled: boolean) {
  const keys = useQueryKeys();
  return useQuery({ queryKey: keys.documents.audit(id), queryFn: () => documentApi.audit(id), enabled: enabled && !!id, retry: false });
}

export function useDocumentMutations() {
  const invalidate = useInvalidation();
  const onSuccess = () => invalidate.documents();
  return {
    create: useMutation({ mutationFn: documentApi.create, onSuccess }),
    generate: useMutation({ mutationFn: ({ id, body }: { id: string; body: GenerateDocumentRequest }) => documentApi.generate(id, body), onSuccess })
  };
}
