import AsyncStorage from '@react-native-async-storage/async-storage';

// White-label branding (client, 26/27 Oct): the login screen renders before any account is known,
// so it can't ask the server which brand to show. This remembers the last successfully logged-in
// account's Logo1 (portrait, the only logo the login screen uses) on this device, so returning to
// it (app relaunch, or after logging out) still shows it instead of falling back to the default
// Tausend look. A first install, or an account with no branded panel, has nothing cached here and
// shows the default. Logo2 (the home screen banner) needs no cache -- it is only ever read after a
// real session exists, where the live account data already has it.
const STORAGE_KEY = 'tausend-cached-brand-logo1';

export async function getCachedBrandLogo1(): Promise<string | null> {
  try {
    return await AsyncStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}

export async function setCachedBrandLogo1(logo1Base64: string | undefined): Promise<void> {
  try {
    if (logo1Base64) {
      await AsyncStorage.setItem(STORAGE_KEY, logo1Base64);
    } else {
      await AsyncStorage.removeItem(STORAGE_KEY);
    }
  } catch {
    // Best-effort -- the login screen just falls back to the default look if this didn't save.
  }
}
