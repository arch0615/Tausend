# Tausend Backend (ASP.NET Core / .NET 8)

A from-scratch port of the WCF/.NET Framework backend in `../backend`, built to run
natively on Ubuntu (or any Linux/macOS/Windows host) instead of requiring IIS.

## Why this exists

The original backend (`../backend`) targets .NET Framework 4.7.2 and hosts WCF
services through IIS -- both Windows-only. This is a re-hosting of the same logic
on modern, cross-platform .NET, so it can run on the Ubuntu VPS instead of requiring
a (more expensive) Windows Server VPS.

## What's ported

Everything in the original WCF backend is now ported: Account, Admin, Device,
Command, and Notification services (including the full push-notification
subsystem), plus the relay integration.

- **Account service** -- create account, login, delete, change password, logout,
  access/refresh tokens, password reset by link. Same bcrypt hashing, same
  legacy-SHA256-account auto-migration, same token-expiry and malformed-token
  handling as the original.
- **Admin service** -- fleet-wide account/device listing, role assignment. Same
  Admin-only authorization check as the original -- including the Day 5 fix that
  distinguishes 401 (not logged in) from 403 (logged in but not admin); this
  backend never had the pre-fix bug ported into its shipped behavior.
- **Device service** -- create/update/delete devices and SMS devices, zones,
  exclusions, PGM outputs, users (PIN-holder labels), block/reset PIN, panel
  disassociation, live PIN validation against the panel on create/update. The
  ownership-check fix from `../backend/DAY5_SUMMARY.md` (a logged-in account must
  actually own the target device, not just be logged in as *someone*) is built in
  from the start here, not ported-then-patched -- see `Dao/DeviceDao.cs`.
- **Command service** -- arm/disarm/day-arm/night-arm, panic/emergency/assault,
  status/fail-status/zone-status, PGM control, installer commands, battery status,
  clock get/sync. `Business/CommandBusiness.cs` is the HTTP client for the relay's
  `PrivateService` (see Relay integration below); it's async throughout (the
  original blocked synchronously on its WCF/RestSharp client) but replicates the
  original's raw-string response parsing deliberately, see the class doc comment.
- **Notification service** -- panel event ingestion (`NotifyEvent`, relay-only),
  event history (`EnumEvents`), test push (`SendTestNotification`), and the full
  push-notification subsystem: ~44 notification-type classes, the FCM v1 HTTP API
  client (OAuth2 via `Google.Apis.Auth`), and the same "real alarm vs routine event"
  channel/sound split described in `../backend/PUSH_ROLLOUT.md` (the iOS
  ROLLOUT-PHASE-1 hardcoding is preserved verbatim, not "fixed").

Routes intentionally match the original WCF `UriTemplate`s exactly
(`POST /AccountService/Login`, `POST /AdminService/EnumAllAccounts`, etc.), and the
JSON is configured to keep the same PascalCase property names. Any existing client
(the admin dashboard, a future mobile app) can point at this backend instead of the
WCF one with no code changes -- just a different base URL.

## Relay integration

This backend is an HTTP client of the relay's `PrivateService` for the command path,
and exposes relay-facing endpoints (`DeviceService/UpdateDeviceConnectionParameters`,
`UpdateDeviceLastConnection`, `NotificationService/NotifyEvent`, plus the relay-or-user
dual path on `GetDeviceByID`/`GetDeviceByIdentifier`/`DisassociateCentral`) gated on
the shared `RelaySystemKey` via `Security/SystemAuth.cs` and the `X-System-Key`
header -- functionally identical to `Tausend.Core.Security.SystemAuth`, just
without a WCF-style static ambient request context (controllers read the header via
`HttpRequest`/`IConfiguration` directly instead). `RelaySystemKey` must be
provisioned with the *same* value as the relay's own config for any of this to
authenticate -- see Configuration below.

**The relay itself is now `../src/TausendRelay`**, a from-scratch .NET 8 port of
`../backend/Tausend.UDPListener` -- same reason this project exists: the original
relay is classic .NET Framework using WCF self-hosting (`System.ServiceModel`), a
Windows-only stack, which meant "run everything on Ubuntu" actually meant "run the
API on Ubuntu and the relay on a second, Windows box." `TausendRelay` removes that
split -- it's a plain ASP.NET Core app (UDP listener as a `BackgroundService`, plus
minimal-API endpoints reproducing `PrivateService`'s exact WCF-Wrapped response shape
so this project's own `CommandBusiness` needed zero changes) that runs on the same
Ubuntu box as this API. See `../src/TausendRelay/README.md` for what changed in the
port, including two real bugs fixed along the way: the original relay project didn't
actually compile (`PrivateService.cs` called `InformUnauthorized()`, which its own
duplicated `BaseResponse` never defined), and its `"DISCONNECTED"` fallback text
never matched either backend's `"DISCONECTED"` (sic) substring check, so a genuinely
offline device has likely never reported as such correctly until now.

The original Windows-only relay (`../backend/Tausend.UDPListener`) and the old
`RELAY_DECISION.md` are left in place as history -- see that file for the original
two-implementation decision this project inherited before being ported here.

## Architecture notes from the port

- **DI-safe by construction, not by luck**: the original's "just `new` whatever you
  need" style has some circular references that don't matter without a container
  (e.g. `DeviceBusiness.CreateDevice` used to `new NotificationBusiness()` to send a
  "device connected" push, while `NotificationBusiness` itself depends on
  `DeviceBusiness` for lookups). With real constructor DI those cycles are fatal.
  Fixed by moving the notification-send call up to `Controllers/DeviceController.cs`
  (which can depend on both services) rather than leaving it inside
  `DeviceBusiness` -- see the doc comment on `DeviceBusiness.CreateDevice`.
- **Singleton-safe FCM OAuth caching**: `PushNotificationBusinessFactory` is a
  singleton (so its `HttpClient` and FCM OAuth token cache live for the app's
  lifetime, matching the original's static-field caching intent) but needs
  scoped `AccountDao` access for one thing (removing an unregistered device
  token) -- done via a short-lived `IServiceScopeFactory` scope rather than
  capturing a scoped dependency into a singleton.

## One deliberate change from the original

`EmailBusiness` does **not** carry over the original's
`ServicePointManager.ServerCertificateValidationCallback => true` (a global TLS
certificate validation bypass). That was already flagged as a critical MITM
vulnerability in the security review of the original codebase; it wasn't ported
into this freshly-written code.

## Configuration

Real secrets (DB connection string, SMTP password, `RelaySystemKey`) live in
`appsettings.Development.json` (gitignored) for local dev -- copy
`appsettings.Development.json.example` to get started. In production, use
environment variables instead (`ConnectionStrings__TausendConnectionString`,
`Smtp__SenderPassword`, `RelaySystemKey`, etc. -- ASP.NET Core's standard `__`
env var convention), or an `appsettings.Production.json` alongside the deployed
binary (also gitignored). `PrivateService` (the relay's base URL) and
`Firebase:CredentialsPath` are non-secret and have real defaults committed in
`appsettings.json` -- override per-environment as needed.

## Database

`database/` contains the same table/stored-procedure/type definitions as
`../backend/TausendDB`, covering everything this backend now uses (Account,
Admin, Device, Zones/Exclusions/PGM/Users/Events). Deployment order matters:
`Tables/` first, then `Types/` (table-valued-parameter types), then
`StoredProcedures/`. SQL Server itself runs on Linux too (Microsoft has
supported this since SQL Server 2017), so the database doesn't need to move
anywhere -- it can live on the same Ubuntu VPS as this API.

## Running locally

```
cd src/TausendBackend.Api
dotnet run
```

## Deploying to Ubuntu (outline)

Both `TausendBackend.Api` and `TausendRelay` deploy the same way, and both need to
run -- the relay is not optional, it's the only thing that actually talks to panels.

1. `dotnet publish -c Release -o /path/to/publish` for each project
2. Copy each publish output to the server (e.g. `/opt/tausend/backend`,
   `/opt/tausend/relay`), along with a real `appsettings.Production.json` for each
   (or set the equivalent environment variables) -- both need a `RelaySystemKey` with
   the *identical* value, and `TausendRelay`'s `BackendBaseUrl` needs to point at
   wherever `TausendBackend.Api` actually listens
3. Run both behind `systemd` -- unit file templates are in `deploy/`
   (`tausend-backend.service`, `tausend-relay.service`); copy them to
   `/etc/systemd/system/`, adjust paths/user, then
   `systemctl enable --now tausend-backend tausend-relay`
4. `nginx` as a reverse proxy in front of `TausendBackend.Api` only (the standard
   ASP.NET Core-on-Linux production pattern) -- `TausendRelay`'s HTTP side
   (`PrivateService`) is intentionally localhost-only and must never go through nginx
   or be exposed publicly
5. **Firewall**: open `TausendRelay`'s UDP port (10000 by default) to the internet --
   this is the one port real alarm panels need to reach. Leave `TausendBackend.Api`
   reachable only via nginx's 80/443, and leave `TausendRelay`'s own HTTP port (10001)
   closed to everything except localhost
6. SQL Server for Linux (or a separate managed SQL Server) for the database

## Current deployment status (157.230.209.102)

Steps 1-3 and 6 above are done and live: SQL Server Express, both services running under
`systemd`, database schema applied, verified end-to-end against a real physical panel
(arm/disarm/status/version all confirmed working).

Step 4 (nginx) is installed and configured (`/etc/nginx/sites-available/tausend-backend`,
reverse-proxying to `TausendBackend.Api` on `127.0.0.1:5000`), and verified working -- but not yet
exposed publicly. `server_name` is currently the catch-all `_` placeholder, and the firewall does
not yet allow port 80/443 in.

**Remaining steps once the real domain is confirmed and its DNS A record points at this server:**
1. Edit `/etc/nginx/sites-available/tausend-backend`, replace `server_name _;` with the real
   domain, `nginx -t && systemctl reload nginx`
2. `ufw allow 80/tcp` and `ufw allow 443/tcp`
3. `certbot --nginx -d <real-domain>` -- obtains the certificate and rewrites the nginx config to
   add the HTTPS server block + HTTP-to-HTTPS redirect automatically
4. Confirm the mobile app's `API_BASE` (`mobile/src/api/client.ts`) matches the real domain

Deliberately not done yet: `TausendBackend.Api` is not reachable from the internet at all right
now (by design, until the domain is ready) -- everything above was verified via `curl` against
`127.0.0.1` on the server itself, over SSH.
