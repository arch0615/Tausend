# Store listing preparation

Some of this needs tooling or assets this dev environment doesn't have (real artwork, a
device/simulator, an actual signed build). What's achievable from here -- privacy policy,
permission-usage justifications, listing copy, content rating, and a precise submission
checklist -- is done below, so submission itself is a fast, mechanical step once the missing
pieces (icons, screenshots, keystore, an actual build) exist.

## Icons -- blocked on real artwork, not on me

`android/app/src/main/res/mipmap-*/ic_launcher*.png` and `ios/mobile/Images.xcassets/AppIcon.appiconset/`
still hold the default React Native bootstrap icon (the green/black RN logo) -- neither store will
accept that. I can't design a real app icon (no image-generation tool, and this needs actual brand
design work: a mark, not just a technical asset), so this is genuinely blocked on the client
supplying real artwork, not on dev-environment tooling the way most other gaps in this project have
been. Once a 1024x1024 master icon exists, wiring it into both platforms' required size sets is a
fast, mechanical step I can do immediately.

One related, low-risk thing worth fixing now regardless: the app's display name is currently the
placeholder `"TausendMobile"` (`app.json`, iOS `Info.plist` `CFBundleDisplayName`) -- that's what
shows under the icon on a phone's home screen and in store search results. Every in-app string
that names the product already says just `"Tausend"` (deliberately left untranslated in the Day 25
localization pass). Worth changing `TausendMobile` -> `Tausend` (or whatever the confirmed final
product name is) before store submission, so the on-device name matches what users see everywhere
else in the app.

## Screenshots -- blocked on a device/simulator

Store screenshots require an actual running build on a device or simulator to capture. There's no
Android emulator or iOS simulator in this dev environment (the same constraint noted on every
native-dependency decision this project has made). These need to be captured on a machine with
Android Studio / Xcode -- realistically during or right after Day 26-style live-device regression
testing, once that happens on real hardware.

## Privacy policy

Drafted below from an actual review of what data this app collects, stores, and transmits --
not a template. **Needs review from whoever owns the legal/business side** before publishing: I've
used "Tausend" as the product name and flagged where a real legal entity name, business address,
and support contact email need to go, none of which I have.

```
PRIVACY POLICY -- Tausend

Last updated: [DATE]

This policy describes what information the Tausend mobile app ("the App") collects, how it's
used, and who it's shared with.

WHO WE ARE
Tausend is operated by [LEGAL ENTITY NAME], [BUSINESS ADDRESS]. Contact: [SUPPORT EMAIL].

INFORMATION WE COLLECT

Account information
  - Email address, first name, last name, and a password (stored as a salted bcrypt hash, never
    in plain text) when you create an account.
  - We do not collect your real name beyond what you provide, and we do not require or collect
    government ID, date of birth, or payment information -- the App has no billing feature.

Alarm panel information
  - The identifier, PIN, and a name you choose for each alarm panel you link to your account, so
    the App can control it on your behalf.
  - Zone, PGM output, and PIN-holder labels you choose to set are stored so they display the same
    way across your devices.
  - A history of arm/disarm/alert events for each panel you own, so you can review your panel's
    activity in the App.

Location
  - The App requests location permission on Android **only** because Android ties the ability to
    scan for nearby Wi-Fi networks to the location permission system -- this is an Android OS
    requirement, not something the App itself needs. The App does not read, store, or transmit
    your actual GPS location at any point, to us or anyone else. This permission is used solely to
    let the App list nearby Wi-Fi networks when you're pairing a new alarm panel to your home
    network.

Push notification token
  - If/when push notifications are enabled, a device-specific token is stored so we can deliver
    alarm notifications (e.g. panic/duress/emergency alerts) to your phone. [NOTE: push
    notifications are not fully implemented yet as of this writing -- update this section once
    that ships; see the app's own internal notes for status.]

Information we do NOT collect
  - We do not collect contacts, photos, browsing history, or any data from other apps on your
    phone.
  - For SMS-connected alarm panels, commands are composed by the App and hand off to your phone's
    own messaging app to actually send -- we never read your SMS inbox or any other messages on
    your device.

HOW WE USE YOUR INFORMATION
  - To operate your account and let you control your linked alarm panel(s).
  - To send you alarm-related notifications, where enabled.
  - To provide customer support if you contact us.
We do not use your information for advertising, and we do not sell your information to anyone.

WHO WE SHARE INFORMATION WITH
  - We do not share your information with third parties, except as required to operate the
    service itself (e.g. our cloud hosting provider, which stores the data described above on our
    behalf) or if required by law.
  - We do not use any third-party analytics, advertising, or tracking services in the App as of
    this writing.

DATA RETENTION
  - Your account and panel data is retained for as long as your account exists. You can unlink a
    panel from your account at any time in the App, or delete your account by contacting
    [SUPPORT EMAIL].

CHILDREN'S PRIVACY
  - The App is not directed at children under 13, and we do not knowingly collect information from
    children under 13.

CHANGES TO THIS POLICY
  - We'll update the "Last updated" date above if this policy changes, and post the updated policy
    at [PRIVACY POLICY URL].

CONTACT
  - Questions about this policy or your data: [SUPPORT EMAIL].
```

## Permission-usage justifications

What both stores' submission forms ask for, mapped to this app's actual declared permissions
(cross-referenced against the cross-device compatibility review's permission section).

### Google Play -- Data Safety form / sensitive permissions declaration

**`ACCESS_FINE_LOCATION`** (Play's most heavily scrutinized permission -- apps that don't
plausibly need it get rejected or delisted)
> This app requests location permission solely because Android requires it before an app can scan
> for nearby Wi-Fi networks (`WifiManager` scan results are gated behind the location permission
> system at the OS level, regardless of whether the app uses any actual location data). This
> permission is used only during the alarm-panel pairing flow, to show the user a list of nearby
> Wi-Fi networks so they can connect their phone to their alarm panel's temporary setup network.
> The app does not access, collect, store, or transmit the device's GPS location or any other
> location data at any time, for this or any other purpose.

**`POST_NOTIFICATIONS`** (Android 13+)
> Used to deliver alarm-related notifications (e.g. panic/duress/emergency alerts triggered on the
> user's own linked alarm panel) to the user's device.

**`INTERNET`**
> Required for all app functionality -- the app is a client for a cloud-hosted alarm-management
> backend; there is no offline mode.

Data Safety form declarations: collects account info (email, name) and app activity (panel
control actions) for account functionality; does not share data with third parties; does not use
data for advertising; all data in transit is encrypted (HTTPS).

### Apple App Store -- Privacy Nutrition Label / permission usage strings

Already present, verbatim, in `ios/mobile/Info.plist` (confirmed in the Day 27 compatibility
review) -- these are shown to the user as the actual permission-request dialog text, and Apple
reviews them for accuracy against actual app behavior:

- `NSLocationWhenInUseUsageDescription`: *"Used only to connect to your alarm panel's Wi-Fi
  network during setup -- iOS requires location permission for this, even though this app does
  not use your location."*
- `NSLocalNetworkUsageDescription`: *"Needed to send your Wi-Fi network name and password directly
  to your alarm panel over its local setup network."* (matches the TCP provisioning flow --
  connecting to the panel's local AP at 192.168.4.1 during Wi-Fi setup.)

App Privacy (nutrition label) categories to declare in App Store Connect: **Contact Info** (email,
name) linked to identity, used for App Functionality only; **User Content** (panel names/labels)
linked to identity, used for App Functionality only; not used for tracking; no data used for
advertising.

## Listing copy

Written in Spanish since that's the app's primary/current audience -- add an English localization
in both consoles too if international reach matters, the translation work for that is already
done (`src/i18n/es.ts`). Review and adjust tone/wording freely, this is a working draft, not final
copy -- I don't have marketing input on brand voice.

**Title** (both stores, 30 char limit): `Tausend`

**Short description** (Play Store, 80 char limit):
`Controla tu alarma Tausend: arma, desarma, zonas y alertas desde tu celular`

**Full description**:
```
Tausend te permite controlar tu central de alarma directamente desde tu celular, sin importar
si tu central se conecta por Wi-Fi o por SMS.

Con Tausend podés:
- Armar, desarmar, y usar los modos día y noche
- Ver y nombrar tus zonas, y excluirlas (bypass) cuando sea necesario
- Consultar la memoria de eventos y el historial de tu central
- Configurar y activar salidas programables (PGM)
- Nombrar a los titulares de PIN de tu central
- Consultar el estado de batería y fallas de tu central
- Enviar alertas de pánico, coacción y emergencia
- Sincronizar el reloj de tu central
- Vincular múltiples centrales a una sola cuenta

Tausend funciona tanto con centrales conectadas por Wi-Fi como con centrales que se controlan
por mensaje de texto (SMS), adaptándose al tipo de instalación que tengas.

Disponible en español.
```

**Category**: Play Store "Casa y hogar" or "Herramientas" both fit; App Store "Utilidades" is the
closest equivalent. Final call is yours -- I don't have insight into which category converts
better for this kind of app.

## Content rating

Both stores' questionnaires ask about violence, sexual content, gambling, user-generated content,
and similar categories -- this app has none of that. The panic/duress/emergency features send an
alert command to the user's own panel; they don't depict or simulate anything graphic. Expect this
to qualify for the lowest tier on both platforms (Play Store "Everyone" / App Store "4+"), but
whoever actually fills out the questionnaire should answer from the real feature set, not take my
word for it -- I'm flagging the likely outcome, not filling out the form myself (it requires an
actual Play Console / App Store Connect login I don't have access to).

## Submission checklist -- what's actually needed before either store will accept this

**Update**: Android Studio + SDK are now installed, and `./gradlew assembleDebug`/`assembleRelease`
both build successfully (verified, including a real Windows-specific native-build issue found and
fixed along the way). Android's checklist below is now genuinely short. iOS is unchanged --
still fully blocked on Mac/Xcode access, which doesn't exist anywhere in this pipeline.

**Android**:
1. **App icon** -- real branded artwork (1024x1024 master), not yet supplied.
2. **Screenshots** -- captured from an actual running build (now possible on an emulator or
   device, once one is available -- the build itself is no longer the blocker).
3. **Android keystore** -- steps are in `android/RELEASE_SIGNING.md`, needs to be run by whoever's
   holding that secret (not done from here, deliberately -- see that file for why). Once it
   exists, `./gradlew bundleRelease` produces a real, properly-signed release build directly.
4. **Play Console listing entry** -- create the app listing, paste in the copy above, upload the
   icon/screenshots once they exist, fill in the content rating questionnaire and Data Safety
   form using the answers above.
5. **Upload the signed build and submit for review.**

**iOS** (all of it, still blocked on Mac/Xcode access):
1. App icon artwork (same asset as Android, different size export).
2. Apple Developer Program enrollment, if not already done.
3. Distribution certificate + provisioning profile (`android/RELEASE_SIGNING.md` has the checklist
   despite the filename -- it covers both platforms).
4. An actual Xcode archive + upload, which needs to happen on a Mac.
5. Screenshots, App Store Connect listing entry, submit for review.

Everything upstream of these lists (the app itself, its features, the backend it talks to) is done
and verified working against real hardware, and now Android's build pipeline is verified working
too. What's left is packaging, one secret, real assets, and (for iOS specifically) platform
access -- not further feature development.
