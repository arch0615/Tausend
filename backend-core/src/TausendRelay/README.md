# Tausend Relay (ASP.NET Core / .NET 8)

A from-scratch port of `../../backend/Tausend.UDPListener` -- the process that
actually talks to physical alarm panels -- built to run on Ubuntu instead of
requiring Windows.

## Why this exists

Panels don't speak HTTP. They speak a proprietary framing protocol (CGM1) over raw
UDP, with a weak 16-bit XOR cipher instead of real encryption. The relay sits between
the panels and the backend: it listens for UDP packets from panels, and exposes a
small internal HTTP service (`PrivateService`) that the backend calls to send
commands (arm/disarm/PGM/bypass/etc.).

The original relay (`../../backend/Tausend.UDPListener`) is classic .NET Framework
using WCF self-hosting (`System.ServiceModel`) -- a Windows-only stack. That meant
"deploy on Ubuntu" for this whole product actually meant two servers: an Ubuntu box
for `TausendBackend.Api` and a separate Windows box for the relay. This project
removes that split: same protocol, same command set, plain ASP.NET Core, runs on the
same Ubuntu box as the API.

## What changed in the port

Behaviorally, this is a faithful port -- same CGM1 cipher, same command byte layout,
same UDP framing, same response-correlation logic. A few things are genuinely
different:

- **Two real bugs fixed, not carried over.** The original relay project as committed
  doesn't actually compile: `Services/PrivateService.cs` calls `InformUnauthorized()`
  on every endpoint, but that method was never added to this project's own
  duplicated `BaseResponse` class (only to the main backend's copy) -- the Day 4
  "authenticate both legs" pass never finished on the relay side. Separately, the
  original's `SendCommand` fallback text is `"DISCONNECTED"`, but both backends'
  `CommandBusiness.GetStatus` check for `"DISCONECTED"` (a pre-existing typo in the
  *original* `Tausend.Core.Business`, confirmed present before this port existed) --
  the two sides have likely never agreed on this string, so a genuinely offline
  device has probably never reported as such correctly. Both fixed here.
- **Thread-safety.** The original's device registry and per-device pending-response
  table were plain `Dictionary`/`List` collections read and written from two
  concurrent contexts (the UDP receive loop, and whichever thread handles a
  `PrivateService` HTTP call) with no lock at all -- the old WCF host's
  `ConcurrencyMode.Single` only serialized `PrivateService` calls against each other,
  never against the receive loop. `Cache/DeviceCache.cs` and the response table in
  `Relay/UdpRelayService.cs` are now properly synchronized.
- **`Encoding.Default` fixed to `Encoding.ASCII`.** `Encoding.Default` means "the OS's
  current ANSI code page" on .NET Framework but always means UTF-8 on .NET Core --
  a silent, platform-dependent meaning change that would have corrupted any non-ASCII
  byte on this port. The protocol's command vocabulary is ASCII-only and the encrypt
  side already used `ASCIIEncoding.ASCII` explicitly, so both directions now do.
- **Dead code not ported.** `ClockCommand`/`ProgramCommand` and the SMS-command
  scaffolding on `BaseCommand` were never actually called anywhere in the relay
  (RTC/PRG commands pass through as raw strings; SMS command composition happens
  elsewhere, for SMS-connected panels, which don't go through this relay at all) --
  omitted rather than carried over unused. The generated SOAP `DeviceService` client
  under the original's `Connected Services/` was already dead code per
  `../../backend/RELAY_DECISION.md`; not ported either.
- **Config**: the original had four separate per-service URLs (`CommandService`,
  `DeviceService`, `AccountService`, `NotificationService`) left over from when the
  backend was several WCF `.svc` endpoints. Only `DeviceService` and
  `NotificationService` were ever actually used, and in this backend they're just
  route groups under one app -- collapsed into a single `BackendBaseUrl`.

## Verified

Live end-to-end against a real `TausendBackend.Api` + SQL Server instance, using a
UDP script standing in for a panel: device registration (`ID`/`IDOK` handshake),
a full `SendCommand` round trip (`STSZ` status query, including the response-wait
logic), and a full alarm-event report (`EVENT` -> `NotifyEvent` -> a real row in the
`Events` table) -- plus the `SystemAuth` shared-secret check rejecting an
unauthenticated request. Not yet verified against a real physical panel or the
original Windows relay side by side.

## Running locally

```
cd src/TausendRelay
dotnet run
```

Needs `TausendBackend.Api` running too (see `../TausendBackend.Api/README`-equivalent
instructions in the parent `backend-core/README.md`) and a matching `RelaySystemKey`
in both projects' config.

## Configuration

See `appsettings.json` for defaults and `appsettings.Development.json` for the local
dev `RelaySystemKey` (must match `TausendBackend.Api`'s). In production, use
environment variables (`RelaySystemKey`, `BackendBaseUrl`, `Udp__Port`, etc.) or an
`appsettings.Production.json` instead of committing secrets.

`PrivateServiceBindAddress` defaults to `127.0.0.1` -- deliberately not
internet-facing, since only `TausendBackend.Api` ever calls it, expected to run on
the same box. `Udp:BindAddress`/`Udp:Port` default to `0.0.0.0:10000` and **must**
stay reachable from the internet -- that's the port real panels connect to.

See `../../backend-core/README.md`'s "Deploying to Ubuntu" section and `../../deploy/`
for systemd unit templates covering both this project and `TausendBackend.Api`.
