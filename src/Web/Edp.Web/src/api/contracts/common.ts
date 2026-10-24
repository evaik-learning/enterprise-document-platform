export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  /** Present when the backend reports it; otherwise only `hasNextPage` is reliable. */
  totalCount?: number;
  totalPages?: number;
  hasNextPage: boolean;
}

export interface PageQuery {
  page?: number;
  pageSize?: number;
}

/** Normalizes bare arrays and the various paged envelopes returned by the services into `Paged<T>`. */
export function toPaged<T>(raw: unknown, page: number, pageSize: number): Paged<T> {
  if (Array.isArray(raw)) return { items: raw as T[], page, pageSize, hasNextPage: raw.length >= pageSize };
  const record = (raw ?? {}) as { items?: T[]; totalCount?: number; totalPages?: number; hasNextPage?: boolean; page?: number; pageSize?: number };
  const items = record.items ?? [];
  const currentPage = record.page ?? page;
  const size = record.pageSize ?? pageSize;
  const totalPages = record.totalPages ?? (record.totalCount !== undefined && size > 0 ? Math.ceil(record.totalCount / size) : undefined);
  const hasNextPage = record.hasNextPage ?? (totalPages !== undefined ? currentPage < totalPages : items.length >= size);
  return { items, page: currentPage, pageSize: size, totalCount: record.totalCount, totalPages, hasNextPage };
}

export const DEFAULT_PAGE_SIZE = 25;
