// Every backend endpoint here is BodyStyle=Bare, POST, JSON in and out --
// see Tausend.Core/Services/I*.cs for the WCF contracts this mirrors.
const API_BASE = '/api'

// Module-level token, injected into every request automatically -- mirrors the mobile app's
// api/client.ts exactly (setAccessToken/getAccessToken + auto-injection here), so every
// api/*.ts function drops the "pass the token in by hand" parameter every call site used to
// repeat. AuthContext calls setAccessToken() whenever the session changes (login, restore from
// localStorage, logout).
let currentAccessToken: string | null = null

export function setAccessToken(token: string | null): void {
  currentAccessToken = token
}

export function getAccessToken(): string | null {
  return currentAccessToken
}

export async function apiPost<TResponse>(path: string, body: Record<string, unknown> = {}): Promise<TResponse> {
  const payload = currentAccessToken ? { AccessToken: currentAccessToken, ...body } : body
  const res = await fetch(`${API_BASE}/${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
  if (!res.ok) {
    throw new Error(`${path} failed with HTTP ${res.status}`)
  }
  return (await res.json()) as TResponse
}
