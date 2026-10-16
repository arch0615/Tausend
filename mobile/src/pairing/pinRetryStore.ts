import AsyncStorage from '@react-native-async-storage/async-storage';

// Rate limit for wrong user codes during pairing.
//
// This used to be a single device-wide counter that only ever got cleared by a successful
// pairing. Three wrong codes and the phone could never pair again: logging out did not help,
// signing in as a different account did not help, and it never expired, so the only way back was
// to uninstall the app. The client hit exactly that and reported it four separate ways (14 Oct,
// items G, H, I and J). A rate limit is meant to slow someone down, not brick the handset.
//
// Two changes fix that without giving up the limit:
//   * counted per panel, so a wrong code for one panel no longer blocks every other panel
//   * the window expires on its own, so the lockout lifts after a cooldown instead of persisting
//
// Scoped to the account as well as the panel. It used to be keyed on the panel alone so that
// signing out could not clear it, but that meant one person exhausting their attempts locked the
// panel for every other account on the phone, which the client reported twice (14 Oct items G
// and H, then again 16 Oct: "si una sesion queda bloqueada por intentos fallidos, no debe
// afectar a otras sesiones"). Their installers share handsets, so this blocked real work.
//
// The trade-off, worth knowing: the limit can now be sidestepped by signing in as somebody else.
// There is no server-side attempt limit behind it, so this is the client's call, not a security
// control we are relying on.
const STORAGE_PREFIX = 'tausend-pairing-pin-retries:';
export const MAX_PIN_RETRIES = 3;
/** How long a run of failed attempts keeps counting before it lapses. */
export const PIN_RETRY_WINDOW_MS = 15 * 60 * 1000;

interface RetryRecord {
  count: number;
  /** When the current run of failures started, epoch ms. */
  startedAt: number;
}

const keyFor = (account: string, identifier: string) =>
  `${STORAGE_PREFIX}${account.trim().toLowerCase()}|${identifier.trim().toUpperCase()}`;

async function read(account: string, identifier: string): Promise<RetryRecord | null> {
  try {
    const raw = await AsyncStorage.getItem(keyFor(account, identifier));
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Partial<RetryRecord>;
    if (typeof parsed?.count !== 'number' || typeof parsed?.startedAt !== 'number') return null;
    return { count: parsed.count, startedAt: parsed.startedAt };
  } catch {
    // Corrupt or legacy (plain-number) value -- treat as no attempts rather than stranding the
    // user, which is the failure mode this whole change exists to remove.
    return null;
  }
}

/** Attempts used against this panel inside the current window. Expired runs report 0. */
export async function getPinRetryCount(account: string, identifier: string, now: number = Date.now()): Promise<number> {
  const rec = await read(account, identifier);
  if (!rec) return 0;
  if (now - rec.startedAt >= PIN_RETRY_WINDOW_MS) {
    await resetPinRetryCount(account, identifier);
    return 0;
  }
  return rec.count;
}

export async function incrementPinRetryCount(account: string, identifier: string, now: number = Date.now()): Promise<number> {
  const rec = await read(account, identifier);
  const live = rec && now - rec.startedAt < PIN_RETRY_WINDOW_MS ? rec : null;
  const next: RetryRecord = live
    ? { count: live.count + 1, startedAt: live.startedAt }
    : { count: 1, startedAt: now };
  await AsyncStorage.setItem(keyFor(account, identifier), JSON.stringify(next));
  return next.count;
}

export async function resetPinRetryCount(account: string, identifier: string): Promise<void> {
  await AsyncStorage.removeItem(keyFor(account, identifier));
}

/** Milliseconds until the lockout lifts, or 0 if it is not locked. Lets the screen tell the user
 * when they can try again instead of leaving them guessing. */
export async function pinRetryCooldownRemaining(account: string, identifier: string, now: number = Date.now()): Promise<number> {
  const rec = await read(account, identifier);
  if (!rec || rec.count < MAX_PIN_RETRIES) return 0;
  return Math.max(0, rec.startedAt + PIN_RETRY_WINDOW_MS - now);
}
