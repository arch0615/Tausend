import { PermissionsAndroid, Platform } from 'react-native';

// Android 13+ (API 33) requires this runtime permission before any notification -- local or
// push -- can be shown. No-op (resolves true) on earlier Android versions. iOS has no equivalent
// standalone permission API in RN core; that side is requested by the push library itself once
// FCM is wired up (see notifications/README.md for what's still pending).
export async function requestNotificationPermission(): Promise<boolean> {
  if (Platform.OS !== 'android' || Platform.Version < 33) return true;
  const result = await PermissionsAndroid.request(PermissionsAndroid.PERMISSIONS.POST_NOTIFICATIONS);
  return result === PermissionsAndroid.RESULTS.GRANTED;
}
