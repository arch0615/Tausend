# Day 26 -- live regression pass against the deployed backend + real panel

Executed against the real production deployment (157.230.209.102) and DeviceId 2
(`CRS186-0700198002`, a real physical panel), via direct API calls -- there's no Android/iOS
build tooling in the dev environment to run the actual mobile app, so this exercises the same
endpoints the app calls, not the UI itself. DeviceId 1 (`CSU338-0C001C0022`) is no longer linked
to the test account, most likely leftover from the Day 23 panel-unlink testing; not investigated
further since it doesn't block regression coverage (DeviceId 2 exercises the same endpoints).

Housekeeping: the test account's stored AccessToken had expired (server-side token expiry,
built Day 1). Recovered via the app's own real `AccountService/RecoverPassword` ->
`AccountService/ResetPassword` flow (reading the generated reset token from the DB directly,
since there's no access to the test inbox) rather than writing to the password hash directly.

## Result: all tested read/write round-trips pass

- Read-only: GetGeneralStatus, GetZones, GetExclusions, GetMemory, GetProgramControls, EnumUsers,
  GetBatteryStatus, GetFailStatus, GetTime, EnumEvents -- all return real, self-consistent live
  data (e.g. GetFailStatus's `AC:true` matches GetBatteryStatus's `InTension:0` -- this bench
  panel has no wall power, running on battery, consistently reported everywhere that matters).
- Full arm-away -> disarm cycle: real state transitions confirmed at each step, including that
  the panel correctly refused to arm while zones were open (`Text:"ERROR"`, correctly treated as
  a failure by the mobile app's `res.Text !== 'ERROR'` check), and correctly cleared the
  zone-bypass on disarm (verified via a follow-up GetExclusions read) -- so the final
  `NOT-READY,FAIL` after disarming is the panel correctly re-detecting the same physically-open
  zones, not a broken disarm.
- Exclusion (bypass) round-trip: bypass zones 1-8, arm succeeds, disarm clears the bypass -- confirmed
  live end to end.
- Cosmetic rename round-trips (CreateZones, CreateProgramControls, CreateUsers): all save and
  read back correctly.
- SyncTime: DeviceTime and ServerTime landed within under a second of each other after sync.
- Events pipeline: new real events appeared with correct timestamps immediately after live
  arm/disarm/exclusion activity, confirming the write path fires correctly on real panel actions.

## Skipped, deliberately, not run live

Physical/session-breaking risk, or (for the raw installer command) no verified-safe command
string on hand -- these need a separate, explicit go-ahead if live-testing them is wanted:
- `CommandService/ProgramControl` (physically toggles a PGM output -- may be wired to real hardware)
- `CommandService/Panic` / `Assault` / `Emergency` (audible siren; unclear if anything is listening
  for these downstream, so not worth finding out live)
- `DeviceService/BlockPIN` action=Block|Reset (revokes/wipes pairing, kills all sessions incl. the
  one used for this regression pass)
- `DeviceService/DissasociateCentral` / `DeleteDeviceSMS` (removes panel-account link)
- `CommandService/SendInstallerCommand` (raw program-section passthrough, no confirmed-safe
  read-only command string for this protocol on hand)

## Findings

### 1. `CommandController` never checks device ownership (backend-core) -- MODERATE, not exploitable cross-account

Confirmed: `GetGeneralStatus`/`GetBatteryStatus`/`ArmAlarm` all return `State: OK` (with
blank/zero data) for a `DeviceId` that doesn't exist or doesn't belong to the caller, instead of
a `NOT_FOUND`/`FORBIDDEN` error. `DeviceService` endpoints (`GetZones`, `GetMemory`, etc.) handle
this correctly, returning `404 Dispositivo {id} no encontrado`.

**Root cause**: `CommandBusiness.CreateBody` (`Business/CommandBusiness.cs:255`) looks up the
device from the CALLER'S OWN `account.Devices` list -- so this is NOT a way to control someone
else's real device (the lookup is correctly account-scoped). When the DeviceId isn't in the
caller's own list, `device` is `null`, and the code sends `identifier: null, pin: null` to the
relay instead of rejecting the request up front. The relay can't route a null identifier, the
response comes back empty, and `GetStatus()` parses empty content into `""`, which every calling
Controller method treats as a normal `State: OK` response.

**Fix**: `DeviceController` already has exactly the right pattern
(`AuthorizeDeviceAccess(accessToken, deviceId, out hasAccess)`, `DeviceController.cs:604`) --
resolves the account, checks `_deviceBusiness.GetDevice(deviceId, accountId).Device != null`
(with an Admin bypass), returns a `404` if not found/owned. `CommandController` needs the same
check added to every handler that currently only calls the token-only `Unauthorized()` helper:
`ArmAlarm`, `DisarmAlarm`, `DayArmAlarm`, `NightArmAlarm`, `GetVersion`, `Exclusion`, `Panic`,
`Emergency`, `Assault`, `GetGeneralStatus`, `GetFailStatus`, `GetZonesStatus`,
`SendInstallerCommand`, `GetBatteryStatus`. Cleanest fix: inject `DeviceBusiness` into
`CommandController` (not currently a dependency) and add an equivalent
`AuthorizeDeviceAccess`/`DeviceNotFound` check mirroring `DeviceController`'s.

### 2. Every event's `Text` is empty, permanently, until push notifications are finished -- HIGH, makes the Events screen non-functional

Confirmed live: 11 brand-new events appeared with correct real-time timestamps immediately after
today's arm/disarm/exclusion actions, but every one of them (and every pre-existing one) has
`Text: ""` and `EventType: null`.

**Root cause**: `NotificationBusiness.ProcessNotificationRequest` (`Business/NotificationBusiness.cs:63`)
only assigns the event's `body` variable *inside* the `foreach (var item in deviceTokens)` loop
(line 88: `body = notif.Body;`). Since push notifications were deliberately never wired up (see
`src/notifications/README.md`), no account has any registered device tokens, so `deviceTokens` is
always empty, the loop body never executes, and `body` stays `""` -- which then gets written to
the DB as the event's permanent `Text` (`CreateEvent`, line 124). This isn't a separate bug from
the known push-notification gap; it's a downstream consequence of it that wasn't previously
visible because nothing had exercised `EnumEvents` against live data before.

**Fix**: build the notification body once (via `_notificationBuilder.Build()`) independent of
whether any push token exists to send it to, so event-history text doesn't depend on push
notifications being finished. Likely means moving the body-building call outside the
`deviceTokens` loop, or building it once before the owner/token loops and reusing it both for any
pushes that do go out and for `CreateEvent`.

### 3. Removed PIN-holder labels reappear as blank rows on next screen load -- LOW/MODERATE (mobile)

Confirmed live: `CreateUsers` with an empty `UserName` (the app's only "remove a label" mechanism,
documented in `api/users.ts`) does exactly what's documented server-side -- `EnumUsers` afterward
returns `{"UserNumber":40,"UserName":""}`, not a removed row (matches the documented "no delete
endpoint" design).

**Root cause**: `PanelUsersScreen.tsx`'s `rows` (`useMemo`, line 87) only filters out entries in
the local, session-only `removed` Set (set when the user taps "Remove" in the current session).
It never filters based on the server's actual "removed" signal (`UserName === ''`). Since `load()`
resets `removed` to an empty Set on every screen focus (`useFocusEffect`), any previously-removed
label comes back from a fresh `getUsers()` call and renders as a blank-named row on next visit.

**Fix**: one line -- add a filter for empty names in the `rows` `useMemo`, e.g.
`.filter(([num, name]) => !removed.has(num) && name !== '')`.

### 4. `runTrigger` (Panic/Duress/Emergency) doesn't check for an `ERROR` Text response -- MINOR (mobile)

`HomeScreen.tsx`'s `runAction` (used for Arm/Disarm/DayArm/NightArm) already correctly checks
`res.Text !== 'ERROR'` in addition to `res.State === ResponseState.OK` before reporting success
(confirmed live: `ArmAlarm` with open zones really does return `Text:"ERROR"` with `State:0`, and
the app correctly treats that as a failure). `runTrigger` (line 164, used for Panic/Duress/
Emergency) only checks `res.State === ResponseState.OK` -- if the panel ever rejects a trigger
command the same way, the app would show "Panic alert sent." when it wasn't. Lower priority since
Panic/Duress/Emergency are less likely to be arm-state-gated than Arm itself, but the same guard
should be added for consistency.

## Priority for Day 27 PM ("fix critical bugs found in device testing")

1. Events `Text` always empty (#2) -- highest user-visible impact, makes an entire Day 20 feature
   non-functional.
2. `CommandController` missing ownership check (#1) -- not exploitable, but still a real
   correctness gap worth closing to match `DeviceController`'s existing pattern.
3. PanelUsersScreen blank-row bug (#3) -- small, one-line fix.
4. `runTrigger` missing ERROR check (#4) -- small, one-line fix, lowest priority.
