export interface ApiErrorDetail {
  field?: string;
  message: string;
}

export interface ApiErrorInit {
  status: number;
  message: string;
  code?: string;
  correlationId?: string;
  details?: ApiErrorDetail[];
}

/** Normalized error for every Gateway/service failure (see the Phase 10 error contract). */
export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;
  readonly correlationId?: string;
  readonly details: ApiErrorDetail[];

  constructor(init: ApiErrorInit) {
    super(init.message);
    this.name = 'ApiError';
    this.status = init.status;
    this.code = init.code;
    this.correlationId = init.correlationId;
    this.details = init.details ?? [];
  }

  get isUnauthorized() { return this.status === 401; }
  get isForbidden() { return this.status === 403; }
  get isNotFound() { return this.status === 404; }
  get isConflict() { return this.status === 409; }
  get isValidation() { return this.status === 400 || this.status === 422; }
  get isRateLimited() { return this.status === 429; }
  get isUnavailable() { return this.status === 503 || this.status === 502 || this.status === 504; }
  get isServerError() { return this.status >= 500; }

  /** Field-level messages keyed by (case-insensitive) field name. */
  fieldErrors(): Record<string, string> {
    const result: Record<string, string> = {};
    for (const detail of this.details) {
      if (detail.field && !(detail.field.toLowerCase() in result)) result[detail.field.toLowerCase()] = detail.message;
    }
    return result;
  }
}

const text = (value: unknown) => (typeof value === 'string' && value.trim() ? value : undefined);

/** Accepts the Gateway contract, RFC 7807 problem details and ASP.NET validation problems. */
export function parseProblem(body: unknown, status: number, headerCorrelationId?: string | null): ApiError {
  const record = (typeof body === 'object' && body !== null ? body : {}) as Record<string, unknown>;
  const details: ApiErrorDetail[] = [];

  if (Array.isArray(record.details)) {
    for (const item of record.details) {
      if (typeof item === 'string') details.push({ message: item });
      else if (item && typeof item === 'object') {
        const entry = item as Record<string, unknown>;
        const message = text(entry.message) ?? text(entry.error);
        if (message) details.push({ field: text(entry.field) ?? text(entry.name), message });
      }
    }
  }

  if (record.errors && typeof record.errors === 'object' && !Array.isArray(record.errors)) {
    for (const [field, messages] of Object.entries(record.errors as Record<string, unknown>)) {
      for (const message of Array.isArray(messages) ? messages : [messages]) {
        if (typeof message === 'string') details.push({ field, message });
      }
    }
  }

  return new ApiError({
    status,
    code: text(record.code),
    message: text(record.message) ?? text(record.detail) ?? text(record.title) ?? defaultMessage(status),
    correlationId: text(record.correlationId) ?? headerCorrelationId ?? undefined,
    details
  });
}

export function defaultMessage(status: number) {
  switch (status) {
    case 400:
    case 422: return 'The request contains invalid data.';
    case 401: return 'Your session has expired. Please sign in again.';
    case 403: return 'You do not have permission to perform this action.';
    case 404: return 'The requested item was not found.';
    case 409: return 'This record was changed by another user.';
    case 429: return 'Too many requests. Please wait a moment and try again.';
    case 503: return 'The service is temporarily unavailable. Please try again shortly.';
    default: return status >= 500 ? 'Something went wrong on our side.' : `Request failed (${status}).`;
  }
}

/** Safe, user-presentable text for any thrown value; never exposes stack traces. */
export function toUserMessage(error: unknown, fallback = 'Something went wrong. Please try again.') {
  if (error instanceof ApiError) {
    // Server errors hide backend wording; the reference id lets support trace the request.
    return error.isServerError && !error.isUnavailable ? defaultMessage(error.status) : error.message;
  }
  return fallback;
}

export const correlationReference = (error: unknown) => (error instanceof ApiError ? error.correlationId : undefined);
