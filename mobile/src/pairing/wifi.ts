import { PermissionsAndroid, Platform } from 'react-native';
import WifiManager, { CONNECT_ERRORS } from 'react-native-wifi-reborn';

// Android 6+ requires ACCESS_FINE_LOCATION to return real Wi-Fi scan results (the OS ties scan
// results to location APIs) even though this app has no use for actual location data.
export async function requestWifiScanPermission(): Promise<boolean> {
  if (Platform.OS !== 'android') return true; // iOS has no scan API to gate -- see scanForNetworks
  const granted = await PermissionsAndroid.request(PermissionsAndroid.PERMISSIONS.ACCESS_FINE_LOCATION, {
    title: 'Location permission needed',
    message: "Android requires this to scan for your alarm panel's Wi-Fi network.",
    buttonPositive: 'Allow',
    buttonNegative: 'Deny',
  });
  return granted === PermissionsAndroid.RESULTS.GRANTED;
}

export interface ScannedNetwork {
  ssid: string;
  bssid: string;
  frequency: number;
}

// Android only -- iOS does not allow apps to list nearby Wi-Fi networks. ConnectToPanelScreen
// falls back to manual SSID entry on iOS, using the same connectToPanelAp() below either way.
export async function scanForNetworks(): Promise<ScannedNetwork[]> {
  const results = await WifiManager.reScanAndLoadWifiList();
  const seen = new Set<string>();
  return results
    .filter((n) => {
      if (!n.SSID || seen.has(n.SSID)) return false;
      seen.add(n.SSID);
      return true;
    })
    .map((n) => ({ ssid: n.SSID, bssid: n.BSSID, frequency: n.frequency }));
}

export type WifiConnectError = 'wrong_password' | 'not_found' | 'timeout' | 'permission_denied' | 'unknown';

function mapConnectError(e: unknown): WifiConnectError {
  const code = (e as { code?: string })?.code ?? '';
  switch (code) {
    case CONNECT_ERRORS.authenticationErrorOccurred:
    case CONNECT_ERRORS.invalidPassphrase:
      return 'wrong_password';
    case CONNECT_ERRORS.didNotFindNetwork:
      return 'not_found';
    case CONNECT_ERRORS.timeoutOccurred:
      return 'timeout';
    case CONNECT_ERRORS.locationPermissionDenied:
    case CONNECT_ERRORS.locationPermissionMissing:
    case CONNECT_ERRORS.locationPermissionRestricted:
      return 'permission_denied';
    default:
      return 'unknown';
  }
}

export type WifiConnectResult = { ok: true } | { ok: false; error: WifiConnectError };

export async function connectToPanelAp(ssid: string, password: string): Promise<WifiConnectResult> {
  try {
    await WifiManager.connectToProtectedSSID(ssid, password, false, false);
    if (Platform.OS === 'android') {
      // The panel's AP has no internet -- without forcing this, Android will often keep routing
      // app traffic over cellular even while showing "connected" to the panel's Wi-Fi, and the
      // TCP provisioning connect would then fail to ever reach 192.168.4.1.
      await WifiManager.forceWifiUsageWithOptions(true, { noInternet: true }).catch(() => {});
    }
    return { ok: true };
  } catch (e) {
    return { ok: false, error: mapConnectError(e) };
  }
}

// Call once pairing is done (success or abandoned) -- undoes the forced-Wi-Fi routing above so
// the phone goes back to normal network selection.
export async function releasePanelApRouting(): Promise<void> {
  if (Platform.OS !== 'android') return;
  await WifiManager.forceWifiUsageWithOptions(false, { noInternet: false }).catch(() => {});
}

export async function getCurrentSsid(): Promise<string | null> {
  try {
    const ssid = await WifiManager.getCurrentWifiSSID();
    return ssid || null;
  } catch {
    return null;
  }
}
