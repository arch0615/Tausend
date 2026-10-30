import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { useAuth } from '../auth/AuthContext';
import { logoDataUri } from '../branding/logoUri';

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
  // IP panels only -- SMS panels have no relay connection to report status. This value comes
  // from the account payload, i.e. it is whatever the database said at login time and never
  // changes until the next login. Use `reachabilityOf` below for what is actually true now.
  isOnline?: boolean;
  // IP panels only -- white-label branding (client, 26/27 Oct) is set against a Devices row by
  // identifier, which SMS panels don't have an equivalent of. Logo1 (portrait) replaces the eagle
  // on the login screen; Logo2 (landscape) replaces the "ALARMAS TAUSEND" banner at the top of the
  // home screen. Absent/empty means no custom branding is set for that slot.
  logo1Base64?: string;
  logo1MimeType?: string;
  logo2Base64?: string;
  logo2MimeType?: string;
}

export interface AccountBrand {
  // Pre-built data: URIs (see branding/logoUri.ts), not raw base64 -- every consumer just needs
  // <Image source={{uri}}>, none of them should have to know about MimeType at all.
  logo1Uri?: string;
  logo2Uri?: string;
}

/**
 * The account's "effective brand" for screens that show a logo regardless of which panel is
 * selected (the login screen, the home screen's top banner) -- the first IP panel with either
 * custom logo set, or undefined for the default Tausend look. Both logos come from that same
 * panel, not mixed from different ones. An account normally belongs to one reseller, so its panels
 * sharing one brand is the expected case; if they genuinely differ, whichever panel sorts first in
 * the account's own list wins, there is no defined "most important panel" otherwise.
 */
export function resolveAccountBrand(panels: Panel[]): AccountBrand | undefined {
  const branded = panels.find((p) => p.kind === 'ip' && (p.logo1Base64 || p.logo2Base64));
  if (!branded) return undefined;
  return {
    logo1Uri: branded.logo1Base64 ? logoDataUri(branded.logo1Base64, branded.logo1MimeType) : undefined,
    logo2Uri: branded.logo2Base64 ? logoDataUri(branded.logo2Base64, branded.logo2MimeType) : undefined,
  };
}

/** 'unknown' means no live command has been attempted for this panel yet this session. */
export type Reachability = 'online' | 'offline' | 'unknown';

interface PanelContextValue {
  panels: Panel[];
  selected: Panel | null;
  selectPanel: (panel: Panel) => void;
  /**
   * Live reachability, as proven by an actual command round-trip rather than the login-time
   * Devices.IsOnline flag. That flag is a database snapshot taken when the session was created
   * and is never refreshed, so it drifted in both directions: it showed "No connection" for a
   * panel that was online and working (client issue #2), and kept showing "Connected" after the
   * Wi-Fi was pulled, on the same screen that was already saying the panel was not responding
   * (client issue #16). Screens that actually talk to the panel call `reportReachability` with
   * what they observed, and every connection indicator reads from here instead.
   */
  reachabilityOf: (kind: PanelKind, deviceId: number) => Reachability;
  reportReachability: (kind: PanelKind, deviceId: number, online: boolean) => void;
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
  // Keyed "<kind>:<deviceId>". Absent = never checked this session.
  const [reachability, setReachability] = useState<Record<string, boolean>>({});

  const panels = useMemo<Panel[]>(() => {
    if (!session) return [];
    // DeviceId 0 is a sentinel the backend injects when an account has no real IP panels
    // (see AccountBusiness.Login) -- not a real device to show.
    const ipPanels: Panel[] = session.account.Devices
      .filter((d) => d.DeviceId !== 0)
      .map((d) => ({
        kind: 'ip',
        deviceId: d.DeviceId,
        description: d.Description,
        identifier: d.Mac,
        isOnline: d.IsOnline,
        logo1Base64: d.Logo1Base64,
        logo1MimeType: d.Logo1MimeType,
        logo2Base64: d.Logo2Base64,
        logo2MimeType: d.Logo2MimeType,
      }));
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
  // unlinked, or the very first panel being paired).
  //
  // It only auto-picks when there is exactly one panel to pick. With several, falling back to
  // panels[0] dropped the user straight into whichever panel happened to be first and the device
  // selector was never shown, so there was no point at which they could see the others or their
  // connection state: "si hay mas de una central registrada, la app deberia abrir la pantalla
  // Selector de equipo" (client, 16 Oct). Leaving the selection null makes Home show the selector
  // instead.
  useEffect(() => {
    if (!hydrated) return;
    const stillExists = selection && panels.some((p) => p.kind === selection.kind && p.deviceId === selection.deviceId);
    if (stillExists) return;
    const next = panels.length === 1 ? { kind: panels[0].kind, deviceId: panels[0].deviceId } : null;
    setSelection(next);
    if (next) {
      AsyncStorage.setItem(STORAGE_KEY, JSON.stringify(next));
    } else {
      AsyncStorage.removeItem(STORAGE_KEY);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hydrated, panels]);

  // A new session means a different account's panels -- anything learned about the old one's
  // reachability is meaningless now.
  useEffect(() => {
    setReachability({});
  }, [session?.account.Email]);

  const reachabilityOf = useCallback(
    (kind: PanelKind, deviceId: number): Reachability => {
      const known = reachability[`${kind}:${deviceId}`];
      return known === undefined ? 'unknown' : known ? 'online' : 'offline';
    },
    [reachability],
  );

  const reportReachability = useCallback((kind: PanelKind, deviceId: number, online: boolean) => {
    // Only writes when the value actually changes, so a status poll on a steady connection
    // doesn't re-render every consumer of this context on every tick.
    setReachability((prev) => (prev[`${kind}:${deviceId}`] === online ? prev : { ...prev, [`${kind}:${deviceId}`]: online }));
  }, []);

  function selectPanel(panel: Panel) {
    const next: StoredSelection = { kind: panel.kind, deviceId: panel.deviceId };
    setSelection(next);
    AsyncStorage.setItem(STORAGE_KEY, JSON.stringify(next));
  }

  const selected = panels.find((p) => selection && p.kind === selection.kind && p.deviceId === selection.deviceId) ?? null;

  return (
    <PanelContext.Provider value={{ panels, selected, selectPanel, reachabilityOf, reportReachability }}>
      {children}
    </PanelContext.Provider>
  );
}

export function usePanels(): PanelContextValue {
  const ctx = useContext(PanelContext);
  if (!ctx) throw new Error('usePanels must be used within PanelProvider');
  return ctx;
}
