import { recordCall } from "../telemetry/callLog";

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5280";

/** Header the API returns so the browser can show a user which trace to quote. */
const TRACE_ID_HEADER = "X-Trace-Id";

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

interface RequestOptions {
  /** Business action this call belongs to, sent as baggage and copied onto every server span. */
  action: string;
  screen: string;
  method?: string;
  body?: unknown;
  query?: Record<string, string | number | undefined>;
}

/**
 * Every call to the API goes through here.
 *
 * The only telemetry this front end does is one `baggage` header. There is no OpenTelemetry
 * SDK in the browser, no exporter and no collector. The server already creates spans for the
 * request, and Shared.Observability copies allow-listed baggage onto all of them, down to the
 * SQL span. So searching `app.action = "create-bookmark"` in the backend shows every piece of
 * work one button press caused.
 */
export async function apiFetch<T>(path: string, options: RequestOptions): Promise<T> {
  const { action, screen, method = "GET", body, query } = options;

  const url = new URL(BASE_URL + path);
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value !== undefined && value !== "") url.searchParams.set(key, String(value));
  }

  const headers = new Headers();
  headers.set(
    "baggage",
    `app.action=${encodeURIComponent(action)},app.screen=${encodeURIComponent(screen)}`,
  );
  if (body !== undefined) headers.set("content-type", "application/json");

  const startedAt = performance.now();
  const response = await fetch(url, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  recordCall({
    action,
    method,
    path: url.pathname + url.search,
    status: response.status,
    traceId: response.headers.get(TRACE_ID_HEADER),
    durationMs: Math.round(performance.now() - startedAt),
  });

  if (!response.ok) throw new ApiError(response.status, await readProblem(response));
  if (response.status === 204) return undefined as T;

  return (await response.json()) as T;
}

async function readProblem(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as { detail?: string; title?: string };
    return problem.detail ?? problem.title ?? `Request failed with ${response.status}`;
  } catch {
    return `Request failed with ${response.status}`;
  }
}
