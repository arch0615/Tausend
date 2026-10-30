import { apiPost } from './client';
import type { BaseResponse } from './types';

// Idempotent upsert server-side (CreateDeviceToken.sql moves the token off any other account it
// was previously registered to, or updates OS if it already belongs to this one) -- safe to call
// on every login and again whenever the push library reports a refreshed token. OS casing doesn't
// matter to the backend (PhoneOSHelper.ToPhoneType uppercases before comparing), but it must
// spell "android"/"ios" or notification building throws server-side for that device.
export function registerDeviceToken(token: string, os: 'Android' | 'iOS'): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AccountService/CreateAccountDeviceToken', { OS: os, Token: token });
}

// "Apagar notificaciones" (client, 27 Oct): mutes push for this one panel on this one phone only.
// Scoped by the phone's own FCM token, not the account, so this never
// affects any other phone or account that also has the panel linked. Backend-enforced (the server
// skips sending to a muted token/identifier pair entirely) rather than a local-only toggle,
// because a background/killed-app notification is shown by the OS before any app code runs --
// nothing on the phone itself could have intercepted it to suppress it after the fact.
export interface NotificationMuteResponse extends BaseResponse {
  Muted: boolean;
}

export function setNotificationMute(identifier: string, deviceToken: string, muted: boolean): Promise<BaseResponse> {
  return apiPost<BaseResponse>('NotificationService/SetNotificationMute', {
    Identifier: identifier,
    DeviceToken: deviceToken,
    Muted: muted,
  });
}

export function getNotificationMute(identifier: string, deviceToken: string): Promise<NotificationMuteResponse> {
  return apiPost<NotificationMuteResponse>('NotificationService/GetNotificationMute', {
    Identifier: identifier,
    DeviceToken: deviceToken,
  });
}
