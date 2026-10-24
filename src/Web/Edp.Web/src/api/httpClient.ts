import { appConfig } from '../app/config';
import { trackApiCall } from '../utils/telemetry';
import { ApiError, parseProblem } from './errors';

export type QueryValue = string | number | boolean | null | undefined;

export interface RequestOptions {
  query?: Record<string, QueryValue>;
  headers?: Record<string, string>;
  signal?: AbortSignal;
  /** Overrides the generated Idempotency-Key on POST requests. */
  idempotencyKey?: string;
  /** Retry budget for safe (GET/HEAD) requests on transient failures. */
  retries?: number;
  /** Do not try to renew the session or trigger the login redirect on 401. */
  skipSessionRenewal?: boolean;
}

export interface DownloadResult {
  blob: Blob;
  fileName?: string;
  contentType: string;
}

type Method = 'GET' | 'HEAD' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

const SAFE_METHODS = new Set<Method>(['GET', 'HEAD']);
const TRANSIENT_STATUSES = new Set([429, 502, 503, 504]);

/** Mutable so tests can remove back-off delays. */
export const httpSettings = { retryBaseDelayMs: 300, maxRetries: 2 };

let unauthorizedHandler: (() => void) | undefined;

/** Registered by the auth layer: clears session state and routes to login (preserving the intended route). */
export function setUnauthorizedHandler(handler: (() => void) | undefined) {
  unauthorizedHandler = handler;
}

const csrfToken = () =>
  document.cookie
    .split('; ')
    .find((part) => part.startsWith('edp-csrf='))
    ?.split('=')
    .slice(1)
    .join('=');

const delay = (ms: number) => new Promise<void>((resolve) => setTimeout(resolve, ms));

function backoff(attempt: number, response?: Response) {
  const retryAfter = Number(response?.headers.get('Retry-After'));
  if (Number.isFinite(retryAfter) && retryAfter > 0) return Math.min(retryAfter * 1000, 10_000);
  const exponential = httpSettings.retryBaseDelayMs * 2 ** attempt;
  return exponential === 0 ? 0 : exponential + Math.random() * 100;
}

export function buildUrl(path: string, query?: Record<string, QueryValue>) {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value !== undefined && value !== null && value !== '') params.set(key, String(value));
  }
  const queryString = params.toString();
  const separator = path.includes('?') ? '&' : '?';
  return `${appConfig.gatewayBaseUrl}${path}${queryString ? `${separator}${queryString}` : ''}`;
}

let renewal: Promise<boolean> | undefined;

/** Asks the Gateway whether the BFF session is still valid. Concurrent callers share one probe. */
function renewSession() {
  renewal ??= fetch(`${appConfig.gatewayBaseUrl}/bff/auth/user`, { credentials: 'include', headers: { Accept: 'application/json' } })
    .then(async (response) => response.ok && ((await response.json()) as { isAuthenticated?: boolean }).isAuthenticated === true)
    .catch(() => false)
    .finally(() => { renewal = undefined; });
  return renewal;
}

async function toApiError(response: Response, correlationId: string) {
  let body: unknown;
  try {
    const text = await response.text();
    body = text ? JSON.parse(text) : undefined;
  } catch {
    body = undefined; // non-JSON bodies (HTML error pages, proxies) are never shown to users
  }
  return parseProblem(body, response.status, response.headers.get('X-Correlation-ID') ?? correlationId);
}

function parseFileName(disposition: string | null) {
  if (!disposition) return undefined;
  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(disposition)?.[1];
  if (encoded) {
    try { return decodeURIComponent(encoded); } catch { /* fall through to plain filename */ }
  }
  return /filename="?([^";]+)"?/i.exec(disposition)?.[1];
}

async function execute(method: Method, path: string, body: unknown, options: RequestOptions, mode: 'json' | 'blob') {
  const correlationId = crypto.randomUUID();
  const url = buildUrl(path, options.query);
  const headers = new Headers(options.headers);
  headers.set('Accept', mode === 'json' ? 'application/json' : '*/*');
  headers.set('X-Requested-With', 'XMLHttpRequest');
  headers.set('X-Correlation-ID', correlationId);

  const isSafe = SAFE_METHODS.has(method);
  if (!isSafe) {
    const token = csrfToken();
    if (token) headers.set('X-CSRF-TOKEN', token);
    if (method === 'POST') headers.set('Idempotency-Key', options.idempotencyKey ?? crypto.randomUUID());
  }

  let payload: BodyInit | undefined;
  if (body instanceof FormData) payload = body;
  else if (body !== undefined) {
    headers.set('Content-Type', 'application/json');
    payload = JSON.stringify(body);
  }

  const maxRetries = isSafe ? (options.retries ?? httpSettings.maxRetries) : 0;
  let attempt = 0;
  let sessionChecked = false;

  for (;;) {
    const started = performance.now();
    let response: Response;
    try {
      response = await fetch(url, { method, headers, body: payload, credentials: 'include', signal: options.signal });
    } catch (error) {
      if (options.signal?.aborted) throw error;
      trackApiCall({ method, path, status: 'network-error', durationMs: performance.now() - started, correlationId });
      if (attempt < maxRetries) {
        await delay(backoff(attempt++));
        continue;
      }
      throw new ApiError({ status: 0, code: 'NETWORK_ERROR', message: 'Unable to reach the server. Check your connection and try again.', correlationId });
    }

    trackApiCall({ method, path, status: response.status, durationMs: performance.now() - started, correlationId });
    if (response.ok) return readBody(response, mode);

    if (response.status === 401 && !options.skipSessionRenewal) {
      const alreadyChecked = sessionChecked;
      sessionChecked = true;
      if (!alreadyChecked && (await renewSession())) {
        if (isSafe) continue; // session is still valid: retry the safe request once
        throw new ApiError({ status: 401, code: 'SESSION_REFRESHED', message: 'Your session was refreshed. Please repeat the action.', correlationId });
      }
      unauthorizedHandler?.();
    }

    if (TRANSIENT_STATUSES.has(response.status) && attempt < maxRetries) {
      await delay(backoff(attempt++, response));
      continue;
    }
    throw await toApiError(response, correlationId);
  }
}

async function readBody(response: Response, mode: 'json' | 'blob'): Promise<unknown> {
  if (mode === 'blob') {
    const blob = await response.blob();
    return { blob, fileName: parseFileName(response.headers.get('Content-Disposition')), contentType: blob.type || response.headers.get('Content-Type') || 'application/octet-stream' } satisfies DownloadResult;
  }
  if (response.status === 204) return undefined;
  const text = await response.text();
  return text ? JSON.parse(text) : undefined;
}

/** Single entry point for every browser -> Gateway call. */
export const httpClient = {
  get: <T>(path: string, options: RequestOptions = {}) => execute('GET', path, undefined, options, 'json') as Promise<T>,
  post: <T>(path: string, body?: unknown, options: RequestOptions = {}) => execute('POST', path, body, options, 'json') as Promise<T>,
  put: <T>(path: string, body?: unknown, options: RequestOptions = {}) => execute('PUT', path, body, options, 'json') as Promise<T>,
  patch: <T>(path: string, body?: unknown, options: RequestOptions = {}) => execute('PATCH', path, body, options, 'json') as Promise<T>,
  delete: <T = void>(path: string, options: RequestOptions = {}) => execute('DELETE', path, undefined, options, 'json') as Promise<T>,
  download: (path: string, options: RequestOptions = {}) => execute('GET', path, undefined, options, 'blob') as Promise<DownloadResult>
};

/** Multipart upload with progress; uses XHR because fetch cannot report upload progress. */
export function uploadWithProgress<T>(path: string, form: FormData, onProgress?: (percent: number) => void, signal?: AbortSignal) {
  return new Promise<T>((resolve, reject) => {
    const correlationId = crypto.randomUUID();
    const request = new XMLHttpRequest();
    request.open('POST', buildUrl(path));
    request.withCredentials = true;
    request.setRequestHeader('Accept', 'application/json');
    request.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
    request.setRequestHeader('X-Correlation-ID', correlationId);
    request.setRequestHeader('Idempotency-Key', crypto.randomUUID());
    const token = csrfToken();
    if (token) request.setRequestHeader('X-CSRF-TOKEN', token);

    request.upload.onprogress = (event) => {
      if (event.lengthComputable) onProgress?.(Math.round((event.loaded / event.total) * 100));
    };
    request.onerror = () => reject(new ApiError({ status: 0, code: 'NETWORK_ERROR', message: 'Upload failed. Check your connection and try again.', correlationId }));
    request.onabort = () => reject(new DOMException('Upload cancelled', 'AbortError'));
    request.onload = () => {
      let parsed: unknown;
      try { parsed = request.responseText ? JSON.parse(request.responseText) : undefined; } catch { parsed = undefined; }
      if (request.status >= 200 && request.status < 300) resolve(parsed as T);
      else {
        if (request.status === 401) unauthorizedHandler?.();
        reject(parseProblem(parsed, request.status, request.getResponseHeader('X-Correlation-ID') ?? correlationId));
      }
    };
    signal?.addEventListener('abort', () => request.abort(), { once: true });
    request.send(form);
  });
}
