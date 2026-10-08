import {
  AuthorizationStatus,
  getInitialNotification,
  getMessaging,
  getToken,
  onMessage,
  onNotificationOpenedApp,
  onTokenRefresh,
  requestPermission,
  setBackgroundMessageHandler,
} from '@react-native-firebase/messaging';
import { Alert, Platform, Vibration } from 'react-native';
import { registerDeviceToken } from '../api/notifications';
import { requestNotificationPermission } from './permissions';
import { navigationRef } from '../navigation/navigationRef';

const PLATFORM_OS: 'Android' | 'iOS' = Platform.OS === 'ios' ? 'iOS' : 'Android';

async function registerCurrentToken(): Promise<void> {
  try {
    const token = await getToken(getMessaging());
    await registerDeviceToken(token, PLATFORM_OS);
  } catch {
    // Best-effort -- see registerDeviceToken's own comment: this is safe to just retry on the
    // next login or the next onTokenRefresh firing, not worth surfacing to the user.
  }
}

// Call once a session exists -- right after login, and again once a stored session is restored
// at launch (see AuthContext). Requests the platform's notification permission first; does
// nothing further if the user declines.
export async function initPushNotifications(): Promise<void> {
  try {
    if (Platform.OS === 'ios') {
      const authStatus = await requestPermission(getMessaging());
      const enabled =
        authStatus === AuthorizationStatus.AUTHORIZED || authStatus === AuthorizationStatus.PROVISIONAL;
      if (!enabled) return;
    } else {
      const granted = await requestNotificationPermission();
      if (!granted) return;
    }
    await registerCurrentToken();
  } catch {
    // Best-effort at startup -- a permission dialog failing or the native module not being
    // ready yet shouldn't block login/app launch.
  }
}

/** Call from a top-level effect (see AuthContext) -- returns the unsubscribe function. */
export function subscribeToTokenRefresh(): () => void {
  return onTokenRefresh(getMessaging(), () => {
    registerCurrentToken();
  });
}

// Used on logout to tell the backend which push token to drop (see Logout.sql) -- without this,
// AccountService/Logout has nothing to delete, and the device keeps receiving this account's
// notifications until some other account re-registers the same token.
export async function getCurrentDeviceToken(): Promise<string | null> {
  try {
    return await getToken(getMessaging());
  } catch {
    return null;
  }
}

// data.type is the one field the backend's FCM payload actually includes (see backend-core's
// FirebasePushNotificationBusiness.CreatePayload) -- there's no DeviceId/AlarmIdentifier in it,
// so a tap can only route to a general screen, not a specific panel. Deep-linking to the exact
// panel/event would need that added to the payload server-side first (see notifications/README.md).
function openFromNotification(): void {
  if (!navigationRef.isReady()) return;
  navigationRef.navigate('Home' as never);
}

/** Foreground -> tapped a system notification while the app was backgrounded. */
export function subscribeToNotificationOpen(): () => void {
  return onNotificationOpenedApp(getMessaging(), () => {
    openFromNotification();
  });
}

/** App was launched (from killed state) by tapping a notification -- call once, after the
 * navigation container is ready (see RootNavigator's onReady). */
export async function handleInitialNotification(): Promise<void> {
  const initial = await getInitialNotification(getMessaging());
  if (initial) openFromNotification();
}

// Matches backend-core's Enums/NotificationTypes.cs -- data.type is that enum's .ToString(), the
// only field FirebasePushNotificationBusiness.CreatePayload sends besides title/body. Used only
// to pick an urgent vs routine presentation for the foreground alert below.
const URGENT_NOTIFICATION_TYPES = new Set([
  'Medical',
  'PersonalMedical',
  'Fire',
  'Panic',
  'Assault',
  'SilentPanic',
  'KeypadAssault',
  'Stole',
  'Sabotage',
  'Gas',
  'SilentAlarm',
]);

// FCM never shows a system notification banner for a message that arrives while the app is
// actively in the foreground on either platform -- that's standard OS behavior, not a bug in the
// backend's send. Without this handler, a foreground message was received and silently dropped;
// nothing else in this file ever processed it.
export function subscribeToForegroundMessages(): () => void {
  return onMessage(getMessaging(), async (message) => {
    const title = message.notification?.title ?? '';
    const body = message.notification?.body ?? '';
    if (!title && !body) return;
    const urgent = URGENT_NOTIFICATION_TYPES.has(message.data?.type as string);
    if (urgent) {
      // Alert.alert on its own is silent, and it can also end up behind whatever modal is
      // already on screen -- after a panic/emergency trigger that is the location share sheet,
      // opened a moment earlier by the same action (see HomeScreen's runTrigger). So an alarm
      // arriving while the app was open produced nothing the user could notice, which is the
      // client's issue #17. Vibration is built into React Native, needs no extra native module,
      // and fires regardless of what is covering the screen.
      //
      // This is NOT the full fix. The siren sound the client expects comes from the Android
      // notification channel (my_channel_id + res/raw/alert.mp3, see MainApplication.kt), and
      // the OS deliberately does not raise a channel notification while the app is foregrounded.
      // Playing alert.mp3 from JS needs an audio library this project does not depend on yet.
      Vibration.vibrate([0, 600, 300, 600, 300, 600]);
    }
    Alert.alert(urgent ? `🚨 ${title}` : title, body);
  });
}

// Registered from index.js, outside the React tree, per Firebase's own setup requirement -- it
// must be set before the app component renders, not from inside a React effect, or messages
// received while the app is fully killed won't be handled. Data-only handling: the payload's
// "notification" block (not "data") is what makes Android/iOS show a system notification while
// backgrounded, and that happens natively without this handler running at all.
export function registerBackgroundHandler(): void {
  setBackgroundMessageHandler(getMessaging(), async () => {});
}
