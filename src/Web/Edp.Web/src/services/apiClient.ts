export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly code?: string,
    public readonly correlationId?: string,
    public readonly errors?: Record<string, string[]>
  ) {
    super(message);
  }
}

function csrfToken() {
  return document.cookie
    .split('; ')
    .find((part) => part.startsWith('edp-csrf='))
    ?.split('=').slice(1).join('=');
}

async function request<TResponse>(path: string, init: RequestInit = {}): Promise<TResponse> {
  const method = (init.method ?? 'GET').toUpperCase();
  const headers = new Headers(init.headers);
  headers.set('Accept', headers.get('Accept') ?? 'application/json');
  headers.set('X-Requested-With', 'XMLHttpRequest');
  if (!['GET', 'HEAD', 'OPTIONS'].includes(method)) {
    const token = csrfToken();
    if (token) headers.set('X-CSRF-TOKEN', token);
  }

  const response = await fetch(path, {
    ...init,
    credentials: 'include',
    headers
  });

  if (!response.ok) {
    let problem: { detail?: string; title?: string; code?: string; errors?: Record<string, string[]> } = {};
    try { problem = await response.json(); } catch { /* non-json response */ }
    throw new ApiError(
      problem.detail ?? problem.title ?? `Request failed with status ${response.status}`,
      response.status,
      problem.code,
      response.headers.get('X-Correlation-ID') ?? response.headers.get('traceparent') ?? undefined,
      problem.errors
    );
  }

  if (response.status === 204) return undefined as TResponse;
  return response.json() as Promise<TResponse>;
}

export async function apiGet<TResponse>(path: string, init?: RequestInit) {
  return request<TResponse>(path, { ...init, method: 'GET' });
}

export async function apiSend<TResponse>(path: string, method: 'POST' | 'PUT' | 'PATCH' | 'DELETE', body?: unknown, init?: RequestInit) {
  const headers = new Headers(init?.headers);
  let requestBody: BodyInit | undefined;
  if (body instanceof FormData) requestBody = body;
  else if (body !== undefined) {
    headers.set('Content-Type', 'application/json');
    requestBody = JSON.stringify(body);
  }
  return request<TResponse>(path, { ...init, method, headers, body: requestBody });
}

export async function apiDownload(path: string, init?: RequestInit) {
  const response = await fetch(path, { ...init, credentials: 'include', headers: { Accept: '*/*', ...init?.headers } });
  if (!response.ok) throw new ApiError(`Download failed with status ${response.status}`, response.status);
  return { blob: await response.blob(), fileName: response.headers.get('Content-Disposition')?.match(/filename="?([^";]+)"?/)?.[1] };
}
