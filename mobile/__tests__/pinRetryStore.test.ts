/**
 * Covers the pairing rate limit.
 *
 * The old version was a single device-wide counter cleared only by a successful pairing, so three
 * wrong codes bricked pairing on that handset permanently: logging out did not clear it, another
 * account did not clear it, and it never expired. The client hit exactly that and reported it four
 * ways (14 Oct, G/H/I/J), ending with "after uninstalling and reinstalling it worked". These tests
 * pin the two properties that stop it happening again: it is per panel, and it lapses.
 *
 * @format
 */

import {
  getPinRetryCount,
  incrementPinRetryCount,
  resetPinRetryCount,
  pinRetryCooldownRemaining,
  MAX_PIN_RETRIES,
  PIN_RETRY_WINDOW_MS,
} from '../src/pairing/pinRetryStore';

const PANEL_A = 'CRS186-0700198002';
const PANEL_B = 'CSU338-0C001C0022';

beforeEach(() => {
  (globalThis as { __resetAsyncStorage?: () => void }).__resetAsyncStorage?.();
});

describe('pairing retry limit', () => {
  it('counts attempts for the panel being paired', async () => {
    expect(await getPinRetryCount(PANEL_A)).toBe(0);
    expect(await incrementPinRetryCount(PANEL_A)).toBe(1);
    expect(await incrementPinRetryCount(PANEL_A)).toBe(2);
    expect(await getPinRetryCount(PANEL_A)).toBe(2);
  });

  it('does not let a wrong code for one panel block a different panel', async () => {
    for (let i = 0; i < MAX_PIN_RETRIES; i++) await incrementPinRetryCount(PANEL_A);
    expect(await getPinRetryCount(PANEL_A)).toBe(MAX_PIN_RETRIES);
    // The client was blocked from pairing *other* panels too. This is that case.
    expect(await getPinRetryCount(PANEL_B)).toBe(0);
  });

  it('lapses on its own, so a lockout is never permanent', async () => {
    const t0 = 1_000_000;
    for (let i = 0; i < MAX_PIN_RETRIES; i++) await incrementPinRetryCount(PANEL_A, t0);
    expect(await getPinRetryCount(PANEL_A, t0)).toBe(MAX_PIN_RETRIES);

    // 15 hours later the client was still locked out. It must have lifted well before then.
    const later = t0 + PIN_RETRY_WINDOW_MS + 1;
    expect(await getPinRetryCount(PANEL_A, later)).toBe(0);
  });

  it('starts a fresh window after one lapses rather than resuming the old count', async () => {
    const t0 = 1_000_000;
    await incrementPinRetryCount(PANEL_A, t0);
    await incrementPinRetryCount(PANEL_A, t0);
    const later = t0 + PIN_RETRY_WINDOW_MS + 1;
    expect(await incrementPinRetryCount(PANEL_A, later)).toBe(1);
  });

  it('reports how long is left so the user is not left guessing', async () => {
    const t0 = 1_000_000;
    for (let i = 0; i < MAX_PIN_RETRIES; i++) await incrementPinRetryCount(PANEL_A, t0);
    expect(await pinRetryCooldownRemaining(PANEL_A, t0)).toBe(PIN_RETRY_WINDOW_MS);
    expect(await pinRetryCooldownRemaining(PANEL_A, t0 + PIN_RETRY_WINDOW_MS)).toBe(0);
  });

  it('reports no cooldown before the limit is reached', async () => {
    await incrementPinRetryCount(PANEL_A);
    expect(await pinRetryCooldownRemaining(PANEL_A)).toBe(0);
  });

  it('clears on a successful pairing', async () => {
    await incrementPinRetryCount(PANEL_A);
    await resetPinRetryCount(PANEL_A);
    expect(await getPinRetryCount(PANEL_A)).toBe(0);
  });

  it('treats the identifier case-insensitively, as the panel field is typed by hand', async () => {
    await incrementPinRetryCount(PANEL_A.toLowerCase());
    expect(await getPinRetryCount(PANEL_A.toUpperCase())).toBe(1);
  });
});
