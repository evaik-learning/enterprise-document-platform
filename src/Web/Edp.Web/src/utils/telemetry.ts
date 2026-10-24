/**
 * Minimal client telemetry. Only route, latency, status, correlation id and app version are captured -
 * never tokens, document content or request/response payloads.
 */
export interface ApiTelemetryEvent {
  method: string;
  path: string;
  status: number | 'network-error';
  durationMs: number;
  correlationId: string;
}

type Sink = (event: ApiTelemetryEvent & { route: string; appVersion: string }) => void;

let sink: Sink | undefined;

export function setTelemetrySink(next: Sink | undefined) {
  sink = next;
}

export function trackApiCall(event: ApiTelemetryEvent) {
  if (!sink) return;
  // Query strings may carry search terms; keep only the path.
  sink({
    ...event,
    path: event.path.split('?')[0] ?? event.path,
    route: window.location.pathname,
    appVersion: import.meta.env.VITE_APP_VERSION ?? '0.0.0'
  });
}
