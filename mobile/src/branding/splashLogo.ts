import { logoDataUri } from './logoUri';
import type { AccountDevice } from '../api/types';
import type { StoredSelection } from '../panels/selectionStorage';

/**
 * Which Logo1 the splash opens on, decided from the account payload at login.
 *
 * The panel the user actually uses decides the brand. First-panel-wins on its own meant a stale
 * logo left on another panel kept winning at every login, so a newly uploaded one showed once and
 * then reverted the next morning ("when opening the app, it appears with a black background" --
 * client, 3 Nov). Before any panel has been chosen there is nothing better to go on, so it still
 * falls back to the first panel carrying one.
 *
 * Pure on purpose: the storage read lives in the caller so this can be tested without one.
 */
export function pickSplashLogo(devices: AccountDevice[], selection: StoredSelection | null): string | undefined {
  const chosen = selection
    ? devices.find((d) => d.DeviceId === selection.deviceId && d.Logo1Base64)
    : undefined;
  const device = chosen ?? devices.find((d) => d.Logo1Base64);
  return device?.Logo1Base64 ? logoDataUri(device.Logo1Base64, device.Logo1MimeType) : undefined;
}
