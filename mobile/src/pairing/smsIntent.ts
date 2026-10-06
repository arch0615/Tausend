import { Linking, Platform } from 'react-native';

// Opens the phone's own SMS app with the panel's number and a command pre-filled -- the user
// still has to tap Send, matching the previous app's explicit `intent: 'INTENT'` choice (as
// opposed to a plugin that sends silently) so the user always sees exactly what's being sent to
// their alarm panel. iOS and Android use different query-string separators for the sms: scheme.
export async function openSmsComposer(phoneNumber: string, body: string): Promise<boolean> {
  const separator = Platform.OS === 'ios' ? '&' : '?';
  const url = `sms:${phoneNumber}${separator}body=${encodeURIComponent(body)}`;
  const canOpen = await Linking.canOpenURL(url);
  if (!canOpen) return false;
  await Linking.openURL(url);
  return true;
}
