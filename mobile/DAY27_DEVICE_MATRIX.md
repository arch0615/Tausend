# Day 27 AM -- cross-device compatibility review (static, not live device testing)

The schedule calls for "cross-device testing matrix -- Android OS spread, iOS version spread."
There's no Android emulator, iOS simulator, or physical device available in this dev environment
(same constraint noted on every prior native-dependency decision this project has made), so this
is a static code/config review of what the app declares support for and how it handles
version-specific platform behavior -- not a substitute for actually running the app across real
OS versions, which belongs on a machine with that tooling before store submission (Day 28).

## Declared support range

- **React Native 0.86.0**, React 19.2.3.
- **Android**: `minSdkVersion 24` (Android 7.0, 2016) -- `targetSdkVersion 36` (current). Wide,
  standard range for a new RN 0.86 app; nothing in the app's own code assumes a narrower floor.
- **iOS**: `min_ios_version_supported` resolves to **15.1** (RN 0.86's own floor, not something
  this app's Podfile overrides) -- current through iOS 15.1+ (Sept 2021 onward).

## Version-conditional code, reviewed for correctness

- `src/notifications/permissions.ts` -- correctly gates the `POST_NOTIFICATIONS` runtime request
  behind `Platform.OS === 'android' && Platform.Version >= 33` (Android 13 is when that permission
  was introduced; requesting it on older Android would be a no-op at best, so the guard is
  necessary, not just defensive).
- `src/pairing/wifi.ts` -- Wi-Fi scan/connect is delegated to `react-native-wifi-reborn`, a
  maintained library that internally handles the Android 10+ `WifiNetworkSuggestion`/
  `NetworkRequest` API split from the older `WifiConfiguration` API. App-level code only branches
  on `Platform.OS`, not specific API levels, which is correct here since the library itself
  absorbs the version differences.
- `src/pairing/smsIntent.ts` -- correctly branches on `Platform.OS` for the `sms:` URI separator
  (`?` on Android, `&` on iOS) -- a real, easy-to-miss platform quirk, already handled.
- `src/auth/tokenStorage.ts` -- no OS branching at all, relies entirely on `react-native-keychain`'s
  own Keychain (iOS)/Keystore (Android) abstraction -- correct, nothing app-specific to gate.

## Permissions declared, cross-checked against OS requirements

- Android: `INTERNET`, `ACCESS_FINE_LOCATION` (required pre-scan on Android 6+, tied to the OS's
  Wi-Fi-scan-implies-location-API design, not this app's own use of location), `POST_NOTIFICATIONS`
  (Android 13+, gated correctly as above).
- iOS: `NSLocationWhenInUseUsageDescription`, `NSLocalNetworkUsageDescription` -- both present with
  clear, honest usage strings (already flagged as required for the Day 28 store-listing
  "permission-usage justifications" deliverable).

## One soft gap, not code-level

Android 6-9 in particular will silently return an empty Wi-Fi scan list if the *system* Location
toggle is off, even with the app's location *permission* granted -- a common real-device gotcha
that isn't a code bug (there's no reliable cross-version API to detect "location services off" vs
"no networks in range" distinctly) but is worth knowing about for Day 26-style live regression on
an actual Android device later. `ConnectToPanelScreen`'s existing "No networks found nearby" empty
state plus its manual-SSID-entry fallback already covers the user-facing side of this either way,
so it's not a blocking issue -- just a note for whoever does hands-on Android testing to check the
system Location toggle first if scan results come back suspiciously empty.

## Bottom line

Nothing found here blocks moving forward. The app's declared SDK/OS ranges are wide and current,
and every place in the codebase that needs to behave differently across OS versions already does.
The real test of this -- actually launching on a spread of physical/emulated devices -- still needs
to happen before store submission (Day 28), on a machine with Android Studio / Xcode available,
which this development environment does not have.
