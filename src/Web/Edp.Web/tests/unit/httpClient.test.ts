import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError, parseProblem, toUserMessage } from '../../src/api/errors';
import { buildUrl, httpClient, httpSettings, setUnauthorizedHandler } from '../../src/api/httpClient';
import { headerOf, json, mockFetch } from '../helpers';

beforeEach(() => {
  httpSettings.retryBaseDelayMs = 0;
  document.cookie = 'edp-csrf=csrf-token-1';
});

afterEach(() => {
  vi.unstubAllGlobals();
  setUnauthorizedHandler(undefined);
  document.cookie = 'edp-csrf=; expires=Thu, 01 Jan 1970 00:00:00 GMT';
});

describe('error contract', () => {
  it('parses the gateway error shape', () => {
    const error = parseProblem({ code: 'DOCUMENT_NOT_FOUND', message: 'Document was not found.', correlationId: 'abc-123', details: [{ field: 'Name', message: 'Required' }, 'plain'] }, 404);
    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 404, code: 'DOCUMENT_NOT_FOUND', message: 'Document was not found.', correlationId: 'abc-123' });
    expect(error.fieldErrors()).toEqual({ name: 'Required' });
    expect(error.isNotFound).toBe(true);
  });

  it('parses ASP.NET validation problems and falls back to the header correlation id', () => {
    const error = parseProblem({ title: 'One or more validation errors occurred.', errors: { Code: ['Code is required.'] } }, 400, 'hdr-1');
    expect(error.isValidation).toBe(true);
    expect(error.correlationId).toBe('hdr-1');
    expect(error.fieldErrors().code).toBe('Code is required.');
  });

  it('uses safe defaults for empty bodies and never leaks server error wording', () => {
    expect(parseProblem(undefined, 403).message).toMatch(/permission/i);
    expect(parseProblem(undefined, 409).message).toMatch(/changed by another user/i);
    const server = parseProblem({ detail: 'SqlException: Login failed for user sa at line 12' }, 500, 'ref-9');
    expect(toUserMessage(server)).not.toMatch(/Sql/);
    expect(toUserMessage(new Error('boom'))).toBe('Something went wrong. Please try again.');
  });
});

describe('request construction', () => {
  it('builds query strings skipping empty values', () => {
    expect(buildUrl('/api/v1/documents', { page: 2, search: '', status: undefined, q: 'a b' })).toBe('/api/v1/documents?page=2&q=a+b');
    expect(buildUrl('/x?a=1', { b: 2 })).toBe('/x?a=1&b=2');
  });

  it('sends correlation id, csrf token and idempotency key on commands', async () => {
    const fetchMock = mockFetch(() => json({ id: 1 }));
    await httpClient.post('/api/v1/templates', { name: 'A' }, { idempotencyKey: 'key-1' });
    const [, init] = fetchMock.mock.calls[0]!;
    expect(headerOf(init, 'X-Correlation-ID')).toBeTruthy();
    expect(headerOf(init, 'X-CSRF-TOKEN')).toBe('csrf-token-1');
    expect(headerOf(init, 'Idempotency-Key')).toBe('key-1');
    expect(headerOf(init, 'Content-Type')).toBe('application/json');
    expect(init?.credentials).toBe('include');
  });

  it('does not attach csrf or idempotency headers to reads', async () => {
    const fetchMock = mockFetch(() => json({}));
    await httpClient.get('/api/v1/me');
    const [, init] = fetchMock.mock.calls[0]!;
    expect(headerOf(init, 'X-CSRF-TOKEN')).toBeNull();
    expect(headerOf(init, 'Idempotency-Key')).toBeNull();
  });

  it('returns undefined for 204 and parses download file names', async () => {
    mockFetch(() => new Response(null, { status: 204 }));
    await expect(httpClient.post('/x')).resolves.toBeUndefined();
    mockFetch(() => new Response('pdf', { status: 200, headers: { 'Content-Type': 'application/pdf', 'Content-Disposition': 'attachment; filename="Offer.pdf"' } }));
    const result = await httpClient.download('/api/v1/documents/1/download');
    expect(result.fileName).toBe('Offer.pdf');
    expect(result.contentType).toContain('pdf');
  });
});

describe('failure handling', () => {
  it('throws a normalized ApiError with the correlation id', async () => {
    mockFetch(() => json({ code: 'TEMPLATE_CODE_EXISTS', message: 'Exists.' }, { status: 409, headers: { 'X-Correlation-ID': 'corr-1' } }));
    await expect(httpClient.post('/api/v1/templates', {})).rejects.toMatchObject({ status: 409, code: 'TEMPLATE_CODE_EXISTS', correlationId: 'corr-1' });
  });

  it('retries safe requests on transient failures and gives up after the budget', async () => {
    let calls = 0;
    mockFetch(() => (++calls < 3 ? new Response('', { status: 503 }) : json({ ok: true })));
    await expect(httpClient.get('/api/v1/dashboard')).resolves.toEqual({ ok: true });
    expect(calls).toBe(3);

    calls = 0;
    mockFetch(() => { calls++; return new Response('', { status: 503 }); });
    await expect(httpClient.get('/api/v1/dashboard')).rejects.toMatchObject({ status: 503 });
    expect(calls).toBe(3);
  });

  it('never retries commands automatically', async () => {
    let calls = 0;
    mockFetch(() => { calls++; return new Response('', { status: 503 }); });
    await expect(httpClient.post('/api/v1/documents', {})).rejects.toMatchObject({ status: 503 });
    expect(calls).toBe(1);
  });

  it('reports network failures after retrying reads', async () => {
    const fetchMock = vi.fn(async () => { throw new TypeError('Failed to fetch'); });
    vi.stubGlobal('fetch', fetchMock);
    await expect(httpClient.get('/api/v1/me')).rejects.toMatchObject({ status: 0, code: 'NETWORK_ERROR' });
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });
});

describe('session expiry (401)', () => {
  it('renews the session and retries a safe request once when the session is still valid', async () => {
    let first = true;
    const fetchMock = mockFetch((url) => {
      if (url.endsWith('/bff/auth/user')) return json({ isAuthenticated: true });
      if (first) { first = false; return new Response('', { status: 401 }); }
      return json({ items: [] });
    });
    const handler = vi.fn();
    setUnauthorizedHandler(handler);
    await expect(httpClient.get('/api/v1/documents')).resolves.toEqual({ items: [] });
    expect(handler).not.toHaveBeenCalled();
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });

  it('clears the session and signals the login redirect when renewal fails', async () => {
    mockFetch((url) => (url.endsWith('/bff/auth/user') ? json({ isAuthenticated: false }) : new Response('', { status: 401 })));
    const handler = vi.fn();
    setUnauthorizedHandler(handler);
    await expect(httpClient.get('/api/v1/documents')).rejects.toMatchObject({ status: 401 });
    expect(handler).toHaveBeenCalledTimes(1);
  });

  it('does not silently replay a command after the session was refreshed', async () => {
    let posts = 0;
    mockFetch((url, init) => {
      if (url.endsWith('/bff/auth/user')) return json({ isAuthenticated: true });
      if (init.method === 'POST') posts++;
      return new Response('', { status: 401 });
    });
    await expect(httpClient.post('/api/v1/documents', {})).rejects.toMatchObject({ status: 401, code: 'SESSION_REFRESHED' });
    expect(posts).toBe(1);
  });

  it('can skip renewal for the session probe itself', async () => {
    const fetchMock = mockFetch(() => new Response('', { status: 401 }));
    await expect(httpClient.get('/api/v1/me', { skipSessionRenewal: true, retries: 0 })).rejects.toMatchObject({ status: 401 });
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });
});
