# Push notification rollout — two-phase plan

> This document describes the coordinated backend + mobile rollout of the
> "split alarm vs default channel" push notification fix. It exists because
> the iOS half of the fix requires a new bundled resource (`alert.caf`) that
> is not present in the currently-published iOS app, so the backend cannot
> safely start referencing `alert.caf` until the new iOS build is live and
> has propagated to users.
>
> **Read this before deploying the backend a second time.** The first deploy
> (phase 1) is safe. The second deploy (phase 2) has a pre-flight checklist
> below.

---

## What the fix does (recap)

`FirebasePushNotificationBusiness.CreatePayload` now uses
`IsAlarmNotification(NotificationTypes)` to split pushes into two profiles:

| Notification type | Android channel | Android sound | iOS sound (final) | APNs priority |
|---|---|---|---|---|
| Real alarm (Fire, Panic, Medical, Sabotage, ZoneCross, Gas, Assault, SilentPanic, KeypadAssault, Stole, PersonalMedical, SilentAlarm) | `my_channel_id` | `alert` (R.raw.alert) | `alert.caf` | `10` |
| Everything else (arm/disarm, battery fail, test, ZoneBypass, etc.) | `default_channel_id` | `default` | `default` | `5` |

Before the fix, every push was forced through `my_channel_id` with `alert`
sound, `high` priority, `PRIORITY_MAX`, and `apns-priority: 10`, so routine
events were as loud and alarming as real fires.

---

## Rollout phases

### Phase 1 — Android-first (currently committed)

**What is in this phase:**

- Full channel split logic in the backend for Android and APNs priority.
- `iosSound` is **temporarily hardcoded to `"default"`** for all iOS pushes,
  including alarms, via a single line in
  `Tausend.Core/Business/FirebasePushNotificationBusiness.cs`. Look for the
  `ROLLOUT PHASE 1` comment.
- The new Android APK can be cut from `mobile` branch `main` and published.
- The new iOS IPA can be cut from `mobile` branch `main` (it bundles
  `alert.caf` at `ios/App/App/alert.caf`) and published.

**Why iOS is degraded in phase 1:**

Users running the *currently published* iOS app do not have `alert.caf` in
their app bundle. If the backend sends `aps.sound: "alert.caf"` to those
installs, iOS's behavior when the file is missing is not consistently
documented — some reports say it falls back to the default sound, others say
it plays nothing. For a life-safety alarm app, "might be silent" is not
acceptable. So phase 1 keeps iOS alarms on the system default chime (exactly
the behavior they have today) for everyone.

**What gets fixed in phase 1:**

- **Existing Android installs immediately get the full split.** The old
  Android `MyFirebaseMessagingService.java` already routes on the incoming
  `channelId` and `MainActivity.onCreate()` already registers both
  `my_channel_id` and `default_channel_id`, so the old client correctly
  interprets the new backend payload without needing an app update. Routine
  events (arm/disarm, battery fail, tests, etc.) will stop ringing the
  siren the moment this deploys. Real alarms still play the siren.
- **New Android app (when published) adds polish:** unique notification IDs
  so back-to-back events stack in the tray instead of overwriting, a NPE
  guard for data-only messages, and log cleanup.
- **iOS behavior is unchanged** — default chime for everything, same as
  before the fix. No regression.

### Phase 2 — Enable iOS siren (after the new iOS build propagates)

**When to run phase 2:**

Wait until **all three** of these are true:

1. The new iOS build is live in the App Store (not just TestFlight).
2. At least two weeks have passed since the new iOS build went live, to
   give users time to update. For a life-safety app you may want to wait
   longer; consult the Apple app analytics "adoption" dashboard for the
   new build's install share.
3. You have a way to reach any stragglers still on the old build
   (in-app update prompt, email, SMS) — because after phase 2, their
   alarms may play no custom sound.

**What phase 2 changes:**

One line in `Tausend.Core/Business/FirebasePushNotificationBusiness.cs`:

```csharp
// BEFORE (phase 1)
var iosSound = "default";

// AFTER (phase 2)
var iosSound = isAlarm ? "alert.caf" : "default";
```

Remove the `ROLLOUT PHASE 1` comment block when you flip the line.

**Phase 2 deploy sequence:**

1. Make the one-line edit above.
2. Commit: `fix(push): phase 2 — enable alert.caf for iOS alarms`.
3. Build and deploy the backend.
4. Run the iOS validation test plan (below).

---

## Phase 1 validation — Android test plan

Run this after deploying the phase-1 backend. You do **not** need the new
Android build to validate most of this — the existing installed Android app
will show the fix immediately. But if you also want to confirm the new
Android build's polish (stacking, NPE guard), install a release APK on a
physical Android 13+ device.

### Setup

1. Deploy the phase-1 backend to production (or a staging environment that
   hits the same Firebase project). Confirm the WCF service is up via a
   health check or any existing smoke test.
2. Confirm the Firebase service account key at
   `E:\Codenmate\tausend.codenmate.com\lomo-tausend-firebase-adminsdk.json`
   is present and current. (No change required; noting this because it's
   the hardcoded runtime path in `FirebasePushNotificationBusiness.cs:62`.)
3. Use a physical Android device (Android 10+) running either the currently
   published app or a freshly built release APK from `mobile` branch `main`.
   Both should work; test at least one.
4. Make sure push notifications are enabled in Android Settings → Apps →
   Tausend → Notifications. Both channels should be listed:
   - "my_channel_id" (or whatever its display name is) — importance High
   - "default_channel_id" — importance Default

### Test 1: Routine event is quiet

**Action:** From the app, arm or disarm the alarm panel (any user-initiated
arm/disarm will do).

**Expected behavior:**

- Notification appears as a standard banner (not heads-up, not full-screen).
- Sound: Android's system default notification chime (**not** the siren).
- No strong vibration.
- Notification goes to the "default_channel_id" channel (visible in Android
  Settings → Apps → Tausend → Notifications → channel breakdown).

**If this fails** (you still hear the siren): the backend wasn't deployed,
or the notification type is mis-classified in `IsAlarmNotification`. Check
the deployed version and look at the received notification's `channel_id`
in the device log.

### Test 2: Real alarm still rings the siren

**Action:** Trigger a real alarm on the panel. Options:
- Option A (cleanest, if you have a test panel): manually trigger a Fire
  event (CID 110).
- Option B (from backend shell): call
  `NotificationService.svc/NotifyEvent` directly with `NotificationType: 110`
  (Fire) and a valid `AlarmIdentifier` + `DeviceToken` matching your test
  device.
- Option C (simplest end-to-end validation): use the panel's panic button
  (CID 120).

**Expected behavior:**

- Notification appears as a heads-up / high-importance banner.
- Sound: the custom siren (`R.raw.alert` — the alert.mp3 in
  `android/app/src/main/res/raw/`). Distinct from the system default.
- Phone vibrates strongly.
- Notification goes to the "my_channel_id" channel.

**If this fails** (quiet or default sound): the notification type is not
in the `IsAlarmNotification` list, or `my_channel_id` is misconfigured on
the device. Open Android Settings → Apps → Tausend → Notifications → the
specific channel → verify sound is set to the custom alert.

### Test 3: Test notification is quiet

**Action:** Hit the "Send test notification" API endpoint (or however the
app triggers `NotificationService.svc/SendTestNotification`).

**Expected behavior:**

- Same as Test 1 — default sound, default channel, no siren. This confirms
  the hardcoded `Test` notification type (12) is correctly classified as
  non-alarm.

### Test 4: Notification stacking (new Android build only)

**Action:** Install the new Android release APK (from `mobile` branch `main`).
Trigger three arm/disarm events in rapid succession (< 5 seconds apart).

**Expected behavior:**

- All three notifications appear in the tray as separate entries, stacked.
  You should see three rows if you pull down the shade.

**If this fails** (only one notification visible, or notifications
overwrite): you're running the old Android build. The fix is in
commit `03a334a` on `mobile` branch `main`.

### Test 5: iOS regression check (important)

**Action:** Trigger a routine event (arm/disarm) to an iOS device running
the **currently published** iOS app (old build, the one in the App Store
today).

**Expected behavior:**

- Notification arrives.
- Plays the **system default chime** — exactly the same sound as before
  the backend deploy. No silence, no change.
- Visual banner appears.

**Then trigger a Fire notification (real alarm) to the same iOS device.**

**Expected behavior:**

- Notification arrives.
- Plays the **system default chime** (NOT a custom siren, because phase 1
  hardcodes iosSound to "default").
- Visual banner appears.
- Same sound as the arm/disarm event — that's intentional. Phase 1 does
  not try to differentiate alarm vs non-alarm on iOS by sound.

**If anything on iOS is silent or fails to arrive**: the issue is NOT this
backend change — it's most likely an APNs Authentication Key problem in
the Firebase Console, unrelated to the code change. See the "APNs auth
sanity check" section of `mobile/BUILD_IOS.md`.

### Test 6: No regression on existing behavior

Arm/disarm flows, login, list of events, alarm panel status queries — all
the paths that don't go through push — should be unchanged. The change is
scoped to `FirebasePushNotificationBusiness.CreatePayload` and touches
nothing else. But run through your normal smoke test to be sure.

---

## Phase 2 validation — iOS test plan (run AFTER flipping the iosSound line)

Do not run this until phase 2 is committed and deployed. All tests require
a physical iOS device (simulator does not receive real APNs).

### Setup

1. Install the new iOS build on a physical device via TestFlight or the
   App Store (not Xcode debug, since that bundle ID may differ). Confirm
   the app bundle contains `alert.caf`:
   ```bash
   find ~/Library/Developer/Xcode/DerivedData -name alert.caf -path '*App.app*'
   ```
   If you cannot SSH into your developer machine, visual confirmation via
   Xcode → Window → Devices → your device → installed apps → Show Container
   also works.
2. Grant notification permission if not already granted.
3. Confirm the device registered with Firebase via the Xcode console (look
   for the registration token line) or via the backend device-token table.

### iOS Test 1: Alarm plays the siren

**Action:** Trigger a Fire event (CID 110) from the backend.

**Expected behavior:**

- Banner notification arrives.
- Plays the custom siren from `alert.caf` (same 7.4-second sound as Android
  alert.mp3, distinct from any iOS system sound).
- Repeat for Panic (120), Medical (100), Sabotage (137) — all should sound
  the same custom siren.

**If the siren is silent but the banner appears**: `alert.caf` is not in
the app bundle. Re-check the Copy Bundle Resources build phase in Xcode and
re-archive.

**If the siren plays but sounds wrong** (like the old default chime): the
device may be caching an old APNs payload. Delete the notification, then
trigger again. On iOS 16+ you may need to reinstall the app.

### iOS Test 2: Non-alarm uses default chime

**Action:** Trigger an arm/disarm event.

**Expected behavior:**

- Banner notification arrives.
- Plays iOS system default chime (not the siren). This is the same as
  phase 1 behavior — only alarms got "upgraded" in phase 2.

### iOS Test 3: Background and killed-app delivery

**Action:** Kill the app entirely (swipe away from the app switcher).
Trigger a Fire event from the backend.

**Expected behavior:**

- Device screen lights up with the notification banner.
- Siren plays even though the app is killed.
- Tapping the notification launches the app.

### iOS Test 4: Old iOS build check (regression watch)

If any users are still on the *pre-phase-2* published iOS build when you
deploy phase 2, they will hit the "missing alert.caf" case. If you can
still reach a test device with the old build installed (or a TestFlight
internal tester group pinned to the previous build), trigger a Fire event
and observe. If the alarm plays nothing at all, you need to push a forced
update to the old-build users. If it falls back to the default chime,
you're fine and the old users are simply on the phase-1 behavior.

### iOS Test 5: End-to-end smoke

Full production smoke test: arm the panel → disarm the panel → trigger a
test event → trigger a real alarm. Confirm all four notifications sound
correct and distinct.

---

## Rollback plan

### Rolling back phase 1

If phase 1 causes unexpected Android regressions:

1. In `Tausend.Core/Business/FirebasePushNotificationBusiness.cs`, revert
   commit `1c0615b` (backend split) plus the phase-1 mitigation commit.
2. Redeploy the backend.
3. Existing Android users will revert to "everything through my_channel_id
   with siren" behavior — the old broken state, but at least consistent.

### Rolling back phase 2

If phase 2 causes iOS regressions (e.g., silent alarms on a large segment):

1. In `Tausend.Core/Business/FirebasePushNotificationBusiness.cs`, change
   `var iosSound = isAlarm ? "alert.caf" : "default";` back to
   `var iosSound = "default";`.
2. Redeploy the backend.
3. This puts you back in phase-1 behavior. No mobile app change needed.

Either rollback is a one-line edit + redeploy. Both are safe.

---

## What you are NOT fixing with this rollout

Surfaced during planning but intentionally out of scope:

1. **Foreground notifications on iOS.** When the app is foregrounded,
   `src/app/app.component.ts` shows an Ionic `AlertController` dialog and
   plays `this.dialogs.beep(1)` — a generic Cordova beep, not the APNs
   sound. The backend's `aps.sound` is ignored in the foreground. This
   is a pre-existing product behavior; not a regression.
2. **Critical Alerts** (bypass Do Not Disturb / silent mode). Requires
   Apple entitlement, App Store review approval, and a different APNs
   payload structure.
3. **"iOS notifications don't arrive at all"** as a symptom. If this is
   still happening after phase 2, the root cause is almost certainly the
   Firebase Console APNs Authentication Key (`.p8`) being missing,
   expired, revoked in App Store Connect, or registered under the wrong
   bundle ID. See `mobile/BUILD_IOS.md` → "Verify Firebase / APNs setup".

---

## Reference: commit ledger

- Backend repo `main`:
  - `1c0615b` — Split FCM payload: real alarms use siren channel, routine events use default
  - *(phase-1 mitigation commit — see this repo's `git log`)* — hardcode iosSound="default" with TODO for phase 2
  - *(phase-2 follow-up, not yet created)* — Restore `isAlarm ? "alert.caf" : "default"` after iOS rollout

- Mobile repo `main`:
  - `c3006c1` — feat: (push) +bundle alert.caf siren for iOS alarms +Mac build guide
  - `03a334a` — fix: (push) +split alarm vs default channel +unique notification IDs +guard data-only messages
