import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { useAuth } from '../auth/AuthContext';

// A "panel" here unifies AccountDevice (IP) and AccountSmsDevice (SMS) into one list for
// selection UI. The previous (Ionic) app tracked selection as an index into whichever of two
// parallel arrays was active, guarded inconsistently across call sites (some defaulted a missing
// index to 0, some didn't) -- see panels/PanelContext.tsx callers for where that mattered. Since
// DeviceId is already a real, globally-unique backend primary key for both panel kinds, there's
// no need to reproduce that fragility: selection here is just {kind, deviceId}, which stays valid
// even if the underlying lists are re-ordered or refreshed.
export type PanelKind = 'ip' | 'sms';

export interface Panel {
  kind: PanelKind;
  deviceId: number;
  description: string;
  identifier: string; // AccountDevice.Mac (IP) or AccountSmsDevice.Identifier (SMS) -- needed by
  // BlockPIN/DissasociateCentral, which key off the panel's identifier, not its DeviceId.
  isOnline?: boolean; // IP panels only -- SMS panels have no relay connection to report status
}

interface PanelContextValue {
  panels: Panel[];
  selected: Panel | null;
  selectPanel: (panel: Panel) => void;
}

const PanelContext = createContext<PanelContextValue | null>(null);

const STORAGE_KEY = 'tausend-selected-panel';

interface StoredSelection {
  kind: PanelKind;
  deviceId: number;
}

export function PanelProvider({ children }: { children: ReactNode }) {
  const { session } = useAuth();
  const [selection, setSelection] = useState<StoredSelection | null>(null);
  const [hydrated, setHydrated] = useState(false);

  const panels = useMemo<Panel[]>(() => {
    if (!session) return [];
    // DeviceId 0 is a sentinel the backend injects when an account has no real IP panels
    // (see AccountBusiness.Login) -- not a real device to show.
    const ipPanels: Panel[] = session.account.Devices
      .filter((d) => d.DeviceId !== 0)
      .map((d) => ({ kind: 'ip', deviceId: d.DeviceId, description: d.Description, identifier: d.Mac, isOnline: d.IsOnline }));
    const smsPanels: Panel[] = session.account.SmsDevices.map((d) => ({
      kind: 'sms',
      deviceId: d.DeviceId,
      description: d.Description,
      identifier: d.Identifier,
    }));
    return [...ipPanels, ...smsPanels];
  }, [session]);

  useEffect(() => {
    (async () => {
      const raw = await AsyncStorage.getItem(STORAGE_KEY);
      if (raw) {
        try {
          setSelection(JSON.parse(raw));
        } catch {
          // ignore corrupt storage -- falls through to the reconcile effect below, which
          // picks a default once the panel list is available
        }
      }
      setHydrated(true);
    })();
  }, []);

  // Keeps the selection valid as the panel list changes (a fresh login, a panel getting
  // unlinked, or the very first panel being paired) -- falls back to the first available panel,
  // or clears out if there are none.
  useEffect(() => {
    if (!hydrated) return;
    const stillExists = selection && panels.some((p) => p.kind === selection.kind && p.deviceId === selection.deviceId);
    if (stillExists) return;
    const next = panels[0] ? { kind: panels[0].kind, deviceId: panels[0].deviceId } : null;
    setSelection(next);
    if (next) {
      AsyncStorage.setItem(STORAGE_KEY, JSON.stringify(next));
    } else {
      AsyncStorage.removeItem(STORAGE_KEY);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hydrated, panels]);

  function selectPanel(panel: Panel) {
    const next: StoredSelection = { kind: panel.kind, deviceId: panel.deviceId };
    setSelection(next);
    AsyncStorage.setItem(STORAGE_KEY, JSON.stringify(next));
  }

  const selected = panels.find((p) => selection && p.kind === selection.kind && p.deviceId === selection.deviceId) ?? null;

  return <PanelContext.Provider value={{ panels, selected, selectPanel }}>{children}</PanelContext.Provider>;
}

export function usePanels(): PanelContextValue {
  const ctx = useContext(PanelContext);
  if (!ctx) throw new Error('usePanels must be used within PanelProvider');
  return ctx;
}
