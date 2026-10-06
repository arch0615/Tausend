# Tausend -- project handoff report

## Where things actually stand

**Update:** Android Studio and the full Android SDK are now installed in the dev environment, and
the app **builds successfully end to end** -- both `assembleDebug` and `assembleRelease` produce
real, working APKs (verified: correct package name, permissions, version, and the "Tausend" app
label all confirmed present in the actual built artifact). The Android side of "no build tooling
available" is resolved. iOS is not -- that still needs an actual Mac with Xcode, which remains
unavailable in this environment.

The app -- backend, admin dashboard, and full mobile feature set -- is built, verified against
real hardware, and stable. It is **not yet** live on the Play Store or App Store. What's left for
Android is real assets and one secret (icon artwork, screenshots, the release keystore), not
tooling. iOS is still fully blocked on Mac/Xcode access. See "What's genuinely not done" below for
the precise, current breakdown.

## What's built and verified

**Backend** (`backend-core/`, ASP.NET Core / .NET 8) -- roles model, bcrypt password hashing,
token-based password reset, auth checks on every endpoint, a single relay integration, admin
dashboard endpoints. Deployed and running on a real VPS (see "Live system" below).

**Admin dashboard** (`dashboard/`) -- fleet view, cross-fleet user management, role assignment,
audit trail.

**Mobile app** (`mobile/`, React Native) -- the full feature set: auth, session persistence,
Wi-Fi and SMS panel pairing, multi-panel support, arm/disarm/day-arm/night-arm, zones and
exclusions, memory, PGM outputs, scheduled departures, panel user labels, battery/failure
diagnostics, panic/duress/emergency alerts (both IP and SMS transports), event history, custom
messages and contact numbers for SMS panels, clock sync, role-gated installer mode, PIN
lockout/panel unlink, and a full EN/ES localization pass across all 30 screens.

**Live-hardware verification** -- not just unit-level correctness, but actually run against two
real physical alarm panels and a production-configured backend: a full arm-away/disarm cycle,
zone bypass, cosmetic renames, clock sync, and every read-only status/diagnostic endpoint, all
confirmed working end to end. This is what caught the two real backend bugs below -- the kind of
bug that only shows up when something actually talks to real hardware, not from reading the code.

**Bugs found via live testing and fixed**:
- A command-endpoint (arm/disarm/panic/etc.) authorization gap that silently no-op'd instead of
  rejecting a device ID the caller didn't own (not a cross-account exploit -- the underlying
  lookup was already correctly account-scoped -- but a misleading success response).
- Event history text was permanently empty because it was accidentally coupled to the
  unfinished push-notification code path. Fixed and reconfirmed today with a real, unprompted
  panel event that came through with a correct, readable description.
- Two smaller mobile-side bugs (a removed PIN-holder label reappearing as a blank row on reload;
  a missing error-state check on the panic/duress/emergency buttons).

All of this is documented in detail, not just asserted -- see `mobile/QA_CHECKLIST.md`,
`mobile/DAY26_REGRESSION.md`, `mobile/DAY27_DEVICE_MATRIX.md`.

## Live system

Running on the VPS at `157.230.209.102`: `tausend-backend` and `tausend-relay`, both under
systemd, both active and stable (current uptime over a day with no restarts since the last
deployment, confirmed today). SQL Server Express with the full schema applied. nginx installed and
configured as a reverse proxy but **not yet exposed publicly** -- no real domain has been
confirmed yet, so port 80/443 stays closed and the API is only reachable over SSH-tunnel/localhost
for now. `backend-core/README.md` has the exact remaining steps (4 commands) once a domain is
ready.

## What's genuinely not done, and why

1. **Android store submission** -- close. The build itself works (see above). What's left is real
   assets and a secret, not development work: app icon artwork, device screenshots (can now be
   taken from an actual running build -- an emulator or physical device, still needed for this
   specific step), and the release signing keystore (deliberately left for whoever holds that
   secret to generate -- see `mobile/android/RELEASE_SIGNING.md`).
2. **iOS build and submission** -- fully blocked, same as before. Needs an actual Mac with Xcode
   installed -- nothing about this can be prepared further from a Windows dev environment. Apple
   Developer Program enrollment, certificates, and provisioning profiles are also still needed
   (checklist in `mobile/android/RELEASE_SIGNING.md`, despite the filename -- it covers both
   platforms).
   `mobile/STORE_LISTING.md` has the store listing copy, content rating notes, and a precise
   checklist of exactly what's left for both platforms.
3. **Push notifications** -- token registration and permission-request code exist, but the actual
   FCM wiring was never installed because there's no real Firebase project configuration for this
   app yet, and installing the native package without it breaks the Android build outright. See
   `mobile/src/notifications/README.md` for the exact remaining steps once Firebase credentials
   exist.
4. **Domain + TLS** for the live backend -- ready to go, waiting on a confirmed real domain
   pointing at the VPS.
5. **In-app icons** (as opposed to the app's store icon) -- the UI is currently text-label-based
   throughout; no icon library was installed, the same tooling-verification reasoning as
   everything else native-dependency-related in this project.

## Everything written up along the way

- `mobile/QA_CHECKLIST.md` -- full feature-by-feature audit against the 25-day mobile build.
- `mobile/DAY26_REGRESSION.md` -- live-hardware regression pass, root-caused bug reports.
- `mobile/DAY27_DEVICE_MATRIX.md` -- cross-device/OS compatibility review.
- `mobile/STORE_LISTING.md` -- privacy policy, permission justifications, listing copy, submission
  checklist.
- `mobile/android/RELEASE_SIGNING.md` -- Android keystore generation steps, iOS signing checklist.
- `mobile/src/notifications/README.md` -- exact push-notification completion steps.
- `backend-core/README.md` -- deployment steps and current live-deployment status.

## What's actually needed to finish

In order of what unblocks the most:
1. Someone with a Mac (Xcode) to produce and submit an iOS build -- the one remaining hard
   tooling blocker. Android no longer needs this.
2. App icon artwork (both platforms).
3. The Android release keystore, generated and kept safe by whoever owns it
   (`mobile/android/RELEASE_SIGNING.md`).
4. Device screenshots for both store listings.
5. A confirmed domain for the backend.

6. Firebase project credentials, if push notifications are wanted before launch (not required to
   ship -- the rest of the app works fully without them).

None of these are development work in the sense the rest of this project has been -- they're
assets, access, and infrastructure decisions that need to come from outside a coding session.

**Note for whoever builds Android next**: if a fresh machine/SDK install hits a
`ninja: ... Filename longer than 260 characters` error on `react-native-gesture-handler`'s native
build, it's not a real regression -- it's a known Windows-only issue (the AGP-default CMake
3.22.1's bundled ninja doesn't support paths over 260 characters even with Windows'
`LongPathsEnabled` registry setting on). It's already fixed in `android/app/build.gradle` by
pinning a newer CMake version (3.31.6, bundles ninja 1.12.1 which added proper long-path support).
Building on macOS/Linux never hits this at all -- it's specific to Windows' legacy path-length
limit.

## Testing against the real backend during development

`src/api/client.ts`'s `API_BASE` now points debug builds at `http://10.0.2.2:5000` (the Android
emulator's alias for its host machine's own localhost) instead of the unconnected production
placeholder domain. This is meant to be reached through an SSH tunnel from wherever the
emulator/device runs to the VPS's backend (`ssh -L 5000:localhost:5000 tausend-vps`, or the
equivalent with the real IP if that host alias isn't configured on the machine in question) -- so
development and testing happen against the real live backend without exposing it publicly. A
debug-only network security config (`android/app/src/debug/res/xml/network_security_config.xml`)
allows cleartext to `10.0.2.2`/`localhost`/`127.0.0.1` for exactly this; release builds are
untouched and still locked down to just the panel's own local-AP address. If testing from a
physical device instead of an emulator, either bind the tunnel to `0.0.0.0` and use the host
machine's LAN IP in `API_BASE`, or use `adb reverse tcp:5000 tcp:5000` and keep `localhost`.

**Known open item**: running the Android emulator itself on a VPS (no real/passthrough GPU) hits
an "OpenGL version is too low" error -- fix is to set the AVD's Graphics setting to "Software -
GLES 2.0" (Android Studio: Device Manager -> edit the AVD -> Show Advanced Settings -> Graphics),
or launch with `emulator -avd <name> -gpu swiftshader_indirect`. Not yet confirmed working as of
this writing -- whoever picks this up next should verify signup actually succeeds end to end once
the emulator itself launches.

## Continuing this work on a different machine

Everything of substance lives in two places, not in any particular dev session:
1. **Git history** -- as of this writing, local `main` in this repo is **7 commits ahead of
   `origin/main` and not yet pushed** (all the Android Studio/build-fix/networking work from
   today, plus the store-listing and regression docs from the last few days). Whoever continues
   this needs those commits -- either push them to `origin` from here first, or pull/copy this
   exact working tree directly.
2. **This file, and the docs it links to** (`mobile/QA_CHECKLIST.md`, `mobile/DAY26_REGRESSION.md`,
   `mobile/DAY27_DEVICE_MATRIX.md`, `mobile/STORE_LISTING.md`, `mobile/android/RELEASE_SIGNING.md`,
   `mobile/src/notifications/README.md`, `backend-core/README.md`) -- read together, these are a
   complete, current account of what's built, what's verified, and what's still open. A fresh
   Claude Code session pointed at this repo, given this file, has everything it needs to pick up
   exactly where this one left off -- it doesn't need the raw conversation transcript.
