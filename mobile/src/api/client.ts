// Every backend endpoint is BodyStyle=Bare, POST, JSON in and out -- see
// Tausend.Core/Services/I*.cs for the WCF contracts this mirrors (same convention
// dashboard/src/api/client.ts talks to). The backend takes AccessToken as a body
// field on each request rather than an Authorization header, so "interceptor" here
// means every apiPost call funnels through this one function that injects it --
// callers never thread the token through by hand.
//
// Production API_BASE rides on the dashboard's existing domain (app.alarmastausend.com/api),
// which nginx already proxies to the backend for the dashboard's own same-origin calls --
// see dashboard/nginx's tausend-dashboard site, "location /api/". A dedicated api.alarmastausend.com
// was tried first, but that domain resolved to an unrelated third-party server (not the VPS),
// so this reuses the domain that's already live with real TLS instead of waiting on separate
// DNS/certificate setup for a second domain.
//
// Debug builds instead hit the backend directly over plain HTTP at its public IP
// (157.230.209.102) -- nginx's port-80 default server answers there for any Host it doesn't have
// a dedicated site for, so this needs no SSH tunnel and works from any network, unlike the
// previous 10.0.2.2-tunnel setup. Cleartext is fine for this dev-only path (see the debug-only
// network_security_config.xml override); it's exactly what production must NOT do.
const API_BASE = __DEV__ ? 'http://157.230.209.102' : 'https://app.alarmastausend.com/api';

let currentAccessToken: string | null = null;

export function setAccessToken(token: string | null): void {
  currentAccessToken = token;
}

export function getAccessToken(): string | null {
  return currentAccessToken;
}

// Panel commands round-trip through a UDP relay to physical hardware that can simply never
// answer (powered off, out of signal) -- without a timeout, `fetch` waits indefinitely and the
// UI just spins forever with no way to distinguish "still trying" from "never coming back".
const REQUEST_TIMEOUT_MS = 20000;

// Thrown instead of letting AbortController's own AbortError leak out -- callers can check
// `error instanceof ApiTimeoutError` to show a specific "took too long" message distinct from
// "could not reach the server" (a real connection/DNS failure) or a parsed error response.
export class ApiTimeoutError extends Error {
  constructor(path: string) {
    super(`${path} timed out after ${REQUEST_TIMEOUT_MS}ms`);
    this.name = 'ApiTimeoutError';
  }
}

export async function apiPost<TResponse>(
  path: string,
  body: Record<string, unknown> = {},
): Promise<TResponse> {
  const payload = currentAccessToken ? { AccessToken: currentAccessToken, ...body } : body;
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);
  let res: Response;
  try {
    res = await fetch(`${API_BASE}/${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
      signal: controller.signal,
    });
  } catch (e) {
    if (e instanceof Error && e.name === 'AbortError') {
      throw new ApiTimeoutError(path);
    }
    throw e;
  } finally {
    clearTimeout(timeoutId);
  }
  if (!res.ok) {
    throw new Error(`${path} failed with HTTP ${res.status}`);
  }
  return (await res.json()) as TResponse;
}
