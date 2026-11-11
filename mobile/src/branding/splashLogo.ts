import { logoDataUri } from './logoUri';
import type { AccountDevice } from '../api/types';
import type { StoredSelection } from '../panels/selectionStorage';

/**
 * Which Logo1 the splash opens on, decided from the account payload at login.
 *
 * The panel the user actually uses decides the brand, and once that panel is known its own value
 * is final -- including "none". The first version matched the selected panel only when it HAD a
 * Logo1 (`devices.find(d => d.DeviceId === selection.deviceId && d.Logo1Base64)`), so a panel that
 * had just been reset back to no branding fell through to "first panel anywhere carrying a Logo1"
 * instead of showing the default. That is exactly "when the default images are restored, the
 * splash screen appears without a background just once (correct). Then, closing and reopening the
 * app, makes the splash screen reappear with a black background as it was (fail)" (client, 6 Nov):
 * the reset screen's own cache write briefly showed the right thing, then the next login
 * recomputed this function, found the selected panel had no Logo1 any more, and went hunting
 * through the other panels for a stale one instead of concluding "this panel has no brand, show
 * the bundled default."
 *
 * Only when NO panel has been chosen at all -- first login, several panels, none picked yet, so
 * there is no "the" panel to ask -- does it fall back to the first panel carrying one, as a
 * best-effort guess rather than an answer.
 *
 * Pure on purpose: the storage read lives in the caller so this can be tested without one.
 */
export function pickSplashLogo(devices: AccountDevice[], selection: StoredSelection | null): string | undefined {
  const selectedDevice = selection ? devices.find((d) => d.DeviceId === selection.deviceId) : undefined;
  const device = selectedDevice ?? devices.find((d) => d.Logo1Base64);
  return device?.Logo1Base64 ? logoDataUri(device.Logo1Base64, device.Logo1MimeType) : undefined;
}
