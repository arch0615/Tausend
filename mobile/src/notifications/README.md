# Push notifications -- status

What's built and working today:
- `src/api/notifications.ts` -- `registerDeviceToken(token, os)`, calling the backend's existing
  `AccountService/CreateAccountDeviceToken` endpoint (idempotent upsert, already implemented
  server-side, confirmed against `backend-core`).
- `src/notifications/permissions.ts` -- `requestNotificationPermission()`, the Android 13+
  runtime `POST_NOTIFICATIONS` permission request. Declared in `AndroidManifest.xml`.

What's blocked, and why:
- The actual FCM wiring (`@react-native-firebase/app` + `@react-native-firebase/messaging`,
  `getToken()`/`onTokenRefresh()`/`onMessage()`/`setBackgroundMessageHandler()`/
  `onNotificationOpenedApp()`) has not been installed or written yet.
- There is no real Firebase project configuration anywhere in this app: no `google-services.json`
  (Android) or `GoogleService-Info.plist` (iOS) registered for this app's package name
  (`com.alarmastausend.tausend`), and the backend's own Firebase service-account credentials
  (`Firebase:CredentialsPath` in `backend-core`'s `appsettings.json`) are a placeholder path with
  no file behind it.
- Installing the native Firebase package and applying its required Gradle plugin without a real
  `google-services.json` present makes the Android build fail outright (the plugin errors out if
  the file is missing) -- so that step is deliberately deferred until the config file exists,
  rather than committing something that breaks every subsequent day's build.

To finish this once credentials exist:
1. Add `com.alarmastausend.tausend` as an app in the Firebase console (existing `lomo-tausend`
   project or a new one) and download `google-services.json` / `GoogleService-Info.plist`.
2. Generate a Firebase service-account key for the backend and point
   `Firebase:CredentialsPath` at it.
3. `npm install @react-native-firebase/app @react-native-firebase/messaging`, add the Gradle
   classpath + plugin application, drop in the two config files, add the iOS `AppDelegate.swift`
   configure call.
4. Call `requestNotificationPermission()` + `messaging().getToken()` after login (and on
   `onTokenRefresh()`), passing the result to `registerDeviceToken()`.
5. Wire `onMessage()` (foreground) and `setBackgroundMessageHandler()` (background/quit state).
6. Wire `onNotificationOpenedApp()` / `getInitialNotification()` to route a tap somewhere useful.
   The backend's FCM payload only includes a `data.type` field (the `NotificationTypes` enum
   name, e.g. `"Panic"`, `"Medical"`) -- there's no `DeviceId`/`AlarmIdentifier` in the payload, so
   a tap can route to a general screen (e.g. Events) but not deep-link to a specific panel without
   a backend payload change.
