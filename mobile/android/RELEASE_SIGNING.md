# Android release signing

**Update**: `./gradlew assembleRelease` and `bundleRelease` both build successfully now (verified
-- Android Studio + SDK installed, and a Windows-specific native-build path-length issue found and
fixed, see the note at the bottom of this file). The only thing separating that from a real,
submittable release build is real signing credentials, which is exactly what this file covers.

`android/app/build.gradle` is already wired to pick up real release-signing credentials from
`android/keystore.properties` automatically once it exists (falls back to debug signing if it
doesn't, so this doesn't break anything for anyone who hasn't done this step yet). Both the
keystore file and `keystore.properties` are gitignored -- **never commit either one.**

## 1. Generate the upload keystore

Run this yourself (don't hand the resulting file or passwords to anyone who doesn't need them --
this key signs every future update to the app; if it's lost or compromised you may not be able to
publish updates under the same app listing). Needs a JDK (`keytool` ships with it) -- this
environment has one at `/c/Program Files/Microsoft/jdk-21.0.11.10-hotspot/bin/keytool` if you're
running this from the same machine.

```
keytool -genkeypair -v \
  -storetype PKCS12 \
  -keystore tausend-upload-key.keystore \
  -alias tausend-upload \
  -keyalg RSA -keysize 2048 -validity 10000 \
  -dname "CN=Tausend, OU=Mobile, O=Tausend, L=, S=, C="
```

- `-validity 10000` is ~27 years -- Google explicitly recommends the key outlive the app itself.
- Fill in `L=` (city), `S=` (state/province), `C=` (two-letter country code) with real values if
  you want the certificate's info to reflect the actual business -- functionally it doesn't matter
  to Android or Google Play, it's just metadata baked into the cert.
- It'll prompt you twice for a password (once for the keystore, once for the key itself -- you can
  use the same value for both, or different ones). Use a real random password, not something
  memorable/guessable -- e.g. `openssl rand -base64 24`.

Move the resulting `tausend-upload-key.keystore` file into `android/app/`.

## 2. Create `android/keystore.properties`

In `android/` (next to `gradle.properties`, NOT inside `app/`), create `keystore.properties`:

```
TAUSEND_UPLOAD_STORE_FILE=tausend-upload-key.keystore
TAUSEND_UPLOAD_KEY_ALIAS=tausend-upload
TAUSEND_UPLOAD_STORE_PASSWORD=<the password you set above>
TAUSEND_UPLOAD_KEY_PASSWORD=<the password you set above>
```

That's it -- `./gradlew bundleRelease` (or `assembleRelease`) will now sign with this key
automatically. Verify by checking the build log mentions your keystore file, not `debug.keystore`.

## 3. Back this up somewhere safe, outside this repo

The keystore file and its passwords are the actual secret -- losing them, or this machine, means
losing them for good unless backed up elsewhere (a password manager for the passwords, encrypted
cloud storage or similar for the `.keystore` file itself). This is exactly the kind of thing that's
easy to lose track of on a dev machine and very costly to lose for real.

## 4. Play App Signing (recommended, and now Google's default for new apps)

When you first upload a release to Play Console, opt into **Play App Signing** if given the
choice. Google then holds the actual app-signing key and re-signs what you upload; the key you
just generated becomes your "upload key" -- lower-stakes if compromised, since Google can revoke
and reissue an upload key without breaking the app's identity on the Play Store. Still keep it
safe regardless.

## If you hit a Windows-specific native build error

If `./gradlew assembleDebug`/`assembleRelease` fails with something like
`ninja: ... Filename longer than 260 characters` on `react-native-gesture-handler`'s native build
step, this is a known Windows-only issue, already fixed here -- see `android/app/build.gradle`'s
`externalNativeBuild.cmake.version` pin (3.31.6). The AGP-default CMake (3.22.1) bundles an older
ninja that doesn't support paths over 260 characters, even with Windows' `LongPathsEnabled`
registry setting turned on (`HKLM\SYSTEM\CurrentControlSet\Control\FileSystem`, also worth having
on regardless). If this build.gradle pin is ever reverted or a different CMake version gets forced
some other way, that's the fix to reapply. Not an issue at all when building on macOS/Linux.

---

## iOS signing -- needs a Mac, not available in this dev environment

There's no Xcode, no macOS, and no Apple Developer account access here, so none of this can be
done from this environment -- it needs to happen on an actual Mac with Xcode installed, by
whoever has (or will enroll in) the Apple Developer Program. Checklist for whoever does that:

1. Apple Developer Program enrollment ($99/year), if not already done.
2. In Xcode or the Apple Developer portal: register the App ID `com.alarmastausend.tausend`
   (must match `PRODUCT_BUNDLE_IDENTIFIER` already set in the Xcode project).
3. Create a **Distribution Certificate** (for App Store submission) in the Developer portal or via
   Xcode's automatic signing.
4. Create an **App Store Provisioning Profile** tied to that App ID and certificate.
5. In Xcode's project signing settings, either let Xcode manage signing automatically (simplest,
   recommended for a single-developer/small-team setup) or select the profile/certificate
   manually.
6. Archive and upload via Xcode Organizer, or `xcodebuild -exportArchive` + Transporter, to get
   the build into App Store Connect.

None of this can be prepared or verified further from here -- it's a hard requirement on
Xcode/macOS access, the same constraint that's applied to every iOS-native-tooling decision in
this project so far.
