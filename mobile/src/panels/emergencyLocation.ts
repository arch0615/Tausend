import { PermissionsAndroid, Platform, Share } from 'react-native';
import Geolocation from '@react-native-community/geolocation';

// Ported from the previous app: triggering Panic/Duress/Emergency also shared the phone's current
// GPS location via the native share sheet, so the user could immediately forward it to an
// emergency contact. That never made it into this rebuild -- the trigger only sent the panel
// command. Best-effort throughout: a location failure should never block or delay the emergency
// command itself, which is why this is fired after the command succeeds, not awaited by it.
async function requestLocationPermission(): Promise<boolean> {
  if (Platform.OS !== 'android') return true; // iOS prompts automatically via getCurrentPosition
  const granted = await PermissionsAndroid.request(PermissionsAndroid.PERMISSIONS.ACCESS_FINE_LOCATION, {
    title: 'Ubicación',
    message: 'Para compartir tu ubicación junto con la alerta de emergencia.',
    buttonPositive: 'Permitir',
    buttonNegative: 'Denegar',
  });
  return granted === PermissionsAndroid.RESULTS.GRANTED;
}

function getPosition(options: {
  enableHighAccuracy: boolean;
  timeout: number;
  maximumAge: number;
}): Promise<{ latitude: number; longitude: number }> {
  return new Promise((resolve, reject) => {
    Geolocation.getCurrentPosition(
      (position) => resolve({ latitude: position.coords.latitude, longitude: position.coords.longitude }),
      (error) => reject(error),
      options,
    );
  });
}

// A GPS fix (enableHighAccuracy) routinely fails to arrive within any reasonable timeout
// indoors -- exactly where an alarm panel (and this emergency button) typically gets used and
// tested -- which silently dropped the location every time with no visible symptom besides
// "it doesn't send the location". Falls back to network/cell-tower location, which is far faster
// and more reliable indoors, rather than giving up outright.
async function getCurrentPosition(): Promise<{ latitude: number; longitude: number }> {
  try {
    return await getPosition({ enableHighAccuracy: true, timeout: 10000, maximumAge: 30000 });
  } catch {
    return await getPosition({ enableHighAccuracy: false, timeout: 10000, maximumAge: 120000 });
  }
}

export async function shareEmergencyLocation(message: string): Promise<void> {
  try {
    const granted = await requestLocationPermission();
    if (!granted) return;
    const { latitude, longitude } = await getCurrentPosition();
    const mapsUrl = `https://maps.google.com/?q=${latitude},${longitude}`;
    await Share.share({ message: `${message}\n${mapsUrl}` });
  } catch {
    // Silent -- the panel command already went through regardless of whether location could be
    // read (GPS off, permission denied, no fix yet, share sheet dismissed, etc).
  }
}
