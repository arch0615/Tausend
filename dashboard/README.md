# Tausend Admin Dashboard

React + TypeScript (Vite) fleet-admin dashboard -- separate from the backend and
the mobile app, talks to `AccountService`/`AdminService` over plain HTTP POST/JSON,
matching the WCF `UriTemplate` contracts both backends (`../backend` and
`../backend-core`) expose.

## Running locally

You need a backend running first -- either works, since both expose the same
routes/JSON shape (see `../backend-core/README.md`):

```
cd ../backend-core/src/TausendBackend.Api
dotnet run
```

Then, in this directory:

```
npm install
npm run dev
```

`vite.config.ts` proxies `/api/*` to `http://localhost:5000` (backend-core's
default Kestrel port) server-to-server, so the browser never makes a
cross-origin request and the backend needs no CORS handling in dev. If you're
testing against the WCF backend instead, point that proxy target at wherever
it's actually hosted.

You'll need an **enabled Admin-role account** to sign in -- `RequireAdmin`
blocks anything else. There's no seed/bootstrap script; promote an account via
`dbo.SetAccountRole` directly against the database, or (once one admin exists)
via `AdminService/SetAccountRole`.

## What's here

- **Routing shell** (`App.tsx`) -- `/login`, admin-gated `/accounts` and
  `/devices` under a shared sidebar layout (`DashboardLayout`); any other path
  redirects to `/accounts` (which itself redirects to `/login` if not
  authenticated).
- **Auth** (`auth/AuthContext.tsx`, `auth/RequireAdmin.tsx`) -- login persists
  the account (including its access token) to `localStorage` so a refresh
  doesn't sign you out. `RequireAdmin` is a client-side UX gate only -- the
  real authorization boundary is server-side, every `AdminService` call
  re-checks the token's role regardless of what this renders. When a call
  comes back `Unauthorized` (dead/expired token), the page signs out and
  returns to `/login` instead of leaving a permanent error banner up.
- **Accounts / Devices pages** -- fleet-wide listings via
  `EnumAllAccounts`/`EnumAllDevices`. No search/filter or role-assignment UI
  yet -- that's Day 7/8 scope.

## Known gaps (not fixed here, worth knowing about)

- The access token lives in `localStorage`, readable by any script that gets
  injected (XSS). That's a common SPA tradeoff, but worth being deliberate
  about given this dashboard can see and modify the entire account/device
  fleet -- moving to an httpOnly cookie would need backend changes on both
  sides and isn't a small follow-up.
- No automatic token refresh -- when the access token expires the admin is
  bounced to `/login` (see above) rather than silently refreshed.
