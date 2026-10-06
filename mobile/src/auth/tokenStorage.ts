import * as Keychain from 'react-native-keychain';

// iOS Keychain / Android Keystore, via react-native-keychain -- tokens never touch
// AsyncStorage (unencrypted, sandbox-only-not-secure) or plain JS memory alone.
const SERVICE = 'com.alarmastausend.tausend.session';

export interface StoredTokens {
  accessToken: string;
  refreshToken: string;
}

export async function saveTokens(tokens: StoredTokens): Promise<void> {
  await Keychain.setGenericPassword('session', JSON.stringify(tokens), { service: SERVICE });
}

export async function loadTokens(): Promise<StoredTokens | null> {
  const result = await Keychain.getGenericPassword({ service: SERVICE });
  if (!result) return null;
  try {
    return JSON.parse(result.password) as StoredTokens;
  } catch {
    return null;
  }
}

export async function clearTokens(): Promise<void> {
  await Keychain.resetGenericPassword({ service: SERVICE });
}
