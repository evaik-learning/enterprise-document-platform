import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router';
import { DEFAULT_PAGE_SIZE } from '../api/contracts';

const PAGE_SIZES = [10, 25, 50, 100];

export interface UrlListState {
  get: (key: string) => string;
  page: number;
  pageSize: number;
  /** Updates filters; resets to page 1 unless `page` is part of the update. */
  update: (updates: Record<string, string | number | null | undefined>) => void;
  setPage: (page: number) => void;
  setPageSize: (pageSize: number) => void;
}

/** List filters/pagination live in the URL so views are refresh-safe, bookmarkable and shareable. */
export function useUrlListState(defaultPageSize = DEFAULT_PAGE_SIZE): UrlListState {
  const [params, setParams] = useSearchParams();

  const update = useCallback(
    (updates: Record<string, string | number | null | undefined>) => {
      setParams((previous) => {
        const next = new URLSearchParams(previous);
        for (const [key, value] of Object.entries(updates)) {
          if (value === undefined || value === null || value === '') next.delete(key);
          else next.set(key, String(value));
        }
        if (!('page' in updates)) next.delete('page');
        return next;
      });
    },
    [setParams]
  );

  return useMemo(() => {
    const rawPage = Number(params.get('page'));
    const rawSize = Number(params.get('pageSize'));
    return {
      get: (key) => params.get(key) ?? '',
      page: Number.isInteger(rawPage) && rawPage >= 1 ? rawPage : 1,
      pageSize: PAGE_SIZES.includes(rawSize) ? rawSize : defaultPageSize,
      update,
      setPage: (page) => update({ page: page <= 1 ? null : page }),
      setPageSize: (pageSize) => update({ pageSize, page: null })
    };
  }, [defaultPageSize, params, update]);
}
