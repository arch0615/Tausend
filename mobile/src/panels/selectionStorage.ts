import AsyncStorage from '@react-native-async-storage/async-storage';
import type { PanelKind } from './PanelContext';

// Which panel the user last chose, on this device. It lives in its own module because two places
// need it and neither can import the other: PanelContext owns the selection, and AuthContext has
// to know it at login to pick the right brand for the splash. PanelContext already imports
// useAuth, so AuthContext importing PanelContext back would be a cycle.
export const SELECTED_PANEL_KEY = 'tausend-selected-panel';

export interface StoredSelection {
  kind: PanelKind;
  deviceId: number;
}

export async function readStoredSelection(): Promise<StoredSelection | null> {
  try {
    const raw = await AsyncStorage.getItem(SELECTED_PANEL_KEY);
    return raw ? (JSON.parse(raw) as StoredSelection) : null;
  } catch {
    // Corrupt or unreadable storage just means "no panel remembered", which every caller handles.
    return null;
  }
}
