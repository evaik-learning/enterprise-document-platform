import type { DownloadResult } from '../api/httpClient';

/** Saves a blob returned by an authorized backend download. Object URLs are short-lived and never persisted. */
export function saveBlob(result: DownloadResult, fallbackName: string) {
  const url = URL.createObjectURL(result.blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = result.fileName ?? fallbackName;
  anchor.rel = 'noopener';
  document.body.append(anchor);
  anchor.click();
  anchor.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
