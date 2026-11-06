/**
 * WHICH LOGO1 THE SPLASH OPENS ON (client, 3 Nov: "when opening the app, it appears with a black
 * background").
 *
 * Two separate faults produced that. One was the save order, covered in
 * SetPanelLogoScreen.test.tsx. The other is this one: the cached splash logo is refilled from the
 * account payload at every login, and the rule was simply "first panel carrying a Logo1". With a
 * stale test logo sitting on another panel, that one won every time, so a newly uploaded logo
 * showed once and reverted the next morning. The panel the user actually uses decides it now.
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
    // The stale one is deliberately first in the list, which is exactly the case that broke.
    const devices = [device(1, 'staleBlackLogo'), device(2, 'theOneJustUploaded')];
    expect(pickSplashLogo(devices, on(2))).toBe('data:image/png;base64,theOneJustUploaded');
  });

  it('falls back to the first panel carrying one when no panel has been chosen yet', () => {
    const devices = [device(1), device(2, 'someLogo')];
    expect(pickSplashLogo(devices, null)).toBe('data:image/png;base64,someLogo');
  });

  it('falls back when the selected panel has no logo of its own', () => {
    const devices = [device(1, 'accountLogo'), device(2)];
    expect(pickSplashLogo(devices, on(2))).toBe('data:image/png;base64,accountLogo');
  });

  it('falls back when the selected panel is no longer on the account', () => {
    const devices = [device(1, 'accountLogo')];
    expect(pickSplashLogo(devices, on(99))).toBe('data:image/png;base64,accountLogo');
  });

  it('gives nothing when no panel has a logo, so the bundled default shows', () => {
    expect(pickSplashLogo([device(1), device(2)], on(1))).toBeUndefined();
    expect(pickSplashLogo([], null)).toBeUndefined();
  });

  it('keeps the stored mime type, so a transparent png stays transparent', () => {
    const devices = [device(1, 'transparentOne', 'image/png')];
    expect(pickSplashLogo(devices, on(1))).toBe('data:image/png;base64,transparentOne');
  });
});
