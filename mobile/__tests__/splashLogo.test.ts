/**
 * WHICH LOGO1 THE SPLASH OPENS ON (client, 3 Nov: "when opening the app, it appears with a black
 * background"; again 6 Nov, after a reset: "the splash screen appears without a background just
 * once. Then, closing and reopening the app, makes the splash screen reappear with a black
 * background as it was").
 *
 * Three separate faults produced that symptom over two rounds. The save order was one, covered in
 * SetPanelLogoScreen.test.tsx. The other two are both here, because both are about what this
 * function does once a specific panel is in view: first "first panel carrying a Logo1, full stop"
 * (3 Nov), then "the selected panel's logo -- but only if it has one" (6 Nov), which still went
 * hunting through other panels whenever the selected one had just been reset back to no branding.
 * Once a panel is known, its own value -- including "none" -- is now final.
 *
 * @format
 */

import { pickSplashLogo } from '../src/branding/splashLogo';
import type { AccountDevice } from '../src/api/types';

const device = (DeviceId: number, Logo1Base64?: string, Logo1MimeType = 'image/png'): AccountDevice =>
  ({ DeviceId, Description: `Panel ${DeviceId}`, Mac: `ID${DeviceId}`, IsOnline: true, Logo1Base64, Logo1MimeType } as AccountDevice);

const on = (deviceId: number) => ({ kind: 'ip' as const, deviceId });

describe('pickSplashLogo', () => {
  it("takes the selected panel's logo, not whichever panel sorts first", () => {
    // The stale one is deliberately first in the list, which is exactly the case that broke on 3 Nov.
    const devices = [device(1, 'staleBlackLogo'), device(2, 'theOneJustUploaded')];
    expect(pickSplashLogo(devices, on(2))).toBe('data:image/png;base64,theOneJustUploaded');
  });

  it('shows the default when the selected panel was reset, rather than falling back to another panel', () => {
    // The 6 Nov repro exactly: reset Logo1 on the selected panel, close and reopen the app. The
    // selected panel genuinely has no Logo1 any more -- that is not "nothing to go on", it is the
    // answer. A stale logo on another panel (panel 1, deliberately first in the list and still
    // branded) must not win just because the selected one came back empty.
    const devices = [device(1, 'staleLogoOnAnotherPanel'), device(2)];
    expect(pickSplashLogo(devices, on(2))).toBeUndefined();
  });

  it('falls back to the first panel carrying one only when no panel has been chosen yet', () => {
    // The one case where there is genuinely no "the" panel to ask: a fresh login with several
    // panels and none picked. Best-effort here is the only option.
    const devices = [device(1), device(2, 'someLogo')];
    expect(pickSplashLogo(devices, null)).toBe('data:image/png;base64,someLogo');
  });

  it('falls back when the selected panel is no longer on the account at all', () => {
    // Different from a reset: the selected panel itself is gone (unlinked, swapped), so there is
    // no "its own value" to defer to -- same as no selection yet, best-effort is all there is.
    const devices = [device(1, 'accountLogo')];
    expect(pickSplashLogo(devices, on(99))).toBe('data:image/png;base64,accountLogo');
  });

  it('gives nothing when no panel has a logo at all, so the bundled default shows', () => {
    expect(pickSplashLogo([device(1), device(2)], on(1))).toBeUndefined();
    expect(pickSplashLogo([], null)).toBeUndefined();
  });

  it('keeps the stored mime type, so a transparent png stays transparent', () => {
    const devices = [device(1, 'transparentOne', 'image/png')];
    expect(pickSplashLogo(devices, on(1))).toBe('data:image/png;base64,transparentOne');
  });
});
