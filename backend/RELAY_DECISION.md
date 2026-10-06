# Relay decision — Day 4

> Resolves the "which relay implementation is actually in production" question that
> Day 3/4 and `backend-core/README.md` flagged as blocking. Read this before touching
> `Tausend.UDPListener`, `PrivateService`, or anything under `Relay Server Source/`.

## Decision

**`Tausend.UDPListener` (in this repo, `backend/Tausend.UDPListener`) is the relay.**
The standalone WinForms project at `Relay Server Source/TausendServer2/` (outside this
repo) is retired -- do not deploy it, do not build on top of it.

## Why

Both speak the same CGM1 framing over UDP to the panels, but only `Tausend.UDPListener`
is an actual candidate for production:

| | `Tausend.UDPListener` | `TausendServer2` (WinForms) |
|---|---|---|
| Backend integration | Self-hosts `PrivateService` (WCF/REST) for the command path; calls back into `Tausend.Core`'s `DeviceService`/`NotificationService` | **None** -- no HTTP/WCF/socket surface at all, just a debug UI |
| Payload encryption | XOR cipher keyed off each device's DB-stored `PublicKey` (weak, but present) | None -- fully cleartext |
| Device capacity | Up to 10,000 (`CacheManager`) | Hardcoded 10-device table |
| Command coverage | Full command set (ARM/DISARM/STSZ/BYP/PGM/RTC/PRG/etc.), with response correlation | Generic framing only, no command-specific logic |

`TausendServer2` reads as the original prototype the CGM1 framing logic was worked out
in; `Tausend.UDPListener` is where that logic was later extended into something a
backend could actually drive. There is no code path today by which the backend could
send a command through `TausendServer2` -- adopting it would mean building an entire
command-in surface from scratch, duplicating what `PrivateService` already does.

## What changed today: authenticating both legs

Neither leg of the backend<->relay boundary had any credential before this pass --
anything that could reach `http://localhost:10001/PrivateService` (backend -> relay)
or the public `DeviceService`/`NotificationService` endpoints at
`tausend.wearelomo.com` (relay -> backend) could act as the relay. That's now closed
with a shared secret:

- **New:** `Tausend.Core.Security.SystemAuth` (`backend/Tausend.Core/Security/SystemAuth.cs`).
  Both processes read the same `RelaySystemKey` from their own local config and compare
  it against an `X-System-Key` request header, using a fixed-time string comparison.
- **Backend -> relay** (the actual "command path" this task is named for):
  `CommandBusiness.Execute` now attaches the header on every call to `PrivateService`;
  `PrivateService.SendCommand`/`PGMCommand`/`BYPCommand` reject the request
  (`InformUnauthorized`) if it's missing or wrong.
- **Relay -> backend:** `ServiceBusiness.CreateRequest` now attaches the same header on
  every call to `DeviceService`/`NotificationService`. On the receiving side:
  - `DeviceService.UpdateDeviceConnectionParameters` / `UpdateDeviceLastConnection` --
    now require the system key. These were the two endpoints explicitly flagged as
    unauthenticated in the Day 3 pass; this closes that.
  - `NotificationService.NotifyEvent` -- now requires the system key. This is how panel
    events (including real alarm/panic/fire events) reach the backend; it had no
    protection at all before, on a public, plain-HTTP hostname.
  - `DeviceService.GetDeviceByID` / `GetDeviceByIdentifier` / `DisassociateCentral` --
    these accept *either* a valid user `AccessToken` **or** the system key, since
    mobile-app end users call these too. This also fixes a live bug found in the
    process: the relay's calls to these (via `ServiceBusiness.GetDevice`) never sent an
    `AccessToken`, so they were unconditionally rejected -- meaning device lookups
    (including encryption-key resolution for the CGM1 cipher) from the relay were
    already failing before this change.

### Deploying this

Both `Tausend.Core/Secrets.config` and `Tausend.UDPListener/Secrets.config` need a
`RelaySystemKey` entry with the **identical** value (see the `.example` files next to
each). Generate one 32+ byte random value and put it in both -- a mismatch means every
relay call fails auth.

## What's still open (not done in this pass)

- **Panel <-> relay authentication is still weak.** The CGM1 "encryption" is a 16-bit
  XOR cipher with a non-cryptographic checksum, and device identification on the wire
  still just trusts whichever IP:port last claimed a given identifier. Fixing this is a
  protocol-level change (effectively a firmware/panel-side change), not something
  fixable from the backend/relay code alone -- out of scope here.
- **The generated SOAP `DeviceService` client proxy** under
  `Tausend.UDPListener/Connected Services/DeviceService/` is dead code -- nothing calls
  it, and it appears to target a binding (`basicHttpBinding`-style SOAP) that doesn't
  match the actual service, which is only exposed via `webHttpBinding` REST. Left as-is;
  removing it is cleanup, not a functional change.
- **Known bugs found while reading this code, not fixed here:**
  - `CommandBusiness.Execute` / `ServiceBusiness.Execute` (Tausend.Core and
    Tausend.UDPListener respectively) both set fields on a `null` `response` inside
    their own `catch` block if the HTTP call itself throws -- an `NullReferenceException`
    masking the real connection failure.
  - `Listener.SendCommand` holds one global lock across every outbound command to every
    panel, and each command can block up to 10x200ms waiting for a reply -- one
    unresponsive panel stalls command delivery to every other panel on the relay.
  - `Listener.SendPGMCommand`/`SendBYPCommand` don't null-check an unknown device the
    way `SendCommand` does, so an unknown/offline device throws instead of returning
    `"DISCONNECTED"`.
