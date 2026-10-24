import { useEffect, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import type { DocumentDetail } from '../../api/contracts';
import { documentApi } from '../../api/endpoints';
import { useQueryKeys } from '../../api/queryKeys';
import { EmptyState, ErrorState } from '../../components/feedback/Feedback';
import { LoadingSkeleton } from '../../components/feedback/LoadingSkeleton';

/** Previews the latest PDF through the authorized download endpoint; no storage URLs are ever built in the browser. */
export function DocumentPreview({ document }: { document: DocumentDetail }) {
  const keys = useQueryKeys();
  const latest = [...document.versions].sort((a, b) => b.versionNumber - a.versionNumber)[0];
  const hasPdf = !!latest?.files.some((file) => file.fileType.toUpperCase() === 'PDF');

  const query = useQuery({
    queryKey: [...keys.documents.detail(document.id), 'preview', latest?.id],
    queryFn: () => documentApi.download(document.id, 'PDF'),
    enabled: hasPdf,
    staleTime: 5 * 60_000,
    gcTime: 60_000,
    retry: 1
  });

  const url = useMemo(() => (query.data && query.data.blob.type.includes('pdf') ? URL.createObjectURL(query.data.blob) : null), [query.data]);
  useEffect(() => () => { if (url) URL.revokeObjectURL(url); }, [url]);

  if (!latest) return <EmptyState title="Nothing to preview yet" description="Preview is available once the document has been generated." />;
  if (!hasPdf) return <EmptyState title="Preview unavailable" description="This document has no PDF output. Download the DOCX file instead." />;
  if (query.isPending) return <LoadingSkeleton rows={6} label="Loading preview" />;
  if (query.isError) return <ErrorState error={query.error} title="The preview could not be loaded" onRetry={() => void query.refetch()} />;
  if (!url) return <EmptyState title="Preview unavailable" description="The generated file is not a previewable PDF. Download it instead." />;
  return <iframe title={`Preview of ${document.name}`} src={url} className="h-[70vh] w-full rounded-xl border border-slate-200 bg-white" />;
}
