import AsyncStorage from '@react-native-async-storage/async-storage';

// Persists across app restarts, matching the previous app's localStorage-backed retry counter --
// deliberately not tied to login/session state, so a user can't work around the lockout by
// simply logging back in.
const STORAGE_KEY = 'tausend-pairing-pin-retries';
export const MAX_PIN_RETRIES = 3;

export async function getPinRetryCount(): Promise<number> {
  const raw = await AsyncStorage.getItem(STORAGE_KEY);
  const parsed = raw ? parseInt(raw, 10) : 0;
  return Number.isFinite(parsed) ? parsed : 0;
}

export async function incrementPinRetryCount(): Promise<number> {
  const next = (await getPinRetryCount()) + 1;
  await AsyncStorage.setItem(STORAGE_KEY, String(next));
  return next;
}

export async function resetPinRetryCount(): Promise<void> {
  await AsyncStorage.removeItem(STORAGE_KEY);
}
