# Phase 4 feature QA checklist -- code freeze (Day 25 PM)

Static-analysis QA pass over every Phase 4 deliverable (Days 9-25 of the work schedule), done
before Phase 5 (QA & store deployment, Days 26-30) begins. This is not a substitute for Day 26's
live-device regression pass -- there's no Android/iOS build tooling in the dev environment this
was written in, so nothing here was actually run on a device or simulator. It verifies: the
screen/feature exists, is registered in navigation, is reachable from the UI, calls a real backend
endpoint, and handles the UNAUTHORIZED/error states the rest of the app handles.

## Day 9 -- Scaffold, API client, token storage
- [x] RN + TypeScript scaffold, navigation structure, design tokens/theme
- [x] Typed API client with a single auth interceptor (`src/api/client.ts`'s `apiPost()` -- the
      backend takes the token as a body field rather than a header, so the interceptor injects it
      into every request body instead; functionally the same "one choke point" the schedule asks for)
- [x] Secure token storage (`src/auth/tokenStorage.ts`, `react-native-keychain`)

## Day 10 -- Auth screens, session, password
- [x] Login and registration screens
- [x] Session persistence (`AuthContext.tsx` restores from the stored refresh token on launch)
- [x] Forgot/change password against the token-link reset flow

## Day 11 -- Wi-Fi pairing
- [x] AP flow, TCP socket provisioning (PRG350/PRG351)
- [x] Wrong password / timeout / permission-denied edge cases surfaced with distinct copy

## Day 12 -- SMS pairing, create-device
- [x] SMS command composition + native SMS intent handoff
- [x] Create-device wiring and pairing-confirmation UX for both paths
- [x] **Fixed during this pass**: `CreateDeviceScreen.tsx` and `SmsPairingScreen.tsx` didn't check
      `ResponseState.UNAUTHORIZED` (every other mutating screen does, routing to `logout()`) --
      added, matching the established pattern.

## Day 13 -- Multi-panel, identification
- [x] Multi-panel selector
- [x] Panel identification screen (SMS-only, correctly has no backend call)

## Day 14 -- Home + arm actions
- [x] Home screen shell + status polling
- [x] Arm / disarm / day-arm / night-arm actions

## Day 15 -- Zones, exclusions
- [x] Zones screen -- list and label
- [x] Exclusions screen, wired to the arm flow (re-checks live armed state before allowing changes)

## Day 16 -- Memory, PGM
- [x] Memory (zone/event history) screen
- [x] PGM outputs -- list, configure, toggle

## Day 17 -- Departures, user labels
- [x] Scheduled departures (same 8 PGM slots as Day 16, different framing -- by design)
- [x] Panel user labels (naming PIN-holders)

## Day 18 -- Battery, failures
- [x] Battery status screen
- [x] Failures/diagnostics screen

## Day 19 -- Duress/panic/emergency
- [x] All three triggers, both IP (backend command) and SMS (text-message fallback) transports

## Day 20 -- Events, push notifications
- [x] Events screen (in-app history)
- [ ] Push notifications -- **intentionally incomplete, documented in
      `src/notifications/README.md`**. Token registration + permission request exist; the actual
      FCM wiring was never installed because there's no real Firebase config
      (`google-services.json`/`GoogleService-Info.plist`) for this app yet, and installing the
      native package without it breaks the Android build. Blocked on the client providing Firebase
      credentials -- not a bug, not part of this freeze.

## Day 21 -- Custom messages, contact phone
- [x] Custom messages screen (SMS-only, no backend equivalent by design)
- [x] Contact phone configuration screen (same)

## Day 22 -- Clock, installer mode
- [x] Clock sync screen
- [x] Installer mode, role-gated client-side (button hidden) AND server-side
      (`CommandController.SendInstallerCommand` checks `IsInstallerOrAdmin`, added Day 22) --
      defense in depth against a captured EndUser token

## Day 23 -- PIN lockout, unlink
- [x] Panel unlink (IP: `disassociateCentral`, SMS: `deleteDeviceSms`)
- [x] PIN lockout (`blockPin`, Lock/Reset actions with distinct confirm dialogs)
- [x] **Fixed during this pass**: `ManagePanelScreen.tsx`'s unlink/lock/reset handlers didn't
      branch on `UNAUTHORIZED` explicitly (fell through to a generic error banner while the session
      was already dead) -- both now route to `logout()` for a session-expired failure, matching
      every other screen.

## Day 24 -- Cross-feature integration, UI polish
- [x] Consistent navigation and state (shared `PanelContext`/`AuthContext`/`ThemeProvider`)
- [x] Empty / error / loading states (present across every list/detail screen)
- [ ] **Icons** -- the schedule calls these out explicitly as a Day 24 PM deliverable; the app is
      currently text-label-and-button UI throughout, no icon library was ever installed. This was
      a deliberate choice at the time (avoiding a native-linking dependency in an environment with
      no way to verify the Android/iOS build still compiles), but it's an open item, not a resolved
      one. **Needs a decision before Day 28 (store listing / screenshots)**: ship text-only, or
      spend a design pass adding icons first.

## Day 25 AM -- Localization
- [x] Every screen (30/30) imports `useLocale` and routes its user-facing copy through `t()`.
      `src/i18n/es.ts` holds the Spanish dictionary; English is the identity key, so no separate
      English dictionary file exists. Verified: `tsc --noEmit`, `eslint`, `jest` all clean.

## Sanity sweep
- [x] No `TODO`/`FIXME`/`XXX` comments in `src/`
- [x] No stray `console.log`/`.warn`/`.error`/`.debug` in `src/`
- [x] No orphaned screens -- every file under `src/screens/**` is registered in `AuthStack`/`MainStack`

## Open items carried into Phase 5
1. **Icons** (Day 24 PM) -- decision needed: ship as-is, or add an icon library before store assets.
2. **Push notifications** (Day 20 PM) -- blocked on the client supplying real Firebase credentials
   for `com.alarmastausend.tausend`; see `src/notifications/README.md` for the exact remaining steps.
3. Everything else in this checklist is functionally complete and passed static verification.
   Day 26's live-device regression pass is the first time any of this actually runs on hardware.
