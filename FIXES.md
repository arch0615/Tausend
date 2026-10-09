# fixes (PDF "app 2026 comentarios 2-10-26", 21 items)

## Verified working

Tested on a release build running on a real device and an emulator.

| # | Issue | Fix |
|---|---|---|
| 5 | "Salidas programadas" did nothing | Removed from the menu, as the client asked |
| 8 | Hamburger menu stopped responding after adding a user label | Menu state was getting stuck open. Resets on every screen change now, and the button toggles instead of only opening |
| 13 | "Central Wi-Fi" wording | Now "Central IP" with the client's new text. 11 other screens said "central Wi-Fi" too, all changed |
| 18 | Fonts inconsistent | Status text was using a monospace font. Removed, everything is the system font now |
| 19 | "PIN de SMS" | Now "Clave SMS", in the field label, the description and all messages |
| 20 | "Instalador" missing from menu | It was hidden behind an account role. Visible again |
| 21 | Wrong password locked you out until app restart | The screen was clearing the password box on failure, which left Android's own input holding the old text. It no longer touches the field |

## Also fixed, not on the client's list

Three bugs found by actually running the app. None of them were visible to the type checker, the linter or the tests.

**Startup crash.** The app died on any activity recreation: rotating the phone, a system setting change, or returning to it after Android reclaimed its memory. `MainActivity` was missing the `onCreate(null)` override that react-native-screens requires.

**Logout left the login screen dead.** Logging out closed the menu and tore down the navigation stack in the same instant, which left an invisible modal window over the login screen. Since the loading overlay is full screen, it swallowed every tap: no error, no reaction to anything, until the app was force closed. Logging out now waits a frame, and the login screen clears the stuck state whenever it regains focus.

**Buttons only responded on the text.** Every button in the app, not just login. Measured on a device: the button is 127px tall but only the middle 54px reacted, exactly the height of its label. Tapping the coloured area above or below did nothing. The padding and shadow were on the Pressable itself with only a Text inside, where the rest of the app wraps a styled View. Now the whole button is tappable, with a little margin past the edge on top.

## Needs the backend redeployed

Code is done. Nothing changes until the server is updated.

| # | Issue | Fix |
|---|---|---|
| 3 | Zone list out of order, editing a zone moved it | Unnamed zones were being appended after the named ones instead of sorted by number. Same bug in exclusions and outputs, all three fixed |
| 12 | Reset password page had no "show password" | Added. The login half of this issue looks like the same fault as #14, worth retesting after deploy |
| 14 | Deleted email became permanently unusable | Deleting an account kept the email attached to the dead row, so re-registering made a duplicate and login picked the wrong one. The email is released now, and a one-time cleanup frees the two stuck addresses (prueba@, diego@) |
| 15 | "Device with repeated description" on an account with no panels | Two faults. The pairing actually succeeded and then the push notification threw, so the error shown was wrong. And the duplicate name check was not ignoring deleted panels |

Push notifications also start working after deploy. The Firebase server key was from the old project, the correct one is now in place.

## Needs the client to check, panel required

No panel here to test against.

| # | Issue | Status |
|---|---|---|
| 1 | Memory / Exclusions / Faults buttons too small | Were already enlarged in an earlier round. These buttons only appear when the panel reports those states, so it needs a look on real hardware |
| 2, 16 | Connection status wrong in both directions | Fixed and unit tested. It was reading a value copied at login and never updated, now it uses what the panel actually answered. Needs a panel to confirm live |
| 4 | Installer screen did not ask for the installer code | Now asks and checks it against section 003 on the panel. The account-role requirement that was hiding this screen is gone, so it works for normal accounts again. Owning the panel is still required and every command is still logged |
| 6, 7 | Wrong user shown in events | The backend reports whatever user number the panel sends, and the lookup is a direct match. The client's own note 9 says the same: once the stored code matched the panel's real user 2 code, events came through correctly. Looks like the stored code belonged to a different user slot, not a code fault. Worth confirming on the panel |
| 10 | "User code already in use" after unlinking | All three unlink paths do remove the link. Most likely the check spans every account, so a code claimed by another test account blocks you. Needs the live database to confirm |
| 17 | Panic notification did nothing with the app open | Not panic versus emergency. Neither platform shows a notification while the app is open, and the app only showed a silent dialog. It now vibrates too. The siren itself needs an audio library, which is a new dependency |

## Notes

Issues 9 and 11 are client observations, not faults.

Tests went from 1 to 10. The new ones cover the connection status and the login fix, and were checked against the old code first to make sure they actually catch the bugs.
